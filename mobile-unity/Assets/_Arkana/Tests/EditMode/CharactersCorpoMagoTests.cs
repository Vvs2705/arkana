using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Arkana.Characters;

namespace Arkana.Tests
{
    /// <summary>
    /// O CORPO da onda 15B no FBX de verdade (so' roda no Unity): o take importado nao leva o Hips junto (o importador tira
    /// o deslocamento), o Mago mede as fases do pulo no proprio take, o cast correndo vai so' no tronco, e quem ainda nao
    /// tem os takes novos cai na reserva — nunca no Idle.
    /// </summary>
    public class CharactersCorpoMagoTests
    {
        const string Pasta = "Assets/_Arkana/Resources/magos/";

        static Mago Criar(string slug)
        {
            // o Mago instancia materiais (o tint de um bot nao pinta o player); no EditMode isso loga erro
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            return Mago.Criar(null, slug);
        }

        static Vector3 Em(AnimationCurve[] eixo, float t, Vector3 rep) => new Vector3(
            eixo[0] != null ? eixo[0].Evaluate(t) : rep.x, eixo[1] != null ? eixo[1].Evaluate(t) : rep.y, eixo[2] != null ? eixo[2].Evaluate(t) : rep.z);

        [Test]
        public void TakesDoCorpo_NoFbxImportado_OHipsSoAgacha_NuncaSobeNemAnda()
        {
            int vistos = 0;
            foreach (string slug in Arkana.Core.Kits.Slugs)
            {
                string caminhoFbx = Pasta + slug + ".fbx";
                var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoFbx);
                if (modelo == null) continue;
                Transform hips = null;
                foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("Hips")) { hips = t; break; }
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminhoFbx))
                {
                    var clip = o as AnimationClip;
                    if (clip == null || clip.name.StartsWith("__preview__")) continue;
                    Clipe? c = PoseMago.Alias(clip.name);
                    if (c != Clipe.Pular && c != Clipe.AndarTras) continue;
                    Assert.IsNotNull(hips, slug + ": sem Hips");
                    string caminho = AnimationUtility.CalculateTransformPath(hips, modelo.transform);
                    var eixo = new AnimationCurve[3];
                    foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip))
                        if (b.path == caminho && b.type == typeof(Transform) && b.propertyName.Contains("LocalPosition"))
                        {
                            int e = "xyz".IndexOf(b.propertyName[b.propertyName.Length - 1]);
                            if (e >= 0) eixo[e] = AnimationUtility.GetEditorCurve(clip, b);
                        }
                    Vector3 cima = hips.parent.InverseTransformDirection(modelo.transform.up).normalized;
                    Vector3 rep = hips.localPosition, p0 = Em(eixo, 0f, rep);
                    float u0 = Vector3.Dot(p0, cima), tol = Mathf.Abs(u0) * 0.01f + 1e-6f, fundo = 0f;
                    for (float t = 0f; t <= clip.length; t += 1f / 120f)
                    {
                        Vector3 p = Em(eixo, t, rep);
                        float u = Vector3.Dot(p, cima);
                        Assert.LessOrEqual(u, u0 + tol, slug + " " + clip.name + ": o Hips SOBE em t=" + t.ToString("F3"));
                        Vector3 anda = (p - cima * u) - (p0 - cima * u0);
                        Assert.Less(anda.magnitude, tol, slug + " " + clip.name + ": o Hips ANDA em t=" + t.ToString("F3"));
                        fundo = Mathf.Max(fundo, u0 - u);
                    }
                    if (c == Clipe.Pular) Assert.Greater(fundo, Mathf.Abs(u0) * 0.1f, slug + ": o pulo perdeu a agachada");
                    vistos++;
                }
            }
            if (vistos == 0) Assert.Ignore("nenhum FBX do elenco com Regular_Jump/Walk_Backward ainda");
        }

        [Test]
        public void ComOTake_OMagoMedeAsFasesDoPulo_EAAterrissagemEOMesmoTake()
        {
            int vistos = 0;
            foreach (string slug in CharactersMagoExternoTests.SlugsExternos())
            {
                Mago m = Criar(slug);
                try
                {
                    if (!m.TemClipe("Regular_Jump")) continue;
                    Assert.IsTrue(m.TemClipe("pousar"), slug + ": o pouso sai do mesmo take");
                    // medido no 01-pyra.fbx (Blender): os dedos saem do chao em ~0,52 s e voltam em ~1,12 s
                    Assert.That(m.FasesDoPulo.x, Is.InRange(0.35f, 0.65f), slug + " decolagem");
                    Assert.That(m.FasesDoPulo.y, Is.InRange(0.95f, 1.25f), slug + " pouso");
                    m.VooS = 1.06f;
                    m.Play(Clipe.Pular);
                    Assert.AreEqual(Clipe.Pular, m.ClipeAtual);
                    StringAssert.Contains("t=" + m.FasesDoPulo.x.ToString("F2"), m.Estado(), "o ar comeca na decolagem, sem a agachada");
                    m.Play(Clipe.Pousar);
                    Assert.AreEqual(Clipe.Pousar, m.ClipeAtual);
                    vistos++;
                }
                finally { Object.DestroyImmediate(m.gameObject); }
            }
            if (vistos == 0) Assert.Ignore("nenhum mago com Regular_Jump ainda");
        }

        [Test]
        public void SemOTake_OPuloSeguraAQueda_EORecuoEACorridaAoContrario()
        {
            int vistos = 0;
            foreach (string slug in CharactersMagoExternoTests.SlugsExternos())
            {
                Mago m = Criar(slug);
                try
                {
                    if (!m.TemClipe("Regular_Jump"))
                    {
                        m.Play(Clipe.Pular);
                        Assert.AreEqual(Clipe.Run, m.ClipeAtual, slug + ": sem o take o ar e' o passo no ar da corrida, nao o Idle");
                        StringAssert.Contains("#salto", m.Estado(), slug + ": numa copia da corrida");
                        StringAssert.Contains("v=" + 0f.ToString("F2"), m.Estado(), slug + ": o quadro fica SEGURO");
                        m.SetVelocidade(0f);
                        m.SetVelocidade(7.5f);
                        Assert.AreEqual(Clipe.Pular, m.ClipePedido, slug + ": a passada nao tira o salto do ar");
                        StringAssert.Contains("v=" + 0f.ToString("F2"), m.Estado(), slug + ": nem descongela o quadro");
                        m.Play(Clipe.Pousar);
                        Assert.AreEqual(Clipe.Idle, m.ClipeAtual);
                        vistos++;
                    }
                    if (!m.TemClipe("Walk_Backward"))
                    {
                        m.SetVelocidade(5f);
                        m.Play(Clipe.AndarTras);
                        Assert.AreEqual(Clipe.Run, m.ClipeAtual, slug);
                        Assert.AreEqual(Clipe.AndarTras, m.ClipePedido, slug);
                        m.SetVelocidade(5f);
                        Assert.AreEqual(Clipe.AndarTras, m.ClipePedido, slug + ": a passada nao volta sozinha para a corrida de frente");
                        vistos++;
                    }
                }
                finally { Object.DestroyImmediate(m.gameObject); }
            }
            if (vistos == 0) Assert.Ignore("todo o elenco ja' tem os takes novos");
        }

        [Test]
        public void CastCorrendo_VaiSoNoTronco_ParadoECorpoInteiro()
        {
            foreach (string slug in CharactersMagoExternoTests.SlugsExternos())
            {
                Mago m = Criar(slug);
                try
                {
                    m.SetVelocidade(7.5f);
                    Assert.AreEqual(Clipe.Run, m.ClipeAtual, slug);
                    m.Play(Clipe.Cast);
                    Assert.IsTrue(m.CastNoTronco, slug + ": correndo, o cast vai so' no tronco");
                    Assert.AreEqual(Clipe.Run, m.ClipeAtual, slug + ": as pernas seguem correndo");
                    m.Play(Clipe.Run);   // o gesto do Pawn acabou
                    Assert.IsFalse(m.CastNoTronco, slug);
                    m.SetVelocidade(0f);
                    Assert.AreEqual(Clipe.Idle, m.ClipeAtual, slug);
                    m.Play(Clipe.Cast);
                    Assert.IsFalse(m.CastNoTronco, slug + ": parado, o cast e' de corpo inteiro");
                    Assert.AreEqual(Clipe.Cast, m.ClipeAtual, slug);
                    m.SetVelocidade(7.5f);   // saiu correndo no meio do gesto
                    Assert.IsTrue(m.CastNoTronco, slug + ": o braco passa para o tronco");
                    Assert.AreEqual(Clipe.Run, m.ClipeAtual, slug + ": e as pernas voltam a andar");
                }
                finally { Object.DestroyImmediate(m.gameObject); }
                return;   // um mago basta: o elenco inteiro tem a mesma coluna
            }
            Assert.Ignore("nenhum FBX do elenco");
        }

        // ------------------------------------------------------------------ 04/10: Mecanim Humanoid (BLOCO C)

        /// <summary>O ANIMATION VALIDATION SET (escolhido por DADO em 04/10): o mais compacto que pisa (Fizz, 0,95 m, quadril a
        /// 30% da altura), a mediana do elenco (Corvomante, 1,80 m, escala 1,00) e o maior/mais largo (Basalto, 2,30 m, ombro
        /// 75,6 cm). A Pip (0,60 m) voa a 1,2 m e nunca pisa: patinar e pousar nao aparecem nela.</summary>
        public static readonly string[] ValidationSet = { "16-fizz", "05-corvomante", "18-basalto" };

        [Test]
        public void Humanoide_OEsqueletoDaMeshyViraAvatarHumanoValido_NosVinte()
        {
            var linhas = new System.Text.StringBuilder("slug | avatar humano | braco abaixo da horizontal no bind (graus)\n");
            var falhas = new System.Collections.Generic.List<string>();
            int vistos = 0;
            foreach (string slug in Arkana.Core.Kits.Slugs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pasta + slug + ".fbx");
                if (prefab == null) continue;
                vistos++;
                GameObject g = Object.Instantiate(prefab);
                try
                {
                    Avatar av = EsqueletoHumano.Construir(g, out string faltando);
                    bool ok = av != null && av.isValid && av.isHuman;
                    float braco = EsqueletoHumano.BracoAbaixoDaHorizontal(g.transform);
                    linhas.AppendLine(slug + " | " + (ok ? "OK" : "FALHOU " + faltando) + " | " + braco.ToString("F0"));
                    if (!ok) falhas.Add(slug + (faltando != null ? " (sem " + faltando + ")" : ""));
                    if (av != null) Object.DestroyImmediate(av);
                }
                finally { Object.DestroyImmediate(g); }
            }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/humanoide.txt", linhas.ToString());
            Assert.AreEqual(20, vistos, "os 20 FBX existem");
            CollectionAssert.IsEmpty(falhas, "Avatar humano:\n" + linhas);
            foreach (string slug in ValidationSet) StringAssert.Contains(slug + " | OK", linhas.ToString(), "o Validation Set fecha");
        }

        [Test]
        public void Humanoide_ColunaInvertidaDaMeshy_OSpineDoMecanimEOFilhoDoHips()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pasta + "05-corvomante.fbx");
            Transform hips = EsqueletoHumano.Osso(prefab.transform, "Hips");
            Transform baseDaColuna = EsqueletoHumano.Osso(prefab.transform, "Spine02");
            Assert.AreSame(hips, baseDaColuna.parent, "a base da coluna e' o Spine02 (o Spine e' o topo): o mapa automatico erraria");
            Assert.IsNull(EsqueletoHumano.Osso(prefab.transform, "pine02"), "nome exato: nada de 'contem'");
        }
    }
}
