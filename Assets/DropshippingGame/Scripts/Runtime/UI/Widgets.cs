using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Zeichenhilfen für eigene UI-Grafik (Dreiecke im Uhrzeigersinn).</summary>
    public static class UIDraw
    {
        public sealed class MeshBuilder
        {
            public readonly List<Vertex> V = new List<Vertex>();
            public readonly List<ushort> I = new List<ushort>();

            public void Tri(Vector2 a, Vector2 b, Vector2 c, Color col)
            {
                // UI-Koordinaten: y zeigt nach unten -> Kreuzprodukt > 0 = im Uhrzeigersinn
                float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                if (Mathf.Abs(cross) < 1e-5f) return;
                if (cross < 0f)
                {
                    var t = b;
                    b = c;
                    c = t;
                }
                if (V.Count > 65000) return;
                ushort i = (ushort)V.Count;
                V.Add(new Vertex { position = new Vector3(a.x, a.y, Vertex.nearZ), tint = col });
                V.Add(new Vertex { position = new Vector3(b.x, b.y, Vertex.nearZ), tint = col });
                V.Add(new Vertex { position = new Vector3(c.x, c.y, Vertex.nearZ), tint = col });
                I.Add(i);
                I.Add((ushort)(i + 1));
                I.Add((ushort)(i + 2));
            }

            public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
            {
                Tri(a, b, c, col);
                Tri(a, c, d, col);
            }

            public void Rect(Rect r, Color col) =>
                Quad(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), col);

            public void Line(Vector2 a, Vector2 b, float width, Color col)
            {
                var d = (b - a);
                if (d.sqrMagnitude < 1e-6f) return;
                var n = new Vector2(-d.y, d.x).normalized * (width / 2f);
                Quad(a + n, b + n, b - n, a - n, col);
            }

            public void Polyline(IList<Vector2> pts, float width, Color col)
            {
                for (int i = 0; i + 1 < pts.Count; i++) Line(pts[i], pts[i + 1], width, col);
                for (int i = 1; i + 1 < pts.Count; i++) Circle(pts[i], width / 2f, col, 6);
            }

            /// <summary>Konvexes (oder sternförmiges) Polygon als Fächer um den Mittelpunkt.</summary>
            public void Fan(IList<Vector2> pts, Color col, Vector2? center = null)
            {
                Vector2 c = Vector2.zero;
                if (center.HasValue) c = center.Value;
                else
                {
                    foreach (var p in pts) c += p;
                    c /= pts.Count;
                }
                for (int i = 0; i < pts.Count; i++) Tri(c, pts[i], pts[(i + 1) % pts.Count], col);
            }

            public void Circle(Vector2 c, float r, Color col, int segments = 24)
            {
                for (int i = 0; i < segments; i++)
                {
                    float a0 = i / (float)segments * Mathf.PI * 2f, a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                    Tri(c, c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r, col);
                }
            }

            public void Flush(MeshGenerationContext ctx)
            {
                if (V.Count == 0) return;
                var mesh = ctx.Allocate(V.Count, I.Count);
                mesh.SetAllVertices(V.ToArray());
                mesh.SetAllIndices(I.ToArray());
            }
        }

        /// <summary>Markenlogo (6 Formen) um den Mittelpunkt c mit Radius r.</summary>
        public static void Logo(MeshBuilder m, int index, Vector2 c, float r, Color col)
        {
            switch (index)
            {
                case 0:
                    m.Circle(c, r, col, 28);
                    break;
                case 1:
                    m.Rect(new Rect(c.x - r * 0.85f, c.y - r * 0.85f, r * 1.7f, r * 1.7f), col);
                    break;
                case 2:
                    m.Quad(c + new Vector2(0, -r), c + new Vector2(r, 0), c + new Vector2(0, r), c + new Vector2(-r, 0), col);
                    break;
                case 3:
                {
                    var pts = new List<Vector2>();
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float rr = i % 2 == 0 ? r : r * 0.45f;
                        pts.Add(c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rr);
                    }
                    m.Fan(pts, col, c);
                    break;
                }
                case 4:
                {
                    Vector2 P(float x, float y) => c + new Vector2(x, y) * r;
                    m.Tri(P(0.2f, -1f), P(-0.55f, 0.15f), P(0.05f, -0.15f), col);
                    m.Tri(P(0.05f, -0.15f), P(-0.55f, 0.15f), P(-0.05f, 0.15f), col);
                    m.Tri(P(-0.05f, 0.15f), P(-0.2f, 1f), P(0.55f, -0.15f), col);
                    m.Tri(P(-0.05f, 0.15f), P(0.55f, -0.15f), P(0.05f, -0.15f), col);
                    break;
                }
                default:
                    m.Circle(c + new Vector2(-0.45f, -0.25f) * r, r * 0.5f, col);
                    m.Circle(c + new Vector2(0.45f, -0.25f) * r, r * 0.5f, col);
                    m.Tri(c + new Vector2(-0.93f, -0.05f) * r, c + new Vector2(0.93f, -0.05f) * r, c + new Vector2(0, 0.95f) * r, col);
                    break;
            }
        }
    }

    /// <summary>Liniendiagramm mit Gitter, Füllung unter der ersten Linie und Endwert-Beschriftung.</summary>
    public sealed class LineChart : VisualElement
    {
        public sealed class Series
        {
            public List<float> Values = new List<float>();
            public Color Color;
            public string Label;
        }

        public List<Series> Data = new List<Series>();
        public string Suffix = "";
        public bool IncludeZero;
        public int Decimals;
        private readonly List<Label> _labels = new List<Label>();

        public LineChart()
        {
            AddToClassList("chart");
            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => Relabel());
        }

        public void Set(params Series[] series)
        {
            Data = new List<Series>(series);
            MarkDirtyRepaint();
            Relabel();
        }

        private string F(float v) => (Decimals > 0 ? Fmt.Dec(v, Decimals) : Mathf.RoundToInt(v).ToString()) + Suffix;

        private bool Range(out float lo, out float hi, out int count)
        {
            lo = float.MaxValue;
            hi = float.MinValue;
            count = 0;
            foreach (var s in Data)
            {
                count = Mathf.Max(count, s.Values.Count);
                foreach (var v in s.Values)
                {
                    lo = Mathf.Min(lo, v);
                    hi = Mathf.Max(hi, v);
                }
            }
            if (count < 2) return false;
            if (IncludeZero)
            {
                lo = Mathf.Min(lo, 0f);
                hi = Mathf.Max(hi, 0f);
            }
            if (Mathf.Abs(hi - lo) < 0.0001f)
            {
                hi += 1f;
                lo -= 1f;
            }
            float span = hi - lo;
            lo -= span * 0.06f;
            hi += span * 0.06f;
            return true;
        }

        private Rect Area()
        {
            var r = contentRect;
            return new Rect(52f, 10f, Mathf.Max(10f, r.width - 52f - 58f), Mathf.Max(10f, r.height - 10f - 22f));
        }

        private void Relabel()
        {
            foreach (var l in _labels) l.RemoveFromHierarchy();
            _labels.Clear();
            if (contentRect.width < 20f) return;
            var area = Area();
            var muted = Theme.LaptopMuted;
            if (!Range(out float lo, out float hi, out int count))
            {
                AddLabel("Noch keine Daten", area.x, area.y + area.height / 2f - 8f, muted);
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                float y = area.y + area.height * i / 4f;
                float val = hi - (hi - lo) * i / 4f;
                AddLabel(F(val), 2f, y - 7f, muted, 48f);
            }
            int si = 0;
            foreach (var s in Data)
            {
                if (s.Values.Count >= 2)
                {
                    float last = s.Values[s.Values.Count - 1];
                    float y = area.y + area.height * (1f - (last - lo) / (hi - lo));
                    AddLabel(F(last), area.xMax + 8f, y - 8f, s.Color);
                }
                if (!string.IsNullOrEmpty(s.Label)) AddLabel(s.Label, area.x + 22f + si * 140f, area.yMax + 4f, s.Color);
                si++;
            }
        }

        private void AddLabel(string text, float x, float y, Color c, float width = -1f)
        {
            var l = new Label(text);
            l.RemoveFromClassList("unity-label");
            l.AddToClassList("chart-label");
            l.style.left = x;
            l.style.top = y;
            if (width > 0) l.style.width = width;
            l.style.color = c;
            l.pickingMode = PickingMode.Ignore;
            Fonts.Auto(l);
            Add(l);
            _labels.Add(l);
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var m = new UIDraw.MeshBuilder();
            var area = Area();
            var grid = Settings.LaptopDark ? new Color(1f, 1f, 1f, 0.07f) : new Color(0f, 0f, 0f, 0.08f);
            for (int i = 0; i < 5; i++)
            {
                float y = area.y + area.height * i / 4f;
                m.Line(new Vector2(area.x, y), new Vector2(area.xMax, y), 1f, grid);
            }
            if (Range(out float lo, out float hi, out int _))
            {
                if (IncludeZero && lo < 0f && hi > 0f)
                {
                    float zy = area.y + area.height * (hi / (hi - lo));
                    m.Line(new Vector2(area.x, zy), new Vector2(area.xMax, zy), 1.2f, grid * 2.5f);
                }
                for (int si = 0; si < Data.Count; si++)
                {
                    var s = Data[si];
                    if (s.Values.Count < 2) continue;
                    var pts = new List<Vector2>();
                    for (int i = 0; i < s.Values.Count; i++)
                    {
                        float x = area.x + area.width * i / (s.Values.Count - 1);
                        float y = area.y + area.height * (1f - (s.Values[i] - lo) / (hi - lo));
                        pts.Add(new Vector2(x, y));
                    }
                    if (si == 0)
                    {
                        var fill = new Color(s.Color.r, s.Color.g, s.Color.b, 0.14f);
                        for (int i = 0; i + 1 < pts.Count; i++)
                            m.Quad(pts[i], pts[i + 1], new Vector2(pts[i + 1].x, area.yMax), new Vector2(pts[i].x, area.yMax), fill);
                    }
                    m.Polyline(pts, 2.4f, s.Color);
                    m.Circle(pts[pts.Count - 1], 4.5f, s.Color, 16);
                }
                for (int si = 0; si < Data.Count; si++)
                    if (!string.IsNullOrEmpty(Data[si].Label))
                        m.Circle(new Vector2(area.x + 14f + si * 140f, area.yMax + 11f), 4f, Data[si].Color, 12);
            }
            m.Flush(ctx);
        }
    }

    /// <summary>Vorschau des bedruckten Versandkartons mit Logo, Farbe und Markenname.</summary>
    public sealed class BrandPreview : VisualElement
    {
        private int _logo;
        private Color _color;
        private bool _small;
        private readonly Label _name, _claim;

        public BrandPreview(int logo, Color color, string brand, bool small = false)
        {
            _logo = logo;
            _color = color;
            _small = small;
            style.width = small ? 46 : 300;
            style.height = small ? 46 : 220;
            style.flexShrink = 0;
            generateVisualContent += Draw;
            if (!small)
            {
                _name = new Label(brand);
                _name.RemoveFromClassList("unity-label");
                _name.style.position = Position.Absolute;
                _name.style.left = 142;
                _name.style.top = 88;
                _name.style.width = 124;
                _name.style.fontSize = 19;
                _name.style.unityFontStyleAndWeight = FontStyle.Bold;
                _name.style.color = new Color(0.12f, 0.1f, 0.08f);
                Add(_name);
                _claim = new Label("Mit Liebe verpackt");
                _claim.RemoveFromClassList("unity-label");
                _claim.style.position = Position.Absolute;
                _claim.style.left = 142;
                _claim.style.top = 114;
                _claim.style.fontSize = 11;
                _claim.style.color = new Color(0.2f, 0.16f, 0.12f);
                Add(_claim);
            }
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var m = new UIDraw.MeshBuilder();
            if (_small)
            {
                var r = contentRect;
                UIDraw.Logo(m, _logo, r.center, Mathf.Min(r.width, r.height) * 0.36f, _color);
                m.Flush(ctx);
                return;
            }
            var box = new Rect(30, 40, 240, 160);
            var tl = new Vector2(box.xMin, box.yMin);
            m.Quad(tl, tl + new Vector2(40, -30), tl + new Vector2(box.width + 40, -30), tl + new Vector2(box.width, 0), new Color(0.8f, 0.64f, 0.44f));
            m.Quad(tl + new Vector2(box.width, 0), tl + new Vector2(box.width + 40, -30), new Vector2(box.xMax + 40, box.yMax - 30), new Vector2(box.xMax, box.yMax), new Color(0.58f, 0.43f, 0.27f));
            m.Rect(box, new Color(0.7f, 0.53f, 0.34f));
            m.Rect(new Rect(box.xMin, box.yMax - 30, box.width, 22), _color);
            m.Rect(new Rect(box.xMin + box.width * 0.44f, box.yMin, box.width * 0.12f, box.height), new Color(0.85f, 0.75f, 0.52f, 0.8f));
            UIDraw.Logo(m, _logo, tl + new Vector2(64, 62), 32f, _color);
            m.Flush(ctx);
        }
    }

    /// <summary>
    /// TikTok-Minispiel als gezeichnetes Handy: Aufnahme starten, Markierung pendelt über den
    /// Balken, im richtigen Moment stoppen. Die Trefferquote bestimmt Stärke und Dauer des Trends.
    /// </summary>
    public sealed class TikTokPhone : VisualElement
    {
        private static readonly string[] Ideas =
        {
            "POV: Du packst um 3 Uhr nachts Pakete", "UNBOXING GONE WRONG", "Life-Hack: Das hier braucht JEDER",
            "Mein Chef (ich) gibt mir frei", "Wie ich mit 19 ... okay, noch nicht", "Kunde bestellt 1x – ich liefere mit Liebe",
        };

        public event Action<float> Posted;
        private string _state = "idle";
        private float _marker, _dir = 1f, _speed = 0.9f, _zoneA = 0.4f, _zoneB = 0.6f, _t;
        public float Score { get; private set; }
        private readonly VisualElement _bar, _zone, _mark, _rec;
        private readonly Label _status, _result;
        private readonly Button _button;

        public bool Busy => _state == "rec" || _state == "done";

        public TikTokPhone()
        {
            AddToClassList("tiktok-phone");
            var screen = UIX.Col(this, 10f, "tiktok-screen");
            var top = UIX.Row(screen, 8f);
            UIX.Icon(top, "music", 18f, Color.white);
            UIX.Colored(top, "TikTok", Color.white, "h3");
            UIX.Spacer(top);
            _rec = UIX.Row(top, 4f);
            UIX.Dot(_rec, new Color(1f, 0.3f, 0.35f), 8f);
            UIX.Colored(_rec, "REC", new Color(1f, 0.3f, 0.35f), "small", "bold");
            UIX.Show(_rec, false);
            UIX.Colored(screen, Ideas[UnityEngine.Random.Range(0, Ideas.Length)], new Color(1f, 1f, 1f, 0.9f), "h3").style.marginTop = 16;
            UIX.Spacer(screen);
            _status = UIX.Colored(screen, "Stopp im grünen Bereich!", new Color(1f, 1f, 1f, 0.85f), "small");
            _result = UIX.Colored(screen, "", new Color(1f, 0.74f, 0.26f), "h2");
            _bar = UIX.Div(screen);
            _bar.style.height = 26;
            _bar.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
            _bar.style.borderTopLeftRadius = 6;
            _bar.style.borderTopRightRadius = 6;
            _bar.style.borderBottomLeftRadius = 6;
            _bar.style.borderBottomRightRadius = 6;
            _zone = new VisualElement();
            _zone.style.position = Position.Absolute;
            _zone.style.top = 0;
            _zone.style.bottom = 0;
            _zone.style.backgroundColor = new Color(0.3f, 0.95f, 0.6f, 0.55f);
            _bar.Add(_zone);
            _mark = new VisualElement();
            _mark.style.position = Position.Absolute;
            _mark.style.top = -6;
            _mark.style.bottom = -6;
            _mark.style.width = 6;
            _mark.style.marginLeft = -3;
            _mark.style.backgroundColor = Color.white;
            _bar.Add(_mark);
            _button = UIX.Button(screen, "Aufnahme starten", OnButton, "accent", false, "dot");
            _button.style.marginTop = 12;
            UpdateZone();
            schedule.Execute(Step).Every(16);
        }

        private void UpdateZone()
        {
            _zone.style.left = Length.Percent(_zoneA * 100f);
            _zone.style.width = Length.Percent((_zoneB - _zoneA) * 100f);
            _mark.style.left = Length.Percent(_marker * 100f);
        }

        private void OnButton()
        {
            switch (_state)
            {
                case "idle":
                    _state = "rec";
                    _marker = 0f;
                    _dir = 1f;
                    _speed = UnityEngine.Random.Range(0.8f, 1.15f);
                    _zoneA = UnityEngine.Random.Range(0.2f, 0.62f);
                    _zoneB = _zoneA + UnityEngine.Random.Range(0.14f, 0.2f);
                    UIX.SetButtonText(_button, "STOPP!");
                    UIX.Show(_rec, true);
                    _status.text = "Jetzt! ... oder jetzt?";
                    break;
                case "rec":
                {
                    _state = "done";
                    float mid = (_zoneA + _zoneB) / 2f, half = (_zoneB - _zoneA) / 2f;
                    float dist = Mathf.Abs(_marker - mid);
                    Score = dist <= half * 0.35f ? 1f : Mathf.Clamp(1f - (dist - half * 0.35f) / (half + 0.28f), 0.05f, 1f);
                    int views = Mathf.RoundToInt(Mathf.Pow(Score, 2f) * 1800000f + 1200f);
                    _result.text = "Treffer: " + Mathf.RoundToInt(Score * 100) + " %";
                    _status.text = "Prognose: " + Fmt.Thousands(views) + " Views";
                    UIX.SetButtonText(_button, "Posten");
                    UIX.Show(_rec, false);
                    Game.Sound(Score > 0.6f ? "notify" : "click");
                    break;
                }
                case "done":
                    _state = "posted";
                    _status.text = "Gepostet!";
                    _button.SetEnabled(false);
                    Game.Sound("levelup", 0f, -6f);
                    Posted?.Invoke(Score);
                    break;
            }
            UpdateZone();
        }

        private void Step()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            if (_state != "rec") return;
            _marker += _dir * _speed * dt;
            _speed += dt * 0.08f;
            if (_marker >= 1f)
            {
                _marker = 1f;
                _dir = -1f;
            }
            else if (_marker <= 0f)
            {
                _marker = 0f;
                _dir = 1f;
            }
            _rec.style.opacity = 0.5f + 0.5f * Mathf.Sin(_t * 8f);
            UpdateZone();
        }
    }

    /// <summary>Per Code erzeugte Hintergrund-Texturen (Glühen, Verläufe). Werden zwischengespeichert.</summary>
    public static class UiTex
    {
        private static Texture2D _radial, _shadeLeft, _shadeRight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _radial = null;
            _shadeLeft = null;
            _shadeRight = null;
        }

        /// <summary>Weißer, weicher Kreis (Alpha fällt nach außen ab) – per Tönung einfärben.</summary>
        public static Texture2D Radial()
        {
            if (_radial != null) return _radial;
            const int n = 128;
            _radial = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "ui_radial" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            _radial.SetPixels32(px);
            _radial.Apply(false, true);
            return _radial;
        }

        /// <summary>Dunkler Verlauf für die Menü-Kulisse (links deckend → rechts transparent, oder umgekehrt).</summary>
        public static Texture2D Shade(bool fromLeft)
        {
            if (fromLeft && _shadeLeft != null) return _shadeLeft;
            if (!fromLeft && _shadeRight != null) return _shadeRight;
            const int w = 256;
            var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = fromLeft ? "ui_shade_l" : "ui_shade_r" };
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                if (!fromLeft) t = 1f - t;
                float a = Mathf.Lerp(fromLeft ? 0.95f : 0.8f, 0f, Mathf.SmoothStep(0f, 1f, t));
                tex.SetPixel(x, 0, new Color(0.02f, 0.03f, 0.05f, a));
            }
            tex.Apply(false, true);
            if (fromLeft) _shadeLeft = tex;
            else _shadeRight = tex;
            return tex;
        }
    }

    /// <summary>Kleiner Verlauf ohne Achsen (Übersicht, Handy): Fläche + Linie + Endpunkt.</summary>
    public sealed class Sparkline : VisualElement
    {
        private readonly List<float> _values = new List<float>();
        private Color _color;

        public Sparkline(IList<float> values, Color color)
        {
            AddToClassList("spark");
            pickingMode = PickingMode.Ignore;
            _color = color;
            if (values != null) _values.AddRange(values);
            generateVisualContent += Draw;
        }

        public void Set(IList<float> values, Color color)
        {
            _values.Clear();
            if (values != null) _values.AddRange(values);
            _color = color;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (_values.Count < 2 || r.width < 8f || r.height < 8f) return;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var v in _values)
            {
                lo = Mathf.Min(lo, v);
                hi = Mathf.Max(hi, v);
            }
            lo = Mathf.Min(lo, 0f);
            if (hi - lo < 0.001f) hi = lo + 1f;
            var m = new UIDraw.MeshBuilder();
            var pts = new List<Vector2>();
            for (int i = 0; i < _values.Count; i++)
            {
                float x = 4f + (r.width - 8f) * i / (_values.Count - 1);
                float y = 6f + (r.height - 12f) * (1f - (_values[i] - lo) / (hi - lo));
                pts.Add(new Vector2(x, y));
            }
            var fill = new Color(_color.r, _color.g, _color.b, 0.16f);
            for (int i = 0; i + 1 < pts.Count; i++)
                m.Quad(pts[i], pts[i + 1], new Vector2(pts[i + 1].x, r.height), new Vector2(pts[i].x, r.height), fill);
            m.Polyline(pts, 3f, _color);
            m.Circle(pts[pts.Count - 1], 5f, _color, 16);
            m.Flush(ctx);
        }
    }

    /// <summary>Gezackte Abrisskante des Kassenbons (oben oder unten).</summary>
    public sealed class ReceiptEdge : VisualElement
    {
        private readonly bool _top;
        private readonly Color _color;

        public ReceiptEdge(bool top, Color color)
        {
            _top = top;
            _color = color;
            AddToClassList("receipt-edge");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (r.width < 4f || r.height < 2f) return;
            var m = new UIDraw.MeshBuilder();
            const float tooth = 12f;
            int n = Mathf.Max(1, Mathf.CeilToInt(r.width / tooth));
            float w = r.width / n;
            for (int i = 0; i < n; i++)
            {
                float x0 = i * w, x1 = x0 + w, xm = x0 + w / 2f;
                if (_top) m.Tri(new Vector2(x0, r.height), new Vector2(xm, 0f), new Vector2(x1, r.height), _color);
                else m.Tri(new Vector2(x0, 0f), new Vector2(x1, 0f), new Vector2(xm, r.height), _color);
            }
            m.Flush(ctx);
        }
    }
}
