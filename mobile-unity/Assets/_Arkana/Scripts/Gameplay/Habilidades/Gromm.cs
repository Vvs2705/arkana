using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Terrain;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO GROMM (14) — Suporte/Vida. Ficha em design/personagens/14-gromm.md; tempos em DIRECAO.md §4 e §6.
    ///   Passiva  Sangue da Estepe   — curar um ALIADO devolve 30% do curado ao Gromm
    ///   Tatica   Totem das Chuvas   — totem de 8 s: chuva que cura 6 hp/s no raio e APAGA fogo (§14)
    ///   Suprema  Espirito do Trovao — bisao de 30 m em linha: empurra inimigos, derruba muro de pedra, rastro que acelera
    /// OS LIMITADORES: o totem tem 80 de vida (tiro inimigo que passa nele gasta, x Estrutura); a chuva e' Agua no terreno
    /// (Bus.TerrainHit) e o §14 nao pergunta o time: apaga o fogo tatico aliado e faz lama para todos; o bisao avisa 2,5 s
    /// numa linha no chao, o dano e' irrelevante (desloca, nao mata) e no fim o Gromm levanta devagar (2 s lento).
    /// ponytail: solo — sem aliado a passiva dorme (a chuva cura o proprio Gromm, que nao conta como aliado dele); acorda
    /// sozinha no dia do esquadrao. O empurrao fala com o Pawn (EmpurrarCorpo): o IConjurador nao tem o verbo.
    /// </summary>
    public sealed class Gromm : IHabilidade
    {
        public const string CANSADO = "gromm_cansado";

        /// <summary>Casca: empurra o corpo (m/s de knockback). ponytail: o padrao fala com o Pawn; o teste troca. Motor: IEntidade/IConjurador.Empurrar.</summary>
        public static Action<IEntidade, Vector3> EmpurrarCorpo = PadraoEmpurrar;

        private readonly List<Totem> _totens = new List<Totem>();
        private Bisao _bisao;
        private Vector3 _origem, _dir;
        private bool _avisando;

        public IReadOnlyList<Totem> Totens => _totens;
        public Bisao BisaoAtivo => _bisao;

        public void Tick(KitRunner k, float dt)
        {
            // ANTECIPA: ajoelha e o chao treme numa LINHA a frente — a linha e' a do bisao (fixada no comeco do aviso)
            if (k.Telegrafia > 0f)
            {
                if (!_avisando)
                {
                    _origem = k.Pos;
                    _dir = k.Mira();
                    Dictionary<string, float> s = k.Dados.Suprema;
                    k.Visual("gromm_linha", _origem, _origem + _dir * s["comprimento"], s["largura"], k.Telegrafia);
                }
                _avisando = true;
            }
            else _avisando = false;
            IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
            for (int i = _totens.Count - 1; i >= 0; i--)
                if (!_totens[i].Tick(k, dt, vivos)) _totens.RemoveAt(i);
            if (_bisao != null && !_bisao.Tick(k, dt)) _bisao = null;
        }

        /// <summary>Totem das Chuvas: fincado com as duas maos, um passo a frente na mira.</summary>
        public void Tatica(KitRunner k)
        {
            _totens.Add(new Totem(k, k.Pos + k.Mira() * k.Dados.Tatica["distancia"]));
        }

        /// <summary>Espirito do Trovao: o bisao sai da linha que o aviso desenhou.</summary>
        public void Suprema(KitRunner k)
        {
            if (!_avisando) { _origem = k.Pos; _dir = k.Mira(); }
            if (_bisao != null) _bisao.Apagar();
            _bisao = new Bisao(k, _origem, _dir);
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void DanoRecebido(KitRunner k, float quanto) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        /// <summary>O PRECO do trovao: levanta com o peso dos anos.</summary>
        private static void Cansar(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            k.BuffVelocidade(s["cansado_vel"], s["cansado_dur"]);
            k.LigarEstado(CANSADO, s["cansado_dur"]);
        }

        /// <summary>Cura grampeada em HpMax (Vitalidade) e o player fica sabendo. Devolve o que ENTROU (a passiva le' isso).</summary>
        private static float Curar(IEntidade a, float quanto)
        {
            if (a == null || a.Vital == null || !a.Vital.Viva || !(quanto > 0f)) return 0f;
            float antes = a.Vital.Hp;
            a.Vital.Curar(quanto);
            float curou = a.Vital.Hp - antes;
            if (curou > 0f && a.EhPlayer) Bus.EmitHealthChanged(a.Vital.Hp, a.Vital.HpMax);
            return curou;
        }

        /// <summary>O terreno reativo da partida (o teste escreve Partida.Terreno); sem ele, o da cena.</summary>
        private static TerrenoReativo Terreno()
        {
            TerrenoReativo t = Partida.Atual != null ? Partida.Atual.Terreno : null;
            if (t == null && TerrenoReativoBehaviour.Atual != null) t = TerrenoReativoBehaviour.Atual.Terreno;
            return t;
        }

        private static void PadraoEmpurrar(IEntidade e, Vector3 v)
        {
            Pawn p = e as Pawn;
            if (p != null) p.Empurrar(v);
        }

        // ------------------------------------------------------------------ TOTEM
        /// <summary>O totem: cura em baldes de 0,25 s, chove Agua no terreno a cada `apaga_cada` s e apanha tiro inimigo.</summary>
        public sealed class Totem
        {
            public const float TICK = 0.25f;
            /// <summary>m — altura do meio do totem (onde o tiro acerta).</summary>
            public const float ALTURA = 1f;
            public readonly Vector3 Pos;
            public float Vida { get; private set; }
            public float Duracao { get; private set; }
            public readonly EfeitoVisual Visual;
            private readonly IEntidade _dono;
            private readonly Dictionary<string, float> _t;
            private float _acc, _apaga;

            public Totem(KitRunner k, Vector3 pos)
            {
                Pos = pos; _dono = k.Dono; _t = k.Dados.Tatica;
                Vida = _t["vida"]; Duracao = _t["duracao"];
                Visual = k.Visual("gromm_totem", pos, pos, _t["raio"], Duracao);
                Chover();
            }

            /// <summary>A CHUVA APAGA: Agua no terreno (fogo apaga, chao vira lama, muralha de brasas aliada apaga junto).</summary>
            private void Chover()
            {
                _apaga = _t["apaga_cada"];
                Bus.EmitTerrainHit(Elemento.Agua, Pos, false);
            }

            /// <summary>Devolve false quando o totem acabou (prazo ou derrubado).</summary>
            public bool Tick(KitRunner k, float dt, IList<Projetil> vivos)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Visual.Restante = 0f; return false; }
                Apanhar(dt, vivos);
                if (Vida <= 0f)
                {
                    Duracao = 0f;
                    Visual.Restante = 0f;
                    k.Visual("gromm_lascas", Pos + Vector3.up * ALTURA, Pos + Vector3.up * ALTURA, 1.4f, 0.6f);
                    return false;
                }
                _apaga -= dt;
                if (_apaga <= 0f) Chover();
                _acc += dt;
                if (_acc < TICK) return true;
                float q = _t["cura"] * _acc;
                _acc = 0f;
                float r = _t["raio"];
                if ((_dono.Pos - Pos).sqrMagnitude <= r * r) Curar(_dono, q);
                foreach (IEntidade a in k.AlvosPerto(Pos, r, _dono))
                {
                    if (!Combat.MesmoTime(_dono, a)) continue;
                    float curou = Curar(a, q);
                    if (curou > 0f) Curar(_dono, curou * k.Dados.Passiva["cura_propria"]);   // Sangue da Estepe
                }
                return true;
            }

            private void Apanhar(float dt, IList<Projetil> vivos)
            {
                if (vivos == null) return;
                Vector3 meio = Pos + Vector3.up * ALTURA;
                for (int i = 0; i < vivos.Count && Vida > 0f; i++)
                {
                    Projetil p = vivos[i];
                    if (p == null || !p.Vivo || Combat.MesmoTime(p.Atirador, _dono)) continue;
                    if (KitRunner.DistSegmento(meio, p.Pos - p.Dir * (p.Velocidade * dt), p.Pos) > _t["raio_corpo"]) continue;
                    Vida -= p.Dano * Balance.Perfil(p.ElementoDoTiro).Estrutura;   // a regua do muro de terra (§14)
                    p.Impacto(null);   // o tiro morre no totem: o impacto acontece ali
                }
            }
        }

        // ------------------------------------------------------------------ BISAO
        /// <summary>
        /// O espirito-bisao corre a linha do aviso; quem esta' na largura dele e' EMPURRADO uma vez (para a frente e para
        /// fora da linha). Muro de pedra no caminho cai (golpes de Terra ate' a celula soltar — nunca um a mais: Terra em
        /// chao limpo ERGUERIA muro). Depois da corrida o rastro fica `rastro_dur` s acelerando quem e' do time.
        /// </summary>
        public sealed class Bisao
        {
            public const float TICK = 0.1f;
            public readonly Vector3 Origem, Dir;
            public float Percorrido { get; private set; }
            public readonly EfeitoVisual Visual, VisualRastro;
            /// <summary>Quem ja' levou o empurrao (uma vez por corrida).</summary>
            public readonly HashSet<IEntidade> Atingidos = new HashSet<IEntidade>();
            private readonly IEntidade _dono;
            private readonly Dictionary<string, float> _s;
            private float _rastro, _acc;

            public bool Correndo => Percorrido < _s["comprimento"];
            public Vector3 Pos => Origem + Dir * Percorrido;

            public Bisao(KitRunner k, Vector3 origem, Vector3 dir)
            {
                Origem = origem; Dir = dir; _dono = k.Dono; _s = k.Dados.Suprema;
                float corrida = _s["comprimento"] / Mathf.Max(_s["velocidade"], 0.1f);
                Visual = k.Visual("gromm_bisao", origem, origem + dir, _s["largura"], corrida + 0.2f);
                VisualRastro = k.Visual("gromm_rastro", origem, origem, _s["rastro_largura"], corrida + _s["rastro_dur"]);
            }

            public void Apagar() { Percorrido = _s["comprimento"]; _rastro = 0f; Visual.Restante = 0f; VisualRastro.Restante = 0f; }

            /// <summary>Devolve false quando corrida e rastro acabaram.</summary>
            public bool Tick(KitRunner k, float dt)
            {
                if (Correndo)
                {
                    Vector3 a = Pos;
                    Percorrido = Mathf.Min(Percorrido + _s["velocidade"] * dt, _s["comprimento"]);
                    Vector3 b = Pos;
                    Visual.Pos = b;
                    Visual.Pos2 = b + Dir;
                    VisualRastro.Pos2 = b;
                    Atropelar(k, a, b);
                    DerrubarMuros(b);
                    if (!Correndo)
                    {
                        Visual.Restante = 0f;
                        k.Visual("gromm_dissolve", b, b, 2.5f, 0.8f);   // o bisao dissolve em chuva fina
                        _rastro = _s["rastro_dur"];
                        Cansar(k);
                    }
                }
                else
                {
                    _rastro -= dt;
                    if (_rastro <= 0f) { VisualRastro.Restante = 0f; return false; }
                }
                _acc += dt;
                if (_acc >= TICK) { _acc = 0f; Acelerar(k); }
                return true;
            }

            private void Atropelar(KitRunner k, Vector3 a, Vector3 b)
            {
                float largura = _s["largura"];
                foreach (IEntidade alvo in k.AlvosPerto(b, largura + Vector3.Distance(a, b), _dono))
                {
                    if (Combat.MesmoTime(_dono, alvo) || Atingidos.Contains(alvo)) continue;   // empurra INIMIGO
                    if (KitRunner.DistSegmento(alvo.Pos, a, b) > largura) continue;
                    if (Efeitos.De(alvo).IframesLeft > 0f) continue;   // a esquiva fura o bisao (imunidade total)
                    Atingidos.Add(alvo);
                    Vector3 fora = alvo.Pos - (a + Dir * Vector3.Dot(alvo.Pos - a, Dir));
                    fora.y = 0f;
                    Vector3 v = (Dir + (fora.sqrMagnitude > 1e-4f ? fora.normalized * 0.6f : Vector3.zero)).normalized;
                    EmpurrarCorpo?.Invoke(alvo, v * _s["empurrao"]);
                    Combat.AplicarDano(alvo, _s["dano"], Elemento.Raio, _dono);   // irrelevante de proposito: desloca, nao mata
                }
            }

            private void DerrubarMuros(Vector3 p)
            {
                TerrenoReativo t = Terreno();
                if (t == null) return;
                int golpes = (int)_s["muro_golpes"];
                for (int g = 0; g < golpes && t.BloqueiaTiro(p); g++) Bus.EmitTerrainHit(Elemento.Terra, p, true);
            }

            /// <summary>O rastro acelera o time do dono; o Gromm cansado nao pega carona no proprio rastro.</summary>
            private void Acelerar(KitRunner k)
            {
                Vector3 fim = VisualRastro.Pos2;
                float largura = _s["rastro_largura"], vel = _s["rastro_vel"];
                if (!k.EstadoAtivo(CANSADO) && KitRunner.DistSegmento(_dono.Pos, Origem, fim) <= largura) k.BuffVelocidade(vel, TICK * 3f);
                Vector3 meio = (Origem + fim) * 0.5f;
                foreach (IEntidade a in k.AlvosPerto(meio, Vector3.Distance(Origem, fim) * 0.5f + largura, _dono))
                    if (Combat.MesmoTime(_dono, a) && KitRunner.DistSegmento(a.Pos, Origem, fim) <= largura) Efeitos.Lentificar(a, vel, TICK * 3f);
            }
        }
    }
}
