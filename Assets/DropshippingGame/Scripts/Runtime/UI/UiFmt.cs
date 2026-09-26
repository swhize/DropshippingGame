using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame.UI
{
    /// <summary>Formatierungen, die nur die Oberfläche braucht (Wochentage, Dauer ...).</summary>
    public static class UiFmt
    {
        /// <summary>Tag 1 ist ein Montag (Core: GameData.WeekdayName).</summary>
        public static string Weekday(int day) => GameData.WeekdayName(day);

        public static string WeekdayShort(int day) => GameData.WeekdayShort(day);

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
        public string Number, City;
        public OrderStage Stage;
        /// <summary>0 frisch … 1 fällig … 3 überfällig (Core).</summary>
        public float Urgency;
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
                Customer = o?.Customer ?? "",
                Note = o?.Note ?? "",
                Express = o != null && o.Express,
                Number = o != null ? o.Number : "",
                City = o?.City ?? "",
                Stage = o != null ? o.Stage : OrderStage.Queued,
            };
            if (s == null || o == null)
            {
                v.Tone = "fresh";
                v.TimeText = "";
                v.Fill = 1f;
                return v;
            }
            v.Age = Mathf.Max(0f, s.BClock() - o.Created);
            float urg = s.OrderUrgency(o);
            v.Urgency = urg;
            float window = Mathf.Max(1f, o.DueWindow);
            v.Fill = Mathf.Clamp01(s.OrderTimeLeft(o) / window);
            v.Tone = s.OrderOverdue(o) ? "late" : (urg < 0.6f ? "fresh" : "ok");
            v.TimeText = s.OrderTimeLeftText(o);
            return v;
        }

        public static string StageText(OrderStage st)
        {
            switch (st)
            {
                case OrderStage.Picked: return "entnommen";
                case OrderStage.Packed: return "verpackt";
                case OrderStage.Labeled: return "etikettiert";
                case OrderStage.Conveyor: return "auf dem Band";
                default: return "wartet";
            }
        }

        public static string StageIcon(OrderStage st)
        {
            switch (st)
            {
                case OrderStage.Picked: return "check";
                case OrderStage.Packed: return "package";
                case OrderStage.Labeled: return "tag";
                case OrderStage.Conveyor: return "truck";
                default: return "clock";
            }
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

    /// <summary>Tipps für Ladebildschirme und Übergänge.</summary>
    public static class Tips
    {
        private static readonly string[] All =
        {
            "Knapp unter dem Marktpreis verkaufst du viel – und behältst eine gute Marge.",
            "Mit {phone} öffnest du dein Handy: Bestellungen, Nachrichten und Nachbestellen.",
            "Pakete, die weit vor der Frist rausgehen, bringen einen Sterne-Bonus. Express-Zettel zählen doppelt.",
            "Retouren kommen später zurück: am Retourenplatz als B-Ware einlagern oder entsorgen.",
            "Großaufträge ab Level 3: ganze Kisten direkt auf die Palette stellen – ohne Auspacken.",
            "Trendradar im Laptop: Kauf ein, bevor der Hype seinen Peak erreicht.",
            "Jeder Level-Aufstieg bringt einen Skillpunkt – Laptop » Firma » Skills.",
            "Einmal pro Woche darfst du ein Wochenziel tauschen.",
            "Premium-Ware gibt's nur einmal pro Produkt und Tag.",
            "Unsicher, was bremst? Handy » Status zeigt den größten Engpass.",
            "Premium-Ware kostet mehr, sorgt aber für bessere Bewertungen.",
            "Ungefaltete Kartons kosten nur die Hälfte – falte sie am Falttisch.",
            "Um 20 Uhr ist Feierabend. Dann werden Miete, Löhne und Zinsen fällig.",
            "Ein Kredit hilft über Engpässe – kostet aber 2 % Zinsen pro Tag.",
            "Der Verkaufsstand bringt Laufkundschaft. Ganz ohne Karton und Versand.",
            "Werbung und TikToks steigern die Nachfrage für eine Weile.",
            "Ist die Warteschlange voll, gehen Bestellungen verloren.",
            "Im Laptop unter Firma » Ziele stehen Meilensteine und Wochenziele.",
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
            new[] { "phone", "Dein Handy ({phone})", "Bestellzettel mit Kundschaft und Frist, Nachrichten mit Entscheidungen und Großauftrags-Angeboten, Schnell-Nachbestellen, Wochenziele und Status." },
            new[] { "paper", "Bestellzettel & Express", "Jede Bestellung hat Nummer, Kundschaft, Notiz und Countdown – oben rechts im HUD. Express ab Level 2: schneller, teurer, strenger bewertet." },
            new[] { "return", "Retouren", "Manche Pakete kommen zurück (Grund steht drauf). Am Retourenplatz als B-Ware einlagern oder entsorgen. Billig-Ware und zu große Kartons erhöhen die Quote." },
            new[] { "fire", "Trends & Hype", "Produkte werden heiß – und wieder kalt. Trendradar mit Verlauf und Prognose-Band in „Markt & Trends“." },
            new[] { "pallet", "Großaufträge (B2B)", "Ab Level 3 bestellen Firmen palettenweise. Annehmen, am Palettenplatz liefern, Frist halten – sonst Vertragsstrafe." },
            new[] { "sparkle", "Hustle-Skills", "Ein Skillpunkt pro Level: Logistik, Vertrieb, Marketing – je 4 Stufen. Umschulung jederzeit gegen Gebühr." },
            new[] { "flag", "Wochenziele", "Jeden Montag drei neue Ziele mit Geld und XP. Eines pro Woche darfst du tauschen." },
            new[] { "bulb", "Engpass-Assistent", "„Warum läuft's nicht?“ – Übersicht und Handy nennen, was gerade am meisten bremst, samt Lösung." },
            new[] { "receipt", "Kassenbon & Wochentage", "Tagesabrechnung als Bon mit Retouren, Strafen und Aufträgen. Tag 1 ist Montag; samstags wird mehr bestellt." },
            new[] { "screen", "HustleOS neu", "Weniger, dafür stärkere Apps mit Reitern, Design „Hype“, Controller-Steuerung, UI-Skalierung." },
            new[] { "coin", "Balance", "Premium-Lieferant 1× pro Produkt und Tag, Tageskampagnen, max. 3 TikToks pro Tag, fairere Trading-Kurse." },
        };
    }
}
