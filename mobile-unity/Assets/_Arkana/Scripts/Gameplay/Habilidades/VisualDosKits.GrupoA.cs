using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O DESENHO DO GRUPO A (02 Ceifadora, 04 Corvus, 05 Corvomante, 06 Olho-de-Eter) pelos ganchos parciais do
    /// VisualDosKits: mesmo pool, mesmas leis (zero Instantiate por quadro, Mostrar/Recolher ligam e desligam, passos de
    /// anime, cor HDR que acende no bloom, zero luz dinamica). Paleta de cada ficha:
    ///   Ceifadora  preto-vazio #1A1A22 · violeta #8A5CF0 · fio dourado #F0C75E (tinta preta + borda dourada)
    ///   Corvus     vento #8FE8C9 (as fitas e o uivo) · vermelho-caca #C2452D (so' a fera)
    ///   Corvomante verde arcano #2E8B57 (o corvo de energia com UM olho claro)
    ///   Olho       magenta #C24B8E · dourado #F0C75E (mariposas de asa geometrica, po de luz)
    /// Tudo o que e' LEITURA de estado (a nevoa da telegrafia da Ceifadora, a fera do Corvus, o corvo no ombro, as
    /// mariposas em volta do Olho) sai do EstadoGrupoA com UM EfeitoVisual fixo por corpo — o molde do braco da Pyra.
    /// </summary>
    public sealed partial class VisualDosKits
    {
        static readonly Color GaVazio = new Color32(0x1A, 0x1A, 0x22, 255);
        static readonly Color GaVioleta = new Color32(0x8A, 0x5C, 0xF0, 255);
        static readonly Color GaDourado = new Color32(0xF0, 0xC7, 0x5E, 255);
        static readonly Color GaVento = new Color32(0x8F, 0xE8, 0xC9, 255);
        static readonly Color GaCaca = new Color32(0xC2, 0x45, 0x2D, 255);
        static readonly Color GaVerde = new Color32(0x2E, 0x8B, 0x57, 255);
        static readonly Color GaMagenta = new Color32(0xC2, 0x4B, 0x8E, 255);
        static readonly Color GaVidro = new Color(0.85f, 0.82f, 1f, 1f);
        /// <summary>O bater de asa do corvo em 4 passos de anime (altura da ponta da asa, m).</summary>
        static readonly float[] GaAsa = { 0.45f, 0.1f, -0.3f, 0.1f };
        const int GaPontosRasgo = 16, GaPontosRacha = 10, GaPontosFita = 64;

        /// <summary>Aditivo branco (a cor vem do Pintar, por objeto) e a tinta ESCURA em alfa (aditivo nao pinta preto).</summary>
        static Material _gaLuz, _gaTinta, _gaMariposa;
        static Mesh _gaMao, _gaRasgo, _gaRasgoBorda, _gaGarras;
        /// <summary>UM efeito fixo por corpo do grupo (o tipo depende do kit dele): chave estavel no pool, Restante 1 fixo.</summary>
        readonly Dictionary<Pawn, EfeitoVisual> _gaFixos = new Dictionary<Pawn, EfeitoVisual>();

        static void GaMateriais()
        {
            if (_gaLuz != null) return;
            _gaLuz = MaterialVfx.Solido(Color.white, MaterialVfx.Mistura.Aditivo, true);
            _gaTinta = MaterialVfx.Solido(GaVazio, MaterialVfx.Mistura.Alfa, true);
            float k = MaterialVfx.BrilhoHdr;
            _gaMariposa = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, new Color(k, k, k, 1f), MaterialVfx.Mistura.Aditivo, true, GaTexMariposa());
        }

        // =============================================================================== NOVO (1x por item do pool)

        partial void NovoGrupoA(Item it, Transform raiz, string tipo, ref bool feito)
        {
            GaMateriais();
            feito = true;
            switch (tipo)
            {
                // ------------------------------------------------------------ Ceifadora
                case "ceifadora_marca":
                    // a SOMBRA da mao no chao (tinta) + a borda violeta que aperta + a RACHADURA que corre ate' la'
                    it.R = Peca(raiz, MalhaVfx.Disco(), _gaTinta);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _gaLuz);
                    it.Linha = Linha(it.Go, 0.14f);
                    it.Pontos = new Vector3[GaPontosRacha];
                    it.Ps = ParticulaVfx.Novo(raiz, "Fumaca", GaVioleta, GaDourado, 30f,
                        new Vector2(0.4f, 0.8f), new Vector2(0.6f, 1.6f), new Vector2(0.08f, 0.2f), true, 0.3f, 40);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.4f, 0f);   // a caixa e' centrada: meio metro fora do chao
                    break;
                case "ceifadora_mao":
                    // cinco DEDOS de tinta (preto no pe', violeta na ponta) curvados para dentro: a jaula que agarra. A borda
                    // dourada no chao e' a emenda kintsugi. Faisca dourada = filha da fumaca (o pool liga as duas).
                    it.R = Peca(raiz, GaMalhaMao(), _mNucleo);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _gaLuz);
                    it.Ps = ParticulaVfx.Novo(raiz, "Vazio", GaVioleta, GaVazio, 30f,
                        new Vector2(0.5f, 1f), new Vector2(0.4f, 1.2f), new Vector2(0.15f, 0.4f), true, 0.2f, 40);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Emenda", new Color(1f, 0.95f, 0.7f), GaDourado, 20f,
                        new Vector2(0.4f, 0.9f), new Vector2(1f, 2.6f), new Vector2(0.04f, 0.09f), true, 0.6f, 30);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    it.Ps.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                    break;
                case "ceifadora_rasgo":
                    // o CORTE vertical de tinta preta com borda dourada (ficha) na ENTRADA + a costura no chao ate' a saida
                    it.R = Peca(raiz, GaMalhaRasgo(), _gaTinta);
                    it.R2 = Peca(raiz, GaMalhaRasgoBorda(), _gaLuz);
                    it.Linha = Linha(it.Go, 0.35f);
                    it.Pontos = new Vector3[GaPontosRasgo];
                    it.Ps = ParticulaVfx.Novo(raiz, "Tinta", GaVioleta, new Color(0.3f, 0.2f, 0.5f), 40f,
                        new Vector2(0.6f, 1.2f), new Vector2(0.3f, 1f), new Vector2(0.2f, 0.5f), true, 0.5f, 60);
                    ParticleSystem.ShapeModule shr = it.Ps.shape;
                    shr.scale = new Vector3(0.8f, 0.3f, 4f);
                    it.Ps.transform.localPosition = new Vector3(0f, 2.2f, 0f);   // a caixa de 4 m em pe' no meio do corte
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Ouro", new Color(1f, 0.95f, 0.7f), GaDourado, 30f,
                        new Vector2(0.4f, 1f), new Vector2(0.5f, 2f), new Vector2(0.04f, 0.1f), true, 1f, 40);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    ParticleSystem.ShapeModule sho = it.Ps2.shape;
                    sho.scale = shr.scale;
                    break;
                case "ceifadora_eco":
                    // silhueta de VIDRO FOSCO (ficha): o cartao do vulto (so' o contorno aceso) + neblina fria miuda
                    it.Linha = Linha(it.Go, 0.8f);
                    it.Linha.sharedMaterial = _mFantasma;
                    it.Linha.numCapVertices = 0;
                    it.Ps = ParticulaVfx.Novo(raiz, "Geada", new Color(0.9f, 0.9f, 1f, 0.3f), new Color(0.6f, 0.5f, 1f, 0.2f), 8f,
                        new Vector2(0.8f, 1.4f), new Vector2(0.1f, 0.4f), new Vector2(0.3f, 0.7f), true, 0.3f, 20);
                    ParticleSystem.ShapeModule she = it.Ps.shape;
                    she.scale = new Vector3(0.5f, 0.5f, 1.4f);
                    break;
                case "ceifadora_coro":
                    // a TELEGRAFIA dela: o chao perde a cor em volta (mancha de tinta que cresce) + motas violeta subindo
                    it.R = Peca(raiz, MalhaVfx.Disco(), _gaTinta);
                    it.Ps = ParticulaVfx.Novo(raiz, "Coro", GaVioleta, new Color(0.45f, 0.3f, 0.9f), 60f,
                        new Vector2(0.8f, 1.5f), new Vector2(1f, 2.5f), new Vector2(0.06f, 0.16f), true, 0.1f, 90);
                    ParticleSystem.ShapeModule shc = it.Ps.shape;
                    shc.shapeType = ParticleSystemShapeType.Circle;
                    shc.radius = 1.4f;
                    shc.radiusThickness = 0f;
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Fio", new Color(1f, 0.95f, 0.7f), GaDourado, 14f,
                        new Vector2(0.6f, 1.2f), new Vector2(1.5f, 3f), new Vector2(0.03f, 0.07f), true, 0.2f, 24);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;

                // ------------------------------------------------------------ Corvus
                case "corvus_trilha":
                    // FITA de cheiro rente ao chao (a cor e' do elemento de quem passou; vermelha na fera)
                    it.Linha = Linha(it.Go, 0.22f);
                    it.Linha.numCapVertices = 0;
                    it.Pontos = new Vector3[GaPontosFita];
                    break;
                case "corvus_uivo":
                    // o uivo e' um ANEL de onda (a forma do som, para quem nao ouve) + o eco dele atrasado
                    it.R = Peca(raiz, MalhaVfx.Anel(), _gaLuz);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _gaLuz);
                    break;
                case "corvus_cheiro":
                    // CONTORNO aceso: o cartao de silhueta sobre o corpo (o corpo tampa o miolo, sobra a borda) + o cheiro subindo
                    it.Linha = Linha(it.Go, 1.05f);
                    it.Linha.sharedMaterial = _mFantasma;
                    it.Linha.numCapVertices = 0;
                    it.Ps = ParticulaVfx.Novo(raiz, "Cheiro", GaVento, new Color(0.7f, 1f, 0.9f), 24f,
                        new Vector2(0.6f, 1.2f), new Vector2(0.3f, 1f), new Vector2(0.1f, 0.25f), true, 0.3f, 40);
                    ParticleSystem.ShapeModule shch = it.Ps.shape;
                    shch.scale = new Vector3(0.5f, 0.5f, 1.6f);
                    break;
                case "corvus_garra":
                    // tres RISCOS de garra virados para a camera + faisca vermelha
                    it.R = Peca(raiz, GaMalhaGarras(), _gaLuz);
                    it.Ps = ParticulaVfx.Novo(raiz, "Rasgo", new Color(1f, 0.6f, 0.45f), GaCaca, 90f,
                        new Vector2(0.15f, 0.35f), new Vector2(2f, 5f), new Vector2(0.04f, 0.1f), true, 1f, 30);
                    break;
                case "corvus_penas":
                    // a transformacao ESTOURA em penas e nevoa (sem gore: o corpo se remonta) — penas caem, a nevoa sobe
                    it.Ps = ParticulaVfx.Novo(raiz, "Penas", new Color(0.55f, 0.42f, 0.3f), new Color(0.2f, 0.16f, 0.12f), 0f,
                        new Vector2(0.8f, 1.6f), new Vector2(2.5f, 6f), new Vector2(0.12f, 0.3f), true, 1f, 90);
                    ParticleSystem.MainModule mpe = it.Ps.main;
                    mpe.gravityModifier = 0.35f;
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Nevoa", GaVento, new Color(0.9f, 1f, 0.95f), 0f,
                        new Vector2(0.8f, 1.5f), new Vector2(0.5f, 1.5f), new Vector2(0.6f, 1.4f), true, 1f, 40);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;
                case "corvus_fera":
                    // A FERA: nevoa vermelha-caca em volta do corpo (fica no rastro: mundo) + brasa miuda
                    it.Ps = ParticulaVfx.Novo(raiz, "Fera", GaCaca, new Color(0.45f, 0.1f, 0.08f), 40f,
                        new Vector2(0.5f, 1f), new Vector2(0.2f, 0.8f), new Vector2(0.3f, 0.7f), true, 0.4f, 60);
                    ParticleSystem.ShapeModule shf = it.Ps.shape;
                    shf.scale = new Vector3(0.9f, 0.9f, 1.8f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Brasa", new Color(1f, 0.55f, 0.35f), GaCaca, 18f,
                        new Vector2(0.4f, 0.9f), new Vector2(0.8f, 2f), new Vector2(0.04f, 0.08f), true, 0.8f, 30);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    ParticleSystem.ShapeModule shf2 = it.Ps2.shape;
                    shf2.scale = shf.scale;
                    break;

                // ------------------------------------------------------------ Corvomante
                case "corvomante_corvo":
                    // o CORVO de energia verde: silhueta em V que bate asa em passos + UM olho claro + rastro de luz
                    it.Linha = Linha(it.Go, 0.2f);
                    it.Linha.widthCurve = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.25f));
                    it.Linha.positionCount = 5;
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _gaLuz);
                    it.Ps = ParticulaVfx.Novo(raiz, "Rastro", new Color(0.6f, 1f, 0.75f), GaVerde, 30f,
                        new Vector2(0.4f, 0.8f), new Vector2(0f, 0.3f), new Vector2(0.08f, 0.2f), true, 1f, 40);
                    break;
                case "corvomante_marca":
                    // MARCA do corvo: coluna verde fina (le'-se de longe) + o losango-olho girando sobre a cabeca
                    it.Linha = Linha(it.Go, 0.3f);
                    it.Linha.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);
                    var gv = new Gradient();
                    gv.SetKeys(new[] { new GradientColorKey(GaVerde, 0f), new GradientColorKey(GaVerde, 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 1f) });
                    it.Linha.colorGradient = gv;
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _gaLuz);
                    break;
                case "corvomante_grasnido":
                    // o anel de PENAS DE LUZ que se desfazem (ficha): coroa que abre + penas nascendo na frente da onda
                    it.R = Peca(raiz, MalhaVfx.Anel(), _gaLuz);
                    it.R2 = Peca(raiz, MalhaVfx.Disco(), _gaLuz);
                    it.Ps = ParticulaVfx.Novo(raiz, "Penas", new Color(0.75f, 1f, 0.85f), GaVerde, 0f,
                        new Vector2(0.6f, 1.2f), new Vector2(0.5f, 2f), new Vector2(0.1f, 0.25f), true, 0.3f, 260);
                    ParticleSystem.ShapeModule shg = it.Ps.shape;
                    shg.shapeType = ParticleSystemShapeType.Circle;
                    shg.radiusThickness = 0f;
                    break;

                // ------------------------------------------------------------ Olho-de-Eter
                case "olho_po":
                    // PO DE ETER em volta de quem conjurou: motas magenta e douradas subindo devagar
                    it.Ps = ParticulaVfx.Novo(raiz, "Po", GaMagenta, GaDourado, 26f,
                        new Vector2(0.8f, 1.6f), new Vector2(0.1f, 0.5f), new Vector2(0.06f, 0.14f), true, 0.5f, 48);
                    ParticleSystem.ShapeModule shp = it.Ps.shape;
                    shp.scale = new Vector3(0.7f, 0.7f, 1.8f);
                    break;
                case "olho_enxame":
                    // o TUNEL (faixa no chao, o aviso) + o ENXAME de mariposas: juntando nas maos, depois voando a linha
                    it.Linha = Linha(it.Go, 2f);
                    it.Linha.numCapVertices = 0;
                    it.Ps = GaMariposas(raiz, "Enxame", 40f, true, 200);
                    ParticleSystem.ShapeModule shx = it.Ps.shape;
                    shx.scale = new Vector3(1.2f, 1.2f, 1f);
                    break;
                case "olho_mariposas":
                    // MARIPOSAS POUSADAS: pequeno enxame orbitando o marcado (LOCAL: vai com ele) + coluna magenta fina
                    it.Linha = Linha(it.Go, 0.28f);
                    it.Linha.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
                    var gm = new Gradient();
                    gm.SetKeys(new[] { new GradientColorKey(GaMagenta, 0f), new GradientColorKey(GaDourado, 1f) },
                        new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) });
                    it.Linha.colorGradient = gm;
                    it.Ps = GaMariposas(raiz, "Pousadas", 12f, false, 24);
                    GaOrbita(it.Ps, 0.65f, 3f);
                    break;
                case "olho_orbita":
                    // as mariposas que SAO os ouvidos dele: poucas, lentas, em volta do peito
                    it.Ps = GaMariposas(raiz, "Ouvidos", 4f, false, 12);
                    GaOrbita(it.Ps, 0.8f, 1.4f);
                    break;
                case "olho_casulo":
                    // o CASULO de luz: ovo magenta pulsando no batimento que acelera + anel dourado no chao + po subindo
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _gaLuz);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _gaLuz);
                    it.Ps = ParticulaVfx.Novo(raiz, "Po", GaDourado, GaMagenta, 30f,
                        new Vector2(0.6f, 1.2f), new Vector2(0.3f, 1.2f), new Vector2(0.05f, 0.12f), true, 0.4f, 40);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                    break;
                case "olho_eclosao":
                    // a ECLOSAO: explosao de po dourado em camera lenta + o anel magenta abrindo ate' o raio
                    it.R = Peca(raiz, MalhaVfx.Anel(), _gaLuz);
                    it.Ps = ParticulaVfx.Novo(raiz, "Eclosao", GaDourado, new Color(1f, 0.95f, 0.75f), 0f,
                        new Vector2(1.2f, 2.2f), new Vector2(3f, 9f), new Vector2(0.12f, 0.32f), true, 1f, 320);
                    it.Ps2 = GaMariposas(it.Ps.transform, "Revoada", 0f, true, 60);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    ParticleSystem.MainModule mrv = it.Ps2.main;
                    mrv.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
                    break;
                default:
                    feito = false;
                    break;
            }
        }

        // =============================================================================== DESENHO (por quadro)

        partial void DesenhoGrupoA(Item it, EfeitoVisual v, float e, Pawn dono, ref bool feito)
        {
            feito = true;
            switch (v.Tipo)
            {
                case "ceifadora_marca": CeifadoraMarca(it, v); break;
                case "ceifadora_mao": CeifadoraMao(it, v, e); break;
                case "ceifadora_rasgo": CeifadoraRasgo(it, v, e); break;
                case "ceifadora_eco": CeifadoraEco(it, v, e, dono); break;
                case "ceifadora_coro": CeifadoraCoro(it, dono); break;
                case "corvus_trilha": CorvusTrilha(it, v, e, dono); break;
                case "corvus_uivo": CorvusUivo(it, v, dono); break;
                case "corvus_cheiro": CorvusCheiro(it, v, e); break;
                case "corvus_garra": CorvusGarra(it, v, e); break;
                case "corvus_penas": CorvusPenas(it, v); break;
                case "corvus_fera": GaSegue(it, dono != null ? dono.Pos : v.Pos, 0.9f); break;
                case "corvomante_corvo": CorvomanteCorvo(it, v, e); break;
                case "corvomante_marca": CorvomanteMarca(it, v, e); break;
                case "corvomante_grasnido": CorvomanteGrasnido(it, v); break;
                case "olho_po": GaTaxa(it.Ps, 26f * e); GaSegue(it, PosDoAlvo(v), 0.9f); break;
                case "olho_enxame": OlhoEnxame(it, v, e, dono); break;
                case "olho_mariposas": OlhoMariposas(it, v, e); break;
                case "olho_orbita": GaSegue(it, dono != null ? dono.Pos : v.Pos, 1.2f); break;
                case "olho_casulo": OlhoCasulo(it, v, e); break;
                case "olho_eclosao": OlhoEclosao(it, v); break;
                default: feito = false; break;
            }
        }

        // ------------------------------------------------------------------ Ceifadora

        /// <summary>A rachadura CORRE do pe' dela ate' o ponto no tempo do aviso; o anel aperta e pisca (le'-se de canto de olho).</summary>
        void CeifadoraMarca(Item it, EfeitoVisual v)
        {
            Vector3 a = NoChao(v.Pos), b = NoChao(v.Pos2);
            float prog = 1f - Mathf.Clamp01(v.Restante / Mathf.Max(v.Duracao, 0.001f));
            it.Go.transform.SetPositionAndRotation(b + Vector3.up * 0.06f, Inclinacao(b));
            it.R.transform.localScale = Vector3.one * v.Raio;
            Pintar(it.R, GaVazio, 0.55f + 0.3f * prog);
            it.R2.transform.localScale = Vector3.one * v.Raio * (1.2f - 0.2f * prog);
            Pintar(it.R2, Hdr(GaVioleta, 2.2f), (Passo() & 1) == 0 ? 1f : 0.65f);
            Vector3 lado = Vector3.Cross(b - a, Vector3.up);
            lado = lado.sqrMagnitude > 1e-6f ? lado.normalized : Vector3.right;
            int n = GaPontosRacha;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                Vector3 p = NoChao(Vector3.Lerp(a, Vector3.Lerp(a, b, prog), t));
                p += lado * ((i & 1) == 0 ? 0.16f : -0.16f) * Mathf.Sin(t * Mathf.PI);   // o zigue-zague de rachadura
                it.Pontos[i] = p + Vector3.up * 0.07f;
            }
            it.Linha.positionCount = n;
            it.Linha.SetPositions(it.Pontos);
            Color c = Hdr(GaVioleta, 1.4f);
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            GaTaxa(it.Ps, 40f * (0.3f + prog));
        }

        /// <summary>A mao IRROMPE em 3 passos de anime ("a foice se condensa em 3 frames") e afunda no fim; segue o preso.</summary>
        void CeifadoraMao(Item it, EfeitoVisual v, float e)
        {
            Vector3 pe = NoChao(v.Alvo != null ? PosDoAlvo(v) : v.Pos);
            float vivo = v.Duracao - v.Restante;
            float sobe = Mathf.Clamp01((Mathf.Floor(vivo / PassoAnime) + 1f) / 3f);
            it.Go.transform.SetPositionAndRotation(pe, Quaternion.Euler(0f, Passo() % 2 == 0 ? 0f : 4f, 0f));   // aperta
            float h = sobe * Mathf.Lerp(0.15f, 1f, (e - LeituraDosKits.Piso) / (1f - LeituraDosKits.Piso));
            it.R.transform.localScale = new Vector3(0.95f, 1.05f * h, 0.95f);
            Pintar(it.R, Color.white, 0.95f);
            it.R2.transform.localScale = Vector3.one * 0.85f;
            Pintar(it.R2, Hdr(GaDourado, 2.2f), sobe * e * Flicker[Passo() % Flicker.Length]);
            GaTaxa(it.Ps, 30f * e);
            GaTaxa(it.Ps2, 20f * e);
        }

        /// <summary>O CORTE na entrada (virado para quem vem de la', abre e cicatriza) + a costura violeta no chao ate' a saida.</summary>
        void CeifadoraRasgo(Item it, EfeitoVisual v, float e)
        {
            Vector3 a = NoChao(v.Pos), b = NoChao(v.Pos2);
            Vector3 d = b - a;
            d.y = 0f;
            Quaternion rot = d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d, Vector3.up) : Quaternion.identity;
            it.Go.transform.SetPositionAndRotation(a, rot);
            float vivo = v.Duracao - v.Restante;
            float abre = Mathf.Clamp01(vivo / 0.25f) * (e - LeituraDosKits.Piso) / (1f - LeituraDosKits.Piso);   // abre rapido, fecha no fim
            float f = Flicker[Passo() % Flicker.Length];
            it.R.transform.localScale = new Vector3(Mathf.Max(abre, 0.02f), 1f, 1f);
            Pintar(it.R, GaVazio, 0.95f);
            it.R2.transform.localScale = new Vector3(Mathf.Max(abre, 0.02f), 1f, 1f);
            Pintar(it.R2, Hdr(GaDourado, 2.4f * f), 1f);
            int n = GaPontosRasgo;
            for (int i = 0; i < n; i++) it.Pontos[i] = NoChao(Vector3.Lerp(a, b, i / (float)(n - 1))) + Vector3.up * 0.1f;
            it.Linha.positionCount = n;
            it.Linha.SetPositions(it.Pontos);
            it.Linha.widthMultiplier = 0.35f * f;
            Color c = Hdr(GaVioleta, 1.6f);
            c.a = e;
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            GaTaxa(it.Ps, 40f * e);
            GaTaxa(it.Ps2, 30f * e);
        }

        /// <summary>O vulto de vidro fosco refazendo os ultimos 3s de quem caiu, em laco (o caminho e' do kit dela).</summary>
        void CeifadoraEco(Item it, EfeitoVisual v, float e, Pawn dono)
        {
            Ceifadora c = dono != null && dono.Runner != null ? dono.Runner.Impl as Ceifadora : null;
            Ceifadora.Eco eco = c != null ? c.EcoDe(v) : null;
            Vector3 pe = NoChao(eco != null ? eco.Em(v.Duracao - v.Restante) : v.Pos);
            it.Go.transform.position = pe + Vector3.up * 0.9f;
            it.Linha.SetPosition(0, pe);
            it.Linha.SetPosition(1, pe + Vector3.up * 1.75f);
            Color k = GaVidro;
            k.a = 0.6f * e * Flicker[Passo() % Flicker.Length];
            it.Linha.startColor = k;
            it.Linha.endColor = k;
            GaTaxa(it.Ps, 8f * e);
        }

        /// <summary>A telegrafia da Travessia: a mancha escura cresce em volta dela e o coro de motas sobe cada vez mais denso.</summary>
        void CeifadoraCoro(Item it, Pawn dono)
        {
            if (dono == null || dono.Runner == null) return;
            KitRunner k = dono.Runner;
            float total = Mathf.Clamp(k.Dados.Telegrafia, Kits.TelegrafiaMin, Kits.TelegrafiaMax);
            float prog = 1f - Mathf.Clamp01(k.Telegrafia / total);
            Vector3 pe = NoChao(dono.Pos);
            it.Go.transform.SetPositionAndRotation(pe + Vector3.up * 0.05f, Inclinacao(pe));
            it.R.transform.localScale = Vector3.one * (1f + 3f * prog);
            Pintar(it.R, GaVazio, 0.7f);
            GaTaxa(it.Ps, 30f + 90f * prog);
            GaTaxa(it.Ps2, 8f + 20f * prog);
        }

        // ------------------------------------------------------------------ Corvus

        /// <summary>A fita de cheiro: os pontos que ja' sao PASSADO (o kit filtra), rente ao chao, na cor do elemento de quem passou.</summary>
        void CorvusTrilha(Item it, EfeitoVisual v, float e, Pawn dono)
        {
            Corvus c = dono != null && dono.Runner != null ? dono.Runner.Impl as Corvus : null;
            int n = 0;
            if (c != null)
            {
                Dictionary<string, float> p = dono.Runner.Dados.Passiva;
                n = c.PontosDaTrilha(v.Alvo, p["trilha_atraso"], p["trilha_janela"], it.Pontos);
            }
            if (n < 2) { it.Linha.positionCount = 0; return; }
            for (int i = 0; i < n; i++) it.Pontos[i].y += 0.15f;
            it.Linha.positionCount = n;
            it.Linha.SetPositions(it.Pontos);
            bool fera = dono.Runner.EstadoAtivo(Corvus.FERA);
            Pawn alvo = v.Alvo as Pawn;
            Color cor = fera ? Hdr(GaCaca, 1.8f) : (alvo != null ? Projetil.Tint(Arkana.Characters.IdentidadeMago.De(alvo.Slug).Elemento) : GaVento);
            cor.a = (fera ? 0.9f : 0.55f) * e;
            it.Linha.widthMultiplier = fera ? 0.32f : 0.22f;
            it.Linha.startColor = new Color(cor.r, cor.g, cor.b, cor.a * 0.2f);   // a ponta velha esmaece
            it.Linha.endColor = cor;
        }

        /// <summary>O anel do uivo abrindo ate' o raio (e o eco dele atrasado). O de TRANSFORMACAO sai vermelho-caca.</summary>
        void CorvusUivo(Item it, EfeitoVisual v, Pawn dono)
        {
            float prog = 1f - Mathf.Clamp01(v.Restante / Mathf.Max(v.Duracao, 0.001f));
            bool fera = dono != null && dono.Runner != null && (dono.Runner.Telegrafia > 0f || dono.Runner.EstadoAtivo(Corvus.FERA));
            Vector3 pe = NoChao(v.Pos);
            it.Go.transform.SetPositionAndRotation(pe + Vector3.up * 0.15f, Inclinacao(pe));
            float r1 = v.Raio * (1f - (1f - prog) * (1f - prog));
            float p2 = Mathf.Clamp01(prog - 0.2f);
            float r2 = v.Raio * (1f - (1f - p2) * (1f - p2));
            it.R.transform.localScale = Vector3.one * Mathf.Max(r1, 0.05f);
            it.R2.transform.localScale = Vector3.one * Mathf.Max(r2, 0.05f);
            Color c = Hdr(fera ? GaCaca : GaVento, 2f);
            Pintar(it.R, c, 1f - prog);
            Pintar(it.R2, c, 0.5f * (1f - prog));
        }

        /// <summary>O contorno de cheiro no alvo: aceso so' ENQUANTO ele anda (o kit zera o Raio quando para).</summary>
        void CorvusCheiro(Item it, EfeitoVisual v, float e)
        {
            Vector3 pe = PosDoAlvo(v);
            float anda = v.Raio > 0f ? 1f : 0f;
            it.Go.transform.position = pe + Vector3.up * 0.9f;
            it.Linha.SetPosition(0, pe - Vector3.up * 0.05f);
            it.Linha.SetPosition(1, pe + Vector3.up * 1.95f);
            Color c = Hdr(GaVento, 1.6f);
            c.a = 0.9f * e * anda * Flicker[Passo() % Flicker.Length];
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            GaTaxa(it.Ps, 24f * e * anda);
        }

        /// <summary>Tres riscos de garra no peito do alvo, virados para a camera, crescendo no golpe.</summary>
        void CorvusGarra(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = PosDoAlvo(v) + Vector3.up * 1.1f;
            Camera cam = Camera.main;
            Quaternion rot = cam != null ? Quaternion.LookRotation(cam.transform.forward, Vector3.up) : Quaternion.identity;
            it.Go.transform.SetPositionAndRotation(p, rot);
            float prog = 1f - Mathf.Clamp01(v.Restante / Mathf.Max(v.Duracao, 0.001f));
            it.R.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.15f, prog);
            Pintar(it.R, Hdr(GaCaca, 2.6f), e);
            GaTaxa(it.Ps, prog < 0.4f ? 90f : 0f);
        }

        /// <summary>A transformacao: um ESTOURO so' (os primeiros 0,3s), depois as penas caem e a nevoa sobe sozinhas.</summary>
        void CorvusPenas(Item it, EfeitoVisual v)
        {
            GaSegue(it, v.Pos, 1f);
            bool estouro = v.Duracao - v.Restante < 0.3f;
            GaTaxa(it.Ps, estouro ? 260f : 0f);
            GaTaxa(it.Ps2, estouro ? 110f : 0f);
        }

        // ------------------------------------------------------------------ Corvomante

        /// <summary>O corvo: V de energia verde batendo asa em passos de anime, com o olho claro na frente, nunca dentro do morro.</summary>
        void CorvomanteCorvo(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = v.Pos;
            float piso = Ilha.AlturaDoChao(p.x, p.z) + 1.5f;
            if (p.y < piso) p.y = piso;
            Vector3 f = v.Pos2 - v.Pos;
            f.y = 0f;
            f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
            Vector3 lado = Vector3.Cross(Vector3.up, f);
            float asa = GaAsa[Passo() % GaAsa.Length];
            const float envergadura = 1f;
            it.Go.transform.position = p;
            it.Linha.SetPosition(0, p - lado * envergadura + Vector3.up * asa - f * 0.25f);
            it.Linha.SetPosition(1, p - lado * envergadura * 0.45f + Vector3.up * (asa * 0.4f + 0.08f));
            it.Linha.SetPosition(2, p + f * 0.35f);
            it.Linha.SetPosition(3, p + lado * envergadura * 0.45f + Vector3.up * (asa * 0.4f + 0.08f));
            it.Linha.SetPosition(4, p + lado * envergadura + Vector3.up * asa - f * 0.25f);
            Color c = Hdr(GaVerde, 2.4f);
            c.a = 0.9f * e;
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            it.R.transform.position = p + f * 0.3f + Vector3.up * 0.06f;
            it.R.transform.localScale = Vector3.one * 0.16f;
            Pintar(it.R, Hdr(new Color(0.95f, 1f, 0.9f), 2.6f), e);   // o olho HUMANO nitido: o ponto mais claro do corvo
            GaTaxa(it.Ps, 30f * e);
        }

        /// <summary>A marca do corvo: coluna verde subindo da cabeca do alvo + o losango-olho girando e flutuando em passos.</summary>
        void CorvomanteMarca(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = PosDoAlvo(v);
            int passo = Passo();
            it.Go.transform.position = p;
            it.Linha.SetPosition(0, p + Vector3.up * 2.1f);
            it.Linha.SetPosition(1, p + Vector3.up * 8f);
            Pintar(it.Linha, Hdr(Color.white, MaterialVfx.BrilhoHdr), 0.8f * e);
            float boia = (passo % 6) * 0.03f;
            it.R.transform.SetPositionAndRotation(p + Vector3.up * (2.35f + boia), Quaternion.Euler(0f, passo % 24 * 15f, 45f));
            it.R.transform.localScale = new Vector3(0.22f, 0.22f, 0.08f);
            Pintar(it.R, Hdr(GaVerde, 2.6f), e);
        }

        /// <summary>O Grasnido: a coroa verde abre ate' o raio (rapido no comeco) e as penas nascem na frente da onda.</summary>
        void CorvomanteGrasnido(Item it, EfeitoVisual v)
        {
            float prog = 1f - Mathf.Clamp01(v.Restante / Mathf.Max(v.Duracao, 0.001f));
            float r = Mathf.Max(v.Raio * (1f - (1f - prog) * (1f - prog)), 0.05f);
            Vector3 pe = NoChao(v.Pos);
            it.Go.transform.SetPositionAndRotation(pe + Vector3.up * 0.2f, Inclinacao(pe));
            it.R.transform.localScale = Vector3.one * r;
            Pintar(it.R, Hdr(GaVerde, 2.6f), 1f - prog * 0.8f);
            it.R2.transform.localScale = Vector3.one * r;
            Pintar(it.R2, Hdr(GaVerde, 1.2f), 0.3f * (1f - prog));
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.radius = r;
            GaTaxa(it.Ps, prog < 0.85f ? 16f * v.Raio : 0f);
        }

        // ------------------------------------------------------------------ Olho-de-Eter

        /// <summary>O enxame: no atraso as mariposas se JUNTAM nas maos e o tunel pulsa no chao; depois voam a linha inteira.</summary>
        void OlhoEnxame(Item it, EfeitoVisual v, float e, Pawn dono)
        {
            float atraso = dono != null && dono.Runner != null ? dono.Runner.Dados.Tatica["atraso"] : 1.4f;
            float vivo = v.Duracao - v.Restante;
            bool juntando = vivo < atraso;
            float prog = juntando ? 0f : Mathf.Clamp01((vivo - atraso) / Mathf.Max(v.Duracao - atraso, 0.01f));
            Vector3 a = NoChao(v.Pos), b = NoChao(v.Pos2);
            Vector3 d = b - a;
            it.Go.transform.SetPositionAndRotation(Vector3.Lerp(a, b, prog) + Vector3.up * 1.2f,
                d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d, Vector3.up) : Quaternion.identity);
            it.Linha.SetPosition(0, a + Vector3.up * 0.08f);
            it.Linha.SetPosition(1, b + Vector3.up * 0.08f);
            it.Linha.widthMultiplier = v.Raio * 2f;
            Color c = Hdr(GaMagenta, 1.2f);
            c.a = juntando ? ((Passo() & 1) == 0 ? 0.32f : 0.18f) : 0.15f * e;
            it.Linha.startColor = c;
            it.Linha.endColor = new Color(c.r, c.g, c.b, c.a * 0.3f);
            GaTaxa(it.Ps, juntando ? 35f : 220f);
        }

        /// <summary>O marcado: o enxame pequeno orbitando a cabeca + a coluna magenta-dourada (le'-se de longe).</summary>
        void OlhoMariposas(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = PosDoAlvo(v);
            it.Go.transform.position = p + Vector3.up * 1.3f;
            it.Linha.SetPosition(0, p + Vector3.up * 1.6f);
            it.Linha.SetPosition(1, p + Vector3.up * 7f);
            Pintar(it.Linha, Hdr(Color.white, MaterialVfx.BrilhoHdr), 0.75f * e);
            GaTaxa(it.Ps, 12f * e);
        }

        /// <summary>O casulo: batimento que ACELERA ate' eclodir (a contagem sem numero), pulsando em HDR.</summary>
        void OlhoCasulo(Item it, EfeitoVisual v, float e)
        {
            Vector3 pe = NoChao(v.Pos);
            float prog = 1f - Mathf.Clamp01(v.Restante / Mathf.Max(v.Duracao, 0.001f));
            float fase = prog * prog * 9f;   // 9 batidas, cada vez mais juntas
            float bate = Mathf.Repeat(fase, 1f) < 0.3f ? 1.25f : 1f;
            it.Go.transform.SetPositionAndRotation(pe, Inclinacao(pe));
            it.R.transform.localPosition = Vector3.up * 0.7f;
            it.R.transform.localScale = new Vector3(0.6f, 0.95f, 0.6f) * bate;
            Pintar(it.R, Hdr(GaMagenta, 1.6f * bate), 0.85f);
            it.R2.transform.localPosition = Vector3.up * 0.05f;
            it.R2.transform.localScale = Vector3.one * v.Raio * bate;
            Pintar(it.R2, Hdr(GaDourado, 2f), e);
            GaTaxa(it.Ps, 30f + 60f * prog);
        }

        /// <summary>A eclosao: um estouro de po dourado + revoada de mariposas nos primeiros 0,3s; o anel abre ate' o raio.</summary>
        void OlhoEclosao(Item it, EfeitoVisual v)
        {
            float prog = 1f - Mathf.Clamp01(v.Restante / Mathf.Max(v.Duracao, 0.001f));
            Vector3 pe = NoChao(v.Pos);
            it.Go.transform.SetPositionAndRotation(pe + Vector3.up * 0.3f, Inclinacao(pe));
            it.R.transform.localScale = Vector3.one * Mathf.Max(v.Raio * (1f - (1f - prog) * (1f - prog)), 0.05f);
            Pintar(it.R, Hdr(GaMagenta, 2.2f), 1f - prog);
            bool estouro = prog < 0.2f;
            GaTaxa(it.Ps, estouro ? 700f : 0f);
            GaTaxa(it.Ps2, estouro ? 180f : 0f);
        }

        // =============================================================================== ESTADO (leitura por corpo)

        /// <summary>
        /// A leitura por corpo do grupo: a telegrafia da Ceifadora (o mundo perde a cor), a FERA do Corvus (corpo 30% maior
        /// + nevoa vermelha — o limitador "silhueta maior" e' visual: a capsula de acerto e' do motor), o corvo no ombro do
        /// Corvomante (some quando voa; parado no ombro quando exausto) e as mariposas-ouvido do Olho (somem no "cego").
        /// </summary>
        partial void EstadoGrupoA(Pawn dono)
        {
            KitRunner k = dono.Runner;
            Corvus corvus = k.Impl as Corvus;
            if (corvus != null)
            {
                bool fera = dono.Viva && k.EstadoAtivo(Corvus.FERA);
                GaSilhueta(dono, fera ? k.Dados.Suprema["silhueta"] : 1f);
                if (fera) Desenhar(GaFixo(dono, "corvus_fera"), dono);
                return;
            }
            if (!dono.Viva) return;
            if (k.Impl is Ceifadora)
            {
                if (k.Telegrafia > 0f) Desenhar(GaFixo(dono, "ceifadora_coro"), dono);
                return;
            }
            Corvomante cm = k.Impl as Corvomante;
            if (cm != null)
            {
                if (cm.CorvoFora) return;
                EfeitoVisual v = GaFixo(dono, "corvomante_corvo");
                Vector3 ombro = dono.Pos + Vector3.up * 1.95f;
                if (k.EstadoAtivo(Corvomante.EXAUSTO))
                {
                    // despencou exausto no ombro: parado, sem orbita
                    Vector3 lado = Vector3.Cross(Vector3.up, dono.Frente) * 0.35f;
                    v.Pos = ombro + lado;
                    v.Pos2 = v.Pos + dono.Frente;
                }
                else
                {
                    float a = Time.time * 1.6f;
                    Vector3 antes = v.Pos;
                    v.Pos = ombro + Vector3.up * 0.35f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.8f;
                    v.Pos2 = v.Pos + new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                    if ((v.Pos - antes).sqrMagnitude > 25f) v.Pos2 = v.Pos + dono.Frente;   // primeiro quadro: sem salto
                }
                Desenhar(v, dono);
                return;
            }
            if (k.Impl is OlhoDeEter && !k.EstadoAtivo(OlhoDeEter.CEGO)) Desenhar(GaFixo(dono, "olho_orbita"), dono);
        }

        EfeitoVisual GaFixo(Pawn dono, string tipo)
        {
            EfeitoVisual v;
            if (!_gaFixos.TryGetValue(dono, out v) || v.Tipo != tipo)
            {
                v = new EfeitoVisual(tipo, dono.Pos, dono.Pos, 0f, 1f);
                _gaFixos[dono] = v;
            }
            return v;
        }

        /// <summary>A fera cresce (e volta) em ~0,2s: o corpo se REMONTA, nao pisca. Morto volta ao tamanho normal.</summary>
        static void GaSilhueta(Pawn dono, float alvo)
        {
            if (dono.Visual == null) return;
            Transform t = dono.Visual.transform;
            float s = t.localScale.x;
            if (Mathf.Approximately(s, alvo)) return;
            t.localScale = Vector3.one * Mathf.MoveTowards(s, alvo, Time.deltaTime * 1.5f);
        }

        // =============================================================================== apoio

        static void GaTaxa(ParticleSystem ps, float taxa)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTimeMultiplier = taxa;
        }

        static void GaSegue(Item it, Vector3 p, float altura) => it.Go.transform.position = p + Vector3.up * altura;

        /// <summary>Emissor de MARIPOSAS: o sprite de asa geometrica (cel-shaded, magenta e ouro), girado ao acaso.</summary>
        static ParticleSystem GaMariposas(Transform pai, string nome, float taxa, bool mundo, int max)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, nome, GaMagenta, GaDourado, taxa,
                new Vector2(0.8f, 1.6f), new Vector2(0.3f, 1.2f), new Vector2(0.16f, 0.3f), mundo, 1f, max);
            ParticleSystem.MainModule m = ps.main;
            m.startRotation = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f);
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = false;   // mariposa nao encolhe ate' virar fiapo: some pelo alfa
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _gaMariposa;
            return ps;
        }

        /// <summary>Orbita em volta do eixo de cima (o emissor ja' esta' deitado: +Z local = cima), num aro de `raio` m.</summary>
        static void GaOrbita(ParticleSystem ps, float raio, float giro)
        {
            ParticleSystem.MainModule m = ps.main;
            m.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.15f);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = raio;
            sh.radiusThickness = 0.2f;
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(giro);
        }

        /// <summary>Mariposa 32x32 gerada 1x (zero arquivo): duas asas de cada lado (a de cima maior) e o corpo fino. Borda
        /// dura: le' "asa geometrica" (ficha), nao bolinha.</summary>
        static Texture2D GaTexMariposa()
        {
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Mariposa", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f), w = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Max(GaElipse(u - 0.45f, w - 0.22f, 0.42f, 0.5f), GaElipse(u - 0.32f, w + 0.45f, 0.28f, 0.3f));
                    if (u < 0.07f && Mathf.Abs(w) < 0.62f) a = 1f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }

        static float GaElipse(float x, float y, float rx, float ry) => Mathf.Clamp01((1f - (x * x) / (rx * rx) - (y * y) / (ry * ry)) * 4f);

        static void GaQuad(MalhaProc.Construtor b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 fora)
        {
            b.Tri(p0, p1, p2, Color.white, fora);
            b.Tri(p0, p2, p3, Color.white, fora);
        }

        /// <summary>A MAO: cinco dedos em aro (raio 0,6 no chao), cada um estufa para fora e fecha para dentro na ponta — a
        /// jaula de quem agarra. Secao triangular (palma para dentro). Degrade por altura: tinta no pe', violeta na ponta.</summary>
        static Mesh GaMalhaMao()
        {
            if (_gaMao != null) return _gaMao;
            var b = new MalhaProc.Construtor();
            Vector3 up = Vector3.up;
            for (int f = 0; f < 5; f++)
            {
                float ang = f * Mathf.PI * 2f / 5f;
                Vector3 fora = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 lado = new Vector3(-fora.z, 0f, fora.x);
                Vector3[] eixo = { fora * 0.6f, fora * 0.8f + up * 0.9f, fora * 0.48f + up * 1.55f, fora * 0.1f + up * 1.95f };
                float[] larg = { 0.17f, 0.13f, 0.08f, 0.01f };
                for (int s = 0; s < 3; s++)
                {
                    Vector3 l0 = eixo[s] - lado * larg[s], r0 = eixo[s] + lado * larg[s], k0 = eixo[s] + fora * larg[s];
                    Vector3 l1 = eixo[s + 1] - lado * larg[s + 1], r1 = eixo[s + 1] + lado * larg[s + 1], k1 = eixo[s + 1] + fora * larg[s + 1];
                    GaQuad(b, l0, r0, r1, l1, -fora);
                    GaQuad(b, r0, k0, k1, r1, lado + fora);
                    GaQuad(b, k0, l0, l1, k1, fora - lado);
                }
            }
            Mesh m = b.ParaMesh("GaMao");
            MalhaVfx.PintarPorAltura(m, GaVazio, GaVioleta);
            return _gaMao = m;
        }

        /// <summary>Contorno da lente do rasgo no plano XY local (pe' em y=0, 4,4 m de altura, 1,5 m de largura).</summary>
        static Vector3 GaLente(int i, int n, float escala)
        {
            float t = i / (float)n * Mathf.PI * 2f;
            float s = Mathf.Sin(t);
            // lente pontuda: largura ~ |cos|^1,5 (bicos em cima e embaixo)
            float c = Mathf.Cos(t);
            float lx = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 1.5f) * 0.75f * escala;
            return new Vector3(lx, 2.2f + s * 2.2f * Mathf.Lerp(1f, escala, 0.35f), 0f);
        }

        static Mesh GaMalhaRasgo()
        {
            if (_gaRasgo != null) return _gaRasgo;
            var b = new MalhaProc.Construtor();
            const int n = 20;
            Vector3 centro = new Vector3(0f, 2.2f, 0f);
            for (int i = 0; i < n; i++) b.Tri(centro, GaLente(i, n, 1f), GaLente(i + 1, n, 1f), Color.white, Vector3.forward);
            return _gaRasgo = b.ParaMesh("GaRasgo");
        }

        static Mesh GaMalhaRasgoBorda()
        {
            if (_gaRasgoBorda != null) return _gaRasgoBorda;
            var b = new MalhaProc.Construtor();
            const int n = 20;
            for (int i = 0; i < n; i++)
                GaQuad(b, GaLente(i, n, 1f), GaLente(i, n, 1.22f), GaLente(i + 1, n, 1.22f), GaLente(i + 1, n, 1f), Vector3.forward);
            return _gaRasgoBorda = b.ParaMesh("GaRasgoBorda");
        }

        /// <summary>Tres riscos de garra (crescentes finos em diagonal) no plano XY local, virados para +Z.</summary>
        static Mesh GaMalhaGarras()
        {
            if (_gaGarras != null) return _gaGarras;
            var b = new MalhaProc.Construtor();
            const int seg = 6;
            for (int g = 0; g < 3; g++)
            {
                float off = (g - 1) * 0.22f;
                for (int s = 0; s < seg; s++)
                {
                    float t0 = s / (float)seg, t1 = (s + 1) / (float)seg;
                    Vector3 c0 = GaRisco(t0, off), c1 = GaRisco(t1, off);
                    float w0 = 0.05f * Mathf.Sin(t0 * Mathf.PI) + 0.004f, w1 = 0.05f * Mathf.Sin(t1 * Mathf.PI) + 0.004f;
                    Vector3 n0 = new Vector3(0.7f, 0.7f, 0f);
                    GaQuad(b, c0 - n0 * w0, c0 + n0 * w0, c1 + n0 * w1, c1 - n0 * w1, Vector3.back);
                }
            }
            return _gaGarras = b.ParaMesh("GaGarras");
        }

        static Vector3 GaRisco(float t, float off) => new Vector3(-0.45f + t * 0.9f + off, 0.45f - t * 0.9f + off + Mathf.Sin(t * Mathf.PI) * 0.08f, 0f);
    }
}
