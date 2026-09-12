using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Arkana.Characters;

namespace Arkana.Tests
{
    /// <summary>
    /// O elenco em FBX (Resources/magos, refeito no site da Meshy em 12/09): carrega como externo e fica na ALTURA DA
    /// FICHA, medida na malha deformada de verdade (BakeMesh), nao nos bounds que o proprio Mago usou para escalar.
    /// Existe porque a Pyra entrou minuscula — um ponto no chao — com o portao verde.
    /// </summary>
    public class CharactersMagoExternoTests
    {
        /// <summary>Todo slug do elenco que ja' tem FBX em Resources/magos (o elenco entra aos poucos).</summary>
        public static System.Collections.Generic.IEnumerable<string> SlugsExternos()
        {
            foreach (string s in Arkana.Core.Kits.Slugs)
                if (Resources.Load<GameObject>("magos/" + s) != null) yield return s;
        }

        [TestCaseSource(nameof(SlugsExternos))]
        public void MagoExterno_CarregaNaAlturaDaFicha_ComPesNoChao(string slug)
        {
            // o Mago instancia materiais (r.materials: o tint de um bot nao pinta o player); no EditMode isso loga erro
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Mago m = Mago.Criar(null, slug);
            try
            {
                Assert.AreEqual("external:magos/" + slug, m.Fonte, "o FBX nao vestiu (sem idle/run/cast?)");
                Assert.IsTrue(m.GetComponentInChildren<Animation>().isPlaying, "nasce na pose congelada do FBX, sem idle");
                Bounds b = MalhaDeformada(m.gameObject, out string diag);
                File.WriteAllText(Path.Combine(Application.dataPath, "..", "Logs", "diag-mago-" + slug + ".txt"), diag);
                float ficha = IdentidadeMago.De(slug).AlturaM;
                Assert.That(b.size.y, Is.EqualTo(ficha).Within(0.2f), "altura real " + b.size.y + " m; ficha " + ficha + " m\n" + diag);
                Assert.That(b.min.y, Is.EqualTo(0f).Within(0.12f), "pes fora do chao: min.y " + b.min.y);
            }
            finally { Object.DestroyImmediate(m.gameObject); }
        }

        /// <summary>Uniao, em mundo, dos vertices ja' deformados de cada SkinnedMeshRenderer (e dos bounds dos demais).</summary>
        static Bounds MalhaDeformada(GameObject raiz, out string diag)
        {
            var sb = new StringBuilder();
            bool tem = false;
            Bounds b = default;
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>())
            {
                var smr = r as SkinnedMeshRenderer;
                sb.AppendLine(r.name + " " + r.GetType().Name + " bounds=" + r.bounds + " lossy=" + r.transform.lossyScale);
                if (smr == null || smr.sharedMesh == null)
                {
                    if (!tem) { b = r.bounds; tem = true; } else b.Encapsulate(r.bounds);
                    continue;
                }
                sb.AppendLine("  mesh.bounds=" + smr.sharedMesh.bounds.ToString("F4") + " localBounds=" + smr.localBounds
                    + " rootBone=" + (smr.rootBone != null ? smr.rootBone.name + " lossy=" + smr.rootBone.lossyScale : "null"));
                var baked = new Mesh();
                smr.BakeMesh(baked);   // sem useScale: o vertice ja' sai na escala do mundo (medido: TransformPoint dobrava a escala)
                foreach (Vector3 v in baked.vertices)
                {
                    Vector3 w = smr.transform.position + smr.transform.rotation * v;
                    if (!tem) { b = new Bounds(w, Vector3.zero); tem = true; } else b.Encapsulate(w);
                }
                Object.DestroyImmediate(baked);
            }
            for (Transform t = raiz.transform; t != null; t = t.childCount > 0 ? t.GetChild(0) : null)
                sb.AppendLine("  cadeia: " + t.name + " pos=" + t.localPosition + " rot=" + t.localEulerAngles + " esc=" + t.localScale);
            sb.AppendLine("MEDIDO: " + b);
            diag = sb.ToString();
            return b;
        }
    }
}
