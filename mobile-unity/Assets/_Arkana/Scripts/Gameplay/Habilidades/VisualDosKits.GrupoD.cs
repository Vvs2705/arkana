using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Characters;
using Arkana.Terrain;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O DESENHO DO GRUPO D (16 Fizz, 17 Sylva, 18 Basalto, 19 Noctus, 20 Pip) pelos ganchos parciais da casca: um Item por
    /// EfeitoVisual, do mesmo pool (R/R2/Linha/Ps/Ps2 e as pecas a mais em Extra — o Mostrar liga e desliga todas). Tipos com o
    /// prefixo do mago. A torreta, a bobina (e a sucata delas) e as placas do Monolito sao as PECAS DA MESHY da onda 11
    /// (PecaDaMeshy: textura no URP Lit); sem o .glb, e nas outras pecas solidas (broto, moita, lascas, nuvem), primitivas
    /// COMBINADAS numa malha so' com cor de vertice, no Particles/Simple Lit (le' cor de vertice, recebe a luz
    /// do sol — em Build.ShadersDoCodigo); magia e' aditiva HDR (acende no bloom); fumaca, poeira e nevoa sao ALFA (aditivo
    /// cinza vira brilho). Sem luz dinamica, sem lixo por quadro: pontos de raio num buffer do Item, cor por MaterialPropertyBlock.
    /// Leitura por mago (EstadoGrupoD): o cabelo-estacao da Sylva (flor -> outono pela vida; cinza com a seiva gasta) e o fio
    /// de seiva ate' o vinculado. ponytail: o resto dos corpos (a nevoa que DISSOLVE o Noctus, a helice da Pip, as molas do
    /// Fizz) pede os ossos do modelo — hoje o efeito cobre o corpo em vez de troca-lo.
    /// </summary>
    public sealed partial class VisualDosKits
    {
        const string GdPropShader = "Universal Render Pipeline/Particles/Simple Lit";
        static Material _gdProp, _gdAlfa, _gdBrilho, _gdFumaca, _gdPedra;
        static Mesh _gdTorreta, _gdBobina, _gdBroto, _gdMoita, _gdLascas, _gdPlacas, _gdRaizes, _gdNuvem;
        static float _gdConeFizz = -1f, _gdFlorescer = -1f, _gdAlturaNuvem = -1f;

        static readonly Color GdCobre = new Color32(0xB0, 0x70, 0x30, 255);
        static readonly Color GdFaisca = new Color32(0xF5, 0xD9, 0x0A, 255);
        static readonly Color GdAzul = new Color32(0x2A, 0xA7, 0xFF, 255);
        static readonly Color GdFolha = new Color32(0x3E, 0x7A, 0x3A, 255);
        static readonly Color GdSeiva = new Color32(0xF0, 0xC7, 0x5E, 255);
        static readonly Color GdFlor = new Color32(0xE4, 0x8A, 0xB0, 255);
        static readonly Color GdBasalto = new Color32(0x4A, 0x4A, 0x50, 255);
        static readonly Color GdMagma = new Color32(0xFF, 0x5A, 0x2A, 255);
        static readonly Color GdCarmesim = new Color32(0x8B, 0x1E, 0x2E, 255);
        static readonly Color GdVioleta = new Color32(0x8A, 0x5C, 0xF0, 255);
        /// <summary>Sucata: o Lit da peca x este tom = cobre queimado. KNOB por foto.</summary>
        static readonly Color GdCorSucata = new Color(0.3f, 0.25f, 0.22f, 1f);

        /// <summary>As pecas da Meshy (onda 11) em Resources; sem o .glb, a primitiva combinada de antes.</summary>
        const string GdGlbTorreta = "42-torreta-fizz", GdGlbBobina = "43-bobina-fizz", GdGlbMonolito = "41-monolito-basalto";
        /// <summary>A LENTE acesa da torreta e o centro da ESFERA da bobina no .glb (medidos nos vertices laranja/azuis da
        /// textura): o farol e a coroa acendem por cima delas. KNOB se a Meshy refizer a peca.</summary>
        static readonly Vector3 GdLenteTorreta = new Vector3(0f, 0.55f, 0.5f);
        const float GdEsferaBobina = 2.3f;

        /// <summary>Os dois efeitos por Sylva (chave estavel no pool, como o braco da Pyra). Restante 1 fixo: quem liga e' a leitura.</summary>
        sealed class GdSylva
        {
            public float Altura;
            public readonly EfeitoVisual Cabelo = new EfeitoVisual("sylva_cabelo", Vector3.zero, Vector3.zero, 0f, 1f);
            public readonly EfeitoVisual Seiva = new EfeitoVisual("sylva_seiva", Vector3.zero, Vector3.zero, 0f, 1f);
        }

        readonly Dictionary<Pawn, GdSylva> _gdSylvas = new Dictionary<Pawn, GdSylva>();

        // ============================================================================ leitura por mago

        partial void EstadoGrupoD(Pawn dono)
        {
            Sylva s = dono.Runner.Impl as Sylva;
            if (s == null || !dono.Viva) return;
            GdSylva d;
            if (!_gdSylvas.TryGetValue(dono, out d)) { d = new GdSylva { Altura = IdentidadeMago.De(dono.Slug).AlturaM }; _gdSylvas[dono] = d; }
            d.Cabelo.Pos = dono.Pos + Vector3.up * (d.Altura * 0.93f);
            Desenhar(d.Cabelo, dono);
            IEntidade a = s.Vinculado;
            if (a == null) return;
            d.Seiva.Pos = dono.Pos + Vector3.up * 1.2f;
            d.Seiva.Pos2 = a.Pos + Vector3.up * 1.1f;
            Desenhar(d.Seiva, dono);
        }

        // ============================================================================ criar (1x por Item do pool)

        partial void NovoGrupoD(Item it, Transform raiz, string tipo, ref bool feito)
        {
            if (!tipo.StartsWith("fizz_") && !tipo.StartsWith("sylva_") && !tipo.StartsWith("basalto_")
                && !tipo.StartsWith("noctus_") && !tipo.StartsWith("pip_")) return;
            GdMateriais();
            feito = true;
            switch (tipo)
            {
                // ---------------------------------------------------------------- FIZZ
                case "fizz_torreta":
                {
                    Mesh m = GdMalhaFizz(false, out Material mat, out float k);
                    it.R = Peca(raiz, m, mat);
                    it.R2 = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _gdBrilho);   // o FAROL que pisca
                    if (k > 0f)
                    {
                        // a cupula da Meshy: o farol acende NA LENTE (o olho que mira o cone)
                        it.R.transform.localScale = Vector3.one * k;
                        it.R2.transform.localPosition = GdLenteTorreta * k;
                        it.R2.transform.localScale = Vector3.one * 0.26f;
                    }
                    else
                    {
                        it.R2.transform.localPosition = new Vector3(0f, 1.2f, -0.05f);
                        it.R2.transform.localScale = Vector3.one * 0.17f;
                    }
                    it.Linha = Linha(it.Go, 0.06f);   // o CONE no chao: o limitador (flanqueavel) se le'
                    it.Pontos = new Vector3[3];
                    it.Linha.positionCount = 3;
                    it.Linha.numCapVertices = 0;
                    break;
                }
                case "fizz_faisca":
                case "pip_faisca":
                    it.Linha = Linha(it.Go, 0.12f);
                    break;
                case "fizz_raio":
                case "pip_raio":
                    it.Linha = Linha(it.Go, tipo == "fizz_raio" ? 0.35f : 0.14f);
                    it.R = Peca(raiz, MalhaVfx.Primitiva(PrimitiveType.Sphere), _gdBrilho);   // o clarao da chegada
                    it.Ps = ParticulaVfx.Novo(raiz, "Faiscas", Color.white, tipo == "fizz_raio" ? GdAzul : GdFaisca, 60f,
                        new Vector2(0.15f, 0.4f), new Vector2(3f, 8f), new Vector2(0.05f, 0.14f), true, 1f, 90);
                    break;
                case "fizz_bobina":
                {
                    // a COLUNA leva a peca, a coroa e os arcos juntos: "arma peca a peca" = a coluna sobe do chao em degraus
                    Transform col = new GameObject("Coluna").transform;
                    col.SetParent(raiz, false);
                    Mesh m = GdMalhaFizz(true, out Material mat, out float k);
                    it.R = Peca(col, m, mat);
                    if (k > 0f) it.R.transform.localScale = Vector3.one * k;
                    float topo = k > 0f ? GdEsferaBobina * k : 2.4f;   // a coroa acende em cima da esfera azul da Meshy
                    it.R2 = Peca(col, MalhaVfx.Primitiva(PrimitiveType.Sphere), _gdBrilho);   // a coroa que carrega
                    it.R2.transform.localPosition = new Vector3(0f, topo, 0f);
                    it.Linha = Linha(it.Go, 0.09f);   // o ARCO que salta da coroa ao chao, mais forte a cada passo
                    it.Pontos = new Vector3[8];
                    it.Linha.positionCount = 8;
                    it.Ps = ParticulaVfx.Novo(col, "Arcos", Color.white, GdAzul, 30f,
                        new Vector2(0.1f, 0.3f), new Vector2(1.5f, 4f), new Vector2(0.05f, 0.12f), false, 1f, 160);
                    it.Ps.transform.localPosition = new Vector3(0f, topo, 0f);
                    ParticleSystem.ShapeModule shb = it.Ps.shape;
                    shb.scale = new Vector3(0.9f, 0.9f, 0.9f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Pingos", GdFaisca, GdCobre, 12f,
                        new Vector2(0.4f, 0.9f), new Vector2(0.2f, 1f), new Vector2(0.04f, 0.08f), true, 0.6f, 40);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    GdGravidade(it.Ps2, 1f);
                    break;
                }
                case "fizz_mira":
                    it.Linha = Linha(it.Go, 0.05f);   // o LASER: quem vai levar ve' de onde vem (e foge do alcance)
                    it.Linha.positionCount = 2;
                    it.R = Peca(raiz, MalhaVfx.Anel(), _gdBrilho);
                    break;
                case "fizz_sucata":
                    it.R = Peca(raiz, MalhaVfx.Disco(), _gdAlfa);   // a mancha de queimado
                    // a PECA vira sucata: a torreta (R2) ou a bobina (Extra), cada uma num pivo na borda do pe' (tomba ali)
                    it.R2 = GdSucata(raiz, false);
                    it.Extra = new Component[] { GdSucata(raiz, true) };
                    it.Ps = GdFumaca(raiz, "Fumaca", new Color(0.35f, 0.33f, 0.3f, 0.55f), new Color(0.2f, 0.19f, 0.18f, 0.4f), 14f);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Brasas", GdFaisca, GdMagma, 16f,
                        new Vector2(0.3f, 0.8f), new Vector2(0.5f, 2.2f), new Vector2(0.04f, 0.09f), true, 0.8f, 40);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;
                case "fizz_mola":
                    it.R = Peca(raiz, MalhaVfx.Anel(), _gdBrilho);
                    it.Ps = ParticulaVfx.Novo(raiz, "Mola", Color.white, GdFaisca, 160f,
                        new Vector2(0.15f, 0.35f), new Vector2(2f, 5f), new Vector2(0.04f, 0.1f), true, 1f, 60);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                    ParticleSystem.ShapeModule shf = it.Ps.shape;
                    shf.scale = new Vector3(0.4f, 0.4f, 0.1f);
                    break;

                // ---------------------------------------------------------------- SYLVA
                case "sylva_broto":
                    it.R = Peca(raiz, GdMalhaBroto(), _gdProp);
                    it.R2 = Peca(raiz, MalhaVfx.Disco(), _gdAlfa);   // a area do polen
                    it.R2.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    it.Ps = ParticulaVfx.Novo(raiz, "Polen", GdSeiva, new Color(1f, 0.95f, 0.6f), 0f,
                        new Vector2(1.2f, 2.4f), new Vector2(0.15f, 0.5f), new Vector2(0.06f, 0.13f), true, 0.5f, 60);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                    break;
                case "sylva_moita":
                    it.R = Peca(raiz, GdMalhaMoita(), _gdProp);
                    break;
                case "sylva_queima":
                    it.Ps = ParticulaVfx.Fogo(raiz, "Queima", 40f, true);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                    ParticleSystem.ShapeModule shq = it.Ps.shape;
                    shq.scale = new Vector3(0.6f, 0.6f, 0.5f);
                    it.Ps2 = GdFumaca(it.Ps.transform, "Cinza", new Color(0.3f, 0.3f, 0.3f, 0.5f), new Color(0.15f, 0.15f, 0.15f, 0.4f), 10f);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;
                case "sylva_raizes":
                    it.R = Peca(raiz, MalhaVfx.Disco(), _gdAlfa);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _gdBrilho);
                    it.R2.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    it.Linha = Linha(it.Go, 0.2f);   // as RAIZES: raios dourados do centro a' borda, em passos (xilogravura)
                    it.Pontos = new Vector3[25];
                    it.Linha.positionCount = 25;
                    it.Linha.numCapVertices = 0;
                    it.Ps = ParticulaVfx.Novo(raiz, "Seiva", GdSeiva, new Color(1f, 0.9f, 0.5f), 40f,
                        new Vector2(1.2f, 2.2f), new Vector2(0.3f, 0.9f), new Vector2(0.06f, 0.14f), true, 0.3f, 160);
                    break;
                case "sylva_agarra":
                    it.R = Peca(raiz, GdMalhaRaizes(), _gdProp);
                    it.Ps = ParticulaVfx.Novo(raiz, "Terra", GdSeiva, new Color(0.45f, 0.3f, 0.15f), 50f,
                        new Vector2(0.3f, 0.6f), new Vector2(1f, 2.5f), new Vector2(0.05f, 0.12f), true, 0.7f, 40);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                    ParticleSystem.ShapeModule shg = it.Ps.shape;
                    shg.scale = new Vector3(1f, 1f, 0.5f);
                    break;
                case "sylva_seiva":
                    it.Linha = Linha(it.Go, 0.08f);
                    it.Pontos = new Vector3[10];
                    it.Linha.positionCount = 10;
                    break;
                case "sylva_cabelo":
                    // o CABELO-ESTACAO: petalas girando na cabeca, a cor e' a vida (flor -> outono); LOCAL: vai com ela
                    it.Ps = ParticulaVfx.Novo(raiz, "Estacao", GdFlor, GdFolha, 9f,
                        new Vector2(0.8f, 1.4f), new Vector2(0.05f, 0.25f), new Vector2(0.07f, 0.13f), false, 0.8f, 24);
                    ParticleSystem.ShapeModule shc = it.Ps.shape;
                    shc.scale = new Vector3(0.5f, 0.5f, 0.35f);
                    break;

                // ---------------------------------------------------------------- BASALTO
                case "basalto_onda":
                    it.R = Peca(raiz, GdMalhaLascas(), _gdProp);   // a CRISTA de pedra na frente da onda
                    it.Linha = Linha(it.Go, 0.22f);                 // a racha de magma que o punho abre
                    it.Pontos = new Vector3[10];
                    it.Linha.positionCount = 10;
                    it.Ps = GdFumaca(raiz, "Poeira", new Color(0.55f, 0.5f, 0.42f, 0.55f), new Color(0.35f, 0.32f, 0.3f, 0.45f), 0f);
                    it.Ps2 = GdPedras(it.Ps.transform, "Pedrisco", 0f, 1.2f);
                    it.Ps2.transform.localRotation = Quaternion.identity;   // ja' herda a caixa da poeira
                    break;
                case "basalto_monolito":
                {
                    // as PLACAS numa raiz propria: o desenho sobe a raiz e as seis vao juntas
                    Transform placas = new GameObject("Placas").transform;
                    placas.SetParent(raiz, false);
                    Mesh m;
                    Material mat;
                    if (PecaDaMeshy.Carregar(GdGlbMonolito, out m, out mat))
                    {
                        // o MONOLITO da Meshy x 6 no anel de antes (0,8 m), achatado em placa (0,64 x 0,4): o Basalto aparece
                        // nas frestas. R = a 1a, as outras 5 em Extra (o Mostrar liga e desliga).
                        Bounds b = m.bounds;
                        var outras = new Component[5];
                        for (int i = 0; i < 6; i++)
                        {
                            Quaternion q = Quaternion.Euler(0f, i * 60f, 0f);
                            Renderer r = Peca(placas, m, mat);
                            r.transform.localPosition = q * new Vector3(0f, 0f, 0.8f);
                            r.transform.localRotation = q * Quaternion.Euler(-6f, 0f, 0f);   // inclinadas para o corpo
                            r.transform.localScale = new Vector3(0.64f / Mathf.Max(b.size.x, 0.01f),
                                (2.5f - 0.2f * (i % 2)) / Mathf.Max(b.size.y, 0.01f), 0.4f / Mathf.Max(b.size.z, 0.01f));
                            if (i == 0) it.R = r; else outras[i - 1] = r;
                        }
                        it.Extra = outras;
                    }
                    else it.R = Peca(placas, GdMalhaPlacas(), _gdProp);
                    it.R2 = Peca(raiz, MalhaVfx.Anel(), _gdBrilho);   // as runas acesas na base
                    it.R2.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                    it.Ps = ParticulaVfx.Novo(raiz, "Brasas", GdSeiva, GdMagma, 40f,
                        new Vector2(0.6f, 1.2f), new Vector2(0.6f, 1.8f), new Vector2(0.05f, 0.11f), false, 0.3f, 90);
                    it.Ps.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    ParticleSystem.ShapeModule shm = it.Ps.shape;
                    shm.scale = new Vector3(1.6f, 1.6f, 2.4f);
                    break;
                }
                case "basalto_estilhaco":
                    it.Ps = GdPedras(raiz, "Estilhacos", 0f, 0.8f);
                    ParticleSystem.MainModule me = it.Ps.main;
                    me.startSpeed = new ParticleSystem.MinMaxCurve(7f, 13f);
                    ParticleSystem.ShapeModule she = it.Ps.shape;
                    she.shapeType = ParticleSystemShapeType.Cone;
                    she.angle = 30f;
                    she.radius = 0.3f;
                    break;
                case "basalto_avalanche":
                    it.Ps = GdPedras(raiz, "Avalanche", 0f, 1.4f);
                    it.Ps.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                    ParticleSystem.ShapeModule sha = it.Ps.shape;
                    sha.scale = new Vector3(1.4f, 1.4f, 2.2f);
                    it.Ps2 = GdFumaca(it.Ps.transform, "Poeira", new Color(0.5f, 0.47f, 0.42f, 0.5f), new Color(0.3f, 0.3f, 0.3f, 0.4f), 0f);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;

                // ---------------------------------------------------------------- NOCTUS
                case "noctus_investida":
                    it.Linha = Linha(it.Go, 0.9f);   // o risco carmesim da investida
                    it.Linha.positionCount = 2;
                    it.Ps = GdFumaca(raiz, "Nevoa", new Color(0.55f, 0.1f, 0.18f, 0.55f), new Color(0.54f, 0.36f, 0.94f, 0.35f), 0f);
                    break;
                case "noctus_dreno":
                    it.Linha = Linha(it.Go, 0.1f);   // o fio de eter que corre do alvo para ele
                    it.Pontos = new Vector3[10];
                    it.Linha.positionCount = 10;
                    it.Ps = ParticulaVfx.Novo(raiz, "Eter", Color.white, GdVioleta, 25f,
                        new Vector2(0.3f, 0.7f), new Vector2(0.5f, 1.5f), new Vector2(0.05f, 0.11f), true, 1f, 40);
                    break;
                case "noctus_marca":
                    it.R = Peca(raiz, MalhaVfx.Anel(), _gdBrilho);   // o sigilo violeta sobre a cabeca do marcado
                    it.Ps = ParticulaVfx.Novo(raiz, "Sigilo", GdVioleta, GdCarmesim, 10f,
                        new Vector2(0.5f, 1f), new Vector2(-0.8f, -0.3f), new Vector2(0.04f, 0.08f), false, 0.3f, 20);
                    break;
                case "noctus_nevoa":
                    // SO' nevoa (sem casulo solido: capsula translucida le' PILULA — a queixa do eco da Veu, foto 19 de 12/09)
                    it.Ps = GdFumaca(raiz, "Nevoa", new Color(0.55f, 0.12f, 0.2f, 0.6f), new Color(0.54f, 0.36f, 0.94f, 0.4f), 0f);
                    ParticleSystem.MainModule mn = it.Ps.main;
                    mn.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
                    mn.maxParticles = 160;
                    it.Ps.transform.localPosition = new Vector3(0f, 1f, 0f);
                    ParticleSystem.ShapeModule shn = it.Ps.shape;
                    shn.scale = new Vector3(1.1f, 1.1f, 1.9f);
                    it.Ps2 = ParticulaVfx.Novo(it.Ps.transform, "Faiscas", GdVioleta, GdCarmesim, 0f,
                        new Vector2(0.3f, 0.7f), new Vector2(0.3f, 1.2f), new Vector2(0.04f, 0.08f), true, 1f, 40);
                    it.Ps2.transform.localRotation = Quaternion.identity;
                    break;
                case "noctus_rastro":
                    it.R = Peca(raiz, MalhaVfx.Disco(), _gdAlfa);
                    it.Ps = GdFumaca(raiz, "Rastro", new Color(0.55f, 0.12f, 0.2f, 0.5f), new Color(0.35f, 0.08f, 0.14f, 0.35f), 0f);
                    it.Ps.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                    break;

                // ---------------------------------------------------------------- PIP
                case "pip_rota":
                    it.Linha = Linha(it.Go, 0.12f);   // a ROTA telegrafada em faisca
                    it.Pontos = new Vector3[7];
                    it.Linha.positionCount = 7;
                    it.Ps = ParticulaVfx.Novo(raiz, "Faiscas", Color.white, GdFaisca, 0f,
                        new Vector2(0.15f, 0.35f), new Vector2(0.8f, 2.5f), new Vector2(0.04f, 0.09f), true, 1f, 70);
                    break;
                case "pip_nuvem":
                case "pip_chuva":
                    it.R = Peca(raiz, GdMalhaNuvem(), _gdProp);
                    if (tipo == "pip_nuvem")
                    {
                        it.Ps = ParticulaVfx.Novo(raiz, "Relampagos", Color.white, GdFaisca, 18f,
                            new Vector2(0.08f, 0.2f), new Vector2(1f, 3f), new Vector2(0.06f, 0.16f), false, 1f, 40);
                        ParticleSystem.ShapeModule shu = it.Ps.shape;
                        shu.scale = new Vector3(1.6f, 1.6f, 0.6f);
                    }
                    else
                    {
                        // CHUVA em cima dela: riscos azuis caindo da nuvem (esticados pela velocidade)
                        it.Ps = ParticulaVfx.Novo(raiz, "Chuva", new Color(0.6f, 0.8f, 1f, 0.8f), GdAzul, 0f,
                            new Vector2(0.35f, 0.5f), new Vector2(-7f, -5f), new Vector2(0.03f, 0.05f), true, 0f, 120);
                        ParticleSystem.ShapeModule shu = it.Ps.shape;
                        shu.scale = new Vector3(1.6f, 1.6f, 0.2f);
                        var rc = it.Ps.GetComponent<ParticleSystemRenderer>();
                        rc.renderMode = ParticleSystemRenderMode.Stretch;
                        rc.velocityScale = 0.06f;
                        rc.lengthScale = 1f;
                    }
                    break;
                case "pip_sino":
                    it.R = Peca(raiz, MalhaVfx.Anel(), _gdBrilho);   // a onda do sino: o counter sonoro tambem se ve'
                    break;
                default:
                    feito = false;
                    break;
            }
        }

        // ============================================================================ desenhar (por quadro)

        partial void DesenhoGrupoD(Item it, EfeitoVisual v, float e, Pawn dono, ref bool feito)
        {
            feito = true;
            int passo = Passo();
            float idade = v.Duracao - v.Restante;
            float prog = v.Duracao > 0f ? Mathf.Clamp01(idade / v.Duracao) : 1f;
            Transform t = it.Go.transform;
            switch (v.Tipo)
            {
                // ---------------------------------------------------------------- FIZZ
                case "fizz_torreta":
                {
                    Vector3 p = NoChao(v.Pos), d = ApoioGrupoD.Plano(v.Pos2 - v.Pos);
                    if (d.sqrMagnitude < 1e-4f) d = Vector3.forward;
                    d.Normalize();
                    t.SetPositionAndRotation(p, Quaternion.LookRotation(d, Vector3.up));
                    t.localScale = Vector3.one * Mathf.Min(1f, idade / 0.25f) * Mathf.Lerp(0.6f, 1f, e);   // finca e cresce
                    Pintar(it.R2, Hdr((passo / 4) % 2 == 0 ? GdAzul : GdFaisca, 2.4f), e);   // o farol pisca
                    if (_gdConeFizz < 0f) _gdConeFizz = Kits.De("16-fizz").Tatica["cone_graus"];
                    float l = Mathf.Min(Vector3.Distance(v.Pos, v.Pos2), 6f);
                    Vector3 esq = p + ApoioGrupoD.Girar(d, -_gdConeFizz) * l, dir = p + ApoioGrupoD.Girar(d, _gdConeFizz) * l;
                    it.Pontos[0] = NoChao(esq) + Vector3.up * 0.08f;
                    it.Pontos[1] = p + Vector3.up * 0.08f;
                    it.Pontos[2] = NoChao(dir) + Vector3.up * 0.08f;
                    it.Linha.SetPositions(it.Pontos);
                    GdCor(it.Linha, GdAzul, 0.45f * e * Flicker[passo % Flicker.Length]);
                    break;
                }
                case "fizz_faisca":
                    GdRaio(it, v.Pos, v.Pos2, 6, 0.12f, 0.12f * e, GdFaisca, e, passo);
                    break;
                case "pip_faisca":
                    GdRaio(it, v.Pos, v.Pos2, 6, 0.12f, 0.1f * e, Color.Lerp(GdFaisca, GdVioleta, 0.3f), e, passo);
                    break;
                case "fizz_raio":
                case "pip_raio":
                {
                    bool mega = v.Tipo == "fizz_raio";
                    float len = Vector3.Distance(v.Pos, v.Pos2);
                    GdRaio(it, v.Pos, v.Pos2, mega ? 16 : 10, Mathf.Min(len * 0.08f, mega ? 0.8f : 0.4f), (mega ? 0.38f : 0.14f) * e,
                        mega ? Color.Lerp(Color.white, GdAzul, 0.35f) : Color.Lerp(GdFaisca, GdVioleta, 0.25f), e, passo);
                    it.R.transform.position = v.Pos2;
                    it.R.transform.localScale = Vector3.one * ((mega ? 1.1f : 0.45f) * Flicker[passo % Flicker.Length]);
                    Pintar(it.R, Hdr(mega ? GdAzul : GdFaisca, 2.2f), e);
                    it.Ps.transform.position = v.Pos2;
                    GdTaxa(it.Ps, (mega ? 140f : 40f) * e);
                    break;
                }
                case "fizz_bobina":
                {
                    t.SetPositionAndRotation(NoChao(v.Pos), Quaternion.Euler(0f, passo % 36 * 10f, 0f));
                    // "arma a bobina peca a peca": a coluna SOBE do chao em 4 trancos (catraca) no 1o terco da carga, depois so'
                    // carrega. Sobe inteira, nao estica: a peca da Meshy achatada leria mola de borracha. KNOB: 4 degraus em 0,75 s.
                    float degrau = Mathf.Min(1f, (Mathf.Floor(prog * 8f) + 1f) / 4f);
                    it.R.transform.parent.localPosition = Vector3.down * ((1f - degrau) * (it.R2.transform.localPosition.y + 0.3f));
                    float f = Flicker[passo % Flicker.Length];
                    it.R2.transform.localScale = Vector3.one * ((0.45f + 0.35f * prog) * f);
                    Pintar(it.R2, Hdr(Color.Lerp(GdAzul, Color.white, prog), 1.2f + 2.2f * prog), 1f);
                    GdTaxa(it.Ps, 30f + 230f * prog);
                    GdTaxa(it.Ps2, 8f + 30f * prog);
                    // o ARCO salta da coroa para um ponto do chao diferente a cada passo (arcos crescentes)
                    float ang = (passo * 137) % 360;
                    Vector3 chao = NoChao(v.Pos + ApoioGrupoD.Girar(Vector3.forward, ang) * (0.8f + 0.8f * prog)) + Vector3.up * 0.05f;
                    GdRaio(it, it.R2.transform.position, chao, 8, 0.25f, (0.04f + 0.1f * prog), Color.white,   // sai da COROA, onde ela estiver
                        prog > 0.15f ? 0.4f + 0.6f * prog : 0f, passo);
                    break;
                }
                case "fizz_mira":
                {
                    if (v.Alvo == null) { it.Linha.widthMultiplier = 0f; Pintar(it.R, Color.clear, 0f); break; }
                    Vector3 alvo = PosDoAlvo(v);
                    it.Linha.SetPosition(0, v.Pos);
                    it.Linha.SetPosition(1, alvo + Vector3.up);
                    it.Linha.widthMultiplier = 0.05f * Flicker[passo % Flicker.Length];
                    GdCor(it.Linha, Color.Lerp(GdFaisca, GdMagma, 0.4f), 0.9f);
                    it.R.transform.SetPositionAndRotation(NoChao(alvo) + Vector3.up * 0.1f, Inclinacao(alvo));
                    it.R.transform.localScale = Vector3.one * (0.9f - 0.3f * prog + 0.1f * ((passo & 1) == 0 ? 1f : 0f));   // aperta com a carga
                    Pintar(it.R, Hdr(GdFaisca, 2f), 1f);
                    break;
                }
                case "fizz_sucata":
                {
                    // tomba para um lado sorteado PELO LUGAR (sem Random: a mesma sucata cai igual no replay)
                    Vector3 p = NoChao(v.Pos);
                    t.SetPositionAndRotation(p, Quaternion.Euler(0f, GdHash(Mathf.RoundToInt(p.x * 7f) * 131 + Mathf.RoundToInt(p.z * 7f)) * 180f, 0f));
                    it.R.transform.localPosition = Vector3.up * 0.04f;
                    it.R.transform.localScale = Vector3.one * (0.7f * v.Raio + 0.3f);
                    Pintar(it.R, new Color(0.12f, 0.09f, 0.07f, 1f), 0.7f * e);
                    // a torreta quebrada (Raio 0,5) ou a bobina (Raio 1): uma acende, a outra apaga. Tomba sobre a borda do pe' em
                    // 0,35 s, escurece (cobre queimado) e no fim afunda no chao junto com a fumaca.
                    bool mega = v.Raio > 0.75f;
                    var bobina = (Renderer)it.Extra[0];
                    it.R2.enabled = !mega;
                    bobina.enabled = mega;
                    Renderer peca = mega ? bobina : it.R2;
                    Transform pivo = peca.transform.parent;
                    pivo.localRotation = Quaternion.Euler(0f, 0f, -(mega ? 80f : 72f) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(idade / 0.35f)));
                    Vector3 pp = pivo.localPosition;
                    pp.y = -0.4f * (1f - Mathf.Clamp01((e - LeituraDosKits.Piso) / (1f - LeituraDosKits.Piso)));
                    pivo.localPosition = pp;
                    Pintar(peca, GdCorSucata, 1f);
                    GdTaxa(it.Ps, 16f * v.Raio * e);
                    GdTaxa(it.Ps2, 22f * v.Raio * e * Flicker[passo % Flicker.Length]);
                    break;
                }
                case "fizz_mola":
                {
                    t.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.06f, Quaternion.identity);
                    it.R.transform.localScale = Vector3.one * (0.3f + 1.3f * prog);
                    Pintar(it.R, Hdr(GdFaisca, 2f), 1f - prog);
                    GdTaxa(it.Ps, 180f * (1f - prog));
                    break;
                }

                // ---------------------------------------------------------------- SYLVA
                case "sylva_broto":
                {
                    if (_gdFlorescer < 0f) _gdFlorescer = Kits.De("17-sylva").Tatica["florescer"];
                    bool aberto = idade >= _gdFlorescer;
                    t.SetPositionAndRotation(NoChao(v.Pos), Quaternion.Euler(0f, 30f * Mathf.Sin(idade * 1.3f), 0f));
                    float cresce = aberto ? 1f + 0.08f * Flicker[passo % Flicker.Length] : 0.25f + 0.75f * (idade / Mathf.Max(_gdFlorescer, 0.01f));
                    it.R.transform.localScale = Vector3.one * (cresce * Mathf.Lerp(0.3f, 1f, e));
                    it.R2.transform.localScale = Vector3.one * v.Raio;
                    Pintar(it.R2, Color.Lerp(GdFolha, GdSeiva, 0.5f), aberto ? 0.2f * e : 0f);
                    ParticleSystem.ShapeModule sh = it.Ps.shape;
                    sh.scale = new Vector3(v.Raio * 1.4f, v.Raio * 1.4f, 1.2f);
                    GdTaxa(it.Ps, aberto ? 22f * e : 0f);
                    break;
                }
                case "sylva_moita":
                    t.SetPositionAndRotation(NoChao(v.Pos), Quaternion.identity);
                    t.localScale = Vector3.one * (v.Raio * Mathf.Min(1f, idade / 0.35f) * Mathf.Lerp(0.2f, 1f, e));
                    break;
                case "sylva_queima":
                    t.position = NoChao(v.Pos);
                    GdTaxa(it.Ps, 45f * e);
                    GdTaxa(it.Ps2, 12f * e);
                    break;
                case "sylva_raizes":
                {
                    Vector3 c = NoChao(v.Pos);
                    t.SetPositionAndRotation(c + Vector3.up * 0.06f, Inclinacao(v.Pos));
                    float r = v.Raio * Mathf.Min(1f, idade / 0.4f);   // as raizes CORREM do cajado ate' a borda
                    it.R.transform.localScale = Vector3.one * r;
                    Pintar(it.R, GdSeiva, 0.16f * e);
                    it.R2.transform.localScale = Vector3.one * r;
                    Pintar(it.R2, Hdr(GdSeiva, 1.6f), e * Flicker[passo % Flicker.Length]);
                    int n = it.Pontos.Length / 2;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i * 360f / n + GdHash(passo / 3 * 7 + i) * 9f;   // treme em passos lentos: xilogravura
                        Vector3 ponta = c + ApoioGrupoD.Girar(Vector3.forward, a) * (r * (0.85f + 0.15f * GdHash(i * 13)));
                        it.Pontos[2 * i] = c + Vector3.up * 0.1f;
                        it.Pontos[2 * i + 1] = NoChao(ponta) + Vector3.up * 0.1f;
                    }
                    it.Pontos[it.Pontos.Length - 1] = c + Vector3.up * 0.1f;
                    it.Linha.SetPositions(it.Pontos);
                    GdCor(it.Linha, Color.Lerp(GdSeiva, GdMagma, 0.2f), 0.75f * e);
                    ParticleSystem.ShapeModule sh = it.Ps.shape;
                    sh.scale = new Vector3(r * 1.4f, r * 1.4f, 0.4f);
                    GdTaxa(it.Ps, 45f * e);
                    break;
                }
                case "sylva_agarra":
                {
                    Vector3 p = PosDoAlvo(v);
                    t.SetPositionAndRotation(NoChao(p), Quaternion.Euler(0f, passo % 12 * 30f, 0f));
                    it.R.transform.localScale = new Vector3(1f, Mathf.Min(1f, idade / 0.15f) * Mathf.Lerp(0.2f, 1f, e), 1f);
                    GdTaxa(it.Ps, 50f * e);
                    break;
                }
                case "sylva_seiva":
                    it.Pontos = LeituraDosKits.PontosDoFio(v.Pos, v.Pos2, it.Pontos.Length, passo * PassoAnime, it.Pontos);
                    it.Linha.SetPositions(it.Pontos);
                    it.Linha.widthMultiplier = 0.08f;
                    GdCor(it.Linha, GdSeiva, 0.7f * Flicker[passo % Flicker.Length]);
                    break;
                case "sylva_cabelo":
                {
                    t.position = v.Pos;
                    // a vida E' o cabelo: flor cheia, outono na metade, galho seco no fim; a seiva gasta tira o verde
                    Vitalidade vit = dono != null ? dono.Vital : null;
                    float f = vit != null && vit.HpMax > 0f ? Mathf.Clamp01(vit.Hp / vit.HpMax) : 1f;
                    Color a, b;
                    if (dono != null && dono.Runner != null && dono.Runner.EstadoAtivo(Sylva.SEIVA_GASTA)) { a = new Color(0.55f, 0.52f, 0.45f); b = new Color(0.35f, 0.33f, 0.3f); }
                    else if (f > 0.66f) { a = GdFlor; b = GdFolha; }
                    else if (f > 0.33f) { a = new Color(0.95f, 0.6f, 0.2f); b = new Color(0.8f, 0.45f, 0.15f); }
                    else { a = new Color(0.55f, 0.3f, 0.15f); b = new Color(0.4f, 0.22f, 0.12f); }
                    ParticleSystem.MainModule m = it.Ps.main;
                    m.startColor = new ParticleSystem.MinMaxGradient(a, b);
                    break;
                }

                // ---------------------------------------------------------------- BASALTO
                case "basalto_onda":
                {
                    Vector3 d = ApoioGrupoD.Plano(v.Pos2 - v.Pos);
                    float f = d.magnitude;
                    if (f < 1e-3f) d = Vector3.forward; else d /= f;
                    Vector3 frente = NoChao(v.Pos2);
                    t.SetPositionAndRotation(frente, Quaternion.LookRotation(d, Vector3.up));
                    it.R.transform.localScale = new Vector3(v.Raio * 2f, Mathf.Lerp(0.2f, 1f, e), 1f);
                    int n = it.Pontos.Length;
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 q = Vector3.Lerp(v.Pos, v.Pos2, i / (float)(n - 1));
                        q += Vector3.Cross(d, Vector3.up) * (GdHash(i * 17 + 3) * 0.25f * Mathf.Sin(Mathf.PI * i / (n - 1)));
                        it.Pontos[i] = NoChao(q) + Vector3.up * 0.06f;
                    }
                    it.Linha.SetPositions(it.Pontos);
                    it.Linha.widthMultiplier = 0.22f * e;
                    GdCor(it.Linha, GdMagma, e * Flicker[passo % Flicker.Length]);
                    ParticleSystem.ShapeModule sh = it.Ps.shape;
                    sh.scale = new Vector3(v.Raio * 2f, 0.4f, 0.3f);
                    it.Ps.transform.rotation = Quaternion.LookRotation(Vector3.up, d);   // caixa: largura na frente, sobe
                    GdTaxa(it.Ps, (prog < 0.7f ? 90f : 0f) * e);
                    GdTaxa(it.Ps2, (prog < 0.7f ? 40f : 0f) * e);
                    break;
                }
                case "basalto_monolito":
                {
                    Vector3 p = NoChao(PosDoAlvo(v));
                    t.SetPositionAndRotation(p, Quaternion.identity);
                    // as placas SOBEM da base ao olho (0,3 s: a raiz das seis) e as runas acendem em passos
                    it.R.transform.parent.localScale = new Vector3(1f, Mathf.Min(1f, idade / 0.3f), 1f);
                    it.R2.transform.localScale = Vector3.one * (1.35f + 0.05f * Flicker[passo % Flicker.Length]);
                    Pintar(it.R2, Hdr(GdSeiva, 2f), e * Flicker[(passo + 1) % Flicker.Length]);
                    GdTaxa(it.Ps, 45f * e);
                    break;
                }
                case "basalto_estilhaco":
                {
                    Vector3 d = ApoioGrupoD.Plano(v.Pos2 - v.Pos);
                    if (d.sqrMagnitude < 1e-4f) d = Vector3.forward;
                    t.position = v.Pos + Vector3.up * 1.2f;
                    it.Ps.transform.rotation = Quaternion.LookRotation(d.normalized + Vector3.up * 0.15f, Vector3.up);
                    GdTaxa(it.Ps, 260f * v.Raio * e);
                    break;
                }
                case "basalto_avalanche":
                    t.position = NoChao(v.Pos) + Vector3.up * 0.2f;
                    GdTaxa(it.Ps, 70f * v.Raio * e);
                    GdTaxa(it.Ps2, 30f * v.Raio * e);
                    break;

                // ---------------------------------------------------------------- NOCTUS
                case "noctus_investida":
                {
                    Vector3 a = v.Pos + Vector3.up * 1.1f, b = v.Pos2 + Vector3.up * 1.1f;
                    it.Linha.SetPosition(0, a);
                    it.Linha.SetPosition(1, b);
                    it.Linha.widthMultiplier = 0.9f * e;
                    Color c0 = GdVioleta; c0.a = 0.15f * e;
                    Color c1 = Color.Lerp(GdCarmesim, Color.white, 0.25f); c1.a = 0.9f * e;
                    it.Linha.startColor = c0;
                    it.Linha.endColor = c1;
                    it.Ps.transform.position = b;
                    GdTaxa(it.Ps, 70f * e);
                    break;
                }
                case "noctus_dreno":
                {
                    Vector3 de = PosDoAlvo(v) + Vector3.up * 1.1f;
                    Vector3 ate = dono != null ? dono.Pos + Vector3.up * 1.4f : de;
                    GdRaio(it, de, ate, it.Pontos.Length, 0.18f, 0.11f * e, GdVioleta, e, passo);
                    it.Ps.transform.position = de;
                    GdTaxa(it.Ps, 25f * e);
                    break;
                }
                case "noctus_marca":
                {
                    Vector3 p = PosDoAlvo(v) + Vector3.up * 2.25f;
                    t.SetPositionAndRotation(p, Quaternion.identity);
                    it.R.transform.localScale = Vector3.one * (0.42f + 0.06f * Flicker[passo % Flicker.Length]);
                    Pintar(it.R, Hdr(GdVioleta, 2.2f), e);
                    GdTaxa(it.Ps, 10f * e);
                    break;
                }
                case "noctus_nevoa":
                {
                    t.position = PosDoAlvo(v) + Vector3.up * 0.1f;
                    GdTaxa(it.Ps, 95f * e);
                    GdTaxa(it.Ps2, 25f * e);
                    break;
                }
                case "noctus_rastro":
                    t.SetPositionAndRotation(NoChao(v.Pos) + Vector3.up * 0.05f, Inclinacao(v.Pos));
                    it.R.transform.localScale = Vector3.one * v.Raio;
                    Pintar(it.R, GdCarmesim, 0.3f * e);
                    GdTaxa(it.Ps, 12f * v.Raio * e);
                    break;

                // ---------------------------------------------------------------- PIP
                case "pip_rota":
                {
                    Vector3 a = NoChao(v.Pos) + Vector3.up * 1.2f, b = NoChao(v.Pos2) + Vector3.up * 1.2f;
                    GdRaio(it, a, b, it.Pontos.Length, 0.15f, 0.12f * Mathf.Lerp(0.5f, 1f, e), GdFaisca, 0.8f * e, passo);
                    t.position = (a + b) * 0.5f;
                    if ((b - a).sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(b - a, Vector3.up);
                    ParticleSystem.ShapeModule sh = it.Ps.shape;
                    sh.scale = new Vector3(0.15f, Vector3.Distance(a, b), 0.15f);   // y da caixa = o segmento (o molde do Fio)
                    GdTaxa(it.Ps, 12f * Vector3.Distance(a, b) * e);
                    break;
                }
                case "pip_nuvem":
                case "pip_chuva":
                {
                    if (_gdAlturaNuvem < 0f) _gdAlturaNuvem = Kits.De("20-pip").Suprema["altura"];
                    Vector3 p = PosDoAlvo(v) + Vector3.up * _gdAlturaNuvem;
                    t.SetPositionAndRotation(p + Vector3.up * (0.08f * Mathf.Sin(idade * 3f)), Quaternion.Euler(0f, idade * 25f, 0f));
                    bool chuva = v.Tipo == "pip_chuva";
                    t.localScale = Vector3.one * ((chuva ? 1f : 1.15f) * Mathf.Lerp(0.3f, 1f, e) * Mathf.Min(1f, idade / 0.3f + 0.2f));
                    GdTaxa(it.Ps, (chuva ? 140f : 20f * Flicker[passo % Flicker.Length]) * e);
                    break;
                }
                case "pip_sino":
                {
                    Vector3 p = PosDoAlvo(v) + Vector3.up * 1.25f;
                    t.SetPositionAndRotation(p, Quaternion.identity);
                    it.R.transform.localScale = Vector3.one * (0.2f + 1.4f * prog);
                    Pintar(it.R, Hdr(GdSeiva, 1.8f), 0.85f * (1f - prog));
                    break;
                }
                default:
                    feito = false;
                    break;
            }
        }

        // ============================================================================ ferramentas do grupo D

        static void GdMateriais()
        {
            if (_gdProp != null) return;
            _gdProp = MaterialVfx.Novo(GdPropShader, Color.white, MaterialVfx.Mistura.Opaco, false, null);
            _gdAlfa = MaterialVfx.Solido(Color.white, MaterialVfx.Mistura.Alfa, true);
            _gdBrilho = MaterialVfx.Solido(Color.white, MaterialVfx.Mistura.Aditivo, true);
            _gdFumaca = MaterialVfx.Novo(MaterialVfx.ParticulaUnlit, Color.white, MaterialVfx.Mistura.Alfa, true, MaterialVfx.PontoSuave());
            _gdPedra = MaterialVfx.Novo(GdPropShader, Color.white, MaterialVfx.Mistura.Opaco, false, null);
        }

        /// <summary>Raio de a a b: zigue-zague DETERMINISTICO por passo de anime (mesmo passo = mesmo desenho), amplitude maxima
        /// no meio, pontas cravadas. Pontos no buffer do Item (zero lixo).</summary>
        static void GdRaio(Item it, Vector3 a, Vector3 b, int n, float amp, float largura, Color cor, float alfa, int passo)
        {
            if (it.Pontos == null || it.Pontos.Length != n) it.Pontos = new Vector3[n];
            Vector3 d = b - a;
            Vector3 lado = Vector3.Cross(d, Vector3.up);
            lado = lado.sqrMagnitude > 1e-6f ? lado.normalized : Vector3.right;
            Vector3 cima = Vector3.Cross(lado, d);
            cima = cima.sqrMagnitude > 1e-6f ? cima.normalized : Vector3.up;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1), env = Mathf.Sin(u * Mathf.PI) * amp;
                it.Pontos[i] = a + d * u + lado * (GdHash(passo * 31 + i * 7) * env) + cima * (GdHash(passo * 17 + i * 13 + 5) * env);
            }
            it.Pontos[0] = a;
            it.Pontos[n - 1] = b;
            it.Linha.positionCount = n;
            it.Linha.SetPositions(it.Pontos);
            it.Linha.widthMultiplier = largura;
            GdCor(it.Linha, cor, alfa);
        }

        /// <summary>-1..1 deterministico (hash inteiro): o tremor sem Random.</summary>
        static float GdHash(int x)
        {
            unchecked
            {
                uint h = (uint)x * 2654435761u;
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
                return (h & 0xFFFF) / 32767.5f - 1f;
            }
        }

        static void GdCor(LineRenderer l, Color c, float alfa)
        {
            c.a = alfa;
            l.startColor = c;
            l.endColor = c;
        }

        static void GdTaxa(ParticleSystem ps, float taxa)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTimeMultiplier = Mathf.Max(taxa, 0f);
        }

        static void GdGravidade(ParticleSystem ps, float g)
        {
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = g;
        }

        /// <summary>Fumaca/poeira/nevoa: ALFA (aditivo cinza vira brilho), sobe devagar e abre ao subir. MUNDO: fica no rastro.</summary>
        static ParticleSystem GdFumaca(Transform pai, string nome, Color a, Color b, float taxa)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, nome, a, b, taxa, new Vector2(0.9f, 1.8f), new Vector2(0.2f, 0.8f),
                new Vector2(0.5f, 1.1f), true, 0.35f, 90);
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _gdFumaca;
            return ps;
        }

        /// <summary>Pedra de verdade: particula de MALHA (cubo girado, iluminado) que cai com a gravidade. MUNDO.</summary>
        static ParticleSystem GdPedras(Transform pai, string nome, float taxa, float gravidade)
        {
            ParticleSystem ps = ParticulaVfx.Novo(pai, nome, GdBasalto, new Color(0.62f, 0.6f, 0.58f), taxa,
                new Vector2(0.6f, 1.2f), new Vector2(2f, 5f), new Vector2(0.09f, 0.26f), true, 0.5f, 120);
            ParticleSystem.MainModule m = ps.main;
            m.gravityModifier = gravidade;
            m.startRotation3D = true;
            m.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            m.startRotationY = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            m.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = false;   // pedra nao some em alfa: cai e encolhe
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = MalhaVfx.Primitiva(PrimitiveType.Cube);
            r.sharedMaterial = _gdPedra;
            return ps;
        }

        // ------------------------------------------------------------------ pecas da Meshy (onda 11)

        /// <summary>A torreta (ou a bobina) do Fizz: a peca da Meshy na ALTURA DO CORPO do kit (corpo_altura: o desenho e o alvo
        /// medem o mesmo) com a escala em k; sem o .glb, a primitiva de antes no material de cor de vertice, com k = 0.</summary>
        static Mesh GdMalhaFizz(bool bobina, out Material mat, out float k)
        {
            Kits.KitDef fizz = Kits.De("16-fizz");
            float alto = (bobina ? fizz.Suprema : fizz.Tatica)["corpo_altura"];
            Mesh m;
            if (PecaDaMeshy.Carregar(bobina ? GdGlbBobina : GdGlbTorreta, out m, out mat))
            {
                k = alto / Mathf.Max(m.bounds.size.y, 0.01f);
                return m;
            }
            GdMateriais();
            mat = _gdProp;
            k = 0f;
            return bobina ? GdMalhaBobina() : GdMalhaTorreta();
        }

        /// <summary>A peca da SUCATA pendurada num PIVO na borda +x do pe': o pivo gira e ela tomba ali, sem enterrar nem flutuar.</summary>
        static Renderer GdSucata(Transform raiz, bool bobina)
        {
            Mesh m = GdMalhaFizz(bobina, out Material mat, out float k);
            float s = k > 0f ? k : 1f, borda = m.bounds.max.x * s;
            Transform pivo = new GameObject("Pivo").transform;
            pivo.SetParent(raiz, false);
            pivo.localPosition = new Vector3(borda, 0f, 0f);
            Renderer r = Peca(pivo, m, mat);
            r.transform.localPosition = new Vector3(-borda, 0f, 0f);
            r.transform.localScale = Vector3.one * s;
            return r;
        }

        // ------------------------------------------------------------------ malhas (1x, primitivas combinadas com cor de vertice)

        static Mesh GdMontar(string nome, Mesh[] malhas, Matrix4x4[] ms, Color[] cores)
        {
            var ci = new CombineInstance[malhas.Length];
            for (int i = 0; i < malhas.Length; i++) { ci[i].mesh = malhas[i]; ci[i].transform = ms[i]; }
            var m = new Mesh { name = nome };
            m.CombineMeshes(ci, true, true);
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            var c = new Color[m.vertexCount];
            int o = 0;
            for (int i = 0; i < malhas.Length; i++)
            {
                Color k = linear ? cores[i].linear : cores[i];
                for (int j = 0; j < malhas[i].vertexCount && o < c.Length; j++) c[o++] = k;
            }
            m.colors = c;
            m.RecalculateBounds();
            return m;
        }

        static Matrix4x4 GdTrs(Vector3 p, Vector3 euler, Vector3 s) => Matrix4x4.TRS(p, Quaternion.Euler(euler), s);

        static Mesh GdCil => MalhaVfx.Primitiva(PrimitiveType.Cylinder);
        static Mesh GdEsf => MalhaVfx.Primitiva(PrimitiveType.Sphere);
        static Mesh GdCubo => MalhaVfx.Primitiva(PrimitiveType.Cube);
        static Mesh GdCaps => MalhaVfx.Primitiva(PrimitiveType.Capsule);

        /// <summary>Torreta: base de cobre, haste, cabeca com o cano para +Z (o cone) e a lente azul dos oculos dele. ~1,2 m.
        /// Reserva: so' sem o 42-torreta-fizz.glb (idem bobina e placas abaixo).</summary>
        static Mesh GdMalhaTorreta()
        {
            if (_gdTorreta != null) return _gdTorreta;
            Color escuro = GdCobre * 0.55f; escuro.a = 1f;
            return _gdTorreta = GdMontar("GdTorreta",
                new[] { GdCil, GdCil, GdCubo, GdCil, GdEsf, GdCil },
                new[] {
                    GdTrs(new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.7f, 0.12f, 0.7f)),
                    GdTrs(new Vector3(0f, 0.5f, 0f), Vector3.zero, new Vector3(0.16f, 0.28f, 0.16f)),
                    GdTrs(new Vector3(0f, 0.95f, 0f), Vector3.zero, new Vector3(0.42f, 0.3f, 0.5f)),
                    GdTrs(new Vector3(0f, 0.95f, 0.42f), new Vector3(90f, 0f, 0f), new Vector3(0.1f, 0.22f, 0.1f)),
                    GdTrs(new Vector3(0f, 1.02f, 0.26f), Vector3.zero, Vector3.one * 0.16f),
                    GdTrs(new Vector3(0f, 0.3f, 0f), Vector3.zero, new Vector3(0.36f, 0.06f, 0.36f)),
                },
                new[] { escuro, GdBasalto, GdCobre, escuro, GdAzul, GdFaisca });
        }

        /// <summary>MEGABOBINA: base larga, coluna de cobre com tres aneis dourados (a coroa e' o R2 que acende). ~2,4 m.</summary>
        static Mesh GdMalhaBobina()
        {
            if (_gdBobina != null) return _gdBobina;
            Color escuro = GdCobre * 0.5f; escuro.a = 1f;
            return _gdBobina = GdMontar("GdBobina",
                new[] { GdCil, GdCil, GdCil, GdCil, GdCil },
                new[] {
                    GdTrs(new Vector3(0f, 0.15f, 0f), Vector3.zero, new Vector3(1.1f, 0.15f, 1.1f)),
                    GdTrs(new Vector3(0f, 1.25f, 0f), Vector3.zero, new Vector3(0.32f, 1.05f, 0.32f)),
                    GdTrs(new Vector3(0f, 0.75f, 0f), Vector3.zero, new Vector3(0.75f, 0.05f, 0.75f)),
                    GdTrs(new Vector3(0f, 1.3f, 0f), Vector3.zero, new Vector3(0.65f, 0.05f, 0.65f)),
                    GdTrs(new Vector3(0f, 1.85f, 0f), Vector3.zero, new Vector3(0.55f, 0.05f, 0.55f)),
                },
                new[] { escuro, GdCobre, GdFaisca, GdFaisca, GdFaisca });
        }

        /// <summary>Broto: caule, duas folhas e o botao de flor rosa (o R cresce de 25% a 100% no florescer).</summary>
        static Mesh GdMalhaBroto()
        {
            if (_gdBroto != null) return _gdBroto;
            return _gdBroto = GdMontar("GdBroto",
                new[] { GdCil, GdEsf, GdEsf, GdEsf, GdEsf },
                new[] {
                    GdTrs(new Vector3(0f, 0.3f, 0f), Vector3.zero, new Vector3(0.07f, 0.3f, 0.07f)),
                    GdTrs(new Vector3(0.18f, 0.35f, 0f), new Vector3(0f, 0f, 35f), new Vector3(0.36f, 0.06f, 0.18f)),
                    GdTrs(new Vector3(-0.16f, 0.45f, 0f), new Vector3(0f, 0f, -35f), new Vector3(0.32f, 0.06f, 0.16f)),
                    GdTrs(new Vector3(0f, 0.66f, 0f), Vector3.zero, new Vector3(0.28f, 0.22f, 0.28f)),
                    GdTrs(new Vector3(0f, 0.72f, 0f), Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f)),
                },
                new[] { GdFolha, GdFolha, GdFolha, GdFlor, GdSeiva });
        }

        /// <summary>Moita: tufos de folha em tres verdes e duas flores (raio 1 — a casca escala pelo Raio).</summary>
        static Mesh GdMalhaMoita()
        {
            if (_gdMoita != null) return _gdMoita;
            Color claro = Color.Lerp(GdFolha, new Color(0.6f, 0.8f, 0.3f), 0.35f), escuro = GdFolha * 0.7f;
            escuro.a = 1f;
            return _gdMoita = GdMontar("GdMoita",
                new[] { GdEsf, GdEsf, GdEsf, GdEsf, GdEsf, GdEsf, GdEsf },
                new[] {
                    GdTrs(new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(1.2f, 0.9f, 1.2f)),
                    GdTrs(new Vector3(0.55f, 0.35f, 0.2f), Vector3.zero, new Vector3(0.8f, 0.7f, 0.8f)),
                    GdTrs(new Vector3(-0.5f, 0.3f, -0.25f), Vector3.zero, new Vector3(0.85f, 0.6f, 0.85f)),
                    GdTrs(new Vector3(-0.1f, 0.35f, 0.55f), Vector3.zero, new Vector3(0.7f, 0.6f, 0.7f)),
                    GdTrs(new Vector3(0.2f, 0.8f, -0.1f), Vector3.zero, new Vector3(0.6f, 0.5f, 0.6f)),
                    GdTrs(new Vector3(0.35f, 0.95f, 0.25f), Vector3.zero, Vector3.one * 0.16f),
                    GdTrs(new Vector3(-0.4f, 0.7f, 0.35f), Vector3.zero, Vector3.one * 0.14f),
                },
                new[] { GdFolha, claro, escuro, claro, GdFolha, GdFlor, GdFlor });
        }

        /// <summary>A crista da onda: 5 lascas de basalto inclinadas lado a lado (x de -0,5 a 0,5; a casca estica pela largura).</summary>
        static Mesh GdMalhaLascas()
        {
            if (_gdLascas != null) return _gdLascas;
            Color claro = Color.Lerp(GdBasalto, Color.white, 0.2f);
            return _gdLascas = GdMontar("GdLascas",
                new[] { GdCubo, GdCubo, GdCubo, GdCubo, GdCubo, GdCubo },
                new[] {
                    GdTrs(new Vector3(-0.4f, 0.3f, 0f), new Vector3(-12f, 8f, 14f), new Vector3(0.16f, 0.7f, 0.3f)),
                    GdTrs(new Vector3(-0.2f, 0.42f, 0.05f), new Vector3(-18f, -6f, -6f), new Vector3(0.18f, 0.95f, 0.34f)),
                    GdTrs(new Vector3(0f, 0.5f, 0f), new Vector3(-15f, 3f, 4f), new Vector3(0.2f, 1.1f, 0.36f)),
                    GdTrs(new Vector3(0.2f, 0.4f, 0.04f), new Vector3(-20f, -8f, -10f), new Vector3(0.18f, 0.9f, 0.32f)),
                    GdTrs(new Vector3(0.41f, 0.28f, 0f), new Vector3(-10f, 5f, -16f), new Vector3(0.15f, 0.62f, 0.3f)),
                    GdTrs(new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(1f, 0.08f, 0.45f)),
                },
                new[] { GdBasalto, claro, GdBasalto, claro, GdBasalto, GdMagma });
        }

        /// <summary>O Monolito: 6 placas de basalto em pe' em volta do corpo (2,3 m), cada uma com uma faixa ambar (a runa).</summary>
        static Mesh GdMalhaPlacas()
        {
            if (_gdPlacas != null) return _gdPlacas;
            const int n = 6;
            var ms = new Mesh[n * 2];
            var ts = new Matrix4x4[n * 2];
            var cs = new Color[n * 2];
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                Quaternion q = Quaternion.Euler(0f, a, 0f);
                Vector3 p = q * new Vector3(0f, 0f, 0.8f);
                ms[2 * i] = GdCubo;
                ts[2 * i] = Matrix4x4.TRS(p + Vector3.up * 1.25f, q * Quaternion.Euler(-6f, 0f, 0f), new Vector3(0.62f, 2.5f - 0.2f * (i % 2), 0.22f));
                cs[2 * i] = i % 2 == 0 ? GdBasalto : Color.Lerp(GdBasalto, Color.white, 0.15f);
                ms[2 * i + 1] = GdCubo;
                ts[2 * i + 1] = Matrix4x4.TRS(p * 1.14f + Vector3.up * (1.3f + 0.3f * (i % 3)), q * Quaternion.Euler(-6f, 0f, 0f), new Vector3(0.4f, 0.08f, 0.06f));
                cs[2 * i + 1] = GdSeiva;
            }
            return _gdPlacas = GdMontar("GdPlacas", ms, ts, cs);
        }

        /// <summary>Raizes que agarram: quatro gavinhas de madeira dourada subindo em volta dos pes, curvadas para dentro.</summary>
        static Mesh GdMalhaRaizes()
        {
            if (_gdRaizes != null) return _gdRaizes;
            Color madeira = new Color(0.45f, 0.3f, 0.15f);
            var ms = new Mesh[4];
            var ts = new Matrix4x4[4];
            var cs = new Color[4];
            for (int i = 0; i < 4; i++)
            {
                Quaternion q = Quaternion.Euler(0f, i * 90f + 20f, 0f);
                ms[i] = GdCaps;
                ts[i] = Matrix4x4.TRS(q * new Vector3(0f, 0.55f, 0.5f), q * Quaternion.Euler(-22f, 0f, 0f), new Vector3(0.12f, 0.6f, 0.12f));
                cs[i] = i % 2 == 0 ? GdSeiva : madeira;
            }
            return _gdRaizes = GdMontar("GdRaizes", ms, ts, cs);
        }

        /// <summary>Nuvem de tempestade: novelo de bolas cinza-azuladas, a de baixo mais escura (a barriga da chuva).</summary>
        static Mesh GdMalhaNuvem()
        {
            if (_gdNuvem != null) return _gdNuvem;
            Color claro = new Color(0.62f, 0.66f, 0.75f), meio = new Color(0.45f, 0.48f, 0.58f), escuro = new Color(0.3f, 0.32f, 0.42f);
            return _gdNuvem = GdMontar("GdNuvem",
                new[] { GdEsf, GdEsf, GdEsf, GdEsf, GdEsf, GdEsf },
                new[] {
                    GdTrs(new Vector3(0f, 0.15f, 0f), Vector3.zero, new Vector3(1.3f, 0.85f, 1.1f)),
                    GdTrs(new Vector3(0.6f, 0f, 0.15f), Vector3.zero, new Vector3(0.9f, 0.7f, 0.85f)),
                    GdTrs(new Vector3(-0.6f, 0.02f, -0.1f), Vector3.zero, new Vector3(0.95f, 0.72f, 0.85f)),
                    GdTrs(new Vector3(0.1f, 0.05f, 0.55f), Vector3.zero, new Vector3(0.8f, 0.6f, 0.75f)),
                    GdTrs(new Vector3(-0.1f, 0.45f, -0.05f), Vector3.zero, new Vector3(0.8f, 0.6f, 0.75f)),
                    GdTrs(new Vector3(0f, -0.25f, 0f), Vector3.zero, new Vector3(1.4f, 0.35f, 1.1f)),
                },
                new[] { claro, meio, meio, claro, claro, escuro });
        }
    }
}
