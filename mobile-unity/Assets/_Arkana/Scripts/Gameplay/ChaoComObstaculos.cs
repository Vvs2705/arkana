using UnityEngine;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O relevo que a QUEDA enxerga: o chao da ilha OU o topo do que estiver em cima dele (rocha, arvore, ruina, peca do
    /// kit, muro). A Queda e' pura e so' pergunta `Altura(x, z)`; ate' 11/09 a resposta era o terreno nu ("pouso sempre
    /// no terreno, sem telhado") e todo mago que caia sobre uma pedra pousava DENTRO dela — a foto (seed 3103) mostrou a
    /// camera no avesso de uma arvore carbonizada. Corpos de mago (CharacterController) e gatilhos nao contam: ninguem
    /// pousa na cabeca do outro.
    /// </summary>
    public sealed class ChaoComObstaculos : IRelevo
    {
        /// <summary>Quanto acima do terreno a sonda comeca (m). O pilar do kit tem 28 m; 60 cobre tudo o que se planta.</summary>
        public const float AlturaSonda = 60f;
        /// <summary>Topo abaixo disto (m acima do terreno) e' o proprio chao ou capim: nao vira telhado.</summary>
        const float Folga = 0.05f;

        readonly IRelevo _relevo;
        static readonly RaycastHit[] _hits = new RaycastHit[16];

        public ChaoComObstaculos(IRelevo relevo) { _relevo = relevo; }

        public float RaioTerra => _relevo != null ? _relevo.RaioTerra : 0f;
        public Poi[] Pois => _relevo != null ? _relevo.Pois : new Poi[0];
        public float SuperficieDaAgua(float x, float z) => _relevo != null ? _relevo.SuperficieDaAgua(x, z) : Relevo.Seco;
        public bool PodePousar(float x, float z) => _relevo == null || _relevo.PodePousar(x, z);

        public float Altura(float x, float z)
        {
            float h = _relevo != null ? _relevo.Altura(x, z) : 0f;
            return Topo(x, z, h);
        }

        /// <summary>O mais alto entre o terreno `h` e o topo dos colisores solidos em (x, z), ate' AlturaSonda acima.</summary>
        public static float Topo(float x, float z, float h)
        {
            var de = new Vector3(x, h + AlturaSonda, z);
            // Verso conta: malha importada com a volta trocada (Meshy) deixaria o raio atravessar o topo da peca.
            bool verso = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            int n = Physics.RaycastNonAlloc(de, Vector3.down, _hits, AlturaSonda + 1f, ~0, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = verso;
            float topo = h;
            for (int i = 0; i < n; i++)
            {
                Collider c = _hits[i].collider;
                if (c == null || c is CharacterController) continue;   // corpo de mago nao e' chao
                float y = _hits[i].point.y;
                if (y > topo + Folga) topo = y;
            }
            return topo;
        }
    }
}
