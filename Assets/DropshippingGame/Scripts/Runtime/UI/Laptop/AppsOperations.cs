using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
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
