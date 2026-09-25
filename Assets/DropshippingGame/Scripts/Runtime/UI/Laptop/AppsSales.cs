using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Webshop: Produkte online stellen, Preise setzen, Rabattaktion, Bewertungen.</summary>
    public sealed class AppShop : LaptopApp
    {
        public override void Build()
        {
            var s = S;
            Header(s.BrandName + " – Webshop", "Stell Produkte online, setz Preise und behalte deine Bewertungen im Blick.");
            var top = Columns(Root);
            Stat(top, "BEWERTUNG", Fmt.Rating(s.Reputation) + " ★", "accent", s.ReviewCount + " Bewertungen");
            Stat(top, "OFFENE BESTELLUNGEN", s.PendingCount() + " / " + s.QueueCapacity());
            float interval = s.OrderIntervalMinutes();
            Stat(top, "BESTELLUNGEN / STUNDE", interval > 0f ? "~" + Fmt.Dec(60f / interval, 1) : "—");
            Stat(top, "VERKAUFT HEUTE", s.Daily.Shipped.ToString(), "good");

            var pc = UIX.Card(Root);
            UIX.Toggle(pc, "Rabattaktion: −" + Mathf.RoundToInt(GameData.PromoDiscount * 100f) + " % auf alle Preise (lockt mehr Kunden an, kleinere Marge)", s.PromoActive, on => s.SetPromoActive(on));
            if (s.ShopOfflineUntil > s.BClock())
                UIX.Text(pc, "Dein Shop ist gerade offline (Serverprobleme). Noch " + Mathf.CeilToInt(s.ShopOfflineUntil - s.BClock()) + " min.", "bad-text");

            Section(Root, "Sortiment");
            var list = UIX.Col(Root, 8f);
            foreach (var p in GameData.Products) ProductRow(list, p);

            var rc = UIX.Card(Root, "Neueste Bewertungen");
            if (s.Reviews.Count == 0) P(rc, "Noch keine Bewertungen – verschick deine ersten Pakete!");
            int n = 0;
            foreach (var r in s.Reviews)
            {
                if (++n > 8) break;
                var rh = UIX.Row(rc, 12f, "table-row");
                rh.style.alignItems = Align.FlexStart;
                var st = UIX.Stars(rh, r.Stars, 13f, Theme.LaptopAccent);
                st.style.width = 80;
                st.style.marginTop = 2;
                var rv = Grow(UIX.Col(rh, 1f));
                UIX.Text(rv, "„" + r.Text + "“");
                string prod = GameData.IsProduct(r.Product) ? GameData.Product(r.Product).Name : r.Product;
                UIX.Text(rv, r.Name + " · " + prod + " · Tag " + r.Day, "small");
            }
        }

        private void ProductRow(VisualElement parent, ProductDef p)
        {
            var s = S;
            var c = UIX.Card(parent);
            var h = UIX.Row(c, 14f);
            UIX.Swatch(h, p.Color.ToColor(), 44f, p.Icon);
            var v = UIX.Col(h, 4f);
            v.style.width = 200;
            UIX.Text(v, p.Name, "h3");
            if (!s.ProductAvailable(p.Id))
            {
                UIX.Text(v, !s.ProductUnlocked(p.Id) ? "Ab Firmenlevel " + p.UnlockLevel : "Braucht die Lagerhalle", "small");
                c.style.opacity = 0.55f;
                return;
            }
            string id = p.Id;
            UIX.Toggle(v, s.IsListed(id) ? "Online" : "Offline", s.IsListed(id), on => s.SetListed(id, on));

            int price = s.ShopPrices.TryGetValue(id, out int pr) ? pr : p.RefPrice;
            var pv = UIX.Col(h, 4f);
            var prow = UIX.Row(pv, 4f);
            Btn(prow, "−5", () => s.SetShopPrice(id, price - 5), "soft");
            Btn(prow, "−1", () => s.SetShopPrice(id, price - 1), "soft");
            var pl = UIX.Text(prow, Fmt.Money(price), "h2", "accent-text");
            pl.style.minWidth = 86;
            pl.style.unityTextAlign = TextAnchor.MiddleCenter;
            Btn(prow, "+1", () => s.SetShopPrice(id, price + 1), "soft");
            Btn(prow, "+5", () => s.SetShopPrice(id, price + 5), "soft");
            float market = s.Market.MarketPrice(id);
            float margin = s.CurrentSalePrice(id) - p.UnitCost;
            var mr = UIX.Row(pv, 8f);
            UIX.Text(mr, "Markt " + Fmt.Money(market) + " · Marge ~" + Fmt.Money(margin) + "/Stk", "small");
            int target = Mathf.Max(1, Mathf.RoundToInt(market * 0.97f));
            if (target != price)
            {
                var b = Btn(mr, "Knapp unter Markt", () => s.SetShopPrice(id, target), "ghost");
                b.style.paddingTop = 2;
                b.style.paddingBottom = 2;
            }

            UIX.Spacer(h);
            var dv = UIX.Col(h, 4f);
            dv.style.width = 190;
            UIX.Text(dv, "Nachfrage: " + s.DemandLabel(id));
            UIX.Bar(dv, Mathf.Min(s.DemandRate(id), 3f) / 3f, Theme.LaptopTeal, 8f, 180f);
            int sold = s.ShippedPerProduct.TryGetValue(id, out int n) ? n : 0;
            UIX.Text(dv, s.PendingCountFor(id) + " offen · " + sold + " verkauft · Lager " + s.StockQty(id), "small");
        }
    }

    /// <summary>Marketing: Werbekampagnen, TikTok-Minispiel und Bekanntheit der Marke.</summary>
    public sealed class AppMarketing : LaptopApp
    {
        private TikTokPhone _phone;

        public override bool CanRebuild() => _phone == null || !_phone.Busy;

        public override void Build()
        {
            _phone = null;
            var s = S;
            Header("Marketing", "Mehr Reichweite = mehr Bestellungen. Kampagnen wirken zeitlich begrenzt, die Bekanntheit deiner Marke bleibt teilweise.");
            var aw = UIX.Card(Root);
            var ah = UIX.Row(aw, 8f);
            UIX.Text(ah, "Bekanntheit deiner Marke", "h3");
            UIX.Spacer(ah);
            UIX.Text(ah, Mathf.RoundToInt(s.Awareness / 1.5f * 100f) + " %", "h3", "accent-text");
            UIX.Bar(aw, s.Awareness / 1.5f, Theme.LaptopTeal, 10f);
            if (s.Boosts.Count > 0)
            {
                var bc = UIX.Card(Root, "Gerade aktiv", "card-hi");
                foreach (var b in s.Boosts)
                {
                    string what = string.IsNullOrEmpty(b.Product) ? "" : " (" + GameData.Product(b.Product).Short + ")";
                    KV(bc, b.Name + what, "×" + Fmt.Dec(b.Mult, 1) + " · noch " + Mathf.Max(0, Mathf.RoundToInt(b.EndsAt - s.BClock())) + " min", b.Mult >= 1f ? "good" : "bad");
                }
            }

            var h = UIX.Row(Root, 16f);
            h.style.alignItems = Align.FlexStart;
            var left = Grow(UIX.Col(h, 10f));
            Section(left, "Werbekampagnen");
            bool running = s.ActiveBoost("ad") != null;
            for (int i = 0; i < GameData.AdTiers.Length; i++)
            {
                var t = GameData.AdTiers[i];
                var c = UIX.Card(left);
                var ch = UIX.Row(c, 12f);
                UIX.Swatch(ch, Theme.LaptopAccent, 42f, t.Icon);
                var v = Grow(UIX.Col(ch, 2f));
                UIX.Text(v, t.Name, "h3");
                UIX.Text(v, "×" + Fmt.Dec(t.Mult, 1) + " Nachfrage · " + (int)t.Minutes + " min · +Bekanntheit", "small");
                bool locked = s.Level < t.Level;
                int idx = i;
                string txt = locked ? LockText(t.Level) : (running ? "Läuft …" : "Starten · " + Fmt.Money(t.Cost));
                Btn(ch, txt, () => s.StartAdCampaign(idx), locked || running ? "" : "accent", locked || running || s.Money < t.Cost, locked ? "lock" : null);
            }

            var right = UIX.Col(h, 10f);
            right.style.width = 270;
            right.style.flexShrink = 0;
            Section(right, "TikTok");
            if (s.Level < GameData.TikTokLevel)
            {
                var lc = UIX.Card(right);
                UIX.Icon(lc, "lock", 26f);
                P(lc, "TikTok schaltet sich ab Firmenlevel " + GameData.TikTokLevel + " frei.", "");
            }
            else if (!s.TikTokAvailable())
            {
                var lc = UIX.Card(right);
                if (s.ActiveBoost("tiktok") != null)
                {
                    UIX.Icon(lc, "fire", 26f, Theme.LaptopAccent);
                    P(lc, "Dein Trend läuft gerade! Genieß die Bestellungen.", "");
                }
                else P(lc, "Nächstes Video in " + Mathf.CeilToInt(s.TikTokReadyAt - s.BClock()) + " Minuten möglich – der Algorithmus braucht Pause.", "");
            }
            else
            {
                _phone = new TikTokPhone();
                _phone.Posted += score => S.TriggerTikTok(score);
                right.Add(_phone);
                P(right, "Starte die Aufnahme und stopp die Markierung im grünen Bereich. Je genauer, desto viraler.", "small");
            }
        }
    }

    /// <summary>Branding: Markenname, Logo und Farbe – mit Live-Vorschau des Kartons.</summary>
    public sealed class AppBranding : LaptopApp
    {
        /// <summary>true, solange ein Textfeld im Laptop den Fokus hat (Tastenkürzel pausieren).</summary>
        public static bool Typing;
        private TextField _name;

        public override bool CanRebuild() => !Typing;

        public override void Closed() => Typing = false;

        public override void Build()
        {
            Typing = false;
            var s = S;
            Header("Branding", "Deine Marke: Name, Logo und Farbe. Erscheint auf neuen Kartons, im Webshop und groß an deinem Gebäude.");
            var h = UIX.Row(Root, 18f);
            h.style.alignItems = Align.FlexStart;
            var left = Grow(UIX.Col(h, 10f));

            var nc = UIX.Card(left, "Markenname");
            var nh = UIX.Row(nc, 8f);
            _name = new TextField { value = s.BrandName, maxLength = 22 };
            _name.AddToClassList("ds-input");
            Grow(_name);
            _name.RegisterCallback<FocusInEvent>(_ => Typing = true);
            _name.RegisterCallback<FocusOutEvent>(_ => Typing = false);
            _name.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) SaveName();
            }, TrickleDown.TrickleDown);
            nh.Add(_name);
            Btn(nh, "Speichern", SaveName, "accent", false, "check");

            var lc = UIX.Card(left, "Logo");
            var lw = UIX.Wrap(lc);
            for (int i = 0; i < GameData.LogoNames.Length; i++)
            {
                int idx = i;
                var t = Tile(lw, i == s.BrandLogoIndex, false, () => s.SetBrandLogo(idx), 88f);
                t.style.alignItems = Align.Center;
                t.Add(new BrandPreview(i, s.BrandColor.ToColor(), "", true));
                UIX.Text(t, GameData.LogoNames[i], "tile-sub");
            }

            var cc = UIX.Card(left, "Farbe");
            var cw = UIX.Wrap(cc);
            foreach (var col in GameData.BrandPalette)
            {
                var c = col;
                bool selected = c.Approx(s.BrandColor);
                var sw = UIX.Swatch(cw, c.ToColor(), 40f, selected ? "check" : null);
                sw.style.borderTopLeftRadius = 8;
                sw.style.borderTopRightRadius = 8;
                sw.style.borderBottomLeftRadius = 8;
                sw.style.borderBottomRightRadius = 8;
                float bw = selected ? 3f : 1f;
                var bcol = selected ? Theme.LaptopAccent : new Color(0.5f, 0.5f, 0.5f, 0.4f);
                sw.style.borderTopWidth = bw;
                sw.style.borderBottomWidth = bw;
                sw.style.borderLeftWidth = bw;
                sw.style.borderRightWidth = bw;
                sw.style.borderTopColor = bcol;
                sw.style.borderBottomColor = bcol;
                sw.style.borderLeftColor = bcol;
                sw.style.borderRightColor = bcol;
                sw.AddManipulator(new Clickable(() =>
                {
                    Game.Sound("click", 0.05f, -6f);
                    s.SetBrandColor(c);
                }));
            }

            var right = UIX.Col(h, 10f);
            right.style.flexShrink = 0;
            Section(right, "Vorschau");
            var pc = UIX.Card(right);
            pc.Add(new BrandPreview(s.BrandLogoIndex, s.BrandColor.ToColor(), s.BrandName));
            var tip = P(right, "Tipp: Nur neu gekaufte Kartons (App „Verpackung“) bekommen dieses Design. Das Schild am Gebäude ändert sich sofort.", "small");
            tip.style.maxWidth = 300;
        }

        private void SaveName()
        {
            if (_name == null) return;
            string before = S.BrandName;
            S.SetBrandName(_name.value);
            Typing = false;
            _name.Blur();
            if (S.BrandName != before) S.Notify("Marke gespeichert: " + S.BrandName, "good");
            Rebuild();
        }
    }

    /// <summary>Marktanalyse: Konkurrenzpreise je Produkt, Marktpreis und Preiskämpfe.</summary>
    public sealed class AppMarket : LaptopApp
    {
        public override void Build()
        {
            var s = S;
            var m = s.Market;
            Header("Marktanalyse", "Der günstigste Konkurrenzpreis ist der Marktpreis. Liegst du darunter, steigt deine Nachfrage deutlich.");
            var cr = Columns(Root);
            cr.style.alignItems = Align.Stretch;
            foreach (var c in Market.Competitors)
            {
                var cc = Grow(UIX.Card(cr));
                UIX.Text(cc, c.Name, "h3");
                UIX.Text(cc, c.Desc, "small");
                UIX.Text(cc, "Preisniveau ×" + Fmt.Dec(c.Factor, 2), "small");
            }
            if (m.CompMods.Count > 0)
            {
                var wc = UIX.Card(Root, null, "card-hi");
                foreach (var kv in m.CompMods)
                {
                    var r = UIX.Row(wc, 8f);
                    UIX.Icon(r, "warning", 16f, Theme.LaptopBad);
                    UIX.Text(r, "Preiskampf: BilligBoy24 unterbietet bei " + GameData.Product(kv.Key).Name + " bis Tag " + kv.Value.Until, "bad-text");
                }
            }

            var tc = UIX.Card(Root);
            float[] w = { -1f, 90f, 96f, 90f, 96f, 96f, 110f };
            TableRow(tc, w, new[] { "PRODUKT", "DEIN PREIS", "BILLIGBOY24", "TRENDHAUS", "ALIEXPRESSO", "MARKTPREIS", "NACHFRAGE" }, true);
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                int mine = s.CurrentSalePrice(p.Id);
                float market = m.MarketPrice(p.Id);
                var cells = new[]
                {
                    p.Name, Fmt.Money(mine),
                    Fmt.Money(m.CompetitorPrice(0, p.Id)), Fmt.Money(m.CompetitorPrice(1, p.Id)), Fmt.Money(m.CompetitorPrice(2, p.Id)),
                    Fmt.Money(market), s.IsListed(p.Id) ? s.DemandLabel(p.Id) : "offline",
                };
                var tones = new[] { "", mine <= market ? "good-text" : "bad-text", "muted", "muted", "muted", "accent-text", "muted" };
                TableRow(tc, w, cells, false, tones);
            }
            var tip = UIX.Card(Root);
            var th = UIX.Row(tip, 10f);
            UIX.Icon(th, "bulb", 20f, Theme.LaptopAccent);
            Grow(P(th, "Faustregel: Knapp unter dem Marktpreis verkaufst du viel mit guter Marge. Premium-Ware bringt bessere Bewertungen – und gute Bewertungen bringen mehr Kunden.", ""));
        }
    }
}
