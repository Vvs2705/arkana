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
        /// <summary>`--ei arkana_fps 120` (ou -1 = sem teto): mede a folga real; sem o extra vale a Config do jogador.</summary>
        public const string EXTRA_FPS = "arkana_fps";
        /// <summary>Progresso da rota do castelo em que o jogador automatico salta (os bots sorteiam 0,18-0,86).</summary>
        public const float SALTO_EM = 0.45f;

        /// <summary>"partida", "treino" ou null.</summary>
        public static string Pedido() => Extra(i => i.Call<string>("getStringExtra", EXTRA));

        /// <summary>0 = sem pedido; -1 = sem teto; N = teto de N quadros.</summary>
        public static int FpsPedido() => Extra(i => i.Call<int>("getIntExtra", EXTRA_FPS, 0));

        static T Extra<T>(System.Func<AndroidJavaObject, T> ler)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var atividade = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = atividade.Call<AndroidJavaObject>("getIntent"))
                {
                    T v = ler(intent);
                    return v is string str && string.IsNullOrEmpty(str) ? default(T) : v;
                }
            }
            catch (System.Exception) { return default(T); }   // sem atividade (teste, outra plataforma): sem pedido
#else
            return default(T);
#endif
        }
    }
}
