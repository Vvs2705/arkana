using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// DERRUBADO E REERGUER — o estado que define o battle royale de esquadrao (GDD §3.7/§3.8, §4.5, §18.6).
    /// VOCABULARIO 10+: ninguem sangra. O mago e' DERRUBADO e ESVAECE — a luz dele se apaga aos poucos.
    ///
    /// O TRUQUE: ao cair a vida NAO fica em zero — vira a RESERVA DE ESVAECIMENTO (Hp = 100, drenando por DoT
    /// pelo ponto unico). A morte de verdade sai do MESMO caminho de sempre (Combat -> EntityDied, uma vez).
    /// Relogio proprio a 4 Hz (TIQUE), so' enquanto alguem esta' caido.
    ///
    /// COSTURA: `Instalar()` pendura `Interceptar` em `Combat.InterceptarMorte` — true = NAO morra.
    /// O dono do loop chama `Combat.TickDot(dt)` por frame, senao o orcamento de DoT (e o esvaecimento) para.
    /// A arena (quem esta' de pe' no esquadrao, quem canaliza) chega por `Derrubado.Arena` (a casca escreve).
    /// </summary>
    public sealed class Derrubado
    {
        // ---------------------------------------------------------------- KNOBS
        /// <summary>Caido ate' apagar. AUMENTAR = resgate vira rotina; DIMINUIR = so' uma morte mais longa.</summary>
        public const float ESVAECER_S = 30f;
        /// <summary>Canalizacao do resgate. AUMENTAR = impossivel no meio da briga; DIMINUIR = o abate perde valor.</summary>
        public const float REERGUER_S = 6f;
        /// <summary>m — precisa CHEGAR no caido.</summary>
        public const float RAIO_M = 2.4f;
        /// <summary>Fator de velocidade do caido (rastejar) — entra em Velocidade.Produto como postura.</summary>
        public const float RASTEJO = 0.35f;
        /// <summary>Volta com vida PARCIAL e ZERO escudo (GDD §3: a fada de revive nao gera escudo).</summary>
        public const float VIDA_REERGUIDO = 0.30f;
        /// <summary>s — mesma cadencia de Balance.Dot.Tick; 0,25 e' exato em binario (o resgate fecha a conta).</summary>
        public const float TIQUE = 0.25f;
        /// <summary>Interrupcao: o progresso DECAI simetrico — nem zera (tudo-ou-nada) nem congela (sem premio).</summary>
        public const float DECAI = 1f;
        /// <summary>SOLO: sem esquadrao derrubado e' morte com passos a mais — entao bot e player solo morrem.</summary>
        public const bool SOLO_DERRUBA = false;

        /// <summary>Os pawns da arena (a casca escreve; o teste tambem). Esquadrao = todos os EhPlayer.</summary>
        public static IList<IEntidade> Arena;
        /// <summary>Proxies sem corpo que reerguem (a Lumen da Vitalis): contam como "de pe'" no esquadrao.</summary>
        public static readonly HashSet<IEntidade> Reanimadores = new HashSet<IEntidade>();

        private static readonly Dictionary<IEntidade, Derrubado> _caidos = new Dictionary<IEntidade, Derrubado>();

        public readonly IEntidade Pawn;
        public IEntidade Causador { get; private set; }
        /// <summary>Canalizacao acumulada em SEGUNDOS (multiplo exato de TIQUE).</summary>
        public float Canal { get; private set; }
        public float MultReerguer { get; private set; } = 1f;
        public bool Caido { get; private set; }
        /// <summary>Quem esta' canalizando AGORA (null = ninguem). A costura do Player barra o disparo dele.</summary>
        public IEntidade Reanimador { get; private set; }

        private float _multLeft;
        private float _acc;

        public Derrubado(IEntidade pawn) { Pawn = pawn; }

        // ------------------------------------------------------------ consultas estaticas

        public static Derrubado De(IEntidade p)
        {
            Derrubado d;
            return p != null && _caidos.TryGetValue(p, out d) ? d : null;
        }

        public static bool Esta(IEntidade p) => De(p) != null;
        /// <summary>Derrubado nao conjura, nao usa tatica/suprema e nao esquiva. Quem nao esta' caido AGE.</summary>
        public static bool PodeAgir(IEntidade p) => !Esta(p);
        /// <summary>Fator de postura para Velocidade.Produto.</summary>
        public static float FatorVelocidade(IEntidade p) => Esta(p) ? RASTEJO : 1f;
        public static float VidaEsvaecer => Balance.Player.Hp;
        public static float DanoTique => VidaEsvaecer / ESVAECER_S * TIQUE;

        public static void Reset()
        {
            _caidos.Clear();
            Reanimadores.Clear();
            Arena = null;
        }

        /// <summary>Liga a costura no ponto unico: o Combat pergunta aqui antes de emitir EntityDied. Chamar no boot da partida.</summary>
        public static void Instalar() { Combat.InterceptarMorte = Interceptar; }

        /// <summary>A COSTURA COM O PONTO UNICO DE DANO. `true` = NAO morra agora, virou derrubado.</summary>
        public static bool Interceptar(IEntidade alvo, IEntidade autor = null)
        {
            if (Esta(alvo)) return false;      // ja' caido: ISTO foi a finalizacao — deixa morrer
            if (!PodeCair(alvo)) return false; // solo/bot: morre como sempre morreu
            new Derrubado(alvo).Cair(autor);
            return true;
        }

        /// <summary>Cai quem TEM quem venha busca-lo.</summary>
        public static bool PodeCair(IEntidade alvo) => alvo != null && alvo.Vital != null && (SOLO_DERRUBA || TemEsquadrao(alvo));

        public static bool TemEsquadrao(IEntidade alvo)
        {
            if (Arena == null) return false;
            for (int i = 0; i < Arena.Count; i++)
            {
                IEntidade n = Arena[i];
                if (n != null && n != alvo && MesmoEsquadrao(n, alvo) && DePe(n)) return true;
            }
            return false;
        }

        /// <summary>Hoje esquadrao = os EhPlayer (mesmo criterio do Combat). Duplas/trios mudam SO' aqui.</summary>
        private static bool MesmoEsquadrao(IEntidade a, IEntidade b) => a.EhPlayer && b.EhPlayer;

        private static bool DePe(IEntidade n) =>
            Reanimadores.Contains(n) || (n.Vital != null && n.Vital.Viva && !Esta(n));

        /// <summary>JARDIM DA AURORA (§3.7): reerguer 50% mais rapido por `dur` segundos.</summary>
        public static void Acelerar(IEntidade alvo, float mult, float dur)
        {
            Derrubado d = De(alvo);
            if (d == null) return;
            d.MultReerguer = Mathf.Max(mult, 0f);
            d._multLeft = Mathf.Max(d._multLeft, dur);
        }

        /// <summary>REERGUER. `por` = quem resgatou (null = kit/roteiro). `frac` = gancho do Ilusionista.</summary>
        public static bool Reerguer(IEntidade alvo, IEntidade por = null, float frac = VIDA_REERGUIDO)
        {
            Derrubado d = De(alvo);
            if (d == null) return false;
            d.Sair();
            Vitalidade v = alvo.Vital;
            v.Hp = Mathf.Max(v.HpMax * frac, 1f);
            // ZERO ESCUDO (GDD §3). O NIVEL fica: e' progressao de dano causado, nao um bem que a queda confisca.
            v.Escudo = 0f;
            Bus.EmitShieldChanged(alvo, 0f, v.EscudoMax, v.Nivel);
            if (alvo.EhPlayer) Bus.EmitHealthChanged(v.Hp, v.HpMax);
            Bus.EmitEntityReerguida(alvo, por);
            return true;
        }

        // --------------------------------------------------------------- ciclo de vida

        /// <summary>Entra no estado. Reserva de esvaecimento = vida cheia; sem escudo; HUD avisada na hora.</summary>
        public Derrubado Cair(IEntidade causador)
        {
            if (Caido || Pawn == null || Pawn.Vital == null) return this;
            Derrubado antigo = De(Pawn);
            if (antigo != null) return antigo;
            Caido = true;
            Causador = causador;
            _caidos[Pawn] = this;
            Vitalidade v = Pawn.Vital;
            v.Hp = VidaEsvaecer;
            // Derrubado nao tem escudo: caido por DoT (que ignora escudo) ficaria com 50 intactos.
            if (v.Escudo > 0f)
            {
                v.Escudo = 0f;
                Bus.EmitShieldBroken(Pawn);
            }
            Bus.EmitShieldChanged(Pawn, 0f, v.EscudoMax, v.Nivel);
            // Limpeza por SINAL, nao pela costura (Bus.Reset() zera ouvintes: reassina idempotente a cada queda).
            Bus.EntityDied -= AoMorrer;
            Bus.EntityDied += AoMorrer;
            Bus.EmitEntityDerrubada(Pawn, causador);
            Avisar();
            return this;
        }

        /// <summary>Um passo de relogio: acumula ate' TIQUE (4 Hz) e roda o tique.</summary>
        public void Tick(float dt)
        {
            if (!Caido) return;
            _acc += dt;
            while (Caido && _acc >= TIQUE - 1e-5f)
            {
                _acc -= TIQUE;
                Tique();
            }
        }

        /// <summary>O UNICO relogio do estado: esvaecimento, resgate e feedback saem daqui.</summary>
        public void Tique()
        {
            if (!Caido) return;
            if (_multLeft > 0f)
            {
                _multLeft = Mathf.Max(_multLeft - TIQUE, 0f);
                if (_multLeft <= 0f) MultReerguer = 1f;
            }
            Reanimador = AcharReanimador();
            Canal = Reanimador != null
                ? Mathf.Min(Canal + TIQUE * MultReerguer, REERGUER_S)
                : Mathf.Max(Canal - TIQUE * DECAI, 0f);
            Avisar();
            if (Canal >= REERGUER_S)
            {
                Reerguer(Pawn, Reanimador);
                return;
            }
            // O ESVAECIMENTO PASSA PELO PONTO UNICO (DoT direto na vida). No ultimo tique fecha a conta EXATA em
            // zero, e a morte sai do mesmo EntityDied de sempre — UMA vez.
            float hp = Pawn.Vital.Hp;
            float quanto = DanoTique;
            if (hp - quanto < 0.001f) quanto = hp;
            Combat.AplicarDot(Pawn, quanto / TIQUE, TIQUE, "esvaecer", Causador);
        }

        public float Esvaecimento => Pawn == null || Pawn.Vital == null ? 0f : Mathf.Clamp01(Pawn.Vital.Hp / VidaEsvaecer);
        public float Progresso => Mathf.Clamp01(Canal / REERGUER_S);

        private static void AoMorrer(IEntidade quem)
        {
            Derrubado d = De(quem);
            if (d != null) d.Sair();
        }

        /// <summary>Desfaz o estado (por reerguer ou por morte). Nao mexe em vida: quem chama sabe o que quer nela.</summary>
        private void Sair()
        {
            Reanimador = null;
            Caido = false;
            _caidos.Remove(Pawn);
        }

        /// <summary>Quem canaliza: o mais proximo do esquadrao, de pe', dentro do raio.</summary>
        private IEntidade AcharReanimador()
        {
            if (Arena == null) return null;
            IEntidade melhor = null;
            float d2 = RAIO_M * RAIO_M;
            for (int i = 0; i < Arena.Count; i++)
            {
                IEntidade n = Arena[i];
                if (n == null || n == Pawn || !MesmoEsquadrao(n, Pawn) || !DePe(n)) continue;
                float dd = (n.Pos - Pawn.Pos).sqrMagnitude;
                if (dd <= d2) { d2 = dd; melhor = n; }
            }
            return melhor;
        }

        /// <summary>FEEDBACK a 4 Hz so' quando o PLAYER esta' envolvido (6 bots caidos seria enxurrada no Bus).</summary>
        private void Avisar()
        {
            if (Pawn.EhPlayer || (Reanimador != null && Reanimador.EhPlayer))
                Bus.EmitDerrubadoProgresso(Pawn, Esvaecimento, Progresso);
        }
    }
}
