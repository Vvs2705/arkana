using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Terrain
{
    /// <summary>Um vidro de pe' na grade: o que a cena DESENHA. Semente = hash deterministico da celula (o mesmo vidro em
    /// duas montagens, como a pedra do muro).</summary>
    public sealed class VidroVisivel
    {
        public int Idx;
        public Vector3 Centro;
        public float Restante;
        public uint Semente;
    }

    /// <summary>
    /// FULGURITO (GDD §16.4, "invencao nossa a partir de ciencia real") — PURO. Raio que cai na AREIA (dunas/praia)
    /// funde o chao em VIDRO: cobertura fragil que TAPA A VISAO e que QUALQUER impacto estilhaca (1 tiro destroi).
    /// Grade propria com a MESMA celula do terreno reativo (Balance.Terrain.CellSize), ligada no Bus.TerrainHit — o
    /// TerrenoReativo nao muda: raio na areia la' nao faz nada (so' agua conduz), aqui vira vidro.
    ///
    /// Regras (ADR-0001, ata 2026-11-19-fulgurito): so' Raio cria; so' em areia; forte = disco RaioForte (3x3);
    /// qualquer elemento numa celula de vidro ESTILHACA (e nao cria outro no mesmo impacto); dura GlassDuration;
    /// teto GlassMax de vidros vivos (orcamento mobile: cheio = o raio nao funde mais, nunca troca o mais velho).
    /// </summary>
    public sealed class Fulgurito
    {
        public const int Seed = 1611;   // a semente do vidro (desenho deterministico); nao e' balanceamento

        public readonly int N;
        public readonly float CellSize;
        private readonly float _meio;
        private readonly Func<float, float, bool> _ehAreia;
        private readonly Dictionary<int, VidroVisivel> _vivos = new Dictionary<int, VidroVisivel>();
        private readonly List<VidroVisivel> _lista = new List<VidroVisivel>();
        private readonly List<int> _expirar = new List<int>();
        private bool _listaSuja;
        private bool _ligado;

        /// <summary>Vidros criados / estilhacados desde o Reset (a casca toca o som; o teste conta).</summary>
        public int Fundidos { get; private set; }
        public int Estilhacados { get; private set; }

        /// <summary>`ehAreia(x, z)` em metros, no centro da celula atingida (a casca passa Dunas/Praia ou a faixa de praia da IlhaMestre).</summary>
        public Fulgurito(float lado, Func<float, float, bool> ehAreia)
        {
            CellSize = Balance.Terrain.CellSize;
            N = Mathf.Max(Mathf.RoundToInt(lado / CellSize), 1);
            _meio = N * CellSize * 0.5f;
            _ehAreia = ehAreia;   // PERGUNTADO so' quando o raio cai (a IlhaMestre tem 1.600x1.600 celulas: pre-classificar custaria segundos)
            Bus.TerrainHit += Reagir;
            _ligado = true;
        }

        /// <summary>Solta o Bus (a casca chama no OnDestroy). Bus.Reset() tambem limpa.</summary>
        public void Desligar()
        {
            if (!_ligado) return;
            Bus.TerrainHit -= Reagir;
            _ligado = false;
        }

        // ------------------------------------------------------------- leitura

        public int Ativos => _vivos.Count;
        public bool EhAreia(int idx) { if (idx < 0 || idx >= N * N || _ehAreia == null) return false; Vector3 c = Centro(idx); return _ehAreia(c.x, c.z); }
        public bool TemVidro(Vector3 pos) => _vivos.ContainsKey(CelulaEm(pos));
        /// <summary>Vidro tapa a visao (e o tiro, quando o projetil perguntar — fora do escopo desta leva).</summary>
        public bool BloqueiaVisao(Vector3 pos) => TemVidro(pos);

        public IReadOnlyList<VidroVisivel> Visiveis
        {
            get
            {
                if (_listaSuja) { _lista.Clear(); _lista.AddRange(_vivos.Values); _listaSuja = false; }
                return _lista;
            }
        }

        public int CelulaEm(Vector3 pos)
        {
            int cx = Mathf.FloorToInt((pos.x + _meio) / CellSize);
            int cz = Mathf.FloorToInt((pos.z + _meio) / CellSize);
            if (cx < 0 || cz < 0 || cx >= N || cz >= N) return -1;
            return cz * N + cx;
        }

        /// <summary>Centro da celula no plano (y = 0: a casca assenta no chao).</summary>
        public Vector3 Centro(int idx) => new Vector3((idx % N + 0.5f) * CellSize - _meio, 0f, (idx / N + 0.5f) * CellSize - _meio);

        // ------------------------------------------------------------- reacao

        /// <summary>Bus.TerrainHit. Sincrono: o teste chama direto.</summary>
        public void Reagir(Elemento el, Vector3 pos, bool forte)
        {
            int c = CelulaEm(pos);
            if (c < 0) return;
            // 1 TIRO DESTROI: qualquer elemento no vidro o estilhaca — e o mesmo impacto nao funde outro ali.
            // EXCECAO (ADR-0001 adendo ciclo 2): vidro NASCENDO (< GlassNascer) ignora RAIO — a rajada que o fundiu (a suprema do
            // Fizz cai a cada 3 m no mesmo quadro) nao o quebra; o disco forte segue fundindo o resto. Outro elemento quebra sempre.
            if (_vivos.TryGetValue(c, out VidroVisivel atingido))
            {
                if (el != Elemento.Raio || !Nascendo(atingido)) { Estilhacar(c); return; }
                if (!forte) return;   // raio fraco no proprio vidro nascendo: nada (nem renova)
            }
            if (el != Elemento.Raio) return;
            if (!forte) { Fundir(c); return; }
            int cx = c % N, cz = c / N, r = Mathf.CeilToInt(TerrenoReativo.RaioForte);
            float r2 = TerrenoReativo.RaioForte * TerrenoReativo.RaioForte;
            for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dz * dz > r2) continue;
                    int x = cx + dx, z = cz + dz;
                    if (x >= 0 && z >= 0 && x < N && z < N) Fundir(z * N + x);
                }
        }

        /// <summary>Ainda subindo do chao (a mesma janela da animacao da casca).</summary>
        public static bool Nascendo(VidroVisivel v) => Balance.Terrain.GlassDuration - v.Restante < Balance.Terrain.GlassNascer;

        private void Fundir(int idx)
        {
            if (_vivos.ContainsKey(idx) || !EhAreia(idx)) return;
            if (_vivos.Count >= Balance.Terrain.GlassMax) return;   // teto de orcamento: cheio, o raio nao funde
            _vivos[idx] = new VidroVisivel { Idx = idx, Centro = Centro(idx), Restante = Balance.Terrain.GlassDuration, Semente = SementeDe(idx) };
            _listaSuja = true;
            Fundidos++;
        }

        private void Estilhacar(int idx)
        {
            if (!_vivos.Remove(idx)) return;
            _listaSuja = true;
            Estilhacados++;
        }

        /// <summary>1x por frame: o vidro some sozinho depois de GlassDuration.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0f) || _vivos.Count == 0) return;
            _expirar.Clear();
            foreach (VidroVisivel v in _vivos.Values)
            {
                v.Restante -= dt;
                if (v.Restante <= 0f) _expirar.Add(v.Idx);
            }
            for (int i = 0; i < _expirar.Count; i++) _vivos.Remove(_expirar[i]);
            if (_expirar.Count > 0) _listaSuja = true;
        }

        /// <summary>Zera o estado transitorio (match_started).</summary>
        public void Reset()
        {
            _vivos.Clear(); _listaSuja = true;
            Fundidos = 0; Estilhacados = 0;
        }

        public static uint SementeDe(int idx)
        {
            unchecked
            {
                uint h = (uint)Seed * 73856093u ^ (uint)idx * 19349663u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return h;
            }
        }
    }
}
