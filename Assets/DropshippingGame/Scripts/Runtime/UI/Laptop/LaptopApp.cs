using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Basis aller Laptop-Seiten (Apps und Reiter). Jede Seite baut ihren Inhalt in
    /// <see cref="Root"/> auf und wird bei Wirtschaftsänderungen neu gebaut (außer
    /// <see cref="CanRebuild"/> sagt nein). Titel und Reiter zeichnet der <see cref="LaptopView"/>,
    /// die Seite liefert nur eine kurze Einleitung (<see cref="Lead"/>).
    /// Enthält die gemeinsamen Bausteine: Abschnitte, Karten, Kacheln, Kennzahlen, Tabellen.
    /// </summary>
    public abstract class LaptopApp
    {
        public LaptopView View;
        public VisualElement Root;

        protected static Sim S => Game.Sim;

        /// <summary>Kurze Einleitung unter dem App-Titel (optional).</summary>
        public virtual string Lead => null;

        public abstract void Build();
        public virtual void Tick(float dt) { }
        public virtual bool CanRebuild() => true;
        public virtual void Closed() { }

        protected void Rebuild() => View?.Rebuild();

        /// <summary>Andere App/Reiter öffnen ("shop/brand", "bank" …).</summary>
        protected void Go(string target) => View?.OpenApp(target);

        // ---- Bausteine ---------------------------------------------------------------------------
        // Erst übersetzen, dann groß schreiben (sonst findet die Übersetzung den Text nicht).
        protected static Label Section(VisualElement parent, string text) => UIX.Text(parent, Lang.T(text).ToUpperInvariant(), "section");

        protected static Label P(VisualElement parent, string text, string cls = "muted")
        {
            var l = UIX.Text(parent, text, cls);
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        /// <summary>Karte mit Kopfzeile (Titel links, Wert rechts).</summary>
        protected static VisualElement Card(VisualElement parent, string title = null, string right = null, string extraClass = null)
        {
            var c = UIX.Card(parent, null, extraClass);
            if (!string.IsNullOrEmpty(title)) UIX.CardHead(c, Lang.T(title).ToUpperInvariant(), right);
            return c;
        }

        /// <summary>Anklickbare Auswahlkachel (Produkt, Lieferant, Asset ...).</summary>
        protected static Button Tile(VisualElement parent, bool selected, bool locked, Action onClick, float width = -1f, params string[] classes) =>
            UIX.Tile(parent, onClick, selected, locked, width, classes);

        /// <summary>Zeile mit gleich hohen Spalten (Kennzahlen oder Karten nebeneinander).</summary>
        protected static VisualElement Columns(VisualElement parent, float gap = 12f)
        {
            var r = UIX.Row(parent, gap);
            r.style.alignItems = Align.Stretch;
            return r;
        }

        /// <summary>Kennzahl-Kachel, Farbe über Klasse, damit sie in allen Laptop-Designs lesbar bleibt.</summary>
        protected static VisualElement Stat(VisualElement parent, string key, string value, string tone = null, string sub = null)
        {
            var s = UIX.Stat(parent, key, value, null, sub);
            if (!string.IsNullOrEmpty(tone)) s.Q<Label>(className: "stat-value").AddToClassList(tone + "-text");
            return s;
        }

        protected static T Grow<T>(T el, float basis = 0f) where T : VisualElement
        {
            el.style.flexGrow = 1;
            el.style.flexShrink = 1;
            el.style.flexBasis = basis;
            return el;
        }

        /// <summary>
        /// Tabellenzeile mit festen Spaltenbreiten (negative Breite = flexibel).
        /// numeric: Spalten rechtsbündig mit fester Zeichenbreite.
        /// </summary>
        protected static VisualElement TableRow(VisualElement parent, float[] widths, string[] cells, bool head = false, string[] tones = null, bool[] numeric = null)
        {
            var r = UIX.Row(parent, 10f, head ? "table-head-row" : "table-row");
            for (int i = 0; i < cells.Length; i++)
            {
                var l = UIX.Text(r, cells[i], head ? "table-head" : "");
                float w = i < widths.Length ? widths[i] : -1f;
                if (w > 0)
                {
                    l.style.width = w;
                    l.style.flexShrink = 0;
                }
                else Grow(l);
                bool num = numeric != null && i < numeric.Length && numeric[i];
                if (num)
                {
                    l.AddToClassList("cell-num");
                    if (!head) Fonts.AddClass(l, "num");
                }
                if (!head && tones != null && i < tones.Length && !string.IsNullOrEmpty(tones[i])) l.AddToClassList(tones[i]);
            }
            return r;
        }

        protected static string LockText(int level) => "Ab Level " + level;

        protected static Button Btn(VisualElement parent, string text, Action onClick, string variant = "", bool disabled = false, string icon = null) =>
            UIX.Button(parent, text, onClick, variant, disabled, icon);

        /// <summary>Tipp-Zeile mit Glühbirne.</summary>
        protected static VisualElement Tip(VisualElement parent, string text)
        {
            var tip = UIX.Card(parent);
            var th = UIX.Row(tip, 10f);
            UIX.Icon(th, "bulb", 20f, Theme.LaptopAccent);
            Grow(P(th, text, ""));
            return tip;
        }
    }

    /// <summary>Platzhalter für gesperrte Apps/Reiter mit Erklärung.</summary>
    public sealed class AppLocked : LaptopApp
    {
        private readonly string _title, _reason, _key;

        public AppLocked(string title, string reason, string key)
        {
            _title = title;
            _reason = reason;
            _key = key ?? "";
        }

        public override string Lead => "Noch gesperrt – " + _reason + ".";

        public override void Build()
        {
            var c = UIX.Card(Root);
            c.style.alignItems = Align.Center;
            c.style.paddingTop = 34;
            c.style.paddingBottom = 34;
            var ic = UIX.Div(c, "empty-icon");
            UIX.Icon(ic, "lock", 26f);
            UIX.Text(c, _title + ": " + _reason, "h2").style.marginTop = 8;
            string hint = _key.Contains("trading")
                ? "Mit dem Trading-Konto steckst du Geld in Krypto, Meme-Aktien oder einen ETF. Riskant, aber manchmal lohnend."
                : (_key.Contains("team")
                    ? "Mitarbeiter packen, lagern und verschicken automatisch. Dafür brauchst du die Lagerhalle und etwas Erfahrung."
                    : "Sammle Erfahrung – jedes verschickte Paket bringt dich weiter.");
            var p = P(c, hint);
            p.style.maxWidth = 520;
            p.style.unityTextAlign = TextAnchor.MiddleCenter;
            Btn(c, "Ziele ansehen", () => Go("company/goals"), "soft", false, "trophy").style.marginTop = 12;
        }
    }

    /// <summary>„Bald verfügbar“ – Platzhalter für Apps, die in Phase B gefüllt werden.</summary>
    public sealed class AppSoon : LaptopApp
    {
        private readonly string _title, _icon, _text;

        public AppSoon(string title, string icon, string text)
        {
            _title = title;
            _icon = icon;
            _text = text;
        }

        public override void Build()
        {
            var c = UIX.Card(Root);
            UIX.Empty(c, string.IsNullOrEmpty(_icon) ? "sparkle" : _icon, _title + " – bald verfügbar", _text);
        }
    }
}
