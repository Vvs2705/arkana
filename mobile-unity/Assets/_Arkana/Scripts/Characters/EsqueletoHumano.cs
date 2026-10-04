using System.Collections.Generic;
using UnityEngine;

namespace Arkana.Characters
{
    /// <summary>
    /// O esqueleto da Meshy (28 ossos, os 20 magos) visto pelo MECANIM HUMANOID — o primeiro passo da biblioteca Mixamo
    /// (BLOCO C, 04/10/2026): com um Avatar humano, um clipe do Mixamo serve nos 20 sem retarget a mao. Nada aqui muda o que
    /// toca hoje (Animation legado + PoseMago): e' o mapa e a prova de que o Avatar fecha (CharactersCorpoMagoTests).
    /// ARMADILHA: a coluna da Meshy tem os nomes INVERTIDOS — Spine02 e' a BASE (filho do Hips) e Spine o topo.
    /// </summary>
    public static class EsqueletoHumano
    {
        /// <summary>(osso do Mecanim, osso da Meshy). Os 15 obrigatorios + peito, peito alto, pescoco, ombros e dedos do pe'.
        /// Dedos da mao, olhos e mandibula nao existem no rig (opcionais no Mecanim).</summary>
        public static readonly string[,] Mapa =
        {
            { "Hips", "Hips" }, { "Spine", "Spine02" }, { "Chest", "Spine01" }, { "UpperChest", "Spine" },
            { "Neck", "neck" }, { "Head", "Head" },
            { "LeftShoulder", "LeftShoulder" }, { "LeftUpperArm", "LeftArm" }, { "LeftLowerArm", "LeftForeArm" }, { "LeftHand", "LeftHand" },
            { "RightShoulder", "RightShoulder" }, { "RightUpperArm", "RightArm" }, { "RightLowerArm", "RightForeArm" }, { "RightHand", "RightHand" },
            { "LeftUpperLeg", "LeftUpLeg" }, { "LeftLowerLeg", "LeftLeg" }, { "LeftFoot", "LeftFoot" }, { "LeftToes", "LeftToeBase" },
            { "RightUpperLeg", "RightUpLeg" }, { "RightLowerLeg", "RightLeg" }, { "RightFoot", "RightFoot" }, { "RightToes", "RightToeBase" },
        };

        /// <summary>O osso pelo nome EXATO (ou com prefixo "algo:", como o "mixamorig:"). Nunca "contem": Spine casaria Spine01.</summary>
        public static Transform Osso(Transform raiz, string nome)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if (t.name == nome || t.name.EndsWith(":" + nome)) return t;
            return null;
        }

        /// <summary>
        /// Poe os ossos na POSE DE BIND do proprio FBX (Mesh.bindposes). O prefab importado guarda a pose do PRIMEIRO take
        /// (Running), nao a T-pose: um Avatar montado nela teria a corrida como "pose zero" e todo clipe sairia torto.
        /// </summary>
        public static void PoseDeBind(SkinnedMeshRenderer smr)
        {
            if (smr == null || smr.sharedMesh == null) return;
            Transform[] ossos = smr.bones;
            Matrix4x4[] bind = smr.sharedMesh.bindposes;
            Matrix4x4 raiz = smr.transform.localToWorldMatrix;
            var porProfundidade = new List<int>();
            for (int i = 0; i < ossos.Length && i < bind.Length; i++) if (ossos[i] != null) porProfundidade.Add(i);
            porProfundidade.Sort((a, b) => Profundidade(ossos[a]).CompareTo(Profundidade(ossos[b])));   // pai antes do filho
            foreach (int i in porProfundidade)
            {
                Matrix4x4 m = raiz * bind[i].inverse;
                ossos[i].SetPositionAndRotation(m.GetColumn(3), m.rotation);
            }
        }

        static int Profundidade(Transform t) { int n = 0; for (; t != null; t = t.parent) n++; return n; }

        /// <summary>A descricao humana do rig em `raiz` (que ja' deve estar na pose de bind), ou null se faltar osso obrigatorio
        /// (`faltando` diz qual).</summary>
        public static HumanDescription? Descricao(Transform raiz, out string faltando)
        {
            faltando = null;
            var humanos = new List<HumanBone>();
            for (int i = 0; i < Mapa.GetLength(0); i++)
            {
                Transform osso = Osso(raiz, Mapa[i, 1]);
                if (osso == null) { faltando = Mapa[i, 1]; return null; }
                var hb = new HumanBone { humanName = Mapa[i, 0], boneName = osso.name };
                hb.limit.useDefaultValues = true;
                humanos.Add(hb);
            }
            var esqueleto = new List<SkeletonBone>();
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                esqueleto.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            return new HumanDescription
            {
                human = humanos.ToArray(), skeleton = esqueleto.ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
        }

        /// <summary>Monta o Avatar humano do modelo (pose de bind + T-pose forcada + mapa). Null = o rig nao fecha (falta osso).</summary>
        public static Avatar Construir(GameObject modelo, out string faltando)
        {
            PoseDeBind(modelo.GetComponentInChildren<SkinnedMeshRenderer>(true));
            ForcarTPose(modelo.transform);
            HumanDescription? d = Descricao(modelo.transform, out faltando);
            return d.HasValue ? AvatarBuilder.BuildHumanAvatar(modelo, d.Value) : null;
        }

        /// <summary>
        /// T-POSE FORCADA (o "Enforce T-Pose" do importador, feito em runtime): braco e antebraco na horizontal, para fora
        /// (o mago olha +Z: a esquerda e' -X). O Mixamo conta a partir da T-pose; Basalto (34 graus abaixo), Gromm (36),
        /// Corvus (43) e Vitalis (72) vem de braco baixo no bind e o clipe sairia com o braco ~35 graus mais baixo.
        /// </summary>
        public static void ForcarTPose(Transform raiz)
        {
            Vector3 fora = raiz.right;
            Alinhar(Osso(raiz, "LeftArm"), Osso(raiz, "LeftForeArm"), -fora);
            Alinhar(Osso(raiz, "LeftForeArm"), Osso(raiz, "LeftHand"), -fora);
            Alinhar(Osso(raiz, "RightArm"), Osso(raiz, "RightForeArm"), fora);
            Alinhar(Osso(raiz, "RightForeArm"), Osso(raiz, "RightHand"), fora);
        }

        static void Alinhar(Transform osso, Transform filho, Vector3 alvo)
        {
            if (osso == null || filho == null) return;
            Vector3 d = filho.position - osso.position;
            if (d.sqrMagnitude < 1e-10f) return;
            osso.rotation = Quaternion.FromToRotation(d, alvo) * osso.rotation;
        }

        /// <summary>Graus do braco (ombro -> cotovelo) abaixo da horizontal na pose atual: ~0 = T-pose (o Mixamo encaixa
        /// direto); ~45 = A-pose; ~80 = braco caido (precisa de "Enforce T-Pose" antes do Mixamo).</summary>
        public static float BracoAbaixoDaHorizontal(Transform raiz)
        {
            Transform a = Osso(raiz, "LeftArm"), c = Osso(raiz, "LeftForeArm");
            if (a == null || c == null) return float.NaN;
            Vector3 d = (c.position - a.position).normalized;
            return Mathf.Asin(Mathf.Clamp(-d.y, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }
}
