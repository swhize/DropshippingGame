using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame.UI
{
    /// <summary>Formatierungen, die nur die Oberfläche braucht (Wochentage, Dauer ...).</summary>
    public static class UiFmt
    {
        public static readonly string[] Weekdays = { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag" };

        /// <summary>Tag 1 ist ein Montag (wird in der UI berechnet, bis der Core eigene Wochentage hat).</summary>
        public static string Weekday(int day) => Weekdays[((day - 1) % 7 + 7) % 7];

        public static string WeekdayShort(int day) => Weekday(day).Substring(0, 2);

        /// <summary>"Mi · Tag 3"</summary>
        public static string DayShort(int day) => WeekdayShort(day) + " · Tag " + day;

        /// <summary>"Mittwoch, Tag 3"</summary>
        public static string DayLong(int day) => Weekday(day) + ", Tag " + day;

        /// <summary>Spielminuten als "45 min" oder "2 h 05 min".</summary>
        public static string Duration(float minutes)
        {
            int m = Mathf.Max(0, Mathf.CeilToInt(minutes));
            if (m < 60) return m + " min";
            int h = m / 60;
            int r = m % 60;
            return r == 0 ? h + " h" : h + " h " + r.ToString("00") + " min";
        }

        /// <summary>Kompakt für kleine Flächen: "45m" / "2h05".</summary>
        public static string DurationShort(float minutes)
        {
            int m = Mathf.Max(0, Mathf.CeilToInt(minutes));
            if (m < 60) return m + " min";
            return (m / 60) + "h" + (m % 60).ToString("00");
        }

        public static string Percent(float v) => Mathf.RoundToInt(v * 100f) + " %";

        /// <summary>Wie lange bis Feierabend (20 Uhr).</summary>
        public static string UntilClosing(Sim s)
        {
            if (s == null) return "";
            float left = GameData.DayEnd - s.TimeMinutes;
            return left <= 0f ? "Feierabend" : "noch " + Duration(left) + " bis 20:00";
        }

        /// <summary>Anteil des Geschäftstags 08–20 Uhr (0..1).</summary>
        public static float DayProgress(Sim s) =>
            s == null ? 0f : Mathf.Clamp01((s.TimeMinutes - GameData.DayStart) / (GameData.DayEnd - GameData.DayStart));

        public static string Stars(float rating) => Fmt.Rating(rating);

        /// <summary>Entfernt Zeichen, die in den eingebundenen Schriften fehlen könnten.</summary>
        public static string Safe(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("★", "").Replace("☆", "").Replace("→", "»").Trim();
    }

    /// <summary>Anzeige-Daten einer offenen Bestellung (Bestellzettel im HUD und im Handy).</summary>
    public struct OrderView
    {
        public ProductDef Product;
        public int Price;
        /// <summary>Wartezeit in Geschäftsminuten.</summary>
        public float Age;
        /// <summary>Restzeit-Balken 1 → 0.</summary>
        public float Fill;
        /// <summary>"fresh" (Blitz-Bonus), "ok", "late"</summary>
        public string Tone;
        public string TimeText;
        public bool Express;
        public string Customer;
        public string Note;
    }

    /// <summary>
    /// Übersetzt eine Core-Bestellung in einen Bestellzettel. Bewertungsregeln des Core:
    /// unter 90 min = Sterne-Bonus, ab 200 min ein Stern weniger, ab 360 min zwei.
    /// Erweiterungspunkt (Phase B): <see cref="Custom"/> setzen, sobald Bestellungen Fälligkeit,
    /// Express und Kundschaft haben.
    /// </summary>
    public static class OrderInfo
    {
        public const float BonusMinutes = 90f;
        public const float DueMinutes = 200f;
        public const float LateMinutes = 360f;

        /// <summary>Optionaler Ersatz für <see cref="Get"/> (z. B. mit echter Frist aus dem Core).</summary>
        public static Func<Sim, Order, OrderView> Custom;

        public static OrderView Get(Sim s, Order o)
        {
            if (Custom != null)
            {
                try
                {
                    return Custom(s, o);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            var v = new OrderView
            {
                Product = GameData.Product(o != null ? o.Product : ""),
                Price = o != null ? o.Price : 0,
                Customer = "",
                Note = "",
            };
            v.Age = s != null && o != null ? Mathf.Max(0f, s.BClock() - o.Created) : 0f;
            v.Fill = Mathf.Clamp01(1f - v.Age / DueMinutes);
            if (v.Age < BonusMinutes)
            {
                v.Tone = "fresh";
                v.TimeText = UiFmt.DurationShort(DueMinutes - v.Age);
            }
            else if (v.Age < DueMinutes)
            {
                v.Tone = "ok";
                v.TimeText = UiFmt.DurationShort(DueMinutes - v.Age);
            }
            else
            {
                v.Tone = "late";
                v.TimeText = "+" + UiFmt.DurationShort(v.Age - DueMinutes);
            }
            return v;
        }

        public static Color ToneColor(string tone)
        {
            switch (tone)
            {
                case "fresh": return Theme.Teal;
                case "ok": return Theme.Accent;
                default: return Theme.Bad;
            }
        }
    }

    /// <summary>
    /// Merkt sich pro Produkt den zuletzt genutzten Lieferanten und die Bestellgröße
    /// (für „Nachbestellen“ im Handy). Nur für diese Sitzung – bewusst ohne Spielstand-Änderung.
    /// </summary>
    public static class ReorderMemory
    {
        private static readonly Dictionary<string, int> Supplier = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> Bulk = new Dictionary<string, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Supplier.Clear();
            Bulk.Clear();
        }

        public static void Remember(string product, int supplier, int bulk)
        {
            if (string.IsNullOrEmpty(product)) return;
            Supplier[product] = supplier;
            Bulk[product] = bulk;
        }

        /// <summary>Bestellgröße als Nächstes wechseln (nur verfügbare).</summary>
        public static void CycleBulk(Sim s, string product)
        {
            if (s == null) return;
            int cur = GetBulk(s, product);
            for (int i = 1; i <= GameData.BulkOptions.Length; i++)
            {
                int n = (cur + i) % GameData.BulkOptions.Length;
                if (!s.BulkAvailable(n)) continue;
                Bulk[product] = n;
                return;
            }
        }

        public static int GetSupplier(Sim s, string product)
        {
            if (s == null) return 0;
            if (Supplier.TryGetValue(product, out int v) && v >= 0 && v < GameData.Suppliers.Length && s.SupplierAvailable(v)) return v;
            if (s.SupplierAvailable(1)) return 1;
            for (int i = 0; i < GameData.Suppliers.Length; i++)
                if (s.SupplierAvailable(i)) return i;
            return 0;
        }

        public static int GetBulk(Sim s, string product)
        {
            if (s == null) return 0;
            if (Bulk.TryGetValue(product, out int v) && v >= 0 && v < GameData.BulkOptions.Length && s.BulkAvailable(v)) return v;
            return 0;
        }
    }

    /// <summary>Tipps für Ladebildschirme und Übergänge.</summary>
    public static class Tips
    {
        private static readonly string[] All =
        {
            "Knapp unter dem Marktpreis verkaufst du viel – und behältst eine gute Marge.",
            "Mit {phone} öffnest du dein Handy: Bestellungen, Nachrichten und Nachbestellen.",
            "Pakete, die in unter 90 Minuten rausgehen, bringen einen Sterne-Bonus.",
            "Premium-Ware kostet mehr, sorgt aber für bessere Bewertungen.",
            "Ungefaltete Kartons kosten nur die Hälfte – falte sie am Falttisch.",
            "Um 20 Uhr ist Feierabend. Dann werden Miete, Löhne und Zinsen fällig.",
            "Ein Kredit hilft über Engpässe – kostet aber 2 % Zinsen pro Tag.",
            "Der Verkaufsstand bringt Laufkundschaft. Ganz ohne Karton und Versand.",
            "Werbung und TikToks steigern die Nachfrage für eine Weile.",
            "Ist die Warteschlange voll, gehen Bestellungen verloren.",
            "Im Laptop unter Firma » Ziele siehst du, was als Nächstes kommt.",
            "Den vollen Laptop „HustleOS“ gibt es nur am Schreibtisch.",
            "Offene Entscheidungen verfallen um 20 Uhr – dann gilt die vorsichtigste Antwort.",
            "Kalle hat immer einen Spruch parat. Ob er stimmt, ist eine andere Frage.",
        };

        public static string Random()
        {
            string t = All[UnityEngine.Random.Range(0, All.Length)];
            return t.Replace("{phone}", GameInput.KeyLabel("phone"));
        }
    }

    /// <summary>Inhalt des „Was ist neu“-Panels im Hauptmenü (Platzhalter, Text finalisiert die Koordination).</summary>
    public static class WhatsNew
    {
        public const string Version = "3.0";
        public const string Title = "Das große Update";

        public static readonly string[][] Items =
        {
            new[] { "phone", "Dein Handy", "Mit Tab: Bestellzettel, Nachrichten mit Entscheidungen, Schnell-Nachbestellen und Status." },
            new[] { "screen", "HustleOS neu gedacht", "Weniger, dafür stärkere Apps mit Reitern. Neues Standard-Design „Hype“ – nur noch am Schreibtisch." },
            new[] { "paper", "Bestellzettel & Express", "Jede Bestellung mit Kundschaft, Frist und Countdown. Express-Aufträge ab Level 2." },
            new[] { "trend", "Trends & Hype", "Produkte werden heiß – und wieder kalt. Der Trendradar hilft beim Timing." },
            new[] { "factory", "Großaufträge (B2B)", "Firmen bestellen palettenweise. Pünktlich liefern bringt Geld und Ruf." },
            new[] { "box", "Retouren", "Manche Pakete kommen zurück: als B-Ware einlagern oder entsorgen." },
            new[] { "sparkle", "Hustle-Skills & Wochenziele", "Skillbaum mit drei Ästen, jeden Montag neue Wochenziele." },
            new[] { "home", "Neue Welt", "Echte 3D-Modelle, Materialien, Sounds und Musik aus freien Quellen." },
        };
    }
}
