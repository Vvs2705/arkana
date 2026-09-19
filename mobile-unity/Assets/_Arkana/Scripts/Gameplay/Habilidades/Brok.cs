using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO BROK (13) — Suporte/Protecao. Ficha em design/personagens/13-brok.md; tempos em DIRECAO.md §4 e §6.
    ///   Passiva  Tempera Eterna — 15% menos dano de TERRENO (fogo e chao eletrico)
    ///   Tatica   Runa-Escudo    — muralha runica curva de 4 m a frente: bloqueia tiro inimigo por 6 s (vida propria)
    ///   Suprema  Forja Viva     — bigorna-totem 12 s: quem esta' no raio regenera ESCUDO (nunca vida) + runa pessoal de 25
    /// OS LIMITADORES: a muralha so' cobre a FRENTE (flanco e por cima passam) e o tiro gasta a vida dela (x Estrutura do
    /// elemento, a regua do muro de terra §14); a bigorna tem 150 de vida e e' BARULHENTA (cada clang e' um Disparo: os
    /// bots a 30 m vem); ao acabar, as runas do martelo apagam 3 s (sem tatica). Escudo so' pela porta
    /// KitRunner.RegenerarEscudo; a runa DEVOLVE no mesmo tique o que o Combat cobrou (como a Pyra devolve o fogo).
    /// ponytail: solo — a tempera e a runa valem para o proprio Brok (sem esquadrao nao ha' aliado a 8 m); a regeneracao
    /// da bigorna ja' pega todo aliado do dono (Combat.MesmoTime) no raio.
    /// </summary>
    public sealed class Brok : IHabilidade
    {
        public const string RUNAS_APAGADAS = "brok_runas_apagadas";

        private readonly List<Muralha> _muralhas = new List<Muralha>();
        private Bigorna _bigorna;
        private EfeitoVisual _tempera, _runaVis;
        private Vector3 _centro;
        private bool _avisando;
        private float _runa, _hpAntes, _escAntes;

        public IReadOnlyList<Muralha> Muralhas => _muralhas;
        public Bigorna BigornaAtiva => _bigorna;
        /// <summary>Quanto a runa pessoal ainda segura.</summary>
        public float Runa => _runa;
        /// <summary>Pisando chao que doi (a tempera esta' trabalhando).</summary>
        public bool Temperando { get; private set; }

        public void Tick(KitRunner k, float dt)
        {
            Passiva(k, dt);
            // ANTECIPA: a bigorna sobe acima da cabeca durante o aviso; ela desce no CENTRO do anel (onde o aviso nasceu)
            if (k.Telegrafia > 0f)
            {
                if (!_avisando)
                {
                    _centro = k.Pos;
                    k.Visual("brok_erguida", k.Pos, k.Pos, 0.6f, k.Telegrafia, k.Dono);
                }
                _avisando = true;
            }
            else _avisando = false;
            IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
            for (int i = _muralhas.Count - 1; i >= 0; i--)
                if (!_muralhas[i].Tick(k, dt, vivos)) _muralhas.RemoveAt(i);
            if (_bigorna != null && !_bigorna.Tick(k, dt, vivos)) FimDaForja(k);
            // a foto do corpo no fim do tique: a runa mede o que o Combat cobrou ate' o proximo
            _hpAntes = k.Dono.Vital.Hp;
            _escAntes = k.Dono.Vital.Escudo;
        }

        /// <summary>Tempera Eterna: a mesma conta da Pyra — so' a FATIA do DoT que veio do chao, depois do teto do Combat.</summary>
        private void Passiva(KitRunner k, float dt)
        {
            float dps = KitRunner.DpsDoTerreno != null ? KitRunner.DpsDoTerreno(k.Pos) : 0f;
            Temperando = dps > 0f;
            if (!Temperando)
            {
                if (_tempera != null) { _tempera.Restante = 0f; _tempera = null; }
                return;
            }
            if (_runa <= 0f)   // com a runa de pe' o dano ja' volta inteiro por ela
            {
                Efeitos.EstadoAlvo s = Efeitos.De(k.Dono);
                float total = dps + (s.BurnLeft > 0f ? s.BurnDps : 0f);
                float cobrado = Mathf.Min(total, Balance.Dot.TetoDps);
                k.DevolverDano(cobrado * (dps / total) * k.Dados.Passiva["reducao_terreno"] * dt);
            }
            // o metal aterra o elemento: faisca azul nos pes enquanto pisa. Um objeto so', renovado (zero lixo por quadro) e
            // desligado na saida: prazo curto morreria no MESMO tique num quadro lento e renasceria todo quadro.
            if (_tempera == null) _tempera = k.Visual("brok_tempera", k.Pos, k.Pos, 0.8f, 1f, k.Dono);
            _tempera.Restante = _tempera.Duracao;
        }

        /// <summary>Runa-Escudo: martelada no chao e a muralha curva sobe a frente, na mira.</summary>
        public void Tatica(KitRunner k)
        {
            Vector3 d = k.Mira();
            _muralhas.Add(new Muralha(k, k.Pos, d));
            Vector3 golpe = k.Pos + d * 0.9f;
            k.Visual("brok_martelada", golpe, golpe, 1.4f, 0.6f);
        }

        /// <summary>Forja Viva: a bigorna desce no centro do aviso. Uma por vez (a nova substitui, sem cobrar o preco duas vezes).</summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            Vector3 pos = _avisando ? _centro : k.Pos;
            if (_bigorna != null) _bigorna.Apagar();
            _bigorna = new Bigorna(k, pos);
            if ((k.Pos - pos).sqrMagnitude <= s["raio"] * s["raio"])
            {
                _runa = s["runa"];
                if (_runaVis != null) _runaVis.Restante = 0f;
                _runaVis = k.Visual("brok_runa", k.Pos, k.Pos, 0.9f, s["duracao"], k.Dono);
            }
        }

        /// <summary>O PRECO da Forja: a runa morre com a bigorna e as runas do martelo apagam (sem tatica).</summary>
        private void FimDaForja(KitRunner k)
        {
            _bigorna = null;
            GastarRuna(_runa);
            float apagadas = k.Dados.Suprema["runas_apagadas"];
            k.ForcarCdTatica(apagadas);
            k.LigarEstado(RUNAS_APAGADAS, apagadas);
        }

        /// <summary>
        /// A RUNA PESSOAL devolve, no mesmo tique, o que o Combat cobrou — escudo no escudo, vida na vida — ate' 25.
        /// ponytail: golpe FATAL nao volta (morto o runner nem ticka): segurar antes do dano pede gancho no Combat.
        /// </summary>
        public void DanoRecebido(KitRunner k, float quanto)
        {
            if (_runa <= 0f) return;
            Vitalidade v = k.Dono.Vital;
            float esc = Mathf.Min(Mathf.Max(_escAntes - v.Escudo, 0f), _runa);
            if (esc > 0f) KitRunner.RegenerarEscudo(k.Dono, esc);
            float hp = Mathf.Min(Mathf.Max(_hpAntes - v.Hp, 0f), _runa - esc);
            if (hp > 0f) k.DevolverDano(hp);
            GastarRuna(esc + hp);
        }

        private void GastarRuna(float q)
        {
            _runa = Mathf.Max(_runa - q, 0f);
            if (_runa > 0f || _runaVis == null) return;
            _runaVis.Restante = 0f;
            _runaVis = null;
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        /// <summary>Tiro some NA peca: gasta a vida dela na regua do muro de terra (§14) e o impacto acontece ali.</summary>
        private static float Bater(Projetil p)
        {
            float dano = p.Dano * Balance.Perfil(p.ElementoDoTiro).Estrutura;
            p.Impacto(null);   // morre no ponto: o terreno e o estouro do impacto reagem na peca, nao no corpo atras
            return dano;
        }

        // ------------------------------------------------------------------ MURALHA
        /// <summary>
        /// Arco de raio `Raio` em volta de onde o Brok estava, aberto na mira. Mede o PASSO do tiro (onde estava -> onde
        /// esta'), nao so' o ponto: um raio a 35 m/s anda mais que a espessura num quadro.
        /// </summary>
        public sealed class Muralha
        {
            public readonly Vector3 Centro, Frente;
            public readonly float Raio, MeiaAbertura, Altura, Espessura;
            public float Vida { get; private set; }
            public float Duracao { get; private set; }
            public int Bloqueados { get; private set; }
            public readonly EfeitoVisual Visual;
            private readonly IEntidade _dono;

            public Vector3 Meio => Centro + Frente * Raio;

            public Muralha(KitRunner k, Vector3 centro, Vector3 frente)
            {
                Dictionary<string, float> t = k.Dados.Tatica;
                Centro = centro; Frente = frente; _dono = k.Dono;
                Raio = t["raio_arco"];
                MeiaAbertura = t["comprimento"] * 0.5f / Raio;   // rad: arco de 4 m no raio de 2,5 m
                Altura = t["altura"]; Espessura = t["espessura"];
                Vida = t["vida"]; Duracao = t["duracao"];
                Visual = k.Visual("brok_muralha", centro, Meio, t["comprimento"], Duracao);
            }

            /// <summary>Devolve false quando a muralha acabou (prazo ou quebrada).</summary>
            public bool Tick(KitRunner k, float dt, IList<Projetil> vivos)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Visual.Restante = 0f; return false; }
                if (vivos == null) return true;
                for (int i = 0; i < vivos.Count; i++)
                {
                    Projetil p = vivos[i];
                    if (p == null || !p.Vivo || Combat.MesmoTime(p.Atirador, _dono)) continue;
                    if (!Cruza(p.Pos - p.Dir * (p.Velocidade * dt), p.Pos)) continue;
                    Vida -= Bater(p);
                    Bloqueados++;
                    if (Vida > 0f) continue;
                    Duracao = 0f;
                    Visual.Restante = 0f;
                    k.Visual("brok_estilhaco", Meio, Meio, 2f, 0.6f);
                    return false;
                }
                return true;
            }

            /// <summary>O passo a -> b atravessa (ou toca) a faixa do arco, dentro da abertura e abaixo do topo?</summary>
            public bool Cruza(Vector3 a, Vector3 b)
            {
                float da = DistXZ(a), db = DistXZ(b);
                bool dentroA = Mathf.Abs(da - Raio) <= Espessura, dentroB = Mathf.Abs(db - Raio) <= Espessura;
                if (!dentroA && !dentroB && (da - Raio) * (db - Raio) > 0f) return false;
                float t = dentroB ? 1f : dentroA ? 0f : (da - Raio) / (da - db);
                Vector3 x = Vector3.Lerp(a, b, t);
                float h = x.y - Centro.y;
                if (h < -0.3f || h > Altura) return false;   // POR CIMA passa (o limitador)
                Vector3 r = x - Centro;
                r.y = 0f;
                return r.sqrMagnitude > 1e-6f && Vector3.Angle(r, Frente) * Mathf.Deg2Rad <= MeiaAbertura;   // FLANCO passa
            }

            private float DistXZ(Vector3 p)
            {
                float dx = p.x - Centro.x, dz = p.z - Centro.z;
                return Mathf.Sqrt(dx * dx + dz * dz);
            }
        }

        // ------------------------------------------------------------------ BIGORNA
        /// <summary>A bigorna-totem: regenera escudo no raio em baldes de 0,25 s, apanha tiro inimigo e faz barulho.</summary>
        public sealed class Bigorna
        {
            public const float TICK = 0.25f;
            /// <summary>m — o centro da bigorna acima do chao (onde o tiro acerta).</summary>
            public const float ALTURA = 0.6f;
            public readonly Vector3 Pos;
            public float Vida { get; private set; }
            public float Duracao { get; private set; }
            public readonly EfeitoVisual Visual;
            private readonly IEntidade _dono;
            private readonly Dictionary<string, float> _s;
            private float _acc, _clang;

            public bool Viva => Duracao > 0f && Vida > 0f;

            public Bigorna(KitRunner k, Vector3 pos)
            {
                Pos = pos; _dono = k.Dono; _s = k.Dados.Suprema;
                Vida = _s["vida"]; Duracao = _s["duracao"];
                _clang = _s["clang"];
                Visual = k.Visual("brok_bigorna", pos, pos, _s["raio"], Duracao);
                Bus.EmitDisparo(_dono, pos);   // o baque do plantio ja' denuncia
            }

            public void Apagar() { Duracao = 0f; Visual.Restante = 0f; }

            /// <summary>Devolve false quando a bigorna acabou (prazo ou destruida).</summary>
            public bool Tick(KitRunner k, float dt, IList<Projetil> vivos)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Visual.Restante = 0f; return false; }
                Apanhar(dt, vivos);
                if (Vida <= 0f)
                {
                    Apagar();
                    k.Visual("brok_estilhaco", Pos + Vector3.up * ALTURA, Pos + Vector3.up * ALTURA, 1.6f, 0.6f);
                    return false;
                }
                _clang -= dt;
                if (_clang <= 0f)
                {
                    _clang = _s["clang"];
                    Bus.EmitDisparo(_dono, Pos);   // BARULHENTA: cada clang anuncia a posicao do time aos bots
                }
                _acc += dt;
                if (_acc < TICK) return true;
                float q = _s["escudo_regen"] * _acc;
                _acc = 0f;
                float r = _s["raio"];
                if ((_dono.Pos - Pos).sqrMagnitude <= r * r) KitRunner.RegenerarEscudo(_dono, q);
                foreach (IEntidade a in k.AlvosPerto(Pos, r, _dono))
                    if (Combat.MesmoTime(_dono, a)) KitRunner.RegenerarEscudo(a, q);   // ESCUDO, nunca vida
                return true;
            }

            private void Apanhar(float dt, IList<Projetil> vivos)
            {
                if (vivos == null) return;
                Vector3 alvo = Pos + Vector3.up * ALTURA;
                for (int i = 0; i < vivos.Count && Vida > 0f; i++)
                {
                    Projetil p = vivos[i];
                    if (p == null || !p.Vivo || Combat.MesmoTime(p.Atirador, _dono)) continue;
                    if (KitRunner.DistSegmento(alvo, p.Pos - p.Dir * (p.Velocidade * dt), p.Pos) > _s["raio_corpo"]) continue;
                    Vida -= Bater(p);
                }
            }
        }
    }
}
