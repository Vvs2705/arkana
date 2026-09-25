using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arkana.Core
{
    /// <summary>
    /// PONTO UNICO de dano (regra provada do projeto). Ninguem toca Hp nem Escudo fora daqui; UI/telemetria
    /// OBSERVAM pelo Bus. A ORDEM (GDD §5 + DANO.md §3.2): 1) escudo absorve com o fator Esc do elemento;
    /// 2) o excedente do MESMO tiro TRANSBORDA para a vida com o fator Vida; 3) DoT pula o passo 1.
    /// </summary>
    public static class Combat
    {
        /// <summary>
        /// DERRUBADO (Gameplay.Derrubado): quem tem esquadrao CAI em vez de morrer. Devolve true = interceptou
        /// (nao emite EntityDied). null = morre mesmo. Gameplay instala; Reset() limpa.
        /// </summary>
        public static Func<IEntidade, IEntidade, bool> InterceptarMorte;

        // ------------------------------------------------------------------ TIMES
        /// <summary>O time do humano (e do parceiro bot dele no modo DUPLA).</summary>
        public const int TIME_DO_PLAYER = 0;
        // A cena registra (0..n). Quem nunca foi registrado ganha, na 1a pergunta, um time NEGATIVO so' dele — o FFA de hoje
        // e os fakes de teste, que nao registram nada; negativo nunca colide com time da cena.
        private static readonly Dictionary<IEntidade, int> _times = new Dictionary<IEntidade, int>();
        private static int _avulso;

        /// <summary>A cena (Main) chama no nascimento — DEPOIS do Partida.Iniciar, que chama Reset(). Reset() limpa o registro.</summary>
        public static void DefinirTime(IEntidade e, int time) { if (e != null) _times[e] = time; }

        /// <summary>
        /// Registrado → o registrado. Sem registro → EhPlayer ? TIME_DO_PLAYER : um time UNICO daquela entidade (cada um
        /// sozinho: compativel com os testes e fakes que ja' existem). EhPlayer so' decide isto; "e' aliado?" e' MesmoTime.
        /// </summary>
        public static int TimeDe(IEntidade e)
        {
            if (e == null) return int.MinValue;   // ninguem
            int t;
            if (_times.TryGetValue(e, out t)) return t;
            if (e.EhPlayer) return TIME_DO_PLAYER;
            t = --_avulso;
            _times[e] = t;
            return t;
        }

        /// <summary>A UNICA pergunta "e' aliado?" do jogo (kits, Derrubado, Partida, anti-farm, bots). null → false.</summary>
        public static bool MesmoTime(IEntidade a, IEntidade b) => a != null && b != null && (a == b || TimeDe(a) == TimeDe(b));

        /// <summary>Janela de DoT por alvo: quanto ja' foi aplicado neste tique (o teto somado mora aqui).</summary>
        private sealed class JanelaDot { public float Tempo; public float Aplicado; }
        // Alvo morto sai do dicionario — sem isso vaza uma entrada por bot por partida (cicatriz do Godot).
        private static readonly Dictionary<IEntidade, JanelaDot> _dot = new Dictionary<IEntidade, JanelaDot>();

        /// <summary>
        /// Devolve o dano EFETIVO (escudo + vida); 0 = nao passou e NADA foi emitido. `fonte` null = terreno/ambiente.
        /// `forte` e' o acerto forte do terreno (Bus.TerrainHit) — o Combat nao escala por ele; quem usa e' o chamador.
        /// </summary>
        public static float AplicarDano(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool forte = false, bool ignoraEscudo = false)
        {
            if (alvo == null || alvo.Vital == null) return 0f;
            if (!(dano > 0f)) return 0f;            // barra NaN, zero e negativo de uma vez so'
            Vitalidade v = alvo.Vital;
            if (!v.Viva) return 0f;                 // ja' morto: nada de dano nem sinal duplicado

            Balance.PerfilElemento p = Balance.Perfil(el);
            float noEscudo = 0f;
            float restante = dano;
            bool pula = ignoraEscudo && Balance.Dot.IgnoraEscudo;
            if (!pula && v.Escudo > 0f)
            {
                float pedido = dano * p.Esc;        // quanto ESTE elemento morde
                noEscudo = Mathf.Min(pedido, v.Escudo);
                v.Escudo -= noEscudo;
                // TRANSBORDO: a sobra volta a dano CRU antes de virar vida.
                restante = Balance.Escudo.Transbordo ? (pedido - noEscudo) / Mathf.Max(p.Esc, 0.001f) : 0f;
                if (v.Escudo <= 0f)
                {
                    v.Escudo = 0f;
                    Bus.EmitShieldBroken(alvo);
                }
                Bus.EmitShieldChanged(alvo, v.Escudo, v.EscudoMax, v.Nivel);
            }
            float naVida = 0f;
            if (restante > 0f)
            {
                // Sem inflar overkill: o efetivo e' o que a barra perdeu de verdade
                // (senao um tiro de 200 numa vida de 10 creditaria 200 para a evolucao).
                naVida = Mathf.Min(restante * p.Vida, v.Hp);
                v.Hp -= naVida;
            }
            float efetivo = noEscudo + naVida;
            if (!(efetivo > 0f)) return 0f;

            Creditar(fonte, alvo, efetivo);
            Bus.EmitDamageApplied(alvo, efetivo, el, fonte, noEscudo > 0f);
            if (naVida > 0f && alvo.EhPlayer) Bus.EmitHealthChanged(v.Hp, v.HpMax);
            if (v.Hp <= 0f)
            {
                v.Hp = 0f;
                _dot.Remove(alvo);
                bool intercepta = InterceptarMorte != null && InterceptarMorte(alvo, fonte);
                if (!intercepta) Bus.EmitEntityDied(alvo);
            }
            return efetivo;
        }

        /// <summary>
        /// MORTE NO VAZIO (Diretor, 25/09: a ilha flutua, sem mar — quem cai da borda morre): direta, sem escudo, sem cair
        /// derrubado (nao ha' chao para rastejar nem quem reerga la' embaixo). Quem reanima depois e' o sistema de reviver.
        /// </summary>
        public static void MorrerNoVazio(IEntidade alvo)
        {
            if (alvo == null || alvo.Vital == null || !alvo.Vital.Viva) return;
            Vitalidade v = alvo.Vital;
            v.Escudo = 0f; v.Hp = 0f;
            _dot.Remove(alvo);
            if (alvo.EhPlayer) Bus.EmitHealthChanged(0f, v.HpMax);
            Bus.EmitEntityDied(alvo);
        }

        /// <summary>
        /// DoT (queimadura, terreno, nevoa): direto na VIDA e com TETO SOMADO por alvo (DANO.md §3.5).
        /// O orcamento da janela e' TetoDps x Tick; a janela fecha em TickDot(dt), que o dono do loop
        /// (Gameplay) chama UMA vez por frame. Sem TickDot o orcamento nunca renova e o DoT para — de proposito
        /// barulhento, nao silencioso. `tipo` ("burn"|"electric"|...) so' escolhe o elemento do evento.
        /// </summary>
        public static float AplicarDot(IEntidade alvo, float dps, float dt, string tipo, IEntidade fonte)
        {
            if (alvo == null || alvo.Vital == null || !alvo.Vital.Viva) return 0f;
            if (!(dps > 0f) || !(dt > 0f)) return 0f;
            JanelaDot j;
            if (!_dot.TryGetValue(alvo, out j))
            {
                j = new JanelaDot();
                _dot[alvo] = j;
            }
            // dt maior que o tique (teste, engasgo) alarga o orcamento na mesma proporcao.
            float teto = Balance.Dot.TetoDps * Mathf.Max(Balance.Dot.Tick, dt);
            float dano = Mathf.Min(dps * dt, teto - j.Aplicado);
            if (!(dano > 0f)) return 0f;
            float efetivo = AplicarDano(alvo, dano, ElementoDoDot(tipo), fonte, false, true);
            j.Aplicado += efetivo;
            return efetivo;
        }

        /// <summary>Relogio das janelas de DoT. Chamar UMA vez por frame (o dono do loop de partida).</summary>
        public static void TickDot(float dt)
        {
            if (!(dt > 0f)) return;
            foreach (KeyValuePair<IEntidade, JanelaDot> kv in _dot)
            {
                JanelaDot j = kv.Value;
                j.Tempo += dt;
                if (j.Tempo >= Balance.Dot.Tick)
                {
                    j.Tempo = 0f;
                    j.Aplicado = 0f;
                }
            }
        }

        /// <summary>m/s do empurrao deste elemento (Combate.Knockback x Perfil.Empurrao).</summary>
        public static float Empurrao(Elemento el) => Balance.Combate.Knockback * Balance.Perfil(el).Empurrao;

        /// <summary>Zera janelas de DoT, o interceptador e o registro de times. Chamar no SetUp de teste e ao trocar de cena.</summary>
        public static void Reset()
        {
            _dot.Clear();
            InterceptarMorte = null;
            _times.Clear();
            _avulso = 0;
        }

        private static Elemento ElementoDoDot(string tipo)
        {
            Elemento el;
            if (Elementos.TryParse(tipo, out el)) return el;
            return tipo == "electric" ? Elemento.Raio : Elemento.Fogo;
        }

        /// <summary>
        /// ANTI-FARM (GDD §5 + DANO.md §3.9): so' conta dano em MAGO INIMIGO. Fora: dano proprio e dano em
        /// aliado. Estrutura (muro, torreta, totem) nao e' IEntidade, entao nem chega aqui — checagem estrutural,
        /// sem lista de excecoes para manter.
        /// </summary>
        private static void Creditar(IEntidade fonte, IEntidade alvo, float dano)
        {
            if (fonte == null || fonte.Vital == null || MesmoTime(fonte, alvo)) return;   // o proprio e o aliado nao contam
            Vitalidade fv = fonte.Vital;
            fv.DanoCausado += dano;
            if (fv.Evoluir()) Bus.EmitShieldChanged(fonte, fv.Escudo, fv.EscudoMax, fv.Nivel);
        }
    }
}
