using UnityEngine;
using UnityEngine.Rendering.Universal;
using Arkana.Core;

namespace Arkana.World
{
    /// <summary>
    /// Altura do jogador -> alcance da sombra. PURA (testavel sem cena).
    ///
    /// POR QUE NAO E' UM NUMERO FIXO: o atlas de sombra (2048 px, UM split — PSSM redesenha os
    /// casters uma vez por split, recusado no ARM64) cobre o alcance inteiro. A 60 m o texel sai
    /// ~6 cm, bom para o pe' do mago; a 380 m sai ~20 cm e a sombra PERTO vira borrao. Fixo em 60
    /// a ilha vista do castelo nao projeta UMA sombra; fixo em 380 o chao vira borrao. Entao o
    /// alcance ABRE no ar (quando o jogador le' o mapa) e FECHA ao pousar (onde passa a partida).
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
            if (urp != null)
            {
                urp.shadowDistance = alcance;
                urp.shadowCascadeCount = 1;   // um split so', sempre
            }
            QualitySettings.shadowDistance = alcance;
        }
    }
}
