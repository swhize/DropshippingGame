using System;
using System.Collections.Generic;
using System.Text;

namespace DropshippingGame.Core
{
    /// <summary>Ein Skill im Hustle-Skillbaum (3 Äste × 4 Stufen).</summary>
    public sealed class SkillDef
    {
        /// <summary>Eindeutige ID, z. B. "l_arme".</summary>
        public string Id;
        /// <summary>Ast: "logistik", "vertrieb" oder "marketing".</summary>
        public string Branch;
        public string Name, Desc, Icon;
        /// <summary>Stufe im Ast (1-4). Stufe n braucht Stufe n-1 im selben Ast.</summary>
        public int Tier;
        /// <summary>Benötigtes Firmenlevel.</summary>
        public int Level;
    }

    /// <summary>Ein Ast des Skillbaums (für die Anzeige).</summary>
    public sealed class SkillBranchDef
    {
        public string Id, Name, Icon, Desc;
        public RGBA Color;
    }

    /// <summary>
    /// v3.0-Inhalte: Wochentage, Bestellzettel (Kundschaft, Orte, Notizen), Retouren, Trends,
    /// Großaufträge (Firmen), Hustle-Skills, Wochenziele und Tastenhinweise für Texte.
    /// </summary>
    public static partial class GameData
    {
        // =====================================================================================
        // Wochentage (Tag 1 = Montag)
        // =====================================================================================
        public static readonly string[] WeekdayNames = { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag" };
        public static readonly string[] WeekdayShorts = { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };

        /// <summary>Nachfrage-Faktor je Wochentag (am Wochenende wird mehr am Handy geshoppt). Mittelwert ≈ 1.</summary>
        public static readonly float[] WeekdayDemand = { 0.95f, 0.95f, 1.0f, 1.0f, 1.05f, 1.1f, 1.0f };

        public static int WeekdayOf(int day) => ((Math.Max(1, day) - 1) % 7);
        public static string WeekdayName(int day) => WeekdayNames[WeekdayOf(day)];
        public static string WeekdayShort(int day) => WeekdayShorts[WeekdayOf(day)];

        // =====================================================================================
        // Tastenhinweise in Texten
        // =====================================================================================
        /// <summary>
        /// Optional von der Oberfläche gesetzt: liefert die Taste für eine Aktion ("interact", "drop",
        /// "phone", "laptop", "pause", "help" ...). Liefert sie nichts oder den Aktionsnamen selbst,
        /// gilt die Standard-Tastaturbelegung aus <see cref="DefaultKey"/>.
        /// </summary>
        public static Func<string, string> KeyLabel;

        public static string DefaultKey(string action)
        {
            switch (action)
            {
                case "interact": return "E";
                case "drop": return "G";
                case "phone": return "Tab";
                case "laptop": return "Tab";
                case "pause": return "Esc";
                case "help": return "F1";
                case "jump": return "Leertaste";
            }
            return action;
        }

        public static string Key(string action)
        {
            string s = null;
            try
            {
                s = KeyLabel?.Invoke(action);
            }
            catch (Exception)
            {
                s = null;
            }
            if (string.IsNullOrEmpty(s) || s == action) s = DefaultKey(action);
            return s;
        }

        /// <summary>Ersetzt Platzhalter <c>{key:aktion}</c> durch die Taste.</summary>
        public static string FillKeys(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("{key:", StringComparison.Ordinal) < 0) return text ?? "";
            var sb = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                int start = text.IndexOf("{key:", i, StringComparison.Ordinal);
                if (start < 0)
                {
                    sb.Append(text, i, text.Length - i);
                    break;
                }
                int end = text.IndexOf('}', start);
                if (end < 0)
                {
                    sb.Append(text, i, text.Length - i);
                    break;
                }
                sb.Append(text, i, start - i);
                sb.Append(Key(text.Substring(start + 5, end - start - 5)));
                i = end + 1;
            }
            return sb.ToString();
        }

        public static string TutorialText(int step) =>
            step >= 0 && step < Tutorial.Length ? FillKeys(Tutorial[step].Text) : "";

        // =====================================================================================
        // F1 Bestellzettel
        // =====================================================================================
        /// <summary>Frist einer normalen Bestellung (Geschäftsminuten, 240 = 4 Spielstunden ≈ 3 echte Minuten).</summary>
        public const float OrderDueMinutes = 240f;
        public const float ExpressDueMinutes = 100f;
        /// <summary>Wartende Express-Bestellungen werden nach dieser Zeit storniert (normale: <see cref="OrderExpireMinutes"/>).</summary>
        public const float ExpressExpireMinutes = 360f;
        public const float ExpressPriceMult = 1.4f;
        public const int ExpressMinLevel = 2;
        public const float ExpressBaseChance = 0.16f;
        public const float ExpressChancePerLevel = 0.012f;
        public const float ExpressMaxChance = 0.25f;
        public const float ExpressReviewChance = 0.7f;
        /// <summary>Express-Bewertungen zählen so viel stärker in die Durchschnittsbewertung.</summary>
        public const float ExpressReviewWeight = 1.6f;
        /// <summary>Neu gelistete, noch nie verkaufte Produkte bekommen innerhalb dieser Zeitspanne eine erste Bestellung.</summary>
        public const float LaunchOrderMin = 20f;
        public const float LaunchOrderMax = 45f;
        public const int FirstOrderId = 1001;

        public static readonly string[] CustomerFirstNames =
        {
            "Sabine", "Dennis", "Gerda", "Jürgen", "Laura", "Tobias", "Mehmet", "Aylin", "Kevin", "Chantal", "Jonas", "Leonie",
            "Marie", "Paul", "Hannah", "Felix", "Ingrid", "Horst", "Fatma", "Olga", "Luca", "Emma", "Ben", "Mia", "Finn",
            "Sophie", "Karl-Heinz", "Uschi", "Dieter", "Svenja", "Can", "Elif", "Niklas", "Janine", "Rüdiger", "Bärbel",
            "Yusuf", "Anastasia", "Pascal", "Jacqueline", "Magdalena", "Dragan", "Lina", "Emil", "Frieda", "Theo", "Gisela",
            "Malte", "Zeynep", "Detlef",
        };

        public static readonly string[] CustomerInitials =
        {
            "A.", "B.", "D.", "F.", "G.", "H.", "K.", "L.", "M.", "N.", "P.", "R.", "S.", "T.", "W.", "Y.", "Z.",
        };

        public static readonly string[] Cities =
        {
            "Bottrop", "Castrop-Rauxel", "Wanne-Eickel", "Bielefeld", "Buxtehude", "Wuppertal", "Gelsenkirchen", "Hamburg",
            "Berlin", "München", "Köln", "Leipzig", "Dresden", "Stuttgart", "Kiel", "Rostock", "Paderborn", "Oer-Erkenschwick",
            "Hintertupfingen", "Kleinkleckersdorf", "Wolfsburg", "Oberammergau", "Fulda", "Gütersloh", "Herne", "Moers",
            "Zwickau", "Lüneburg", "Bad Salzuflen", "Wattenscheid", "Wismar", "Görlitz", "Tuttlingen", "Bremen", "Mainz",
        };

        public static readonly string[] OrderNotes =
        {
            "Bitte nicht klingeln – Baby schläft.",
            "Ist ein Geschenk. Bitte keine Rechnung beilegen!",
            "Falls ich nicht da bin: beim Nachbarn mit dem Gartenzwerg.",
            "Bitte schnell, ich hab's meiner Freundin schon versprochen.",
            "Hab's auf TikTok gesehen. Hoffe, es ist echt.",
            "Paket bitte in die blaue Tonne legen. NICHT die graue.",
            "Kann man das auch in Lila haben? Egal, nehm ich so.",
            "Meine dritte Bestellung diese Woche. Ich hab ein Problem.",
            "Bitte gut polstern, mein Postbote wirft gern.",
            "Für meinen Opa. Er versteht kein Internet, aber er liebt Pakete.",
            "Keine Eile. (Doch, bitte beeilen.)",
            "Bitte nicht in die Garage legen, da wohnt ein Marder.",
            "Achtung: Der Hund ist lieb, die Katze nicht.",
            "Lieferung bitte nach 17 Uhr. Vorher bin ich im Homeoffice (also eigentlich da).",
            "Wenn das gut ist, bestell ich nochmal. Für die ganze Familie.",
            "Mein Horoskop sagt: heute online shoppen.",
            "Bitte mit Liebe verpacken. Oder mit Klebeband. Hauptsache fest.",
            "Hab aus Versehen auf 'Kaufen' gedrückt. Passt aber schon.",
            "Grüße an die Person, die das hier liest!",
            "Bitte diskret verpacken, meine Frau zählt die Pakete.",
        };

        public static readonly string[] ExpressNotes =
        {
            "EXPRESS! Die Party ist heute Abend!",
            "Brauche es SOFORT. Also, so sofort wie möglich.",
            "Express, weil Geduld nicht mein Ding ist.",
            "Geburtstag ist morgen und ich bin ein schlechter Mensch. Bitte schnell!",
            "Doppelt bezahlt, doppelt so schnell? So hab ich das verstanden.",
            "Mein Chef kommt gleich. Bitte vorher liefern.",
        };

        /// <summary>Produktbezogene Notizen (werden gelegentlich statt der allgemeinen gewählt).</summary>
        public static readonly Dictionary<string, string[]> ProductNotes = new Dictionary<string, string[]>
        {
            { "huelle", new[] { "Passt die auch auf ein Nokia 3310?", "Die Farbe muss zu meinem Nagellack passen." } },
            { "led", new[] { "Für meinen Balkon. Die Nachbarn sollen neidisch werden.", "Weihnachten ist bei mir das ganze Jahr." } },
            { "massage", new[] { "Mein Rücken dankt dir schon jetzt.", "Für meinen Mann. Er jammert zu viel." } },
            { "kopfhoerer", new[] { "Damit ich meinen Mitbewohner nicht mehr hören muss.", "Bitte mit extra viel Bass." } },
            { "ringlicht", new[] { "Ich werde Influencer. Ab Montag.", "Für bessere Videocalls. Mein Chef sieht nur Schatten." } },
            { "katzenbrunnen", new[] { "Meine Katze trinkt nur fließendes Wasser. Sie ist eine Diva.", "Für Herrn Schnurrbert. Er hat Ansprüche." } },
            { "haltung", new[] { "Mein Chef sagt, ich sehe aus wie ein Fragezeichen.", "Zu viel Handy. Weiß ich selbst." } },
            { "smartwatch", new[] { "Damit ich meine Schritte zählen kann. Es sind nicht viele.", "Endlich pünktlich sein. Vielleicht." } },
            { "beamer", new[] { "Kinoabend im Schrebergarten!", "Für PowerPoint-Karaoke im Büro." } },
            { "drohne", new[] { "Nur für Landschaftsaufnahmen. Ehrlich.", "Mein Nachbar hat auch eine. Jetzt ist Krieg." } },
        };

        // =====================================================================================
        // F2 Retouren
        // =====================================================================================
        public const float ReturnBaseChance = 0.03f;
        public const float ReturnMaxChance = 0.35f;
        /// <summary>Verspätete Pakete kommen öfter zurück.</summary>
        public const float ReturnLateMult = 1.5f;
        /// <summary>Zu großer Karton (Ware rutscht) erhöht das Risiko.</summary>
        public const float ReturnOversizeMult = 1.6f;
        /// <summary>Rücksendungen treffen so viele Geschäftsminuten nach dem Versand ein.</summary>
        public const float ReturnDelayMin = 150f;
        public const float ReturnDelayMax = 480f;
        /// <summary>B-Ware: Qualität der Retoure × diesen Faktor beim Einlagern.</summary>
        public const float BStockQualityMult = 0.75f;
        public const int ReturnRestockXp = 3;
        public const int ReturnDisposeXp = 1;

        public static readonly string[] ReturnReasons =
        {
            "Gefällt mir doch nicht.", "Die Farbe sieht in echt anders aus.", "Hab's aus Versehen doppelt bestellt.",
            "Mein Mann hat das gleiche schon gekauft.", "Passt nicht zu meinem Sternzeichen.", "Hab's mir irgendwie anders vorgestellt.",
            "Meine Katze mag es nicht.", "Zu schön für mich. Ernsthaft.",
        };

        public static readonly string[] ReturnReasonsQuality =
        {
            "Qualität ist leider mies.", "Nach zwei Tagen kaputt.", "Riecht komisch.", "Wackelt, knarzt und leuchtet falsch.",
        };

        public const string ReturnReasonLate = "Kam zu spät. Brauche ich jetzt nicht mehr.";
        public const string ReturnReasonOversize = "Karton viel zu groß – die Ware ist drin herumgeflogen.";

        // =====================================================================================
        // F3 Trends / Hype
        // =====================================================================================
        public const float TrendMin = 0.5f;
        public const float TrendMax = 2.0f;
        public const int TrendHistoryDays = 14;
        public const int TrendForecastDays = 3;
        /// <summary>Höchstens so viele Produkte sind gleichzeitig im Aufwind oder auf dem Höhepunkt.</summary>
        public const int MaxHypedProducts = 2;
        public const float TrendAccuracyBase = 0.45f;
        public const float TrendAccuracySkill = 0.85f;

        public static readonly string[] TrendPhaseNames = { "stabil", "steigend", "Hype!", "fallend", "tot" };

        public static readonly string[] TrendReasonsRising =
        {
            "Ein Streamer hat {p} live in die Kamera gehalten.",
            "#{tag} geht auf TikTok viral.",
            "Ein Promi wurde mit {p} beim Bäcker gesehen.",
            "Eine Oma erklärt {p} auf YouTube – drei Millionen Aufrufe.",
            "Das Frühstücksfernsehen hat {p} zum Must-have erklärt.",
        };

        public static readonly string[] TrendReasonsPeak =
        {
            "Alle wollen {p}. Wirklich alle.", "{p} ist DAS Gesprächsthema in jeder Kaffeeküche.",
            "Selbst Kalle fragt nach {p}.",
        };

        public static readonly string[] TrendReasonsFalling =
        {
            "Der Hype um {p} flaut ab.", "Die ersten Memes über {p} sind schon cringe.", "Das Internet schaut schon woanders hin.",
        };

        public static readonly string[] TrendReasonsDead =
        {
            "{p} ist so 2019.", "Ein Tech-YouTuber nennt {p} „Elektroschrott mit Marketing“.", "Niemand redet mehr über {p}. Niemand.",
        };

        public const string TrendReasonNormal = "{p} verkauft sich wieder ganz normal.";

        // =====================================================================================
        // F4 Großaufträge (B2B)
        // =====================================================================================
        public const int ContractLevel = 3;
        /// <summary>Grundwert eines Auftrags in €: (Basis + je Level) × Faktor in der Lagerhalle × Zufall 0,85-1,25.</summary>
        public const float ContractValueBase = 60f;
        public const float ContractValuePerLevel = 55f;
        public const float ContractWarehouseMult = 1.25f;
        /// <summary>Vergütung je Stück als Anteil am Richtpreis (B2B = Großhandelspreis).</summary>
        public const float ContractPayMin = 0.3f;
        public const float ContractPayMax = 0.4f;
        public const float ContractPenaltyFactor = 0.25f;
        public const float ContractXpFactor = 0.2f;
        /// <summary>So viele Stück lässt die Lagerist:in beim Bestücken von Paletten im Regal (für Kundenbestellungen).</summary>
        public const int ContractStockReserve = 20;
        public const float ContractRepBonus = 0.06f;
        public const float ContractRepPenalty = 0.18f;
        /// <summary>Scheitert ein Auftrag, werden bereits gelieferte Stück zu diesem Anteil bezahlt.</summary>
        public const float ContractPartialPay = 0.5f;
        /// <summary>So viele abgeschlossene/abgelehnte Aufträge bleiben für die Anzeige gespeichert.</summary>
        public const int ContractHistory = 12;

        public static int MaxActiveContracts(int level) => level < ContractLevel ? 0 : (level >= 8 ? 3 : (level >= 5 ? 2 : 1));

        public static readonly string[] Companies =
        {
            "Büro-Bedarf Möller GmbH", "Kita Sonnenschein e. V.", "Fitnessstudio Muckibude", "Autohaus Brummer & Söhne",
            "Hotel Zur Goldenen Gans", "Zahnarztpraxis Dr. Bohr", "SynergyHub Start-up GmbH", "Katzencafé Schnurrhaus",
            "TSV Kickers 09", "Bäckerei Krümel", "Seniorenstift Abendrot", "Coworking „Die Denkfabrik“", "Influencer-Agentur GlowUp",
            "Friseursalon Haarmonie", "Umzüge Schlepp & Trag", "Yoga-Studio Om Sweet Om", "Tattoo-Studio Nadelöhr",
            "Pizzeria Mamma Mia", "Gaming-Café Respawn", "Gartencenter Grünzeug", "Verein der Gartenzwergfreunde e. V.",
            "Hochzeitsagentur Ja-Wort GmbH", "Escape Room „Kein Ausweg“", "IT-Beratung Byte & Söhne", "Stadtwerke Kleinkleckersdorf",
            "Tanzschule Wiener Walzer", "Fahrschule Vollgas", "Physiopraxis Rückgrat",
        };

        public static readonly string[] ContractReasons =
        {
            "für die Weihnachtsfeier", "als Kundengeschenke", "für die Tombola", "für das Sommerfest", "für neue Mitarbeitende",
            "zum Firmenjubiläum", "für eine Marketing-Aktion", "für den Tag der offenen Tür", "als Preise für ein Gewinnspiel",
            "zur Motivation der Belegschaft (statt Gehaltserhöhung)", "für die Goodie-Bags beim Firmenlauf",
        };

        // =====================================================================================
        // F5 Hustle-Skills
        // =====================================================================================
        /// <summary>Skillpunkte zum Start (dazu 1 je Level-Aufstieg).</summary>
        public const int StartSkillPoints = 1;
        /// <summary>Benötigtes Firmenlevel je Stufe 1-4.</summary>
        public static readonly int[] SkillTierLevels = { 1, 3, 5, 7 };
        public const int RespecCostPerLevel = 150;

        public static readonly SkillBranchDef[] SkillBranches =
        {
            new SkillBranchDef { Id = "logistik", Name = "Logistik", Icon = "truck", Color = new RGBA(0.3f, 0.6f, 0.95f),
                Desc = "Schneller tragen, mehr lagern, schneller liefern." },
            new SkillBranchDef { Id = "vertrieb", Name = "Vertrieb", Icon = "coin", Color = new RGBA(0.35f, 0.78f, 0.45f),
                Desc = "Günstiger einkaufen, weniger Retouren, bessere Großaufträge." },
            new SkillBranchDef { Id = "marketing", Name = "Marketing", Icon = "mega", Color = new RGBA(0.92f, 0.4f, 0.65f),
                Desc = "Stärkere TikToks, günstigere Werbung, Trends vorhersehen." },
        };

        public static readonly SkillDef[] Skills =
        {
            // Logistik
            new SkillDef { Id = "l_arme", Branch = "logistik", Tier = 1, Level = 1, Icon = "box", Name = "Starke Arme",
                Desc = "Kisten bremsen dich nicht mehr aus, und mit vollen Händen läufst du 10 % schneller. Proteinshake inklusive." },
            new SkillDef { Id = "l_tetris", Branch = "logistik", Tier = 2, Level = 3, Icon = "rack", Name = "Lager-Tetris",
                Desc = "Du stapelst wie ein Weltmeister: +30 % Lagerplatz." },
            new SkillDef { Id = "l_wege", Branch = "logistik", Tier = 3, Level = 5, Icon = "truck", Name = "Kurze Wege",
                Desc = "Du kennst jeden Lieferanten persönlich: Lieferungen kommen 30 % schneller." },
            new SkillDef { Id = "l_flow", Branch = "logistik", Tier = 4, Level = 7, Icon = "gear", Name = "Prozess-Flow",
                Desc = "Lean Management, Baby: Dein Team arbeitet 25 % schneller, das Förderband doppelt so schnell." },
            // Vertrieb
            new SkillDef { Id = "v_feilschen", Branch = "vertrieb", Tier = 1, Level = 1, Icon = "coin", Name = "Feilschen",
                Desc = "Hart, aber herzlich verhandelt: Einkaufspreise −10 %." },
            new SkillDef { Id = "v_kulanz", Branch = "vertrieb", Tier = 2, Level = 3, Icon = "heart", Name = "Kundenflüsterer",
                Desc = "Ehrliche Produktfotos, freundliche Mails: 35 % weniger Retouren." },
            new SkillDef { Id = "v_netzwerk", Branch = "vertrieb", Tier = 3, Level = 5, Icon = "users", Name = "Networking",
                Desc = "Visitenkarten mit Goldrand: Großaufträge zahlen 20 % mehr, +1 gleichzeitiger Großauftrag." },
            new SkillDef { Id = "v_stamm", Branch = "vertrieb", Tier = 4, Level = 7, Icon = "star", Name = "Stammkundschaft",
                Desc = "Deine Kundschaft verzeiht dir fast alles: schlechte Bewertungen wirken nur halb so stark, +3 Plätze in der Warteschlange." },
            // Marketing
            new SkillDef { Id = "m_content", Branch = "marketing", Tier = 1, Level = 1, Icon = "phone", Name = "Content Creator",
                Desc = "Ringlicht an, Selfie-Stick raus: TikToks wirken 25 % stärker und länger." },
            new SkillDef { Id = "m_radar", Branch = "marketing", Tier = 2, Level = 3, Icon = "eye", Name = "Trendradar",
                Desc = "Du scrollst jetzt beruflich: Die Trendprognose wird viel genauer und sieht 3 Tage voraus." },
            new SkillDef { Id = "m_viral", Branch = "marketing", Tier = 3, Level = 5, Icon = "fire", Name = "Viral-Gen",
                Desc = "TikTok-Abklingzeit −40 %, Werbekampagnen 25 % günstiger." },
            new SkillDef { Id = "m_trendsetter", Branch = "marketing", Tier = 4, Level = 7, Icon = "sparkle", Name = "Trendsetter",
                Desc = "Du machst die Trends: Ein gutes eigenes TikTok (ab 60 % Treffer) löst einen Hype für dein Produkt aus." },
        };

        public static SkillDef Skill(string id)
        {
            foreach (var s in Skills)
                if (s.Id == id) return s;
            return null;
        }

        public static SkillBranchDef SkillBranch(string id)
        {
            foreach (var b in SkillBranches)
                if (b.Id == id) return b;
            return null;
        }

        // =====================================================================================
        // F6 Wochenziele
        // =====================================================================================
        public const int ChallengesPerWeek = 3;
        /// <summary>Belohnung je Wochenziel: (Basis + je Level) × Schwierigkeitsfaktor (0,8-1,2) in €, dazu XP.</summary>
        public const float ChallengeRewardBase = 50f;
        public const float ChallengeRewardPerLevel = 30f;
        public const int ChallengeXpBase = 30;
        public const int ChallengeXpPerLevel = 15;

        // =====================================================================================
        // Erklär-Nachrichten (einmalig ins Postfach)
        // =====================================================================================
        public const string CoachName = "Marvin (Hustle-Akademie)";

        public static readonly string[] MailWeek =
        {
            "Deine Wochenziele",
            "Guten Morgen, Champion! Jeden Montag bekommst du drei Wochenziele. Schaffst du sie bis Sonntagabend, gibt's Geld und Erfahrung. Rise and grind! (Aber schlaf trotzdem genug.)",
        };

        public static readonly string[] MailSkills =
        {
            "Du hast Skillpunkte!",
            "Level-Up! Für jeden Aufstieg bekommst du einen Skillpunkt (einen hattest du schon vom Start). Verteil sie in der Firma-App unter 'Skills': Logistik, Vertrieb oder Marketing. Für alles reicht es nicht – wähle weise.",
        };

        public static readonly string[] MailContracts =
        {
            "Großaufträge freigeschaltet",
            "Jetzt fragen auch Firmen bei dir an! Angebote findest du in der App 'Aufträge'. Nimm nur an, was du schaffst: Ganze Kisten gibst du direkt am Palettenplatz ab – ohne Auspacken. Einzelne Artikel nimmst du dafür aus dem Regal. Aber Achtung: Frist verpasst = Vertragsstrafe.",
        };

        public static readonly string[] MailReturns =
        {
            "Deine erste Retoure",
            "Tja, nicht jede Kundschaft bleibt glücklich. Retouren landen am Wareneingang, die Erstattung ist schon abgebucht. Bring das Päckchen zum Retourenplatz: als B-Ware zurück ins Lager (etwas schlechtere Qualität) oder ab in die Tonne. Tipp: Bessere Ware, passende Kartons und pünktlicher Versand = weniger Retouren.",
        };

        public static readonly string[] MailExpress =
        {
            "Express-Bestellungen",
            "Manche Kundschaft hat's eilig: Express-Bestellungen zahlen 40 % mehr, haben aber eine viel kürzere Frist. Pünktlich = Jubel-Bewertung, zu spät = Drama. Du erkennst sie am Blitz auf dem Bestellzettel.",
        };
    }
}
