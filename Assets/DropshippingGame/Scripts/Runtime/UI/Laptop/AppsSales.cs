using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
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
            // Controller ohne Tastatur: Namensvorschlag per Knopfdruck
            Btn(nh, "Vorschlag", () =>
            {
                string[] ideas = { "HustleHub", "PaketPeak", "Glanzstück", "TrendKiste", "NextLevelShop", "Kartonkönig", "BoxBoss", "Wunderware", "ClickCraft", "Lieblingsladen" };
                string cur = _name != null ? _name.value : "";
                string pick = ideas[UnityEngine.Random.Range(0, ideas.Length)];
                if (pick == cur) pick = ideas[(System.Array.IndexOf(ideas, pick) + 1) % ideas.Length];
                if (_name != null) _name.value = pick;
                SaveName();
            }, "", false, "sparkle");

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
