using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A QUEDA — a abertura de todo battle royale: castelo, salto, queda livre, planeio, pouso.
    /// Ordem do Diretor: durante a queda NAO existe magia, so' o corpo (`PodeConjurar => !NoAr`).
    /// Sem fisica de colisao: a posicao e' escrita direto e a verdade do chao e' `relevo.Altura` — a ilha e'
    /// procedural; quem supoe y = 0 pousa dentro do morro. O Pawn entrega um ChaoComObstaculos: pousa no TOPO da pedra.
    ///
    /// OS NUMEROS (documentados, KNOBs): do castelo a ~120 m do chao, mergulho a ate' 55 m/s (rampa de 40 m/s²)
    /// ate' 90 m acima do solo, depois planeio a 10 m/s de descida e 19 m/s de deriva. Resultado: ~9,4 s de ar
    /// e ~180 m de alcance com o joystick cravado (27 m no mergulho + ~150 m planando) — a metade do raio de
    /// terra (264 m), entao saltar cedo e planar chega tao longe quanto saltar em cima.
    /// </summary>
    public sealed class Queda
    {
        public const string NO_CASTELO = "no_castelo";
        public const string CAINDO = "caindo";
        public const string PLANANDO = "planando";
        public const string POUSOU = "pousou";

        /// <summary>Altura padrao do portao do castelo acima do solo (a casca pode posicionar outra).</summary>
        public const float ALTURA_CASTELO = 120f;
        /// <summary>Velocidade TERMINAL do mergulho. SUBIR = queda seca; DESCER = abertura arrastada.</summary>
        public const float VEL_QUEDA = 55f;
        /// <summary>Rampa ate' a terminal (~4g, de proposito): da' pra SENTIR o vazio do primeiro instante.</summary>
        public const float ACEL_QUEDA = 40f;
        /// <summary>Deriva horizontal mergulhando: mergulhar e' a forma de ir LONGE.</summary>
        public const float VEL_QUEDA_HORIZ = 22f;
        /// <summary>Altura acima do SOLO em que o corpo acha o eixo e vira planeio sozinho.</summary>
        public const float ALTURA_PLANEIO = 90f;
        /// <summary>Descida no planeio: ~5x mais lenta que a queda, para o corpo sentir a transicao sem HUD.</summary>
        public const float VEL_PLANEIO = 10f;
        /// <summary>Deriva planando: menor que a do mergulho de proposito — planar e' ajustar o pouso.</summary>
        public const float VEL_PLANEIO_HORIZ = 19f;
        /// <summary>Freio ao entrar no planeio: 55 -> 10 m/s em ~0,5 s, o "puxao" do corpo achando o eixo.</summary>
        public const float FREIO_PLANEIO = 90f;
        /// <summary>Folga acima do terreno que ja' conta como chao.</summary>
        public const float ALTURA_POUSO = 0.15f;
        /// <summary>Hz do altimetro no Bus. NUNCA por frame.</summary>
        public const float HZ_ALTIMETRO = 10f;

        public string Fase { get; private set; } = NO_CASTELO;
        public Vector3 Pos { get; private set; }
        /// <summary>Velocidade de DESCIDA, positiva (m/s).</summary>
        public float Vy { get; private set; }
        /// <summary>Direcao horizontal do joystick (a casca escreve; y ignorado).</summary>
        public Vector3 Direcao = Vector3.zero;
        public readonly bool EhPlayer;

        private readonly IRelevo _relevo;
        private float _altAcc;
        private bool _mergulhoManual;

        public Queda(IRelevo relevo, Vector3 posInicial, bool ehPlayer = true)
        {
            _relevo = relevo;
            Pos = posInicial;
            EhPlayer = ehPlayer;
            EmitirFase();
        }

        public bool NoAr => Fase != POUSOU;
        /// <summary>Nenhuma magia sai do ar.</summary>
        public bool PodeConjurar => !NoAr;
        public bool Planando => Fase == PLANANDO;
        /// <summary>Altura acima do CHAO (nao do mar): a ilha tem morro e lago.</summary>
        public float Altura => Pos.y - Solo(Pos.x, Pos.z);

        /// <summary>No castelo o corpo viaja pendurado no portao: a casca move por aqui.</summary>
        public void Posicionar(Vector3 p) { if (Fase == NO_CASTELO) Pos = p; }

        /// <summary>Pula. So' na BORDA: chamar duas vezes nao emite duas.</summary>
        public bool Saltar()
        {
            if (Fase != NO_CASTELO) return false;
            Vy = 0f;
            MudarFase(CAINDO);
            return true;
        }

        /// <summary>
        /// Abre/fecha o planeio a mao (abrir cedo viaja mais; fechar mergulha ate' o chao — queda nao causa dano,
        /// DIRECAO.md §3). Fechar desliga o planeio automatico desta queda. Fora do ar nao faz nada.
        /// </summary>
        public bool AlternarPlanar()
        {
            if (Fase == CAINDO) { MudarFase(PLANANDO); return true; }
            if (Fase == PLANANDO) { _mergulhoManual = true; MudarFase(CAINDO); return true; }
            return false;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            switch (Fase)
            {
                case CAINDO:
                    Vy = Mathf.MoveTowards(Vy, VEL_QUEDA, ACEL_QUEDA * dt);
                    Mover(dt, VEL_QUEDA_HORIZ);
                    if (Altura <= ALTURA_POUSO) Pousar();
                    else if (!_mergulhoManual && Altura <= ALTURA_PLANEIO) MudarFase(PLANANDO);  // o freio mora no planar: nada teleporta velocidade
                    break;
                case PLANANDO:
                    Vy = Mathf.MoveTowards(Vy, VEL_PLANEIO, FREIO_PLANEIO * dt);
                    Mover(dt, VEL_PLANEIO_HORIZ);
                    if (Altura <= ALTURA_POUSO) Pousar();
                    break;
                default:
                    return;
            }
            if (EhPlayer && NoAr) Altimetro(dt);
        }

        private void Mover(float dt, float velH)
        {
            Vector3 d = Direcao; d.y = 0f;
            if (d.sqrMagnitude > 1f) d.Normalize();
            Pos += new Vector3(d.x * velH, -Vy, d.z * velH) * dt;
        }

        private void Pousar()
        {
            Pos = new Vector3(Pos.x, Solo(Pos.x, Pos.z), Pos.z);
            Vy = 0f;
            MudarFase(POUSOU);
            if (EhPlayer) Bus.EmitQuedaAltura(0f, 0f);  // a HUD apaga o altimetro
        }

        private float Solo(float x, float z) => _relevo != null ? _relevo.Altura(x, z) : 0f;

        private void MudarFase(string nova)
        {
            Fase = nova;
            EmitirFase();
        }

        private void EmitirFase() { if (EhPlayer) Bus.EmitQuedaFase(Fase); }

        private void Altimetro(float dt)
        {
            _altAcc += dt;
            if (_altAcc < 1f / HZ_ALTIMETRO) return;
            _altAcc = 0f;
            Bus.EmitQuedaAltura(Mathf.Max(Altura, 0f), Vy);
        }
    }
}
