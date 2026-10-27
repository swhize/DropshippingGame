namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Allgemeine Kernbegriffe, Einheiten, Wochentage, Formate, Sim-Meldungen (Notify) aus Sim*.cs.
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnCore
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
            Basics(A);
            SimMain(A);
            SimOrders(A);
            SimAds(A);
            SimChallenges(A);
            SimContracts(A);
            SimEndgame(A);
            SimFestival(A);
            SimProgress(A);
            SimShopsSkills(A);
            SimSaveModel(A);
            MarketTexts(A);
            TrendTexts(A);
            PitchTikTokFurniture(A);
        }

        // =====================================================================================
        // Grundbegriffe: Wochentage, Stufen, Qualität, Einheiten
        // =====================================================================================
        private static void Basics(System.Action<string, string> A)
        {
            A("Montag", "Monday");
            A("Dienstag", "Tuesday");
            A("Mittwoch", "Wednesday");
            A("Donnerstag", "Thursday");
            A("Freitag", "Friday");
            A("Samstag", "Saturday");
            A("Sonntag", "Sunday");
            A("Mo", "Mon");
            A("Di", "Tue");
            A("Mi", "Wed");
            A("Do", "Thu");
            A("Fr", "Fri");
            A("Sa", "Sat");
            A("So", "Sun");
            A("Premium", "Premium");
            A("Billig", "Cheap");
            A("Standard", "Standard");
            A("Tag {0}", "Day {0}");
        }

        // =====================================================================================
        // Sim.cs
        // =====================================================================================
        private static void SimMain(System.Action<string, string> A)
        {
            A("Trend: {0} ist im Aufwind!", "Trend: {0} is on the rise!");
            A("Hype! Alle wollen {0}.", "Hype! Everybody wants {0}.");
            A("Der Hype um {0} ist vorbei – kaum noch Nachfrage.", "The {0} hype is over – barely any demand left.");
            A("Neue Nachricht: „{0}“ · Handy ({1})", "New message: “{0}” · Phone ({1})");
            A("Viel Erfolg, Schatz!", "Good luck, sweetie!");
            A("Ich hab gehört, du machst jetzt was mit Internet. Iss was Ordentliches und vergiss nicht zu schlafen. Hab dich lieb!", "I heard you're doing something with the internet now. Eat something proper and don't forget to sleep. Love you!");
            A("Kalle hat angerufen. Du hast gekündigt?! Naja... ich glaub an dich. Iss was Ordentliches!", "Kalle called. You quit?! Well... I believe in you. Eat something proper!");
            A("Wochenbilanz: {0} von {1} Wochenzielen geschafft.", "Weekly review: {0} of {1} weekly goals done.");
            A("offline", "offline");
            A("sehr niedrig", "very low");
            A("niedrig", "low");
            A("mittel", "medium");
            A("hoch", "high");
            A("sehr hoch", "very high");
            A("Express bestellt, NICHTS bekommen. Storniert!", "Ordered EXPRESS, got NOTHING. Cancelled!");
            A("Express-Bestellung {0} ({1}, {2}) wurde storniert – zu lange gewartet.", "Express order {0} ({1}, {2}) was cancelled – waited too long.");
            A("Bestellung {0} ({1}, {2}) wurde storniert – zu lange gewartet.", "Order {0} ({1}, {2}) was cancelled – waited too long.");
            A("Dieses Produkt ist noch gesperrt.", "This product is still locked.");
            A("Dafür brauchst du erst die Lagerhalle (kein Regal in der Garage).", "You need the warehouse for that first (no shelf in the garage).");
            A("Die Premium-Manufaktur liefert nur eine Bestellung pro Produkt und Tag.", "The premium manufacturer only ships one order per product per day.");
            A("Dieser Anbieter ist gerade nicht verfügbar.", "This supplier is currently unavailable.");
            A("Diese Bestellmenge ist noch nicht freigeschaltet.", "This order quantity isn't unlocked yet.");
            A("Nicht genug Geld für diese Bestellung!", "Not enough money for this order!");
            A("{0}× {1} bestellt bei {2}.", "Ordered {0}× {1} from {2}.");
            A("Beschädigte Lieferung: {0} Stück unbrauchbar.", "Damaged delivery: {0} units unusable.");
            A("Lieferung angekommen: {0}× {1} am Wareneingang.", "Delivery arrived: {0}× {1} at goods-in.");
            A("{0} min", "{0} min");
            A("Nichts am Wareneingang abzuholen.", "Nothing to pick up at goods-in.");
            A("Lager voll! ({0}/{1}) – verkaufe erst etwas oder baue aus.", "Storage full! ({0}/{1}) – sell something first or expand.");
            A("Keine offene Bestellung für {0}.", "No open order for {0}.");
            A("Kein {0} mehr auf Lager! Im Laptop nachbestellen.", "{0} is out of stock! Reorder on the laptop.");
            A("Der Artikel ist für einen Großauftrag – bring ihn zum Palettenplatz. Verpacken musst du dafür nichts.", "This item is for a bulk order – take it to the pallet spot. No packing needed.");
            A("Kein gefalteter Karton {0} – erst am Falttisch falten!", "No folded {0} box – fold one at the folding table first!");
            A("Keine Kartons in Größe {0}! Im Laptop unter 'Verpackung' kaufen.", "No {0} boxes! Buy some on the laptop under 'Packaging'.");
            A("Kein Karton {0} mehr – in {1} verpackt. Zu groß: höheres Retourenrisiko!", "Out of {0} boxes – packed in {1}. Too big: higher return risk!");
            A("Kein Karton {0} mehr – in {1} verpackt. Zu groß: höheres Retourenrisiko! (Tipp: {2}-Kartons am Falttisch falten.)", "Out of {0} boxes – packed in {1}. Too big: higher return risk! (Tip: fold {2} boxes at the folding table.)");
            A("Hier liegt kein fertiges Paket.", "There's no finished parcel here.");
            A("Keine ungefalteten Kartons da.", "No unfolded boxes left.");
            A("Firmenlevel {0}! Neu: {1} · +1 Skillpunkt", "Company level {0}! New: {1} · +1 skill point");
            A("Firmenlevel {0}! Neu:  · +1 Skillpunkt", "Company level {0}! · +1 skill point");
            A("Ziel erreicht: {0} (+{1})", "Goal reached: {0} (+{1})");
            A("Ziel erreicht: {0} (+{1} · +{2} XP)", "Goal reached: {0} (+{1} · +{2} XP)");
            A("Tutorial geschafft! Ab jetzt läuft dein Shop. Neue Funktionen erklärt dir Marvin per Nachricht (Handy: {0}).", "Tutorial done! Your shop is live now. Marvin will explain new features by message (phone: {0}).");
            A("Schicht im Imbiss", "Shift at the diner");
            A("Sprich mit Kalle an der Theke.", "Talk to Kalle at the counter.");
            A("Teller austragen ({0}/3)", "Serve plates ({0}/3)");
            A("Hol an der Durchreiche einen Teller und bring ihn an den richtigen Tisch.", "Grab a plate at the pass and bring it to the right table.");
            A("Feierabend?", "Clocking out?");
            A("Sprich mit Kalle. Es ist Zeit für eine Entscheidung.", "Talk to Kalle. It's time for a decision.");
            A("Tutorial {0}/{1}: {2}", "Tutorial {0}/{1}: {2}");
            A("Ziel: {0}", "Goal: {0}");
            A("Neues Video: 'Mit 19 Millionär – dank Dropshipping!' ... interessant.", "New video: 'Millionaire at 19 – thanks to dropshipping!' ... interesting.");
            A("Dieses Produkt ist noch nicht verfügbar.", "This product isn't available yet.");
            A("Nicht genug Geld für Verpackungsmaterial!", "Not enough money for packaging!");
            A("{0} Kartons {1} gekauft – mit deinem Logo bedruckt.", "Bought {0} {1} boxes – printed with your logo.");
            A("{0} Kartons {1} (ungefaltet) gekauft – mit deinem Logo bedruckt.", "Bought {0} {1} boxes (flat-packed) – printed with your logo.");
            A("Diese Werbeform ist erst ab Level {0} verfügbar.", "This ad format unlocks at level {0}.");
            A("Es läuft schon eine Werbekampagne.", "An ad campaign is already running.");
            A("Nicht genug Geld für diese Kampagne!", "Not enough money for this campaign!");
            A("{0} gestartet! Mehr Bestellungen heute und morgen.", "{0} launched! More orders today and tomorrow.");
            A("{0} gestartet! Mehr Bestellungen bis Feierabend.", "{0} launched! More orders until closing time.");
            A("TikTok-Trend", "TikTok trend");
            A("TikTok gepostet! {0} % Treffer → ×{1} Nachfrage für {2} min.", "TikTok posted! {0} % hit → ×{1} demand for {2} min.");
            A("Trendsetter! Dein Video startet einen Hype um {0}.", "Trendsetter! Your video kicks off a {0} hype.");
            A("{0} ist ausgelaufen.", "{0} has ended.");
            A("Personal gibt es erst mit Lagerhalle und Level {0}.", "Staff only becomes available with the warehouse and level {0}.");
            A("Eine zweite Kraft als {0} gibt es ab Firmenlevel {1}.", "A second {0} unlocks at company level {1}.");
            A("Mehr {0} brauchst du nicht.", "You don't need more {0}.");
            A("Aushilfe", "Temp");
            A("{0} fängt als {1} an ({2}/Tag).", "{0} starts as {1} ({2}/day).");
            A("{0} wurde entlassen.", "{0} was let go.");
            A("{0} hat ein TikTok gepostet ({1} % Treffer).", "{0} posted a TikTok ({1} % hit).");
            A("Nicht genug Geld!", "Not enough money!");
            A("{0} gekauft!", "{0} bought!");
            A("{0} aufgestellt.", "{0} set up.");
            A("Gönn dir: {0}!", "Treat yourself: {0}!");
            A("So viel Kredit gibt dir die Bank nicht. Dein Rahmen: {0}.", "The bank won't lend you that much. Your limit: {0}.");
            A("Kredit über {0} ausgezahlt. Zinsen: {1} % pro Tag.", "Loan of {0} paid out. Interest: {1} % per day.");
            A("Du hast keine Schulden.", "You have no debt.");
            A("Nicht genug Geld zum Tilgen.", "Not enough money to repay.");
            A("Kredit komplett getilgt. Schuldenfrei!", "Loan fully repaid. Debt-free!");
            A("{0} getilgt. Restschuld: {1}.", "{0} repaid. Remaining debt: {1}.");
            A("Den Verkaufsstand musst du erst in der Ausbau-App kaufen.", "Buy the market stall in the upgrades app first.");
            A("Der Stand ist voll.", "The stall is full.");
        }

        // =====================================================================================
        // Sim.Admin / Sim.Orders / Sim.Returns
        // =====================================================================================
        private static void SimOrders(System.Action<string, string> A)
        {
            A("ADMIN: Alles freigeschaltet.", "ADMIN: Everything unlocked.");
            A("Admin", "Admin");
            A("Shop überlastet – Bestellung ging nicht durch", "Shop overloaded – order didn't go through");
            A("Bestellung verloren – deine Warteschlange ist voll! (Preis erhöhen oder Personal holen)", "Order lost – your queue is full! (Raise prices or hire staff)");
            A("Express-Bestellung {0}: {1} – fällig in {2}!", "Express order {0}: {1} – due in {2}!");
            A("Neu im Shop: die erste Bestellung für {0} ist da!", "New in the shop: the first order for {0} is in!");
            A("überfällig ({0})", "overdue ({0})");
            A("Bestellung {0} ({1}) ist verloren gegangen.", "Order {0} ({1}) got lost.");
            A("Packen", "Pack");
            A("Label drucken", "Print label");
            A("Versenden", "Ship");
            A("Auf dem Band", "On the belt");
            A("Holen", "Pick");
            A("Packtisch", "Packing table");
            A("Labeldrucker", "Label printer");
            A("Versand", "Shipping");
            A("läuft automatisch", "runs automatically");
            A("Regal {0}", "Shelf {0}");
            A("Regal", "Shelf");
            A("Mo {0}:{1}", "Mon {0}:{1}");
            A("Di {0}:{1}", "Tue {0}:{1}");
            A("Mi {0}:{1}", "Wed {0}:{1}");
            A("Do {0}:{1}", "Thu {0}:{1}");
            A("Fr {0}:{1}", "Fri {0}:{1}");
            A("Sa {0}:{1}", "Sat {0}:{1}");
            A("So {0}:{1}", "Sun {0}:{1}");
            A("Hab ein Video gesehen: Man kann einfach alles zurückschicken!", "Saw a video: you can just send everything back!");
            A("Kundschaft", "Customer");
            A("Retoure von {0}: {1} – {2} erstattet. „{3}“", "Return from {0}: {1} – {2} refunded. “{3}”");
            A("Keine Retouren am Wareneingang.", "No returns at goods-in.");
            A("Lager voll – die B-Ware passt nicht mehr rein. Entsorgen oder Platz schaffen.", "Storage full – the B-stock won't fit. Dispose of it or make room.");
            A("B-Ware eingelagert: 1× {0} ({1}).", "B-stock shelved: 1× {0} ({1}).");
            A("Retoure entsorgt: {0}.", "Return disposed of: {0}.");
        }

        // =====================================================================================
        // Sim.Ads (Fakebook, Gugel, Fake-Profile, Fake-Seiten)
        // =====================================================================================
        private static void SimAds(System.Action<string, string> A)
        {
            A("Ab Level {0}", "From level {0}");
            A("Produkt gesperrt", "Product locked");
            A("Produkt ist offline", "Product is offline");
            A("Max. {0} Kampagnen", "Max. {0} campaigns");
            A("Läuft schon", "Already running");
            A("Kein Budget", "No budget");
            A("Zu wenig Geld", "Not enough money");
            A("Fakebook: {0}.", "Fakebook: {0}.");
            A("Fakebook-Anzeige läuft: {0} ×{1}.", "Fakebook ad running: {0} ×{1}.");
            A("Gugel Ads: {0}.", "Gugel Ads: {0}.");
            A("Gugel-Anzeige auf Platz {0}: „{1}“.", "Gugel ad in position {0}: “{1}”.");
            A("{0} Views · {1} Klicks · {2} Verkäufe", "{0} views · {1} clicks · {2} sales");
            A("{0}-Anzeige {1} beendet: {2}", "{0} ad {1} ended: {2}");
            A("{0}-Anzeige {1} beendet (Budget weg): {2}", "{0} ad {1} ended (budget spent): {2}");
            A("{0}-Anzeige {1} beendet (Konto leer): {2}", "{0} ad {1} ended (account empty): {2}");
            A("{0}-Anzeige {1} beendet (gestoppt): {2}", "{0} ad {1} ended (stopped): {2}");
            A("{0}-Anzeige  beendet: {1}", "{0} ad ended: {1}");
            A("{0}-Anzeige  beendet (Budget weg): {1}", "{0} ad ended (budget spent): {1}");
            A("{0}-Anzeige  beendet (Konto leer): {1}", "{0} ad ended (account empty): {1}");
            A("{0}-Anzeige  beendet (gestoppt): {1}", "{0} ad ended (stopped): {1}");
            A("Mehr Fake-Profile fallen auf. Sogar Fakebook merkt das.", "More fake profiles get noticed. Even Fakebook catches on.");
            A("Nicht genug Geld für eine Prepaid-SIM.", "Not enough money for a prepaid SIM.");
            A("Fake-Profile gelöscht. Spuren verwischt.", "Fake profiles deleted. Tracks covered.");
            A("Max. {0} Seiten", "Max. {0} sites");
            A("Max. {0} pro Produkt", "Max. {0} per product");
            A("Fake-Seite: {0}.", "Fake site: {0}.");
            A("Neue KI-Seite online: {0}", "New AI site online: {0}");
            A("Aufgeflogen! Fakebook löscht deine {0} Fake-Profile. Bewertung −{1}.", "Busted! Fakebook deletes your {0} fake profiles. Rating −{1}.");
            A("Verbraucherschutz entlarvt {0}. Bewertung −{1}.", "Consumer protection exposes {0}. Rating −{1}.");
            A("{0}-Ad", "{0} ad");
            A("Social Proof", "Social proof");
            A("SEO", "SEO");
            A("ADMIN: Fake-Profile aufgeflogen (−{0}).", "ADMIN: Fake profiles busted (−{0}).");
            A("ADMIN: {0} entlarvt.", "ADMIN: {0} exposed.");
        }

        // =====================================================================================
        // Sim.Challenges (Wochenziele)
        // =====================================================================================
        private static void SimChallenges(System.Action<string, string> A)
        {
            A("Neue Woche, neue Ziele! {0} Wochenziele warten auf dich.", "New week, new goals! {0} weekly goals are waiting for you.");
            A("Paketlawine", "Parcel Avalanche");
            A("Verschicke {0} Pakete.", "Ship {0} parcels.");
            A("Umsatzmaschine", "Revenue Machine");
            A("Mach {0} Umsatz (Shop, Stand und Großaufträge).", "Make {0} in revenue (shop, stall and bulk orders).");
            A("Sternenhimmel", "Starry Sky");
            A("Bring deine Bewertung auf mindestens {0} Sterne.", "Get your rating to at least {0} stars.");
            A("Fünf-Sterne-Regen", "Five-Star Shower");
            A("Sammle {0} Fünf-Sterne-Bewertungen.", "Collect {0} five-star reviews.");
            A("Blitzversand", "Lightning Shipping");
            A("Verschicke {0} Express-Bestellungen pünktlich.", "Ship {0} express orders on time.");
            A("Großkunden-Liebling", "Big-Client Darling");
            A("Erfülle {0} Großaufträge.", "Complete {0} bulk orders.");
            A("Erfülle einen Großauftrag.", "Complete a bulk order.");
            A("Marktschreier", "Market Crier");
            A("Verkaufe {0} Artikel am Verkaufsstand.", "Sell {0} items at the market stall.");
            A("Content-Maschine", "Content Machine");
            A("Poste {0} TikToks.", "Post {0} TikToks.");
            A("{0}-Spezialist", "{0} Specialist");
            A("Verkaufe {0}× {1}.", "Sell {0}× {1}.");
            A("Zweite Chance", "Second Chance");
            A("Bereite {0} Retouren als B-Ware auf.", "Refurbish {0} returns as B-stock.");
            A("Fehlerfreier Tag", "Flawless Day");
            A("Verschicke an einem Tag {0} Pakete, ohne eine Bestellung zu verlieren.", "Ship {0} parcels in one day without losing a single order.");
            A("Diese Woche hast du schon ein Ziel getauscht.", "You've already swapped a goal this week.");
            A("Wochenziel geschafft: {0} (+{1})", "Weekly goal done: {0} (+{1})");
            A("Wochenziel geschafft: {0}! +{1} · +{2} XP", "Weekly goal done: {0}! +{1} · +{2} XP");
        }

        // =====================================================================================
        // Sim.Contracts (Großaufträge)
        // =====================================================================================
        private static void SimContracts(System.Action<string, string> A)
        {
            A("{0}× {1}", "{0}× {1}");
            A("nur Premium-Qualität", "premium quality only");
            A("mind. Standard-Qualität", "at least standard quality");
            A("Qualität egal", "any quality");
            // Description = Firma + " braucht " + Titel + " " + Grund + "." (Gründe aus GameData.ContractReasons)
            string[,] reasons =
            {
                { "für die Weihnachtsfeier", "for the Christmas party" },
                { "als Kundengeschenke", "as client gifts" },
                { "für die Tombola", "for the raffle" },
                { "für das Sommerfest", "for the summer party" },
                { "für neue Mitarbeitende", "for new hires" },
                { "zum Firmenjubiläum", "for the company anniversary" },
                { "für eine Marketing-Aktion", "for a marketing stunt" },
                { "für den Tag der offenen Tür", "for the open day" },
                { "als Preise für ein Gewinnspiel", "as prizes for a giveaway" },
                { "zur Motivation der Belegschaft (statt Gehaltserhöhung)", "to motivate the staff (instead of a raise)" },
                { "für die Goodie-Bags beim Firmenlauf", "for the goodie bags at the company fun run" },
                { "für 14 Filialen (Markenware)", "for 14 branches (branded goods)" },
                { "fürs Weihnachtsregal (Markenware)", "for the Christmas shelf (branded goods)" },
                { "für die Kassenzone (Markenware)", "for the checkout area (branded goods)" },
                { "als Aktionsware im Prospekt", "as a promo item in the flyer" },
                { "für den Online-Shop des Händlers (Markenware)", "for the retailer's online shop (branded goods)" },
            };
            for (int i = 0; i < reasons.GetLength(0); i++)
            {
                A(reasons[i, 0], reasons[i, 1]);
                A("{0} braucht {1}× {2} " + reasons[i, 0] + ".", "{0} needs {1}× {2} " + reasons[i, 1] + ".");
            }
            A("{0} braucht {1}.", "{0} needs {1}.");
            A("heute bis 20:00", "today by 20:00");
            A("morgen bis 20:00", "tomorrow by 20:00");
            A("bis {0}, 20:00 ({1} Tage)", "by {0}, 20:00 ({1} days)");
            A("Dieses Angebot gibt es nicht mehr.", "This offer is no longer available.");
            A("Großaufträge gibt es ab Level {0}.", "Bulk orders unlock at level {0}.");
            A("Du hast schon {0} laufende(n) Großauftrag/-aufträge. Erst liefern!", "You already have {0} active bulk order(s). Deliver first!");
            A("Neue Großanfrage: {0} will {1} ({2}).", "New bulk request: {0} wants {1} ({2}).");
            A("Großauftrag angenommen: {0} für {1} – {2}.", "Bulk order accepted: {0} for {1} – {2}.");
            A("Der Artikel gehört zur Bestellung #{0} – ab zum Packtisch damit.", "This item belongs to order #{0} – take it to the packing table.");
            A("Die Spedition nimmt hier nur Kisten und einzelne Artikel für Großaufträge an.", "The freight company only takes crates and single items for bulk orders here.");
            A("Kein laufender Großauftrag. Angebote findest du in der App 'Aufträge'.", "No active bulk order. You'll find offers in the 'Contracts' app.");
            A("Kein laufender Großauftrag braucht {0}.", "No active bulk order needs {0}.");
            A("Qualität reicht nicht: Die Firma will {0}.", "Quality isn't good enough: the company wants {0}.");
            A("{0}× {1} für {2} auf die Palette ({3}/{4}).", "{0}× {1} for {2} onto the pallet ({3}/{4}).");
            A("bessere Qualität", "better quality");
            A("Die Ware im Regal ist zu billig für {0} ({1}).", "The stock on the shelf is too cheap for {0} ({1}).");
            A("Großauftrag erfüllt: {0} (+{1})", "Bulk order completed: {0} (+{1})");
            A("Großauftrag erfüllt! {0} zahlt {1}. Die Spedition holt die Palette ab.", "Bulk order completed! {0} pays {1}. The freight company picks up the pallet.");
            A("Großauftrag geplatzt: {0} (Strafe {1})", "Bulk order failed: {0} (penalty {1})");
            A("Frist verpasst! {0} verlangt {1} Vertragsstrafe.", "Deadline missed! {0} demands a {1} contract penalty.");
            A("Frist verpasst! {0} verlangt {1} Vertragsstrafe (Teillieferung: +{2}).", "Deadline missed! {0} demands a {1} contract penalty (partial delivery: +{2}).");
        }

        // =====================================================================================
        // Sim.Endgame (Erfolge, Mama, Börsengang, Endlos-Ziele, Statistik)
        // =====================================================================================
        private static void SimEndgame(System.Action<string, string> A)
        {
            A("MeinShop", "MyShop");
            A("Mama", "Mom");
            A("Kiez-Börse", "Block Exchange");
            A("Erfolg freigeschaltet: {0} – {1}", "Achievement unlocked: {0} – {1}");
            A("An Mama zurückgezahlt: {0} (noch {1})", "Paid Mom back: {0} ({1} to go)");
            A("An Mama zurückgezahlt: {0} – alles beglichen!", "Paid Mom back: {0} – all settled!");
            A("Alles zurück!", "All paid back!");
            A("Schatz, du hast mir alles zurückgezahlt. Ich hab doch gesagt, du schaffst das. Ich bin so stolz auf dich!", "Sweetie, you paid me back every cent. I told you you'd make it. I'm so proud of you!");
            A("Mama hat dich vor der Pleite gerettet: {0} zinslos – du zahlst es zurück, sobald es wieder läuft.", "Mom saved you from bankruptcy: {0} interest-free – you'll pay it back once business picks up.");
            A("Mama hat dich vor der Pleite gerettet: {0} zinslos – du zahlst es zurück, sobald es wieder läuft. Ein zweites Mal kann sie nicht.", "Mom saved you from bankruptcy: {0} interest-free – you'll pay it back once business picks up. She can't do it a second time.");
            A("Ich hab dir was überwiesen", "I sent you some money");
            A("Schatz, die Bank hat bei mir angerufen. Ich hab dir {0} überwiesen – zahl's zurück, wenn's wieder läuft. Aber bitte: weniger Ausgaben, mehr verkaufen, und lass die Finger von diesem Krypto-Zeug!",
                "Sweetie, the bank called me. I sent you {0} – pay it back when things are going again. But please: spend less, sell more, and keep your hands off that crypto stuff!");
            A("Schatz, die Bank hat bei mir angerufen. Ich hab dir {0} überwiesen – zahl's zurück, wenn's wieder läuft. Aber bitte: weniger Ausgaben, mehr verkaufen, und lass die Finger von diesem Krypto-Zeug!\n\nPS: Mehr hab ich wirklich nicht. Das nächste Mal musst du allein klarkommen.",
                "Sweetie, the bank called me. I sent you {0} – pay it back when things are going again. But please: spend less, sell more, and keep your hands off that crypto stuff!\n\nPS: That's really all I have. Next time you're on your own.");
            A("Mama hat dich gerettet (+{0})! Noch mal klappt das {1}×.", "Mom bailed you out (+{0})! She can do it {1}× more.");
            A("Mama hat dich gerettet (+{0})! Noch mal klappt das nicht.", "Mom bailed you out (+{0})! That won't work again.");
            A("Börsengang: {0} AG", "IPO: {0} Inc.");
            A("Die Glocke läutet! {0} ist ab heute an der Kiez-Börse notiert. Erster Kurs: 12,50 €. Kalle hat drei Aktien gekauft „für die Rente“, Mama hat den Zeitungsartikel eingerahmt. Und du? Packst weiter Pakete. Natürlich.\n\nAb jetzt zählt nur noch eins: mehr. Neue Ziele: 2, 4, 8 Millionen …",
                "The bell rings! As of today, {0} is listed on the Block Exchange. Opening price: 12.50 €. Kalle bought three shares “for his pension”, Mom framed the newspaper article. And you? Keep packing parcels. Obviously.\n\nFrom now on only one thing counts: more. New goals: 2, 4, 8 million …");
            A("Die Glocke läutet! {0} ist ab heute an der Kiez-Börse notiert. Erster Kurs: 12,50 €. Kalle hat drei Aktien gekauft „für die Rente“, Mama hat den Zeitungsartikel eingerahmt. Und du? Packst weiter Pakete. Natürlich.",
                "The bell rings! As of today, {0} is listed on the Block Exchange. Opening price: 12.50 €. Kalle bought three shares “for his pension”, Mom framed the newspaper article. And you? Keep packing parcels. Obviously.");
            A("Endlos {0}: {1} € Umsatz", "Endless {0}: {1} € revenue");
            A("Immer weiter: erreiche {0} € Umsatz.", "Keep going: reach {0} € in revenue.");
            A("Endlos-Ziel geschafft: {0} € Umsatz (+{1})! Nächstes: {2} €.", "Endless goal done: {0} € revenue (+{1})! Next: {2} €.");
            // Lebenszeit-Statistik
            A("Allgemein", "General");
            A("Spieltage", "Days played");
            A("Spielzeit", "Play time");
            A("{0} h {1} min", "{0} h {1} min");
            A("Schwierigkeit", "Difficulty");
            A("Erfolge", "Achievements");
            A("Firmenlevel", "Company level");
            A("{0} ({1} XP)", "{0} ({1} XP)");
            A("100.000 € geschafft an Tag", "100,000 € reached on day");
            A("Verkauf", "Sales");
            A("Umsatz gesamt", "Total revenue");
            A("Pakete verschickt", "Parcels shipped");
            A("Verlorene Bestellungen", "Lost orders");
            A("Verkaufsstand", "Market stall");
            A("{0} Artikel", "{0} items");
            A("Straßenfeste", "Street festivals");
            A("{0} ({1} Artikel, {2})", "{0} ({1} items, {2})");
            A("Großaufträge", "Bulk orders");
            A("{0} erfüllt, {1} geplatzt", "{0} completed, {1} failed");
            A("Bewertung", "Rating");
            A("{0} Sterne ({1} Bewertungen)", "{0} stars ({1} reviews)");
            A("Logistik", "Logistics");
            A("Porto bezahlt", "Postage paid");
            A("Retouren", "Returns");
            A("{0} ({1} erstattet)", "{0} ({1} refunded)");
            A("Defekte Ware verschickt", "Defective goods shipped");
            A("Müll entsorgt", "Trash disposed of");
            A("{0} Ladungen", "{0} loads");
            A("Verspätet durch Störungen", "Delayed by disruptions");
            A("Marketing", "Marketing");
            A("Werbung ausgegeben", "Spent on ads");
            A("TikToks", "TikToks");
            A("{0} eigene (bestes: {1} %)", "{0} own (best: {1} %)");
            A("Fakes aufgeflogen", "Fakes busted");
            A("Finanzen", "Finances");
            A("Höchster Kontostand", "Highest balance");
            A("Dividenden", "Dividends");
            A("Sparzinsen", "Savings interest");
            A("Einkaufsbummel (Läden)", "Shopping sprees (stores)");
            A("Mama-Notkredit", "Mom's emergency loan");
            A("nie gebraucht", "never needed");
            A("{0}× (offen: {1})", "{0}× (outstanding: {1})");
        }

        // =====================================================================================
        // Sim.Festival (Straßenfest / Flohmarkt)
        // =====================================================================================
        private static void SimFestival(System.Action<string, string> A)
        {
            A("Straßenfest", "Street Festival");
            A("Flohmarkt", "Flea Market");
            A("Sommerfest", "Summer Fair");
            A("Nachtflohmarkt", "Night Flea Market");
            A("Nehm ich!", "I'll take it!");
            A("Super Teil!", "Great stuff!");
            A("Perfekt als Geschenk!", "Perfect as a gift!");
            A("Hab ich schon auf TikTok gesehen!", "Saw this on TikTok!");
            A("Da kann man nicht meckern.", "Can't complain about that.");
            A("Einmal bitte!", "One please!");
            A("Endlich hab ich's!", "Finally got one!");
            A("Machst du's billiger?", "Can you do it cheaper?");
            A("Letztes Angebot!", "Final offer!");
            A("Na komm, Festpreis?", "Come on, festival price?");
            A("Mal schauen ...", "Let's see ...");
            A("Nett hier.", "Nice stall.");
            A("Hmm.", "Hmm.");
            A("Ich guck nur.", "Just looking.");
            A("{0} heute ab {1}", "{0} today from {1}");
            A("{0} am Tag {1} ({2})", "{0} on day {1} ({2})");
            A("Vorbereitung – noch {0} bis Start", "Setup – {0} until start");
            A("{0} läuft – noch {1}", "{0} is on – {1} left");
            A("Kein Fest geplant", "No festival planned");
            A("Stadtmarketing", "City Marketing");
            A("heute", "today");
            A("morgen", "tomorrow");
            A("am Tag {0}", "on day {0}");
            A("{0} heute!", "{0} today!");
            A("{0} morgen!", "{0} tomorrow!");
            A("{0} am Tag {1}!", "{0} on day {1}!");
            A("Hallo {0},\n\n{1} ({2}) ist {3} vor deiner Tür – die Straße wird gesperrt, es kommen hunderte Besucher. Wir haben dir einen Marktstand reserviert, gratis!\n\nAb {4} Uhr: Aufbau. Pack Ware aus dem Lager in Festkisten (Packtisch am Stand) oder bring volle Lieferkisten direkt hin.\nVon {5} bis {6} Uhr: Verkauf zu deinen Webshop-Preisen. Faire Preise und gehypte Produkte ziehen, zu teure Ware wird weggefeilscht.\n\nBis dann!",
                "Hi {0},\n\ncoming up {1} ({2}): {3}, right outside your door – the street gets closed off and hundreds of visitors are coming. We've reserved a market stall for you, free of charge!\n\nFrom {4}: setup. Pack stock from your storage into festival crates (packing table at the stall) or bring full delivery crates straight over.\nFrom {5} to {6}: selling at your web shop prices. Fair prices and hyped products pull crowds, overpriced stuff gets haggled down.\n\nSee you there!");
            A("{0}: Aufbau läuft! Pack Ware ein und bring sie zum Marktstand auf der Straße.", "{0}: setup has started! Pack some stock and bring it to the market stall on the street.");
            A("{0} beginnt! {1} Artikel am Stand.", "{0} starts! {1} items at the stall.");
            A("{0} beginnt! Dein Stand ist noch leer!", "{0} starts! Your stall is still empty!");
            A("Packen geht nur während des Festes (ab Aufbau).", "Packing only works during the festival (from setup on).");
            A("Davon ist nichts auf Lager.", "None of that in stock.");
            A("Der Marktstand ist gerade nicht aufgebaut.", "The market stall isn't set up right now.");
            A("Der Marktstand ist voll.", "The market stall is full.");
            A("{0}: {1} verkauft, {2} Umsatz.", "{0}: {1} sold, {2} revenue.");
            A("Bilanz: {0}", "Wrap-up: {0}");
            A("Danke fürs Mitmachen!\n\n{0} Besucher am Stand, {1} haben gekauft ({2} Stück).\nUmsatz: {3} · +{4} XP · Bekanntheit +{5} %\nGefeilscht: {6}× · Zu teuer: {7}×",
                "Thanks for joining!\n\n{0} visitors at the stall, {1} bought something ({2} units).\nRevenue: {3} · +{4} XP · Awareness +{5} %\nHaggled: {6}× · Too expensive: {7}×");
            A("Danke fürs Mitmachen!\n\n{0} Besucher am Stand, {1} haben gekauft ({2} Stück).\nUmsatz: {3} · +{4} XP · Bekanntheit +{5} %\nGefeilscht: {6}× · Zu teuer: {7}×\n{8} Artikel gingen zurück ins Lager.",
                "Thanks for joining!\n\n{0} visitors at the stall, {1} bought something ({2} units).\nRevenue: {3} · +{4} XP · Awareness +{5} %\nHaggled: {6}× · Too expensive: {7}×\n{8} items went back into storage.");
            A("{0} ist vorbei: {1} verkauft, {2}.", "{0} is over: {1} sold, {2}.");
            A("Ein Fest geht nur im laufenden Geschäftstag.", "A festival only works during a running business day.");
        }

        // =====================================================================================
        // Sim.Progress (Perks, Einrichtung, Ausrüstung, Porto, Störungen, Defekte, Müll, Großhändler)
        // =====================================================================================
        private static void SimProgress(System.Action<string, string> A)
        {
            A("Dafür brauchst du Firmenlevel {0}.", "You need company level {0} for that.");
            A("Kein Hustle-Punkt frei. Nächster in {0} XP.", "No hustle point left. Next one in {0} XP.");
            A("Perk: {0} Stufe {1} ({2})", "Perk: {0} rank {1} ({2})");
            A("+1 Hustle-Punkt! Ausgeben: Laptop › Firma › Perks", "+1 hustle point! Spend it: Laptop › Company › Perks");
            A("Ab Firmenlevel {0}.", "From company level {0}.");
            A("{0} gekauft: {1}", "{0} bought: {1}");
            A("Du wirst müde und langsamer. Besseres Bett, Kaffee oder Stuhl helfen (Firma › Einrichtung).", "You're getting tired and slower. A better bed, coffee or chair helps (Company › Furnishing).");
            A("Versand ab jetzt mit {0}.", "Shipping with {0} from now on.");
            A("{0}! {1} Tipp: PaketBlitz-App › TurboKurier.", "{0}! {1} Tip: ParcelBolt app › TurboKurier.");
            A("PaketBlitz Störungsmeldung", "ParcelBolt service alert");
            A("{0} Voraussichtlich bis heute Abend. Für pünktliche Lieferung kannst du auf TurboKurier Express umstellen (teurer).", "{0} Expected to last until this evening. For on-time delivery you can switch to TurboKurier Express (pricier).");
            A("{0} Voraussichtlich bis {1}. Für pünktliche Lieferung kannst du auf TurboKurier Express umstellen (teurer).", "{0} Expected to last until {1}. For on-time delivery you can switch to TurboKurier Express (pricier).");
            A("Lieferstörung: {0}", "Delivery disruption: {0}");
            A("Der Paketdienst läuft wieder normal.", "The parcel service is back to normal.");
            A("Qualitätsalarm: {0}× {1} aus der letzten Lieferung sind defekt! Rückruf im Laptop (Firma › Logistik) – sonst drohen Retouren.", "Quality alert: {0}× {1} from the last delivery are defective! Recall them on the laptop (Company › Logistics) – or brace for returns.");
            A("Qualitätskontrolle", "Quality Control");
            A("Fehlerhafte Charge: {0}", "Defective batch: {0}");
            A("Bei der Wareneingangsprüfung ist uns aufgefallen: Etwa {0} Stück {1} sind defekt. Verkaufst du sie, kommen viele zurück und es hagelt 1-Stern-Bewertungen. Du kannst die defekten Stück an den Lieferanten zurückschicken (volle Erstattung).",
                "Our goods-in check found that about {0} units of {1} are defective. If you sell them, lots will come back and the 1-star reviews will rain down. You can send the defective units back to the supplier (full refund).");
            A("Fehlerhafte Charge: {0}× {1}", "Defective batch: {0}× {1}");
            A("{0}× {1} zurückgeschickt. Erstattung: {2}.", "{0}× {1} sent back. Refund: {2}.");
            A("{0}: Pakete kommen zu spät an – Kundschaft ist sauer. TurboKurier ist immun.", "{0}: parcels arrive late – customers are mad. TurboKurier is immune.");
            A("Mülltonnen voll! Personal wird langsamer, du stolperst über Säcke, die Nachbarn meckern. Sonderabholung oder Container (Firma › Einrichtung).", "Bins are full! Staff slow down, you trip over bags, the neighbours complain. Book a special pickup or a container (Company › Furnishing).");
            A("Mülltonnen fast voll ({0}/{1}). Nächste Abholung: {2}.", "Bins almost full ({0}/{1}). Next pickup: {2}.");
            A("Müllabfuhr war da: {0} Ladungen Kartons und Folie weg.", "The garbage truck came by: {0} loads of boxes and film gone.");
            A("Der Müllwagen ist schon unterwegs.", "The garbage truck is already on its way.");
            A("Nicht genug Geld für die Sonderabholung.", "Not enough money for the special pickup.");
            A("Sonderabholung bestellt ({0}).", "Special pickup booked ({0}).");
            A("heute {0}:{1}", "today {0}:{1}");
            A("bald", "soon");
            A("Großhändler-Anfrage: {0} will {1} deiner Marke ({2}).", "Wholesaler request: {0} wants {1} of your brand ({2}).");
            A("Müll quillt über – die Nachbarn beschweren sich (Bewertung −{0}).", "Trash is overflowing – the neighbours are complaining (rating −{0}).");
            A("Müde · {0} Energie", "Tired · {0} energy");
            A("Müll voll {0}/{1}", "Trash full {0}/{1}");
            A("Müll {0}/{1}", "Trash {0}/{1}");
            A("{0} (TurboKurier)", "{0} (TurboKurier)");
            A("Defekte Charge im Lager", "Defective batch in storage");
        }

        // =====================================================================================
        // Sim.Shops (Läden, Haustiere) / Sim.Skills
        // =====================================================================================
        private static void SimShopsSkills(System.Action<string, string> A)
        {
            A("MediaMarkd", "MegaMarked");
            A("Fressnix", "Chewnothing");
            A("Gibt's hier nicht.", "Not sold here.");
            A("Erst die Schicht bei Kalle beenden.", "Finish your shift at Kalle's first.");
            A("Ausverkauft für heute.", "Sold out for today.");
            A("Wagen voll ({0} Packungen).", "Cart full ({0} packs).");
            A("Dein Shop darf das erst ab Level {0} verkaufen.", "Your shop can't sell that until level {0}.");
            A("Dafür hast du noch kein Regal (Lagerhalle).", "You don't have a shelf for that yet (warehouse).");
            A("Hast du schon.", "You already have that.");
            A("Erst ein Tier adoptieren.", "Adopt a pet first.");
            A("Du hast schon einen Hund.", "You already have a dog.");
            A("Du hast schon eine Katze.", "You already have a cat.");
            A("Mehr Tiere passen nicht in die Garage.", "No more pets fit in the garage.");
            A("Der Wagen ist leer.", "The cart is empty.");
            A("Karte abgelehnt! Dir fehlen {0}.", "Card declined! You're {0} short.");
            A("Bezahlt: {0} bei {1}.", "Paid: {0} at {1}.");
            A("Bezahlt: {0} bei {1} (gespart: {2}).", "Paid: {0} at {1} (saved: {2}).");
            A("Bezahlt: {0} bei {1}. Ware liefert der Lieferservice an deinen Wareneingang.", "Paid: {0} at {1}. Delivery service brings the goods to your goods-in.");
            A("Bezahlt: {0} bei {1} (gespart: {2}). Ware liefert der Lieferservice an deinen Wareneingang.", "Paid: {0} at {1} (saved: {2}). Delivery service brings the goods to your goods-in.");
            A("{0} ({1}) zieht bei dir ein! Futter nicht vergessen.", "{0} ({1}) moves in with you! Don't forget the food.");
            A("hungrig", "hungry");
            A("überglücklich", "overjoyed");
            A("gut gelaunt", "in a good mood");
            A("mürrisch", "grumpy");
            A("traurig", "sad");
            A("{0} hat heute schon genug Streicheleinheiten.", "{0} has had enough cuddles for today.");
            A("{0} schnurrt/wedelt. Dein Foto davon sammelt Likes.", "{0} purrs/wags. Your photo of it racks up likes.");
            A("{0} kommt mit.", "{0} is coming along.");
            A("{0} bleibt in der Garage.", "{0} stays in the garage.");
            A("{0} hat Hunger! Futter gibt's bei {1}.", "{0} is hungry! Get food at {1}.");
            A("{0} war gut drauf: Tier-Content bringt Bekanntheit.", "{0} was in a great mood: pet content brings awareness.");
            A("{0} ist zu Oma Hildegard nebenan gezogen. Da gibt's wenigstens Futter.", "{0} moved in with Grandma Hildegard next door. At least she has food.");
            A("{0} ist weggelaufen (kein Futter).", "{0} ran away (no food).");
            A("Lern zuerst „{0}“.", "Learn “{0}” first.");
            A("Kein Skillpunkt frei. Den nächsten gibt's beim Level-Aufstieg.", "No skill point left. You get the next one on level-up.");
            A("Skill gelernt: {0}!", "Skill learned: {0}!");
            A("Die Umschulung kostet {0}. So viel hast du nicht.", "Retraining costs {0}. You don't have that much.");
            A("Umschulung abgeschlossen – alle Skillpunkte sind wieder frei.", "Retraining done – all skill points are free again.");
        }

        // =====================================================================================
        // Sim.Save / Model (Gegenstände in der Hand)
        // =====================================================================================
        private static void SimSaveModel(System.Action<string, string> A)
        {
            A("Speichern fehlgeschlagen!", "Saving failed!");
            A("Spiel gespeichert (Spielstand {0}).", "Game saved (slot {0}).");
            A("Kiste {0}× {1}", "Crate {0}× {1}");
            A("{0} (Großauftrag)", "{0} (bulk order)");
            A("Paket ({0})", "Parcel ({0})");
            A("Versandfertiges Paket ({0})", "Ready-to-ship parcel ({0})");
            A("Teller", "Plate");
            A("Retoure ({0})", "Return ({0})");
            A("Gegenstand", "Item");
        }

        // =====================================================================================
        // Market / Market.Exchange (Börse, Bank, Krypto)
        // =====================================================================================
        private static void MarketTexts(System.Action<string, string> A)
        {
            A("Billig, schnell, fragwürdig.", "Cheap, fast, questionable.");
            A("Hip, teuer, schöne Fotos.", "Hip, pricey, pretty photos.");
            A("Lieferzeit: ja.", "Delivery time: yes.");
            A("Hochriskante Krypto. Kann alles – vor allem abstürzen.", "High-risk crypto. Can do anything – mostly crash.");
            A("Meme-Aktie. Das Internet liebt sie. Meistens.", "Meme stock. The internet loves it. Mostly.");
            A("Welt-ETF", "World ETF");
            A("KeksCoin", "CookieCoin");
            A("MondMuffin", "MoonMuffin");
            A("BlockKette", "BlokChain");
            A("Langweilig. Und genau deshalb solide.", "Boring. And that's exactly why it's solid.");
            A("Krypto", "Crypto");
            A("Gaming", "Gaming");
            A("Welt", "World");
            A("Verpackung", "Packaging");
            A("Medien", "Media");
            A("Food", "Food");
            A("Tech", "Tech");
            A("Mobilität", "Mobility");
            A("Breiter Markt", "Broad market");
            A("Meme", "Meme");
            A("Aktie", "Stock");
            A("ETF", "ETF");
            A("Macht Kartons. Langweilig, aber jeder Shop braucht sie. Zahlt brav Dividende.", "Makes boxes. Boring, but every shop needs them. Pays its dividend like a good boy.");
            A("Liefert schnell. Manchmal sogar an die richtige Adresse.", "Delivers fast. Sometimes even to the right address.");
            A("Influencer-Agentur. Umsatz: Likes. Gewinn: auch Likes.", "Influencer agency. Revenue: likes. Profit: also likes.");
            A("Pommes für ganz Europa. Krisensicher, weil Hunger immer geht.", "Fries for all of Europe. Recession-proof, because hunger never goes out of style.");
            A("KI für alles. Auch für Toaster. Vor allem für Toaster.", "AI for everything. Even toasters. Especially toasters.");
            A("E-Roller-Verleih. Die Roller liegen im Fluss, die Aktie manchmal auch.", "E-scooter rental. The scooters end up in the river, and sometimes so does the stock.");
            A("Der ganze Kiez-Markt in einem Klick. Breit gestreut, wenig Drama.", "The whole block market in one click. Well diversified, little drama.");
            A("Alles mit KI, Likes und Pixeln in einem Korb.", "Everything with AI, likes and pixels in one basket.");
            A("Ein Hund auf einem Keks. Marktkapitalisierung: unerklärlich.", "A dog on a cookie. Market cap: inexplicable.");
            A("Die seriöse Krypto. Hat ein Whitepaper. Niemand hat es gelesen.", "The serious crypto. Has a whitepaper. Nobody has read it.");
            A("To the moon! Oder in den Keller. Keine Fahrstuhlmusik.", "To the moon! Or down to the basement. No elevator music.");
            A("Nicht genug Geld für diesen Kauf.", "Not enough money for this purchase.");
            A("Dividende über Nacht: +{0}.", "Overnight dividend: +{0}.");
            A("So viel ist nicht auf dem Girokonto.", "There isn't that much in your checking account.");
            A("Bank: {0} vom Sparkonto aufs Girokonto umgebucht (Dispo-Schutz).", "Bank: {0} moved from savings to checking (overdraft protection).");
            A("Goldkunde", "Gold customer");
            A("Solide", "Solid");
            A("Joa", "Meh");
            A("Wackelig", "Shaky");
            A("Bitte gehen Sie", "Please leave");
        }

        // =====================================================================================
        // Trends (Phasen, Trendradar) + Stufen/Logos
        // =====================================================================================
        private static void TrendTexts(System.Action<string, string> A)
        {
            A("stabil", "stable");
            A("steigend", "rising");
            A("Hype!", "Hype!");
            A("fallend", "falling");
            A("tot", "dead");
            A("{0}Challenge", "{0}Challenge");
            A("Der Hype wächst – jetzt Lager auffüllen!", "The hype is growing – stock up now!");
            A("Höhepunkt: Die Leute kaufen fast jeden Preis. Aber bald ist es vorbei.", "Peak: people pay almost any price. But it'll be over soon.");
            A("Der Hype flaut ab – nicht mehr zu viel nachkaufen.", "The hype is fading – don't restock too much.");
            A("Keiner will's gerade. Preis senken oder aussitzen.", "Nobody wants it right now. Lower the price or wait it out.");
            A("Hype im Anflug", "Hype incoming");
            A("Der Trendradar sieht etwas kommen. Vorräte anlegen?", "The trend radar sees something coming. Stock up?");
            A("erholt sich", "recovering");
            A("Die Nachfrage kommt langsam zurück.", "Demand is slowly coming back.");
            A("Keine Auffälligkeiten.", "Nothing unusual.");
            A("Keine Auffälligkeiten – soweit das Radar es erkennt.", "Nothing unusual – as far as the radar can tell.");
            A("Garage", "Garage");
            A("Lagerhalle", "Warehouse");
            A("Kreis", "Circle");
            A("Quadrat", "Square");
            A("Raute", "Diamond");
            A("Stern", "Star");
            A("Blitz", "Bolt");
            A("Herz", "Heart");
        }

        // =====================================================================================
        // PitchGame (Investoren-Pitch) / TikTokStudio / Furniture
        // =====================================================================================
        private static void PitchTikTokFurniture(System.Action<string, string> A)
        {
            A("Frau Dr. Brandt, Einkauf", "Dr. Brandt, Purchasing");
            A("Warum sollten wir ausgerechnet Ihre Marke ins Regal stellen?", "Why on earth should we put your brand on our shelves?");
            A("Unsere Kunden lieben uns – schauen Sie sich die Bewertungen an.", "Our customers love us – just look at the reviews.");
            A("Wir haben schon {0} Pakete verschickt. Die Nachfrage ist da.", "We've already shipped {0} parcels. The demand is there.");
            A("Weil ich heute extra einen Anzug trage. Einen richtigen.", "Because I wore a suit today. A real one.");
            A("Herr Okafor, Logistik", "Mr Okafor, Logistics");
            A("Wie wollen Sie 500 Filialen zuverlässig beliefern?", "How do you plan to reliably supply 500 stores?");
            A("Mit meinem eigenen Lager und einem eingespielten Team.", "With my own warehouse and a well-oiled team.");
            A("Unsere Lieferanten liefern blitzschnell, auch in großen Mengen.", "Our suppliers deliver lightning fast, even in bulk.");
            A("Notfalls fahre ich selbst. Mit dem Fahrrad.", "If need be, I'll drive myself. By bike.");
            A("Frau Petrović, Marketing", "Ms Petrović, Marketing");
            A("Was unterscheidet Sie von BilligBoy24?", "What sets you apart from BilligBoy24?");
            A("Qualität statt Ramsch. Unsere Ware hält.", "Quality, not junk. Our stuff lasts.");
            A("Wir sind günstiger als der Markt – und trotzdem profitabel.", "We're cheaper than the market – and still profitable.");
            A("{0} hat Stil. Die Leute kennen uns.", "{0} has style. People know us.");
            A("Herr Lindqvist, Finanzen", "Mr Lindqvist, Finance");
            A("Zeigen Sie mir Zahlen. Wie läuft das Geschäft?", "Show me numbers. How's business?");
            A("Wir haben bisher {0} umgesetzt.", "We've done {0} in revenue so far.");
            A("Wir wachsen jeden Tag. Schauen Sie sich die Kurve an.", "We grow every day. Just look at that curve.");
            A("Genug, um im Winter nicht zu frieren.", "Enough not to freeze in winter.");
            A("Frau Weber, Nachhaltigkeit", "Ms Weber, Sustainability");
            A("Wie gehen Sie mit Beschwerden und Rücksendungen um?", "How do you handle complaints and returns?");
            A("Schnell und freundlich. Deshalb bleiben unsere Bewertungen gut.", "Fast and friendly. That's why our reviews stay good.");
            A("Unsere Retourenquote liegt bei nur {0}. Wir prüfen jede Retoure.", "Our return rate is just {0}. We check every single return.");
            A("Beschwerden? Hatten wir noch nie. Glaube ich.", "Complaints? Never had any. I think.");
            A("verschickte Pakete", "parcels shipped");
            A("Glück", "Luck");
            A("Lager & Team", "Warehouse & team");
            A("Warenqualität", "Product quality");
            A("Preise", "Prices");
            A("Marke & Bekanntheit", "Brand & awareness");
            A("Umsatz", "Revenue");
            A("Wachstum", "Growth");
            A("Retourenquote", "Return rate");
            A("Die Jury nickt anerkennend. Jemand macht sich eine Notiz.", "The jury nods approvingly. Someone takes a note.");
            A("Solide Antwort. Die Jury bleibt freundlich, aber vorsichtig.", "Solid answer. The jury stays friendly but cautious.");
            A("Eine Jurorin zieht die Augenbraue hoch.", "One juror raises an eyebrow.");
            A("Stille. Irgendwo hustet jemand.", "Silence. Somewhere, someone coughs.");
            A("Deal! MegaMarkt nimmt dich ins Sortiment.", "Deal! MegaMarkt adds you to its range.");
            A("Standing Ovations. Du bekommst einen Großauftrag über {0}.", "Standing ovation. You land a bulk order worth {0}.");
            A("Ein kleiner Testauftrag.", "A small trial order.");
            A("Die Jury ist noch nicht ganz überzeugt, bestellt aber eine Testlieferung über {0}.", "The jury isn't fully convinced yet, but orders a trial delivery worth {0}.");
            A("Kein Deal.", "No deal.");
            A("'Wir melden uns.' Sie melden sich nicht. Nächstes Mal mit besseren Zahlen!", "'We'll be in touch.' They won't. Better numbers next time!");
            // TikTok-Studio
            A("Unboxing", "Unboxing");
            A("POV", "POV");
            A("Review", "Review");
            A("Life-Hack", "Life hack");
            A("Video", "Video");
            A("Nah ran, starker Hook in den ersten 2 Sekunden, Produkt auspacken (Aktion).", "Get close, strong hook in the first 2 seconds, unpack the product (action).");
            A("Ruhige Kamera, einmal ums Produkt herum, Deko/Marke mit ins Bild.", "Steady camera, once around the product, get decor/brand in the shot.");
            A("Produkt mittig, gutes Licht, ruhig halten und lang genug (15 s+).", "Product centred, good light, hold steady and long enough (15 s+).");
            A("Hook zuerst, dann mehrmals vorführen (Aktion), Produkt immer im Bild.", "Hook first, then demo it several times (action), product always in shot.");
            A("das hier", "this thing");
            A("UNBOXING: {0} (gone right!)", "UNBOXING: {0} (gone right!)");
            A("Unboxing: {0}", "Unboxing: {0}");
            A("POV: du hast endlich {0}", "POV: you finally got {0}");
            A("Ehrliche Review: {0} (5/5!)", "Honest review: {0} (5/5!)");
            A("Review: {0} – lohnt sich das?", "Review: {0} – is it worth it?");
            A("Life-Hack mit {0}, den JEDER braucht", "Life hack with {0} that EVERYONE needs");
            A("Produkt im Bild", "Product in shot");
            A("Bildmitte", "Centre of frame");
            A("Abstand", "Distance");
            A("Ruhige Kamera", "Steady camera");
            A("Licht", "Light");
            A("Blickwinkel", "Angles");
            A("Länge", "Length");
            A("Hook (erste 2 s)", "Hook (first 2 s)");
            A("Aktionen", "Actions");
            A("Marke/Deko im Bild", "Brand/decor in shot");
            A("Halte das Produkt die ganze Zeit im Rahmen.", "Keep the product in frame the whole time.");
            A("Produkt in die Bildmitte.", "Put the product in the centre.");
            A("Näher ran – etwa eine Armlänge.", "Get closer – about an arm's length.");
            A("Langsamer schwenken.", "Pan more slowly.");
            A("Mehr Licht: Lampe, Ringlicht oder tagsüber drehen.", "More light: lamp, ring light or shoot during the day.");
            A("Geh einmal ums Produkt herum.", "Walk once around the product.");
            A("Mindestens {0} Sekunden aufnehmen.", "Record at least {0} seconds.");
            A("Gleich am Anfang eine Aktion (E) für den Hook.", "Do an action (E) right at the start for the hook.");
            A("Mehr Aktionen: auspacken, schütteln, zeigen.", "More actions: unpack, shake, show off.");
            A("Poster, Neon oder Schild mit ins Bild.", "Get a poster, neon or sign into the shot.");
            A("Das Produkt war kaum im Bild.", "The product was barely in the shot.");
            A("Viel zu kurz.", "Way too short.");
            A("Viral-Material", "Viral material");
            A("Stark", "Strong");
            A("Ganz okay", "Pretty okay");
            A("Naja", "Meh");
            A("Cringe", "Cringe");
            A("Produkt nicht im Bild!", "Product not in shot!");
            A("Produkt im Bild ✓", "Product in shot ✓");
            A("mehr zur Mitte", "more to the centre");
            A("näher ran", "get closer");
            A("zu nah", "too close");
            A("wackelt", "shaky");
            A("zu dunkel", "too dark");
            A("Jetzt Hook!", "Hook now!");
            A("Länge ✓", "Length ✓");
            A("Feierabend – morgen wieder.", "Closing time – back tomorrow.");
            A("TikTok gibt's ab Firmenlevel {0}.", "TikTok unlocks at company level {0}.");
            A("Tageslimit erreicht ({0} pro Tag).", "Daily limit reached ({0} per day).");
            A("Nächstes Video in {0} min.", "Next video in {0} min.");
            // Möbel-Platzierung
            A("Ungültige Position", "Invalid position");
            A("Muss im Gebäude stehen", "Must be inside the building");
            A("Tür / Durchgang frei halten", "Keep doors / passages clear");
            A("Platz belegt", "Space occupied");
            A("Stößt an: {0}", "Bumps into: {0}");
        }

        // @@NEXT
    }
}
