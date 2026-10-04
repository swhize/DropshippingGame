using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Beschreibung einer prozeduralen Textur (Art + Parameter). Dient auch als Cache-Schlüssel.</summary>
    public struct TexSpec
    {
        public string Kind;
        public Color A, B;
        public float Scale, Tile, Stain, Ribs, Seed, LitRatio, PlankW;
        public Vector2 Size;

        public string Key()
        {
            return Kind + "|" + ColorUtility.ToHtmlStringRGB(A) + "|" + ColorUtility.ToHtmlStringRGB(B) + "|" +
                   Mathf.RoundToInt(Scale * 1000) + "|" + Mathf.RoundToInt(Tile * 1000) + "|" + Mathf.RoundToInt(Stain * 1000) + "|" +
                   Mathf.RoundToInt(Ribs * 100) + "|" + Mathf.RoundToInt(Seed * 100) + "|" + Mathf.RoundToInt(LitRatio * 100) + "|" +
                   Mathf.RoundToInt(PlankW * 1000) + "|" + Mathf.RoundToInt(Size.x * 1000) + "x" + Mathf.RoundToInt(Size.y * 1000);
        }
    }

    /// <summary>Fertiger Textursatz: Grundfarbe, Normal-Map, optional Leucht-Map. Meters = Kachelgröße in Metern.</summary>
    public sealed class TexSet
    {
        public Texture2D Albedo, Normal, Emission;
        public Vector2 Meters = Vector2.one;
        public float NormalStrength = 1f;
        public float Smoothness = 0.15f;
        public float Metallic;
    }

    /// <summary>
    /// Erzeugt alle Oberflächen-Texturen per Code (Beton, Asphalt, Ziegel, Holz, Fliesen, Gras,
    /// Wellblech, Fassaden mit Fenstern, Karton, Förderband). Alle Texturen sind nahtlos kachelbar.
    /// </summary>
    public static class TexGen
    {
        private static readonly Dictionary<string, TexSet> Cache = new Dictionary<string, TexSet>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();

        // ---- Rauschen (kachelbar) -------------------------------------------------------------------
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        private static int Wrap(int v, int p) => ((v % p) + p) % p;

        /// <summary>Wertrauschen mit Periode p (in Gitterzellen).</summary>
        private static float VNoise(float x, float y, int p, int seed)
        {
            p = Mathf.Max(1, p);
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy);
            float a = Hash(Wrap(ix, p), Wrap(iy, p), seed);
            float b = Hash(Wrap(ix + 1, p), Wrap(iy, p), seed);
            float c = Hash(Wrap(ix, p), Wrap(iy + 1, p), seed);
            float d = Hash(Wrap(ix + 1, p), Wrap(iy + 1, p), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        /// <summary>Fraktales Rauschen, u/v in 0..1 (eine Kachel), basePeriod = Zellen pro Kachel.</summary>
        private static float Fbm(float u, float v, int basePeriod, int seed, int octaves = 4)
        {
            float val = 0f, amp = 0.5f;
            int p = Mathf.Max(1, basePeriod);
            for (int i = 0; i < octaves; i++)
            {
                val += amp * VNoise(u * p, v * p, p, seed + i * 17);
                p *= 2;
                amp *= 0.5f;
            }
            return val;
        }

        private static Texture2D NewTex(int w, int h, bool linear, string name)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true, linear)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };
            return t;
        }

        private static Texture2D ToTexture(Color[] px, int w, int h, string name)
        {
            var t = NewTex(w, h, false, name);
            t.SetPixels(px);
            t.Apply(true, true);
            return t;
        }

        /// <summary>Normal-Map aus einer Höhenkarte (kachelbar, lineare Textur).</summary>
        private static Texture2D NormalFromHeight(float[] hgt, int w, int h, float strength, string name)
        {
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float l = hgt[y * w + Wrap(x - 1, w)], r = hgt[y * w + Wrap(x + 1, w)];
                    float d = hgt[Wrap(y - 1, h) * w + x], u = hgt[Wrap(y + 1, h) * w + x];
                    var n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    px[y * w + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            var t = NewTex(w, h, true, name);
            t.SetPixels(px);
            t.Apply(true, true);
            return t;
        }

        public static TexSet Get(TexSpec s)
        {
            string key = s.Key();
            if (Cache.TryGetValue(key, out var cached) && cached != null && cached.Albedo != null) return cached;
            TexSet set;
            switch (s.Kind)
            {
                case "concrete": set = Concrete(s); break;
                case "asphalt": set = Asphalt(s); break;
                case "brick": set = Brick(s); break;
                case "planks": set = Planks(s); break;
                case "tiles": set = Tiles(s); break;
                case "grass": set = Grass(s); break;
                case "metal_sheet": set = MetalSheet(s); break;
                case "facade": set = Facade(s); break;
                case "cardboard": set = Cardboard(s); break;
                case "conveyor": set = Conveyor(); break;
                default: set = Concrete(s); break;
            }
            Cache[key] = set;
            return set;
        }

        // ---- Einzelne Oberflächen -----------------------------------------------------------------------
        private static TexSet Concrete(TexSpec s)
        {
            const int n = 256;
            const float meters = 8f;
            var px = new Color[n * n];
            var hgt = new float[n * n];
            int coarse = Mathf.Max(1, Mathf.RoundToInt(meters * Mathf.Max(0.05f, s.Scale)));
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float nz = Fbm(u, v, coarse, 11);
                    float fine = VNoise(u * 72f, v * 72f, 72, 5) * 0.07f;
                    float stain = Mathf.SmoothStep(0.62f, 0.8f, Fbm(u, v, 1, 29, 3)) * s.Stain;
                    Color c = Color.Lerp(s.B, s.A, nz) - new Color(fine, fine, fine, 0f) - new Color(stain * 0.18f, stain * 0.18f, stain * 0.18f, 0f);
                    float hv = fine * 4f + nz * 0.3f;
                    if (s.Tile > 0f)
                    {
                        float gx = Mathf.Abs(Mathf.Repeat(u * meters / s.Tile, 1f) - 0.5f);
                        float gy = Mathf.Abs(Mathf.Repeat(v * meters / s.Tile, 1f) - 0.5f);
                        if (Mathf.Max(gx, gy) > 0.475f)
                        {
                            c *= 0.72f;
                            hv -= 0.6f;
                        }
                    }
                    c.a = 1f;
                    px[y * n + x] = c;
                    hgt[y * n + x] = hv;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "concrete"), Normal = NormalFromHeight(hgt, n, n, 3f, "concrete_n"),
                Meters = new Vector2(meters, meters), NormalStrength = 0.6f, Smoothness = 0.12f,
            };
        }

        private static TexSet Asphalt(TexSpec s)
        {
            const int n = 256;
            const float meters = 8f;
            var px = new Color[n * n];
            var hgt = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float nz = Fbm(u, v, 4, 3);
                    float speck = VNoise(u * 288f, v * 288f, 288, 9) > 0.83f ? 0.05f : 0f;
                    float g = 0.82f + nz * 0.35f;
                    px[y * n + x] = new Color(s.A.r * g + speck, s.A.g * g + speck, s.A.b * g + speck, 1f);
                    hgt[y * n + x] = VNoise(u * 128f, v * 128f, 128, 4) + speck * 4f;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "asphalt"), Normal = NormalFromHeight(hgt, n, n, 2.5f, "asphalt_n"),
                Meters = new Vector2(meters, meters), NormalStrength = 0.5f, Smoothness = 0.08f,
            };
        }

        private static TexSet Brick(TexSpec s)
        {
            const int n = 512;
            Vector2 size = s.Size.x > 0f ? s.Size : new Vector2(0.5f, 0.2f);
            int cols = Mathf.Max(1, Mathf.RoundToInt(2f / size.x));
            int rows = Mathf.Max(2, Mathf.RoundToInt(2f / size.y));
            if (rows % 2 == 1) rows++;
            var meters = new Vector2(cols * size.x, rows * size.y);
            var px = new Color[n * n];
            var hgt = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n * cols, v = y / (float)n * rows;
                    int row = Mathf.FloorToInt(v);
                    float uu = u + (row % 2) * 0.5f;
                    int col = Mathf.FloorToInt(uu);
                    float fx = uu - col, fy = v - row;
                    bool mortar = fx < 0.035f || fy < 0.08f;
                    float h = Hash(Wrap(col, cols), Wrap(row, rows), 21);
                    float grain = VNoise(x / (float)n * 48f, y / (float)n * 48f, 48, 6);
                    Color bc = s.A * (0.78f + 0.36f * h) * (0.9f + 0.2f * grain);
                    Color c = mortar ? s.B * (0.92f + 0.1f * grain) : bc;
                    c.a = 1f;
                    px[y * n + x] = c;
                    float edge = Mathf.Min(Mathf.Min(fx, 1f - fx) * 6f, Mathf.Min(fy, 1f - fy) * 3f);
                    hgt[y * n + x] = mortar ? 0f : Mathf.Clamp01(0.6f + edge) + grain * 0.15f;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "brick"), Normal = NormalFromHeight(hgt, n, n, 4f, "brick_n"),
                Meters = meters, NormalStrength = 1f, Smoothness = 0.1f,
            };
        }

        private static TexSet Planks(TexSpec s)
        {
            const int n = 256;
            float pw = s.PlankW > 0f ? s.PlankW : 0.2f;
            int count = Mathf.Max(1, Mathf.RoundToInt(2f / pw));
            var meters = new Vector2(pw * count, 2.4f);
            var px = new Color[n * n];
            var hgt = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float pu = u * count;
                    int idx = Mathf.FloorToInt(pu);
                    float h = Hash(idx, 3, 31);
                    float along = Mathf.Repeat(v + h, 1f);
                    bool seam = (pu - idx) > 0.95f || along > 0.988f;
                    float grain = VNoise(pu * 40f / count * count, along * 8f, 8, 7) * 0.22f + Fbm(u, along, 3, 8, 3) * 0.2f;
                    Color c = s.A * (0.72f + 0.3f * h + grain);
                    if (seam) c *= 0.55f;
                    c.a = 1f;
                    px[y * n + x] = c;
                    hgt[y * n + x] = seam ? 0f : 0.7f + grain * 0.5f;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "planks"), Normal = NormalFromHeight(hgt, n, n, 2.5f, "planks_n"),
                Meters = meters, NormalStrength = 0.8f, Smoothness = 0.35f,
            };
        }

        private static TexSet Tiles(TexSpec s)
        {
            const int n = 128;
            float ts = s.Scale > 0f ? s.Scale : 0.5f;
            var px = new Color[n * n];
            var hgt = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n * 2f, v = y / (float)n * 2f;
                    int cx = Mathf.FloorToInt(u), cy = Mathf.FloorToInt(v);
                    bool chk = (cx + cy) % 2 == 1;
                    float gx = Mathf.Abs(Mathf.Repeat(u, 1f) - 0.5f), gy = Mathf.Abs(Mathf.Repeat(v, 1f) - 0.5f);
                    bool grout = Mathf.Max(gx, gy) > 0.47f;
                    Color c = chk ? s.B : s.A;
                    if (grout) c = Color.Lerp(c, new Color(0.45f, 0.45f, 0.45f), 0.6f);
                    c *= 0.93f + 0.07f * VNoise(u * 3f, v * 3f, 6, 2);
                    c.a = 1f;
                    px[y * n + x] = c;
                    hgt[y * n + x] = grout ? 0f : 1f;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "tiles"), Normal = NormalFromHeight(hgt, n, n, 2f, "tiles_n"),
                Meters = new Vector2(ts * 2f, ts * 2f), NormalStrength = 0.7f, Smoothness = 0.65f,
            };
        }

        private static TexSet Grass(TexSpec s)
        {
            const int n = 256;
            const float meters = 8f;
            var px = new Color[n * n];
            var hgt = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float nz = Fbm(u, v, 3, 12);
                    float d = VNoise(u * 112f, v * 112f, 112, 13);
                    Color c = Color.Lerp(s.A, s.B, nz) * (0.88f + d * 0.22f);
                    c.a = 1f;
                    px[y * n + x] = c;
                    hgt[y * n + x] = d;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "grass"), Normal = NormalFromHeight(hgt, n, n, 1.5f, "grass_n"),
                Meters = new Vector2(meters, meters), NormalStrength = 0.6f, Smoothness = 0.05f,
            };
        }

        private static TexSet MetalSheet(TexSpec s)
        {
            const int n = 256;
            int ribs = Mathf.Max(1, Mathf.RoundToInt((s.Ribs > 0f ? s.Ribs : 22f) / (2f * Mathf.PI)));
            var px = new Color[n * n];
            var hgt = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float r = Mathf.Sin(u * ribs * Mathf.PI * 2f) * 0.5f + 0.5f;
                    float grime = Fbm(u, v, 2, 14, 3);
                    float streak = VNoise(u * 12f, v * 1f, 12, 15);
                    Color c = s.A * (0.78f + 0.2f * r) * (0.82f + 0.3f * grime) * (0.92f + 0.1f * streak);
                    c.a = 1f;
                    px[y * n + x] = c;
                    hgt[y * n + x] = r;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "metal"), Normal = NormalFromHeight(hgt, n, n, 5f, "metal_n"),
                Meters = new Vector2(1f, 1f), NormalStrength = 1f, Smoothness = 0.45f, Metallic = 0.55f,
            };
        }

        private static TexSet Facade(TexSpec s)
        {
            const int n = 512;
            const int cols = 6, rows = 5;
            var meters = new Vector2(cols * 3f, rows * 3.2f);
            var px = new Color[n * n];
            var em = new Color[n * n];
            var hgt = new float[n * n];
            int seed = Mathf.RoundToInt(s.Seed * 10f);
            float lit = s.LitRatio > 0f ? s.LitRatio : 0.45f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n * cols, v = y / (float)n * rows;
                    int cx = Mathf.FloorToInt(u), cy = Mathf.FloorToInt(v);
                    float fx = u - cx, fy = v - cy;
                    bool win = fx > 0.24f && fx < 0.76f && fy > 0.3f && fy < 0.82f;
                    bool sill = fx > 0.2f && fx < 0.8f && fy > 0.26f && fy < 0.3f;
                    float wn = VNoise(x / (float)n * 32f, y / (float)n * 32f, 32, 40 + seed);
                    Color wall = s.A * (0.85f + 0.15f * wn);
                    Color c = wall;
                    Color e = Color.black;
                    float h = 0.5f;
                    if (win)
                    {
                        c = Color.Lerp(new Color(0.16f, 0.21f, 0.27f), new Color(0.34f, 0.43f, 0.52f), fy);
                        bool frame = Mathf.Abs(fx - 0.5f) < 0.012f || Mathf.Abs(fy - 0.56f) < 0.012f;
                        if (frame) c = new Color(0.85f, 0.85f, 0.82f);
                        float lh = Hash(cx, cy, 77 + seed);
                        if (lh > 1f - lit && !frame)
                        {
                            float warm = Hash(cx, cy, 99 + seed);
                            e = Color.Lerp(new Color(1f, 0.78f, 0.45f), new Color(0.75f, 0.85f, 1f), warm * 0.4f);
                        }
                        h = 0.2f;
                    }
                    else if (sill)
                    {
                        c = new Color(0.8f, 0.79f, 0.76f);
                        h = 0.8f;
                    }
                    c.a = 1f;
                    e.a = 1f;
                    px[y * n + x] = c;
                    em[y * n + x] = e;
                    hgt[y * n + x] = h;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "facade"), Emission = ToTexture(em, n, n, "facade_e"),
                Normal = NormalFromHeight(hgt, n, n, 3f, "facade_n"), Meters = meters, NormalStrength = 0.8f, Smoothness = 0.25f,
            };
        }

        private static TexSet Cardboard(TexSpec s)
        {
            const int n = 128;
            var px = new Color[n * n];
            var hgt = new float[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n;
                    float nz = VNoise(u * 18f, v * 18f, 18, 50) * 0.08f + Fbm(u, v, 2, 51, 3) * 0.1f;
                    float corr = Mathf.Sin(u * Mathf.PI * 2f * 22f) * 0.015f;
                    Color c = s.A * (0.9f + nz + corr);
                    c.a = 1f;
                    px[y * n + x] = c;
                    hgt[y * n + x] = corr * 10f + nz;
                }
            }
            return new TexSet
            {
                Albedo = ToTexture(px, n, n, "cardboard"), Normal = NormalFromHeight(hgt, n, n, 1.2f, "cardboard_n"),
                Meters = new Vector2(1f, 1f), NormalStrength = 0.4f, Smoothness = 0.08f,
            };
        }

        private static TexSet Conveyor()
        {
            const int w = 64, h = 8;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float s = x < w / 2 ? 0.07f : 0.12f;
                    px[y * w + x] = new Color(s, s, s, 1f);
                }
            }
            var t = ToTexture(px, w, h, "conveyor");
            t.filterMode = FilterMode.Bilinear;
            return new TexSet { Albedo = t, Meters = new Vector2(1f / 3f, 1f), Smoothness = 0.2f };
        }
    }
}
