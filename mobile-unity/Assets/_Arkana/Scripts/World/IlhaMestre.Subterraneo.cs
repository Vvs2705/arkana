using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arkana.World
{
    /// <summary>
    /// Subterraneo do Documento Mestre (§6.2, §7, R10-R12) a partir de Resources/ilha-mestre-subterraneo.json, o mesmo
    /// arquivo que gera as cascas do Blender (arte/tools/subterraneo.py): tuneis (perfil chao plano + abobada), saloes
    /// (cupula com chao plano), pocos com escada em caracol, e BURACOS no Terrain onde a superficie corta um vao (bocas).
    /// As cascas sao os vazios: material dos dois lados e MeshCollider — o chao real la' dentro e' a casca (Pawn nao puxa
    /// para a superficie: Ilha.Subterraneo).
    /// </summary>
    public sealed partial class IlhaMestre
    {
        [Serializable] public class Salao { public string id, regiao; public float x, y, piso, largura, comprimento, altura, giro, rocha_min; }
        [Serializable] public class Tunel { public string id, regiao, de, para; public float[] p; }   // x, y, piso, largura, altura por ponto
        [Serializable] public class Poco { public string id; public float x, y, piso, topo, raio; }
        [Serializable] public class Sub { public Salao[] saloes; public Tunel[] tuneis; public Poco[] pocos; }

        static readonly Color Rocha = new Color(0.16f, 0.16f, 0.18f);

        void Subterraneo()
        {
            if (S == null) return;
            var grupos = new Dictionary<string, Transform>
            {
                ["R10"] = Grupo("ARKANA_R10_Caverna_Profunda"), ["R11"] = Grupo("ARKANA_R11_Rede_Tuneis"),
                ["R12"] = Grupo("ARKANA_R12_Caverna_Subterranea"),
            };
            Material rocha = Mat(Rocha);
            rocha.SetFloat("_Cull", 0f);   // casca vista por dentro E por fora (inspecao com o terreno oculto)
            rocha.SetFloat("_Smoothness", 0.05f);
            foreach (Tunel t in S.tuneis) Objeto(grupos[t.regiao], t.id, MalhaTunel(t.p), rocha);
            int k = 0;
            foreach (Salao s in S.saloes) Objeto(grupos[s.regiao], s.id, MalhaSalao(s, 77 + k++), rocha);
            foreach (Poco p in S.pocos)
            {
                Objeto(grupos["R11"], "R11_Poco_" + p.id, MalhaPoco(p.x, p.y, p.piso, p.topo + 5f, p.raio), rocha);
                Objeto(grupos["R11"], "R11_Escada_" + p.id, MalhaEscada(p.x, p.y, p.piso, p.topo), Mat(PedraEsc));
            }
            Buracos();
        }

        GameObject Objeto(Transform pai, string nome, Mesh m, Material mat)
        {
            var g = new GameObject(nome) { hideFlags = HideFlags.DontSave };
            g.transform.SetParent(pai, false);
            g.AddComponent<MeshFilter>().sharedMesh = m;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            g.AddComponent<MeshCollider>().sharedMesh = m;
            return g;
        }

        static Mesh Malha(string nome, List<Vector3> v, List<int> f)
        {
            var m = new Mesh { name = nome, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetTriangles(f, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Perfil do tunel (u lateral -0,5..0,5 x largura; v 0..1 x altura): chao plano, paredes, abobada.</summary>
        static readonly Vector2[] Perfil = PerfilTunel();

        static Vector2[] PerfilTunel()
        {
            var p = new List<Vector2> { new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f) };
            for (int i = 0; i <= 10; i++)
            {
                float t = Mathf.PI * i / 10f;
                p.Add(new Vector2(0.5f * Mathf.Cos(t), 0.45f + 0.55f * Mathf.Sin(t)));
            }
            return p.ToArray();
        }

        static Mesh MalhaTunel(float[] p)
        {
            int np = p.Length / 5, K = Perfil.Length;
            var v = new List<Vector3>(np * K + 2);
            var f = new List<int>(np * K * 6);
            for (int i = 0; i < np; i++)
            {
                int a = Mathf.Max(i - 1, 0), b = Mathf.Min(i + 1, np - 1);
                float tx = p[b * 5] - p[a * 5], ty = p[b * 5 + 1] - p[a * 5 + 1];
                float len = Mathf.Max(Mathf.Sqrt(tx * tx + ty * ty), 1e-4f);
                float nx = -ty / len, ny = tx / len;
                float x = p[i * 5], y = p[i * 5 + 1], piso = p[i * 5 + 2], w = p[i * 5 + 3], h = p[i * 5 + 4];
                foreach (Vector2 q in Perfil) v.Add(U(x + nx * q.x * w, y + ny * q.x * w, piso + q.y * h));
            }
            for (int i = 0; i + 1 < np; i++)
            for (int k = 0; k < K; k++)
            {
                int a0 = i * K + k, a1 = i * K + (k + 1) % K, b0 = a0 + K, b1 = a1 + K;
                f.AddRange(new[] { a0, b0, b1, a0, b1, a1 });
            }
            foreach (int i in new[] { 0, np - 1 })      // tampas
            {
                Vector3 c = Vector3.zero;
                for (int k = 0; k < K; k++) c += v[i * K + k];
                int ci = v.Count;
                v.Add(c / K);
                for (int k = 0; k < K; k++) f.AddRange(new[] { ci, i * K + k, i * K + (k + 1) % K });
            }
            return Malha("tunel", v, f);
        }

        static Mesh MalhaSalao(Salao s, int semente)
        {
            var rnd = new System.Random(semente);
            float[] fase = { (float)rnd.NextDouble() * 6.3f, (float)rnd.NextDouble() * 6.3f, (float)rnd.NextDouble() * 6.3f };
            const int M = 64, N = 12;
            float g = s.giro * Mathf.Deg2Rad, cg = Mathf.Cos(g), sg = Mathf.Sin(g);
            var v = new List<Vector3>((N + 1) * M + 1);
            var f = new List<int>(N * M * 6 + M * 3);
            for (int j = 0; j <= N; j++)
            {
                float vv = (float)j / N;
                float fator = j < N ? Mathf.Pow(1f - Mathf.Pow(vv, 2.4f), 1f / 2.4f) : 0f;
                for (int i = 0; i < M; i++)
                {
                    float t = 2f * Mathf.PI * i / M;
                    float r = 1f + 0.10f * Mathf.Sin(3f * t + fase[0]) + 0.06f * Mathf.Sin(5f * t + fase[1]) + 0.05f * Mathf.Sin(2f * t + 3f * vv + fase[2]);
                    float lx = r * fator * s.largura * 0.5f * Mathf.Cos(t), ly = r * fator * s.comprimento * 0.5f * Mathf.Sin(t);
                    v.Add(U(s.x + lx * cg - ly * sg, s.y + lx * sg + ly * cg, s.piso + vv * s.altura));
                }
            }
            for (int j = 0; j < N; j++)
            for (int i = 0; i < M; i++)
            {
                int a0 = j * M + i, a1 = j * M + (i + 1) % M, b0 = a0 + M, b1 = a1 + M;
                f.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
            }
            int c = v.Count;
            v.Add(U(s.x, s.y, s.piso));   // chao plano
            for (int i = 0; i < M; i++) f.AddRange(new[] { c, (i + 1) % M, i });
            return Malha("salao", v, f);
        }

        /// <summary>Cilindro aberto em cima (a entrada) com chao.</summary>
        static Mesh MalhaPoco(float x, float y, float z0, float z1, float r, int seg = 32)
        {
            var v = new List<Vector3>();
            var f = new List<int>();
            foreach (float z in new[] { z0, z1 })
                for (int i = 0; i < seg; i++)
                    v.Add(U(x + r * Mathf.Cos(2f * Mathf.PI * i / seg), y + r * Mathf.Sin(2f * Mathf.PI * i / seg), z));
            int c = v.Count;
            v.Add(U(x, y, z0));
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                f.AddRange(new[] { i, j, seg + j, i, seg + j, seg + i, c, j, i });
            }
            return Malha("poco", v, f);
        }

        /// <summary>Rampa helicoidal andavel (~30 %) com coluna central — e' PISO, nao vazio.</summary>
        static Mesh MalhaEscada(float x, float y, float z0, float z1, float r0 = 1.8f, float r1 = 6.4f, float subidaVolta = 7.5f, float esp = 0.5f)
        {
            float voltas = (z1 - z0) / subidaVolta;
            int n = Mathf.Max((int)(voltas * 36f), 2);
            var v = new List<Vector3>((n + 1) * 4 + 32);
            var f = new List<int>(n * 24 + 48);
            for (int i = 0; i <= n; i++)
            {
                float t = 2f * Mathf.PI * voltas * i / n, z = z0 + (z1 - z0) * i / n;
                float c = Mathf.Cos(t), s = Mathf.Sin(t);
                v.Add(U(x + r0 * c, y + r0 * s, z)); v.Add(U(x + r1 * c, y + r1 * s, z));
                v.Add(U(x + r1 * c, y + r1 * s, z - esp)); v.Add(U(x + r0 * c, y + r0 * s, z - esp));
            }
            for (int i = 0; i < n; i++)
            for (int k = 0; k < 4; k++)
            {
                int a0 = i * 4 + k, a1 = i * 4 + (k + 1) % 4, b0 = a0 + 4, b1 = a1 + 4;
                f.AddRange(new[] { a0, b0, b1, a0, b1, a1 });
            }
            int o = v.Count;   // coluna central
            const int seg = 16;
            foreach (float z in new[] { z0, z1 + 3f })
                for (int i = 0; i < seg; i++)
                    v.Add(U(x + (r0 + 0.05f) * Mathf.Cos(2f * Mathf.PI * i / seg), y + (r0 + 0.05f) * Mathf.Sin(2f * Mathf.PI * i / seg), z));
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                f.AddRange(new[] { o + i, o + j, o + seg + j, o + i, o + seg + j, o + seg + i });
            }
            return Malha("escada", v, f);
        }

        /// <summary>
        /// Buraco no Terrain onde o chao passa por dentro de um vao (so' acontece nas bocas) e nos pocos — a mesma regra do
        /// buracos.png do Blender. SetHoles: true = terreno, false = buraco; colisor acompanha.
        /// </summary>
        void Buracos()
        {
            if (terrenos == null) return;
            int hr = terrenos[0, 0].terrainData.holesResolution;   // = Res - 1 (512): 2.048 celulas no mapa inteiro
            int total = hr * Blocos;
            float cs = (D.x1 - D.x0) / total, csy = (D.y1 - D.y0) / total;
            var buraco = new HashSet<long>();
            void Marcar(float x, float y, float raio, float zmin, float zmax)
            {
                int c0 = Mathf.Max((int)((x - raio - D.x0) / cs), 0), c1 = Mathf.Min((int)((x + raio - D.x0) / cs) + 1, total - 1);
                int r0 = Mathf.Max((int)((y - raio - D.y0) / csy), 0), r1 = Mathf.Min((int)((y + raio - D.y0) / csy) + 1, total - 1);
                for (int rj = r0; rj <= r1; rj++)
                for (int ci = c0; ci <= c1; ci++)
                {
                    float cx = D.x0 + (ci + 0.5f) * cs, cy = D.y0 + (rj + 0.5f) * csy;
                    if ((cx - x) * (cx - x) + (cy - y) * (cy - y) >= raio * raio) continue;
                    float ch = Altura(cx, cy);
                    if (ch > zmin && ch < zmax) buraco.Add((long)rj * total + ci);
                }
            }
            foreach (Tunel t in S.tuneis)
                for (int i = 0; i + 4 < t.p.Length; i += 5)
                    Marcar(t.p[i], t.p[i + 1], t.p[i + 3] * 0.5f - 0.5f, t.p[i + 2] - 0.5f, t.p[i + 2] + t.p[i + 4]);
            foreach (Poco p in S.pocos) Marcar(p.x, p.y, 6.5f, float.MinValue, float.MaxValue);
            if (buraco.Count == 0) return;
            for (int bj = 0; bj < Blocos; bj++)
            for (int bi = 0; bi < Blocos; bi++)
            {
                var h = new bool[hr, hr];
                bool algum = false;
                for (int j = 0; j < hr; j++)
                for (int i = 0; i < hr; i++)
                {
                    bool furo = buraco.Contains((long)(bj * hr + j) * total + bi * hr + i);
                    h[j, i] = !furo;
                    algum |= furo;
                }
                if (algum) terrenos[bi, bj].terrainData.SetHoles(0, 0, h);
            }
        }
    }
}
