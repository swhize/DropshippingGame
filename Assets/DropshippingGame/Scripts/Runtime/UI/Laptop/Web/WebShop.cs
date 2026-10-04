using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Mein Shop (shopifly, verspielt blau/lila) – Vorschau des eigenen Webshops mit Admin-Leiste:
    /// Produkte online/offline, Preise, Rabattaktion, echte Kundenbewertungen.
    /// Routen: "" · "item/{produkt}".
    /// </summary>
    public sealed class WebShop : WebApp
    {
        public override string AppId => "shop";
        public override string SkinClass => "sf";
        public override WebSkin Skin => WebSkin.Round;
        protected override string Domain => W.Slug(S != null ? S.BrandName : "") + ".shopifly.de";

        public override string Url
        {
            get
            {
                string id = RouteArg("item/");
                return id != null ? Domain + "/products/" + id : Domain;
            }
        }

        private static int _tab;
        private static string _pageFor = "";

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            string r = Route ?? "";
            if (r == "store")
            {
                Header(s);
                Home(s);
                return;
            }
            var shell = Row(Root, 0f, "sa-shell");
            shell.style.alignItems = Align.Stretch;
            var nav = Wd(Col(shell, 4f, "sa-nav"), 170f);
            var lg = Row(nav, 6f, "sa-logo");
            Div(lg, "sf-logo-drop");
            H(lg, "shopifly", "sa-logo-text");
            string item = RouteArg("item/");
            NavItem(nav, "home", "Start", "", r == "");
            NavItem(nav, "box", "Produkte", "products", r == "products" || item != null);
            NavItem(nav, "tag", "Preise", "prices", r == "prices");
            NavItem(nav, "eye", "Shop ansehen", "store", false);
            T(nav, "", "sa-gap");
            NavLink(nav, "thumb", "Fakebook", "fakebook/ads");
            NavLink(nav, "search", "Gugel", "gugel/ads");
            NavLink(nav, "brush", "Branding", "company/brand");
            var main = Flex(Col(shell, 0f, "sa-main"));
            var root = Root;
            Root = main;
            try
            {
                var top = Row(main, 10f, "sa-top");
                var sb = Flex(Row(top, 6f, "sa-search"));
                UIX.Icon(sb, "search", 14f, null, "sa-ic");
                T(sb, "Suchen", "sf-muted");
                top.Add(new FaceAvatar(W.Hash(s.BrandName), 30f));
                B(top, s.BrandName, "sf-brandname");
                if (item != null && GameData.IsProduct(item)) ProductPage(s, item);
                else if (r == "products") ProductsPage(s);
                else if (r == "prices") PricesPage(s);
                else Dashboard(s);
            }
            finally
            {
                Root = root;
            }
        }

        private void NavItem(VisualElement parent, string icon, string text, string route, bool on)
        {
            var b = Press(parent, () => Nav(route), "sa-navitem");
            b.EnableInClassList("on", on);
            var row = Row(b, 8f);
            UIX.Icon(row, icon, 16f, null, "sa-ic");
            B(row, text, "sa-navtext");
            UIX.PassThrough(b);
        }

        private void NavLink(VisualElement parent, string icon, string text, string target)
        {
            var b = Press(parent, () => View?.OpenApp(target), "sa-navitem");
            var row = Row(b, 8f);
            UIX.Icon(row, icon, 16f, null, "sa-ic");
            T(row, text, "sa-navtext");
            UIX.PassThrough(b);
        }

        // ---- Admin: Start ---------------------------------------------------------------------
        private void Dashboard(Sim s)
        {
            H(Root, "Hallo, " + s.BrandName + "!", "sa-h1");
            var k = Row(Root, 10f, "sa-kpis");
            MK.Kpi(k, "coin", Fmt.Money(s.Daily.Revenue), "Umsatz heute", Skin);
            MK.Kpi(k, "package", s.Daily.Shipped.ToString(), "Verschickt", Skin);
            MK.Kpi(k, "cart", s.PendingCount() + "/" + s.QueueCapacity(), "Offen", Skin, s.PendingCount() >= s.QueueCapacity() ? "bad" : "");
            float interval = s.OrderIntervalMinutes();
            MK.Kpi(k, "trend", interval > 0f ? Fmt.Dec(60f / interval, 1) : "0", "Bestell./Std.", Skin);
            MK.Kpi(k, "star", Fmt.Rating(s.Reputation), s.ReviewCount + " Bew.", Skin);
            if (s.ShopOfflineUntil > s.BClock()) B(Root, "Shop offline · noch " + UiFmt.Duration(s.ShopOfflineUntil - s.BClock()), "sf-warn", "sa-pad");

            var acts = Row(Root, 10f, "sa-pad");
            int pct = Mathf.RoundToInt(GameData.PromoDiscount * 100f);
            Btn(acts, s.PromoActive ? "Rabatt −" + pct + " % läuft" : "Rabatt −" + pct + " %", () => S.SetPromoActive(!S.PromoActive), s.PromoActive ? "sf-btn-main" : "sf-btn-white");
            Btn(acts, "Anzeige schalten", () => View?.OpenApp("fakebook/ads"), "sf-btn-white");
            Btn(acts, "Preise", () => Nav("prices"), "sf-btn-white");
            ProductTable(s, 6);
        }

        private void ProductsPage(Sim s)
        {
            int listed = 0;
            foreach (var p in GameData.Products)
                if (s.IsListed(p.Id)) listed++;
            var h = Row(Root, 10f, "sa-pad");
            H(h, "Produkte", "sa-h1");
            B(h, listed + " aktiv", "sf-pill");
            ProductTable(s, 99);
        }

        private void ProductTable(Sim s, int max)
        {
            var box = Col(Root, 0f, "sa-table");
            var hd = Row(box, 10f, "sa-tr", "head");
            Flex(T(hd, "Produkt", "sf-muted"));
            Wd(T(hd, "Status", "sf-muted"), 80f);
            Wd(T(hd, "Lager", "sf-muted"), 60f);
            Wd(T(hd, "Preis", "sf-muted"), 70f);
            Wd(T(hd, "Nachfrage", "sf-muted"), 90f);
            int n = 0;
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id) || ++n > max) continue;
                string id = p.Id;
                bool listed = s.IsListed(id);
                var tr = Press(box, () => Nav("item/" + id), "sa-tr");
                var c = Flex(Row(tr, 10f));
                Wd(MK.Photo(c, id, 44f), 44f);
                B(c, p.Name, "pp-text");
                Wd(B(tr, !s.ProductAvailable(id) ? "Gesperrt" : (listed ? "Aktiv" : "Entwurf"), "sa-status", listed ? "on" : "off"), 80f);
                Wd(B(tr, s.StockQty(id).ToString(), "pp-text"), 60f);
                Wd(B(tr, Fmt.Money(s.CurrentSalePrice(id)), "pp-text"), 70f);
                Wd(T(tr, s.DemandLabel(id), "sf-muted"), 90f);
                UIX.PassThrough(tr);
            }
        }

        // ---- Admin: Preise & Marke ---------------------------------------------------------------
        private void PricesPage(Sim s)
        {
            var h = Row(Root, 12f, "sa-pad");
            h.Add(new BrandPreview(s.BrandLogoIndex, s.BrandColor.ToColor(), s.BrandName, true));
            var hv = Flex(Col(h, 2f));
            H(hv, "Preise", "sa-h1");
            T(hv, BrandPrice.FromBrand ? "Vorschlag aus deiner Marke" : "Vorschlag: knapp unter Markt", "sf-muted");
            Btn(h, "Alle −5 %", () => ScaleAll(0.95f), "sf-btn-white");
            Btn(h, "Alle +5 %", () => ScaleAll(1.05f), "sf-btn-white");
            Btn(h, "Alle auf Vorschlag", () =>
            {
                foreach (var p in GameData.Products)
                    if (S.ProductUnlocked(p.Id)) S.SetShopPrice(p.Id, BrandPrice.Suggested(S, p.Id));
            }, "sf-btn-main");
            var box = Col(Root, 0f, "sa-table");
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                string id = p.Id;
                int price = s.ShopPrices.TryGetValue(id, out int v) ? v : p.RefPrice;
                int sug = BrandPrice.Suggested(s, id);
                var tr = Row(box, 8f, "sa-tr");
                Wd(MK.Photo(tr, id, 40f), 40f);
                Flex(B(tr, p.Short, "pp-text"));
                Btn(tr, "−1", () => S.SetShopPrice(id, price - 1), "sf-step");
                Wd(H(tr, Fmt.Money(price), "sf-step-value"), 70f);
                Btn(tr, "+1", () => S.SetShopPrice(id, price + 1), "sf-step");
                Wd(T(tr, "Markt " + Fmt.Money(s.Market.MarketPrice(id)), "sf-muted"), 100f);
                var g = Wd(B(tr, "+" + Fmt.Money(price - p.UnitCost) + "/Stk", "pp-text"), 90f);
                g.style.color = price - p.UnitCost >= 0 ? W.Hex("#1A8A3A") : W.Hex("#D0341C");
                BtnIf(tr, sug != price, "Vorschlag " + Fmt.Money(sug), () => S.SetShopPrice(id, sug), "sf-btn-white");
            }
        }

        private static void ScaleAll(float f)
        {
            var s = S;
            if (s == null) return;
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                int price = s.ShopPrices.TryGetValue(p.Id, out int v) ? v : p.RefPrice;
                int np = Mathf.Max(1, Mathf.RoundToInt(price * f));
                if (np == price) np += f > 1f ? 1 : -1;
                s.SetShopPrice(p.Id, Mathf.Max(1, np));
            }
        }

        private void Header(Sim s)
        {
            var ann = Row(Root, 0f, "sf-ann");
            string best = "";
            foreach (var p in GameData.Products)
                if (s.IsListed(p.Id) && s.TrendMult(p.Id) >= 1.3f) best = p.Name;
            T(ann, "Gratis Versand ab 30 € · " + Fmt.Rating(s.Reputation) + " Sterne von " + Fmt.Thousands(s.ReviewCount) + " Kunden" +
                   (best != "" ? " · Gerade im Trend: " + best : " · Neu: Dinge, die du nicht brauchst") + " · Gratis Versand ab 30 €", "sf-ann-text");
            var hd = Row(Root, 16f, "sf-hd");
            var logo = Press(hd, () => Nav(""), "sf-logo");
            var lr = Row(logo, 6f);
            Div(lr, "sf-logo-drop");
            H(lr, "shopifly", "sf-logo-text");
            UIX.PassThrough(logo);
            B(hd, s.BrandName, "sf-brandname");
            Fill(hd);
            Btn(hd, "Zurück zum Admin", () => Nav(""), "sf-btn-white");
        }

        // =====================================================================================
        private void Home(Sim s)
        {
            // Marken-Banner
            var brand = Col(Root, 6f, "sf-brand");
            Div(brand, "sf-blob1");
            Div(brand, "sf-blob2");
            H(brand, s.BrandName.ToUpperInvariant(), "sf-brand-title");
            T(brand, "Dinge, die du nicht brauchst. Schnell geliefert.", "sf-brand-sub");
            Btn(brand, "Jetzt stöbern", () => Toast("Kunden stöbern. Du packst."), "sf-cta");

            var h2 = Row(Root, 10f, "sf-h2");
            H(h2, "Beliebt gerade", "sf-h2-text");
            int listed = 0;
            foreach (var p in GameData.Products)
                if (s.IsListed(p.Id)) listed++;
            B(h2, listed + " online", "sf-pill");
            var grid = Div(Root, "sf-grid");
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                Card(grid, s, p);
            }

            // Bewertungen
            var rh = Row(Root, 10f, "sf-h2");
            H(rh, "Das sagen deine Kunden", "sf-h2-text");
            var rb = Col(Root, 8f, "sf-box");
            if (s.Reviews.Count == 0) T(rb, "Noch keine Bewertungen. Verschick deine ersten Pakete – schnell und in guter Qualität.", "sf-muted");
            int n = 0;
            foreach (var r in s.Reviews)
            {
                if (++n > 6) break;
                var row = Row(rb, 10f, "pp-rev");
                row.style.alignItems = Align.FlexStart;
                var av = Div(row, "pp-av");
                av.style.backgroundColor = W.Hex("#DDE4FF");
                H(av, string.IsNullOrEmpty(r.Name) ? "?" : r.Name.Substring(0, 1).ToUpperInvariant(), "pp-av-text");
                var col = Flex(Col(row, 2f));
                var head = Row(col, 6f);
                B(head, r.Name, "pp-text");
                W.StarText(head, r.Stars, "pp-stars");
                string prod = GameData.IsProduct(r.Product) ? GameData.Product(r.Product).Name : r.Product;
                T(head, "· " + prod + " · Tag " + r.Day, "sf-muted");
                T(col, "„" + r.Text + "“", "pp-text");
            }
        }

        private void ShopStat(VisualElement parent, string key, string value, string sub)
        {
            var c = Flex(Col(parent, 2f, "sf-stat"));
            T(c, key, "sf-stat-key");
            H(c, value, "sf-stat-value");
            T(c, sub, "sf-muted");
        }

        private void Card(VisualElement grid, Sim s, ProductDef p)
        {
            string id = p.Id;
            bool avail = s.ProductAvailable(p.Id);
            bool listed = s.IsListed(p.Id);
            var card = Press(grid, () => Nav("item/" + id), "sf-card");
            card.EnableInClassList("off", !listed);
            var img = MK.Photo(card, p.Id, 140f);
            img.AddToClassList("sf-card-img");
            var tph = s.Trends.Phase(p.Id);
            if (tph == TrendPhase.Rising || tph == TrendPhase.Peak) B(card, "HYPE", "sf-new");
            else if (listed && (s.ShippedPerProduct.TryGetValue(p.Id, out int sold0) ? sold0 : 0) == 0) B(card, "NEU", "sf-new");
            if (!avail) B(img, "Braucht Lagerhalle", "sf-stamp");
            else if (!listed) B(img, "OFFLINE", "sf-stamp");
            B(card, p.Name, "sf-card-title");
            T(card, s.BrandName + " Edition", "sf-muted");
            H(card, Fmt.Money(s.CurrentSalePrice(p.Id)), "sf-card-price");
            T(card, "Lager " + s.StockQty(p.Id) + " · " + s.DemandLabel(p.Id), "sf-muted");
            UIX.PassThrough(card);
        }

        // =====================================================================================
        private void ProductPage(Sim s, string pid)
        {
            var p = GameData.Product(pid);
            var fl = ProductFlavor.Get(pid);
            if (_pageFor != pid)
            {
                _pageFor = pid;
                _tab = 0;
            }
            var crumbs = Row(Root, 6f, "ae-crumbs");
            Btn(crumbs, "Produkte", () => Nav("products"), "sf-link");
            T(crumbs, "›", "sf-muted");
            T(crumbs, p.Name, "sf-muted");

            var pp = Row(Root, 18f, "pp");
            pp.style.alignItems = Align.FlexStart;
            var gal = Wd(Col(pp, 10f, "pp-gal"), 330f);
            MK.Photo(gal, pid, 300f, s.IsListed(pid) ? "AKTIV" : "ENTWURF", s.PromoActive ? "−" + Mathf.RoundToInt(GameData.PromoDiscount * 100f) + " %" : "").AddToClassList("pp-main");
            var th = Row(gal, 8f, "pp-thumbs");
            for (int i = 0; i < 4; i++)
            {
                var t = Div(th, "pp-thumb");
                t.style.backgroundColor = i % 3 == 0 ? W.Lighten(p.Color.ToColor(), 0.5f) : (i == 1 ? Color.white : new Color(0.93f, 0.93f, 0.97f));
            }

            var reviews = new List<string[]>();
            float sum = 0f;
            foreach (var r in s.Reviews)
            {
                if (r.Product != pid) continue;
                sum += r.Stars;
                if (reviews.Count < 8) reviews.Add(new[] { r.Name, r.Stars.ToString(), r.Text });
            }
            int count = 0;
            foreach (var r in s.Reviews)
                if (r.Product == pid) count++;
            float rating = count > 0 ? sum / count : s.Reputation;

            var info = Flex(Col(pp, 8f, "pp-info"));
            B(info, p.Name + " – " + s.BrandName + " Edition", "pp-title");
            var rr = Row(info, 8f);
            W.StarText(rr, rating, "pp-stars");
            B(rr, Fmt.Rating(rating), "pp-text");
            Btn(rr, count + " Bewertungen", () =>
            {
                _tab = 1;
                Rebuild();
            }, "sf-link");
            int sold = s.ShippedPerProduct.TryGetValue(pid, out int sv) ? sv : 0;
            T(rr, sold > 20 ? "· Bestseller in Kram" : "· " + sold + " verkauft", "pp-text");

            int price = s.ShopPrices.TryGetValue(pid, out int prc) ? prc : p.RefPrice;
            int sale = s.CurrentSalePrice(pid);
            int old = Mathf.RoundToInt(sale * 1.6f);
            var pb = Row(info, 10f, "pp-price");
            H(pb, Fmt.Money(sale), "pp-now");
            T(pb, "<s>" + Fmt.Money(old) + "</s>", "pp-old");
            B(pb, "−" + Mathf.RoundToInt((1f - sale / (float)Mathf.Max(1, old)) * 100f) + "%", "pp-off");
            if (s.PromoActive) B(pb, "Rabattaktion aktiv", "sf-pill");
            B(info, "Gratis Versand ab 30 €", "pp-coupon");

            // Verwaltung
            var adm = Col(info, 8f, "sf-admin");
            B(adm, "Verwaltung (nur für dich)", "sf-admin-title");
            bool avail = s.ProductAvailable(pid);
            if (!avail)
            {
                B(adm, !s.ProductUnlocked(pid) ? "Ab Firmenlevel " + p.UnlockLevel : "Braucht die Lagerhalle (mehr Regalplatz).", "sf-warn");
            }
            else
            {
                bool listed = s.IsListed(pid);
                var lr = Row(adm, 8f);
                Btn(lr, listed ? "Online – ausschalten" : "Offline – einschalten", () => S.SetListed(pid, !S.IsListed(pid)), listed ? "sf-btn-main" : "sf-btn-white");
                T(lr, listed ? "Kunden können bestellen." : "Niemand sieht das Produkt.", "sf-muted");
                var step = Row(adm, 6f, "sf-stepper");
                Btn(step, "−5", () => S.SetShopPrice(pid, price - 5), "sf-step");
                Btn(step, "−1", () => S.SetShopPrice(pid, price - 1), "sf-step");
                H(step, Fmt.Money(price), "sf-step-value");
                Btn(step, "+1", () => S.SetShopPrice(pid, price + 1), "sf-step");
                Btn(step, "+5", () => S.SetShopPrice(pid, price + 5), "sf-step");
                float market = s.Market.MarketPrice(pid);
                int sug = BrandPrice.Suggested(s, pid);
                if (sug != price) Btn(step, "Vorschlag " + Fmt.Money(sug), () => S.SetShopPrice(pid, sug), "sf-btn-white");
                T(adm, "Marktpreis ~" + Fmt.Money(market) + " · Nachfrage: " + s.DemandLabel(pid), "sf-muted");
            }
            var qr = Row(info, 8f, "pp-qty");
            B(qr, "Menge:", "pp-text");
            Btn(qr, "−", () => Toast("Kunden bestellen immer genau 1 Stück. Weil sie es können."), "sf-step");
            H(qr, "1", "pp-qty-box");
            Btn(qr, "+", () => Toast("Kunden bestellen immer genau 1 Stück. Weil sie es können."), "sf-step");
            T(qr, "Nur noch " + Mathf.Min(3, Mathf.Max(1, s.StockQty(pid))) + " auf Lager (es sind " + s.StockQty(pid) + ")", "sf-muted");
            var br = Row(info, 10f, "pp-buy");
            Btn(br, "In den Warenkorb", () => Toast("Du kannst nicht bei dir selbst bestellen. Steuerberater sagt nein."), "sf-btn-main");
            Btn(br, "Nachkaufen bei AllesExpress", () => View?.Navigate("allesexpress", "item/" + pid), "sf-btn-yellow");

            var side = Wd(Col(pp, 6f, "pp-side"), 250f);
            B(side, s.BrandName, "pp-side-title");
            T(side, "Deine Marke · Level " + s.Level, "sf-muted");
            SideRow(side, "Lager", s.StockQty(pid) + " Stk.");
            SideRow(side, "Offen", s.PendingCountFor(pid) + " Bestellungen");
            SideRow(side, "Verkauft gesamt", sold.ToString());
            SideRow(side, "Einkauf ab", W.Eur(p.UnitCost));
            var g = SideRow(side, "Gewinn/Stk. ~", Fmt.Money(sale - p.UnitCost));
            g.AddToClassList(sale - p.UnitCost >= 0 ? "good" : "bad");
            SideRow(side, "Retourenquote", UiFmt.Percent(s.ExpectedReturnRate(pid)));
            Btn(side, "Trends ansehen", () => View?.OpenApp("market/trends"), "sf-btn-white", "wide");

            var tabs = Row(Root, 8f, "pp-tabs");
            string[] names = { "Beschreibung", "Bewertungen (" + count + ")", "Details" };
            for (int i = 0; i < names.Length; i++)
            {
                int ti = i;
                Btn(tabs, names[i], () =>
                {
                    _tab = ti;
                    Rebuild();
                }, "pp-tab", _tab == i ? "on" : "");
            }
            var body = Col(Root, 8f, "pp-body");
            if (_tab == 0)
            {
                T(body, "Das Original von " + s.BrandName + ". Handverlesen aus dem Lager, liebevoll verpackt in einem Karton mit unserem Logo. " +
                        fl.Desc.Split('.')[0] + ".", "pp-text");
                T(body, "Versand am selben Tag, wenn der Chef nicht gerade TikToks dreht.", "sf-muted");
            }
            else if (_tab == 1) WebAllesExpress.ReviewsBlock(body, rating, count, reviews, "", W.Hex("#4C6FFF"), WebSkin.Round, true);
            else
            {
                DetailRow(body, "Marke", s.BrandName);
                DetailRow(body, "Versand", "PaketBlitz Standard / Express");
                DetailRow(body, "Kartongröße", GameData.SizeName(p.Size));
                DetailRow(body, "Lagerqualität", GameData.QualityName(s.StockQuality(pid)));
                DetailRow(body, "Trend", s.TrendLabel(pid));
            }
        }

        private VisualElement SideRow(VisualElement parent, string k, string v)
        {
            var r = Row(parent, 8f, "pp-side-row");
            T(r, k, "pp-text");
            Fill(r);
            B(r, v, "pp-side-value");
            return r;
        }

        private void DetailRow(VisualElement parent, string k, string v)
        {
            var r = Row(parent, 12f, "pp-detail");
            Wd(T(r, k, "sf-muted"), 210f);
            B(r, v, "pp-text");
        }
    }
}
