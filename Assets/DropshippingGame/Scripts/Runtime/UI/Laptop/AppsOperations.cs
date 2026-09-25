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

        public override void Build()
        {
            var ev = S.Events;
            Header("Postfach", "Nachrichten, Angebote und Entscheidungen. Offene Entscheidungen verfallen um 20 Uhr.");
            if (ev.Mails.Count == 0)
            {
                var c = UIX.Card(Root);
                P(c, "Noch keine Nachrichten. Das ändert sich, sobald dein Shop läuft.");
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
            var list = UIX.Col(h, 0f, "card");
            list.style.width = 310;
            list.style.flexShrink = 0;
            list.style.paddingTop = 4;
            list.style.paddingBottom = 4;
            list.style.paddingLeft = 0;
            list.style.paddingRight = 0;
            int shown = 0;
            foreach (var m in ev.Mails)
            {
                if (++shown > 16) break;
                var item = UIX.Row(list, 10f, "mail-item");
                item.EnableInClassList("selected", m.Id == _selected);
                item.style.alignItems = Align.FlexStart;
                if (m.Pending) UIX.Div(item, "pending-dot").style.marginTop = 6;
                UIX.Icon(item, MailIcon(m.Icon), 18f).style.marginTop = 1;
                var tv = Grow(UIX.Col(item, 1f));
                var title = UIX.Text(tv, m.Title);
                if (!m.Read || m.Pending) title.style.unityFontStyleAndWeight = FontStyle.Bold;
                UIX.Text(tv, m.Sender + " · Tag " + m.Day, "small");
                int id = m.Id;
                item.AddManipulator(new Clickable(() =>
                {
                    _selected = id;
                    Game.Sound("click", 0.05f, -6f);
                    Rebuild();
                }));
            }

            var mail = ev.Find(_selected);
            ev.MarkRead(_selected);
            var d = Grow(UIX.Card(h));
            var dh = UIX.Row(d, 12f);
            UIX.Swatch(dh, Theme.LaptopAccent, 40f, MailIcon(mail.Icon));
            var dv = Grow(UIX.Col(dh, 2f));
            UIX.Text(dv, mail.Title, "h2");
            UIX.Text(dv, "Von: " + mail.Sender + " · Tag " + mail.Day + ", " + Fmt.Clock(mail.Time) + " Uhr", "small");
            UIX.Separator(d);
            P(d, mail.Text, "");
            if (mail.Pending)
            {
                Section(d, "Deine Entscheidung");
                for (int i = 0; i < mail.Choices.Count; i++)
                {
                    var c = mail.Choices[i];
                    string txt = c.Label + (c.Cost > 0 ? "  ·  " + Fmt.Money(c.Cost) : "");
                    if (!string.IsNullOrEmpty(c.Minigame)) txt += "  ·  Minispiel";
                    int ci = i;
                    var b = Btn(d, txt, () => Choose(ci), i == 0 ? "accent" : "", c.Cost > S.Money,
                        string.IsNullOrEmpty(c.Minigame) ? null : "gamepad");
                    b.AddToClassList("btn-left");
                }
            }
            else if (!string.IsNullOrEmpty(mail.Result))
            {
                var rc = UIX.Card(d, "Ergebnis", "card-hi");
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

    /// <summary>Einkauf: Produkt, Lieferant und Menge wählen – die Kiste kommt per Lieferwagen.</summary>
    public sealed class AppBuy : LaptopApp
    {
        private static int _product;
        private static int _supplier = 1;

        public override void Build()
        {
            var s = S;
            Header("Einkauf", "Bestell Ware bei Lieferanten. Jede Bestellung kommt als Kiste am Wareneingang an – der Lieferwagen hält vor der Tür.");
            if (!s.ProductAvailable(GameData.Products[_product].Id)) _product = 0;
            if (!s.SupplierAvailable(_supplier)) _supplier = s.SupplierAvailable(1) ? 1 : 0;

            Section(Root, "Produkt");
            var grid = UIX.Wrap(Root);
            for (int i = 0; i < GameData.Products.Length; i++)
            {
                var p = GameData.Products[i];
                bool avail = s.ProductAvailable(p.Id);
                int idx = i;
                var t = Tile(grid, i == _product, !avail, () =>
                {
                    _product = idx;
                    Rebuild();
                }, 158f);
                var tr = UIX.Row(t, 8f);
                UIX.Swatch(tr, p.Color.ToColor(), 30f, p.Icon);
                var tv = UIX.Col(tr, 0f);
                UIX.Text(tv, p.Short, "tile-title");
                string sub = avail ? "Lager: " + s.StockQty(p.Id)
                    : (!s.ProductUnlocked(p.Id) ? LockText(p.UnlockLevel) : "Braucht Lagerhalle");
                UIX.Text(tv, sub, "tile-sub");
            }

            var pr = GameData.Products[_product];
            var info = UIX.Card(Root);
            var ih = UIX.Row(info, 14f);
            UIX.Swatch(ih, pr.Color.ToColor(), 54f, pr.Icon);
            var iv = Grow(UIX.Col(ih, 2f));
            UIX.Text(iv, pr.Name, "h2");
            P(iv, "Einkauf ab " + Fmt.Eur(pr.UnitCost) + "/Stk · Marktpreis ~" + Fmt.Money(s.Market.MarketPrice(pr.Id)) + " · Karton " + GameData.SizeName(pr.Size));
            var sv = UIX.Col(ih, 2f);
            sv.style.alignItems = Align.FlexEnd;
            UIX.Text(sv, "Lager: " + s.StockQty(pr.Id) + " Stück (" + GameData.QualityName(s.StockQuality(pr.Id)) + ")", "h3");
            UIX.Text(sv, "Unterwegs: " + s.TravelingCountFor(pr.Id) + " · Am Eingang: " + s.DockCountFor(pr.Id), "small");
            UIX.Text(sv, "Lagerplatz: " + Fmt.Thousands(s.StockTotal()) + " / " + Fmt.Thousands(s.Capacity()), "small");

            Section(Root, "Lieferant");
            var sr = UIX.Row(Root, 10f);
            sr.style.alignItems = Align.Stretch;
            for (int i = 0; i < GameData.Suppliers.Length; i++)
            {
                var sup = GameData.Suppliers[i];
                bool avail = s.SupplierAvailable(i);
                int idx = i;
                var t = Grow(Tile(sr, i == _supplier, !avail, () =>
                {
                    _supplier = idx;
                    Rebuild();
                }));
                UIX.Text(t, sup.Name, "tile-title");
                UIX.Stars(t, sup.Quality < 0.8f ? 1 : (sup.Quality < 1.3f ? 3 : 5), 12f, Theme.LaptopAccent);
                UIX.Text(t, "Preis ×" + Fmt.Dec(sup.PriceMult, 2) + " · Lieferzeit ~" + Mathf.RoundToInt(s.LeadMinutes(i)) + " min", "tile-sub");
                UIX.Text(t, sup.Desc, "tile-sub");
                if (s.Level < sup.Level) Stamp(t, LockText(sup.Level).ToUpperInvariant());
                else if (s.BlockedSuppliers.ContainsKey(i)) Stamp(t, "GESCHLOSSEN");
            }

            Section(Root, "Menge");
            var qr = UIX.Row(Root, 10f);
            qr.style.alignItems = Align.Stretch;
            for (int bi = 0; bi < GameData.BulkOptions.Length; bi++)
            {
                var bo = GameData.BulkOptions[bi];
                int cost = s.BulkCost(_product, bi, _supplier);
                bool avail = s.BulkAvailable(bi);
                int idx = bi;
                var t = Grow(Tile(qr, false, !avail || s.Money < cost, () => s.BuyBulk(_product, idx, _supplier)));
                UIX.Text(t, bo.Name + " · " + bo.Quantity + " Stück", "tile-title");
                if (avail)
                {
                    UIX.Text(t, Fmt.Money(cost), "h2").AddToClassList("accent-text");
                    UIX.Text(t, Fmt.Eur(cost / (float)bo.Quantity) + " pro Stück" + (bo.Discount < 1f ? " · −" + Mathf.RoundToInt((1f - bo.Discount) * 100f) + " %" : ""), "tile-sub");
                    if (s.Money < cost) Stamp(t, "ZU TEUER");
                }
                else UIX.Text(t, s.Level < bo.Level ? LockText(bo.Level) : "Braucht Lagerhalle", "tile-sub");
            }

            var ex = UIX.Card(Root);
            UIX.Toggle(ex, "Express-Lieferung: halbe Lieferzeit, +" + Fmt.Money(GameData.ExpressSurcharge) + " pro Bestellung", s.ExpressDelivery, on => s.SetExpressDelivery(on));
            if (s.HasUpgrade("van")) P(ex, "Eigener Lieferwagen: alle Lieferungen 25 % schneller.", "small");

            if (s.TravelingDeliveries.Count > 0)
            {
                var tc = UIX.Card(Root, "Unterwegs");
                foreach (var d in s.TravelingDeliveries)
                    KV(tc, d.Quantity + "× " + GameData.Product(d.Product).Name + " (" + GameData.QualityName(d.Quality) + ")", "Ankunft in " + s.EtaText(d), "accent");
            }
        }
    }

    /// <summary>Verpackung: Kartons S/M/L kaufen – fertig oder ungefaltet, mit Marken-Aufdruck.</summary>
    public sealed class AppPackaging : LaptopApp
    {
        public override void Build()
        {
            var s = S;
            Header("Verpackung", "Kartons in drei Größen. Beim Kauf wird dein aktuelles Branding aufgedruckt.");
            var ph = UIX.Row(Root, 18f, "card");
            ph.Add(new BrandPreview(s.BrandLogoIndex, s.BrandColor.ToColor(), s.BrandName));
            var pv = Grow(UIX.Col(ph, 6f));
            UIX.Text(pv, "Dein aktueller Druck", "h3");
            P(pv, "Neue Kartons bekommen Logo und Farbe deiner Marke. Schon gekaufte Kartons behalten ihren alten Druck.");
            P(pv, "Ungefaltete Kartons kosten nur die Hälfte, müssen aber erst am Falttisch gefaltet werden.");
            Btn(pv, "Branding ändern", () => View.OpenApp("brand"), "soft", false, "tag").style.alignSelf = Align.FlexStart;

            for (int size = 0; size < 3; size++)
            {
                var c = UIX.Card(Root);
                var h = UIX.Row(c, 14f);
                var brand = s.PackagingBrand[size];
                UIX.Swatch(h, brand != null ? brand.Color.ToColor() : s.BrandColor.ToColor(), 50f, null, GameData.SizeName(size));
                var v = Grow(UIX.Col(h, 2f));
                UIX.Text(v, "Karton " + GameData.SizeName(size), "h3");
                var fits = new List<string>();
                foreach (var p in GameData.Products)
                    if (p.Size == size) fits.Add(p.Short);
                UIX.Text(v, "Passt für: " + string.Join(", ", fits), "small");
                var stock = UIX.Text(v, s.Packaging[size] + " gefaltet · " + s.FlatPackaging[size] + " ungefaltet");
                if (s.Packaging[size] + s.FlatPackaging[size] < 5) stock.AddToClassList("bad-text");
                var bv = UIX.Col(h, 6f);
                var r1 = UIX.Row(bv, 6f);
                int sz = size;
                for (int batch = 0; batch < GameData.PackagingBatches.Length; batch++)
                {
                    int b = batch;
                    int cost = s.PackagingCost(size, false, b);
                    Btn(r1, GameData.PackagingBatches[b].Qty + " Stk · " + Fmt.Money(cost), () => s.BuyPackaging(sz, false, b), b == 0 ? "accent" : "", s.Money < cost);
                }
                var r2 = UIX.Row(bv, 6f);
                for (int batch = 0; batch < GameData.PackagingBatches.Length; batch++)
                {
                    int b = batch;
                    int cost = s.PackagingCost(size, true, b);
                    Btn(r2, GameData.PackagingBatches[b].Qty + " ungefaltet · " + Fmt.Money(cost), () => s.BuyPackaging(sz, true, b), "", s.Money < cost);
                }
            }
        }
    }
}
