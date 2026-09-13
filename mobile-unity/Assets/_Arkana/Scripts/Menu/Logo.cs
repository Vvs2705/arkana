using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;

namespace Arkana.Menu
{
    /// <summary>
    /// A MARCA "ARKANA" desenhada em codigo — sem fonte (nao ha' fonte de fantasia com licenca no projeto; a do Unity e' a
    /// Arial que a foto chamou de amadora). As letras sao POLIGONOS proprios: serifa em cunha, traco grosso-fino, o A em
    /// ponta de lanca com a barra em losango e a perna do K em relampago na cor do Raio (GDD §10). Assadas UMA vez por
    /// processo numa textura, por campo de distancia (o mesmo espirito do Selo): degrade de ouro, bisel, contorno escuro
    /// grosso, a espessura em bronze descendo, sombra macia e o halo do raio. O Graphic desenha a arte num quad e, por cima,
    /// o REFLEXO que corre (faixas com alfa por vertice que amostram a mascara da face) e a cintilancia nas pontas dos As.
    /// Titulo, menu e tela de carregamento: a MESMA marca (Menu.Wordmark).
    /// </summary>
    public sealed class Logo : MaskableGraphic
    {
        // ---------- KNOBs (unidade = 1/100 da altura de caixa das letras) ----------

        /// <summary>KNOB por foto: contorno escuro, espessura (o bronze que desce sob cada letra), bisel, sombra (lado, desce,
        /// borra, alfa) e o halo do raio (alcance e alfa).</summary>
        public const float Contorno = 6.5f, Espessura = 6f, Bisel = 3.6f, SombraLado = 2f, SombraDesce = 8f, SombraBorra = 5f,
            SombraAlfa = 0.55f, HaloRaio = 24f, HaloAlfa = 0.9f;
        /// <summary>KNOB: px de largura da arte assada (o titulo tem ~740 px no Poco F4; os menores descem por mipmap).</summary>
        public const int LarguraTextura = 1024;
        /// <summary>KNOB: o reflexo — varredura (s), ciclo inteiro (s), meia-largura e inclinacao (fracao da altura), alfa;
        /// e a cintilancia nas pontas (tamanho em unidades).</summary>
        public const float VarreS = 1.6f, CicloS = 5f, FaixaMeia = 0.2f, FaixaInclina = 0.35f, FaixaAlfa = 0.7f, FaiscaTam = 80f;

        /// <summary>Altura de caixa: todo glifo e' desenhado com a base em y=0 e o topo das hastes em y=100.</summary>
        public const float Altura = 100f;
        /// <summary>Folga da textura em volta das letras: cabe o contorno, a espessura, a sombra e o halo.</summary>
        const float Margem = 30f;
        const int CurvaPassos = 6;

        /// <summary>KNOB: o degrade da face (claro no topo -> ouro -> ambar -> bronze na base), o bisel (luz de cima a'
        /// esquerda), o contorno, o bronze da espessura e o raio (borda -> nucleo quente) com o halo azul-eletrico. Hex direto
        /// (sem ColorUtility: o assado e' conta pura); Face1 = Estilo.Ouro e RaioBorda = Estilo.Raio (GDD §10).</summary>
        static readonly Color Face0 = H(0xFFF6D2), Face1 = H(0xF0C75E), Face2 = H(0xD48E2C), Face3 = H(0x8C4F16);
        static readonly Color BiselLuz = H(0xFFFBEA), BiselSombra = H(0x5A2E08);
        static readonly Color CorContorno = H(0x1A0C05), Espessura0 = H(0x6E3C11), Espessura1 = H(0x341808);
        static readonly Color RaioBorda = H(0xF5D90A), RaioNucleo = H(0xFFF9CF), CorHalo = H(0x7FDFFF);
        static readonly Vector2 Luz = new Vector2(-0.55f, 0.84f).normalized;

        /// <summary>O reflexo corre (titulo e menu). Falso = estatico (a tela de carregamento).</summary>
        public bool Brilho = true;
        /// <summary>Foto: >= 0 congela o reflexo nesse ponto da varredura (0..1). Negativo = o relogio.</summary>
        public float Fase = -1f;
        bool _reflexoNaTela;

        // ---------- os glifos ----------

        sealed class Glifo
        {
            public readonly char C; public readonly float Avanco; public readonly string[] Pecas;
            public Glifo(char c, float avanco, params string[] pecas) { C = c; Avanco = avanco; Pecas = pecas; }
        }

        // Caminho de cada peca: "M x y" abre um contorno, "L x y" reta, "Q cx cy x y" curva; contorno fecha sozinho, varios
        // contornos na mesma peca furam por par-impar. "!" no comeco = peca do RAIO (a perna do K). As pecas se somam
        // (uniao): serifa, barra e losango sobrepoem a haste, sem costura. Avanco = onde a proxima letra comeca.
        static readonly Glifo[] Glifos =
        {
            new Glifo('A', 101f,
                "M 9 0 L 19.5 0 L 45.8 72 L 72 0 L 95 0 L 52 118",
                "M 25.4 26 L 70.6 26 L 70.6 34 L 25.4 34",
                "M 35.8 30 L 45.8 18 L 55.8 30 L 45.8 42",
                "M 0 0 L 23.5 0 L 23.5 11 L 13 11 Q 13 1.8 0 1.8",
                "M 68 0 L 104 0 L 104 1.8 Q 91 1.8 91 11 L 68 11",
                "M 46.5 90 L 39.5 97.5 L 47.5 99.5",
                "M 57.5 90 L 56.5 99.5 L 64.5 97.5"),
            new Glifo('R', 96f,
                "M 9 0 L 32 0 L 32 100 L 9 100",
                "M 0 0 L 41 0 L 41 1.8 Q 32 1.8 32 11 L 9 11 Q 9 1.8 0 1.8",
                "M 0 100 L 32 100 L 32 89 L 9 89 Q 9 98.2 0 98.2",
                "M 29 100 L 54 100 Q 83 100 83 76 Q 83 51 54 51 L 29 51 L 29 59 L 51 59 Q 64 59 64 76 Q 64 92 51 92 L 29 92",
                "M 42 57 L 61 57 Q 70 28 99 0 L 70 0 Q 62 30 42 57"),
            new Glifo('K', 99f,
                "M 9 0 L 32 0 L 32 100 L 9 100",
                "M 0 0 L 41 0 L 41 1.8 Q 32 1.8 32 11 L 9 11 Q 9 1.8 0 1.8",
                "M 0 100 L 41 100 L 41 98.2 Q 32 98.2 32 89 L 9 89 Q 9 98.2 0 98.2",
                "M 24 44 L 30 36 L 88 100 L 76 100",
                "M 70 100 L 98 100 L 98 98.2 Q 78 98.2 78 89 L 65.8 89 Q 65.8 98.2 70 98.2",
                "!M 31 46 L 50 64 L 73 29 L 66 29 L 101 -9 L 35 35 L 42.6 35"),
            new Glifo('N', 104f,
                "M 9 0 L 19 0 L 19 100 L 9 100",
                "M 0 0 L 28 0 L 28 1.8 Q 19 1.8 19 11 L 9 11 Q 9 1.8 0 1.8",
                "M 9 100 L 33 100 L 95 0 L 93 -7 L 71 0",
                "M 0 100 L 33 100 L 33 89 L 9 89 Q 9 98.2 0 98.2",
                "M 85 8 L 95 8 L 95 100 L 85 100",
                "M 76 100 L 104 100 L 104 98.2 Q 95 98.2 95 89 L 85 89 Q 85 98.2 76 98.2"),
        };

        /// <summary>Uma peca da marca ja' posta na palavra (unidades).</summary>
        public struct Peca
        {
            public int Letra;
            public bool Raio;
            public Vector2[][] Contornos;
            public Rect Caixa;
        }

        static List<Peca> _pecas;
        static List<Vector2> _pontas;
        static Rect _letras;

        /// <summary>As pecas de "ARKANA" postas lado a lado (unidades; a base das letras em y=0).</summary>
        public static IReadOnlyList<Peca> Pecas { get { Montar(); return _pecas; } }
        /// <summary>A CAIXA DA TINTA (letras + contorno), em unidades: o RectTransform mostra exatamente isto.</summary>
        public static Rect Caixa { get { Montar(); return Crescer(_letras, Contorno); } }
        public static float Aspecto => Caixa.width / Caixa.height;

        static Rect Crescer(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        static void Montar()
        {
            if (_pecas != null) return;
            var pecas = new List<Peca>();
            var pontas = new List<Vector2>();
            float x = 0f;
            string marca = Textos.Marca;
            for (int l = 0; l < marca.Length; l++)
            {
                Glifo g = Achar(marca[l]);
                Vector2 topo = new Vector2(0f, float.MinValue);
                foreach (string s in g.Pecas)
                {
                    Peca p = Ler(s, x, l);
                    pecas.Add(p);
                    foreach (Vector2[] c in p.Contornos) foreach (Vector2 v in c) if (v.y > topo.y) topo = v;
                }
                if (topo.y > Altura + 5f) pontas.Add(topo);   // a ponta de lanca dos As: onde a cintilancia acende
                x += g.Avanco;
            }
            Rect r = pecas[0].Caixa;
            foreach (Peca p in pecas) r = Rect.MinMaxRect(Mathf.Min(r.xMin, p.Caixa.xMin), Mathf.Min(r.yMin, p.Caixa.yMin), Mathf.Max(r.xMax, p.Caixa.xMax), Mathf.Max(r.yMax, p.Caixa.yMax));
            _letras = r;
            _pontas = pontas;
            _pecas = pecas;
        }

        static Glifo Achar(char c)
        {
            foreach (Glifo g in Glifos) if (g.C == c) return g;
            throw new KeyNotFoundException("Logo: sem glifo para '" + c + "' (Textos.Marca mudou?)");
        }

        static Peca Ler(string caminho, float dx, int letra)
        {
            var p = new Peca { Letra = letra, Raio = caminho.StartsWith("!") };
            var contornos = new List<Vector2[]>();
            List<Vector2> atual = null;
            string[] t = caminho.TrimStart('!').Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            Vector2 ultimo = Vector2.zero;
            int i = 0;
            Func<Vector2> ponto = () => { var v = new Vector2(Num(t[i]) + dx, Num(t[i + 1])); i += 2; return v; };
            while (i < t.Length)
            {
                string cmd = t[i++];
                if (cmd == "M") { if (atual != null) contornos.Add(atual.ToArray()); atual = new List<Vector2> { (ultimo = ponto()) }; }
                else if (cmd == "L") atual.Add(ultimo = ponto());
                else if (cmd == "Q")
                {
                    Vector2 c = ponto(), f = ponto();
                    for (int k = 1; k <= CurvaPassos; k++)
                    {
                        float u = k / (float)CurvaPassos;
                        atual.Add((1f - u) * (1f - u) * ultimo + 2f * (1f - u) * u * c + u * u * f);
                    }
                    ultimo = f;
                }
                else throw new FormatException("Logo: comando '" + cmd + "' no caminho " + caminho);
            }
            contornos.Add(atual.ToArray());
            p.Contornos = contornos.ToArray();
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (Vector2[] c in p.Contornos)
                foreach (Vector2 v in c) { x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y); x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y); }
            p.Caixa = Rect.MinMaxRect(x0, y0, x1, y1);
            return p;
        }

        static float Num(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);   // o aparelho e' pt-BR: virgula

        /// <summary>Distancia assinada (unidades) do ponto a' peca: negativa dentro (par-impar), exata por aresta.</summary>
        static float Distancia(in Peca pc, float px, float py)
        {
            float melhor = float.MaxValue;
            bool dentro = false;
            foreach (Vector2[] c in pc.Contornos)
                for (int i = 0, j = c.Length - 1; i < c.Length; j = i++)
                {
                    float ax = c[j].x, ay = c[j].y, ex = c[i].x - ax, ey = c[i].y - ay;
                    float wx = px - ax, wy = py - ay;
                    float t = Mathf.Clamp01((wx * ex + wy * ey) / Mathf.Max(ex * ex + ey * ey, 1e-9f));
                    float qx = wx - ex * t, qy = wy - ey * t;
                    float d2 = qx * qx + qy * qy;
                    if (d2 < melhor) melhor = d2;
                    if ((ay > py) != (c[i].y > py) && px < ax + (py - ay) * ex / ey) dentro = !dentro;
                }
            float d = Mathf.Sqrt(melhor);
            return dentro ? -d : d;
        }

        // ---------- o assado (conta pura, testavel) ----------

        /// <summary>O retangulo (unidades) que a arte de `largura` px cobre, e o atlas: arte embaixo (`altArte` linhas),
        /// a mascara da face logo acima (mesmo tamanho) e a celula da cintilancia em cima (`cel` x `cel`).</summary>
        public static Rect Moldura(int largura, out int altArte, out int cel, out int alt)
        {
            Montar();
            Rect m = Crescer(_letras, Margem);
            altArte = Mathf.CeilToInt(largura * m.height / m.width);
            cel = largura / 16;
            alt = altArte * 2 + cel;
            return new Rect(m.xMin, m.yMin, m.width, altArte * m.width / largura);   // a mesma escala nos dois eixos
        }

        /// <summary>Conta pura: os pixels RGBA do atlas (linha 0 = embaixo, padrao Unity).</summary>
        public static Color32[] Pixels(int largura, out int alt, out int altArte)
        {
            int cel;
            Rect mo = Moldura(largura, out altArte, out cel, out alt);
            int W = largura, H = altArte;
            float s = W / mo.width;   // px por unidade
            List<Peca> pecas = _pecas;
            var dG = new float[W * H];
            var dR = new float[W * H];
            float alcG = (Contorno + 3f) * s, alcR = HaloRaio * s;   // alem disso ninguem olha: satura (e poupa conta)
            for (int y = 0; y < H; y++)
            {
                float uy = mo.yMin + (y + 0.5f) / s;
                for (int x = 0; x < W; x++)
                {
                    float ux = mo.xMin + (x + 0.5f) / s;
                    float g = alcG, r = alcR;
                    for (int i = 0; i < pecas.Count; i++)
                    {
                        Peca pc = pecas[i];
                        float alc = (pc.Raio ? alcR : alcG) / s;
                        Rect c = pc.Caixa;
                        if (ux < c.xMin - alc || ux > c.xMax + alc || uy < c.yMin - alc || uy > c.yMax + alc) continue;
                        float d = Distancia(pc, ux, uy) * s;
                        if (pc.Raio) { if (d < r) r = d; }
                        else if (d < g) g = d;
                    }
                    dG[y * W + x] = g;
                    dR[y * W + x] = r;
                }
            }

            var d0 = new float[W * H];
            for (int i = 0; i < d0.Length; i++) d0[i] = Mathf.Min(dG[i], dR[i]);
            // a ESPESSURA: a face varrida para baixo (o bronze aparece sob cada letra; o contorno abraca os dois)
            int e = Mathf.RoundToInt(Espessura * s);
            var dv = new float[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float m = d0[y * W + x];
                    for (int k = 1; k <= e && y + k < H; k++) m = Mathf.Min(m, d0[(y + k) * W + x]);
                    dv[y * W + x] = m;
                }
            float cont = Contorno * s;
            var sombra = new float[W * H];
            int sx = Mathf.RoundToInt(SombraLado * s), sy = Mathf.RoundToInt(SombraDesce * s);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int ox = x - sx, oy = y + sy;
                    if (ox >= 0 && ox < W && oy >= 0 && oy < H) sombra[y * W + x] = Mathf.Clamp01(0.5f + cont - dv[oy * W + ox]);
                }
            Borrar(sombra, W, H, Mathf.RoundToInt(SombraBorra * s * 0.5f));

            var px = new Color32[W * alt];
            float bisel = Bisel * s;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    float d = d0[i], v = dv[i], rr = dR[i];
                    float uy = mo.yMin + (y + 0.5f) / s;
                    // premultiplicado, de tras para a frente: sombra, halo do raio, contorno, espessura, face
                    Color acc = new Color(0f, 0f, 0f, 0f);
                    acc = Sobre(acc, Color.black, SombraAlfa * sombra[i]);
                    float q = Mathf.Clamp01(1f - Mathf.Max(rr, 0f) / alcR);
                    acc = Sobre(acc, CorHalo, HaloAlfa * q * q);
                    acc = Sobre(acc, CorContorno, Mathf.Clamp01(0.5f + cont - v));
                    float tv = Mathf.Clamp01((Altura - uy) / Altura);
                    acc = Sobre(acc, Color.Lerp(Espessura0, Espessura1, tv), Mathf.Clamp01(0.5f - v));
                    float cf = Mathf.Clamp01(0.5f - d);
                    if (cf > 0f)
                    {
                        // bisel em cinzel: a faixa de `Bisel` da borda inclina para fora; a luz vem de cima a' esquerda
                        float gx = d0[y * W + Mathf.Min(x + 1, W - 1)] - d0[y * W + Mathf.Max(x - 1, 0)];
                        float gy = d0[Mathf.Min(y + 1, H - 1) * W + x] - d0[Mathf.Max(y - 1, 0) * W + x];
                        float n = Mathf.Sqrt(gx * gx + gy * gy);
                        float f = n > 1e-5f ? (gx * Luz.x + gy * Luz.y) / n : 0f;
                        float wb = Mathf.Clamp01(0.5f + d + bisel);
                        Color cor = Degrade(tv);
                        Color raio = Color.Lerp(RaioBorda, RaioNucleo, Mathf.Clamp01((-rr / s - 3f) / 5f));   // nucleo quente so' no miolo
                        cor = Color.Lerp(cor, raio, Mathf.Clamp01(0.5f - rr));
                        cor = f > 0f ? Color.Lerp(cor, BiselLuz, f * 0.85f * wb) : Color.Lerp(cor, BiselSombra, -f * 0.7f * wb);
                        acc = Sobre(acc, cor, cf);
                    }
                    px[i] = Reta(acc);
                    byte m = (byte)Mathf.RoundToInt(cf * 255f);
                    px[(y + H) * W + x] = new Color32(255, 255, 255, m);   // a mascara da face: o reflexo so' corre no ouro
                }
            // a cintilancia: estrela de quatro pontas macia, branca (a cor vem do vertice)
            for (int y = 0; y < cel; y++)
                for (int x = 0; x < cel; x++)
                {
                    float u = (x + 0.5f) / cel * 2f - 1f, w = (y + 0.5f) / cel * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + w * w);
                    float a = Mathf.Exp(-Mathf.Abs(u) * 4.5f - w * w * 70f) + Mathf.Exp(-Mathf.Abs(w) * 4.5f - u * u * 70f) + Mathf.Exp(-r * r * 22f);
                    a = Mathf.Clamp01(a) * Mathf.Clamp01((1f - r) * 4f);
                    px[(2 * H + y) * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            return px;
        }

        static Color Degrade(float t)
        {
            if (t < 0.45f) return Color.Lerp(Face0, Face1, t / 0.45f);
            if (t < 0.55f) return Color.Lerp(Face1, Face2, (t - 0.45f) / 0.1f);   // o "horizonte" do metal
            return Color.Lerp(Face2, Face3, (t - 0.55f) / 0.45f);
        }

        /// <summary>`c` (reta) com alfa `a` por cima do acumulado (premultiplicado).</summary>
        static Color Sobre(Color acc, Color c, float a) =>
            new Color(c.r * a + acc.r * (1f - a), c.g * a + acc.g * (1f - a), c.b * a + acc.b * (1f - a), a + acc.a * (1f - a));

        static Color32 Reta(Color p)
        {
            if (p.a <= 1e-4f) return new Color32(0, 0, 0, 0);
            return new Color32(B(p.r / p.a), B(p.g / p.a), B(p.b / p.a), B(p.a));
        }

        static Color H(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        static byte B(float v) => (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);

        /// <summary>Caixa (box blur) separavel, duas passadas: a sombra macia. Fora da imagem conta zero.</summary>
        static void Borrar(float[] a, int W, int H, int r)
        {
            if (r <= 0) return;
            var tmp = new float[a.Length];
            float k = 1f / (2 * r + 1);
            for (int passo = 0; passo < 2; passo++)
            {
                for (int y = 0; y < H; y++)
                {
                    float soma = 0f;
                    for (int x = 0; x <= r && x < W; x++) soma += a[y * W + x];
                    for (int x = 0; x < W; x++)
                    {
                        tmp[y * W + x] = soma * k;
                        if (x + r + 1 < W) soma += a[y * W + x + r + 1];
                        if (x - r >= 0) soma -= a[y * W + x - r];
                    }
                }
                for (int x = 0; x < W; x++)
                {
                    float soma = 0f;
                    for (int y = 0; y <= r && y < H; y++) soma += tmp[y * W + x];
                    for (int y = 0; y < H; y++)
                    {
                        a[y * W + x] = soma * k;
                        if (y + r + 1 < H) soma += tmp[(y + r + 1) * W + x];
                        if (y - r >= 0) soma -= tmp[(y - r) * W + x];
                    }
                }
            }
        }

        // ---------- o Graphic ----------

        static Texture2D _tex;

        public override Texture mainTexture
        {
            get
            {
                if (_tex == null)
                {
                    int alt, altArte;
                    // ponytail: assado no primeiro quadro, ~0,2 s uma vez por processo (medido no mono do editor, 1024 px);
                    // se pesar no boot do aparelho: Pixels e' conta pura, roda numa thread e so' o Apply fica no principal
                    Color32[] px = Pixels(LarguraTextura, out alt, out altArte);
                    _tex = new Texture2D(LarguraTextura, alt, TextureFormat.RGBA32, true);
                    _tex.name = "LogoArkana";
                    // so' um campo estatico segura a textura: sem isto o UnloadUnusedAssets a destroi e a marca some (foto 45)
                    _tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    _tex.wrapMode = TextureWrapMode.Clamp;
                    _tex.filterMode = FilterMode.Trilinear;
                    _tex.SetPixels32(px);
                    _tex.Apply(true, true);
                }
                return _tex;
            }
        }

        static readonly List<UIVertex> _v = new List<UIVertex>();
        static readonly List<int> _t = new List<int>();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            Preencher(_v, _t, GetPixelAdjustedRect(), color, Brilho ? FaseAgora() : -1f);
            vh.Clear();
            vh.AddUIVertexStream(_v, _t);
        }

        float FaseAgora()
        {
            if (Fase >= 0f) return Fase;
            float t = Mathf.Repeat(Time.unscaledTime, CicloS);
            return t < VarreS ? t / VarreS : -1f;   // fora da varredura: so' o ouro parado
        }

        void Update()
        {
            if (!Brilho) return;
            bool agora = FaseAgora() >= 0f;
            if (agora || _reflexoNaTela) SetVerticesDirty();   // o reflexo anda: refaz a malha (quad + ~20 vertices)
            _reflexoNaTela = agora;
        }

        static readonly float[] FaixaK = { -1f, -0.55f, -0.22f, 0f, 0.22f, 0.55f, 1f };
        static readonly float[] FaixaA = { 0f, 0.3f, 0.78f, 1f, 0.78f, 0.3f, 0f };

        /// <summary>
        /// Conta pura (testavel): a malha da marca no retangulo `r` (vertices + indices de triangulo) — o quad da arte (a
        /// CAIXA da tinta cabe em `r` sem entortar, centrada; a sombra e o halo vazam em volta) e, com `fase` 0..1, o REFLEXO
        /// inclinado atravessando a face e a cintilancia acendendo na ponta de cada A quando ele passa. Fase negativa = so' o quad.
        /// </summary>
        public static void Preencher(List<UIVertex> v, List<int> t, Rect r, Color32 cor, float fase)
        {
            v.Clear();
            t.Clear();
            Rect cx = Caixa;
            float k = Mathf.Min(r.width / cx.width, r.height / cx.height);   // px por unidade
            if (!(k > 0f)) return;
            Vector2 o = r.center - cx.center * k;   // tela = o + unidade * k
            int altArte, cel, alt;
            Rect mo = Moldura(LarguraTextura, out altArte, out cel, out alt);
            float vArte = altArte / (float)alt;
            Quad(v, t, o + mo.min * k, o + mo.max * k, Vector2.zero, new Vector2(1f, vArte), cor);
            if (fase < 0f) return;

            float meia = FaixaMeia * cx.height, incl = FaixaInclina * mo.height * 0.5f;
            float c = Mathf.Lerp(cx.xMin - meia - incl, cx.xMax + meia + incl, Mathf.Clamp01(fase));
            for (int i = 0; i < FaixaK.Length; i++)
            {
                var branco = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(FaixaA[i] * FaixaAlfa * cor.a));
                float ux = c + FaixaK[i] * meia;
                Vector2 baixo = new Vector2(ux - incl, mo.yMin), cima = new Vector2(ux + incl, mo.yMax);
                if (i > 0)
                {
                    int b = v.Count;   // b-2/b-1 = baixo/cima anteriores, b/b+1 = estes
                    t.Add(b - 2); t.Add(b - 1); t.Add(b + 1);
                    t.Add(b + 1); t.Add(b); t.Add(b - 2);
                }
                Vert(v, o + baixo * k, branco, UvMascara(baixo, mo, vArte));
                Vert(v, o + cima * k, branco, UvMascara(cima, mo, vArte));
            }
            float v0 = 2f * altArte / alt;
            foreach (Vector2 p in _pontas)
            {
                float perto = Mathf.Clamp01(1f - Mathf.Abs(p.x - c) / (meia * 2.5f));
                if (perto <= 0f) continue;
                float lado = FaiscaTam * (0.5f + 0.5f * perto) * 0.5f;
                Quad(v, t, o + (p - Vector2.one * lado) * k, o + (p + Vector2.one * lado) * k, new Vector2(0f, v0),
                    new Vector2(cel / (float)LarguraTextura, 1f), new Color32(255, 255, 255, (byte)Mathf.RoundToInt(perto * perto * cor.a)));
            }
        }

        static Vector2 UvMascara(Vector2 u, Rect mo, float vArte) =>
            new Vector2((u.x - mo.xMin) / mo.width, vArte + (u.y - mo.yMin) / mo.height * vArte);

        static void Vert(List<UIVertex> v, Vector2 p, Color32 cor, Vector2 uv)
        {
            UIVertex u = UIVertex.simpleVert;
            u.position = p;
            u.color = cor;
            u.uv0 = uv;
            v.Add(u);
        }

        static void Quad(List<UIVertex> v, List<int> t, Vector2 p0, Vector2 p1, Vector2 uv0, Vector2 uv1, Color32 cor)
        {
            int b = v.Count;
            Vert(v, p0, cor, uv0);
            Vert(v, new Vector2(p0.x, p1.y), cor, new Vector2(uv0.x, uv1.y));
            Vert(v, p1, cor, uv1);
            Vert(v, new Vector2(p1.x, p0.y), cor, new Vector2(uv1.x, uv0.y));
            t.Add(b); t.Add(b + 1); t.Add(b + 2);
            t.Add(b + 2); t.Add(b + 3); t.Add(b);
        }
    }
}
