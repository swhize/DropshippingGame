using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Baukasten für alle Oberflächen (UI Toolkit). Container merken sich ihren Abstand und
    /// setzen ihn automatisch zwischen neue Kinder (USS kennt kein "gap").
    /// Aussehen kommt aus Resources/UI/Game.uss, Schriften aus <see cref="Fonts"/>.
    ///
    /// Alles, was man anklicken kann, ist ein UI-Toolkit-<see cref="UnityEngine.UIElements.Button"/>:
    /// dadurch funktionieren Maus, Tastatur (Enter) und Controller (Steuerkreuz + A) überall gleich.
    /// </summary>
    public static class UIX
    {
        private sealed class Gap
        {
            public float Size;
            public bool Horizontal;
        }

        public sealed class TabItem
        {
            public string Id, Title, Icon, Badge;
            public bool Locked;
        }

        // =====================================================================================
        // Grundelemente
        // =====================================================================================
        public static T Attach<T>(VisualElement parent, T el) where T : VisualElement
        {
            if (parent == null) return el;
            // Abstand nur setzen, wenn > 0 – sonst würden Außenabstände aus dem Stylesheet überschrieben.
            if (parent.userData is Gap g && g.Size > 0f && parent.childCount > 0 && el.style.position != Position.Absolute)
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
            AddClasses(el, classes);
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

        /// <summary>Setzt den automatischen Abstand eines beliebigen Containers.</summary>
        public static T WithGap<T>(T el, float gap, bool horizontal) where T : VisualElement
        {
            el.userData = new Gap { Size = gap, Horizontal = horizontal };
            return el;
        }

        private static void AddClasses(VisualElement el, string[] classes)
        {
            if (classes == null) return;
            foreach (var c in classes)
                if (!string.IsNullOrEmpty(c))
                    el.AddToClassList(c);
        }

        public static Label Text(VisualElement parent, string text, params string[] classes)
        {
            var l = new Label(text ?? "");
            l.RemoveFromClassList("unity-label");
            l.enableRichText = true;
            AddClasses(l, classes);
            Fonts.Auto(l);
            return Attach(parent, l);
        }

        /// <summary>Zahl/Geld/Uhrzeit mit fester Zeichenbreite.</summary>
        public static Label Num(VisualElement parent, string text, bool bold = false, params string[] classes)
        {
            var l = Text(parent, text, classes);
            Fonts.AddClass(l, bold ? "num-b" : "num");
            return l;
        }

        public static Label Colored(VisualElement parent, string text, Color color, params string[] classes)
        {
            var l = Text(parent, text, classes);
            l.style.color = color;
            return l;
        }

        /// <summary>Text fett machen (echte Fett-Schrift, falls vorhanden).</summary>
        public static Label Bold(Label l)
        {
            if (l == null) return null;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            Fonts.Set(l, Fonts.Bold);
            return l;
        }

        /// <summary>Alle vier Ecken abrunden.</summary>
        public static T Round<T>(T el, float r) where T : VisualElement
        {
            if (el == null) return null;
            el.style.borderTopLeftRadius = r;
            el.style.borderTopRightRadius = r;
            el.style.borderBottomLeftRadius = r;
            el.style.borderBottomRightRadius = r;
            return el;
        }

        /// <summary>Einzeilig mit „…“ am Ende statt Überlauf.</summary>
        public static T Ellipsis<T>(T el) where T : VisualElement
        {
            el.AddToClassList("ellipsis");
            return el;
        }

        public static VisualElement Spacer(VisualElement parent)
        {
            var el = new VisualElement();
            el.style.flexGrow = 1;
            el.pickingMode = PickingMode.Ignore;
            parent?.Add(el);
            return el;
        }

        public static VisualElement Icon(VisualElement parent, string name, float size = 18f, Color? tint = null, params string[] classes)
        {
            var el = new VisualElement();
            el.AddToClassList("icon");
            AddClasses(el, classes);
            el.style.width = size;
            el.style.height = size;
            el.style.flexShrink = 0;
            el.style.backgroundImage = new StyleBackground(Icons.Get(name));
            if (tint.HasValue) el.style.unityBackgroundImageTintColor = tint.Value;
            el.pickingMode = PickingMode.Ignore;
            return Attach(parent, el);
        }

        public static void SetIcon(VisualElement el, string name)
        {
            if (el != null) el.style.backgroundImage = new StyleBackground(Icons.Get(name));
        }

        public static VisualElement Dot(VisualElement parent, Color color, float size = 8f)
        {
            var d = Div(parent, "dot");
            d.style.width = size;
            d.style.height = size;
            d.style.borderTopLeftRadius = size / 2f;
            d.style.borderTopRightRadius = size / 2f;
            d.style.borderBottomLeftRadius = size / 2f;
            d.style.borderBottomRightRadius = size / 2f;
            d.style.backgroundColor = color;
            d.style.flexShrink = 0;
            d.pickingMode = PickingMode.Ignore;
            return d;
        }

        // =====================================================================================
        // Buttons & anklickbare Flächen
        // =====================================================================================
        private static Button NewButton()
        {
            var b = new Button();
            b.RemoveFromClassList("unity-button");
            b.RemoveFromClassList("unity-text-element");
            b.text = "";
            return b;
        }

        /// <summary>Button ohne Unity-Standardstil. variant: "", "accent", "ghost", "danger", "soft".</summary>
        public static Button Button(VisualElement parent, string text, Action onClick, string variant = "", bool disabled = false, string icon = null)
        {
            var b = NewButton();
            b.AddToClassList("btn");
            if (!string.IsNullOrEmpty(variant)) b.AddToClassList("btn-" + variant);
            if (!string.IsNullOrEmpty(icon)) Icon(b, icon, 16f, null, "btn-icon");
            if (!string.IsNullOrEmpty(text))
            {
                var l = new Label(text);
                l.RemoveFromClassList("unity-label");
                l.enableRichText = true;
                l.AddToClassList("btn-label");
                l.pickingMode = PickingMode.Ignore;
                Fonts.Set(l, Fonts.Bold);
                b.Add(l);
            }
            else b.AddToClassList("btn-icon-only");
            HookButton(b, onClick);
            b.SetEnabled(!disabled);
            return Attach(parent, b);
        }

        private static void HookButton(Button b, Action onClick)
        {
            b.clicked += () =>
            {
                if (!b.enabledInHierarchy) return;
                Game.Sound("click", 0.05f, -4f);
                try
                {
                    onClick?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                // Mit Maus bedient: Fokus wieder lösen – kein hängender Fokusrahmen und
                // Enter/Leertaste lösen den Button später nicht versehentlich erneut aus.
                if (!GameInput.UsingGamepad && b.panel != null && b.focusController != null && b.focusController.focusedElement == b)
                    b.schedule.Execute(() =>
                    {
                        if (b.panel != null && b.focusController != null && b.focusController.focusedElement == b) b.Blur();
                    });
            };
            b.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (b.enabledInHierarchy) Game.Sound("hover", 0.05f, -12f);
            });
        }

        public static void SetButtonText(Button b, string text)
        {
            if (b == null) return;
            var l = b.Q<Label>(className: "btn-label");
            if (l != null) l.text = text;
        }

        /// <summary>
        /// Beliebige anklickbare Fläche (Kachel, Listeneintrag, Reiter …) – intern ein Button,
        /// damit Tastatur und Controller sie fokussieren und auslösen können.
        /// </summary>
        public static Button Pressable(VisualElement parent, Action onClick, params string[] classes)
        {
            var b = NewButton();
            b.AddToClassList("pressable");
            AddClasses(b, classes);
            HookButton(b, onClick);
            return Attach(parent, b);
        }

        public static Button PressRow(VisualElement parent, float gap, Action onClick, params string[] classes)
        {
            var b = Pressable(parent, onClick, classes);
            b.AddToClassList("row");
            b.userData = new Gap { Size = gap, Horizontal = true };
            return b;
        }

        public static Button PressCol(VisualElement parent, float gap, Action onClick, params string[] classes)
        {
            var b = Pressable(parent, onClick, classes);
            b.AddToClassList("col");
            b.userData = new Gap { Size = gap, Horizontal = false };
            return b;
        }

        /// <summary>Auswahlkachel. Gesperrt = sichtbar, aber nicht anklickbar.</summary>
        public static Button Tile(VisualElement parent, Action onClick, bool selected = false, bool locked = false, float width = -1f, params string[] classes)
        {
            var t = PressCol(parent, 4f, onClick, classes);
            t.AddToClassList("tile");
            t.EnableInClassList("selected", selected);
            t.EnableInClassList("locked", locked);
            if (width > 0) t.style.width = width;
            if (locked) t.SetEnabled(false);
            return t;
        }

        /// <summary>Kinder eines Buttons für Mausereignisse durchlässig machen (Hover/Klick landen beim Button).</summary>
        public static void PassThrough(VisualElement root)
        {
            if (root == null) return;
            foreach (var c in root.Children())
            {
                if (c is Button || c is TextField) continue;
                c.pickingMode = PickingMode.Ignore;
                PassThrough(c);
            }
        }

        // =====================================================================================
        // Karten, Kennzahlen, Listen
        // =====================================================================================
        public static VisualElement Card(VisualElement parent, string title = null, string extraClass = null)
        {
            var c = Col(parent, 10f, "card");
            if (!string.IsNullOrEmpty(extraClass)) c.AddToClassList(extraClass);
            if (!string.IsNullOrEmpty(title)) Text(c, title, "card-title");
            return c;
        }

        /// <summary>Kartenkopf: Titel links, optional Wert/Aktion rechts.</summary>
        public static VisualElement CardHead(VisualElement card, string title, string right = null, string rightClass = null)
        {
            var r = Row(card, 8f, "card-head");
            Text(r, title, "card-title");
            Spacer(r);
            if (!string.IsNullOrEmpty(right)) Text(r, right, "card-head-value", rightClass);
            return r;
        }

        /// <summary>Fortschrittsbalken 0..1.</summary>
        public static VisualElement Bar(VisualElement parent, float value, Color? color = null, float height = 8f, float width = -1f)
        {
            var track = Div(parent, "bar");
            track.style.height = height;
            if (width > 0)
            {
                track.style.width = width;
                track.style.flexShrink = 0;
            }
            else track.style.flexGrow = 1;
            var fill = new VisualElement();
            fill.AddToClassList("bar-fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
            if (color.HasValue) fill.style.backgroundColor = color.Value;
            fill.pickingMode = PickingMode.Ignore;
            track.Add(fill);
            track.pickingMode = PickingMode.Ignore;
            return track;
        }

        public static void SetBar(VisualElement bar, float value, Color? color = null)
        {
            if (bar == null || bar.childCount == 0) return;
            bar[0].style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
            if (color.HasValue) bar[0].style.backgroundColor = color.Value;
        }

        public static VisualElement Swatch(VisualElement parent, Color color, float size = 30f, string icon = null, string text = null)
        {
            var s = Div(parent, "swatch");
            s.style.width = size;
            s.style.height = size;
            s.style.backgroundColor = color;
            s.style.flexShrink = 0;
            s.pickingMode = PickingMode.Ignore;
            if (!string.IsNullOrEmpty(icon)) Icon(s, icon, size * 0.56f, Theme.OnColor(color));
            else if (!string.IsNullOrEmpty(text))
            {
                var l = Text(s, text, "swatch-text");
                l.style.color = Theme.OnColor(color);
                l.style.fontSize = size * 0.42f;
                l.pickingMode = PickingMode.Ignore;
            }
            return s;
        }

        /// <summary>Rundes Profilbild mit Anfangsbuchstaben.</summary>
        public static VisualElement Avatar(VisualElement parent, string name, Color color, float size = 36f)
        {
            string initial = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
            var a = Swatch(parent, color, size, null, initial);
            a.AddToClassList("avatar");
            float r = size * 0.32f;
            a.style.borderTopLeftRadius = r;
            a.style.borderTopRightRadius = r;
            a.style.borderBottomLeftRadius = r;
            a.style.borderBottomRightRadius = r;
            return a;
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
            c.pickingMode = PickingMode.Ignore;
            return c;
        }

        public static Label Badge(VisualElement parent, string text, string extra = null)
        {
            var b = Text(parent, text, "badge");
            if (!string.IsNullOrEmpty(extra)) b.AddToClassList(extra);
            b.pickingMode = PickingMode.Ignore;
            return b;
        }

        /// <summary>Sterne-Anzeige mit Icons.</summary>
        public static VisualElement Stars(VisualElement parent, float rating, float size = 14f, Color? color = null)
        {
            var r = Row(parent, 1f, "stars");
            int full = Mathf.RoundToInt(rating);
            var c = color ?? Theme.Accent;
            for (int i = 0; i < 5; i++) Icon(r, i < full ? "star" : "star_o", size, c);
            r.pickingMode = PickingMode.Ignore;
            return r;
        }

        /// <summary>Leerer Zustand: Symbol, Überschrift, Erklärung.</summary>
        public static VisualElement Empty(VisualElement parent, string icon, string title, string text = null)
        {
            var c = Col(parent, 6f, "empty");
            var ic = Div(c, "empty-icon");
            Icon(ic, icon, 24f);
            Text(c, title, "empty-title");
            if (!string.IsNullOrEmpty(text)) Text(c, text, "empty-text");
            return c;
        }

        // =====================================================================================
        // Schalter, Regler, Reiter
        // =====================================================================================
        /// <summary>Umschalter (Pille) mit Beschriftung – als Button, also auch per Controller bedienbar.</summary>
        public static Button Toggle(VisualElement parent, string label, bool value, Action<bool> onChange)
        {
            bool state = value;
            VisualElement sw = null;
            var row = PressRow(parent, 12f, null, "toggle-row");
            sw = Div(row, "switch");
            Div(sw, "switch-knob");
            sw.EnableInClassList("on", state);
            if (!string.IsNullOrEmpty(label))
            {
                var l = Text(row, label, "toggle-label");
                l.style.flexShrink = 1;
            }
            row.clicked += () =>
            {
                state = !state;
                sw.EnableInClassList("on", state);
                try
                {
                    onChange?.Invoke(state);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
            PassThrough(row);
            return row;
        }

        /// <summary>
        /// Eigener Schieberegler. format bestimmt die Wertanzeige. onChange kommt bei jeder
        /// Änderung, onCommit erst beim Loslassen (bzw. sofort bei Tastatur/Controller).
        /// Controller/Tastatur: fokussieren, dann links/rechts.
        /// </summary>
        public static VisualElement Slider(VisualElement parent, string label, float min, float max, float value, float step,
            Func<float, string> format, Action<float> onChange, Action<float> onCommit = null)
        {
            var row = Row(parent, 14f, "slider-row");
            Text(row, label, "slider-label");
            var track = Div(row, "slider");
            track.style.flexGrow = 1;
            track.focusable = true;
            track.tabIndex = 0;
            var rail = new VisualElement();
            rail.AddToClassList("slider-rail");
            rail.pickingMode = PickingMode.Ignore;
            track.Add(rail);
            var fill = new VisualElement();
            fill.AddToClassList("slider-fill");
            fill.pickingMode = PickingMode.Ignore;
            track.Add(fill);
            var knob = new VisualElement();
            knob.AddToClassList("slider-knob");
            knob.pickingMode = PickingMode.Ignore;
            track.Add(knob);
            var vl = Text(row, "", "slider-value");
            float cur = Mathf.Clamp(value, min, max);

            void Show()
            {
                float t = Mathf.InverseLerp(min, max, cur);
                fill.style.width = Length.Percent(t * 100f);
                knob.style.left = Length.Percent(t * 100f);
                vl.text = format != null ? format(cur) : cur.ToString("0.0");
            }

            void SetValue(float v, bool commit)
            {
                if (step > 0f) v = Mathf.Round(v / step) * step;
                v = Mathf.Clamp(v, min, max);
                bool changed = Mathf.Abs(v - cur) > 0.0001f;
                cur = v;
                Show();
                try
                {
                    if (changed) onChange?.Invoke(cur);
                    if (commit) onCommit?.Invoke(cur);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            void SetFromX(float localX, bool commit)
            {
                float w = track.resolvedStyle.width;
                if (w <= 1f) return;
                SetValue(Mathf.Lerp(min, max, Mathf.Clamp01(localX / w)), commit);
            }

            bool dragging = false;
            track.RegisterCallback<PointerDownEvent>(e =>
            {
                dragging = true;
                track.CapturePointer(e.pointerId);
                SetFromX(e.localPosition.x, false);
            });
            track.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (dragging) SetFromX(e.localPosition.x, false);
            });
            track.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!dragging) return;
                dragging = false;
                track.ReleasePointer(e.pointerId);
                SetFromX(e.localPosition.x, true);
                Game.Sound("click", 0.05f, -8f);
            });
            track.RegisterCallback<NavigationMoveEvent>(e =>
            {
                if (e.direction != NavigationMoveEvent.Direction.Left && e.direction != NavigationMoveEvent.Direction.Right) return;
                float st = step > 0f ? step : (max - min) / 20f;
                SetValue(cur + (e.direction == NavigationMoveEvent.Direction.Right ? st : -st), true);
                Game.Sound("click", 0.05f, -10f);
                e.StopPropagation();
#if UNITY_6000_0_OR_NEWER
                track.focusController?.IgnoreEvent(e);
#else
                e.PreventDefault();
#endif
            });
            Show();
            return row;
        }

        /// <summary>Segment-Umschalter (z. B. Grafikqualität). Gibt den Container zurück.</summary>
        public static VisualElement Segmented(VisualElement parent, string[] labels, int index, Action<int> onChange)
        {
            var seg = Row(parent, 0f, "seg");
            var items = new List<Button>();
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var b = Pressable(seg, null, "seg-item");
                Text(b, labels[i], "seg-label");
                PassThrough(b);
                b.EnableInClassList("on", i == index);
                b.clicked += () =>
                {
                    for (int k = 0; k < items.Count; k++) items[k].EnableInClassList("on", k == idx);
                    try
                    {
                        onChange?.Invoke(idx);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                };
                items.Add(b);
            }
            return seg;
        }

        /// <summary>Reiterleiste. Gesperrte Reiter zeigen ein Schloss, bleiben aber anklickbar (Erklärung).</summary>
        public static VisualElement Tabs(VisualElement parent, IList<TabItem> tabs, string current, Action<string> onSelect, string cls = "tabbar")
        {
            var bar = Row(parent, 4f, cls);
            foreach (var t in tabs)
            {
                string id = t.Id;
                var b = PressRow(bar, 6f, () => onSelect?.Invoke(id), "tab");
                b.EnableInClassList("active", t.Id == current);
                b.EnableInClassList("locked", t.Locked);
                if (!string.IsNullOrEmpty(t.Icon)) Icon(b, t.Locked ? "lock" : t.Icon, 15f, null, "tab-icon");
                Text(b, t.Title, "tab-label");
                if (!string.IsNullOrEmpty(t.Badge)) Badge(b, t.Badge);
                PassThrough(b);
            }
            return bar;
        }

        /// <summary>Tastenkappe für Hinweise ("E", "Tab" ...).</summary>
        public static Label Key(VisualElement parent, string key, string extra = null)
        {
            var k = Text(parent, key, "keycap");
            if (!string.IsNullOrEmpty(extra)) k.AddToClassList(extra);
            k.pickingMode = PickingMode.Ignore;
            return k;
        }

        /// <summary>Mehrere Tasten, durch Leerzeichen getrennt ("W A S D").</summary>
        public static VisualElement Keys(VisualElement parent, string keys, string extra = null)
        {
            var r = Row(parent, 4f, "keys");
            foreach (var part in (keys ?? "").Split(' '))
            {
                if (part == "") continue;
                if (part == "/" || part == "–") Text(r, part, "keys-sep");
                else Key(r, part, extra);
            }
            r.pickingMode = PickingMode.Ignore;
            return r;
        }

        /// <summary>Tastenhinweis: [Taste] Beschreibung.</summary>
        public static VisualElement KeyHint(VisualElement parent, string key, string text)
        {
            var r = Row(parent, 6f, "keyhint");
            Key(r, key, "keycap-sm");
            Text(r, text, "keyhint-text");
            r.pickingMode = PickingMode.Ignore;
            return r;
        }

        // =====================================================================================
        // Sichtbarkeit & Animationen (alle ≤ 200 ms, laufen auch bei Pause)
        // =====================================================================================
        public static void Show(VisualElement el, bool on)
        {
            if (el != null) el.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static bool IsShown(VisualElement el) => el != null && el.resolvedStyle.display != DisplayStyle.None && el.style.display != DisplayStyle.None;

        /// <summary>Einblenden mit kurzer Animation (Deckkraft + Skalierung).</summary>
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

        /// <summary>Einblenden mit leichtem Hochrutschen (App-Wechsel, neue Einträge).</summary>
        public static void FadeSlideIn(VisualElement el, float dy = 8f, float duration = 0.16f, float delay = 0f, float dx = 0f)
        {
            if (el == null) return;
            el.style.opacity = 0f;
            el.style.translate = new Translate(dx, dy, 0);
            Anim.Run(duration, t =>
            {
                el.style.opacity = t;
                el.style.translate = new Translate(dx * (1f - t), dy * (1f - t), 0);
            }, null, Ease.OutCubic, true, el, delay);
        }

        /// <summary>Zahl hochzählen (Kassenbon, Kennzahlen).</summary>
        public static void CountUp(Label l, int to, Func<int, string> fmt, float duration = 0.7f, float delay = 0f, int from = 0)
        {
            if (l == null) return;
            if (fmt == null) fmt = v => v.ToString();
            l.text = fmt(from);
            Anim.Run(duration, t => l.text = fmt(Mathf.RoundToInt(Mathf.Lerp(from, to, t))), () => l.text = fmt(to), Ease.OutCubic, true, l, delay);
        }

        // =====================================================================================
        // Fokus (Controller/Tastatur)
        // =====================================================================================
        private static bool Skip(VisualElement el) =>
            el.ClassListContains("unity-scroller") || el.ClassListContains("no-autofocus");

        private static bool Displayed(VisualElement el) =>
            el.style.display != DisplayStyle.None && el.resolvedStyle.display != DisplayStyle.None && el.visible;

        private static bool CanFocus(VisualElement el) =>
            el.focusable && el.canGrabFocus && el.enabledInHierarchy && !(el is TextField) && !el.ClassListContains("no-autofocus");

        private static void CollectFocusable(VisualElement root, List<VisualElement> into, int max)
        {
            foreach (var c in root.Children())
            {
                if (into.Count >= max) return;
                if (!Displayed(c) || Skip(c)) continue;
                if (CanFocus(c)) into.Add(c);
                if (c is TextField) continue;
                CollectFocusable(c, into, max);
            }
        }

        public static List<VisualElement> Focusables(VisualElement container, int max = 400)
        {
            var list = new List<VisualElement>();
            if (container != null) CollectFocusable(container, list, max);
            return list;
        }

        /// <summary>Erstes bedienbares Element fokussieren. Gibt false zurück, wenn es keins gibt.</summary>
        public static bool FocusFirst(VisualElement container)
        {
            if (container == null || container.panel == null) return false;
            var list = Focusables(container, 1);
            if (list.Count == 0) return false;
            list[0].Focus();
            return true;
        }

        /// <summary>Liegt das Element sichtbar in container?</summary>
        public static bool IsInside(VisualElement container, VisualElement el)
        {
            if (container == null || el == null) return false;
            var p = el;
            while (p != null)
            {
                if (!Displayed(p)) return false;
                if (p == container) return true;
                p = p.hierarchy.parent;
            }
            return false;
        }

        /// <summary>Position des Fokus unter den bedienbaren Elementen (für Neuaufbau), -1 = keiner.</summary>
        public static int FocusIndex(VisualElement container)
        {
            var f = container?.focusController?.focusedElement as VisualElement;
            if (f == null) return -1;
            var list = Focusables(container);
            return list.IndexOf(f);
        }

        public static void RestoreFocus(VisualElement container, int index)
        {
            if (index < 0 || container == null || container.panel == null) return;
            var list = Focusables(container);
            if (list.Count == 0) return;
            list[Mathf.Clamp(index, 0, list.Count - 1)].Focus();
        }

        /// <summary>ScrollView folgt dem Fokus (Controller-Navigation durch lange Listen).</summary>
        public static void FollowFocus(ScrollView sv)
        {
            if (sv == null) return;
            sv.RegisterCallback<FocusInEvent>(e =>
            {
                if (!(e.target is VisualElement ve)) return;
                sv.schedule.Execute(() =>
                {
                    if (ve.panel != null && sv.panel != null && sv.contentContainer.Contains(ve)) sv.ScrollTo(ve);
                });
            });
        }

        /// <summary>Mit dem rechten Stick scrollen.</summary>
        public static void PadScroll(ScrollView sv, float dt)
        {
            if (sv == null || sv.panel == null) return;
            float s = GameInput.UiScroll;
            if (Mathf.Abs(s) < 0.01f) return;
            sv.scrollOffset = new Vector2(sv.scrollOffset.x, Mathf.Max(0f, sv.scrollOffset.y + s * 1100f * dt));
        }

        public static ScrollView Scroll(VisualElement parent, params string[] classes)
        {
            var sv = new ScrollView(ScrollViewMode.Vertical);
            AddClasses(sv, classes);
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.mouseWheelScrollSize = 60f;
            FollowFocus(sv);
            Attach(parent, sv);
            return sv;
        }
    }
}
