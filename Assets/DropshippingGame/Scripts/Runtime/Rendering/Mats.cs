using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Material-Bibliothek: PBR-Materialien (URP Lit, Fallback: Standard) mit prozeduralen Texturen,
    /// Leuchtmaterialien, Glas und der Hervorhebung beim Anvisieren. Alles wird gecached.
    /// Materialien, die nachts anders aussehen (Fenster, Lampen, Fassaden), werden über
    /// <see cref="SetNight"/> zentral nachgeführt.
    /// </summary>
    public static class Mats
    {
        private sealed class NightMat
        {
            public Material Mat;
            public Color BaseDay, BaseNight, EmDay, EmNight;
            public bool ChangeBase;
        }

        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static readonly List<NightMat> NightMats = new List<NightMat>();
        private static Shader _lit, _unlit;
        private static Material _tplLit, _tplTransparent;
        private static bool _init;
        private static float _night = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Cache.Clear();
            NightMats.Clear();
            _init = false;
            _night = -1f;
        }

        public static bool Urp => GraphicsSettings.currentRenderPipeline != null;

        private static void Init()
        {
            if (_init) return;
            _init = true;
            if (Urp)
            {
                _tplLit = Resources.Load<Material>("DSMaterials/Lit");
                _tplTransparent = Resources.Load<Material>("DSMaterials/LitTransparent");
                _lit = Shader.Find("Universal Render Pipeline/Lit");
                _unlit = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (_lit == null) _lit = Shader.Find("Standard");
            if (_unlit == null) _unlit = Shader.Find("Unlit/Color");
        }

        private static Material NewLit(bool transparent = false)
        {
            Init();
            Material m;
            if (transparent && _tplTransparent != null) m = new Material(_tplTransparent);
            else if (!transparent && _tplLit != null) m = new Material(_tplLit);
            else m = new Material(_lit);
            if (transparent) MakeTransparent(m);
            return m;
        }

        private static void SetColor(Material m, Color c)
        {
            m.SetColor("_BaseColor", c);
            m.SetColor("_Color", c);
        }

        private static void SetSurface(Material m, float smoothness, float metallic)
        {
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
        }

        private static void SetEmission(Material m, Color e, Texture2D map = null)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", e);
            if (map != null) m.SetTexture("_EmissionMap", map);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private static void MakeTransparent(Material m)
        {
            if (Urp)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetShaderPassEnabled("DepthOnly", false);
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            else
            {
                m.SetFloat("_Mode", 3f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.DisableKeyword("_ALPHATEST_ON");
                m.DisableKeyword("_ALPHABLEND_ON");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private static string K(string kind, Color c, float a = 0f, float b = 0f) =>
            kind + ":" + ColorUtility.ToHtmlStringRGBA(c) + ":" + Mathf.RoundToInt(a * 100) + ":" + Mathf.RoundToInt(b * 100);

        // ---- Grundmaterialien -----------------------------------------------------------------------
        /// <summary>Einfaches PBR-Material. roughness wie in Godot (0 = glänzend, 1 = matt).</summary>
        public static Material Std(Color color, float roughness = 0.8f, float metallic = 0f)
        {
            string key = K("std", color, roughness, metallic);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = NewLit(color.a < 0.99f);
            m.name = key;
            SetColor(m, color);
            SetSurface(m, 1f - roughness, metallic);
            Cache[key] = m;
            return m;
        }

        /// <summary>Selbstleuchtendes Material (Neon, Displays, Lampen). energy &gt; 1 lässt es im Bloom glühen.</summary>
        public static Material Emit(Color color, float energy = 2f)
        {
            string key = K("emit", color, energy);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = NewLit();
            m.name = key;
            SetColor(m, color);
            SetSurface(m, 0.5f, 0f);
            SetEmission(m, color * energy);
            Cache[key] = m;
            return m;
        }

        public static Material Glass(Color? tint = null)
        {
            Color t = tint ?? new Color(0.7f, 0.85f, 0.95f, 0.25f);
            string key = K("glass", t);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = NewLit(true);
            m.name = key;
            SetColor(m, t);
            SetSurface(m, 0.95f, 0.3f);
            Cache[key] = m;
            return m;
        }

        /// <summary>Material mit prozeduraler Textur (Beton, Ziegel, Holz ...).</summary>
        public static Material Tex(TexSpec spec, float? smoothness = null, Color? tint = null)
        {
            string key = "tex:" + spec.Key() + ":" + (smoothness.HasValue ? Mathf.RoundToInt(smoothness.Value * 100) : -1) +
                         ":" + (tint.HasValue ? ColorUtility.ToHtmlStringRGB(tint.Value) : "");
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var set = TexGen.Get(spec);
            m = NewLit();
            m.name = spec.Kind;
            SetColor(m, tint ?? Color.white);
            ApplyTexSet(m, set, smoothness ?? set.Smoothness);
            Cache[key] = m;
            return m;
        }

        private static void ApplyTexSet(Material m, TexSet set, float smoothness)
        {
            var scale = new Vector2(1f / set.Meters.x, 1f / set.Meters.y);
            m.SetTexture("_BaseMap", set.Albedo);
            m.SetTexture("_MainTex", set.Albedo);
            m.SetTextureScale("_BaseMap", scale);
            m.SetTextureScale("_MainTex", scale);
            if (set.Normal != null && Settings.Quality > 0)
            {
                m.SetTexture("_BumpMap", set.Normal);
                m.SetTextureScale("_BumpMap", scale);
                m.SetFloat("_BumpScale", set.NormalStrength);
                m.EnableKeyword("_NORMALMAP");
            }
            SetSurface(m, smoothness, set.Metallic);
        }

        // ---- Oberflächen wie im Godot-Original ----------------------------------------------------------
        public static Material Concrete(Color a, Color b, float scale = 0.35f, float tile = 0f, float stain = 0.25f) =>
            Tex(new TexSpec { Kind = "concrete", A = a, B = b, Scale = scale, Tile = tile, Stain = stain });

        public static Material Concrete() => Concrete(new Color(0.56f, 0.56f, 0.54f), new Color(0.44f, 0.44f, 0.43f));
        public static Material Asphalt() => Tex(new TexSpec { Kind = "asphalt", A = new Color(0.17f, 0.17f, 0.18f) });

        public static Material Brick(Color brick, Color mortar, Vector2 size = default) =>
            Tex(new TexSpec { Kind = "brick", A = brick, B = mortar, Size = size.x > 0 ? size : new Vector2(0.5f, 0.2f) });

        public static Material Planks(Color wood, float plankW = 0.2f) => Tex(new TexSpec { Kind = "planks", A = wood, PlankW = plankW });
        public static Material Tiles(float tileSize = 0.5f) =>
            Tex(new TexSpec { Kind = "tiles", A = new Color(0.92f, 0.92f, 0.9f), B = new Color(0.13f, 0.13f, 0.15f), Scale = tileSize });

        public static Material Grass() => Tex(new TexSpec { Kind = "grass", A = new Color(0.27f, 0.48f, 0.2f), B = new Color(0.42f, 0.6f, 0.27f) });
        public static Material MetalSheet(Color baseColor, float ribs = 22f) => Tex(new TexSpec { Kind = "metal_sheet", A = baseColor, Ribs = ribs });
        public static Material Cardboard() => Tex(new TexSpec { Kind = "cardboard", A = new Color(0.68f, 0.51f, 0.33f) });
        public static Material Wood() => Planks(new Color(0.6f, 0.42f, 0.26f), 0.12f);
        public static Material Metal() => Std(new Color(0.6f, 0.62f, 0.65f), 0.35f, 0.8f);
        public static Material DarkMetal() => Std(new Color(0.18f, 0.19f, 0.21f), 0.45f, 0.7f);

        /// <summary>Laub: matte Farbe (die Facetten kommen vom Mesh).</summary>
        public static Material Foliage(Color c) => Std(c, 0.92f);

        /// <summary>Hausfassade mit Fenstern, die nachts zufällig leuchten.</summary>
        public static Material Facade(Color wall, float seed, float litRatio = 0.45f)
        {
            var spec = new TexSpec { Kind = "facade", A = wall, Seed = Mathf.Round(seed * 10f) / 10f, LitRatio = litRatio };
            string key = "facade:" + spec.Key();
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var set = TexGen.Get(spec);
            m = NewLit();
            m.name = "facade";
            SetColor(m, Color.white);
            ApplyTexSet(m, set, set.Smoothness);
            SetEmission(m, Color.black, set.Emission);
            RegisterNight(m, Color.white, Color.white, Color.black, new Color(2.4f, 2.4f, 2.4f), false);
            Cache[key] = m;
            return m;
        }

        /// <summary>Fensterscheibe. interior = Innenseite (tagsüber hell, nachts warm).</summary>
        public static Material Window(bool interior)
        {
            string key = interior ? "window_in" : "window_out";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = NewLit();
            m.name = key;
            SetSurface(m, 0.94f, 0.2f);
            if (interior)
            {
                SetColor(m, new Color(0.75f, 0.87f, 1f));
                SetEmission(m, new Color(0.5f, 0.66f, 0.82f));
                RegisterNight(m, new Color(0.75f, 0.87f, 1f), new Color(0.12f, 0.13f, 0.16f), new Color(0.5f, 0.66f, 0.82f), new Color(0.05f, 0.06f, 0.08f), true);
            }
            else
            {
                SetColor(m, new Color(0.22f, 0.3f, 0.38f));
                SetEmission(m, Color.black);
                RegisterNight(m, new Color(0.22f, 0.3f, 0.38f), new Color(0.06f, 0.07f, 0.09f), Color.black, new Color(1.9f, 1.52f, 0.99f), true);
            }
            Cache[key] = m;
            return m;
        }

        /// <summary>Leuchtfläche einer Lampe: nachts hell, tagsüber (außer alwaysOn) fast aus.</summary>
        public static Material Lamp(Color glow, bool alwaysOn = false)
        {
            string key = K("lamp", glow, alwaysOn ? 1f : 0f);
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = NewLit();
            m.name = "lamp";
            SetColor(m, new Color(0.9f, 0.9f, 0.9f));
            SetSurface(m, 0.5f, 0f);
            float day = 0.15f + (alwaysOn ? 1.5f : 0f);
            float night = 0.15f + Mathf.Max(4f, alwaysOn ? 1.5f : 0f);
            SetEmission(m, glow * day);
            RegisterNight(m, Color.white, Color.white, glow * day, glow * night, false);
            Cache[key] = m;
            return m;
        }

        /// <summary>Förderband mit laufenden Streifen (Bewegung über <see cref="BeltScroller"/>).</summary>
        public static Material Conveyor()
        {
            const string key = "conveyor";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var set = TexGen.Get(new TexSpec { Kind = "conveyor" });
            m = NewLit();
            m.name = key;
            SetColor(m, Color.white);
            ApplyTexSet(m, set, 0.2f);
            Cache[key] = m;
            return m;
        }

        /// <summary>Additive, pulsierende Kontur beim Anvisieren (eigener Shader, Fallback: halbtransparent).</summary>
        public static Material Highlight()
        {
            const string key = "highlight";
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            var sh = Shader.Find("DS/Highlight");
            if (sh != null)
            {
                m = new Material(sh) { name = key };
                m.SetColor("_Color", new Color(1f, 0.72f, 0.25f, 1f));
            }
            else
            {
                m = NewLit(true);
                SetColor(m, new Color(1f, 0.75f, 0.3f, 0.18f));
                SetEmission(m, new Color(0.6f, 0.4f, 0.1f));
            }
            Cache[key] = m;
            return m;
        }

        private static void RegisterNight(Material m, Color baseDay, Color baseNight, Color emDay, Color emNight, bool changeBase)
        {
            NightMats.Add(new NightMat { Mat = m, BaseDay = baseDay, BaseNight = baseNight, EmDay = emDay, EmNight = emNight, ChangeBase = changeBase });
            if (_night >= 0f) ApplyNight(NightMats[NightMats.Count - 1], _night);
        }

        private static void ApplyNight(NightMat n, float v)
        {
            if (n.Mat == null) return;
            if (n.ChangeBase) SetColor(n.Mat, Color.Lerp(n.BaseDay, n.BaseNight, v));
            n.Mat.SetColor("_EmissionColor", Color.Lerp(n.EmDay, n.EmNight, v));
        }

        /// <summary>0 = Tag, 1 = Nacht. Aktualisiert alle Fenster, Lampen und Fassaden.</summary>
        public static void SetNight(float v)
        {
            if (Mathf.Abs(v - _night) < 0.004f) return;
            _night = v;
            foreach (var n in NightMats) ApplyNight(n, v);
        }

        public static float Night => Mathf.Max(0f, _night);
    }

    /// <summary>Lässt die Streifen des Förderbands laufen.</summary>
    public sealed class BeltScroller : MonoBehaviour
    {
        public float Speed = -0.9f;
        private static float _offset;
        private static int _frame = -1;

        private void Update()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            _offset = Mathf.Repeat(_offset + Speed * Time.deltaTime, 1f);
            var m = Mats.Conveyor();
            m.SetTextureOffset("_BaseMap", new Vector2(_offset, 0f));
            m.SetTextureOffset("_MainTex", new Vector2(_offset, 0f));
        }
    }
}
