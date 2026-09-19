using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Terrain;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA MARIS (15) — Longa distancia/Controle. Ficha em design/personagens/15-maris.md; tempos em DIRECAO.md §4 e §6.
    ///   Passiva  Mare Viva   — sobre agua (lago liquido, poca/lama, a propria Mare Cheia) regenera 3 hp/s
    ///   Tatica   Onda Prisao — esfera LENTA; quem ela toca e' suspenso numa coluna d'agua 1,2 s (ainda conjura) e molhado
    ///   Suprema  Mare Cheia  — 9 m de agua por 10 s: o time desliza (+20%), o inimigo afunda (-20%), todo mundo molhado
    /// OS LIMITADORES: a esfera e' lenta e visivel (a esquiva fura, i-frames); a suspensao nao silencia; a Mare CONDUZ
    /// raio — qualquer raio que cai dentro (tiro, fio da Tessa) eletrifica a area inteira por 3 s com o dps do lago
    /// eletrico (Balance.Terrain), ELA inclusive (§14, dois gumes maximo); e no fim a roupa encharcada deixa 2 s lenta.
    /// Molhar e' Efeitos.Molhar (a regra termica exclusiva: agua apaga a queimadura) — o raio direto no molhado conduz.
    /// ponytail: "a mana recarrega 25% mais rapido" e' do MOTOR — nenhum kit escreve Mana (lei do KitRunner). E a Mare nao
    /// muda o terreno (celula nao vira agua): quem conduz e' o proprio kit; o fogo do chao debaixo dela segue aceso.
    /// </summary>
    public sealed class Maris : IHabilidade
    {
        public const string ENCHARCADA = "maris_encharcada";
        public const float TICK = 0.25f;

        private readonly List<Esfera> _esferas = new List<Esfera>();
        private Mare _mare;
        private EfeitoVisual _viva;
        private Vector3 _centro;
        private bool _avisando;
        private float _regenAcc;

        public bool SobreAgua { get; private set; }
        public IReadOnlyList<Esfera> Esferas => _esferas;
        public Mare MareAtiva => _mare;

        public void Tick(KitRunner k, float dt)
        {
            Passiva(k, dt);
            // ANTECIPA: a agua da area RECUA primeiro — o aviso e' o mundo secando no anel
            if (k.Telegrafia > 0f)
            {
                if (!_avisando)
                {
                    _centro = k.Pos;
                    k.Visual("maris_recuo", _centro, _centro, k.Dados.Suprema["raio"], k.Telegrafia);
                }
                _avisando = true;
            }
            else _avisando = false;
            for (int i = _esferas.Count - 1; i >= 0; i--)
                if (!_esferas[i].Tick(k, dt)) _esferas.RemoveAt(i);
            if (_mare != null && !_mare.Tick(k, dt))
            {
                _mare = null;
                Dictionary<string, float> s = k.Dados.Suprema;
                k.BuffVelocidade(s["encharcada_vel"], s["encharcada_dur"]);   // o PRECO: a roupa encharcada pesa
                k.LigarEstado(ENCHARCADA, s["encharcada_dur"]);
            }
        }

        /// <summary>Mare Viva: cura em baldes de 0,25 s (nada de HealthChanged por quadro), so' faltando vida.</summary>
        private void Passiva(KitRunner k, float dt)
        {
            SobreAgua = (_mare != null && _mare.Cobre(k.Pos)) || AguaNoChao(k.Pos);
            if (!SobreAgua)
            {
                _regenAcc = 0f;
                if (_viva != null) { _viva.Restante = 0f; _viva = null; }
                return;
            }
            // um objeto so', renovado enquanto pisa agua e desligado na saida (como a tempera do Brok)
            if (_viva == null) _viva = k.Visual("maris_mare_viva", k.Pos, k.Pos, 0.9f, 1f, k.Dono);
            _viva.Restante = _viva.Duracao;
            _regenAcc += dt;
            if (_regenAcc < TICK) return;
            Vitalidade v = k.Dono.Vital;
            if (v.Hp < v.HpMax) k.DevolverDano(k.Dados.Passiva["regen"] * _regenAcc);
            _regenAcc = 0f;
        }

        /// <summary>Lago LIQUIDO (celula de agua nao congelada) ou poca (lama que a agua deixou) sob os pes.</summary>
        public static bool AguaNoChao(Vector3 p)
        {
            TerrenoReativo t = Terreno();
            if (t == null) return false;
            int c = t.CelulaEm(p);
            if (c < 0) return false;
            EstadoCelula e = t.Estado(c);
            return e == EstadoCelula.Lama || (t.Tipo(c) == TipoCelula.Agua && e != EstadoCelula.Congelado);
        }

        /// <summary>Onda Prisao: a esfera sai da mao, na altura do peito, na mira.</summary>
        public void Tatica(KitRunner k)
        {
            Vector3 d = k.Mira();
            _esferas.Add(new Esfera(k, k.Pos + Vector3.up * k.Dados.Tatica["altura"] + d * 0.9f, d));
        }

        /// <summary>Mare Cheia: no CENTRO do anel do aviso (onde o mundo secou), nao onde ela correu depois.</summary>
        public void Suprema(KitRunner k)
        {
            if (_mare != null) _mare.Apagar();
            _mare = new Mare(k, _avisando ? _centro : k.Pos);
        }

        /// <summary>A AGUA CONDUZ (§14): raio que cai dentro da Mare eletrifica a area inteira — o time dela tambem.</summary>
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte)
        {
            if (el == Elemento.Raio && _mare != null && _mare.Cobre(pos)) _mare.Eletrificar(k);
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void DanoRecebido(KitRunner k, float quanto) { }

        /// <summary>O terreno reativo da partida (o teste escreve Partida.Terreno); sem ele, o da cena.</summary>
        private static TerrenoReativo Terreno()
        {
            TerrenoReativo t = Partida.Atual != null ? Partida.Atual.Terreno : null;
            if (t == null && TerrenoReativoBehaviour.Atual != null) t = TerrenoReativoBehaviour.Atual.Terreno;
            return t;
        }

        /// <summary>
        /// Molha e acerta a velocidade SEM atropelar um estado de movimento mais forte (a prisao de 1,2 s nao vira -20% so'
        /// porque o preso esta' na Mare): nesse caso so' renova o molhado. Queimando, a agua vence (Molhar apaga).
        /// </summary>
        private static void Banhar(IEntidade a, float fator, float dur)
        {
            Efeitos.EstadoAlvo s = Efeitos.De(a);
            if (s.SlowLeft > 0f && s.StatusMult < fator && s.BurnLeft <= 0f) { s.WetLeft = Mathf.Max(s.WetLeft, dur); return; }
            Efeitos.Molhar(a, dur, fator);
        }

        // ------------------------------------------------------------------ ESFERA
        /// <summary>A Onda Prisao em voo: lenta, reta, na altura do peito. Mede o PASSO (a -> b), pega o inimigo mais perto.</summary>
        public sealed class Esfera
        {
            public Vector3 Pos { get; private set; }
            public readonly Vector3 Dir;
            public float Alcance { get; private set; }
            public IEntidade Presa { get; private set; }
            public readonly EfeitoVisual Visual;
            private readonly IEntidade _dona;
            private readonly Dictionary<string, float> _t;

            public Esfera(KitRunner k, Vector3 origem, Vector3 dir)
            {
                Pos = origem; Dir = dir; _dona = k.Dono; _t = k.Dados.Tatica;
                Alcance = _t["alcance"];
                Visual = k.Visual("maris_esfera", origem, origem, 0.45f, Alcance / Mathf.Max(_t["velocidade"], 0.1f) + 0.1f);
            }

            /// <summary>Devolve false quando a esfera acabou (prendeu, bateu no muro ou chegou ao fim).</summary>
            public bool Tick(KitRunner k, float dt)
            {
                Vector3 a = Pos;
                float passo = Mathf.Min(_t["velocidade"] * dt, Alcance);
                Pos += Dir * passo;
                Alcance -= passo;
                Visual.Pos = Pos;
                Visual.Pos2 = Pos;
                IEntidade alvo = Tocou(k, a, Pos);
                if (alvo != null) { Prender(k, alvo); return false; }
                TerrenoReativo t = Terreno();
                if (Alcance <= 0f || (t != null && t.BloqueiaTiro(Pos)))
                {
                    // estoura no fim: a agua cai no chao (poca/lama — a Mare Viva dela pisa ali depois)
                    Visual.Restante = 0f;
                    k.Visual("maris_respingo", Pos, Pos, 1.2f, 0.5f);
                    Bus.EmitTerrainHit(Elemento.Agua, Pos, false);
                    return false;
                }
                return true;
            }

            private IEntidade Tocou(KitRunner k, Vector3 a, Vector3 b)
            {
                float r = _t["raio_toque"], alt = _t["altura"];
                IEntidade melhor = null;
                float melhorD = r;
                foreach (IEntidade e in k.AlvosPerto(b, r + Vector3.Distance(a, b) + alt, _dona))
                {
                    if (Combat.MesmoTime(_dona, e) || Efeitos.De(e).IframesLeft > 0f) continue;   // aliado nao; a ESQUIVA fura (o limitador)
                    float d = KitRunner.DistSegmento(e.Pos + Vector3.up * alt, a, b);
                    if (d <= melhorD) { melhorD = d; melhor = e; }
                }
                return melhor;
            }

            /// <summary>A COLUNA: molha pela regra do elemento e SUSPENDE (o piso do produto de velocidade) — ele ainda conjura e esquiva.</summary>
            private void Prender(KitRunner k, IEntidade alvo)
            {
                Presa = alvo;
                Visual.Restante = 0f;
                float dur = _t["suspensao"];
                Efeitos.Aplicar(alvo, Elemento.Agua, 0f, _dona);
                Efeitos.Lentificar(alvo, Velocidade.Piso, dur);
                k.Visual("maris_coluna", alvo.Pos, alvo.Pos, 0.9f, dur, alvo);
                Bus.EmitTerrainHit(Elemento.Agua, alvo.Pos, false);
            }
        }

        // ------------------------------------------------------------------ MARE CHEIA
        /// <summary>A area alagada: banha quem esta' dentro em baldes de 0,25 s; eletrificada, cobra o dps do lago eletrico de todos.</summary>
        public sealed class Mare
        {
            public readonly Vector3 Centro;
            public readonly float Raio;
            public float Duracao { get; private set; }
            /// <summary>s de area ELETRIFICADA que faltam.</summary>
            public float Choque { get; private set; }
            public readonly EfeitoVisual Visual;
            private EfeitoVisual _choqueVis;
            private readonly IEntidade _dona;
            private readonly Dictionary<string, float> _s;
            private float _acc;

            public Mare(KitRunner k, Vector3 centro)
            {
                Centro = centro; _dona = k.Dono; _s = k.Dados.Suprema;
                Raio = _s["raio"]; Duracao = _s["duracao"];
                Visual = k.Visual("maris_mare", centro, centro, Raio, Duracao);
            }

            public bool Cobre(Vector3 p)
            {
                float dx = p.x - Centro.x, dz = p.z - Centro.z;
                return dx * dx + dz * dz <= Raio * Raio;
            }

            public void Apagar()
            {
                Duracao = 0f;
                Visual.Restante = 0f;
                if (_choqueVis != null) _choqueVis.Restante = 0f;
            }

            public void Eletrificar(KitRunner k)
            {
                Choque = Balance.Terrain.ElectrifyDuration;
                if (_choqueVis == null || _choqueVis.Restante <= 0f) _choqueVis = k.Visual("maris_choque", Centro, Centro, Raio, Choque);
                else _choqueVis.Restante = _choqueVis.Duracao;
            }

            /// <summary>Devolve false quando a agua drenou.</summary>
            public bool Tick(KitRunner k, float dt)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Apagar(); return false; }
                Choque = Mathf.Max(Choque - dt, 0f);
                _acc += dt;
                if (_acc < TICK) return true;
                float janela = _acc;
                _acc = 0f;
                foreach (IEntidade a in k.AlvosPerto(Centro, Raio))   // SEM excluir ninguem: ela tambem esta' na agua
                {
                    bool time = Combat.MesmoTime(_dona, a);
                    Banhar(a, time ? _s["buff_vel"] : _s["lentidao"], TICK * 2f);
                    if (Choque > 0f) Combat.AplicarDot(a, Balance.Terrain.ElectrifyDps, janela, "electric", null);
                }
                return true;
            }
        }
    }
}
