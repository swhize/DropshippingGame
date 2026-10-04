using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Shop › Webshop: Produkte online stellen, Preise setzen, Rabattaktion, Bewertungen.</summary>
    public sealed class AppShop : LaptopApp
    {
        public override string Lead => "Deine Produkte, deine Preise. Knapp unter dem Marktpreis verkaufst du viel und behältst eine gute Marge.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "BEWERTUNG", Fmt.Rating(s.Reputation) + " / 5", "accent", s.ReviewCount + " Bewertungen");
            Stat(top, "OFFENE BESTELLUNGEN", s.PendingCount() + " / " + s.QueueCapacity(), s.PendingCount() >= s.QueueCapacity() ? "bad" : null);
            float interval = s.OrderIntervalMinutes();
            Stat(top, "BESTELLUNGEN / STUNDE", interval > 0f ? "~" + Fmt.Dec(60f / interval, 1) : "—");
            Stat(top, "VERKAUFT HEUTE", s.Daily.Shipped.ToString(), "good");

            var pc = UIX.Card(Root);
            UIX.Toggle(pc, "Rabattaktion: −" + Mathf.RoundToInt(GameData.PromoDiscount * 100f) + " % auf alle Preise (lockt mehr Kunden an, kleinere Marge)", s.PromoActive, on => S.SetPromoActive(on));
            if (s.ShopOfflineUntil > s.BClock())
            {
                var w = UIX.Row(pc, 8f);
                UIX.Icon(w, "warning", 16f, Theme.LaptopBad);
                UIX.Text(w, "Dein Shop ist gerade offline (Serverprobleme). Noch " + UiFmt.Duration(s.ShopOfflineUntil - s.BClock()) + ".", "bad-text");
            }

            float ipm = interval > 0f ? 60f / interval : 0f;
            var list = Card(Root, "Sortiment", ipm > 0f ? "~" + Fmt.Dec(ipm, 1) + " Bestellungen / Std" : "offline");
            bool first = true;
            foreach (var p in GameData.Products)
            {
                ProductRow(list, p, first);
                first = false;
            }

            var rc = Card(Root, "Neueste Bewertungen", s.ReviewCount > 0 ? s.ReviewCount + " insgesamt" : null);
            if (s.Reviews.Count == 0) UIX.Empty(rc, "star_o", "Noch keine Bewertungen", "Verschick deine ersten Pakete – schnell und in guter Qualität.");
            int n = 0;
            foreach (var r in s.Reviews)
            {
                if (++n > 8) break;
                var rh = UIX.Row(rc, 12f, "review-item");
                if (n == 1) rh.style.borderTopWidth = 0;
                var st = UIX.Stars(rh, r.Stars, 13f, Theme.LaptopAccent);
                st.style.width = 84;
                st.style.marginTop = 2;
                var rv = Grow(UIX.Col(rh, 1f));
                UIX.Text(rv, "„" + r.Text + "“");
                string prod = GameData.IsProduct(r.Product) ? GameData.Product(r.Product).Name : r.Product;
                UIX.Text(rv, r.Name + " · " + prod + " · Tag " + r.Day, "small");
            }
        }

        private void ProductRow(VisualElement parent, ProductDef p, bool first)
        {
            var s = S;
            var row = UIX.Row(parent, 14f, "listing");
            if (first) row.AddToClassList("first");
            UIX.Round(UIX.Swatch(row, p.Color.ToColor(), 52f, p.Icon), 14f);
            var v = UIX.Col(row, 4f);
            v.style.width = 210;
            v.style.flexShrink = 0;
            var nameRow = UIX.Row(v, 6f);
            UIX.Text(nameRow, p.Name, "h3");
            var tph = s.Trends.Phase(p.Id);
            if (tph == TrendPhase.Rising || tph == TrendPhase.Peak)
                UIX.Icon(nameRow, "fire", 15f, Theme.LaptopAccent).tooltip = "Trend: " + s.TrendLabel(p.Id) + " ×" + Fmt.Dec(s.TrendMult(p.Id), 1);
            else if (tph == TrendPhase.Falling || tph == TrendPhase.Dead)
                UIX.Icon(nameRow, "trend_down", 15f, Theme.LaptopBad).tooltip = "Trend: " + s.TrendLabel(p.Id);
            if (!s.ProductAvailable(p.Id))
            {
                UIX.Text(v, !s.ProductUnlocked(p.Id) ? "Ab Firmenlevel " + p.UnlockLevel : "Braucht die Lagerhalle", "small");
                row.style.opacity = 0.5f;
                return;
            }
            string id = p.Id;
            UIX.Toggle(v, s.IsListed(id) ? "Online" : "Offline", s.IsListed(id), on => S.SetListed(id, on));

            int price = s.ShopPrices.TryGetValue(id, out int pr) ? pr : p.RefPrice;
            var pv = UIX.Col(row, 6f);
            var stepper = UIX.Row(pv, 3f, "stepper");
            Step(stepper, "−5", () => S.SetShopPrice(id, price - 5));
            Step(stepper, "−1", () => S.SetShopPrice(id, price - 1));
            UIX.Num(stepper, Fmt.Money(price), true, "step-value");
            Step(stepper, "+1", () => S.SetShopPrice(id, price + 1));
            Step(stepper, "+5", () => S.SetShopPrice(id, price + 5));
            float market = s.Market.MarketPrice(id);
            float margin = s.CurrentSalePrice(id) - p.UnitCost;
            var mr = UIX.Row(pv, 8f);
            UIX.Text(mr, "Markt " + Fmt.Money(market) + " · Marge ~" + Fmt.Money(margin) + "/Stk", "small");
            int target = Mathf.Max(1, Mathf.RoundToInt(market * 0.97f));
            if (target != price) Btn(mr, "Knapp unter Markt", () => S.SetShopPrice(id, target), "ghost", false, "tag").AddToClassList("btn-sm");

            UIX.Spacer(row);
            var dv = UIX.Col(row, 4f);
            dv.style.width = 200;
            dv.style.flexShrink = 0;
            var dh = UIX.Row(dv, 6f);
            UIX.Text(dh, "Nachfrage", "small");
            UIX.Spacer(dh);
            UIX.Text(dh, s.DemandLabel(id), "bold");
            UIX.Bar(dv, Mathf.Min(s.DemandRate(id), 3f) / 3f, Theme.LaptopTeal, 8f);
            int sold = s.ShippedPerProduct.TryGetValue(id, out int n) ? n : 0;
            UIX.Text(dv, s.PendingCountFor(id) + " offen · " + sold + " verkauft · Lager " + s.StockQty(id), "small");
        }

        private static void Step(VisualElement parent, string text, Action onClick)
        {
            var b = UIX.Button(parent, text, onClick, "", false);
            b.AddToClassList("step-btn");
        }
    }

    /// <summary>Shop › Marketing: Werbekampagnen, TikTok-Aufnahme (Einstieg) und Bekanntheit der Marke.</summary>
    public sealed class AppMarketing : LaptopApp
    {
        public override string Lead => "Mehr Reichweite = mehr Bestellungen. Kampagnen wirken zeitlich begrenzt, die Bekanntheit deiner Marke bleibt teilweise.";

        public override void Build()
        {
            var s = S;
            var aw = Card(Root, "Bekanntheit deiner Marke", UiFmt.Percent(s.Awareness / 1.5f));
            UIX.Bar(aw, s.Awareness / 1.5f, Theme.LaptopTeal, 10f);
            if (s.Boosts.Count > 0)
            {
                var bc = Card(Root, "Gerade aktiv", null, "card-hi");
                foreach (var b in s.Boosts)
                {
                    string what = string.IsNullOrEmpty(b.Product) ? "" : " (" + GameData.Product(b.Product).Short + ")";
                    KV(bc, b.Name + what, "×" + Fmt.Dec(b.Mult, 1) + " · noch " + UiFmt.Duration(b.EndsAt - s.BClock()), b.Mult >= 1f ? "good" : "bad");
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
                UIX.Round(UIX.Swatch(ch, Theme.LaptopAccent, 44f, t.Icon), 14f);
                var v = Grow(UIX.Col(ch, 2f));
                UIX.Text(v, t.Name, "h3");
                string dur = t.Minutes >= 1440f ? "heute + morgen" : (t.Minutes >= 720f ? "Tageskampagne bis 20 Uhr" : UiFmt.Duration(t.Minutes));
                UIX.Text(v, "×" + Fmt.Dec(t.Mult, 1) + " Nachfrage · " + dur + " · +Bekanntheit", "small");
                bool locked = s.Level < t.Level;
                int idx = i;
                int cost = s.AdCost(i);
                string txt = locked ? LockText(t.Level) : (running ? "Läuft …" : "Starten · " + Fmt.Money(cost));
                Btn(ch, txt, () => S.StartAdCampaign(idx), locked || running ? "" : "accent", locked || running || s.Money < cost, locked ? "lock" : "play");
            }

            var right = UIX.Col(h, 10f);
            right.style.width = 280;
            right.style.flexShrink = 0;
            Section(right, "TikTok");
            if (s.Level < GameData.TikTokLevel)
            {
                var lc = UIX.Card(right);
                UIX.Empty(lc, "lock", "Ab Level " + GameData.TikTokLevel, "Dann kannst du hier virale Videos drehen.");
            }
            else if (s.TikToksLeftToday() <= 0)
            {
                var lc = UIX.Card(right);
                UIX.Empty(lc, "calendar", "Tageslimit erreicht", GameData.TikTokPerDay + " TikToks pro Tag – morgen geht's weiter.");
            }
            else if (!s.TikTokAvailable())
            {
                var lc = UIX.Card(right);
                if (s.ActiveBoost("tiktok") != null) UIX.Empty(lc, "fire", "Dein Trend läuft!", "Genieß die Bestellungen.");
                else UIX.Empty(lc, "hourglass", "Kurze Pause", "Nächstes Video in " + UiFmt.Duration(s.TikTokReadyAt - s.BClock()) + " – der Algorithmus braucht Ruhe.");
            }
            else
            {
                TikTokStudio.EnsureSelection(s);
                var tc = UIX.Card(right);
                string rb = TikTokStudio.RecordBlocker(s);
                if (rb != null) UIX.Empty(tc, "box", "Nichts zum Filmen", rb);
                else
                {
                    TikTokStudio.BuildPicker(tc, s, Rebuild, false);
                    Btn(tc, "Video aufnehmen", () => TikTokStudio.StartRecording(), "accent", false, "play");
                }
                P(right, "Heute noch " + s.TikToksLeftToday() + " von " + GameData.TikTokPerDay + " TikToks. Ein Produkt mit Hype wirkt stärker.", "small");
            }
        }
    }

    /// <summary>Shop › Branding: Markenname, Logo und Farbe – mit Live-Vorschau des Kartons.</summary>
    public sealed class AppBranding : LaptopApp
    {
        /// <summary>true, solange ein Textfeld im Laptop den Fokus hat (Tastenkürzel pausieren).</summary>
        public static bool Typing;
        private TextField _name;

        public override string Lead => "Deine Marke: Name, Logo und Farbe. Erscheint auf neuen Kartons, im Webshop und groß an deinem Gebäude.";

        public override bool CanRebuild() => !Typing;

        public override void Closed() => Typing = false;

        public override void Build()
        {
            Typing = false;
            var s = S;
            var h = UIX.Row(Root, 18f);
            h.style.alignItems = Align.FlexStart;
            var left = Grow(UIX.Col(h, 12f));

            var nc = Card(left, "Markenname");
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

            var lc = Card(left, "Logo");
            var lw = UIX.Wrap(lc);
            for (int i = 0; i < GameData.LogoNames.Length; i++)
            {
                int idx = i;
                var t = Tile(lw, i == s.BrandLogoIndex, false, () => S.SetBrandLogo(idx), 92f);
                t.style.alignItems = Align.Center;
                t.Add(new BrandPreview(i, s.BrandColor.ToColor(), "", true));
                UIX.Text(t, GameData.LogoNames[i], "tile-sub");
                UIX.PassThrough(t);
            }

            var cc = Card(left, "Farbe");
            var cw = UIX.Wrap(cc);
            foreach (var col in GameData.BrandPalette)
            {
                var c = col;
                bool selected = c.Approx(s.BrandColor);
                var sw = UIX.Pressable(cw, () => S.SetBrandColor(c), "tile");
                sw.style.width = 44;
                sw.style.height = 44;
                sw.style.paddingLeft = 0;
                sw.style.paddingRight = 0;
                sw.style.paddingTop = 0;
                sw.style.paddingBottom = 0;
                sw.style.backgroundColor = c.ToColor();
                sw.style.alignItems = Align.Center;
                sw.style.justifyContent = Justify.Center;
                UIX.Round(sw, 10f);
                sw.EnableInClassList("selected", selected);
                if (selected) UIX.Icon(sw, "check", 20f, Theme.OnColor(c.ToColor()));
            }

            var right = UIX.Col(h, 10f);
            right.style.flexShrink = 0;
            var pc = Card(right, "Vorschau");
            pc.Add(new BrandPreview(s.BrandLogoIndex, s.BrandColor.ToColor(), s.BrandName));
            var tip = P(right, "Tipp: Nur neu gekaufte Kartons (Einkauf › Verpackung) bekommen dieses Design. Das Schild am Gebäude ändert sich sofort.", "small");
            tip.style.maxWidth = 320;
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

    /// <summary>Markt & Trends › Marktanalyse: Konkurrenzpreise je Produkt, Marktpreis und Preiskämpfe.</summary>
    public sealed class AppMarket : LaptopApp
    {
        public override string Lead => "Der günstigste Konkurrenzpreis ist der Marktpreis. Liegst du darunter, steigt deine Nachfrage deutlich.";

        public override void Build()
        {
            var s = S;
            var m = s.Market;
            var cr = Columns(Root);
            foreach (var c in Market.Competitors)
            {
                var cc = Grow(UIX.Card(cr));
                var ch = UIX.Row(cc, 8f);
                UIX.Avatar(ch, c.Name, Theme.LaptopMuted, 34f);
                var cv = Grow(UIX.Col(ch, 0f));
                UIX.Text(cv, c.Name, "h3");
                UIX.Text(cv, "Preisniveau ×" + Fmt.Dec(c.Factor, 2), "small");
                UIX.Text(cc, c.Desc, "muted");
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

            var tc = Card(Root, "Preise im Vergleich");
            float[] w = { -1f, 96f, 104f, 96f, 104f, 104f, 110f };
            bool[] num = { false, true, true, true, true, true, false };
            TableRow(tc, w, new[] { "PRODUKT", "DEIN PREIS", "BILLIGBOY24", "TRENDHAUS", "ALIEXPRESSO", "MARKTPREIS", "NACHFRAGE" }, true, null, num);
            bool first = true;
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
                var row = TableRow(tc, w, cells, false, tones, num);
                if (first) row.AddToClassList("first");
                first = false;
            }
            Tip(Root, "Faustregel: Knapp unter dem Marktpreis verkaufst du viel mit guter Marge. Premium-Ware bringt bessere Bewertungen – und gute Bewertungen bringen mehr Kunden.");
        }
    }
}
