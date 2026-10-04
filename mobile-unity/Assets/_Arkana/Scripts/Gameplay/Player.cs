using UnityEngine;
using Arkana.Core;
using Arkana.UI;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A casca de INPUT do jogador: intencao (HUD) -> Pawn, mais a camera. Nao le' Input direto (toque e' territorio
    /// da UI). `Ligar(hud)` pendura os eventos da HUD; `Stick`/`Flutuar` sao lidos por frame (o joystick e' estado).
    /// </summary>
    public sealed class Player : MonoBehaviour
    {
        public Pawn Pawn { get; private set; }
        public CameraTerceiraPessoa Camera { get; private set; }
        /// <summary>x direita, y frente (0..1) — a cena/HUD escreve por frame.</summary>
        public Vector2 Stick;

        /// <summary>s que a mira fica ARMADA depois de um tiro por toque: o corpo vira para o reticulo e o tronco torce (onda 15B) em vez
        /// de largar a mira no quadro seguinte e atirar de lado. KNOB: a duracao do gesto de conjurar (PoseMago: 0,55 s).</summary>
        public const float MIRA_APOS_TIRO_S = 0.55f;

        private Hud _hud;
        private bool _mirando;
        private float _miraAteS;

        public static Player Criar(Transform pai, string slug)
        {
            Pawn pawn = Pawn.Criar(pai, slug, true);
            var p = pawn.gameObject.AddComponent<Player>();
            p.Pawn = pawn;
            p.Camera = CameraTerceiraPessoa.Criar(pawn);
            Bus.EmitElementChanged(pawn.Elemento);   // a HUD sincroniza o carrossel no spawn
            return p;
        }

        /// <summary>Pendura a HUD: cada botao vira um verbo daqui. Chamar uma vez por partida, depois de hud.Vincular(Pawn).</summary>
        public void Ligar(Hud hud)
        {
            _hud = hud;
            if (hud == null) return;
            hud.Disparo.Disparou += DisparoMirado;
            hud.Disparo.Cancelou += () => _mirando = false;
            hud.Disparo.MiraMudou += _ => _mirando = true;
            hud.Esquiva.Tocado += Esquivar;
            hud.Salto.Tocado += Salto;
            hud.Pegar.Tocado += Interagir;
            hud.OlharDelta += OlharDelta;
            hud.ElementoEscolhido += EscolherElemento;
            hud.Tatica.Tocado += Tatica;       // o KitRunner do Pawn decide (cooldown, carga, silencio); o botao so' pede
            hud.Suprema.Tocado += Suprema;
            hud.Dupla.PediuCombo += PedirCombo;   // o anel da Sintonia pronto: "COMBO?" ao parceiro
        }

        /// <summary>Embarca no castelo da partida (a camera vai ao coracao dele).</summary>
        public void Embarcar(Castelo castelo)
        {
            Pawn.Embarcar(castelo);
            Camera.Castelo = castelo != null ? castelo.transform : null;
        }

        void Update()
        {
            if (_hud != null)
            {
                Stick = _hud.Joystick.Direcao;
                Flutuar(_hud.SegurandoSalto);
            }
            Pawn.Stick = Stick;
            Pawn.YawCam = Camera.Logica.Yaw;
            Pawn.YawAlvo = _mirando || Time.time < _miraAteS ? Camera.Logica.Yaw : (float?)null;
            if (Pawn.Queda.Fase != Queda.NO_CASTELO) Camera.Castelo = null;
        }

        // ---------------------------------------------------------------- verbos

        public void OlharDelta(Vector2 deltaPx) { if (Pawn.Viva) Camera.Logica.Olhar(EmDp(deltaPx), _mirando); }

        /// <summary>O arrasto do olhar em DP (regra do projeto: o que o dedo sente vive em dp). Em px, o Poco (395 ppi) girava
        /// ~71 graus por cm, 2,5x o calibrado (diagnostico de 24/09); a SENS_BASE foi pensada para 160 dpi.</summary>
        public static Vector2 EmDp(Vector2 px) => px / Dp.Px(1f);

        /// <summary>Tap (gesto zero) = o reticulo; arrasto = o raio da camera girado pelo gesto. Nos dois, o tiro sai da MAO para
        /// o ponto que o raio acha — antes o arrasto saia paralelo a camera e caia ~1,9 m curto e 0,78 m a esquerda.</summary>
        public void DisparoMirado(Vector2 gesto)
        {
            _mirando = false;
            Vector3 origem = Pawn.Pos + Vector3.up * Pawn.ALTURA_MAO;
            Vector3 raio = gesto.sqrMagnitude < 0.0001f ? Camera.Logica.Direcao3D : Camera.Logica.MiraDoGesto(gesto);
            float alcance = Pawn.Slot.Spec(Pawn.Slot.ProximoDisparo(Pawn.Elemento)).Range + CameraLogica.BRACO;
            Vector3 dir = (PontoDoRaio(Camera.transform.position, raio, alcance, origem, Pawn.transform) - origem).normalized;
            if (Pawn.Atirar(dir)) { Pawn.YawAlvo = Camera.Logica.Yaw; _miraAteS = Time.time + MIRA_APOS_TIRO_S; }   // o corpo gira para a mira ao disparar e segura o gesto inteiro
        }

        public void DisparoRapido() => DisparoMirado(Vector2.zero);
        public void Esquivar() { Pawn.Dodge(); }
        public void Pular() { Pawn.Pular(); }
        public void Flutuar(bool on) { Pawn.Flutuar(on); }
        public void Interagir() { Pawn.Pegar(); }
        public void Saltar() { Pawn.Saltar(); }
        public void Planar() { Pawn.Planar(); }
        // O botao do kit MIRA como o disparo: o kit sai para o reticulo (camera), nao para onde o corpo parou. Sem isto, parado
        // depois de girar a camera, a muralha da Pyra nascia atras dela, fora da tela (diag da foto 17 de 12/09).
        public void Tatica() { Pawn.YawAlvo = Camera.Logica.Yaw; Pawn.UsarTatica(); }
        public void Suprema() { Pawn.YawAlvo = Camera.Logica.Yaw; Pawn.UsarSuprema(); }

        /// <summary>O PING DE SINTONIA (GDD §18.7): pede o combo ao parceiro no inimigo SOB A MIRA — sem ninguem na mira, no
        /// ultimo que acertei. Quem responde e prende o foco e' o Ping do parceiro (Bot); sem alvo, a faixa diz o que fazer.</summary>
        public void PedirCombo()
        {
            Partida m = Partida.Atual;
            PingDeSintonia ping = Bot.PingDe(m != null ? m.ParceiroVivo() : null);
            Elemento[] meus = Dupla.ElementosDe(Pawn);
            if (ping == null || _hud == null || !ping.PodePropor(Pawn, meus)) return;
            MarcasLogica marcas = _hud.Marcas.Logica;
            IEntidade alvo = PingDeSintonia.AlvoDoPing(Pawn, marcas.SobAMira, marcas.UltimoAcertado);
            if (!ping.Propor(Pawn, alvo, meus)) _hud.Dupla.Logica.SemAlvo();
        }

        /// <summary>UM botao, tres leituras: no castelo salta; no ar abre/fecha o planeio; no chao pula.</summary>
        public void Salto()
        {
            if (Pawn.Queda.Fase == Queda.NO_CASTELO) Saltar();
            else if (Pawn.Queda.NoAr) Planar();
            else Pular();
        }

        public void EscolherElemento(Elemento e)
        {
            if (e == Pawn.Elemento) return;
            Pawn.Elemento = e;
            Bus.EmitElementChanged(e);
        }

        static readonly RaycastHit[] _hits = new RaycastHit[32];

        /// <summary>
        /// O ponto que um raio da CAMERA olha (`de` -> `dir`) ate' `alcance`: o primeiro colisor que nao seja o proprio corpo
        /// (`ignorar`) nem esteja ENTRE a camera e a mao — um pilar colado no ombro da camera parava o raio antes da mao e o
        /// tiro saia de lado. Sem nada no caminho, o fim do alcance. Sem lixo por tiro (NonAlloc).
        /// </summary>
        public static Vector3 PontoDoRaio(Vector3 de, Vector3 dir, float alcance, Vector3 mao, Transform ignorar)
        {
            float antesDaMao = Mathf.Max(Vector3.Dot(mao - de, dir), 0f);
            Vector3 ate = de + dir * alcance;
            int n = Physics.RaycastNonAlloc(de, dir, _hits, alcance, ~0, QueryTriggerInteraction.Ignore);
            float melhor = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                Collider c = _hits[i].collider;
                if (c == null || (ignorar != null && c.transform.IsChildOf(ignorar)) || _hits[i].distance < antesDaMao) continue;
                if (_hits[i].distance < melhor) { melhor = _hits[i].distance; ate = _hits[i].point; }
            }
            return ate;
        }
    }
}
