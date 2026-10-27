using System;
using DropshippingGame.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.Testing
{
    /// <summary>
    /// Oberfläche des Selbsttests: kleine Einblendung oben links während des Laufs („Selbsttest 5/22 …“),
    /// Start-Abfrage im Hauptmenü (F10, nur mit Entwickler-Werkzeugen) und das Fertig-Fenster mit
    /// Ergebnis und Knöpfen „Ordner öffnen“ / „Bericht öffnen“. Liegt über allen Ebenen von <see cref="UIRoot"/>,
    /// sperrt keine Eingaben und wird vom Übersetzer (LocSweeper) nicht angefasst.
    /// </summary>
    public static class SelfTestUI
    {
        private static VisualElement _overlay, _prompt, _summary;
        private static Label _overlayText;

        private static readonly Color Bg = new Color(0.07f, 0.07f, 0.09f, 0.96f);
        private static readonly Color Accent = new Color(1f, 0.35f, 0.2f, 1f);
        private static readonly Color Good = new Color(0.3f, 0.85f, 0.45f, 1f);
        private static readonly Color Bad = new Color(1f, 0.38f, 0.32f, 1f);
        private static readonly Color Text = new Color(0.93f, 0.93f, 0.95f, 1f);
        private static readonly Color Muted = new Color(0.65f, 0.65f, 0.7f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _overlay = _prompt = _summary = null;
            _overlayText = null;
        }

        private static VisualElement Root => Game.UI != null ? Game.UI.Root : null;

        // =====================================================================================
        // Hauptmenü: F10 öffnet die Start-Abfrage (Hook in GameRoot.Update)
        // =====================================================================================

        /// <summary>Im Hauptmenü jeden Frame aufrufen. true = Eingabe verbraucht (Menü soll nichts tun).</summary>
        public static bool HandleMenuInput()
        {
            if (!DevTools.Enabled) return false;
            if (SelfTest.Running) return true; // Test steuert selbst – Esc soll das Menü nicht umblättern
            if (_summary != null)
            {
                if (GameInput.CancelDown || GameInput.AdminDown) CloseSummary();
                return true;
            }
            if (_prompt != null)
            {
                if (GameInput.CancelDown || GameInput.AdminDown) ClosePrompt();
                return true;
            }
            if (GameInput.AdminDown)
            {
                OpenPrompt();
                return true;
            }
            return false;
        }

        private static void OpenPrompt()
        {
            var root = Root;
            if (root == null || _prompt != null) return;
            var card = Card(root, 620f, out _prompt);
            Title(card, "SELBSTTEST");
            Para(card, "Spielt das Spiel ca. 3 Minuten lang automatisch durch und schreibt einen Bericht mit Screenshots, " +
                          "den du einfach als ZIP verschicken kannst.", Text, 18);
            Para(card, "Der Test nutzt einen eigenen Spielstand – deine Spielstände bleiben unberührt. " +
                          "Bitte Maus und Tastatur während des Tests nicht benutzen. Am Ende öffnet sich der Ordner mit dem Bericht.", Muted, 16);
            var row = Row(card);
            row.Add(Btn("Selbsttest starten", () =>
            {
                ClosePrompt();
                SelfTest.Launch();
            }, Accent));
            row.Add(Btn("Abbrechen", ClosePrompt, new Color(0.2f, 0.2f, 0.24f)));
        }

        private static void ClosePrompt()
        {
            _prompt?.RemoveFromHierarchy();
            _prompt = null;
        }

        private static void CloseSummary()
        {
            if (_summary != null) Game.Root?.Lock("selftest", false);
            _summary?.RemoveFromHierarchy();
            _summary = null;
        }

        /// <summary>Start-Abfrage und Fertig-Fenster schließen (vor jedem Testschritt).</summary>
        public static void ClosePanels()
        {
            ClosePrompt();
            CloseSummary();
        }

        // =====================================================================================
        // Einblendung während des Tests
        // =====================================================================================
        public static void ShowOverlay()
        {
            var root = Root;
            if (root == null) return;
            RemoveOverlay();
            _overlay = new VisualElement { name = "selftest-overlay", pickingMode = PickingMode.Ignore };
            var st = _overlay.style;
            st.position = Position.Absolute;
            st.left = 16;
            st.top = 16;
            st.maxWidth = 640;
            st.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
            st.paddingLeft = st.paddingRight = 12;
            st.paddingTop = st.paddingBottom = 8;
            Round(_overlay, 8);
            st.borderLeftWidth = 4;
            st.borderLeftColor = Accent;
            _overlayText = Lbl("SELBSTTEST läuft – bitte Maus und Tastatur nicht benutzen", Text, 16);
            _overlayText.pickingMode = PickingMode.Ignore;
            _overlay.Add(_overlayText);
            root.Add(_overlay);
        }

        public static void UpdateOverlay(int index, int total, string title)
        {
            if (_overlay == null || _overlay.panel == null) ShowOverlay();
            if (_overlayText != null)
                _overlayText.text = "SELBSTTEST " + index + "/" + total + " · " + title + "\nBitte Maus und Tastatur nicht benutzen.";
            _overlay?.BringToFront();
        }

        public static void SetOverlayHidden(bool hidden)
        {
            if (_overlay != null) _overlay.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public static void RemoveOverlay()
        {
            _overlay?.RemoveFromHierarchy();
            _overlay = null;
            _overlayText = null;
        }

        // =====================================================================================
        // Fertig-Fenster
        // =====================================================================================
        public static void ShowSummary(SelfTest t)
        {
            var root = Root;
            if (root == null || t == null) return;
            ClosePanels();
            var r = t.Report;
            var card = Card(root, 760f, out _summary);
            Game.Root?.Lock("selftest", true); // Mauszeiger frei, Spiel reagiert nicht
            Title(card, "SELBSTTEST FERTIG");
            var head = Lbl(r.Headline, r.Failed == 0 ? Good : Bad, 30);
            head.style.unityFontStyleAndWeight = FontStyle.Bold;
            head.style.marginBottom = 4;
            card.Add(head);
            Para(card, r.Subline, Muted, 16);
            int shown = 0;
            foreach (var s in r.Steps)
            {
                if (s.State != StepState.Fail) continue;
                if (shown++ >= 6)
                {
                    Para(card, "… weitere Fehler im Bericht.", Muted, 15);
                    break;
                }
                string why = s.Problems.Count > 0 ? s.Problems[0] : (s.LogErrors.Count > 0 ? "Fehler im Log: " + StepResult.FirstLine(s.LogErrors[0]) : "");
                Para(card, s.Title + " – " + why, Bad, 15);
            }
            if (r.Perf != null && r.Perf.Frames > 0) Para(card, "Leistung: " + r.Perf.Text(), Text, 15);
            Para(card, "Bericht und Screenshots liegen hier:\n" + t.Folder, Text, 15);
            Para(card, "Bitte den ganzen Ordner als ZIP an den Entwickler schicken (Rechtsklick › Senden an › ZIP-komprimierter Ordner).", Muted, 15);
            var row = Row(card);
            row.Add(Btn("Ordner öffnen", t.OpenFolder, Accent));
            row.Add(Btn("Bericht öffnen", t.OpenReport, new Color(0.2f, 0.2f, 0.24f)));
            row.Add(Btn("Schließen", CloseSummary, new Color(0.2f, 0.2f, 0.24f)));
        }

        // =====================================================================================
        // Bausteine (Inline-Stile, unabhängig vom USS)
        // =====================================================================================
        /// <summary>Abgedunkelte Ebene (<paramref name="layer"/>) mit zentrierter Karte; Inhalte kommen in die Karte.</summary>
        private static VisualElement Card(VisualElement root, float width, out VisualElement layer)
        {
            var dim = new VisualElement { pickingMode = PickingMode.Position };
            var ds = dim.style;
            ds.position = Position.Absolute;
            ds.left = ds.right = ds.top = ds.bottom = 0;
            ds.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            ds.alignItems = Align.Center;
            ds.justifyContent = Justify.Center;
            var card = new VisualElement();
            var st = card.style;
            st.width = width;
            st.maxWidth = new Length(92, LengthUnit.Percent);
            st.backgroundColor = Bg;
            st.paddingLeft = st.paddingRight = 24;
            st.paddingTop = st.paddingBottom = 20;
            Round(card, 14);
            st.borderTopWidth = st.borderBottomWidth = st.borderLeftWidth = st.borderRightWidth = 2;
            st.borderTopColor = st.borderBottomColor = st.borderLeftColor = st.borderRightColor = Accent;
            dim.Add(card);
            root.Add(dim);
            dim.BringToFront();
            layer = dim;
            return card;
        }

        private static void Title(VisualElement parent, string text)
        {
            var l = Lbl(text, Accent, 22);
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.marginBottom = 10;
            parent.Add(l);
        }

        private static void Para(VisualElement parent, string text, Color color, int size)
        {
            var l = Lbl(text, color, size);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginBottom = 8;
            parent.Add(l);
        }

        private static Label Lbl(string text, Color color, int size)
        {
            var l = new Label(text);
            l.AddToClassList(LocSweeper.SkipClass);
            l.style.color = color;
            l.style.fontSize = size;
            return l;
        }

        private static VisualElement Row(VisualElement parent)
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row;
            r.style.flexWrap = Wrap.Wrap;
            r.style.marginTop = 10;
            parent.Add(r);
            return r;
        }

        private static Button Btn(string text, Action onClick, Color bg)
        {
            var b = new Button(() =>
            {
                try { onClick?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }) { text = text };
            b.AddToClassList(LocSweeper.SkipClass);
            var st = b.style;
            st.backgroundColor = bg;
            st.color = Text;
            st.fontSize = 17;
            st.paddingLeft = st.paddingRight = 16;
            st.paddingTop = st.paddingBottom = 8;
            st.marginRight = 8;
            st.marginTop = st.marginBottom = st.marginLeft = 0;
            Round(b, 8);
            return b;
        }

        private static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }
    }
}
