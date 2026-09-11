using UnityEngine;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A REGRA DA AGUA (DIRECAO.md §3): lamina no PEITO (1,2 m) = NADAR a 55% no produto unico, e o corpo
    /// FLUTUA na lamina (a gravidade nao puxa ao leito); sair = ENCHARCADO a 80% por 2,5 s; poca no tornozelo
    /// nao e' nado; afogamento nao existe. Quem responde onde ha' agua e' `relevo.SuperficieDaAgua`. Sem ilha, tudo vale 1.0.
    /// `Fator` entra em Velocidade.Produto como terreno — nunca escreve m/s.
    /// </summary>
    public sealed class Agua
    {
        public const float NADO_MULT = 0.55f;
        public const float MOLHADO_MULT = 0.8f;
        public const float MOLHADO_S = 2.5f;
        public const float PEITO = 1.2f;
        /// <summary>Acima disto (m/s) a bracada e' "nadar"; abaixo, "nadar_parado" (boiar).</summary>
        public const float NADO_ANIM_V = 0.5f;

        public bool Nadando { get; private set; }
        public float Fator { get; private set; } = 1f;
        public float MolhadoRestante { get; private set; }
        /// <summary>Y da lamina onde se nada (valido so' com Nadando).</summary>
        public float SuperficieNado { get; private set; }

        private readonly IRelevo _relevo;

        public Agua(IRelevo relevo) { _relevo = relevo; }

        public bool Encharcado => !Nadando && MolhadoRestante > 0f;

        /// <summary>O relogio da agua: decide nadar/molhado e o fator do produto unico. 1x por frame.</summary>
        public void Tick(float dt, Vector3 pos)
        {
            float sup = Superficie(pos);
            bool fundo = sup > Relevo.Seco && (sup - pos.y) >= PEITO;
            if (fundo)
            {
                SuperficieNado = sup;
                Nadando = true;
            }
            else if (Nadando)
            {
                Nadando = false;
                MolhadoRestante = MOLHADO_S;  // roupa encharcada: a saida e' lenta uns segundos
            }
            if (!Nadando) MolhadoRestante = Mathf.Max(MolhadoRestante - dt, 0f);
            Fator = Nadando ? NADO_MULT : (MolhadoRestante > 0f ? MOLHADO_MULT : 1f);
        }

        /// <summary>Nadando, o corpo persegue a lamina no peito (gravidade zero). Devolve a posicao corrigida.</summary>
        public Vector3 Flutuar(Vector3 pos, float dt)
        {
            if (!Nadando) return pos;
            pos.y = Mathf.Lerp(pos.y, SuperficieNado - PEITO, Mathf.Min(6f * dt, 1f));
            return pos;
        }

        /// <summary>Nome da locomocao na agua ("" = nao esta' nadando).</summary>
        public string Locomocao(float velocidadeHorizontal)
        {
            if (!Nadando) return "";
            return velocidadeHorizontal > NADO_ANIM_V ? "nadar" : "nadar_parado";
        }

        private float Superficie(Vector3 pos) =>
            _relevo == null ? Relevo.Seco : _relevo.SuperficieDaAgua(pos.x, pos.z);
    }
}
