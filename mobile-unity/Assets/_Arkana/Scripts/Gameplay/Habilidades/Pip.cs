using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA PIP (20) — ataque / perseguicao. Ficha em design/personagens/20-pip.md; tempos no DIRECAO.md §4 e §6.
    ///   Passiva  Nunca Pousar — 2 s parada, DEFINHA 2 hp/s (agressao obrigatoria); voa: a agua eletrificada do chao nao a alcanca
    ///   Tatica   Zip-Zag      — tres dashes em zigue-zague (0,8 s entre cada) numa ROTA telegrafada; cada inimigo que um dash
    ///                           ATRAVESSA solta uma faisca que salta para o inimigo mais perto dele
    ///   Suprema  Supercelula  — 8 s de nuvem que a SEGUE e atira raio FRACO no inimigo mais perto DELA
    /// OS LIMITADORES SAO PARTE DO KIT: o SINO do tornozelo denuncia (onda visivel a cada 1,2 s — o counter tambem e' visual);
    /// definha parada; a rota inteira aparece em faisca no toque; a nuvem prioriza o mais PERTO (mirar e' impossivel, a
    /// distancia e' a contra-jogada) e ao acabar chove NELA — encharcada e lenta 2 s, e molhada CONDUZ raio.
    /// Os 55 de vida da ficha sao do CORPO (IdentidadeMago.VidaBase: valem ate' para a Pip bot, que nao tem kit).
    /// ponytail: "ignora lama/agua" nao entrou — o fator de terreno e' do Pawn (Pawn.VelocidadeMaxima), fora do kit.
    /// </summary>
    public sealed class Pip : IHabilidade
    {
        public const string SUPERCELULA = "supercelula";
        public const string ENCHARCADA = "encharcada";

        private readonly List<ApoioGrupoD.RaioGuiado> _raios = new List<ApoioGrupoD.RaioGuiado>();
        private readonly HashSet<IEntidade> _cruzados = new HashSet<IEntidade>();
        private readonly Vector3[] _rota = new Vector3[8];
        private ApoioGrupoD.Investida _dash;
        private int _dashI = -1, _dashes;
        private float _dashAcc, _toqueAcc;
        private Vector3 _ancora;
        private bool _ancorou;
        private float _parada, _definhaAcc, _sinoAcc, _nuvemAcc;

        public IReadOnlyList<ApoioGrupoD.RaioGuiado> Raios => _raios;
        /// <summary>Segundos parada (a partir de `parada_s`, definha).</summary>
        public float Parada => _parada;
        /// <summary>Dash em curso (0..dashes-1); -1 = nenhum.</summary>
        public int DashAtual => _dashI;
        /// <summary>Os pontos da rota telegrafada (0 = de onde saiu).</summary>
        public Vector3 Rota(int i) => _rota[i];

        public void Tick(KitRunner k, float dt)
        {
            Passiva(k, dt);
            if (_dashI >= 0) ZipZag(k, dt);
            if (k.EstadoAtivo(SUPERCELULA)) Nuvem(k, dt);
            for (int i = _raios.Count - 1; i >= 0; i--)
                if (!_raios[i].Tick(dt)) _raios.RemoveAt(i);
        }

        private void Passiva(KitRunner k, float dt)
        {
            Dictionary<string, float> p = k.Dados.Passiva;
            // DEFINHA PARADA: a ancora fica onde ela parou; andou mais que parada_dist, a ancora vai junto e o relogio zera.
            // O DoT e' do ponto unico, no balde do kernel (Balance.Dot.Tick), nunca dano por quadro.
            float r = p["parada_dist"];
            if (!_ancorou || (k.Pos - _ancora).sqrMagnitude > r * r) { _ancora = k.Pos; _ancorou = true; _parada = 0f; }
            else _parada += dt;
            if (_parada >= p["parada_s"])
            {
                _definhaAcc += dt;
                if (_definhaAcc >= Balance.Dot.Tick) { Combat.AplicarDot(k.Dono, p["definha"], _definhaAcc, "electric", null); _definhaAcc = 0f; }
            }
            else _definhaAcc = 0f;
            // VOA: a agua ELETRIFICADA do chao nao a alcanca — devolve a fatia do chao no mesmo tique (molde da Pyra)
            float dps = KitRunner.DpsDoTerreno != null ? KitRunner.DpsDoTerreno(k.Pos) : 0f;
            if (Mathf.Approximately(dps, Balance.Terrain.ElectrifyDps)) k.DevolverDano(Mathf.Min(dps, Balance.Dot.TetoDps) * dt);
            // o SINO do tornozelo toca sempre — com onda visivel (acessibilidade: o counter sonoro tambem se ve')
            _sinoAcc += dt;
            if (_sinoAcc >= p["sino_periodo"]) { _sinoAcc = 0f; k.Visual("pip_sino", k.Pos, k.Pos, 1.6f, 0.8f, k.Dono); }
        }

        // ------------------------------------------------------------------ tatica

        /// <summary>Zip-Zag: a rota INTEIRA e' decidida e desenhada no toque (zig, zag, zig em torno da mira).</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            _dashes = Mathf.Clamp((int)t["dashes"], 1, _rota.Length - 1);
            Vector3 d = k.Mira();
            _rota[0] = k.Pos;
            for (int i = 0; i < _dashes; i++)
            {
                _rota[i + 1] = _rota[i] + ApoioGrupoD.Girar(d, (i % 2 == 0 ? 1f : -1f) * t["zig_graus"]) * t["dash_dist"];
                k.Visual("pip_rota", _rota[i], _rota[i + 1], 0.15f, t["intervalo"] * (i + 1) + 0.2f);
            }
            _dashI = -1;
            ProximoDash(k);
        }

        private void ProximoDash(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            _dashI++;
            if (_dashI >= _dashes) { _dashI = -1; _dash = null; return; }
            Vector3 a = _rota[_dashI], b = _rota[_dashI + 1];
            _dash = new ApoioGrupoD.Investida(k, null, a, (b - a).normalized, t["dash_dist"], 0f);   // o dash leva o corpo junto
            _cruzados.Clear();
            _dashAcc = 0f;
            _toqueAcc = 0f;
        }

        private void ZipZag(KitRunner k, float dt)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            _dash.Andar(dt);
            _dashAcc += dt;
            _toqueAcc += dt;
            if (_toqueAcc >= 0.05f)
            {
                _toqueAcc = 0f;
                IEntidade e;
                // cada inimigo que ESTE dash atravessa solta UMA faisca
                while ((e = _dash.Tocou(k, t["raio_toque"], _cruzados)) != null) { _cruzados.Add(e); Faisca(k, e); }
            }
            if (_dashAcc >= t["intervalo"]) ProximoDash(k);
        }

        /// <summary>A faisca sai de quem foi atravessado e SALTA para o inimigo mais perto DELE; sozinho, cai nele mesmo.</summary>
        private void Faisca(KitRunner k, IEntidade cruzado)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            IEntidade alvo = ApoioGrupoD.MaisProximo(k, cruzado.Pos, t["faisca_raio"], cruzado) ?? cruzado;
            _raios.Add(new ApoioGrupoD.RaioGuiado(k, "pip_faisca", ApoioGrupoD.RaioGuiado.Peito(cruzado), alvo,
                t["faisca_dano"], t["faisca_vel"], 0.1f, 1.4f));
        }

        // ------------------------------------------------------------------ suprema

        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            k.LigarEstado(SUPERCELULA, s["duracao"]);
            k.Visual("pip_nuvem", k.Pos, k.Pos, 1.4f, s["duracao"], k.Dono);
            _nuvemAcc = s["cadencia"];   // o 1o raio sai logo
        }

        private void Nuvem(KitRunner k, float dt)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            _nuvemAcc += dt;
            if (_nuvemAcc < s["cadencia"]) return;
            IEntidade alvo = ApoioGrupoD.MaisProximo(k, k.Pos, s["raio"]);
            if (alvo == null) { _nuvemAcc = s["cadencia"] - 0.1f; return; }   // ninguem perto: procura de novo em 0,1 s
            _nuvemAcc = 0f;
            _raios.Add(new ApoioGrupoD.RaioGuiado(k, "pip_raio", k.Pos + Vector3.up * s["altura"], alvo, s["dano"], s["vel"], 0.14f, 0f));
        }

        /// <summary>O preco: a nuvem chove em cima DELA — encharcada (molhada conduz raio) e lenta 2 s.</summary>
        public void EstadoAcabou(KitRunner k, string nome)
        {
            if (nome != SUPERCELULA) return;
            Dictionary<string, float> s = k.Dados.Suprema;
            Efeitos.Molhar(k.Dono, s["chuva_dur"], s["chuva_vel"]);
            k.LigarEstado(ENCHARCADA, s["chuva_dur"]);
            k.Visual("pip_chuva", k.Pos, k.Pos, 1f, s["chuva_dur"], k.Dono);
        }

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }
    }
}
