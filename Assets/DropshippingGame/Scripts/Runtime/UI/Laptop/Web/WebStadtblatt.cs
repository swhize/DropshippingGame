using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// „Stadtblatt“ – lokale Online-Zeitung im Comic-Look: Tagesaktionen von MediaMarkd und Fressnix,
    /// satirische Lokalnachrichten, Kleinanzeigen. Routen: "" (Titelseite) · "angebote" · "anzeigen".
    /// Eigenes Stylesheet: Resources/UI/WebStadtblatt.uss (Skin-Klasse .sb).
    /// </summary>
    public sealed class WebStadtblatt : WebApp
    {
        public override string AppId => "stadtblatt";
        public override string SkinClass => "sb";
        public override WebSkin Skin => WebSkin.Comic;
        protected override string Domain => "stadtblatt.de";

        /// <summary>Browser-Tab (Hook in LaptopView.CreateApps).</summary>
        public static LaptopView.AppDef Def() => new LaptopView.AppDef
        {
            Id = "stadtblatt", Title = "Stadtblatt", Icon = "paper", Group = "BETRIEB", Make = () => new WebStadtblatt(),
            Fav = "S", FavColor = new Color(0.12f, 0.12f, 0.12f),
            Badge = () => Game.Sim != null && Game.Sim.ShopCheaperThanWholesaleAny() ? "%" : "",
        };

        private static readonly string[][] News =
        {
            new[] { "Garage wird Logistikzentrum", "Nachbarn melden: Seit Wochen klebt jemand Pakete zu. „Das Geräusch verfolgt mich in den Schlaf“, so Rentner Horst (71)." },
            new[] { "Kalle verkauft jetzt Döner mit QR-Code", "Wer scannt, bekommt eine Speisekarte. Wer nicht scannt, auch. „Ist modern“, sagt Kalle." },
            new[] { "Taube stiehlt Pommes – Polizei ermittelt", "Der Verdächtige ist grau, gurrt und flüchtig. Hinweise bitte an die Wache." },
            new[] { "MediaMarkd testet Verkäufer, die sich auskennen", "Pilotprojekt nach 20 Minuten abgebrochen. „Zu verwirrend für die Kundschaft.“" },
            new[] { "Hund Bello zum Mitarbeiter des Monats gewählt", "Der Mischling überzeugte durch Pünktlichkeit und schlechte Laune gegenüber Postboten." },
            new[] { "Stadtrat beschließt neuen Kreisverkehr", "Auf die Frage „Wozu?“ antwortet der Bürgermeister: „Weil wir es können.“" },
            new[] { "Influencer zieht in die Hauptstraße", "Erste Beschwerde nach drei Stunden: Das Ringlicht blende die Straßenlaterne." },
            new[] { "Fressnix: Katze kauft sich selbst ein Spielzeug", "Kassierer verblüfft. Bezahlt wurde mit einer toten Maus. „Haben wir angenommen.“" },
            new[] { "Paketbote findet Hausnummer 7b", "Nach elf Jahren Suche. Er hat die Karte trotzdem eingeworfen: „Wir haben Sie nicht angetroffen.“" },
            new[] { "Klamottenladen „Hype & Hoodie“ weiter zu", "Schild verspricht: „Demnächst“. Experten tippen auf ein kostenpflichtiges Update." },
            new[] { "Studie: 9 von 10 Start-ups sind eigentlich ein Laptop", "Der zehnte ist ein Laptop mit Aufkleber." },
            new[] { "Ampel an der Hauptstraße schaltet jetzt nach Gefühl", "Die Stadtwerke sprechen von „KI“. Die Ampel spricht nicht." },
        };

        private static readonly string[] Weather = { "Sonnig, 22°", "Wolkig mit Paketen", "Nieselregen, 14°", "Windig – Kartons festhalten!", "Heiter, 19°", "Gewitter am Abend" };

        public override void Build()
        {
            var s = S;
            if (s == null) return;
            var sheet = Resources.Load<StyleSheet>("UI/WebStadtblatt");
            if (sheet != null && !Root.styleSheets.Contains(sheet)) Root.styleSheets.Add(sheet);

            // Kopf
            var top = Row(Root, 8f, "sb-top");
            T(top, "Ausgabe Nr. " + s.Day, "sb-small");
            Fill(top);
            T(top, s.WeekdayName() + " · Tag " + s.Day + " · " + Weather[W.Hash("w" + s.Day) % Weather.Length], "sb-small");
            var mast = Press(Root, () => Nav(""), "sb-mast");
            H(mast, "Stadtblatt", "sb-title");
            T(mast, "Unabhängig. Lokal. Meistens richtig.", "sb-motto");
            UIX.PassThrough(mast);
            var nav = Row(Root, 6f, "sb-nav");
            Btn(nav, "Titelseite", () => Nav(""), "sb-nav-btn", Route == "" ? "on" : "");
            Btn(nav, "Angebote", () => Nav("angebote"), "sb-nav-btn", Route == "angebote" ? "on" : "");
            Btn(nav, "Anzeigen", () => Nav("anzeigen"), "sb-nav-btn", Route == "anzeigen" ? "on" : "");

            switch (Route)
            {
                case "angebote": Offers(s); break;
                case "anzeigen": Ads(s); break;
                default: Front(s); break;
            }
            T(Root, "© Stadtblatt · Preis dieser Ausgabe: 0,00 € (wie alles im Internet)", "sb-foot");
        }

        // ---- Titelseite ---------------------------------------------------------------------------
        private void Front(Sim s)
        {
            var cols = Row(Root, 14f, "sb-cols");
            cols.style.alignItems = Align.FlexStart;
            var main = Flex(Col(cols, 10f));
            var picks = PickNews(s.Day, 3);
            var lead = Col(main, 4f, "sb-box", "sb-lead");
            H(lead, Headline(s, picks[0]), "sb-h1");
            T(lead, News[picks[0]][1], "sb-text");
            for (int i = 1; i < picks.Count; i++)
            {
                var b = Col(main, 2f, "sb-box");
                B(b, News[picks[i]][0], "sb-h2");
                T(b, News[picks[i]][1], "sb-text");
            }
            if (s.BrandNamed || s.TotalShipped > 0)
            {
                var b = Col(main, 2f, "sb-box");
                B(b, "Lokale Firma " + s.BrandName + " verschickt " + s.TotalShipped + " Pakete", "sb-h2");
                T(b, "Bewertung: " + Fmt.Rating(s.Reputation) + " Sterne. „Ich hab's ja immer gewusst“, sagt die Mutter des Gründers.", "sb-text");
            }

            var side = Wd(Col(cols, 10f), 290f);
            Teaser(side, s, ShopData.Electro);
            Teaser(side, s, ShopData.PetShop);
            if (s.Pets.Count > 0)
            {
                var b = Col(side, 3f, "sb-box");
                B(b, "Haustier der Woche", "sb-h2");
                foreach (var p in s.Pets) T(b, p.Name + " – " + s.PetMoodText(p), "sb-text");
                T(b, "Futter im Napf: " + s.PetFoodTotal() + " Portionen", "sb-small");
            }
            var ad = Col(side, 2f, "sb-box", "sb-ad");
            B(ad, ShopData.ClothesName, "sb-h2");
            T(ad, "Eröffnung: DEMNÄCHST. Wirklich. Bald. Als DLC.", "sb-text");
        }

        private string Headline(Sim s, int idx)
        {
            if (s.Day % 5 == 0 && s.Pets.Count > 0) return s.Pets[0].Name + " erobert TikTak – Stadt im Tierfieber";
            return News[idx][0];
        }

        private static List<int> PickNews(int day, int n)
        {
            var list = new List<int>();
            var rnd = new System.Random(ShopData.Hash(77, day, "news"));
            while (list.Count < n && list.Count < News.Length)
            {
                int i = rnd.Next(News.Length);
                if (!list.Contains(i)) list.Add(i);
            }
            return list;
        }

        private void Teaser(VisualElement parent, Sim s, string store)
        {
            var deals = s.ShopDealsToday(store);
            var b = Col(parent, 4f, "sb-box", store == ShopData.Electro ? "sb-mm" : "sb-fn");
            H(b, ShopData.StoreName(store), "sb-store");
            if (deals.Count == 0) return;
            var d = deals[0];
            var it = ShopData.Item(d.ItemId);
            if (it == null) return;
            B(b, "KNALLER: " + it.Name, "sb-h2");
            B(b, Fmt.Money(s.ShopPrice(it.Id)) + "  statt " + Fmt.Money(s.ShopBasePrice(it.Id)), "sb-price");
            if (s.ShopCheaperThanWholesale(it.Id)) B(b, "BILLIGER ALS DER GROSSHANDEL!", "sb-stamp");
            Btn(b, "Alle Angebote", () => Nav("angebote"), "sb-btn");
        }

        // ---- Angebote ------------------------------------------------------------------------------
        private void Offers(Sim s)
        {
            foreach (var store in new[] { ShopData.Electro, ShopData.PetShop })
            {
                var box = Col(Root, 6f, "sb-box", store == ShopData.Electro ? "sb-mm" : "sb-fn");
                var hd = Row(box, 8f);
                H(hd, ShopData.StoreName(store), "sb-store");
                T(hd, store == ShopData.Electro ? ShopData.ElectroSlogan : ShopData.PetShopSlogan, "sb-small");
                foreach (var d in s.ShopDealsToday(store))
                {
                    var it = ShopData.Item(d.ItemId);
                    if (it == null) continue;
                    var r = Row(box, 10f, "sb-line");
                    if (it.Kind == "ware") Wd(W.Art(r, it.Product, 54f, true), 54f);
                    else UIX.Icon(r, it.Icon, 32f, it.Color.ToColor());
                    var c = Flex(Col(r, 1f));
                    B(c, (d.Mega ? "KNALLER · " : "") + it.Name, "sb-h2");
                    string sub = it.Desc;
                    if (it.Kind == "ware")
                        sub += " · " + Fmt.Eur(s.ShopUnitPrice(it.Id)) + "/Stk (Großhandel " + Fmt.Eur(s.ShopWholesalePackPrice(it.Id) / Mathf.Max(1, it.Pack)) + ")";
                    T(c, sub, "sb-small");
                    var pc = Col(r, 0f);
                    B(pc, "−" + Mathf.RoundToInt(d.Discount * 100f) + " %", "sb-stamp");
                    B(pc, Fmt.Money(s.ShopPrice(it.Id)), "sb-price");
                    T(pc, "statt " + Fmt.Money(s.ShopBasePrice(it.Id)) + " · nur " + d.Limit + "×", "sb-small");
                    if (s.ShopCheaperThanWholesale(it.Id)) B(pc, "< Großhandel", "sb-good");
                }
            }
            T(Root, "Morgen im Angebot? Das wissen nur die Sterne – und die Druckerei.", "sb-muted");
        }

        // ---- Kleinanzeigen ---------------------------------------------------------------------------
        private void Ads(Sim s)
        {
            var grid = Row(Root, 10f, "sb-grid");
            grid.style.flexWrap = Wrap.Wrap;
            Ad(grid, "Kalles Imbiss", "Currywurst 4,–. Ehemalige Mitarbeiter zahlen voll.");
            Ad(grid, "Verkaufe Fahrrad", "Fast neu. Nur einmal geklaut. Abholung nachts.");
            Ad(grid, "Suche WG-Zimmer", "Bin ruhig, sauber, verschicke nur ab und zu 300 Pakete.");
            Ad(grid, ShopData.PetShopName, "Tiere adoptieren statt kaufen! (Schutzgebühr ist kein Kaufen. Versprochen.)");
            Ad(grid, ShopData.ElectroName, "Diese Woche: Kabel, die passen. Fast.");
            Ad(grid, ShopData.ClothesName, "Demnächst. Als DLC. Bitte nicht an der Tür rütteln.");
            if (s.BrandNamed) Ad(grid, s.BrandName, "Lokal verschickt, global bestellt. " + Fmt.Rating(s.Reputation) + " Sterne!");
            Ad(grid, "Bekanntschaften", "Er (34), Unternehmer, sucht Sie, die Kartons faltet.");
        }

        private void Ad(VisualElement parent, string title, string text)
        {
            var b = Wd(Col(parent, 2f, "sb-box", "sb-ad"), 250f);
            B(b, title, "sb-h2");
            T(b, text, "sb-text");
        }
    }
}
