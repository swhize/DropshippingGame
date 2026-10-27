using System;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Synthetisierte Feedback-Sounds („Juice“): Klebeband-Ratsch, Drucker-Surren, Schließfach, Münzregen,
    /// Kassen-Ding, Plopp, Konfetti-Knaller, Perk-Glitzern, Etikett-Klatsch, Funkeln.
    /// Fallback, falls keine Aufnahme in Resources/Audio/SFX liegt.
    /// </summary>
    public static partial class AudioSynth
    {
        /// <summary>Liefert null, wenn der Name hier nicht bekannt ist.</summary>
        private static float[] RenderFeelSfx(string name, Rng rng)
        {
            float inv = 1f / Rate;
            float[] b;
            switch (name)
            {
                case "tape_rip":
                {
                    const float dur = 0.5f;
                    b = NoiseBuf(dur, rng);
                    var low = (float[])b.Clone();
                    Lowpass(low, 900f);
                    float phase = 0f;
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float f = 55f + 150f * t / dur;
                        phase += f * inv;
                        float crackle = PosMod(phase, 1f) < 0.3f ? 1f : 0.18f;
                        float env = Math.Min(t / 0.015f, 1f) * Mathx.Clamp((0.47f - t) / 0.03f, 0f, 1f);
                        float v = (b[i] - low[i]) * crackle * env * 0.75f;
                        if (t >= 0.44f) v += Sin(Tau * 140f * t) * Exp(-(t - 0.44f) * 40f) * 0.3f;
                        b[i] = v;
                    }
                    break;
                }
                case "print_whirr":
                {
                    const float dur = 0.7f;
                    b = NoiseBuf(dur, rng);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float saw = PosMod(140f * t, 1f) * 2f - 1f;
                        float tick = PosMod(33f * t, 1f) < 0.08f ? 1f : 0f;
                        float env = Math.Min(t / 0.04f, 1f) * Mathx.Clamp((dur - t) / 0.08f, 0f, 1f);
                        b[i] = (saw * 0.18f + b[i] * tick * 0.4f + Sin(Tau * 280f * t) * 0.08f) * env;
                    }
                    Lowpass(b, 2200f);
                    break;
                }
                case "locker":
                {
                    b = NoiseBuf(0.4f, rng);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float v = b[i] * Exp(-t * 90f) * 0.6f;
                        v += Sin(Tau * 520f * t) * Exp(-t * 14f) * 0.25f;
                        v += Sin(Tau * 1180f * t) * Exp(-t * 20f) * 0.18f;
                        v += Sin(Tau * 1960f * t) * Exp(-t * 28f) * 0.12f;
                        v += Sin(Tau * 3100f * t) * Exp(-t * 40f) * 0.08f;
                        v += Sin(Tau * 85f * t) * Exp(-t * 25f) * 0.4f;
                        b[i] = v * 0.8f;
                    }
                    break;
                }
                case "coin_cascade":
                {
                    b = Buf(1.0f);
                    float[] starts = { 0f, 0.06f, 0.11f, 0.17f, 0.25f, 0.33f, 0.44f, 0.58f, 0.72f };
                    for (int k = 0; k < starts.Length; k++)
                    {
                        int st = (int)((starts[k] + rng.Range(0f, 0.02f)) * Rate);
                        float f = rng.Range(2600f, 4300f);
                        float amp = 0.26f * (1f - k * 0.07f);
                        for (int i = st; i < b.Length; i++)
                        {
                            float tt = (i - st) * inv;
                            if (tt > 0.35f) break;
                            float v = (Sin(Tau * f * tt) + 0.5f * Sin(Tau * f * 2.76f * tt) + 0.25f * Sin(Tau * f * 5.4f * tt)) * Exp(-tt * 22f) * amp * 0.5f;
                            if (tt < 0.003f) v += rng.Range(-1f, 1f) * amp * 0.5f;
                            b[i] += v;
                        }
                    }
                    break;
                }
                case "ding":
                {
                    b = Buf(1.2f);
                    float[] ratio = { 1f, 2f, 2.76f, 4.07f, 5.4f };
                    float[] amp = { 1f, 0.45f, 0.35f, 0.15f, 0.1f };
                    float[] dec = { 3.2f, 4.5f, 6f, 9f, 12f };
                    const float f0 = 1568f;
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float v = 0f;
                        for (int k = 0; k < ratio.Length; k++) v += Sin(Tau * f0 * ratio[k] * t) * amp[k] * Exp(-t * dec[k]);
                        v *= 0.2f * Math.Min(t / 0.002f, 1f);
                        if (t < 0.01f) v += rng.Range(-1f, 1f) * 0.3f * (1f - t / 0.01f);
                        b[i] = v;
                    }
                    break;
                }
                case "pop":
                {
                    b = Buf(0.14f);
                    float phase = 0f;
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        float f = 900f * Exp(-t * 28f) + 180f;
                        phase += Tau * f * inv;
                        b[i] = Sin(phase) * Exp(-t * 30f) * 0.6f + rng.Range(-1f, 1f) * Exp(-t * 120f) * 0.2f;
                    }
                    break;
                }
                case "popper":
                {
                    b = NoiseBuf(0.7f, rng);
                    int block = (int)(0.004f * Rate);
                    float gate = 1f;
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        if (block > 0 && i % block == 0) gate = rng.Value() < 0.35f ? 1f : 0f;
                        float v = b[i] * Exp(-t * 55f) * 0.8f;
                        v += b[i] * gate * Exp(-t * 4.5f) * 0.22f;
                        v += Sin(Tau * 70f * t) * Exp(-t * 30f) * 0.35f;
                        b[i] = v;
                    }
                    Lowpass(b, 6000f);
                    break;
                }
                case "perk":
                {
                    b = Buf(0.9f);
                    float[] notes = { 84f, 88f, 91f, 96f };
                    for (int n = 0; n < notes.Length; n++)
                    {
                        int st = (int)(n * 0.06f * Rate);
                        float f = Midi(notes[n]);
                        for (int i = st; i < b.Length; i++)
                        {
                            float tt = (i - st) * inv;
                            float vib = 1f + 0.004f * Sin(Tau * 6f * tt);
                            b[i] += (Sin(Tau * f * vib * tt) + 0.25f * Sin(Tau * f * 3f * tt)) * Exp(-tt * 5f) * 0.14f * Math.Min(tt / 0.003f, 1f);
                        }
                    }
                    break;
                }
                case "stick":
                {
                    b = NoiseBuf(0.16f, rng);
                    Lowpass(b, 2500f);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float t = i * inv;
                        b[i] = b[i] * Exp(-t * 45f) * 0.9f + Sin(Tau * 120f * t) * Exp(-t * 35f) * 0.35f;
                    }
                    break;
                }
                case "sparkle":
                {
                    b = Buf(0.6f);
                    for (int k = 0; k < 7; k++)
                    {
                        int st = (int)(rng.Range(0f, 0.4f) * Rate);
                        float f = rng.Range(3500f, 6500f);
                        for (int i = st; i < b.Length; i++)
                        {
                            float tt = (i - st) * inv;
                            b[i] += Sin(Tau * f * tt) * Exp(-tt * 16f) * 0.12f * Math.Min(tt / 0.002f, 1f);
                        }
                    }
                    break;
                }
                default:
                    return null;
            }
            return b;
        }
    }
}
