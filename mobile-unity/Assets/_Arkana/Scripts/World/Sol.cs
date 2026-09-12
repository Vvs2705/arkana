using UnityEngine;
using UnityEngine.Rendering.Universal;
using Arkana.Core;

namespace Arkana.World
{
    /// <summary>
    /// Altura do jogador -> alcance da sombra. PURA (testavel sem cena).
    ///
    /// CASCATAS (onda 5C, 12/09). A regra antiga era UM split de 2048 px cobrindo o alcance inteiro (a
    /// recusa do PSSM vinha do Godot mobile): numa tela 20:9 a esfera de 60 m tem ~88 m de raio e o
    /// texel saia ~8,6 cm — a sombra do mago em degraus e borrada da foto 24. Agora o URP_Base tem atlas
    /// de 4096 em 4 cascatas (2048 px cada) com as fatias em 0,1/0,25/0,5 do alcance: no chao a 1a vai
    /// ate' 6 m (o mago a 4 m da camera e a sombra dele cabem nela, texel ~0,9 cm) e a ultima (30-60 m)
    /// fica no texel do split antigo. Cada cascata so' redesenha os casters da esfera dela.
    ///
    /// POR QUE O ALCANCE AINDA NAO E' UM NUMERO FIXO: as fatias sao FRACAO do alcance. Fixo em 380 a
    /// 1a cascata iria a 38 m e a sombra PERTO voltaria a borrao; fixo em 60 a ilha vista do castelo
    /// nao projeta UMA sombra. Entao o alcance ABRE no ar (quando o jogador le' o mapa) e FECHA ao
    /// pousar (onde passa a partida).
    /// </summary>
    public static class SombraDoSol
    {
        /// <summary>Em repouso, mago no chao. Teto do jogo a pe'.</summary>
        public const float Perto = 60f;
        /// <summary>Teto no ar: cobre a ilha de 600 m com folga.</summary>
        public const float Longe = 380f;
        /// <summary>Metros de alcance por metro de altura: a 190 m ja' bate o teto.</summary>
        public const float PorMetro = 2f;

        public static float AlcancePara(float metros)
        {
            // `!(x > 0)` barra NaN, zero e negativo de uma vez (mesmo idioma de Combat).
            if (!(metros > 0f)) return Perto;
            return Mathf.Clamp(metros * PorMetro, Perto, Longe);
        }
    }

    /// <summary>
    /// A luz direcional da ilha. Casca: escuta Bus.QuedaAltura/QuedaFase e aplica SombraDoSol
    /// no asset URP ativo (QualitySettings.shadowDistance nao manda no URP; fica como fallback).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class Sol : MonoBehaviour
    {
        void Awake()
        {
            var luz = GetComponent<Light>();
            luz.type = LightType.Directional;
            luz.shadows = LightShadows.Soft;
            // O sol do Godot (Island.tscn, no "Sun"): quente, forte, sombra quase cheia. Sem ele o toon fica frio e chapado.
            luz.color = new Color(1f, 0.86f, 0.63f);
            luz.intensity = 1.55f;
            luz.shadowStrength = 0.92f;
            // Macia ALTA por luz. Sem o UniversalAdditionalLightData (a cena e o Main criam a luz sem ele) o URP
            // amostra a macia BAIXA (4 taps) e escala o bias para o kernel 5x5; com Alta os dois batem no 7x7.
            luz.GetUniversalAdditionalLightData().softShadowQuality = SoftShadowQuality.High;
            if (transform.rotation == Quaternion.identity)
                transform.rotation = Quaternion.Euler(30f, -28f, 0f);   // 30 graus como no Godot: sombra longa, relevo legivel
        }

        void OnEnable()
        {
            Aplicar(SombraDoSol.Perto);
            Bus.QuedaAltura += AoMudarAltura;
            Bus.QuedaFase += AoMudarFase;
        }

        void OnDisable()
        {
            Bus.QuedaAltura -= AoMudarAltura;
            Bus.QuedaFase -= AoMudarFase;
        }

        void AoMudarAltura(float metros, float velocidade)
        {
            Aplicar(SombraDoSol.AlcancePara(metros));
        }

        void AoMudarFase(string fase)
        {
            // Pousou = vai andar. Fecha mesmo que o ultimo altimetro tenha chegado com altura residual.
            if (fase == "pousou") Aplicar(SombraDoSol.Perto);
        }

        public static void Aplicar(float alcance)
        {
            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            // resolucao, cascatas, macia e bias moram no URP_Base. ponytail: bias 1/1 do URP (o 7x7 escala para 3,5 texels:
            // sem acne ate' ~70 graus de rasante; na 1a cascata o calcanhar fica ~3 cm acima da sombra e o SSAO fecha o
            // contato). Pe' flutuando na foto: depth bias 0,6 no asset e conferir a acne na encosta.
            if (urp != null) urp.shadowDistance = alcance;
            QualitySettings.shadowDistance = alcance;
        }

        /// <summary>
        /// A sombra que o URP ativo vai desenhar, numa linha: vai no diag.txt da foto e o teste cobra. Regressao
        /// (cascata 1, sombra dura, SSAO fora do renderer) aparece aqui antes de aparecer no aparelho.
        /// O SSAO do URP_Base_Renderer e' DEPOIS DO OPACO e le' so' a PROFUNDIDADE: os shaders Arkana nao leem
        /// _SCREEN_SPACE_OCCLUSION nem tem passe DepthNormals, entao o AO multiplica o quadro pronto e a profundidade
        /// vem da copia (sem pre-passe). Ele mora no asset, nao em codigo: o build so' leva o shader do SSAO se o
        /// renderer tiver a feature. ponytail: depois do opaco o AO escurece tambem a luz direta; o certo e' o toon ler
        /// _SCREEN_SPACE_OCCLUSION so' no ambiente (troca nos shaders) se o vinco sob o sol pesar na foto.
        /// </summary>
        public static string Estado()
        {
            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            if (urp == null) return "sem URP";
            bool ssao = false;
            foreach (ScriptableRendererData d in urp.rendererDataList)
                if (d != null)
                    foreach (ScriptableRendererFeature f in d.rendererFeatures)
                        ssao |= f is ScreenSpaceAmbientOcclusion && f.isActive;
            return urp.mainLightShadowmapResolution + "px x" + urp.shadowCascadeCount
                + (urp.supportsSoftShadows ? " macia" : " dura") + (ssao ? " ssao" : " sem-ssao");
        }
    }
}
