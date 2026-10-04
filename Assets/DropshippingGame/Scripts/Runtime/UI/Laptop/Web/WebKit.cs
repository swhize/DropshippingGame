using System;
using System.Collections.Generic;
using System.Globalization;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Schrift-Familie einer Webseite im Laptop-Browser.</summary>
    public enum WebSkin
    {
        /// <summary>Verspielt-rund (AllesExpress, Mein Shop): Fredoka.</summary>
        Round,
        /// <summary>Comic (iWolke, PaketBlitz, TradingViech): Comic Neue + Bangers.</summary>
        Comic,
        /// <summary>Neobank / Creator-Studio (Revoluut, TikTak): Inter + Bricolage.</summary>
        Neo,
    }

    /// <summary>Seiten, die Live-Werte ohne Neuaufbau aktualisieren (Trading-Ticks).</summary>
    public interface ILiveApp
    {
        void LiveUpdate();
    }

    /// <summary>
    /// Zusatzschriften der Browser-Seiten (Resources/Fonts/Web-*.ttf, alle SIL OFL). Fehlt eine
    /// Datei, wird auf die normalen HustleOS-Schriften zurückgefallen.
    /// </summary>
    public static class WebFonts
    {
        public const int Body = 0, Bold = 1, Title = 2, Comic = 3;
        private static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Cache.Clear();

        private static Font Get(string path)
        {
            if (Cache.TryGetValue(path, out var f)) return f;
            try
            {
                f = Resources.Load<Font>(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Web-Schrift " + path + " nicht geladen: " + e.Message);
                f = null;
            }
            Cache[path] = f;
            return f;
        }

        private static string PathFor(WebSkin skin, int weight)
        {
            if (weight == Comic) return "Fonts/Web-Comic-Title";
            switch (skin)
            {
                case WebSkin.Round:
                    return weight == Body ? "Fonts/Web-Round-Regular" : "Fonts/Web-Round-Bold";
                case WebSkin.Comic:
                    return weight == Body ? "Fonts/Web-Comic-Regular" : (weight == Bold ? "Fonts/Web-Comic-Bold" : "Fonts/Web-Comic-Title");
                default:
                    return weight == Body ? "Fonts/UI-Regular" : (weight == Bold ? "Fonts/UI-Bold" : "Fonts/Display-Bold");
            }
        }

        public static void Set(VisualElement el, WebSkin skin, int weight)
        {
            if (el == null) return;
            var f = Get(PathFor(skin, weight));
            if (f == null)
            {
                Fonts.Set(el, weight == Body ? Fonts.Regular : (weight == Bold ? Fonts.Bold : Fonts.Display));
                return;
            }
            try
            {
                el.style.unityFontDefinition = FontDefinition.FromFont(f);
                el.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Web-Schrift nicht gesetzt: " + e.Message);
            }
        }
    }

    /// <summary>Kleine Bausteine für die Browser-Seiten (eigene Klassen, eigene Schriften).</summary>
    public static class W
    {
        public static readonly CultureInfo En = CultureInfo.InvariantCulture;

        public static Label Text(VisualElement parent, string text, WebSkin skin, int weight, params string[] classes)
        {
            var l = new Label(text ?? "");
            l.RemoveFromClassList("unity-label");
            l.enableRichText = true;
            if (classes != null)
                foreach (var c in classes)
                    if (!string.IsNullOrEmpty(c))
                        l.AddToClassList(c);
            WebFonts.Set(l, skin, weight);
            return UIX.Attach(parent, l);
        }

        /// <summary>Button mit Beschriftung in der Seitenschrift. Klassen bestimmen das Aussehen.</summary>
        public static Button Button(VisualElement parent, string text, Action onClick, WebSkin skin, bool disabled, params string[] classes)
        {
            var b = UIX.Pressable(parent, onClick, "w-btn");
            if (classes != null)
                foreach (var c in classes)
                    if (!string.IsNullOrEmpty(c))
                        b.AddToClassList(c);
            b.style.flexDirection = FlexDirection.Row;
            b.style.alignItems = Align.Center;
            b.style.justifyContent = Justify.Center;
            var l = new Label(text ?? "");
            l.RemoveFromClassList("unity-label");
            l.enableRichText = true;
            l.AddToClassList("w-btn-label");
            l.pickingMode = PickingMode.Ignore;
            WebFonts.Set(l, skin, WebFonts.Bold);
            b.Add(l);
            b.SetEnabled(!disabled);
            return b;
        }

        public static void SetColor(VisualElement el, Color c) => el.style.backgroundColor = c;

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        public static Color Lighten(Color c, float t) => Color.Lerp(c, Color.white, Mathf.Clamp01(t));

        /// <summary>Deterministische Zahl aus einem Text (für stabile Fake-Werte).</summary>
        public static int Hash(string s)
        {
            unchecked
            {
                int h = 23;
                if (s != null)
                    foreach (char ch in s)
                        h = h * 31 + ch;
                return h & 0x7fffffff;
            }
        }

        public static string Eur(float v) => Fmt.Eur(v);

        /// <summary>Englisches Geldformat für Revoluut: "€3,480".</summary>
        public static string EnMoney(int v) => (v < 0 ? "−€" : "€") + Math.Abs(v).ToString("N0", En);

        public static string EnSigned(int v) => (v > 0 ? "+" : "") + EnMoney(v);

        public static string Thousands(int v) => Fmt.Thousands(v);

        /// <summary>"12.000+" – grob gerundet für Verkaufszahlen.</summary>
        public static string Rough(int v)
        {
            if (v >= 10000) return Fmt.Thousands(v / 1000 * 1000) + "+";
            if (v >= 1000) return Fmt.Thousands(v / 100 * 100) + "+";
            return v.ToString();
        }

        /// <summary>Markenname als URL-tauglicher Teil ("Nordlicht Goods" → "nordlicht-goods").</summary>
        public static string Slug(string s)
        {
            if (string.IsNullOrEmpty(s)) return "mein-shop";
            var sb = new System.Text.StringBuilder();
            foreach (char raw in s.ToLowerInvariant())
            {
                char ch = raw;
                if (ch == 'ä') sb.Append("ae");
                else if (ch == 'ö') sb.Append("oe");
                else if (ch == 'ü') sb.Append("ue");
                else if (ch == 'ß') sb.Append("ss");
                else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
                else if ((ch == ' ' || ch == '-' || ch == '_') && sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            }
            string r = sb.ToString().Trim('-');
            return r.Length == 0 ? "mein-shop" : r;
        }

        /// <summary>Sterne als Text (★ gefüllt, leere in Grau über Rich-Text). Nur mit Inter-Schrift (hat ★).</summary>
        public static string Stars(float rating)
        {
            int f = Mathf.Clamp(Mathf.RoundToInt(rating), 0, 5);
            return "<color=#FFB400>" + new string('★', f) + "</color><color=#D5D5D5>" + new string('★', 5 - f) + "</color>";
        }

        /// <summary>Sternezeile als eigenes Label in Inter (die Comic-/Rund-Schriften haben kein ★).</summary>
        public static Label StarText(VisualElement parent, float rating, params string[] classes)
        {
            var l = Text(parent, Stars(rating), WebSkin.Neo, WebFonts.Body, classes);
            l.AddToClassList("w-stars");
            return l;
        }

        /// <summary>Symbol (↻, ♥, ✓ …) in Inter, weil die Seitenschriften solche Zeichen nicht haben.</summary>
        public static Label Sym(VisualElement parent, string glyph, params string[] classes) => Text(parent, glyph, WebSkin.Neo, WebFonts.Bold, classes);

        /// <summary>Fläche mit Produktbild (Hintergrundfarbe, Punktmuster, Vektor-Icon).</summary>
        public static VisualElement Art(VisualElement parent, string productId, float height, bool comic, params string[] classes)
        {
            var box = UIX.Div(parent, "w-art");
            if (classes != null)
                foreach (var c in classes)
                    if (!string.IsNullOrEmpty(c))
                        box.AddToClassList(c);
            var col = GameData.IsProduct(productId) ? GameData.Product(productId).Color.ToColor() : new Color(0.8f, 0.8f, 0.9f);
            box.style.backgroundColor = Lighten(col, 0.55f);
            box.style.overflow = Overflow.Hidden;
            if (height > 0) box.style.height = height;
            box.pickingMode = PickingMode.Ignore;
            var art = new ProductArt(productId, comic);
            art.style.position = Position.Absolute;
            art.style.left = 0;
            art.style.top = 0;
            art.style.right = 0;
            art.style.bottom = 0;
            box.Add(art);
            return box;
        }
    }

    /// <summary>
    /// Einfache Vektor-Produktbilder per Painter2D (kein Web-Bild). Gezeichnet in einem
    /// 120×120-Raster und auf die Elementgröße skaliert. Dazu Punktmuster und Glanzlicht.
    /// </summary>
    public sealed class ProductArt : VisualElement
    {
        private readonly string _id;
        private readonly bool _comic;
        public bool Dots = true;
        public float IconScale = 0.78f;

        public ProductArt(string productId, bool comic)
        {
            _id = productId ?? "";
            _comic = comic;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 4f || r.height < 4f) return;
            var p = ctx.painter2D;
            if (p == null) return;
            try
            {
                if (Dots)
                {
                    p.fillColor = new Color(1f, 1f, 1f, 0.45f);
                    const float step = 22f;
                    for (float y = step / 2f; y < r.height; y += step)
                    for (float x = step / 2f; x < r.width; x += step)
                    {
                        p.BeginPath();
                        p.Arc(new Vector2(x, y), 2.6f, Angle.Degrees(0f), Angle.Degrees(360f));
                        p.Fill();
                    }
                    p.fillColor = new Color(1f, 1f, 1f, 0.35f);
                    p.BeginPath();
                    p.Arc(new Vector2(r.width * 0.28f, r.height * 0.24f), Mathf.Min(r.width, r.height) * 0.32f, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Fill();
                }
                float size = Mathf.Min(r.width, r.height) * IconScale;
                float s = size / 120f;
                var o = new Vector2((r.width - size) / 2f, (r.height - size) / 2f);
                Icon(p, _id, o, s, _comic);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static Vector2 V(Vector2 o, float s, float x, float y) => o + new Vector2(x * s, y * s);

        private static void RoundRect(Painter2D p, Vector2 o, float s, float x, float y, float w, float h, float rad)
        {
            rad = Mathf.Min(rad, Mathf.Min(w, h) / 2f);
            p.BeginPath();
            p.MoveTo(V(o, s, x + rad, y));
            p.LineTo(V(o, s, x + w - rad, y));
            p.ArcTo(V(o, s, x + w, y), V(o, s, x + w, y + rad), rad * s);
            p.LineTo(V(o, s, x + w, y + h - rad));
            p.ArcTo(V(o, s, x + w, y + h), V(o, s, x + w - rad, y + h), rad * s);
            p.LineTo(V(o, s, x + rad, y + h));
            p.ArcTo(V(o, s, x, y + h), V(o, s, x, y + h - rad), rad * s);
            p.LineTo(V(o, s, x, y + rad));
            p.ArcTo(V(o, s, x, y), V(o, s, x + rad, y), rad * s);
            p.ClosePath();
        }

        private static void Ellipse(Painter2D p, Vector2 o, float s, float cx, float cy, float rx, float ry)
        {
            const float k = 0.5523f;
            p.BeginPath();
            p.MoveTo(V(o, s, cx + rx, cy));
            p.BezierCurveTo(V(o, s, cx + rx, cy + ry * k), V(o, s, cx + rx * k, cy + ry), V(o, s, cx, cy + ry));
            p.BezierCurveTo(V(o, s, cx - rx * k, cy + ry), V(o, s, cx - rx, cy + ry * k), V(o, s, cx - rx, cy));
            p.BezierCurveTo(V(o, s, cx - rx, cy - ry * k), V(o, s, cx - rx * k, cy - ry), V(o, s, cx, cy - ry));
            p.BezierCurveTo(V(o, s, cx + rx * k, cy - ry), V(o, s, cx + rx, cy - ry * k), V(o, s, cx + rx, cy));
            p.ClosePath();
        }

        private static void Circle(Painter2D p, Vector2 o, float s, float cx, float cy, float rad)
        {
            p.BeginPath();
            p.Arc(V(o, s, cx, cy), rad * s, Angle.Degrees(0f), Angle.Degrees(360f));
            p.ClosePath();
        }

        private static void Line(Painter2D p, Vector2 o, float s, float x0, float y0, float x1, float y1)
        {
            p.BeginPath();
            p.MoveTo(V(o, s, x0, y0));
            p.LineTo(V(o, s, x1, y1));
            p.Stroke();
        }

        private static void FillStroke(Painter2D p)
        {
            p.Fill();
            p.Stroke();
        }

        /// <summary>Zeichnet das Icon eines Produkts. Auch für TikTak-Videos genutzt.</summary>
        public static void Icon(Painter2D p, string id, Vector2 o, float s, bool comic)
        {
            var ink = comic ? new Color(0.086f, 0.086f, 0.086f) : new Color(0.16f, 0.08f, 0.24f, 0.62f);
            var white = Color.white;
            p.strokeColor = ink;
            p.fillColor = white;
            p.lineJoin = UnityEngine.UIElements.LineJoin.Round;
            p.lineCap = UnityEngine.UIElements.LineCap.Round;
            p.lineWidth = 5f * s;
            switch (id)
            {
                case "huelle":
                    RoundRect(p, o, s, 32, 12, 56, 96, 12);
                    FillStroke(p);
                    p.strokeColor = new Color(0.54f, 0.54f, 0.6f);
                    p.lineWidth = 4f * s;
                    p.BeginPath();
                    p.MoveTo(V(o, s, 40, 32));
                    p.BezierCurveTo(V(o, s, 55, 42), V(o, s, 50, 62), V(o, s, 70, 72));
                    p.BezierCurveTo(V(o, s, 82, 80), V(o, s, 80, 95), V(o, s, 84, 100));
                    p.Stroke();
                    p.fillColor = ink;
                    Circle(p, o, s, 46, 25, 5);
                    p.Fill();
                    break;
                case "led":
                {
                    p.lineWidth = 12f * s;
                    p.BeginPath();
                    p.MoveTo(V(o, s, 10, 70));
                    p.BezierCurveTo(V(o, s, 30, 30), V(o, s, 50, 110), V(o, s, 70, 60));
                    p.BezierCurveTo(V(o, s, 85, 25), V(o, s, 100, 40), V(o, s, 112, 60));
                    p.Stroke();
                    Color[] bulbs = { W.Hex("#FF4B6E"), W.Hex("#FFD23F"), W.Hex("#3DD6B0"), W.Hex("#4C6FFF"), W.Hex("#B36BFF") };
                    for (int i = 0; i <= 12; i++)
                    {
                        float t = i / 12f;
                        Vector2 q = t < 0.5f
                            ? Bez(new Vector2(10, 70), new Vector2(30, 30), new Vector2(50, 110), new Vector2(70, 60), t * 2f)
                            : Bez(new Vector2(70, 60), new Vector2(85, 25), new Vector2(100, 40), new Vector2(112, 60), (t - 0.5f) * 2f);
                        p.fillColor = bulbs[i % bulbs.Length];
                        Circle(p, o, s, q.x, q.y, 4.2f);
                        p.Fill();
                    }
                    break;
                }
                case "massage":
                    p.fillColor = W.Hex("#9AA3B8");
                    RoundRect(p, o, s, 48, 54, 22, 52, 8);
                    FillStroke(p);
                    p.fillColor = white;
                    RoundRect(p, o, s, 18, 30, 76, 30, 14);
                    FillStroke(p);
                    p.fillColor = W.Hex("#FF8A5C");
                    Circle(p, o, s, 102, 45, 11);
                    FillStroke(p);
                    p.strokeColor = ink;
                    Line(p, o, s, 30, 45, 60, 45);
                    break;
                case "kopfhoerer":
                    p.fillColor = new Color(0, 0, 0, 0);
                    p.lineWidth = 9f * s;
                    p.BeginPath();
                    p.Arc(V(o, s, 60, 66), 40f * s, Angle.Degrees(180f), Angle.Degrees(360f));
                    p.Stroke();
                    p.lineWidth = 5f * s;
                    p.fillColor = W.Hex("#4C6FFF");
                    RoundRect(p, o, s, 12, 58, 22, 38, 10);
                    FillStroke(p);
                    RoundRect(p, o, s, 86, 58, 22, 38, 10);
                    FillStroke(p);
                    break;
                case "ringlicht":
                    p.lineWidth = 16f * s;
                    p.BeginPath();
                    p.Arc(V(o, s, 60, 50), 34f * s, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Stroke();
                    p.strokeColor = W.Hex("#FFF6C8");
                    p.lineWidth = 7f * s;
                    p.BeginPath();
                    p.Arc(V(o, s, 60, 50), 34f * s, Angle.Degrees(0f), Angle.Degrees(360f));
                    p.Stroke();
                    p.strokeColor = ink;
                    p.lineWidth = 6f * s;
                    Line(p, o, s, 60, 84, 60, 114);
                    Line(p, o, s, 40, 114, 80, 114);
                    break;
                case "katzenbrunnen":
                    Ellipse(p, o, s, 60, 88, 46, 16);
                    FillStroke(p);
                    p.BeginPath();
                    p.MoveTo(V(o, s, 28, 86));
                    p.BezierCurveTo(V(o, s, 28, 50), V(o, s, 92, 50), V(o, s, 92, 86));
                    p.ClosePath();
                    FillStroke(p);
                    p.strokeColor = W.Hex("#3AA0FF");
                    p.lineWidth = 6f * s;
                    p.BeginPath();
                    p.MoveTo(V(o, s, 60, 52));
                    p.BezierCurveTo(V(o, s, 60, 30), V(o, s, 74, 24), V(o, s, 76, 44));
                    p.Stroke();
                    break;
                case "haltung":
                    p.lineWidth = 14f * s;
                    p.BeginPath();
                    p.MoveTo(V(o, s, 30, 20));
                    p.LineTo(V(o, s, 60, 60));
                    p.LineTo(V(o, s, 90, 20));
                    p.Stroke();
                    Line(p, o, s, 60, 60, 60, 106);
                    Line(p, o, s, 32, 106, 88, 106);
                    p.strokeColor = W.Hex("#FF9A5C");
                    p.lineWidth = 6f * s;
                    p.BeginPath();
                    p.MoveTo(V(o, s, 30, 20));
                    p.LineTo(V(o, s, 60, 60));
                    p.LineTo(V(o, s, 90, 20));
                    p.Stroke();
                    break;
                case "smartwatch":
                    p.fillColor = W.Hex("#3A3F4B");
                    RoundRect(p, o, s, 46, 6, 28, 108, 10);
                    FillStroke(p);
                    p.fillColor = white;
                    RoundRect(p, o, s, 30, 32, 60, 56, 16);
                    FillStroke(p);
                    p.lineWidth = 4f * s;
                    Line(p, o, s, 60, 60, 60, 44);
                    Line(p, o, s, 60, 60, 72, 66);
                    break;
                case "beamer":
                    RoundRect(p, o, s, 14, 42, 92, 50, 10);
                    FillStroke(p);
                    p.fillColor = W.Hex("#6AA9FF");
                    Circle(p, o, s, 42, 67, 15);
                    FillStroke(p);
                    p.lineWidth = 4f * s;
                    Line(p, o, s, 62, 32, 80, 12);
                    Line(p, o, s, 74, 36, 102, 22);
                    p.fillColor = ink;
                    RoundRect(p, o, s, 70, 58, 26, 6, 2);
                    p.Fill();
                    break;
                case "drohne":
                    p.lineWidth = 6f * s;
                    Line(p, o, s, 26, 30, 94, 90);
                    Line(p, o, s, 94, 30, 26, 90);
                    p.lineWidth = 5f * s;
                    p.fillColor = W.Hex("#FF5A5F");
                    RoundRect(p, o, s, 42, 46, 36, 28, 10);
                    FillStroke(p);
                    p.fillColor = new Color(1f, 1f, 1f, 0.9f);
                    foreach (var c in new[] { new Vector2(26, 30), new Vector2(94, 30), new Vector2(26, 90), new Vector2(94, 90) })
                    {
                        Ellipse(p, o, s, c.x, c.y, 18, 6);
                        FillStroke(p);
                    }
                    break;
                default:
                    RoundRect(p, o, s, 24, 34, 72, 60, 10);
                    FillStroke(p);
                    p.lineWidth = 4f * s;
                    Line(p, o, s, 24, 50, 96, 50);
                    Line(p, o, s, 60, 34, 60, 50);
                    break;
            }
        }

        private static Vector2 Bez(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
        }
    }

    /// <summary>Kerzen-Chart (TradingViech) aus einer Kursreihe, in Kerzen zusammengefasst.</summary>
    public sealed class CandleChart : VisualElement
    {
        private struct Candle
        {
            public float O, H, L, C;
        }

        private readonly List<Candle> _c = new List<Candle>();
        private float _min, _max;

        public CandleChart()
        {
            AddToClassList("tv-candles");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        /// <summary>values = Kursverlauf, count = Anzahl Kerzen (Zeitraum).</summary>
        public void Set(IList<float> values, int count)
        {
            _c.Clear();
            if (values == null || values.Count < 2)
            {
                MarkDirtyRepaint();
                return;
            }
            count = Mathf.Clamp(count, 4, 120);
            int per = Mathf.Max(1, Mathf.CeilToInt(values.Count / (float)count));
            for (int i = 0; i < values.Count; i += per)
            {
                int end = Mathf.Min(values.Count, i + per + 1);
                var k = new Candle { O = values[i], C = values[end - 1], H = float.MinValue, L = float.MaxValue };
                for (int j = i; j < end; j++)
                {
                    k.H = Mathf.Max(k.H, values[j]);
                    k.L = Mathf.Min(k.L, values[j]);
                }
                _c.Add(k);
            }
            _min = float.MaxValue;
            _max = float.MinValue;
            foreach (var k in _c)
            {
                _min = Mathf.Min(_min, k.L);
                _max = Mathf.Max(_max, k.H);
            }
            if (_max - _min < 0.0001f) _max = _min + 1f;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 10f || r.height < 10f) return;
            var p = ctx.painter2D;
            if (p == null) return;
            p.strokeColor = new Color(0.165f, 0.18f, 0.224f);
            p.lineWidth = 1f;
            for (int i = 1; i <= 6; i++)
            {
                float y = r.height * i / 7f;
                p.BeginPath();
                p.MoveTo(new Vector2(0, y));
                p.LineTo(new Vector2(r.width, y));
                p.Stroke();
            }
            if (_c.Count == 0) return;
            float pad = 18f;
            float bw = r.width / _c.Count;
            float Y(float v) => r.height - pad - (v - _min) / (_max - _min) * (r.height - pad * 2f);
            var up = new Color(0.149f, 0.651f, 0.604f);
            var dn = new Color(0.937f, 0.325f, 0.314f);
            for (int i = 0; i < _c.Count; i++)
            {
                var k = _c[i];
                bool isUp = k.C >= k.O;
                var col = isUp ? up : dn;
                float x = i * bw + bw / 2f;
                p.strokeColor = col;
                p.lineWidth = 2f;
                p.BeginPath();
                p.MoveTo(new Vector2(x, Y(k.H)));
                p.LineTo(new Vector2(x, Y(k.L)));
                p.Stroke();
                float top = Y(Mathf.Max(k.O, k.C));
                float h = Mathf.Max(2f, Mathf.Abs(Y(k.O) - Y(k.C)));
                float w = Mathf.Max(2f, bw * 0.7f);
                p.fillColor = col;
                p.strokeColor = Color.black;
                p.lineWidth = 1.5f;
                p.BeginPath();
                p.MoveTo(new Vector2(x - w / 2f, top));
                p.LineTo(new Vector2(x + w / 2f, top));
                p.LineTo(new Vector2(x + w / 2f, top + h));
                p.LineTo(new Vector2(x - w / 2f, top + h));
                p.ClosePath();
                p.Fill();
                p.Stroke();
            }
            // letzter Kurs als Linie
            float ly = Y(_c[_c.Count - 1].C);
            p.strokeColor = new Color(0.16f, 0.38f, 1f, 0.8f);
            p.lineWidth = 1.5f;
            for (float x = 0; x < r.width; x += 10f)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(x, ly));
                p.LineTo(new Vector2(Mathf.Min(r.width, x + 5f), ly));
                p.Stroke();
            }
        }
    }

    /// <summary>Ringdiagramm (Revoluut-Ausgaben).</summary>
    public sealed class Donut : VisualElement
    {
        private readonly List<float> _v = new List<float>();
        private readonly List<Color> _c = new List<Color>();
        public float Thickness = 16f;

        public Donut(float size)
        {
            style.width = size;
            style.height = size;
            style.flexShrink = 0;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void Add(float value, Color color)
        {
            if (value <= 0f) return;
            _v.Add(value);
            _c.Add(color);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            var p = ctx.painter2D;
            if (p == null || r.width < 8f) return;
            var c = r.center;
            float rad = Mathf.Min(r.width, r.height) / 2f - Thickness / 2f;
            p.lineWidth = Thickness;
            p.lineCap = UnityEngine.UIElements.LineCap.Butt;
            p.strokeColor = new Color(0.925f, 0.933f, 0.96f);
            p.BeginPath();
            p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Stroke();
            float total = 0f;
            foreach (var v in _v) total += v;
            if (total <= 0f) return;
            float a = -90f;
            for (int i = 0; i < _v.Count; i++)
            {
                float sweep = _v[i] / total * 360f;
                p.strokeColor = _c[i];
                p.BeginPath();
                p.Arc(c, rad, Angle.Degrees(a), Angle.Degrees(a + Mathf.Max(0.5f, sweep - 1.5f)));
                p.Stroke();
                a += sweep;
            }
        }
    }

    /// <summary>Strichcode für Paketlabels (deterministisch aus einem Text).</summary>
    public sealed class Barcode : VisualElement
    {
        private readonly int _seed;

        public Barcode(string text)
        {
            _seed = W.Hash(text);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            var p = ctx.painter2D;
            if (p == null || r.width < 8f) return;
            var rng = new System.Random(_seed);
            p.fillColor = Color.black;
            float x = 0f;
            while (x < r.width - 2f)
            {
                float w = 1f + rng.Next(0, 3);
                p.BeginPath();
                p.MoveTo(new Vector2(x, 0));
                p.LineTo(new Vector2(x + w, 0));
                p.LineTo(new Vector2(x + w, r.height));
                p.LineTo(new Vector2(x, r.height));
                p.ClosePath();
                p.Fill();
                x += w + 1f + rng.Next(0, 3);
            }
        }
    }

    /// <summary>Punktmuster-Fläche (Comic-Raster) als Hintergrund-Ebene.</summary>
    public sealed class DotLayer : VisualElement
    {
        private readonly Color _c;
        private readonly float _step, _r;

        public DotLayer(Color c, float step = 22f, float radius = 3f)
        {
            _c = c;
            _step = step;
            _r = radius;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            var p = ctx.painter2D;
            if (p == null || r.width < 4f) return;
            p.fillColor = _c;
            for (float y = _step / 2f; y < r.height; y += _step)
            for (float x = _step / 2f; x < r.width; x += _step)
            {
                p.BeginPath();
                p.Arc(new Vector2(x, y), _r, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Fill();
            }
        }
    }

    /// <summary>
    /// Fake-Texte für die Produktseiten: keyword-gestopfte Titel, Beschreibungen, Varianten,
    /// Bewertungen. Reine Deko – Preise, Lager und Lieferanten kommen aus dem Sim.
    /// </summary>
    public sealed class ProductFlavor
    {
        public string Title, Desc, Store, Cat;
        public string[] Variants;
        public string[][] Reviews;
        public float Rating;
        public int ReviewCount, Sold;

        private static readonly Dictionary<string, ProductFlavor> All = new Dictionary<string, ProductFlavor>
        {
            {
                "huelle", new ProductFlavor
                {
                    Title = "2026 NEU Luxus Marmor Handyhülle Stoßfest Silikon für iPhone Samsung Xiaomi Alle Modelle Frau Mann Geschenk Top",
                    Desc = "Hochwertige Premium-Marmor-Optik aus echtem Kunststoff. Schützt Ihr Telefon vor Sturz aus bis zu 2 cm Höhe. Passt auf fast alle Modelle (bitte Größe vorher messen). Farbe kann abweichen durch Bildschirm, Licht und Laune.",
                    Store = "Shenzhen Happy Case Store", Cat = "Handy", Variants = new[] { "Marmor weiß", "Marmor schwarz", "Marmor „Gold“" },
                    Rating = 4.3f, ReviewCount = 3412, Sold = 12000,
                    Reviews = new[]
                    {
                        new[] { "Mehmet", "5", "Passt perfekt auf Samsung. Ist aber iPhone-Hülle. Trotzdem 5 Sterne weil schnell." },
                        new[] { "Sabine T.", "3", "Marmor sieht aus wie Marmor auf Foto von Marmor. Okay für Preis." },
                        new[] { "kevin2009", "1", "Kam nach 61 Tagen. Handy inzwischen verloren." },
                        new[] { "Anonymous", "5", "good good very good seller recommend" },
                    },
                }
            },
            {
                "led", new ProductFlavor
                {
                    Title = "LED Lichterkette 5m 10m 20m RGB Bluetooth App Musik Sync Gaming Zimmer TikTok Licht Deko Wasserdicht (nicht wasserdicht)",
                    Desc = "Verwandelt jedes Zimmer in ein Gaming-Paradies oder eine Zahnarztpraxis. 16 Millionen Farben (davon 7 unterschiedlich). Steuerung per App „LEDlight Pro Max“ (braucht Standort, Kontakte und Kamera).",
                    Store = "Guangzhou Bright Future Co.", Cat = "Deko", Variants = new[] { "5 m", "10 m", "20 m (klebt nicht)" },
                    Rating = 4.1f, ReviewCount = 9120, Sold = 48000,
                    Reviews = new[]
                    {
                        new[] { "Jonas", "5", "Mein Zimmer sieht jetzt aus wie ein Aquarium. Freundin weg. 5 Sterne." },
                        new[] { "Petra W.", "2", "Kleber hält genau eine Nacht. Morgens alles auf dem Boden." },
                        new[] { "Leon", "4", "Musik-Sync funktioniert, aber nur bei Schlager." },
                    },
                }
            },
            {
                "massage", new ProductFlavor
                {
                    Title = "Massagepistole Profi Tiefengewebe 30 Stufen Muskel Faszien Gun Sport Fitness Rücken Nacken Leise Akku Geschenk Mann",
                    Desc = "Löst Verspannungen, Knoten und gelegentlich Zahnfüllungen. 30 Stufen: von „Streicheln“ bis „Presslufthammer“. Leise wie ein Rasenmäher mit Schalldämpfer.",
                    Store = "PowerRelax Official Store", Cat = "Gesund", Variants = new[] { "Schwarz", "Titan-Optik", "Rosa (stärker)" },
                    Rating = 4.4f, ReviewCount = 2890, Sold = 9100,
                    Reviews = new[]
                    {
                        new[] { "Sven", "5", "Nacken wieder locker. Nachbarn nicht mehr." },
                        new[] { "Uschi", "4", "Stufe 30 hat meinen Mann einmal durchs Wohnzimmer geschoben." },
                        new[] { "fitboy_ruhr", "2", "Akku hält 4 Minuten. Mein Training auch." },
                    },
                }
            },
            {
                "kopfhoerer", new ProductFlavor
                {
                    Title = "Bluetooth Kopfhörer In-Ear Kabellos 5.3 HiFi Bass Noise Cancelling Sport Wasserdicht Mikrofon für iPhone Android Pro Max",
                    Desc = "Kristallklarer Klang* und tiefer Bass**. *im Vergleich zu keinem Kopfhörer. **laut Verpackung. Noise Cancelling blendet alles aus, außer Rauschen.",
                    Store = "SoundWave Global Store", Cat = "Handy", Variants = new[] { "Weiß", "Schwarz", "Links (einzeln)" },
                    Rating = 4.0f, ReviewCount = 7450, Sold = 31000,
                    Reviews = new[]
                    {
                        new[] { "Laura", "4", "Klang gut. Einer verbindet sich immer mit dem Fernseher vom Nachbarn." },
                        new[] { "Timo K.", "5", "Sehen aus wie die teuren. Hören sich an wie die günstigen. Fair." },
                        new[] { "Oma Gerda", "3", "Wo steckt man das Kabel rein?" },
                    },
                }
            },
            {
                "ringlicht", new ProductFlavor
                {
                    Title = "Ringlicht 26cm mit Stativ Handyhalter Selfie TikTok Influencer Make Up Video LED Dimmbar Streaming Zoom Beauty",
                    Desc = "Werde in 24 Stunden Influencer. Ergebnis nicht garantiert. Stativ bis 1,60 m (wackelt ab 1,20 m).",
                    Store = "Star Creator Official", Cat = "Influencer", Variants = new[] { "26 cm", "33 cm", "46 cm" },
                    Rating = 4.2f, ReviewCount = 6100, Sold = 31000,
                    Reviews = new[]
                    {
                        new[] { "Chantal", "5", "Sehe aus wie ein Engel, Zimmer sieht aus wie Verhörraum." },
                        new[] { "Marco", "3", "Stativ kippt, Ring hält." },
                    },
                }
            },
            {
                "katzenbrunnen", new ProductFlavor
                {
                    Title = "Katzen Trinkbrunnen 2,5L Leise Automatisch Filter LED Hund Haustier Wasserspender Smart App Edelstahl Optik",
                    Desc = "Fließendes Wasser regt Ihre Katze zum Trinken an. Wissenschaftlich getestet an einer Katze. Flüsterleise (38 dB, entspricht einem kleinen Rasenmäher im Nebenzimmer).",
                    Store = "PetLove Global Store", Cat = "Haustier", Variants = new[] { "Weiß", "Blau", "Rosa" },
                    Rating = 3.9f, ReviewCount = 1880, Sold = 7800,
                    Reviews = new[]
                    {
                        new[] { "Dr. Lehmann", "1", "Katze trinkt weiterhin aus der Toilette." },
                        new[] { "Mia", "5", "Meine Katze liebt es. Spielt damit. Trinkt nicht." },
                        new[] { "Tobi", "3", "Leise ist relativ." },
                    },
                }
            },
            {
                "haltung", new ProductFlavor
                {
                    Title = "Haltungskorrektur Rücken Geradehalter Schulter Gurt Büro Homeoffice Unsichtbar Unter Kleidung Damen Herren Verstellbar",
                    Desc = "Richtet Ihre Haltung in nur 3 Tagen auf, oder Ihre Laune ab. Unsichtbar unter der Kleidung, außer man sieht es.",
                    Store = "Healthy Body Store", Cat = "Gesund", Variants = new[] { "S", "M", "L", "XL (wie M)" },
                    Rating = 4.0f, ReviewCount = 4020, Sold = 15000,
                    Reviews = new[]
                    {
                        new[] { "Kevin P.", "4", "Sitze gerader. Atme weniger." },
                        new[] { "Nina", "2", "Größe L ist Kinder-L." },
                    },
                }
            },
            {
                "smartwatch", new ProductFlavor
                {
                    Title = "Smartwatch Herren Damen 2026 Fitness Tracker Anrufe Puls Blutdruck Schlaf Schritte Wasserdicht IP68 für Android iOS Ultra",
                    Desc = "Misst Puls, Schritte, Schlaf und Blutdruck (ungefähr, ungefähr, ungefähr und gar nicht). Akkulaufzeit: 7 Tage im Flugmodus in der Schublade.",
                    Store = "Smart Life Tech Store", Cat = "Handy", Variants = new[] { "Schwarz", "Silber", "Rosé" },
                    Rating = 4.1f, ReviewCount = 5230, Sold = 18000,
                    Reviews = new[]
                    {
                        new[] { "Dieter S.", "5", "Sagt mir jede Stunde, ich soll aufstehen. Wie meine Frau." },
                        new[] { "Jana", "3", "Hab laut Uhr im Schlaf 12.000 Schritte gemacht. Beunruhigend." },
                        new[] { "Anonymous", "4", "very smart. more smart than me" },
                    },
                }
            },
            {
                "beamer", new ProductFlavor
                {
                    Title = "Mini Beamer 4K Full HD 1080P Tragbar Heimkino Projektor WLAN Handy 9500 Lumen (ca.) Outdoor Kino Netflix",
                    Desc = "Echtes 4K* Kinoerlebnis zu Hause. *Unterstützt 4K-Dateien, zeigt sie in 480p. Helligkeit: sehr hell bei komplett dunklem Raum mit geschlossenen Augen.",
                    Store = "Mega Vision Official Store", Cat = "Heimkino", Variants = new[] { "Weiß", "Schwarz" },
                    Rating = 3.8f, ReviewCount = 1204, Sold = 3100,
                    Reviews = new[]
                    {
                        new[] { "Frank", "4", "Für den Preis okay. Film nur im Keller schaubar." },
                        new[] { "Laura", "1", "9500 Lumen? Meine Kerze ist heller." },
                        new[] { "Dieter S.", "5", "Enkel hat eingestellt, läuft." },
                    },
                }
            },
            {
                "drohne", new ProductFlavor
                {
                    Title = "Kamera Drohne 4K GPS Faltbar Profi Anfänger Kinder Quadrocopter 60 Min Flugzeit (Summe aller Akkus) Follow Me",
                    Desc = "Filmt atemberaubende Luftaufnahmen – bevorzugt von Baumkronen, in denen sie hängen bleibt. „Follow Me“-Modus folgt Ihnen, Ihrem Hund oder einer Taube.",
                    Store = "SkyHigh Toys Factory", Cat = "Technik", Variants = new[] { "1 Akku", "3 Akkus", "Mit Ersatzpropellern (dringend)" },
                    Rating = 3.7f, ReviewCount = 980, Sold = 2400,
                    Reviews = new[]
                    {
                        new[] { "Paul", "4", "Tolle Bilder von unserem Dach. Drohne wohnt jetzt dort." },
                        new[] { "Svenja", "2", "Follow-Me folgt lieber dem Nachbarn." },
                        new[] { "Rüdiger", "5", "Der Hund hat Angst, ich habe Spaß." },
                    },
                }
            },
        };

        public static ProductFlavor Get(string id)
        {
            if (id != null && All.TryGetValue(id, out var f)) return f;
            string name = GameData.IsProduct(id) ? GameData.Product(id).Name : "Produkt";
            return new ProductFlavor
            {
                Title = "2026 NEU " + name + " Premium Top Qualität Geschenk Alle Modelle",
                Desc = "Ein " + name + ". Wie auf dem Bild. Ungefähr.",
                Store = "Global Everything Store", Cat = "Sachen", Variants = new[] { "Standard" }, Rating = 4f, ReviewCount = 500, Sold = 1000,
                Reviews = new[] { new[] { "Anonymous", "4", "ist angekommen" } },
            };
        }

        /// <summary>Verteilung 5..1 Sterne (in %) passend zur Durchschnittsnote.</summary>
        public static int[] Histogram(float rating)
        {
            float t = Mathf.InverseLerp(3f, 5f, rating);
            int five = Mathf.RoundToInt(Mathf.Lerp(30f, 80f, t));
            int four = Mathf.RoundToInt(Mathf.Lerp(24f, 12f, t));
            int one = Mathf.RoundToInt(Mathf.Lerp(18f, 2f, t));
            int two = Mathf.RoundToInt(Mathf.Lerp(12f, 2f, t));
            int three = Mathf.Max(0, 100 - five - four - two - one);
            return new[] { five, four, three, two, one };
        }
    }
}
