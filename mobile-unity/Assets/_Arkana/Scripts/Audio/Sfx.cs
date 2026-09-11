using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Menu;

namespace Arkana.Audio
{
    /// <summary>
    /// SINTESE PURA: cada timbre e' um float[] em [-1,1] a 22050 Hz mono, gerado por codigo (zero arquivo de audio).
    /// Espelho da paleta de audio/Sfx.gd (48 timbres). Nome desconhecido = null (silencio, nunca timbre errado).
    /// Determinista: o mesmo nome gera o mesmo PCM em todo aparelho (seed por nome).
    /// </summary>
    public static class Sintese
    {
        public const int SR = 22050;

        public static readonly string[] Nomes =
        {
            "shot_fire", "shot_water", "shot_lightning", "shot_earth", "shot_wind",
            "hit", "kill", "dodge",
            "click_fire", "click_water", "click_lightning", "click_earth", "click_wind",
            "terrain_fire", "terrain_ice", "terrain_zap", "terrain_wall",
            "sting_start", "fanfare", "defeat", "ui_tick",
            "ambient_wind", "bird",
            "estalo_manopla", "manopla_equipar", "arma_equipar", "loot_perto",
            "telegrafia", "telegrafia_rugido", "kit_tatica", "kit_pronto", "sino", "estado_liga", "estado_desliga",
            "bau_anuncio", "bau_queda", "bau_pouso", "bau_canal", "bau_aberto",
            "escudo_clank", "escudo_quebra", "baque", "dot_terreno",
            "st_queimar", "st_burn", "st_wet", "st_frost", "st_stun",
        };

        static readonly HashSet<string> _loops = new HashSet<string> { "ambient_wind", "telegrafia_rugido", "bau_queda", "bau_canal" };
        public static bool EhLoop(string nome) => _loops.Contains(nome);

        public static string Click(Elemento e) => "click_" + Elementos.Id(e);
        public static string Shot(Elemento e) => "shot_" + Elementos.Id(e);

        /// <summary>PCM do timbre (ja' preso em [-1,1]) ou null se o nome nao existe.</summary>
        public static float[] Gerar(string nome)
        {
            float[] b = GerarCru(nome);
            if (b == null || b.Length == 0) return null;
            for (int i = 0; i < b.Length; i++)
            {
                float v = b[i];
                b[i] = float.IsNaN(v) || float.IsInfinity(v) ? 0f : Mathf.Clamp(v, -1f, 1f);   // sintese nunca derruba o jogo
            }
            return b;
        }

        static float[] GerarCru(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return null;
            if (nome == "st_burn") nome = "st_queimar";   // apelido: MESMO PCM (a seed vem do nome)
            var g = new Gerador(nome);
            switch (nome)
            {
                case "shot_fire": { var b = g.Buf(0.30f); g.Noise(b, 0f, 0.30f, 1200f, 250f, 0.9f, "decay"); g.Tone(b, 0f, 0.30f, 95f, 55f, 0.35f); return b; }
                case "shot_water": { var b = g.Buf(0.24f); g.Tone(b, 0f, 0.13f, 260f, 940f, 0.5f, 0.25f); g.Noise(b, 0.09f, 0.15f, 2600f, 900f, 0.3f, "decay"); return b; }
                case "shot_lightning": { var b = g.Buf(0.14f); g.Noise(b, 0f, 0.14f, 6500f, 2500f, 0.55f, "decay"); g.Tone(b, 0f, 0.12f, 1750f, 1400f, 0.3f, 0.6f); g.Tone(b, 0f, 0.08f, 2600f, 2300f, 0.18f); return b; }
                case "shot_earth": { var b = g.Buf(0.28f); g.Tone(b, 0f, 0.26f, 105f, 42f, 0.7f); g.Noise(b, 0f, 0.07f, 900f, 400f, 0.5f, "decay"); return b; }
                case "shot_wind": { var b = g.Buf(0.35f); g.Noise(b, 0f, 0.35f, 700f, 3600f, 0.55f, "hump"); return b; }
                case "hit": { var b = g.Buf(0.09f); g.Noise(b, 0f, 0.09f, 2200f, 800f, 0.6f, "decay"); g.Tone(b, 0f, 0.07f, 240f, 180f, 0.4f); return b; }
                case "kill": { var b = g.Buf(0.45f); g.Tone(b, 0f, 0.12f, 90f, 45f, 0.8f); g.Noise(b, 0f, 0.05f, 1500f, 600f, 0.4f, "decay"); g.Tone(b, 0.10f, 0.16f, 659f, 659f, 0.3f, 0.35f); g.Tone(b, 0.20f, 0.25f, 880f, 880f, 0.32f, 0.35f); return b; }
                case "dodge": { var b = g.Buf(0.25f); g.Noise(b, 0f, 0.25f, 400f, 1900f, 0.45f, "hump"); return b; }
                case "click_fire": return g.Click(262f);
                case "click_water": return g.Click(330f);
                case "click_lightning": return g.Click(523f);
                case "click_earth": return g.Click(196f);
                case "click_wind": return g.Click(440f);
                case "estado_liga": return g.Click(880f);
                case "estado_desliga": return g.Click(587f);
                case "terrain_fire": return g.Crepitar(0.60f, 0.0025f, 0.990f, 0.7f, true);
                case "terrain_ice": { var b = g.Buf(0.45f); g.Tone(b, 0f, 0.45f, 1568f, 1568f, 0.22f, 0.4f); g.Tone(b, 0f, 0.40f, 1573f, 1573f, 0.18f); g.Tone(b, 0.04f, 0.35f, 2093f, 2093f, 0.15f); return b; }
                case "terrain_zap":
                    {
                        var b = g.Buf(0.50f); double ph = 0;
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = (float)i / SR; ph += 2 * Math.PI * 110.0 / SR;
                            float am = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 9f * t);
                            float fade = Mathf.Pow(1f - (float)i / b.Length, 0.8f);
                            b[i] = Mathf.Sign((float)Math.Sin(ph)) * 0.22f * am * fade;
                        }
                        g.Tone(b, 0f, 0.45f, 880f, 870f, 0.1f); return b;
                    }
                case "terrain_wall": { var b = g.Buf(0.45f); g.Noise(b, 0f, 0.40f, 100f, 320f, 1.0f, "rise"); g.Tone(b, 0f, 0.42f, 50f, 115f, 0.4f); g.Noise(b, 0.38f, 0.07f, 700f, 300f, 0.5f, "decay"); return b; }
                case "sting_start": { var b = g.Buf(0.55f); g.Tone(b, 0f, 0.15f, 440f, 440f, 0.35f, 0.3f); g.Tone(b, 0.14f, 0.40f, 659f, 659f, 0.4f, 0.35f); return b; }
                case "fanfare": { var b = g.Buf(0.95f); g.Tone(b, 0f, 0.20f, 523f, 523f, 0.35f, 0.4f); g.Tone(b, 0.18f, 0.20f, 659f, 659f, 0.35f, 0.4f); g.Tone(b, 0.36f, 0.58f, 784f, 784f, 0.4f, 0.45f); g.Tone(b, 0.36f, 0.58f, 1568f, 1568f, 0.12f); return b; }
                case "defeat": { var b = g.Buf(0.85f); g.Tone(b, 0f, 0.35f, 220f, 220f, 0.4f, 0.2f); g.Tone(b, 0.32f, 0.52f, 175f, 170f, 0.42f, 0.2f); return b; }
                case "ui_tick": { var b = g.Buf(0.05f); g.Tone(b, 0f, 0.05f, 1200f, 1100f, 0.4f); return b; }
                case "ambient_wind":
                    {
                        int n = (int)(2.0f * SR), fade = (int)(0.25f * SR);
                        var raw = new float[n + fade]; float lp = 0f;
                        for (int i = 0; i < raw.Length; i++)
                        {
                            float t = (float)i / SR;
                            float fc = 260f + 140f * Mathf.Sin(2f * Mathf.PI * t / 2.0f);
                            float k = Mathf.Min(1f, 2f * Mathf.PI * fc / SR);
                            lp += k * (g.Rand() - lp);
                            raw[i] = lp * 0.9f;
                        }
                        return Gerador.Costura(raw, n, fade);
                    }
                case "bird": { var b = g.Buf(0.32f); g.Tone(b, 0f, 0.06f, 3400f, 2500f, 0.22f); g.Tone(b, 0.12f, 0.05f, 3000f, 2300f, 0.18f); g.Tone(b, 0.20f, 0.07f, 3600f, 2700f, 0.2f); return b; }
                case "estalo_manopla": { var b = g.Buf(0.30f); g.Noise(b, 0f, 0.008f, 9000f, 4000f, 1.0f, "decay"); g.Tone(b, 0f, 0.025f, 2100f, 850f, 0.55f); g.Tone(b, 0.006f, 0.24f, 2093f, 2093f, 0.20f, 0.30f); g.Tone(b, 0.006f, 0.20f, 3136f, 3136f, 0.12f); return b; }
                case "manopla_equipar":
                    {
                        var b = g.Buf(1.05f); g.Noise(b, 0f, 0.010f, 9000f, 4000f, 0.9f, "decay"); g.Tone(b, 0f, 0.09f, 420f, 260f, 0.5f, 0.5f);
                        float[] ts = { 0.10f, 0.22f, 0.34f, 0.46f }; float[] fs = { 523f, 659f, 784f, 1047f };
                        for (int i = 0; i < 4; i++) g.Tone(b, ts[i], 0.55f, fs[i], fs[i], 0.26f, 0.35f);
                        g.Noise(b, 0.10f, 0.85f, 5200f, 2600f, 0.10f, "hump"); return b;
                    }
                case "arma_equipar": { var b = g.Buf(0.38f); g.Noise(b, 0f, 0.05f, 2400f, 700f, 0.55f, "decay"); g.Tone(b, 0f, 0.10f, 300f, 170f, 0.6f); g.Tone(b, 0.05f, 0.28f, 660f, 655f, 0.20f, 0.3f); return b; }
                case "loot_perto": { var b = g.Buf(0.14f); g.Tone(b, 0f, 0.06f, 880f, 880f, 0.35f); g.Tone(b, 0.05f, 0.08f, 1174f, 1174f, 0.30f); return b; }
                case "telegrafia": { var b = g.Buf(1.15f); g.Tone(b, 0f, 1.10f, 165f, 96f, 0.60f, 0.85f); g.Tone(b, 0.02f, 0.95f, 440f, 415f, 0.30f, 0.5f); g.Tone(b, 0.02f, 0.95f, 622f, 587f, 0.30f, 0.5f); g.Noise(b, 0f, 1.10f, 400f, 4200f, 0.35f, "rise"); return b; }
                case "telegrafia_rugido":
                    {
                        int n = (int)(1.6f * SR), fade = (int)(0.25f * SR);
                        var raw = new float[n + fade]; float lp = 0f; double ph = 0;
                        for (int i = 0; i < raw.Length; i++)
                        {
                            float t = (float)i / SR;
                            float k = Mathf.Min(1f, 2f * Mathf.PI * 320f / SR);
                            lp += k * (g.Rand() - lp);
                            ph += 2 * Math.PI * 82.0 / SR;
                            float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                            raw[i] = (lp * 0.75f + (float)Math.Sin(ph) * 0.35f) * trem;
                        }
                        return Gerador.Costura(raw, n, fade);
                    }
                case "kit_tatica": { var b = g.Buf(0.36f); g.Noise(b, 0f, 0.22f, 600f, 3000f, 0.40f, "hump"); g.Tone(b, 0.02f, 0.30f, 392f, 587f, 0.28f, 0.35f); return b; }
                case "kit_pronto": { var b = g.Buf(0.22f); g.Tone(b, 0f, 0.08f, 784f, 784f, 0.30f); g.Tone(b, 0.07f, 0.14f, 1047f, 1047f, 0.28f, 0.25f); return b; }
                case "sino":
                    {
                        var b = g.Buf(1.00f);
                        float[] f = { 440f, 1056f, 1496f, 2024f, 2860f }; float[] a = { 0.30f, 0.18f, 0.12f, 0.08f, 0.05f };
                        for (int i = 0; i < 5; i++) g.Tone(b, 0f, 0.98f, f[i], f[i] * 0.995f, a[i]);
                        g.Noise(b, 0f, 0.02f, 7000f, 3000f, 0.35f, "decay"); return b;
                    }
                case "bau_anuncio":
                    {
                        var b = g.Buf(1.70f); g.Noise(b, 0f, 0.03f, 6000f, 1500f, 0.5f, "decay");
                        float[] f = { 65f, 98f, 147f, 233f, 311f }; float[] a = { 0.55f, 0.30f, 0.22f, 0.14f, 0.10f };
                        for (int i = 0; i < 5; i++) g.Tone(b, 0f, 1.65f, f[i], f[i] * 0.998f, a[i], 0.2f);
                        g.Tone(b, 0.25f, 1.30f, 392f, 392f, 0.14f, 0.3f); g.Tone(b, 0.25f, 1.30f, 588f, 588f, 0.11f); return b;
                    }
                case "bau_queda":
                    {
                        int n = (int)(1.2f * SR), fade = (int)(0.2f * SR);
                        var raw = new float[n + fade]; float lp = 0f; double ph = 0;
                        for (int i = 0; i < raw.Length; i++)
                        {
                            float t = (float)i / SR;
                            float k = Mathf.Min(1f, 2f * Mathf.PI * 1400f / SR);
                            lp += k * (g.Rand() - lp);
                            ph += 2 * Math.PI * (520.0 + 40.0 * Math.Sin(2 * Math.PI * 3.0 * t)) / SR;
                            raw[i] = lp * 0.35f + (float)Math.Sin(ph) * 0.40f;
                        }
                        return Gerador.Costura(raw, n, fade);
                    }
                case "bau_pouso": { var b = g.Buf(1.00f); g.Tone(b, 0f, 0.45f, 62f, 24f, 1.0f); g.Noise(b, 0f, 0.28f, 1800f, 300f, 0.65f, "decay"); g.Noise(b, 0.06f, 0.90f, 4800f, 2200f, 0.14f, "hump"); g.Tone(b, 0.10f, 0.70f, 784f, 784f, 0.14f, 0.3f); return b; }
                case "bau_canal":
                    {
                        int n = (int)(1.0f * SR), fade = (int)(0.15f * SR);
                        var raw = new float[n + fade]; double p1 = 0, p2 = 0;
                        for (int i = 0; i < raw.Length; i++)
                        {
                            float t = (float)i / SR;
                            p1 += 2 * Math.PI * 160.0 / SR; p2 += 2 * Math.PI * 240.0 / SR;
                            float trem = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 7f * t);
                            raw[i] = ((float)Math.Sin(p1) * 0.45f + (float)Math.Sin(p2) * 0.28f + (float)Math.Sin(p1 * 4.0) * 0.06f) * trem;
                        }
                        return Gerador.Costura(raw, n, fade);
                    }
                case "bau_aberto":
                    {
                        var b = g.Buf(1.30f); g.Noise(b, 0f, 0.35f, 900f, 5000f, 0.55f, "decay");
                        float[] f = { 523f, 659f, 784f, 1047f }; float[] a = { 0.26f, 0.24f, 0.24f, 0.20f };
                        for (int i = 0; i < 4; i++) g.Tone(b, 0f, 1.25f, f[i], f[i], a[i], 0.35f);
                        g.Noise(b, 0.05f, 1.10f, 6000f, 3000f, 0.10f, "hump"); return b;
                    }
                case "escudo_clank": { var b = g.Buf(0.16f); g.Noise(b, 0f, 0.03f, 5000f, 2000f, 0.45f, "decay"); g.Tone(b, 0f, 0.14f, 920f, 900f, 0.35f, 0.5f); g.Tone(b, 0f, 0.10f, 1380f, 1360f, 0.20f); return b; }
                case "escudo_quebra":
                    {
                        var b = g.Buf(0.60f); g.Noise(b, 0f, 0.30f, 9000f, 1800f, 0.85f, "decay"); g.Tone(b, 0f, 0.08f, 700f, 300f, 0.45f);
                        float[] f = { 2400f, 3100f, 1900f, 2750f };
                        for (int i = 0; i < 4; i++) g.Tone(b, 0.04f + i * 0.06f, 0.16f, f[i], f[i] * 0.7f, 0.16f);
                        return b;
                    }
                case "baque": { var b = g.Buf(0.40f); g.Tone(b, 0f, 0.28f, 88f, 33f, 0.85f); g.Noise(b, 0f, 0.16f, 600f, 180f, 0.45f, "decay"); return b; }
                case "dot_terreno": { var b = g.Buf(0.16f); g.Noise(b, 0f, 0.16f, 520f, 190f, 0.40f, "decay"); return b; }
                case "st_queimar":
                case "st_burn": return g.Crepitar(0.22f, 0.006f, 0.985f, 0.55f, false);
                case "st_wet": { var b = g.Buf(0.28f); g.Noise(b, 0f, 0.20f, 3000f, 600f, 0.45f, "hump"); g.Tone(b, 0f, 0.16f, 520f, 190f, 0.30f, 0.2f); return b; }
                case "st_frost": { var b = g.Buf(0.42f); g.Noise(b, 0f, 0.30f, 1200f, 3600f, 0.35f, "rise"); g.Tone(b, 0.10f, 0.32f, 1760f, 1866f, 0.20f, 0.3f); g.Tone(b, 0.16f, 0.26f, 2637f, 2700f, 0.12f); return b; }
                case "st_stun":
                    {
                        var b = g.Buf(0.48f); double ph = 0;
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = (float)i / SR; float u = (float)i / b.Length;
                            ph += 2 * Math.PI * Mathf.Lerp(400f, 250f, u) / SR;
                            float am = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 11f * t);
                            b[i] = (float)Math.Sin(ph) * 0.35f * am * Mathf.Pow(1f - u, 1.2f);
                        }
                        return b;
                    }
            }
            return null;
        }

        /// <summary>Ferramentas de sintese com RNG proprio (seed por nome: determinista).</summary>
        sealed class Gerador
        {
            readonly System.Random _rng;
            public Gerador(string nome) { int seed = 1337; foreach (char c in nome) seed = seed * 31 + c; _rng = new System.Random(seed); }
            public float Rand() => (float)(_rng.NextDouble() * 2.0 - 1.0);
            float Rand01() => (float)_rng.NextDouble();
            public float[] Buf(float dur) => new float[(int)(dur * SR)];

            /// <summary>Seno com varredura f0->f1 + 2o harmonico; ataque 3 ms, decay exp.</summary>
            public void Tone(float[] buf, float start, float dur, float f0, float f1, float amp, float h2 = 0f)
            {
                int s = (int)(start * SR), n = (int)(dur * SR); double ph = 0;
                for (int i = 0; i < n; i++)
                {
                    int idx = s + i; if (idx >= buf.Length) break;
                    float t = (float)i / n;
                    ph += 2 * Math.PI * Mathf.Lerp(f0, f1, t) / SR;
                    float env = Mathf.Min(t * n / (0.003f * SR), 1f) * Mathf.Pow(1f - t, 1.6f);
                    buf[idx] += ((float)Math.Sin(ph) + h2 * (float)Math.Sin(2 * ph)) * amp * env;
                }
            }

            /// <summary>Ruido por lowpass 1 polo com corte varrendo fc0->fc1; env: decay/hump/rise.</summary>
            public void Noise(float[] buf, float start, float dur, float fc0, float fc1, float amp, string env)
            {
                int s = (int)(start * SR), n = (int)(dur * SR); float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    int idx = s + i; if (idx >= buf.Length) break;
                    float t = (float)i / n;
                    float k = Mathf.Min(1f, 2f * Mathf.PI * Mathf.Lerp(fc0, fc1, t) / SR);
                    lp += k * (Rand() - lp);
                    float e = env == "hump" ? Mathf.Sin(Mathf.PI * t) : env == "rise" ? t * t : Mathf.Pow(1f - t, 2f);
                    buf[idx] += lp * amp * e;
                }
            }

            public float[] Click(float freq) { var b = Buf(0.09f); Tone(b, 0f, 0.09f, freq, freq, 0.5f, 0.3f); return b; }

            /// <summary>Crepitar: estouros esparsos de energia que decai (fogo no chao / brasa no corpo).</summary>
            public float[] Crepitar(float dur, float chance, float decai, float amp, bool cama)
            {
                var b = Buf(dur); float energy = 0f;
                for (int i = 0; i < b.Length; i++)
                {
                    if (Rand01() < chance) energy = 0.5f + Rand01() * 0.5f;
                    float fade = 1f - (float)i / b.Length;
                    b[i] = Rand() * energy * amp * fade;
                    energy *= decai;
                }
                if (cama) Noise(b, 0f, dur, 300f, 200f, 0.15f, "hump");
                return b;
            }

            /// <summary>Costura de loop: mistura a cauda no comeco para o loop nao estalar na emenda.</summary>
            public static float[] Costura(float[] raw, int n, int fade)
            {
                var b = new float[n];
                Array.Copy(raw, b, n);
                for (int i = 0; i < fade && i < n && n + i < raw.Length; i++)
                {
                    float a = (float)i / fade;
                    b[i] = b[i] * a + raw[n + i] * (1f - a);
                }
                return b;
            }
        }
    }

    /// <summary>
    /// MIXAGEM PURA com prioridade: som novo NUNCA rouba a voz de um som mais importante; repeticao dentro do throttle
    /// nao sai; pool cheio recusa o menos importante. Tempo em ms, injetado (testavel sem relogio).
    /// </summary>
    public sealed class Vozes
    {
        public const int P_AMBIENTE = 0, P_BAIXA = 1, P_NORMAL = 2, P_ALTA = 3, P_CRITICA = 4;
        readonly long[] _freeAt;
        readonly int[] _prio;
        readonly Dictionary<string, long> _throttle = new Dictionary<string, long>();

        public int Tamanho => _freeAt.Length;
        public Vozes(int n) { _freeAt = new long[Mathf.Max(n, 1)]; _prio = new int[_freeAt.Length]; }

        public int PrioDe(int voz) => _prio[voz];
        public bool Ocupada(int voz, long agora) => _freeAt[voz] > agora;

        /// <summary>Voz livre; senao a PIOR (menor prio, mais velha) — e so' se a prio nova for &gt;= a dela.</summary>
        public int Escolher(long agora, int prio)
        {
            int pior = -1;
            for (int i = 0; i < _freeAt.Length; i++)
            {
                if (_freeAt[i] <= agora) return i;
                if (pior < 0 || _prio[i] < _prio[pior] || (_prio[i] == _prio[pior] && _freeAt[i] < _freeAt[pior])) pior = i;
            }
            if (pior < 0) return -1;
            return _prio[pior] <= prio ? pior : -1;
        }

        /// <summary>Devolve o indice da voz ou -1 (throttle ou pool cheio de coisa mais importante).</summary>
        public int Tocar(string chave, long agora, int prio, int throttleMs, int duracaoMs)
        {
            if (throttleMs > 0)
            {
                long ultimo;
                if (_throttle.TryGetValue(chave, out ultimo) && agora - ultimo < throttleMs) return -1;
                _throttle[chave] = agora;
            }
            int i = Escolher(agora, prio);
            if (i < 0) return -1;
            _freeAt[i] = agora + Mathf.Max(duracaoMs, 1);
            _prio[i] = prio;
            return i;
        }

        public void Liberar() { for (int i = 0; i < _freeAt.Length; i++) { _freeAt[i] = 0; _prio[i] = 0; } _throttle.Clear(); }

        // ---------- distancia (o projeto nao usa audio 3D: atenua em dB pela distancia do ouvinte) ----------
        public const float OUVIDO_M = 8f, SURDO_M = 110f, QUEDA_DB = 22f;

        /// <summary>0 dB ate' OUVIDO_M, -QUEDA_DB em SURDO_M (log), null alem (nem ocupa voz).</summary>
        public static float? DbDistancia(float d)
        {
            if (d > SURDO_M) return null;
            if (d <= OUVIDO_M) return 0f;
            return -QUEDA_DB * Mathf.Log(d / OUVIDO_M) / Mathf.Log(SURDO_M / OUVIDO_M);
        }

        public static float DbParaGanho(float db) => Mathf.Pow(10f, db / 20f);
    }

    /// <summary>
    /// Casca: AudioClips gerados UMA vez (cache), pool de 10 vozes + UI + musica + evento + ambiente. OBSERVA o Bus
    /// (checagem defensiva em toda ponta): se a sintese quebrar, a partida segue em silencio — nunca o contrario.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        public const int POOL = 10;
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        /// <summary>Posicao do ouvinte para atenuacao (a cena injeta o jogador; padrao: a camera).</summary>
        public Func<Vector3> PosOuvinte;
        /// <summary>O player porta manopla? (estalo dos dedos no disparo — assinatura do GDD §16.2)</summary>
        public bool Manopla;
        /// <summary>Espiao: ultima chave que de fato tocou.</summary>
        public string UltimaChave { get; private set; } = "";

        AudioSource[] _pool;
        AudioSource _ui, _music, _evento, _wind, _birds;
        Vozes _vozes;
        float _volEfeitos = 0.85f, _volMusica = 0.7f, _volUi = 0.7f;
        float _birdTimer = -1f;
        float _eventoFim = -1f;
        float _pitchDe = 1f, _pitchAte = 1f, _pitchT = -1f, _pitchDur = 1f;
        System.Random _rng = new System.Random(7);

        public static Sfx Criar()
        {
            var go = new GameObject("Sfx", typeof(Sfx));
            return go.GetComponent<Sfx>();
        }

        /// <summary>AudioClip do timbre (cache por processo). Nome desconhecido/sintese vazia = null.</summary>
        public static AudioClip Clip(string nome)
        {
            AudioClip c;
            if (_clips.TryGetValue(nome, out c)) return c;
            float[] pcm = null;
            try { pcm = Sintese.Gerar(nome); } catch (Exception) { pcm = null; }
            if (pcm == null || pcm.Length == 0) { _clips[nome] = null; return null; }
            for (int i = 0; i < pcm.Length; i++) pcm[i] = Mathf.Clamp(pcm[i], -1f, 1f);
            c = AudioClip.Create(nome, pcm.Length, 1, Sintese.SR, false);
            c.SetData(pcm, 0);
            _clips[nome] = c;
            return c;
        }

        void Awake()
        {
            _pool = new AudioSource[POOL];
            for (int i = 0; i < POOL; i++) _pool[i] = Novo("Voz" + i);
            _ui = Novo("Ui"); _music = Novo("Musica"); _evento = Novo("Evento"); _wind = Novo("Vento"); _birds = Novo("Passaros");
            _wind.loop = true; _evento.loop = true;
            _vozes = new Vozes(POOL);
            foreach (var n in Sintese.Nomes) Clip(n);   // sintetiza a paleta inteira no boot: custo por frame zero
            AplicarConfig(ConfigLogica.Atual);
            ConfigLogica.Mudou += AplicarConfig;
            Assinar();
        }

        AudioSource Novo(string nome)
        {
            var go = new GameObject(nome, typeof(AudioSource));
            go.transform.SetParent(transform, false);
            var a = go.GetComponent<AudioSource>();
            a.playOnAwake = false; a.spatialBlend = 0f;
            return a;
        }

        void AplicarConfig(ConfigLogica c)
        {
            _volEfeitos = Mathf.Clamp01(c.Float(ConfigLogica.K_VOL_EFEITOS) / 100f);
            _volMusica = Mathf.Clamp01(c.Float(ConfigLogica.K_VOL_MUSICA) / 100f);
            _volUi = Mathf.Clamp01(c.Float(ConfigLogica.K_VOL_INTERFACE) / 100f);
            if (_wind != null) _wind.volume = Vozes.DbParaGanho(-4f - 18f) * _volEfeitos;
        }

        void Assinar()
        {
            Bus.DamageApplied += OnDano; Bus.EntityDied += OnMorreu; Bus.PlayerKilledBot += OnAbate; Bus.DodgePerformed += OnEsquiva;
            Bus.ElementChanged += OnElemento; Bus.MatchStarted += OnInicio; Bus.MatchOver += OnFim; Bus.TerrainHit += OnTerrenoHit;
            Bus.TerrainChanged += OnTerreno; Bus.GameStartRequested += OnPediuPartida; Bus.SpellCast += OnDisparo; Bus.ShieldBroken += OnEscudoQuebrou;
            Bus.LootPrompt += OnLoot; Bus.WeaponEquipped += OnArma; Bus.KitTelegraph += OnTelegrafia; Bus.KitCooldown += OnKitCooldown;
            Bus.KitState += OnKitState; Bus.BauAnunciado += OnBauAnunciado; Bus.BauPousou += OnBauPousou; Bus.BauCanalizando += OnBauCanal;
            Bus.BauAberto += OnBauAberto; Bus.StatusAplicado += OnStatus;
        }

        void OnDestroy()
        {
            Bus.DamageApplied -= OnDano; Bus.EntityDied -= OnMorreu; Bus.PlayerKilledBot -= OnAbate; Bus.DodgePerformed -= OnEsquiva;
            Bus.ElementChanged -= OnElemento; Bus.MatchStarted -= OnInicio; Bus.MatchOver -= OnFim; Bus.TerrainHit -= OnTerrenoHit;
            Bus.TerrainChanged -= OnTerreno; Bus.GameStartRequested -= OnPediuPartida; Bus.SpellCast -= OnDisparo; Bus.ShieldBroken -= OnEscudoQuebrou;
            Bus.LootPrompt -= OnLoot; Bus.WeaponEquipped -= OnArma; Bus.KitTelegraph -= OnTelegrafia; Bus.KitCooldown -= OnKitCooldown;
            Bus.KitState -= OnKitState; Bus.BauAnunciado -= OnBauAnunciado; Bus.BauPousou -= OnBauPousou; Bus.BauCanalizando -= OnBauCanal;
            Bus.BauAberto -= OnBauAberto; Bus.StatusAplicado -= OnStatus;
            ConfigLogica.Mudou -= AplicarConfig;
        }

        // ---------- reproducao ----------
        static long AgoraMs => (long)(Time.realtimeSinceStartup * 1000f);

        public bool Tocar(string chave, float db = 0f, int prio = Vozes.P_NORMAL, int throttleMs = 0)
        {
            var clip = Clip(chave);
            if (clip == null) return false;   // stream nulo: NAO toca, NAO crasha
            int i = _vozes.Tocar(chave, AgoraMs, prio, throttleMs, (int)(clip.length * 1000f));
            if (i < 0) return false;
            var p = _pool[i];
            p.Stop();
            p.clip = clip;
            p.volume = Vozes.DbParaGanho(db - 8f) * _volEfeitos;
            p.pitch = 1f;
            p.Play();
            UltimaChave = chave;
            return true;
        }

        public bool TocarEm(string chave, Vector3 pos, float db = 0f, int prio = Vozes.P_NORMAL, int throttleMs = 0)
        {
            float? at = Vozes.DbDistancia(Vector3.Distance(Ouvinte(), pos));
            if (!at.HasValue) return false;   // longe demais: nem ocupa voz
            return Tocar(chave, db + at.Value, prio, throttleMs);
        }

        Vector3 Ouvinte()
        {
            if (PosOuvinte != null) return PosOuvinte();
            var cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        void TocarUi(string chave, float db = -6f)
        {
            var c = Clip(chave); if (c == null) return;
            _ui.Stop(); _ui.clip = c; _ui.volume = Vozes.DbParaGanho(db - 10f) * _volUi; _ui.Play(); UltimaChave = chave;
        }

        void TocarMusica(string chave, float db = -2f)
        {
            var c = Clip(chave); if (c == null) return;
            _music.Stop(); _music.clip = c; _music.volume = Vozes.DbParaGanho(db - 6f) * _volMusica; _music.Play(); UltimaChave = chave;
        }

        void LoopEvento(string chave, float db, float pitch = 1f)
        {
            var c = Clip(chave); if (c == null) return;
            if (_evento.clip != c || !_evento.isPlaying) { _evento.clip = c; _evento.Play(); }
            _evento.volume = Vozes.DbParaGanho(db - 6f - 8f) * _volEfeitos;
            _evento.pitch = pitch;
            UltimaChave = chave;
        }

        void PararEvento() { _pitchT = -1f; _eventoFim = -1f; _evento.Stop(); _evento.pitch = 1f; }

        // ---------- handlers ----------
        void OnDisparo(Elemento e)
        {
            if (Manopla) Tocar("estalo_manopla", -1f, Vozes.P_CRITICA);
            Tocar(Sintese.Shot(e), -4f, Vozes.P_NORMAL, 40);
        }

        /// <summary>Acerto direto / em escudo / tique de DoT sao TRES leituras: fonte e onShield separam.</summary>
        void OnDano(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool emEscudo)
        {
            Vector3 pos = alvo != null ? alvo.Pos : Ouvinte();
            int minMs = (int)((float)Balance.Feedback.HitSoundMinS * 1000f);
            if (fonte == null) { TocarEm(el == Elemento.Fogo ? "st_queimar" : "dot_terreno", pos, -13f, Vozes.P_BAIXA, 320); return; }
            if (emEscudo) TocarEm("escudo_clank", pos, -7f, Vozes.P_NORMAL, minMs);
            else TocarEm("hit", pos, -6f, Vozes.P_NORMAL, minMs);
        }

        void OnMorreu(IEntidade e) { TocarEm("baque", e != null ? e.Pos : Ouvinte(), -4f, Vozes.P_ALTA); }
        void OnAbate(string nome) { Tocar("kill", -3f, Vozes.P_ALTA); }
        void OnEsquiva() { Tocar("dodge", -8f, Vozes.P_NORMAL); }
        void OnElemento(Elemento e) { if (Clip(Sintese.Click(e)) != null) TocarUi(Sintese.Click(e), -8f); else TocarUi("ui_tick", -10f); }
        void OnInicio() { Manopla = false; TocarMusica("sting_start", -4f); AmbienteLigar(); }
        void OnFim(bool vitoria) { AmbienteDesligar(); TocarMusica(vitoria ? "fanfare" : "defeat", -2f); }
        void OnPediuPartida() { TocarUi("ui_tick", -10f); }
        void OnEscudoQuebrou(IEntidade e) { TocarEm("escudo_quebra", e != null ? e.Pos : Ouvinte(), -2f, Vozes.P_ALTA); }
        void OnLoot(string nome, string raridade, bool perto) { if (perto) TocarUi("loot_perto", -12f); }

        void OnArma(IEntidade pawn, string armaId, string nome, string raridade, Elemento[] els)
        {
            bool manopla = raridade == "lendaria" || (els != null && els.Length >= 2);
            if (pawn != null && pawn.EhPlayer) Manopla = manopla;   // arma de BOT nao mexe na flag do jogador
            if (manopla) Tocar("manopla_equipar", -1f, Vozes.P_CRITICA);
            else Tocar("arma_equipar", raridade == "comum" ? -6f : -4f, Vozes.P_ALTA, 250);   // bots trocam de arma no chao: um encaixe por vez
        }

        /// <summary>A lei do §4.3: toda suprema avisa antes, alto — P_CRITICA, sem atenuacao por distancia.</summary>
        void OnTelegrafia(string slug, string tipo, float duracao, Vector3 pos)
        {
            Tocar("telegrafia", 0f, Vozes.P_CRITICA);
            float d = Mathf.Clamp(duracao, 0.5f, 6f);
            LoopEvento("telegrafia_rugido", -3f, Mathf.Clamp(1.6f / d, 0.35f, 2f));
            _eventoFim = Time.unscaledTime + d;
        }

        void OnKitCooldown(string tipo, float restante, float total)
        {
            if (restante <= 0f) TocarUi("kit_pronto", -12f);
            else if (tipo == "tatica") Tocar("kit_tatica", -5f, Vozes.P_ALTA);
        }

        void OnKitState(string nome, bool ligado)
        {
            if (nome == "sino_espectral") { if (ligado) Tocar("sino", -4f, Vozes.P_ALTA); return; }
            if (nome == "escudo_quebrado") return;   // ja' soa por ShieldBroken
            Tocar(ligado ? "estado_liga" : "estado_desliga", -14f, Vozes.P_BAIXA, 180);
        }

        void OnBauAnunciado(Vector3 pos, float segundos)
        {
            Tocar("bau_anuncio", 0f, Vozes.P_CRITICA);
            LoopEvento("bau_queda", -8f, 0.8f);
            _pitchDe = 0.8f; _pitchAte = 1.7f; _pitchT = 0f; _pitchDur = Mathf.Max(segundos, 0.1f);
        }
        void OnBauPousou(Vector3 pos) { PararEvento(); Tocar("bau_pouso", -1f, Vozes.P_CRITICA); }
        void OnBauCanal(IEntidade pawn, float prog)
        {
            if (prog <= 0f) { PararEvento(); return; }
            LoopEvento("bau_canal", -8f, 0.9f + 0.55f * Mathf.Clamp01(prog));   // o zumbido SOBE com a barra
        }
        void OnBauAberto(bool porPlayer, Elemento[] els) { PararEvento(); Tocar("bau_aberto", 0f, Vozes.P_CRITICA); if (porPlayer) Manopla = true; }

        void OnStatus(IEntidade alvo, string nome)
        {
            string k = "st_" + nome;
            if (Clip(k) == null) return;   // estado sem timbre: silencio, nunca timbre errado
            TocarEm(k, alvo != null ? alvo.Pos : Ouvinte(), -9f, Vozes.P_BAIXA, 200);
        }

        void OnTerrenoHit(Elemento e, Vector3 pos, bool forte) { TocarEm(Sintese.Shot(e), pos, forte ? -3f : -6f, Vozes.P_NORMAL, 40); }

        /// <summary>Kinds do Bus: burn|ice|electric|mud|wall|ash. Desconhecido (ou sem timbre) = silencio.</summary>
        public static string TimbreDoTerreno(string kind)
        {
            string k = (kind ?? "").ToLowerInvariant();
            if (k.Contains("fire") || k.Contains("fogo") || k.Contains("burn")) return "terrain_fire";
            if (k.Contains("ice") || k.Contains("gelo") || k.Contains("frost")) return "terrain_ice";
            if (k.Contains("zap") || k.Contains("elec") || k.Contains("shock") || k.Contains("raio")) return "terrain_zap";
            if (k.Contains("wall") || k.Contains("muro") || k.Contains("stone") || k.Contains("rock") || k.Contains("earth") || k.Contains("terra")) return "terrain_wall";
            return null;
        }

        void OnTerreno(string kind, Vector3 pos)
        {
            string t = TimbreDoTerreno(kind);
            if (t == null) return;
            TocarEm(t, pos, t == "terrain_zap" ? -6f : -4f, Vozes.P_BAIXA, 150);
        }

        // ---------- ambiente ----------
        void AmbienteLigar()
        {
            var w = Clip("ambient_wind");
            if (w != null) { _wind.clip = w; _wind.Play(); }
            _birdTimer = 4f + (float)_rng.NextDouble() * 6f;
        }

        void AmbienteDesligar() { _wind.Stop(); _birds.Stop(); _birdTimer = -1f; PararEvento(); }

        void Update()
        {
            if (_birdTimer > 0f)
            {
                _birdTimer -= Time.deltaTime;
                if (_birdTimer <= 0f)
                {
                    var c = Clip("bird");
                    if (c != null) { _birds.clip = c; _birds.volume = Vozes.DbParaGanho(-6f - 18f) * _volEfeitos; _birds.Play(); }
                    _birdTimer = 6f + (float)_rng.NextDouble() * 10f;   // esparso, entardecer
                }
            }
            if (_pitchT >= 0f)
            {
                _pitchT += Time.deltaTime;
                _evento.pitch = Mathf.Lerp(_pitchDe, _pitchAte, Mathf.Clamp01(_pitchT / _pitchDur));
                if (_pitchT >= _pitchDur) _pitchT = -1f;
            }
            if (_eventoFim > 0f && Time.unscaledTime >= _eventoFim) PararEvento();
        }
    }
}
