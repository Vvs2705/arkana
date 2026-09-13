using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO CORVUS (04) — Errante. GDD §3, ficha em design/personagens/04-corvus.md, DIRECAO.md §6.
    ///   Passiva  Mundo de Cheiros   — fitas de cheiro dos inimigos (60s) rente ao chao, na cor do elemento de cada um
    ///   Tatica   Uivo de Caca       — raio de 25m: quem estiver ANDANDO fica com o cheiro aceso 4s (so' enquanto anda)
    ///   Suprema  Forma de Lobisomem — 30s de fera: +40%, garras, cura ao abater, faro total (rastros vermelhos a 70m)
    /// OS LIMITADORES SAO PARTE DO KIT: o uivo DENUNCIA ele (os bots ouvem como um disparo) e quem fica PARADO escapa;
    /// na forma SEM MAGIAS (nem tiro, nem tatica — so' garras), silhueta 30% maior e o uivo de transformacao e' ouvido a
    /// 60m; ao desmanchar, 2s ofegante (lento e sem conjurar); as fitas mostram o PASSADO (a ponta fica 2s atras do agora).
    /// ponytail: as garras sao AUTOMATICAS (encostou, rasga): o botao de ataque virar garra pede o motor — Pawn.Atirar
    /// perguntar ao kit por um golpe corpo a corpo. A fera e' o mesmo corpo maior (VisualDosKits); o modelo de lobo e' arte.
    /// </summary>
    public sealed class Corvus : IHabilidade
    {
        public const string FERA = "corvus_fera";
        public const string OFEGANTE = "corvus_ofegante";
        /// <summary>Folga do re-buff da fera: a porta de velocidade e' exclusiva, entao a fera se re-aplica em pedacos.</summary>
        const float PEDACO_BUFF = 0.25f;
        /// <summary>s entre duas buscas de presa da garra armada (a busca na arena aloca: nunca por quadro).</summary>
        const float FAREJA_S = 0.1f;

        /// <summary>A fita de UM inimigo: anel de posicoes com o relogio do kit em que cada uma foi farejada.</summary>
        public sealed class Trilha
        {
            public readonly Vector3[] Pts;
            public readonly float[] T;
            public int N, Prox;
            public EfeitoVisual Visual;
            public Trilha(int n) { Pts = new Vector3[n]; T = new float[n]; }
            public void Por(Vector3 p, float t) { Pts[Prox] = p; T[Prox] = t; Prox = (Prox + 1) % Pts.Length; if (N < Pts.Length) N++; }
        }

        /// <summary>Quem o uivo acendeu: o contorno so' aparece enquanto ele ANDA (Visual.Raio 0 = parado).</summary>
        public sealed class Aceso
        {
            public IEntidade Alvo;
            public Vector3 Antes;
            public EfeitoVisual Visual;
        }

        private readonly Dictionary<IEntidade, Trilha> _trilhas = new Dictionary<IEntidade, Trilha>();
        private readonly List<IEntidade> _ouvintes = new List<IEntidade>();
        private readonly List<Vector3> _ouvintesPos = new List<Vector3>();
        private readonly List<Aceso> _acesos = new List<Aceso>();
        private float _agora, _amostraAcc, _janela, _garraAcc;
        private bool _uivouFera;

        public IReadOnlyList<Aceso> Acesos => _acesos;
        public bool Fareja(IEntidade e) => e != null && _trilhas.ContainsKey(e);

        public void Tick(KitRunner k, float dt)
        {
            _agora += dt;
            bool fera = k.EstadoAtivo(FERA);
            Faro(k, dt, fera);
            Uivo(k, dt);
            // o uivo de TRANSFORMACAO: na borda da telegrafia, ouvido num raio enorme (o aviso do GDD §4.3 com som e forma)
            if (k.Telegrafia > 0f && !_uivouFera)
            {
                _uivouFera = true;
                Uivar(k, k.Dados.Suprema["uivo_raio"], k.Telegrafia, true);
            }
            else if (k.Telegrafia <= 0f) _uivouFera = false;
            if (fera) Fera(k, dt);
        }

        // ------------------------------------------------------------------ passiva

        /// <summary>Mundo de Cheiros: a cada `trilha_amostra` s fareja quem esta' no raio (na fera, o faro dobra).</summary>
        private void Faro(KitRunner k, float dt, bool fera)
        {
            Dictionary<string, float> p = k.Dados.Passiva;
            _amostraAcc += dt;
            if (_amostraAcc < p["trilha_amostra"]) return;
            _amostraAcc = 0f;
            float raio = fera ? k.Dados.Suprema["faro_raio"] : p["trilha_raio"];
            int cap = Mathf.CeilToInt(p["trilha_janela"] / p["trilha_amostra"]) + 1;
            foreach (IEntidade e in k.AlvosPerto(k.Pos, raio, k.Dono))
            {
                if (!Inimigo(k.Dono, e)) continue;
                Trilha t;
                if (!_trilhas.TryGetValue(e, out t)) { t = new Trilha(cap); _trilhas[e] = t; }
                t.Por(e.Pos, _agora);
                if (t.Visual == null || t.Visual.Restante <= 0f) t.Visual = k.Visual("corvus_trilha", e.Pos, e.Pos, 0.22f, p["trilha_janela"], e);
                else t.Visual.Restante = t.Visual.Duracao;   // ainda farejando: a fita segue viva
            }
        }

        /// <summary>
        /// Os pontos da fita de `alvo` que ja' sao PASSADO — mais velhos que `atraso` e mais novos que `janela` — do mais
        /// velho ao mais novo, em `saida` (reaproveitada pelo desenho: zero lixo por quadro). Devolve quantos.
        /// </summary>
        public int PontosDaTrilha(IEntidade alvo, float atraso, float janela, Vector3[] saida)
        {
            Trilha t;
            if (alvo == null || saida == null || !_trilhas.TryGetValue(alvo, out t)) return 0;
            int n = 0;
            for (int i = 0; i < t.N && n < saida.Length; i++)
            {
                int j = (t.Prox - t.N + i + t.Pts.Length) % t.Pts.Length;
                float idade = _agora - t.T[j];
                if (idade > janela || idade < atraso) continue;
                saida[n++] = t.Pts[j];
            }
            return n;
        }

        // ------------------------------------------------------------------ tatica

        /// <summary>Uivo de Caca: guarda quem ouviu e onde estava; `janela` s depois, so' quem ANDOU fica aceso.</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            _ouvintes.Clear();
            _ouvintesPos.Clear();
            foreach (IEntidade e in k.AlvosPerto(k.Pos, t["raio"], k.Dono))
                if (Inimigo(k.Dono, e)) { _ouvintes.Add(e); _ouvintesPos.Add(e.Pos); }
            _janela = t["janela"];
            Uivar(k, t["raio"], 1.2f, t["denuncia"] > 0f);
        }

        /// <summary>O anel do uivo (forma, para quem nao ouve) + a DENUNCIA: os bots escutam como um disparo na posicao dele.</summary>
        private static void Uivar(KitRunner k, float raio, float dur, bool denuncia)
        {
            k.Visual("corvus_uivo", k.Pos, k.Pos, raio, dur);
            if (denuncia) Bus.EmitDisparo(k.Dono, k.Pos);
        }

        private void Uivo(KitRunner k, float dt)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            if (_janela > 0f)
            {
                _janela -= dt;
                if (_janela <= 0f)
                {
                    // PARADO ESCONDE: so' quem andou `movimento_min` m/s na janela fica aceso (disciplina, nao sorte)
                    float minimo = t["movimento_min"] * t["janela"];
                    for (int i = 0; i < _ouvintes.Count; i++)
                    {
                        IEntidade e = _ouvintes[i];
                        if (e.Vital == null || !e.Vital.Viva || Plano(e.Pos, _ouvintesPos[i]) < minimo * minimo) continue;
                        _acesos.Add(new Aceso { Alvo = e, Antes = e.Pos, Visual = k.Visual("corvus_cheiro", e.Pos, e.Pos, 0.6f, t["aceso_dur"], e) });
                    }
                    _ouvintes.Clear();
                    _ouvintesPos.Clear();
                }
            }
            // "contorno visivel ENQUANTO se moverem": parado, o desenho apaga (Raio 0); voltou a andar, acende de novo
            float passo = t["movimento_min"] * dt;
            for (int i = _acesos.Count - 1; i >= 0; i--)
            {
                Aceso a = _acesos[i];
                if (a.Visual.Restante <= 0f) { _acesos.RemoveAt(i); continue; }
                a.Visual.Raio = Plano(a.Alvo.Pos, a.Antes) >= passo * passo ? 0.6f : 0f;
                a.Antes = a.Alvo.Pos;
            }
        }

        // ------------------------------------------------------------------ suprema

        /// <summary>Forma de Lobisomem: so' depois do aviso. O silencio cobre a fera E o ofego num relogio so' (chip continuo).</summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            k.LigarEstado(FERA, s["duracao"]);
            k.BuffVelocidade(s["buff_vel"], PEDACO_BUFF);
            k.Silenciar(s["duracao"] + s["ofegante_dur"]);   // SEM MAGIAS (GDD §4.4): nem tiro, nem tatica — so' garras
            k.Visual("corvus_penas", k.Pos, k.Pos, 1.2f, 1.2f);   // o corpo estoura em penas e nevoa e se REMONTA
            _garraAcc = 0f;
        }

        private void Fera(KitRunner k, float dt)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            // a porta de velocidade e' EXCLUSIVA: lentidao de fora SUBSTITUI o +40% ate' passar; passou, a fera volta a correr
            if (Efeitos.De(k.Dono).SlowLeft <= 0f) k.BuffVelocidade(s["buff_vel"], PEDACO_BUFF);
            _garraAcc += dt;
            if (_garraAcc < s["garra_cadencia"]) return;
            IEntidade alvo = null;
            float melhor = float.MaxValue;
            foreach (IEntidade e in k.AlvosPerto(k.Pos, s["garra_alcance"], k.Dono))
            {
                if (!Inimigo(k.Dono, e) || Efeitos.De(e).IframesLeft > 0f) continue;   // esquiva e' imunidade total
                float d = (e.Pos - k.Pos).sqrMagnitude;
                if (d < melhor) { melhor = d; alvo = e; }
            }
            if (alvo == null) { _garraAcc = s["garra_cadencia"] - FAREJA_S; return; }   // armada: volta a farejar em 0,1s (nao por quadro)
            _garraAcc = 0f;
            // GARRA, nao magia: dano direto no ponto unico (fonte = ele: credita o escudo e a carga). Terra = o golpe bruto.
            if (Combat.AplicarDano(alvo, s["garra_dano"], Elemento.Terra, k.Dono) <= 0f) return;
            k.Visual("corvus_garra", alvo.Pos, k.Pos, 0.8f, 0.3f, alvo);
            if (!alvo.Vital.Viva) k.DevolverDano(s["cura_abate"]);   // cura ao abater
        }

        /// <summary>O PRECO: a fera desmancha com ele de joelhos — 2s lento (o silencio ja' vinha no mesmo relogio).</summary>
        public void EstadoAcabou(KitRunner k, string nome)
        {
            if (nome != FERA) return;
            Dictionary<string, float> s = k.Dados.Suprema;
            k.BuffVelocidade(s["ofegante_vel"], s["ofegante_dur"]);
            k.LigarEstado(OFEGANTE, s["ofegante_dur"]);
        }

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        private static bool Inimigo(IEntidade eu, IEntidade e) => e != null && e != eu && !(eu.EhPlayer && e.EhPlayer);

        private static float Plano(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
