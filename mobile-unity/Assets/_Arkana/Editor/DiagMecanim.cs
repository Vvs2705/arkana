using System.Text;
using UnityEditor;
using UnityEngine;
using Arkana.Characters;

namespace Arkana.EditorTools
{
    /// <summary>
    /// Diagnostico do retarget (04/10/2026): direcao dos bracos (ombro -> cotovelo) no espaco do modelo — no prefab, no bind,
    /// depois da T-pose forcada e depois do Animator tocar o idle do Mixamo. Grava Logs/mecanim-diag.txt. Ferramenta de
    /// bancada (menu Arkana), nao roda no jogo.
    /// </summary>
    public static class DiagMecanim
    {
        [MenuItem("Arkana/Diagnostico do Mecanim (bracos)")]
        public static void Rodar()
        {
            var sb = new StringBuilder("ARKANA MECANIM DIAG (direcoes no espaco do modelo; +X direita, +Y cima, +Z frente)\n");
            foreach (string slug in new[] { "05-corvomante", "16-fizz", "18-basalto" })
            {
                var prefab = Resources.Load<GameObject>("magos/" + slug);
                GameObject g = Object.Instantiate(prefab);
                Transform r = g.transform;
                sb.AppendLine("== " + slug + " filhos da raiz: " + Filhos(r));
                sb.AppendLine("  prefab : " + Bracos(r));
                EsqueletoHumano.PoseDeBind(g.GetComponentInChildren<SkinnedMeshRenderer>(true));
                sb.AppendLine("  bind   : " + Bracos(r));
                EsqueletoHumano.ForcarTPose(r);
                sb.AppendLine("  tpose  : " + Bracos(r));
                Object.DestroyImmediate(g);

                g = Object.Instantiate(prefab);
                r = g.transform;
                foreach (Animation an in g.GetComponentsInChildren<Animation>()) an.enabled = false;
                Avatar av = EsqueletoHumano.Construir(g, out string falta);
                var a = g.AddComponent<Animator>();
                a.avatar = av;
                a.runtimeAnimatorController = MagoMecanim.Ctrl();
                a.applyRootMotion = false;
                a.Update(0f);
                for (int i = 0; i < 30; i++) a.Update(1f / 30f);
                sb.AppendLine("  idle   : " + Bracos(r) + " humanScale=" + a.humanScale.ToString("F2") + " falta=" + falta);
                Transform la = a.GetBoneTransform(HumanBodyBones.LeftUpperArm), ra = a.GetBoneTransform(HumanBodyBones.RightUpperArm);
                Transform ls = a.GetBoneTransform(HumanBodyBones.LeftShoulder), rs = a.GetBoneTransform(HumanBodyBones.RightShoulder);
                Transform ch = a.GetBoneTransform(HumanBodyBones.Chest), sp = a.GetBoneTransform(HumanBodyBones.Spine), uc = a.GetBoneTransform(HumanBodyBones.UpperChest);
                sb.AppendLine("  mapa   : LUpperArm=" + N(la) + " RUpperArm=" + N(ra) + " LShoulder=" + N(ls) + " RShoulder=" + N(rs)
                    + " Spine=" + N(sp) + " Chest=" + N(ch) + " UpperChest=" + N(uc));
                Object.DestroyImmediate(g);
            }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/mecanim-diag.txt", sb.ToString());
            Debug.Log("ARKANA MIXAMO diag gravado");
        }

        static string N(Transform t) => t != null ? t.name : "null";

        static string Filhos(Transform r)
        {
            var s = new StringBuilder();
            foreach (Transform t in r) s.Append(t.name + " rot=" + t.localEulerAngles.ToString("F0") + " esc=" + t.localScale.ToString("F0") + "; ");
            return s.ToString();
        }

        static string Bracos(Transform r)
        {
            return "E " + Dir(r, "LeftArm", "LeftForeArm") + " D " + Dir(r, "RightArm", "RightForeArm")
                + " antebracoE " + Dir(r, "LeftForeArm", "LeftHand") + " cima(Hips->Head) " + Dir(r, "Hips", "Head");
        }

        static string Dir(Transform r, string a, string b)
        {
            Transform ta = EsqueletoHumano.Osso(r, a), tb = EsqueletoHumano.Osso(r, b);
            if (ta == null || tb == null) return "?";
            Vector3 d = r.InverseTransformDirection(tb.position - ta.position).normalized;
            return d.ToString("F2");
        }
    }
}
