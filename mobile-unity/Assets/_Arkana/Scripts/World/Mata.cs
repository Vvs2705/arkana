using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arkana.World
{
    /// <summary>
    /// O CHAO DA MATA (onda 14A): a floresta da foto 31 tinha so' arvore, moita e chao escuro. Aqui entram troncos caidos com
    /// musgo, cachos de cogumelos luminosos ao pe' das arvores e dos troncos, samambaias em manchas e uns poucos cristais arcanos
    /// brilhando na sombra (a ilha e' arcana: o cristal e' o tempero, poucos e marcantes). Modelos da Meshy (50-53-*.glb, base em y = 0).
    /// PLANTIO puro por seed, no molde da Praia: so' no Bioma.Floresta (a campina ate' BordaDaMata alem dele recebe uma fracao),
    /// fora das arvores, pedras e moitas da Vegetacao (`ocupados`), longe dos nascimentos para quem colide. DESENHO como a Praia:
    /// RenderMeshInstanced por BLOCO de 60 m, um lote por peca x LOD; cogumelo e samambaia trocam de LOD perto (Lod1Cogumelo,
    /// Lod1Samambaia) e somem cedo (CorteCogumelo, CorteSamambaia), o tronco (LOD unico) some a CorteTronco e o cristal a
    /// CorteCristal (e' ponto de referencia). Tronco e cristal ganham caixa de colisao; cristal e cogumelo ACENDEM por emissao (a
    /// propria textura como mapa, sem luz pontual). Foge tambem do KIT, que monta depois (PegadasDoKit). Sem o .glb a peca nao
    /// nasce; sem instancing (-nographics) nasce so' o colisor. Montada pela Vegetacao (a Ilha nao muda).
    /// </summary>
    public sealed class Mata : MonoBehaviour
    {
        public enum Peca { Cristal, Tronco, Cogumelo, Samambaia }

        /// <summary>Os .glb em Resources, na ordem de Peca. O LOD1 e' o mesmo nome + "-lod1" (cogumelo e samambaia).</summary>
        public static readonly string[] Glb = { "50-cristal-arcano", "51-tronco-musgo", "52-cogumelos", "53-samambaia" };
        /// <summary>O tamanho de cada .glb (m: X, Y altura, Z), medido nos bounds. O tronco deitado tem ~1 m de corpo; os 2,15 m
        /// sao do galho partido erguido.</summary>
        public static readonly Vector3[] Tamanho =
        {
            new Vector3(0.79f, 1.5f, 0.82f), new Vector3(4f, 2.15f, 2.36f), new Vector3(0.8f, 0.53f, 0.71f), new Vector3(1.18f, 0.98f, 1.2f),
        };
        /// <summary>Quem tem o "-lod1": cogumelo e samambaia. Tronco e cristal sao LOD unico (somem pelo corte).</summary>
        // o tronco ganhou LOD (16/09): o de 1,5 K de perto lia como musgo em blocos (foto 51-mata-chao); perto vai o remesh inteiro
        // de 3,1 K do site e o de 1,5 K vira o LOD1
        static readonly bool[] TemLod1 = { false, true, true, true };

        /// <summary>Uma peca da mata, PURA: a matriz leva o molde (centro da base na origem, +Y para cima) ao mundo.</summary>
        public struct PecaPlantada
        {
            public Peca Tipo;
            public Matrix4x4 M;
            public float Escala;
            public bool Colide;
        }

        /// <summary>Quantas pecas a ilha de REFERENCIA (300 m) pede; planta x AREA. Na de 600 m: 16 cristais, 32 troncos, ~110 cachos
        /// de cogumelo, 40 manchas de samambaia (~6 touceiras cada). KNOB: por foto.</summary>
        public const int CristaisRef = 4, TroncosRef = 8, CachosRef = 28, ManchasRef = 10;
        /// <summary>Quanto a mata vaza para a campina alem do bioma (m) e que fracao dos sorteios entra la' (densidade caindo). KNOB.</summary>
        public const float BordaDaMata = 5f, DensidadeDaBorda = 0.4f;
        /// <summary>Dois cristais nunca a menos disto (m): poucos e marcantes. KNOB.</summary>
        public const float EntreCristais = 14f;
        /// <summary>Quanto a base desce abaixo do chao desenhado (m, x escala): a samambaia e o cogumelo trazem um disco de terra. KNOB.</summary>
        const float AfundaSamambaia = 0.05f, AfundaCogumelo = 0.05f;
        /// <summary>O corpo deitado do tronco (raio da pegada, x escala): tres circulos ao longo do eixo, para cogumelo e samambaia
        /// encostarem no lado dele (um disco de 2 m so' deixaria chegar nas pontas).</summary>
        const float CorpoDoTronco = 0.55f, PassoDoCorpo = 1.3f;
        /// <summary>Folga livre em volta de cada nascimento para quem colide (m, alem do raio da peca): o PlantioDoKit.FolgaDoNascimento.</summary>
        const float FolgaDoNascimento = 6f;

        /// <summary>Distancia 3D do LOD1 (cogumelo de 1,2K, samambaia de 1,4K) e os cortes (m). No miolo da mata, com os dois a 40 m
        /// como a arvore e o miudo ate' 70 m, o chao da mata dava 320K tris antes do frustum (sonda de 16/09). A samambaia e' a
        /// mais numerosa e o LOD1 dela (455) ainda pesa: some a 45 m; o cogumelo (o brilho) vai ate' 60. KNOB: por foto e FPS.</summary>
        public const float Lod1Tronco = 25f;
        public const float Lod1Cogumelo = 22f, Lod1Samambaia = 16f, CorteCogumelo = 60f, CorteSamambaia = 45f, CorteTronco = 150f, CorteCristal = 200f;
        const float PassoBloco = 60f;
        const float Periodo = 0.25f;

        /// <summary>A TINTA de cada peca (multiplica a textura, sRGB). O cristal ja' vem com o brilho pintado; o tronco (desneon no
        /// Blender) fica como veio; o cogumelo ganha um pouco de luz; a samambaia da Meshy chega verde-limao e cai para o verde
        /// da mata (CorMusgo). KNOB: por foto.</summary>
        static readonly Color[] Tinta =
        {
            new Color(1f, 1f, 1.04f), new Color(1.02f, 1f, 0.94f), new Color(1.06f, 1.04f, 1f), new Color(0.78f, 0.92f, 0.8f),
        };
        /// <summary>A EMISSAO do cristal e do cogumelo: multiplica a PROPRIA textura (o brilho ja' esta' pintado nela). O bloom do pos
        /// (Ilha.MontarPos) acende acima de 1,1: o cristal encosta no limiar nas facetas claras, o cogumelo fica bem abaixo. KNOB por foto.</summary>
        static readonly Color EmissaoCristal = new Color(0.85f, 0.8f, 1.1f), EmissaoCogumelo = new Color(0.22f, 0.32f, 0.45f);

        // ---------------------------------------------------------------- plantio (puro)

        /// <summary>
        /// A mata da ilha, PURA por seed: troncos caidos (girados, meio enterrados), cristais espalhados e longe uns dos outros,
        /// cachos de cogumelo ao pe' de uma ANCORA (arvore, pedra ou moita de `ocupados` dentro da mata, pedra do kit, ou um tronco
        /// daqui) e as manchas de samambaia. `ocupados` (x, z, raio em w) e' respeitado e recebe o que for plantado; as pegadas do
        /// KIT (PegadasDoKit, `temGlb` como la') entram junto.
        /// </summary>
        public static List<PecaPlantada> Plantio(Relevo r, List<Vector4> ocupados = null, System.Func<string, bool> temGlb = null,
            IList<Vector4> reservadosDoKit = null)
        {
            var lista = new List<PecaPlantada>();
            if (r == null) return lista;
            if (ocupados == null) ocupados = new List<Vector4>();
            ocupados.AddRange(PegadasDoKit(r, temGlb, reservadosDoKit));
            float area = r.Escala * r.Escala;
            float raioMata = RaioDaMata(r);
            var ancoras = new List<Vector4>();
            foreach (Vector4 o in ocupados)
                if (Vector2.Distance(new Vector2(o.x, o.y), r.Floresta) < raioMata) ancoras.Add(o);

            // troncos caidos: o corpo (3 circulos) entra nos ocupados e nas ancoras
            var rng = new Sorteio(141);
            int alvo = Mathf.RoundToInt(TroncosRef * area), feitas = 0, tentativas = 0;
            while (feitas < alvo && tentativas++ < alvo * 800)
            {
                Vector2 p = NaMata(rng, r);
                float e = rng.Faixa(0.8f, 1.2f);
                float giro = rng.Faixa(0f, 360f);
                if (!Lugar(r, rng, p, 0.85f)) continue;   // tronco na ladeira rola
                var eixo = new Vector2(Mathf.Cos(giro * Mathf.Deg2Rad), -Mathf.Sin(giro * Mathf.Deg2Rad));   // +X do molde girado em Y
                float corpo = CorpoDoTronco * e;
                bool livre = !PertoDeNascimento(r, p, FolgaDoNascimento + 0.5f * Tamanho[1].x * e);
                for (int k = -1; k <= 1 && livre; k++) livre = !Invade(ocupados, p + eixo * (k * PassoDoCorpo * e), corpo + 0.4f);
                if (!livre) continue;
                lista.Add(Tronco(r, rng, p, giro, e));
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 c = p + eixo * (k * PassoDoCorpo * e);
                    ocupados.Add(new Vector4(c.x, c.y, 0f, corpo));
                    ancoras.Add(new Vector4(c.x, c.y, 0f, corpo));
                }
                feitas++;
            }

            // cristais: poucos, longe uns dos outros, com colisor (longe dos nascimentos)
            rng = new Sorteio(142);
            alvo = Mathf.RoundToInt(CristaisRef * area);
            feitas = 0;
            tentativas = 0;
            var cristais = new List<Vector2>();
            while (feitas < alvo && tentativas++ < alvo * 900)
            {
                Vector2 p = NaMata(rng, r);
                float e = rng.Faixa(0.6f, 1.4f);
                if (!Lugar(r, rng, p, 0.7f)) continue;
                float raio = 0.5f * Tamanho[0].x * e;
                if (Invade(ocupados, p, raio + 0.6f) || PertoDeNascimento(r, p, FolgaDoNascimento + raio)) continue;
                bool longe = true;
                foreach (Vector2 c in cristais) longe &= Vector2.Distance(c, p) >= EntreCristais;
                if (!longe) continue;
                lista.Add(Cristal(r, rng, p, e));
                ocupados.Add(new Vector4(p.x, p.y, 0f, raio));
                cristais.Add(p);
                feitas++;
            }

            // cogumelos em CACHOS ao pe' de uma ancora (sem ancora nenhuma: soltos na mata)
            rng = new Sorteio(143);
            alvo = Mathf.RoundToInt(CachosRef * area);
            feitas = 0;
            tentativas = 0;
            while (feitas < alvo && tentativas++ < alvo * 600)
            {
                float e = rng.Faixa(0.5f, 1.3f), raio = 0.5f * Tamanho[2].x * e;
                Vector2 p;
                if (ancoras.Count > 0)
                {
                    Vector4 a = ancoras[rng.Int(ancoras.Count)];
                    float ang = rng.Faixa(0f, Mathf.PI * 2f), d = a.w + raio + rng.Faixa(0.1f, 0.5f);
                    p = new Vector2(a.x + Mathf.Cos(ang) * d, a.y + Mathf.Sin(ang) * d);
                }
                else p = NaMata(rng, r);
                if (!Lugar(r, rng, p, 0.6f) || Invade(ocupados, p, raio * 0.8f)) continue;
                lista.Add(Cogumelo(r, rng, p, e));
                ocupados.Add(new Vector4(p.x, p.y, 0f, raio * 0.8f));
                feitas++;
            }

            // samambaias em MANCHAS: uma semente e 4-9 touceiras em volta (sorteio parelho le' como plantacao)
            rng = new Sorteio(144);
            alvo = Mathf.RoundToInt(ManchasRef * area);
            int manchas = 0;
            tentativas = 0;
            while (manchas < alvo && tentativas++ < alvo * 600)
            {
                Vector2 s = NaMata(rng, r);
                if (!Lugar(r, rng, s, 0.6f) || Invade(ocupados, s, 1f)) continue;
                manchas++;
                int n = 4 + rng.Int(6);
                feitas = lista.Count;
                for (int k = 0; k < n * 3 && lista.Count - feitas < n; k++)
                {
                    float a = rng.Faixa(0f, Mathf.PI * 2f), d = Mathf.Sqrt(rng.Float()) * rng.Faixa(1.2f, 3.5f);
                    Vector2 p = s + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
                    float e = rng.Faixa(0.7f, 1.3f), raio = 0.3f * e;
                    // a touceira reserva meia pegada: vizinha encosta (a mancha fecha), nunca uma dentro da outra
                    if (!Lugar(r, rng, p, 0.6f) || Invade(ocupados, p, raio)) continue;
                    lista.Add(Samambaia(r, rng, p, e));
                    ocupados.Add(new Vector4(p.x, p.y, 0f, raio));
                }
            }
            return lista;
        }

        static int Contar(List<PecaPlantada> l, Peca t)
        {
            int n = 0;
            for (int i = 0; i < l.Count; i++) if (l[i].Tipo == t) n++;
            return n;
        }

        static PecaPlantada Tronco(Relevo r, Sorteio rng, Vector2 p, float giro, float e)
        {
            // o sorteio sai SEMPRE na mesma ordem: tombo (2), afundar
            var tombo = new Vector3(rng.Faixa(-6f, 6f), 0f, rng.Faixa(-4f, 4f));
            // meio enterrado: 12-30% do corpo de ~1 m dentro do humus (na ladeira a ponta de cima afunda mais). KNOB
            float afunda = e * rng.Faixa(0.12f, 0.3f);
            return new PecaPlantada { Tipo = Peca.Tronco, Escala = e, Colide = true, M = Praia.Assentar(r, Tamanho[1], p, giro, tombo, e, 0.85f, afunda) };
        }

        static PecaPlantada Cristal(Relevo r, Sorteio rng, Vector2 p, float e)
        {
            float giro = rng.Faixa(0f, 360f);
            var tombo = new Vector3(rng.Faixa(-4f, 4f), 0f, rng.Faixa(-4f, 4f));
            float afunda = Tamanho[0].y * e * rng.Faixa(0.06f, 0.12f);   // a pedra da base entra no chao. KNOB
            return new PecaPlantada { Tipo = Peca.Cristal, Escala = e, Colide = true, M = Praia.Assentar(r, Tamanho[0], p, giro, tombo, e, 0.7f, afunda) };
        }

        static PecaPlantada Cogumelo(Relevo r, Sorteio rng, Vector2 p, float e)
        {
            float giro = rng.Faixa(0f, 360f);
            var tombo = new Vector3(rng.Faixa(-5f, 5f), 0f, rng.Faixa(-5f, 5f));
            return new PecaPlantada { Tipo = Peca.Cogumelo, Escala = e, M = Praia.Assentar(r, Tamanho[2], p, giro, tombo, e, 0.9f, AfundaCogumelo * e) };
        }

        static PecaPlantada Samambaia(Relevo r, Sorteio rng, Vector2 p, float e)
        {
            float giro = rng.Faixa(0f, 360f);
            var tombo = new Vector3(rng.Faixa(-5f, 5f), 0f, rng.Faixa(-5f, 5f));
            return new PecaPlantada { Tipo = Peca.Samambaia, Escala = e, M = Praia.Assentar(r, Tamanho[3], p, giro, tombo, e, 0.85f, AfundaSamambaia * e) };
        }

        /// <summary>
        /// As pegadas do KIT perto da mata (x, z, raio em w): a Ilha monta o KitCenario DEPOIS da Vegetacao e ele nao sabe da mata —
        /// sem isto, 29 troncos e cristais nasciam dentro da rocha de basalto, da ponte de raiz e da torre (sonda de 16/09). PURO:
        /// o mesmo PlantioDoKit, na ordem do KitCenario.Montar (miolo das ruinas, altar, pecas). `temGlb` null = todas (a sonda);
        /// a casca passa o Resources.Load (peca sem .glb nao reserva chao, como no kit).
        /// `reservados` = as Pegadas das ruinas, que o kit real tambem recebe (Ilha.cs monta as Ruinas ANTES da Vegetacao): sem
        /// elas o sorteio daqui divergia do real perto das ruinas e, em cascata, no fim da sequencia (foto 51: um tronco na rocha
        /// de basalto, 16/09).
        /// </summary>
        public static List<Vector4> PegadasDoKit(Relevo r, System.Func<string, bool> temGlb = null, IList<Vector4> reservados = null)
        {
            var kit = new List<Vector4> { new Vector4(r.Ruinas.x, r.Ruinas.y, 0f, r.RuinasR * 0.7f) };
            if (reservados != null) kit.AddRange(reservados);
            PlantioDoKit.Altar(r, kit);
            foreach (PecaDoKit peca in PlantioDoKit.Pecas)
                if (temGlb == null || temGlb(peca.Id)) PlantioDoKit.Posicoes(r, PlantioDoKit.SeedKit, peca, kit);
            float alcance = RaioDaMata(r);
            kit.RemoveAll(k => Vector2.Distance(new Vector2(k.x, k.y), r.Floresta) - k.w > alcance);
            return kit;
        }

        /// <summary>Ate' onde o sorteio procura chao de mata (m do centro da floresta): o bioma (peso 0,5 da rampa do
        /// Relevo.PesosEm: 0,3 R .. R + 9) mais a borda. ponytail: repete a rampa do Relevo; se ela mudar, muda aqui (o teste pega).</summary>
        public static float RaioDaMata(Relevo r) => 0.65f * r.FlorestaR + 4.5f + BordaDaMata;

        static Vector2 NaMata(Sorteio rng, Relevo r)
        {
            float a = rng.Faixa(0f, Mathf.PI * 2f), d = Mathf.Sqrt(rng.Float()) * RaioDaMata(r);
            return r.Floresta + new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
        }

        /// <summary>Chao de mata em `p`: o Bioma.Floresta (seco, fora de ladeira); a campina so' se BordaDaMata para dentro ja' e'
        /// mata, e so' DensidadeDaBorda dos sorteios (a densidade cai na borda em vez de cortar numa linha).</summary>
        static bool Lugar(Relevo r, Sorteio rng, Vector2 p, float nyMin)
        {
            if (!NaBorda(r, p, rng.Float())) return false;
            return r.NormalY(p.x, p.y) >= nyMin;
        }

        /// <summary>`p` e' mata (Bioma.Floresta) ou a borda dela: campina cujo ponto BordaDaMata para o centro da floresta ainda e'
        /// mata — e ai' so' entra se o `sorteio` (0-1) cair abaixo de DensidadeDaBorda.</summary>
        public static bool NaBorda(Relevo r, Vector2 p, float sorteio)
        {
            Bioma b = r.BiomaEm(p.x, p.y);
            if (b == Bioma.Floresta) return true;
            if (b != Bioma.Campina || sorteio >= DensidadeDaBorda) return false;
            Vector2 dentro = p + (r.Floresta - p).normalized * BordaDaMata;
            return r.BiomaEm(dentro.x, dentro.y) == Bioma.Floresta;
        }

        static bool PertoDeNascimento(Relevo r, Vector2 p, float raio)
        {
            foreach (Vector3 n in r.Nascimentos)
                if ((n.x - p.x) * (n.x - p.x) + (n.z - p.y) * (n.z - p.y) < raio * raio) return true;
            return false;
        }

        // ponytail: copia do Praia.Invade (privado la'); juntar quando alguem mexer na Praia
        static bool Invade(List<Vector4> ocupados, Vector2 p, float raio)
        {
            for (int i = 0; i < ocupados.Count; i++)
            {
                float dx = ocupados[i].x - p.x, dz = ocupados[i].y - p.y, s = ocupados[i].w + raio;
                if (dx * dx + dz * dz < s * s) return true;
            }
            return false;
        }

        /// <summary>Ate' onde cada peca desenha (m).</summary>
        public static float Corte(Peca t) =>
            t == Peca.Cristal ? CorteCristal : t == Peca.Tronco ? CorteTronco : t == Peca.Cogumelo ? CorteCogumelo : CorteSamambaia;

        /// <summary>A peca em `peca` vista de `olho` desenha? Distancia 3D (da queda, bem em cima, o miudo some).</summary>
        public static bool Visivel(Peca t, Vector3 olho, Vector3 peca) => (olho - peca).sqrMagnitude < Corte(t) * Corte(t);

        /// <summary>Tronco, cogumelo e samambaia em `olho` vao no LOD1? Distancia 3D, como a arvore. O cristal nunca (LOD unico).</summary>
        public static bool Lod1(Peca t, Vector3 olho, Vector3 peca)
        {
            if (!TemLod1[(int)t]) return false;
            float d = t == Peca.Samambaia ? Lod1Samambaia : t == Peca.Tronco ? Lod1Tronco : Lod1Cogumelo;
            return (olho - peca).sqrMagnitude > d * d;
        }

        // ---------------------------------------------------------------- casca

        sealed class Lote
        {
            public Matrix4x4[] M;
            public int N;
        }

        /// <summary>Bloco de desenho: a caixa que o frustum corta e um lote por peca x LOD ([tipo * 2 + lod]).</summary>
        sealed class Bloco
        {
            public Bounds Caixa;
            public readonly List<int> Pecas = new List<int>();
            public readonly Lote[] Lotes = new Lote[8];
        }

        readonly List<PecaPlantada> pecas = new List<PecaPlantada>();
        readonly List<Matrix4x4> vis = new List<Matrix4x4>();   // a matriz com o molde, por peca
        readonly List<Bloco> blocos = new List<Bloco>();
        readonly Mesh[] malha = new Mesh[8];                     // [tipo * 2 + lod]; null = sem o .glb ou sem instancing
        readonly int[] tris = new int[8];
        static readonly Material[] mats = new Material[8];
        Camera olho;
        float relogio = Periodo;

        /// <summary>Quantas pecas do tipo nasceram (0 = sem o .glb).</summary>
        public int Contar(Peca t) => Contar(pecas, t);
        public PecaPlantada Plantada(int i) => pecas[i];
        public int Total => pecas.Count;
        /// <summary>O que carregou (diag da foto): os .glb e o LOD1 de cada um.</summary>
        public string Moldes { get; private set; } = "";
        /// <summary>Do ultimo corte (4 Hz): lotes mandados por quadro e triangulos ANTES do frustum.</summary>
        public int LotesEnviados { get; private set; }
        public int TrisEnviados { get; private set; }

        /// <summary>A camera do LOD e do corte; null = Camera.main. A Vegetacao.Olho repassa a da foto.</summary>
        public Camera Olho
        {
            get => olho;
            set { olho = value; relogio = Periodo; }
        }

        public void Montar(Relevo relevo, List<Vector4> ocupados, IList<Vector4> reservadosDoKit = null)
        {
            pecas.Clear(); vis.Clear(); blocos.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            if (relevo == null) return;
            var tem = new bool[4];
            var molde = new Matrix4x4[4];
            var tex = new Texture[8];
            Moldes = "";
            for (int t = 0; t < 4; t++)
            {
                Mesh m = Vegetacao.MalhaDoGlb(Glb[t], out Texture tx);
                if (m == null) continue;
                tem[t] = true;
                Bounds b = m.bounds;
                molde[t] = Matrix4x4.Translate(-new Vector3(b.center.x, b.min.y, b.center.z));
                Moldes += (Moldes.Length > 0 ? " + " : "") + Glb[t];
                if (!SystemInfo.supportsInstancing || tx == null) continue;   // -nographics: so' o colisor
                int k = t * 2;
                malha[k] = m; tex[k] = tx;
                if (!TemLod1[t]) continue;
                // o LOD1 e' outro .glb (outra UV, outra textura -> material proprio); sem ele, o LOD0 longe (custa triangulo, nao some)
                malha[k + 1] = Vegetacao.MalhaDoGlb(Glb[t] + "-lod1", out Texture tx1);
                if (malha[k + 1] != null && tx1 != null) { tex[k + 1] = tx1; Moldes += "(lod1)"; }
                else { malha[k + 1] = m; tex[k + 1] = tx; }
            }
            for (int k = 0; k < 8; k++)
            {
                if (tex[k] == null) continue;
                var t = (Peca)(k / 2);
                bool brilha = t == Peca.Cristal || t == Peca.Cogumelo;
                Material m = Vegetacao.MaterialDaMeshy(ref mats[k], "Mata" + t + (k & 1), tex[k], Tinta[k / 2],
                    brilha ? Vegetacao.MoldeBrilho : "ArkanaArvoreInstancing");
                if (m == null) { malha[k] = null; continue; }   // sem o Lit: a peca nao desenha (o colisor fica)
                if (t == Peca.Cristal) Acender(m, tex[k], EmissaoCristal);
                else if (t == Peca.Cogumelo) Acender(m, tex[k], EmissaoCogumelo);
            }
            for (int k = 0; k < 8; k++) tris[k] = malha[k] != null ? (int)(malha[k].GetIndexCount(0) / 3) : 0;

            foreach (PecaPlantada p in Plantio(relevo, ocupados, id => Resources.Load<GameObject>(id) != null, reservadosDoKit))
            {
                int t = (int)p.Tipo;
                if (!tem[t]) continue;   // sem o .glb a peca nao nasce (nem colisor invisivel)
                pecas.Add(p);
                vis.Add(p.M * molde[t]);
                if (p.Colide) Colisor(p);
            }
            MontarBlocos();
            relogio = Periodo;
        }

        /// <summary>A EMISSAO com a propria textura de cor como mapa: o brilho que a Meshy pintou (facetas do cristal, pintas e
        /// chapeu do cogumelo) acende na sombra da mata. Sem luz pontual (custo). O material e' cache: ligar duas vezes nao muda nada.</summary>
        static void Acender(Material m, Texture tex, Color emissao)
        {
            if (!m.HasProperty("_EmissionColor")) return;
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", tex);
            m.SetColor("_EmissionColor", emissao);
        }

        /// <summary>A caixa de colisao do tronco (o corpo deitado, ~0,9 m: o galho erguido nao vira parede) e do cristal (a pedra
        /// da base e o cacho, 55% da largura), girada com a peca.</summary>
        void Colisor(PecaPlantada p)
        {
            var go = new GameObject(p.Tipo.ToString());
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(p.M.GetColumn(3), Quaternion.LookRotation(p.M.GetColumn(2), p.M.GetColumn(1)));
            go.transform.localScale = Vector3.one * p.Escala;
            var bc = go.AddComponent<BoxCollider>();
            Vector3 tam = Tamanho[(int)p.Tipo];
            if (p.Tipo == Peca.Tronco) { bc.center = new Vector3(0f, 0.45f, 0f); bc.size = new Vector3(tam.x * 0.9f, 0.9f, tam.z * 0.5f); }
            else { bc.center = new Vector3(0f, tam.y * 0.4f, 0f); bc.size = new Vector3(tam.x * 0.55f, tam.y * 0.8f, tam.z * 0.55f); }
        }

        /// <summary>Bina as pecas em blocos de ~60 m e reserva os lotes (os dois LOD de quem tem). Tudo alocado AQUI: o corte de
        /// 4 Hz so' copia matriz. ponytail: ilha na origem (a Vegetacao ja' supoe): matriz e caixa em coordenada de mundo.</summary>
        void MontarBlocos()
        {
            float meio = PassoBloco * 1000f;
            var porBloco = new Dictionary<long, Bloco>();
            for (int i = 0; i < pecas.Count; i++)
            {
                int t = (int)pecas[i].Tipo;
                if (malha[t * 2] == null) continue;
                Vector3 p = pecas[i].M.GetColumn(3);
                long k = (long)Mathf.FloorToInt((p.x + meio) / PassoBloco) * 100000L + Mathf.FloorToInt((p.z + meio) / PassoBloco);
                float e = pecas[i].Escala, lado = 0.75f * e * Mathf.Max(Tamanho[t].x, Tamanho[t].z) + 0.5f;
                var caixa = new Bounds(p + Vector3.up * (0.5f * Tamanho[t].y * e), new Vector3(2f * lado, Tamanho[t].y * e + 1f, 2f * lado));
                if (!porBloco.TryGetValue(k, out Bloco b))
                {
                    porBloco[k] = b = new Bloco { Caixa = caixa };
                    blocos.Add(b);
                }
                b.Pecas.Add(i);
                b.Caixa.Encapsulate(caixa);
            }
            var n = new int[4];
            foreach (Bloco b in blocos)
            {
                System.Array.Clear(n, 0, 4);
                foreach (int i in b.Pecas) n[(int)pecas[i].Tipo]++;
                for (int t = 0; t < 4; t++)
                {
                    if (n[t] == 0) continue;
                    b.Lotes[t * 2] = new Lote { M = new Matrix4x4[n[t]] };
                    if (TemLod1[t]) b.Lotes[t * 2 + 1] = new Lote { M = new Matrix4x4[n[t]] };
                }
            }
        }

        /// <summary>4 Hz: cada peca no lote do LOD dela; alem do corte do tipo nao vai.</summary>
        void Reclassificar(Vector3 o)
        {
            int lotes = 0, soma = 0;
            foreach (Bloco b in blocos)
            {
                foreach (Lote l in b.Lotes) if (l != null) l.N = 0;
                foreach (int i in b.Pecas)
                {
                    PecaPlantada p = pecas[i];
                    Vector3 pos = p.M.GetColumn(3);
                    if (!Visivel(p.Tipo, o, pos)) continue;
                    Lote l = b.Lotes[(int)p.Tipo * 2 + (Lod1(p.Tipo, o, pos) ? 1 : 0)];
                    l.M[l.N++] = vis[i];
                }
                for (int k = 0; k < 8; k++)
                {
                    if (b.Lotes[k] == null || b.Lotes[k].N == 0) continue;
                    lotes++;
                    soma += b.Lotes[k].N * tris[k];
                }
            }
            LotesEnviados = lotes;
            TrisEnviados = soma;
        }

        void Update()
        {
            if (blocos.Count == 0) return;
            relogio += Time.deltaTime;
            if (relogio >= Periodo)
            {
                relogio = 0f;
                Camera cam = olho != null ? olho : Camera.main;
                Reclassificar(cam != null ? cam.transform.position : new Vector3(0f, 1e4f, 0f));   // sem camera: tudo longe
            }
            foreach (Bloco b in blocos)
                for (int k = 0; k < 8; k++)
                {
                    Lote l = b.Lotes[k];
                    if (l == null || l.N == 0) continue;
                    var rp = new RenderParams(mats[k])
                    {
                        worldBounds = b.Caixa,
                        shadowCastingMode = k < 4 ? ShadowCastingMode.On : ShadowCastingMode.Off,   // cogumelo e samambaia nao pagam sombra
                        receiveShadows = true,
                        lightProbeUsage = LightProbeUsage.Off,
                        layer = gameObject.layer,
                    };
                    Graphics.RenderMeshInstanced(rp, malha[k], 0, l.M, l.N);
                }
        }
    }
}
