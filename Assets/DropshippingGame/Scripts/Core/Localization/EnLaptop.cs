namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Laptop/Tablet und Web-Apps (Runtime/UI/Laptop/**).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// Aufgeteilt nach Bereichen (Chrome, HustleOS-Seiten, je Web-App eine Methode).
    /// </summary>
    internal static class EnLaptop
    {
        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
            Chrome(A);
            Company(A);
            Market(A);
            AllesExpress(A);
            ProductFlavors(A);
            Shop(A);
            Mail(A);
            Paket(A);
            Trading(A);
            Bank(A);
            TikTak(A);
            Marketing(A);
            Fakebook(A);
            Gugel(A);
            Stadtblatt(A);
        }

        // =====================================================================================
        // Browser-/Tablet-Chrome, Dock, Startbildschirm, Boot, Tutorial-Leiste
        // =====================================================================================
        private static void Chrome(System.Action<string, string> A)
        {
            // Marken (Parodie-Namen laut Leitfaden)
            A("AllesExpress", "EverythingXpress");
            A("Mein Shop", "My Shop");
            A("iWolke Mail", "iCloudy Mail");
            A("iWolke", "iCloudy");
            A("PaketBlitz", "ParcelBolt");
            A("TradingViech", "TradingBeast");
            A("Stadtblatt", "City Gazette");
            A("Kalles Imbiss", "Kalle's Diner");

            // App-Titel und Reiter
            A("Übersicht", "Overview");
            A("Markt & Trends", "Market & Trends");
            A("Marktanalyse", "Market analysis");
            A("Trendradar", "Trend radar");
            A("Aufträge", "Contracts");
            A("Angebote", "Offers");
            A("Laufend", "Active");
            A("Verlauf", "History");
            A("Retouren", "Returns");
            A("Firma", "Company");
            A("Team", "Team");
            A("Skills", "Skills");
            A("Perks", "Perks");
            A("Ausbau", "Upgrades");
            A("Einrichtung", "Interior");
            A("Logistik", "Logistics");
            A("Ziele", "Goals");
            A("Branding", "Branding");
            A("Verpackung", "Packaging");
            A("Statistik", "Stats");
            A("Lifestyle", "Lifestyle");
            A("Erfolge", "Achievements");

            // Dock-Beschriftungen
            A("Einkauf", "Buy");
            A("Shop", "Shop");
            A("Mail", "Mail");
            A("Paket", "Parcel");
            A("Börse", "Stocks");
            A("Bank", "Bank");
            A("Markt", "Market");

            // Leisten, Tooltips, Controller-Hinweise
            A("Schließen (Esc)", "Close (Esc)");
            A("Zurück", "Back");
            A("Startseite der App", "App home page");
            A("Neu laden", "Reload");
            A("Tab & Reiter", "Apps & tabs");
            A("Auswählen", "Select");
            A("Schließen", "Close");
            A("Ab Firmenlevel {0}", "From company level {0}");
            A("Braucht die Lagerhalle", "Needs the warehouse");
            A("Diese App ist noch nicht fertig.", "This app isn't finished yet.");
            A("Diese Seite hatte einen Fehler.", "This page crashed.");
            A("Einfach eine andere App öffnen und zurückkommen. ({0})", "Just open another app and come back. ({0})");

            // Hochfahren
            A("Willkommen zurück.", "Welcome back.");
            A("Willkommen zurück, {0}.", "Welcome back, {0}.");
            A("Starte …", "Starting …");
            A("Lade Bestellungen …", "Loading orders …");
            A("Öffne 7 Tabs gleichzeitig …", "Opening 7 tabs at once …");
            A("Verbinde mit PaketBlitz …", "Connecting to ParcelBolt …");
            A("Motivation wird geladen …", "Loading motivation …");

            // Tutorial-Leiste
            A("TUTORIAL · SCHRITT {0}/{1} · {2}", "TUTORIAL · STEP {0}/{1} · {2}");
            A("Klapp den Laptop zu ({0}) – {1}", "Close the laptop ({0}) – {1}");

            // Startbildschirm (Tablet)
            A("{0} · Tag {1}", "{0} · Day {1}");
            A("Konto", "Balance");
            A("Heute", "Today");
            A("Bestellungen", "Orders");
            A("Bewertung", "Rating");
            A("Boosts", "Boosts");

            // Gesperrt / bald verfügbar
            A("Noch gesperrt – Ab Firmenlevel {0}.", "Still locked – from company level {0}.");
            A("Noch gesperrt – Braucht die Lagerhalle.", "Still locked – needs the warehouse.");
            A("{0}: Ab Firmenlevel {1}", "{0}: from company level {1}");
            A("{0}: Braucht die Lagerhalle", "{0}: needs the warehouse");
            A("Mit dem Trading-Konto steckst du Geld in Krypto, Meme-Aktien oder einen ETF. Riskant, aber manchmal lohnend.",
                "With a trading account you can throw money at crypto, meme stocks or an ETF. Risky, but sometimes worth it.");
            A("Mitarbeiter packen, lagern und verschicken automatisch. Dafür brauchst du die Lagerhalle und etwas Erfahrung.",
                "Staff pack, store and ship automatically. For that you need the warehouse and a bit of experience.");
            A("Sammle Erfahrung – jedes verschickte Paket bringt dich weiter.", "Gain experience – every parcel you ship gets you further.");
            A("Ziele ansehen", "View goals");
            A("{0} – bald verfügbar", "{0} – coming soon");
            A("Ab Level {0}", "From level {0}");
            A("LV {0}", "LV {0}");
            A("MAX", "MAX");
        }

        // =====================================================================================
        // HustleOS: Firma (Team, Ausbau, Ziele, Lifestyle, Perks, Einrichtung, Logistik, Erfolge,
        // Statistik, Skills, Branding, Verpackung), Wochenziele
        // =====================================================================================
        private static void Company(System.Action<string, string> A)
        {
            // Team
            A("Mitarbeiter übernehmen Arbeit automatisch – solange Nachschub da ist. Löhne werden jeden Abend abgerechnet.",
                "Staff take over work automatically – as long as there's supply. Wages are paid every evening.");
            A("MITARBEITER", "STAFF");
            A("LÖHNE PRO TAG", "WAGES PER DAY");
            A("UMSATZ HEUTE", "REVENUE TODAY");
            A("Einstellen", "Hire");
            A("{0} pro Tag · {1}/{2} besetzt", "{0} per day · {1}/{2} filled");
            A("{0} pro Tag · {1}/{2} besetzt (2. Kraft ab Level {3})", "{0} per day · {1}/{2} filled (2nd hire from level {3})");
            A("Voll besetzt", "Fully staffed");
            A("Dein Team", "Your team");
            A("{0} Personen", "{0} people");
            A("Noch niemand eingestellt", "Nobody hired yet");
            A("Dein erstes Teammitglied bringt dir auch das Ziel „Chef sein“.", "Your first team member also unlocks the goal “Be the boss”.");
            A("Entlassen", "Fire");
            A("Packer:innen brauchen Lagerbestand und Kartons, Versandkräfte verpackte Pakete, Lagerist:innen Kisten am Wareneingang. Fehlt etwas, warten sie.",
                "Packers need stock and boxes, shippers need packed parcels, stockers need crates at goods-in. If something's missing, they wait.");

            // Ausbau
            A("Investiere in größere Räume, bessere Technik und ein bisschen Gemütlichkeit.", "Invest in bigger rooms, better tech and a little bit of cosiness.");
            A("STANDORT", "LOCATION");
            A("LAGERPLATZ", "STORAGE");
            A("MIETE", "RENT");
            A("{0}/Tag", "{0}/day");
            A("WARTESCHLANGE", "QUEUE");
            A("{0} Plätze", "{0} slots");
            A("Upgrades", "Upgrades");
            A("Gekauft", "Bought");
            A("Braucht {0}", "Needs {0}");
            A("Kaufen · {0}", "Buy · {0}");
            A("Aufgestellt", "Placed");
            A("Nur in der Lagerhalle", "Warehouse only");

            // Ziele
            A("Deine Meilensteine auf dem Weg zum Imperium.", "Your milestones on the road to an empire.");
            A("Firmenlevel {0}", "Company level {0}");
            A("{0} / {1} XP", "{0} / {1} XP");
            A("NÄCHSTES LEVEL SCHALTET FREI", "NEXT LEVEL UNLOCKS");
            A("Maximales Level erreicht. Legende.", "Max level reached. Legend.");
            A("Wochenziele", "Weekly goals");
            A("{0} / {1} erreicht", "{0} / {1} reached");

            // Wochenziele (Laptop + Handy)
            A("Woche {0} · {1} / {2} geschafft · {3}", "Week {0} · {1} / {2} done · {3}");
            A("endet heute Abend", "ends tonight");
            A("noch {0} Tag", "{0} day left");
            A("noch {0} Tage", "{0} days left");
            A("Noch keine Wochenziele", "No weekly goals yet");
            A("Jeden Montag gibt es drei neue.", "Three new ones every Monday.");
            A("Tauschen (1× pro Woche)", "Swap (1× per week)");
            A("{0} XP", "{0} XP");
            A("+{0} XP", "+{0} XP");

            // Lifestyle
            A("Statussymbole ohne Nutzen, aber mit Stil. Die meisten tauchen in der Welt auf.", "Status symbols with zero use but plenty of style. Most of them show up in the world.");
            A("GEGÖNNT", "TREATED YOURSELF");
            A("KONTOSTAND", "BALANCE");
            A("Gegönnt", "Treated yourself");

            // Perks
            A("Für gesammelte Erfahrung gibt's Hustle-Punkte – zusätzlich zu Level und Skills. Jede Stufe kostet 1 Punkt.",
                "Experience earns you hustle points – on top of levels and skills. Every rank costs 1 point.");
            A("PUNKTE FREI", "POINTS FREE");
            A("AUSGEGEBEN", "SPENT");
            A("NÄCHSTER PUNKT", "NEXT POINT");
            A("in {0} XP", "in {0} XP");
            A("{0} je Stufe · {1}/{2}", "{0} per rank · {1}/{2}");
            A("Maximal", "Maxed");
            A("Kein Punkt frei", "No point free");
            A("Stufe {0} · 1 Punkt", "Rank {0} · 1 point");

            // Einrichtung
            A("Bessere Einrichtung = mehr Energie und schnellere Abläufe. Müde wirst du langsamer.", "Better furniture = more energy and faster work. Tired you gets slower.");
            A("ENERGIE", "ENERGY");
            A("morgen früh: {0}", "tomorrow morning: {0}");
            A("LAUFTEMPO", "WALK SPEED");
            A("VERBRAUCH", "DRAIN");
            A("MÜLL", "TRASH");
            A("Abholung {0}", "Pickup {0}");
            A("{0} · Stufe {1}", "{0} · tier {1}");
            A("Jetzt: {0}", "Now: {0}");
            A("Voll ausgebaut", "Fully upgraded");
            A("Ausrüstung", "Equipment");
            A("{0} ({1}× aufgestellt)", "{0} ({1}× placed)");
            A("Vorhanden", "Owned");
            A("Verpackungsmüll", "Packaging waste");
            A("Jedes Paket und jede Kiste macht Müll. Volle Tonnen bremsen dich und dein Team und kosten Bewertung. Abholung Mo/Mi/Fr 10:00.",
                "Every parcel and every crate makes trash. Full bins slow you and your team down and cost rating. Pickup Mon/Wed/Fri 10:00.");
            A("Sonderabholung · {0}", "Special pickup · {0}");

            // Logistik
            A("Je mehr du verschickst und einkaufst, desto günstiger wird's. Und manchmal streikt halt jemand.",
                "The more you ship and buy, the cheaper it gets. And sometimes somebody's just on strike.");
            A("TARIF", "RATE");
            A("noch {0} Pakete bis {1}", "{0} parcels to {1}");
            A("Höchste Stufe", "Top tier");
            A("PORTO S/M/L", "POSTAGE S/M/L");
            A("PORTO HEUTE", "POSTAGE TODAY");
            A("MARKENSTÄRKE", "BRAND STRENGTH");
            A("+{0} % Preis möglich", "+{0} % price possible");
            A("{0} Betroffen: ca. {1} % der Standard-Pakete.", "{0} Affected: approx. {1} % of standard parcels.");
            A("Paketdienst", "Carrier");
            A("Paket M: {0} €", "Parcel M: {0} €");
            A("Paket M: {0} € · immer pünktlich", "Parcel M: {0} € · always on time");
            A("Aktiv", "Active");
            A("Wählen", "Choose");
            A("Fehlerhafte Chargen", "Defective batches");
            A("{0} Produkt(e)", "{0} product(s)");
            A("{0}: {1} defekt ({2} % des Bestands)", "{0}: {1} defective ({2} % of stock)");
            A("Zurückschicken", "Send back");
            A("Defekte Ware kommt oft zurück und bringt 1-Stern-Bewertungen. Der Lieferant erstattet den Einkaufspreis.",
                "Defective goods often come back and bring 1-star reviews. The supplier refunds the purchase price.");
            A("Mengenrabatt beim Einkauf", "Bulk discount on purchases");
            A("Händlerstatus −{0} %", "Trader status −{0} %");
            A("{0} gekauft", "{0} bought");
            A("nächste Stufe in {0}", "next tier in {0}");
            A("Maximum", "Maximum");
            A("Großhändler (ab Level {0}, mit Markenname) bestellen große Mengen deiner bewährten Produkte – Angebote in der App 'Aufträge'.",
                "Wholesalers (from level {0}, with a brand name) order large amounts of your proven products – offers in the 'Contracts' app.");

            // Erfolge & Statistik
            A("Kleine und große Meilensteine deiner Firma. Manche sind geheim – die findest du nur durch Ausprobieren.",
                "Small and big milestones of your company. Some are secret – you'll only find them by trying things out.");
            A("FREIGESCHALTET", "UNLOCKED");
            A("QUOTE", "RATE");
            A("SCHWIERIGKEIT", "DIFFICULTY");
            A("Geheimer Erfolg", "Secret achievement");
            A("Freigeschaltet an Tag {0}", "Unlocked on day {0}");
            A("Wird verraten, sobald du ihn geschafft hast.", "Revealed once you've earned it.");
            A("Alles, was deine Firma seit dem ersten Paket geschafft hat.", "Everything your company has achieved since the first parcel.");
            A("UMSATZ", "REVENUE");
            A("PAKETE", "PARCELS");
            A("TAG", "DAY");
            A("ERFOLGE", "ACHIEVEMENTS");
            A("Endlos-Modus · nächstes Ziel", "Endless mode · next goal");
            A("Nächstes Ziel", "Next goal");

            // Skills
            A("1 Skillpunkt zum Start und je Level-Aufstieg. Stufe 2–4 brauchen die Vorstufe und Level 3 / 5 / 7. Alle 12 schaffst du nicht – wähle bewusst.",
                "1 skill point to start and one per level-up. Tiers 2–4 need the previous tier and level 3 / 5 / 7. You can't get all 12 – choose wisely.");
            A("1 Skillpunkt frei", "1 skill point free");
            A("{0} Skillpunkte frei", "{0} skill points free");
            A("{0} gelernt · {1} Punkte insgesamt", "{0} learned · {1} points total");
            A("Wirklich zurücksetzen? · {0}", "Really reset? · {0}");
            A("Umschulung · {0}", "Respec · {0}");
            A("Stufe {0} · ab Level {1}", "Tier {0} · from level {1}");
            A("Gelernt", "Learned");
            A("Lernen · 1 Punkt", "Learn · 1 point");
            A("Erst die Vorstufe", "Previous tier first");
            A("Kein Skillpunkt frei", "No skill point free");

            // Branding
            A("Deine Marke: Name, Logo und Farbe. Erscheint auf neuen Kartons, im Webshop und groß an deinem Gebäude.",
                "Your brand: name, logo and colour. Shows up on new boxes, in your web shop and big on your building.");
            A("Markenname", "Brand name");
            A("Speichern", "Save");
            A("Vorschlag", "Suggest");
            A("Logo", "Logo");
            A("Farbe", "Colour");
            A("Vorschau", "Preview");
            A("Tipp: Nur neu gekaufte Kartons (Einkauf › Verpackung) bekommen dieses Design. Das Schild am Gebäude ändert sich sofort.",
                "Tip: only newly bought boxes (Buy › Packaging) get this design. The sign on the building changes right away.");
            A("Marke gespeichert: {0}", "Brand saved: {0}");

            // Verpackung (HustleOS)
            A("Kartons in drei Größen. Beim Kauf wird dein aktuelles Branding aufgedruckt.", "Boxes in three sizes. Your current branding gets printed on when you buy them.");
            A("Dein aktueller Druck", "Your current print");
            A("Neue Kartons bekommen Logo und Farbe deiner Marke. Schon gekaufte Kartons behalten ihren alten Druck.",
                "New boxes get your brand's logo and colour. Boxes you already bought keep their old print.");
            A("Ungefaltete Kartons kosten nur die Hälfte, müssen aber erst am Falttisch gefaltet werden.", "Flat boxes cost only half, but have to be folded at the folding table first.");
            A("Branding ändern", "Change branding");
            A("Karton {0}", "Box {0}");
            A("Passt für: {0}", "Fits: {0}");
            A("{0} gefaltet · {1} ungefaltet", "{0} folded · {1} flat");
            A("{0} Stk · {1}", "{0} pcs · {1}");
            A("{0} ungefaltet · {1}", "{0} flat · {1}");
        }

        // =====================================================================================
        // HustleOS: Markt & Trends, Aufträge, Retouren
        // =====================================================================================
        private static void Market(System.Action<string, string> A)
        {
            // Marktanalyse
            A("Der günstigste Konkurrenzpreis ist der Marktpreis. Liegst du darunter, steigt deine Nachfrage deutlich.",
                "The cheapest competitor price is the market price. Go below it and your demand rises noticeably.");
            A("Preisniveau ×{0}", "Price level ×{0}");
            A("Preiskampf: BilligBoy24 unterbietet bei {0} bis Tag {1}", "Price war: BilligBoy24 undercuts {0} until day {1}");
            A("Preise im Vergleich", "Price comparison");
            A("PRODUKT", "PRODUCT");
            A("DEIN PREIS", "YOUR PRICE");
            A("BILLIGBOY24", "BILLIGBOY24");
            A("TRENDHAUS", "TRENDHAUS");
            A("ALIEXPRESSO", "ALIEXPRESSO");
            A("MARKTPREIS", "MARKET PRICE");
            A("NACHFRAGE", "DEMAND");
            A("offline", "offline");
            A("Faustregel: Knapp unter dem Marktpreis verkaufst du viel mit guter Marge. Premium-Ware bringt bessere Bewertungen – und gute Bewertungen bringen mehr Kunden.",
                "Rule of thumb: just under the market price you sell a lot with a good margin. Premium goods bring better reviews – and good reviews bring more customers.");

            // Trendradar
            A("Hype multipliziert die Nachfrage (×0,5 bis ×2). Prognose {0} Tag(e), Genauigkeit {1}.",
                "Hype multiplies demand (×0.5 to ×2). Forecast {0} day(s), accuracy {1}.");
            A("Prognose ungenau – Skill „Trendradar“ (Marketing) macht sie genauer und zeigt 3 Tage.",
                "Forecast is fuzzy – the skill “Trend radar” (Marketing) makes it sharper and shows 3 days.");
            A("{0} · nicht online", "{0} · not online");
            A("14 Tage", "14 days");
            A("Prognose: {0}", "Forecast: {0}");

            // Aufträge
            A("Großaufträge gibt es ab Firmenlevel {0}.", "Bulk contracts are available from company level {0}.");
            A("Laufend {0} / {1} · erfüllt {2}. Geliefert wird am Palettenplatz – ganze Kisten oder Einzelstücke.",
                "Active {0} / {1} · fulfilled {2}. Deliver at the pallet spot – whole crates or single items.");
            A("Laufend {0} / {1} · erfüllt {2} · verpasst {3}. Geliefert wird am Palettenplatz – ganze Kisten oder Einzelstücke.",
                "Active {0} / {1} · fulfilled {2} · missed {3}. Deliver at the pallet spot – whole crates or single items.");
            A("Firmen bestellen palettenweise: Angebot annehmen, am Palettenplatz liefern, Frist einhalten.",
                "Companies order by the pallet: accept an offer, deliver at the pallet spot, meet the deadline.");
            A("Sonderkonditionen", "Special terms");
            A("Gerade keine Anfragen", "No requests right now");
            A("Werktags melden sich Firmen – mit höherem Level öfter.", "Companies get in touch on weekdays – more often at higher levels.");
            A("Frist {0} Tage ab Annahme", "Deadline {0} days after accepting");
            A("Strafe {0}", "Penalty {0}");
            A("Bestand {0} (Lager {1})", "Stock {0} (warehouse {1})");
            A("Angebot gilt nur noch heute.", "Offer valid today only.");
            A("Angebot gilt noch bis morgen.", "Offer valid until tomorrow.");
            A("Angebot gilt noch {0} Tage.", "Offer valid for {0} more days.");
            A("Ablehnen", "Decline");
            A("Annehmen", "Accept");
            A("Kein Auftrag läuft", "No contract running");
            A("Nimm unter „Angebote“ einen Auftrag an.", "Accept a contract under “Offers”.");
            A("Zu den Angeboten", "Go to offers");
            A("Frist {0}", "Deadline {0}");
            A("Lager {0}", "Stock {0}");
            A("Lager {0} · {1} unterwegs", "Stock {0} · {1} on the way");
            A("Lager {0} · {1} am Eingang", "Stock {0} · {1} at goods-in");
            A("Lager {0} · {1} unterwegs · {2} am Eingang", "Stock {0} · {1} on the way · {2} at goods-in");
            A("Noch {0} Stück · bei Fristablauf Strafe {1}", "{0} left · penalty {1} if the deadline passes");
            A("Nachbestellen · {0}", "Reorder · {0}");
            A("Ganze Kisten kannst du direkt auf die Palette stellen – ohne Auspacken. Die Lagerist:in bestückt Paletten automatisch aus dem Lager.",
                "You can put whole crates straight onto the pallet – no unpacking. The stocker fills pallets from storage automatically.");
            A("ERFÜLLT", "FULFILLED");
            A("VERPASST", "MISSED");
            A("GLEICHZEITIG", "AT ONCE");
            A("Noch nichts abgeschlossen", "Nothing completed yet");
            A("AUFTRAG", "CONTRACT");
            A("FIRMA", "COMPANY");
            A("STATUS", "STATUS");
            A("AUSGEZAHLT", "PAID OUT");
            A("erfüllt", "fulfilled");
            A("verpasst", "missed");
            A("abgelehnt", "declined");
            A("verfallen", "expired");

            // Retouren
            A("Zurückgeschickte Pakete kommen am Wareneingang an. Am Retourenplatz: als B-Ware einlagern oder entsorgen.",
                "Returned parcels arrive at goods-in. At the returns station: restock as seconds or bin them.");
            A("QUOTE GESAMT", "OVERALL RATE");
            A("AM WARENEINGANG", "AT GOODS-IN");
            A("Am Wareneingang", "At goods-in");
            A("UNTERWEGS", "ON THE WAY");
            A("ERSTATTET", "REFUNDED");
            A("{0} B-Ware · {1} entsorgt", "{0} seconds · {1} binned");
            A("{0} Pakete", "{0} parcels");
            A("Nichts zu tun", "Nothing to do");
            A("Grund: {0}", "Reason: {0}");
            A("keine Angabe", "not stated");
            A("Kommen noch", "Still coming");
            A("Keine Rücksendung unterwegs", "No returns on the way");
            A("Ankunft {0}", "Arrives {0}");
            A("{0} · Ankunft {1}", "{0} · arrives {1}");
            A("Quote je Produkt", "Rate per product");
            A("RETOUREN", "RETURNS");
            A("BISHER", "SO FAR");
            A("ERWARTET", "EXPECTED");
            A("Billig-Ware kommt deutlich öfter zurück, Premium kaum. Zu große Kartons und verspätete Pakete erhöhen die Quote. Skill „Kundenflüsterer“ senkt sie um ein Drittel.",
                "Cheap goods come back much more often, premium hardly ever. Oversized boxes and late parcels push the rate up. The skill “Customer whisperer” cuts it by a third.");
        }

        // =====================================================================================
        // AllesExpress → EverythingXpress (Großhandel)
        // =====================================================================================
        private static void AllesExpress(System.Action<string, string> A)
        {
            A("Hallo! Ich bin Ali, dein Einkaufsberater. Ich kann nichts, bin aber gelb.", "Hi! I'm Ali, your shopping assistant. I can't do anything, but I'm yellow.");
            A("led streifen günstig 1000m", "led strip cheap 1000m");
            A("Suchen", "Search");
            A("247.000 Ergebnisse gefunden. Alle heißen „2026 NEU“.", "247,000 results found. All of them are called “2026 NEW”.");
            A("Wagen ({0})", "Cart ({0})");

            // Kategorien
            A("Super Deals", "Super Deals");
            A("Handy", "Phone");
            A("Deko", "Decor");
            A("Haustier", "Pets");
            A("Gesund", "Health");
            A("Influencer", "Influencer");
            A("Heimkino", "Home cinema");
            A("Technik", "Tech");
            A("Sachen", "Stuff");
            A("Kartons", "Boxes");

            // Gutscheine
            A("ab 10 €", "from 10 €");
            A("ab 29 €", "from 29 €");
            A("Neukunde", "New customer");
            A("Gratis", "Free");
            A("Versand", "Shipping");
            A("Holen", "Grab");
            A("Gutschein gesichert! Gilt ab einem Einkauf von 10 € für Artikel ab 11 €.", "Coupon secured! Valid on orders over 10 € for items over 11 €.");
            A("Gutschein gesichert! Einlösbar am 30. Februar.", "Coupon secured! Redeemable on February 30th.");
            A("Du bist leider kein Neukunde mehr. Seit gerade eben.", "Sorry, you're no longer a new customer. As of just now.");
            A("Versand ist schon gratis. Den Gutschein kannst du einrahmen.", "Shipping is already free. Feel free to frame the coupon.");

            // Deals
            A("SUPER DEALS −97 %", "SUPER DEALS −97 %");
            A("Nur heute!", "Today only!");
            A("Endet in", "Ends in");
            A("Blitzangebote", "Flash deals");
            A("endet in {0}:{1}:{2}", "ends in {0}:{1}:{2}");
            A("ab {0}", "from {0}");
            A("{0} % weg", "{0} % gone");
            A("Heute keine Blitzangebote. Der Blitz ist im Urlaub.", "No flash deals today. Flash is on vacation.");
            A("Mehr für dich", "More for you");
            A("Alle anzeigen", "Show all");
            A("In dieser Kategorie gibt es nichts. Noch nicht mal Staub.", "There's nothing in this category. Not even dust.");
            A("HOT", "HOT");
            A("<s>{0}</s> · {1} verkauft", "<s>{0}</s> · {1} sold");
            A("Im Lager: {0}", "In stock: {0}");
            A("Braucht Lagerhalle", "Needs warehouse");

            // Produktseite
            A("Startseite", "Home");
            A("{0} Bewertungen", "{0} reviews");
            A("· {0} verkauft", "· {0} sold");
            A("pro Stück", "per piece");
            A("0,50 € Rabatt ab 50 Stk.", "0.50 € off from 50 pcs.");
            A("Neukunde −1 €", "New customer −1 €");
            A("Farbe:", "Colour:");
            A("Händler:", "Seller:");
            A("{0} · ab Lv {1}", "{0} · from Lv {1}");
            A("{0} · geschlossen", "{0} · closed");
            A("Menge:", "Quantity:");
            A("{0} Stk", "{0} pcs");
            A("{0} Stk (−{1} %)", "{0} pcs (−{1} %)");
            A("{0} · Lagerhalle", "{0} · warehouse");
            A("Summe:", "Total:");
            A("Express-Lieferung (+{0})", "Express delivery (+{0})");
            A("An: Express-Lieferung (+{0})", "On: Express delivery (+{0})");
            A("Kaufen", "Buy");
            A("Bestellt! Kommt per Lieferwagen.", "Ordered! Coming by delivery van.");
            A("Kartons dazu", "Add boxes");
            A("{0} % positive Bewertungen · seit 2014", "{0} % positive reviews · since 2014");
            A("Lieferung", "Delivery");
            A("~{0} (Express)", "~{0} (express)");
            A("Qualität", "Quality");
            A("naja", "meh");
            A("solide", "solid");
            A("top", "top");
            A("Rückgabe", "Returns");
            A("theoretisch", "theoretically");
            A("Dein Verkaufspreis", "Your sale price");
            A("Marge pro Stück", "Margin per piece");
            A("Im Lager", "In stock");
            A("{0} Stk.", "{0} pcs");
            A("Unterwegs", "On the way");
            A("Beschreibung", "Description");
            A("Bewertungen ({0})", "Reviews ({0})");
            A("Details", "Details");
            A("Marke", "Brand");
            A("NONE", "NONE");
            A("Herkunft", "Origin");
            A("Festland", "Mainland");
            A("Material", "Material");
            A("Ja", "Yes");
            A("Gewicht", "Weight");
            A("ca. leicht", "approx. light");
            A("Zertifikat", "Certificate");
            A("CE (China Export)", "CE (China Export)");
            A("Kartongröße", "Box size");
            A("Erwartete Retourenquote", "Expected return rate");
            A("Trend", "Trend");
            A("{0} ×{1}", "{0} ×{1}");
            A("Marktpreis", "Market price");

            // Bewertungsblock (auch Mein Shop)
            A("{0} Sterne", "{0} stars");
            A("Noch keine Bewertungen. Sei der Erste! (Bitte nicht der Letzte.)", "No reviews yet. Be the first! (Please not the last.)");
            A("· Verifizierter Kauf", "· Verified purchase");
            A("· Farbe: {0} · aus DE", "· Colour: {0} · from DE");
            A("Hilfreich ({0})", "Helpful ({0})");

            // Kartons
            A("Kartons & Verpackung", "Boxes & packaging");
            A("mit deinem Logo bedruckt", "printed with your logo");
            A("Mit deinem Logo. Ungefaltet = halber Preis.", "With your logo. Flat = half price.");

            // Meine Bestellungen
            A("Meine Bestellungen", "My orders");
            A("{0} unterwegs", "{0} on the way");
            A("Gerade ist nichts unterwegs.", "Nothing on the way right now.");
            A("Weiter shoppen", "Keep shopping");
            A("{0}× {1} ({2})", "{0}× {1} ({2})");
            A("Eigener Wagen", "Own van");
            A("in {0}", "in {0}");
            A("Am Wareneingang warten {0} Stück aufs Einräumen.", "{0} items are waiting at goods-in to be put away.");

            // Kaufsperren
            A("Noch nicht kaufbar: Ab Level {0}.", "Not buyable yet: from level {0}.");
            A("Noch nicht kaufbar: Braucht Lagerhalle.", "Not buyable yet: needs warehouse.");
            A("Dieser Händler liefert gerade nicht.", "This seller isn't delivering right now.");
            A("Diese Menge ist noch gesperrt.", "This quantity is still locked.");
            A("Premium-Hersteller: heute schon bestellt (1× pro Produkt und Tag).", "Premium manufacturer: already ordered today (1× per product per day).");
            A("Zu teuer – dir fehlen {0}. (Revoluut hätte da was …)", "Too expensive – you're {0} short. (Revoluut might have something …)");

            // Händlernamen je Lieferant
            A("{0} Outlet (B-Ware?)", "{0} Outlet (seconds?)");
            A("{0} Manufaktur", "{0} Manufactory");
        }

        // =====================================================================================
        // Fake-Produkttexte (ProductFlavor): Titel, Beschreibung, Varianten, Bewertungen
        // =====================================================================================
        private static void ProductFlavors(System.Action<string, string> A)
        {
            // Handyhülle
            A("2026 NEU Luxus Marmor Handyhülle Stoßfest Silikon für iPhone Samsung Xiaomi Alle Modelle Frau Mann Geschenk Top",
                "2026 NEW Luxury Marble Phone Case Shockproof Silicone for iPhone Samsung Xiaomi All Models Woman Man Gift Top");
            A("Hochwertige Premium-Marmor-Optik aus echtem Kunststoff. Schützt Ihr Telefon vor Sturz aus bis zu 2 cm Höhe. Passt auf fast alle Modelle (bitte Größe vorher messen). Farbe kann abweichen durch Bildschirm, Licht und Laune.",
                "High-quality premium marble look made of genuine plastic. Protects your phone from drops of up to 2 cm. Fits almost all models (please measure first). Colour may vary due to screen, lighting and mood.");
            A("Marmor weiß", "Marble white");
            A("Marmor schwarz", "Marble black");
            A("Marmor „Gold“", "Marble “gold”");
            A("Passt perfekt auf Samsung. Ist aber iPhone-Hülle. Trotzdem 5 Sterne weil schnell.", "Fits my Samsung perfectly. But it's an iPhone case. Still 5 stars because fast.");
            A("Marmor sieht aus wie Marmor auf Foto von Marmor. Okay für Preis.", "Marble looks like marble in a photo of marble. Okay for the price.");
            A("Kam nach 61 Tagen. Handy inzwischen verloren.", "Arrived after 61 days. Lost the phone in the meantime.");

            // LED
            A("LED Lichterkette 5m 10m 20m RGB Bluetooth App Musik Sync Gaming Zimmer TikTok Licht Deko Wasserdicht (nicht wasserdicht)",
                "LED String Lights 5m 10m 20m RGB Bluetooth App Music Sync Gaming Room TikTok Light Decor Waterproof (not waterproof)");
            A("Verwandelt jedes Zimmer in ein Gaming-Paradies oder eine Zahnarztpraxis. 16 Millionen Farben (davon 7 unterschiedlich). Steuerung per App „LEDlight Pro Max“ (braucht Standort, Kontakte und Kamera).",
                "Turns any room into a gaming paradise or a dentist's office. 16 million colours (7 of them different). Controlled via the “LEDlight Pro Max” app (needs location, contacts and camera).");
            A("20 m (klebt nicht)", "20 m (doesn't stick)");
            A("Mein Zimmer sieht jetzt aus wie ein Aquarium. Freundin weg. 5 Sterne.", "My room looks like an aquarium now. Girlfriend left. 5 stars.");
            A("Kleber hält genau eine Nacht. Morgens alles auf dem Boden.", "Adhesive holds for exactly one night. Everything on the floor by morning.");
            A("Musik-Sync funktioniert, aber nur bei Schlager.", "Music sync works, but only with polka.");

            // Massagepistole
            A("Massagepistole Profi Tiefengewebe 30 Stufen Muskel Faszien Gun Sport Fitness Rücken Nacken Leise Akku Geschenk Mann",
                "Massage Gun Pro Deep Tissue 30 Levels Muscle Fascia Gun Sport Fitness Back Neck Quiet Battery Gift Man");
            A("Löst Verspannungen, Knoten und gelegentlich Zahnfüllungen. 30 Stufen: von „Streicheln“ bis „Presslufthammer“. Leise wie ein Rasenmäher mit Schalldämpfer.",
                "Loosens tension, knots and occasionally dental fillings. 30 levels: from “stroke” to “jackhammer”. Quiet as a lawnmower with a silencer.");
            A("Schwarz", "Black");
            A("Titan-Optik", "Titanium look");
            A("Rosa (stärker)", "Pink (stronger)");
            A("Nacken wieder locker. Nachbarn nicht mehr.", "Neck is relaxed again. The neighbours aren't.");
            A("Stufe 30 hat meinen Mann einmal durchs Wohnzimmer geschoben.", "Level 30 pushed my husband across the living room once.");
            A("Akku hält 4 Minuten. Mein Training auch.", "Battery lasts 4 minutes. So does my workout.");

            // Kopfhörer
            A("Bluetooth Kopfhörer In-Ear Kabellos 5.3 HiFi Bass Noise Cancelling Sport Wasserdicht Mikrofon für iPhone Android Pro Max",
                "Bluetooth Headphones In-Ear Wireless 5.3 HiFi Bass Noise Cancelling Sport Waterproof Microphone for iPhone Android Pro Max");
            A("Kristallklarer Klang* und tiefer Bass**. *im Vergleich zu keinem Kopfhörer. **laut Verpackung. Noise Cancelling blendet alles aus, außer Rauschen.",
                "Crystal-clear sound* and deep bass**. *compared to no headphones. **according to the box. Noise cancelling blocks out everything except noise.");
            A("Weiß", "White");
            A("Links (einzeln)", "Left (single)");
            A("Klang gut. Einer verbindet sich immer mit dem Fernseher vom Nachbarn.", "Sound is good. One of them always connects to the neighbour's TV.");
            A("Sehen aus wie die teuren. Hören sich an wie die günstigen. Fair.", "Look like the expensive ones. Sound like the cheap ones. Fair.");
            A("Wo steckt man das Kabel rein?", "Where do you plug the cable in?");

            // Ringlicht
            A("Ringlicht 26cm mit Stativ Handyhalter Selfie TikTok Influencer Make Up Video LED Dimmbar Streaming Zoom Beauty",
                "Ring Light 26cm with Tripod Phone Holder Selfie TikTok Influencer Make Up Video LED Dimmable Streaming Zoom Beauty");
            A("Werde in 24 Stunden Influencer. Ergebnis nicht garantiert. Stativ bis 1,60 m (wackelt ab 1,20 m).",
                "Become an influencer in 24 hours. Results not guaranteed. Tripod up to 1.60 m (wobbles from 1.20 m).");
            A("Sehe aus wie ein Engel, Zimmer sieht aus wie Verhörraum.", "I look like an angel, my room looks like an interrogation room.");
            A("Stativ kippt, Ring hält.", "Tripod tips over, ring holds.");

            // Katzenbrunnen
            A("Katzen Trinkbrunnen 2,5L Leise Automatisch Filter LED Hund Haustier Wasserspender Smart App Edelstahl Optik",
                "Cat Water Fountain 2.5L Quiet Automatic Filter LED Dog Pet Water Dispenser Smart App Stainless Steel Look");
            A("Fließendes Wasser regt Ihre Katze zum Trinken an. Wissenschaftlich getestet an einer Katze. Flüsterleise (38 dB, entspricht einem kleinen Rasenmäher im Nebenzimmer).",
                "Running water encourages your cat to drink. Scientifically tested on one cat. Whisper-quiet (38 dB, roughly a small lawnmower in the next room).");
            A("Blau", "Blue");
            A("Rosa", "Pink");
            A("Katze trinkt weiterhin aus der Toilette.", "Cat still drinks out of the toilet.");
            A("Meine Katze liebt es. Spielt damit. Trinkt nicht.", "My cat loves it. Plays with it. Doesn't drink.");
            A("Leise ist relativ.", "Quiet is relative.");

            // Haltungskorrektor
            A("Haltungskorrektur Rücken Geradehalter Schulter Gurt Büro Homeoffice Unsichtbar Unter Kleidung Damen Herren Verstellbar",
                "Posture Corrector Back Straightener Shoulder Strap Office Home Office Invisible Under Clothes Women Men Adjustable");
            A("Richtet Ihre Haltung in nur 3 Tagen auf, oder Ihre Laune ab. Unsichtbar unter der Kleidung, außer man sieht es.",
                "Straightens your posture in just 3 days, or wrecks your mood. Invisible under clothes, unless you can see it.");
            A("XL (wie M)", "XL (same as M)");
            A("Sitze gerader. Atme weniger.", "Sitting straighter. Breathing less.");
            A("Größe L ist Kinder-L.", "Size L is kids' L.");

            // Smartwatch
            A("Smartwatch Herren Damen 2026 Fitness Tracker Anrufe Puls Blutdruck Schlaf Schritte Wasserdicht IP68 für Android iOS Ultra",
                "Smartwatch Men Women 2026 Fitness Tracker Calls Pulse Blood Pressure Sleep Steps Waterproof IP68 for Android iOS Ultra");
            A("Misst Puls, Schritte, Schlaf und Blutdruck (ungefähr, ungefähr, ungefähr und gar nicht). Akkulaufzeit: 7 Tage im Flugmodus in der Schublade.",
                "Measures pulse, steps, sleep and blood pressure (roughly, roughly, roughly and not at all). Battery life: 7 days in flight mode in a drawer.");
            A("Silber", "Silver");
            A("Rosé", "Rosé");
            A("Sagt mir jede Stunde, ich soll aufstehen. Wie meine Frau.", "Tells me to get up every hour. Like my wife.");
            A("Hab laut Uhr im Schlaf 12.000 Schritte gemacht. Beunruhigend.", "According to the watch I walked 12,000 steps in my sleep. Unsettling.");

            // Beamer
            A("Mini Beamer 4K Full HD 1080P Tragbar Heimkino Projektor WLAN Handy 9500 Lumen (ca.) Outdoor Kino Netflix",
                "Mini Projector 4K Full HD 1080P Portable Home Cinema Projector WiFi Phone 9500 Lumens (approx.) Outdoor Cinema Netflix");
            A("Echtes 4K* Kinoerlebnis zu Hause. *Unterstützt 4K-Dateien, zeigt sie in 480p. Helligkeit: sehr hell bei komplett dunklem Raum mit geschlossenen Augen.",
                "Real 4K* cinema experience at home. *Supports 4K files, shows them in 480p. Brightness: very bright in a completely dark room with your eyes closed.");
            A("Für den Preis okay. Film nur im Keller schaubar.", "Okay for the price. Movies only watchable in the basement.");
            A("9500 Lumen? Meine Kerze ist heller.", "9500 lumens? My candle is brighter.");
            A("Enkel hat eingestellt, läuft.", "Grandson set it up, works.");

            // Drohne
            A("Kamera Drohne 4K GPS Faltbar Profi Anfänger Kinder Quadrocopter 60 Min Flugzeit (Summe aller Akkus) Follow Me",
                "Camera Drone 4K GPS Foldable Pro Beginner Kids Quadcopter 60 Min Flight Time (all batteries combined) Follow Me");
            A("Filmt atemberaubende Luftaufnahmen – bevorzugt von Baumkronen, in denen sie hängen bleibt. „Follow Me“-Modus folgt Ihnen, Ihrem Hund oder einer Taube.",
                "Films breathtaking aerial shots – preferably of treetops it gets stuck in. “Follow Me” mode follows you, your dog or a pigeon.");
            A("1 Akku", "1 battery");
            A("3 Akkus", "3 batteries");
            A("Mit Ersatzpropellern (dringend)", "With spare propellers (urgent)");
            A("Tolle Bilder von unserem Dach. Drohne wohnt jetzt dort.", "Great pictures of our roof. Drone lives there now.");
            A("Follow-Me folgt lieber dem Nachbarn.", "Follow-Me prefers following the neighbour.");
            A("Der Hund hat Angst, ich habe Spaß.", "The dog is scared, I'm having fun.");

            // Ersatz für Produkte ohne eigene Texte
            A("2026 NEU {0} Premium Top Qualität Geschenk Alle Modelle", "2026 NEW {0} Premium Top Quality Gift All Models");
            A("Ein {0}. Wie auf dem Bild. Ungefähr.", "A {0}. As pictured. Roughly.");
            A("Global Everything Store", "Global Everything Store");
            A("Standard", "Standard");
            A("ist angekommen", "has arrived");

            // Namen der Rezensenten
            A("Oma Gerda", "Grandma Gerda");
        }

        // =====================================================================================
        // Mein Shop (shopifly) – Admin und Shop-Ansicht
        // =====================================================================================
        private static void Shop(System.Action<string, string> A)
        {
            A("Produkte", "Products");
            A("Preise", "Prices");
            A("Shop ansehen", "View shop");
            A("Hallo, {0}!", "Hello, {0}!");
            A("Umsatz heute", "Revenue today");
            A("Verschickt", "Shipped");
            A("Offen", "Open");
            A("Bestell./Std.", "Orders/hr");
            A("{0} Bew.", "{0} reviews");
            A("Shop offline · noch {0}", "Shop offline · {0} left");
            A("Rabatt −{0} % läuft", "Discount −{0} % running");
            A("Rabatt −{0} %", "Discount −{0} %");
            A("Anzeige schalten", "Place ad");
            A("{0} aktiv", "{0} active");
            A("Produkt", "Product");
            A("Status", "Status");
            A("Lager", "Stock");
            A("Preis", "Price");
            A("Nachfrage", "Demand");
            A("Gesperrt", "Locked");
            A("Entwurf", "Draft");
            A("Vorschlag aus deiner Marke", "Suggested by your brand");
            A("Vorschlag: knapp unter Markt", "Suggestion: just under market");
            A("Alle −5 %", "All −5 %");
            A("Alle +5 %", "All +5 %");
            A("Alle auf Vorschlag", "All to suggestion");
            A("Markt {0}", "Market {0}");
            A("+{0}/Stk", "+{0}/pc");
            A("Vorschlag {0}", "Suggestion {0}");

            // Shop-Ansicht
            A("Gratis Versand ab 30 € · {0} Sterne von {1} Kunden · Gerade im Trend: {2} · Gratis Versand ab 30 €",
                "Free shipping over 30 € · {0} stars from {1} customers · Trending now: {2} · Free shipping over 30 €");
            A("Gratis Versand ab 30 € · {0} Sterne von {1} Kunden · Neu: Dinge, die du nicht brauchst · Gratis Versand ab 30 €",
                "Free shipping over 30 € · {0} stars from {1} customers · New: things you don't need · Free shipping over 30 €");
            A("Zurück zum Admin", "Back to admin");
            A("Dinge, die du nicht brauchst. Schnell geliefert.", "Things you don't need. Delivered fast.");
            A("Jetzt stöbern", "Browse now");
            A("Kunden stöbern. Du packst.", "Customers browse. You pack.");
            A("Beliebt gerade", "Popular right now");
            A("{0} online", "{0} online");
            A("Das sagen deine Kunden", "What your customers say");
            A("Noch keine Bewertungen. Verschick deine ersten Pakete – schnell und in guter Qualität.", "No reviews yet. Ship your first parcels – fast and in good quality.");
            A("· {0} · Tag {1}", "· {0} · Day {1}");
            A("„{0}“", "“{0}”");
            A("HYPE", "HYPE");
            A("NEU", "NEW");
            A("OFFLINE", "OFFLINE");
            A("{0} Edition", "{0} Edition");
            A("Lager {0} · {1}", "Stock {0} · {1}");

            // Produktseite
            A("AKTIV", "ACTIVE");
            A("ENTWURF", "DRAFT");
            A("{0} – {1} Edition", "{0} – {1} Edition");
            A("· Bestseller in Kram", "· Bestseller in Stuff");
            A("Rabattaktion aktiv", "Sale active");
            A("Gratis Versand ab 30 €", "Free shipping over 30 €");
            A("Verwaltung (nur für dich)", "Management (just for you)");
            A("Braucht die Lagerhalle (mehr Regalplatz).", "Needs the warehouse (more shelf space).");
            A("Online – ausschalten", "Online – switch off");
            A("Offline – einschalten", "Offline – switch on");
            A("Kunden können bestellen.", "Customers can order.");
            A("Niemand sieht das Produkt.", "Nobody can see the product.");
            A("Marktpreis ~{0} · Nachfrage: {1}", "Market price ~{0} · demand: {1}");
            A("Markenpreis ({0})", "Brand price ({0})");
            A("Markenstärke {0} % → bis {1} ohne Kundenverlust", "Brand strength {0} % → up to {1} without losing customers");
            A("Kunden bestellen immer genau 1 Stück. Weil sie es können.", "Customers always order exactly 1 piece. Because they can.");
            A("Nur noch {0} auf Lager (es sind {1})", "Only {0} left in stock (there are {1})");
            A("In den Warenkorb", "Add to cart");
            A("Du kannst nicht bei dir selbst bestellen. Steuerberater sagt nein.", "You can't order from yourself. Your tax advisor says no.");
            A("Nachkaufen bei AllesExpress", "Restock at EverythingXpress");
            A("Deine Marke · Level {0}", "Your brand · level {0}");
            A("{0} Bestellungen", "{0} orders");
            A("Verkauft gesamt", "Sold in total");
            A("Einkauf ab", "Cost from");
            A("Gewinn/Stk. ~", "Profit/pc ~");
            A("Retourenquote", "Return rate");
            A("Trends ansehen", "View trends");
            A("Das Original von {0}. Handverlesen aus dem Lager, liebevoll verpackt in einem Karton mit unserem Logo. {1}.",
                "The original from {0}. Hand-picked from the warehouse, lovingly packed in a box with our logo. {1}.");
            A("Versand am selben Tag, wenn der Chef nicht gerade TikToks dreht.", "Same-day shipping, unless the boss is busy shooting TikToks.");
            A("PaketBlitz Standard / Express", "ParcelBolt Standard / Express");
            A("Lagerqualität", "Stock quality");
        }

        // =====================================================================================
        // iWolke Mail → iCloudy Mail
        // =====================================================================================
        private static void Mail(System.Action<string, string> A)
        {
            A("Posteingang", "Inbox");
            A("Entscheidungen", "Decisions");
            A("Ungelesen", "Unread");
            A("Lieferanten", "Suppliers");
            A("Gesendet", "Sent");
            A("Werbung", "Ads");
            A("Tag {0}", "Day {0}");
            A("[!] {0}", "[!] {0}");
            A("Leer!", "Empty!");
            A("Keine Mail ausgewählt", "No mail selected");
            A("an: info@{0}.de · Tag {1}, {2} Uhr", "to: info@{0}.com · day {1}, {2}");
            A("Deine Entscheidung:", "Your decision:");
            A("{0} · {1} · Minispiel", "{0} · {1} · minigame");
            A("{0} · Minispiel", "{0} · minigame");
            A("20 Uhr: automatisch die vorsichtigste Antwort.", "8 PM: the most cautious answer is picked automatically.");
            A("ERGEBNIS", "RESULT");
            A("Du hast gewählt: {0}", "You chose: {0}");
            A("Antworten", "Reply");
            A("Antwort geschrieben, gelöscht, neu geschrieben, nicht gesendet. Wie immer.", "Reply written, deleted, rewritten, not sent. As always.");
            A("Weiterleiten", "Forward");
            A("An wen denn? Du hast keine Kollegen. Noch.", "To whom? You don't have colleagues. Yet.");
            A("38 Mails von „Mega Vision Official“. Alle mit „Dear friend!!!“.", "38 mails from “Mega Vision Official”. All starting with “Dear friend!!!”.");
            A("Leer. Wie der Kühlschrank.", "Empty. Like the fridge.");
        }

        // =====================================================================================
        // PaketBlitz → ParcelBolt
        // =====================================================================================
        private static void Paket(System.Action<string, string> A)
        {
            A("Bestellt", "Ordered");
            A("Entnommen", "Picked");
            A("Verpackt", "Packed");
            A("Label drauf", "Labelled");
            A("Auf dem Band", "On the belt");
            A("Senden", "Send");
            A("Du sendest schon. Ständig.", "You're already sending. Constantly.");
            A("Verfolgen", "Track");
            A("Abholung", "Pickup");
            A("Abholung täglich um 20:00 Uhr. Der Fahrer hupt einmal und fährt dann.", "Pickup daily at 20:00. The driver honks once and leaves.");
            A("Wareneingang", "Goods-in");
            A("Kundennr. {0}-{1}", "Customer no. {0}-{1}");
            A("Sendung verfolgen", "Track shipment");
            A("Gefunden! Sie ist da, wo sie ist.", "Found it! It's where it is.");
            A("Keine Sendung. Keine Sorgen.", "No shipment. No worries.");
            A("Nichts zu verschicken.", "Nothing to ship.");
            A("Auf dem Band. Abholung 20:00.", "On the belt. Pickup 20:00.");
            A("EXPRESS – schnell packen!", "EXPRESS – pack it fast!");
            A("Zustellung morgen.", "Delivery tomorrow.");
            A("{0} · {1} an {2} · {3}", "{0} · {1} to {2} · {3}");
            A("{0} · {1} an {2} in {3} · {4}", "{0} · {1} to {2} in {3} · {4}");
            A("Kundschaft", "Customer");
            A("Heute versenden", "Ship today");
            A("· Abholung 20:00 · noch {0}", "· Pickup 20:00 · {0} left");
            A("Express", "Express");
            A("Zu spät", "Late");
            A("Storniert", "Cancelled");
            A("Keine offenen Pakete.", "No open parcels.");
            A("EXPRESS", "EXPRESS");
            A("Ansehen", "View");
            A("+{0} weitere (Stapel wird höher)", "+{0} more (the pile is growing)");
            A("Kein Label", "No label");
            A("Kommt mit der nächsten Bestellung.", "Comes with the next order.");
            A("· am Tor: {0} Stk", "· at the gate: {0} pcs");
            A("· am Tor: {0} Stk · Retouren: {1}", "· at the gate: {0} pcs · returns: {1}");
            A("Nichts unterwegs.", "Nothing on the way.");
            A("LKW", "TRUCK");
            A("Spedition", "Freight");
            A("Retouren bearbeiten", "Process returns");
            A("PaketBlitz {0}", "ParcelBolt {0}");
            A("AN:", "TO:");
            A("Irgendwo in DE", "Somewhere in DE");
            A("Hinweis: {0}", "Note: {0}");
            A("Zechenstraße {0}", "Colliery Street {0}");
            A("Am Kanal {0}", "Canal Side {0}");
            A("Hauptstraße {0}", "High Street {0}");
            A("Lindenweg {0}", "Linden Lane {0}");
            A("Bahnhofstr. {0}", "Station Rd {0}");
            A("Kirchplatz {0}", "Church Square {0}");
            A("Im Winkel {0}", "The Nook {0}");
        }

        // =====================================================================================
        // TradingViech → TradingBeast
        // =====================================================================================
        private static void Trading(System.Action<string, string> A)
        {
            A("1T", "1D");
            A("ALLES", "ALL");
            A("Das ist garantiert der Boden.", "This is definitely the bottom.");
            A("Linie geht hoch = gut. Glaube ich.", "Line goes up = good. I think.");
            A("Onkel Ralf sagt: jetzt kaufen.", "Uncle Ralf says: buy now.");
            A("Nicht Finanzberatung. Eher Finanzraten.", "Not financial advice. More like financial guessing.");
            A("Der Kurs fühlt sich heute nach Tee an.", "The chart feels like tea today.");
            A("Buy high, sell low. Oder umgekehrt?", "Buy high, sell low. Or the other way round?");
            A("{0} · {1} % Gebühr pro Kauf und Verkauf.", "{0} · {1} % fee per buy and sell.");
            A("Beobachtungsliste", "Watchlist");
            A("Verkaufen", "Sell");
            A("Alles", "All");
            A("Verkauft für {0}.", "Sold for {0}.");
            A("Depot: {0}", "Portfolio: {0}");
            A("Konto: {0}", "Balance: {0}");
            A("Bestand {0}: {1} Anteile · {2}", "Holding {0}: {1} shares · {2}");
            A("G/V: {0}", "P/L: {0}");
        }

        // =====================================================================================
        // Revoluut (schon englisch – nur deutsche Reste)
        // =====================================================================================
        private static void Bank(System.Action<string, string> A)
        {
            A("PaketBlitz boxes", "ParcelBolt boxes");
            A("Open TradingViech", "Open TradingBeast");
            A("TradingViech: {0}", "TradingBeast: {0}");
            A("{0} (dream)", "{0} (dream)");
            A("Rent ({0})", "Rent ({0})");
        }

        // =====================================================================================
        // TikTak (Creator-Studio)
        // =====================================================================================
        private static void TikTak(System.Action<string, string> A)
        {
            A("wo gibt's das?? link???", "where do i get this?? link???");
            A("meins kam nach 61 tagen lol", "mine came after 61 days lol");
            A("Wie kann man hier bestellen? Mit Überweisung?", "How do I order here? By bank transfer?");
            A("bro das gibts auf allesexpress für 0,89", "bro this is on everythingxpress for 0.89");
            A("gekauft. danke tiktak", "bought. thanks tiktak");
            A("Für dich", "For You");
            A("Video aufnehmen", "Record video");
            A("Analysen", "Analytics");
            A("Werbung (Ads)", "Advertising (ads)");
            A("+ Video aufnehmen ({0}/{1} heute)", "+ Record video ({0}/{1} today)");
            A("Noch keine Videos", "No videos yet");
            A("Hier landen deine echten Aufnahmen. Produkt wählen, Laptop zu, Handy hoch – und los.", "Your real recordings end up here. Pick a product, close the laptop, phone up – go.");
            A("Mein Video", "My video");
            A("{0} · Tag {1} · Treffer {2} % #fyp #{3}", "{0} · Day {1} · score {2} % #fyp #{3}");
            A("{0} Aufrufe · Video {1}/{2}", "{0} views · video {1}/{2}");
            A("„wo gibt's das?? link???“ – {0} Mal.", "“where do i get this?? link???” – {0} times.");
            A("Teilen", "Share");
            A("Link kopiert. An wen? An Mama.", "Link copied. To whom? To Mom.");
            A("Nächstes", "Next");
            A("Dein Trend läuft noch! Genieß die Bestellungen.", "Your trend is still running! Enjoy the orders.");
            A("Format", "Format");
            A("{0} · {1} im Lager", "{0} · {1} in stock");
            A("{0} · {1} im Lager · Hype ×{2}", "{0} · {1} in stock · hype ×{2}");
            A("{0} · in der Hand", "{0} · in hand");
            A("{0} · in der Hand · Hype ×{1}", "{0} · in hand · hype ×{1}");
            A("{0} · TREND", "{0} · TREND");
            A("Der Laptop klappt zu, das Produkt steht vor dir und du filmst 10–20 Sekunden in Ego-Sicht. {0} = Aktion (gleich am Anfang = Hook), {1} = Stopp, {2} = Abbrechen. Bewertet werden Bildmitte, Abstand, ruhige Kamera, Licht, Blickwinkel, Länge, Aktionen und Deko im Bild. Trend-Format heute: {3} (+{4} %).",
                "The laptop closes, the product is in front of you and you film 10–20 seconds in first person. {0} = action (right at the start = hook), {1} = stop, {2} = cancel. Rated: framing, distance, steady camera, light, angle, length, actions and decor in shot. Trending format today: {3} (+{4} %).");
            A("Noch {0} von {1} heute. Produkte mit Hype wirken stärker. Tipp: Das Ringlicht am Schreibtisch startet die Aufnahme auch.",
                "{0} of {1} left today. Hyped products hit harder. Tip: the ring light on your desk starts a recording too.");
            A("Entwurf von vorhin", "Draft from earlier");
            A("Deine letzte Aufnahme wartet noch. Posten oder neu aufnehmen.", "Your last recording is still waiting. Post it or record again.");
            A("Entwurf posten", "Post draft");
            A("Verwerfen", "Discard");
            A("Aufrufe (letzte {0} Videos)", "Views (last {0} videos)");
            A("Follower", "Followers");
            A("Bekanntheit {0}", "Awareness {0}");
            A("Reichweite", "Reach");
            A("{0} Boost(s) aktiv", "{0} boost(s) active");
            A("Videos heute", "Videos today");
            A("bereit", "ready");
            A("Pause", "Pause");
            A("Dein bestes Video", "Your best video");
            A("{0} Aufrufe · Treffer {1} % · Tag {2}", "{0} views · score {1} % · day {2}");
            A("Aktive Boosts", "Active boosts");
            A("Nichts aktiv. Der Algorithmus hat dich vergessen.", "Nothing active. The algorithm forgot about you.");
            A("{0} ×{1} · noch {2}", "{0} ×{1} · {2} left");
            A("Werbekampagnen", "Ad campaigns");
            A("×{0} Nachfrage · {1} · +Bekanntheit", "×{0} demand · {1} · +awareness");
            A("Läuft …", "Running …");
            A("Starten · {0}", "Start · {0}");
            A("Kommentare", "Comments");
            A("vor {0} Std. · Antworten", "{0} h ago · Reply");
            A("Trend-Ideen für heute", "Trend ideas for today");
            A("„Things TikTak made me buy“", "“Things TikTak made me buy”");
            A("{0} · Hype ×{1}", "{0} · hype ×{1}");
            A("Dafür brauchst du {0} im Lager.", "You need {0} in stock for that.");
            A("Katzen reagieren auf Dinge", "Cats react to things");
            A("Geht immer · kein Hype gerade", "Always works · no hype right now");
            A("„Wie ich mit 19 meine Firma …“", "“How I started my company at 19 …”");
            A("Riskant: 30 % Cringe-Chance", "Risky: 30 % cringe chance");
            A("Kommentare: „bro ist 34“.", "Comments: “bro is 34”.");
        }

        // =====================================================================================
        // Marketing-Bausteine (Fakebook/Gugel/Handy): Leute, Posts, Kommentare, Fake-Profile
        // =====================================================================================
        private static void Marketing(System.Action<string, string> A)
        {
            A("Uwe aus Bottrop", "Uwe from Bottrop");
            A("Tante Gabi", "Aunt Gabi");
            A("Fitness-Ben", "Fitness Ben");
            A("Mia Katzenlieb", "Mia Catlove");
            A("Montag. Wieder. Wer hat das erlaubt?", "Monday. Again. Who allowed this?");
            A("Hab heute 10.000 Schritte gemacht. Zum Kühlschrank und zurück.", "Did 10,000 steps today. To the fridge and back.");
            A("Teilen, wenn du auch findest, dass früher alles besser war!!", "Share if you also think everything was better in the old days!!");
            A("Suche Handwerker. Bitte kein Kevin.", "Looking for a handyman. No Kevins please.");
            A("Mein Kuchen. 3 Stunden. Sieht aus wie ein Stein. Schmeckt auch so.", "My cake. 3 hours. Looks like a rock. Tastes like one too.");
            A("Wer kennt den Ort? Bitte keine falschen Antworten.", "Who knows this place? No wrong answers please.");
            A("Urlaub! (Balkon)", "Vacation! (balcony)");
            A("Sonnenuntergang. Gefiltert. 47 Mal.", "Sunset. Filtered. 47 times.");
            A("Bin jetzt Coach. Für was, weiß ich noch nicht.", "I'm a coach now. For what, I don't know yet.");
            A("Achtung! Neuer Trick von Betrügern. Bitte an alle weiterleiten!!!", "Warning! New scam going around. Please forward to everyone!!!");
            A("Hat jemand mein Fahrrad gesehen? Blau. Oder grün.", "Has anyone seen my bike? Blue. Or green.");
            A("Heute ist Weltkatzentag. Wie jeden Tag.", "Today is World Cat Day. Like every day.");
            A("Ist das seriös?", "Is this legit?");
            A("Hab ich gekauft. Kam an! Glaub ich.", "Bought it. It arrived! I think.");
            A("Wo ist der Haken?", "What's the catch?");
            A("@Sven guck mal, das brauchst du", "@Sven look, you need this");
            A("Zu teuer. Kauf ich trotzdem.", "Too expensive. Buying it anyway.");
            A("Gesehen auf TikTak!", "Saw it on TikTak!");
            A("Mein Nachbar hat das auch.", "My neighbour has this too.");
            A("Fake.", "Fake.");
            A("Bestes Produkt EVER!!! 10/10", "Best product EVER!!! 10/10");
            A("Hat mein Leben verändert. Echt jetzt.", "Changed my life. For real.");
            A("Kaufe ich schon zum dritten Mal!", "Buying this for the third time!");
            A("Super Shop, super schnell, super echt!", "Super shop, super fast, super real!");
            A("Ich bin ein ganz normaler Mensch und finde das toll.", "I am a completely normal human and I think this is great.");
            A("Meine ganze Familie liebt es (alle 4 Profile).", "My whole family loves it (all 4 profiles).");

            // Fake-Profile (Namen aus Ads.Content)
            A("Sandra Echtmensch", "Sandra Realhuman");
            A("Klaus Nichtbot", "Klaus Notabot");
            A("Echte Kundin 1987", "Real Customer 1987");
            A("Bernd Zufrieden", "Bernd Satisfied");
            A("Mandy Glücklich", "Mandy Happy");
            A("Gisela Kaufgern", "Gisela Lovestobuy");
            A("Jens Unbezahlt", "Jens Unpaid");
            A("Lisa Total-Echt", "Lisa Totally-Real");
            A("Horst Fünfsterne", "Horst Fivestars");
            A("Chantal Begeistert", "Chantal Thrilled");
            A("Uwe Verifiziert", "Uwe Verified");

            // Handy-Versionen von Fakebook und Gugel
            A("Fakeb.", "Fakeb.");
            A("Keine Anzeige aktiv.", "No ad running.");
            A("{0} · Platz {1}  ×{2}", "{0} · slot {1}  ×{2}");
            A("{0}  ×{1}", "{0}  ×{1}");
            A("{0} Views · {1} Klicks · {2} Verk.", "{0} views · {1} clicks · {2} sold");
            A("ANZEIGEN", "ADS");
            A("Schnell-Ad {0} · 50 €", "Quick ad {0} · 50 €");
            A("FAKE-PROFILE", "FAKE PROFILES");
            A("{0} / {1} · Risiko {2}", "{0} / {1} · risk {2}");
            A("+{0} €", "+{0} €");
            A("Platz 1: „{0}“ · 60 €", "Slot 1: “{0}” · 60 €");
            A("KI-SEITEN", "AI SITES");
            A("Noch keine. Am Laptop bei Gugel erstellen.", "None yet. Create them in Gugel on the laptop.");
        }

        // =====================================================================================
        // Fakebook
        // =====================================================================================
        private static void Fakebook(System.Action<string, string> A)
        {
            A("Suche auf Fakebook", "Search Fakebook");
            A("Feed", "Feed");
            A("Fake-Profile ({0})", "Fake profiles ({0})");
            A("Was verkaufst du heute, {0}?", "What are you selling today, {0}?");
            A("Deine Anzeigen", "Your ads");
            A("Keine aktiv.", "None active.");
            A("+ Anzeige", "+ Ad");
            A("{0} Std. · Öffentlich", "{0} h · Public");
            A("{0} Kommentare", "{0} comments");
            A("Gesponsert · {0}", "Sponsored · {0}");
            A("ANGEBOT", "OFFER");
            A("{0}.shopifly.de", "{0}.shopifly.com");
            A("Jetzt kaufen", "Buy now");
            A("{0} Views · {1} Klicks · {2} Verk. · {3}/{4}", "{0} views · {1} clicks · {2} sold · {3}/{4}");
            A("Werbung ab Level {0}", "Ads from level {0}");
            A("Fake-Profile gehen schon.", "Fake profiles already work.");
            A("Erst ein Produkt online stellen.", "Put a product online first.");
            A("Neue Anzeige", "New ad");
            A("Zielgruppe", "Audience");
            A("Slogan", "Slogan");
            A("Budget", "Budget");
            A("{0}  [Clickbait]", "{0}  [clickbait]");
            A("Views", "Views");
            A("Klicks", "Clicks");
            A("Laufzeit", "Duration");
            A("Schalten · {0}", "Launch · {0}");
            A("Kampagnen", "Campaigns");
            A("Noch keine.", "None yet.");
            A("{0}  ×{1}  · beendet", "{0}  ×{1}  · ended");
            A("Stopp", "Stop");
            A("Fake-Profile", "Fake profiles");
            A("+ Profil · {0}", "+ Profile · {0}");
            A("Alle löschen", "Delete all");
            A("Profile", "Profiles");
            A("Social Proof", "Social proof");
            A("Risiko/Tag", "Risk/day");
            A("wenn erwischt", "if caught");
            A("Niemand lobt dich. Noch.", "Nobody's praising you. Yet.");
        }

        // =====================================================================================
        // Gugel (Suche, Ads, KI-Seiten)
        // =====================================================================================
        private static void Gugel(System.Action<string, string> A)
        {
            A("Suche", "Search");
            A("Ads", "Ads");
            A("KI-Seiten", "AI sites");
            A("Was willst du verkaufen?", "What do you want to sell?");
            A("Stell erst ein Produkt online.", "Put a product online first.");
            A("Gugel Ads", "Gugel Ads");
            A("Auf gut Glück!", "I'm Feeling Lucky!");
            A("Glück gibt's nicht. Nur Gebote.", "There's no luck. Only bids.");
            A("Ungefähr {0} Ergebnisse (0,42 Sek.)", "About {0} results (0.42 sec.)");
            A("{0}: {1} ab {2}", "{0}: {1} from {2}");
            A("Schneller Versand · ★ {0} · {1}", "Fast shipping · ★ {0} · {1}");
            A("allesexpress.cn", "everythingxpress.cn");
            A("allesexpress.de", "everythingxpress.com");
            A("schlechtefrage.net", "badquestion.net");
            A("{0} – BilligBoy24", "{0} – BilligBoy24");
            A("{0} – Amazonas", "{0} – Amazonas");
            A("{0} – AllesExpress", "{0} – EverythingXpress");
            A("Riesenauswahl. Kleine Preise. Große Versprechen.", "Huge selection. Small prices. Big promises.");
            A("{0} will dein Geld.", "{0} wants your money.");
            A("AllesExpress will dein Geld.", "EverythingXpress wants your money.");
            A("2026 NEU {0} Premium Top", "2026 NEW {0} Premium Top");
            A("Ab 1 Stück. Oder 5000.", "From 1 piece. Or 5000.");
            A("{0} – Wikipädia", "{0} – Wikipaedia");
            A("{0} ist ein Gegenstand, den Menschen kaufen.", "{0} is an object that people buy.");
            A("Bearbeitungskrieg seit 2019.", "Edit war since 2019.");
            A("Ist {0} Betrug??", "Is {0} a scam??");
            A("Beste Antwort: „Kommt drauf an.“", "Best answer: “It depends.”");
            A("17 Antworten, alle falsch.", "17 answers, all wrong.");
            A("Eigene KI-Seite bauen (SEO)", "Build your own AI site (SEO)");
            A("Hier eigene Anzeige schalten", "Place your own ad here");
            A("Gesponsert", "Sponsored");
            A("Gugel Ads ab Level {0}", "Gugel Ads from level {0}");
            A("Suchbegriff", "Search term");
            A("Top {0}", "Top {0}");
            A("Gebot pro Klick", "Bid per click");
            A("+{0} ct", "+{0} ct");
            A("−{0} ct", "−{0} ct");
            A("Platz 1", "Slot 1");
            A("Platz {0}", "Slot {0}");
            A("Klicks/Tag", "Clicks/day");
            A("pro Klick", "per click");
            A("Du zahlst nur das nächste Gebot + 1 ct.", "You only pay the next bid + 1 ct.");
            A("Vorschau ansehen", "View preview");
            A("„{0}“ · Platz {1}", "“{0}” · slot {1}");
            A("„{0}“ · Platz {1} · beendet", "“{0}” · slot {1} · ended");
            A("{0} Klicks · {1} Verk. · {2}/{3}", "{0} clicks · {1} sold · {2}/{3}");
            A("KI-Seiten-Generator", "AI site generator");
            A("Neue KI-Seite", "New AI site");
            A("Seiten", "Sites");
            A("pro Seite", "per site");
            A("+ {0}", "+ {0}");
            A("Deine Seiten", "Your sites");
            A("Öffnen", "Open");
            A("Löschen", "Delete");
            A("Zurück zu Gugel", "Back to Gugel");
            A("WERBUNG · WERBUNG · WERBUNG", "ADVERTISEMENT · ADVERTISEMENT · ADVERTISEMENT");
            A("von {0} · aktualisiert vor 3 Min. · 14 Min. Lesezeit", "by {0} · updated 3 min. ago · 14 min. read");
            A("TESTSIEGER", "TEST WINNER");
            A("JETZT BEI {0} KAUFEN!!!", "BUY NOW AT {0}!!!");
            A("* Diese Seite enthält Affiliate-Links, Meinungen und 0 % Fakten.", "* This page contains affiliate links, opinions and 0 % facts.");

            // Suchbegriffe (GameData.GugelKeywords, Produktname eingesetzt)
            A("{0} kaufen", "buy {0}");
            A("{0} günstig", "{0} cheap");
            A("bester {0} 2026", "best {0} 2026");
            A("{0} Test", "{0} review");
            A("{0} Erfahrungen Betrug", "{0} experiences scam");

            // KI-Geschwurbel (SlopGen): Titel, Gründe, Absätze, Autoren
            A("Top 10 Gründe, warum {0} dein Leben verändert", "Top 10 reasons why {0} will change your life");
            A("{0} im Test: Wir waren sprachlos (Platz 1 wird dich schockieren)", "{0} tested: we were speechless (number 1 will shock you)");
            A("Experten verraten: Darum hat jetzt jeder {0}", "Experts reveal: this is why everyone has {0} now");
            A("Ich habe 30 Tage lang {0} benutzt – das ist passiert", "I used {0} for 30 days – here's what happened");
            A("{0}: Der ultimative Ratgeber 2026 (aktualisiert gestern)", "{0}: the ultimate guide 2026 (updated yesterday)");
            A("Warum {0} besser ist als Urlaub, Freunde und Schlaf", "Why {0} is better than vacation, friends and sleep");
            A("{0} macht glücklich. Studien zeigen das. Welche, ist egal.", "{0} makes you happy. Studies show it. Which ones doesn't matter.");
            A("Mit {0} sparst du Zeit, die du dann mit {0} verbringst.", "With {0} you save time that you then spend with {0}.");
            A("Dein Nachbar hat schon {0}. Willst du schlechter sein?", "Your neighbour already has {0}. Do you want to be worse?");
            A("{0} ist nachhaltig, weil man es nie wieder wegwirft.", "{0} is sustainable because you never throw it away again.");
            A("Influencer lieben {0}. Und Influencer lügen nie.", "Influencers love {0}. And influencers never lie.");
            A("{0} passt zu jedem Outfit, jeder Wohnung und jedem Sternzeichen.", "{0} goes with every outfit, every flat and every star sign.");
            A("Wissenschaftler sind sich einig: {0} existiert.", "Scientists agree: {0} exists.");
            A("Ohne {0} ist der Alltag grau. Mit {0} auch, aber in HD.", "Without {0} everyday life is grey. With {0} too, but in HD.");
            A("{0} ist das perfekte Geschenk für Leute, die schon alles haben.", "{0} is the perfect gift for people who already have everything.");
            A("Die Bewertungen von {0} sind fast alle echt.", "The reviews of {0} are almost all real.");
            A("{0} war im Fernsehen. Im Hintergrund. Kurz.", "{0} was on TV. In the background. Briefly.");
            A("Jeder Kauf von {0} rettet ein bisschen die Welt (gefühlt).", "Every purchase of {0} saves the world a little (emotionally).");
            A("In der heutigen schnelllebigen Welt ist {0} wichtiger denn je. Als unabhängige Experten haben wir {0} gründlich getestet, also fünf Minuten lang angeschaut.",
                "In today's fast-paced world, {0} is more important than ever. As independent experts we tested {0} thoroughly, i.e. looked at it for five minutes.");
            A("Viele fragen sich: Brauche ich {0}? Die Antwort ist ein klares Ja, gefolgt von einem Link zu {1}.",
                "Many people ask: do I need {0}? The answer is a clear yes, followed by a link to {1}.");
            A("Zusammenfassend lässt sich sagen, dass {0} in vielerlei Hinsicht ein Produkt ist. Unser Fazit: 11 von 10 Punkten.",
                "In summary, {0} is, in many respects, a product. Our verdict: 11 out of 10 points.");
            A("Als KI-Sprachmodell kann ich keine Produkte testen. Trotzdem: {0} ist hervorragend und bei {1} besonders günstig.",
                "As an AI language model I can't test products. Still: {0} is excellent and especially cheap at {1}.");
            A("Unser Testsieger kostet nur {0}. Das ist weniger als ein Abendessen, wenn man sehr teuer isst.",
                "Our test winner costs only {0}. That's less than a dinner, if you eat very expensively.");
            A("Fun Fact: Das Wort „{0}“ kommt aus dem Altgriechischen und bedeutet „jetzt kaufen“.",
                "Fun fact: the word “{0}” comes from Ancient Greek and means “buy now”.");
            A("Dr. Prof. Max Testmann", "Dr. Prof. Max Testman");
            A("Redaktion (2 Personen, eine davon KI)", "Editorial team (2 people, one of them AI)");
            A("Sabine, 34, Expertin", "Sabine, 34, expert");
            A("Das Ratgeber-Team", "The advice team");
            A("Hans G. Utachter", "Hans G. Utachter");
        }

        // =====================================================================================
        // Stadtblatt → City Gazette
        // =====================================================================================
        private static void Stadtblatt(System.Action<string, string> A)
        {
            A("Garage wird Logistikzentrum", "Garage becomes logistics hub");
            A("Nachbarn melden: Seit Wochen klebt jemand Pakete zu. „Das Geräusch verfolgt mich in den Schlaf“, so Rentner Horst (71).",
                "Neighbours report: someone has been taping up parcels for weeks. “The sound follows me into my sleep,” says pensioner Horst (71).");
            A("Kalle verkauft jetzt Döner mit QR-Code", "Kalle now sells kebabs with a QR code");
            A("Wer scannt, bekommt eine Speisekarte. Wer nicht scannt, auch. „Ist modern“, sagt Kalle.",
                "Scan it and you get a menu. Don't scan it and you get one too. “It's modern,” says Kalle.");
            A("Taube stiehlt Pommes – Polizei ermittelt", "Pigeon steals fries – police investigating");
            A("Der Verdächtige ist grau, gurrt und flüchtig. Hinweise bitte an die Wache.", "The suspect is grey, coos and at large. Tips to the station, please.");
            A("MediaMarkd testet Verkäufer, die sich auskennen", "MegaMarked tests sales staff who know things");
            A("Pilotprojekt nach 20 Minuten abgebrochen. „Zu verwirrend für die Kundschaft.“", "Pilot project cancelled after 20 minutes. “Too confusing for customers.”");
            A("Hund Bello zum Mitarbeiter des Monats gewählt", "Dog Bello voted employee of the month");
            A("Der Mischling überzeugte durch Pünktlichkeit und schlechte Laune gegenüber Postboten.", "The mutt impressed with punctuality and a bad attitude towards postmen.");
            A("Stadtrat beschließt neuen Kreisverkehr", "City council approves new roundabout");
            A("Auf die Frage „Wozu?“ antwortet der Bürgermeister: „Weil wir es können.“", "Asked “What for?”, the mayor replies: “Because we can.”");
            A("Influencer zieht in die Hauptstraße", "Influencer moves to the high street");
            A("Erste Beschwerde nach drei Stunden: Das Ringlicht blende die Straßenlaterne.", "First complaint after three hours: the ring light is dazzling the street lamp.");
            A("Fressnix: Katze kauft sich selbst ein Spielzeug", "Chewnothing: cat buys itself a toy");
            A("Kassierer verblüfft. Bezahlt wurde mit einer toten Maus. „Haben wir angenommen.“", "Cashier baffled. Payment was made with a dead mouse. “We accepted it.”");
            A("Paketbote findet Hausnummer 7b", "Delivery driver finds house number 7b");
            A("Nach elf Jahren Suche. Er hat die Karte trotzdem eingeworfen: „Wir haben Sie nicht angetroffen.“",
                "After eleven years of searching. He still dropped the card in: “Sorry we missed you.”");
            A("Klamottenladen „Hype & Hoodie“ weiter zu", "Clothes shop “Hype & Hoodie” still closed");
            A("Schild verspricht: „Demnächst“. Experten tippen auf ein kostenpflichtiges Update.", "Sign promises: “Coming soon”. Experts are betting on a paid update.");
            A("Studie: 9 von 10 Start-ups sind eigentlich ein Laptop", "Study: 9 out of 10 start-ups are actually a laptop");
            A("Der zehnte ist ein Laptop mit Aufkleber.", "The tenth is a laptop with a sticker.");
            A("Ampel an der Hauptstraße schaltet jetzt nach Gefühl", "Traffic light on the high street now switches by feel");
            A("Die Stadtwerke sprechen von „KI“. Die Ampel spricht nicht.", "The utility company calls it “AI”. The traffic light isn't talking.");

            A("Sonnig, 22°", "Sunny, 22°");
            A("Wolkig mit Paketen", "Cloudy with a chance of parcels");
            A("Nieselregen, 14°", "Drizzle, 14°");
            A("Windig – Kartons festhalten!", "Windy – hold on to your boxes!");
            A("Heiter, 19°", "Fair, 19°");
            A("Gewitter am Abend", "Thunderstorms in the evening");

            A("Ausgabe Nr. {0}", "Issue no. {0}");
            A("{0} · Tag {1} · {2}", "{0} · Day {1} · {2}");
            A("Unabhängig. Lokal. Meistens richtig.", "Independent. Local. Mostly right.");
            A("Titelseite", "Front page");
            A("Anzeigen", "Classifieds");
            A("© Stadtblatt · Preis dieser Ausgabe: 0,00 € (wie alles im Internet)", "© City Gazette · Price of this issue: 0.00 € (like everything on the internet)");
            A("Lokale Firma {0} verschickt {1} Pakete", "Local company {0} ships {1} parcels");
            A("Bewertung: {0} Sterne. „Ich hab's ja immer gewusst“, sagt die Mutter des Gründers.", "Rating: {0} stars. “I always knew it,” says the founder's mother.");
            A("Haustier der Woche", "Pet of the week");
            A("Futter im Napf: {0} Portionen", "Food in the bowl: {0} portions");
            A("Eröffnung: DEMNÄCHST. Wirklich. Bald. Als DLC.", "Opening: COMING SOON. Really. Soon. As DLC.");
            A("{0} erobert TikTak – Stadt im Tierfieber", "{0} conquers TikTak – city gripped by pet fever");
            A("KNALLER: {0}", "SMASH DEAL: {0}");
            A("{0}  statt {1}", "{0}  instead of {1}");
            A("BILLIGER ALS DER GROSSHANDEL!", "CHEAPER THAN WHOLESALE!");
            A("Alle Angebote", "All offers");
            A("KNALLER · {0}", "SMASH DEAL · {0}");
            A("{0} · {1}/Stk (Großhandel {2})", "{0} · {1}/pc (wholesale {2})");
            A("statt {0} · nur {1}×", "instead of {0} · only {1}×");
            A("< Großhandel", "< wholesale");
            A("Morgen im Angebot? Das wissen nur die Sterne – und die Druckerei.", "Tomorrow's deals? Only the stars know – and the printers.");

            // Kleinanzeigen
            A("Currywurst 4,–. Ehemalige Mitarbeiter zahlen voll.", "Currywurst 4.–. Former employees pay full price.");
            A("Verkaufe Fahrrad", "Bike for sale");
            A("Fast neu. Nur einmal geklaut. Abholung nachts.", "Almost new. Only stolen once. Pickup at night.");
            A("Suche WG-Zimmer", "Looking for a flatshare room");
            A("Bin ruhig, sauber, verschicke nur ab und zu 300 Pakete.", "I'm quiet, tidy, only ship 300 parcels now and then.");
            A("Tiere adoptieren statt kaufen! (Schutzgebühr ist kein Kaufen. Versprochen.)", "Adopt, don't shop! (An adoption fee isn't shopping. Promise.)");
            A("Diese Woche: Kabel, die passen. Fast.", "This week: cables that fit. Almost.");
            A("Demnächst. Als DLC. Bitte nicht an der Tür rütteln.", "Coming soon. As DLC. Please don't rattle the door.");
            A("Lokal verschickt, global bestellt. {0} Sterne!", "Shipped locally, ordered globally. {0} stars!");
            A("Bekanntschaften", "Personals");
            A("Er (34), Unternehmer, sucht Sie, die Kartons faltet.", "Him (34), entrepreneur, seeks her who folds boxes.");
        }
    }
}
