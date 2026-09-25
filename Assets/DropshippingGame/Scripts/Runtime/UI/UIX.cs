using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Baukasten für alle Oberflächen (UI Toolkit). Container merken sich ihren Abstand und
    /// setzen ihn automatisch zwischen neue Kinder (USS kennt kein "gap").
    /// Aussehen kommt aus den USS-Dateien in Resources/UI.
    /// </summary>
    public static class UIX
    {
        private sealed class Gap
        {
            public float Size;
            public bool Horizontal;
        }

        public static T Attach<T>(VisualElement parent, T el) where T : VisualElement
        {
            if (parent == null) return el;
            if (parent.userData is Gap g && parent.childCount > 0 && el.style.position != Position.Absolute)
            {
                if (g.Horizontal) el.style.marginLeft = g.Size;
                else el.style.marginTop = g.Size;
            }
            parent.Add(el);
            return el;
        }

        public static VisualElement Div(VisualElement parent, params string[] classes)
        {
            var el = new VisualElement();
            foreach (var c in classes) el.AddToClassList(c);
            return Attach(parent, el);
        }

        public static VisualElement Row(VisualElement parent, float gap = 10f, params string[] classes)
        {
            var el = Div(parent, classes);
            el.AddToClassList("row");
            el.userData = new Gap { Size = gap, Horizontal = true };
            return el;
        }

        public static VisualElement Col(VisualElement parent, float gap = 8f, params string[] classes)
        {
            var el = Div(parent, classes);
            el.AddToClassList("col");
            el.userData = new Gap { Size = gap, Horizontal = false };
            return el;
        }

        /// <summary>Umbrechende Zeile (Kacheln). Abstand über Außenabstände der Kinder.</summary>
        public static VisualElement Wrap(VisualElement parent, params string[] classes)
        {
            var el = Div(parent, classes);
            el.AddToClassList("wrap");
            return el;
        }

        public static Label Text(VisualElement parent, string text, params string[] classes)
        {
            var l = new Label(text ?? "");
            l.RemoveFromClassList("unity-label");
            l.enableRichText = true;
            foreach (var c in classes) l.AddToClassList(c);
            return Attach(parent, l);
        }

        public static Label Colored(VisualElement parent, string text, Color color, params string[] classes)
        {
            var l = Text(parent, text, classes);
            l.style.color = color;
            return l;
        }

        public static VisualElement Spacer(VisualElement parent)
        {
            var el = new VisualElement();
            el.style.flexGrow = 1;
            parent?.Add(el);
            return el;
        }

        public static VisualElement Icon(VisualElement parent, string name, float size = 18f, Color? tint = null, params string[] classes)
        {
            var el = new VisualElement();
            el.AddToClassList("icon");
            foreach (var c in classes) el.AddToClassList(c);
            el.style.width = size;
            el.style.height = size;
            el.style.flexShrink = 0;
            el.style.backgroundImage = new StyleBackground(Icons.Get(name));
            if (tint.HasValue) el.style.unityBackgroundImageTintColor = tint.Value;
            return Attach(parent, el);
        }

        public static void SetIcon(VisualElement el, string name) => el.style.backgroundImage = new StyleBackground(Icons.Get(name));

        /// <summary>Button ohne Unity-Standardstil. variant: "", "accent", "ghost", "danger", "soft".</summary>
        public static Button Button(VisualElement parent, string text, Action onClick, string variant = "", bool disabled = false, string icon = null)
        {
            var b = new Button();
            b.RemoveFromClassList("unity-button");
            b.RemoveFromClassList("unity-text-element");
            b.AddToClassList("btn");
            if (!string.IsNullOrEmpty(variant)) b.AddToClassList("btn-" + variant);
            b.text = "";
            if (!string.IsNullOrEmpty(icon)) Icon(b, icon, 16f, null, "btn-icon");
            if (!string.IsNullOrEmpty(text))
            {
                var l = new Label(text);
                l.RemoveFromClassList("unity-label");
                l.enableRichText = true;
                l.AddToClassList("btn-label");
                l.pickingMode = PickingMode.Ignore;
                b.Add(l);
            }
            b.clicked += () =>
            {
                if (!b.enabledSelf) return;
                Game.Sound("click", 0.05f, -4f);
                onClick?.Invoke();
            };
            b.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (b.enabledSelf) Game.Sound("hover", 0.05f, -10f);
            });
            b.SetEnabled(!disabled);
            return Attach(parent, b);
        }

        public static void SetButtonText(Button b, string text)
        {
            var l = b.Q<Label>(className: "btn-label");
            if (l != null) l.text = text;
        }

        public static VisualElement Card(VisualElement parent, string title = null, string extraClass = null)
        {
            var c = Col(parent, 8f, "card");
            if (!string.IsNullOrEmpty(extraClass)) c.AddToClassList(extraClass);
            if (!string.IsNullOrEmpty(title)) Text(c, title, "card-title");
            return c;
        }

        /// <summary>Fortschrittsbalken 0..1.</summary>
        public static VisualElement Bar(VisualElement parent, float value, Color? color = null, float height = 8f, float width = -1f)
        {
            var track = Div(parent, "bar");
            track.style.height = height;
            if (width > 0) track.style.width = width;
            else track.style.flexGrow = 1;
            var fill = new VisualElement();
            fill.AddToClassList("bar-fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
            if (color.HasValue) fill.style.backgroundColor = color.Value;
            track.Add(fill);
            return track;
        }

        public static void SetBar(VisualElement bar, float value)
        {
            if (bar == null || bar.childCount == 0) return;
            bar[0].style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
        }

        public static VisualElement Swatch(VisualElement parent, Color color, float size = 30f, string icon = null, string text = null)
        {
            var s = Div(parent, "swatch");
            s.style.width = size;
            s.style.height = size;
            s.style.backgroundColor = color;
            s.style.flexShrink = 0;
            if (!string.IsNullOrEmpty(icon)) Icon(s, icon, size * 0.56f, color.grayscale > 0.62f ? new Color(0.1f, 0.1f, 0.12f) : Color.white);
            else if (!string.IsNullOrEmpty(text))
            {
                var l = Text(s, text, "swatch-text");
                l.style.color = color.grayscale > 0.62f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
                l.style.fontSize = size * 0.42f;
            }
            return s;
        }

        /// <summary>Schlüssel-Wert-Zeile (Abrechnung, Details).</summary>
        public static VisualElement KV(VisualElement parent, string key, string value, Color? color = null)
        {
            var r = Row(parent, 8f, "kv");
            var k = Text(r, key, "kv-key");
            k.style.flexGrow = 1;
            k.style.flexShrink = 1;
            var v = Text(r, value, "kv-value");
            if (color.HasValue) v.style.color = color.Value;
            return r;
        }

        public static VisualElement Separator(VisualElement parent) => Div(parent, "sep");

        /// <summary>Kennzahl-Kachel.</summary>
        public static VisualElement Stat(VisualElement parent, string title, string value, Color? color = null, string sub = null)
        {
            var c = Col(parent, 2f, "stat");
            c.style.flexGrow = 1;
            c.style.flexBasis = 0;
            Text(c, title, "stat-key");
            var v = Text(c, value, "stat-value");
            if (color.HasValue) v.style.color = color.Value;
            if (!string.IsNullOrEmpty(sub)) Text(c, sub, "stat-sub");
            return c;
        }

        public static VisualElement Chip(VisualElement parent, string text, string icon = null, Color? color = null, string extra = null)
        {
            var c = Row(parent, 6f, "chip");
            if (!string.IsNullOrEmpty(extra)) c.AddToClassList(extra);
            if (!string.IsNullOrEmpty(icon)) Icon(c, icon, 14f, color);
            var l = Text(c, text, "chip-text");
            if (color.HasValue) l.style.color = color.Value;
            return c;
        }

        /// <summary>Sterne-Anzeige mit Icons.</summary>
        public static VisualElement Stars(VisualElement parent, float rating, float size = 14f, Color? color = null)
        {
            var r = Row(parent, 1f, "stars");
            int full = Mathf.RoundToInt(rating);
            var c = color ?? Theme.Accent;
            for (int i = 0; i < 5; i++) Icon(r, i < full ? "star" : "star_o", size, c);
            return r;
        }

        /// <summary>Eigener Umschalter (Pille), unabhängig vom Unity-Theme.</summary>
        public static VisualElement Toggle(VisualElement parent, string label, bool value, Action<bool> onChange)
        {
            var row = Row(parent, 12f, "toggle-row");
            var sw = new VisualElement();
            sw.AddToClassList("switch");
            var knob = new VisualElement();
            knob.AddToClassList("switch-knob");
            sw.Add(knob);
            Attach(row, sw);
            bool state = value;
            sw.EnableInClassList("on", state);
            if (!string.IsNullOrEmpty(label))
            {
                var l = Text(row, label, "toggle-label");
                l.style.flexShrink = 1;
            }
            row.AddManipulator(new Clickable(() =>
            {
                state = !state;
                sw.EnableInClassList("on", state);
                Game.Sound("click", 0.05f, -4f);
                onChange?.Invoke(state);
            }));
            return row;
        }

        /// <summary>Eigener Schieberegler. format bestimmt die Wertanzeige.</summary>
        public static VisualElement Slider(VisualElement parent, string label, float min, float max, float value, float step,
            Func<float, string> format, Action<float> onChange)
        {
            var row = Row(parent, 14f, "slider-row");
            var l = Text(row, label, "slider-label");
            var track = Div(row, "slider");
            track.style.flexGrow = 1;
            var fill = new VisualElement();
            fill.AddToClassList("slider-fill");
            track.Add(fill);
            var knob = new VisualElement();
            knob.AddToClassList("slider-knob");
            track.Add(knob);
            var vl = Text(row, "", "slider-value");
            float cur = value;

            void Show()
            {
                float t = Mathf.InverseLerp(min, max, cur);
                fill.style.width = Length.Percent(t * 100f);
                knob.style.left = Length.Percent(t * 100f);
                vl.text = format != null ? format(cur) : cur.ToString("0.0");
            }

            void SetFromX(float localX)
            {
                float w = track.resolvedStyle.width;
                if (w <= 1f) return;
                float t = Mathf.Clamp01(localX / w);
                float v = Mathf.Lerp(min, max, t);
                if (step > 0f) v = Mathf.Round(v / step) * step;
                v = Mathf.Clamp(v, min, max);
                if (Mathf.Abs(v - cur) < 0.0001f) return;
                cur = v;
                Show();
                onChange?.Invoke(cur);
            }

            bool dragging = false;
            track.RegisterCallback<PointerDownEvent>(e =>
            {
                dragging = true;
                track.CapturePointer(e.pointerId);
                SetFromX(e.localPosition.x);
            });
            track.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (dragging) SetFromX(e.localPosition.x);
            });
            track.RegisterCallback<PointerUpEvent>(e =>
            {
                dragging = false;
                track.ReleasePointer(e.pointerId);
            });
            Show();
            return row;
        }

        /// <summary>Tastenkappe für Hinweise ("E", "Tab" ...).</summary>
        public static Label Key(VisualElement parent, string key)
        {
            var k = Text(parent, key, "keycap");
            return k;
        }

        public static void Show(VisualElement el, bool on)
        {
            if (el != null) el.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static bool IsShown(VisualElement el) => el != null && el.resolvedStyle.display != DisplayStyle.None && el.style.display != DisplayStyle.None;

        /// <summary>Einblenden mit kurzer Animation (Deckkraft + Skalierung), läuft auch bei Pause.</summary>
        public static void PopIn(VisualElement el, float duration = 0.18f)
        {
            if (el == null) return;
            el.style.opacity = 0f;
            el.style.scale = new Scale(new Vector3(0.97f, 0.97f, 1f));
            Anim.Run(duration, t =>
            {
                el.style.opacity = t;
                float s = Mathf.Lerp(0.97f, 1f, t);
                el.style.scale = new Scale(new Vector3(s, s, 1f));
            }, null, Ease.OutCubic, true, el);
        }
    }
}
