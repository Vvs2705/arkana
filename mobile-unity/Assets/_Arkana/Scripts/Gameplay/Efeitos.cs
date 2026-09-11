using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// EFEITOS SECUNDARIOS E REACOES NO ALVO (GDD §4.6, §14, §16.4; DANO.md §3.3/§3.4).
    /// Todo estado e' EXCLUSIVO por categoria — UM termico (queimando OU molhado) e UM de movimento; a reacao
    /// SUBSTITUI, nunca soma. Aqui so' se mexe no ESTADO DO ALVO: dano e' do Combat, mundo e' do Terrain.
    /// Estado por entidade num registro (IEntidade nao carrega status); `Reset()` nos testes.
    /// </summary>
    public static class Efeitos
    {
        public sealed class EstadoAlvo
        {
            public float BurnDps, BurnLeft, WetLeft, SlowLeft, StunLeft, IframesLeft;
            /// <summary>Fator de lentidao para Velocidade.Produto (status).</summary>
            public float StatusMult = 1f;
            internal float DotAcc;
            internal string Dominante = "";

            /// <summary>Estado dominante para o icone da HUD ("" = nenhum).</summary>
            public string Nome()
            {
                if (StunLeft > 0f) return "stun";
                if (BurnLeft > 0f) return "burn";
                if (WetLeft > 0f) return "wet";
                return "";
            }
        }

        private static readonly Dictionary<IEntidade, EstadoAlvo> _estados = new Dictionary<IEntidade, EstadoAlvo>();

        public static EstadoAlvo De(IEntidade e)
        {
            EstadoAlvo s;
            if (!_estados.TryGetValue(e, out s)) { s = new EstadoAlvo(); _estados[e] = s; }
            return s;
        }

        public static void Reset() => _estados.Clear();

        /// <summary>
        /// Resolve o elemento chegando no alvo e devolve o MULTIPLICADOR DE IMPACTO (conducao = +50% no MESMO
        /// tiro, entao se resolve ANTES do dano). `vizinhos` alimenta o arco de conducao.
        /// </summary>
        public static float Aplicar(IEntidade alvo, Elemento el, float dano, IEntidade fonte, IList<IEntidade> vizinhos = null)
        {
            // So' MAGO tem estado (muro, arvore e loot reagem no Terrain). i-frames = imunidade TOTAL: nem estado.
            if (alvo == null || alvo.Vital == null) return 1f;
            EstadoAlvo s = De(alvo);
            if (s.IframesLeft > 0f || !alvo.Vital.Viva) return 1f;
            bool molhado = s.WetLeft > 0f;
            bool queimando = s.BurnLeft > 0f;
            float mult = 1f;
            switch (el)
            {
                case Elemento.Fogo:
                    if (molhado) Secar(alvo);                       // VAPOR: nao acende, evapora o molhado
                    else Acender(alvo, Balance.Status.BurnDps, Balance.Status.BurnDur);
                    break;
                case Elemento.Agua:
                    // EXTINCAO de graca (a contra-jogada e' barata para o fogo poder doer). HIPOTERMIA: molhado de novo.
                    Molhar(alvo, Balance.Status.WetDur, molhado ? Balance.Status.HypothermiaSlow : Balance.Status.WetSlow);
                    if (molhado) Borda(alvo, s, "frost");
                    break;
                case Elemento.Raio:
                    // CONDUCAO so' em alvo MOLHADO: +50%, atordoamento curto e UM arco para outro molhado perto.
                    if (molhado)
                    {
                        Atordoar(alvo, Balance.Status.ConductStun);
                        Arco(alvo, dano * Balance.Status.ConductMult * Balance.Status.ConductArcMult, fonte, vizinhos);
                        mult = Balance.Status.ConductMult;
                    }
                    break;
                case Elemento.Vento:
                    // ATICAR (triangulo do fogo): o vento sopra a brasa — inclusive o do inimigo.
                    if (queimando) Acender(alvo, Balance.Status.BurnFannedDps, s.BurnLeft + Balance.Status.BurnFannedBonus);
                    break;
            }
            return mult;
        }

        /// <summary>ESTADO TERMICO EXCLUSIVO: acender apaga o molhado. A queimadura refresca, nao empilha.</summary>
        public static void Acender(IEntidade alvo, float dps, float dur)
        {
            EstadoAlvo s = De(alvo);
            s.BurnDps = dps; s.BurnLeft = dur; s.WetLeft = 0f;
            Borda(alvo, s, "burn");
        }

        public static void Molhar(IEntidade alvo, float dur, float slow)
        {
            EstadoAlvo s = De(alvo);
            s.WetLeft = dur; s.BurnLeft = 0f; s.BurnDps = 0f;
            Lentificar(alvo, slow, dur);
            Borda(alvo, s, "wet");
        }

        public static void Secar(IEntidade alvo)
        {
            EstadoAlvo s = De(alvo);
            s.WetLeft = 0f;
            if (s.SlowLeft > 0f) { s.SlowLeft = 0f; s.StatusMult = 1f; }
        }

        /// <summary>ESTADO DE MOVIMENTO EXCLUSIVO: a lentidao nova SUBSTITUI a antiga.</summary>
        public static void Lentificar(IEntidade alvo, float fator, float dur)
        {
            EstadoAlvo s = De(alvo);
            s.StatusMult = fator; s.SlowLeft = dur;
        }

        /// <summary>TETO DO KERNEL: NUNCA acima de StunCap (0.8 s) — acima disso o jogador perde o controle.</summary>
        public static void Atordoar(IEntidade alvo, float s)
        {
            EstadoAlvo e = De(alvo);
            e.StunLeft = Mathf.Min(Mathf.Max(e.StunLeft, s), Balance.Status.StunCap);
            Borda(alvo, e, "stun");
        }

        /// <summary>
        /// RELOGIO DOS ESTADOS + DoT da queimadura, no balde de Balance.Dot.Tick (nunca dano por frame).
        /// Chamar 1x por frame por entidade viva.
        /// </summary>
        public static void Tick(IEntidade alvo, float dt)
        {
            EstadoAlvo s = De(alvo);
            s.StunLeft = Mathf.Max(s.StunLeft - dt, 0f);
            s.WetLeft = Mathf.Max(s.WetLeft - dt, 0f);
            s.IframesLeft = Mathf.Max(s.IframesLeft - dt, 0f);
            if (s.SlowLeft > 0f)
            {
                s.SlowLeft = Mathf.Max(s.SlowLeft - dt, 0f);
                if (s.SlowLeft <= 0f) s.StatusMult = 1f;
            }
            if (s.BurnLeft > 0f)
            {
                s.BurnLeft = Mathf.Max(s.BurnLeft - dt, 0f);
                s.DotAcc += dt;
                if (s.DotAcc >= Balance.Dot.Tick || s.BurnLeft <= 0f)
                {
                    if (alvo.Vital != null && alvo.Vital.Viva)
                        Combat.AplicarDot(alvo, s.BurnDps, s.DotAcc, "burn", null);
                    s.DotAcc = 0f;
                }
                if (s.BurnLeft <= 0f) s.BurnDps = 0f;
            }
            else s.DotAcc = 0f;
            // a borda de SAIDA do estado dominante (a HUD apaga o icone)
            string agora = s.Nome();
            if (agora != s.Dominante) { s.Dominante = agora; }
        }

        /// <summary>StatusAplicado na BORDA: so' quando o estado dominante MUDA para este nome.</summary>
        private static void Borda(IEntidade alvo, EstadoAlvo s, string nome)
        {
            if (s.Dominante == nome) return;
            s.Dominante = nome;
            Bus.EmitStatusAplicado(alvo, nome);
        }

        /// <summary>UM arco por acerto, para o molhado mais proximo dentro de ConductArcM. Sem cadeia.</summary>
        private static void Arco(IEntidade de, float dano, IEntidade fonte, IList<IEntidade> vizinhos)
        {
            if (vizinhos == null) return;
            float r2 = Balance.Status.ConductArcM * Balance.Status.ConductArcM;
            IEntidade perto = null;
            for (int i = 0; i < vizinhos.Count; i++)
            {
                IEntidade o = vizinhos[i];
                if (o == null || o == de || o.Vital == null || !o.Vital.Viva) continue;
                EstadoAlvo so;
                if (!_estados.TryGetValue(o, out so) || so.WetLeft <= 0f) continue;
                float dd = (o.Pos - de.Pos).sqrMagnitude;
                if (dd <= r2) { r2 = dd; perto = o; }
            }
            if (perto != null) Combat.AplicarDano(perto, dano, Elemento.Raio, fonte);
        }
    }
}
