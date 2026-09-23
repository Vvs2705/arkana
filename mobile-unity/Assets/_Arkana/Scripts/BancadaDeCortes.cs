using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Arkana.Core;

namespace Arkana
{
    /// <summary>
    /// BANCADA DE CORTES NO APARELHO (21/09/2026): o Poco F4 mediu 17-20 FPS no chao (60 em 12/09, antes das ondas 5-17) e o
    /// processador sobra (UnityMain 17% de um nucleo): o gargalo e' a GPU. Em vez de um APK por hipotese, UMA partida mede o
    /// custo de cada corte: `--ei arkana_bancada 1` junto de `--es arkana_auto partida`. Depois do pouso, cada corte roda
    /// 10 s (2 s para assentar + 8 medidos) partindo do jogo inteiro, e o logcat recebe
    /// "ARKANA BANCADA <corte> fps=.. ms=.. pior=..ms". O jogador fica parado e com vida cheia (a vista nao muda).
    /// Sem o extra, nada disto existe.
    /// </summary>
    public sealed class BancadaDeCortes : MonoBehaviour
    {
        const float ASSENTAR = 2f, MEDIR = 8f, DEPOIS_DO_POUSO = 6f;

        /// <summary>Padrao; `--es arkana_cortes "base,ssao+sombra_dura,..."` troca a lista sem APK novo.</summary>
        static readonly string[] CortesPadrao =
        {
            "base", "sem_ssao", "sombra_2cascatas_2048", "sem_sombra", "escala_085", "escala_070", "sem_hdr", "sem_pos",
            "sem_grama", "sem_vegetacao", "chao_simples", "sem_agua", "sem_nuvens",
            "ssao+sombra2+escala085", "base_de_novo",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Ligar()
        {
            if (PartidaPeloAdb.BancadaPedida() <= 0) return;
            var go = new GameObject("BancadaDeCortes");
            DontDestroyOnLoad(go);
            go.AddComponent<BancadaDeCortes>();
        }

        UniversalRenderPipelineAsset _urp;
        float _escala, _distSombra;
        int _cascatas, _resSombra;
        bool _hdr, _pousou;
        Light _sol;
        LightShadows _sombraSol;
        SoftShadowQuality _suave;
        readonly List<ScriptableRendererFeature> _ssao = new List<ScriptableRendererFeature>();
        bool _ssaoLigado;   // estado REAL do asset; desde 21/09 e' desligado e o build nem leva os recursos do SSAO

        void OnEnable() { Bus.QuedaFase += AoFase; }
        void OnDisable() { Bus.QuedaFase -= AoFase; }
        void AoFase(string f) { if (f == Gameplay.Queda.POUSOU && !_pousou) { _pousou = true; StartCoroutine(Rodar()); } }

        IEnumerator Rodar()
        {
            _urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_urp == null) { Debug.Log("ARKANA BANCADA sem URP"); yield break; }
            _escala = _urp.renderScale; _cascatas = _urp.shadowCascadeCount; _resSombra = _urp.mainLightShadowmapResolution;
            _distSombra = _urp.shadowDistance; _hdr = _urp.supportsHDR;
            AcharSsao();
            _ssaoLigado = _ssao.Count > 0 && _ssao[0].isActive;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && (_sol == null || l.intensity > _sol.intensity)) _sol = l;
            if (_sol != null) { _sombraSol = _sol.shadows; _suave = _sol.GetUniversalAdditionalLightData().softShadowQuality; }
            Debug.Log("ARKANA BANCADA inicio escala=" + _escala + " cascatas=" + _cascatas + " sombra=" + _resSombra + " dist=" + _distSombra
                + " hdr=" + _hdr + " ssao=" + _ssao.Count + (_ssaoLigado ? " ligado" : " desligado") + " tela=" + Screen.width + "x" + Screen.height);
            yield return new WaitForSecondsRealtime(DEPOIS_DO_POUSO);
            string pedidos = PartidaPeloAdb.CortesPedidos();
            string[] cortes = string.IsNullOrEmpty(pedidos) ? CortesPadrao : pedidos.Split(',');
            foreach (string corte in cortes)
            {
                Restaurar();
                Aplicar(corte);
                yield return new WaitForSecondsRealtime(ASSENTAR);
                float t = 0f, pior = 0f;
                int quadros = 0;
                while (t < MEDIR)
                {
                    yield return null;
                    float dt = Time.unscaledDeltaTime;
                    t += dt; quadros++;
                    if (dt > pior) pior = dt;
                    SegurarJogador();
                }
                Debug.Log(string.Format("ARKANA BANCADA {0} fps={1:F1} ms={2:F1} pior={3:F1}ms", corte, quadros / t, 1000f * t / quadros, pior * 1000f));
            }
            Restaurar();
            Debug.Log("ARKANA BANCADA FIM");
        }

        /// <summary>O time do jogador nao cai nem morre durante a bancada (em 21/09 a dupla perdeu no meio e a bancada mediu o
        /// cartao de fim): levanta quem caiu e enche a vida a cada quadro.</summary>
        static void SegurarJogador()
        {
            Gameplay.Partida partida = Gameplay.Partida.Atual;
            if (partida == null || partida.Player == null) return;
            foreach (IEntidade e in partida.Arena)
            {
                if (e == null || e.Vital == null || !e.Vital.Viva || !Combat.MesmoTime(e, partida.Player)) continue;
                if (Gameplay.Derrubado.Esta(e)) Gameplay.Derrubado.Reerguer(e);
                e.Vital.Hp = e.Vital.HpMax;
            }
        }

        void AcharSsao()
        {
            // ponytail: reflexao no campo serializado do asset (a lista de renderers nao e' publica no URP 17); so' a bancada usa
            FieldInfo f = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (!(f?.GetValue(_urp) is ScriptableRendererData[] lista)) return;
            foreach (ScriptableRendererData d in lista)
                if (d != null)
                    foreach (ScriptableRendererFeature rf in d.rendererFeatures)
                        if (rf != null && rf.GetType().Name.Contains("AmbientOcclusion")) _ssao.Add(rf);
        }

        void Restaurar()
        {
            _urp.renderScale = _escala; _urp.shadowCascadeCount = _cascatas; _urp.mainLightShadowmapResolution = _resSombra;
            _urp.shadowDistance = _distSombra; _urp.supportsHDR = _hdr;
            // Restaurar ao que ERA, nunca a "ligado": religar SSAO num build que o retirou faz o URP reclamar
            // "Couldn't find the required resources for the ScreenSpaceAmbientOcclusion" a cada quadro (visto no AVD, 23/09)
            foreach (ScriptableRendererFeature rf in _ssao) rf.SetActive(_ssaoLigado);
            Camera cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Ligados(true, true, true);
            Floats(1f);
            if (_sol != null) { _sol.shadows = _sombraSol; _sol.GetUniversalAdditionalLightData().softShadowQuality = _suave; }
        }

        void Aplicar(string corte)
        {
            foreach (string c in corte.Split('+'))
            {
                switch (c)
                {
                    case "sem_ssao": case "ssao": foreach (ScriptableRendererFeature rf in _ssao) rf.SetActive(false); break;
                    case "sombra_2cascatas_2048": case "sombra2": _urp.shadowCascadeCount = 2; _urp.mainLightShadowmapResolution = 2048; break;
                    case "sem_sombra": _urp.shadowDistance = 0f; break;
                    case "escala_085": case "escala085": _urp.renderScale = 0.85f; break;
                    case "escala_070": _urp.renderScale = 0.7f; break;
                    case "escala_080": case "escala080": _urp.renderScale = 0.8f; break;
                    case "escala_075": case "escala075": _urp.renderScale = 0.75f; break;
                    case "sombra_dura": if (_sol != null) _sol.shadows = LightShadows.Hard; break;
                    case "sombra_suave_baixa": if (_sol != null) _sol.GetUniversalAdditionalLightData().softShadowQuality = SoftShadowQuality.Low; break;
                    case "sombra_suave_media": if (_sol != null) _sol.GetUniversalAdditionalLightData().softShadowQuality = SoftShadowQuality.Medium; break;
                    case "sem_hdr": _urp.supportsHDR = false; break;
                    case "sem_pos": Camera cam = Camera.main; if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = false; break;
                    case "sem_grama": Ligados(false, true, true); break;
                    case "sem_vegetacao": Ligados(true, false, true); break;
                    case "sem_agua": Ligados(true, true, false); break;
                    case "chao_simples": Floats(0f); break;
                    case "sem_nuvens": foreach (Material m in Materiais("_Cobertura")) m.SetFloat("_Cobertura", 0f); break;
                }
            }
        }

        // ponytail: os valores originais dos KNOBs de material ficam guardados na 1a vez; so' a bancada mexe neles
        readonly Dictionary<Material, Vector3> _orig = new Dictionary<Material, Vector3>();

        void Floats(float f)
        {
            foreach (Material m in Materiais("_Fissura"))
            {
                if (!_orig.ContainsKey(m)) _orig[m] = new Vector3(m.GetFloat("_Fissura"), m.HasProperty("_Pintado") ? m.GetFloat("_Pintado") : 0f,
                    m.HasProperty("_Cobertura") ? m.GetFloat("_Cobertura") : 0f);
                Vector3 o = _orig[m];
                m.SetFloat("_Fissura", o.x * f);
                if (m.HasProperty("_Pintado")) m.SetFloat("_Pintado", o.y * f);
            }
            foreach (Material m in Materiais("_Cobertura"))
            {
                if (!_orig.ContainsKey(m)) _orig[m] = new Vector3(0f, 0f, m.GetFloat("_Cobertura"));
                m.SetFloat("_Cobertura", _orig[m].z);
            }
        }

        static IEnumerable<Material> Materiais(string prop)
        {
            var vistos = new HashSet<Material>();
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (Material m in r.sharedMaterials)
                    if (m != null && m.HasProperty(prop) && vistos.Add(m)) yield return m;
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty(prop) && vistos.Add(RenderSettings.skybox)) yield return RenderSettings.skybox;
        }

        static void Ligados(bool grama, bool vegetacao, bool agua)
        {
            World.Ilha ilha = World.Ilha.Atual;
            if (ilha == null) return;
            if (ilha.Grama != null) ilha.Grama.gameObject.SetActive(grama);
            if (ilha.Vegetacao != null) ilha.Vegetacao.gameObject.SetActive(vegetacao);
            foreach (Renderer r in ilha.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                    if (m != null && m.shader != null && m.shader.name.Contains("Agua")) { r.enabled = agua; break; }
        }
    }
}
