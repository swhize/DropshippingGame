namespace DropshippingGame.Core
{
    public sealed class BoostEffect
    {
        public float Mult, Minutes;
        public string Name;
        /// <summary>"" = alle Produkte, "random_listed" = zufälliges gelistetes Produkt</summary>
        public string Product = "";
    }

    /// <summary>v3.0: Ereignis setzt ein Produkt in eine Trend-Phase.</summary>
    public sealed class HypeEffect
    {
        public TrendPhase Phase = TrendPhase.Rising;
        /// <summary>Höhe des Hypes (0 = Standard).</summary>
        public float Peak;
        /// <summary>"" oder "random_listed" = Produkt aus dem Ereignis-Kontext, sonst eine Produkt-ID.</summary>
        public string Product = "";
    }

    /// <summary>Wirkungen eines Ereignisses oder einer Entscheidung. Nicht gesetzte Felder wirken nicht.</summary>
    public sealed class Effects
    {
        public int? Money;
        public float? MoneyPct;
        public float? Rep;
        public float? Awareness;
        public BoostEffect Boost;
        public int? BlockSupplier;
        public int BlockDays;
        public float? DelayDelivery;
        public float? DamageNext;
        public float? PriceWarMult;
        public int PriceWarDays;
        public float? Offline;
        public float? TimeSkip;
        public int? SellStockUnits;
        public float SellStockPriceMult = 1f;
        public float? StockLoss;
        public float? TikTok;
        public string AssetId;
        public float AssetMult = 1f;

        // ---- v3.0 ---------------------------------------------------------------------------------
        /// <summary>Trend-Phase für ein Produkt setzen.</summary>
        public HypeEffect Hype;
        /// <summary>So viele Retouren aus zuletzt verkaufter Ware treffen bald ein.</summary>
        public int? ReturnWave;
        /// <summary>"offer" = Großauftrags-Angebot(e), "accept" = sofort annehmen (falls ein Platz frei ist).</summary>
        public string Contract;
        public float ContractBonus = 1f;
        public int ContractCount = 1;
        /// <summary>Eilauftrag: Frist des Kontext-Auftrags 1 Tag kürzer, Vergütung × Wert.</summary>
        public float? ContractRush;
        /// <summary>Zusätzliche Express-Chance für <see cref="ExpressBoostMinutes"/>.</summary>
        public float? ExpressBoost;
        public float ExpressBoostMinutes = 180f;
        /// <summary>Zusätzliche Skillpunkte.</summary>
        public int? SkillPoints;
        /// <summary>Bonus-Wochenziel "Kalles Wette" ({n} Pakete bis Sonntag).</summary>
        public bool BonusChallenge;
    }

    public sealed class Outcome
    {
        /// <summary>Wahrscheinlichkeit (0..1) oder eine der Konstanten unten.</summary>
        public float P;
        public const float Rest = -1f;
        public const float ByReputation = -2f;
        public const float ByLevel = -3f;

        public string Text;
        public Effects Effects = new Effects();
    }

    public sealed class Choice
    {
        public string Label;
        public int Cost;
        /// <summary>Wenn gesetzt, startet die Wahl ein Minispiel (z.B. "pitch") statt direkt zu würfeln.</summary>
        public string Minigame;
        public Outcome[] Outcomes = new Outcome[0];
    }

    public sealed class EventDef
    {
        /// <summary>
        /// Titel, Absender und Text dürfen Platzhalter enthalten, die beim Auslösen ersetzt werden:
        /// {product} (Produkt aus dem Kontext), {tag} (Hashtag), {company} (Firma), {n} (Zahl), {brand}.
        /// </summary>
        public string Id, Title, Sender, Icon, Text;
        public int MinLevel = 1, MinDay = 2, Cooldown = 3, Default = -1;
        public int? MinMoney, MaxMoney, Stage;
        public string Needs = "";
        public float Weight = 1f;
        public Effects Effects;
        public Choice[] Choices;

        public bool HasChoices => Choices != null && Choices.Length > 0;
        public int DefaultChoice => Default >= 0 ? Default : Choices.Length - 1;
    }

    /// <summary>Alle Ereignisse. Humorvoll-satirisch, wie im Game Design Document beschrieben.</summary>
    public static class EventData
    {
        private static Outcome O(float p, string text, Effects e = null) => new Outcome { P = p, Text = text, Effects = e ?? new Effects() };

        public static readonly EventDef[] All =
        {
            new EventDef
            {
                Id = "black_friday", Title = "Black Friday!", Sender = "Shop-System", Icon = "bag",
                Text = "Heute drehen alle durch. Die Leute kaufen alles, was nicht festgeschraubt ist – drei Stunden lang mehr als doppelt so viele Bestellungen!",
                MinLevel = 2, Cooldown = 6, Weight = 1f,
                Effects = new Effects { Boost = new BoostEffect { Mult = 2.4f, Minutes = 180f, Name = "Black Friday" } },
            },
            new EventDef
            {
                Id = "viral", Title = "Du gehst viral!", Sender = "TikTok", Icon = "fire",
                Text = "Ein Video mit {product} hat über Nacht 2 Millionen Aufrufe. Die Nachfrage explodiert – und der Hype fängt gerade erst an!",
                Needs = "listed", Cooldown = 4, Weight = 1f,
                Effects = new Effects
                {
                    Boost = new BoostEffect { Mult = 3f, Minutes = 120f, Name = "Viraler Hit", Product = "random_listed" }, Awareness = 0.1f,
                    Hype = new HypeEffect { Phase = TrendPhase.Rising, Peak = 1.9f },
                },
            },
            new EventDef
            {
                Id = "supplier_down", Title = "Billig-Fabrik geschlossen", Sender = "Billig-Fabrik", Icon = "factory",
                Text = "Sehr geehrter Kunde, wegen 'Inspektion' (die Polizei war da) liefern wir 2 Tage nicht. Danke für Verständnis.",
                Cooldown = 5, Weight = 0.7f, Effects = new Effects { BlockSupplier = 0, BlockDays = 2 },
            },
            new EventDef
            {
                Id = "customs", Title = "Zollkontrolle", Sender = "Zollamt", Icon = "shield",
                Text = "Eine deiner Lieferungen wird vom Zoll geprüft. Sie kommt zwei Stunden später an.",
                Needs = "deliveries", Cooldown = 3, Weight = 1f, Effects = new Effects { DelayDelivery = 120f },
            },
            new EventDef
            {
                Id = "damaged", Title = "Transportschaden", Sender = "Spedition Rumpel & Söhne", Icon = "box",
                Text = "Beim Verladen ist ein Gabelstapler 'leicht' gegen deine nächste Lieferung gefahren. Ein Viertel ist hin.",
                Needs = "deliveries", Cooldown = 4, Weight = 0.8f, Effects = new Effects { DamageNext = 0.25f },
            },
            new EventDef
            {
                Id = "shitstorm", Title = "Shitstorm!", Sender = "Social Media", Icon = "angry",
                Text = "Ein Kunde behauptet auf TikTok, dein Produkt hätte sein Handy 'geschmolzen'. 40.000 Kommentare. Was tust du?",
                MinLevel = 2, Cooldown = 5, Weight = 0.9f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Entschuldigen + Gutscheine verteilen", Cost = 100, Outcomes = new[] { O(1f, "Die Community findet's fair. Der Sturm legt sich.", new Effects { Rep = 0.1f }) } },
                    new Choice { Label = "Ignorieren", Outcomes = new[] { O(1f, "Der Sturm tobt weiter. Deine Bewertung leidet.", new Effects { Rep = -0.35f }) } },
                    new Choice
                    {
                        Label = "Mit einem Meme antworten", Outcomes = new[]
                        {
                            O(0.5f, "Legendär! Dein Meme geht viral – alle lieben dich.", new Effects { Rep = 0.2f, Awareness = 0.12f, Boost = new BoostEffect { Mult = 1.8f, Minutes = 90f, Name = "Meme-Hype" } }),
                            O(0.5f, "Cringe. Das Internet vergisst nicht.", new Effects { Rep = -0.5f }),
                        },
                    },
                },
            },
            new EventDef
            {
                Id = "crypto_bro", Title = "Bro, hör mir kurz zu", Sender = "Maximilian (Krypto-Bro)", Icon = "rocket",
                Text = "Bro. $HUSTLE-Coin. 100x garantiert. Nur heute. Investier 500 € und wir sehen uns auf den Malediven.",
                MinLevel = 2, MinMoney = 600, Cooldown = 7, Weight = 0.7f, Default = 1,
                Choices = new[]
                {
                    new Choice
                    {
                        Label = "All in, Bro", Cost = 500, Outcomes = new[]
                        {
                            O(0.2f, "UNFASSBAR. Der Coin ist wirklich explodiert: +3.000 €!", new Effects { Money = 3000 }),
                            O(0.8f, "Maximilians Profil ist gelöscht. Die 500 € sind weg."),
                        },
                    },
                    new Choice { Label = "Nein danke", Outcomes = new[] { O(1f, "Maximilian: 'Have fun staying poor.' Du schläfst trotzdem gut.") } },
                },
            },
            new EventDef
            {
                Id = "influencer", Title = "Kooperationsanfrage", Sender = "@lara.lifestyle (1,2 Mio.)", Icon = "star",
                Text = "Hey Babe! Ich würde dein Produkt in meiner Story zeigen. Kostet dich nur 300 €. Deal?",
                MinLevel = 3, MinMoney = 350, Cooldown = 5, Weight = 0.9f, Default = 1,
                Choices = new[]
                {
                    new Choice
                    {
                        Label = "Deal!", Cost = 300, Outcomes = new[]
                        {
                            O(0.65f, "Die Story ballert! Die Bestellungen fliegen nur so rein.", new Effects { Boost = new BoostEffect { Mult = 2.3f, Minutes = 240f, Name = "Influencer-Story" }, Awareness = 0.15f }),
                            O(0.35f, "Sie hat aus Versehen den Link der Konkurrenz gepostet. Ups."),
                        },
                    },
                    new Choice { Label = "Ablehnen", Outcomes = new[] { O(1f, "'Okay, dann halt nicht' – Augenrollen inklusive.") } },
                },
            },
            new EventDef
            {
                Id = "pitch_day", Title = "Einladung zum Pitch Day", Sender = "MegaMarkt Einkauf", Icon = "mic",
                Text = "Die Handelskette MegaMarkt sucht neue Marken für ihre Filialen. Drei Juroren, drei Fragen, fünf Minuten. Überzeugst du sie?",
                MinLevel = 3, Cooldown = 6, Weight = 0.8f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Pitch halten (Minispiel)", Minigame = "pitch" },
                    new Choice { Label = "Absagen", Outcomes = new[] { O(1f, "Du konzentrierst dich lieber auf deinen Shop.") } },
                },
            },
            new EventDef
            {
                Id = "fake_reviews", Title = "Unmoralisches Angebot", Sender = "ReviewBoost Agentur", Icon = "star",
                Text = "50 Fünf-Sterne-Bewertungen für nur 200 €. Sehen garantiert echt aus. Niemand merkt was.",
                MinLevel = 2, Cooldown = 8, Weight = 0.6f, Default = 1,
                Choices = new[]
                {
                    new Choice
                    {
                        Label = "Kaufen", Cost = 200, Outcomes = new[]
                        {
                            O(0.5f, "Deine Bewertung schießt nach oben. Niemand merkt was ... noch.", new Effects { Rep = 0.6f }),
                            O(0.5f, "Aufgeflogen! Ein Verbraucherportal berichtet über dich. Katastrophe.", new Effects { Rep = -1f }),
                        },
                    },
                    new Choice { Label = "Ablehnen", Outcomes = new[] { O(1f, "Ehrlich währt am längsten. Deine Kunden merken das.", new Effects { Rep = 0.05f }) } },
                },
            },
            new EventDef
            {
                Id = "copycat", Title = "Die Konkurrenz kopiert dich", Sender = "Marktbeobachtung", Icon = "eye",
                Text = "BilligBoy24 verkauft plötzlich exakt dein Produkt – 25 % billiger. Die nächsten zwei Tage wird's hart.",
                Needs = "listed", MinLevel = 2, Cooldown = 6, Weight = 0.8f, Effects = new Effects { PriceWarMult = 0.75f, PriceWarDays = 2 },
            },
            new EventDef
            {
                Id = "server_down", Title = "Webshop offline!", Sender = "Hosting-Anbieter", Icon = "warning",
                Text = "Unser Server hat eine Kaffeepause gemacht. Dein Shop ist eine Stunde lang offline. Sorry!",
                Cooldown = 5, Weight = 0.6f, Effects = new Effects { Offline = 60f },
            },
            new EventDef
            {
                Id = "tax", Title = "Post vom Finanzamt", Sender = "Finanzamt", Icon = "mail",
                Text = "Wir haben Fragen zu Ihren Einnahmen. Bitte reichen Sie sämtliche Unterlagen ein.",
                MinLevel = 3, MinMoney = 800, Cooldown = 8, Weight = 0.6f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Steuerberater beauftragen", Cost = 150, Outcomes = new[] { O(1f, "Alles sauber. Der Berater hat sogar was zurückgeholt: +80 €.", new Effects { Money = 80 }) } },
                    new Choice
                    {
                        Label = "Selbst machen", Outcomes = new[]
                        {
                            O(0.5f, "Glück gehabt. Alles in Ordnung."),
                            O(0.5f, "Formfehler! Nachzahlung: 10 % deines Kontostands.", new Effects { MoneyPct = -0.1f }),
                        },
                    },
                },
            },
            new EventDef
            {
                Id = "oma", Title = "Ein Brief von Oma", Sender = "Oma Gerda", Icon = "heart",
                Text = "Mein Enkel, der Geschäftsmann! Ich hab dir 100 Euro beigelegt. Kauf dir was Warmes.",
                MaxMoney = 300, Cooldown = 10, Weight = 1.2f, Effects = new Effects { Money = 100 },
            },
            new EventDef
            {
                Id = "kalle_calls", Title = "Kalle ruft an", Sender = "Kalle", Icon = "phone",
                Text = "Ey. Mein Aushilfskellner ist krank. Kannst du heute zwei Stunden aushelfen? 80 Euro. Bar auf die Hand.",
                MinDay = 3, Cooldown = 7, Weight = 0.6f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Aushelfen (2 Stunden weg)", Outcomes = new[] { O(1f, "Alte Zeiten. Du riechst nach Fritteuse, aber 80 € sind 80 €.", new Effects { Money = 80, TimeSkip = 120f }) } },
                    new Choice { Label = "Nie wieder", Outcomes = new[] { O(1f, "Kalle: 'Pff. Undankbar.' *legt auf*") } },
                },
            },
            new EventDef
            {
                Id = "street_fest", Title = "Straßenfest vor der Tür", Sender = "Stadtverwaltung", Icon = "stand",
                Text = "Heute ist Straßenfest! Ein Verkaufsstand kostet 50 € – du verkaufst direkt aus deinem Lager an Passanten.",
                Needs = "stock", MinLevel = 2, Cooldown = 6, Weight = 0.8f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Stand aufbauen", Cost = 50, Outcomes = new[] { O(1f, "Die Leute lieben deinen Stand!", new Effects { SellStockUnits = 18, SellStockPriceMult = 1.15f }) } },
                    new Choice { Label = "Keine Zeit", Outcomes = new[] { O(1f, "Du hörst die Musik nur von drinnen.") } },
                },
            },
            new EventDef
            {
                Id = "water_damage", Title = "Wasserschaden!", Sender = "Vermieter", Icon = "drop",
                Text = "Das Garagendach ist undicht. Ein Teil deiner Ware ist nass geworden.",
                Stage = 0, Needs = "stock", Cooldown = 8, Weight = 0.5f, Effects = new Effects { StockLoss = 0.15f },
            },
            new EventDef
            {
                Id = "tiktok_algo", Title = "Der Algorithmus liebt dich", Sender = "TikTok", Icon = "music",
                Text = "Eines deiner alten Videos wird plötzlich wieder ausgespielt. Gratis-Reichweite!",
                MinLevel = 2, Cooldown = 5, Weight = 0.8f, Effects = new Effects { TikTok = 0.6f },
            },
            new EventDef
            {
                Id = "crypto_crash", Title = "Krypto-Crash!", Sender = "Finanznews", Icon = "trend_down",
                Text = "Ein Tech-Milliardär hat einen kritischen Post abgesetzt. DROPCOIN stürzt ab.",
                MinLevel = 3, Cooldown = 6, Weight = 0.6f, Effects = new Effects { AssetId = "DROP", AssetMult = 0.45f },
            },
            new EventDef
            {
                Id = "to_the_moon", Title = "DROPCOIN to the moon", Sender = "Finanznews", Icon = "trend",
                Text = "Ein Tech-Milliardär hat ein Hunde-Meme mit DROPCOIN gepostet. Der Kurs explodiert.",
                MinLevel = 3, Cooldown = 6, Weight = 0.6f, Effects = new Effects { AssetId = "DROP", AssetMult = 2.2f },
            },

            // ================= v3.0: Ereignisse für Retouren, Trends, Großaufträge, Skills, Wochenziele =================
            new EventDef
            {
                Id = "retourenwelle", Title = "Retourenwelle!", Sender = "Kundenservice-Postfach", Icon = "box",
                Text = "Ein Influencer erklärt in einem viralen Video, wie man „einfach alles zurückschickt, was nicht perfekt ist“. Ein paar deiner Kundinnen und Kunden haben zugeschaut.",
                Needs = "shipped15", MinLevel = 2, Cooldown = 6, Weight = 0.7f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Kulanz-Gutscheine verschicken", Cost = 60, Outcomes = new[] { O(1f, "Die meisten behalten ihre Ware doch – und freuen sich über den Gutschein.", new Effects { ReturnWave = 1, Rep = 0.05f }) } },
                    new Choice { Label = "Hinnehmen", Outcomes = new[] { O(1f, "Die Pakete sind schon auf dem Rückweg. Tja.", new Effects { ReturnWave = 3 }) } },
                },
            },
            new EventDef
            {
                Id = "grosskunde", Title = "Großkunde fragt an", Sender = "{company}", Icon = "factory",
                Text = "Wir haben von Ihrem Shop gehört! Wir brauchen dringend {product} in größerer Menge – und zahlen 30 % über dem üblichen Preis. Interesse?",
                MinLevel = 3, Cooldown = 5, Weight = 0.9f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Sofort zusagen", Outcomes = new[] { O(1f, "Handschlag per Mail. Ab zum Palettenplatz!", new Effects { Contract = "accept", ContractBonus = 1.3f }) } },
                    new Choice { Label = "Erst mal ansehen", Outcomes = new[] { O(1f, "Das Angebot liegt in der App 'Aufträge' – gültig bis morgen Abend.", new Effects { Contract = "offer", ContractBonus = 1.3f }) } },
                    new Choice { Label = "Absagen", Outcomes = new[] { O(1f, "„Schade. Dann fragen wir halt bei BilligBoy24.“") } },
                },
            },
            new EventDef
            {
                Id = "trend_alarm", Title = "Trend-Alarm!", Sender = "Trendradar", Icon = "fire",
                Text = "#{tag} trendet! Auf TikTok filmen sich gerade alle mit {product}. Das könnte richtig groß werden.",
                Needs = "listed", MinLevel = 2, Cooldown = 4, Weight = 0.9f,
                Effects = new Effects { Hype = new HypeEffect { Phase = TrendPhase.Rising, Peak = 1.9f } },
            },
            new EventDef
            {
                Id = "trend_crash", Title = "Verriss im Netz", Sender = "Tech-Blog „Gadget-Gurus“", Icon = "trend_down",
                Text = "Ein bekannter Tech-YouTuber nennt {product} „Elektroschrott mit Ringlicht“. Die Kommentare eskalieren. Reagierst du?",
                Needs = "listed", MinLevel = 3, Cooldown = 6, Weight = 0.6f, Default = 1,
                Choices = new[]
                {
                    new Choice
                    {
                        Label = "Reaktionsvideo drehen", Outcomes = new[]
                        {
                            O(0.5f, "Dein Video ist lustiger als seins. Plötzlich ist {product} wieder cool!", new Effects { Awareness = 0.05f, Hype = new HypeEffect { Phase = TrendPhase.Rising, Peak = 1.7f } }),
                            O(0.5f, "Niemand schaut es. Der Hype ist tot.", new Effects { Hype = new HypeEffect { Phase = TrendPhase.Dead } }),
                        },
                    },
                    new Choice { Label = "Aussitzen", Outcomes = new[] { O(1f, "Du wartest ab. Die Nachfrage sackt erst mal ab.", new Effects { Hype = new HypeEffect { Phase = TrendPhase.Falling } }) } },
                },
            },
            new EventDef
            {
                Id = "express_rush", Title = "Express-Fieber", Sender = "PaketBlitz", Icon = "bolt",
                Text = "Bei BilligBoy24 streiken die Paketboten. Die Leute wollen es heute schnell – und zahlen gern dafür. Drei Stunden lang kommen deutlich mehr Express-Bestellungen!",
                MinLevel = 2, Cooldown = 5, Weight = 0.7f,
                Effects = new Effects { ExpressBoost = 0.3f, ExpressBoostMinutes = 180f, Boost = new BoostEffect { Mult = 1.2f, Minutes = 180f, Name = "Express-Fieber" } },
            },
            new EventDef
            {
                Id = "eilauftrag", Title = "Eilauftrag!", Sender = "{company}", Icon = "clock",
                Text = "Planänderung: Wir brauchen die Lieferung ({product}) einen Tag früher. Dafür legen wir 30 % drauf. Schaffen Sie das?",
                Needs = "contract_rushable", MinLevel = 3, Cooldown = 5, Weight = 0.8f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Klar, machen wir!", Outcomes = new[] { O(1f, "Der Auftrag ist jetzt 30 % mehr wert – aber die Frist ist einen Tag kürzer.", new Effects { ContractRush = 1.3f }) } },
                    new Choice { Label = "Nein, es bleibt beim Termin", Outcomes = new[] { O(1f, "„Verstehe. Dann wie besprochen.“ Ein bisschen enttäuscht klingt es schon.") } },
                },
            },
            new EventDef
            {
                Id = "kuriose_retoure", Title = "Kuriose Retoure", Sender = "Retourenabteilung", Icon = "box",
                Text = "In einem Rücksendepaket lag statt {product} ein benutzter Toaster. Dazu ein Zettel: „Passt schon.“ Was machst du damit?",
                Needs = "returns", Cooldown = 10, Weight = 0.6f, Default = 0,
                Choices = new[]
                {
                    new Choice { Label = "Bei eBay verkaufen", Outcomes = new[] { O(1f, "Ein Sammler zahlt 25 € für den „Vintage-Toaster“. Business ist Business.", new Effects { Money = 25 }) } },
                    new Choice
                    {
                        Label = "Foto posten", Outcomes = new[]
                        {
                            O(0.6f, "„Kunde schickt Toaster zurück“ geht durch die Decke. Gratis-Werbung!", new Effects { Awareness = 0.06f, Boost = new BoostEffect { Mult = 1.3f, Minutes = 60f, Name = "Toaster-Meme" } }),
                            O(0.4f, "Drei Likes. Einer davon von deiner Mama."),
                        },
                    },
                    new Choice { Label = "Dem Kunden zurückschicken", Outcomes = new[] { O(1f, "Der Kunde ist so gerührt, dass er dir fünf Sterne gibt.", new Effects { Rep = 0.05f }) } },
                },
            },
            new EventDef
            {
                Id = "seminar", Title = "Hustle-Seminar", Sender = GameData.CoachName, Icon = "mic",
                Text = "Exklusives Wochenend-Seminar „Vom Pleitier zum Privatjet“ – nur 400 €, mit Zertifikat (selbst ausgedruckt). Bist du dabei?",
                MinLevel = 3, MinMoney = 700, Cooldown = 12, Weight = 0.4f, Default = 1,
                Choices = new[]
                {
                    new Choice
                    {
                        Label = "Anmelden", Cost = 400, Outcomes = new[]
                        {
                            O(0.75f, "Überraschend gut! Du lernst tatsächlich was: +1 Skillpunkt.", new Effects { SkillPoints = 1 }),
                            O(0.25f, "Das Seminar war eine Werbeveranstaltung für ein weiteres Seminar. Immerhin gab es Kekse."),
                        },
                    },
                    new Choice { Label = "Nein danke", Outcomes = new[] { O(1f, "Marvin: „Deine Konkurrenz sitzt jetzt im Seminar!“ Du sitzt lieber im Lager.") } },
                },
            },
            new EventDef
            {
                Id = "kalles_wette", Title = "Kalles Wette", Sender = "Kalle", Icon = "phone",
                Text = "Ey, Unternehmer. Ich wette, du schaffst diese Woche keine {n} Pakete mehr. Wenn doch: Döner geht auf mich. Und ich leg noch was drauf.",
                Needs = "early_week", MinDay = 3, Cooldown = 7, Weight = 0.7f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Die Wette gilt!", Outcomes = new[] { O(1f, "Kalle: „Abgemacht. Und wehe, du schummelst.“ (Neues Wochenziel!)", new Effects { BonusChallenge = true }) } },
                    new Choice { Label = "Keine Zeit für Wetten", Outcomes = new[] { O(1f, "Kalle: „Pff. Feigling.“ *legt auf*") } },
                },
            },
            new EventDef
            {
                Id = "messe", Title = "Einladung zur Messe", Sender = "Dropshipping-Expo Kleinkleckersdorf", Icon = "calendar",
                Text = "Ein Stand auf der Dropshipping-Expo kostet 250 €. Dafür gibt es Kontakte zu Firmen, Kaffee aus Pappbechern und garantiert zwei Großanfragen.",
                MinLevel = 4, MinMoney = 400, Cooldown = 8, Weight = 0.5f, Default = 1,
                Choices = new[]
                {
                    new Choice { Label = "Hin da!", Cost = 250, Outcomes = new[] { O(1f, "Du verteilst 300 Visitenkarten und isst 14 Gratis-Kekse. Zwei Firmen melden sich!", new Effects { Contract = "offer", ContractCount = 2, ContractBonus = 1.1f }) } },
                    new Choice { Label = "Keine Zeit", Outcomes = new[] { O(1f, "Du hörst später, dass es dort Gratis-Kugelschreiber gab. Schade.") } },
                },
            },
        };

        public static EventDef Find(string id)
        {
            foreach (var e in All)
                if (e.Id == id) return e;
            return null;
        }
    }
}
