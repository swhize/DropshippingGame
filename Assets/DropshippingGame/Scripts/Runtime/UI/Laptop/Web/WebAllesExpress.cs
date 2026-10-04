using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// AllesExpress – der Großhandel (verspielt, orange). Startseite mit Kategorien, Gutscheinen,
    /// Blitzangeboten und Produktraster; Produktseiten mit Händler (= Lieferant), Menge
    /// (= Mengenstaffel) und Kaufknopf (<see cref="Sim.BuyBulk"/>). Dazu Kartons und
    /// „Meine Bestellungen“ (Lieferungen unterwegs).
    /// Routen: "" · "cat/{Kategorie}" · "item/{produkt}" · "pack" · "orders".
    /// </summary>
    public sealed class WebAllesExpress : WebApp
    {
        public override string AppId => "allesexpress";
        public override string SkinClass => "ae";
        public override WebSkin Skin => WebSkin.Round;
        protected override string Domain => "allesexpress.cn";

        public override string Url
        {
            get
            {
                string id = RouteArg("item/");
                if (id != null)
                {
                    int idx = Mathf.Max(0, ProductIndex(id));
                    return Domain + "/item/" + (1004000000 + idx * 7919) + ".html";
                }
                return base.Url;
            }
        }

        // Zustand der Produktseite (pro Produkt zurückgesetzt)
        private static string _pageFor = "";
        private static int _variant, _supplier = 1, _bulk, _tab;

        private static readonly string[][] Cats =
        {
            new[] { "Super Deals", "#E63312", "%" }, new[] { "Handy", "#4C6FFF", "H" }, new[] { "Deko", "#FF7AB6", "D" },
            new[] { "Haustier", "#20B3A8", "P" }, new[] { "Gesund", "#7AC142", "G" }, new[] { "Influencer", "#9B6BFF", "I" },
            new[] { "Heimkino", "#FFB020", "!" }, new[] { "Technik", "#3A2140", "?" },
        };

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            Header(s);
            string r = Route ?? "";
            string item = RouteArg("item/");
            if (item != null && GameData.IsProduct(item)) ProductPage(s, item);
            else if (r == "pack") PackPage(s);
            else if (r == "orders") OrdersPage(s);
            else Home(s, RouteArg("cat/"));
        }

        // =====================================================================================
        private void Header(Sim s)
        {
            var hd = Row(Root, 16f, "ae-hd");
            var mascot = Press(hd, () => Toast("Hallo! Ich bin Ali, dein Einkaufsberater. Ich kann nichts, bin aber gelb."), "ae-mascot");
            Div(mascot, "ae-eye", "l");
            Div(mascot, "ae-eye", "r");
            Div(mascot, "ae-mouth");
            UIX.PassThrough(mascot);
            var logo = Press(hd, () => Nav(""), "ae-logo");
            var lr = Row(logo, 0f);
            H(lr, "AllesExpress", "ae-logo-text");
            Div(lr, "ae-logo-star");
            UIX.PassThrough(logo);
            var search = Flex(Row(hd, 0f, "ae-search"));
            var field = Flex(Row(search, 0f, "ae-search-field"));
            T(field, "led streifen günstig 1000m", "ae-search-text");
            Btn(search, "Suchen", () => Toast("247.000 Ergebnisse gefunden. Alle heißen „2026 NEU“."), "ae-search-btn");
            int traveling = s.TravelingDeliveries.Count;
            Btn(hd, "Wagen (" + traveling + ")", () => Nav("orders"), "ae-btn-white");
        }

        // =====================================================================================
        private void Home(Sim s, string cat)
        {
            // Kategorie-Kreise
            var circ = Row(Root, 14f, "ae-circ");
            foreach (var c in Cats)
            {
                string name = c[0];
                var b = Press(circ, () => Nav(name == "Super Deals" ? "" : "cat/" + name), "ae-circ-item");
                b.EnableInClassList("on", cat == name);
                var dot = Div(b, "ae-circ-dot");
                dot.style.backgroundColor = W.Hex(c[1]);
                H(dot, c[2], "ae-circ-letter");
                T(b, name, "ae-circ-label");
                UIX.PassThrough(b);
            }
            var kb = Press(circ, () => Nav("pack"), "ae-circ-item");
            var kd = Div(kb, "ae-circ-dot");
            kd.style.backgroundColor = W.Hex("#C98A4B");
            H(kd, "K", "ae-circ-letter");
            T(kb, "Kartons", "ae-circ-label");
            UIX.PassThrough(kb);

            // Gutscheine
            var tickets = Row(Root, 12f, "ae-tickets");
            string[][] tk = { new[] { "1 €", "ab 10 €" }, new[] { "3 €", "ab 29 €" }, new[] { "−15 %", "Neukunde" }, new[] { "Gratis", "Versand" } };
            string[] jokes =
            {
                "Gutschein gesichert! Gilt ab einem Einkauf von 10 € für Artikel ab 11 €.",
                "Gutschein gesichert! Einlösbar am 30. Februar.",
                "Du bist leider kein Neukunde mehr. Seit gerade eben.",
                "Versand ist schon gratis. Den Gutschein kannst du einrahmen.",
            };
            for (int i = 0; i < tk.Length; i++)
            {
                string joke = jokes[i];
                var t = Row(tickets, 10f, "ae-ticket");
                H(t, tk[i][0], "ae-ticket-value");
                Div(t, "ae-ticket-sep");
                var tv = Col(t, 2f);
                T(tv, tk[i][1], "ae-ticket-sub");
                Btn(tv, "Holen", () => Toast(joke), "ae-ticket-get");
            }

            // Super-Deal-Banner
            var deal = Row(Root, 18f, "ae-deal");
            deal.Add(new DotLayer(new Color(1f, 1f, 1f, 0.5f)));
            H(deal, "SUPER DEALS −97 %", "ae-deal-big");
            var dv = Col(deal, 6f);
            B(dv, "Nur heute!", "ae-deal-text");
            var timer = Row(dv, 4f, "ae-timer");
            T(timer, "Endet in", "ae-deal-text");
            var parts = UntilClose();
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) B(timer, ":", "ae-deal-text");
                B(timer, parts[i], "ae-timer-box");
            }

            // Blitzangebote
            var h2 = Row(Root, 10f, "ae-h2");
            H(h2, "Blitzangebote", "ae-h2-text");
            B(h2, "endet in " + parts[0] + ":" + parts[1] + ":" + parts[2], "ae-pill");
            var flash = Row(Root, 14f, "ae-flash");
            int shown = 0;
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                if (++shown > 6) break;
                string id = p.Id;
                var fl = Press(flash, () => Nav("item/" + id), "ae-fl");
                W.Art(fl, p.Id, 104f, false, "ae-fl-img");
                H(fl, "ab " + W.Eur(UnitPrice(s, ProductIndex(p.Id), 1, 0)), "ae-fl-price");
                int pct = 17 + (W.Hash(p.Id + s.Day) % 80);
                var prog = Div(fl, "ae-prog");
                var bar = Div(prog, "ae-prog-fill");
                bar.style.width = Length.Percent(pct);
                B(prog, pct + " % weg", "ae-prog-text");
                UIX.PassThrough(fl);
            }
            if (shown == 0) T(flash, "Heute keine Blitzangebote. Der Blitz ist im Urlaub.", "ae-muted");

            // Raster
            var h3 = Row(Root, 10f, "ae-h2");
            H(h3, string.IsNullOrEmpty(cat) ? "Mehr für dich" : cat, "ae-h2-text");
            if (!string.IsNullOrEmpty(cat)) Btn(h3, "Alle anzeigen", () => Nav(""), "ae-chip-btn");
            var grid = Div(Root, "ae-grid");
            int n = 0;
            foreach (var p in GameData.Products)
            {
                var fl = ProductFlavor.Get(p.Id);
                if (!string.IsNullOrEmpty(cat) && fl.Cat != cat) continue;
                Card(grid, s, p);
                n++;
            }
            if (n == 0) T(grid, "In dieser Kategorie gibt es nichts. Noch nicht mal Staub.", "ae-muted");
        }

        private void Card(VisualElement grid, Sim s, ProductDef p)
        {
            var fl = ProductFlavor.Get(p.Id);
            int idx = ProductIndex(p.Id);
            float unit = UnitPrice(s, idx, 1, 0);
            float old = OldPrice(p);
            string id = p.Id;
            var card = Press(grid, () => Nav("item/" + id), "ae-card");
            var img = W.Art(card, p.Id, 140f, false, "ae-card-img");
            B(img, "−" + Mathf.Clamp(Mathf.RoundToInt((1f - unit / old) * 100f), 1, 99) + "%", "ae-badge");
            if (fl.Sold >= 30000) B(card, "HOT", "ae-hot");
            T(card, fl.Title, "ae-card-title");
            H(card, W.Eur(unit), "ae-card-price");
            T(card, "<s>" + W.Eur(old) + "</s> · " + W.Rough(fl.Sold) + " verkauft", "ae-card-meta");
            var sr = Row(card, 4f, "ae-card-stars");
            W.StarText(sr, fl.Rating, "ae-card-meta");
            T(sr, Fmt.Rating(fl.Rating), "ae-card-meta");
            string lockTxt = LockText(s, p);
            if (lockTxt != null)
            {
                card.AddToClassList("locked");
                B(img, lockTxt, "ae-lockstamp");
            }
            else if (s.StockQty(p.Id) > 0) B(card, "Im Lager: " + s.StockQty(p.Id), "ae-card-stock");
            UIX.PassThrough(card);
        }

        // =====================================================================================
        private void ProductPage(Sim s, string pid)
        {
            var p = GameData.Product(pid);
            var fl = ProductFlavor.Get(pid);
            int idx = ProductIndex(pid);
            if (_pageFor != pid)
            {
                _pageFor = pid;
                _variant = 0;
                _tab = 0;
                _bulk = 0;
            }
            if (_supplier < 0 || _supplier >= GameData.Suppliers.Length || !s.SupplierAvailable(_supplier)) _supplier = s.SupplierAvailable(1) ? 1 : 0;
            _bulk = Mathf.Clamp(_bulk, 0, GameData.BulkOptions.Length - 1);
            if (!s.BulkAvailable(_bulk)) _bulk = 0;
            _variant = Mathf.Clamp(_variant, 0, Mathf.Max(0, fl.Variants.Length - 1));

            var crumbs = Row(Root, 6f, "ae-crumbs");
            Btn(crumbs, "Startseite", () => Nav(""), "ae-link");
            T(crumbs, "›", "ae-muted");
            Btn(crumbs, fl.Cat, () => Nav("cat/" + fl.Cat), "ae-link");
            T(crumbs, "›", "ae-muted");
            T(crumbs, p.Name, "ae-muted");

            var pp = Row(Root, 18f, "pp");
            pp.style.alignItems = Align.FlexStart;

            // Galerie
            var gal = Wd(Col(pp, 10f, "pp-gal"), 330f);
            W.Art(gal, pid, 300f, false, "pp-main");
            var th = Row(gal, 8f, "pp-thumbs");
            for (int i = 0; i < 4; i++)
            {
                var t = Div(th, "pp-thumb");
                t.style.backgroundColor = i % 3 == 0 ? W.Lighten(p.Color.ToColor(), 0.5f) : (i == 1 ? Color.white : new Color(0.93f, 0.93f, 0.93f));
            }

            // Infos
            var info = Flex(Col(pp, 8f, "pp-info"));
            B(info, fl.Title, "pp-title");
            var rr = Row(info, 8f);
            W.StarText(rr, fl.Rating, "pp-stars");
            B(rr, Fmt.Rating(fl.Rating), "pp-text");
            Btn(rr, Fmt.Thousands(fl.ReviewCount) + " Bewertungen", () =>
            {
                _tab = 1;
                Rebuild();
            }, "ae-link");
            T(rr, "· " + W.Rough(fl.Sold) + " verkauft", "pp-text");
            

            float unit = UnitPrice(s, idx, _supplier, _bulk);
            float old = OldPrice(p);
            var price = Row(info, 10f, "pp-price");
            H(price, W.Eur(unit), "pp-now");
            T(price, "<s>" + W.Eur(old) + "</s>", "pp-old");
            B(price, "−" + Mathf.Clamp(Mathf.RoundToInt((1f - unit / old) * 100f), 1, 99) + "%", "pp-off");
            T(price, "pro Stück", "ae-muted");
            var cp = Row(info, 6f);
            B(cp, "0,50 € Rabatt ab 50 Stk.", "pp-coupon");
            B(cp, "Neukunde −1 €", "pp-coupon");

            B(info, "Farbe:", "pp-label");
            var vars = Div(info, "pp-vars");
            for (int i = 0; i < fl.Variants.Length; i++)
            {
                int vi = i;
                Btn(vars, fl.Variants[i], () =>
                {
                    _variant = vi;
                    Rebuild();
                }, "pp-var", i == _variant ? "on" : "");
            }

            B(info, "Händler:", "pp-label");
            var sups = Div(info, "pp-vars");
            for (int i = 0; i < GameData.Suppliers.Length; i++)
            {
                var sup = GameData.Suppliers[i];
                int si = i;
                bool ok = s.SupplierAvailable(i);
                string txt = sup.Name + (ok ? "" : (s.Level < sup.Level ? " · ab Lv " + sup.Level : " · geschlossen"));
                var b = BtnIf(sups, ok, txt, () =>
                {
                    _supplier = si;
                    Rebuild();
                }, "pp-var", i == _supplier ? "on" : "");
                b.tooltip = sup.Desc;
            }
            

            B(info, "Menge:", "pp-label");
            var qty = Div(info, "pp-vars");
            for (int bi = 0; bi < GameData.BulkOptions.Length; bi++)
            {
                var bo = GameData.BulkOptions[bi];
                int b2 = bi;
                bool ok = s.BulkAvailable(bi);
                string txt = bo.Quantity + " Stk" + (bo.Discount < 1f ? " (−" + Mathf.RoundToInt((1f - bo.Discount) * 100f) + " %)" : "") +
                             (ok ? "" : (s.Level < bo.Level ? " · ab Lv " + bo.Level : " · Lagerhalle"));
                BtnIf(qty, ok, txt, () =>
                {
                    _bulk = b2;
                    Rebuild();
                }, "pp-var", bi == _bulk ? "on" : "");
            }
            int total = idx >= 0 ? s.BulkCost(idx, _bulk, _supplier) : 0;
            var qr = Row(info, 8f, "pp-qty");
            B(qr, "Summe:", "pp-text");
            H(qr, Fmt.Money(total), "pp-qty-box");
            T(qr, "Karton " + GameData.SizeName(p.Size), "ae-muted");

            var ex = Row(info, 8f);
            Btn(ex, (s.ExpressDelivery ? "An: " : "") + "Express-Lieferung (+" + Fmt.Money(GameData.ExpressSurcharge) + ")", () => S.SetExpressDelivery(!S.ExpressDelivery),
                "pp-var", s.ExpressDelivery ? "on" : "");

            string reason = BuyBlock(s, p, total);
            var br = Row(info, 10f, "pp-buy");
            int prodIdx = idx, supIdx = _supplier, bulkIdx = _bulk;
            BtnIf(br, reason == null, "Kaufen", () =>
            {
                if (S.BuyBulk(prodIdx, bulkIdx, supIdx)) Toast("Bestellt! Kommt per Lieferwagen.", "good");
            }, "ae-btn-main");
            Btn(br, "Kartons dazu", () => Nav("pack"), "ae-btn-yellow");
            if (reason != null) B(info, reason, "ae-warn");

            // Händlerbox
            var side = Wd(Col(pp, 6f, "pp-side"), 250f);
            var sup0 = GameData.Suppliers[_supplier];
            B(side, StoreName(fl, _supplier), "pp-side-title");
            T(side, Fmt.Dec(88f + sup0.Quality * 5f, 1) + " % positive Bewertungen · seit 2014", "ae-muted");
            SideRow(side, "Versand", "Gratis");
            SideRow(side, "Lieferung", "~" + UiFmt.Duration(s.LeadMinutes(_supplier)) + (s.ExpressDelivery ? " (Express)" : ""));
            SideRow(side, "Qualität", sup0.Quality < 0.8f ? "naja" : (sup0.Quality < 1.3f ? "solide" : "top"));
            SideRow(side, "Rückgabe", "theoretisch");
            int sale = s.CurrentSalePrice(pid);
            SideRow(side, "Dein Verkaufspreis", Fmt.Money(sale));
            var mr = SideRow(side, "Marge pro Stück", W.Eur(sale - unit));
            mr.AddToClassList(sale - unit >= 0 ? "good" : "bad");
            SideRow(side, "Im Lager", s.StockQty(pid) + " Stk.");
            SideRow(side, "Unterwegs", s.TravelingCountFor(pid) + " Stk.");
            

            // Reiter
            var tabs = Row(Root, 8f, "pp-tabs");
            string[] names = { "Beschreibung", "Bewertungen (" + Fmt.Thousands(fl.ReviewCount) + ")", "Details" };
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
                T(body, fl.Desc, "pp-text");
                
            }
            else if (_tab == 1) ReviewsBlock(body, fl.Rating, fl.ReviewCount, fl.Reviews, fl.Variants.Length > 0 ? fl.Variants[0] : "", p.Color.ToColor());
            else
            {
                DetailRow(body, "Marke", "NONE");
                DetailRow(body, "Herkunft", "Festland");
                DetailRow(body, "Material", "Ja");
                DetailRow(body, "Gewicht", "ca. leicht");
                DetailRow(body, "Zertifikat", "CE (China Export)");
                DetailRow(body, "Kartongröße", GameData.SizeName(p.Size));
                DetailRow(body, "Erwartete Retourenquote", UiFmt.Percent(s.ExpectedReturnRate(pid)));
                DetailRow(body, "Trend", s.TrendLabel(pid) + " ×" + Fmt.Dec(s.TrendMult(pid), 1));
                DetailRow(body, "Marktpreis", "~" + Fmt.Money(s.Market.MarketPrice(pid)));
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
            Wd(T(r, k, "ae-muted"), 210f);
            B(r, v, "pp-text");
        }

        /// <summary>Bewertungsübersicht (Note, Sterne, Histogramm) und Einzelbewertungen – auch für Mein Shop.</summary>
        public static void ReviewsBlock(VisualElement body, float rating, int count, IList<string[]> reviews, string variant, Color avatar, WebSkin skin = WebSkin.Round, bool verified = false)
        {
            var sum = UIX.Row(body, 24f, "pp-rsum");
            W.Text(sum, Fmt.Rating(rating), skin, WebFonts.Title, "pp-rsum-n");
            var sv = UIX.Col(sum, 2f);
            W.StarText(sv, rating, "pp-stars");
            W.Text(sv, Fmt.Thousands(count) + " Bewertungen", skin, WebFonts.Body, "pp-muted");
            var hist = UIX.Col(sum, 3f, "pp-hist");
            var h = ProductFlavor.Histogram(rating);
            for (int i = 0; i < 5; i++)
            {
                var r = UIX.Row(hist, 6f);
                W.Text(r, (5 - i) + " Sterne", skin, WebFonts.Body, "pp-small");
                var bar = UIX.Div(r, "pp-hist-bar");
                bar.style.width = Mathf.Max(3f, h[i] * 1.4f);
                W.Text(r, h[i] + " %", skin, WebFonts.Body, "pp-small");
            }
            if (reviews == null || reviews.Count == 0)
            {
                W.Text(body, "Noch keine Bewertungen. Sei der Erste! (Bitte nicht der Letzte.)", skin, WebFonts.Body, "pp-muted");
                return;
            }
            foreach (var rv in reviews)
            {
                if (rv == null || rv.Length < 3) continue;
                var row = UIX.Row(body, 10f, "pp-rev");
                row.style.alignItems = Align.FlexStart;
                var av = UIX.Div(row, "pp-av");
                av.style.backgroundColor = W.Lighten(avatar, 0.45f);
                W.Text(av, string.IsNullOrEmpty(rv[0]) ? "?" : rv[0].Substring(0, 1).ToUpperInvariant(), skin, WebFonts.Title, "pp-av-text");
                var col = UIX.Col(row, 3f);
                col.style.flexGrow = 1;
                col.style.flexShrink = 1;
                var head = UIX.Row(col, 6f);
                W.Text(head, rv[0], skin, WebFonts.Bold, "pp-text");
                int st = 3;
                int.TryParse(rv[1], out st);
                W.StarText(head, st, "pp-stars");
                W.Text(head, "· " + (verified ? "Verifizierter Kauf" : "Farbe: " + variant + " · aus DE"), skin, WebFonts.Body, "pp-muted");
                W.Text(col, rv[2], skin, WebFonts.Body, "pp-text");
                W.Text(col, "Hilfreich (" + (rv[2].Length * 7 % 97) + ")", skin, WebFonts.Body, "pp-small");
            }
        }

        // =====================================================================================
        private void PackPage(Sim s)
        {
            var h2 = Row(Root, 10f, "ae-h2");
            H(h2, "Kartons & Verpackung", "ae-h2-text");
            B(h2, "mit deinem Logo bedruckt", "ae-pill");
            var intro = Row(Root, 18f, "ae-box");
            intro.Add(new BrandPreview(s.BrandLogoIndex, s.BrandColor.ToColor(), s.BrandName));
            var iv = Flex(Col(intro, 6f));
            B(iv, "Dein aktueller Druck", "pp-title");
            T(iv, "Mit deinem Logo. Ungefaltet = halber Preis.", "pp-text");
            Btn(iv, "Branding ändern", () => View?.OpenApp("company/brand"), "ae-btn-white");

            var grid = Div(Root, "ae-grid");
            for (int size = 0; size < 3; size++)
            {
                int sz = size;
                var c = Col(grid, 6f, "ae-card", "ae-pack");
                var img = Div(c, "ae-pack-img");
                var brand = s.PackagingBrand[size];
                img.style.backgroundColor = brand != null ? brand.Color.ToColor() : s.BrandColor.ToColor();
                H(img, GameData.SizeName(size), "ae-pack-size");
                B(c, "Karton " + GameData.SizeName(size), "ae-card-title");
                var fits = new List<string>();
                foreach (var p in GameData.Products)
                    if (p.Size == size) fits.Add(p.Short);
                T(c, "Passt für: " + string.Join(", ", fits), "ae-card-meta");
                var stock = B(c, s.Packaging[size] + " gefaltet · " + s.FlatPackaging[size] + " ungefaltet", "ae-card-meta");
                if (s.Packaging[size] + s.FlatPackaging[size] < 5) stock.AddToClassList("bad");
                for (int batch = 0; batch < GameData.PackagingBatches.Length; batch++)
                {
                    int b = batch;
                    int cost = s.PackagingCost(size, false, b);
                    BtnIf(c, s.Money >= cost, GameData.PackagingBatches[b].Qty + " Stk · " + Fmt.Money(cost), () => S.BuyPackaging(sz, false, b), b == 0 ? "ae-btn-main" : "ae-btn-white", "wide");
                }
                for (int batch = 0; batch < GameData.PackagingBatches.Length; batch++)
                {
                    int b = batch;
                    int cost = s.PackagingCost(size, true, b);
                    BtnIf(c, s.Money >= cost, GameData.PackagingBatches[b].Qty + " ungefaltet · " + Fmt.Money(cost), () => S.BuyPackaging(sz, true, b), "ae-btn-soft", "wide");
                }
            }
        }

        // =====================================================================================
        private void OrdersPage(Sim s)
        {
            var h2 = Row(Root, 10f, "ae-h2");
            H(h2, "Meine Bestellungen", "ae-h2-text");
            B(h2, s.TravelingDeliveries.Count + " unterwegs", "ae-pill");
            var box = Col(Root, 8f, "ae-box");
            if (s.TravelingDeliveries.Count == 0)
            {
                B(box, "Gerade ist nichts unterwegs.", "pp-title");
                
                Btn(box, "Weiter shoppen", () => Nav(""), "ae-btn-main");
            }
            foreach (var d in s.TravelingDeliveries)
            {
                if (!GameData.IsProduct(d.Product)) continue;
                var dp = GameData.Product(d.Product);
                var r = Row(box, 12f, "ae-order");
                Wd(W.Art(r, d.Product, 56f, false, "ae-order-img"), 56f);
                var v = Flex(Col(r, 2f));
                B(v, d.Quantity + "× " + dp.Name + " (" + GameData.QualityName(d.Quality) + ")", "pp-text");
                T(v, d.VanSent ? "Eigener Wagen" : "Unterwegs", "ae-muted");
                H(r, "in " + s.EtaText(d), "ae-card-price");
            }
            int dock = 0;
            foreach (var p in GameData.Products) dock += s.DockCountFor(p.Id);
            if (dock > 0) B(Root, "Am Wareneingang warten " + dock + " Stück aufs Einräumen.", "ae-warn");
            Btn(Root, "PaketBlitz", () => View?.OpenApp("paket"), "ae-btn-white");
        }

        // =====================================================================================
        private static float UnitPrice(Sim s, int idx, int supplier, int bulk)
        {
            if (s == null || idx < 0) return 0f;
            try
            {
                int q = GameData.BulkOptions[bulk].Quantity;
                return q > 0 ? s.BulkCost(idx, bulk, supplier) / (float)q : 0f;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0f;
            }
        }

        private static float OldPrice(ProductDef p) => Mathf.Max(p.UnitCost * 6f, p.RefPrice * 0.9f) - 0.01f;

        private static string LockText(Sim s, ProductDef p)
        {
            if (!s.ProductUnlocked(p.Id)) return "Ab Level " + p.UnlockLevel;
            if (!s.ProductAvailable(p.Id)) return "Braucht Lagerhalle";
            return null;
        }

        private static string BuyBlock(Sim s, ProductDef p, int total)
        {
            string l = LockText(s, p);
            if (l != null) return "Noch nicht kaufbar: " + l + ".";
            if (!s.SupplierAvailable(_supplier)) return "Dieser Händler liefert gerade nicht.";
            if (!s.BulkAvailable(_bulk)) return "Diese Menge ist noch gesperrt.";
            if (_supplier == 2 && !s.PremiumQuotaLeft(p.Id)) return "Premium-Hersteller: heute schon bestellt (1× pro Produkt und Tag).";
            if (s.Money < total) return "Zu teuer – dir fehlen " + Fmt.Money(total - s.Money) + ". (Revoluut hätte da was …)";
            return null;
        }

        private static string StoreName(ProductFlavor fl, int supplier)
        {
            switch (supplier)
            {
                case 0: return fl.Store.Replace("Official ", "").Replace(" Store", "") + " Outlet (B-Ware?)";
                case 2: return fl.Store.Replace("Store", "Manufaktur").Replace("Co.", "Manufaktur");
                default: return fl.Store;
            }
        }
    }
}
