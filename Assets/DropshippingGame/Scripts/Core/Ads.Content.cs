using System;
using System.Collections.Generic;
using System.Text;

namespace DropshippingGame.Core
{
    /// <summary>Zielgruppe einer Fakebook-Anzeige. Passt das Produkt (Products), wirkt die Anzeige voll.</summary>
    public sealed class AdTarget
    {
        public string Id, Name, Icon;
        public string[] Products = new string[0];
        /// <summary>Passung für alle Produkte (Gießkanne), sonst 0.</summary>
        public float Flat;
    }

    /// <summary>Werbespruch ({p} = Produktname). Ctr = Klickrate, Conv = Kaufwirkung, Target = Lieblings-Zielgruppe.</summary>
    public sealed class AdSlogan
    {
        public string Text, Target = "";
        public float Ctr = 1f, Conv = 1f;
        public bool Clickbait;
    }

    /// <summary>Suchbegriff bei Gugel ({p} = Produktname). Volume = Suchen pro Tag (×Beliebtheit), Intent = Kaufabsicht.</summary>
    public sealed class GugelKeywordDef
    {
        public string Text;
        public int Volume;
        public float Intent;
    }

    public static partial class GameData
    {
        // ---- Fakebook ---------------------------------------------------------------------------
        public const int FakebookLevel = 2;
        public const int MaxAdsPerPlatform = 3;
        /// <summary>Laufzeit einer Kampagne in Geschäftsminuten (ein Geschäftstag).</summary>
        public const float AdCampaignMinutes = 720f;
        public static readonly int[] FakebookBudgets = { 20, 50, 100, 250 };
        public const float FakebookPower = 0.22f;
        public const float FakebookMaxMult = 1.9f;
        public const float ImpressionsPerEuro = 220f;
        public const float FakebookBaseCtr = 0.014f;

        public static readonly AdTarget[] AdTargets =
        {
            new AdTarget { Id = "teens", Name = "Teenies", Icon = "phone", Products = new[] { "huelle", "led", "kopfhoerer", "ringlicht" } },
            new AdTarget { Id = "gamer", Name = "Gamer", Icon = "gamepad", Products = new[] { "led", "kopfhoerer", "beamer" } },
            new AdTarget { Id = "eltern", Name = "Eltern", Icon = "users", Products = new[] { "beamer", "drohne", "katzenbrunnen", "huelle" } },
            new AdTarget { Id = "fitness", Name = "Fitness", Icon = "heart", Products = new[] { "massage", "smartwatch", "kopfhoerer", "haltung" } },
            new AdTarget { Id = "senioren", Name = "Rentner", Icon = "clock", Products = new[] { "massage", "haltung", "smartwatch", "katzenbrunnen" } },
            new AdTarget { Id = "creator", Name = "Influencer", Icon = "mic", Products = new[] { "ringlicht", "drohne", "huelle", "led" } },
            new AdTarget { Id = "alle", Name = "Alle", Icon = "globe", Flat = 0.7f },
        };

        public static readonly AdSlogan[] AdSlogans =
        {
            new AdSlogan { Text = "{p}. Braucht keiner. Will jeder.", Ctr = 1.0f, Conv = 1.05f, Target = "alle" },
            new AdSlogan { Text = "Ärzte hassen diesen {p}-Trick!", Ctr = 1.4f, Conv = 0.8f, Target = "senioren", Clickbait = true },
            new AdSlogan { Text = "Nur heute: {p} fast geschenkt!*", Ctr = 1.2f, Conv = 1.0f, Target = "eltern" },
            new AdSlogan { Text = "POV: Du hast endlich {p}.", Ctr = 1.15f, Conv = 1.0f, Target = "teens" },
            new AdSlogan { Text = "Level up mit {p}!", Ctr = 1.05f, Conv = 1.05f, Target = "gamer" },
            new AdSlogan { Text = "No pain, no {p}.", Ctr = 1.0f, Conv = 1.1f, Target = "fitness" },
            new AdSlogan { Text = "Link in Bio: {p}!!!", Ctr = 1.25f, Conv = 0.9f, Target = "creator", Clickbait = true },
        };

        // ---- Gugel ------------------------------------------------------------------------------
        public const int GugelLevel = 3;
        public static readonly int[] GugelBudgets = { 30, 60, 120, 250 };
        /// <summary>Klickanteil je Anzeigenplatz 1–4.</summary>
        public static readonly float[] GugelPosShare = { 0.32f, 0.17f, 0.10f, 0.06f };
        public const float GugelPower = 0.45f;
        public const int GugelMinBid = 5;
        public const int GugelMaxBid = 400;
        public static readonly string[] GugelRivals = { "BilligBoy24", "Amazonas", "AllesExpress" };

        public static readonly GugelKeywordDef[] GugelKeywords =
        {
            new GugelKeywordDef { Text = "{p} kaufen", Volume = 700, Intent = 1.0f },
            new GugelKeywordDef { Text = "{p} günstig", Volume = 550, Intent = 0.85f },
            new GugelKeywordDef { Text = "bester {p} 2026", Volume = 380, Intent = 0.9f },
            new GugelKeywordDef { Text = "{p} Test", Volume = 320, Intent = 0.6f },
            new GugelKeywordDef { Text = "{p} Erfahrungen Betrug", Volume = 140, Intent = 0.3f },
        };

        // ---- Fake-Profile & Fake-Seiten -----------------------------------------------------------
        public const int FakeProfileCost = 5;
        public const int MaxFakeProfiles = 12;
        public const float FakeProfileProof = 0.015f;
        public const float FakeProfileRiskEach = 0.025f;
        public const float FakeProfileRepBase = 0.08f;
        public const float FakeProfileRepEach = 0.015f;

        public const int FakeSiteLevel = 2;
        public const int FakeSiteCost = 15;
        public const int MaxFakeSites = 12;
        public const int MaxFakeSitesPerProduct = 3;
        public const float FakeSiteSeo = 0.05f;
        public const float FakeSiteRisk = 0.03f;
        public const float FakeSiteRep = 0.06f;

        public static readonly string[] FakeProfileNames =
        {
            "Sandra Echtmensch", "Klaus Nichtbot", "Echte Kundin 1987", "Bernd Zufrieden", "Mandy Glücklich", "Tobi Realo",
            "Gisela Kaufgern", "Jens Unbezahlt", "Lisa Total-Echt", "Horst Fünfsterne", "Chantal Begeistert", "Uwe Verifiziert",
        };

        /// <summary>Text ({p} ersetzt) für Anzeige, Slogan oder Suchbegriff.</summary>
        public static string FillProduct(string template, string productId)
        {
            string name = IsProduct(productId) ? Product(productId).Name : "Produkt";
            return (template ?? "").Replace("{p}", name);
        }

        public static int AdTargetIndex(string id)
        {
            for (int i = 0; i < AdTargets.Length; i++)
                if (AdTargets[i].Id == id) return i;
            return -1;
        }

        /// <summary>Gebote der drei Konkurrenten (Cent) für einen Suchbegriff – absteigend sortiert.</summary>
        public static int[] GugelRivalBids(string productId, int keyword)
        {
            var p = IsProduct(productId) ? Product(productId) : null;
            float basis = Mathx.Clamp((p != null ? p.RefPrice : 30) * 1.2f, 15f, 250f);
            var kw = GugelKeywords[Mathx.Clamp(keyword, 0, GugelKeywords.Length - 1)];
            float k = 0.4f + 0.8f * kw.Intent;
            return new[]
            {
                Math.Max(GugelMinBid, Mathx.RoundToInt(basis * k * 1.3f)),
                Math.Max(GugelMinBid, Mathx.RoundToInt(basis * k * 0.95f)),
                Math.Max(GugelMinBid, Mathx.RoundToInt(basis * k * 0.6f)),
            };
        }

        /// <summary>Suchen pro Geschäftstag für einen Begriff.</summary>
        public static int GugelVolume(string productId, int keyword)
        {
            var p = IsProduct(productId) ? Product(productId) : null;
            var kw = GugelKeywords[Mathx.Clamp(keyword, 0, GugelKeywords.Length - 1)];
            return Math.Max(10, Mathx.RoundToInt(kw.Volume * (p != null ? p.Popularity : 0.6f)));
        }
    }

    /// <summary>Eine automatisch erzeugte Fake-Webseite („KI-Ratgeber“) für ein Produkt.</summary>
    public sealed class FakeSite
    {
        public string Product = "";
        public int Seed, Day;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "product", Product }, { "seed", Seed }, { "day", Day },
        };

        public static FakeSite FromJson(object o)
        {
            var d = J.Obj(o);
            return new FakeSite { Product = J.S(d, "product", ""), Seed = J.I(d, "seed"), Day = J.I(d, "day", 1) };
        }
    }

    /// <summary>
    /// KI-Geschwurbel für Fake-Webseiten: Domain, Titel, Top-Liste und Absätze aus Vorlagen und
    /// Produktdaten. Rein deterministisch aus (Produkt, Seed) – keine Zufallszahlen der Sim.
    /// </summary>
    public static class SlopGen
    {
        private static readonly string[] Domains =
        {
            "beste-{s}-test.info", "{s}-vergleich24.net", "ehrliche-{s}-tests.biz", "top10-{s}.de.com", "{s}-ratgeber-pro.online",
            "unabhaengig-{s}.shop", "{s}-guru.click",
        };

        private static readonly string[] Titles =
        {
            "Top 10 Gründe, warum {p} dein Leben verändert",
            "{p} im Test: Wir waren sprachlos (Platz 1 wird dich schockieren)",
            "Experten verraten: Darum hat jetzt jeder {p}",
            "Ich habe 30 Tage lang {p} benutzt – das ist passiert",
            "{p}: Der ultimative Ratgeber 2026 (aktualisiert gestern)",
            "Warum {p} besser ist als Urlaub, Freunde und Schlaf",
        };

        private static readonly string[] Reasons =
        {
            "{p} macht glücklich. Studien zeigen das. Welche, ist egal.",
            "Mit {p} sparst du Zeit, die du dann mit {p} verbringst.",
            "Dein Nachbar hat schon {p}. Willst du schlechter sein?",
            "{p} ist nachhaltig, weil man es nie wieder wegwirft.",
            "Influencer lieben {p}. Und Influencer lügen nie.",
            "{p} passt zu jedem Outfit, jeder Wohnung und jedem Sternzeichen.",
            "Wissenschaftler sind sich einig: {p} existiert.",
            "Ohne {p} ist der Alltag grau. Mit {p} auch, aber in HD.",
            "{p} ist das perfekte Geschenk für Leute, die schon alles haben.",
            "Die Bewertungen von {b} sind fast alle echt.",
            "{p} war im Fernsehen. Im Hintergrund. Kurz.",
            "Jeder Kauf von {p} rettet ein bisschen die Welt (gefühlt).",
        };

        private static readonly string[] Paragraphs =
        {
            "In der heutigen schnelllebigen Welt ist {p} wichtiger denn je. Als unabhängige Experten haben wir {p} gründlich getestet, also fünf Minuten lang angeschaut.",
            "Viele fragen sich: Brauche ich {p}? Die Antwort ist ein klares Ja, gefolgt von einem Link zu {b}.",
            "Zusammenfassend lässt sich sagen, dass {p} in vielerlei Hinsicht ein Produkt ist. Unser Fazit: 11 von 10 Punkten.",
            "Als KI-Sprachmodell kann ich keine Produkte testen. Trotzdem: {p} ist hervorragend und bei {b} besonders günstig.",
            "Unser Testsieger kostet nur {price}. Das ist weniger als ein Abendessen, wenn man sehr teuer isst.",
            "Fun Fact: Das Wort „{p}“ kommt aus dem Altgriechischen und bedeutet „jetzt kaufen“.",
        };

        private static readonly string[] Authors =
        {
            "Dr. Prof. Max Testmann", "Redaktion (2 Personen, eine davon KI)", "Sabine, 34, Expertin", "Das Ratgeber-Team", "Hans G. Utachter",
        };

        private static string Fill(string t, string pid, string brand, int price)
        {
            string name = GameData.IsProduct(pid) ? GameData.Product(pid).Name : "Produkt";
            return t.Replace("{p}", name).Replace("{b}", string.IsNullOrEmpty(brand) ? "MeinShop" : brand)
                .Replace("{price}", Fmt.Money(price)).Replace("{s}", Slug(name));
        }

        /// <summary>URL-tauglich: "Bluetooth-Kopfhörer" → "bluetooth-kopfhoerer".</summary>
        public static string Slug(string s)
        {
            if (string.IsNullOrEmpty(s)) return "produkt";
            var sb = new StringBuilder();
            foreach (char raw in s.ToLowerInvariant())
            {
                char ch = raw;
                if (ch == 'ä') sb.Append("ae");
                else if (ch == 'ö') sb.Append("oe");
                else if (ch == 'ü') sb.Append("ue");
                else if (ch == 'ß') sb.Append("ss");
                else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            }
            string r = sb.ToString().Trim('-');
            return r.Length == 0 ? "produkt" : r;
        }

        private static int Pick(int seed, int salt, int count) => count <= 0 ? 0 : (int)(((uint)(seed * 7919 + salt * 104729 + 13)) % (uint)count);

        public static string Domain(FakeSite site) => site == null ? "" : Fill(Domains[Pick(site.Seed, 1, Domains.Length)], site.Product, "", 0);
        public static string Title(FakeSite site) => site == null ? "" : Fill(Titles[Pick(site.Seed, 2, Titles.Length)], site.Product, "", 0);
        public static string Author(FakeSite site) => site == null ? "" : Authors[Pick(site.Seed, 3, Authors.Length)];

        /// <summary>Top-Liste mit count Gründen (ohne Wiederholung).</summary>
        public static List<string> Reasons10(FakeSite site, string brand, int count = 10)
        {
            var list = new List<string>();
            if (site == null) return list;
            count = Math.Min(count, Reasons.Length);
            int start = Pick(site.Seed, 4, Reasons.Length);
            int step = 5; // teilerfremd zu 12 → alle verschieden
            for (int i = 0; i < count; i++) list.Add(Fill(Reasons[(start + i * step) % Reasons.Length], site.Product, brand, 0));
            return list;
        }

        public static List<string> Body(FakeSite site, string brand, int price)
        {
            var list = new List<string>();
            if (site == null) return list;
            int a = Pick(site.Seed, 5, Paragraphs.Length);
            int b = (a + 1 + Pick(site.Seed, 6, Paragraphs.Length - 1)) % Paragraphs.Length;
            list.Add(Fill(Paragraphs[a], site.Product, brand, price));
            list.Add(Fill(Paragraphs[b], site.Product, brand, price));
            return list;
        }

        /// <summary>Kurzer Vorschautext für Suchergebnisse.</summary>
        public static string Snippet(FakeSite site, string brand)
        {
            var r = Reasons10(site, brand, 1);
            return r.Count > 0 ? r[0] : "";
        }
    }
}
