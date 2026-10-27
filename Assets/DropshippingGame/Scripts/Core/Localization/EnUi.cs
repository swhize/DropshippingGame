namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Oberfläche außerhalb des Laptops (Runtime/UI: HUD, Handy, Menüs, Einstellungen, Hilfe, Admin, Dialoge, TikTok-Studio, Kasse, Finanz-UI).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnUi
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
            Common(A);
            Hud(A);
            Phone(A);
            Menus(A);
            SettingsUi(A);
            Input(A);
            DaySummary(A);
            TikTok(A);
            Shops(A);
            Finance(A);
            Advisor(A);
            Root(A);
            Admin(A);
            Misc(A);
        }

        // ---- Allgemein: Wochentage (Großschrift für Banner), Zeit, kleine Wörter ----------------------
        private static void Common(System.Action<string, string> A)
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
            A("MONTAG", "MONDAY");
            A("DIENSTAG", "TUESDAY");
            A("MITTWOCH", "WEDNESDAY");
            A("DONNERSTAG", "THURSDAY");
            A("FREITAG", "FRIDAY");
            A("SAMSTAG", "SATURDAY");
            A("SONNTAG", "SUNDAY");
            A("MONTAG · TAG {0}", "MONDAY · DAY {0}");
            // UiFmt.DayShort / DayLong
            A("{0} · Tag {1}", "{0} · Day {1}");
            A("{0}, Tag {1}", "{0}, Day {1}");
            A("Tag {0}", "Day {0}");
            A("Woche {0}", "Week {0}");
            A("WOCHE {0}", "WEEK {0}");
            // UiFmt.Duration / Percent / UntilClosing
            A("{0} min", "{0} min");
            A("{0} h {1} min", "{0} h {1} min");
            A("Feierabend", "Closing time");
            A("noch {0} bis 20:00", "{0} left until 20:00");
            A("{0} · {1} Uhr", "{0} · {1}");
            A("{0}. Dann wird abgerechnet.", "{0}. Then it's payday for the landlord.");
            A("Zurück", "Back");
            A("Abbrechen", "Cancel");
            A("Schließen", "Close");
            A("Fertig", "Done");
            A("Später", "Later");
            A("Weiter", "Next");
            A("Ja", "Yes");
            A("Nein", "No");
            A("Laden", "Load");
            A("Speichern", "Save");
            A("Löschen", "Delete");
            A("Verstanden", "Got it");
            A("Super", "Awesome");
            A("Los geht's", "Let's go");
            A("Zeigen", "Show");
            A("Lesen", "Read");
            A("EXPRESS", "EXPRESS");
            A("Express", "Express");
            A("MAX", "MAX");
            A("LV {0}", "LV {0}");
            A("Level {0}", "Level {0}");
            A("Ups.", "Oops.");
            A("Du", "You");
            A("DU", "YOU");
            A("Kalles Imbiss", "Kalle's Diner");
            A("Passant:in", "Passer-by");
            A("Bestellung erledigt", "Order done");
            A("Kundschaft glücklich", "Happy customer");
            A("Status", "Status");
            A("Unbenannt", "Untitled");
            A("Sonstiges", "Miscellaneous");
            A("gerade eben", "just now");
            A("vor {0} min", "{0} min ago");
            A("vor {0} h", "{0} h ago");
            A("Weiter einkaufen", "Keep shopping");
        }

        // ---- HUD (HudView, HudCompass, Untertitel) --------------------------------------------------
        private static void Hud(System.Action<string, string> A)
        {
            A("Mo · Tag 1", "Mon · Day 1");
            A("MINI-EVENT", "MINI EVENT");
            A("am Wareneingang", "at goods-in");
            A("unterwegs", "in transit");
            A("versandbereit", "ready to ship");
            A("BESTELLUNGEN", "ORDERS");
            A("HÄNDE FREI", "HANDS FREE");
            A("Nichts in der Hand", "Nothing in hand");
            A("IN DER HAND", "IN HAND");
            A("{0} FPS", "{0} FPS");
            // Straßenfest-Karte
            A("MINI-EVENT · ANGEKÜNDIGT", "MINI EVENT · ANNOUNCED");
            A("{0} heute", "{0} today");
            A("{0} morgen", "{0} tomorrow");
            A("Aufbau ab {0} (in {1})", "Setup from {0} (in {1})");
            A("Aufbau ab {0} · Lager auffüllen!", "Setup from {0} · stock up!");
            A("AUFBAU · START IN {0}", "SETUP · STARTS IN {0}");
            A("{0}: Stand bestücken", "{0}: stock the stall");
            A("Stand: {0}/{1} · Festkisten am Packtisch packen", "Stall: {0}/{1} · pack festival crates at the packing table");
            A("LÄUFT · NOCH {0}", "LIVE · {0} LEFT");
            A("{0} verkauft · {1} · Stand: {2}", "{0} sold · {1} · stall: {2}");
            // Tastenhinweise
            A("Handy", "Phone");
            A("Menü", "Menu");
            A("Hilfe", "Help");
            // Bestellzettel
            A("Warte auf Bestellungen …", "Waiting for orders …");
            A("Nichts online – Laptop » Shop", "Nothing listed – Laptop » Shop");
            A("weitere", "more");
            A("Holen", "Pick");
            A("Packen", "Pack");
            A("Label", "Label");
            A("Versand", "Ship");
            A("1× {0}", "1× {0}");
            A("» {0}", "» {0}");
            A("{0} · {1} · {2} · {3} · nächster Schritt: {4} ({5})", "{0} · {1} · {2} · {3} · next step: {4} ({5})");
            // Imbiss-Zettel
            A("Currywurst", "Currywurst");
            A("Pommes rot-weiß", "Fries with ketchup & mayo");
            A("Döner-Teller", "Döner plate");
            A("Tisch {0}", "Table {0}");
            A("serviert", "served");
            A("in der Hand", "in hand");
            A("wartet", "waiting");
            // Ziel-Karte
            A("SCHICHT IM IMBISS", "SHIFT AT THE DINER");
            A("TUTORIAL · SCHRITT {0}/{1}", "TUTORIAL · STEP {0}/{1}");
            A("NÄCHSTES ZIEL", "NEXT GOAL");
            A("ZIEL", "GOAL");
            // Hände
            A("Kiste · {0}× {1}", "Crate · {0}× {1}");
            A("{0}-Qualität · ins Regal oder auf den Stand", "{0} quality · onto a shelf or the stall");
            A("Express · {0}", "Express · {0}");
            A("Für Großauftrag · zum Palettenplatz", "For a bulk order · to the pallet spot");
            A("Für Großauftrag · {0} · zum Palettenplatz", "For a bulk order · {0} · to the pallet spot");
            A("für #{0} · zum Packtisch", "for #{0} · to the packing table");
            A("Für eine Bestellung ({0}) · zum Packtisch", "For an order ({0}) · to the packing table");
            A("Express-Paket · {0}", "Express parcel · {0}");
            A("Paket · {0}", "Parcel · {0}");
            A("zum Labeldrucker", "to the label printer");
            A("{0} · zum Labeldrucker", "{0} · to the label printer");
            A("Express · versandfertig · {0}", "Express · ready to ship · {0}");
            A("Versandfertig · {0}", "Ready to ship · {0}");
            A("ab zur Packstation", "off to the parcel locker");
            A("An: {0} · ab zur Packstation", "To: {0} · off to the parcel locker");
            A("RETOURE", "RETURN");
            A("Retoure: {0}", "Return: {0}");
            A("zum Retourenplatz", "to the returns bay");
            A("„{0}“ · zum Retourenplatz", "“{0}” · to the returns bay");
            A("Teller für Tisch {0}", "Plate for table {0}");
            A("Bring ihn an den richtigen Tisch", "Take it to the right table");
            A("Ablegen", "Drop");
            // Feiern
            A("LEVEL UP", "LEVEL UP");
            A("Firmenlevel {0} erreicht", "Company level {0} reached");
            A("NEU FREIGESCHALTET", "NEWLY UNLOCKED");
            A("+1 Skillpunkt · Laptop » Firma » Skills", "+1 skill point · Laptop » Company » Skills");
            A("GESCHAFFT!", "YOU DID IT!");
            A("Dein erstes Paket ist unterwegs", "Your first parcel is on its way");
            A("+{0} auf dem Konto", "+{0} in the bank");
            A("Das war der ganze Kreislauf – ab jetzt läuft dein Shop!", "That was the whole loop – your shop is officially in business!");
            // Kompass (Himmelsrichtungen + Orte)
            A("O", "E");
            A("NO", "NE");
            A("SO", "SE");
            A("Garage", "Garage");
            A("Packstation", "Parcel locker");
            A("PaketBlitz-Filiale", "ParcelBolt branch");
            A("Bank", "Bank");
            A("Börse", "Stock exchange");
            A("MediaMarkd", "MegaMarked");
            A("Fressnix", "Chewnothing");
            A("Moped", "Moped");
            A("Ziel", "Goal");
            A("Ziel: {0}", "Goal: {0}");
            A("{0} km", "{0} km");
            // Untertitel: <color=#…><b>Name:</b></color> Text
            A("<color={0}><b>{1}:</b></color> {2}", "<color={0}><b>{1}:</b></color> {2}");
        }

        // ---- Handy (PhoneView) ---------------------------------------------------------------------
        private static void Phone(System.Action<string, string> A)
        {
            A("Bestellungen", "Orders");
            A("Nachrichten", "Messages");
            A("Nachbestellen", "Reorder");
            A("Nachkauf", "Restock");
            A("Wochenziele", "Weekly goals");
            A("Woche", "Week");
            A("Kalle hat den Router hinter der Fritteuse versteckt. Erst die Schicht, dann das Business.",
              "Kalle hid the router behind the deep fryer. Shift first, empire later.");
            A("Der Imbiss ist ein Funkloch. Angeblich wegen der Dunstabzugshaube.",
              "The diner is a dead zone. Allegedly because of the extractor hood.");
            A("Einziges Netz hier: das Frittierfett-Sieb.", "The only network here: the deep-fryer sieve.");
            A("Diese App ist kurz abgestürzt. Einfach nochmal öffnen.", "This app just crashed. Simply open it again.");
            A("Kein Empfang", "No signal");
            A("SOS · NUR NOTRUFE", "SOS · EMERGENCY CALLS ONLY");
            A("{0} offen · Platz für {1}", "{0} open · room for {1}");
            A("{0} Eingang", "{0} goods-in");
            A("{0} unterwegs", "{0} in transit");
            A("{0} verpackt", "{0} packed");
            A("Warteschlange voll – neue Bestellungen gehen verloren!", "Queue full – new orders are getting lost!");
            A("Keine offenen Bestellungen", "No open orders");
            A("Neue Bestellungen erscheinen hier – und oben rechts als Zettel.", "New orders show up here – and as slips in the top right.");
            A("Stell am Laptop unter „Shop“ ein Produkt online.", "List a product on the laptop under “Shop”.");
            A("wartet · im Regal: {0}", "waiting · on shelf: {0}");
            A("wartet · nicht auf Lager!", "waiting · out of stock!");
            A("{0} · nächster Schritt: {1} ({2})", "{0} · next step: {1} ({2})");
            A("fällig {0}", "due {0}");
            A("1 Paket wartet auf Label & Versand.", "1 parcel is waiting for label & shipping.");
            A("{0} Pakete warten auf Label & Versand.", "{0} parcels are waiting for label & shipping.");
            A("1 Entscheidung offen", "1 decision pending");
            A("{0} Entscheidungen offen", "{0} decisions pending");
            A("{0} ungelesen", "{0} unread");
            A("Alles gelesen", "All read");
            A("Keine Nachrichten", "No messages");
            A("Hier landen Angebote, Ereignisse und Post von Mama.", "Offers, events and mail from Mum land here.");
            A("Entscheidung · {0} · Tag {1}", "Decision · {0} · Day {1}");
            A("Offene Entscheidungen verfallen um 20 Uhr – dann gilt die vorsichtigste Antwort.",
              "Open decisions expire at 20:00 – then the most cautious answer counts.");
            A("GROSSAUFTRÄGE", "BULK ORDERS");
            A("{0} · {1} Tage Frist · Strafe {2} · Lager {3}", "{0} · {1}-day deadline · penalty {2} · stock {3}");
            A("Ablehnen", "Decline");
            A("Annehmen", "Accept");
            A("NACHRICHTEN", "MESSAGES");
            A("Von {0} · Tag {1}, {2} Uhr", "From {0} · Day {1}, {2}");
            A("DEINE ENTSCHEIDUNG", "YOUR DECISION");
            A("{0}  ·  {1}  ·  Minispiel", "{0}  ·  {1}  ·  minigame");
            A("{0}  ·  Minispiel", "{0}  ·  minigame");
            A("{0}  ·  {1}", "{0}  ·  {1}");
            A("Unbeantwortet gilt um 20 Uhr die vorsichtigste Antwort.", "If you don't answer, the most cautious option kicks in at 20:00.");
            A("ERGEBNIS", "RESULT");
            A("Du hast gewählt: {0}", "You chose: {0}");
            A("Gleiche Menge, gleicher Lieferant wie beim letzten Einkauf.", "Same amount, same supplier as last time.");
            A("Express an: +{0} je Bestellung", "Express on: +{0} per order");
            // Nachbestellen: "Lager 5 · 3 unterwegs · 2 am Eingang · 1 offen · 4 für Aufträge" (Kettenvorlagen)
            A("Lager {0}", "Stock {0}");
            A("Lager {0} · {1}", "Stock {0} · {1}");
            A("{0} unterwegs · {1}", "{0} in transit · {1}");
            A("{0} am Eingang", "{0} at goods-in");
            A("{0} am Eingang · {1}", "{0} at goods-in · {1}");
            A("{0} offen", "{0} open");
            A("{0} offen · {1}", "{0} open · {1}");
            A("{0} für Aufträge", "{0} for bulk orders");
            A("{0} Stk · {1}", "{0} pcs · {1}");
            A("{0} Stk · {1} (heute schon bestellt)", "{0} pcs · {1} (already ordered today)");
            A("Noch nichts im Sortiment", "Nothing in your range yet");
            A("Kaufe am Laptop bei „AllesExpress“ deine erste Ware – danach geht's hier mit einem Tipp.",
              "Buy your first goods on the laptop at “EverythingXpress” – after that, restocking here is one tap.");
            A("Andere Lieferanten, Mengen & neue Produkte: Laptop am Schreibtisch » AllesExpress.",
              "Other suppliers, amounts & new products: laptop at the desk » EverythingXpress.");
            // Status
            A("WARUM LÄUFT'S NICHT?", "WHY ISN'T IT WORKING?");
            A("Kein Engpass in Sicht. Weiter so!", "No bottleneck in sight. Keep hustling!");
            A("Lösung am Laptop (Schreibtisch).", "Fix it at the laptop (desk).");
            A("KONTOSTAND", "BALANCE");
            A("+{0} heute", "+{0} today");
            A("Kredit {0}", "Loan {0}");
            A("BEWERTUNG", "RATING");
            A("{0} Bewertungen", "{0} reviews");
            A("FIRMENLEVEL", "COMPANY LEVEL");
            A("Maximum erreicht", "Maximum reached");
            A("{0} bis Level {1}", "{0} to level {1}");
            A("UMSATZ HEUTE", "REVENUE TODAY");
            A("{0} Pakete verschickt", "{0} parcels shipped");
            A("FIXKOSTEN 20 UHR", "FIXED COSTS 20:00");
            A("Miete, Löhne, Zinsen", "Rent, wages, interest");
            A("{0} heute verloren", "{0} lost today");
            A("keine verloren", "none lost");
            A("LAGER", "STORAGE");
            A("WOCHENZIELE", "WEEKLY GOALS");
            A("Woche endet heute", "Week ends today");
            A("noch {0} Tage", "{0} days left");
            A("SKILLPUNKTE", "SKILL POINTS");
            A("{0} frei", "{0} free");
            A("{0} gelernt", "{0} learned");
            A("RETOUREN", "RETURNS");
            A("{0} unterwegs · Quote {1}", "{0} in transit · rate {1}");
            A("{0} Angebot(e)", "{0} offer(s)");
            A("AKTIVE BOOSTS", "ACTIVE BOOSTS");
        }

        // ---- Hauptmenü, Pause, Hilfe, Credits, Spielstände (MenuViews) -----------------------------
        private static void Menus(System.Action<string, string> A)
        {
            // Hilfe
            A("STEUERUNG", "CONTROLS");
            A("Controller wird automatisch erkannt", "Controllers are detected automatically");
            A("AKTION", "ACTION");
            A("TASTATUR & MAUS", "KEYBOARD & MOUSE");
            A("CONTROLLER", "CONTROLLER");
            A("PlayStation: Kreuz = A · Kreis = B · Quadrat = X · Dreieck = Y · Options = Start · Touchpad/Share = Select",
              "PlayStation: Cross = A · Circle = B · Square = X · Triangle = Y · Options = Start · Touchpad/Share = Select");
            A("SO LÄUFT DEIN BUSINESS", "HOW YOUR BUSINESS WORKS");
            A("Am <b>Laptop</b> (Werkbank in der Garage) bei <b>AllesExpress</b> Ware bestellen – Nachbestellen geht später auch per Handy.",
              "Order goods on the <b>laptop</b> (workbench in the garage) at <b>EverythingXpress</b> – later you can also reorder by phone.");
            A("Die Kiste vom Lieferwagen am <b>Wareneingang</b> holen und ins passende <b>Regal</b> räumen.",
              "Grab the crate from the van at <b>goods-in</b> and put it on the matching <b>shelf</b>.");
            A("Im Laptop unter <b>Mein Shop</b> das Produkt online schalten und den Preis festlegen.",
              "On the laptop under <b>My Shop</b>, list the product and set the price.");
            A("Bestellungen erscheinen <b>oben rechts als Zettel</b> mit Countdown. Je schneller verschickt, desto besser die Bewertung.",
              "Orders appear <b>as slips in the top right</b> with a countdown. The faster you ship, the better the rating.");
            A("Artikel aus dem Regal nehmen und am <b>Packtisch</b> verpacken.",
              "Take the item from the shelf and pack it at the <b>packing table</b>.");
            A("Das Paket vom Packtisch nehmen und am <b>Labeldrucker</b> das Versandlabel drucken.",
              "Take the parcel from the packing table and print the shipping label at the <b>label printer</b>.");
            A("Paket abgeben: <b>Packstation</b> in der Stadt oder <b>PaketBlitz-Filiale</b> (gratis) – oder bequem an der <b>Abholbox am Garagentor</b> (3 € pro Paket). Geld kassieren!",
              "Drop off the parcel: <b>parcel locker</b> in town or <b>ParcelBolt branch</b> (free) – or the lazy way at the <b>pickup box by the garage door</b> (3 € per parcel). Cash in!");
            A("HANDY, LAPTOP & WACHSTUM", "PHONE, LAPTOP & GROWTH");
            A("Mit dem <b>Handy</b> ({0} / {1}) siehst du Bestellzettel, beantwortest Nachrichten, bestellst mit einem Tipp nach und prüfst deinen Kontostand – die Welt läuft dabei weiter. Den Laptop gibt es an der Werkbank bzw. am Schreibtisch: AllesExpress, Mein Shop, PaketBlitz, TikTak, Revoluut, Firma und mehr. Der <b>Kompass</b> oben zeigt Garage, nächste Packstation, Post, Bank, Börse, MediaMarkd und Fressnix.",
              "With your <b>phone</b> ({0} / {1}) you see order slips, answer messages, reorder with one tap and check your balance – the world keeps running meanwhile. The laptop lives on the workbench or desk: EverythingXpress, My Shop, ParcelBolt, TikTak, Revoluut, Company and more. The <b>compass</b> at the top shows the garage, nearest parcel locker, post office, bank, stock exchange, MegaMarked and Chewnothing.");
            A("Gute Preise, schneller Versand und Qualität bringen gute Bewertungen und mehr Kundschaft. Mit Erfahrung steigt dein Firmenlevel und schaltet Produkte, Lagerhalle, Personal und mehr frei. Um 20 Uhr ist Feierabend – dann werden Miete, Löhne und Zinsen fällig. Unter 40 <b>Energie</b> wirst du langsamer (Schlaf, Bett, Kaffee helfen). Neue Funktionen erklärt dir Marvin per Nachricht im Handy.",
              "Good prices, fast shipping and quality bring good reviews and more customers. Experience raises your company level and unlocks products, the warehouse, staff and more. Closing time is 20:00 – that's when rent, wages and interest are due. Below 40 <b>energy</b> you slow down (sleep, a bed and coffee help). Marvin explains new features via phone messages.");
            A("<b>TikTok</b> (ab Level {0}): Handy › TikTak, Laptop › TikTak oder das Ringlicht am Schreibtisch. Produkt und Format wählen, dann filmst du 10–20 Sekunden selbst: Produkt mittig im 9:16-Rahmen, nah genug, ruhig, gutes Licht, einmal herumgehen, gleich zu Beginn eine Aktion ({1} / {2}) als Hook und Deko mit ins Bild. Danach Ergebnis ansehen und posten oder neu aufnehmen.",
              "<b>TikTok</b> (from level {0}): Phone › TikTak, Laptop › TikTak or the ring light at the desk. Pick product and format, then film 10–20 seconds yourself: product centred in the 9:16 frame, close enough, steady, good light, walk around it once, an action right at the start ({1} / {2}) as the hook, and some decor in the shot. Then review the result and post it or record again.");
            // Credits
            A("Dropshipping Simulator – Vom Imbiss zum Imperium", "Dropshipping Simulator – From Diner to Empire");
            A("Idee & Game Design", "Idea & game design");
            A("du", "you");
            A("Code, Spielwelt & Oberfläche", "Code, game world & UI");
            A("Engine", "Engine");
            A("Noch keine Asset-Liste", "No asset list yet");
            A("credits.json konnte nicht gelesen werden ({0}).", "credits.json could not be read ({0}).");
            A("ungültiges JSON", "invalid JSON");
            A("Resources/credits.json wurde nicht gefunden. Alles, was ohne importierte Assets auskommt, entsteht beim Start per Code.",
              "Resources/credits.json was not found. Everything that works without imported assets is generated in code at startup.");
            A("{0} Assets aus freien Quellen (CC0, CC-BY, SIL OFL). Danke an alle Urheberinnen und Urheber!",
              "{0} assets from free sources (CC0, CC-BY, SIL OFL). Thanks to all the creators!");
            A("von {0}", "by {0}");
            A("Dateien: {0}", "Files: {0}");
            // Pause
            A("PAUSE", "PAUSED");
            A("Pause", "Pause");
            A("{0} · {1} Uhr · Spielstand {2}", "{0} · {1} · save slot {2}");
            A("Kalles Imbiss · Schicht läuft", "Kalle's Diner · shift in progress");
            A("Zuletzt gespeichert {0}", "Last saved {0}");
            A("Spiel speichern", "Save game");
            A("Einstellungen", "Settings");
            A("Steuerung & Hilfe", "Controls & help");
            A("Feedback & Bug melden", "Feedback & bug report");
            A("Admin-Panel (Test)", "Admin panel (test)");
            A("Zum Hauptmenü", "Main menu");
            A("Zum Hauptmenü?", "Back to the main menu?");
            A("Speichern & Beenden", "Save & quit");
            A("Spiel beenden", "Quit game");
            A("Spiel beenden?", "Quit the game?");
            A("Version {0}", "Version {0}");
            A("Dein Spiel wird vorher automatisch gespeichert (Spielstand {0}).", "Your game is saved automatically first (save slot {0}).");
            A("Die Imbiss-Schicht wird nicht gespeichert – du fängst sie beim nächsten Mal neu an.",
              "The diner shift isn't saved – you'll start it over next time.");
            A("Der Tag ist vorbei – gespeichert wird beim nächsten Tagesstart.", "The day is over – the game saves at the start of the next day.");
            A("Ja, beenden", "Yes, quit");
            A("Ja, zum Hauptmenü", "Yes, to the main menu");
            A("Während der Imbiss-Schicht kann nicht gespeichert werden.", "You can't save during the diner shift.");
            // Hauptmenü
            A("HUSTLE GAMES PRÄSENTIERT", "HUSTLE GAMES PRESENTS");
            A("DROPSHIPPING", "DROPSHIPPING");
            A("SIMULATOR", "SIMULATOR");
            A("Vom Imbiss zum Imperium.", "From diner to empire.");
            A("Version {0} · Asset-Quellen unter Credits", "Version {0} · asset sources under Credits");
            A("Imbiss-Schicht · noch kein Business", "Diner shift · no business yet");
            A("{0} · Level {1} · {2}", "{0} · Level {1} · {2}");
            A("Spielstand {0}", "Save slot {0}");
            A("FORTSETZEN", "CONTINUE");
            A("Neues Spiel", "New game");
            A("Drei Wege ins Business – mit oder ohne Imbiss-Story", "Three ways into business – with or without the diner story");
            A("Spielstand laden", "Load game");
            A("Credits & Quellen", "Credits & sources");
            A("Beenden", "Quit");
            A("Bis bald – deine Spielstände bleiben erhalten.", "See you soon – your saves stay put.");
            A("Wie willst du starten?", "How do you want to start?");
            A("Mit Story starten (empfohlen)", "Start with story (recommended)");
            A("Beginne als Aushilfe in Kalles Imbiss. Inklusive Tutorial.", "Start as a temp at Kalle's Diner. Tutorial included.");
            A("Direkt in die Garage", "Straight to the garage");
            A("Ohne Imbiss-Intro, mit Tutorial.", "No diner intro, with tutorial.");
            A("Profi-Start", "Pro start");
            A("Kein Intro, kein Tutorial. Du weißt, was du tust.", "No intro, no tutorial. You know what you're doing.");
            A("Schwierigkeit", "Difficulty");
            A("Wie hart soll das Hustler-Leben werden? Gilt für diesen Spielstand.", "How hard should the hustler life be? Applies to this save.");
            A("{0} (empfohlen)", "{0} (recommended)");
            A("Spielstand wählen", "Choose a save slot");
            A("In welchem Speicherplatz soll dein neues Spiel liegen?", "Which slot should your new game live in?");
            A("Freier Platz", "Empty slot");
            A("Neues Spiel hier starten", "Start a new game here");
            A("{0} überschreiben", "Overwrite {0}");
            A("Spielstand {0} überschreiben?", "Overwrite save slot {0}?");
            A("{0} ({1}) wird beim ersten Speichern ersetzt.", "{0} ({1}) will be replaced on the first save.");
            A("Dieser Spielstand", "This save");
            A("Ja, neu beginnen", "Yes, start over");
            A("Leer", "Empty");
            A("Noch kein Spielstand in diesem Platz", "No save in this slot yet");
            A("Spielstand {0} löschen?", "Delete save slot {0}?");
            A("{0} ist danach weg. Das lässt sich nicht rückgängig machen.", "{0} will be gone. This can't be undone.");
            A("Endgültig löschen", "Delete for good");
            // Versions-Panel im Hauptmenü (UiFmt.WhatsNew)
            A("NEU IN VERSION 1.0", "NEW IN VERSION 1.0");
            A("Das steckt drin", "What's inside");
            A("Handy ({0}) & Tablet", "Phone ({0}) & tablet");
            A("Bestellzettel, Nachrichten und Status immer dabei; am Schreibtisch das Tablet mit AllesExpress, Mein Shop, TikTak, Fakebook, Gugel, Bank und Börse.",
              "Order slips, messages and status always at hand; at your desk the tablet with EverythingXpress, My Shop, TikTak, Fakebook, Gugel, bank and stock exchange.");
            A("Jedes Paket von Hand", "Every parcel by hand");
            A("Einkaufen, einräumen, kommissionieren, Karton falten, packen, etikettieren und ab zur Packstation – mit Express, Retouren und Großaufträgen auf Paletten.",
              "Buy, shelve, pick, fold the box, pack, label and off to the parcel locker – with express orders, returns and bulk contracts by the pallet.");
            A("TikToks selbst drehen", "Film your own TikToks");
            A("Ringlicht an, Format wählen und in der Ego-Sicht filmen: Licht, Hook und Bildaufbau entscheiden über den Hype.",
              "Switch on the ring light, pick a format and film in first person: light, hook and framing decide the hype.");
            A("Trends & Werbung", "Trends & ads");
            A("Produkte werden heiß und wieder kalt. Trendradar, Flyer, Influencer, Fakebook- und Gugel-Anzeigen – und für Mutige Fake-Profile.",
              "Products get hot and go cold. Trend radar, flyers, influencers, Fakebook and Gugel ads – and fake profiles for the bold.");
            A("Eine ganze Stadt", "A whole town");
            A("Mit dem Moped durch Wohn-, Einkaufs- und Finanzviertel: MediaMarkd, Fressnix mit Haustieren, Post, Bank und Börse.",
              "Ride your moped through the residential, shopping and finance districts: MegaMarked, Chewnothing with pets, post office, bank and stock exchange.");
            A("Börse, Krypto, Kredite", "Stocks, crypto, loans");
            A("Aktien, ETFs und Krypto mit Live-Kursen, Sparkonto und Kredite – nur Spielgeld, aber echte Nerven.",
              "Shares, ETFs and crypto with live prices, a savings account and loans – play money only, real nerves.");
            A("Straßenfest & Pitch Day", "Street festival & pitch day");
            A("Eigener Stand beim Straßenfest, Verkaufsstand vor der Garage und ein Pitch vor drei Juroren.",
              "Your own stall at the street festival, a sales stand outside the garage and a pitch in front of three judges.");
            A("Wachsen bis zum Börsengang", "Grow all the way to an IPO");
            A("Firmenlevel, Skills, Perks, Personal, Lagerhalle und Außenlager – 100.000 € Umsatz sind erst der Anfang.",
              "Company levels, skills, perks, staff, a warehouse and an outpost warehouse – 100,000 € in revenue is just the beginning.");
            A("40 Erfolge & 3 Schwierigkeiten", "40 achievements & 3 difficulties");
            A("Wochenziele, Ereignisse, Erfolge und Statistik; Entspannt, Normal oder Hart – mit Mamas Notkredit als Rettungsanker.",
              "Weekly goals, events, achievements and stats; Relaxed, Normal or Hard – with Mom's emergency loan as a safety net.");
            A("Deutsch & Englisch", "German & English");
            A("Sprache jederzeit umschaltbar, komplett mit Controller spielbar, Streamer-Modus und viele Optionen für Barrierefreiheit.",
              "Switch language any time, full controller support, streamer mode and plenty of accessibility options.");
            // Tipps (UiFmt.Tips, Ladebildschirm)
            A("TIPP", "TIP");
            A("Knapp unter dem Marktpreis verkaufst du viel – und behältst eine gute Marge.", "Just below market price you sell a lot – and keep a decent margin.");
            A("Mit {0} öffnest du dein Handy: Bestellungen, Nachrichten und Nachbestellen.", "Press {0} to open your phone: orders, messages and reordering.");
            A("Pakete, die weit vor der Frist rausgehen, bringen einen Sterne-Bonus. Express-Zettel zählen doppelt.", "Parcels shipped well before the deadline earn a star bonus. Express slips count double.");
            A("Retouren kommen später zurück: am Retourenplatz als B-Ware einlagern oder entsorgen.", "Returns come back later: restock them as B-stock at the returns bay or bin them.");
            A("Großaufträge ab Level 3: ganze Kisten direkt auf die Palette stellen – ohne Auspacken.", "Bulk orders from level 3: put whole crates straight onto the pallet – no unpacking.");
            A("Trendradar im Laptop: Kauf ein, bevor der Hype seinen Peak erreicht.", "Trend radar on the laptop: buy in before the hype peaks.");
            A("Jeder Level-Aufstieg bringt einen Skillpunkt – Laptop » Firma » Skills.", "Every level-up grants a skill point – Laptop » Company » Skills.");
            A("Einmal pro Woche darfst du ein Wochenziel tauschen.", "Once a week you may swap a weekly goal.");
            A("Premium-Ware gibt's nur einmal pro Produkt und Tag.", "Premium goods are available once per product per day.");
            A("Unsicher, was bremst? Handy » Status zeigt den größten Engpass.", "Not sure what's slowing you down? Phone » Status shows the biggest bottleneck.");
            A("Premium-Ware kostet mehr, sorgt aber für bessere Bewertungen.", "Premium goods cost more but earn better reviews.");
            A("Ungefaltete Kartons kosten nur die Hälfte – falte sie am Falttisch.", "Flat-pack boxes cost half – fold them at the folding table.");
            A("Um 20 Uhr ist Feierabend. Dann werden Miete, Löhne und Zinsen fällig.", "Closing time is 20:00. That's when rent, wages and interest are due.");
            A("Ein Kredit hilft über Engpässe – kostet aber 2 % Zinsen pro Tag.", "A loan bridges tight spots – but costs 2 % interest per day.");
            A("Der Verkaufsstand bringt Laufkundschaft. Ganz ohne Karton und Versand.", "The sales stall brings walk-in customers. No boxes, no shipping.");
            A("Werbung und TikToks steigern die Nachfrage für eine Weile.", "Ads and TikToks boost demand for a while.");
            A("Ist die Warteschlange voll, gehen Bestellungen verloren.", "When the queue is full, orders get lost.");
            A("Im Laptop unter Firma » Ziele stehen Meilensteine und Wochenziele.", "Milestones and weekly goals live on the laptop under Company » Goals.");
            A("Packstationen in der Stadt nehmen Pakete gratis an – die Abholung am Garagentor kostet 3 € pro Paket.", "Parcel lockers in town take parcels for free – pickup at the garage door costs 3 € per parcel.");
            A("Der Kompass oben zeigt Garage, Packstation, Post, Bank, Börse und Läden.", "The compass at the top shows the garage, parcel locker, post office, bank, stock exchange and shops.");
            A("Unter 40 Energie wirst du langsamer. Bett, Stuhl und Kaffee gibt's unter Firma » Einrichtung.", "Below 40 energy you slow down. Bed, chair and coffee are under Company » Furnishings.");
            A("Die Müllabfuhr kommt Mo, Mi und Fr um 10 Uhr. Volle Tonnen bremsen alle.", "The bin men come Mon, Wed and Fri at 10:00. Full bins slow everyone down.");
            A("Kalle hat immer einen Spruch parat. Ob er stimmt, ist eine andere Frage.", "Kalle always has a saying ready. Whether it's true is another matter.");
            // Dialog
            A("weiter  ·  Klick", "continue  ·  click");
            A("Antwort wählen", "Choose an answer");
        }

        // ---- Einstellungen (SettingsView, Settings) ------------------------------------------------
        private static void SettingsUi(System.Action<string, string> A)
        {
            A("Grafik", "Graphics");
            A("Audio", "Audio");
            A("Steuerung", "Controls");
            A("Gameplay", "Gameplay");
            A("Barrierefreiheit", "Accessibility");
            A("Diese Seite konnte nicht geladen werden.", "This page couldn't be loaded.");
            A("Wirklich zurücksetzen?", "Really reset?");
            A("Standardwerte", "Defaults");
            A("BILDSCHIRM", "DISPLAY");
            A("Fenstermodus", "Window mode");
            A("Vollbild", "Fullscreen");
            A("Rahmenlos", "Borderless");
            A("Fenster", "Windowed");
            A("Automatisch (Bildschirm)", "Automatic (display)");
            A("Auflösung", "Resolution");
            A("Fenstermodus und Auflösung wirken nur im fertigen Spiel, nicht im Unity-Editor.", "Window mode and resolution only apply in the built game, not in the Unity editor.");
            A("VSync (Bildrate an den Monitor koppeln)", "VSync (sync frame rate to the monitor)");
            A("Unbegrenzt", "Unlimited");
            A("FPS-Limit", "FPS limit");
            A("Das FPS-Limit greift nur bei ausgeschaltetem VSync.", "The FPS limit only applies with VSync off.");
            A("QUALITÄT", "QUALITY");
            A("Grafikqualität", "Graphics quality");
            A("Niedrig", "Low");
            A("Mittel", "Medium");
            A("Hoch", "High");
            A("Renderauflösung", "Render resolution");
            A("Unter 100 % läuft das Spiel flüssiger, wird aber etwas unschärfer.", "Below 100 % the game runs smoother but gets a little blurrier.");
            A("FPS anzeigen", "Show FPS");
            A("Wechselndes Wetter (Wolken, Regen)", "Changing weather (clouds, rain)");
            A("KAMERA", "CAMERA");
            A("Sichtfeld (FOV)", "Field of view (FOV)");
            A("Bewegungsunschärfe", "Motion blur");
            A("Kopfwippen beim Laufen", "Head bob while walking");
            A("Kamerawackeln (Landen, Stöße)", "Camera shake (landing, bumps)");
            A("LAUTSTÄRKE", "VOLUME");
            A("Gesamtlautstärke", "Master volume");
            A("Musik", "Music");
            A("Effekte & Stimmen", "Effects & voices");
            A("Streamer-Modus und Untertitel findest du unter Gameplay bzw. Barrierefreiheit.", "Streamer mode and subtitles are under Gameplay and Accessibility.");
            A("EMPFINDLICHKEIT", "SENSITIVITY");
            A("Maus", "Mouse");
            A("Controller", "Controller");
            A("Y-Achse invertieren", "Invert Y axis");
            A("TASTENBELEGUNG", "KEY BINDINGS");
            A("TASTATUR", "KEYBOARD");
            A("Feld anklicken (bzw. mit A wählen) und die neue Taste drücken – Esc bzw. Start bricht ab. Ist die Taste schon vergeben, tauschen die Aktionen. Laufen (WASD/Stick), Esc/Start und die Menü-Bedienung bleiben fest.",
              "Click a field (or select it with A) and press the new key – Esc or Start cancels. If the key is already taken, the actions swap. Walking (WASD/stick), Esc/Start and menu controls are fixed.");
            A("Standardbelegung", "Default bindings");
            A("Taste am Controller …", "Press a controller button …");
            A("Taste drücken …", "Press a key …");
            A("SPRACHE", "LANGUAGE");
            A("Sprache / Language", "Language / Sprache");
            A("Einzelne Texte, die gerade offen sind, wechseln die Sprache erst beim nächsten Öffnen – ganz sicher nach einer Rückkehr ins Hauptmenü.",
              "Some texts that are open right now switch language the next time they're opened – guaranteed after a trip back to the main menu.");
            A("ANZEIGE IM SPIEL", "IN-GAME DISPLAY");
            A("Kompass mit wichtigen Orten (oben)", "Compass with key places (top)");
            A("Tastenhinweise im HUD", "Key hints in the HUD");
            A("Design von Laptop & Handy", "Laptop & phone design");
            A("Hype", "Hype");
            A("Frachtbrief", "Waybill");
            A("Dunkles Design (Laptop & Handy)", "Dark theme (laptop & phone)");
            A("STREAMER-MODUS", "STREAMER MODE");
            A("Streamer-Modus", "Streamer mode");
            A("Für Streams und Videos: kein Kamerawackeln, keine blinkenden Effekte, keine Pfade im Fehlerbericht. Alle Musik im Spiel ist CC0 (gemeinfrei).",
              "For streams and videos: no camera shake, no flashing effects, no file paths in the bug report. All music in the game is CC0 (public domain).");
            A("Musik im Streamer-Modus stumm (falls Content-ID fälschlich anschlägt)", "Mute music in streamer mode (in case Content ID misfires)");
            A("OBERFLÄCHE", "INTERFACE");
            A("UI-Größe", "UI scale");
            A("Weniger Bewegung (kein Blinken/Pulsieren)", "Reduced motion (no blinking/pulsing)");
            A("TEXT & UNTERTITEL", "TEXT & SUBTITLES");
            A("Untertitel für Figuren (Kalle, Kundschaft, Passanten)", "Subtitles for characters (Kalle, customers, passers-by)");
            A("Textgeschwindigkeit", "Text speed");
            A("Langsam", "Slow");
            A("Normal", "Normal");
            A("Schnell", "Fast");
            A("Sofort", "Instant");
            A("FARBEN", "COLOURS");
            A("Farbenblind-freundlich (Blau/Orange statt Grün/Rot)", "Colour-blind friendly (blue/orange instead of green/red)");
            A("Gut: pünktlich", "Good: on time");
            A("Schlecht: zu spät", "Bad: too late");
            A("Deutsch", "Deutsch");
            A("English", "English");
        }

        // ---- Tasten (GameInput, InputBindings) und Hilfe-Tabelle ------------------------------------
        private static void Input(System.Action<string, string> A)
        {
            A("Leertaste", "Space");
            A("Strg", "Ctrl");
            A("Rücktaste", "Backspace");
            A("Feststell", "Caps Lock");
            A("Num {0}", "Num {0}");
            A("Strg+{0}", "Ctrl+{0}");
            A("Mausrad", "Mouse wheel");
            A("Linksklick", "Left click");
            A("R / Mausrad", "R / mouse wheel");
            A("Linksklick / {0}", "Left click / {0}");
            A("Enter / Linksklick", "Enter / left click");
            A("Linksklick / Enter", "Left click / Enter");
            A("Steuerkreuz ↑", "D-pad ↑");
            A("Steuerkreuz ↓", "D-pad ↓");
            A("Steuerkreuz ←", "D-pad ←");
            A("Steuerkreuz →", "D-pad →");
            A("Linker Stick", "Left stick");
            A("Rechter Stick", "Right stick");
            A("RT / LT / Linker Stick", "RT / LT / left stick");
            A("{0} · Maus / 1–6", "{0} · mouse / 1–6");
            A("{0} · Rechter Stick", "{0} · right stick");
            A("Linksklick / {0} · Esc", "Left click / {0} · Esc");
            A("Enter / Linksklick · Esc", "Enter / left click · Esc");
            // Aktionsnamen (Hilfe + Tastenbelegung)
            A("Laufen", "Walk");
            A("Umsehen", "Look around");
            A("Sprinten", "Sprint");
            A("Ducken", "Crouch");
            A("Springen", "Jump");
            A("Benutzen / Aufheben", "Use / pick up");
            A("Möbel verschieben (anvisieren)", "Move furniture (aim at it)");
            A("Möbel verschieben", "Move furniture");
            A("Möbel drehen", "Rotate furniture");
            A("Möbel abstellen / abbrechen", "Place furniture / cancel");
            A("TikTok aufnehmen (Handy › TikTak, Laptop, Ringlicht am Schreibtisch)", "Record a TikTok (Phone › TikTak, laptop, ring light at the desk)");
            A("Aufnahme: Aktion / Hook", "Recording: action / hook");
            A("Aufnahme: Stopp / abbrechen", "Recording: stop / cancel");
            A("Moped: auf-/absteigen", "Moped: get on/off");
            A("Moped: Gas / Bremse / Lenken", "Moped: throttle / brake / steer");
            A("Moped: Hupe · Ansicht", "Moped: horn · camera");
            A("Moped: Heckbox beladen (Pakete in der Hand) / entladen", "Moped: load top box (parcels in hand) / unload");
            A("Emote-Rad (halten, zielen, loslassen)", "Emote wheel (hold, aim, release)");
            A("Handy öffnen / schließen", "Open / close phone");
            A("Handy-App wechseln", "Switch phone app");
            A("Laptop (an der Werkbank / am Schreibtisch)", "Laptop (at the workbench / desk)");
            A("Laptop-App wechseln", "Switch laptop app");
            A("Auswählen", "Select");
            A("Zurück / Schließen", "Back / close");
            A("Scrollen", "Scroll");
            A("Pause-Menü", "Pause menu");
            A("Screenshot", "Screenshot");
            A("Noclip / Fliegen (Test): Leertaste hoch, Strg runter", "Noclip / fly (test): Space up, Ctrl down");
            A("Sprinten (halten)", "Sprint (hold)");
            A("Ducken (halten)", "Crouch (hold)");
            A("Emote-Rad (halten)", "Emote wheel (hold)");
            A("Moped: Hupe", "Moped: horn");
            A("Moped: Ansicht wechseln", "Moped: switch camera");
        }

        // ---- Tagesabrechnung (Kassenbon), Ereignis-Fenster ------------------------------------------
        private static void DaySummary(System.Action<string, string> A)
        {
            A("Feierabend – Tag {0}", "Closing time – Day {0}");
            A("Zeit für die Abrechnung.", "Time to settle up.");
            A("Nächster Tag", "Next day");
            A("MeinShop", "MyShop");
            A("KASSENBON · TAG {0}", "RECEIPT · DAY {0}");
            A("{0} · 20:00 Uhr", "{0} · 20:00");
            A("Pakete verschickt", "Parcels shipped");
            A("  davon Express", "  of which express");
            A("  davon verspätet", "  of which late");
            A("Bestellungen verloren", "Orders lost");
            A("  davon storniert", "  of which cancelled");
            A("  B-Ware / entsorgt", "  B-stock / binned");
            A("Großaufträge erfüllt", "Bulk orders fulfilled");
            A("Großaufträge verpasst", "Bulk orders missed");
            A("Wochenziele geschafft", "Weekly goals done");
            A("Erfahrung", "Experience");
            A("+{0} XP", "+{0} XP");
            A("Umsatz", "Revenue");
            A("  davon Verkaufsstand", "  of which sales stall");
            A("  davon Großaufträge", "  of which bulk orders");
            A("Sonstige Einnahmen", "Other income");
            A("  davon Wochenziele", "  of which weekly goals");
            A("Wareneinkauf", "Goods purchased");
            A("Verpackung", "Packaging");
            A("Porto (Versand)", "Postage (shipping)");
            A("Marketing", "Marketing");
            A("Erstattungen (Retouren)", "Refunds (returns)");
            A("Vertragsstrafen", "Contract penalties");
            A("Miete", "Rent");
            A("Löhne", "Wages");
            A("Strom Förderband", "Conveyor power");
            A("Kreditzinsen", "Loan interest");
            A("TAGESERGEBNIS", "DAILY RESULT");
            A("Kontostand danach", "Balance afterwards");
            A("Offener Kredit", "Outstanding loan");
            A("Trading (Käufe/Verkäufe)", "Trading (buys/sells)");
            A("Bewertung", "Rating");
            A("PLEITE", "BROKE");
            A("GEWINN", "PROFIT");
            A("VERLUST", "LOSS");
            A("Wochenbilanz", "Weekly balance");
            A("{0} von {1} Zielen", "{0} of {1} goals");
            A("· {0}", "· {0}");
            A("Offen", "Pending");
            A("{0} Retouren unterwegs", "{0} returns in transit");
            A("{0} Aufträge laufen", "{0} bulk orders running");
            A("{0} Retouren unterwegs · {1} Aufträge laufen", "{0} returns in transit · {1} bulk orders running");
            A("Danke für deinen Einkauf bei {0}! Bis morgen.", "Thanks for shopping at {0}! See you tomorrow.");
            A("Dein Konto fällt unter {0} – das ist die Insolvenz!", "Your account drops below {0} – that's bankruptcy!");
            A("Du bist im Minus. Ab {0} ist Schluss – verkauf mehr, spar Kosten oder nimm einen Kredit.", "You're in the red. At {0} it's game over – sell more, cut costs or take a loan.");
            // Ereignis
            A("Von {0} · {1} Uhr", "From {0} · {1}");
            A("Später beantworten", "Answer later");
            A("Minispiel", "Minigame");
            A("Dafür fehlt dir das Geld.", "You can't afford that.");
            A("Du kannst auch später im Handy ({0}) antworten. Um 20 Uhr gilt automatisch die vorsichtigste Antwort.",
              "You can also answer later on your phone ({0}). At 20:00 the most cautious answer applies automatically.");
            A("Ergebnis", "Result");
            A("OK", "OK");
        }

        // ---- TikTok-Studio, Sucher -------------------------------------------------------------------
        private static void TikTok(System.Action<string, string> A)
        {
            A("Produkt", "Product");
            A("{0} im Lager", "{0} in stock");
            A("{0} · Hype ×{1}", "{0} · hype ×{1}");
            A("{0} · TREND", "{0} · TREND");
            A("Gerade nicht möglich.", "Not possible right now.");
            A("Nichts zum Filmen: erst Ware einlagern oder ein Produkt in die Hand nehmen.", "Nothing to film: stock some goods or pick up a product first.");
            A("Aufnahme konnte nicht starten. Such dir etwas mehr Platz.", "Recording couldn't start. Find a bit more space.");
            A("Posten geht gerade nicht: {0}", "Can't post right now: {0}");
            A("später nochmal versuchen.", "try again later.");
            A("Video ist online!", "Video is live!");
            A("{0} · ≈ {1} Aufrufe", "{0} · ≈ {1} views");
            A("TIKTAK", "TIKTAK");
            A("Aufnahme fertig: {0} %. Posten über Handy › TikTak.", "Recording done: {0} %. Post via Phone › TikTak.");
            A("Posten", "Post");
            A("Nochmal aufnehmen", "Record again");
            A("Dein TikTok ist im Kasten", "Your TikTok is in the can");
            A("Wertung: {0} %", "Score: {0} %");
            A("Prognose: ≈ {0} Aufrufe", "Forecast: ≈ {0} views");
            A("Qualität {0}", "Quality {0}");
            A("Trend-Format +{0}", "Trend format +{0}");
            A("Hype +{0}", "Hype +{0}");
            A("Aufschlüsselung", "Breakdown");
            A("wichtig", "important");
            A("Nebensache", "minor");
            A("Nächstes Mal besser", "Do better next time");
            A("• {0}", "• {0}");
            A("Posten bringt ×{0} Nachfrage für {1}. Heute noch {2} von {3} Videos.", "Posting brings ×{0} demand for {1}. {2} of {3} videos left today.");
            A("Posten gerade nicht möglich: {0} Die Aufnahme bleibt bis Feierabend als Entwurf (Handy › TikTak).",
              "Can't post right now: {0} The recording stays as a draft until closing time (Phone › TikTak).");
            A("„Später“ hebt die Aufnahme bis Feierabend als Entwurf auf (Handy › TikTak).", "“Later” keeps the recording as a draft until closing time (Phone › TikTak).");
            A("TikTok aufnehmen", "Record a TikTok");
            A("Creator-Ecke · Produkt und Format wählen", "Creator corner · pick product and format");
            A("Aufnahme starten", "Start recording");
            A("PRODUKT", "PRODUCT");
            A("FORMAT", "FORMAT");
            A("So geht's: Das Produkt steht vor dir, du filmst in Ego-Sicht 10–20 Sekunden. {0} = Aktion (gleich am Anfang = Hook), {1} = Stopp, {2} = Abbrechen. Bewertet werden Bildmitte, Abstand, ruhige Kamera, Licht, Blickwinkel, Länge und Deko im Bild.",
              "How it works: the product stands in front of you and you film 10–20 seconds in first person. {0} = action (right at the start = hook), {1} = stop, {2} = cancel. Scored: framing, distance, steady camera, light, angle, length and decor in the shot.");
            A("Heute noch {0} von {1} · Trend-Format: {2}", "{0} of {1} left today · trend format: {2}");
            A("ENTWURF", "DRAFT");
            A("{0} % · {1} · ≈ {2} Aufrufe", "{0} % · {1} · ≈ {2} views");
            A("Verwerfen", "Discard");
            A("Gerade keine Aufnahme", "No recording right now");
            A("DEINE VIDEOS", "YOUR VIDEOS");
            A("Tag {0} · {1} % · {2} Aufrufe", "Day {0} · {1} % · {2} views");
            // Sucher
            A("REC", "REC");
            A("BEREIT", "READY");
            A("max {0} s", "max {0} s");
            A("TREND-FORMAT +{0} %", "TREND FORMAT +{0} %");
            A("Aktion", "Action");
            A("Stopp", "Stop");
            A("Stopp ab {0} s", "Stop from {0} s");
        }

        // ---- Kasse (MediaMarkd/Fressnix), Pitch, Bug-Report-Ansicht --------------------------------
        private static void Shops(System.Action<string, string> A)
        {
            A("„Sammeln Sie Punkte? Nein? Wir auch nicht.“", "“Collecting points? No? Us neither.”");
            A("„Brauchen Sie eine Tüte? Kostet 20 Cent. Und Ihre Würde.“", "“Need a bag? That's 20 cents. And your dignity.”");
            A("„Garantieverlängerung? Wir verlängern alles. Außer Öffnungszeiten.“", "“Extended warranty? We extend everything. Except opening hours.”");
            A("„Bar oder mit Karte? Oder mit Influencer-Rabattcode?“", "“Cash or card? Or an influencer discount code?”");
            A("Bezahlen · {0}", "Pay · {0}");
            A("{0} · Konto: {1}", "{0} · account: {1}");
            A("Kasse", "Checkout");
            A("Dein Wagen ist leer. Schieb ihn zu den Regalen!", "Your trolley is empty. Push it over to the shelves!");
            A("Dein Korb ist leer.", "Your basket is empty.");
            A("{0}× {1}", "{0}× {1}");
            A("Summe", "Total");
            A("Gespart (Aktion)", "Saved (special offer)");
            A("Ware fürs Lager bringt der Lieferservice als Kiste an deinen Wareneingang.", "Goods for your storage arrive by delivery as a crate at your goods-in.");
            A("Achtung: Dir fehlen {0}.", "Heads up: you're {0} short.");
            // Pitch
            A("Pitch Day bei MegaMarkt", "Pitch Day at MegaMarkt");
            A("Drei Juroren. Drei Fragen. Eine Chance.", "Three judges. Three questions. One shot.");
            A("Du stehst auf der Bühne, das Mikro knackt. Wähle Antworten, die zu deinen echten Zahlen passen – die Jury durchschaut leere Versprechen sofort.",
              "You're on stage, the mic crackles. Pick answers that match your real numbers – the jury sees through empty promises instantly.");
            A("DEINE STÄRKEN", "YOUR STRENGTHS");
            A("Verschickte Pakete", "Parcels shipped");
            A("Marke & Bekanntheit", "Brand & awareness");
            A("Lager & Team", "Storage & team");
            A("Preise", "Prices");
            A("Frage {0} von {1}", "Question {0} of {1}");
            A("„{0}“", "“{0}”");
            A("DEINE ANTWORT", "YOUR ANSWER");
            A("stützt sich auf: {0}", "based on: {0}");
            A("Die Jury reagiert", "The jury reacts");
            A("Zum Ergebnis", "See the result");
            A("Nächste Frage", "Next question");
            A("Eindruck", "Impression");
            A("Pitch gehalten", "Pitch delivered");
            A("Gesamteindruck: {0} %", "Overall impression: {0} %");
            A("Auftragswert", "Order value");
            // Bug-Report
            A("INFOS KOPIERT", "INFO COPIED");
            A("Version und Systeminfos liegen jetzt in deiner Zwischenablage, der Ordner mit der Logdatei ist geöffnet.",
              "Version and system info are now on your clipboard, and the folder with the log file is open.");
            A("Version und Systeminfos liegen jetzt in deiner Zwischenablage. (Streamer-Modus: Pfade werden nicht angezeigt.)",
              "Version and system info are now on your clipboard. (Streamer mode: paths are hidden.)");
            A("Version und Systeminfos liegen jetzt in deiner Zwischenablage. Den Log-Ordner findest du hier:",
              "Version and system info are now on your clipboard. You'll find the log folder here:");
            A("Beschreibe kurz, <b>was du gemacht hast</b> und <b>was passiert ist</b> (bei Grafikfehlern gern mit Screenshot – F12).",
              "Briefly describe <b>what you did</b> and <b>what happened</b> (for graphics glitches, a screenshot helps – F12).");
            A("Füge die kopierten Infos ein (Strg + V).", "Paste the copied info (Ctrl + V).");
            A("Hänge die Datei <b>Player.log</b> aus dem geöffneten Ordner an (bei Abstürzen auch <b>Player-prev.log</b>).",
              "Attach the file <b>Player.log</b> from the opened folder (for crashes, also <b>Player-prev.log</b>).");
            A("Schick alles an: <b>{0}</b>", "Send everything to: <b>{0}</b>");
            A("Steam › Community-Hub des Spiels › Diskussionen › „Bugs & Feedback“", "Steam › the game's Community Hub › Discussions › “Bugs & Feedback”");
            A("INHALT DER ZWISCHENABLAGE", "CLIPBOARD CONTENTS");
            A("Erneut kopieren", "Copy again");
            A("Infos in die Zwischenablage kopiert.", "Info copied to the clipboard.");
            A("Log-Ordner öffnen", "Open log folder");
            A("Datenschutz: Das Spiel sendet keine Daten. Du entscheidest selbst, was du teilst.", "Privacy: the game sends no data. You decide what you share.");
            // Zwischenablage-Vorschau (zeilenweise; der kopierte Bericht selbst bleibt deutsch für den Entwickler)
            A("== {0} – Fehlerbericht ==", "== {0} – bug report ==");
            A("Version: {0}", "Version: {0}");
            A("Zeit: {0}", "Time: {0}");
            A("Plattform: {0}", "Platform: {0}");
            A("Bildschirm: {0}", "Screen: {0}");
            A("Qualität: {0}", "Quality: {0}");
            A("Spielstand: Slot {0} · Tag {1} · Phase {2}", "Save: slot {0} · day {1} · phase {2}");
            A("Logdatei: {0}", "Log file: {0}");
            A("Sitzungsprotokoll: {0} (Ordner logs, letzte 5 Sitzungen)", "Session log: {0} (folder logs, last 5 sessions)");
            A("Letzte Fehler:", "Recent errors:");
            A("Letzte Fehler: keine", "Recent errors: none");
            A("Systeminfo unvollständig: {0}", "System info incomplete: {0}");
            A("Was ist passiert? (bitte kurz beschreiben, was du gemacht hast und was du erwartet hast)", "What happened? (please briefly describe what you did and what you expected)");
            A("(unbekannt)", "(unknown)");
            A("(nicht verfügbar)", "(not available)");
        }

        // ---- Bank, Börse, Wallet (FinanceUI) ---------------------------------------------------------
        private static void Finance(System.Action<string, string> A)
        {
            A("Wallet", "Wallet");
            A("Geldautomat", "ATM");
            A("Kiezbank", "Block Bank");
            A("Tschüss", "Bye");
            A("GIROKONTO", "CHECKING");
            A("SPARKONTO", "SAVINGS");
            A("DEPOT", "PORTFOLIO");
            A("KREDIT", "LOAN");
            A("0 €", "0 €");
            A("Vermögen (alles minus Schulden)", "Net worth (everything minus debt)");
            A("Schufi-Score", "Schufi score");
            A("{0} / 1000 · {1}", "{0} / 1000 · {1}");
            A("Konten", "Accounts");
            A("Kredit", "Loan");
            A("Sparen", "Savings");
            A("Dividende heute Nacht (geschätzt)", "Dividend tonight (estimated)");
            A("Sparzinsen heute Nacht", "Savings interest tonight");
            A("Kreditzinsen heute Abend", "Loan interest tonight");
            A("Dividenden bisher", "Dividends so far");
            A("Sparzinsen bisher", "Savings interest so far");
            A("Depot: Aktien / ETFs / Krypto", "Portfolio: stocks / ETFs / crypto");
            A("Kontoführung: 0 €. Kontoatmung: auch 0 €. Noch.", "Account fees: 0 €. Breathing fees: also 0 €. For now.");
            A("Kreditrahmen (Level {0})", "Credit limit (level {0})");
            A("Noch verfügbar", "Still available");
            A("Zinsen", "Interest");
            A("{0} % pro Tag", "{0} % per day");
            A("Ab Lv {0}: {1}", "From Lv {0}: {1}");
            A("Tilgen {0}", "Repay {0}");
            A("Alles tilgen", "Repay all");
            A("Kredit + Aktien kaufen? Die Zinsen fressen jede Rendite. Fragen Sie Gary.", "Loan + buying stocks? The interest eats every gain. Ask Gary.");
            A("Sparzins", "Savings rate");
            A("{0} % pro Tag (bis {1})", "{0} % per day (up to {1})");
            A("Keine Zinsen – erst Kredit tilgen!", "No interest – repay your loan first!");
            A("Einzahlen {0}", "Deposit {0}");
            A("Abheben {0}", "Withdraw {0}");
            A("Auszahlen {0}", "Withdraw {0}");
            A("Alles auszahlen", "Withdraw all");
            A("Börsen-Terminal", "Trading terminal");
            A("Kurse live. Keine Anlageberatung. Eher Anlageunterhaltung.", "Live prices. Not investment advice. More like investment entertainment.");
            A("ETFs (ruhig)", "ETFs (calm)");
            A("Aktien", "Stocks");
            A("Krypto (wild)", "Crypto (wild)");
            A("{0}  {1}", "{0}  {1}");
            A("{0} ({1}) · {2} · {3}", "{0} ({1}) · {2} · {3}");
            A("{0} Gebühr: {1} % je Kauf/Verkauf.", "{0} Fee: {1} % per buy/sell.");
            A("{0} Dividende: {1} % pro Tag. Gebühr: {2} % je Kauf/Verkauf.", "{0} Dividend: {1} % per day. Fee: {2} % per buy/sell.");
            A("{0} Gebühr: {1} % je Kauf/Verkauf. Enthält: {2}.", "{0} Fee: {1} % per buy/sell. Contains: {2}.");
            A("{0} Dividende: {1} % pro Tag. Gebühr: {2} % je Kauf/Verkauf. Enthält: {3}.", "{0} Dividend: {1} % per day. Fee: {2} % per buy/sell. Contains: {3}.");
            A("Kaufen {0}", "Buy {0}");
            A("Alles verkaufen", "Sell all");
            A("Verkaufen {0} %", "Sell {0} %");
            A("Verkaufen", "Sell");
            A("Verkauft für {0}.", "Sold for {0}.");
            A("Noch keine Schlagzeilen. Die Makler langweilen sich.", "No headlines yet. The brokers are bored.");
            A("{0}  {1} ({2})", "{0}  {1} ({2})");
            A("Bestand: {0} Anteile · {1} · G/V {2}", "Holding: {0} shares · {1} · P/L {2}");
            A("Konto {0} · Depot {1} · Hektik {2} %", "Account {0} · portfolio {1} · frenzy {2} %");
            A("Konto {0} · Sparkonto {1}", "Account {0} · savings {1}");
            A("KRYPTO", "CRYPTO");
            A("ETFs", "ETFs");
            A("AKTIEN (Bestand)", "STOCKS (holdings)");
            A("kein Bestand", "no holding");
            A("+50 €", "+50 €");
            A("News: {0}", "News: {0}");
            A("Mehr Auswahl im Börsen-Terminal (Finanzviertel).", "More choice at the trading terminal (financial district).");
            A("G/V gesamt {0}", "Total P/L {0}");
            // Admin (Finanzviertel)
            A("FINANZVIERTEL", "FINANCIAL DISTRICT");
            A("Depot {0} € · Sparkonto {1} € · Hektik {2} % · Nachrichten {3}", "Portfolio {0} € · savings {1} € · frenzy {2} % · news {3}");
            A("Gute Aktien-News", "Good stock news");
            A("Schlechte Aktien-News", "Bad stock news");
            A("Krypto-Sommer", "Crypto summer");
            A("Krypto-Winter", "Crypto winter");
            A("Börsencrash −20 %", "Market crash −20 %");
            A("Krypto ×2", "Crypto ×2");
            A("Makler-Panik (Hektik 100 %)", "Broker panic (frenzy 100 %)");
            A("Nacht simulieren (Dividende/Zinsen)", "Simulate night (dividend/interest)");
            A("Bank öffnen", "Open bank");
            A("Börsen-Terminal öffnen", "Open trading terminal");
            A("Teleport Finanzviertel", "Teleport financial district");
        }

        // ---- Engpass-Assistent (Advisor) --------------------------------------------------------------
        private static void Advisor(System.Action<string, string> A)
        {
            A("Tutorial {0}/{1}: {2}", "Tutorial {0}/{1}: {2}");
            A("Dein Shop ist leer – stell ein Produkt online.", "Your shop is empty – list a product.");
            A("Zum Shop", "To the shop");
            A("{0} ist ausverkauft – nachbestellen.", "{0} is sold out – reorder.");
            A("Keine {0}-Kartons – am Falttisch falten ({1} ungefaltet).", "No {0} boxes – fold some at the folding table ({1} flat).");
            A("Keine {0}-Kartons mehr – kaufen.", "Out of {0} boxes – buy more.");
            A("Kartons kaufen", "Buy boxes");
            A("Großauftrag {0}: Frist heute 20 Uhr, noch {1} Stück.", "Bulk order {0}: due today 20:00, {1} units to go.");
            A("Aufträge", "Bulk orders");
            A("Heute Abend droht die Pleite – dir fehlen {0}.", "Bankruptcy looms tonight – you're {0} short.");
            A("Miete & Löhne heute Abend: {0} – du hast {1}.", "Rent & wages tonight: {0} – you have {1}.");
            A("Warteschlange fast voll ({0}/{1}) – schneller packen oder Preis leicht erhöhen.", "Queue almost full ({0}/{1}) – pack faster or nudge the price up.");
            A("1 Bestellung heute verloren.", "1 order lost today.");
            A("{0} Bestellungen heute verloren.", "{0} orders lost today.");
            A("{0} Retoure wartet am Wareneingang – zum Retourenplatz.", "{0} return is waiting at goods-in – take it to the returns bay.");
            A("{0} Retouren warten am Wareneingang – zum Retourenplatz.", "{0} returns are waiting at goods-in – take them to the returns bay.");
            A("{0} Kiste wartet am Wareneingang.", "{0} crate is waiting at goods-in.");
            A("{0} Kisten warten am Wareneingang.", "{0} crates are waiting at goods-in.");
            A("{0}: Preis {1} % über Markt – kaum Bestellungen.", "{0}: price {1} % above market – barely any orders.");
            A("Preise", "Prices");
            A("Deine Bewertung ({0}) schreckt Kundschaft ab – pünktlich liefern, bessere Ware.", "Your rating ({0}) scares customers off – ship on time, better goods.");
            A("Lager fast voll – weniger nachbestellen oder ausbauen.", "Storage almost full – reorder less or expand.");
            A("Eine Entscheidung wartet auf dem Handy.", "A decision is waiting on your phone.");
            A("{0} Entscheidungen warten auf dem Handy.", "{0} decisions are waiting on your phone.");
            A("Ein Großauftrag wartet auf deine Antwort.", "A bulk order is waiting for your answer.");
            A("{0} Großaufträge warten auf deine Antwort.", "{0} bulk orders are waiting for your answer.");
            A("1 Skillpunkt frei – Firma › Skills.", "1 skill point free – Company › Skills.");
            A("{0} Skillpunkte frei – Firma › Skills.", "{0} skill points free – Company › Skills.");
            A("{0} Pakete liegen am Packtisch – Label drucken und ab zur Packstation.", "{0} parcels are at the packing table – print labels and off to the parcel locker.");
            A("Zeit für ein TikTok? (heute noch {0})", "Time for a TikTok? ({0} left today)");
            A("Fast geschafft: {0} ({1}).", "Almost there: {0} ({1}).");
            A("Fast geschafft: „{0}“ ({1}).", "Almost there: “{0}” ({1}).");
        }

        // ---- GameRoot: Banner, Meldungen, Imbiss-Intro, Feierabend, Pleite, Ende; Demo -------------------
        private static void Root(System.Action<string, string> A)
        {
            A("{0} neue Wochenziele · Handy › Woche", "{0} new weekly goals · Phone › Week");
            A("Großauftrag erfüllt", "Bulk order fulfilled");
            A("{0} · +{1}", "{0} · +{1}");
            A("B2B", "B2B");
            A("Großauftrag verpasst", "Bulk order missed");
            A("{0} · Strafe {1}", "{0} · penalty {1}");
            A("Skill gelernt", "Skill learned");
            A("HUSTLE-SKILL", "HUSTLE SKILL");
            A("Wochenziel geschafft", "Weekly goal done");
            A("Ziel erreicht", "Goal reached");
            A("MEILENSTEIN", "MILESTONE");
            A("Aufbau bis {0}: Festkisten packen & zum Marktstand bringen", "Setup until {0}: pack festival crates & bring them to the market stall");
            A("{0} läuft!", "{0} is on!");
            A("Die Besucher kommen · bis {0}", "Visitors incoming · until {0}");
            A("{0} vorbei", "{0} is over");
            A("{0} verkauft · {1} · {2} Besucher", "{0} sold · {1} · {2} visitors");
            A("BILANZ", "SUMMARY");
            A("Neuer Tag, neue Pakete.", "New day, new parcels.");
            A("Kaffee an, Laptop auf.", "Coffee on, laptop open.");
            A("Die Kunden warten schon.", "The customers are already waiting.");
            A("Heute wird ein guter Tag.", "Today's going to be a good day.");
            A("Dein Imperium wächst.", "Your empire is growing.");
            A("Zettel checken: {0} fürs Handy.", "Check your slips: {0} for the phone.");
            A("Neue Woche, neues Glück.", "New week, new luck.");
            A("Das Spiel konnte nicht gestartet werden. Details im Protokoll (logs).", "The game couldn't start. Details in the log (logs).");
            A("Spielstand {0} konnte nicht geladen werden.", "Save slot {0} couldn't be loaded.");
            A("Der Spielstand war beschädigt – die letzte Sicherung wurde geladen.", "The save was corrupted – the last backup was loaded.");
            A("Kalles Imbiss, 17:30 Uhr. Deine Schicht beginnt.", "Kalle's Diner, 17:30. Your shift starts.");
            A("Willkommen zurück bei {0}. {1}.", "Welcome back to {0}. {1}.");
            A("Deine Garage. Dein Business.", "Your garage. Your business.");
            A("Tag 1", "Day 1");
            A("Tipp: {0} öffnet dein Handy – Bestellungen, Nachrichten, Nachbestellen.", "Tip: {0} opens your phone – orders, messages, reordering.");
            A("Screenshot gespeichert.", "Screenshot saved.");
            // Lagerhalle-Mail
            A("Immobilien Schulze", "Schulze Real Estate");
            A("Willkommen in der Lagerhalle!", "Welcome to the warehouse!");
            A("Die Schlüssel liegen unter der Fußmatte, das Rolltor ist offen. Viel Erfolg mit dem Laden! PS: Das Förderband gibt's gegen Aufpreis.",
              "The keys are under the doormat, the roller door is open. Good luck with the business! PS: the conveyor belt costs extra.");
            // Imbiss-Intro (Kalle)
            A("Da bist du ja endlich! Fünf Minuten zu spät. Wie immer.", "There you are! Five minutes late. As always.");
            A("Tisch 2, 3 und 4 warten auf ihr Essen. Die Teller stehen da vorne an der Durchreiche.", "Tables 2, 3 and 4 are waiting for their food. The plates are up front at the serving hatch.");
            A("Zack zack! Und Handy weg, sonst zieh ich's dir vom Lohn ab.", "Chop chop! And put that phone away or it comes out of your wages.");
            A("Was stehst du hier rum?! Die Teller werden kalt!", "Why are you standing around?! The plates are getting cold!");
            A("Na endlich. Hat ja ewig gedauert.", "Finally. Took you forever.");
            A("Und glaub nicht, ich hätte nicht gesehen, wie du vorhin wieder auf dein Handy gestarrt hast.", "And don't think I didn't see you staring at your phone again earlier.");
            A("(Dieses Video ... „Mit 19 Millionär – dank Dropshipping“. Und die Garage gegenüber steht leer ...)",
              "(That video ... “Millionaire at 19 – thanks to dropshipping”. And the garage across the street is empty ...)");
            A("Ich kündige.", "I quit.");
            A("Ich mach mein eigenes Ding. Online-Shop.", "I'm doing my own thing. Online shop.");
            A("Hunderttausend Umsatz?! Kleiner ... hast du vielleicht 'nen Job für mich?", "A hundred grand in revenue?! Kid ... got a job for me, maybe?");
            A("Ich hab gehört, du hast jetzt 'ne ganze Lagerhalle. Respekt. Sag's aber keinem, dass ich das gesagt hab.",
              "Heard you've got a whole warehouse now. Respect. But don't tell anyone I said that.");
            A("Online-Shop? DU? Hahaha! ... Na gut. Viel Glück mit deinem Internet-Kram.", "Online shop? YOU? Hahaha! ... Fine. Good luck with your internet stuff.");
            A("Du ... WAS? Nach allem, was ich für dich getan hab? Pff. Na dann geh doch!", "You ... WHAT? After everything I've done for you? Pff. Fine, go then!");
            A("Hier. 150 Euro, dein letzter Lohn. Mach was draus – und komm nicht angekrochen.", "Here. 150 euros, your last wage. Make something of it – and don't come crawling back.");
            A("Am nächsten Morgen ...", "The next morning ...");
            A("Tag 1. Deine Garage. Dein Business. Los geht's!", "Day 1. Your garage. Your business. Let's go!");
            // Feierabend / Pleite / Ende
            A("Feierabend machen?", "Call it a day?");
            A("Es ist {0} Uhr.", "It's {0}.");
            A("Ja, Tag beenden", "Yes, end the day");
            A("Weiterarbeiten", "Keep working");
            A("Offene Bestellungen bleiben bis morgen liegen. Miete, Löhne und Zinsen ({0}) werden abgerechnet.",
              "Open orders wait until tomorrow. Rent, wages and interest ({0}) get charged.");
            A("Gewinn heute: {0}", "Profit today: {0}");
            A("Kontostand nach Fixkosten: {0}", "Balance after fixed costs: {0}");
            A("Pleite!", "Bankrupt!");
            A("Dein Konto ist tief im Minus. Die Bank zieht den Stecker.", "Your account is deep in the red. The bank pulls the plug.");
            A("Letzten Spielstand laden", "Load last save");
            A("Hauptmenü", "Main menu");
            A("Du hast {0} Tage durchgehalten, {1} Pakete verschickt und {2} umgesetzt. Nicht schlecht – aber das Geld ist weg.",
              "You lasted {0} days, shipped {1} parcels and turned over {2}. Not bad – but the money's gone.");
            A("Hinweis: Für diesen Spielstand gibt es noch keine Speicherung.", "Note: this game hasn't been saved yet.");
            A("Du hast es geschafft!", "You made it!");
            A("100.000 € Umsatz. Vom Imbiss zum Imperium.", "100,000 € in revenue. From diner to empire.");
            A("Weiterspielen", "Keep playing");
            A("Credits", "Credits");
            A("Vor {0} Tagen hast du noch Teller bei Kalle getragen. Heute gehört dir eine Marke mit {1} Sternen, {2} verschickten Paketen und einem Team, das für dich arbeitet.",
              "{0} days ago you were still carrying plates for Kalle. Today you own a brand with {1} stars, {2} shipped parcels and a team that works for you.");
            A("Das Spiel ist hier nicht zu Ende: Im Endlos-Modus warten Viertelmillion, Außenlager, Börsengang und immer größere Ziele – dazu zweite Kräfte für dein Team ab Level 10 und {0} von {1} Erfolgen (Firma › Erfolge).",
              "The game doesn't end here: endless mode has a quarter million, an outside warehouse, an IPO and ever bigger goals – plus second hires for your team from level 10 and {0} of {1} achievements (Company › Achievements).");
            A("Endlos-Modus", "Endless mode");
            A("Weiter hustlen", "Keep hustling");
            A("Nächstes Ziel: {0} € Umsatz. Und dann das Doppelte. Und dann …", "Next goal: {0} € revenue. Then double that. And then …");
            A("Tastatur, Maus und Controller – und wie dein Business läuft.", "Keyboard, mouse and controller – and how your business works.");
            // Demo
            A("Danke fürs Spielen der Demo!", "Thanks for playing the demo!");
            A("{0} Tage Hustle – und das war erst der Anfang.", "{0} days of hustle – and that was just the beginning.");
            A("Auf die Wunschliste!", "Add to wishlist!");
            A("Deine Bilanz: {0} Pakete verschickt, {1} umgesetzt, {2} Sterne.", "Your stats: {0} parcels shipped, {1} turnover, {2} stars.");
            A("Im fertigen Spiel geht es weiter: Lagerhalle und Personal, Großaufträge, TikTok-Hypes, Börse und Krypto, Straßenfest, Shitstorms – bis zum Imperium mit 100.000 € Umsatz und darüber hinaus.",
              "The full game keeps going: warehouse and staff, bulk orders, TikTok hypes, stocks and crypto, street festival, shitstorms – all the way to an empire with 100,000 € in revenue and beyond.");
            A("Setz das Spiel auf deine Steam-Wunschliste – dann sagt Steam dir Bescheid, sobald es erscheint.", "Put the game on your Steam wishlist – Steam will let you know as soon as it's out.");
            // Übersetzungs-Werkzeug
            A("{0} fehlende Übersetzungen gespeichert: {1}", "{0} missing translations saved: {1}");
            A("Speichern fehlgeschlagen: {0}", "Saving failed: {0}");
        }

        // ---- Admin-Panel (nur Entwickler, F10) ------------------------------------------------------------
        private static void Admin(System.Action<string, string> A)
        {
            A("Allgemein", "General");
            A("Ware", "Goods");
            A("Ereignisse", "Events");
            A("Trends", "Trends");
            A("Welt & Zeit", "World & time");
            A("Wirtschaft+", "Economy+");
            A("ADMIN-PANEL", "ADMIN PANEL");
            A("F10 / Esc schließen", "F10 / Esc to close");
            A("Fehler beim Aufbau: {0}", "Build error: {0}");
            A("ALLES FREISCHALTEN  (Max-Level, alle Upgrades, Lagerhalle, Skills, 1 Mio €)", "UNLOCK EVERYTHING  (max level, all upgrades, warehouse, skills, 1 M €)");
            A("Perf-Overlay (FPS, Draw Calls, GC/Frame, NPCs, Lichter)", "Perf overlay (FPS, draw calls, GC/frame, NPCs, lights)");
            A("Protokoll-Ordner öffnen (Fehlerberichte: {0})", "Open log folder (bug reports: {0})");
            A("Fehlende Übersetzungen speichern ({0}, nur bei Englisch gesammelt)", "Save missing translations ({0}, only collected in English)");
            A("[AN]  {0}", "[ON]  {0}");
            A("[AUS]  {0}", "[OFF]  {0}");
            A("Geld", "Money");
            A("+{0} €", "+{0} €");
            A("−10.000 €", "−10,000 €");
            A("Geld auf 0", "Money to 0");
            A("Schulden weg", "Clear debt");
            A("Unendlich Geld (nie unter 1 Mio)", "Infinite money (never below 1 M)");
            A("Keine Insolvenz", "No bankruptcy");
            A("Level & Skills", "Level & skills");
            A("Lv {0}", "Lv {0}");
            A("+500 XP", "+500 XP");
            A("Alle Skills lernen", "Learn all skills");
            A("+3 Skillpunkte", "+3 skill points");
            A("Alle Upgrades + Deko + Lifestyle", "All upgrades + decor + lifestyle");
            A("Firma", "Company");
            A("Bewertung 5,0", "Rating 5.0");
            A("Bewertung 1,0", "Rating 1.0");
            A("Bekanntheit max", "Max awareness");
            A("TikToks zurücksetzen", "Reset TikToks");
            A("Intro überspringen", "Skip intro");
            A("Kiste landet sofort und gratis am Wareneingang.", "Crate lands instantly and free at goods-in.");
            A("Menge pro Kiste:", "Amount per crate:");
            A("Alle Produkte vor mir anzeigen", "Show all products in front of me");
            A("Vorschau weg", "Remove preview");
            A("Alle Lieferungen sofort da", "All deliveries arrive now");
            A("+100 Kartons (jede Größe)", "+100 boxes (each size)");
            A("Alle Produkte im Shop listen", "List all products in the shop");
            A("+1 Bestellung", "+1 order");
            A("+5 Bestellungen", "+5 orders");
            A("+3 Express", "+3 express");
            A("Großauftrag-Angebot", "Bulk order offer");
            A("Retourenwelle (3)", "Return wave (3)");
            A("Bestellungen nehmen gelistete Produkte (sonst zufällig). Offene Bestellungen: {0}", "Orders use listed products (otherwise random). Open orders: {0}");
            A("Marketing (Fakebook / Gugel)", "Marketing (Fakebook / Gugel)");
            A("Kampagnen: {0} Fakebook, {1} Gugel · Fake-Profile {2} · KI-Seiten {3}", "Campaigns: {0} Fakebook, {1} Gugel · fake profiles {2} · AI sites {3}");
            A("Fakebook-Ad (bestes Produkt, 100 €)", "Fakebook ad (best product, 100 €)");
            A("Gugel-Ad Platz 1 (60 €)", "Gugel ad spot 1 (60 €)");
            A("+3 Fake-Profile", "+3 fake profiles");
            A("+1 KI-Seite", "+1 AI site");
            A("Fakes auffliegen lassen", "Expose the fakes");
            A("Alle Kampagnen beenden", "End all campaigns");
            A("Mini-Event: Straßenfest", "Mini event: street festival");
            A("Status: {0}", "Status: {0}");
            A("Status: {0} · Stand: {1} Artikel · {2} verkauft", "Status: {0} · stall: {1} items · {2} sold");
            A("Straßenfest jetzt (mit Aufbau)", "Street festival now (with setup)");
            A("Straßenfest sofort live", "Street festival live right away");
            A("Einladung für morgen", "Invitation for tomorrow");
            A("Fest beenden", "End festival");
            A("Zufallsereignisse", "Random events");
            A("Klick löst das Ereignis sofort aus (Mail kommt aufs Handy/Laptop).", "Click triggers the event immediately (mail lands on phone/laptop).");
            A("{0}   –   {1}", "{0}   –   {1}");
            A("Steigend", "Rising");
            A("HYPE", "HYPE");
            A("Fallend", "Falling");
            A("Tot", "Dead");
            A("{0}  ({1})", "{0}  ({1})");
            A("Zeit", "Time");
            A("Zeit anhalten", "Pause time");
            A("Zeit ×{0}", "Time ×{0}");
            A("+1 Stunde", "+1 hour");
            A("Feierabend jetzt", "Closing time now");
            A("Teleport", "Teleport");
            A("Lagerhalle", "Warehouse");
            A("Noclip AUS (Alt)", "Noclip OFF (Alt)");
            A("Noclip AN (Alt)", "Noclip ON (Alt)");
            A("Wetter (Test)", "Weather (test)");
            A("Automatisch", "Automatic");
            A("Sonnig", "Sunny");
            A("Bewölkt", "Cloudy");
            A("Regen", "Rain");
            A("Tipp: Die Lagerhalle steht erst nach „Alle Upgrades“ bzw. „Alles freischalten“.", "Tip: the warehouse only appears after “All upgrades” or “Unlock everything”.");
            A("Stadt, Post & Emotes", "City, post & emotes");
            A("Karte", "Map");
            A("Vogelperspektive AUS", "Bird's-eye view OFF");
            A("Vogelperspektive", "Bird's-eye view");
            A("Karte als PNG speichern", "Save map as PNG");
            A("Einkaufsviertel", "Shopping district");
            A("Teleport: Läden", "Teleport: shops");
            A("Hund adoptieren", "Adopt a dog");
            A("Katze adoptieren", "Adopt a cat");
            A("+14 Futter", "+14 food");
            A("Tier-Tag simulieren", "Simulate pet day");
            A("Aktionen neu würfeln", "Reroll special offers");
            A("Tiere weg", "Remove pets");
            A("Alle Tierprodukte vor mir anzeigen", "Show all pet products in front of me");
            A("Tier-Vorschau weg", "Remove pet preview");
            A("Alle Tier-Gadgets geben", "Give all pet gadgets");
            A("Admin-Fehler: {0}", "Admin error: {0}");
            // Fortschritt
            A("Energie {0} · Müll {1}/{2} · Hustle-Punkte {3} · Porto heute {4} € · Marke {5} %", "Energy {0} · trash {1}/{2} · hustle points {3} · postage today {4} € · brand {5} %");
            A("Perks & Einrichtung", "Perks & furnishings");
            A("+5 Hustle-Punkte (XP)", "+5 hustle points (XP)");
            A("Alle Einrichtung kaufen", "Buy all furnishings");
            A("Energie 100", "Energy 100");
            A("Energie 5", "Energy 5");
            A("Ausrüstung & Müll", "Equipment & trash");
            A("Müll füllen", "Fill trash");
            A("Müll abholen", "Collect trash");
            A("Logistik", "Logistics");
            A("Störung beenden", "End disruption");
            A("Defekte Charge (Hülle)", "Defective batch (phone case)");
            A("Großhändler-Anfrage", "Wholesaler request");
            A("Kein Produkt verfügbar.", "No product available.");
            // Stadt-Debug (World/CityDebug)
            A("TP: PaketBlitz-Filiale", "TP: ParcelBolt branch");
            A("TP: Packstation Parkecke", "TP: parcel locker park corner");
            A("TP: Wohnviertel", "TP: residential area");
            A("TP: Finanzviertel (Lot)", "TP: financial district (lot)");
            A("TP: Einkaufsviertel (Lot)", "TP: shopping district (lot)");
            A("TP: Westende", "TP: west end");
            A("TP: Ostende", "TP: east end");
            A("Müllabfuhr jetzt", "Garbage truck now");
            A("Alle Mülleimer voll", "Fill all bins");
            A("Verkehr: {0}", "Traffic: {0}");
            A("+3 etikettierte Pakete in die Hand", "+3 labelled parcels into hand");
            A("Moped zu mir", "Bring moped to me");
            A("Emote: {0}", "Emote: {0}");
        }

        // ---- Sonstiges: Diagramme, Perf-Overlay, Lade-/Überblendungen ------------------------------------
        private static void Misc(System.Action<string, string> A)
        {
            A("Noch keine Daten", "No data yet");
            // Bestell-Stufen (UiFmt.OrderInfo.StageText)
            A("entnommen", "picked");
            A("verpackt", "packed");
            A("etikettiert", "labelled");
            A("auf dem Band", "on the belt");
            // Boost-Chips im HUD
            A("{0} ({1})  ×{2} · {3}", "{0} ({1})  ×{2} · {3}");
            A("{0}  ×{1} · {2}", "{0}  ×{1} · {2}");
            A("Shop offline · noch {0}", "Shop offline · {0} left");
            // Spielstand-Liste
            A("gespeichert am {0} um {1} Uhr", "saved on {0} at {1}");
            // Admin-Statuszeile
            A("Geld {0} €   ·   Level {1} ({2} XP)   ·   Tag {3} {4}   ·   Bewertung {5}   ·   Schulden {6} €   ·   Bestellungen {7}   ·   Zeit ×{8}",
              "Money {0} €   ·   Level {1} ({2} XP)   ·   Day {3} {4}   ·   rating {5}   ·   debt {6} €   ·   orders {7}   ·   time ×{8}");
            A("Mit Liebe verpackt", "Packed with love");
            // Perf-Overlay (Entwickler)
            A("FPS {0}  ·  Ø {1} ms  ·  max {2} ms", "FPS {0}  ·  avg {1} ms  ·  max {2} ms");
            A("Draw Calls {0}", "Draw calls {0}");
            A("Draw Calls {0}  ·  Batches {1}  ·  SetPass {2}  ·  Tris {3}k", "Draw calls {0}  ·  batches {1}  ·  SetPass {2}  ·  tris {3}k");
            A("Renderer sichtbar {0} / {1} (Draw-Call-Näherung)", "Renderers visible {0} / {1} (draw call estimate)");
            A("GC/Frame Ø {0}  ·  max {1}  ·  Heap {2} MB", "GC/frame avg {0}  ·  max {1}  ·  heap {2} MB");
            A("GC/Frame Ø {0}  ·  max {1}  ·  {2}× GC  ·  Heap {3} MB", "GC/frame avg {0}  ·  max {1}  ·  {2}× GC  ·  heap {3} MB");
            A("GC/Frame Ø {0} (Hauptthread)  ·  Heap {1} MB", "GC/frame avg {0} (main thread)  ·  heap {1} MB");
            A("GC/Frame Ø {0} (Hauptthread)  ·  {1}× GC  ·  Heap {2} MB", "GC/frame avg {0} (main thread)  ·  {1}× GC  ·  heap {2} MB");
            A("NPCs {0}  ·  Fahrzeuge {1}  ·  Lichter {2} / {3}  ·  Pakete am Boden {4}", "NPCs {0}  ·  vehicles {1}  ·  lights {2} / {3}  ·  parcels on the ground {4}");
            A("Fehler {0}  ·  abgefangen {1}  ·  Qualität {2}", "Errors {0}  ·  caught {1}  ·  quality {2}");
        }

        // @@MORE
    }
}
