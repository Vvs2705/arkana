using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Terrain
{
    /// <summary>Material da celula, escrito 1x na montagem.</summary>
    public enum TipoCelula : byte { Fora, Chao, Combustivel, Agua }

    /// <summary>Estado da celula — e' aqui que a magia mexe (GDD §14).</summary>
    public enum EstadoCelula : byte { Normal, Queimando, Carvao, Congelado, Eletrificado, Lama, Muro }

    /// <summary>
    /// O que a cena DESENHA de uma celula fora do normal: fogo, carvao, gelo, lamina eletrica, lama, muro.
    /// Restante 0 = permanente (carvao). Semente = numero deterministico por celula (seed 907) para a pedra do
    /// muro nascer IGUAL em duas montagens — o "caixote bege" de 26/08 vira rocha por noise, nunca por sorteio.
    /// </summary>
    public sealed class CelulaVisivel
    {
        public int Idx;
        public Vector3 Centro;
        public EstadoCelula Estado;
        public float Restante;
        public float MuroHp;
        public uint Semente;
    }

    /// <summary>
    /// TERRENO REATIVO (GDD §14, o pilar) — espelho de terrain/TerrainSystem.gd, PURO. Grade logica de celulas
    /// (Balance.Terrain.CellSize) sobre a ilha. O gameplay EMITE Bus.TerrainHit no impacto; AQUI o mundo muda:
    /// fogo acende a floresta, agua apaga/congela/faz lama, raio eletrifica a agua CONECTADA, terra ergue muro
    /// destrutivel, vento espalha o fogo PAGANDO do mesmo orcamento.
    ///
    /// FOGO POR ORCAMENTO (lei com medicao — nota de 19/08): UMA rolagem por ARESTA + FuelBudget por ignicao.
    /// Chance por tique = 99,67% acumulado = 380/380 celulas carbonizadas. Para regular o incendio, mexa no
    /// ORCAMENTO — nunca na chance. `Rolagens` conta as rolagens: o teste prova que nao crescem com o relogio.
    ///
    /// DANO: ambiental passa pelo ponto unico (Combat.AplicarDot, fonte null), no balde de Balance.Dot.Tick.
    /// </summary>
    public sealed class TerrenoReativo
    {
        /// <summary>s por passo da simulacao — KNOB.</summary>
        public const float TICK = 0.5f;
        /// <summary>Seed FIXA da pedra do muro: determinismo por contrato (2 montagens = o MESMO muro).</summary>
        public const int WallSeed = 907;
        /// <summary>Raio (celulas) de sementes/muro da magia forte: 3x3.</summary>
        public const float RaioForte = 1.5f;
        public const float RaioSplash = 1.5f;
        public const float RaioSplashForte = 2.5f;
        /// <summary>Guarda de perf do flood eletrico (nao e' balanceamento).</summary>
        public const int LimiteFlood = 200;

        public readonly int N;
        public readonly float CellSize;
        private readonly float _meio;
        private readonly Func<float, float, float> _altura;
        private readonly TipoCelula[] _tipo;
        private readonly EstadoCelula[] _estado;
        private readonly float[] _muroHp;
        private readonly System.Random _rng;

        private sealed class Frente { public int Id, Orcamento, Vivas; }
        private readonly Dictionary<int, Frente> _frentes = new Dictionary<int, Frente>();
        private readonly int[] _frenteDe;                                   // idx aceso -> id da frente (0 = nenhuma)
        private readonly HashSet<int> _pendentes = new HashSet<int>();      // ainda deve a sua UNICA rolagem por aresta
        private readonly HashSet<int> _queimando = new HashSet<int>();
        private readonly Dictionary<int, List<int>> _arvores = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, CelulaVisivel> _vis = new Dictionary<int, CelulaVisivel>();
        private readonly List<CelulaVisivel> _visLista = new List<CelulaVisivel>();
        private bool _visSuja;
        private int _proximaFrente;
        private float _acc, _dotAcc;
        private IList<IEntidade> _alvos;
        private bool _ligado;

        /// <summary>Casca: arvore `i` queimou (true) / voltou no restart (false) — Vegetacao.MarcarQueimada.</summary>
        public Action<int, bool> AoQueimarArvore;
        /// <summary>Rolagens de aresta feitas desde o Reset (a prova de "uma por aresta").</summary>
        public int Rolagens { get; private set; }
        /// <summary>Celulas acesas por PROPAGACAO (rolagem ou vento) desde o Reset — nunca passa de FuelBudget por frente.</summary>
        public int TotalEspalhado { get; private set; }

        /// <summary>
        /// `tipoDe(cx, cz)` classifica a celula; null = deriva do relevo (BiomaEm quando e' a Relevo real; senao
        /// agua/raio de terra do IRelevo). `lado` em metros (o relevo real tem Lado; o teste passa o que quiser).
        /// </summary>
        public TerrenoReativo(IRelevo relevo, float lado, int seed, Func<int, int, TipoCelula> tipoDe = null)
        {
            CellSize = Balance.Terrain.CellSize;
            N = Mathf.Max(Mathf.RoundToInt(lado / CellSize), 1);
            _meio = N * CellSize * 0.5f;
            _altura = relevo != null ? (Func<float, float, float>)relevo.Altura : ((x, z) => 0f);
            _tipo = new TipoCelula[N * N];
            _estado = new EstadoCelula[N * N];
            _muroHp = new float[N * N];
            _frenteDe = new int[N * N];
            _rng = new System.Random(seed);
            Relevo real = relevo as Relevo;
            for (int cz = 0; cz < N; cz++)
                for (int cx = 0; cx < N; cx++)
                {
                    TipoCelula t;
                    if (tipoDe != null) t = tipoDe(cx, cz);
                    else
                    {
                        Vector3 c = Centro(cz * N + cx);
                        if (relevo == null) t = TipoCelula.Chao;
                        else if (real != null)
                        {
                            Bioma b = real.BiomaEm(c.x, c.z);
                            t = b == Bioma.Agua ? TipoCelula.Agua
                                : new Vector2(c.x, c.z).magnitude > relevo.RaioTerra ? TipoCelula.Fora
                                : b == Bioma.Floresta ? TipoCelula.Combustivel : TipoCelula.Chao;
                        }
                        else t = relevo.SuperficieDaAgua(c.x, c.z) != Relevo.Seco ? TipoCelula.Agua
                            : new Vector2(c.x, c.z).magnitude > relevo.RaioTerra ? TipoCelula.Fora : TipoCelula.Chao;
                    }
                    _tipo[cz * N + cx] = t;
                }
            Bus.TerrainHit += Reagir;
            _ligado = true;
        }

        /// <summary>A ilha real: grade do tamanho do Lado dela, biomas de BiomaEm.</summary>
        public static TerrenoReativo Da(Relevo relevo, int seed) => new TerrenoReativo(relevo, relevo.Lado, seed);

        /// <summary>Solta o Bus (a casca chama no OnDestroy). Bus.Reset() tambem limpa.</summary>
        public void Desligar()
        {
            if (!_ligado) return;
            Bus.TerrainHit -= Reagir;
            _ligado = false;
        }

        /// <summary>Arvore da ilha: a celula vira combustivel e o carvao a consome (AoQueimarArvore).</summary>
        public void RegistrarArvore(int i, Vector3 pos)
        {
            int idx = CelulaEm(pos);
            if (idx < 0) return;
            _tipo[idx] = TipoCelula.Combustivel;
            List<int> l;
            if (!_arvores.TryGetValue(idx, out l)) { l = new List<int>(); _arvores[idx] = l; }
            l.Add(i);
        }

        // ------------------------------------------------------------- leitura

        public TipoCelula Tipo(int idx) => _tipo[idx];
        public EstadoCelula Estado(int idx) => _estado[idx];
        public EstadoCelula EstadoEm(Vector3 pos) { int i = CelulaEm(pos); return i < 0 ? EstadoCelula.Normal : _estado[i]; }
        public int Queimando => _queimando.Count;
        public int Idx(int cx, int cz) => cz * N + cx;

        /// <summary>Celulas fora do normal, para a cena desenhar.</summary>
        public IReadOnlyList<CelulaVisivel> Visiveis
        {
            get
            {
                if (_visSuja) { _visLista.Clear(); _visLista.AddRange(_vis.Values); _visSuja = false; }
                return _visLista;
            }
        }

        /// <summary>dps do perigo no ponto: queimando = BurnDps, eletrificado = ElectrifyDps, resto 0.</summary>
        public float DpsEm(Vector3 pos)
        {
            int idx = CelulaEm(pos);
            if (idx < 0) return 0f;
            switch (_estado[idx])
            {
                case EstadoCelula.Queimando: return Balance.Terrain.BurnDps;
                case EstadoCelula.Eletrificado: return Balance.Terrain.ElectrifyDps;
            }
            return 0f;
        }

        /// <summary>Fator de TERRENO do produto unico de velocidade (lama = MudSlow). Nunca 0.</summary>
        public float FatorTerreno(Vector3 pos)
        {
            int idx = CelulaEm(pos);
            return idx >= 0 && _estado[idx] == EstadoCelula.Lama ? Balance.Terrain.MudSlow : 1f;
        }

        /// <summary>Muro e' cobertura: bloqueia tiro. Carvao NAO — a cobertura sumiu de verdade.</summary>
        public bool BloqueiaTiro(Vector3 pos)
        {
            int idx = CelulaEm(pos);
            return idx >= 0 && _estado[idx] == EstadoCelula.Muro;
        }

        /// <summary>Agua liquida nao se anda (nado e' outra pergunta); congelada vira ROTA. Muro nao se atravessa.</summary>
        public bool Caminhavel(Vector3 pos)
        {
            int idx = CelulaEm(pos);
            if (idx < 0) return false;
            if (_estado[idx] == EstadoCelula.Muro) return false;
            if (_tipo[idx] == TipoCelula.Agua) return _estado[idx] == EstadoCelula.Congelado;
            return _tipo[idx] != TipoCelula.Fora;
        }

        public int CelulaEm(Vector3 pos)
        {
            int cx = Mathf.FloorToInt((pos.x + _meio) / CellSize);
            int cz = Mathf.FloorToInt((pos.z + _meio) / CellSize);
            if (cx < 0 || cz < 0 || cx >= N || cz >= N) return -1;
            return cz * N + cx;
        }

        public Vector3 Centro(int idx)
        {
            float x = (idx % N + 0.5f) * CellSize - _meio;
            float z = (idx / N + 0.5f) * CellSize - _meio;
            return new Vector3(x, _altura(x, z), z);
        }

        // ------------------------------------------------------------- reacoes

        /// <summary>Reacao a um impacto (Bus.TerrainHit). Sincrono: o teste chama direto.</summary>
        public void Reagir(Elemento el, Vector3 pos, bool forte)
        {
            int c = CelulaEm(pos);
            if (c < 0) return;
            if (_estado[c] == EstadoCelula.Muro)
            {
                // muro e' cobertura DESTRUTIVEL: qualquer elemento o danifica, x Estrutura do elemento (terra 2.0, raio 1.6)
                Balance.PerfilElemento p = Balance.Perfil(el);
                DanificarMuro(c, p.Dmg * p.Estrutura);
                return;
            }
            switch (el)
            {
                case Elemento.Fogo: AplicarFogo(c, forte ? RaioForte : 0f); break;
                case Elemento.Agua: AplicarAgua(c, forte ? RaioSplashForte : RaioSplash); break;
                case Elemento.Raio: AplicarRaio(c, RaioSplash); break;
                case Elemento.Terra: AplicarTerra(c, forte); break;
                case Elemento.Vento: AplicarVento(c, RaioSplash); break;
            }
        }

        /// <summary>FOGO: acende a cobertura (abre uma FRENTE com orcamento proprio), derrete o gelo e seca a lama.
        /// As sementes do impacto sao de graca — o orcamento cobre a PROPAGACAO, nao o impacto.</summary>
        private void AplicarFogo(int c, float r)
        {
            Frente f = null;
            foreach (int idx in Disco(c, r))
            {
                if (_estado[idx] == EstadoCelula.Congelado || _estado[idx] == EstadoCelula.Lama) SetEstado(idx, EstadoCelula.Normal, 0f);
                else if (Inflamavel(idx))
                {
                    if (f == null) f = NovaFrente();
                    Acender(idx, f, false);
                }
            }
        }

        /// <summary>AGUA: apaga o fogo (apagar != carbonizar), congela a agua (vira ROTA e corta a conducao) e faz
        /// LAMACAL no chao de terra (GDD §14: lentidao severa na area).</summary>
        private void AplicarAgua(int c, float r)
        {
            foreach (int idx in Disco(c, r))
            {
                EstadoCelula s = _estado[idx];
                if (s == EstadoCelula.Queimando) SetEstado(idx, EstadoCelula.Normal, 0f);
                else if (_tipo[idx] == TipoCelula.Agua && (s == EstadoCelula.Normal || s == EstadoCelula.Eletrificado))
                    SetEstado(idx, EstadoCelula.Congelado, Balance.Terrain.FreezeDuration);
                else if (_tipo[idx] == TipoCelula.Chao && (s == EstadoCelula.Normal || s == EstadoCelula.Lama))
                    SetEstado(idx, EstadoCelula.Lama, Balance.Terrain.MudDuration);
            }
        }

        /// <summary>RAIO: eletrifica TODA a agua conectada (flood 4-vizinhos). Gelo e terra isolam.</summary>
        private void AplicarRaio(int c, float r)
        {
            int fonte = -1;
            if (Liquida(c)) fonte = c;
            else foreach (int idx in Disco(c, r)) if (Liquida(idx)) { fonte = idx; break; }
            if (fonte < 0) return;
            float dur = Balance.Terrain.ElectrifyDuration;
            var fila = new List<int> { fonte };
            var visto = new HashSet<int> { fonte };
            int cabeca = 0;
            while (cabeca < fila.Count && cabeca < LimiteFlood)
            {
                int idx = fila[cabeca++];
                SetEstado(idx, EstadoCelula.Eletrificado, dur);   // refresca se ja' estava
                foreach (int nb in Vizinhos(idx))
                    if (Liquida(nb) && visto.Add(nb)) fila.Add(nb);
            }
        }

        /// <summary>TERRA: ergue muro — cobertura destrutivel com hp e prazo. NAO nasce em celula OCUPADA (emparedar
        /// gente por WallDuration e' botao de deletar, nao jogada — provado no projeto-mae).</summary>
        private void AplicarTerra(int c, bool forte)
        {
            HashSet<int> ocupadas = null;
            foreach (int idx in forte ? Disco(c, RaioForte) : new List<int> { c })
            {
                if (_estado[idx] != EstadoCelula.Normal) continue;
                TipoCelula m = _tipo[idx];
                if (m != TipoCelula.Chao && !(m == TipoCelula.Combustivel && !_arvores.ContainsKey(idx))) continue;
                if (ocupadas == null) ocupadas = CelulasOcupadas();
                if (ocupadas.Contains(idx)) continue;
                _muroHp[idx] = Balance.Terrain.WallHp;
                SetEstado(idx, EstadoCelula.Muro, Balance.Terrain.WallDuration);
            }
        }

        /// <summary>VENTO em fogo: espalha SEM rolagem — mas PAGA do MESMO orcamento da frente.</summary>
        private void AplicarVento(int c, float r)
        {
            foreach (int idx in Disco(c, r))
            {
                if (_estado[idx] != EstadoCelula.Queimando) continue;
                Frente f;
                if (!_frentes.TryGetValue(_frenteDe[idx], out f)) continue;
                foreach (int nb in Vizinhos(idx)) Acender(nb, f, true);
            }
        }

        /// <summary>Quem esta' DE PE em cada celula (os alvos do ultimo Tick). ponytail: celula do centro do corpo.</summary>
        private HashSet<int> CelulasOcupadas()
        {
            var saida = new HashSet<int>();
            if (_alvos == null) return saida;
            for (int i = 0; i < _alvos.Count; i++)
            {
                IEntidade e = _alvos[i];
                if (e == null || e.Vital == null || !e.Vital.Viva) continue;
                int idx = CelulaEm(e.Pos);
                if (idx >= 0) saida.Add(idx);
            }
            return saida;
        }

        private void DanificarMuro(int idx, float dano)
        {
            if (_estado[idx] != EstadoCelula.Muro || !(dano > 0f)) return;
            _muroHp[idx] -= dano;
            CelulaVisivel v;
            if (_vis.TryGetValue(idx, out v)) v.MuroHp = _muroHp[idx];
            if (_muroHp[idx] <= 0f) SetEstado(idx, EstadoCelula.Normal, 0f);
        }

        // ----------------------------------------------------------- fogo (a lei)

        private Frente NovaFrente()
        {
            var f = new Frente { Id = ++_proximaFrente, Orcamento = Balance.Terrain.FuelBudget };
            _frentes[f.Id] = f;
            return f;
        }

        /// <summary>Acende `idx` na frente `f`. `cobrado` = veio da PROPAGACAO (aresta ou vento): SO' ela paga.</summary>
        private bool Acender(int idx, Frente f, bool cobrado)
        {
            if (!Inflamavel(idx)) return false;
            if (cobrado)
            {
                if (f.Orcamento <= 0) return false;   // teto duro: a frente para mesmo cercada de floresta
                f.Orcamento--;
                TotalEspalhado++;
            }
            f.Vivas++;
            _frenteDe[idx] = f.Id;
            _pendentes.Add(idx);   // fara' a sua UNICA rolagem por aresta no proximo passo
            SetEstado(idx, EstadoCelula.Queimando, Balance.Terrain.BurnDuration);
            return true;
        }

        /// <summary>Celula deixou de queimar: devolve a vaga; a ultima chama fecha o incendio. Ponto UNICO.</summary>
        private void SairDoFogo(int idx)
        {
            _pendentes.Remove(idx);
            Frente f;
            int id = _frenteDe[idx];
            _frenteDe[idx] = 0;
            if (!_frentes.TryGetValue(id, out f)) return;
            if (--f.Vivas <= 0) _frentes.Remove(id);
        }

        // ------------------------------------------------------------ simulacao

        /// <summary>
        /// 1x por frame: acumula passos de TICK e cobra o DoT ambiental dos `alvos` no balde de Balance.Dot.Tick
        /// (nunca dano por frame), pelo ponto unico, com fonte null.
        /// </summary>
        public void Tick(float dt, IList<IEntidade> alvos)
        {
            _alvos = alvos;
            if (!(dt > 0f)) return;
            _acc += dt;
            while (_acc >= TICK) { _acc -= TICK; Simular(TICK); }
            _dotAcc += dt;
            if (_dotAcc < Balance.Dot.Tick) return;
            if (alvos != null)
                for (int i = 0; i < alvos.Count; i++)
                {
                    IEntidade e = alvos[i];
                    if (e == null) continue;
                    int idx = CelulaEm(e.Pos);
                    if (idx < 0) continue;
                    if (_estado[idx] == EstadoCelula.Queimando)
                        Combat.AplicarDot(e, Balance.Terrain.BurnDps, _dotAcc, "burn", null);
                    else if (_estado[idx] == EstadoCelula.Eletrificado)
                        Combat.AplicarDot(e, Balance.Terrain.ElectrifyDps, _dotAcc, "electric", null);
                }
            _dotAcc = 0f;
        }

        /// <summary>Um passo. `dt = 0` = relogio parado (pior caso do teste: nada expira; quem segura a frente e' SO' o orcamento).</summary>
        public void Simular(float dt)
        {
            foreach (int idx in new List<int>(_queimando))
            {
                if (_estado[idx] != EstadoCelula.Queimando || !_pendentes.Remove(idx)) continue;
                // UMA ROLAGEM POR ARESTA, e nunca mais — a frente avanca um anel por passo em vez de
                // sentenciar a floresta inteira.
                Frente f;
                if (!_frentes.TryGetValue(_frenteDe[idx], out f)) continue;
                foreach (int nb in Vizinhos(idx))
                {
                    if (f.Orcamento <= 0 || !Inflamavel(nb)) continue;
                    Rolagens++;
                    if (_rng.NextDouble() < Balance.Terrain.EdgeChance) Acender(nb, f, true);
                }
            }
            if (!(dt > 0f)) return;
            foreach (CelulaVisivel v in new List<CelulaVisivel>(_vis.Values))
            {
                if (!(v.Restante > 0f)) continue;   // 0 = permanente (carvao)
                v.Restante -= dt;
                if (v.Restante <= 0f) Expirar(v.Idx);
            }
        }

        private void Expirar(int idx)
        {
            if (_estado[idx] == EstadoCelula.Queimando) SetEstado(idx, EstadoCelula.Carvao, 0f);   // queimou ate' o fim: CARVAO
            else SetEstado(idx, EstadoCelula.Normal, 0f);                                          // derreteu / muro caiu / secou / descarregou
        }

        /// <summary>Escreve o estado, agenda a expiracao e avisa a cena. Ponto UNICO de mudanca; TerrainChanged na BORDA.</summary>
        private void SetEstado(int idx, EstadoCelula s, float duracao)
        {
            EstadoCelula velho = _estado[idx];
            if (velho == EstadoCelula.Queimando && s != EstadoCelula.Queimando) { SairDoFogo(idx); _queimando.Remove(idx); }
            if (velho == EstadoCelula.Muro && s != EstadoCelula.Muro) _muroHp[idx] = 0f;
            _estado[idx] = s;
            if (s == EstadoCelula.Normal)
            {
                if (_vis.Remove(idx)) _visSuja = true;
            }
            else
            {
                CelulaVisivel v;
                if (!_vis.TryGetValue(idx, out v))
                {
                    v = new CelulaVisivel { Idx = idx, Centro = Centro(idx), Semente = SementeDoMuro(idx) };
                    _vis[idx] = v;
                    _visSuja = true;
                }
                v.Estado = s; v.Restante = duracao; v.MuroHp = _muroHp[idx];
            }
            if (s == EstadoCelula.Queimando) _queimando.Add(idx);
            if (s == EstadoCelula.Carvao) QueimarArvores(idx, true);
            if (velho != s) Bus.EmitTerrainChanged(Nome(s), Centro(idx));
        }

        private void QueimarArvores(int idx, bool queimada)
        {
            List<int> l;
            if (AoQueimarArvore == null || !_arvores.TryGetValue(idx, out l)) return;
            for (int i = 0; i < l.Count; i++) AoQueimarArvore(l[i], queimada);
        }

        /// <summary>Zera TODO o estado transitorio e restaura a floresta (match_started / entre blocos de teste).</summary>
        public void Reset()
        {
            for (int i = 0; i < _estado.Length; i++)
            {
                if (_estado[i] == EstadoCelula.Carvao) QueimarArvores(i, false);
                _estado[i] = EstadoCelula.Normal; _muroHp[i] = 0f; _frenteDe[i] = 0;
            }
            _frentes.Clear(); _pendentes.Clear(); _queimando.Clear(); _vis.Clear(); _visSuja = true;
            _acc = 0f; _dotAcc = 0f; Rolagens = 0; TotalEspalhado = 0;
        }

        // ------------------------------------------------------------------ grade

        private bool Liquida(int idx) => _tipo[idx] == TipoCelula.Agua && _estado[idx] != EstadoCelula.Congelado;
        private bool Inflamavel(int idx) => _tipo[idx] == TipoCelula.Combustivel && _estado[idx] == EstadoCelula.Normal;

        private List<int> Disco(int c, float r)
        {
            var saida = new List<int>();
            int cx = c % N, cz = c / N, ri = Mathf.CeilToInt(r);
            for (int dz = -ri; dz <= ri; dz++)
                for (int dx = -ri; dx <= ri; dx++)
                {
                    if (dx * dx + dz * dz > r * r) continue;
                    int x = cx + dx, z = cz + dz;
                    if (x >= 0 && z >= 0 && x < N && z < N) saida.Add(z * N + x);
                }
            return saida;
        }

        private List<int> Vizinhos(int c)
        {
            var saida = new List<int>(4);
            int cx = c % N;
            if (cx > 0) saida.Add(c - 1);
            if (cx < N - 1) saida.Add(c + 1);
            if (c >= N) saida.Add(c - N);
            if (c < N * (N - 1)) saida.Add(c + N);
            return saida;
        }

        /// <summary>Hash deterministico (WallSeed, idx): a pedra do muro e' a mesma em qualquer montagem.</summary>
        public static uint SementeDoMuro(int idx)
        {
            unchecked
            {
                uint h = (uint)WallSeed * 73856093u ^ (uint)idx * 19349663u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return h;
            }
        }

        private static string Nome(EstadoCelula s)
        {
            switch (s)
            {
                case EstadoCelula.Queimando: return "burn";
                case EstadoCelula.Carvao: return "ash";
                case EstadoCelula.Congelado: return "ice";
                case EstadoCelula.Eletrificado: return "electric";
                case EstadoCelula.Lama: return "mud";
                case EstadoCelula.Muro: return "wall";
            }
            return "clear";
        }
    }
}
