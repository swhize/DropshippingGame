namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: 3D-Welt (Runtime/World, Player, Vehicles, Feel, Rendering: Schilder, Sprechblasen, Hinweise, NPC-Sprüche, Ladennamen).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// </summary>
    internal static class EnWorld
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
            Stations(A);
            StationPrompts(A);
            Garage(A);
            City(A);
            Shops(A);
            Finance(A);
            Post(A);
            Festival(A);
            People(A);
            Player(A);
            Moped(A);
            Feel(A);
        }

        // ---- Stationen: Titel (HUD-Hinweis) ----------------------------------------------------------
        private static void Stations(System.Action<string, string> A)
        {
            A("Laptop", "Laptop");
            A("Büro-PC", "Office PC");
            A("Wareneingang", "Goods-in");
            A("Regal: {0}", "Shelf: {0}");
            A("Packtisch", "Packing table");
            A("Labeldrucker", "Label printer");
            A("Falttisch", "Folding table");
            A("Förderband", "Conveyor belt");
            A("Durchreiche", "Serving hatch");
            A("Kalle (Chef)", "Kalle (boss)");
            A("Matratze", "Mattress");
            A("Stempeluhr", "Time clock");
            A("Verkaufsstand", "Sales stall");
            A("Retourenfach", "Returns tray");
            A("Retourenplatz: Prüftisch", "Returns bay: inspection table");
            A("Retourenplatz: Container", "Returns bay: container");
            A("Palettenplatz (B2B)", "Pallet spot (B2B)");
            A("Bestell-Monitor", "Order monitor");
            A("Faltmaschine", "Folding machine");
            A("Mülltonnen", "Bins");
            A("Ringlicht (Creator-Ecke)", "Ring light (creator corner)");
            A("Einkaufswagen", "Shopping trolley");
            A("Preisscanner", "Price scanner");
            A("Heckbox", "Top box");
            A("Marktstand", "Market stall");
            A("Festkisten-Packtisch", "Festival crate table");
            A("Bankschalter", "Bank counter");
            A("Handelsterminal", "Trading terminal");
            A("PaketBlitz-Abholung", "ParcelBolt pickup");
            A("Versandkäfig (Abholung)", "Shipping cage (pickup)");
            A("PaketBlitz-Filiale: Schalter", "ParcelBolt branch: counter");
            A("PaketBlitz-Packstation", "ParcelBolt parcel locker");
            A("Katze", "Cat");
            A("Hund", "Dog");
            A("Möbel", "Furniture");
            // Möbel-Namen (Möbel verschieben)
            A("Pflanze", "Plant");
            A("Stehlampe", "Floor lamp");
            A("Teppich", "Rug");
            A("Bücherregal", "Bookshelf");
            A("Kaffeemaschine", "Coffee machine");
            A("Sofa", "Sofa");
            A("Sneaker-Vitrine", "Sneaker display");
            A("Gaming-Stuhl", "Gaming chair");
        }

        // ---- Stationen: Interaktions-Hinweise und Meldungen (Station.cs) -----------------------------------
        private static void StationPrompts(System.Action<string, string> A)
        {
            A("Erst die Schicht bei Kalle beenden", "Finish your shift at Kalle's first");
            A("Laptop öffnen", "Open laptop");
            A("PC benutzen", "Use PC");
            A("Kiste zurückstellen", "Put the crate back");
            A("Retoure zurücklegen", "Put the return back");
            A("Hände voll", "Hands full");
            A("Keine Kisten · Retouren liegen im Retourenfach", "No crates · returns are in the returns tray");
            A("Keine Lieferung da", "No delivery here");
            A("Lieferung ist unterwegs ...", "Delivery is on its way ...");
            A("Kiste aufnehmen ({0}× {1})", "Pick up crate ({0}× {1})");
            A("Gesperrt – ab Firmenlevel {0}", "Locked – from company level {0}");
            A("Falsches Regal (Kiste: {0})", "Wrong shelf (crate: {0})");
            A("Kiste einräumen (+{0})", "Stock the shelf (+{0})");
            A("Hände voll – Artikel erst zum Packtisch", "Hands full – take the item to the packing table first");
            A("Hände voll – erst zum Packtisch (2× {0}: zurücklegen)", "Hands full – packing table first (2× {0}: put back)");
            A("Zurücklegen", "Put back");
            A("Hände voll – erst Pakete wegbringen", "Hands full – deliver the parcels first");
            A("Regal leer – Großauftrag braucht noch {0} (nachbestellen!)", "Shelf empty – bulk order still needs {0} (reorder!)");
            A("Regal leer – im Laptop nachbestellen", "Shelf empty – reorder on the laptop");
            A("Für Großauftrag entnehmen (noch {0})", "Take for bulk order ({0} to go)");
            A("{0}: {1} auf Lager · keine Bestellung", "{0}: {1} in stock · no order");
            A("{0} entnehmen ({1} offen)", "Take {0} ({1} open)");
            A("Großauftrag-Artikel – ab zum Palettenplatz", "Bulk order item – off to the pallet spot");
            A("Verpacken (Karton {0} · {1} übrig)", "Pack it (box {0} · {1} left)");
            A("Weiteres Paket nehmen ({0}/{1} in der Hand)", "Take another parcel ({0}/{1} in hand)");
            A("Paket zurücklegen", "Put the parcel back");
            A("Paket nehmen ({0} bereit)", "Take parcel ({0} ready)");
            A("Bring einen Artikel zum Verpacken", "Bring an item to pack");
            A("Alle {0} Pakete etikettieren", "Label all {0} parcels");
            A("Versandlabel drucken", "Print shipping label");
            A("Hat schon ein Label", "Already has a label");
            A("Bring ein verpacktes Paket hierher", "Bring a packed parcel here");
            A("Hände frei machen zum Falten", "Free your hands to fold");
            A("Karton falten ({0} ungefaltet)", "Fold a box ({0} flat)");
            A("Keine ungefalteten Kartons", "No flat boxes");
            A("Alle {0} Pakete aufs Band legen", "Put all {0} parcels on the belt");
            A("Aufs Förderband legen", "Put on the conveyor belt");
            A("Erst etikettieren", "Label it first");
            A("Etikettierte Pakete aufs Band legen", "Put labelled parcels on the belt");
            A("Die Küche ist nicht mehr dein Problem", "The kitchen isn't your problem anymore");
            A("Bring den Teller zu Tisch {0}", "Take the plate to table {0}");
            A("Sprich zuerst mit Kalle", "Talk to Kalle first");
            A("Teller nehmen", "Take plate");
            A("Keine Teller mehr", "No plates left");
            A("Teller servieren", "Serve plate");
            A("Falscher Tisch (Teller für Tisch {0})", "Wrong table (plate is for table {0})");
            A("Mit Kalle sprechen", "Talk to Kalle");
            A("Feierabend machen (Tag beenden)", "Call it a day (end the day)");
            A("Verkaufsstand – in der Ausbau-App kaufen", "Sales stall – buy it in the build app");
            A("Kiste auf den Stand stellen ({0}/{1})", "Put the crate on the stall ({0}/{1})");
            A("Stand: {0} Artikel – Passanten kaufen hier", "Stall: {0} items – passers-by buy here");
            A("Stell eine Kiste auf den Stand", "Put a crate on the stall");
            A("Retourenfach leer · {0} Retoure(n) unterwegs", "Returns tray empty · {0} return(s) in transit");
            A("Retourenfach leer", "Returns tray empty");
            A("Retoure aufnehmen ({0} · {1} im Fach)", "Pick up return ({0} · {1} in the tray)");
            A("Als B-Ware einlagern ({0})", "Restock as B-stock ({0})");
            A("Hier werden nur Retouren geprüft", "Only returns get inspected here");
            A("Hol eine Retoure aus dem Retourenfach ({0} warten)", "Grab a return from the returns tray ({0} waiting)");
            A("Retourenplatz – keine Retouren", "Returns bay – no returns");
            A("Retoure entsorgen ({0})", "Bin the return ({0})");
            A("Container für unverkäufliche Retouren", "Container for unsellable returns");
            A("Bestell-Monitor: keine offenen Bestellungen", "Order monitor: no open orders");
            A("Bestell-Monitor: {0} offen", "Order monitor: {0} open");
            A("Bestell-Monitor: {0} offen · {1} Express", "Order monitor: {0} open · {1} express");
            A("Bestell-Monitor: {0} offen · {1} überfällig", "Order monitor: {0} open · {1} overdue");
            A("Bestell-Monitor: {0} offen · {1} Express · {2} überfällig", "Order monitor: {0} open · {1} express · {2} overdue");
            A("Palettenplatz – Großaufträge ab Firmenlevel {0}", "Pallet spot – bulk orders from company level {0}");
            A("Auf die Palette: {0} (noch {1})", "Onto the pallet: {0} ({1} to go)");
            A("Artikel gehört zu einer Bestellung – zum Packtisch", "This item belongs to an order – to the packing table");
            A("Kein laufender Großauftrag", "No bulk order running");
            A("Qualität reicht für den Auftrag nicht", "Quality isn't good enough for the order");
            A("Kein Großauftrag braucht {0}", "No bulk order needs {0}");
            A("Nur Kisten oder Artikel für Großaufträge", "Only crates or items for bulk orders");
            A("Palettenplatz – Aufträge in der App 'Aufträge' annehmen", "Pallet spot – accept orders in the 'Bulk orders' app");
            // Meldungen
            A("Die Schicht ist noch nicht vorbei. Kalle wartet!", "The shift isn't over yet. Kalle is waiting!");
            A("Hände sind voll.", "Your hands are full.");
            A("Hände voll – erst die Pakete versenden.", "Hands full – ship the parcels first.");
            A("Damit kannst du hier nichts anfangen.", "That's no use here.");
            A("Das Paket hat schon ein Versandlabel.", "The parcel already has a shipping label.");
            A("Bring ein verpacktes Paket zum Labeldrucker.", "Bring a packed parcel to the label printer.");
            A("Zum Falten brauchst du freie Hände.", "You need free hands to fold.");
            A("Nur etikettierte Pakete aufs Band legen.", "Only labelled parcels go on the belt.");
            A("Das ist Tisch {0} – der Teller gehört an Tisch {1}.", "That's table {0} – the plate goes to table {1}.");
            A("Den Verkaufsstand kaufst du im Laptop unter 'Ausbau' (ab Level 2).", "You buy the sales stall on the laptop under 'Build' (from level 2).");
            A("{0}× {1} auf den Stand gestellt.", "Put {0}× {1} on the stall.");
            A("Stell eine volle Kiste auf den Stand. Passanten kaufen zu deinem Webshop-Preis.", "Put a full crate on the stall. Passers-by pay your web shop price.");
            A("Nimm eine Retoure aus dem Retourenfach am Wareneingang und bring sie her.", "Take a return from the returns tray at goods-in and bring it here.");
            A("Hier prüfst du Retouren: links als B-Ware einlagern, rechts entsorgen.", "This is where you inspect returns: left to restock as B-stock, right to bin.");
            A("Auf die Palette kommen nur Kisten und einzelne Artikel für Großaufträge.", "Only crates and single items for bulk orders go on the pallet.");
            A("Großaufträge von Firmen gibt es ab Firmenlevel {0}.", "Bulk orders from companies start at company level {0}.");
            A("Kein laufender Großauftrag. Angebote findest du in der App 'Aufträge'.", "No bulk order running. You'll find offers in the 'Bulk orders' app.");
            A("Bring ganze Kisten vom Wareneingang oder einzelne Artikel aus dem Regal hierher.", "Bring whole crates from goods-in or single items from the shelf here.");
            A("Alle Bestellungen siehst du auch auf dem Handy ({0}).", "You can also see all orders on your phone ({0}).");
            A("Dieses Produkt wird erst ab Firmenlevel {0} freigeschaltet.", "This product unlocks at company level {0}.");
            A("Falsches Regal! Die Kiste gehört zu '{0}'.", "Wrong shelf! The crate belongs to '{0}'.");
            A("Hände voll – erst den Artikel in der Hand zum Packtisch bringen (oder ins Regal '{0}' zurücklegen).",
              "Hands full – take the item you're holding to the packing table first (or put it back on the '{0}' shelf).");
            A("Du trägst schon einen Artikel (#{0}) – erst zum Packtisch, dann den nächsten holen. Nochmal {1} = zurücklegen.",
              "You're already carrying an item (#{0}) – packing table first, then grab the next one. {1} again = put back.");
            A("Hände voll – erst die Pakete wegbringen (Label/Versand), dann neue Artikel holen.", "Hands full – deliver the parcels first (label/shipping), then grab new items.");
            A("Hände voll – damit kannst du am Regal nichts anfangen.", "Hands full – that's no use at the shelf.");
            // Schilder an Stationen
            A("GROSSAUFTRÄGE\nab Level {0}", "BULK ORDERS\nfrom level {0}");
            A("Kein Großauftrag", "No bulk order");
            A("Wartet auf Essen", "Waiting for food");
            A("BALD HIER:\nDEIN STAND", "COMING SOON:\nYOUR STALL");
            A("ab {0}", "from {0}");
            A("WARENEINGANG", "GOODS-IN");
            A("VERSAND", "SHIPPING");
            A("RETOUREN", "RETURNS");
            A("B-WARE", "B-STOCK");
            A("ENTSORGEN", "BIN IT");
            A("B2B · SPEDITION", "B2B · FREIGHT");
            A("ZONE", "ZONE");
            A("LAGER", "STORAGE");
            A("PACKEN", "PACKING");
            A("RETOURE", "RETURN");
            A("Retoure: {0}", "Return: {0}");
            A("BESTELLUNGEN", "ORDERS");
            A("BESTELLUNGEN ({0})", "ORDERS ({0})");
            A("+ {0} weitere", "+ {0} more");
            A("Keine offenen Bestellungen", "No open orders");
            A("{0} EXPRESS {1} · {2}", "{0} EXPRESS {1} · {2}");
            // Ausrüstung
            A("FALTOMAT 3000", "FOLDOMATIC 3000");
            A("Faltmaschine: faltet automatisch ({0} ungefaltet)", "Folding machine: folds automatically ({0} flat)");
            A("Faltmaschine: keine ungefalteten Kartons", "Folding machine: no flat boxes");
            A("Müll {0}/{1} · Abholung {2}", "Trash {0}/{1} · pickup {2}");
            A("Müll {0}/{1} – VOLL! · Abholung {2}", "Trash {0}/{1} – FULL! · pickup {2}");
            A("{0} · Müllwagen kommt", "{0} · garbage truck on its way");
            A("{0} · {1}: Sonderabholung ({2})", "{0} · {1}: special pickup ({2})");
            A("Die Faltmaschine arbeitet von allein. Ungefaltete Kartons kaufst du günstiger bei AllesExpress.", "The folding machine works on its own. Flat boxes are cheaper at EverythingXpress.");
            A("Müll: {0}/{1}. Nächste Abholung: {2}.", "Trash: {0}/{1}. Next pickup: {2}.");
            A("Nochmal {0} = Sonderabholung für {1}.", "{0} again = special pickup for {1}.");
            // Ringlicht
            A("TikTok aufnehmen", "Record a TikTok");
            A("TikTok: {0}", "TikTok: {0}");
        }

        // ---- Garage, Imbiss, Lagerhalle (WorldBuilder, Props) -----------------------------------------
        private static void Garage(System.Action<string, string> A)
        {
            A("KALLES IMBISS", "KALLE'S DINER");
            A("DÖNER 5,50\nPOMMES 3,-\nCURRYWURST 4,-", "DÖNER 5.50\nFRIES 3.00\nCURRYWURST 4.00");
            A("KALLES KARTE", "KALLE'S MENU");
            A("Döner ....... 5,50\nCurrywurst .. 4,00\nPommes ...... 3,00\nSchnitzel ... 8,90", "Döner ....... 5.50\nCurrywurst .. 4.00\nFries ....... 3.00\nSchnitzel ... 8.90");
            A("GARAGE", "GARAGE");
            A("ZU VERKAUFEN · 4.500 €", "FOR SALE · 4,500 €");
            A("LAGERHALLE", "WAREHOUSE");
            A("DROPSHIPPING", "DROPSHIPPING");
            A("Vom Imbiss zum Imperium.", "From diner to empire.");
            A("Bald auch in deiner Stadt.", "Coming soon to your town.");
            A("Jetzt online bestellen!", "Order online now!");
            A("Schon {0} Pakete verschickt!", "{0} parcels shipped already!");
            A("Die ganze Stadt redet drüber.", "The whole town is talking about it.");
            A("HUSTLE\nHARDER", "HUSTLE\nHARDER");
            A("HUSTLE", "HUSTLE");
            A("BUSINESSPLAN\n1. Kaufen\n2. Verkaufen\n3. ???\n4. PROFIT", "BUSINESS PLAN\n1. Buy\n2. Sell\n3. ???\n4. PROFIT");
            A("Puh, zu teuer ...", "Phew, too pricey ...");
            A("PaketBlitz", "ParcelBolt");
            A("PaketBlitz\nfür {0}", "ParcelBolt\nfor {0}");
            A("MeinShop", "MyShop");
            A("MEIN STAND", "MY STALL");
        }

        // ---- Stadt: Läden, Straßen, Verkehr, Post (CityBuilder, CityTraffic, CityEdges) --------------------
        private static void City(System.Action<string, string> A)
        {
            // Lustige Ladennamen (Schilder sind in Großschrift)
            A("Haarmonie", "Hairmony");
            A("Hair Force One", "Hair Force One");
            A("Kamm in", "Comb In");
            A("Bäckerei Krümel", "Crumbs Bakery");
            A("Brotzeit", "Loaf Time");
            A("Kaffeeklatsch", "Bean Gossip");
            A("Bohne & Söhne", "Bean & Sons");
            A("Döner Ehrlich", "Honest Döner");
            A("Wurstcase", "Wurst Case");
            A("Pizza Mamma Mia", "Pizza Mamma Mia");
            A("Wok'n'Roll", "Wok'n'Roll");
            A("Eiszeit", "Ice Age");
            A("Kiosk 24/7½", "Kiosk 24/7½");
            A("Späti Spätzünder", "Late Bloomer Shop");
            A("Blumen Rosenkavalier", "Rose Cavalier Flowers");
            A("Optik Durchblick", "Clear View Optics");
            A("Copyshop Doppelt", "Copy That");
            A("Waschsalon Schleudertrauma", "Whiplash Laundromat");
            A("Drogerie Schaumschläger", "Windbag Drugstore");
            A("Buchladen Seitenwechsel", "Page Turner Books");
            A("Getränke Durstlöscher", "Thirst Aid Drinks");
            A("Speiche & Co.", "Spoke & Co.");
            A("Handy-Doktor", "Phone Doctor");
            A("Muckibude", "Muscle Shack");
            A("Nagelneu", "Nailed It");
            A("Stichfest Tattoo", "Ink-Proof Tattoo");
            A("Reisebüro Weitweg", "Far Away Travel");
            A("Schuh-Bidu", "Shoe-Bidoo");
            A("Zweite Chance", "Second Chance");
            A("Pfandastisch", "Refundtastic");
            A("Hustle Hub Coworking", "Hustle Hub Coworking");
            A("Krypto-Café Mondflug", "Moonshot Crypto Café");
            A("Escape Room Raus hier", "Escape Room Let Me Out");
            A("Bio Grünzeug", "Organic Greens");
            A("Metzgerei Wurstbrot", "Butcher Sausage Roll");
            A("Schlüsseldienst Aufsperr", "Open Sesame Locksmith");
            A("Fahrschule Vollgas", "Full Throttle School");
            A("Yoga Om-Nom", "Yoga Om-Nom");
            A("Karaoke Schiefton", "Off-Key Karaoke");
            A("Sushi Fischers Fritz", "Sushi She Sells");
            A("Nähstube Fadenschein", "Threadbare Tailoring");
            A("Gemüse Rübezahl", "Turnip the Beet");
            A("Senf dazu", "Cut the Mustard");
            A("Uhren Tick Tack", "Tick Tock Watches");
            A("Kostüme Verkleidet", "Dressed Up Costumes");
            A("Fotostudio Cheese", "Say Cheese Studio");
            A("Pommesbude Frittiererei", "Fry Me to the Moon");
            A("Socken-Paradies", "Sock Paradise");
            A("Dönerwetter", "Döner Thunder");
            A("Schreibwaren Klecks", "Splodge Stationery");
            // Straßen & Schilder
            A("Paketweg", "Parcel Way");
            A("Hustlegasse", "Hustle Lane");
            A("Retourenring", "Returns Ring");
            A("Kartonallee", "Box Avenue");
            A("Lieferantenstr.", "Supplier St.");
            A("Expressweg", "Express Way");
            A("Lindenweg · Wohngebiet\nSchritttempo", "Lindenweg · residential area\nWalking pace");
            A("HEUTE\n2 FÜR 1", "TODAY\n2 FOR 1");
            A("NEU!\nJETZT\nHIER", "NEW!\nNOW\nHERE");
            A("RABATT\n-10%", "SALE\n-10%");
            A("FRISCH\nAUS DEM\nOFEN", "FRESH\nFROM THE\nOVEN");
            A("WIR\nSIND\nOFFEN", "WE\nARE\nOPEN");
            A("STADTTUNNEL\n← Autobahn", "CITY TUNNEL\n← Motorway");
            A("STADTTUNNEL\nAutobahn →", "CITY TUNNEL\nMotorway →");
            A("ALTPAPIER", "PAPER ONLY");
            // Post
            A("FILIALE", "BRANCH");
            A("Mo–Sa 8–20 Uhr · Pakete gratis abgeben", "Mon–Sat 8–20 · drop off parcels for free");
            A("Heute schon\ngehustlet?", "Hustled\nyet today?");
            A("Frau Blitz", "Ms Bolt");
            A("PaketBlitz Filiale", "ParcelBolt branch");
            // Verkehr
            A("HUUUP!", "HOOONK!");
            A("Ey, Straße!", "Oi, eyes on the road!");
            A("Linie 7 · Hustle-Express", "Line 7 · Hustle Express");
            A("Blitzblank\nReinigung", "Squeaky Clean\nCleaning Co.");
            A("Spedition", "Freight");
            A("Stadtreinigung\n\"Wir holen alles ab\"", "City Cleaning\n\"We pick up everything\"");
            A("Verkehr: {0}", "Traffic: {0}");
        }

        // ---- Einkaufsviertel: MediaMarkd, Fressnix, Hype & Hoodie (ShoppingDistrict, ShopInteractables) -----
        private static void Shops(System.Action<string, string> A)
        {
            A("MediaMarkd", "MegaMarked");
            A("Fressnix", "Chewnothing");
            A("Hype & Hoodie", "Hype & Hoodie");
            A("GADGETS & LICHT", "GADGETS & LIGHTING");
            A("AUDIO, VIDEO & DROHNEN", "AUDIO, VIDEO & DRONES");
            A("KNALLER DES TAGES", "DEAL OF THE DAY");
            A("TV & HIFI – nur zum Angucken", "TV & HIFI – look, don't touch");
            A("WEISSE WARE · ab Level 99", "WHITE GOODS · from level 99");
            A("WAGEN", "TROLLEYS");
            A("PREISSCANNER", "PRICE SCANNER");
            A("KASSE", "CHECKOUT");
            A("TIERHEIM-ECKE · ADOPTIEREN", "SHELTER CORNER · ADOPT");
            A("DEMNÄCHST", "COMING SOON");
            A("(als DLC)", "(as DLC)");
            A("ZU", "CLOSED");
            A("AUSVERKAUFT", "SOLD OUT");
            A("AKTION −{0} %", "DEAL −{0} %");
            A("Preis: scannen!", "Price: scan it!");
            A("AKTION {0}", "DEAL {0}");
            A("−{0} % · nur {1}× !", "−{0} % · only {1}× !");
            A("{0} −{1} % · nur {2}× !", "{0} −{1} % · only {2}× !");
            A("Piep piep! Unbezahlte Ware – alles zurück ins Regal.", "Beep beep! Unpaid goods – everything back on the shelf.");
            A("Der Wagen bleibt im Laden.", "The trolley stays in the shop.");
            A("Den Preisscanner hast du am Eingang zurückgelegt.", "You put the price scanner back at the entrance.");
            // Preis- und Einkaufs-Hinweise (Loc.F-Vorlagen in ShopInteractables)
            A("{0} statt {1}", "{0} instead of {1}");
            A("{0} ({1}/Stk · Großhandel {2})", "{0} ({1}/pc · wholesale {2})");
            A("{0} · GÜNSTIGER!", "{0} · CHEAPER!");
            A("{0} · Erst einen Einkaufswagen holen (Eingang)", "{0} · grab a shopping trolley first (entrance)");
            A("Preis? Scanner am Eingang", "Price? Scanner at the entrance");
            A("({0} Stk)", "({0} pcs)");
            A("({0} Portionen)", "({0} portions)");
            A("{0} · {1} drin", "{0} · {1} in cart");
            A("In den Wagen: {0} · noch {1}", "Into the trolley: {0} · {1} left");
            A("In den Korb: {0} · noch {1}", "Into the basket: {0} · {1} left");
            A("Ohne Einkaufswagen geht hier nichts. Die stehen am Eingang.", "Nothing happens here without a trolley. They're at the entrance.");
            A("Du schiebst schon einen Wagen", "You're already pushing a trolley");
            A("Einkaufswagen schieben ({0}: loslassen)", "Push shopping trolley ({0}: let go)");
            A("{0} Packungen · {1}", "{0} packs · {1}");
            A("Wagen leer", "Trolley empty");
            A("Preisscanner zurücklegen", "Put the price scanner back");
            A("Preisscanner nehmen (zeigt Preise + Großhandels-Vergleich)", "Take the price scanner (shows prices + wholesale comparison)");
            A("Kasse · Wagen ist leer", "Checkout · trolley is empty");
            A("Kasse · Korb ist leer", "Checkout · basket is empty");
            A("Bezahlen: {0} Packungen · {1}", "Pay: {0} packs · {1}");
            A("Abgeschlossen – {0} eröffnet demnächst (DLC)", "Locked – {0} opens soon (DLC)");
            A("Abgeschlossen. Auf dem Zettel steht: „Demnächst – als DLC“.", "Locked. The note says: “Coming soon – as DLC”.");
            A("Rüttel, rüttel. Immer noch zu.", "Rattle, rattle. Still locked.");
            A("Drinnen hängt ein Hoodie für 89 €. Gut, dass zu ist.", "There's an 89 € hoodie inside. Good thing it's closed.");
            A("Ein Schild: „Season Pass gibt's noch nicht. Bitte nicht klopfen.“", "A sign: “No season pass yet. Please don't knock.”");
            // Haustiere
            A("Futter: {0} Portionen", "Food: {0} portions");
            A("Bleib in der Garage", "Stay in the garage");
            A("Komm mit", "Come along");
            A("{0}: Bleib in der Garage · {1}", "{0}: stay in the garage · {1}");
            A("{0}: Komm mit · {1}", "{0}: come along · {1}");
            A("{0} ({1}) · Futter: {2} Portionen · {3}: Bleib in der Garage", "{0} ({1}) · food: {2} portions · {3}: stay in the garage");
            A("{0} ({1}) · Futter: {2} Portionen · {3}: Komm mit", "{0} ({1}) · food: {2} portions · {3}: come along");
            A("{0} streicheln ({1}) · Futter: {2} Portionen · {3}: Bleib in der Garage", "Pet {0} ({1}) · food: {2} portions · {3}: stay in the garage");
            A("{0} streicheln ({1}) · Futter: {2} Portionen · {3}: Komm mit", "Pet {0} ({1}) · food: {2} portions · {3}: come along");
        }

        // ---- Finanzviertel: Bank, Börse, Geldautomat (FinanceDistrict) ------------------------------------
        private static void Finance(System.Action<string, string> A)
        {
            A("DER GOLDENE BULLE\n(Anfassen bringt Rendite. Nicht.)", "THE GOLDEN BULL\n(Touching it brings returns. Not.)");
            A("FINANZVIERTEL", "FINANCIAL DISTRICT");
            A("KIEZBANK", "BLOCK BANK");
            A("Seit 2019. Gefühlt.", "Since 2019. Feels like it.");
            A("SCHALTER 1 VON 1", "COUNTER 1 OF 1");
            A("Sparen ist das neue Ausgeben!", "Saving is the new spending!");
            A("GELD", "CASH");
            A("BÖRSE", "STOCK EXCHANGE");
            A("KURSE LIVE", "LIVE PRICES");
            A("HANDELN", "TRADE");
            A("EILMELDUNG: {0}", "BREAKING: {0}");
            A("Keine Nachrichten. Verdächtig ruhig.", "No news. Suspiciously quiet.");
            A("HEKTIK {0}%", "FRENZY {0}%");
            A("Mit dem Bankberater sprechen (Konten, Kredit, Sparen)", "Talk to the bank advisor (accounts, loans, savings)");
            A("Geldautomat · Konto {0}", "ATM · account {0}");
            A("Handeln: Aktien, ETFs, Krypto · Depot {0}", "Trade: stocks, ETFs, crypto · portfolio {0}");
            A("Geschlossen. Erst mal Geld verdienen.", "Closed. Go make some money first.");
            A("Geldautomat", "ATM");
            A("Finanzviertel", "Financial district");
            A("Einkaufsviertel", "Shopping district");
            A("Elektromarkt", "Electronics store");
            A("Tierladen", "Pet shop");
        }

        // ---- Post, Packstationen (PostService) ---------------------------------------------------------
        private static void Post(System.Action<string, string> A)
        {
            A("Parkecke", "Park corner");
            A("Filiale", "Branch");
            A("West", "West");
            A("Lindenweg", "Lindenweg");
            A("Ost", "East");
            A("Paket abgeben (+{0} · gratis)", "Drop off parcel (+{0} · free)");
            A("Paket abgeben (+{0} · Abholung −{1})", "Drop off parcel (+{0} · pickup −{1})");
            A("{0} Pakete abgeben (+{1} · gratis)", "Drop off {0} parcels (+{1} · free)");
            A("{0} Pakete abgeben (+{1} · Abholung −{2})", "Drop off {0} parcels (+{1} · pickup −{2})");
            A("Erst ein Versandlabel drucken!", "Print a shipping label first!");
            A("Abholung: {0}/Paket · Packstation gratis", "Pickup: {0}/parcel · parcel locker free");
            A("Etikettierte Pakete gratis abgeben", "Drop off labelled parcels for free");
            A("Ohne Versandlabel nimmt PaketBlitz nichts an!", "ParcelBolt won't take anything without a shipping label!");
            A("Abholung kostet pro Paket. Packstationen in der Stadt nehmen gratis an.", "Pickup costs per parcel. Parcel lockers in town take them for free.");
            A("Hier gibst du etikettierte Pakete gratis ab.", "Drop off labelled parcels here for free.");
            A("Zu wenig Geld für die Abholung – bring die Pakete zur Packstation (gratis).", "Not enough money for the pickup – take the parcels to a parcel locker (free).");
            A("Pakete ohne Versandlabel bleiben in der Hand.", "Parcels without a shipping label stay in your hands.");
            A("PaketBlitz-Abholung: −{0}. Tipp: An Packstationen ist die Abgabe gratis.", "ParcelBolt pickup: −{0}. Tip: drop-off at parcel lockers is free.");
            A("Danke! Geht sofort raus.", "Thanks! Goes out right away.");
            A("Wieder so viele? Respekt!", "That many again? Respect!");
            A("Express? Alles ist Express bei uns.", "Express? Everything's express with us.");
            A("Schönen Tag noch!", "Have a nice day!");
            A("PAKETANNAHME", "PARCEL DROP-OFF");
            A("PaketBlitz Packstation", "ParcelBolt parcel locker");
        }

        // ---- Straßenfest (StreetFestival) -----------------------------------------------------------------
        private static void Festival(System.Action<string, string> A)
        {
            A("Straßenfest", "Street festival");
            A("STRASSENFEST", "STREET FESTIVAL");
            A("AUFBAU · START {0}", "SETUP · STARTS {0}");
            A("HEUTE BIS {0}", "TODAY UNTIL {0}");
            A("{0} Artikel", "{0} items");
            A("Noch leer – Kisten bringen!", "Still empty – bring crates!");
            A("Ausverkauft", "Sold out");
            A("FESTKISTEN PACKEN", "PACK FESTIVAL CRATES");
            A("NACHSCHUB PACKEN", "PACK MORE STOCK");
            A("Kiste auspacken ({0}× {1} · Stand {2}/{3})", "Unpack crate ({0}× {1} · stall {2}/{3})");
            A("Hände voll – bring eine Kiste", "Hands full – bring a crate");
            A("Stand: {0} Artikel · Start in {1}", "Stall: {0} items · starts in {1}");
            A("Stand: {0} Artikel · {1} verkauft ({2})", "Stall: {0} items · {1} sold ({2})");
            A("Pack am Packtisch vor deiner Tür eine Festkiste (oder nimm eine Lieferkiste) und bring sie her.",
              "Pack a festival crate at the packing table outside your door (or grab a delivery crate) and bring it here.");
            A("{0}× {1} am Stand ausgelegt.", "Laid out {0}× {1} at the stall.");
            A("Hände frei machen zum Packen", "Free your hands to pack");
            A("Lager ist leer – nichts zum Einpacken", "Storage is empty – nothing to pack");
            A("Festkiste packen: {0}× {1} ({2} auf Lager)", "Pack festival crate: {0}× {1} ({2} in stock)");
            A("Festkiste packen: {0}× {1} ({2} auf Lager) · danach: {3}", "Pack festival crate: {0}× {1} ({2} in stock) · next: {3}");
            A("Zum Packen brauchst du freie Hände.", "You need free hands to pack.");
            A("Nichts auf Lager. Bestell Ware im Laptop oder bring Lieferkisten direkt zum Stand.", "Nothing in stock. Order goods on the laptop or bring delivery crates straight to the stall.");
            A("Deal!", "Deal!");
            A("+{0} (gefeilscht)", "+{0} (haggled)");
            A("Für {0}? Nein? Schade.", "For {0}? No? Shame.");
            A("Puh, viel zu teuer ...", "Phew, way too pricey ...");
            A("Schon ausverkauft?", "Sold out already?");
            A("Nichts für mich.", "Not for me.");
        }

        // ---- Passanten, Kundschaft, Tiere: Sprechblasen ----------------------------------------------------
        private static void People(System.Action<string, string> A)
        {
            A("Wo bleibt mein Paket?!", "Where's my parcel?!");
            A("Sendungsverfolgung sagt: morgen.", "Tracking says: tomorrow.");
            A("Oh, Rabattcode!", "Ooh, discount code!");
            A("Noch 3 Stück auf Lager?!", "Only 3 left in stock?!");
            A("Hmm, 4,8 Sterne ...", "Hmm, 4.8 stars ...");
            A("Wer schreibt mir da?", "Who's texting me?");
            A("Akku fast leer ...", "Battery almost dead ...");
            A("Schon wieder Werbung.", "Ads again.");
            A("Hey! Lange nicht gesehen!", "Hey! Long time no see!");
            A("Ja, echt! Alles gut?", "Yeah, right! All good?");
            A("Hast du den neuen Laden gesehen?", "Seen the new shop?");
            A("Die verschicken voll schnell!", "They ship super fast!");
            A("Schönes Wetter heute.", "Nice weather today.");
            A("Endlich mal!", "About time!");
            A("Kennst du den Stand da?", "Know that stall over there?");
            A("Klar, hab da was gekauft.", "Sure, bought something there.");
            A("Na, auch unterwegs?", "Out and about too?");
            A("Muss noch was abholen.", "Got to pick something up.");
            A("Hübsch.", "Pretty.");
            A("Hmm ...", "Hmm ...");
            A("Brauch ich das?", "Do I need this?");
            A("Nicht schlecht!", "Not bad!");
            // Lustige Gangarten (GoofyWalk)
            A("Zzz ...", "Zzz ...");
            A("*gähn*", "*yawn*");
            A("Erst mal Kaffee ...", "Coffee first ...");
            A("Ist schon Montag?", "Is it Monday already?");
            A("Keine Zeit!", "No time!");
            A("Juhuu!", "Wheee!");
            A("Schneller!", "Faster!");
            A("Zu viel Espresso!", "Too much espresso!");
            A("Huch!", "Whoa!");
            A("Wo war ich?", "Where was I?");
            A("Hoppla!", "Oops-a-daisy!");
            A("Alles dreht sich ... vom Karussell!", "Everything's spinning ... that carousel!");
            A("Autsch!", "Ouch!");
            A("Wer stellt da 'ne Laterne hin?!", "Who puts a lamppost there?!");
            A("Aua! Mein Kopf!", "Ow! My head!");
            A("Entschuldigung ... Laterne.", "Sorry ... lamppost.");
            A("Das ist mein Lied!", "That's my song!");
            A("♪ Dum dum dum ♪", "♪ Dum dum dum ♪");
            A("Party!", "Party!");
            // Reaktionen auf Emotes
            A("Hallo!", "Hello!");
            A("Hi! Kennen wir uns?", "Hi! Do we know each other?");
            A("*winkt zurück*", "*waves back*");
            A("Moin!", "Hiya!");
            A("Yeah!", "Yeah!");
            A("Was für Moves!", "What moves!");
            A("Tanzt der etwa?!", "Is he actually dancing?!");
            A("Mach mit!", "Join in!");
            A("Läuft bei dir ...", "Going great for you, huh ...");
            A("Kenn ich.", "Been there.");
            A("Montag, oder?", "Monday, right?");
            A("Kopf hoch!", "Chin up!");
            A("Wow, Muckis!", "Wow, muscles!");
            A("Beeindruckend ... nicht.", "Impressive ... not.");
            A("Pakete schleppen hilft!", "Hauling parcels pays off!");
            A("Oha!", "Whoa!");
            A("Ist das echt?!", "Is that real?!");
            A("Hustle-Bro!", "Hustle bro!");
            A("Spielgeld, oder?", "Monopoly money, right?");
            A("Kann ich was haben?", "Can I have some?");
            A("Pause verdient.", "Break well earned.");
            A("Gemütlich?", "Comfy?");
            A("Platz da noch frei?", "Room for one more?");
            A("Chillig.", "Chill.");
            // Moped-Reaktionen
            A("Ey!", "Oi!");
            A("Ey, pass doch auf!", "Oi, watch it!");
            A("Hey, das ist ein Gehweg!", "Hey, that's a pavement!");
            A("Langsam, du Raser!", "Slow down, speed demon!");
            A("Boah, ey!", "Oh come on!");
            A("Hallo?! Füße!", "Hello?! Feet!");
            A("Jaja!", "Yeah, yeah!");
            A("Is ja gut!", "All right, all right!");
            A("Hupen kann jeder!", "Anyone can honk!");
            A("Hm?", "Hm?");
        }

        // ---- Spieler: Möbel verschieben, Emotes, TikTok-Aufnahme, Noclip, Karte ---------------------------
        private static void Player(System.Action<string, string> A)
        {
            A("Platz frei", "Space free");
            A("{0} verschieben – {1}\n{2}: abstellen · {3}: drehen · {4}: abbrechen", "Move {0} – {1}\n{2}: place · {3}: rotate · {4}: cancel");
            A("{0}: Verschieben", "{0}: move");
            A("{0}: {1} verschieben", "{0}: move {1}");
            A("Ziele auf ein Möbelstück, um es zu verschieben.", "Aim at a piece of furniture to move it.");
            A("Du stehst im Weg", "You're in the way");
            A("Keine Welt", "No world");
            A("Stößt an: {0}", "Bumps into: {0}");
            A("Platz belegt", "Spot taken");
            A("Hier geht das nicht: {0}", "Can't do that here: {0}");
            // Emotes
            A("Winken", "Wave");
            A("Tanzen", "Dance");
            A("Facepalm", "Facepalm");
            A("Flexen", "Flex");
            A("Geldregen", "Make it rain");
            A("Hinsetzen", "Sit down");
            A("{0} Winken", "{0} Wave");
            A("{0} Tanzen", "{0} Dance");
            A("{0} Facepalm", "{0} Facepalm");
            A("{0} Flexen", "{0} Flex");
            A("{0} Geldregen", "{0} Make it rain");
            A("{0} Hinsetzen", "{0} Sit down");
            A("EMOTES", "EMOTES");
            A("{0} loslassen", "release {0}");
            A("EMOTES\n{0} loslassen", "EMOTES\nrelease {0}");
            // TikTok-Aufnahme
            A("Produkt anvisieren – gleich zu Beginn auspacken!", "Aim at the product – unbox it right at the start!");
            A("Ruhig bleiben und einmal ums Produkt gehen.", "Stay steady and walk once around the product.");
            A("Hook zuerst, dann mehrmals vorführen.", "Hook first, then demo it a few times.");
            A("Produkt mittig, ruhig halten, gutes Licht.", "Product centred, hold steady, good light.");
            A("Auspacken", "Unbox");
            A("Vorführen", "Demo");
            A("Drehen", "Rotate");
            A("Aufnahme abgebrochen.", "Recording cancelled.");
            // Noclip, Vogelperspektive
            A("Noclip AN (Alt zum Beenden)", "Noclip ON (Alt to exit)");
            A("Noclip AUS", "Noclip OFF");
            A("Vogelperspektive: WASD verschieben, Mausrad zoomen, Esc zurück", "Bird's-eye view: WASD to pan, mouse wheel to zoom, Esc to return");
            A("Vogelperspektive aus", "Bird's-eye view off");
            A("Karte gespeichert: {0}", "Map saved: {0}");
            A("Karte speichern fehlgeschlagen: {0}", "Saving the map failed: {0}");
            // Gegenstände am Boden
            A("Aufheben: {0}", "Pick up: {0}");
            A("Auf den Stapel legen ({0}/{1})", "Add to the stack ({0}/{1})");
            A("Hände sind schon voll.", "Your hands are already full.");
            // Wetter
            A("Regen", "Rain");
            A("Bewölkt", "Cloudy");
            A("Sonnig", "Sunny");
        }

        // ---- Moped (Moped, MopedHud) ------------------------------------------------------------------------
        private static void Moped(System.Action<string, string> A)
        {
            A("Kiste", "Crate");
            A("Retoure", "Return");
            A("Paket", "Parcel");
            A("{0} Pakete", "{0} parcels");
            A("{0} in die Heckbox laden ({1}/{2})", "Load {0} into the top box ({1}/{2})");
            A("Heckbox voll – erst ausladen", "Top box full – unload first");
            A("Hände frei machen zum Aufsteigen", "Free your hands to get on");
            A("Aufsteigen", "Get on");
            A("Aufsteigen  ·  {0}", "Get on  ·  {0}");
            A("Die Heckbox ist voll (4 Pakete oder 1 Kiste).", "The top box is full (4 parcels or 1 crate).");
            A("Erst die Hände frei machen – {0} legt ab.", "Free your hands first – {0} drops things.");
            A("{0} in der Heckbox ({1}).", "{0} in the top box ({1}).");
            A("Heckbox leer", "Top box empty");
            A("Heckbox: 1 Kiste", "Top box: 1 crate");
            A("Heckbox {0}/{1}", "Top box {0}/{1}");
            A("Heckbox {0}/{1} · {2} mit Label", "Top box {0}/{1} · {2} labelled");
            A("Erst die Hände frei machen – Pakete in die Heckbox laden oder ablegen.", "Free your hands first – load the parcels into the top box or drop them.");
            A("Moped: W/S Gas/Bremse · A/D lenken · {0} Hupe · {1} Ansicht · {2} absteigen", "Moped: W/S throttle/brake · A/D steer · {0} horn · {1} camera · {2} get off");
            A("Moped: RT/LT Gas/Bremse · Stick lenken · {0} Hupe · {1} Ansicht · {2} absteigen", "Moped: RT/LT throttle/brake · stick to steer · {0} horn · {1} camera · {2} get off");
            A("Absteigen …", "Getting off …");
            A("{0} – aus der Heckbox", "{0} – from the top box");
            A("Das Moped gibt es erst nach Kalles Imbiss (Business-Phase).", "The moped only comes after Kalle's Diner (business phase).");
            A("Moped steht vor dir.", "The moped is right in front of you.");
            A("In die Heckbox laden ({0}/{1})", "Load into the top box ({0}/{1})");
            A("Passt nicht – nur Pakete oder eine Kiste", "Doesn't fit – only parcels or one crate");
            A("Heckbox ist leer (Pakete in der Hand → einladen)", "Top box is empty (parcels in hand → load them)");
            A("Heckbox ist leer (Pakete in der Hand » einladen)", "Top box is empty (parcels in hand » load them)");
            A("Herausnehmen: {0}", "Take out: {0}");
            A("Herausnehmen: {0} ({1} drin)", "Take out: {0} ({1} inside)");
            A(" km/h", " km/h");
            A("{0} Absteigen  ·  {1} Hupe  ·  {2} Ansicht", "{0} Get off  ·  {1} Horn  ·  {2} Camera");
            A("35 DSH", "35 DSH");
        }

        // ---- Effekte: schwebende Texte, Rekorde (Feel) --------------------------------------------------------
        private static void Feel(System.Action<string, string> A)
        {
            A("NEUER REKORD: {0}", "NEW RECORD: {0}");
            A("REKORD: {0} PAKETE", "RECORD: {0} PARCELS");
            A("aufs Band", "onto the belt");
            A("{0}× aufs Band", "{0}× onto the belt");
            A("+{0} am Stand", "+{0} at the stall");
            A("+{0} Palette", "+{0} pallet");
            A("B-Ware eingelagert", "Restocked as B-stock");
            A("Entsorgt", "Binned");
            A("Guten Appetit!", "Enjoy your meal!");
            A("+1 Karton {0}", "+1 box {0}");
            A("{0}× etikettiert", "{0}× labelled");
        }
    }
}
