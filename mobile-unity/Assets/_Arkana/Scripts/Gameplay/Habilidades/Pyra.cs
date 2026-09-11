using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA PYRA (01) — Vanguarda. GDD §3, ficha em design/personagens/01-pyra.md; espelho de habilidades/pyra.gd.
    ///   Passiva  Coracao de Fornalha — fogo NO CHAO nao a fere e reacende o braco (+10% vel por 2s)
    ///   Tatica   Muralha de Brasas   — linha de fogo de 8m por 5s, perpendicular a mira
    ///   Suprema  Braco Livre         — 6s de leque continuo + dash que deixa fogo
    /// OS LIMITADORES SAO PARTE DO KIT: agua APAGA a muralha e molha o braco (+1s de recarga); vento EMPURRA a
    /// muralha 3m; a suprema e' telegrafada pelo motor; ao acabar o braco ESFRIA (4s sem tatica, -15% vel).
    /// </summary>
    public sealed class Pyra : IHabilidade
    {
        public const string BRACO_LIVRE = "braco_livre";
        public const string BRACO_FRIO = "braco_frio";
        public const string BRACO_MOLHADO = "braco_molhado";

        private readonly List<Brasas> _brasas = new List<Brasas>();
        private float _lequeAcc, _dashAcc, _dashLeft;

        /// <summary>As brasas vivas (muralhas e pocas) — a cena le' Visuais; o teste le' daqui.</summary>
        public IReadOnlyList<Brasas> BrasasVivas => _brasas;

        public void Tick(KitRunner k, float dt)
        {
            for (int i = _brasas.Count - 1; i >= 0; i--)
                if (!_brasas[i].Tick(k, dt)) _brasas.RemoveAt(i);
            Passiva(k, dt);
            if (k.EstadoAtivo(BRACO_LIVRE)) BracoLivre(k, dt);
        }

        /// <summary>
        /// Coracao de Fornalha. A imunidade e' DEVOLUCAO no mesmo tique (o Combat nao tem gancho de prevencao):
        /// repoe exatamente a FATIA do DoT que veio do chao em chamas — o Combat soma queimadura + terreno e corta no
        /// teto; devolver o dps cru curaria de graca quem queima E pisa fogo. So' fogo: chao ELETRIFICADO doi nela.
        /// </summary>
        private void Passiva(KitRunner k, float dt)
        {
            float dps = KitRunner.DpsDoTerreno != null ? KitRunner.DpsDoTerreno(k.Pos) : 0f;
            bool noFogo = Mathf.Approximately(dps, Balance.Terrain.BurnDps);
            if (noFogo)
            {
                Efeitos.EstadoAlvo s = Efeitos.De(k.Dono);
                float total = dps + (s.BurnLeft > 0f ? s.BurnDps : 0f);
                float cobrado = Mathf.Min(total, Balance.Dot.TetoDps);
                k.DevolverDano(cobrado * (dps / Mathf.Max(total, 0.001f)) * dt);
            }
            if (noFogo || TemFogo(k.Pos))
                k.BuffVelocidade(k.Dados.Passiva["buff_vel"], k.Dados.Passiva["buff_dur"]);
        }

        /// <summary>Alguma brasa desta Pyra cobre o ponto?</summary>
        public bool TemFogo(Vector3 p)
        {
            for (int i = 0; i < _brasas.Count; i++)
                if (KitRunner.DistSegmento(p, _brasas[i].A, _brasas[i].B) <= _brasas[i].Espessura) return true;
            return false;
        }

        /// <summary>Muralha de Brasas: linha PERPENDICULAR a mira, 3m a frente.</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            Vector3 d = k.Mira();
            Vector3 centro = k.Pos + d * 3f;
            Vector3 perp = Vector3.Cross(d, Vector3.up).normalized;
            float meio = t["comprimento"] * 0.5f;
            _brasas.Add(new Brasas(k, centro - perp * meio, centro + perp * meio,
                t["duracao"], t["dano"], t["espessura"], t["rearme"], t["aceso_dur"], true, false));
        }

        /// <summary>Braco Livre: so' quando a telegrafia acaba (o KitRunner chama).</summary>
        public void Suprema(KitRunner k)
        {
            _lequeAcc = 0f; _dashAcc = 0f; _dashLeft = 0f;
            k.LigarEstado(BRACO_LIVRE, k.Dados.Suprema["duracao"]);
        }

        /// <summary>Manopla destravada: leque continuo + fogo no rastro do dash. NADA aqui toca em mana.</summary>
        private void BracoLivre(KitRunner k, float dt)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            _lequeAcc += dt;
            if (_lequeAcc >= s["cadencia"])
            {
                _lequeAcc = 0f;
                Leque(k, s);
            }
            if (k.DashIniciou)
            {
                _dashLeft = Balance.Dodge.Duration;
                _dashAcc = 1f;   // forca a primeira poca no mesmo frame
            }
            if (_dashLeft > 0f)
            {
                _dashLeft -= dt;
                _dashAcc += dt * 16f;
                if (_dashAcc >= 1f)
                {
                    _dashAcc = 0f;
                    _brasas.Add(new Brasas(k, k.Pos, k.Pos, s["fogo_dash_dur"], s["fogo_dash_dano"], s["fogo_dash_raio"],
                        0.6f, k.Dados.Tatica["aceso_dur"], false, true));
                }
            }
        }

        private void Leque(KitRunner k, Dictionary<string, float> s)
        {
            Vector3 d = k.Mira();
            Vector3 origem = k.Pos + new Vector3(0f, 1.4f, 0f) + d * 0.9f;
            int n = Mathf.Max((int)s["leque_tiros"], 1);
            for (int i = 0; i < n; i++)
            {
                float t = n <= 1 ? 0f : (i / (float)(n - 1)) * 2f - 1f;
                // giro em torno de Y a mao (sem Quaternion): o leque e' simetrico, o sentido nao importa
                float ang = s["leque_graus"] * t * Mathf.Deg2Rad;
                float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                k.Lancar(origem, new Vector3(d.x * c + d.z * sn, 0f, -d.x * sn + d.z * c), Elemento.Fogo);
            }
        }

        /// <summary>O PRECO DA SUPREMA: o braco esfria. 4s sem tatica e -15% de velocidade, pelo RELOGIO de estados
        /// (o chip da HUD desliga sozinho — no video de 26/08 "BRACO FRIO" ficou preso do segundo 2 ao 55).</summary>
        public void EstadoAcabou(KitRunner k, string nome)
        {
            if (nome != BRACO_LIVRE) return;
            Dictionary<string, float> s = k.Dados.Suprema;
            k.ForcarCdTatica(s["esfria_dur"]);
            k.BuffVelocidade(s["esfria_vel"], s["esfria_dur"]);
            k.LigarEstado(BRACO_FRIO, s["esfria_dur"]);
        }

        public void DanoRecebido(KitRunner k, float quanto) { }

        /// <summary>LIMITADORES ELEMENTAIS (§14) na MURALHA: agua apaga e molha o braco; vento empurra 3m.</summary>
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            for (int i = _brasas.Count - 1; i >= 0; i--)
            {
                Brasas b = _brasas[i];
                if (!b.EhMuralha) continue;
                float d = KitRunner.DistSegmento(pos, b.A, b.B);
                if (el == Elemento.Agua && d <= t["apaga_raio"])
                {
                    k.AcrescentarCdTatica(t["molhado_cd_extra"]);
                    k.LigarEstado(BRACO_MOLHADO, t["molhado_cd_extra"]);
                    b.Apagar();
                    _brasas.RemoveAt(i);
                }
                else if (el == Elemento.Vento && d <= t["vento_raio"])
                {
                    Vector3 dir = b.Centro - pos;
                    dir.y = 0f;
                    dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
                    b.Empurrar(dir * t["vento_empurra"]);
                }
            }
        }

        // ------------------------------------------------------------------ BRASAS
        /// <summary>
        /// Fogo NO CHAO: a muralha (segmento de 8m) e a poca do dash (A == B) sao o mesmo objeto. Tique proprio de
        /// 0,1s (mobile). A POCA acende o mundo pelo Bus (o terreno reage; este codigo nunca muda o mundo); a
        /// MURALHA nao — passar o orcamento do §14 nela queimaria a ilha a cada 9s.
        /// </summary>
        public sealed class Brasas
        {
            public const float TICK = 0.1f;
            public Vector3 A { get; private set; }
            public Vector3 B { get; private set; }
            public readonly float Espessura, Dano, Rearme, AcesoDur;
            public readonly bool EhMuralha;
            public float Duracao { get; private set; }
            public readonly EfeitoVisual Visual;
            private readonly IEntidade _dono;
            private float _acc;
            private readonly Dictionary<IEntidade, float> _rearme = new Dictionary<IEntidade, float>();
            private readonly List<IEntidade> _vencidos = new List<IEntidade>();

            public Vector3 Centro => (A + B) * 0.5f;
            public bool Viva => Duracao > 0f;

            public Brasas(KitRunner k, Vector3 a, Vector3 b, float duracao, float dano, float espessura, float rearme,
                float acesoDur, bool muralha, bool acendeTerreno)
            {
                A = a; B = b; Duracao = duracao; Dano = dano; Espessura = espessura; Rearme = rearme; AcesoDur = acesoDur;
                EhMuralha = muralha; _dono = k.Dono;
                Visual = k.Visual(muralha ? "muralha" : "poca", a, b, espessura, duracao);
                if (acendeTerreno) Bus.EmitTerrainHit(Elemento.Fogo, Centro, false);
            }

            /// <summary>Devolve false quando a brasa acabou.</summary>
            public bool Tick(KitRunner k, float dt)
            {
                Duracao -= dt;
                if (Duracao <= 0f) { Visual.Restante = 0f; return false; }
                _vencidos.Clear();
                foreach (KeyValuePair<IEntidade, float> kv in new List<KeyValuePair<IEntidade, float>>(_rearme))
                {
                    float r = kv.Value - dt;
                    if (r <= 0f) _vencidos.Add(kv.Key); else _rearme[kv.Key] = r;
                }
                foreach (IEntidade e in _vencidos) _rearme.Remove(e);
                _acc += dt;
                if (_acc < TICK) return true;
                _acc = 0f;
                float alcance = Vector3.Distance(A, B) * 0.5f + Espessura + 1f;
                foreach (IEntidade alvo in k.AlvosPerto(Centro, alcance, _dono))
                {
                    if (_rearme.ContainsKey(alvo)) continue;
                    if (KitRunner.DistSegmento(alvo.Pos, A, B) > Espessura) continue;
                    // `dono` como FONTE: dano de habilidade credita a evolucao do escudo (GDD §5).
                    if (Combat.AplicarDano(alvo, Dano, Elemento.Fogo, _dono) > 0f)
                    {
                        _rearme[alvo] = Rearme;
                        // "quem atravessa sai ACESO (rastro visivel 2s)" — informacao para os outros.
                        k.Visual("aceso", alvo.Pos, alvo.Pos, 0.45f, AcesoDur, alvo);
                    }
                }
                return true;
            }

            public void Apagar() { Duracao = 0f; Visual.Restante = 0f; }

            public void Empurrar(Vector3 delta)
            {
                A += delta; B += delta;
                Visual.Pos = A; Visual.Pos2 = B;
            }
        }
    }
}
