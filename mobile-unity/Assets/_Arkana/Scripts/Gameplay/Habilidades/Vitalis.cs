using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA VITALIS (07) — Guardia. GDD §3, ficha em design/personagens/07-vitalis.md.
    ///   Passiva  Maos Livres      — a Lumen reergue o aliado CAIDO a ate' 10m enquanto a Vitalis segue lutando
    ///   Tatica   Vai, Lumen       — a Lumen cura 8 hp/s por 12s
    ///   Suprema  Jardim da Aurora — circulo de 6m por 10s: o time dentro regenera 8 hp/s e reergue 50% mais rapido;
    ///                               INIMIGO dentro nao recebe cura nenhuma (o selo do jardim — o mesmo Vex.Selo)
    /// OS LIMITADORES: reerguer pela Lumen nao gera escudo (Derrubado.Reerguer volta com escudo zero); QUALQUER dano dissipa
    /// a Lumen (a cura para); com a Lumen fora — curando, ou apagada 3s depois do Jardim — a passiva DESLIGA; o Jardim e' um
    /// FAROL (coluna de luz de `farol_altura` m que a casca desenha, por cima de qualquer parede).
    /// SOLO (hoje): ninguem cai (Derrubado.SOLO_DERRUBA) e nao ha' aliado — a passiva espera o esquadrao e a Lumen cura a
    /// propria Vitalis. ponytail: com duplas/trios, Vai Lumen segue o aliado EhPlayer mais ferido a `alcance` m e o dano
    /// que a dissipa passa a ser o DELE.
    /// </summary>
    public sealed class Vitalis : IHabilidade
    {
        public const string LUMEN_CURA = "lumen_cura";
        public const string JARDIM = "jardim";
        public const string LUMEN_APAGADA = "lumen_apagada";
        /// <summary>s do tique de area (busca de caido, aliados e selo do Jardim).</summary>
        public const float TICK = 0.25f;

        float _cura, _canal, _busca;
        Jardim _jardim;
        IEntidade _reerguendo;
        bool _avisando;
        Vector3 _centroAviso;

        public bool Curando => _cura > 0f;
        public Jardim JardimAtivo => _jardim;
        /// <summary>O caido que a Lumen esta' reerguendo (null = ninguem) e quanto ja' canalizou (s).</summary>
        public IEntidade Reerguendo => _reerguendo;
        public float Canal => _canal;
        /// <summary>Onde o Jardim vai florescer (a Lumen voa para la' desenhando o circulo durante o aviso).</summary>
        public Vector3 CentroDoAviso => _centroAviso;

        /// <summary>A Lumen esta' COM ela (nem curando, nem apagada, nem a Vitalis caida): so' assim existe passiva.</summary>
        public bool LumenEmCasa(KitRunner k) => _cura <= 0f && !k.EstadoAtivo(LUMEN_APAGADA) && !Derrubado.Esta(k.Dono);

        static float Plano(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>A cura na Vitalis: pela porta do runner (Vitalidade.Curar + HealthChanged). Cheia ou caida nao cura.</summary>
        internal static void Curar(KitRunner k, float quanto)
        {
            Vitalidade v = k.Dono.Vital;
            if (v.Hp < v.HpMax && !Derrubado.Esta(k.Dono)) k.DevolverDano(quanto);
        }

        public void Tick(KitRunner k, float dt)
        {
            // o aviso no chao sai onde ela APERTOU (Bus.KitTelegraph leva Dono.Pos): o Jardim floresce la'
            if (k.Telegrafia > 0f && !_avisando) { _avisando = true; _centroAviso = k.Pos; }
            if (_cura > 0f)
            {
                _cura -= dt;
                Curar(k, k.Dados.Tatica["cura"] * dt);
                if (_cura <= 0f) k.AvisarEstado(LUMEN_CURA, false);
            }
            if (_jardim != null && !_jardim.Tick(k, dt)) FimDoJardim(k);
            Passiva(k, dt);
        }

        /// <summary>Vai, Lumen: ela beija a fada e sopra. A Lumen larga o caido que reerguia — foi curar.</summary>
        public void Tatica(KitRunner k)
        {
            _cura = k.Dados.Tatica["duracao"];
            _reerguendo = null;
            _canal = 0f;
            k.AvisarEstado(LUMEN_CURA, true);
        }

        /// <summary>Jardim da Aurora: so' depois do aviso (a Lumen desenhando o circulo).</summary>
        public void Suprema(KitRunner k)
        {
            Vector3 c = _avisando ? _centroAviso : k.Pos;
            _avisando = false;
            _jardim = new Jardim(k, c);
            k.AvisarEstado(JARDIM, true);
        }

        /// <summary>O PRECO do Jardim: as flores murcham em petalas e a Lumen volta arrastada, APAGADA — sem passiva.</summary>
        void FimDoJardim(KitRunner k)
        {
            k.Visual("vitalis_murcha", _jardim.Centro, _jardim.Centro, _jardim.Raio, 1.5f);
            _jardim = null;
            k.AvisarEstado(JARDIM, false);
            k.LigarEstado(LUMEN_APAGADA, k.Dados.Suprema["apagada_dur"]);
        }

        /// <summary>O PRECO da cura: QUALQUER dano dissipa a Lumen — ela volta assustada e a cura PARA.</summary>
        public void DanoRecebido(KitRunner k, float quanto)
        {
            if (_cura <= 0f || k.Dados.Tatica["dissipa_ao_dano"] <= 0f) return;
            _cura = 0f;
            k.AvisarEstado(LUMEN_CURA, false);
            k.Visual("vitalis_susto", k.Pos, k.Pos, 0.6f, 0.5f);
        }

        /// <summary>
        /// Maos Livres: com a Lumen em casa, o aliado CAIDO a `alcance` m e' reerguido por ELA (a Vitalis nao canaliza, segue
        /// lutando). O canal anda no MultReerguer do caido (o Jardim acelera pelo Derrubado.Acelerar); longe demais ou a Lumen
        /// saiu, zera.
        /// </summary>
        void Passiva(KitRunner k, float dt)
        {
            if (!LumenEmCasa(k)) { _reerguendo = null; _canal = 0f; return; }
            Dictionary<string, float> p = k.Dados.Passiva;
            Derrubado d = _reerguendo != null ? Derrubado.De(_reerguendo) : null;
            if (d == null || Plano(_reerguendo.Pos - k.Pos) > p["alcance"]) { _reerguendo = null; _canal = 0f; d = null; }
            if (d == null)
            {
                _busca += dt;
                if (_busca < TICK) return;
                _busca = 0f;
                foreach (IEntidade a in k.AlvosPerto(k.Pos, p["alcance"], k.Dono))
                    if (Vex.MesmoTime(k.Dono, a) && Derrubado.Esta(a)) { _reerguendo = a; break; }
                if (_reerguendo == null) return;
                d = Derrubado.De(_reerguendo);
            }
            _canal += dt * d.MultReerguer;
            if (_canal < p["reerguer_s"]) return;
            Derrubado.Reerguer(_reerguendo, k.Dono);   // volta com escudo ZERO: a Lumen reerguendo nao gera escudo
            _reerguendo = null;
            _canal = 0f;
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        // ------------------------------------------------------------------ JARDIM
        /// <summary>Circulo FIXO onde floresceu. A Vitalis dentro cura por quadro; aliado e selo no tique de area.</summary>
        public sealed class Jardim
        {
            public readonly Vector3 Centro;
            public readonly float Raio;
            public float Restante { get; private set; }
            public readonly EfeitoVisual Visual;
            public readonly Vex.Selo Selo = new Vex.Selo();
            readonly Dictionary<string, float> _s;
            float _acc;

            public Jardim(KitRunner k, Vector3 centro)
            {
                _s = k.Dados.Suprema;
                Centro = centro;
                Raio = _s["raio"];
                Restante = _s["duracao"];
                _acc = TICK;   // o primeiro tique de area sai no florescer
                Visual = k.Visual("vitalis_jardim", centro, centro, Raio, Restante);
            }

            public bool Dentro(Vector3 p) => Plano(p - Centro) <= Raio;

            public bool Tick(KitRunner k, float dt)
            {
                Restante -= dt;
                if (Restante <= 0f) { Visual.Restante = 0f; Selo.Soltar(); return false; }
                if (Dentro(k.Pos)) Curar(k, _s["cura"] * dt);
                _acc += dt;
                if (_acc >= TICK)
                {
                    _acc = 0f;
                    Selo.Abrir();
                    foreach (IEntidade e in k.AlvosPerto(Centro, Raio, k.Dono))
                    {
                        if (!Vex.MesmoTime(k.Dono, e)) { if (_s["selo_inimigo"] > 0f) Selo.Selar(e); continue; }
                        // aliado dentro (so' existe com esquadrao): reergue mais rapido ou regenera
                        if (Derrubado.Esta(e)) Derrubado.Acelerar(e, _s["reerguer_mult"], TICK * 2f);
                        else e.Vital.Curar(_s["cura"] * TICK);
                    }
                    Selo.Fechar();
                }
                Selo.Tick();
                return true;
            }
        }
    }
}
