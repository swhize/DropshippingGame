using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Postfach: Nachrichten, Angebote und Ereignisse mit Entscheidungen.</summary>
    public sealed class AppMail : LaptopApp
    {
        private static int _selected = -1;

        public override string Lead => "Nachrichten, Angebote und Entscheidungen. Offene Entscheidungen verfallen um 20 Uhr.";

        public override void Build()
        {
            var ev = S.Events;
            if (ev.Mails.Count == 0)
            {
                var c = UIX.Card(Root);
                UIX.Empty(c, "inbox", "Noch keine Nachrichten", "Das ändert sich, sobald dein Shop läuft.");
                return;
            }
            if (_selected == -1 || ev.Find(_selected) == null)
            {
                _selected = ev.Mails[0].Id;
                foreach (var m in ev.Mails)
                {
                    if (!m.Pending) continue;
                    _selected = m.Id;
                    break;
                }
            }

            var h = UIX.Row(Root, 16f);
            h.style.alignItems = Align.FlexStart;
            var list = UIX.Col(h, 0f);
            list.style.width = 330;
            list.style.flexShrink = 0;
            int shown = 0;
            foreach (var m in ev.Mails)
            {
                if (++shown > 20) break;
                int id = m.Id;
                var item = UIX.PressRow(list, 10f, () =>
                {
                    _selected = id;
                    Rebuild();
                }, "mail-item");
                item.EnableInClassList("selected", m.Id == _selected);
                var dot = UIX.Div(item, "pending-dot");
                dot.style.marginTop = 6;
                dot.EnableInClassList("urgent", m.Pending);
                dot.style.visibility = m.Pending || !m.Read ? Visibility.Visible : Visibility.Hidden;
                UIX.Icon(item, MailIcon(m.Icon), 18f).style.marginTop = 1;
                var tv = Grow(UIX.Col(item, 1f));
                var title = UIX.Text(tv, m.Title, "mail-title");
                if (!m.Read || m.Pending) UIX.Bold(title);
                UIX.Text(tv, (m.Pending ? "Entscheidung · " : "") + m.Sender + " · Tag " + m.Day, "mail-meta");
                UIX.PassThrough(item);
            }

            var mail = ev.Find(_selected);
            if (mail == null) return;
            ev.MarkRead(_selected);
            var d = Grow(UIX.Card(h));
            var dh = UIX.Row(d, 12f);
            UIX.Round(UIX.Swatch(dh, Theme.LaptopAccent, 44f, MailIcon(mail.Icon)), 14f);
            var dv = Grow(UIX.Col(dh, 2f));
            UIX.Text(dv, mail.Title, "msg-title", "display");
            UIX.Text(dv, "Von " + mail.Sender + " · Tag " + mail.Day + ", " + Fmt.Clock(mail.Time) + " Uhr", "small");
            UIX.Separator(d);
            UIX.Text(d, mail.Text, "msg-text");
            if (mail.Pending)
            {
                Section(d, "Deine Entscheidung");
                for (int i = 0; i < mail.Choices.Count; i++)
                {
                    var c = mail.Choices[i];
                    int ci = i;
                    string txt = c.Label + (c.Cost > 0 ? "  ·  " + Fmt.Money(c.Cost) : "");
                    if (!string.IsNullOrEmpty(c.Minigame)) txt += "  ·  Minispiel";
                    var b = Btn(d, txt, () => Choose(ci), i == 0 ? "accent" : "", c.Cost > S.Money,
                        string.IsNullOrEmpty(c.Minigame) ? null : "gamepad");
                    b.AddToClassList("btn-left");
                    b.AddToClassList("choice");
                }
                P(d, "Keine Zeit? Um 20 Uhr gilt automatisch die vorsichtigste Antwort.", "small");
            }
            else if (!string.IsNullOrEmpty(mail.Result))
            {
                var rc = UIX.Card(d, "ERGEBNIS", "card-hi");
                if (!string.IsNullOrEmpty(mail.Chosen)) UIX.Text(rc, "Du hast gewählt: " + mail.Chosen, "muted");
                P(rc, mail.Result, "");
            }
        }

        private void Choose(int index)
        {
            S.Events.Choose(_selected, index);
            Rebuild();
        }

        public static string MailIcon(string icon)
        {
            if (string.IsNullOrEmpty(icon)) return "mail";
            return Icons.Has(icon) ? icon : "mail";
        }
    }

    /// <summary>Einkauf › Ware: Produkt, Lieferant und Menge wählen – die Kiste kommt per Lieferwagen.</summary>
    public sealed class AppBuy : LaptopApp
    {
        private static int _product;
        private static int _supplier = 1;

        public override string Lead => "Produkt, Lieferant, Menge – die Kiste kommt am Wareneingang an. Nachbestellen geht später auch per Handy.";

        public override void Build()
        {
            var s = S;
            _product = Mathf.Clamp(_product, 0, GameData.Products.Length - 1);
            if (!s.ProductAvailable(GameData.Products[_product].Id)) _product = 0;
            if (!s.SupplierAvailable(_supplier)) _supplier = s.SupplierAvailable(1) ? 1 : 0;

            var pc = Card(Root, "Produkt");
            var grid = UIX.Wrap(pc);
            for (int i = 0; i < GameData.Products.Length; i++)
            {
                var p = GameData.Products[i];
                bool avail = s.ProductAvailable(p.Id);
                int idx = i;
                var t = Tile(grid, i == _product, !avail, () =>
                {
                    _product = idx;
                    Rebuild();
                }, 196f, "prod-tile");
                var img = UIX.Div(t, "prod-img");
                img.style.backgroundColor = p.Color.ToColor();
                UIX.Icon(img, p.Icon, 26f, Theme.OnColor(p.Color.ToColor()));
                UIX.Ellipsis(UIX.Text(t, p.Name, "tile-title"));
                UIX.Text(t, "ab " + Fmt.Eur(p.UnitCost) + " · Markt ~" + Fmt.Money(s.Market.MarketPrice(p.Id)), "tile-sub");
                var pill = UIX.Div(t, "pill");
                UIX.Text(pill, avail ? "Lager " + s.StockQty(p.Id) + " · Karton " + GameData.SizeName(p.Size)
                    : (!s.ProductUnlocked(p.Id) ? LockText(p.UnlockLevel) : "Braucht Lagerhalle"), "pill-text");
                UIX.PassThrough(t);
            }

            var pr = GameData.Products[_product];
            var info = UIX.Card(Root, null, "card-hi");
            var ih = UIX.Row(info, 14f);
            UIX.Round(UIX.Swatch(ih, pr.Color.ToColor(), 56f, pr.Icon), 16f);
            var iv = Grow(UIX.Col(ih, 2f));
            UIX.Text(iv, pr.Name, "h2");
            P(iv, "Einkauf ab " + Fmt.Eur(pr.UnitCost) + "/Stk · Marktpreis ~" + Fmt.Money(s.Market.MarketPrice(pr.Id)) + " · Karton " + GameData.SizeName(pr.Size));
            var tph = s.Trends.Phase(pr.Id);
            if (tph != TrendPhase.Normal) UIX.Chip(iv, "Trend: " + s.TrendLabel(pr.Id) + " ×" + Fmt.Dec(s.TrendMult(pr.Id), 1), tph == TrendPhase.Falling || tph == TrendPhase.Dead ? "trend_down" : "fire",
                tph == TrendPhase.Falling || tph == TrendPhase.Dead ? Theme.LaptopBad : Theme.LaptopAccent).style.alignSelf = Align.FlexStart;
            P(iv, "Erwartete Retourenquote: " + UiFmt.Percent(s.ExpectedReturnRate(pr.Id)) + (s.ContractUnitsNeeded(pr.Id) > 0 ? " · " + s.ContractUnitsNeeded(pr.Id) + " Stk für Großaufträge" : ""), "small");
            var sv = UIX.Col(ih, 2f);
            sv.style.alignItems = Align.FlexEnd;
            UIX.Text(sv, "Lager: " + s.StockQty(pr.Id) + " Stück (" + GameData.QualityName(s.StockQuality(pr.Id)) + ")", "h3");
            UIX.Text(sv, "Unterwegs: " + s.TravelingCountFor(pr.Id) + " · Am Eingang: " + s.DockCountFor(pr.Id), "small");
            UIX.Text(sv, "Lagerplatz: " + Fmt.Thousands(s.StockTotal()) + " / " + Fmt.Thousands(s.Capacity()), "small");

            var sc = Card(Root, "Lieferant");
            var sr = Columns(sc, 10f);
            for (int i = 0; i < GameData.Suppliers.Length; i++)
            {
                var sup = GameData.Suppliers[i];
                bool avail = s.SupplierAvailable(i);
                int idx = i;
                var t = Grow(Tile(sr, i == _supplier, !avail, () =>
                {
                    _supplier = idx;
                    Rebuild();
                }, -1f, "opt-tile"));
                var top = UIX.Row(t, 8f);
                Grow(UIX.Text(top, sup.Name, "tile-title"));
                UIX.Stars(top, sup.Quality < 0.8f ? 1 : (sup.Quality < 1.3f ? 3 : 5), 11f, Theme.LaptopAccent);
                UIX.Text(t, "Preis ×" + Fmt.Dec(sup.PriceMult, 2) + " · Lieferzeit ~" + UiFmt.Duration(s.LeadMinutes(i)), "tile-sub");
                UIX.Text(t, sup.Desc, "tile-sub");
                if (s.Level < sup.Level) Stamp(t, LockText(sup.Level).ToUpperInvariant());
                else if (s.BlockedSuppliers.ContainsKey(i)) Stamp(t, "GESCHLOSSEN");
                UIX.PassThrough(t);
            }

            var qc = Card(Root, "Menge · " + pr.Name, GameData.Suppliers[_supplier].Name);
            var qr = Columns(qc, 10f);
            for (int bi = 0; bi < GameData.BulkOptions.Length; bi++)
            {
                var bo = GameData.BulkOptions[bi];
                int cost = s.BulkCost(_product, bi, _supplier);
                bool quota = _supplier != 2 || s.PremiumQuotaLeft(pr.Id);
                bool avail = s.BulkAvailable(bi) && quota;
                int idx = bi;
                int prodIdx = _product, supIdx = _supplier;
                var t = Grow(Tile(qr, false, !avail || s.Money < cost, () =>
                {
                    S.BuyBulk(prodIdx, idx, supIdx);
                }, -1f, "qty-tile"));
                UIX.Text(t, bo.Quantity + " Stk", "qty-value", "display");
                UIX.Text(t, bo.Name, "tile-sub");
                if (avail)
                {
                    UIX.Num(t, Fmt.Money(cost), true, "qty-cost");
                    UIX.Text(t, Fmt.Eur(cost / (float)bo.Quantity) + "/Stk" + (bo.Discount < 1f ? " · −" + Mathf.RoundToInt((1f - bo.Discount) * 100f) + " %" : ""), "tile-sub");
                    if (s.Money < cost) Stamp(t, "ZU TEUER");
                }
                else if (!quota) UIX.Text(t, "Premium: heute schon bestellt", "tile-sub");
                else UIX.Text(t, s.Level < bo.Level ? LockText(bo.Level) : "Braucht Lagerhalle", "tile-sub");
                UIX.PassThrough(t);
            }
            var ex = UIX.Row(qc, 12f);
            ex.style.flexWrap = Wrap.Wrap;
            UIX.Toggle(ex, "Express-Lieferung: halbe Lieferzeit, +" + Fmt.Money(GameData.ExpressSurcharge) + " pro Bestellung", s.ExpressDelivery, on => S.SetExpressDelivery(on));
            if (s.HasUpgrade("van")) P(ex, "Eigener Lieferwagen: alle Lieferungen 25 % schneller.", "small");

            var tc = Card(Root, "Unterwegs", s.TravelingDeliveries.Count > 0 ? s.TravelingDeliveries.Count + " Lieferung(en)" : null);
            if (s.TravelingDeliveries.Count == 0) P(tc, "Gerade ist nichts unterwegs.");
            foreach (var d in s.TravelingDeliveries)
            {
                var r = UIX.Row(tc, 10f, "feed-item");
                var dp = GameData.Product(d.Product);
                UIX.Swatch(r, dp.Color.ToColor(), 30f, dp.Icon);
                Grow(UIX.Text(r, d.Quantity + "× " + dp.Name + " (" + GameData.QualityName(d.Quality) + ")", "feed-title"));
                UIX.Icon(r, "truck", 15f, Theme.LaptopAccent);
                UIX.Num(r, "in " + s.EtaText(d), true);
            }
        }
    }

    /// <summary>Einkauf › Verpackung: Kartons S/M/L kaufen – fertig oder ungefaltet, mit Marken-Aufdruck.</summary>
    public sealed class AppPackaging : LaptopApp
    {
        public override string Lead => "Kartons in drei Größen. Beim Kauf wird dein aktuelles Branding aufgedruckt.";

        public override void Build()
        {
            var s = S;
            var ph = UIX.Row(Root, 18f, "card");
            ph.Add(new BrandPreview(s.BrandLogoIndex, s.BrandColor.ToColor(), s.BrandName));
            var pv = Grow(UIX.Col(ph, 6f));
            UIX.Text(pv, "Dein aktueller Druck", "h3");
            P(pv, "Neue Kartons bekommen Logo und Farbe deiner Marke. Schon gekaufte Kartons behalten ihren alten Druck.");
            P(pv, "Ungefaltete Kartons kosten nur die Hälfte, müssen aber erst am Falttisch gefaltet werden.");
            Btn(pv, "Branding ändern", () => Go("shop/brand"), "soft", false, "tag").style.alignSelf = Align.FlexStart;

            for (int size = 0; size < 3; size++)
            {
                var c = UIX.Card(Root);
                var h = UIX.Row(c, 14f);
                var brand = s.PackagingBrand[size];
                UIX.Round(UIX.Swatch(h, brand != null ? brand.Color.ToColor() : s.BrandColor.ToColor(), 52f, null, GameData.SizeName(size)), 14f);
                var v = Grow(UIX.Col(h, 2f));
                UIX.Text(v, "Karton " + GameData.SizeName(size), "h3");
                var fits = new List<string>();
                foreach (var p in GameData.Products)
                    if (p.Size == size) fits.Add(p.Short);
                UIX.Text(v, "Passt für: " + string.Join(", ", fits), "small");
                var stock = UIX.Text(v, s.Packaging[size] + " gefaltet · " + s.FlatPackaging[size] + " ungefaltet");
                if (s.Packaging[size] + s.FlatPackaging[size] < 5) stock.AddToClassList("bad-text");
                var bv = UIX.Col(h, 6f);
                bv.style.alignItems = Align.FlexEnd;
                var r1 = UIX.Row(bv, 6f);
                int sz = size;
                for (int batch = 0; batch < GameData.PackagingBatches.Length; batch++)
                {
                    int b = batch;
                    int cost = s.PackagingCost(size, false, b);
                    Btn(r1, GameData.PackagingBatches[b].Qty + " Stk · " + Fmt.Money(cost), () => S.BuyPackaging(sz, false, b), b == 0 ? "accent" : "", s.Money < cost, b == 0 ? "package" : null);
                }
                var r2 = UIX.Row(bv, 6f);
                for (int batch = 0; batch < GameData.PackagingBatches.Length; batch++)
                {
                    int b = batch;
                    int cost = s.PackagingCost(size, true, b);
                    Btn(r2, GameData.PackagingBatches[b].Qty + " ungefaltet · " + Fmt.Money(cost), () => S.BuyPackaging(sz, true, b), "", s.Money < cost);
                }
            }
        }
    }
}
