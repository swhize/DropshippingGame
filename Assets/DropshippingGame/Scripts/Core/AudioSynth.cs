using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Prozedurale Klangerzeugung: ALLE Soundeffekte und Musikstücke werden per Code synthetisiert
    /// (keine Audiodateien im Projekt). Liefert Mono-Samples (-1..1) mit <see cref="Rate"/> Hz.
    /// Reines C#, damit es auch ohne Unity getestet werden kann.
    /// </summary>
    public static class AudioSynth
    {
        public const int Rate = 22050;
        private const int TableSize = 2048;
        private const float Tau = (float)(Math.PI * 2.0);

        public static readonly string[] SfxNames =
        {
            "click", "hover", "pickup", "drop", "place", "cash", "notify", "order", "error",
            "tape", "printer", "levelup", "step", "fold", "truck", "door", "whoosh", "plate", "bad",
        };

        public static readonly string[] MusicNames = { "menu", "work", "diner" };

        /// <summary>Rendert einen Klang. Musik wird auf einen Spitzenpegel von 0,82 normalisiert.</summary>
        public static float[] Render(string name, bool isMusic)
        {
            if (!isMusic) return RenderSfx(name);
            var buf = RenderMusic(name);
            float peak = Peak(buf);
            float gain = 0.82f / Math.Max(peak, 0.0001f);
            for (int i = 0; i < buf.Length; i++) buf[i] = Mathx.Clamp(buf[i] * gain, -1f, 1f);
            return buf;
        }

        // ---- Hilfen --------------------------------------------------------------------------------
        private static float[] Buf(float seconds) => new float[Math.Max(1, (int)(seconds * Rate))];

        private static float Peak(float[] b)
        {
            float p = 0f;
            foreach (float v in b) p = Math.Max(p, Math.Abs(v));
            return p;
        }

        private static float Midi(float m) => 440f * (float)Math.Pow(2.0, (m - 69.0) / 12.0);
        private static float Sin(float x) => (float)Math.Sin(x);
        private static float Exp(float x) => (float)Math.Exp(x);
        private static float Sign(float x) => x > 0f ? 1f : (x < 0f ? -1f : 0f);

        private static float PosMod(float a, float b)
        {
            float r = a % b;
            return r < 0 ? r + b : r;
        }

        private static float[] Table(float[] harmonics)
        {
            var t = new float[TableSize];
            float norm = 0f;
            foreach (float h in harmonics) norm += h;
            for (int i = 0; i < TableSize; i++)
            {
                float ph = Tau * i / TableSize;
                float v = 0f;
                for (int k = 0; k < harmonics.Length; k++) v += harmonics[k] * Sin(ph * (k + 1));
                t[i] = v / norm;
            }
            return t;
        }

        private static float[] Tone(float[] table, float midi, float seconds, float attack, float decay, float wobble)
        {
            int n = Math.Max(1, (int)(seconds * Rate));
            var b = new float[n];
            float inc = Midi(midi) * TableSize / Rate;
            float phase = 0f;
            float invRate = 1f / Rate;
            for (int i = 0; i < n; i++)
            {
                float t = i * invRate;
                float env = Math.Min(t / attack, 1f) * Exp(-t * decay);
                phase += inc * (1f + wobble * Sin(2.2f * t));
                while (phase >= TableSize) phase -= TableSize;
                b[i] = table[(int)phase] * env;
            }
            int fade = Math.Min(n, (int)(0.04f * Rate));
            for (int i = 0; i < fade; i++) b[n - 1 - i] *= (float)i / fade;
            return b;
        }

        private static void Mix(float[] outBuf, float[] src, int start, float gain)
        {
            int total = outBuf.Length;
            for (int i = 0; i < src.Length; i++)
            {
                int idx = (start + i) % total;
                outBuf[idx] += src[i] * gain;
            }
        }

        private static float[] NoiseBuf(float seconds, Rng rng)
        {
            var b = Buf(seconds);
            for (int i = 0; i < b.Length; i++) b[i] = rng.Range(-1f, 1f);
            return b;
        }

        private static void Lowpass(float[] b, float cutoff)
        {
            float a = 1f - Exp(-Tau * cutoff / Rate);
            float y = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                y += a * (b[i] - y);
                b[i] = y;
            }
        }

        private static int Seed(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }

        // ---- Soundeffekte --------------------------------------------------------------------------
        private static float[] RenderSfx(string name)
        {
            var rng = new Rng(Seed(name));
            float inv = 1f / Rate;
            float[] b;
            switch (name)
            {
                case "click":
                    b = Buf(0.05f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        b[i] = Sin(Tau * 1900f * t) * Exp(-t * 90f) * 0.45f;
                    }
                    break;
                case "hover":
                    b = Buf(0.03f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        b[i] = Sin(Tau * 2700f * t) * Exp(-t * 130f) * 0.15f;
                    }
                    break;
                case "pickup":
                case "place":
                case "drop":
                case "step":
                {
                    var dur = new Dictionary<string, float> { { "pickup", 0.2f }, { "place", 0.16f }, { "drop", 0.35f }, { "step", 0.1f } }[name];
                    var f0 = new Dictionary<string, float> { { "pickup", 180f }, { "place", 150f }, { "drop", 110f }, { "step", 90f } }[name];
                    var dec = new Dictionary<string, float> { { "pickup", 22f }, { "place", 28f }, { "drop", 12f }, { "step", 45f } }[name];
                    var noiseAmt = new Dictionary<string, float> { { "pickup", 0.25f }, { "place", 0.15f }, { "drop", 0.4f }, { "step", 0.55f } }[name];
                    b = Buf(dur);
                    var nz = NoiseBuf(dur, rng);
                    Lowpass(nz, name != "step" ? 1400f : 900f);
                    float phase = 0f;
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float f = f0 * Exp(-t * 5f) + 40f;
                        phase += Tau * f * inv;
                        b[i] = Sin(phase) * Exp(-t * dec) * 0.7f + nz[i] * Exp(-t * dec * 2f) * noiseAmt;
                    }
                    break;
                }
                case "cash":
                    b = Buf(0.8f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float v = 0f;
                        if (t < 0.03f) v += rng.Range(-1f, 1f) * 0.3f * (1f - t / 0.03f);
                        v += Sin(Tau * 90f * t) * Exp(-t * 30f) * 0.3f;
                        v += (Sin(Tau * 1568f * t) + 0.35f * Sin(Tau * 1568f * 2.76f * t)) * Exp(-t * 5f) * 0.26f;
                        if (t > 0.08f)
                        {
                            float t2 = t - 0.08f;
                            v += (Sin(Tau * 2093f * t2) + 0.35f * Sin(Tau * 2093f * 2.76f * t2)) * Exp(-t2 * 4.5f) * 0.24f;
                        }
                        b[i] = v;
                    }
                    break;
                case "notify":
                case "order":
                {
                    float f1 = name == "notify" ? 880f : 1318.5f;
                    float f2 = name == "notify" ? 1318.5f : 1760f;
                    float gap = name == "notify" ? 0.11f : 0.07f;
                    b = Buf(0.55f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float v = (Sin(Tau * f1 * t) + 0.3f * Sin(Tau * f1 * 2f * t)) * Exp(-t * 9f);
                        if (t > gap)
                        {
                            float t2 = t - gap;
                            v += (Sin(Tau * f2 * t2) + 0.3f * Sin(Tau * f2 * 2f * t2)) * Exp(-t2 * 7f);
                        }
                        b[i] = v * 0.22f * Math.Min(t / 0.004f, 1f);
                    }
                    break;
                }
                case "error":
                case "bad":
                    b = Buf(name == "bad" ? 0.45f : 0.28f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float f = 150f;
                        if (name == "bad") f = t < 0.18f ? 440f : 311f;
                        float sq = Sign(Sin(Tau * f * t)) + Sign(Sin(Tau * f * 1.01f * t));
                        b[i] = sq * Exp(-t * (name == "error" ? 9f : 5f)) * 0.09f;
                    }
                    Lowpass(b, 2500f);
                    break;
                case "tape":
                    b = NoiseBuf(0.5f, rng);
                    Lowpass(b, 4200f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float am = 0.5f + 0.5f * Sin(Tau * 65f * t);
                        b[i] *= am * am * Math.Min(t / 0.03f, 1f) * Mathx.Clamp((0.5f - t) / 0.08f, 0f, 1f) * (0.3f + t) * 0.8f;
                    }
                    break;
                case "printer":
                    b = NoiseBuf(0.65f, rng);
                    Lowpass(b, 600f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float v = b[i] * 0.35f + (PosMod(80f * t, 1f) * 2f - 1f) * 0.05f;
                        v *= Math.Min(t / 0.05f, 1f) * Mathx.Clamp((0.65f - t) / 0.1f, 0f, 1f);
                        foreach (float start in new[] { 0.02f, 0.14f, 0.52f })
                            if (t >= start && t < start + 0.045f) v += Sin(Tau * 2400f * t) * 0.22f;
                        b[i] = v;
                    }
                    break;
                case "levelup":
                {
                    b = Buf(1.1f);
                    float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 2093f };
                    float[] starts = { 0f, 0.09f, 0.18f, 0.27f, 0.45f };
                    for (int n = 0; n < notes.Length; n++)
                    {
                        int st = (int)(starts[n] * Rate);
                        for (int i = st; i < b.Length; i++)
                        {
                            float t = (i - st) * inv;
                            b[i] += (Sin(Tau * notes[n] * t) + 0.3f * Sin(Tau * notes[n] * 2f * t)) * Exp(-t * 4f) * 0.16f;
                        }
                    }
                    break;
                }
                case "fold":
                    b = NoiseBuf(0.32f, rng);
                    Lowpass(b, 1800f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float env = Exp(-t * 30f);
                        if (t > 0.13f) env += Exp(-(t - 0.13f) * 26f);
                        b[i] *= env * 0.5f;
                    }
                    break;
                case "truck":
                {
                    b = Buf(1.7f);
                    var nz = NoiseBuf(1.7f, rng);
                    Lowpass(nz, 300f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float env = Math.Min(t / 0.35f, 1f) * Mathx.Clamp((1.7f - t) / 0.6f, 0f, 1f);
                        float saw = PosMod(42f * t, 1f) * 2f - 1f;
                        float saw2 = PosMod(84.5f * t, 1f) * 2f - 1f;
                        b[i] = (saw * 0.35f + saw2 * 0.15f + nz[i] * 1.2f) * env * 0.4f;
                    }
                    Lowpass(b, 900f);
                    break;
                }
                case "door":
                    b = NoiseBuf(1.3f, rng);
                    Lowpass(b, 1200f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float rattle = 0.55f + 0.45f * Sign(Sin(Tau * 26f * t));
                        float env = Math.Min(t / 0.08f, 1f) * Mathx.Clamp((1.3f - t) / 0.3f, 0f, 1f);
                        b[i] = (b[i] * rattle + Sin(Tau * 58f * t) * 0.15f) * env * 0.55f;
                    }
                    break;
                case "whoosh":
                {
                    b = NoiseBuf(0.45f, rng);
                    float y = 0f;
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float a = 0.02f + 0.25f * Sin((float)Math.PI * t / 0.45f);
                        y += a * (b[i] - y);
                        b[i] = y * Sin((float)Math.PI * t / 0.45f) * 0.6f;
                    }
                    break;
                }
                case "plate":
                    b = Buf(0.3f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float v = Sin(Tau * 2650f * t) * 0.5f + Sin(Tau * 3980f * t) * 0.3f + Sin(Tau * 5210f * t) * 0.2f;
                        b[i] = v * Exp(-t * 16f) * 0.28f;
                    }
                    break;
                default:
                    b = Buf(0.05f);
                    break;
            }
            return b;
        }

        // ---- Musik ------------------------------------------------------------------------------------
        private sealed class TrackCfg
        {
            public float Bpm, Cutoff;
            public string Style;
            public int Seed, Repeats;
            public int[][] Chords;
            public int[] Scale;
        }

        private static TrackCfg Cfg(string track)
        {
            switch (track)
            {
                case "menu":
                    return new TrackCfg
                    {
                        Bpm = 68f, Style = "pad", Seed = 11, Repeats = 2, Cutoff = 5200f,
                        Chords = new[] { new[] { 48, 55, 59, 62, 64 }, new[] { 45, 52, 55, 60, 64 }, new[] { 41, 48, 52, 57, 60 }, new[] { 43, 50, 55, 59, 62 } },
                        Scale = new[] { 72, 74, 76, 79, 81, 84 },
                    };
                case "diner":
                    return new TrackCfg
                    {
                        Bpm = 112f, Style = "jazz", Seed = 23, Repeats = 2, Cutoff = 2600f,
                        Chords = new[] { new[] { 50, 53, 57, 60 }, new[] { 43, 47, 50, 53 }, new[] { 48, 52, 55, 59 }, new[] { 45, 49, 52, 55 } },
                        Scale = new[] { 74, 76, 77, 79, 81, 84 },
                    };
                default:
                    return new TrackCfg
                    {
                        Bpm = 84f, Style = "lofi", Seed = 7, Repeats = 2, Cutoff = 4600f,
                        Chords = new[] { new[] { 53, 57, 60, 64 }, new[] { 52, 55, 59, 62 }, new[] { 50, 53, 57, 60 }, new[] { 48, 52, 55, 59 } },
                        Scale = new[] { 72, 74, 76, 79, 81, 84 },
                    };
            }
        }

        private static float[] RenderMusic(string track)
        {
            var cfg = Cfg(track);
            var rng = new Rng(cfg.Seed);
            string style = cfg.Style;
            float beat = 60f / cfg.Bpm;
            float bar = beat * 4f;
            var chords = cfg.Chords;
            int bars = chords.Length * cfg.Repeats;
            var outBuf = Buf(bar * bars);
            var ep = Table(new[] { 1f, 0.42f, 0.14f, 0.06f, 0.03f });
            var pad = Table(new[] { 1f, 0.55f, 0.36f, 0.24f, 0.16f, 0.1f, 0.06f });
            var bass = Table(new[] { 1f, 0.28f, 0.06f });

            // Akkord-Anschläge nur einmal pro Akkord rendern, danach an alle Positionen mischen.
            var hitLong = new List<float[]>();
            var hitShort = new List<float[]>();
            foreach (var c in chords)
            {
                var longBuf = Buf(style == "pad" ? bar * 1.15f : beat * 1.8f);
                var shortBuf = Buf(beat * 1.3f);
                foreach (int m in c)
                {
                    if (style == "pad")
                    {
                        Mix(longBuf, Tone(pad, m, bar * 1.15f, 0.6f, 0.35f, 0.002f), 0, 0.12f);
                    }
                    else
                    {
                        Mix(longBuf, Tone(ep, m, beat * 1.8f, 0.008f, 1.6f, 0.003f), 0, 0.14f);
                        Mix(shortBuf, Tone(ep, m, beat * 1.3f, 0.008f, 2.4f, 0.003f), 0, 0.11f);
                    }
                }
                hitLong.Add(longBuf);
                hitShort.Add(shortBuf);
            }

            var kick = DrumKick();
            var snare = DrumSnare(rng);
            var hat = DrumHat(rng);
            var scale = cfg.Scale;

            for (int bi = 0; bi < bars; bi++)
            {
                int ci = bi % chords.Length;
                var chord = chords[ci];
                int barStart = (int)(bi * bar * Rate);
                float root = chord[0] - 12f;
                switch (style)
                {
                    case "pad":
                        Mix(outBuf, hitLong[ci], barStart, 1f);
                        Mix(outBuf, Tone(bass, root, bar * 0.95f, 0.2f, 0.6f, 0f), barStart, 0.28f);
                        Mix(outBuf, kick, barStart, 0.25f);
                        for (int s = 0; s < 8; s++)
                            if (s % 2 == 1) Mix(outBuf, hat, barStart + (int)((s * 0.5f + 0.08f) * beat * Rate), 0.05f);
                        break;
                    case "jazz":
                    {
                        Mix(outBuf, hitShort[ci], barStart + (int)(beat * 0.02f * Rate), 1f);
                        Mix(outBuf, hitShort[ci], barStart + (int)(beat * 2.5f * Rate), 0.8f);
                        float[] walk = { root, root + 4f, root + 7f, root + 10f };
                        for (int q = 0; q < 4; q++)
                            Mix(outBuf, Tone(bass, walk[q], beat * 0.95f, 0.01f, 3f, 0f), barStart + (int)(q * beat * Rate), 0.34f);
                        for (int s = 0; s < 8; s++)
                        {
                            float swing = s % 2 == 1 ? 0.16f : 0f;
                            Mix(outBuf, s % 4 == 2 ? snare : hat, barStart + (int)((s * 0.5f + swing) * beat * Rate), s % 4 == 2 ? 0.12f : 0.07f);
                        }
                        break;
                    }
                    default:
                        Mix(outBuf, hitLong[ci], barStart, 1f);
                        Mix(outBuf, hitShort[ci], barStart + (int)(beat * 2.5f * Rate), 0.75f);
                        Mix(outBuf, Tone(bass, root, beat * 1.6f, 0.01f, 1.2f, 0f), barStart, 0.36f);
                        Mix(outBuf, Tone(bass, root + 7f, beat * 0.9f, 0.01f, 2f, 0f), barStart + (int)(beat * 2.5f * Rate), 0.26f);
                        Mix(outBuf, kick, barStart, 0.62f);
                        Mix(outBuf, kick, barStart + (int)(beat * 2.5f * Rate), 0.45f);
                        if (bi % 2 == 1) Mix(outBuf, kick, barStart + (int)(beat * 1.75f * Rate), 0.3f);
                        Mix(outBuf, snare, barStart + (int)(beat * Rate), 0.34f);
                        Mix(outBuf, snare, barStart + (int)(beat * 3f * Rate), 0.34f);
                        for (int s = 0; s < 8; s++)
                        {
                            float swing = s % 2 == 1 ? 0.17f : 0f;
                            Mix(outBuf, hat, barStart + (int)((s * 0.5f + swing) * beat * Rate), rng.Range(0.06f, 0.11f));
                        }
                        break;
                }
                // Kleine Melodie aus der Pentatonik (seed-basiert, also bei jedem Start gleich)
                for (int s = 0; s < 8; s++)
                {
                    if (rng.Value() < (style != "pad" ? 0.3f : 0.22f))
                    {
                        float note = scale[rng.RangeInt(0, scale.Length - 1)];
                        float len = beat * rng.Range(0.4f, 0.9f);
                        Mix(outBuf, Tone(ep, note, len, 0.01f, 2.8f, 0.004f), barStart + (int)(s * 0.5f * beat * Rate), 0.09f);
                    }
                }
            }

            // Vinyl-Knistern + Rauschteppich + warmer Tiefpass über das ganze Stück
            var hiss = NoiseBuf(1f, rng);
            Lowpass(hiss, 3000f);
            int crackles = (int)(outBuf.Length * 0.0003f);
            for (int k = 0; k < crackles; k++)
            {
                int pos = rng.RangeInt(0, outBuf.Length - 2);
                outBuf[pos] += rng.Range(-0.2f, 0.2f);
                outBuf[pos + 1] -= rng.Range(0f, 0.1f);
            }
            float aa = 1f - Exp(-Tau * cfg.Cutoff / Rate);
            float hpA = 1f - Exp(-Tau * 280f / Rate);
            float yy = 0f, low = 0f;
            bool radio = style == "jazz";
            int hissN = hiss.Length;
            for (int i = 0; i < outBuf.Length; i++)
            {
                float x = outBuf[i] + hiss[i % hissN] * 0.012f;
                yy += aa * (x - yy);
                if (radio)
                {
                    low += hpA * (yy - low);
                    outBuf[i] = (yy - low) * 1.4f;
                }
                else outBuf[i] = yy;
            }
            return outBuf;
        }

        private static float[] DrumKick()
        {
            var b = Buf(0.35f);
            float phase = 0f;
            float inv = 1f / Rate;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i * inv;
                phase += Tau * (48f + 70f * Exp(-t * 28f)) * inv;
                b[i] = Sin(phase) * Exp(-t * 9f);
            }
            return b;
        }

        private static float[] DrumSnare(Rng rng)
        {
            var b = NoiseBuf(0.24f, rng);
            Lowpass(b, 5200f);
            float inv = 1f / Rate;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i * inv;
                b[i] = b[i] * Exp(-t * 17f) * 0.8f + Sin(Tau * 190f * t) * Exp(-t * 28f) * 0.45f;
            }
            return b;
        }

        private static float[] DrumHat(Rng rng)
        {
            var b = NoiseBuf(0.06f, rng);
            float inv = 1f / Rate;
            float y = 0f;
            float a = 1f - Exp(-Tau * 6000f / Rate);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i * inv;
                y += a * (b[i] - y);
                b[i] = (b[i] - y) * Exp(-t * 75f);
            }
            return b;
        }
    }
}
