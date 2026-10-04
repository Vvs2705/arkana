using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Terrain;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O DESENHO do Grupo C (12/09) pelos ganchos parciais — Umbra (penumbra roxa, lamina violeta, isca de sombra, tinta
    /// preta), Brok (a Runa-Escudo da Meshy que sobe FUNDIDA e esfria, aro azul, bigorna que ressoa), Gromm (totem de chuva
    /// dourada, linha que treme, bisao de energia, rastro) e Maris (esfera d'agua, coluna, mare em bandas cel, choque).
    /// As regras da casca valem: pool por tipo, zero luz dinamica, passos de 12 fps, nada alocado por quadro. Peca a mais
    /// so' como FILHA do Ps (o Play(true)/Stop(true) do pool liga as filhas); renderer em Extra o Mostrar nao desligaria.
    /// Todo membro daqui leva o sufixo C (ou o nome do mago): os quatro grupos escrevem na MESMA classe.
    /// </summary>
    public sealed partial class VisualDosKits
    {
        static readonly Color CorUmbraC = new Color32(0x8A, 0x5C, 0xF0, 255);
        static readonly Color CorLunarC = new Color32(0xE8, 0xE6, 0xF0, 255);
        static readonly Color CorBronzeC = new Color32(0xA8, 0x76, 0x3E, 255);
        static readonly Color CorBronzeEscuroC = new Color32(0x7A, 0x52, 0x2A, 255);
        static readonly Color CorRunaC = new Color32(0x2A, 0xA7, 0xFF, 255);
        static readonly Color CorBrasaC = new Color32(0xFF, 0x5A, 0x2A, 255);
        static readonly Color CorOlivaC = new Color32(0x5C, 0x6B, 0x3C, 255);
        static readonly Color CorChuvaC = new Color(0.8f, 1f, 0.45f);          // "chuva dourada-esverdeada" (ficha 14)
        static readonly Color CorBisaoC = new Color(0.72f, 0.88f, 1f);         // energia branco-azulada
        static readonly Color CorAguaC = new Color32(0x2A, 0xA7, 0xFF, 255);
        static readonly Color CorVerdeMarC = new Color32(0x2E, 0x8B, 0x74, 255);
        static readonly Color CorPerolaC = new Color32(0xED, 0xE8, 0xE0, 255);
        /// <summary>A Umbra no veu: quase preta PARADA, um vulto roxo andando. KNOB por foto.</summary>
        static readonly Color TintPenumbraParadaC = new Color(0.14f, 0.11f, 0.24f), TintPenumbraAndandoC = new Color(0.42f, 0.35f, 0.62f);
        const int PontosChaoC = 16;
        /// <summary>A Runa-Escudo da Meshy (onda 11): escudo oval de pranchas, runas de ferro, aro azul. Sem o .glb, as placas de bronze.</summary>
        const string GlbEscudoBrokC = "44-escudo-brok";
        /// <summary>KNOB por foto: a face das RUNAS virada para o Brok (a camera dele e' quem mais olha o escudo); false = para o
        /// inimigo (o Brok ve' as pranchas e as alcas).</summary>
        static readonly bool RunasParaDentroC = true;

        static Material _cFumaca, _cTinta;
        static Mesh _cArco, _cRunas, _cBigorna, _cTotem, _cBisao;
        static float _cArcoRaio, _cArcoMeia, _cArcoAltura, _cArcoEsp;
        /// <summary>A muralha em cena e' o escudo da Meshy (true) ou as placas no arco (false, sem o .glb).</summary>
        static bool _cEscudoMeshy;
        /// <summary>Qual tinta de penumbra cada Umbra esta' usando (0 = nenhuma): o SetTint so' na BORDA.</summary>
        readonly Dictionary<Pawn, int> _penumbraC = new Dictionary<Pawn, int>();

        // =============================================================================== ganchos

        partial void NovoGrupoC(Item it, Transform raiz, string tipo, ref bool feito)
        {
            MateriaisC();
            feito = true;
            ParticleSystem.ShapeModule sh;
            switch (tipo)
            {
                // ---------------------------------------------------------------- UMBRA
                case "umbra_veu":
                    // FUMACA roxa em ALFA (escurece de verdade) e em MUNDO: andando, o vulto deixa rastro; + fitas violeta.
                    it.Ps = FumacaC(raiz, "Penumbra", new Color(0.2f, 0.17f, 0.28f, 0.6f), new Color(0.36f, 0.22f, 0.6f, 0.45f),
                        new Vector2(0.6f, 1.2f), new Vector2(0.1f, 0.5f), new Vector2(0.5f, 1f), true, 0.3f, 60);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                    FormaC(it.Ps, 0.5f, 0.5f, 1.5f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Fitas", CorUmbraC, CorLunarC, 0f,
                        new Vector2(0.5f, 1f), new Vector2(0.2f, 0.8f), new Vector2(0.05f, 0.12f), true, 0.6f, 30);
                    it.Ps2.transform.localRotation = Quaternion.identity;   // ja' herda o -90 da fumaca
                    FormaC(it.Ps2, 0.5f, 0.5f, 1.5f);
                    break;
                case "umbra_corte":
                    // a LAMINA VIOLETA: linha fina nas pontas e grossa no meio, riscada em diagonal na tela + faisca
                    it.Linha = Linha(it.Go, 0.35f);
                    it.Linha.positionCount = 2;
                    it.Linha.widthCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.05f));
                    it.Ps = ParticulaVfx.Novo(raiz, "Faiscas", CorLunarC, CorUmbraC, 0f,
                        new Vector2(0.2f, 0.45f), new Vector2(2f, 5f), new Vector2(0.04f, 0.1f), true, 1f, 40);
                    break;
                case "umbra_suga":
                    // a luz SUGADA: cupula escura (alfa) + motas de luz caindo para dentro (velocidade negativa na casca)
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAviso);
                    it.Ps = ParticulaVfx.Novo(raiz, "Luz", CorLunarC, CorUmbraC, 0f,
                        new Vector2(0.4f, 0.7f), new Vector2(-4f, -2.5f), new Vector2(0.05f, 0.14f), false, 0f, 80);
                    sh = it.Ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Sphere;
                    sh.radius = 2.4f;
                    sh.radiusThickness = 0f;
                    break;
                case "umbra_sombra":
                    // a ISCA: o vulto de capuz (a silhueta do eco, aditiva, em violeta) + fumaca escura + o anel do estouro
                    it.Linha = Linha(it.Go, 0.8f);
                    it.Linha.sharedMaterial = _mFantasma;
                    it.Linha.numCapVertices = 0;
                    it.Linha.positionCount = 2;
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Ps = FumacaC(raiz, "Sombra", new Color(0.08f, 0.06f, 0.12f, 0.7f), new Color(0.28f, 0.16f, 0.45f, 0.5f),
                        new Vector2(0.5f, 1f), new Vector2(0.1f, 0.4f), new Vector2(0.4f, 0.9f), true, 0.3f, 40);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                    FormaC(it.Ps, 0.5f, 0.5f, 1.4f);
                    break;
                case "umbra_tinta":
                    // o CORTE DE TINTA PRETA (ficha 12): linha em ALFA (aditivo nao escurece) + gotas que caem
                    it.Linha = Linha(it.Go, 0.5f);
                    it.Linha.sharedMaterial = _cTinta;
                    it.Linha.numCapVertices = 0;
                    it.Linha.positionCount = 2;
                    it.Ps = FumacaC(raiz, "Gotas", new Color(0.05f, 0.03f, 0.08f, 0.9f), new Color(0.22f, 0.12f, 0.34f, 0.8f),
                        new Vector2(0.4f, 0.8f), new Vector2(1.5f, 3.5f), new Vector2(0.06f, 0.16f), true, 1f, 40);
                    GravidadeC(it.Ps, 1.2f);
                    it.Ps.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                    FormaC(it.Ps, 0.15f, 0.15f, 1.8f);
                    break;
                case "umbra_estouro":
                    NovoEstouroC(it, raiz, CorUmbraC, CorLunarC, 0f, 4f);
                    break;
                case "umbra_luz":
                    // CEGA DE LUZ (o oposto dela): clarao branco que abre + raios espirrando
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mTear);
                    it.Ps = ParticulaVfx.Novo(raiz, "Raios", Color.white, CorLunarC, 0f,
                        new Vector2(0.3f, 0.6f), new Vector2(4f, 8f), new Vector2(0.05f, 0.14f), false, 1f, 60);
                    sh = it.Ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Sphere;
                    sh.radius = 0.3f;
                    break;

                // ---------------------------------------------------------------- BROK
                case "brok_tempera":
                    // o metal ATERRA o elemento: anel de runa nos pes + faisca azul rasteira
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Aterra", CorLunarC, CorRunaC, 0f,
                        new Vector2(0.25f, 0.5f), new Vector2(0.8f, 2.2f), new Vector2(0.03f, 0.08f), true, 0.8f, 30);
                    FormaC(it.Ps, 0.8f, 0.8f, 0.05f);
                    break;
                case "brok_martelada":
                    NovoEstouroC(it, raiz, new Color(1f, 0.9f, 0.5f), CorBrasaC, 1.2f, 5f);   // faisca de forja em todo cast
                    break;
                case "brok_muralha":
                {
                    // RUNA-ESCUDO: o escudo da Meshy (sem o .glb, placas de bronze no arco) + o brilho azul + o clarao que corre +
                    // faisca de forja e vapor. Tudo sai dos numeros do kit: o desenho e a logica medem o MESMO arco. A peca e o
                    // brilho num CORPO que sobe (escala y): fundido, do chao.
                    MedirArcoC();
                    Transform corpo = new GameObject("Corpo").transform;
                    corpo.SetParent(raiz, false);
                    Mesh m;
                    Material mat;
                    _cEscudoMeshy = PecaDaMeshy.Carregar(GlbEscudoBrokC, out m, out mat);
                    if (_cEscudoMeshy)
                    {
                        // ponytail: escudo PLANO na corda media do arco (entre a corda e o apice): com os 4 m do comprimento ele
                        // fica inteiro dentro da faixa e da abertura da logica. Arco de verdade pediria curvar a malha.
                        Bounds b = m.bounds;
                        float largura = 2f * _cArcoRaio * _cArcoMeia, z = ZEscudoC();
                        float kx = largura / Mathf.Max(b.size.x, 0.01f);
                        it.R = Peca(corpo, m, mat);
                        it.R.transform.localPosition = new Vector3(0f, 0f, z);
                        it.R.transform.localRotation = Quaternion.Euler(0f, RunasParaDentroC ? 180f : 0f, 0f);
                        it.R.transform.localScale = new Vector3(kx, _cArcoAltura / Mathf.Max(b.size.y, 0.01f), kx);   // 4 x 2,2 m
                        // o ARO AZUL: oval de pe' no plano do escudo, um tico maior que a borda — acende em volta, dos dois lados
                        it.R2 = Peca(corpo, MalhaVfx.Anel(), _mFeixe);
                        it.R2.transform.localPosition = new Vector3(0f, _cArcoAltura * 0.5f, z);
                        it.R2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        it.R2.transform.localScale = new Vector3(largura * 0.52f, 1f, _cArcoAltura * 0.53f);
                    }
                    else
                    {
                        it.R = Peca(corpo, MalhaArcoBrok(), Ilha.MaterialPadrao());
                        it.R2 = Peca(corpo, MalhaRunasBrok(), _mFeixe);
                    }
                    it.Linha = Linha(it.Go, 0.45f);
                    it.Linha.positionCount = 2;
                    it.Ps = ParticulaVfx.Novo(raiz, "Faiscas", new Color(1f, 0.9f, 0.5f), CorBrasaC, 0f,
                        new Vector2(0.4f, 0.9f), new Vector2(2f, 5f), new Vector2(0.04f, 0.1f), true, 0.5f, 140);
                    GravidadeC(it.Ps, 1.1f);
                    float corda = 2f * _cArcoRaio * Mathf.Sin(_cArcoMeia);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.1f, ZEscudoC());
                    FormaC(it.Ps, corda, 0.8f, 0.1f);
                    it.Ps2 = FumacaC(it.Ps.transform, "Vapor", new Color(0.85f, 0.85f, 0.9f, 0.35f), new Color(0.6f, 0.6f, 0.65f, 0.25f),
                        new Vector2(0.9f, 1.6f), new Vector2(0.5f, 1.4f), new Vector2(0.6f, 1.3f), true, 0.2f, 40);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    FormaC(it.Ps2, corda, 0.8f, 0.1f);
                    break;
                }
                case "brok_estilhaco":
                    NovoEstouroC(it, raiz, CorBronzeC, new Color(1f, 0.85f, 0.45f), 1.5f, 6f);
                    break;
                case "brok_erguida":
                    // a bigorna ACIMA DA CABECA no aviso: clang a cada passo (tranco + faisca)
                    it.R = Peca(raiz, MalhaBigornaBrok(), Ilha.MaterialPadrao());
                    it.Ps = ParticulaVfx.Novo(raiz, "Faiscas", new Color(1f, 0.9f, 0.5f), CorBrasaC, 0f,
                        new Vector2(0.3f, 0.7f), new Vector2(1.5f, 3.5f), new Vector2(0.04f, 0.09f), true, 1f, 60);
                    GravidadeC(it.Ps, 1.2f);
                    break;
                case "brok_bigorna":
                    // a BIGORNA-TOTEM: malha propria + anel azul do raio (acende no clang) + faisca no clang + runas subindo
                    it.R = Peca(raiz, MalhaBigornaBrok(), Ilha.MaterialPadrao());
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Clang", new Color(1f, 0.9f, 0.5f), CorBrasaC, 0f,
                        new Vector2(0.3f, 0.8f), new Vector2(3f, 7f), new Vector2(0.04f, 0.1f), true, 0.7f, 140);
                    GravidadeC(it.Ps, 1.2f);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                    FormaC(it.Ps, 0.6f, 0.4f, 0.05f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Runas", CorLunarC, CorRunaC, 0f,
                        new Vector2(1f, 1.8f), new Vector2(0.5f, 1.2f), new Vector2(0.06f, 0.14f), true, 0.05f, 90);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    it.Ps2.transform.localPosition = new Vector3(0f, 0f, -0.9f);   // no chao (o -Z local do Ps e' baixo)
                    break;
                case "brok_runa":
                    // a RUNA PESSOAL: dois aneis inclinados girando em passos + motas azuis subindo em volta
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Runas", CorLunarC, CorRunaC, 0f,
                        new Vector2(0.6f, 1.1f), new Vector2(0.1f, 0.3f), new Vector2(0.05f, 0.1f), true, 0.1f, 30);
                    Subir(it.Ps, 0.3f);
                    sh = it.Ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Circle;
                    sh.radius = 0.8f;
                    sh.radiusThickness = 0f;
                    break;

                // ---------------------------------------------------------------- GROMM
                case "gromm_totem":
                    // TOTEM (malha propria: poste, faixas pintadas, cranio de bisao, coroa de raios) + anel no chao + CHUVA
                    // dourada em riscos (Stretch) caindo de uma nuvem de tempestade + motas de cura subindo do chao.
                    it.R = Peca(raiz, MalhaTotemGromm(), Ilha.MaterialPadrao());
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Chuva", new Color(0.9f, 1f, 0.6f), CorChuvaC, 0f,
                        new Vector2(0.5f, 0.7f), new Vector2(-9f, -7f), new Vector2(0.03f, 0.06f), true, 0f, 400);
                    it.Ps.transform.localPosition = new Vector3(0f, 5.5f, 0f);
                    var riscos = it.Ps.GetComponent<ParticleSystemRenderer>();
                    riscos.renderMode = ParticleSystemRenderMode.Stretch;
                    riscos.lengthScale = 3f;
                    riscos.velocityScale = 0.03f;
                    it.Ps2 = FumacaC(it.Ps.transform, "Nuvem", new Color(0.29f, 0.35f, 0.48f, 0.55f), new Color(0.45f, 0.5f, 0.6f, 0.45f),
                        new Vector2(1.5f, 2.5f), new Vector2(0.05f, 0.2f), new Vector2(2.2f, 3.6f), true, 0.3f, 40);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    ParticleSystem cura = ParticulaVfx.Novo(it.Ps.transform, "Cura", new Color(0.7f, 1f, 0.5f), new Color(0.95f, 0.95f, 0.4f), 0f,
                        new Vector2(0.8f, 1.4f), new Vector2(0.6f, 1.3f), new Vector2(0.06f, 0.14f), true, 0.05f, 60);
                    cura.transform.localRotation = Quaternion.identity;
                    cura.transform.localPosition = new Vector3(0f, 0f, -5.4f);   // no chao (o -Z local da chuva e' baixo)
                    it.Extra = new Component[] { cura };
                    break;
                case "gromm_lascas":
                    NovoEstouroC(it, raiz, new Color(0.75f, 0.55f, 0.3f), CorChuvaC, 1.4f, 4f);
                    break;
                case "gromm_linha":
                case "gromm_rastro":
                    // FAIXA deitada no chao (TransformZ: a raiz aponta o Z para cima) seguindo o relevo + poeira/faisca na linha
                    it.Linha = Linha(it.Go, 0.6f);
                    it.Linha.alignment = LineAlignment.TransformZ;
                    it.Linha.numCapVertices = 0;
                    it.Pontos = new Vector3[PontosChaoC];
                    it.Linha.positionCount = PontosChaoC;
                    bool linha = tipo == "gromm_linha";
                    it.Ps = ParticulaVfx.Novo(raiz, linha ? "Tremor" : "Faiscas", linha ? new Color(1f, 0.9f, 0.4f) : CorBisaoC,
                        linha ? new Color(0.7f, 0.55f, 0.35f) : new Color(1f, 0.95f, 0.5f), 0f,
                        new Vector2(0.3f, 0.6f), new Vector2(1.5f, 3.5f), new Vector2(0.05f, 0.14f), true, 0.3f, 160);
                    it.Ps.transform.localRotation = Quaternion.identity;   // a raiz ja' tem o Z para cima (ver GrommFaixa)
                    GravidadeC(it.Ps, 1f);
                    break;
                case "gromm_bisao":
                    // o ESPIRITO: bisao de energia (malha aditiva) + casca que tremeluz + arco de raio no lombo + cascos de raio
                    it.R = Peca(raiz, MalhaBisaoGromm(), _mTear);
                    it.R2 = Peca(raiz, MalhaBisaoGromm(), _mTear);
                    it.R2.transform.localScale = Vector3.one * 1.1f;
                    it.Linha = Linha(it.Go, 0.12f);
                    it.Pontos = new Vector3[8];
                    it.Linha.positionCount = 8;
                    it.Ps = ParticulaVfx.Novo(raiz, "Cascos", Color.white, CorRaio, 0f,
                        new Vector2(0.15f, 0.35f), new Vector2(1.5f, 4f), new Vector2(0.05f, 0.12f), true, 1f, 90);
                    FormaC(it.Ps, 1f, 2.2f, 0.1f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Espirito", CorBisaoC, CorLunarC, 0f,
                        new Vector2(0.4f, 0.9f), new Vector2(0.2f, 0.8f), new Vector2(0.15f, 0.35f), true, 0.5f, 90);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    it.Ps2.transform.localPosition = new Vector3(0f, 0f, 1.2f);   // na altura do corpo (o +Z local do Ps e' cima)
                    FormaC(it.Ps2, 1f, 2.2f, 0.8f);
                    break;
                case "gromm_dissolve":
                    NovoEstouroC(it, raiz, CorBisaoC, CorLunarC, 1f, 2.5f);   // dissolve em chuva fina
                    break;

                // ---------------------------------------------------------------- MARIS
                case "maris_mare_viva":
                    // ONDULA na lamina sob os pes + perolas subindo (a cura)
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Perolas", CorPerolaC, CorAguaC, 0f,
                        new Vector2(0.8f, 1.4f), new Vector2(0.4f, 1f), new Vector2(0.05f, 0.11f), true, 0.1f, 30);
                    FormaC(it.Ps, 0.7f, 0.7f, 0.1f);
                    break;
                case "maris_esfera":
                    // ESFERA d'agua em BANDAS CEL: casca azul em alfa chapado + miolo perola aceso + gotas pingando
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mAviso);
                    it.R2 = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Gotas", CorPerolaC, CorAguaC, 0f,
                        new Vector2(0.3f, 0.6f), new Vector2(0.2f, 0.8f), new Vector2(0.05f, 0.12f), true, 1f, 50);
                    GravidadeC(it.Ps, 1f);
                    sh = it.Ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Sphere;
                    sh.radius = 0.4f;
                    break;
                case "maris_coluna":
                    // a COLUNA D'AGUA em volta do preso: cilindro em alfa + espuma na base + bolhas subindo + borrifo no topo
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Cylinder), _mAviso);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Ps = ParticulaVfx.Novo(raiz, "Bolhas", CorPerolaC, CorAguaC, 0f,
                        new Vector2(0.5f, 1f), new Vector2(1.5f, 3f), new Vector2(0.06f, 0.14f), false, 0.1f, 60);
                    FormaC(it.Ps, 0.9f, 0.9f, 0.3f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Borrifo", CorPerolaC, CorAguaC, 0f,
                        new Vector2(0.5f, 0.9f), new Vector2(2f, 4f), new Vector2(0.06f, 0.15f), true, 0.7f, 60);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    it.Ps2.transform.localPosition = new Vector3(0f, 0f, 2.6f);
                    GravidadeC(it.Ps2, 1.3f);
                    break;
                case "maris_respingo":
                    NovoEstouroC(it, raiz, CorAguaC, CorPerolaC, 1.4f, 4f);
                    break;
                case "maris_recuo":
                    // o AVISO e' o mundo secando: a linha d'agua (espuma) recua para o centro com o disco que ainda e' agua
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.R2 = Peca(raiz, MalhaVfx.Disco(), _mAviso);
                    it.Ps = ParticulaVfx.Novo(raiz, "Espuma", CorPerolaC, CorAguaC, 0f,
                        new Vector2(0.6f, 1f), new Vector2(-5f, -3f), new Vector2(0.08f, 0.2f), false, 0f, 160);
                    sh = it.Ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Circle;
                    sh.radiusThickness = 0f;
                    break;
                case "maris_mare":
                    // a MARE: lamina d'agua em alfa chapado (banda cel, sem transparencia realista) + borda de espuma + brilho
                    // na superficie + a parede de mare espirrando na borda enquanto chega
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mAviso);
                    it.R2 = Peca(raiz, MalhaVfx.Disco(), _mAviso);
                    it.Ps = ParticulaVfx.Novo(raiz, "Brilho", CorPerolaC, CorVerdeMarC, 0f,
                        new Vector2(0.6f, 1.2f), new Vector2(0.1f, 0.4f), new Vector2(0.08f, 0.2f), true, 0.3f, 160);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Onda", CorPerolaC, CorAguaC, 0f,
                        new Vector2(0.4f, 0.8f), new Vector2(0.5f, 1.8f), new Vector2(0.1f, 0.25f), true, 0.6f, 160);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    sh = it.Ps2.shape;
                    sh.shapeType = ParticleSystemShapeType.Circle;
                    sh.radiusThickness = 0f;
                    break;
                case "maris_choque":
                    // AGUA CONDUZ: arco de raio em zigue-zague cruzando a mare (re-sorteado a cada passo) + borda + faiscas
                    it.R = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
                    it.Linha = Linha(it.Go, 0.25f);
                    it.Pontos = new Vector3[10];
                    it.Linha.positionCount = 10;
                    it.Ps = ParticulaVfx.Novo(raiz, "Choque", Color.white, CorRaio, 0f,
                        new Vector2(0.1f, 0.25f), new Vector2(2f, 6f), new Vector2(0.04f, 0.1f), true, 1f, 150);
                    break;
                default:
                    feito = false;
                    break;
            }
        }

        partial void DesenhoGrupoC(Item it, EfeitoVisual v, float e, Pawn dono, ref bool feito)
        {
            feito = true;
            switch (v.Tipo)
            {
                case "umbra_veu": UmbraVeu(it, v, e, dono); break;
                case "umbra_corte": UmbraCorte(it, v); break;
                case "umbra_suga": UmbraSuga(it, v); break;
                case "umbra_sombra": UmbraSombra(it, v); break;
                case "umbra_tinta": UmbraTinta(it, v); break;
                case "umbra_estouro": EstouroC(it, v, CorUmbraC); break;
                case "umbra_luz": UmbraLuz(it, v); break;
                case "brok_tempera": BrokTempera(it, v, e); break;
                case "brok_martelada": EstouroC(it, v, CorBrasaC); break;
                case "brok_muralha": BrokMuralha(it, v); break;
                case "brok_estilhaco": EstouroC(it, v, CorRunaC); break;
                case "brok_erguida": BrokErguida(it, v, dono); break;
                case "brok_bigorna": BrokBigorna(it, v, e); break;
                case "brok_runa": BrokRuna(it, v, e); break;
                case "gromm_totem": GrommTotem(it, v, e); break;
                case "gromm_lascas": EstouroC(it, v, CorChuvaC); break;
                case "gromm_linha": GrommFaixa(it, v, CorRaio, ProgC(v), 1f); break;
                case "gromm_rastro": GrommFaixa(it, v, CorBisaoC, 0f, e); break;
                case "gromm_bisao": GrommBisao(it, v, e); break;
                case "gromm_dissolve": EstouroC(it, v, CorBisaoC); break;
                case "maris_mare_viva": MarisViva(it, v, e); break;
                case "maris_esfera": MarisEsfera(it, v); break;
                case "maris_coluna": MarisColuna(it, v); break;
                case "maris_respingo": EstouroC(it, v, CorPerolaC); break;
                case "maris_recuo": MarisRecuo(it, v); break;
                case "maris_mare": MarisMare(it, v, e); break;
                case "maris_choque": MarisChoque(it, v, e); break;
                default: feito = false; break;
            }
        }

        /// <summary>
        /// A PENUMBRA no corpo da Umbra (veu ou danca): quase preta parada, vulto roxo andando. Leitura do estado, como o
        /// braco da Pyra. So' na BORDA (SetTint mexe nos materiais). Morta, a tinta da morte (Pawn) manda: nao devolvo branco.
        /// </summary>
        partial void EstadoGrupoC(Pawn dono)
        {
            Umbra u = dono.Runner.Impl as Umbra;
            if (u == null || dono.Visual == null) return;
            int era;
            _penumbraC.TryGetValue(dono, out era);
            if (!dono.Viva) { if (era != 0) _penumbraC.Remove(dono); return; }
            int agora = u.NoVeu || dono.Runner.EstadoAtivo(Umbra.DANCA) ? (dono.VelocidadeHorizontal > 1f ? 1 : 2) : 0;
            if (agora == era) return;
            dono.Visual.SetTint(agora == 2 ? TintPenumbraParadaC : agora == 1 ? TintPenumbraAndandoC : Color.white);
            if (agora == 0) _penumbraC.Remove(dono);
            else _penumbraC[dono] = agora;
        }

        // =============================================================================== UMBRA

        void UmbraVeu(Item it, EfeitoVisual v, float e, Pawn dono)
        {
            it.Go.transform.position = PosDoAlvo(v);
            bool anda = dono != null && dono.VelocidadeHorizontal > 1f;   // o vulto em movimento e' visivel (o limitador)
            float k = v.Raio * e;
            EmitirC(it.Ps, (anda ? 28f : 9f) * k);
            EmitirC(it.Ps2, (anda ? 18f : 5f) * k);
        }

        void UmbraCorte(Item it, EfeitoVisual v)
        {
            float p = ProgC(v);
            Vector3 c = PosDoAlvo(v) + Vector3.up * 1.1f;
            Vector3 lado = LadoDaCameraC();
            Vector3 a = c + lado * 0.8f + Vector3.up * 0.7f, b = c - lado * 0.8f - Vector3.up * 0.7f;
            it.Go.transform.position = c;
            it.Linha.SetPosition(0, a);
            it.Linha.SetPosition(1, Vector3.Lerp(a, b, Mathf.Clamp01(p / 0.25f)));   // RISCA num piscar e fica
            it.Linha.widthMultiplier = 0.35f * (1f - p * 0.6f);
            it.Linha.startColor = CorUmbraC;
            it.Linha.endColor = CorLunarC;
            Pintar(it.Linha, Hdr(Color.white, 2.4f), 1f - p);
            EmitirC(it.Ps, p < 0.3f ? 160f : 0f);
        }

        void UmbraSuga(Item it, EfeitoVisual v)
        {
            float p = ProgC(v);
            it.Go.transform.position = PosDoAlvo(v) + Vector3.up * 1f;
            it.R.transform.localScale = Vector3.one * (v.Raio * 2f * (1f - 0.3f * p));   // a escuridao aperta
            Pintar(it.R, new Color(0.05f, 0.03f, 0.1f, 1f), 0.15f + 0.45f * p);
            EmitirC(it.Ps, 40f + 60f * p);
        }

        void UmbraSombra(Item it, EfeitoVisual v)
        {
            Vector3 pe = NoChao(v.Pos);
            float p = ProgC(v);
            int passo = Passo();
            it.Go.transform.position = pe;
            it.Linha.SetPosition(0, pe);
            it.Linha.SetPosition(1, pe + Vector3.up * 1.7f);
            it.Linha.widthMultiplier = 0.8f * (0.9f + 0.25f * p);   // incha antes de estourar
            Color c = CorUmbraC;
            c.a = 0.9f * Flicker[(passo + (int)(p * 8f)) % Flicker.Length];
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            it.R2.transform.SetPositionAndRotation(pe + Vector3.up * 0.06f, Inclinacao(v.Pos));
            it.R2.transform.localScale = Vector3.one * v.Raio;
            Pintar(it.R2, Hdr(CorUmbraC, 1.6f), (passo & 1) == 0 ? 0.9f : 0.45f);   // o raio da isca PISCA: quem ve' sai
            EmitirC(it.Ps, 26f);
        }

        void UmbraTinta(Item it, EfeitoVisual v)
        {
            Vector3 pe = NoChao(v.Pos);
            float p = ProgC(v);
            Vector3 topo = pe + Vector3.up * 2.4f;
            it.Go.transform.position = pe;
            it.Linha.SetPosition(0, topo);
            it.Linha.SetPosition(1, Vector3.Lerp(topo, pe, Mathf.Clamp01(p / 0.2f)));   // o corte DESCE do alto
            it.Linha.widthMultiplier = 0.55f * (1f - p * 0.7f);
            Color c = new Color(0.04f, 0.02f, 0.07f, 0.95f * (1f - p * p));
            it.Linha.startColor = c;
            it.Linha.endColor = c;
            EmitirC(it.Ps, p < 0.3f ? 110f : 0f);
        }

        void UmbraLuz(Item it, EfeitoVisual v)
        {
            float p = ProgC(v);
            it.Go.transform.position = PosDoAlvo(v) + Vector3.up * 1.1f;
            it.R.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, v.Raio * 2f, Mathf.Sqrt(p));
            Pintar(it.R, Hdr(Color.white, 2.5f), 0.8f * (1f - p));
            EmitirC(it.Ps, p < 0.25f ? 200f : 0f);
        }

        // =============================================================================== BROK

        void BrokTempera(Item it, EfeitoVisual v, float e)
        {
            Vector3 pe = NoChao(PosDoAlvo(v));
            float f = Flicker[Passo() % Flicker.Length];
            it.Go.transform.position = pe + Vector3.up * 0.05f;
            it.R2.transform.SetPositionAndRotation(pe + Vector3.up * 0.06f, Inclinacao(pe));
            it.R2.transform.localScale = Vector3.one * (v.Raio * f);
            Pintar(it.R2, Hdr(CorRunaC, 1.8f), 0.8f * e * f);
            EmitirC(it.Ps, 22f * e);
        }

        /// <summary>
        /// Sobe do chao como METAL FUNDIDO que esfria (brasa em HDR -> a madeira e o ferro da peca) em 0,35 s e afunda nos
        /// ultimos 0,4 s; o aro (ou as runas das placas) acende azul e um CLARAO corre a face de ponta a ponta (rapido na
        /// subida, depois no compasso da martelada).
        /// </summary>
        void BrokMuralha(Item it, EfeitoVisual v)
        {
            Vector3 f = v.Pos2 - v.Pos;
            f.y = 0f;
            float raio = f.magnitude;
            Transform t = it.Go.transform;
            t.SetPositionAndRotation(new Vector3(v.Pos.x, NoChao(v.Pos2).y, v.Pos.z),
                raio > 0.01f ? Quaternion.LookRotation(f, Vector3.up) : Quaternion.identity);
            float vivido = v.Duracao - v.Restante;
            float sobe = Mathf.Clamp01(vivido / 0.35f);
            float h = Mathf.Max(Mathf.Min(sobe * (2f - sobe), Mathf.Clamp01(v.Restante / 0.4f)), 0.01f);
            it.R.transform.parent.localScale = new Vector3(1f, h, 1f);   // o CORPO: a peca e o brilho sobem juntos
            float esfria = Mathf.Clamp01(vivido / 1.4f);
            Pintar(it.R, Color.Lerp(Hdr(CorBrasaC, 2.4f), Color.white, esfria * esfria), 1f);
            int passo = Passo();
            Pintar(it.R2, Hdr(CorRunaC, 2f), (vivido < 1.2f ? 1f : 0.7f) * Flicker[passo % Flicker.Length]);
            float ciclo = vivido < 1.2f ? vivido / 1.2f : ((vivido - 1.2f) % 1.5f) / 1.5f;
            if (_cEscudoMeshy)
            {
                // na face do escudo virada para o Brok (a que o jogador ve'), de uma ponta a outra, na altura das runas
                float x = Mathf.Lerp(-0.85f, 0.85f, ciclo) * _cArcoRaio * _cArcoMeia, ze = ZEscudoC() - 0.45f, ye = _cArcoAltura * 0.55f * h;
                it.Linha.SetPosition(0, t.TransformPoint(new Vector3(x - 0.35f, ye, ze)));
                it.Linha.SetPosition(1, t.TransformPoint(new Vector3(x + 0.35f, ye, ze)));
            }
            else
            {
                float ang = Mathf.Lerp(-_cArcoMeia, _cArcoMeia, ciclo);
                float r = raio - _cArcoEsp * 0.5f - 0.08f;   // na face de DENTRO: e' a que o jogador ve' atras do Brok
                float y = _cArcoAltura * 0.55f * h;
                it.Linha.SetPosition(0, t.TransformPoint(new Vector3(Mathf.Sin(ang - 0.14f) * r, y, Mathf.Cos(ang - 0.14f) * r)));
                it.Linha.SetPosition(1, t.TransformPoint(new Vector3(Mathf.Sin(ang + 0.14f) * r, y, Mathf.Cos(ang + 0.14f) * r)));
            }
            it.Linha.startColor = CorLunarC;
            it.Linha.endColor = CorRunaC;
            Pintar(it.Linha, Hdr(Color.white, 2.6f), h);
            EmitirC(it.Ps, vivido < 0.5f ? 220f : 5f);
            EmitirC(it.Ps2, vivido < 1.6f ? 30f * (1f - vivido / 1.6f) : 0f);
        }

        void BrokErguida(Item it, EfeitoVisual v, Pawn dono)
        {
            int passo = Passo();
            bool clang = passo % 6 == 0;   // um clang a cada meio segundo (6 passos de anime)
            Vector3 p = PosDoAlvo(v) + Vector3.up * (clang ? 2.2f : 2.35f);
            Quaternion rot = dono != null ? Quaternion.LookRotation(dono.Frente, Vector3.up) * Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            it.Go.transform.SetPositionAndRotation(p, rot);
            it.R.transform.localScale = Vector3.one * 0.75f;
            Pintar(it.R, clang ? Hdr(CorBrasaC, 1.8f) : Color.white, 1f);
            EmitirC(it.Ps, clang ? 500f : 0f);
        }

        /// <summary>Desce do alto e CRAVA (0,25 s); a cada clang aquece em brasa, o anel acende e a faisca espirra; no fim
        /// racha e afunda. O compasso do clang sai do kit (o mesmo Disparo que os bots ouvem).</summary>
        void BrokBigorna(Item it, EfeitoVisual v, float e)
        {
            float vivido = v.Duracao - v.Restante;
            Vector3 pe = NoChao(v.Pos);
            float queda = Mathf.Clamp01(vivido / 0.25f);
            float afunda = (1f - Mathf.Clamp01((e - LeituraDosKits.Piso) / (1f - LeituraDosKits.Piso))) * 0.9f;
            it.Go.transform.SetPositionAndRotation(pe, Quaternion.Euler(0f, 35f, 0f));
            it.R.transform.localPosition = Vector3.up * ((1f - queda * queda) * 3f - afunda);
            it.R.transform.localScale = Vector3.one * 1.2f;
            float clang = Mathf.Max(Kits.De("13-brok").Suprema["clang"], 0.1f);
            float fase = vivido % clang;
            bool bate = queda >= 1f && fase < 0.15f;
            Pintar(it.R, bate ? Hdr(CorBrasaC, 2f) : Color.white, 1f);
            it.R2.transform.SetPositionAndRotation(pe + Vector3.up * 0.07f, Inclinacao(v.Pos));
            it.R2.transform.localScale = Vector3.one * v.Raio;
            float onda = 1f - fase / clang;   // acende no clang e esmaece ate' o proximo
            Pintar(it.R2, Hdr(CorRunaC, 1.6f + onda), (0.35f + 0.65f * onda) * e);
            EmitirC(it.Ps, bate ? 500f : 4f);
            FormaC(it.Ps2, v.Raio * 1.4f, v.Raio * 1.4f, 0.1f);
            EmitirC(it.Ps2, 30f * e);
        }

        void BrokRuna(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = PosDoAlvo(v);
            int passo = Passo();
            float f = Flicker[passo % Flicker.Length];
            it.Go.transform.position = p;
            it.R.transform.SetPositionAndRotation(p + Vector3.up * 0.5f, Quaternion.Euler(14f, passo % 24 * 15f, 0f));
            it.R.transform.localScale = Vector3.one * v.Raio;
            it.R2.transform.SetPositionAndRotation(p + Vector3.up * 1.3f, Quaternion.Euler(-10f, -(passo % 24) * 15f, 0f));
            it.R2.transform.localScale = Vector3.one * (v.Raio * 0.8f);
            Pintar(it.R, Hdr(CorRunaC, 1.8f), 0.75f * e * f);
            Pintar(it.R2, Hdr(CorRunaC, 1.8f), 0.6f * e * f);
            EmitirC(it.Ps, 16f * e);
        }

        // =============================================================================== GROMM

        void GrommTotem(Item it, EfeitoVisual v, float e)
        {
            float vivido = v.Duracao - v.Restante;
            Vector3 pe = NoChao(v.Pos);
            it.Go.transform.SetPositionAndRotation(pe, Quaternion.identity);
            float finca = Mathf.Min(Mathf.Clamp01(vivido / 0.25f), Mathf.Clamp01(v.Restante / 0.3f));
            it.R.transform.localScale = new Vector3(1f, Mathf.Max(finca, 0.01f), 1f);
            it.R2.transform.SetPositionAndRotation(pe + Vector3.up * 0.06f, Inclinacao(v.Pos));
            it.R2.transform.localScale = Vector3.one * v.Raio;
            Pintar(it.R2, Hdr(CorChuvaC, 1.5f), 0.55f * e * Flicker[Passo() % Flicker.Length]);
            float lado = v.Raio * 1.5f;   // quadrado dentro do circulo da chuva
            FormaC(it.Ps, lado, lado, 0.1f);
            FormaC(it.Ps2, lado * 0.8f, lado * 0.8f, 0.4f);
            var cura = (ParticleSystem)it.Extra[0];
            FormaC(cura, lado, lado, 0.1f);
            EmitirC(it.Ps, 260f * e);
            EmitirC(it.Ps2, 10f * e);
            EmitirC(cura, 24f * e);
        }

        /// <summary>A faixa no chao: o TREMOR do aviso (engrossa e acende ate' o bisao sair) e o RASTRO que ele deixa.</summary>
        void GrommFaixa(Item it, EfeitoVisual v, Color cor, float p, float alfa)
        {
            Vector3 d = v.Pos2 - v.Pos;
            d.y = 0f;
            float comp = d.magnitude;
            Vector3 dir = comp > 0.01f ? d / comp : Vector3.forward;
            it.Go.transform.SetPositionAndRotation(NoChao((v.Pos + v.Pos2) * 0.5f) + Vector3.up * 0.1f, Quaternion.LookRotation(Vector3.up, dir));
            for (int i = 0; i < it.Pontos.Length; i++)
                it.Pontos[i] = NoChao(Vector3.Lerp(v.Pos, v.Pos2, i / (float)(it.Pontos.Length - 1))) + Vector3.up * 0.12f;
            it.Linha.SetPositions(it.Pontos);
            float f = Flicker[Passo() % Flicker.Length];
            it.Linha.widthMultiplier = v.Raio * (0.5f + 0.5f * p) * f;
            it.Linha.startColor = cor;
            it.Linha.endColor = cor;
            Pintar(it.Linha, Hdr(Color.white, 1.6f + 1.4f * p), alfa * (0.55f + 0.45f * f));
            FormaC(it.Ps, v.Raio, Mathf.Max(comp, 0.1f), 0.1f);
            EmitirC(it.Ps, comp * (4f + 26f * p) * alfa);
        }

        void GrommBisao(Item it, EfeitoVisual v, float e)
        {
            Vector3 d = v.Pos2 - v.Pos;
            d.y = 0f;
            int passo = Passo();
            float galope = Mathf.Abs(Mathf.Sin(passo * 0.9f)) * 0.25f;   // em passos de anime, nunca lerp continuo
            Transform t = it.Go.transform;
            t.SetPositionAndRotation(NoChao(v.Pos), d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d, Vector3.up) : Quaternion.identity);
            it.R.transform.localPosition = Vector3.up * galope;
            it.R2.transform.localPosition = Vector3.up * galope;
            Pintar(it.R, Hdr(CorBisaoC, 1.4f), 0.55f * e);
            Pintar(it.R2, Hdr(CorBisaoC, 1.2f), 0.22f * e * Flicker[passo % Flicker.Length]);
            Vector3 a = t.TransformPoint(new Vector3(0f, 2f + galope, 0.9f)), b = t.TransformPoint(new Vector3(0f, 1.6f + galope, -1.1f));
            it.Pontos = LeituraDosKits.PontosDoFio(a, b, it.Pontos.Length, passo * PassoAnime, it.Pontos);
            it.Linha.SetPositions(it.Pontos);
            it.Linha.startColor = CorRaio;
            it.Linha.endColor = Color.white;
            EmitirC(it.Ps, 140f * e);
            EmitirC(it.Ps2, 60f * e);
        }

        // =============================================================================== MARIS

        void MarisViva(Item it, EfeitoVisual v, float e)
        {
            Vector3 p = PosDoAlvo(v);
            p.y = LaminaC(p);
            int passo = Passo();
            float ciclo = (passo % 11) / 10f;   // a ondula abre em passos e recomeca
            it.Go.transform.position = p;
            it.R2.transform.SetPositionAndRotation(p + Vector3.up * 0.04f, Quaternion.identity);
            it.R2.transform.localScale = Vector3.one * (0.3f + 1.1f * ciclo);
            Pintar(it.R2, Hdr(CorAguaC, 1.4f), (1f - ciclo) * 0.8f * e);
            EmitirC(it.Ps, 12f * e);
        }

        void MarisEsfera(Item it, EfeitoVisual v)
        {
            float f = Flicker[Passo() % Flicker.Length];
            it.Go.transform.position = v.Pos;
            float s = v.Raio * 2f;
            it.R.transform.localScale = new Vector3(s * (1.1f - 0.1f * f), s * (0.9f + 0.1f * f), s * (1.1f - 0.1f * f));   // agua balanca
            it.R2.transform.localScale = Vector3.one * (s * 0.55f);
            Pintar(it.R, CorAguaC, 0.75f);
            Pintar(it.R2, Hdr(CorPerolaC, 1.4f), 0.8f * f);
            EmitirC(it.Ps, 40f);
        }

        void MarisColuna(Item it, EfeitoVisual v)
        {
            float vivido = v.Duracao - v.Restante;
            Vector3 pe = PosDoAlvo(v);
            float h = 2.8f * Mathf.Min(Mathf.Clamp01(vivido / 0.15f), Mathf.Clamp01(v.Restante / 0.2f));
            it.Go.transform.position = pe;
            it.R.transform.localPosition = Vector3.up * (h * 0.5f);
            it.R.transform.localScale = new Vector3(1.9f, Mathf.Max(h * 0.5f, 0.01f), 1.9f);   // o cilindro do Unity tem 2 m
            Pintar(it.R, CorAguaC, 0.5f * Flicker[Passo() % Flicker.Length] + 0.1f);
            it.R2.transform.SetPositionAndRotation(pe + Vector3.up * 0.05f, Quaternion.identity);
            it.R2.transform.localScale = Vector3.one * 1.2f;
            Pintar(it.R2, Hdr(CorPerolaC, 1.5f), 0.8f);
            EmitirC(it.Ps, 50f);
            EmitirC(it.Ps2, 40f);
        }

        void MarisRecuo(Item it, EfeitoVisual v)
        {
            float p = ProgC(v);
            Vector3 pe = NoChao(v.Pos);
            float r = Mathf.Max(v.Raio * (1f - 0.9f * p), 0.3f);
            it.Go.transform.SetPositionAndRotation(pe + Vector3.up * 0.1f, Inclinacao(v.Pos));
            it.R.transform.localScale = Vector3.one * r;
            it.R2.transform.localScale = Vector3.one * r;
            Pintar(it.R, Hdr(CorPerolaC, 1.6f), 0.9f);
            Pintar(it.R2, CorAguaC, 0.3f);
            ParticleSystem.ShapeModule sh = it.Ps.shape;
            sh.radius = r;
            EmitirC(it.Ps, 60f);
        }

        /// <summary>A mare CHEGA do centro para a borda (a parede em camera lenta, 0,7 s) a 30 cm do chao e DRENA no fim.</summary>
        void MarisMare(Item it, EfeitoVisual v, float e)
        {
            float vivido = v.Duracao - v.Restante;
            float chega = Mathf.Clamp01(vivido / 0.7f);
            chega = 1f - (1f - chega) * (1f - chega);
            float drena = Mathf.Clamp01((e - LeituraDosKits.Piso) / (1f - LeituraDosKits.Piso));
            float r = Mathf.Max(v.Raio * Mathf.Min(chega, drena), 0.05f);
            Vector3 pe = NoChao(v.Pos) + Vector3.up * 0.3f;
            it.Go.transform.SetPositionAndRotation(pe, Inclinacao(v.Pos));
            it.R2.transform.localScale = Vector3.one * r;
            it.R.transform.localScale = Vector3.one * r;
            Pintar(it.R2, CorAguaC, 0.5f);
            Pintar(it.R, CorPerolaC, 0.85f * Flicker[Passo() % Flicker.Length]);
            FormaC(it.Ps, r * 1.4f, r * 1.4f, 0.1f);
            ParticleSystem.ShapeModule sh = it.Ps2.shape;
            sh.radius = r;
            EmitirC(it.Ps, 50f * drena);
            EmitirC(it.Ps2, chega < 1f ? 220f : 40f * drena);
        }

        void MarisChoque(Item it, EfeitoVisual v, float e)
        {
            Vector3 c = NoChao(v.Pos) + Vector3.up * 0.4f;
            int passo = Passo();
            it.Go.transform.position = c;
            // dois pontos da borda sorteados POR PASSO (deterministico, sem Random): o arco pula de lugar como raio
            float a1 = HashC(passo, 1) * Mathf.PI * 2f, a2 = a1 + Mathf.PI * (0.6f + 0.8f * HashC(passo, 2));
            Vector3 a = c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * (v.Raio * 0.9f);
            Vector3 b = c + new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2)) * (v.Raio * 0.9f);
            Vector3 lado = Vector3.Cross(b - a, Vector3.up).normalized;
            int n = it.Pontos.Length;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float env = Mathf.Sin(t * Mathf.PI);
                it.Pontos[i] = Vector3.Lerp(a, b, t) + lado * ((HashC(passo, i + 3) - 0.5f) * 2.4f * env) + Vector3.up * (HashC(passo, i + 20) * 0.5f * env);
            }
            it.Linha.SetPositions(it.Pontos);
            it.Linha.widthMultiplier = 0.25f * Flicker[passo % Flicker.Length];
            it.Linha.startColor = CorRaio;
            it.Linha.endColor = Color.white;
            Pintar(it.Linha, Hdr(Color.white, 2.2f), e);
            it.R.transform.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.35f, Inclinacao(v.Pos));
            it.R.transform.localScale = Vector3.one * v.Raio;
            Pintar(it.R, Hdr(CorRaio, 1.8f), e * Flicker[(passo + 1) % Flicker.Length]);
            FormaC(it.Ps, v.Raio * 1.4f, v.Raio * 1.4f, 0.1f);
            EmitirC(it.Ps, 150f * e);
        }

        // =============================================================================== ferramentas do grupo

        static void MateriaisC()
        {
            if (_cFumaca != null) return;
            // ALFA com o ponto macio (fumaca que ESCURECE) e com a faixa macia (a tinta preta da Umbra): aditivo so' clareia
            _cFumaca = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
            _cTinta = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.FaixaSuave());
        }

        static ParticleSystem FumacaC(Transform pai, string nome, Color a, Color b, Vector2 vida, Vector2 vel, Vector2 tam, bool mundo, float aleatorio, int max)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, nome, a, b, 0f, vida, vel, tam, mundo, aleatorio, max);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _cFumaca;
            return ps;
        }

        /// <summary>Estouro curto: anel que abre no chao + rajada de particula (taxa alta so' no primeiro terco da vida).</summary>
        void NovoEstouroC(Item it, Transform raiz, Color a, Color b, float gravidade, float vel)
        {
            it.R2 = Peca(raiz, MalhaVfx.Anel(), _mFeixe);
            it.Ps = ParticulaVfx.Novo(raiz, "Estouro", a, b, 0f, new Vector2(0.35f, 0.8f), new Vector2(vel * 0.4f, vel),
                new Vector2(0.06f, 0.2f), true, 0.9f, 80);
            GravidadeC(it.Ps, gravidade);
            FormaC(it.Ps, 0.4f, 0.4f, 0.4f);
        }

        void EstouroC(Item it, EfeitoVisual v, Color anel)
        {
            float p = ProgC(v);
            Vector3 pe = NoChao(v.Pos);
            it.Go.transform.position = v.Pos;
            it.R2.transform.SetPositionAndRotation(pe + Vector3.up * 0.08f, Inclinacao(v.Pos));
            it.R2.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, v.Raio, 1f - (1f - p) * (1f - p));
            Pintar(it.R2, Hdr(anel, 1.8f), 1f - p);
            EmitirC(it.Ps, p < 0.3f ? 260f : 0f);
        }

        static void EmitirC(ParticleSystem ps, float taxa)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTimeMultiplier = taxa;
        }

        /// <summary>Caixa de emissao: x e y no plano do emissor, z na vertical dele (o Novo gira -90 em X).</summary>
        static void FormaC(ParticleSystem ps, float x, float y, float z)
        {
            if (ps == null) return;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.scale = new Vector3(x, y, z);
        }

        static void GravidadeC(ParticleSystem ps, float g)
        {
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = g;
        }

        static float ProgC(EfeitoVisual v) => v.Duracao > 0f ? Mathf.Clamp01(1f - v.Restante / v.Duracao) : 1f;

        /// <summary>0..1 deterministico por (passo, i): o raio pula igual em replay e em teste.</summary>
        static float HashC(int passo, int i)
        {
            float x = Mathf.Sin(passo * 12.9898f + i * 78.233f) * 43758.547f;
            return x - Mathf.Floor(x);
        }

        /// <summary>A diagonal da lamina cruza a TELA (a direita da camera); sem camera, o X do mundo.</summary>
        static Vector3 LadoDaCameraC()
        {
            Camera c = Camera.main;
            return c != null ? c.transform.right : Vector3.right;
        }

        /// <summary>Onde a Maris pisa: o chao, ou a lamina do lago se ela estiver nadando por cima.</summary>
        static float LaminaC(Vector3 p)
        {
            float y = NoChao(p).y;
            if (Ilha.Atual == null) return Mathf.Max(y, p.y);
            float s = Ilha.SuperficieDaAgua(p.x, p.z);   // a agua de qualquer ilha (a do Documento Mestre deixa o Relevo nulo)
            return s > y ? s : y;
        }

        // =============================================================================== malhas (1x, zero arquivo)

        /// <summary>O arco da muralha sai da FICHA (Kits.De("13-brok")): desenho e logica medem o mesmo arco.</summary>
        static void MedirArcoC()
        {
            if (_cArcoRaio > 0f) return;
            Dictionary<string, float> t = Kits.De("13-brok").Tatica;
            _cArcoRaio = t["raio_arco"];
            _cArcoMeia = t["comprimento"] * 0.5f / _cArcoRaio;
            _cArcoAltura = t["altura"];
            _cArcoEsp = t["espessura"];
        }

        /// <summary>m do centro do arco ate' o escudo plano: a corda MEDIA (meio caminho entre a corda e o apice do arco).</summary>
        static float ZEscudoC() => _cArcoRaio * (1f + Mathf.Cos(_cArcoMeia)) * 0.5f;

        const int PlacasBrokC = 7;

        /// <summary>Sete placas de bronze alternando altura (le' ESCUDO, nao parede de casa), inclinadas para o Brok (braco
        /// escorado). Material do mundo (le' cor de vertice e recebe luz): o bronze tem face. Reserva: so' sem o 44-escudo-brok.glb.</summary>
        static Mesh MalhaArcoBrok()
        {
            if (_cArco != null) return _cArco;
            MedirArcoC();
            var laje = new MalhaProc.Construtor();
            laje.Caixa(new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0.5f), Color.white);
            var b = new MalhaProc.Construtor();
            float passo = _cArcoMeia * 2f / PlacasBrokC;
            for (int i = 0; i < PlacasBrokC; i++)
            {
                float a = -_cArcoMeia + passo * (i + 0.5f);
                Vector3 pos = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * _cArcoRaio;
                float alto = _cArcoAltura * (i % 2 == 0 ? 1f : 0.88f);
                Quaternion rot = Quaternion.Euler(-7f, a * Mathf.Rad2Deg, 0f);
                b.Adicionar(laje, Matrix4x4.TRS(pos, rot, new Vector3(passo * _cArcoRaio * 0.94f, alto, _cArcoEsp)), i % 2 == 0 ? CorBronzeC : CorBronzeEscuroC);
            }
            return _cArco = b.ParaMesh("BrokMuralha", QualitySettings.activeColorSpace == ColorSpace.Linear);
        }

        /// <summary>Um glifo por placa nas DUAS faces (o inimigo ve' a de fora, o jogador a de dentro): haste + losango + braco.</summary>
        static Mesh MalhaRunasBrok()
        {
            if (_cRunas != null) return _cRunas;
            MedirArcoC();
            var b = new MalhaProc.Construtor();
            float passo = _cArcoMeia * 2f / PlacasBrokC;
            for (int i = 0; i < PlacasBrokC; i++)
            {
                float a = -_cArcoMeia + passo * (i + 0.5f);
                Vector3 radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)), tang = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
                for (int lado = -1; lado <= 1; lado += 2)
                    RunaC(b, radial * (_cArcoRaio + lado * (_cArcoEsp * 0.5f + 0.04f)) + Vector3.up * (_cArcoAltura * 0.55f), tang, radial * lado, 0.3f, i);
            }
            return _cRunas = b.ParaMesh("BrokRunas");
        }

        static void RunaC(MalhaProc.Construtor b, Vector3 c, Vector3 lado, Vector3 fora, float t, int i)
        {
            Vector3 up = Vector3.up;
            QuadC(b, c - up * t * 1.3f - lado * 0.045f, c - up * t * 1.3f + lado * 0.045f, c + up * t * 1.3f + lado * 0.045f, c + up * t * 1.3f - lado * 0.045f, fora);
            Vector3 m = c + up * t * 0.45f;
            b.Tri(m + up * t * 0.45f, m + lado * t * 0.42f, m - up * t * 0.45f, Color.white, fora);
            b.Tri(m + up * t * 0.45f, m - up * t * 0.45f, m - lado * t * 0.42f, Color.white, fora);
            float s = i % 2 == 0 ? 1f : -1f;   // placa sim, placa nao: o braco troca de lado (runas diferentes)
            Vector3 p0 = c - up * t, p1 = c - up * t * 0.3f + lado * (t * 0.65f * s);
            QuadC(b, p0, p0 + up * 0.08f, p1 + up * 0.08f, p1, fora);
        }

        static void QuadC(MalhaProc.Construtor b, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 fora)
        {
            b.Tri(p0, p1, p2, Color.white, fora);
            b.Tri(p0, p2, p3, Color.white, fora);
        }

        /// <summary>Bigorna: pe' de bronze, cintura, mesa de ferro, CHIFRE (a silhueta que diz bigorna) e calcanhar.</summary>
        static Mesh MalhaBigornaBrok()
        {
            if (_cBigorna != null) return _cBigorna;
            var b = new MalhaProc.Construtor();
            Color ferro = new Color(0.32f, 0.3f, 0.33f), escuro = new Color(0.2f, 0.19f, 0.21f);
            b.Caixa(new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.12f, 0.32f), CorBronzeC);
            b.Caixa(new Vector3(0f, 0.36f, 0f), new Vector3(0.2f, 0.14f, 0.18f), escuro);
            b.Caixa(new Vector3(-0.05f, 0.64f, 0f), new Vector3(0.52f, 0.14f, 0.26f), ferro);
            Vector3 t0 = new Vector3(0.47f, 0.78f, 0.2f), t1 = new Vector3(0.47f, 0.78f, -0.2f);
            Vector3 t2 = new Vector3(0.47f, 0.52f, -0.12f), t3 = new Vector3(0.47f, 0.52f, 0.12f), bico = new Vector3(1.05f, 0.74f, 0f);
            b.Tri(t0, t1, bico, ferro, Vector3.up);
            b.Tri(t1, t2, bico, escuro, Vector3.back);
            b.Tri(t2, t3, bico, escuro, Vector3.down);
            b.Tri(t3, t0, bico, escuro, Vector3.forward);
            b.Caixa(new Vector3(-0.66f, 0.7f, 0f), new Vector3(0.1f, 0.08f, 0.2f), ferro);
            return _cBigorna = b.ParaMesh("BrokBigorna", QualitySettings.activeColorSpace == ColorSpace.Linear);
        }

        /// <summary>Totem: poste de madeira, faixas pintadas (osso/oliva), CRANIO DE BISAO com chifres e coroa de raios.</summary>
        static Mesh MalhaTotemGromm()
        {
            if (_cTotem != null) return _cTotem;
            var b = new MalhaProc.Construtor();
            Color madeira = new Color32(0x6B, 0x4A, 0x2B, 255), clara = new Color32(0x8C, 0x66, 0x3E, 255), osso = new Color32(0xE8, 0xE0, 0xC8, 255);
            b.Tronco(Vector3.zero, 0.2f, 0.15f, 2.3f, 6, madeira, clara);
            for (int i = 0; i < 3; i++) b.Tronco(new Vector3(0f, 0.55f + i * 0.55f, 0f), 0.25f, 0.25f, 0.14f, 6, i == 1 ? CorOlivaC : osso);
            b.Caixa(new Vector3(0f, 2.45f, 0.04f), new Vector3(0.26f, 0.17f, 0.2f), osso);
            b.Caixa(new Vector3(0f, 2.36f, 0.26f), new Vector3(0.15f, 0.1f, 0.1f), osso);
            ChifreC(b, new Vector3(0.24f, 2.52f, 0.05f), new Vector3(0.62f, 2.8f, 0.1f), osso);
            ChifreC(b, new Vector3(-0.24f, 2.52f, 0.05f), new Vector3(-0.62f, 2.8f, 0.1f), osso);
            for (int i = -1; i <= 1; i++)
                ChifreC(b, new Vector3(i * 0.12f, 2.6f, 0f), new Vector3(i * 0.22f, i == 0 ? 3.2f : 3.02f, 0f), CorRaio);
            return _cTotem = b.ParaMesh("GrommTotem", QualitySettings.activeColorSpace == ColorSpace.Linear);
        }

        /// <summary>Bisao de caixas (tronco, CORCOVA, cabeca baixa, focinho, chifres, patas, rabo): 3 m, cabeca em +Z. Cor
        /// branca: quem tinge e' o material aditivo — sobreposicao de caixa acende, e isso le' ENERGIA.</summary>
        static Mesh MalhaBisaoGromm()
        {
            if (_cBisao != null) return _cBisao;
            var b = new MalhaProc.Construtor();
            Color c = Color.white;
            b.Caixa(new Vector3(0f, 1.05f, -0.2f), new Vector3(0.5f, 0.42f, 0.85f), c);
            b.Caixa(new Vector3(0f, 1.5f, 0.35f), new Vector3(0.55f, 0.45f, 0.5f), c);
            b.Caixa(new Vector3(0f, 1.05f, 1.05f), new Vector3(0.34f, 0.34f, 0.3f), c);
            b.Caixa(new Vector3(0f, 0.82f, 1.38f), new Vector3(0.24f, 0.2f, 0.14f), c);
            ChifreC(b, new Vector3(0.3f, 1.25f, 1.1f), new Vector3(0.62f, 1.55f, 1.25f), c);
            ChifreC(b, new Vector3(-0.3f, 1.25f, 1.1f), new Vector3(-0.62f, 1.55f, 1.25f), c);
            for (int i = 0; i < 4; i++)
                b.Caixa(new Vector3(i % 2 == 0 ? 0.3f : -0.3f, 0.34f, i < 2 ? 0.55f : -0.75f), new Vector3(0.12f, 0.34f, 0.12f), c);
            b.Caixa(new Vector3(0f, 1.2f, -1.12f), new Vector3(0.06f, 0.25f, 0.06f), c);
            return _cBisao = b.ParaMesh("GrommBisao");
        }

        /// <summary>Piramide de 4 faces da raiz a' ponta (chifre, ponta de raio).</summary>
        static void ChifreC(MalhaProc.Construtor b, Vector3 raiz, Vector3 ponta, Color cor)
        {
            const float r = 0.07f;
            Vector3 d = (ponta - raiz).normalized;
            Vector3 u = Vector3.Cross(d, Vector3.forward);
            if (u.sqrMagnitude < 1e-4f) u = Vector3.Cross(d, Vector3.right);
            u = u.normalized * r;
            Vector3 w = Vector3.Cross(d, u).normalized * r;
            Vector3 a0 = raiz + u, a1 = raiz + w, a2 = raiz - u, a3 = raiz - w;
            b.Tri(a0, a1, ponta, cor, u + w);
            b.Tri(a1, a2, ponta, cor, w - u);
            b.Tri(a2, a3, ponta, cor, -u - w);
            b.Tri(a3, a0, ponta, cor, u - w);
        }
    }
}
