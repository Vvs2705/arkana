using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO FIZZ (16) — longa distancia / armadilhas. Ficha em design/personagens/16-fizz.md; tempos no DIRECAO.md §4 e §6.
    ///   Passiva  Mola nos Calcanhares — o pulo sobe 50% mais alto (queda ja' nao fere ninguem: o jogo nao tem dano de queda)
    ///   Tatica   Torreta Faisca       — torreta de 40 de vida que solta faisca FRACA em quem entra no cone FIXO; no max. 2
    ///   Suprema  MEGABOBINA            — a bobina se ARMA na telegrafia (2 s, 60 de vida, laser no alvo) e no fim dispara UM
    ///                                   raio guiado no inimigo mais perto; o raio publica Raio no trajeto (ancora, muro)
    /// OS LIMITADORES SAO PARTE DO KIT: cone fixo (flanqueavel) e dano baixo (zoneia, nao mata); a bobina fica PARADA, brilha e
    /// canta — quebrada antes do fim, a carga foi embora sem raio; o raio e' UM alvo, nunca area; depois a bobina vira sucata e
    /// as MOLAS TRAVAM 2 s (sem pulo alto). Estrutura apanha de projetil inimigo que a toca e de TerrainHit perto — o do
    /// proprio Fizz (raio chegando, trajeto da bobina) nao conta.
    /// </summary>
    public sealed class Fizz : IHabilidade
    {
        public const string MOLAS_TRAVADAS = "molas_travadas";

        private readonly List<Torreta> _torretas = new List<Torreta>();
        private readonly List<ApoioGrupoD.RaioGuiado> _raios = new List<ApoioGrupoD.RaioGuiado>();
        private Bobina _bobina;
        private bool _proprio;
        private int _pulos;

        public IReadOnlyList<Torreta> Torretas => _torretas;
        public IReadOnlyList<ApoioGrupoD.RaioGuiado> Raios => _raios;
        /// <summary>A bobina da carga em curso (null fora da telegrafia; morta = quebraram).</summary>
        public Bobina BobinaArmada => _bobina;

        /// <summary>Mola: quantas vezes mais ALTO o pulo sobe — 1 (normal) com as molas travadas.</summary>
        public float FatorDoPulo(KitRunner k) => k.EstadoAtivo(MOLAS_TRAVADAS) ? 1f : k.Dados.Passiva["pulo_mult"];

        public void Tick(KitRunner k, float dt)
        {
            // a MOLA e' do corpo (o pulo sai na altura certa); a faisca dos calcanhares, na borda da decolagem
            float mola = FatorDoPulo(k);
            k.Dono.FatorDePulo = mola;
            if (k.Dono.Pulos != _pulos)
            {
                _pulos = k.Dono.Pulos;
                if (mola > 1f) k.Visual("fizz_mola", k.Pos, k.Pos, 0.6f, 0.45f);
            }
            // a MEGABOBINA nasce no 1o tique da telegrafia (o motor avisa, o Fizz monta) e so' dispara quando o aviso acaba
            if (k.Telegrafia > 0f && _bobina == null) _bobina = new Bobina(k);
            if (_bobina != null) _bobina.Tick(k, dt);
            for (int i = _torretas.Count - 1; i >= 0; i--)
                if (!_torretas[i].Tick(k, dt, _raios)) _torretas.RemoveAt(i);
            _proprio = true;   // o raio que chega publica Raio no chao: nao e' golpe nas estruturas dele
            for (int i = _raios.Count - 1; i >= 0; i--)
                if (!_raios[i].Tick(dt)) _raios.RemoveAt(i);
            _proprio = false;
        }

        /// <summary>Torreta Faisca: fincada a `dist` m na mira, o cone olha para onde ele mirou e NAO gira. A 3a recolhe a mais velha.</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            while (_torretas.Count >= Mathf.Max((int)t["max_torretas"], 1))
            {
                _torretas[0].Desmontar(k, false);
                _torretas.RemoveAt(0);
            }
            Vector3 d = k.Mira();
            _torretas.Add(new Torreta(k, k.Pos + d * t["dist"], d));
        }

        /// <summary>O fim da telegrafia: bobina viva dispara; quebrada, nada (a contra-jogada pagou).</summary>
        public void Suprema(KitRunner k)
        {
            Bobina b = _bobina;
            _bobina = null;
            if (b == null || !b.Viva) return;
            Dictionary<string, float> s = k.Dados.Suprema;
            b.Encerrar();
            k.Visual("fizz_sucata", b.Pos, b.Pos, 1f, s["sucata_dur"]);   // derrete em sucata fumegante
            k.LigarEstado(MOLAS_TRAVADAS, s["trava_dur"]);                 // o preco no corpo: 2 s sem pulo alto
            IEntidade alvo = ApoioGrupoD.MaisProximo(k, b.Pos, s["alcance"]);
            if (alvo == null) return;   // ninguem no alcance: a bobina derrete a toa
            _raios.Add(new ApoioGrupoD.RaioGuiado(k, "fizz_raio", b.Topo, alvo, s["dano"], s["vel"], 0.35f, 0f));
            // "desliga construcoes no caminho": Raio forte no trajeto, a cada passo — o §14 (e quem escuta o Bus) decide
            float dist = Vector3.Distance(b.Pos, alvo.Pos), passo = Mathf.Max(s["passo_trajeto"], 0.5f);
            _proprio = true;
            for (float x = passo; x < dist; x += passo) Bus.EmitTerrainHit(Elemento.Raio, Vector3.Lerp(b.Pos, alvo.Pos, x / dist), true);
            _proprio = false;
        }

        public void EstadoAcabou(KitRunner k, string nome) { }
        public void DanoRecebido(KitRunner k, float quanto) { }

        /// <summary>Golpe no chao perto de uma estrutura dele fere a estrutura (o dano do muro). O proprio nao conta.</summary>
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte)
        {
            if (_proprio) return;
            float dano = ApoioGrupoD.DanoDeGolpe(el, forte);
            for (int i = 0; i < _torretas.Count; i++) _torretas[i].Golpe(pos, dano);   // morta sai no proximo tique
            if (_bobina != null) _bobina.Golpe(k, pos, dano);
        }

        // ------------------------------------------------------------------ TORRETA
        /// <summary>Mini-torreta arcana: cone FIXO na direcao em que foi fincada; faisca guiada no inimigo mais perto DENTRO dele.</summary>
        public sealed class Torreta
        {
            public const float TICK = 0.1f;
            public readonly Vector3 Pos, Frente;
            public readonly EfeitoVisual Visual;
            public float Vida { get; private set; }
            public float Restante { get; private set; }
            private readonly Dictionary<string, float> _t;
            private float _cd, _acc;

            public bool Viva => Vida > 0f && Restante > 0f;
            public Vector3 Cabeca => Pos + Vector3.up * 0.95f;

            public Torreta(KitRunner k, Vector3 pos, Vector3 frente)
            {
                _t = k.Dados.Tatica;
                Pos = pos; Frente = frente; Vida = _t["vida"]; Restante = _t["duracao"];
                // Pos2 = a ponta do eixo do cone: a casca le' dali a direcao e o alcance (o cone desenhado E' o limitador)
                Visual = k.Visual("fizz_torreta", pos, pos + frente * _t["alcance"], _t["corpo_raio"], Restante);
            }

            public void Golpe(Vector3 p, float dano)
            {
                if (Vector3.Distance(p, Pos) <= _t["golpe_raio"]) Vida -= dano;
            }

            /// <summary>False = acabou (quebrou ou venceu). A faisca disparada entra em `raios` (o Fizz tica todas).</summary>
            public bool Tick(KitRunner k, float dt, List<ApoioGrupoD.RaioGuiado> raios)
            {
                Restante -= dt;
                Vida -= ApoioGrupoD.EngolirTiros(k, Pos, _t["corpo_raio"], _t["corpo_altura"]);
                if (!Viva) { Desmontar(k, Vida <= 0f); return false; }
                _cd -= dt;
                _acc += dt;
                if (_acc < TICK || _cd > 0f) return true;
                _acc = 0f;
                IEntidade alvo = NoCone(k);
                if (alvo == null) return true;
                _cd = _t["cadencia"];
                raios.Add(new ApoioGrupoD.RaioGuiado(k, "fizz_faisca", Cabeca, alvo, _t["dano"], _t["vel"], 0.12f, 1.2f));
                return true;
            }

            private IEntidade NoCone(KitRunner k)
            {
                IEntidade melhor = null;
                float d2 = float.MaxValue;
                foreach (IEntidade e in k.AlvosPerto(Pos, _t["alcance"], k.Dono))
                {
                    if (!ApoioGrupoD.Inimigo(k.Dono, e) || !ApoioGrupoD.NoCone(Pos, Frente, e.Pos, _t["cone_graus"])) continue;
                    float d = (e.Pos - Pos).sqrMagnitude;
                    if (d < d2) { d2 = d; melhor = e; }
                }
                return melhor;
            }

            public void Desmontar(KitRunner k, bool quebrada)
            {
                Visual.Restante = 0f;
                if (quebrada) k.Visual("fizz_sucata", Pos, Pos, 0.5f, 1.2f);
            }
        }

        // ------------------------------------------------------------------ BOBINA
        /// <summary>A MEGABOBINA em carga: parada ao lado dele, 60 de vida, e um LASER no inimigo que vai levar (o aviso de
        /// quem: sair do alcance e' a saida). O visual dura exatamente a telegrafia — a casca le' dali o quanto ja' armou.</summary>
        public sealed class Bobina
        {
            public readonly Vector3 Pos;
            public readonly EfeitoVisual Visual, Mira;
            public float Vida { get; private set; }
            private readonly Dictionary<string, float> _s;
            private float _acc = 1f;   // o laser ja' procura no 1o tique

            public bool Viva => Vida > 0f;
            public Vector3 Topo => Pos + Vector3.up * 2.4f;
            /// <summary>Quem o laser marca agora (null = ninguem no alcance).</summary>
            public IEntidade Alvo => Mira.Alvo;

            public Bobina(KitRunner k)
            {
                _s = k.Dados.Suprema;
                Pos = k.Pos + k.Mira() * 1.2f;
                Vida = _s["vida"];
                Visual = k.Visual("fizz_bobina", Pos, Topo, _s["corpo_raio"], k.Telegrafia);
                Mira = k.Visual("fizz_mira", Topo, Topo, 0.6f, k.Telegrafia);
            }

            public void Golpe(KitRunner k, Vector3 p, float dano)
            {
                if (Viva && Vector3.Distance(p, Pos) <= _s["golpe_raio"]) Ferir(k, dano);
            }

            public void Tick(KitRunner k, float dt)
            {
                if (!Viva) return;
                Ferir(k, ApoioGrupoD.EngolirTiros(k, Pos, _s["corpo_raio"], _s["corpo_altura"]));
                if (!Viva) return;
                _acc += dt;
                if (_acc < 0.1f) return;
                _acc = 0f;
                Mira.Alvo = ApoioGrupoD.MaisProximo(k, Pos, _s["alcance"]);
            }

            private void Ferir(KitRunner k, float dano)
            {
                if (!(dano > 0f) || !Viva) return;
                Vida -= dano;
                if (Viva) return;
                Encerrar();
                k.Visual("fizz_sucata", Pos, Pos, 1f, 1.5f);   // quebrou na carga: sucata sem raio
            }

            public void Encerrar()
            {
                Visual.Restante = 0f;
                Mira.Restante = 0f;
            }
        }
    }
}
