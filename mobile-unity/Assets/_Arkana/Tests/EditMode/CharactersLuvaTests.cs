using System.IO;
using NUnit.Framework;
using UnityEngine;
using Arkana.Characters;
using Arkana.Core;
using Arkana.Gameplay;

namespace Arkana.Tests
{
    /// <summary>
    /// A LUVA NA MAO (onda 15A). O osso RightHand da Meshy herda a escala 100 da Armature e a luva virou blocos de 10-17 m na
    /// frente da camera (diag 46 de 16/09) com o portao verde. Aqui a luva equipada se mede em METROS DE MUNDO: tamanho de
    /// mao, colada no osso, soquete na escala do corpo, dedos no eixo antebraco->mao, dorso no lado medido do osso.
    /// Esquecer a compensacao da escala poe a luva com ~32 m: cai aqui.
    /// </summary>
    public class CharactersLuvaTests
    {
        const string Procedural = "slug-que-nao-existe";

        [TestCase("01-pyra", Arma.VARINHA)]
        [TestCase("01-pyra", Arma.CAJADO)]
        [TestCase("01-pyra", Arma.MANOPLA)]
        [TestCase(Procedural, Arma.VARINHA)]
        [TestCase(Procedural, Arma.MANOPLA)]
        public void Luva_NaMao_TamanhoDeMao_ColadaNoOsso_NaEscalaDoCorpo(string slug, string arma)
        {
            // o Mago instancia materiais (r.materials) e isso loga erro no EditMode
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Mago m = Mago.Criar(null, slug);
            try
            {
                bool externo = slug != Procedural;
                Assert.AreEqual(externo ? "external:magos/" + slug : "procedural", m.Fonte, "o mago nao montou do jeito que o caso pede");
                Transform mao = m.MaoDireita;
                GameObject luva = LuvaVisual.Criar(mao, arma, new[] { Elemento.Fogo, Elemento.Agua });
                Transform soquete = luva.transform;
                float corpo = m.transform.Find("Rig").lossyScale.x;
                Bounds b = Limites(luva);
                float maior = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                float dist = Vector3.Distance(b.center, mao.position);
                Vector3 braco = (mao.position - mao.parent.position).normalized;
                float dedos = Vector3.Angle(soquete.up, braco);
                Vector3 dorsoNoOsso = soquete.localRotation * Vector3.forward;
                string diag = slug + " " + arma + ": fonte=" + m.Fonte + " mao=" + mao.name + " lossyMao=" + mao.lossyScale.x.ToString("F3")
                    + " lossySoquete=" + soquete.lossyScale.ToString("F4") + " corpo=" + corpo.ToString("F4")
                    + " antebraco=" + Vector3.Distance(mao.position, mao.parent.position).ToString("F3")
                    + " luva maior=" + maior.ToString("F3") + " tam=" + b.size.ToString("F3") + " centro-osso=" + dist.ToString("F3")
                    + " dedos x braco=" + dedos.ToString("F1") + "g dorso(osso)=" + dorsoNoOsso.ToString("F2")
                    + " dorso(mundo)=" + soquete.forward.ToString("F2") + " polegar(mundo)=" + soquete.right.ToString("F2");
                File.AppendAllText(Path.Combine(Application.dataPath, "..", "Logs", "diag-luva.txt"), diag + "\n");

                Assert.That(maior, Is.InRange(0.12f, 0.4f), "a luva tem que ter tamanho de MAO\n" + diag);
                Assert.Less(dist, 0.3f, "a luva saiu da mao\n" + diag);
                Assert.That(soquete.lossyScale.x, Is.EqualTo(corpo).Within(corpo * 0.01f), "o soquete nao cancelou a escala do osso\n" + diag);
                Assert.That(soquete.lossyScale.y, Is.EqualTo(corpo).Within(corpo * 0.01f), diag);
                Assert.Less(dedos, 45f, "os dedos da luva nao seguem o antebraco\n" + diag);
                int gemas = 0, runas = 0;
                foreach (Transform f in soquete)
                {
                    if (f.name.StartsWith("Gema")) gemas++;
                    if (f.name == "Runa") runas++;
                }
                if (externo)
                {
                    // medido no FBX da Pyra (pose de bind): palma para +X do osso, dorso para -X (cos 0,90 com o polegar ~+Z)
                    Assert.Greater(Vector3.Dot(dorsoNoOsso, Vector3.left), 0.8f, "o dorso da luva nao esta' no dorso da mao\n" + diag);
                    Mesh glb = Resources.Load<GameObject>(LootVisual.ModeloDe(arma)).GetComponentInChildren<MeshFilter>().sharedMesh;
                    bool mesma = false;
                    foreach (MeshFilter mf in luva.GetComponentsInChildren<MeshFilter>()) mesma |= mf.sharedMesh == glb;
                    Assert.IsTrue(mesma, "a luva da mao nao e' a mesma do chao (" + LootVisual.ModeloDe(arma) + ")");
                    // a cor do elemento acesa em cima da gema: a manopla acende as duas
                    Assert.AreEqual(arma == Arma.MANOPLA ? 2 : 0, gemas, diag);
                    Assert.AreEqual(arma == Arma.MANOPLA ? 0 : 1, runas, diag);
                }
                else
                    Assert.Greater(Vector3.Dot(dorsoNoOsso, Vector3.forward), 0.99f, "procedural: o dorso olha +Z do pivo Mao\n" + diag);
            }
            finally { Object.DestroyImmediate(m.gameObject); }
        }

        [TestCase(Arma.VARINHA)]
        [TestCase(Arma.CAJADO)]
        [TestCase(Arma.MANOPLA)]
        public void Luva_NoChao_EOModeloDaMao_CentradoNoTamanhoDoLoot(string arma)
        {
            var pai = new GameObject("PivoDoLoot");
            try
            {
                GameObject g = LuvaVisual.Modelo(arma, pai.transform, LootVisual.TAMANHO_LUVA);
                Assert.IsNotNull(g, "sem o .glb da luva " + arma);
                Bounds b = Limites(g);
                Assert.That(Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)), Is.EqualTo(LootVisual.TAMANHO_LUVA).Within(0.01f), b.ToString());
                Assert.That(b.size.y, Is.EqualTo(LootVisual.TAMANHO_LUVA).Within(0.01f), "a luva fica de pe' (dedos em +Y): " + b);
                Assert.Less(b.center.magnitude, 0.03f, "centrada no pivo: " + b);
            }
            finally { Object.DestroyImmediate(pai); }
        }

        static Bounds Limites(GameObject g)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>();
            Assert.Greater(rs.Length, 0, "luva sem nada para desenhar");
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
