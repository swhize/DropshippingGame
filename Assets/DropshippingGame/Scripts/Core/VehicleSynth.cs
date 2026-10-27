using System;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Prozedurale Mopedklänge (wie <see cref="AudioSynth"/>, reines C#): nahtloser Zweitakt-Motor-Loop
    /// (Tonhöhe regelt die Unity-Schicht über AudioSource.pitch), Hupe und dumpfer Rumms beim Anstoßen.
    /// Mono-Samples (-1..1) mit <see cref="AudioSynth.Rate"/> Hz.
    /// </summary>
    public static class VehicleSynth
    {
        private const float Tau = (float)(Math.PI * 2.0);
        /// <summary>Länge des Motor-Loops in Sekunden (ganzzahlige Frequenzen ⇒ nahtlos).</summary>
        public const float LoopSeconds = 1f;
        /// <summary>Zündfrequenz im Leerlauf (Hz).</summary>
        public const int IdleHz = 46;

        public static float[] Render(string name)
        {
            switch (name)
            {
                case "moped_engine": return EngineLoop();
                case "moped_horn": return Horn();
                case "moped_bump": return Bump();
            }
            return new float[1];
        }

        /// <summary>Zweitakt-„Ring-ding-ding“: Pulsfolge mit Obertönen + gefiltertes Rauschen, ringförmig gefiltert.</summary>
        public static float[] EngineLoop()
        {
            int rate = AudioSynth.Rate;
            int n = (int)(LoopSeconds * rate);
            var b = new float[n];
            var rng = new Rng(4711);
            // Obertöne der Zündfrequenz mit festen Phasen (deterministisch)
            const int harmonics = 14;
            var phase = new float[harmonics];
            for (int k = 0; k < harmonics; k++) phase[k] = rng.Range(0f, Tau);
            float inv = 1f / rate;
            for (int i = 0; i < n; i++)
            {
                float t = i * inv;
                float v = 0f;
                for (int k = 1; k <= harmonics; k++)
                {
                    float amp = 1f / (float)Math.Pow(k, 0.85);
                    // Resonanz um den 5./6. Oberton (Auspuff)
                    if (k == 5 || k == 6) amp *= 1.8f;
                    v += amp * (float)Math.Sin(Tau * IdleHz * k * t + phase[k - 1]);
                }
                // leichtes Stottern (ganzzahlige Modulationsfrequenz bleibt nahtlos)
                float am = 0.82f + 0.18f * (float)Math.Sin(Tau * 7f * t) * (float)Math.Sin(Tau * 3f * t + 1.3f);
                b[i] = v * am * 0.16f;
            }
            // Rauschen, ringförmig tiefpassgefiltert (zweimal durchlaufen, damit der Filterzustand am Ende = Anfang)
            var nz = new float[n];
            for (int i = 0; i < n; i++) nz[i] = rng.Range(-1f, 1f);
            float a = 1f - (float)Math.Exp(-Tau * 900f / rate);
            float y = 0f;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    y += a * (nz[i] - y);
                    if (pass == 1) nz[i] = y;
                }
            for (int i = 0; i < n; i++)
            {
                float t = i * inv;
                float puff = 0.5f + 0.5f * (float)Math.Sin(Tau * IdleHz * t);
                b[i] += nz[i] * (0.25f + 0.35f * puff) * 0.6f;
            }
            Normalize(b, 0.7f);
            return b;
        }

        /// <summary>Kurze, quäkige Mopedhupe (zwei Töne).</summary>
        public static float[] Horn()
        {
            int rate = AudioSynth.Rate;
            int n = (int)(0.42f * rate);
            var b = new float[n];
            float inv = 1f / rate;
            for (int i = 0; i < n; i++)
            {
                float t = i * inv;
                float s1 = Square(410f * t), s2 = Square(515f * t);
                float env = Math.Min(t / 0.012f, 1f) * Mathx.Clamp((0.42f - t) / 0.05f, 0f, 1f);
                b[i] = (s1 * 0.5f + s2 * 0.42f) * env;
            }
            // etwas weicher (Quäke statt Rechteck)
            float a = 1f - (float)Math.Exp(-Tau * 2600f / rate);
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                y += a * (b[i] - y);
                b[i] = y;
            }
            Normalize(b, 0.75f);
            return b;
        }

        /// <summary>Dumpfer Rumms (Anstoßen, Bordstein).</summary>
        public static float[] Bump()
        {
            int rate = AudioSynth.Rate;
            int n = (int)(0.3f * rate);
            var b = new float[n];
            var rng = new Rng(99);
            float inv = 1f / rate;
            float y = 0f;
            float a = 1f - (float)Math.Exp(-Tau * 500f / rate);
            for (int i = 0; i < n; i++)
            {
                float t = i * inv;
                y += a * (rng.Range(-1f, 1f) - y);
                float thump = (float)Math.Sin(Tau * (95f - 120f * t) * t) * (float)Math.Exp(-t * 18f);
                float rattle = y * (float)Math.Exp(-t * 26f) * 1.4f;
                b[i] = thump * 0.8f + rattle;
            }
            Normalize(b, 0.8f);
            return b;
        }

        private static float Square(float x)
        {
            float f = x - (float)Math.Floor(x);
            return f < 0.5f ? 1f : -1f;
        }

        private static void Normalize(float[] b, float peak)
        {
            float p = 0f;
            foreach (float v in b) p = Math.Max(p, Math.Abs(v));
            if (p < 1e-6f) return;
            float g = peak / p;
            for (int i = 0; i < b.Length; i++) b[i] = Mathx.Clamp(b[i] * g, -1f, 1f);
        }
    }
}
