using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// 3D-Text in der Welt (Schilder, Namensschilder, schwebende Beträge).
    /// Basiert auf TextMesh mit eigenem Shader (DS/Text3D), damit Text korrekt verdeckt wird
    /// und optional im Bloom leuchten kann. Größe wie in Godot: size = Schriftgröße (× 4 mm).
    /// </summary>
    /// <summary>Schriftstil für Schilder (Resources/Fonts/Sign-*.ttf). Default = bisherige Standardschrift.</summary>
    public enum SignFont { Default, Display, Condensed, Script, Stencil, Retro }

    public sealed class Label3D : MonoBehaviour
    {
        private const int FontResolution = 64;

        public TextMesh Mesh;
        public bool Billboard;
        public float MaxDistance;
        private MeshRenderer _renderer;
        private Material _material;

        private static Font _font;
        private static readonly List<Material> Materials = new List<Material>();
        private static Transform _cam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _font = null;
            Materials.Clear();
            Shared.Clear();
            _cam = null;
            SignFonts.Clear();
            FontMaterials.Clear();
            SharedStyled.Clear();
        }

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch (System.Exception)
                {
                    _font = null;
                }
                if (_font == null)
                {
                    try
                    {
                        _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                    catch (System.Exception)
                    {
                        _font = null;
                    }
                }
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica", "DejaVu Sans" }, FontResolution);
                Font.textureRebuilt += OnFontRebuilt;
                return _font;
            }
        }

        private static void OnFontRebuilt(Font f)
        {
            if (f == null) return;
            if (f == _font)
                foreach (var m in Materials)
                    if (m != null) m.mainTexture = f.material.mainTexture;
            if (FontMaterials.TryGetValue(f, out var list))
                foreach (var m in list)
                    if (m != null) m.mainTexture = f.material.mainTexture;
        }

        private static readonly Dictionary<SignFont, Font> SignFonts = new Dictionary<SignFont, Font>();
        private static readonly Dictionary<Font, List<Material>> FontMaterials = new Dictionary<Font, List<Material>>();
        private static readonly Dictionary<string, Material> SharedStyled = new Dictionary<string, Material>();

        /// <summary>Schrift zu einem Stil (null = Standardschrift / fehlt).</summary>
        public static Font GetSignFont(SignFont style)
        {
            if (style == SignFont.Default) return null;
            if (SignFonts.TryGetValue(style, out var f)) return f;
            string key = style == SignFont.Display ? "display" : style == SignFont.Condensed ? "sign_condensed" :
                style == SignFont.Script ? "sign_script" : style == SignFont.Stencil ? "sign_stencil" : "sign_retro";
            f = null;
            try { f = AssetLib.Font(key); }
            catch (System.Exception) { f = null; }
            if (f != null && f.material == null) f = null;
            SignFonts[style] = f;
            if (f != null)
            {
                var unused = Font; // sorgt dafür, dass OnFontRebuilt registriert ist
            }
            return f;
        }

        private static Material StyledMaterial(Font f, float intensity, bool onTop)
        {
            string key = f.GetInstanceID() + ":" + Mathf.RoundToInt(intensity * 10f) + (onTop ? "t" : "n");
            if (SharedStyled.TryGetValue(key, out var m) && m != null) return m;
            var sh = Shader.Find("DS/Text3D");
            m = sh != null ? new Material(sh) : new Material(f.material);
            m.mainTexture = f.material.mainTexture;
            if (sh != null)
            {
                m.SetFloat("_Intensity", intensity);
                m.SetFloat("_ZTest", onTop ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            }
            m.renderQueue = onTop ? 3100 : 3000;
            if (!FontMaterials.TryGetValue(f, out var list)) { list = new List<Material>(); FontMaterials[f] = list; }
            list.Add(m);
            SharedStyled[key] = m;
            return m;
        }

        private float _glow = 1f;
        private bool _onTop;

        /// <summary>Wechselt die Schrift (fehlt sie, bleibt alles wie es ist). Gibt this zurück.</summary>
        public Label3D UseFont(SignFont style)
        {
            if (!Alive || style == SignFont.Default) return this;
            var f = GetSignFont(style);
            if (f == null || _renderer == null) return this;
            try
            {
                Mesh.font = f;
                _material = StyledMaterial(f, _glow, _onTop);
                _renderer.sharedMaterial = _material;
            }
            catch (System.Exception) { }
            return this;
        }

        private static Material NewMaterial(float intensity, bool onTop)
        {
            var sh = Shader.Find("DS/Text3D");
            Material m = sh != null ? new Material(sh) : new Material(Font.material);
            m.mainTexture = Font.material.mainTexture;
            if (sh != null)
            {
                m.SetFloat("_Intensity", intensity);
                m.SetFloat("_ZTest", onTop ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            }
            m.renderQueue = onTop ? 3100 : 3000;
            Materials.Add(m);
            return m;
        }

        private static readonly Dictionary<string, Material> Shared = new Dictionary<string, Material>();

        private static Material SharedMaterial(float intensity, bool onTop)
        {
            string key = Mathf.RoundToInt(intensity * 10f) + (onTop ? "t" : "n");
            if (Shared.TryGetValue(key, out var m) && m != null) return m;
            m = NewMaterial(intensity, onTop);
            Shared[key] = m;
            return m;
        }

        /// <summary>
        /// Erzeugt ein Label. billboard = dreht sich zur Kamera. Nicht-Billboards sind (wie in Godot)
        /// von der +Z-Seite ihres Elternobjekts lesbar. glow &gt; 1 lässt den Text leuchten.
        /// </summary>
        public static Label3D Create(Transform parent, string text, float size, Color color, Vector3 localPos, bool billboard = true,
            float maxDistance = 0f, bool onTop = false, float glow = 1f, int wrapChars = 0, SignFont font = SignFont.Default)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            if (!billboard) go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.fontSize = FontResolution;
            tm.characterSize = size * 0.004f / (FontResolution * 0.1f);
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.lineSpacing = 0.9f;
            tm.richText = false;
            tm.color = color;
            var l = go.AddComponent<Label3D>();
            l.Mesh = tm;
            l._renderer = r;
            l.Billboard = billboard;
            l.MaxDistance = maxDistance;
            l._material = SharedMaterial(glow, onTop);
            r.sharedMaterial = l._material;
            l._glow = glow;
            l._onTop = onTop;
            l.Wrap = wrapChars;
            l.SetText(text);
            if (font != SignFont.Default) l.UseFont(font);
            return l;
        }

        public int Wrap;

        /// <summary>False, wenn Label oder TextMesh schon zerstört wurden (z. B. Welt neu gebaut).</summary>
        public bool Alive => this != null && Mesh != null;

        public void SetText(string text)
        {
            if (!Alive) return;
            Mesh.text = Wrap > 0 ? WordWrap(text, Wrap) : text;
        }

        public string Text => Alive ? Mesh.text : "";

        public void SetColor(Color c)
        {
            if (Alive) Mesh.color = c;
        }

        public void SetAlpha(float a)
        {
            if (!Alive) return;
            var c = Mesh.color;
            c.a = a;
            Mesh.color = c;
        }

        public static string WordWrap(string text, int maxChars)
        {
            var sb = new StringBuilder();
            foreach (var para in text.Split('\n'))
            {
                if (sb.Length > 0) sb.Append('\n');
                int lineLen = 0;
                foreach (var word in para.Split(' '))
                {
                    if (lineLen > 0 && lineLen + 1 + word.Length > maxChars)
                    {
                        sb.Append('\n');
                        lineLen = 0;
                    }
                    else if (lineLen > 0)
                    {
                        sb.Append(' ');
                        lineLen++;
                    }
                    sb.Append(word);
                    lineLen += word.Length;
                }
            }
            return sb.ToString();
        }

        private void LateUpdate()
        {
            if (!Billboard && MaxDistance <= 0f) return;
            if (_renderer == null) return;
            if (_cam == null)
            {
                var c = Camera.main;
                if (c == null) return;
                _cam = c.transform;
            }
            if (Billboard) transform.rotation = _cam.rotation;
            if (MaxDistance > 0f)
            {
                bool vis = (transform.position - _cam.position).sqrMagnitude < MaxDistance * MaxDistance;
                if (_renderer.enabled != vis) _renderer.enabled = vis;
            }
        }

        /// <summary>Kamera wechselt (Menü ↔ Spiel): Zwischenspeicher leeren.</summary>
        public static void ResetCamera() => _cam = null;
    }
}
