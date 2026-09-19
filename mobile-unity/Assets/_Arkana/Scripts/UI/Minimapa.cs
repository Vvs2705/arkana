using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.Menu;
using Arkana.World;

namespace Arkana.UI
{
    /// <summary>
    /// Conta PURA do mapa (minimapa, mapa grande e bussola). Norte = +z (o yaw 0 da camera), leste = +x; na tela o norte
    /// fica EM CIMA e o mapa NAO gira — a seta gira. E a PINTURA da ilha num Color32[]: pura, roda fora da thread
    /// principal (o jogo nao trava esperando) e no teste EditMode.
    /// </summary>
    public static class MapaLogica
    {
        /// <summary>Texels por lado: ~1,3 m por texel na ilha de 600 m — o mapa grande (quase a tela) ainda le' o morro.</summary>
        public const int TexturaLado = 512;
        /// <summary>A textura cobre a ilha e um anel de mar em volta: Relevo.Lado x isto.</summary>
        public const float MargemMar = 1.1f;
        /// <summary>Metros de chao que o minimapa mostra de borda a borda depois do pouso. KNOB por foto: menor = mais perto.</summary>
        public const float VistaLocalM = 240f;
        /// <summary>Graus de cada lado do centro da bussola (a faixa mostra 180 graus).</summary>
        public const float MeiaBussola = 90f;
        /// <summary>SOMBREADO de relevo: o morro exagerado (a ilha tem 28 m de altura em 600 m: sem exagero a sombra nem
        /// aparece) e o quanto ela pesa na cor. KNOB por foto.</summary>
        public const float Exagero = 4f, ForcaSombra = 1f;

        // paleta do MAPA: a do chao (Relevo) e a da agua (Ilha.MontarAgua), chapada por bioma — mapa e' leitura, nao foto
        static readonly Color MarRaso = Relevo.Hex(0x3a9fe0), MarFundo = Relevo.Hex(0x123f86), Espuma = Relevo.Hex(0xd8f3ff);
        static readonly Color LagoRaso = Relevo.Hex(0x35b0f2), LagoFundo = Relevo.Hex(0x0f4f9e);
        static readonly Color BrejoRaso = Relevo.Hex(0x4ba589), BrejoFundo = Relevo.Hex(0x1e6b62);
        /// <summary>Floresta vista do alto = COPA escura em tufos (o chao de mata do terreno fica debaixo das arvores).</summary>
        static readonly Color Copa = Relevo.Hex(0x3a8f4f), CopaFunda = Relevo.Hex(0x1d5431);
        static readonly Color Lama = Relevo.Escurecer(Relevo.CorLama, 0.2f);
        /// <summary>O fundo da janela (fora da textura e antes dela ficar pronta): o mar fundo, sem emenda.</summary>
        public static Color Fundo => MarFundo;

        /// <summary>Metros de mundo que a textura cobre de lado a lado (centrada na origem). Sem ilha: a padrao.</summary>
        public static float Extensao(Relevo r) => (r != null ? r.Lado : Relevo.BaseLado * 2f) * MargemMar;

        /// <summary>Mundo -> uv 0..1 da textura (linha 0 = sul).</summary>
        public static Vector2 Uv(Vector3 p, float extensao) => new Vector2(p.x / extensao + 0.5f, p.z / extensao + 0.5f);

        /// <summary>Mundo -> px na janela, com a origem no MEIO dela. `escala` = px por metro.</summary>
        public static Vector2 NaJanela(Vector3 p, Vector3 centro, float escala) =>
            new Vector2((p.x - centro.x) * escala, (p.z - centro.z) * escala);

        /// <summary>Janela uv (x, y, lado) de `vistaM` metros centrada em `centro`: o uvRect do minimapa.</summary>
        public static Rect UvDaJanela(Vector3 centro, float vistaM, float extensao)
        {
            Vector2 c = Uv(centro, extensao);
            float s = vistaM / extensao;
            return new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s, s);
        }

        /// <summary>O centro da janela ANDA com o zoom: ilha inteira (castelo, queda) = centro da ilha; perto = o jogador.</summary>
        public static Vector3 CentroDaJanela(Vector3 jogador, float vistaM, float extensao)
        {
            float k = Mathf.InverseLerp(extensao, VistaLocalM, vistaM);
            return new Vector3(jogador.x * k, 0f, jogador.z * k);
        }

        /// <summary>Rumo (graus, 0 = norte, horario) de `para` visto de `de`.</summary>
        public static float Rumo(Vector3 de, Vector3 para) => Mathf.Atan2(para.x - de.x, para.z - de.z) * Mathf.Rad2Deg;

        /// <summary>Onde um rumo cai na faixa da bussola para a camera em `yaw`: -1 (borda esquerda) .. 1 (direita).</summary>
        public static float NaBussola(float rumo, float yaw, float meia = MeiaBussola) => Mathf.DeltaAngle(yaw, rumo) / meia;

        /// <summary>Alfa de uma marca da bussola: inteira no meio, esvaece perto da borda (nada corta seco).</summary>
        public static float AlfaNaBussola(float f)
        {
            float t = Mathf.Clamp01((Mathf.Abs(f) - 0.62f) / 0.38f);
            return 1f - t * t * (3f - 2f * t);
        }

        /// <summary>Giro (graus no eixo z da UI, anti-horario) da seta do jogador: o yaw da camera e' horario a partir do norte.</summary>
        public static float GiroDaSeta(float yaw) => -yaw;

        /// <summary>
        /// A ILHA PINTADA: `n` x `n` texels, linha 0 = sul. UMA Relevo.Altura por texel; declive, sombra e praia saem da
        /// GRADE (vizinhos), nao de chamadas novas. Floresta = copa escura, pico = pedra clara, ruinas = pedra gasta, mar do
        /// raso ao fundo com espuma na costa. SOMBREADO de relevo com luz do NOROESTE a 45 graus (a convencao de carta:
        /// morro le' como morro, nao como mancha). So' le' o Relevo (imutavel): pode rodar em outra thread.
        /// </summary>
        public static Color32[] PintarIlha(Relevo r, int n)
        {
            float ext = Extensao(r), passo = ext / n, meio = ext * 0.5f;
            var alt = new float[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                    alt[j * n + i] = r.Altura((i + 0.5f) * passo - meio, (j + 0.5f) * passo - meio);
            var px = new Color32[n * n];
            int k = Mathf.Max(1, Mathf.RoundToInt(4.5f * r.Escala / passo));   // declive AMPLO (~9 m): o mesmo do Relevo.PesosEm
            float ex = Exagero / (2f * passo);
            for (int j = 0; j < n; j++)
            {
                float z = (j + 0.5f) * passo - meio;
                int jc = j * n, jn = Mathf.Min(j + 1, n - 1) * n, js = Mathf.Max(j - 1, 0) * n;
                int jN = Mathf.Min(j + k, n - 1) * n, jS = Mathf.Max(j - k, 0) * n;
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) * passo - meio, h = alt[jc + i];
                    Color c;
                    if (Agua(r, x, z, h, out c)) { px[jc + i] = c; continue; }   // agua e' plana: sem sombra
                    float declive = Mathf.Max(Mathf.Abs(alt[jc + Mathf.Min(i + k, n - 1)] - alt[jc + Mathf.Max(i - k, 0)]),
                                              Mathf.Abs(alt[jN + i] - alt[jS + i])) / (2f * k * passo);
                    c = Chao(r, x, z, h, declive);
                    float s = Sombra((alt[jc + Mathf.Min(i + 1, n - 1)] - alt[jc + Mathf.Max(i - 1, 0)]) * ex, (alt[jn + i] - alt[js + i]) * ex);
                    px[jc + i] = new Color(c.r * s, c.g * s, c.b * s, 1f);   // o Color32 satura em 1
                }
            }
            return px;
        }

        /// <summary>
        /// O SOMBREADO de um texel, multiplicador da cor: 1 = chao plano, &gt; 1 encosta virada para a luz (NOROESTE, 45 graus
        /// acima), &lt; 1 a de costas. `dx`/`dz` = subida por metro para leste/norte, ja' exagerada.
        /// </summary>
        public static float Sombra(float dx, float dz)
        {
            // normal (-dx, 1, -dz) . luz (-0,5; 0,707; 0,5), dividida pela do chao plano (0,707)
            float luz = (0.5f * dx - 0.5f * dz + 0.70710678f) / (Mathf.Sqrt(dx * dx + dz * dz + 1f) * 0.70710678f);
            return Mathf.Clamp(1f + (luz - 1f) * ForcaSombra, 0.5f, 1.18f);   // teto baixo: o verde nao estoura em neon
        }

        /// <summary>Lago e brejo sao os discos da Ilha, mas so' onde a lamina cobre o chao; mar = abaixo do nivel. Do raso ao
        /// fundo em poucos metros e um fio de ESPUMA na margem: a costa sai nitida, nao um halo borrado.</summary>
        static bool Agua(Relevo r, float x, float z, float h, out Color c)
        {
            float d;
            if (Dist(x, z, r.Lago) <= r.LagoDiscoR && h < Relevo.LagoY) { d = Relevo.LagoY - h; c = Color.Lerp(LagoRaso, LagoFundo, Relevo.Suave(0.1f, 2.2f, d)); }
            else if (Dist(x, z, r.Alagado) <= r.AlagadoDiscoR && h < Relevo.AlagadoY) { d = Relevo.AlagadoY - h; c = Color.Lerp(BrejoRaso, BrejoFundo, Relevo.Suave(0f, 0.5f, d)); }
            else if (h < Relevo.AguaY) { d = Relevo.AguaY - h; c = Color.Lerp(MarRaso, MarFundo, Relevo.Suave(0.05f, 1.5f, d)); }
            else { c = default(Color); return false; }
            c = Color.Lerp(c, Espuma, (1f - Relevo.Suave(0f, 0.12f, d)) * 0.7f);
            return true;
        }

        /// <summary>O bioma em rampa, com os mesmos raios do Relevo.Cor. Sem a mancha de regiao do terreno (no mapa ela le'
        /// sujeira); no lugar, grao miudo: copa em tufos na floresta, capim que nao e' plastico liso.</summary>
        static Color Chao(Relevo r, float x, float z, float h, float declive)
        {
            float wFlor = 1f - Relevo.Suave(r.FlorestaR * 0.3f, r.FlorestaR + 9f, Dist(x, z, r.Floresta));
            float wRuina = 1f - Relevo.Suave(r.RuinasR * 0.85f, r.RuinasR + 6f, Dist(x, z, r.Ruinas));
            float wLama = Mathf.Min((1f - Relevo.Suave(r.AlagadoR * 0.2f, r.AlagadoR + 13f, Dist(x, z, r.Alagado))) * Relevo.Suave(5.6f, 0.2f, h) * 1.5f, 1f);
            float wDuna = 1f - Relevo.Suave(r.DunasR * 0.5f, r.DunasR + 14f, Dist(x, z, r.Dunas));
            float wRocha = Mathf.Max(Relevo.Suave(0.30f, 0.62f, declive), Relevo.Suave(10.5f, 16f, h));
            Color c = Color.Lerp(Relevo.CorGramaClara, Relevo.CorGrama, Mathf.Clamp01((h - 1f) / 10.5f));
            c *= 0.93f + 0.05f * Grao(x / 9f, z / 9f) + 0.05f * Grao(x / 2.5f, z / 2.5f);
            Color copa = Color.Lerp(CopaFunda, Copa, Relevo.Suave(0.3f, 0.75f, Grao(x / 3.5f, z / 3.5f)));   // tufos de ~4 m: copas
            c = Color.Lerp(c, copa, wFlor * 0.95f);
            c = Color.Lerp(c, Relevo.CorRuina, wRuina * 0.9f);
            c = Color.Lerp(c, Lama, wLama);
            c = Color.Lerp(c, Relevo.CorDuna, wDuna * 0.92f);
            c = Color.Lerp(c, Relevo.CorRocha, wRocha * 0.85f);
            c = Color.Lerp(c, Relevo.CorPico, Relevo.Suave(Relevo.PicoH + 4f, Relevo.PicoH + 8f, h) * 0.8f);
            float seco = (1f - wLama) * (1f - wLama);
            return Color.Lerp(c, Relevo.CorAreia, Relevo.Suave(2.2f, 0.5f, h) * seco);   // praia: a faixa clara que desenha a costa
        }

        static float Dist(float x, float z, Vector2 p)
        {
            float dx = x - p.x, dz = z - p.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Ruido de valor barato 0..1 (hash inteiro + bilinear suave), deterministico: so' textura de mapa.</summary>
        static float Grao(float x, float z)
        {
            int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(z);
            float fx = x - ix, fz = z - iz;
            fx = fx * fx * (3f - 2f * fx); fz = fz * fz * (3f - 2f * fz);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iz), Hash(ix + 1, iz), fx), Mathf.Lerp(Hash(ix, iz + 1), Hash(ix + 1, iz + 1), fx), fz);
        }

        static float Hash(int x, int z)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)z * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) * (1f / 4294967296f);
            }
        }
    }

    /// <summary>
    /// ANEL de espessura FIXA em px: o circulo da tempestade vai de 264 m a zero, e um sprite de anel escalado engrossaria
    /// e sumiria junto. Raio = metade do rect; faixa cheia de raio - Dentro ate' raio + Fora, com 1 px de borda macia dos
    /// dois lados. MOVER o no' nao refaz a malha; so' raio ou faixa novos refazem.
    /// Tinta da tempestade = Dentro 0 e Fora enorme: pinta tudo FORA do circulo (a mascara da janela corta o resto).
    /// </summary>
    public sealed class AnelUi : MaskableGraphic
    {
        const int Segmentos = 96;
        const float Pena = 1f;
        static readonly float[] _raios = new float[4];   // rascunho do OnPopulateMesh (thread principal)
        static readonly float[] _alfas = { 0f, 1f, 1f, 0f };
        float _dentro = 1f, _fora = 1f;

        public void Faixa(float dentro, float fora)
        {
            if (dentro == _dentro && fora == _fora) return;
            _dentro = dentro; _fora = fora;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rr = rectTransform.rect;
            float r = rr.width * 0.5f;
            _raios[0] = Mathf.Max(r - _dentro - Pena, 0f); _raios[1] = Mathf.Max(r - _dentro, 0f);
            _raios[2] = r + _fora; _raios[3] = r + _fora + Pena;
            Color32 cor = color;
            for (int s = 0; s < Segmentos; s++)
            {
                float a = s * (Mathf.PI * 2f / Segmentos);
                float cx = Mathf.Cos(a), sy = Mathf.Sin(a);
                for (int b = 0; b < 4; b++)
                {
                    Color32 c = cor;
                    c.a = (byte)(cor.a * _alfas[b]);
                    vh.AddVert(new Vector3(rr.center.x + cx * _raios[b], rr.center.y + sy * _raios[b], 0f), c, Vector4.zero);
                }
            }
            for (int s = 0; s < Segmentos; s++)
            {
                int a0 = s * 4, a1 = ((s + 1) % Segmentos) * 4;
                for (int b = 0; b < 3; b++)
                {
                    vh.AddTriangle(a0 + b, a0 + b + 1, a1 + b + 1);
                    vh.AddTriangle(a0 + b, a1 + b + 1, a1 + b);
                }
            }
        }
    }

    /// <summary>
    /// MINIMAPA (canto superior direito, abaixo do relogio), MAPA GRANDE (toque no minimapa; toque de novo fecha; o jogo
    /// NAO pausa) e BUSSOLA (topo central). So' OBSERVA: zona e bau vem da Partida.Atual, a ilha da Ilha.Atual, o jogador e
    /// a fase da queda da Hud, o yaw da Camera.main e a rota do castelo do Bus.
    /// NORTE FIXO e a seta gira: o pequeno e o grande leem igual (a zona "a nordeste" e' nordeste nos dois), o giro de
    /// cabeca fica com a bussola, e foto de mapa girado le' torto. No castelo e na queda o minimapa mostra a ilha INTEIRA
    /// (escolher onde pousar); no chao, VistaLocalM em volta do jogador. No treino: so' a ilha e o jogador (nao ha' zona).
    /// O PARCEIRO (dupla) e' um ponto AZUL-ALIADO com contorno escuro, preso na borda da janela como o jogador.
    /// </summary>
    public sealed class Minimapa
    {
        const float RaioDp = 10f, RaioGrandeDp = 14f;

        readonly RectTransform _mini, _grande;
        readonly VistaDoMapa _vistaMini, _vistaGrande;
        readonly Bussola _bussola;
        bool _temRota;
        Vector3 _rotaA, _rotaB;
        float _vista = -1f;   // metros que o minimapa mostra agora (anda para o alvo); < 0 = crava no proximo quadro
        QuadroDoMapa _q;      // o ultimo quadro lido: o Layout redesenha com ele (a foto muda a tela e renderiza na hora)
        bool _temQuadro;

        /// <summary>O mapa grande esta' aberto.</summary>
        public bool Aberto => _grande.gameObject.activeSelf;

        public Minimapa(RectTransform raiz)
        {
            _bussola = new Bussola(raiz);
            // minimapa: a PLACA da HUD (fio de ouro + miolo) e a janela mascarada dentro; o no' inteiro pega o dedo
            var placa = Hud.Placa(raiz, "Minimapa", Dp.Px(RaioDp));
            placa.raycastTarget = true;
            var b = placa.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Alternar);
            _mini = placa.rectTransform;
            _vistaMini = new VistaDoMapa(_mini, Dp.Px(RaioDp), 1f, false);
            // mapa grande: veu que escurece o jogo (NAO pega toque: joystick e olhar seguem vivos) e a placa quadrada no meio
            var g = Formas.No(raiz, "MapaGrande");
            AreaSegura.Esticar(g);
            var veu = Formas.Imagem(g, "Veu", null, new Color(0.01f, 0.015f, 0.03f, 0.5f));
            AreaSegura.Esticar(veu.rectTransform);
            var pg = Hud.Placa(g, "PlacaDoMapa", Dp.Px(RaioGrandeDp));
            pg.raycastTarget = true;
            var bg = pg.gameObject.AddComponent<Button>();
            bg.transition = Selectable.Transition.None;
            bg.onClick.AddListener(Fechar);
            _vistaGrande = new VistaDoMapa(pg.rectTransform, Dp.Px(RaioGrandeDp), 1.4f, true);
            _grande = g;
            _grande.gameObject.SetActive(false);
        }

        /// <summary>Retangulos do HudLayout (px de tela); o veu do mapa grande e' a tela toda. As marcas moram em px da
        /// janela: tela nova REDESENHA na hora (a foto troca a tela e renderiza sem passar por outro Update).</summary>
        public void Layout(Rect mini, Rect bussola, Rect grande)
        {
            AreaSegura.NoRect(_mini, mini);
            AreaSegura.NoRect(_vistaGrande.Placa, grande);
            _bussola.Layout(bussola);
            _vistaMini.Medir();
            _vistaGrande.Medir();
            if (_temQuadro) Desenhar();
        }

        /// <summary>A rota do castelo (Bus.CasteloRota): a linha por onde da' pra saltar, ate' o jogador sair do castelo.</summary>
        public void Rota(Vector3 inicio, Vector3 fim) { _temRota = true; _rotaA = inicio; _rotaB = fim; }

        /// <summary>Partida nova: sem rota, mapa grande fechado, zoom cravado no proximo quadro.</summary>
        public void Zerar() { _temRota = false; _vista = -1f; Fechar(); }

        public void Alternar()
        {
            _grande.gameObject.SetActive(!Aberto);
            if (Aberto && _temQuadro) Desenhar();   // abre JA' desenhado: o primeiro quadro nao pisca marca velha
        }

        public void Fechar() { if (Aberto) _grande.gameObject.SetActive(false); }

        /// <summary>Por quadro (dt sem escala: o zoom anda na pausa tambem). `fase` = a fase da queda do jogador.</summary>
        public void Pintar(float dt, IEntidade jogador, string fase)
        {
            Relevo relevo = Ilha.Atual != null ? Ilha.Atual.Relevo : null;
            float ext = MapaLogica.Extensao(relevo);
            var q = new QuadroDoMapa { Textura = Textura(relevo), Extensao = ext };
            q.TemJogador = jogador != null;
            q.Jogador = jogador != null ? jogador.Pos : Vector3.zero;
            Camera cam = Camera.main;
            q.Yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            Partida p = Partida.Atual;
            Zona z = p != null ? p.Zona : null;
            q.ZonaAtiva = z != null && z.Ativa;
            if (q.ZonaAtiva) { q.ZonaCentro = z.Centro; q.ZonaRaio = z.Raio; }
            Zona.Circulo c;
            q.TemProximo = ZonaVisual.Proximo(z, out c);
            q.ProximoCentro = c.Centro; q.ProximoRaio = c.Raio;
            BauCelestial bau = p != null ? p.Bau : null;
            q.TemBau = bau != null && (bau.FaseAtual == BauCelestial.Fase.Caindo || bau.FaseAtual == BauCelestial.Fase.Pousado);
            if (q.TemBau) q.Bau = bau.Pos;
            IEntidade parceiro = p != null ? p.ParceiroVivo() : null;
            q.TemParceiro = parceiro != null && parceiro != jogador;   // espectando, o "jogador" da HUD ja' e' ele
            if (q.TemParceiro) q.Parceiro = parceiro.Pos;
            q.TemRota = _temRota && fase == Queda.NO_CASTELO;
            q.RotaA = _rotaA; q.RotaB = _rotaB;
            // zoom: castelo e queda = a ilha inteira (escolher o pouso); no chao, a janela local. Anda em ~1 s
            float alvo = fase == Queda.POUSOU ? MapaLogica.VistaLocalM : ext;
            _vista = _vista < 0f ? alvo : Mathf.Lerp(_vista, alvo, 1f - Mathf.Exp(-dt * 3f));
            _q = q;
            _temQuadro = true;
            Desenhar();
        }

        void Desenhar()
        {
            _vistaMini.Pintar(ref _q, MapaLogica.CentroDaJanela(_q.Jogador, _vista, _q.Extensao), _vista);
            if (Aberto) _vistaGrande.Pintar(ref _q, Vector3.zero, _q.Extensao);
            _bussola.Pintar(_q.Yaw, _q.TemProximo && _q.TemJogador, MapaLogica.Rumo(_q.Jogador, _q.ProximoCentro),
                _q.TemBau && _q.TemJogador, MapaLogica.Rumo(_q.Jogador, _q.Bau));
        }

        // ---------- a textura da ilha: pintada UMA vez por processo, fora da thread principal ----------

        static Texture2D _textura;
        static Relevo _texturaDe, _pintandoDe;
        static Task<Color32[]> _pintando;

        /// <summary>A ilha ja' subiu para a GPU (a foto cobra: a Task pode falhar calada no aparelho).</summary>
        public static bool IlhaPintada => _textura != null;

        /// <summary>
        /// A ilha nao muda entre partidas (nasce no boot): a textura e' uma so' no processo. A PINTURA (262 mil alturas)
        /// roda numa Task e a thread principal so' sobe o resultado (SetPixels32 + Apply, ~1 MB, uma vez): nada de quadro
        /// travado no carregamento. Null enquanto pinta (a janela mostra o mar fundo) ou se falhar (o jogo segue).
        /// </summary>
        static Texture2D Textura(Relevo r)
        {
            if (r == null) return null;
            if (_textura != null && _texturaDe == r) return _textura;
            if (_pintandoDe != r)
            {
                _pintandoDe = r;
                _pintando = Task.Run(() => MapaLogica.PintarIlha(r, MapaLogica.TexturaLado));
            }
            if (_pintando == null || !_pintando.IsCompleted || _pintando.IsFaulted) return null;
            const int n = MapaLogica.TexturaLado;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "MapaDaIlha",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            t.SetPixels32(_pintando.Result);
            t.Apply(false, true);   // solta a copia da CPU: memoria de celular
            if (_textura != null) Object.Destroy(_textura);
            _textura = t; _texturaDe = r; _pintando = null;
            return t;
        }
    }

    /// <summary>O que o mapa desenha neste quadro (lido uma vez, servido as duas vistas): struct, zero lixo.</summary>
    struct QuadroDoMapa
    {
        public Texture2D Textura;
        public float Extensao, Yaw;
        public bool TemJogador, ZonaAtiva, TemProximo, TemBau, TemRota, TemParceiro;
        public Vector3 Jogador, ZonaCentro, ProximoCentro, Bau, RotaA, RotaB, Parceiro;
        public float ZonaRaio, ProximoRaio;
    }

    /// <summary>
    /// UMA vista do mapa dentro de uma placa: a janela MASCARADA (cantos redondos) com a textura da ilha, a tinta da
    /// tempestade fora da zona, a borda da zona ATUAL, o PROXIMO circulo em branco, a rota tracejada do castelo, os nomes
    /// dos POIs (so' no grande), o bau e o jogador (cone de visao + seta com contorno). O minimapa e o mapa grande sao
    /// duas destas; `icone` escala marcas e tracos (no grande eles crescem junto com a janela).
    /// </summary>
    sealed class VistaDoMapa
    {
        public readonly RectTransform Placa;
        readonly RectTransform _janela;
        readonly RawImage _ilha;
        readonly AnelUi _tinta, _atual, _proximo;
        readonly Image _rota, _bau, _bauFundo, _cone, _seta, _setaFundo, _parceiro, _parceiroFundo;
        readonly RectTransform[] _pois;
        readonly Vector3[] _poisM;
        readonly float _icone;
        float _lado = -1f;
        const float ConeGraus = 70f;

        public VistaDoMapa(RectTransform placa, float raioPx, float icone, bool comPois)
        {
            Placa = placa;
            _icone = icone;
            float recuo = Mathf.Max(Dp.Px(1f), 1f) + Dp.Px(1.5f * icone);   // o fio + um vao escuro: a moldura le' como bisel
            var janela = Formas.Arredondada(placa, "Janela", MapaLogica.Fundo, Mathf.Max(raioPx - recuo, 1f));
            AreaSegura.Esticar(janela.rectTransform);
            janela.rectTransform.offsetMin = new Vector2(recuo, recuo); janela.rectTransform.offsetMax = new Vector2(-recuo, -recuo);
            janela.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _janela = janela.rectTransform;
            var ilha = new GameObject("Ilha", typeof(RectTransform), typeof(RawImage));
            ilha.transform.SetParent(_janela, false);
            _ilha = ilha.GetComponent<RawImage>();
            _ilha.raycastTarget = false;
            _ilha.enabled = false;   // textura null pinta BRANCO: so' liga quando a pintura chegar
            AreaSegura.Esticar(_ilha.rectTransform);
            _tinta = Anel("Tempestade", CorTinta());
            _atual = Anel("ZonaAtual", CorDaBorda());
            _proximo = Anel("Proximo", new Color(1f, 1f, 1f, 0.95f));
            _rota = Formas.Imagem(_janela, "Rota", Tracejado(), Formas.ComAlfa(Estilo.Ouro, 0.9f));
            _rota.type = Image.Type.Tiled;
            if (comPois)
            {
                Relevo r = Ilha.Atual != null ? Ilha.Atual.Relevo : null;
                Poi[] ps = r != null ? r.Pois : new Poi[0];
                _pois = new RectTransform[ps.Length];
                _poisM = new Vector3[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    string nome;
                    if (!Textos.MapaPois.TryGetValue(ps[i].Nome, out nome)) nome = "";
                    var t = Formas.Texto(_janela, "Poi", nome, 11f, new Color(1f, 1f, 1f, 0.9f));
                    t.fontStyle = FontStyle.Bold;
                    t.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(1f), -Dp.Px(1f));
                    _pois[i] = t.rectTransform;
                    _pois[i].anchorMin = _pois[i].anchorMax = new Vector2(0.5f, 0.5f);
                    _poisM[i] = new Vector3(ps[i].Centro.x, 0f, ps[i].Centro.y);
                }
                var dica = Formas.Texto(_janela, "Dica", Textos.MapaFechar, 10f, Estilo.TextoFosco);
                dica.fontStyle = FontStyle.Bold;
                dica.rectTransform.anchorMin = dica.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                dica.rectTransform.anchoredPosition = new Vector2(0f, Dp.Px(12f));
            }
            _bauFundo = Marca("BauFundo", Formas.Losango(), Formas.ComAlfa(Estilo.NoiteFunda, 0.9f), 15f);
            _bau = Marca("Bau", Formas.Losango(), Estilo.Ouro, 10.5f);
            _parceiroFundo = Marca("ParceiroFundo", Formas.Disco(), Formas.ComAlfa(Estilo.NoiteFunda, 0.9f), 12f);   // por baixo da seta:
            _parceiro = Marca("Parceiro", Formas.Disco(), Dupla.CorAliado, 8f);                                       // colados, o jogador le' por cima
            _cone = Marca("Cone", Formas.Sombra(), new Color(1f, 1f, 1f, 0.32f), 58f);
            _cone.type = Image.Type.Filled; _cone.fillMethod = Image.FillMethod.Radial360;
            _cone.fillOrigin = (int)Image.Origin360.Top; _cone.fillClockwise = true; _cone.fillAmount = ConeGraus / 360f;
            _setaFundo = Marca("SetaFundo", Ponteiro(), Formas.ComAlfa(Estilo.NoiteFunda, 0.9f), 19f);
            _seta = Marca("Seta", Ponteiro(), Color.white, 13f);
            if (!comPois)
            {
                // o N no alto da janela: o norte fixo se le' de primeira
                var n = Formas.Texto(_janela, "Norte", Textos.MapaRumos[0], 9f, Estilo.Ouro);
                n.fontStyle = FontStyle.Bold;
                n.rectTransform.anchorMin = n.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                n.rectTransform.anchoredPosition = new Vector2(0f, -Dp.Px(7f));
            }
        }

        static Color CorTinta() => Formas.ComAlfa(ZonaVisual.COR, 0.36f);
        static Color CorDaBorda() => Color.Lerp(ZonaVisual.COR, Color.white, 0.25f);

        AnelUi Anel(string nome, Color cor)
        {
            var go = new GameObject(nome, typeof(RectTransform), typeof(AnelUi));
            go.transform.SetParent(_janela, false);
            var a = go.GetComponent<AnelUi>();
            a.color = cor;
            a.raycastTarget = false;
            a.rectTransform.anchorMin = a.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            a.enabled = false;
            return a;
        }

        Image Marca(string nome, Sprite s, Color cor, float ladoDp)
        {
            var img = Formas.Imagem(_janela, nome, s, cor);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = Vector2.one * Dp.Px(ladoDp * _icone);
            img.enabled = false;
            return img;
        }

        /// <summary>Espessuras em px: a tinta vai 3 janelas para fora (cobre a janela com o centro da zona em qualquer lugar).</summary>
        public void Medir()
        {
            _lado = -1f;
            float borda = Dp.Px(2f * _icone), prox = Dp.Px(1.6f * _icone);
            _atual.Faixa(borda * 0.5f, borda * 0.5f);
            _proximo.Faixa(prox * 0.5f, prox * 0.5f);
        }

        public void Pintar(ref QuadroDoMapa q, Vector3 centro, float vistaM)
        {
            float lado = _janela.rect.width;
            if (lado <= 1f) return;
            if (lado != _lado)
            {
                _lado = lado;
                _tinta.Faixa(0f, lado * 3f);
                _rota.pixelsPerUnitMultiplier = 32f / Mathf.Max(Dp.Px(7f * _icone), 1f);   // traco de ~7dp (sprite de 32 texels)
            }
            float escala = lado / vistaM;
            if (q.Textura != null && _ilha.texture != q.Textura) { _ilha.texture = q.Textura; _ilha.enabled = true; }
            if (_ilha.enabled) _ilha.uvRect = MapaLogica.UvDaJanela(centro, vistaM, q.Extensao);
            Circulo(_tinta, q.ZonaAtiva, q.ZonaCentro, q.ZonaRaio, centro, escala);
            Circulo(_atual, q.ZonaAtiva, q.ZonaCentro, q.ZonaRaio, centro, escala);
            Circulo(_proximo, q.TemProximo, q.ProximoCentro, q.ProximoRaio, centro, escala);
            // rota: segmento do inicio ao fim (a mascara corta o que sai da janela)
            _rota.enabled = q.TemRota;
            if (q.TemRota)
            {
                Vector2 a = MapaLogica.NaJanela(q.RotaA, centro, escala), b = MapaLogica.NaJanela(q.RotaB, centro, escala);
                Vector2 d = b - a;
                RectTransform rt = _rota.rectTransform;
                rt.anchoredPosition = (a + b) * 0.5f;
                rt.sizeDelta = new Vector2(d.magnitude, Dp.Px(2f * _icone));
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            if (_pois != null)
                for (int i = 0; i < _pois.Length; i++) _pois[i].anchoredPosition = MapaLogica.NaJanela(_poisM[i], centro, escala);
            // bau (caindo ou no chao): losango de ouro que respira
            _bau.enabled = _bauFundo.enabled = q.TemBau;
            if (q.TemBau)
            {
                Vector2 pb = MapaLogica.NaJanela(q.Bau, centro, escala);
                _bau.rectTransform.anchoredPosition = pb; _bauFundo.rectTransform.anchoredPosition = pb;
                float s = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f);
                _bau.rectTransform.localScale = new Vector3(s, s, 1f);
            }
            float m = lado * 0.5f - Dp.Px(6f * _icone);
            // parceiro: o ponto azul, preso na borda como o jogador (longe nao some: e' para onde voltar)
            _parceiro.enabled = _parceiroFundo.enabled = q.TemParceiro;
            if (q.TemParceiro)
            {
                Vector2 pp = MapaLogica.NaJanela(q.Parceiro, centro, escala);
                pp = new Vector2(Mathf.Clamp(pp.x, -m, m), Mathf.Clamp(pp.y, -m, m));
                _parceiro.rectTransform.anchoredPosition = pp; _parceiroFundo.rectTransform.anchoredPosition = pp;
            }
            // jogador: cone da camera + seta; fora da janela (castelo no mar) ele encosta na borda, nunca some
            _seta.enabled = _setaFundo.enabled = _cone.enabled = q.TemJogador;
            if (!q.TemJogador) return;
            Vector2 pj = MapaLogica.NaJanela(q.Jogador, centro, escala);
            pj = new Vector2(Mathf.Clamp(pj.x, -m, m), Mathf.Clamp(pj.y, -m, m));
            float giro = MapaLogica.GiroDaSeta(q.Yaw);
            _seta.rectTransform.anchoredPosition = pj; _setaFundo.rectTransform.anchoredPosition = pj; _cone.rectTransform.anchoredPosition = pj;
            _seta.rectTransform.localRotation = _setaFundo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, giro);
            _cone.rectTransform.localRotation = Quaternion.Euler(0f, 0f, giro + ConeGraus * 0.5f);   // o leque centrado no rumo
        }

        /// <summary>O no' no centro do circulo; o tamanho (2 x raio) so' muda quando o raio ou o zoom mudam (refaz a malha).</summary>
        static void Circulo(AnelUi a, bool on, Vector3 c, float raioM, Vector3 centro, float escala)
        {
            if (a.enabled != on) a.enabled = on;
            if (!on) return;
            RectTransform rt = a.rectTransform;
            rt.anchoredPosition = MapaLogica.NaJanela(c, centro, escala);
            float d = Mathf.Max(raioM, 0f) * 2f * escala;
            if (Mathf.Abs(rt.sizeDelta.x - d) > 0.25f) rt.sizeDelta = new Vector2(d, d);
        }

        static Sprite _ponteiro, _tracejado;

        /// <summary>A seta do jogador: chevron com entalhe atras (a seta de mapa de todo BR), 64 texels, 4 amostras por texel.</summary>
        static Sprite Ponteiro()
        {
            if (_ponteiro != null) return _ponteiro;
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Ponteiro", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int k = 0;
                    for (int s = 0; s < 4; s++)
                    {
                        float fx = (x + 0.25f + (s & 1) * 0.5f) / n * 2f - 1f, fy = (y + 0.25f + (s >> 1) * 0.5f) / n * 2f - 1f;
                        // triangulo de ponta (0; 0,9) e base em y = -0,8, menos o entalhe que sobe ate' (0; -0,35)
                        if (fy > -0.35f - Mathf.Abs(fx) * 0.625f && Mathf.Abs(fx) < (0.9f - fy) * 0.4235f) k++;
                    }
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(k * 63));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return _ponteiro = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Um traco da rota: 32x8 texels, 20 cheios e 12 vazios (Image.Tiled repete ao longo da linha).</summary>
        static Sprite Tracejado()
        {
            if (_tracejado != null) return _tracejado;
            var t = new Texture2D(32, 8, TextureFormat.RGBA32, false) { name = "Tracejado", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[32 * 8];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(i % 32 < 20 ? 255 : 0));
            t.SetPixels32(px);
            t.Apply(false, true);
            return _tracejado = Sprite.Create(t, new Rect(0, 0, 32, 8), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    /// <summary>
    /// BUSSOLA do topo central: placa fina com N, NE, L, SE, S, SO, O, NO e marcas a cada 15 graus correndo com o YAW da
    /// camera (180 graus visiveis; perto da borda esvaecem), o triangulo de ouro no meio = para onde a camera olha, e as
    /// marcas do PROXIMO circulo (anel violeta) e do bau (losango de ouro). Cor + forma (GDD §10). Texto fixo: nada de
    /// string por quadro; parado (yaw igual) nao mexe em nada.
    /// </summary>
    sealed class Bussola
    {
        const int Passo = 15;
        readonly RectTransform _raiz;
        readonly Graphic[] _itens;     // marcas e letras, na ordem de _graus
        readonly float[] _graus;
        readonly Image _zona, _bau;
        float _meia = 1f, _yaw = float.NaN;

        public Bussola(Transform pai)
        {
            _raiz = Hud.Placa(pai, "Bussola", Dp.Px(7f)).rectTransform;
            var baixo = new Vector2(0.5f, 0f);
            var meio = new Vector2(0.5f, 0.5f);
            int n = 360 / Passo;
            _itens = new Graphic[n];
            _graus = new float[n];
            for (int i = 0; i < n; i++)
            {
                int g = i * Passo;
                _graus[i] = g;
                if (g % 45 == 0)
                {
                    bool cardeal = g % 90 == 0;
                    var t = Formas.Texto(_raiz, "Rumo" + g, Textos.MapaRumos[g / 45], cardeal ? 13f : 10f, g == 0 ? Estilo.Ouro : Color.white);
                    t.fontStyle = FontStyle.Bold;
                    t.rectTransform.anchorMin = t.rectTransform.anchorMax = meio;
                    t.rectTransform.sizeDelta = new Vector2(Dp.Px(30f), Dp.Px(20f));
                    t.rectTransform.anchoredPosition = new Vector2(0f, Dp.Px(1.5f));   // um tico acima: as marcas moram embaixo
                    _itens[i] = t;
                }
                else
                {
                    var m = Formas.Imagem(_raiz, "Marca" + g, null, new Color(1f, 1f, 1f, 0.6f));
                    m.rectTransform.anchorMin = m.rectTransform.anchorMax = baixo;
                    m.rectTransform.pivot = baixo;
                    m.rectTransform.sizeDelta = new Vector2(Mathf.Max(Dp.Px(1f), 1f), Dp.Px(5f));
                    m.rectTransform.anchoredPosition = new Vector2(0f, Dp.Px(2f));
                    _itens[i] = m;
                }
            }
            _zona = Formas.Imagem(_raiz, "Zona", Formas.Anel(0.5f), CorZona());
            _bau = Formas.Imagem(_raiz, "Bau", Formas.Losango(), Estilo.Ouro);
            foreach (var img in new[] { _zona, _bau })
            {
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = baixo;
                img.rectTransform.sizeDelta = Vector2.one * Dp.Px(9f);
                img.enabled = false;
            }
            // o MEIO: triangulo de ouro pendurado na borda de cima, apontando para dentro
            var tri = Formas.Imagem(_raiz, "Meio", Formas.Triangulo(), Estilo.Ouro);
            tri.rectTransform.anchorMin = tri.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            tri.rectTransform.sizeDelta = Vector2.one * Dp.Px(8f);
            tri.rectTransform.anchoredPosition = new Vector2(0f, -Dp.Px(2f));
            tri.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        static Color CorZona() => new Color(0.55f, 0.35f, 1f);

        public void Layout(Rect r)
        {
            AreaSegura.NoRect(_raiz, r);
            _meia = Mathf.Max(r.width * 0.5f - Dp.Px(10f), 1f);
            _yaw = float.NaN;   // tela nova: recoloca tudo
        }

        public void Pintar(float yaw, bool temZona, float rumoZona, bool temBau, float rumoBau)
        {
            if (yaw != _yaw)
            {
                _yaw = yaw;
                for (int i = 0; i < _itens.Length; i++)
                {
                    float f = MapaLogica.NaBussola(_graus[i], yaw);
                    float a = MapaLogica.AlfaNaBussola(f);
                    _itens[i].canvasRenderer.SetAlpha(a);
                    if (a <= 0f) continue;
                    RectTransform rt = _itens[i].rectTransform;
                    rt.anchoredPosition = new Vector2(f * _meia, rt.anchoredPosition.y);
                }
            }
            Marcador(_zona, temZona, rumoZona, yaw);
            Marcador(_bau, temBau, rumoBau, yaw);
        }

        /// <summary>Marca de alvo na borda de baixo; fora da faixa ela ENCOSTA na ponta (o alvo esta' atras: vire).</summary>
        void Marcador(Image img, bool on, float rumo, float yaw)
        {
            if (img.enabled != on) img.enabled = on;
            if (!on) return;
            float f = Mathf.Clamp(MapaLogica.NaBussola(rumo, yaw), -1f, 1f);
            img.rectTransform.anchoredPosition = new Vector2(f * _meia, 0f);
        }
    }
}
