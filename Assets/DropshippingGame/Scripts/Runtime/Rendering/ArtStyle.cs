using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Zentrale Art Direction im Code (siehe docs/ART_DIRECTION.md): Markenfarben, Stadtpaletten je Viertel,
    /// Lichtfarben und die Lichtstimmungen über den Tag (Sonne, Umgebungslicht, Nebel, Himmel, Farbkorrektur).
    /// Alles hier ist reine Konfiguration – Atmosphere, PostFX, Mats und CityBuilder lesen daraus.
    /// </summary>
    public static class ArtStyle
    {
        // ---- Marke --------------------------------------------------------------------------------
        /// <summary>Hustle-Gold: Hauptakzent (UI --accent, Plakatwand im Menü, Hervorhebung).</summary>
        public static readonly Color BrandGold = new Color(1f, 0.741f, 0.259f);
        /// <summary>Comic-Tinte: Konturen/Schrift auf hellem Grund (nie reines Schwarz).</summary>
        public static readonly Color Ink = new Color(0.086f, 0.086f, 0.094f);
        /// <summary>Papier: helle Flächen (nie reines Weiß).</summary>
        public static readonly Color Paper = new Color(0.98f, 0.965f, 0.93f);
        /// <summary>Signalrot: Badges, Sale, Warnungen.</summary>
        public static readonly Color SignalRed = new Color(0.9f, 0.2f, 0.07f);
        /// <summary>HustleOS-Orange (Laptop/Handy „Hype“).</summary>
        public static readonly Color HypeOrange = new Color(1f, 0.48f, 0.27f);
        /// <summary>Mint: „gut“, Geld rein.</summary>
        public static readonly Color Mint = new Color(0.32f, 0.86f, 0.78f);

        // ---- Material-Regeln ------------------------------------------------------------------------
        /// <summary>Hellster erlaubter Albedo-Kanal (kein reines Weiß, sonst brennt es im ACES-Tonemapping aus).</summary>
        public const float AlbedoMax = 0.94f;
        /// <summary>Dunkelster erlaubter Albedo-Kanal (kein reines Schwarz, Formen bleiben lesbar).</summary>
        public const float AlbedoMin = 0.025f;
        public const float RoughnessMin = 0.08f, RoughnessMax = 0.97f;

        /// <summary>Albedo in den erlaubten Bereich bringen (Alpha bleibt).</summary>
        public static Color SafeAlbedo(Color c)
        {
            return new Color(Mathf.Clamp(c.r, AlbedoMin, AlbedoMax), Mathf.Clamp(c.g, AlbedoMin, AlbedoMax),
                Mathf.Clamp(c.b, AlbedoMin, AlbedoMax), c.a);
        }

        // ---- Stadtpaletten --------------------------------------------------------------------------
        /// <summary>Geschäftsstraße (Altbau, warm): Ocker, Terrakotta, Creme, Salbei, Lachs.</summary>
        public static readonly Color[] WallsMainStreet =
        {
            new Color(0.87f, 0.76f, 0.58f), new Color(0.8f, 0.54f, 0.42f), new Color(0.92f, 0.86f, 0.72f), new Color(0.7f, 0.74f, 0.6f),
            new Color(0.88f, 0.66f, 0.5f), new Color(0.86f, 0.62f, 0.56f), new Color(0.76f, 0.68f, 0.6f), new Color(0.93f, 0.8f, 0.6f),
        };

        /// <summary>Wohnviertel (Pastell, freundlich): Mint, Puderblau, Zitrone, Flieder, Pfirsich, Altweiß.</summary>
        public static readonly Color[] WallsResidential =
        {
            new Color(0.72f, 0.84f, 0.74f), new Color(0.66f, 0.76f, 0.86f), new Color(0.94f, 0.88f, 0.62f), new Color(0.78f, 0.72f, 0.86f),
            new Color(0.95f, 0.78f, 0.66f), new Color(0.9f, 0.88f, 0.82f), new Color(0.86f, 0.7f, 0.7f), new Color(0.8f, 0.82f, 0.7f),
        };

        /// <summary>Finanzviertel (kühl, Glas/Stein): Schiefer, Sandstein, Stahlblau.</summary>
        public static readonly Color[] WallsFinance =
        {
            new Color(0.6f, 0.63f, 0.68f), new Color(0.78f, 0.74f, 0.66f), new Color(0.5f, 0.56f, 0.64f), new Color(0.84f, 0.82f, 0.78f),
        };

        /// <summary>Ladenschilder: kräftig, aber nicht grell (Satire-Werbung, gut lesbar).</summary>
        public static readonly Color[] SignColors =
        {
            new Color(0.86f, 0.22f, 0.2f), new Color(0.16f, 0.38f, 0.72f), new Color(0.14f, 0.56f, 0.38f), new Color(0.96f, 0.74f, 0.16f),
            new Color(0.56f, 0.28f, 0.7f), new Color(0.12f, 0.12f, 0.14f), new Color(0.95f, 0.46f, 0.18f), new Color(0.12f, 0.6f, 0.68f),
        };

        public static readonly Color[] RoofColors =
        {
            new Color(0.62f, 0.27f, 0.2f), new Color(0.36f, 0.34f, 0.34f), new Color(0.48f, 0.3f, 0.22f), new Color(0.27f, 0.32f, 0.4f),
        };

        // ---- Lichtfarben ----------------------------------------------------------------------------
        /// <summary>Glühbirne/Practical, ca. 2700 K: Imbiss, Wohnungen, Schreibtischlampen.</summary>
        public static readonly Color LightWarm = new Color(1f, 0.8f, 0.58f);
        /// <summary>Arbeitslicht, ca. 3500 K: Garage, Lagerhalle (warm-neutral statt Büro-Blau).</summary>
        public static readonly Color LightWork = new Color(1f, 0.9f, 0.76f);
        /// <summary>Ladenlicht, ca. 4000 K: Läden, Bank, Post.</summary>
        public static readonly Color LightShop = new Color(1f, 0.94f, 0.85f);
        /// <summary>Straßenlaterne (Natriumdampf-Anmutung, nicht ganz orange).</summary>
        public static readonly Color LightStreet = new Color(1f, 0.78f, 0.5f);

        // ---- Lichtstimmungen ----------------------------------------------------------------------
        /// <summary>Eine Lichtstimmung zu einer Uhrzeit. Zwischen den Stützstellen wird linear überblendet.</summary>
        public struct Mood
        {
            public float Hour;
            public Color Sun;
            public float SunIntensity;
            public Color AmbientSky, AmbientEquator, AmbientGround;
            public Color Fog;
            public float FogDensity;
            public Color SkyTint;
            public float SkyThickness, SkyExposure;
            /// <summary>Weißabgleich (−100..100, + = wärmer).</summary>
            public float Temperature;
            public float Saturation, Contrast;
            /// <summary>Farbton der Schatten bzw. Lichter (Shadows/Midtones/Highlights, w = Helligkeit).</summary>
            public Vector4 Shadows, Highlights;

            public static Mood Lerp(Mood a, Mood b, float t)
            {
                return new Mood
                {
                    Hour = Mathf.Lerp(a.Hour, b.Hour, t),
                    Sun = Color.Lerp(a.Sun, b.Sun, t),
                    SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
                    AmbientSky = Color.Lerp(a.AmbientSky, b.AmbientSky, t),
                    AmbientEquator = Color.Lerp(a.AmbientEquator, b.AmbientEquator, t),
                    AmbientGround = Color.Lerp(a.AmbientGround, b.AmbientGround, t),
                    Fog = Color.Lerp(a.Fog, b.Fog, t),
                    FogDensity = Mathf.Lerp(a.FogDensity, b.FogDensity, t),
                    SkyTint = Color.Lerp(a.SkyTint, b.SkyTint, t),
                    SkyThickness = Mathf.Lerp(a.SkyThickness, b.SkyThickness, t),
                    SkyExposure = Mathf.Lerp(a.SkyExposure, b.SkyExposure, t),
                    Temperature = Mathf.Lerp(a.Temperature, b.Temperature, t),
                    Saturation = Mathf.Lerp(a.Saturation, b.Saturation, t),
                    Contrast = Mathf.Lerp(a.Contrast, b.Contrast, t),
                    Shadows = Vector4.Lerp(a.Shadows, b.Shadows, t),
                    Highlights = Vector4.Lerp(a.Highlights, b.Highlights, t),
                };
            }
        }

        private static Mood M(float hour, Color sun, float sunI, Color sky, Color eq, Color ground, Color fog, float fogD,
            Color skyTint, float thick, float exposure, float temp, float sat, float contrast, Vector4 shadows, Vector4 highlights)
        {
            return new Mood
            {
                Hour = hour, Sun = sun, SunIntensity = sunI, AmbientSky = sky, AmbientEquator = eq, AmbientGround = ground,
                Fog = fog, FogDensity = fogD, SkyTint = skyTint, SkyThickness = thick, SkyExposure = exposure,
                Temperature = temp, Saturation = sat, Contrast = contrast, Shadows = shadows, Highlights = highlights,
            };
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b);
        private static Vector4 V(float r, float g, float b, float w = 0f) => new Vector4(r, g, b, w);

        /// <summary>
        /// Stützstellen über den Tag (Spieltag 08:00–20:00, Intro 17:30). Morgen = frisch/diesig, Mittag = klar
        /// und neutral, Goldene Stunde = warm mit kühlen Schatten, Blaue Stunde = Laternen an, Nacht = Blau + Natrium.
        /// </summary>
        public static readonly Mood[] Moods =
        {
            // Nacht
            M(0f, C(0.55f, 0.65f, 1f), 0.2f, C(0.07f, 0.09f, 0.17f), C(0.13f, 0.12f, 0.18f), C(0.06f, 0.06f, 0.07f),
                C(0.07f, 0.08f, 0.13f), 0.0042f, C(0.3f, 0.32f, 0.45f), 0.9f, 0.25f, -10f, 0f, 10f, V(0.93f, 0.98f, 1.1f), V(1f, 1f, 1.04f)),
            M(5f, C(0.55f, 0.65f, 1f), 0.2f, C(0.07f, 0.09f, 0.17f), C(0.13f, 0.12f, 0.18f), C(0.06f, 0.06f, 0.07f),
                C(0.07f, 0.08f, 0.13f), 0.0042f, C(0.3f, 0.32f, 0.45f), 0.9f, 0.25f, -10f, 0f, 10f, V(0.93f, 0.98f, 1.1f), V(1f, 1f, 1.04f)),
            // Morgendämmerung
            M(6.3f, C(1f, 0.6f, 0.42f), 0.45f, C(0.3f, 0.33f, 0.5f), C(0.78f, 0.56f, 0.5f), C(0.2f, 0.18f, 0.17f),
                C(0.7f, 0.58f, 0.58f), 0.0055f, C(0.6f, 0.45f, 0.45f), 1.5f, 0.8f, 4f, 4f, 8f, V(0.95f, 0.97f, 1.07f), V(1.05f, 0.99f, 0.94f)),
            // Morgen: frisch, leicht diesig
            M(8f, C(1f, 0.86f, 0.7f), 1.1f, C(0.42f, 0.54f, 0.74f), C(0.74f, 0.74f, 0.74f), C(0.27f, 0.25f, 0.22f),
                C(0.78f, 0.8f, 0.84f), 0.0045f, C(0.5f, 0.5f, 0.53f), 1.05f, 1.2f, 2f, 8f, 8f, V(0.96f, 0.99f, 1.05f), V(1.03f, 1f, 0.96f)),
            // Mittag: klar, neutral
            M(12f, C(1f, 0.96f, 0.9f), 1.45f, C(0.42f, 0.57f, 0.84f), C(0.7f, 0.74f, 0.78f), C(0.28f, 0.26f, 0.23f),
                C(0.72f, 0.79f, 0.88f), 0.0026f, C(0.5f, 0.5f, 0.52f), 0.9f, 1.25f, 0f, 10f, 10f, V(0.97f, 0.99f, 1.04f), V(1.02f, 1f, 0.97f)),
            // Nachmittag: leicht wärmer
            M(15.5f, C(1f, 0.9f, 0.76f), 1.35f, C(0.42f, 0.54f, 0.8f), C(0.76f, 0.72f, 0.68f), C(0.29f, 0.26f, 0.22f),
                C(0.78f, 0.78f, 0.8f), 0.003f, C(0.52f, 0.5f, 0.5f), 1f, 1.2f, 3f, 12f, 10f, V(0.96f, 0.98f, 1.05f), V(1.03f, 1f, 0.95f)),
            // Goldene Stunde: warmes Licht, kühle Schatten
            M(17.8f, C(1f, 0.68f, 0.38f), 1.25f, C(0.38f, 0.4f, 0.58f), C(0.95f, 0.64f, 0.44f), C(0.3f, 0.24f, 0.2f),
                C(0.96f, 0.7f, 0.52f), 0.0036f, C(0.64f, 0.46f, 0.4f), 1.45f, 1.05f, 8f, 14f, 12f, V(0.95f, 0.97f, 1.08f), V(1.06f, 0.99f, 0.9f)),
            // Sonnenuntergang
            M(19f, C(1f, 0.46f, 0.26f), 0.6f, C(0.3f, 0.28f, 0.48f), C(0.88f, 0.46f, 0.4f), C(0.2f, 0.15f, 0.15f),
                C(0.72f, 0.44f, 0.44f), 0.004f, C(0.68f, 0.4f, 0.42f), 1.7f, 0.8f, 6f, 12f, 12f, V(0.94f, 0.96f, 1.1f), V(1.06f, 0.98f, 0.92f)),
            // Blaue Stunde: Laternen gehen an
            M(20f, C(0.5f, 0.6f, 1f), 0.25f, C(0.14f, 0.17f, 0.34f), C(0.24f, 0.22f, 0.36f), C(0.08f, 0.08f, 0.1f),
                C(0.16f, 0.17f, 0.28f), 0.0045f, C(0.35f, 0.35f, 0.55f), 1.2f, 0.45f, -6f, 4f, 10f, V(0.93f, 0.97f, 1.1f), V(1.02f, 1f, 1f)),
            // Nacht
            M(21.5f, C(0.55f, 0.65f, 1f), 0.2f, C(0.07f, 0.09f, 0.17f), C(0.13f, 0.12f, 0.18f), C(0.06f, 0.06f, 0.07f),
                C(0.07f, 0.08f, 0.13f), 0.0042f, C(0.3f, 0.32f, 0.45f), 0.9f, 0.25f, -10f, 0f, 10f, V(0.93f, 0.98f, 1.1f), V(1f, 1f, 1.04f)),
        };

        /// <summary>Lichtstimmung zur Uhrzeit (0–24, wird in den Tag gefaltet).</summary>
        public static Mood MoodAt(float hours)
        {
            float h = Mathf.Repeat(hours, 24f);
            var keys = Moods;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (h < keys[i + 1].Hour)
                {
                    float span = Mathf.Max(0.0001f, keys[i + 1].Hour - keys[i].Hour);
                    return Mood.Lerp(keys[i], keys[i + 1], Mathf.SmoothStep(0f, 1f, (h - keys[i].Hour) / span));
                }
            }
            return keys[keys.Length - 1];
        }

        /// <summary>Wetter-Abwandlung einer Stimmung: overcast = Bewölkung (0..1), rain = Regen (0..1).</summary>
        public static Mood ApplyWeather(Mood m, float overcast, float rain)
        {
            if (overcast <= 0.001f && rain <= 0.001f) return m;
            float o = Mathf.Clamp01(overcast);
            float lumSun = m.Sun.grayscale;
            m.Sun = Color.Lerp(m.Sun, new Color(0.9f, 0.93f, 1f) * Mathf.Max(0.5f, lumSun), 0.55f * o);
            m.SunIntensity *= 1f - 0.78f * o;
            // Bedeckter Himmel: flacheres, helleres und graueres Umgebungslicht
            float skyLum = Mathf.Max(0.05f, (m.AmbientSky.grayscale + m.AmbientEquator.grayscale) * 0.5f);
            var grey = new Color(0.52f, 0.55f, 0.6f) * (skyLum / 0.55f);
            m.AmbientSky = Color.Lerp(m.AmbientSky, grey * 1.05f, 0.7f * o);
            m.AmbientEquator = Color.Lerp(m.AmbientEquator, grey * 1.1f, 0.7f * o);
            m.AmbientGround = Color.Lerp(m.AmbientGround, m.AmbientGround * 0.85f, o);
            float fogLum = Mathf.Max(0.04f, m.Fog.grayscale);
            m.Fog = Color.Lerp(m.Fog, new Color(0.62f, 0.65f, 0.7f) * (fogLum / 0.65f), 0.75f * o);
            m.FogDensity *= 1f + 0.9f * o + 1.6f * Mathf.Clamp01(rain);
            m.SkyTint = Color.Lerp(m.SkyTint, new Color(0.45f, 0.47f, 0.5f), o);
            m.SkyThickness += 0.4f * o;
            m.SkyExposure *= 1f - 0.3f * o;
            m.Temperature -= 4f * o;
            m.Saturation -= 14f * o;
            m.Contrast -= 4f * o;
            return m;
        }

        /// <summary>Sonnenhöhe in Grad zur Uhrzeit: flacher Bogen, damit die Goldene Stunde lange flach bleibt.</summary>
        public static float SunElevation(float hours)
        {
            float t = Mathf.Clamp01((hours - 6.2f) / 13.4f);
            return 2f + 62f * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 1.35f);
        }

        /// <summary>0 = Tag, 1 = Nacht (Fenster, Laternen, Schilder leuchten).</summary>
        public static float NightAmount(float hours)
        {
            float night = Mathf.Clamp01((hours - 19.1f) / 1.1f);
            if (hours < 6.6f) night = Mathf.Max(night, Mathf.Clamp01((6.6f - hours) / 1.1f));
            return night;
        }
    }
}
