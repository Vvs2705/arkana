using System;
using Unity.Profiling;
using UnityEngine;

namespace Arkana
{
    /// <summary>
    /// A MEDICAO QUE FALTAVA DESDE 25/08: FPS no aparelho, escrito no logcat a cada JANELA segundos
    /// ("ARKANA FPS media=58,7 min=41,2 quadros=294 dt_max=31,0ms"). Le-se com `adb logcat -s Unity`.
    /// Media = quadros / tempo; min = o pior segundo da janela; dt_max = o pior quadro (engasgo).
    /// 04/10 (BLOCO H): e ONDE o quadro gasta — "ARKANA CUSTO cpu_main=.. cpu_render=.. gpu=.. gc=.. mem=.. batches=..":
    /// CPU principal/render e GPU pelo FrameTimingManager (o Build liga PlayerSettings.enableFrameTimingStats), coletas de
    /// GC na janela, memoria e lotes de desenho pelo ProfilerRecorder (contador indisponivel no build = "n/d").
    /// Fora do editor e dos testes: a linha e' um Debug.Log e o BootTests reprova log inesperado.
    /// </summary>
    public sealed class MedidorDeFps : MonoBehaviour
    {
        public const float JANELA = 5f;

        float _t, _tSeg, _dtMax, _piorSeg = float.MaxValue;
        int _quadros, _quadrosSeg;

        readonly FrameTiming[] _ft = new FrameTiming[1];
        double _cpu, _render, _gpu;
        int _nft, _gc0;
        ProfilerRecorder _mem, _batches, _setpass, _tris;

        void OnEnable()
        {
            _gc0 = GC.CollectionCount(0);
            _mem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setpass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        void OnDisable() { _mem.Dispose(); _batches.Dispose(); _setpass.Dispose(); _tris.Dispose(); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt; _tSeg += dt; _quadros++; _quadrosSeg++;
            if (dt > _dtMax) _dtMax = dt;
            if (_tSeg >= 1f)
            {
                float fpsSeg = _quadrosSeg / _tSeg;
                if (fpsSeg < _piorSeg) _piorSeg = fpsSeg;
                _tSeg = 0f; _quadrosSeg = 0;
            }
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _ft) > 0)
            {
                _cpu += _ft[0].cpuMainThreadFrameTime; _render += _ft[0].cpuRenderThreadFrameTime; _gpu += _ft[0].gpuFrameTime; _nft++;
            }
            if (_t < JANELA) return;
            Debug.Log(Linha(_quadros / _t, _piorSeg == float.MaxValue ? _quadros / _t : _piorSeg, _quadros, _dtMax));
            int gc = GC.CollectionCount(0);
            Debug.Log(Custo(_nft > 0 ? _cpu / _nft : -1, _nft > 0 ? _render / _nft : -1, _nft > 0 ? _gpu / _nft : -1, gc - _gc0,
                Ultimo(_mem), Ultimo(_batches), Ultimo(_setpass), Ultimo(_tris)));
            _t = 0f; _quadros = 0; _dtMax = 0f; _piorSeg = float.MaxValue;
            _cpu = _render = _gpu = 0; _nft = 0; _gc0 = gc;
        }

        static long Ultimo(ProfilerRecorder r) => r.Valid ? r.LastValue : -1;

        public static string Linha(float media, float min, int quadros, float dtMax) =>
            string.Format("ARKANA FPS media={0:F1} min={1:F1} quadros={2} dt_max={3:F1}ms", media, min, quadros, dtMax * 1000f);

        /// <summary>Uma linha por janela; valor negativo = o aparelho/build nao entrega esse contador ("n/d").</summary>
        public static string Custo(double cpuMs, double renderMs, double gpuMs, int coletasGc, long memBytes, long batches, long setpass, long tris) =>
            "ARKANA CUSTO cpu_main=" + Ms(cpuMs) + " cpu_render=" + Ms(renderMs) + " gpu=" + Ms(gpuMs) + " gc=" + coletasGc
            + " mem=" + (memBytes < 0 ? "n/d" : (memBytes / (1024 * 1024)) + "MB") + " batches=" + Num(batches) + " setpass=" + Num(setpass)
            + " tris=" + (tris < 0 ? "n/d" : (tris / 1000) + "K");

        static string Ms(double v) => v < 0 || double.IsNaN(v) ? "n/d" : v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "ms";
        static string Num(long v) => v < 0 ? "n/d" : v.ToString();
    }
}
