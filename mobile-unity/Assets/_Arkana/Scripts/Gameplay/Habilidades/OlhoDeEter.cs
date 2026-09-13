using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO OLHO-DE-ETER (06) — Vidente. GDD §3, ficha em design/personagens/06-olho-de-eter.md, DIRECAO.md §6.
    ///   Passiva  Po de Eter          — inimigo que CONJUROU (tiro no ar) brilha 5s para ele, a ate' 40m
    ///   Tatica   Enxame Perscrutador — linha de 24m: quem o enxame tocar tem a conjuracao INTERROMPIDA e fica revelado 6s
    ///   Suprema  Crisalida           — casulo que eclode no fim do aviso: mariposas pousam em todo inimigo a 30m (6s)
    /// OS LIMITADORES SAO PARTE DO KIT: a passiva so' sente quem CONJUROU (segurar a magia e' a contra-jogada); o enxame
    /// tem 1,4s de atraso (a linha aparece no toque) e tunel estreito; o casulo tem 60 de vida ANTES de eclodir (destruido
    /// = nada eclode); FOGO perto de um marcado queima as mariposas e limpa a marca (§14); ao eclodir ele fica 1s "cego"
    /// (sem po de eter). ponytail: SOLO, "revelado para o time" = revelado para ele (o Visual e' do runner dele); o
    /// esquadrao herda a mesma lista de marcas.
    /// </summary>
    public sealed class OlhoDeEter : IHabilidade
    {
        public const string CEGO = "olho_cego";
        /// <summary>s entre duas leituras (tiros no ar da passiva, passo da cabeca do enxame): nada varre a arena por quadro.</summary>
        const float SENTIR_S = 0.1f;

        /// <summary>O enxame em curso: Atraso > 0 = ainda juntando; depois a cabeca corre de A ate' `comprimento`.</summary>
        public sealed class Enxame
        {
            public Vector3 A, Dir;
            public float Atraso, Cabeca, Acc;
            public readonly List<IEntidade> Tocados = new List<IEntidade>();
        }

        /// <summary>O casulo da Crisalida: nasce no COMECO do aviso (o aviso E' ele pulsando) e pode morrer antes de eclodir.</summary>
        public sealed class Casulo
        {
            public Vector3 Pos;
            public float Vida;
            public EfeitoVisual Visual;
            public bool Inteiro => Vida > 0f;
        }

        private readonly Dictionary<IEntidade, EfeitoVisual> _po = new Dictionary<IEntidade, EfeitoVisual>();
        private readonly Dictionary<IEntidade, EfeitoVisual> _marcas = new Dictionary<IEntidade, EfeitoVisual>();
        private readonly List<IEntidade> _apagar = new List<IEntidade>();
        private Enxame _enxame;
        private Casulo _casulo;
        private float _sentirAcc;

        public Enxame EnxameAtivo => _enxame;
        public Casulo CasuloPlantado => _casulo;
        public bool TemPo(IEntidade e) => Vivo(_po, e);
        public bool Marcado(IEntidade e) => Vivo(_marcas, e);

        public void Tick(KitRunner k, float dt)
        {
            if (!k.EstadoAtivo(CEGO)) Po(k, dt);
            TickEnxame(k, dt);
            if (k.Telegrafia > 0f && _casulo == null)
            {
                Dictionary<string, float> s = k.Dados.Suprema;
                Vector3 p = k.Pos + k.Mira() * s["casulo_dist"];   // a frente dele: tiro no CORPO dele nao racha o casulo
                _casulo = new Casulo { Pos = p, Vida = s["casulo_vida"], Visual = k.Visual("olho_casulo", p, p, s["casulo_raio"], k.Telegrafia) };
            }
            if (_casulo != null && _casulo.Inteiro)
            {
                // tiro que passa pelo casulo e' ENGOLIDO por ele e doi (dano x Estrutura, a regra do muro)
                _casulo.Vida -= TirosNoCasulo(k, _casulo.Pos + Vector3.up * 0.6f, k.Dados.Suprema["casulo_raio"]);
                if (!_casulo.Inteiro) Rachar();
            }
        }

        // ------------------------------------------------------------------ passiva

        /// <summary>
        /// Po de Eter: as mariposas sentem a VIBRACAO da magia — todo tiro inimigo no ar denuncia quem o soltou (le' os
        /// projeteis vivos da arena, sem assinar o Bus: nada vaza quando o corpo morre). So' sente quem CONJUROU.
        /// </summary>
        private void Po(KitRunner k, float dt)
        {
            _sentirAcc += dt;
            if (_sentirAcc < SENTIR_S) return;
            _sentirAcc = 0f;
            IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
            if (vivos == null) return;
            Dictionary<string, float> p = k.Dados.Passiva;
            float r2 = p["raio"] * p["raio"];
            for (int i = 0; i < vivos.Count; i++)
            {
                IEntidade quem = vivos[i] != null ? vivos[i].Atirador : null;
                if (!Inimigo(k.Dono, quem) || quem.Vital == null || !quem.Vital.Viva) continue;
                if ((quem.Pos - k.Pos).sqrMagnitude > r2) continue;
                Renovar(k, _po, quem, "olho_po", p["janela"]);
            }
        }

        // ------------------------------------------------------------------ tatica

        /// <summary>Enxame: a linha inteira aparece NO TOQUE (o atraso de 1,4s e' o aviso — quem ve' o tunel sai dele).</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            Vector3 d = k.Mira();
            _enxame = new Enxame { A = k.Pos, Dir = d, Atraso = t["atraso"] };
            k.Visual("olho_enxame", k.Pos, k.Pos + d * t["comprimento"], t["tunel"], t["atraso"] + t["comprimento"] / t["vel"]);
        }

        /// <summary>Depois do atraso o enxame VIAJA (GDD §4.1): so' quem esta' no tunel quando a cabeca passa e' tocado.</summary>
        private void TickEnxame(KitRunner k, float dt)
        {
            if (_enxame == null) return;
            Dictionary<string, float> t = k.Dados.Tatica;
            if (_enxame.Atraso > 0f) { _enxame.Atraso -= dt; return; }
            // a cabeca anda em passos de 0,1s (3m): a busca na arena aloca, nunca por quadro; o SEGMENTO do passo cobre o vao
            _enxame.Acc += dt;
            if (_enxame.Acc < SENTIR_S) return;
            float antes = _enxame.Cabeca;
            _enxame.Cabeca = Mathf.Min(antes + t["vel"] * _enxame.Acc, t["comprimento"]);
            _enxame.Acc = 0f;
            Vector3 a = _enxame.A + _enxame.Dir * antes, b = _enxame.A + _enxame.Dir * _enxame.Cabeca;
            foreach (IEntidade e in k.AlvosPerto((a + b) * 0.5f, (_enxame.Cabeca - antes) * 0.5f + t["tunel"] + 3f, k.Dono))
            {
                if (!Inimigo(k.Dono, e) || _enxame.Tocados.Contains(e) || DistPlano(e.Pos, a, b) > t["tunel"]) continue;
                _enxame.Tocados.Add(e);
                // INTERROMPE: conjurar no motor e' instantaneo, entao o corte e' `interrompe` s SEM CONJURAR no corpo tocado
                // (o bot sente: Pawn.PodeConjurar). ponytail: canalizacao (bau, reerguer) espera um Interromper() no corpo.
                IConjurador c = e as IConjurador;
                if (c != null) c.AplicarEstado("silencio", t["interrompe"]);
                Renovar(k, _marcas, e, "olho_mariposas", t["revela_dur"]);
            }
            if (_enxame.Cabeca >= t["comprimento"]) _enxame = null;
        }

        // ------------------------------------------------------------------ suprema

        /// <summary>Crisalida: eclode no fim do aviso SE o casulo sobreviveu; mariposas pousam em todo inimigo no raio.</summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            Casulo c = _casulo;
            _casulo = null;
            if (c == null || !c.Inteiro) return;   // destruido antes de eclodir: a Crisalida morre sem mariposa (o preco)
            c.Visual.Restante = 0f;
            float r2 = s["raio"] * s["raio"];
            foreach (IEntidade e in k.AlvosPerto(c.Pos, s["raio"] + 5f, k.Dono))
                if (Inimigo(k.Dono, e) && Plano(e.Pos, c.Pos) <= r2) Renovar(k, _marcas, e, "olho_mariposas", s["revela_dur"]);
            k.Visual("olho_eclosao", c.Pos, c.Pos, s["raio"], 1.6f);
            // ENCERRA: a traducao vibracao->luz satura — 1s "cego" (sem po de eter; as mariposas dele somem do corpo)
            k.LigarEstado(CEGO, s["cego_dur"]);
        }

        private void Rachar()
        {
            _casulo.Visual.Restante = 0f;   // o desenho some; o casulo morto fica ate' a telegrafia acabar (nao replanta)
        }

        /// <summary>
        /// Impacto perto do CASULO racha (dano do elemento x Estrutura); FOGO perto de um MARCADO queima as mariposas
        /// pousadas e limpa a marca (§14 — o kit nao decide o mundo, so' escuta o TerrainHit que todo impacto publica).
        /// </summary>
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte)
        {
            if (_casulo != null && _casulo.Inteiro && (pos - _casulo.Pos).sqrMagnitude <= Sq(k.Dados.Suprema["casulo_raio"]))
            {
                Balance.PerfilElemento p = Balance.Perfil(el);
                _casulo.Vida -= p.Dmg * p.Estrutura;
                if (!_casulo.Inteiro) Rachar();
            }
            if (el != Elemento.Fogo) return;
            float r2 = Sq(k.Dados.Tatica["fogo_raio"]);
            _apagar.Clear();
            foreach (KeyValuePair<IEntidade, EfeitoVisual> kv in _marcas)
                if (kv.Value.Restante > 0f && (kv.Key.Pos - pos).sqrMagnitude <= r2) _apagar.Add(kv.Key);
            for (int i = 0; i < _apagar.Count; i++)
            {
                _marcas[_apagar[i]].Restante = 0f;
                _marcas.Remove(_apagar[i]);
            }
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void DanoRecebido(KitRunner k, float quanto) { }

        // ------------------------------------------------------------------ apoio

        /// <summary>Uma marca por alvo: viva, renova o relogio; morta (o runner ja' recolheu), nasce outra.</summary>
        private static void Renovar(KitRunner k, Dictionary<IEntidade, EfeitoVisual> mapa, IEntidade e, string tipo, float dur)
        {
            EfeitoVisual v;
            if (mapa.TryGetValue(e, out v) && v.Restante > 0f) { v.Duracao = dur; v.Restante = dur; return; }
            mapa[e] = k.Visual(tipo, e.Pos, e.Pos, 0.6f, dur, e);
        }

        private static bool Vivo(Dictionary<IEntidade, EfeitoVisual> mapa, IEntidade e)
        {
            EfeitoVisual v;
            return e != null && mapa.TryGetValue(e, out v) && v.Restante > 0f;
        }

        /// <summary>Tiro inimigo a `raio` do casulo e' engolido por ele. Devolve o dano x Estrutura somado.</summary>
        private static float TirosNoCasulo(KitRunner k, Vector3 centro, float raio)
        {
            IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
            if (vivos == null) return 0f;
            float dano = 0f, r2 = raio * raio;
            for (int i = vivos.Count - 1; i >= 0; i--)
            {
                Projetil p = vivos[i];
                if (p == null || !p.Vivo || !Inimigo(k.Dono, p.Atirador) || (p.Pos - centro).sqrMagnitude > r2) continue;
                vivos.RemoveAt(i);
                if (k.AoAbsorver != null) k.AoAbsorver(p);
                dano += p.Dano * Balance.Perfil(p.ElementoDoTiro).Estrutura;
            }
            return dano;
        }

        private static bool Inimigo(IEntidade eu, IEntidade e) => e != null && e != eu && !(eu.EhPlayer && e.EhPlayer);

        private static float Sq(float x) => x * x;

        private static float Plano(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Distancia NO PLANO ao segmento a-b (o tunel e' reto no chao; morro nao tira ninguem dele).</summary>
        private static float DistPlano(Vector3 p, Vector3 a, Vector3 b)
        {
            p.y = 0f; a.y = 0f; b.y = 0f;
            return KitRunner.DistSegmento(p, a, b);
        }
    }
}
