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
        protected static Label Section(VisualElement parent, string text) => UIX.Text(parent, text.ToUpperInvariant(), "section");

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
            if (!string.IsNullOrEmpty(title)) UIX.CardHead(c, title.ToUpperInvariant(), right);
            return c;
        }

        /// <summary>Anklickbare Auswahlkachel (Produkt, Lieferant, Asset ...).</summary>
        protected static Button Tile(VisualElement parent, bool selected, bool locked, Action onClick, float width = -1f, params string[] classes) =>
            UIX.Tile(parent, onClick, selected, locked, width, classes);

        /// <summary>Stempel oben rechts in einer Kachel ("GESPERRT", "AKTIV" ...).</summary>
        protected static Label Stamp(VisualElement parent, string text, bool good = false)
        {
            var s = UIX.Text(parent, text, "stamp");
            s.style.position = Position.Absolute;
            if (good) s.AddToClassList("good");
            s.pickingMode = PickingMode.Ignore;
            return s;
        }

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

        protected static VisualElement KV(VisualElement parent, string key, string value, string tone = null)
        {
            var r = UIX.KV(parent, key, value);
            if (!string.IsNullOrEmpty(tone)) r.Q<Label>(className: "kv-value").AddToClassList(tone + "-text");
            return r;
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

    /// <summary>
    /// Übersicht: Umsatz heute mit Verlauf, offene Bestellungen, Reichweite/Boosts, nächstes Ziel
    /// und eine Aufgabenliste mit allem, was gerade Aufmerksamkeit braucht.
    /// </summary>
    public sealed class AppHome : LaptopApp
    {
        public override string Lead
        {
            get
            {
                var s = S;
                if (s == null) return null;
                int hour = (int)(s.TimeMinutes / 60f);
                string greet = hour < 11 ? "Guten Morgen" : (hour < 17 ? "Hey" : "Guten Abend");
                return greet + ", " + s.BrandName + ". " + UiFmt.DayLong(s.Day) + " · " + GameData.StageNames[Mathf.Clamp(s.LocationStage, 0, GameData.StageNames.Length - 1)] +
                       " · " + UiFmt.UntilClosing(s) + ".";
            }
        }

        public override void Build()
        {
            var s = S;
            if (s == null) return;

            // ---- Reihe 1: Umsatz + Bestellungen -------------------------------------------------
            var r1 = Columns(Root, 14f);
            var hero = Grow(Card(r1, null, null, "hero"), 0f);
            hero.style.flexGrow = 7;
            var hh = UIX.Row(hero, 8f, "card-head");
            UIX.Text(hh, "UMSATZ HEUTE", "card-title");
            UIX.Spacer(hh);
            if (s.History.Count > 0)
            {
                int y = s.History[s.History.Count - 1].Revenue;
                if (y > 0)
                {
                    float pct = (s.Daily.Revenue - y) / (float)y;
                    var d = UIX.Chip(hh, (pct >= 0 ? "+" : "−") + Mathf.RoundToInt(Mathf.Abs(pct) * 100f) + " % zu gestern", pct >= 0 ? "trend" : "trend_down",
                        pct >= 0 ? Theme.LaptopGood : Theme.LaptopBad, "delta");
                    if (pct < 0) d.AddToClassList("down");
                }
            }
            UIX.Num(hero, Fmt.Money(s.Daily.Revenue), true, "hero-value");
            P(hero, s.Daily.Shipped + " Pakete verschickt · " + (s.Daily.Lost > 0 ? s.Daily.Lost + " Bestellung(en) verloren" : "keine Bestellung verloren"));
            var values = new List<float>();
            int from = Math.Max(0, s.History.Count - 9);
            for (int i = from; i < s.History.Count; i++) values.Add(s.History[i].Revenue);
            values.Add(s.Daily.Revenue);
            if (values.Count >= 2) hero.Add(new Sparkline(values, Theme.LaptopAccent));
            else P(hero, "Der Verlauf füllt sich mit jedem Tag.", "small");
            var minis = UIX.Row(hero, 10f);
            minis.style.alignItems = Align.Stretch;
            Mini(minis, "Bewertung", Fmt.Rating(s.Reputation), m => UIX.Stars(m, s.Reputation, 11f, Theme.LaptopAccent));
            Mini(minis, "Level " + s.Level, s.Level >= GameData.MaxLevel ? "MAX" : UiFmt.Percent(s.LevelProgress()), m => UIX.Bar(m, s.LevelProgress(), Theme.LaptopAccent, 5f).style.marginTop = 4);
            Mini(minis, "Lager", Fmt.Thousands(s.StockTotal()) + "/" + Fmt.Thousands(s.Capacity()), null);

            var orders = Grow(Card(r1, "Offene Bestellungen", s.PendingCount() + " / " + s.QueueCapacity()), 0f);
            orders.style.flexGrow = 5;
            if (s.OrderQueue.Count == 0)
            {
                bool anyListed = false;
                foreach (var p in GameData.Products)
                    if (s.IsListed(p.Id)) anyListed = true;
                UIX.Empty(orders, "cart", "Gerade keine Bestellungen", anyListed ? "Mehr Reichweite = mehr Bestellungen. Marketing hilft." : "Stell ein Produkt im Shop online.");
            }
            else
            {
                var feed = UIX.Col(orders, 8f);
                int n = 0;
                foreach (var o in s.OrderQueue)
                {
                    if (++n > 5) break;
                    var v = OrderInfo.Get(s, o);
                    var item = UIX.Row(feed, 10f, "feed-item");
                    UIX.Swatch(item, v.Product.Color.ToColor(), 34f, v.Product.Icon);
                    var mid = Grow(UIX.Col(item, 0f));
                    UIX.Ellipsis(UIX.Text(mid, v.Product.Name, "feed-title"));
                    var sub = UIX.Text(mid, v.Tone == "late" ? "überfällig · wartet " + UiFmt.Duration(v.Age) : "wartet " + UiFmt.Duration(v.Age), "feed-sub");
                    if (v.Tone == "late") sub.AddToClassList("bad-text");
                    UIX.Num(item, Fmt.Money(v.Price), true);
                }
                if (s.OrderQueue.Count > 5) P(orders, "+" + (s.OrderQueue.Count - 5) + " weitere", "small");
            }

            // ---- Reihe 2: Reichweite + Ziel ------------------------------------------------------
            var r2 = Columns(Root, 14f);
            float mult = 1f;
            foreach (var b in s.Boosts) mult *= b.Mult;
            var reach = Grow(Card(r2, "Reichweite", "×" + Fmt.Dec(mult, 1)), 0f);
            var ar = UIX.Row(reach, 8f);
            P(ar, "Bekanntheit deiner Marke", "");
            UIX.Spacer(ar);
            UIX.Num(ar, UiFmt.Percent(s.Awareness / 1.5f), true);
            UIX.Bar(reach, s.Awareness / 1.5f, Theme.LaptopAccent, 10f);
            if (s.Boosts.Count == 0) P(reach, "Keine Kampagne aktiv.", "small");
            foreach (var b in s.Boosts)
            {
                var br = UIX.Row(reach, 8f, "feed-item");
                UIX.Icon(br, b.Source == "tiktok" ? "fire" : "bolt", 16f, Theme.LaptopAccent);
                Grow(UIX.Ellipsis(UIX.Text(br, b.Name + "  ×" + Fmt.Dec(b.Mult, 1), "feed-title")));
                UIX.Num(br, "noch " + UiFmt.Duration(b.EndsAt - s.BClock()), false, "feed-sub");
            }
            if (s.TikTokAvailable()) Btn(reach, "TikTok drehen", () => Go("shop/marketing"), "accent", false, "music").style.alignSelf = Align.FlexStart;
            else Btn(reach, "Marketing", () => Go("shop/marketing"), "soft", false, "mega").style.alignSelf = Align.FlexStart;

            var goal = Grow(Card(r2, s.TutorialStep >= 0 ? "Tutorial" : "Nächstes Ziel"), 0f);
            var obj = s.CurrentObjective();
            string title = obj.Title ?? "";
            if (title.StartsWith("Ziel: ")) title = title.Substring(6);
            UIX.Text(goal, title, "h2");
            P(goal, (obj.Text ?? "").Replace("oben links", "oben rechts"), "");
            if (obj.Progress >= 0f)
            {
                var gr = UIX.Row(goal, 10f);
                UIX.Bar(gr, obj.Progress, Theme.LaptopAccent, 10f);
                UIX.Num(gr, UiFmt.Percent(obj.Progress), true);
            }
            var ng = s.NextGoal();
            var gb = UIX.Row(goal, 8f);
            if (s.TutorialStep < 0 && ng != null) UIX.Chip(gb, "Belohnung +" + Fmt.Money(ng.Reward), "coin", Theme.LaptopAccent);
            UIX.Spacer(gb);
            Btn(gb, "Alle Ziele", () => Go("company/goals"), "ghost", false, "trophy");

            // ---- Reihe 3: Aufgaben --------------------------------------------------------------
            var todo = Card(Root, "Zu erledigen");
            int count = BuildTodos(todo);
            if (count == 0) UIX.Empty(todo, "check_circle", "Alles im Griff", "Zeit für Marketing – oder einen Kaffee.");

            var q = UIX.Row(Root, 8f);
            q.style.flexWrap = Wrap.Wrap;
            Btn(q, "Ware kaufen", () => Go("buy/ware"), "soft", false, "cart");
            Btn(q, "Preise", () => Go("shop/webshop"), "soft", false, "tag");
            Btn(q, "Werbung", () => Go("shop/marketing"), "soft", false, "mega");
            Btn(q, "Finanzen", () => Go("finance/bank"), "soft", false, "bank");
            Btn(q, "Ausbau", () => Go("company/build"), "soft", false, "building");
        }

        private static void Mini(VisualElement parent, string key, string value, Action<VisualElement> extra)
        {
            var m = UIX.Col(parent, 2f, "mini");
            UIX.Text(m, key, "mini-key");
            UIX.Text(m, value, "mini-value");
            extra?.Invoke(m);
        }

        private int BuildTodos(VisualElement card)
        {
            var s = S;
            int n = 0;
            var list = UIX.Col(card, 0f);

            void Todo(string icon, string text, string app, bool urgent = false)
            {
                n++;
                VisualElement r;
                if (app != null)
                {
                    string target = app;
                    r = UIX.PressRow(list, 10f, () => Go(target), "todo-item");
                }
                else r = UIX.Row(list, 10f, "todo-item");
                var dot = UIX.Div(r, "pending-dot");
                dot.EnableInClassList("urgent", urgent);
                UIX.Icon(r, icon, 16f);
                UIX.Text(r, text, "todo-text");
                if (app != null) UIX.Icon(r, "chevron", 12f);
                if (r is Button) UIX.PassThrough(r);
            }

            int pending = s.Events.PendingCount();
            if (pending > 0) Todo("mail", pending == 1 ? "1 Entscheidung wartet im Postfach" : pending + " Entscheidungen warten im Postfach", "mail", true);
            if (s.DockCrates.Count > 0) Todo("box", s.DockCrates.Count + " Kiste(n) am Wareneingang einräumen", null);
            if (s.PackedPackages.Count > 0) Todo("truck", s.PackedPackages.Count + " Paket(e) etikettieren und verschicken", null);
            if (s.TravelingDeliveries.Count > 0)
            {
                var d = s.TravelingDeliveries[0];
                Todo("clock", "Lieferung unterwegs: " + d.Quantity + "× " + GameData.Product(d.Product).Name + " in " + s.EtaText(d), "buy/ware");
            }
            foreach (var p in GameData.Products)
            {
                if (!s.IsListed(p.Id)) continue;
                int stock = s.StockQty(p.Id) + s.TravelingCountFor(p.Id) + s.DockCountFor(p.Id);
                if (stock < 5) Todo("warning", p.Name + ": fast ausverkauft – nachbestellen", "buy/ware", stock == 0);
            }
            int pack = s.Packaging[0] + s.Packaging[1] + s.Packaging[2] + s.FlatTotal();
            if (pack < 6) Todo("package", "Kartons werden knapp (" + pack + " übrig)", "buy/pack", pack == 0);
            bool anyListed = false;
            foreach (var p in GameData.Products)
            {
                if (s.IsListed(p.Id)) anyListed = true;
                else if (s.ProductAvailable(p.Id) && s.StockQty(p.Id) > 0) Todo("globe", p.Name + " liegt im Lager, ist aber nicht online", "shop/webshop");
            }
            if (!anyListed && s.StoryStage == "business") Todo("globe", "Kein Produkt online – stell etwas in den Webshop", "shop/webshop", true);
            if (s.TikTokAvailable()) Todo("music", "Ein neues TikTok ist möglich", "shop/marketing");
            if (!s.BrandNamed) Todo("tag", "Gib deiner Marke einen Namen", "shop/brand");
            if (s.HasUpgrade("stand") && s.StandTotal() == 0) Todo("store", "Der Verkaufsstand ist leer – stell eine Kiste drauf", null);
            if (s.Money < 0) Todo("bank", "Konto im Minus – ab " + Fmt.Money(GameData.BankruptLimit) + " droht die Pleite", "finance/bank", true);
            return n;
        }
    }
}
