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

        private Hud _hud;
        private bool _mirando;

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
            Pawn.YawAlvo = _mirando ? Camera.Logica.Yaw : (float?)null;
            if (Pawn.Queda.Fase != Queda.NO_CASTELO) Camera.Castelo = null;
        }

        // ---------------------------------------------------------------- verbos

        public void OlharDelta(Vector2 deltaPx) { if (Pawn.Viva) Camera.Logica.Olhar(deltaPx, _mirando); }

        /// <summary>Tap (gesto zero) = mira da camera no reticulo; arrasto = direcao do gesto.</summary>
        public void DisparoMirado(Vector2 gesto)
        {
            _mirando = false;
            Vector3 origem = Pawn.Pos + Vector3.up * Pawn.ALTURA_MAO;
            Vector3 dir = gesto.sqrMagnitude < 0.0001f ? (PontoDeMira() - origem).normalized : Camera.Logica.MiraDoGesto(gesto);
            if (Pawn.Atirar(dir)) Pawn.YawAlvo = Camera.Logica.Yaw;   // o corpo gira para a mira ao disparar
        }

        public void DisparoRapido() => DisparoMirado(Vector2.zero);
        public void Esquivar() { Pawn.Dodge(); }
        public void Pular() { Pawn.Pular(); }
        public void Flutuar(bool on) { Pawn.Flutuar(on); }
        public void Interagir() { Pawn.Pegar(); }
        public void Saltar() { Pawn.Saltar(); }
        public void Planar() { Pawn.Planar(); }
        public void Tatica() { Pawn.UsarTatica(); }
        public void Suprema() { Pawn.UsarSuprema(); }

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

        /// <summary>O ponto que o reticulo olha: raio do olho da camera ate' o alcance da arma, ignorando o proprio corpo.</summary>
        private Vector3 PontoDeMira()
        {
            Vector3 de = Camera.transform.position;
            Vector3 dir = Camera.Logica.Direcao3D;
            float alcance = Pawn.Slot.Spec(Pawn.Slot.ElementoDaLuva ?? Pawn.Elemento).Range + CameraLogica.BRACO;
            Vector3 ate = de + dir * alcance;
            RaycastHit[] hits = Physics.RaycastAll(de, dir, alcance, ~0, QueryTriggerInteraction.Ignore);
            float melhor = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].collider.transform.IsChildOf(Pawn.transform)) continue;
                if (hits[i].distance < melhor) { melhor = hits[i].distance; ate = hits[i].point; }
            }
            return ate;
        }
    }
}
