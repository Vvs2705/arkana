using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Core;
using Arkana.Characters;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O DESENHO do GRUPO B (Vitalis, Ilusionista, Vex, Aelion) pelos ganchos parciais da casca: mesmo pool, mesmas
    /// ferramentas (Peca, Linha, ParticulaVfx, MaterialVfx), mesma leitura (EscalaPorRestante, passos de anime de 12 fps).
    /// Tipos com o prefixo do mago. Por mago (EstadoGrupoB), a LEITURA do estado sem EfeitoVisual do kit, como o braco da
    /// Pyra: a Lumen que acompanha a Vitalis, o vidro do Ilusionista invisivel (e o modelo dele apagado), as runas e a tosse
    /// do Vex, o brilho da corda e o po' de estrela do Aelion.
    /// Os REFLEXOS do Ilusionista sao o proprio modelo dele (Mago.Criar) espelhado em X — "a animacao dos reflexos = a dele
    /// com mirror no eixo X" (ficha 08). Os renderers da copia moram em Item.Extra: o Mostrar do pool nao os conhece, entao
    /// BVarrer acende o que foi desenhado NESTE quadro e apaga o que voltou ao pool.
    /// Paleta das fichas: Vitalis branco/dourado/agua; Ilusionista roxo/dourado/prata; Vex verde/latao/vidro; Aelion
    /// crepusculo/dourado/violeta.
    /// </summary>
    public sealed partial class VisualDosKits
    {
        static readonly Color BOuro = new Color32(0xF0, 0xC7, 0x5E, 255);
        static readonly Color BBranco = new Color32(0xF5, 0xF2, 0xE8, 255);
        static readonly Color BAgua = new Color32(0x2A, 0xA7, 0xFF, 255);
        static readonly Color BRoxo = new Color32(0x6B, 0x3F, 0xA0, 255);
        static readonly Color BPrata = new Color32(0xD8, 0xD8, 0xE0, 255);
        static readonly Color BLavanda = new Color(0.8f, 0.74f, 1f, 1f);
        static readonly Color BVerdeVivo = new Color(0.45f, 0.95f, 0.35f, 1f);
        static readonly Color BVidroVex = new Color32(0xB8, 0xD8, 0xC0, 255);
        static readonly Color BVioleta = new Color32(0x8A, 0x5C, 0xF0, 255);
        /// <summary>m — a Lumen e' MINUSCULA (ficha 07: silhueta de menina, nunca vagalume).</summary>
        const float BAlturaLumen = 0.56f, BLarguraLumen = 0.36f;
        /// <summary>m acima da faixa de onde as flechas da Chuva caem.</summary>
        const float BAlturaChuva = 17f;
        const int BPontosFaixa = 16;
        /// <summary>s do brilho de moldura dourada quando o reflexo SURGE (ficha 08: "por 1 frame" — 1 frame nao se ve').</summary>
        const float BMolduraS = 0.2f;

        static Material _bSuave, _bSuaveAd, _bVidro, _bFrasco, _bMenina;
        static Texture2D _bMeninaTex;
        static Mesh _bCirculo;

        readonly Dictionary<Pawn, CorpoB> _bCorpos = new Dictionary<Pawn, CorpoB>();
        readonly List<Item> _bComExtra = new List<Item>();

        /// <summary>A leitura por mago: os dois visuais dele (chave estavel no pool) + o que a casca lembra entre quadros.</summary>
        sealed class CorpoB
        {
            public EfeitoVisual A, B;
            public Vector3 LumenPos;
            public bool LumenTem, Escondido, Aquecido;
            public Renderer[] Rs;
        }

        static void BMateriais()
        {
            if (_bSuave != null) return;
            _bSuave = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
            _bSuaveAd = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Aditivo, true, MaterialVfx.PontoSuave());
            _bVidro = MaterialVfx.Solido(BPrata, MaterialVfx.Mistura.Alfa, true);
            _bFrasco = MaterialVfx.Solido(BVidroVex, MaterialVfx.Mistura.Alfa, true);
            float k = MaterialVfx.BrilhoHdr;
            _bMenina = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, new Color(k, k, k, 1f), MaterialVfx.Mistura.Aditivo, true, BSilhuetaMenina());
        }

        // ================================================================== ganchos

        partial void NovoGrupoB(Item it, Transform raiz, string tipo, ref bool feito)
        {
            BMateriais();
            feito = true;
            switch (tipo)
            {
                case "vitalis_jardim": NovoJardim(it, raiz); break;
                case "vitalis_murcha":
                    it.Ps = ParticulaVfx.Novo(raiz, "Murcha", BOuro, BBranco, 60f, new Vector2(1f, 1.6f), new Vector2(1f, 2.6f), new Vector2(0.08f, 0.18f), true, 0.35f, 160);
                    BDisco(it.Ps, 1f);
                    break;
                case "vitalis_susto":
                    it.Ps = BEstouro(raiz, "Susto", Color.white, BOuro, 26, new Vector2(0.25f, 0.5f), new Vector2(1.5f, 3.5f), new Vector2(0.05f, 0.12f), 0.3f);
                    break;
                case "vitalis_lumen": NovoLumen(it, raiz); break;
                case "ilusionista_espelho": NovoEspelho(it, raiz); break;
                case "ilusionista_devolvido":
                    it.Linha = Linha(it.Go, 0.16f);
                    it.Linha.widthCurve = AnimationCurve.Linear(0f, 0.2f, 1f, 1f);   // fina na cauda, cheia na ponta
                    it.Ps = ParticulaVfx.Novo(raiz, "Rastro", Color.white, BLavanda, 70f, new Vector2(0.2f, 0.45f), new Vector2(0f, 0.4f), new Vector2(0.05f, 0.11f), true, 1f, 60);
                    break;
                case "ilusionista_brilho":
                    it.Ps = BEstouro(raiz, "Acorde", Color.white, BOuro, 22, new Vector2(0.15f, 0.35f), new Vector2(2f, 4.5f), new Vector2(0.08f, 0.2f), 0f);
                    break;
                case "ilusionista_cacos":
                    // CACOS DE VIDRO (ficha 08: "tudo nele estilhaca e se recompoe" — nunca sangue): lascas esticadas na
                    // velocidade, caindo, + o ouro da moldura. Filhas: o Play(true)/Stop(true) do pool liga as duas.
                    it.Ps = BEstouro(raiz, "Cacos", BPrata, BLavanda, 36, new Vector2(0.45f, 0.85f), new Vector2(2.5f, 6f), new Vector2(0.06f, 0.15f), 1.1f);
                    BEsticar(it.Ps, 0.05f, 1.4f);
                    it.Ps2 = BEstouro(it.Ps.transform, "Ouro", Color.white, BOuro, 14, new Vector2(0.3f, 0.6f), new Vector2(1f, 3f), new Vector2(0.05f, 0.1f), 0.2f);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;
                case "ilusionista_reflexo": NovoReflexo(it, raiz, false); break;
                case "ilusionista_caido": NovoReflexo(it, raiz, true); break;
                case "ilusionista_shimmer":
                    // o INVISIVEL: vidro tremendo no lugar do corpo (a mesma silhueta vazada do eco) + lascas de luz soltas
                    it.Linha = Linha(it.Go, 0.8f);
                    it.Linha.sharedMaterial = _mFantasma;
                    it.Linha.numCapVertices = 0;
                    it.Ps = ParticulaVfx.Novo(raiz, "Lascas", Color.white, BLavanda, 26f, new Vector2(0.3f, 0.7f), new Vector2(0.05f, 0.4f), new Vector2(0.04f, 0.1f), true, 1f, 50);
                    BCaixa(it.Ps, new Vector3(0.7f, 0.7f, 1.7f));
                    it.Ps.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                    break;
                case "vex_frasco":
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Capsule), _bFrasco);
                    it.R.transform.localScale = new Vector3(0.14f, 0.12f, 0.14f);
                    it.R2 = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mFeixe);   // o reagente aceso dentro do vidro
                    it.R2.transform.localScale = Vector3.one * 0.09f;
                    it.Ps = ParticulaVfx.Novo(raiz, "Pingos", BVerdeVivo, BVidroVex, 40f, new Vector2(0.25f, 0.5f), new Vector2(0f, 0.3f), new Vector2(0.04f, 0.09f), true, 1f, 40);
                    break;
                case "vex_poca": NovoPoca(it, raiz); break;
                case "vex_nuvem": NovoGas(it, raiz, false); break;
                case "vex_obra": NovoGas(it, raiz, true); break;
                case "vex_fogo":
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Quad), _bSuaveAd);
                    it.R.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    it.Ps = ParticulaVfx.Fogo(raiz, "Chamas", 60f, true);
                    ParticleSystem.MainModule mf = it.Ps.main;
                    mf.maxParticles = 160;
                    BDisco(it.Ps, 1f);
                    break;
                case "vex_contorno":
                    it.Linha = Linha(it.Go, 1f);
                    it.Linha.sharedMaterial = _mFantasma;   // a silhueta vazada: so' o CONTORNO acende
                    it.Linha.numCapVertices = 0;
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    break;
                case "vex_runas":
                    it.R = Peca(raiz, BTransmutacao(), _mFeixe);
                    it.R2 = Peca(raiz, BTransmutacao(), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Glifos", BVerdeVivo, BVidroVex, 30f, new Vector2(0.6f, 1.2f), new Vector2(0.6f, 1.6f), new Vector2(0.05f, 0.12f), true, 0.1f, 90);
                    BDisco(it.Ps, 1.3f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Fole", BVerdeVivo, Color.white, 20f, new Vector2(0.15f, 0.3f), new Vector2(0f, 0.2f), new Vector2(0.18f, 0.36f), true, 1f, 24);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    BCaixa(it.Ps2, Vector3.one * 0.15f);
                    break;
                case "vex_tosse":
                    it.Ps = ParticulaVfx.Novo(raiz, "Tosse", new Color(0.72f, 0.76f, 0.66f, 0.6f), new Color(0.45f, 0.6f, 0.4f, 0.5f), 12f, new Vector2(0.6f, 1.1f), new Vector2(0.4f, 1f), new Vector2(0.25f, 0.5f), true, 0.5f, 30);
                    it.Ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _bSuave;
                    BCaixa(it.Ps, Vector3.one * 0.2f);
                    break;
                case "aelion_flecha":
                    it.Linha = Linha(it.Go, 0.14f);
                    it.Linha.widthCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);
                    it.Linha.colorGradient = BDegrade(BVioleta, BOuro, 0f, 1f);   // a cauda some no ar, a ponta queima
                    it.Ps = ParticulaVfx.Novo(raiz, "Estrelas", Color.white, BOuro, 90f, new Vector2(0.25f, 0.6f), new Vector2(0f, 0.5f), new Vector2(0.05f, 0.12f), true, 1f, 90);
                    break;
                case "aelion_estilhaco":
                    // "impacto em estilhaco de vidro estelar" (ficha 11)
                    it.Ps = BEstouro(raiz, "Estilhaco", Color.white, BVioleta, 30, new Vector2(0.3f, 0.6f), new Vector2(2f, 5.5f), new Vector2(0.06f, 0.15f), 0.6f);
                    BEsticar(it.Ps, 0.05f, 1.2f);
                    it.Ps2 = BEstouro(it.Ps.transform, "Ouro", BOuro, Color.white, 14, new Vector2(0.2f, 0.45f), new Vector2(0.5f, 2f), new Vector2(0.1f, 0.22f), 0f);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;
                case "aelion_marca":
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Linha = Linha(it.Go, 0.07f);
                    break;
                case "aelion_aviso":
                    it.Linha = BFaixaNoChao(it.Go);
                    var enche = new GameObject("Enche");
                    enche.transform.SetParent(raiz, false);
                    it.R = BFaixaNoChao(enche);   // LineRenderer E' Renderer: o Mostrar do pool liga/desliga como qualquer peca
                    break;
                case "aelion_risco":
                    it.Linha = Linha(it.Go, 0.3f);
                    it.Linha.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);
                    it.Linha.colorGradient = BDegrade(BOuro, BVioleta, 0.35f, 0.5f);
                    it.Ps = ParticulaVfx.Novo(raiz, "Ponta", Color.white, BOuro, 70f, new Vector2(0.4f, 0.9f), new Vector2(0f, 0.6f), new Vector2(0.08f, 0.2f), true, 1f, 80);
                    break;
                case "aelion_chuva":
                    // o FEIXE de flechas espectrais: riscos esticados caindo de BAlturaChuva m sobre a faixa + o espirro no chao
                    it.Ps = ParticulaVfx.Novo(raiz, "Flechas", new Color(1f, 0.92f, 0.62f), BVioleta, 200f, new Vector2(0.42f, 0.52f), new Vector2(-40f, -32f), new Vector2(0.07f, 0.12f), true, 0f, 280);
                    it.Ps.transform.localPosition = new Vector3(0f, BAlturaChuva, 0f);
                    BEsticar(it.Ps, 0.07f, 1f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Impacto", Color.white, BOuro, 120f, new Vector2(0.2f, 0.45f), new Vector2(1f, 3.5f), new Vector2(0.05f, 0.12f), true, 0.7f, 160);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;
                case "aelion_carga":
                    it.Ps = ParticulaVfx.Novo(raiz, "Brilho", Color.white, BOuro, 20f, new Vector2(0.12f, 0.25f), new Vector2(0f, 0.1f), new Vector2(0.25f, 0.55f), false, 1f, 24);
                    BCaixa(it.Ps, Vector3.one * 0.08f);
                    // a luz JUNTANDO na corda: nasce na casca de uma esfera e anda para dentro (velocidade negativa)
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Juntando", BVioleta, BOuro, 30f, new Vector2(0.35f, 0.45f), new Vector2(-2.4f, -1.8f), new Vector2(0.04f, 0.09f), false, 0f, 48);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    ParticleSystem.ShapeModule shc = it.Ps2.shape;
                    shc.shapeType = ParticleSystemShapeType.Sphere;
                    shc.radius = 0.9f;
                    shc.radiusThickness = 0f;
                    break;
                case "aelion_po":
                    it.Ps = ParticulaVfx.Novo(raiz, "Po", BOuro, BVioleta, 30f, new Vector2(0.8f, 1.4f), new Vector2(0.1f, 0.4f), new Vector2(0.04f, 0.09f), true, 1f, 50);
                    ParticleSystem.MainModule mp = it.Ps.main;
                    mp.gravityModifier = 0.25f;   // o po' de estrela ESCORRE do arco
                    BCaixa(it.Ps, new Vector3(0.3f, 0.3f, 0.6f));
                    break;
                default: feito = false; break;
            }
        }

        partial void DesenhoGrupoB(Item it, EfeitoVisual v, float e, Pawn dono, ref bool feito)
        {
            feito = true;
            switch (v.Tipo)
            {
                case "vitalis_jardim": VitalisJardim(it, v, e); break;
                case "vitalis_murcha":
                    it.Go.transform.position = NoChao(v.Pos) + Vector3.up * 0.1f;
                    BRaio(it.Ps, Mathf.Max(v.Raio, 0.5f));
                    BTaxa(it.Ps, 18f * v.Raio * e);
                    break;
                case "vitalis_susto": it.Go.transform.position = v.Pos + Vector3.up * 1.5f; break;
                case "vitalis_lumen": VitalisLumenDesenho(it, v); break;
                case "ilusionista_espelho": IluEspelho(it, v, e); break;
                case "ilusionista_devolvido":
                    it.Linha.SetPosition(0, v.Pos2);   // cauda
                    it.Linha.SetPosition(1, v.Pos);    // ponta
                    Color cd = BLavanda;
                    cd.a = e;
                    it.Linha.startColor = cd;
                    it.Linha.endColor = Color.white;
                    it.Ps.transform.position = v.Pos;
                    break;
                case "ilusionista_brilho": it.Go.transform.position = v.Pos; break;
                case "ilusionista_cacos":
                    it.Go.transform.position = v.Pos;
                    ParticleSystem.ShapeModule shk = it.Ps.shape;
                    shk.radius = 0.3f * Mathf.Max(v.Raio, 0.2f);
                    break;
                case "ilusionista_reflexo": IluReflexo(it, v, dono, false); break;
                case "ilusionista_caido": IluReflexo(it, v, dono, true); break;
                case "ilusionista_shimmer":
                    it.Go.transform.position = v.Pos;
                    it.Linha.SetPosition(0, v.Pos);
                    it.Linha.SetPosition(1, v.Pos + Vector3.up * 1.75f);
                    Color cs = BLavanda;
                    cs.a = 0.3f * Flicker[Passo() % Flicker.Length];   // o shimmer: se ve' DE PERTO (o limitador), some de longe
                    it.Linha.startColor = cs;
                    it.Linha.endColor = cs;
                    break;
                case "vex_frasco":
                    int pf = Passo();
                    it.Go.transform.SetPositionAndRotation(v.Pos, Quaternion.Euler(pf * 40f, pf * 25f, 0f));   // gira no ar, em passos
                    Pintar(it.R, BVidroVex, 0.75f);
                    Pintar(it.R2, Hdr(BVerdeVivo, 2f), 1f);
                    break;
                case "vex_poca": VexPoca(it, v, e); break;
                case "vex_nuvem": VexGas(it, v, e, false); break;
                case "vex_obra": VexGas(it, v, e, true); break;
                case "vex_fogo":
                    float rf = Mathf.Max(v.Raio, 0.5f);
                    it.Go.transform.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.08f, Inclinacao(v.Pos));
                    it.R.transform.localScale = new Vector3(rf * 2f, rf * 2f, 1f);
                    Pintar(it.R, Hdr(CorFogo, 1.4f), 0.6f * e * Flicker[Passo() % Flicker.Length]);
                    BRaio(it.Ps, rf * 0.8f);
                    BTaxa(it.Ps, 9f * rf * e);
                    break;
                case "vex_contorno":
                    Vector3 pa = PosDoAlvo(v);
                    it.Go.transform.position = pa;
                    it.Linha.SetPosition(0, pa + Vector3.up * 0.02f);
                    it.Linha.SetPosition(1, pa + Vector3.up * 1.9f);
                    Color cv = BVerdeVivo;
                    cv.a = 0.95f * e * Flicker[Passo() % Flicker.Length];
                    it.Linha.startColor = cv;
                    it.Linha.endColor = cv;
                    it.R.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    it.R.transform.localScale = Vector3.one * 0.6f;
                    Pintar(it.R, Hdr(BVerdeVivo, 1.6f), e);
                    break;
                case "vex_runas": VexRunas(it, v); break;
                case "vex_tosse": it.Go.transform.position = v.Pos; break;
                case "aelion_flecha":
                    float ff = v.Duracao > 0f ? Mathf.Clamp01(v.Restante / v.Duracao) : 0f;   // voando fica cheio; parado, apaga no rastro
                    it.Linha.SetPosition(0, v.Pos);
                    it.Linha.SetPosition(1, v.Pos2);
                    Pintar(it.Linha, Hdr(Color.white, MaterialVfx.BrilhoHdr), ff);
                    it.Ps.transform.position = v.Pos2;
                    BTaxa(it.Ps, 90f * ff);
                    break;
                case "aelion_estilhaco": it.Go.transform.position = v.Pos; break;
                case "aelion_marca": AelionMarca(it, v, e); break;
                case "aelion_aviso": AelionAviso(it, v); break;
                case "aelion_risco":
                    // a flecha SOBE (140 m/s) e o risco fica no ceu ate' a chuva cair: todo o servidor ve'
                    float h = Mathf.Min((v.Duracao - v.Restante) * 140f, 90f);
                    Vector3 ra = v.Pos + Vector3.up * 1.6f, rb = ra + Vector3.up * h;
                    it.Linha.SetPosition(0, ra);
                    it.Linha.SetPosition(1, rb);
                    Pintar(it.Linha, Hdr(Color.white, MaterialVfx.BrilhoHdr), e);
                    it.Ps.transform.position = rb;
                    BTaxa(it.Ps, h < 90f ? 70f : 20f);
                    break;
                case "aelion_chuva": AelionChuva(it, v, e); break;
                case "aelion_carga":
                    it.Go.transform.position = v.Pos;
                    BTaxa(it.Ps, 10f + 50f * v.Raio);
                    BTaxa(it.Ps2, 20f + 60f * v.Raio);
                    break;
                case "aelion_po": it.Go.transform.position = v.Pos; break;
                default: feito = false; break;
            }
        }

        /// <summary>Leitura por mago do Grupo B (1x por quadro por corpo com kit, depois dos visuais dele). A varredura das
        /// copias vem POR ULTIMO: pega tambem o que o aquecimento do pool acabou de criar (senao 1 quadro na origem).</summary>
        partial void EstadoGrupoB(Pawn dono)
        {
            BEstado(dono);
            BVarrer();
        }

        void BEstado(Pawn dono)
        {
            IHabilidade h = dono.Runner.Impl;
            KitRunner k = dono.Runner;
            Ilusionista ilu = h as Ilusionista;
            if (ilu != null) { IluCorpo(dono, ilu, BCorpo(dono, "ilusionista_shimmer", null)); return; }   // apaga/religa ate' morto
            if (!dono.Viva || dono.Queda.Fase == Queda.NO_CASTELO) return;
            Vector3 peito = dono.Pos + Vector3.up * 1.35f + dono.Frente * 0.3f;
            Vitalis vit = h as Vitalis;
            Vex vex = h as Vex;
            Aelion ae = h as Aelion;
            if (vit != null) VitalisLumen(dono, vit, BCorpo(dono, "vitalis_lumen", null));
            else if (vex != null)
            {
                CorpoB c = BCorpo(dono, "vex_runas", "vex_tosse");
                if (k.Telegrafia > 0f)
                {
                    // o fole INFLA e as runas giram nos pes: o aviso cresce ate' a Grande Obra sair
                    c.A.Pos = dono.Pos;
                    c.A.Pos2 = peito;
                    c.A.Raio = 1f - k.Telegrafia / Mathf.Max(k.Dados.Telegrafia, 0.01f);
                    Desenhar(c.A, dono);
                }
                if (k.EstadoAtivo(Vex.OFEGANTE)) { c.B.Pos = peito; Desenhar(c.B, dono); }
            }
            else if (ae != null)
            {
                CorpoB c = BCorpo(dono, "aelion_carga", "aelion_po");
                if (ae.Carregando)
                {
                    c.A.Pos = dono.Pos + Vector3.up * 1.45f + dono.Frente * 0.55f;
                    c.A.Raio = ae.FracCarga(k);
                    Desenhar(c.A, dono);
                }
                if (k.EstadoAtivo(Aelion.ARCO_FRIO)) { c.B.Pos = peito; Desenhar(c.B, dono); }
            }
        }

        CorpoB BCorpo(Pawn dono, string a, string b)
        {
            CorpoB c;
            if (_bCorpos.TryGetValue(dono, out c)) return c;
            c = new CorpoB
            {
                A = new EfeitoVisual(a, Vector3.zero, Vector3.zero, 1f, 1f),
                B = b != null ? new EfeitoVisual(b, Vector3.zero, Vector3.zero, 1f, 1f) : null,
            };
            _bCorpos[dono] = c;
            return c;
        }

        /// <summary>As copias do Ilusionista (Item.Extra) ficam fora do Mostrar do pool: acende a desenhada NESTE quadro, apaga
        /// a que voltou ao pool. ponytail: vale enquanto so' o player tem kit (1 chamada de EstadoGrupoB por quadro, depois
        /// dos visuais dele). Quando o Mostrar do pool ligar/desligar Item.Extra (patch do grupo D, 12/09), esta varredura
        /// SOBRA — apagar BVarrer e _bComExtra.</summary>
        void BVarrer()
        {
            for (int i = 0; i < _bComExtra.Count; i++)
            {
                Item it = _bComExtra[i];
                bool on = it.Quadro == _quadro;
                for (int j = 1; j < it.Extra.Length; j++)
                {
                    Renderer r = it.Extra[j] as Renderer;
                    if (r != null && r.enabled != on) r.enabled = on;
                }
            }
        }

        // ================================================================== VITALIS

        /// <summary>Jardim da Aurora: aquarela dourada no chao (disco macio que respira), borda acesa, petalas subindo e flores
        /// miudas — e o FAROL: coluna de luz de `farol_altura` m, por cima de qualquer parede (o limitador dela).</summary>
        void NovoJardim(Item it, Transform raiz)
        {
            it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Quad), _bSuave);
            it.R.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
            it.R2.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            it.Ps = ParticulaVfx.Novo(raiz, "Petalas", BBranco, BOuro, 40f, new Vector2(1.4f, 2.4f), new Vector2(0.5f, 1.3f), new Vector2(0.07f, 0.16f), true, 0.2f, 140);
            BDisco(it.Ps, 1f);
            it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Flores", new Color(1f, 0.95f, 0.75f), BAgua, 20f, new Vector2(1.2f, 2.2f), new Vector2(0.02f, 0.12f), new Vector2(0.12f, 0.26f), true, 0f, 80);
            it.Ps2.transform.localRotation = Quaternion.identity;
            BDisco(it.Ps2, 1f);
            it.Linha = Linha(it.Go, 1.6f);
            it.Linha.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
            it.Linha.colorGradient = BDegrade(BOuro, BBranco, 0.9f, 0f);
        }

        void VitalisJardim(Item it, EfeitoVisual v, float e)
        {
            Vector3 c = NoChao(v.Pos);
            float r = Mathf.Max(v.Raio, 0.5f);
            int passo = Passo();
            it.Go.transform.SetPositionAndRotation(c + Vector3.up * 0.05f, Inclinacao(v.Pos));
            it.R.transform.localScale = new Vector3(r * 2.3f, r * 2.3f, 1f);
            Pintar(it.R, BOuro, (0.55f + 0.08f * Mathf.Sin(passo * 0.35f)) * e);
            it.R2.transform.localScale = Vector3.one * r;
            Pintar(it.R2, Hdr(BOuro, 1.7f), e * Flicker[passo % Flicker.Length]);
            BRaio(it.Ps, r);
            BRaio(it.Ps2, r);
            BTaxa(it.Ps, 14f * r * e);
            BTaxa(it.Ps2, 7f * r * e);
            float alto;
            if (!Kits.De("07-vitalis").Suprema.TryGetValue("farol_altura", out alto)) alto = 28f;
            it.Linha.SetPosition(0, c);
            it.Linha.SetPosition(1, c + Vector3.up * alto);
            Pintar(it.Linha, Hdr(Color.white, MaterialVfx.BrilhoHdr), 0.85f * e);
        }

        /// <summary>A LUMEN (ficha 07): a irma de luz, silhueta de MENINA com o laco de fita. Cartao virado para a camera +
        /// rastro de brilho de mundo (desenha o circulo no chao durante o aviso do Jardim) + petalas que sobem da Vitalis
        /// quando ela cura.</summary>
        void NovoLumen(Item it, Transform raiz)
        {
            it.Linha = Linha(it.Go, BLarguraLumen);
            it.Linha.sharedMaterial = _bMenina;
            it.Linha.numCapVertices = 0;
            it.Ps = ParticulaVfx.Novo(raiz, "Rastro", Color.white, BOuro, 30f, new Vector2(0.4f, 0.9f), new Vector2(0.05f, 0.35f), new Vector2(0.04f, 0.1f), true, 1f, 90);
            BCaixa(it.Ps, new Vector3(0.18f, 0.18f, 0.3f));
            it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Petalas", BBranco, BOuro, 0f, new Vector2(0.8f, 1.4f), new Vector2(0.5f, 1.2f), new Vector2(0.06f, 0.14f), true, 0.2f, 60);
            it.Ps2.transform.localRotation = Quaternion.identity;
            BCaixa(it.Ps2, new Vector3(0.6f, 0.6f, 1.4f));
        }

        /// <summary>Onde a Lumen VAI (voa ate' la', nunca teleporta): no ombro; orbitando-a ao curar; sobre o caido que reergue;
        /// rodando o circulo do Jardim durante o aviso e flutuando no centro dele; arrastada, baixa e apagada no fim.</summary>
        void VitalisLumen(Pawn dono, Vitalis vit, CorpoB c)
        {
            KitRunner k = dono.Runner;
            float t = Time.time;
            bool apagada = k.EstadoAtivo(Vitalis.LUMEN_APAGADA);
            float rapidez = 6f;
            Vector3 alvo;
            if (k.Telegrafia > 0f)
            {
                float prog = 1f - k.Telegrafia / Mathf.Max(k.Dados.Telegrafia, 0.01f);
                float a = prog * Mathf.PI * 2f, r = k.Dados.Suprema["raio"];
                alvo = NoChao(vit.CentroDoAviso) + new Vector3(Mathf.Cos(a) * r, 0.7f, Mathf.Sin(a) * r);
                rapidez = 14f;
            }
            else if (vit.JardimAtivo != null)
                alvo = NoChao(vit.JardimAtivo.Centro) + new Vector3(Mathf.Cos(t * 0.8f) * 0.6f, 2.6f + Mathf.Sin(t * 1.6f) * 0.15f, Mathf.Sin(t * 0.8f) * 0.6f);
            else if (vit.Reerguendo != null)
                alvo = vit.Reerguendo.Pos + new Vector3(Mathf.Cos(t * 3f) * 0.5f, 1.1f, Mathf.Sin(t * 3f) * 0.5f);
            else if (vit.Curando)
                alvo = dono.Pos + new Vector3(Mathf.Cos(t * 2.6f) * 0.75f, 1.3f + Mathf.Sin(t * 5.2f) * 0.2f, Mathf.Sin(t * 2.6f) * 0.75f);
            else if (apagada) alvo = dono.Pos - dono.Frente * 0.7f + Vector3.up * 0.9f;
            else alvo = dono.Pos + dono.transform.right * 0.55f + Vector3.up * (1.75f + Mathf.Sin(t * 2f) * 0.08f);
            c.LumenPos = c.LumenTem ? Vector3.Lerp(c.LumenPos, alvo, 1f - Mathf.Exp(-Time.deltaTime * rapidez)) : alvo;
            c.LumenTem = true;
            EfeitoVisual v = c.A;
            v.Pos = c.LumenPos;
            v.Pos2 = vit.Curando ? dono.Pos : c.LumenPos;   // curando: as petalas sobem DELA
            v.Raio = apagada ? 0.3f : k.Telegrafia > 0f ? 1.6f : 1f;   // o brilho da irma
            Desenhar(v, dono);
        }

        void VitalisLumenDesenho(Item it, EfeitoVisual v)
        {
            Vector3 p = v.Pos;
            float brilho = v.Raio * Flicker[Passo() % Flicker.Length];
            it.Linha.SetPosition(0, p - Vector3.up * (BAlturaLumen * 0.5f));
            it.Linha.SetPosition(1, p + Vector3.up * (BAlturaLumen * 0.5f));
            Color c = BBranco;
            c.a = Mathf.Min(0.95f * brilho, 1f);
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            it.Ps.transform.position = p;
            BTaxa(it.Ps, 34f * v.Raio);
            bool curando = (v.Pos2 - v.Pos).sqrMagnitude > 0.0001f;
            it.Ps2.transform.position = v.Pos2 + Vector3.up * 0.9f;
            BTaxa(it.Ps2, curando ? 26f : 0f);
        }

        // ============================================================== ILUSIONISTA

        /// <summary>Espelho de corpo inteiro: vidro prata-lavanda com cintilacao, halo roxo atras e a MOLDURA DOURADA.</summary>
        void NovoEspelho(Item it, Transform raiz)
        {
            it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Quad), _bVidro);
            it.R2 = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Quad), _bSuaveAd);
            it.Linha = Linha(it.Go, 0.08f);
            it.Linha.numCapVertices = 0;
            it.Linha.positionCount = 5;
            it.Ps = ParticulaVfx.Novo(raiz, "Brilhos", Color.white, BPrata, 16f, new Vector2(0.25f, 0.6f), new Vector2(0f, 0.15f), new Vector2(0.05f, 0.13f), false, 1f, 30);
        }

        void IluEspelho(Item it, EfeitoVisual v, float e)
        {
            Vector3 a = NoChao(v.Pos), b = NoChao(v.Pos2);
            Vector3 d = v.Pos2 - v.Pos;
            d.y = 0f;
            float len = Mathf.Max(d.magnitude, 0.1f), h = Ilusionista.Espelho.ALTURA;
            Vector3 n = Vector3.Cross(Vector3.up, d);   // o kit: lado = Normal x cima  =>  Normal = cima x (B - A)
            it.Go.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(n.sqrMagnitude > 1e-6f ? n : Vector3.forward, Vector3.up));
            int passo = Passo();
            it.R.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            it.R.transform.localScale = new Vector3(len, h, 1f);
            Pintar(it.R, BLavanda, (0.26f + Cintila[passo % Cintila.Length]) * e);
            it.R2.transform.localPosition = new Vector3(0f, h * 0.5f, -0.03f);
            it.R2.transform.localScale = new Vector3(len * 1.7f, h * 1.35f, 1f);
            Pintar(it.R2, Hdr(BRoxo, 1.6f), 0.5f * e);
            Vector3 a0 = a + Vector3.up * 0.02f, b0 = b + Vector3.up * 0.02f, cima = Vector3.up * h;
            it.Linha.SetPosition(0, a0);
            it.Linha.SetPosition(1, b0);
            it.Linha.SetPosition(2, b0 + cima);
            it.Linha.SetPosition(3, a0 + cima);
            it.Linha.SetPosition(4, a0);
            Color c = BOuro;
            c.a = e;
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.scale = new Vector3(len, 0.05f, h);   // y da caixa = a espessura (o Novo gira -90 em X: z = cima)
            it.Ps.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            BTaxa(it.Ps, 18f * e);
        }

        /// <summary>Um REFLEXO (ou o reflexo CAIDO da passiva): o modelo dele, espelhado em X, tingido de vidro lavanda, sem
        /// sombra (reflexo e' luz) + lascas de brilho. A animacao so' e' amostrada com o renderer ligado.</summary>
        void NovoReflexo(Item it, Transform raiz, bool caido)
        {
            Mago m = Mago.Criar(raiz, "08-ilusionista");
            m.transform.localScale = new Vector3(-1f, 1f, 1f);
            m.SetTint(BLavanda);
            Animation anim = m.GetComponentInChildren<Animation>();
            if (anim != null) anim.cullingType = AnimationCullingType.BasedOnRenderers;
            Renderer[] rs = m.GetComponentsInChildren<Renderer>(true);
            it.Extra = new Component[rs.Length + 1];
            it.Extra[0] = m;
            for (int i = 0; i < rs.Length; i++)
            {
                rs[i].shadowCastingMode = ShadowCastingMode.Off;
                it.Extra[i + 1] = rs[i];
            }
            _bComExtra.Add(it);
            it.Ps = ParticulaVfx.Novo(raiz, "Brilho", Color.white, BLavanda, 12f, new Vector2(0.4f, 0.9f), new Vector2(0.05f, 0.3f), new Vector2(0.04f, 0.1f), true, 1f, 30);
            BCaixa(it.Ps, new Vector3(0.7f, 0.7f, caido ? 0.5f : 1.8f));
            it.Ps.transform.localPosition = new Vector3(0f, caido ? 0.3f : 0.9f, 0f);
            if (caido) return;
            it.Linha = Linha(it.Go, 0.06f);
            it.Linha.numCapVertices = 0;
            it.Linha.positionCount = 5;
        }

        void IluReflexo(Item it, EfeitoVisual v, Pawn dono, bool caido)
        {
            Vector3 p = NoChao(v.Pos);
            Vector3 n = v.Pos2 - v.Pos;
            n.y = 0f;
            n = n.sqrMagnitude > 1e-4f ? n.normalized : Vector3.forward;
            Vector3 f = n;
            if (!caido && dono != null)
            {
                // a frente DELE vista no espelho posto entre os dois: a componente na direcao do reflexo inverte
                f = dono.Frente;
                f.y = 0f;
                f -= 2f * Vector3.Dot(f, n) * n;
                if (f.sqrMagnitude < 1e-4f) f = n;
            }
            it.Go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(f.normalized, Vector3.up));
            Mago m = it.Extra != null ? it.Extra[0] as Mago : null;
            if (m != null)
            {
                if (caido) { if (m.ClipeAtual != Clipe.Derrubado) m.Play(Clipe.Derrubado); }
                else if (dono != null)
                {
                    Clipe? c = PoseMago.Alias(dono.Clipe);
                    if (c.HasValue && c.Value != Clipe.Idle && c.Value != Clipe.Run && c.Value != m.ClipeAtual) m.Play(c.Value);
                    m.SetVelocidade(dono.VelocidadeHorizontal);   // corre quando ele corre: o mesmo passo, trocado
                }
            }
            if (it.Linha == null) return;
            // "brilho de moldura dourada quando surgem" (ficha 08): a moldura do espelho em volta do reflexo recem-nascido
            float a = Mathf.Clamp01(1f - (v.Duracao - v.Restante) / BMolduraS);
            Vector3 lado = Vector3.Cross(n, Vector3.up) * 0.55f, cima = Vector3.up * 2.05f, q = p + Vector3.up * 0.02f;
            it.Linha.SetPosition(0, q - lado);
            it.Linha.SetPosition(1, q + lado);
            it.Linha.SetPosition(2, q + lado + cima);
            it.Linha.SetPosition(3, q - lado + cima);
            it.Linha.SetPosition(4, q - lado);
            Color cor = BOuro;
            cor.a = a;
            it.Linha.startColor = cor;
            it.Linha.endColor = cor;
        }

        /// <summary>Invisivel: o modelo dele APAGA (na borda; a busca de renderers aloca 1x por sumico, pegando a luva nova) e o
        /// vidro tremendo fica no lugar. Morto religa (a alma e o corpo que afunda tem de aparecer). Na 1a vez que ve' o
        /// Ilusionista, AQUECE o pool dos reflexos: os modelos nascem no spawn, nao no quadro da suprema.</summary>
        void IluCorpo(Pawn dono, Ilusionista ilu, CorpoB c)
        {
            if (!c.Aquecido)
            {
                c.Aquecido = true;
                int n = (int)Kits.De("08-ilusionista").Suprema["reflexos"];
                var quentes = new Item[n];
                for (int i = 0; i < n; i++) quentes[i] = Pegar("ilusionista_reflexo");
                for (int i = 0; i < n; i++) Devolver(quentes[i]);
            }
            bool some = dono.Viva && ilu.Invisivel;
            if (some != c.Escondido)
            {
                c.Escondido = some;
                if (some) c.Rs = dono.Visual != null ? dono.Visual.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
                if (c.Rs != null) for (int i = 0; i < c.Rs.Length; i++) if (c.Rs[i] != null) c.Rs[i].enabled = !some;
            }
            if (!some) return;
            c.A.Pos = dono.Pos;
            Desenhar(c.A, dono);
        }

        // ====================================================================== VEX

        void NovoPoca(Item it, Transform raiz)
        {
            it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Quad), _bSuave);
            it.R.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            it.R2 = Peca(raiz, BTransmutacao(), _mFeixe);
            it.R2.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            it.Ps = ParticulaVfx.Novo(raiz, "Bolhas", BVerdeVivo, BVidroVex, 0f, new Vector2(0.5f, 1f), new Vector2(0.2f, 0.6f), new Vector2(0.05f, 0.12f), true, 0.2f, 24);
            BDisco(it.Ps, 0.6f);
            it.Ps2 = BEstouro(it.Ps.transform, "Vidro", Color.white, BVidroVex, 18, new Vector2(0.3f, 0.6f), new Vector2(1.5f, 3.5f), new Vector2(0.04f, 0.1f), 1f);
            it.Ps2.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Poca: o frasco quebrou em CIRCULO DE TRANSMUTACAO. Inerte e' apagado; ARMADO gira e pulsa aceso com bolhas —
        /// quem olha le' o perigo antes de pisar.</summary>
        void VexPoca(Item it, EfeitoVisual v, float e)
        {
            float r = Mathf.Max(v.Raio, 0.3f), arma;
            if (!Kits.De("09-vex").Tatica.TryGetValue("arma", out arma)) arma = 1f;
            bool armado = v.Duracao - v.Restante >= arma;
            int passo = Passo();
            it.Go.transform.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.04f, Inclinacao(v.Pos));
            it.R.transform.localScale = new Vector3(r * 2.2f, r * 2.2f, 1f);
            Pintar(it.R, new Color(0.2f, 0.45f, 0.15f, 1f), 0.75f * e);
            it.R2.transform.localScale = Vector3.one * r;
            it.R2.transform.localRotation = Quaternion.Euler(0f, passo * (armado ? 6f : 1f), 0f);
            Pintar(it.R2, Hdr(BVerdeVivo, armado ? 1.8f : 0.5f), e * (armado ? Flicker[passo % Flicker.Length] : 0.55f));
            BTaxa(it.Ps, armado ? 7f * e : 0f);
        }

        /// <summary>A nevoa (nuvem do frasco e a Grande Obra): baforadas em BANDAS CEL, disco verde no chao, borda acesa (na
        /// Grande Obra, o circulo de transmutacao inteiro) e motas de miasma subindo.</summary>
        void NovoGas(Item it, Transform raiz, bool obra)
        {
            it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Quad), _bSuave);
            it.R.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            it.R2 = Peca(raiz, obra ? BTransmutacao() : MalhaVfx.Anel(), _mFeixe);
            it.R2.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            // KNOB por foto: teto de baforadas (sobreposicao alfa custa no aparelho — 110 na Grande Obra)
            it.Ps = ParticulaVfx.Novo(raiz, "Gas", Color.white, Color.white, 10f, new Vector2(2f, 3.2f), new Vector2(0.15f, 0.5f),
                obra ? new Vector2(3f, 5.5f) : new Vector2(1.4f, 2.6f), true, 0.35f, obra ? 110 : 60);
            BGas(it.Ps);
            BDisco(it.Ps, 1f);
            it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Miasma", BVerdeVivo, BVidroVex, 10f, new Vector2(0.8f, 1.6f), new Vector2(0.3f, 0.9f), new Vector2(0.05f, 0.12f), true, 0.5f, 60);
            it.Ps2.transform.localRotation = Quaternion.identity;
            BDisco(it.Ps2, 1f);
        }

        void VexGas(Item it, EfeitoVisual v, float e, bool obra)
        {
            float r = Mathf.Max(v.Raio, 0.5f);
            int passo = Passo();
            it.Go.transform.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.05f, Inclinacao(v.Pos));
            it.R.transform.localScale = new Vector3(r * 2.2f, r * 2.2f, 1f);
            Pintar(it.R, new Color(0.3f, 0.55f, 0.2f, 1f), 0.5f * e);
            it.R2.transform.localScale = Vector3.one * r;
            it.R2.transform.localRotation = Quaternion.Euler(0f, passo * (obra ? 1f : 3f), 0f);
            Pintar(it.R2, Hdr(BVerdeVivo, 1.5f), 0.7f * e * Flicker[passo % Flicker.Length]);
            BRaio(it.Ps, r * 0.85f);
            BRaio(it.Ps2, r * 0.9f);
            BTaxa(it.Ps, (obra ? 4.5f * r : 16f) * e);
            BTaxa(it.Ps2, 3f * r * e);
        }

        /// <summary>O aviso da Grande Obra no corpo: o circulo de transmutacao gira nos pes (dois, em sentidos opostos) e o FOLE
        /// do peito brilha e acelera — tudo cresce com o aviso (Raio = 0..1).</summary>
        void VexRunas(Item it, EfeitoVisual v)
        {
            float prog = Mathf.Clamp01(v.Raio);
            int passo = Passo();
            it.Go.transform.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.06f, Inclinacao(v.Pos));
            it.R.transform.localScale = Vector3.one * 1.5f;
            it.R.transform.localRotation = Quaternion.Euler(0f, passo * 8f, 0f);
            it.R2.transform.localScale = Vector3.one * 0.95f;
            it.R2.transform.localRotation = Quaternion.Euler(0f, -passo * 13f, 0f);
            float k = 0.45f + 0.55f * prog;
            Pintar(it.R, Hdr(BVerdeVivo, 1.8f), k * Flicker[passo % Flicker.Length]);
            Pintar(it.R2, Hdr(BVerdeVivo, 1.8f), k);
            BTaxa(it.Ps, 20f + 70f * prog);
            it.Ps2.transform.position = v.Pos2;
            BTaxa(it.Ps2, 12f + 50f * prog);
        }

        /// <summary>Nevoa em BANDAS CEL (ficha 09: "nunca realista"): cada baforada sorteia UMA de tres cores chapadas; alfa
        /// macio, nasce pequena e abre, entra e sai devagar.</summary>
        static void BGas(ParticleSystem ps)
        {
            var bandas = new Gradient { mode = GradientMode.Fixed };
            bandas.SetKeys(new[]
                {
                    new GradientColorKey(new Color(0.2f, 0.36f, 0.14f), 0.34f), new GradientColorKey(new Color(0.34f, 0.56f, 0.22f), 0.67f),
                    new GradientColorKey(new Color(0.56f, 0.78f, 0.38f), 1f),
                },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0.5f, 1f) });
            ParticleSystem.MainModule m = ps.main;
            m.startColor = new ParticleSystem.MinMaxGradient(bandas) { mode = ParticleSystemGradientMode.RandomColor };
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.3f));
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.color = new ParticleSystem.MinMaxGradient(fade);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _bSuave;
        }

        /// <summary>CIRCULO DE TRANSMUTACAO (ficha 09): anel duplo + hexagrama de faixas finas, raio 1 no plano XZ. Gerado 1x.</summary>
        static Mesh BTransmutacao()
        {
            if (_bCirculo != null) return _bCirculo;
            var b = new MalhaProc.Construtor();
            BAnelEm(b, 1f, 0.06f);
            BAnelEm(b, 0.82f, 0.04f);
            for (int i = 0; i < 6; i++)   // os 6 lados dos dois triangulos (0-2-4 e 1-3-5)
            {
                float a0 = Mathf.PI * 2f * i / 6f, a1 = Mathf.PI * 2f * (i + 2) / 6f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * 0.82f, p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * 0.82f;
                Vector3 lado = Vector3.Cross(p1 - p0, Vector3.up).normalized * 0.03f;
                b.Tri(p0 - lado, p0 + lado, p1 + lado, Color.white, Vector3.up);
                b.Tri(p0 - lado, p1 + lado, p1 - lado, Color.white, Vector3.up);
            }
            return _bCirculo = b.ParaMesh("VfxTransmutacao");
        }

        static void BAnelEm(MalhaProc.Construtor b, float r, float esp)
        {
            const int lados = 48;
            float r0 = r - esp;
            for (int i = 0; i < lados; i++)
            {
                float a0 = Mathf.PI * 2f * i / lados, a1 = Mathf.PI * 2f * (i + 1) / lados;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                b.Tri(d0 * r0, d0 * r, d1 * r, Color.white, Vector3.up);
                b.Tri(d0 * r0, d1 * r, d1 * r0, Color.white, Vector3.up);
            }
        }

        // =================================================================== AELION

        /// <summary>Mira de SNIPER sobre a cabeca do marcado: dois aneis cruzados girando em passos + o fio violeta ao ceu.</summary>
        void AelionMarca(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = PosDoAlvo(v) + Vector3.up * 2.3f;
            int passo = Passo();
            it.Go.transform.position = p;
            it.R.transform.localRotation = Quaternion.Euler(0f, passo * 15f, 0f);
            it.R.transform.localScale = Vector3.one * 0.42f;
            it.R2.transform.localRotation = Quaternion.Euler(90f, passo * 15f, 0f);
            it.R2.transform.localScale = Vector3.one * 0.3f;
            Pintar(it.R, Hdr(BOuro, 2f), e * Flicker[passo % Flicker.Length]);
            Pintar(it.R2, Hdr(BVioleta, 2.2f), e);
            it.Linha.SetPosition(0, p + Vector3.up * 0.3f);
            it.Linha.SetPosition(1, p + Vector3.up * 6f);
            Color c = BVioleta;
            c.a = 0.8f * e;
            it.Linha.startColor = c;
            c.a = 0f;
            it.Linha.endColor = c;
        }

        /// <summary>A FAIXA da Chuva no chao — a mesma lingua do anel do aviso (vermelho que pulsa + miolo que ENCHE ate' a chuva
        /// cair), deitada no relevo ponto a ponto (TransformZ = cima).</summary>
        void AelionAviso(Item it, EfeitoVisual v)
        {
            it.Go.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            float prog = v.Duracao > 0f ? Mathf.Clamp01(1f - v.Restante / v.Duracao) : 1f;
            var enche = (LineRenderer)it.R;
            it.Linha.widthMultiplier = v.Raio * 2f;
            enche.widthMultiplier = v.Raio * 1.4f;
            for (int i = 0; i < BPontosFaixa; i++)
            {
                float t = i / (float)(BPontosFaixa - 1);
                it.Linha.SetPosition(i, NoChao(Vector3.Lerp(v.Pos, v.Pos2, t)) + Vector3.up * 0.12f);
                enche.SetPosition(i, NoChao(Vector3.Lerp(v.Pos, v.Pos2, t * prog)) + Vector3.up * 0.14f);
            }
            Pintar(it.Linha, CorAviso, (Passo() & 1) == 0 ? 0.55f : 0.35f);
            Pintar(enche, CorAviso, 0.45f);
        }

        void AelionChuva(Item it, EfeitoVisual v, float e)
        {
            Vector3 a = NoChao(v.Pos), b = NoChao(v.Pos2);
            Vector3 d = b - a;
            d.y = 0f;
            float comp = Mathf.Max(d.magnitude, 1f);
            Vector3 c = (a + b) * 0.5f;
            it.Go.transform.SetPositionAndRotation(c, Quaternion.LookRotation(d.sqrMagnitude > 1e-4f ? d : Vector3.forward, Vector3.up));
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.scale = new Vector3(v.Raio * 2f, comp, 0.2f);   // y da caixa = a faixa (o Novo gira -90 em X)
            ParticleSystem.ShapeModule sh2 = it.Ps2.shape;
            sh2.scale = new Vector3(v.Raio * 2f, comp, 0.05f);
            it.Ps2.transform.position = c + Vector3.up * 0.1f;
            BTaxa(it.Ps, 9f * comp * e);
            BTaxa(it.Ps2, 4f * comp * e);
        }

        static LineRenderer BFaixaNoChao(GameObject go)
        {
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.alignment = LineAlignment.TransformZ;
            l.sharedMaterial = _mAviso;
            l.numCapVertices = 0;
            l.positionCount = BPontosFaixa;
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            return l;
        }

        // ============================================================== ferramentas

        /// <summary>Estouro de UMA vez: Burst no t=0 de um sistema em laco (5 s padrao) — o pool da' Play ao mostrar e o visual
        /// vive menos que o laco, entao estoura uma vez por uso.</summary>
        static ParticleSystem BEstouro(Transform pai, string nome, Color a, Color b, int n, Vector2 vida, Vector2 vel, Vector2 tam, float gravidade)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, nome, a, b, 0f, vida, vel, tam, true, 1f, n + 8);
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)n) });
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = gravidade;
            return ps;
        }

        /// <summary>Area de disco virada para cima (cone quase reto): nasce no chao do circulo e sobe.</summary>
        static void BDisco(ParticleSystem ps, float r)
        {
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 6f;
            sh.radius = r;
            sh.radiusThickness = 1f;
        }

        static void BCaixa(ParticleSystem ps, Vector3 escala)
        {
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = escala;
        }

        static void BRaio(ParticleSystem ps, float r)
        {
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.radius = r;
        }

        static void BTaxa(ParticleSystem ps, float taxa)
        {
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTimeMultiplier = taxa;
        }

        /// <summary>Particula esticada na velocidade (lasca, risco de flecha).</summary>
        static void BEsticar(ParticleSystem ps, float escalaVel, float comprimento)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = escalaVel;
            r.lengthScale = comprimento;
        }

        /// <summary>Degrade fixo de linha (criado 1x no Novo, nunca por quadro): cor a->b, alfa na cauda e no meio.</summary>
        static Gradient BDegrade(Color a, Color b, float alfaInicio, float alfaFim)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(alfaInicio, 0f), new GradientAlphaKey(Mathf.Max(alfaInicio, alfaFim) * 0.8f + 0.2f, 0.6f), new GradientAlphaKey(alfaFim, 1f) });
            return g;
        }

        /// <summary>A Lumen (ficha 07: silhueta de MENINA, "nao de inseto", com o laco de fita): vestido abrindo na barra, bracos
        /// junto ao corpo, cabeca redonda e o LACO no alto. Cheia (e' luz, nao contorno), borda macia. u = da barra ao laco.</summary>
        static Texture2D BSilhuetaMenina()
        {
            if (_bMeninaTex != null) return _bMeninaTex;
            const int nu = 64, nv = 32;
            var t = new Texture2D(nu, nv, TextureFormat.RGBA32, false) { name = "SilhuetaLumen", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[nu * nv];
            for (int x = 0; x < nu; x++)
            {
                float h = (x + 0.5f) / nu, w = Mathf.Max(BMeiaMenina(h), 1e-3f);
                for (int y = 0; y < nv; y++)
                {
                    float d = Mathf.Abs((y + 0.5f) / nv * 2f - 1f) / w;
                    float a = Mathf.Clamp01((1f - d) * 3f);
                    px[y * nu + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return _bMeninaTex = t;
        }

        /// <summary>Meia largura da menina (1 = borda do cartao) na altura h (0 = barra do vestido, 1 = topo do laco).</summary>
        static float BMeiaMenina(float h)
        {
            if (h < 0.42f) return Mathf.Lerp(0.62f, 0.3f, h / 0.42f);          // vestido: abre na barra
            if (h < 0.62f) return Mathf.Lerp(0.3f, 0.36f, (h - 0.42f) / 0.2f); // tronco e bracos
            if (h < 0.66f) return 0.14f;                                          // pescoco
            if (h < 0.88f)                                                         // cabeca redonda
            {
                float c = (h - 0.77f) / 0.11f;
                return 0.3f * Mathf.Sqrt(Mathf.Max(0f, 1f - c * c)) + 0.12f;
            }
            return 0.42f * Mathf.Sin(Mathf.Clamp01((h - 0.88f) / 0.12f) * Mathf.PI);   // o LACO: mais largo que a cabeca
        }
    }
}
