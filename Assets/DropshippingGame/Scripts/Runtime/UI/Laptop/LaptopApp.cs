using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Basis aller Laptop-Apps. Jede App baut ihren Inhalt in <see cref="Root"/> auf und wird bei
    /// Wirtschaftsänderungen komplett neu gebaut (außer <see cref="CanRebuild"/> sagt nein).
    /// Enthält die gemeinsamen Bausteine: Kopfzeile, Abschnitte, Kacheln, Tabellen.
    /// </summary>
    public abstract class LaptopApp
    {
        public LaptopView View;
        public VisualElement Root;

        protected static Sim S => Game.Sim;

        public abstract void Build();
        public virtual void Tick(float dt) { }
        public virtual bool CanRebuild() => true;
        public virtual void Closed() { }

        protected void Rebuild() => View?.Rebuild();

        // ---- Bausteine ---------------------------------------------------------------------------
        protected VisualElement Header(string title, string sub, VisualElement parent = null)
        {
            var head = UIX.Col(parent ?? Root, 0f, "app-head");
            UIX.Text(head, title, "app-title");
            if (!string.IsNullOrEmpty(sub)) UIX.Text(head, sub, "app-sub");
            return head;
        }

        protected static Label Section(VisualElement parent, string text) => UIX.Text(parent, text.ToUpperInvariant(), "section");

        protected static Label P(VisualElement parent, string text, string cls = "muted")
        {
            var l = UIX.Text(parent, text, cls);
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        /// <summary>Anklickbare Auswahlkachel (Produkt, Lieferant, Asset ...).</summary>
        protected static VisualElement Tile(VisualElement parent, bool selected, bool locked, Action onClick, float width = -1f)
        {
            var t = UIX.Col(parent, 3f, "tile");
            t.EnableInClassList("selected", selected);
            t.EnableInClassList("locked", locked);
            if (width > 0) t.style.width = width;
            if (!locked && onClick != null)
            {
                t.AddManipulator(new Clickable(() =>
                {
                    Game.Sound("click", 0.05f, -6f);
                    onClick();
                }));
                t.RegisterCallback<PointerEnterEvent>(_ => Game.Sound("hover", 0.05f, -12f));
            }
            return t;
        }

        /// <summary>Stempel oben rechts in einer Kachel ("GESPERRT", "AKTIV" ...).</summary>
        protected static Label Stamp(VisualElement parent, string text, bool good = false)
        {
            var s = UIX.Text(parent, text, "stamp");
            if (good)
            {
                s.style.color = Theme.LaptopGood;
                s.style.borderTopColor = Theme.LaptopGood;
                s.style.borderBottomColor = Theme.LaptopGood;
                s.style.borderLeftColor = Theme.LaptopGood;
                s.style.borderRightColor = Theme.LaptopGood;
            }
            return s;
        }

        /// <summary>Zeile mit gleich breiten Spalten (für Kennzahlen oder Karten nebeneinander).</summary>
        protected static VisualElement Columns(VisualElement parent, float gap = 12f) => UIX.Row(parent, gap);

        /// <summary>Kennzahl-Kachel, Farbe über Klasse, damit sie in beiden Laptop-Themes lesbar bleibt.</summary>
        protected static VisualElement Stat(VisualElement parent, string key, string value, string tone = null, string sub = null)
        {
            var s = UIX.Stat(parent, key, value, null, sub);
            if (!string.IsNullOrEmpty(tone)) s.Q<Label>(className: "stat-value").AddToClassList(tone + "-text");
            return s;
        }

        protected static VisualElement KV(VisualElement parent, string key, string value, string tone = null)
        {
            var r = UIX.KV(parent, key, value);
            if (!string.IsNullOrEmpty(tone)) r.Q<Label>(className: "kv-value").AddToClassList(tone + "-text");
            return r;
        }

        protected static VisualElement Grow(VisualElement el, float basis = 0f)
        {
            el.style.flexGrow = 1;
            el.style.flexShrink = 1;
            el.style.flexBasis = basis;
            return el;
        }

        /// <summary>Tabellenzeile mit festen Spaltenbreiten (negative Breite = flexibel).</summary>
        protected static VisualElement TableRow(VisualElement parent, float[] widths, string[] cells, bool head = false, string[] tones = null)
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
                if (!head && tones != null && i < tones.Length && !string.IsNullOrEmpty(tones[i])) l.AddToClassList(tones[i]);
            }
            return r;
        }

        protected static string LockText(int level) => "Ab Level " + level;

        protected static Button Btn(VisualElement parent, string text, Action onClick, string variant = "", bool disabled = false, string icon = null) =>
            UIX.Button(parent, text, onClick, variant, disabled, icon);
    }

    /// <summary>Platzhalter für noch gesperrte Apps.</summary>
    public sealed class AppLocked : LaptopApp
    {
        private readonly LaptopView.AppDef _def;
        private readonly string _reason;

        public AppLocked(LaptopView.AppDef def, string reason)
        {
            _def = def;
            _reason = reason;
        }

        public override void Build()
        {
            Header(_def.Title, "Diese App ist noch gesperrt.");
            var c = UIX.Card(Root);
            c.style.alignItems = Align.Center;
            c.style.paddingTop = 40;
            c.style.paddingBottom = 40;
            UIX.Icon(c, "lock", 46f);
            UIX.Text(c, _reason, "h2").style.marginTop = 12;
            string hint = _def.Id == "trading"
                ? "Mit dem Trading-Konto kannst du dein Geld in Krypto, Meme-Aktien und einen ETF stecken. Riskant, aber manchmal lohnend."
                : "Mitarbeiter packen, lagern und verschicken automatisch. Dafür brauchst du die Lagerhalle und etwas Erfahrung.";
            var p = P(c, hint);
            p.style.maxWidth = 520;
            p.style.unityTextAlign = TextAnchor.MiddleCenter;
            p.style.marginTop = 6;
            Btn(c, "Ziele ansehen", () => View.OpenApp("goals"), "soft", false, "trophy").style.marginTop = 16;
        }
    }

    /// <summary>
    /// Übersicht: aktuelles Ziel, die wichtigsten Kennzahlen, eine Aufgabenliste mit allem,
    /// was gerade Aufmerksamkeit braucht, und der Umsatzverlauf der letzten Tage.
    /// </summary>
    public sealed class AppHome : LaptopApp
    {
        public override void Build()
        {
            var s = S;
            int hour = (int)(s.TimeMinutes / 60f);
            string greet = hour < 11 ? "Guten Morgen" : (hour < 17 ? "Hallo" : "Guten Abend");
            Header(greet + ", " + s.BrandName + ".", "Tag " + s.Day + " · " + GameData.StageNames[s.LocationStage] + " · Firmenlevel " + s.Level);

            var obj = s.CurrentObjective();
            var oc = UIX.Card(Root, null, "card-hi");
            var oh = UIX.Row(oc, 10f);
            UIX.Icon(oh, "trophy", 20f);
            UIX.Text(oh, obj.Title, "h3");
            P(oc, obj.Text, "");
            if (obj.Progress >= 0f) UIX.Bar(oc, obj.Progress, Theme.LaptopAccent, 8f);

            var st = Columns(Root);
            Stat(st, "KONTOSTAND", Fmt.Money(s.Money), s.Money < 0 ? "bad" : null, s.Debt > 0 ? "Kredit: " + Fmt.Money(s.Debt) : null);
            Stat(st, "UMSATZ HEUTE", Fmt.Money(s.Daily.Revenue), "good", s.Daily.Shipped + " Pakete verschickt");
            Stat(st, "OFFENE BESTELLUNGEN", s.PendingCount() + " / " + s.QueueCapacity(), s.PendingCount() >= s.QueueCapacity() ? "bad" : null,
                s.Daily.Lost > 0 ? s.Daily.Lost + " heute verloren" : "keine verloren");
            Stat(st, "BEWERTUNG", Fmt.Rating(s.Reputation) + " ★", "accent", s.ReviewCount + " Bewertungen");

            var h = UIX.Row(Root, 14f);
            h.style.alignItems = Align.FlexStart;
            var left = Grow(UIX.Col(h, 10f), 0f);
            var todo = UIX.Card(left, "Zu erledigen");
            int n = BuildTodos(todo);
            if (n == 0) P(todo, "Alles im Griff. Zeit für Marketing oder einen Kaffee.");

            var right = Grow(UIX.Col(h, 10f), 0f);
            var cc = UIX.Card(right, "Umsatz der letzten Tage");
            var chart = new LineChart { IncludeZero = true, Suffix = " €" };
            chart.style.height = 150;
            var rev = new LineChart.Series { Color = Theme.LaptopAccent, Label = "Umsatz" };
            int from = Math.Max(0, s.History.Count - 10);
            for (int i = from; i < s.History.Count; i++) rev.Values.Add(s.History[i].Revenue);
            rev.Values.Add(s.Daily.Revenue);
            chart.Set(rev);
            cc.Add(chart);
            if (s.History.Count == 0) P(cc, "Der Verlauf füllt sich mit jedem Tag.", "small");

            var q = UIX.Card(right, "Schnellzugriff");
            var w = UIX.Wrap(q);
            QuickLink(w, "Ware kaufen", "cart", "buy");
            QuickLink(w, "Preise", "globe", "shop");
            QuickLink(w, "Werbung", "mega", "marketing");
            QuickLink(w, "Ausbau", "building", "build");
        }

        private void QuickLink(VisualElement parent, string text, string icon, string app) =>
            Btn(parent, text, () => View.OpenApp(app), "soft", false, icon);

        private int BuildTodos(VisualElement card)
        {
            var s = S;
            int n = 0;

            void Todo(string icon, string text, string app, bool urgent = false)
            {
                n++;
                var r = UIX.Row(card, 10f, "mail-item");
                if (urgent) UIX.Div(r, "pending-dot");
                UIX.Icon(r, icon, 16f);
                Grow(UIX.Text(r, text));
                if (app != null)
                {
                    UIX.Icon(r, "chevron", 12f);
                    r.AddManipulator(new Clickable(() => View.OpenApp(app)));
                }
            }

            int pending = s.Events.PendingCount();
            if (pending > 0) Todo("mail", pending == 1 ? "1 Entscheidung wartet im Postfach" : pending + " Entscheidungen warten im Postfach", "mail", true);
            if (s.DockCrates.Count > 0) Todo("box", s.DockCrates.Count + " Kiste(n) am Wareneingang einräumen", null);
            if (s.PackedPackages.Count > 0) Todo("truck", s.PackedPackages.Count + " Paket(e) etikettieren und verschicken", null);
            if (s.TravelingDeliveries.Count > 0)
            {
                var d = s.TravelingDeliveries[0];
                Todo("clock", "Lieferung unterwegs: " + d.Quantity + "× " + GameData.Product(d.Product).Name + " in " + s.EtaText(d), "buy");
            }
            foreach (var p in GameData.Products)
            {
                if (!s.IsListed(p.Id)) continue;
                int stock = s.StockQty(p.Id) + s.TravelingCountFor(p.Id) + s.DockCountFor(p.Id);
                if (stock < 5) Todo("warning", p.Name + ": fast ausverkauft – nachbestellen", "buy", stock == 0);
            }
            int pack = s.Packaging[0] + s.Packaging[1] + s.Packaging[2] + s.FlatTotal();
            if (pack < 6) Todo("package", "Kartons werden knapp (" + pack + " übrig)", "pack", pack == 0);
            bool anyListed = false;
            foreach (var p in GameData.Products)
            {
                if (s.IsListed(p.Id)) anyListed = true;
                else if (s.ProductAvailable(p.Id) && s.StockQty(p.Id) > 0) Todo("globe", p.Name + " liegt im Lager, ist aber nicht online", "shop");
            }
            if (!anyListed && s.StoryStage == "business") Todo("globe", "Kein Produkt online – stell etwas in den Webshop", "shop", true);
            if (s.TikTokAvailable()) Todo("music", "Ein neues TikTok ist möglich", "marketing");
            if (!s.BrandNamed) Todo("tag", "Gib deiner Marke einen Namen", "brand");
            if (s.HasUpgrade("stand") && s.StandTotal() == 0) Todo("store", "Der Verkaufsstand ist leer – stell eine Kiste drauf", null);
            if (s.Money < 0) Todo("bank", "Konto im Minus – ab " + Fmt.Money(GameData.BankruptLimit) + " droht die Pleite", "bank", true);
            return n;
        }
    }
}
