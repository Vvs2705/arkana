using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Arkana.World;

namespace Arkana.EditorTools
{
    /// <summary>
    /// Leva a ilha do Documento Mestre para o Blender (ou qualquer ferramenta): abre a cena IlhaMestre, monta a ilha e grava
    ///   pecas.txt    tipo|grupo|fonte|matriz 4x4 do Unity (16, coluna a coluna)|cor   — P = GLB do kit, B = bloco primitivo
    ///   arvores.txt  fonte|x|y|z (mundo Unity)|giro (rad)|escala
    ///   chao.png     2048x2048, a mistura das 5 camadas do Terrain (linha 0 = SUL), cobrindo os 4,8 x 4,4 km
    ///   conferencia-vila.png  vista de cima da vila (conferir o lado da porta das casas no Blender)
    /// Pasta: variavel ARKANA_EXPORT, ou Temp/ilha-blender. Batchmode:
    ///   Unity -batchmode -projectPath mobile-unity -executeMethod Arkana.EditorTools.ExportarIlhaMestre.Exportar -quit
    /// O relevo NAO vai aqui: e' o Resources/ilha-mestre-altura.bytes (fonte unica).
    /// </summary>
    public static class ExportarIlhaMestre
    {
        const string Cena = "Assets/_Arkana/Scenes/IlhaMestre.unity";
        // cor media MEDIDA de cada camada (sRGB), na ordem do IlhaMestre.Camadas: grama, mata, rocha, areia, terra.
        // As texturas nao sao legiveis por codigo (isReadable falso): a media vem de arte/tools/texturas_terreno.py.
        static readonly Color32[] Media =
        {
            new Color32(91, 127, 57, 255), new Color32(77, 69, 43, 255), new Color32(111, 105, 97, 255),
            new Color32(195, 179, 141, 255), new Color32(117, 91, 59, 255),
        };

        [MenuItem("Arkana/Ilha Mestre/Exportar para o Blender")]
        public static void Exportar()
        {
            string dir = System.Environment.GetEnvironmentVariable("ARKANA_EXPORT");
            if (string.IsNullOrEmpty(dir)) dir = Path.GetFullPath("Temp/ilha-blender");
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(Cena);
            var ilha = Object.FindFirstObjectByType<IlhaMestre>();
            ilha.Construir();
            Transform g = ilha.transform.Find("_gerado");
            CultureInfo inv = CultureInfo.InvariantCulture;

            var pecas = new StringBuilder();
            int np = 0;
            foreach (Transform grupo in g)
            {
                if (!grupo.name.StartsWith("ARKANA_")) continue;
                foreach (Transform t in grupo)
                {
                    string tipo, fonte, cor = "";
                    if (t.name.EndsWith("(Clone)")) { tipo = "P"; fonte = t.name.Replace("(Clone)", "").Trim(); }
                    else
                    {
                        var mf = t.GetComponent<MeshFilter>();
                        var mr = t.GetComponent<MeshRenderer>();
                        if (mf == null || mf.sharedMesh == null || mr == null) continue;
                        tipo = "B";
                        fonte = mf.sharedMesh.name;
                        Color c = mr.sharedMaterial.color;
                        cor = string.Format(inv, "{0:0.###},{1:0.###},{2:0.###}", c.r, c.g, c.b);
                    }
                    Matrix4x4 m = t.localToWorldMatrix;
                    pecas.Append(tipo).Append('|').Append(grupo.name).Append('|').Append(fonte).Append('|');
                    for (int i = 0; i < 16; i++) pecas.Append(m[i].ToString("0.#####", inv)).Append(i < 15 ? "," : "");
                    pecas.Append('|').Append(cor).Append('\n');
                    np++;
                }
            }
            File.WriteAllText(Path.Combine(dir, "pecas.txt"), pecas.ToString());

            var arv = new StringBuilder();
            int na = 0;
            UnityEngine.Terrain[] ters = g.GetComponentsInChildren<UnityEngine.Terrain>();
            foreach (UnityEngine.Terrain ter in ters)
            {
                TerrainData td = ter.terrainData;
                TreePrototype[] protos = td.treePrototypes;
                foreach (TreeInstance ti in td.treeInstances)
                {
                    Vector3 p = ter.transform.position + Vector3.Scale(ti.position, td.size);
                    string nome = protos[ti.prototypeIndex].prefab.name.Replace("molde_", "");
                    arv.Append(nome).Append('|').Append(p.x.ToString("0.##", inv)).Append('|').Append(p.y.ToString("0.##", inv))
                       .Append('|').Append(p.z.ToString("0.##", inv)).Append('|').Append(ti.rotation.ToString("0.###", inv))
                       .Append('|').Append(ti.heightScale.ToString("0.####", inv)).Append('\n');
                    na++;
                }
            }
            File.WriteAllText(Path.Combine(dir, "arvores.txt"), arv.ToString());

            // chao: 4x4 blocos de 512 -> 2048 x 2048 (x oeste->leste, y sul->norte, como a textura do Unity)
            const int A = 512, L = 2048;
            var px = new Color32[L * L];
            foreach (UnityEngine.Terrain ter in ters)
            {
                TerrainData td = ter.terrainData;
                int bi = Mathf.RoundToInt((ter.transform.position.x - ilha.D.x0) / td.size.x);
                int bj = Mathf.RoundToInt((ter.transform.position.z - ilha.D.y0) / td.size.z);
                float[,,] w = td.GetAlphamaps(0, 0, A, A);
                for (int j = 0; j < A; j++)
                for (int i = 0; i < A; i++)
                {
                    float r = 0, gg = 0, b = 0;
                    for (int k = 0; k < Media.Length && k < w.GetLength(2); k++)
                    {
                        r += w[j, i, k] * Media[k].r; gg += w[j, i, k] * Media[k].g; b += w[j, i, k] * Media[k].b;
                    }
                    px[(bj * A + j) * L + bi * A + i] = new Color32((byte)r, (byte)gg, (byte)b, 255);
                }
            }
            var tex = new Texture2D(L, L, TextureFormat.RGB24, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, "chao.png"), tex.EncodeToPNG());

            // conferencia: vila vista de cima (o lado da porta das casas tem de bater com o Blender)
            var cg = new GameObject("cam_conferencia");
            Camera cam = cg.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(new Vector3(-1200f, 520f, 650f), Quaternion.Euler(90f, 0f, 0f));
            cam.fieldOfView = 40f;
            cam.farClipPlane = 2000f;
            var rt = new RenderTexture(1024, 1024, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var foto = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
            foto.Apply();
            File.WriteAllBytes(Path.Combine(dir, "conferencia-vila.png"), foto.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(cg);
            Debug.Log($"[ExportarIlhaMestre] {np} pecas, {na} arvores, chao {L}x{L} -> {dir}");
        }
    }
}
