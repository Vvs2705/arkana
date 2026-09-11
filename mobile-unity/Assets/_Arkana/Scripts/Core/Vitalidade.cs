using UnityEngine;

namespace Arkana.Core
{
    /// <summary>
    /// Estado puro de vida/escudo de UM pawn. Quem ESCREVE dano aqui e' so' Core.Combat;
    /// o resto observa pelo Bus. Setters publicos existem para o Combat e para os testes, nao para atalho.
    /// </summary>
    public sealed class Vitalidade
    {
        public float Hp { get; set; }
        public float HpMax { get; set; }
        public float Escudo { get; set; }
        public float EscudoMax { get; private set; }
        /// <summary>1..4 (Balance.Escudo).</summary>
        public int Nivel { get; private set; }
        /// <summary>Dano causado ACUMULADO em mago inimigo — o combustivel da evolucao.</summary>
        public float DanoCausado { get; set; }
        public bool Viva => Hp > 0f;

        /// <summary>Nasce com escudo nivel 1 cheio (GDD §5: todo mago cai com N1).</summary>
        public Vitalidade(float hpMax)
        {
            HpMax = hpMax;
            Reset();
        }

        public void Reset()
        {
            Hp = HpMax;
            DanoCausado = 0f;
            Nivel = 1;
            EscudoMax = Balance.Escudo.Niveis[0];
            Escudo = EscudoMax;
        }

        /// <summary>
        /// Escudo EVOLUTIVO (GDD §5): DanoCausado sobe o nivel pela tabela Balance.Escudo.Evoluir.
        /// Subir entrega SO' a capacidade NOVA — quem estava com 3 de 50 fica com 28 de 75, nunca 75 cheio
        /// (senao "subir de nivel" viraria cura gratis no meio da luta). Devolve true se o nivel mudou.
        /// </summary>
        public bool Evoluir()
        {
            float[] passos = Balance.Escudo.Evoluir;
            float[] niveis = Balance.Escudo.Niveis;
            int novo = Nivel;
            while (novo < passos.Length && DanoCausado >= passos[novo]) novo++;
            if (novo == Nivel) return false;
            Escudo += niveis[novo - 1] - niveis[Nivel - 1];
            Nivel = novo;
            EscudoMax = niveis[novo - 1];
            return true;
        }

        /// <summary>Cura grampeada em HpMax. NaN/&lt;=0 nao faz nada (mesma guarda do dano).</summary>
        public void Curar(float quanto)
        {
            if (!(quanto > 0f)) return;
            Hp = Mathf.Min(Hp + quanto, HpMax);
        }
    }
}
