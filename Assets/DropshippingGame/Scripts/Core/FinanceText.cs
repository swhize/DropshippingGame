using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Texte des Finanzviertels: Börsen-Schlagzeilen, Makler-Geschwafel, Bankberater-Sprüche.
    /// Bewusst kurz – wird später übersetzt.
    /// </summary>
    public static class FinanceText
    {
        // ---- Schlagzeilen je Aktie (gut / schlecht) -----------------------------------------------
        private static readonly Dictionary<string, string[][]> Stock = new Dictionary<string, string[][]>
        {
            {
                "KART", new[]
                {
                    new[] { "Kartonwerk Müller: Rekordnachfrage dank Dropshipping-Boom", "Karton des Jahres: Müller gewinnt den goldenen Tesafilm", "Kartonwerk erfindet Karton, der sich selbst faltet" },
                    new[] { "Kartonwerk Müller: Lager voller Luftpolsterfolie geplatzt", "Studie: Kartons sind eckig. Anleger verunsichert", "Kartonwerk-Chef verwechselt Karton mit Pizza-Box" },
                }
            },
            {
                "PAKT", new[]
                {
                    new[] { "PaketPiraten liefern erstmals an die richtige Adresse", "PaketPiraten testen Lieferung per Möwe – erfolgreich", "PaketPiraten: Pakete kommen jetzt sogar heil an" },
                    new[] { "PaketPiraten: Paket seit drei Wochen in Zustellung", "Streik bei PaketPiraten: Fahrer wollen Pinkelpausen", "PaketPiraten werfen Paket aufs Dach. Wieder." },
                }
            },
            {
                "HYPE", new[]
                {
                    new[] { "HypeHaus nimmt Hamster-Influencer unter Vertrag", "HypeHaus-Video viral: Ich esse einen Karton", "HypeHaus erfindet neuen Tanz. Alle machen mit." },
                    new[] { "HypeHaus-Star im Shitstorm: Werbung nicht markiert", "Algorithmus-Update: HypeHaus-Reichweite halbiert", "HypeHaus-Influencer zeigt aus Versehen echtes Leben" },
                }
            },
            {
                "FRIT", new[]
                {
                    new[] { "Frittenwerk erfindet Pommes mit Mayo innen", "Hitzewelle? Egal – Frittenwerk meldet Rekordquartal", "Frittenwerk eröffnet Drive-in für Fahrräder" },
                    new[] { "Kartoffelknappheit: Frittenwerk rationiert Ketchup", "Ministerium empfiehlt Salat. Frittenwerk schockiert", "Fritteuse in Zentrale explodiert. Duftet aber gut." },
                }
            },
            {
                "BOTX", new[]
                {
                    new[] { "Botomat-KI schreibt besseres Gedicht als Praktikant", "Botomat zeigt KI-Toaster mit Gefühlen", "Botomat-KI löst Sudoku in 0,1 Sekunden. Investoren weinen." },
                    new[] { "Botomat-KI bestellt aus Versehen 40.000 Gummienten", "Botomat-Chatbot beleidigt den Vorstand", "Botomat-KI will Urlaub. Server streiken." },
                }
            },
            {
                "ROLL", new[]
                {
                    new[] { "Voltwagen fischt 500 Roller aus dem Kanal – Bilanz gerettet", "Stadt erlaubt E-Roller im Park", "Voltwagen-Roller fahren jetzt auch bergauf" },
                    new[] { "Voltwagen-Roller fahren nur noch rückwärts", "Rückruf: Voltwagen-Akku riecht nach Fritten", "Roller-Verbot in der Innenstadt. Voltwagen geschockt." },
                }
            },
        };

        public static string StockHeadline(string id, bool good, Rng rng)
        {
            if (id != null && Stock.TryGetValue(id, out var sets))
            {
                var set = sets[good ? 0 : 1];
                return set[rng.Index(set.Length)];
            }
            return rng.Pick(good ? MarketGood : MarketBad);
        }

        public static readonly string[] MarketGood =
        {
            "Leitzins gesenkt: Börse feiert mit Sekt aus Pappbechern",
            "Analysten: Alles wird gut. Vermutlich.",
            "Konsumlaune steigt: Alle kaufen Dinge, die sie nicht brauchen",
            "Börsenguru Onkel Ralf: Jetzt einsteigen!",
        };

        public static readonly string[] MarketBad =
        {
            "Inflationsangst: Die Börse zittert",
            "Zentralbank-Chef niest im Interview – Märkte panisch",
            "Rezession? Analysten zählen nach. Mit den Fingern.",
            "Gerücht: Jemand hat den Stecker der Börse gezogen",
        };

        public static readonly string[] CoinGood =
        {
            "{0}: Promi postet Raketen-Emoji",
            "{0} jetzt im Späti als Zahlungsmittel akzeptiert",
            "{0}: Wal kauft alles auf",
            "{0} bekommt ein eigenes Maskottchen",
        };

        public static readonly string[] CoinBad =
        {
            "{0}: Gründer wäscht Passwort-Zettel mit",
            "{0}-Börse kurz offline. Seit gestern.",
            "{0}: Wal verkauft alles. Der Wal hatte keine Lust mehr.",
            "{0}: Whitepaper war nur ein Einkaufszettel",
        };

        public static readonly string[] CryptoGood =
        {
            "Krypto-Sommer! Alle Coins grün",
            "Großbank kauft Krypto. Niemand weiß, warum.",
        };

        public static readonly string[] CryptoBad =
        {
            "Krypto-Winter: Coins frieren ein",
            "Regierung prüft Krypto. Krypto prüft Ausgang.",
        };

        // ---- Makler auf dem Parkett ----------------------------------------------------------------
        public static readonly string[] BrokerBabble =
        {
            "Short the dip!", "Synergie!", "Bullish auf Bärenmarkt!", "Kaufen! Nein, verkaufen!", "Das ist der Boden!",
            "Diamond Hands!", "Hebel mal zehn!", "Wer hat meinen Kaffee geshortet?", "Buy the rumor!", "Sell the news!",
            "Disruptiv!", "Skalierbar!", "To the moon!", "Margin Call?!", "Liquidität!", "Rendite! Rendite!",
            "Mehr Kaffee, mehr Alpha!", "Blockchain!", "KI! KI! KI!", "Kopf-Schulter-Kopf!", "Das Chart ist eine Ente!",
            "Gewinne mitnehmen!", "Welche Gewinne?", "Langfristig! Also bis Mittag!", "Antizyklisch!", "Ich spür das!",
        };

        public static readonly string[] BrokerPanic =
        {
            "PANIK!!", "ALLES VERKAUFEN!", "WO IST DER STOP-LOSS?!", "MEIN BONUS!!", "MAMA, ICH BIN REICH!", "RAKETE!!!",
            "HALTET DIE KURSE FEST!", "NICHT JETZT, MARKT!", "ICH HAB'S GEWUSST!", "JEMAND SOLL WAS TUN!",
        };

        public const string BrokerUp = "{0} geht ab!";
        public const string BrokerDown = "{0} crasht!";

        // ---- Bank --------------------------------------------------------------------------------------
        public static readonly string[] BankerLines =
        {
            "Willkommen bei der Kiezbank. Ihr Geld ist bei uns sicher. Meistens.",
            "Schon an einen Bausparvertrag für Ihre Kartons gedacht?",
            "Kredit? Gerne! Zinsen? Auch gerne.",
            "Vertrauen ist gut, Gebühren sind besser.",
            "Die Kontoführungsgebühr ist abgeschafft. Es gibt jetzt eine Kontoatmungsgebühr. Scherz!",
            "Ihr Berater ist heute leider im Homeoffice. Ich bin sein Pappaufsteller.",
            "Sparen lohnt sich! Ab 50.000 € fragen wir aber nach.",
        };

        public static readonly string[] AtmLines =
        {
            "Bitte Karte einführen. Oder einfach so tun.",
            "Heute 0 € Gebühr! Gilt nicht heute.",
            "Ihr Kontostand wird geladen ... bitte nicht weinen.",
        };

        public static readonly string[] SavingsTips =
        {
            "Tagesgeld: jederzeit verfügbar, Zinsen über Nacht.",
            "Mit Schulden gibt es keine Sparzinsen. Wir sind ja nicht blöd.",
            "Reicht das Girokonto am Abend nicht, buchen wir automatisch vom Sparkonto um.",
        };
    }
}
