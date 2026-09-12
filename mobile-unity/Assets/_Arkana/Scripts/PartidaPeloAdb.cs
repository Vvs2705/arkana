using UnityEngine;

namespace Arkana
{
    /// <summary>
    /// TESTE SEM DEDO: o MIUI do Poco F4 recusa toque injetado pelo adb ("INJECT_EVENTS", 12/09/2026). Entao o jogo
    /// aceita um extra de intent e comeca sozinho:
    ///   adb shell am start -n br.com.vstack.arkana/com.unity3d.player.UnityPlayerGameActivity --es arkana_auto partida
    ///   ... --es arkana_auto treino
    /// A queda acontece sozinha (o castelo empurra quem nao saltou no fim da rota); o jogador fica parado e os bots
    /// jogam — o bastante para medir FPS em todas as fases. So' Android, so' fora do editor; sem extra, nada muda.
    /// </summary>
    public static class PartidaPeloAdb
    {
        public const string EXTRA = "arkana_auto";

        /// <summary>"partida", "treino" ou null.</summary>
        public static string Pedido()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var atividade = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = atividade.Call<AndroidJavaObject>("getIntent"))
                {
                    string v = intent.Call<string>("getStringExtra", EXTRA);
                    return string.IsNullOrEmpty(v) ? null : v;
                }
            }
            catch (System.Exception) { return null; }   // sem atividade (teste, outra plataforma): sem pedido
#else
            return null;
#endif
        }
    }
}
