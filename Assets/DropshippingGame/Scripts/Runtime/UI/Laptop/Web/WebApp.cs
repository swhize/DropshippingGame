using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Basis der "Webseiten" im Laptop-Browser (AllesExpress, Mein Shop, iWolke …). Eine Seite
    /// füllt die ganze Browserfläche (ohne HustleOS-Titel), hat eine eigene Skin-Klasse fürs
    /// Stylesheet (Resources/UI/Web.uss) und eine Route ("", "item/led", "loans" …), die der
    /// <see cref="LaptopView"/> pro App merkt und für Zurück/URL-Leiste nutzt.
    /// </summary>
    public abstract class WebApp : LaptopApp
    {
        /// <summary>App-Id im <see cref="LaptopView"/> (z. B. "allesexpress").</summary>
        public abstract string AppId { get; }

        /// <summary>Klasse der Seitenwurzel (z. B. "ae") – alle Regeln in Web.uss hängen daran.</summary>
        public abstract string SkinClass { get; }

        public abstract WebSkin Skin { get; }

        /// <summary>Domain ohne https:// (z. B. "allesexpress.cn").</summary>
        protected abstract string Domain { get; }

        /// <summary>Anzeige in der Adressleiste.</summary>
        public virtual string Url => Domain + (string.IsNullOrEmpty(Route) ? "" : "/" + Route);

        public override string Lead => null;

        public string Route
        {
            get => LaptopView.GetRoute(AppId);
            set => LaptopView.SetRoute(AppId, value);
        }

        /// <summary>Innerhalb der Seite navigieren (mit Verlauf für den Zurück-Knopf).</summary>
        protected void Nav(string route)
        {
            if (View != null) View.Navigate(AppId, route ?? "");
            else Route = route ?? "";
        }

        /// <summary>Teil der Route nach einem Präfix ("item/led" mit "item/" → "led"), sonst null.</summary>
        protected string RouteArg(string prefix)
        {
            string r = Route ?? "";
            return r.StartsWith(prefix, StringComparison.Ordinal) ? r.Substring(prefix.Length) : null;
        }

        // ---- Bausteine in der Seitenschrift -------------------------------------------------------
        protected Label T(VisualElement parent, string text, params string[] cls) => W.Text(parent, text, Skin, WebFonts.Body, cls);
        protected Label B(VisualElement parent, string text, params string[] cls) => W.Text(parent, text, Skin, WebFonts.Bold, cls);
        protected Label H(VisualElement parent, string text, params string[] cls) => W.Text(parent, text, Skin, WebFonts.Title, cls);

        protected Button Btn(VisualElement parent, string text, Action onClick, params string[] cls) => W.Button(parent, text, onClick, Skin, false, cls);

        protected Button BtnIf(VisualElement parent, bool enabled, string text, Action onClick, params string[] cls) =>
            W.Button(parent, text, onClick, Skin, !enabled, cls);

        protected static Button Press(VisualElement parent, Action onClick, params string[] cls) => UIX.Pressable(parent, onClick, cls);

        protected static VisualElement Div(VisualElement parent, params string[] cls) => UIX.Div(parent, cls);
        protected static VisualElement Row(VisualElement parent, float gap, params string[] cls) => UIX.Row(parent, gap, cls);
        protected static VisualElement Col(VisualElement parent, float gap, params string[] cls) => UIX.Col(parent, gap, cls);

        protected static VisualElement Fill(VisualElement parent)
        {
            var d = UIX.Div(parent);
            d.style.flexGrow = 1;
            return d;
        }

        /// <summary>Flexibles Element (wächst, darf schrumpfen).</summary>
        protected static T Flex<T>(T el, float grow = 1f) where T : VisualElement
        {
            el.style.flexGrow = grow;
            el.style.flexShrink = 1;
            el.style.flexBasis = 0;
            return el;
        }

        protected static T Wd<T>(T el, float width) where T : VisualElement
        {
            el.style.width = width;
            el.style.flexShrink = 0;
            return el;
        }

        protected static void Toast(string text, string kind = "info") => S?.Notify(text, kind);

        /// <summary>Uhr bis Feierabend (20:00) als "02:14:59"-Teile (Stunden, Minuten, Sekunden = Spielminuten-Bruchteil).</summary>
        protected static string[] UntilClose()
        {
            float left = S != null ? Mathf.Max(0f, GameData.DayEnd - S.TimeMinutes) : 0f;
            int h = (int)(left / 60f);
            int m = (int)(left % 60f);
            int sec = (int)((left - Mathf.Floor(left)) * 60f);
            return new[] { h.ToString("00"), m.ToString("00"), sec.ToString("00") };
        }

        protected static int ProductIndex(string id)
        {
            for (int i = 0; i < GameData.Products.Length; i++)
                if (GameData.Products[i].Id == id)
                    return i;
            return -1;
        }
    }
}
