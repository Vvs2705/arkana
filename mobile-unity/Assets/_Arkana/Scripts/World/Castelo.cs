using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.World
{
    /// <summary>
    /// A rota do castelo voador, PURA: um seed gera a travessia inteira (inicio, fim, duracao)
    /// de uma vez. Determinismo DENTRO da partida — o seed e' sorteado por partida e guardado
    /// (`Seed`) para a rede transmitir; passe-o a mao e a rota se repete.
    /// Porte de mobile-godot/godot/world/Castelo.gd.
    /// </summary>
    public sealed class RotaDoCastelo
    {
        /// <summary>Altura de voo (m sobre o mar). E' o TETO da queda: ~9,5 s de ar.</summary>
        public const float Altura = 320f;
        /// <summary>Janela de decisao, em segundos. NAO escala com o mapa: mapa maior = castelo mais rapido.</summary>
        public const float Duracao = 22f;
        /// <summary>Ponta da rota como fracao do lado: entra de fora da ilha e sai pelo outro lado.</summary>
        public const float Margem = 0.62f;
        /// <summary>Desvio lateral maximo (fracao do lado). Sem ele toda rota cruzaria o centro.</summary>
        public const float Desvio = 0.18f;
        /// <summary>Seed de FALLBACK, so' para teste e cena solta. Em partida, sorteia-se.</summary>
        public const int SeedPadrao = 3103;

        public readonly int Seed;
        public readonly float Lado;
        public readonly Vector3 Inicio;
        public readonly Vector3 Fim;

        public RotaDoCastelo(int seed, Relevo relevo)
        {
            Seed = seed;
            // Fiacao defensiva: sem ilha, a ilha padrao (Escala 2 = 600 m).
            Lado = relevo != null ? relevo.Lado : Relevo.BaseLado * 2f;
            var rng = new Sorteio(seed);
            float ang = rng.Float() * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 lat = new Vector3(-dir.z, 0f, dir.x) * (rng.Faixa(-Desvio, Desvio) * Lado);
            float alcance = Lado * Margem;
            var alto = new Vector3(0f, Altura, 0f);
            Inicio = lat - dir * alcance + alto;
            Fim = lat + dir * alcance + alto;
        }

        public Vector3 PosicaoEm(float t01) => Vector3.Lerp(Inicio, Fim, t01);

        public Vector3 Direcao => (Fim - Inicio).normalized;

        /// <summary>m/s. A 600 m de lado: 372 x 2 / 22 = ~34 m/s.</summary>
        public float Velocidade => (Fim - Inicio).magnitude / Duracao;

        /// <summary>Distancia horizontal do centro do mapa ao ponto mais proximo da rota.</summary>
        public float DistanciaAoCentro
        {
            get
            {
                Vector2 a = new Vector2(Inicio.x, Inicio.z), b = new Vector2(Fim.x, Fim.z);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(-a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                return (a + ab * t).magnitude;
            }
        }

        // ---- espelho DOCUMENTAL das constantes de gameplay/queda/Queda.gd. A queda de verdade e'
        // da raia GAMEPLAY; estes numeros so' existem para a rota provar que a duracao e a altura
        // produzem um alcance de salto compativel (~185 m em ~9,5 s de ar, com chao a 5 m). ----
        const float VelQueda = 55f, AcelQueda = 40f, VelQuedaHoriz = 22f;
        const float AlturaPlaneio = 60f, VelPlaneio = 12f, VelPlaneioHoriz = 16f, FreioPlaneio = 90f;

        /// <summary>
        /// Alcance HORIZONTAL do salto (m) a partir da rota, e o tempo no ar, para um pouso a
        /// `alturaDoChao` m. Acelera a 40 m/s2 ate' 55 m/s, cai reto, freia a 90 m/s2 ao entrar
        /// no planeio (60 m sobre o chao) e plana a 12 m/s descendo / 16 m/s andando.
        /// </summary>
        public static float AlcanceHorizontalDaQueda(float alturaDoChao, out float segundosNoAr)
        {
            float t1 = VelQueda / AcelQueda;
            float d1 = 0.5f * AcelQueda * t1 * t1;
            float livre = Mathf.Max(0f, Altura - alturaDoChao - AlturaPlaneio - d1);
            float t2 = livre / VelQueda;
            float tb = (VelQueda - VelPlaneio) / FreioPlaneio;
            float db = 0.5f * (VelQueda + VelPlaneio) * tb;
            float t3 = Mathf.Max(0f, AlturaPlaneio - db) / VelPlaneio;
            segundosNoAr = t1 + t2 + tb + t3;
            return VelQuedaHoriz * (t1 + t2) + 0.5f * (VelQuedaHoriz + VelPlaneioHoriz) * tb + VelPlaneioHoriz * t3;
        }
    }

    /// <summary>
    /// O castelo voador que abre a partida: anda pela rota, emite Bus.CasteloRota UMA vez, some
    /// no fim. Decisao nº 14 (26/08): o castelo viaja SEM bonecos — o corpo embarcado fica
    /// invisivel e SURGE no portao no instante de `Saltar()`.
    /// </summary>
    public sealed class Castelo : MonoBehaviour
    {
        /// <summary>Onde o mago viaja: pendurado SOB o portao (a camera fica atras e acima).</summary>
        public static readonly Vector3 Portao = new Vector3(0f, -6f, 0f);

        public RotaDoCastelo Rota { get; private set; }
        public int SeedDaRota => Rota != null ? Rota.Seed : RotaDoCastelo.SeedPadrao;
        /// <summary>0..1 ao longo da rota.</summary>
        public float Progresso { get; private set; }
        /// <summary>Todos que embarcaram ja' saltaram (ou foram empurrados no fim da rota).</summary>
        public bool Saltou => embarcouAlguem && passageiros.Count == 0;
        public int Passageiros => passageiros.Count;
        public Vector3 PosicaoDoPortao => transform.position + Portao;

        // N passageiros (11/09/2026): o jogador E os 12 bots caem do MESMO castelo, cada um no seu
        // instante. Com um passageiro so', o primeiro bot a saltar derrubava o jogador junto.
        readonly List<GameObject> passageiros = new List<GameObject>();
        readonly Dictionary<GameObject, Renderer[]> renderers = new Dictionary<GameObject, Renderer[]>();
        bool embarcouAlguem;
        bool anunciado;

        /// <summary>Poe um castelo em rota. seed &lt; 0 = sorteia por partida (o seed fica em SeedDaRota).</summary>
        public static Castelo Criar(Transform parent, Relevo relevo, int seed = -1)
        {
            var go = new GameObject("Castelo");
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Castelo>();
            if (seed < 0) seed = new System.Random().Next();
            c.Definir(new RotaDoCastelo(seed, relevo));
            return c;
        }

        public void Definir(RotaDoCastelo rota)
        {
            Rota = rota;
            Progresso = 0f;
            transform.position = rota.Inicio;
            if ((rota.Fim - rota.Inicio).sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(rota.Direcao, Vector3.up);   // a proa aponta pra rota
        }

        void Start()
        {
            if (Rota == null)
                Definir(new RotaDoCastelo(RotaDoCastelo.SeedPadrao, Ilha.Atual != null ? Ilha.Atual.Relevo : null));
            if (transform.childCount == 0) MontarVisual();
            if (!anunciado)
            {
                anunciado = true;
                // A HUD/minimapa desenham a linha por onde da' pra saltar. UI OBSERVA.
                Bus.EmitCasteloRota(Rota.Inicio, Rota.Fim, RotaDoCastelo.Duracao);
            }
        }

        void Update()
        {
            if (Rota == null) return;
            Progresso += Time.deltaTime / RotaDoCastelo.Duracao;
            transform.position = Rota.PosicaoEm(Progresso);
            for (int i = 0; i < passageiros.Count; i++)
                if (passageiros[i] != null) passageiros[i].transform.position = PosicaoDoPortao;
            if (Progresso >= 1f)
            {
                // Quem nao saltou e' EMPURRADO: ninguem fica preso num castelo que saiu do mapa.
                SaltarTodos();
                Destroy(gameObject);
            }
        }

        /// <summary>Embarca um corpo: invisivel e grudado no portao ate' Saltar(corpo).</summary>
        public void Embarcar(GameObject corpo)
        {
            if (corpo == null || passageiros.Contains(corpo)) return;
            embarcouAlguem = true;
            passageiros.Add(corpo);
            renderers[corpo] = corpo.GetComponentsInChildren<Renderer>(true);
            Mostrar(corpo, false);
            corpo.transform.position = PosicaoDoPortao;
        }

        public bool Embarcado(GameObject corpo) => corpo != null && passageiros.Contains(corpo);

        /// <summary>Salta UM corpo: ele SURGE no portao. Devolve onde a queda comeca. Na BORDA: repetir nao faz nada.</summary>
        public Vector3 Saltar(GameObject corpo)
        {
            Vector3 p = PosicaoDoPortao;
            if (corpo == null || !passageiros.Remove(corpo)) return p;
            Mostrar(corpo, true);
            renderers.Remove(corpo);
            return p;
        }

        /// <summary>Compatibilidade: salta o passageiro mais antigo (o Godot tinha um so').</summary>
        public Vector3 Saltar() => passageiros.Count > 0 ? Saltar(passageiros[0]) : PosicaoDoPortao;

        public void SaltarTodos()
        {
            while (passageiros.Count > 0) Saltar(passageiros[0]);
        }

        void Mostrar(GameObject corpo, bool visivel)
        {
            Renderer[] rs;
            if (!renderers.TryGetValue(corpo, out rs) || rs == null) return;
            for (int i = 0; i < rs.Length; i++)
                if (rs[i] != null) rs[i].enabled = visivel;
        }

        // ------------------------------------------------------------------ visual

        /// <summary>
        /// Ponta a ponta do castelo que o Godot desenhava (m): o castelo.glb decimado a 30k tris, sem escala
        /// nenhuma la' — 35,8 m de envergadura, 52 m de altura. E' o numero que manda aqui, venha o modelo
        /// do tamanho que vier (um .glb normalizado pela Meshy chega com ~1,9 unidade).
        /// </summary>
        public const float Envergadura = 35.8f;

        /// <summary>Fator que leva o modelo a' envergadura do Godot. Tamanho invalido (zero, NaN) = 1: nunca some nem explode.</summary>
        public static float EscalaDoModelo(Vector3 tamanho)
        {
            float maior = Mathf.Max(tamanho.x, tamanho.z);
            return maior > 0.01f ? Envergadura / maior : 1f;
        }

        /// <summary>Modelo de arte em Resources/castelo (prefab); ausente, primitivas — fiacao defensiva.</summary>
        void MontarVisual()
        {
            var prefab = Resources.Load<GameObject>("castelo");
            if (prefab != null)
            {
                // mede NA ORIGEM e sem giro (o castelo ja' esta' rodado pra rota): bounds do mundo = do modelo
                GameObject m = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                m.name = "ModeloCastelo";
                Renderer[] rs = m.GetComponentsInChildren<Renderer>();
                Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(m.transform.position, Vector3.zero);
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                float k = EscalaDoModelo(b.size);
                Vector3 raiz = m.transform.position;
                m.transform.localScale = m.transform.localScale * k;
                m.transform.SetParent(transform, false);
                // o MIOLO do castelo no eixo da rota (no Godot: pe' em y=0 no export, descido meio corpo)
                m.transform.localPosition = -(b.center - raiz) * k;
                return;
            }
            MontarFallback();
        }

        /// <summary>Visto a 300 m por ~30 s: 5 malhas sem sombra leem "castelo" na silhueta.</summary>
        void MontarFallback()
        {
            Color pedra = new Color(0.42f, 0.40f, 0.47f);
            Color telhado = Relevo.Hex(0xb06cff);   // violeta arcano (paleta GDD §10)
            var b = new MalhaProc.Construtor();
            // rocha flutuante: larga em cima, em bico embaixo
            b.Tronco(new Vector3(0f, -14f, 0f), 4f, 15f, 14f, 8, pedra, Relevo.Escurecer(pedra, 0.25f));
            b.Caixa(new Vector3(0f, 2.5f, 0f), new Vector3(13f, 2.5f, 9f), pedra);
            // tres torres — a silhueta que faz o jogador reconhecer o castelo de longe
            Vector3[] torres = { new Vector3(-9f, 5f, -6f), new Vector3(9f, 5f, -6f), new Vector3(0f, 5f, 6f) };
            for (int i = 0; i < torres.Length; i++)
            {
                b.Tronco(torres[i], 3f, 3f, 13f, 10, pedra);
                b.Tronco(torres[i] + new Vector3(0f, 13f, 0f), 4.2f, 0f, 6f, 10, telhado);
            }
            var go = new GameObject("CasteloProcedural");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = b.ParaMesh("Castelo");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Ilha.MaterialPadrao();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
