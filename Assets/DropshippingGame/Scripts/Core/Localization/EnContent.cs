using System.Collections.Generic;
using System.Text;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Spielinhalte aus GameData*.cs (Produkte, Upgrades, Skills, Perks, Ziele, Bewertungen, Kundennotizen, Lieferanten, Mitarbeiter, Tutorial, Onboarding-Mails), ShopData, Ads.Content, FinanceText, Achievements, Difficulty, Vehicles.
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// Abgesichert durch Tests/EditMode/LangContentTests.cs (jeder Inhaltstext muss übersetzt werden).
    /// </summary>
    internal static class EnContent
    {
        public static void Register()
        {
            Products();
            Catalog();
            Progression();
            Reviews();
            Tutorial();
            Orders();
            Trends();
            Contracts();
            Skills();
            Mails();
            Onboarding();
            ProgressContent();
            Shops();
            Ads();
            Finance();
            Achievements();
            Misc();
            TikTok();
            Challenges();
        }

        private static void A(string de, string en) => Lang.Add(de, en);

        /// <summary>
        /// Text mit Tasten-Platzhaltern {key:aktion}: trägt den Rohtext ein und zusätzlich die Vorlage für den
        /// ausgefüllten Text (FillKeys), bei der jede Taste ein Platzhalter {0}, {1} … ist.
        /// </summary>
        private static void K(string de, string en)
        {
            A(de, en);
            var keys = new List<string>();
            string d = KeyTemplate(de, keys);
            string e = KeyTemplate(en, keys);
            if (d != de) A(d, e);
        }

        private static string KeyTemplate(string s, List<string> keys)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                int start = s.IndexOf("{key:", i, System.StringComparison.Ordinal);
                if (start < 0)
                {
                    sb.Append(s, i, s.Length - i);
                    break;
                }
                int end = s.IndexOf('}', start);
                if (end < 0)
                {
                    sb.Append(s, i, s.Length - i);
                    break;
                }
                sb.Append(s, i, start - i);
                string action = s.Substring(start + 5, end - start - 5);
                int idx = keys.IndexOf(action);
                if (idx < 0)
                {
                    idx = keys.Count;
                    keys.Add(action);
                }
                sb.Append('{').Append(idx).Append('}');
                i = end + 1;
            }
            return sb.ToString();
        }

        // =====================================================================================
        // Produkte (Name, Kurzname, URL-Slug für KI-Seiten, Hashtag für Trends)
        // =====================================================================================
        private static void Products()
        {
            // Produktnamen: einzeln, mit Menge ("50× Handyhülle") und als Gugel-Suchbegriffe ("{p} kaufen" …)
            string[][] products =
            {
                new[] { "Handyhülle", "Phone case" },
                new[] { "LED-Lichterkette", "LED fairy lights" },
                new[] { "Massagepistole", "Massage gun" },
                new[] { "Bluetooth-Kopfhörer", "Bluetooth headphones" },
                new[] { "Ringlicht", "Ring light" },
                new[] { "Katzen-Trinkbrunnen", "Cat water fountain" },
                new[] { "Haltungskorrektor", "Posture corrector" },
                new[] { "Smartwatch", "Smartwatch" },
                new[] { "Mini-Beamer", "Mini projector" },
                new[] { "Kamera-Drohne", "Camera drone" },
                new[] { "Selfie-Toaster", "Selfie toaster" },
                new[] { "Bart-Glitzer", "Beard glitter" },
                new[] { "Smarte Gießkanne mit App", "Smart watering can with app" },
                new[] { "Wackeldackel mit Bluetooth", "Bluetooth nodding dachshund" },
            };
            foreach (var p in products)
            {
                A(p[0], p[1]);
                A("{0}× " + p[0], "{0}× " + p[1]);
                A(p[0] + " kaufen", "buy " + p[1]);
                A(p[0] + " günstig", "cheap " + p[1]);
                A(p[0] + " Test", p[1] + " review");
            }
            A("Hülle", "Case");
            A("Kopfhörer", "Headphones");
            A("Katzenbr.", "Cat fount.");
            A("Haltung", "Posture");
            A("Beamer", "Projector");
            A("Drohne", "Drone");
            A("Glitzer", "Glitter");
            A("Gießkanne", "Watering can");
            A("Dackel", "Dachshund");
            A("Produkt", "Product");
            A("das hier", "this thing");

            // URL-Slugs (SlopGen.Slug) – damit KI-Seiten englische Domains bekommen
            A("handyhuelle", "phone-case");
            A("led-lichterkette", "led-fairy-lights");
            A("massagepistole", "massage-gun");
            A("bluetooth-kopfhoerer", "bluetooth-headphones");
            A("ringlicht", "ring-light");
            A("katzen-trinkbrunnen", "cat-water-fountain");
            A("haltungskorrektor", "posture-corrector");
            A("mini-beamer", "mini-projector");
            A("kamera-drohne", "camera-drone");
            A("bart-glitzer", "beard-glitter");
            A("smarte-giesskanne-mit-app", "smart-watering-can-with-app");
            A("wackeldackel-mit-bluetooth", "bluetooth-nodding-dachshund");
            A("produkt", "product");

            // Trend-Hashtags (TrendSystem.HashTag = Kurzname + "Challenge")
            A("HülleChallenge", "CaseChallenge");
            A("KopfhörerChallenge", "HeadphonesChallenge");
            A("RinglichtChallenge", "RingLightChallenge");
            A("KatzenbrChallenge", "CatFountainChallenge");
            A("HaltungChallenge", "PostureChallenge");
            A("BeamerChallenge", "ProjectorChallenge");
            A("DrohneChallenge", "DroneChallenge");
            A("GlitzerChallenge", "GlitterChallenge");
            A("GießkanneChallenge", "WateringCanChallenge");
            A("DackelChallenge", "DachshundChallenge");
        }

        // =====================================================================================
        // Einkauf, Lieferanten, Werbung, Standorte, Logos, Qualität
        // =====================================================================================
        private static void Catalog()
        {
            A("Klein", "Small");
            A("Mittel", "Medium");
            A("Groß", "Large");
            A("Palette", "Pallet");

            A("Billig-Fabrik", "Cheapo Factory");
            A("Spottbillig, aber langsam und die Ware ist... naja.", "Dirt cheap, but slow, and the goods are... well.");
            A("Standard-Großhändler", "Standard Wholesaler");
            A("Solide Qualität, normale Lieferzeit.", "Solid quality, normal delivery time.");
            A("Premium-Hersteller", "Premium Manufacturer");
            A("Manufaktur: teuer, blitzschnell, Kunden lieben es – nur eine Bestellung pro Produkt und Tag.",
                "Artisan workshop: pricey, lightning fast, customers love it – only one order per product per day.");

            A("Flyer & Plakate", "Flyers & Posters");
            A("Facebook-Ads", "Facebook Ads");
            A("Google-Ads", "Google Ads");
            A("Influencer-Kampagne", "Influencer campaign");

            A("Lagerhalle", "Warehouse");

            A("Kreis", "Circle");
            A("Quadrat", "Square");
            A("Raute", "Diamond");
            A("Stern", "Star");
            A("Blitz", "Bolt");
            A("Herz", "Heart");

            A("Billig", "Cheap");

            // Großauftrag: Qualitätsanspruch
            A("nur Premium-Qualität", "Premium quality only");
            A("mind. Standard-Qualität", "at least Standard quality");
            A("Qualität egal", "any quality");
        }

        // =====================================================================================
        // Level-Freischaltungen, Upgrades, Deko, Lifestyle, Ziele, Personal
        // =====================================================================================
        private static void Progression()
        {
            A("Massagepistole · Bart-Glitzer · Express-Bestellungen · TikTok · Verkaufsstand",
                "Massage gun · Beard glitter · Express orders · TikTok · Sales stand");
            A("Großaufträge (B2B) · Fakebook-Werbung · Premium-Hersteller · Großbestellung · Shop-Server-Upgrade · Skill-Stufe 2",
                "Bulk contracts (B2B) · Fakebook ads · Premium manufacturer · Large orders · Shop server upgrade · Skill tier 2");
            A("Lagerhalle kaufbar · Bluetooth-Kopfhörer · Ringlicht · Straßenfeste · Börse & Trading · größerer Kredit · Großhändler (mit Lagerhalle)",
                "Warehouse for sale · Bluetooth headphones · Ring light · Street festivals · Stock exchange & trading · bigger loan · Wholesalers (with warehouse)");
            A("Katzen-Trinkbrunnen · Smarte Gießkanne · Personal · Palettenbestellung · eigener Lieferwagen · Gugel-Anzeigen · KI-Ratgeberseiten · Skill-Stufe 3",
                "Cat water fountain · Smart watering can · Staff · Pallet orders · own delivery van · Gugel ads · AI advice sites · Skill tier 3");
            A("Haltungskorrektor · Wackeldackel · Google-Ads · Förderband · 2 Großaufträge gleichzeitig",
                "Posture corrector · Nodding dachshund · Google Ads · Conveyor belt · 2 bulk contracts at once");
            A("Smartwatch · Influencer-Kampagnen · Großkredit · Skill-Stufe 4", "Smartwatch · Influencer campaigns · Big loan · Skill tier 4");
            A("Mini-Beamer · Hochregal-Erweiterung", "Mini projector · High-rack extension");
            A("Kamera-Drohne · 3 Großaufträge gleichzeitig", "Camera drone · 3 bulk contracts at once");
            A("Legendenstatus · zweite Kraft je Rolle im Team · Außenlager (Ausbau)",
                "Legend status · second hire per team role · Outpost warehouse (expansion)");

            // Upgrades
            A("Verkaufsstand", "Sales stand");
            A("Ein Stand vor der Garage. Stell eine Kiste drauf – Passanten kaufen direkt, ganz ohne Karton und Versand.",
                "A stand in front of the garage. Put a crate on it – passers-by buy straight from it, no box, no shipping.");
            A("Shop-Server-Upgrade", "Shop server upgrade");
            A("+6 Plätze in der Bestell-Warteschlange. Weniger verlorene Bestellungen.", "+6 slots in the order queue. Fewer lost orders.");
            A("Lagerhalle kaufen", "Buy warehouse");
            A("Endlich raus aus der Garage: 3.000 Lagerplätze, 10 Regale, Platz für Personal. Miete 140 €/Tag.",
                "Finally out of the garage: 3,000 storage slots, 10 shelves, room for staff. Rent 140 €/day.");
            A("Eigener Lieferwagen", "Own delivery van");
            A("Alle Einkäufe kommen 25 % schneller an.", "All purchases arrive 25% faster.");
            A("Förderband", "Conveyor belt");
            A("Etikettierte Pakete aufs Band legen - sie werden automatisch verschickt. 10 €/Tag Strom.",
                "Put labeled parcels on the belt – they ship automatically. 10 €/day in electricity.");
            A("Hochregal-Erweiterung", "High-rack extension");
            A("+3.000 Lagerplätze.", "+3,000 storage slots.");
            A("Außenlager (zweite Halle)", "Outpost warehouse (second hall)");
            A("Endlos-Modus: eine zweite Halle am Stadtrand. +3.000 Lagerplätze, +6 Warteschlange, Personal 20 % schneller (kürzere Wege). Miete +150 €/Tag.",
                "Endless mode: a second hall on the edge of town. +3,000 storage slots, +6 queue, staff 20% faster (shorter walks). Rent +150 €/day.");

            // Deko
            A("Zimmerpflanze FLÖRP", "Houseplant FLÖRP");
            A("Motivationsposter", "Motivational poster");
            A("Stehlampe GLÜMP", "Floor lamp GLÜMP");
            A("Teppich LÅNGSAM", "Rug LÅNGSAM");
            A("BILLY-Regal", "BILLY bookcase");
            A("Whiteboard mit Businessplan", "Whiteboard with business plan");
            A("Siebträgermaschine", "Espresso machine");
            A("Ledersofa (nur Lagerhalle)", "Leather sofa (warehouse only)");

            // Lifestyle
            A("Designer-Sneaker", "Designer sneakers");
            A("Stehen in der Vitrine. Zum Anschauen, nicht zum Tragen.", "They live in a display case. For looking at, not for wearing.");
            A("Gaming-Stuhl", "Gaming chair");
            A("RGB macht 20 % produktiver. Angeblich.", "RGB makes you 20% more productive. Allegedly.");
            A("Neon-Schild 'HUSTLE'", "Neon sign 'HUSTLE'");
            A("Leuchtet dich jeden Morgen motivierend an.", "Glows at you motivationally every morning.");
            A("Gebrauchter Kombi", "Used station wagon");
            A("Parkt vor der Garage. TÜV bis nächsten Monat.", "Parked outside the garage. Inspection sticker valid until next month.");
            A("Protzige Uhr", "Flashy watch");
            A("Deine Uhrzeit-Anzeige wird golden.", "Your clock display turns gold.");
            A("Sportwagen", "Sports car");
            A("Parkt vor der Lagerhalle. Nachbarn gucken.", "Parked outside the warehouse. The neighbors are staring.");
            A("Penthouse mit Stadtblick", "Penthouse with city view");
            A("Das oberste Stockwerk im Hochhaus gegenüber leuchtet golden.", "The top floor of the tower across the street glows gold.");

            // Ziele
            A("Der erste Verkauf", "The first sale");
            A("Verschicke dein erstes Paket.", "Ship your first parcel.");
            A("Eine echte Marke", "A real brand");
            A("Gib deinem Shop in der Branding-App einen eigenen Namen.", "Give your shop its own name in the Branding app.");
            A("Läuft bei dir", "On a roll");
            A("Verschicke 10 Pakete.", "Ship 10 parcels.");
            A("Vierstellig", "Four figures");
            A("Erreiche 1.000 € Umsatz.", "Reach 1,000 € in revenue.");
            A("Straßenhändler", "Street vendor");
            A("Verkaufe 10 Artikel am Verkaufsstand.", "Sell 10 items at the sales stand.");
            A("Kundenliebling", "Customer favorite");
            A("Erreiche eine Bewertung von 4,0 Sternen.", "Reach a rating of 4.0 stars.");
            A("Business to Business", "Business-to-Business");
            A("Erfülle deinen ersten Großauftrag (App 'Aufträge', ab Level 3).", "Fulfill your first bulk contract ('Contracts' app, from level 3).");
            A("Paketprofi", "Parcel pro");
            A("Verschicke 100 Pakete.", "Ship 100 parcels.");
            A("Wochenheld", "Weekly hero");
            A("Schaffe insgesamt 3 Wochenziele.", "Complete 3 weekly goals in total.");
            A("Raus aus der Garage", "Out of the garage");
            A("Kaufe die Lagerhalle (Ausbau-App).", "Buy the warehouse (Expansion app).");
            A("Chef sein", "Being the boss");
            A("Stelle deinen ersten Mitarbeiter ein.", "Hire your first employee.");
            A("Fünfstellig", "Five figures");
            A("Erreiche 10.000 € Umsatz.", "Reach 10,000 € in revenue.");
            A("Volles Sortiment", "Full range");
            A("Erreiche Firmenlevel 7.", "Reach company level 7.");
            A("Kleines Team", "Small team");
            A("Beschäftige 4 Mitarbeiter gleichzeitig.", "Employ 4 people at once.");
            A("Imperium", "Empire");
            A("Erreiche 100.000 € Umsatz. Kalle wird staunen.", "Reach 100,000 € in revenue. Kalle will be amazed.");
            A("Viertelmillion", "Quarter million");
            A("Endlos-Modus: erreiche 250.000 € Umsatz.", "Endless mode: reach 250,000 € in revenue.");
            A("Zweites Standbein", "Second leg to stand on");
            A("Kaufe das Außenlager (Ausbau-App, ab Level 10).", "Buy the outpost warehouse (Expansion app, from level 10).");
            A("Halbe Million", "Half a million");
            A("Erreiche 500.000 € Umsatz.", "Reach 500,000 € in revenue.");
            A("Börsengang", "IPO");
            A("Erreiche 1.000.000 € Umsatz – dann geht deine Firma an die Börse.", "Reach 1,000,000 € in revenue – then your company goes public.");

            // Personal
            A("Lagerist:in", "Stock clerk");
            A("Räumt Kisten vom Wareneingang ein, bestückt Großauftrags-Paletten aus dem Lager und bearbeitet Retouren.",
                "Shelves crates from goods-in, loads bulk-contract pallets from stock and handles returns.");
            A("Packer:in", "Packer");
            A("Kommissioniert offene Bestellungen und verpackt sie (braucht Kartons!).", "Picks open orders and packs them (needs boxes!).");
            A("Versandkraft", "Shipping clerk");
            A("Etikettiert fertige Pakete und verschickt sie.", "Labels finished parcels and ships them.");
            A("Social-Media-Praktikant:in", "Social media intern");
            A("Postet jeden Vormittag ein TikTok. Mal viral, mal peinlich.", "Posts a TikTok every morning. Sometimes viral, sometimes cringe.");
        }

        // =====================================================================================
        // Bewertungen
        // =====================================================================================
        private static void Reviews()
        {
            // 5 Sterne
            A("Top! Kam super schnell an.", "Top! Arrived super fast.");
            A("Genau wie beschrieben, gerne wieder.", "Exactly as described, would buy again.");
            A("Meine Oma liebt es. 10/10.", "My grandma loves it. 10/10.");
            A("Die Verpackung allein ist schon ein Erlebnis.", "The packaging alone is an experience.");
            A("Schneller als der Pizzadienst!", "Faster than pizza delivery!");
            A("Unboxing war besser als Weihnachten.", "The unboxing was better than Christmas.");
            A("Hat meine Ehe gerettet. Danke!", "Saved my marriage. Thank you!");
            A("Der Karton ist so schön, meine Katze wohnt jetzt drin.", "The box is so nice, my cat lives in it now.");
            A("Hab's meinem Chef gezeigt. Jetzt will er auch eins.", "Showed it to my boss. Now he wants one too.");
            A("Fünf Sterne, weil es keine sechs gibt.", "Five stars, because there's no sixth.");
            // 4 Sterne
            A("Gutes Produkt, Versand okay.", "Good product, shipping okay.");
            A("Macht, was es soll.", "Does what it says on the box.");
            A("Solide. Karton war etwas zerdrückt.", "Solid. Box was a bit squashed.");
            A("Würde wieder kaufen.", "Would buy again.");
            A("Gut. Nicht weltbewegend, aber gut.", "Good. Not earth-shattering, but good.");
            A("Mein Hund mag den Karton sehr.", "My dog really likes the box.");
            A("Alles okay, nur der Paketbote hat geseufzt.", "All fine, the courier just sighed a lot.");
            // 3 Sterne
            A("Naja. Hatte es mir größer vorgestellt.", "Meh. I imagined it bigger.");
            A("Ganz okay für den Preis.", "Fine for the price.");
            A("Hat gedauert, ist aber angekommen.", "Took a while, but it got here.");
            A("Ist halt ein Produkt.", "It's... a product.");
            A("Erfüllt seinen Zweck. Mehr nicht.", "Serves its purpose. Nothing more.");
            A("Drei Sterne. Wie mein Hotel im letzten Urlaub.", "Three stars. Like my hotel on my last vacation.");
            // 2 Sterne
            A("Riecht irgendwie nach Plastik.", "Kinda smells like plastic.");
            A("Hat ewig gedauert.", "Took forever.");
            A("Anleitung nur auf Chinesisch.", "Manual only in Chinese.");
            A("Der Karton war größer als der Inhalt.", "The box was bigger than what was in it.");
            A("Hat zwei Tage gedauert. ZWEI!", "Took two days. TWO!");
            A("Mein Horoskop hatte mich gewarnt.", "My horoscope warned me.");
            // 1 Stern
            A("Nie angekommen. Nie wieder!", "Never arrived. Never again!");
            A("Nach fünf Minuten kaputt.", "Broke after five minutes.");
            A("Ich will mein Geld zurück!!!", "I want my money back!!!");
            A("Sieht nicht aus wie auf dem Foto.", "Doesn't look like the photo.");
            A("Mein Hamster hätte es besser verpackt.", "My hamster could have packed it better.");
            A("Retoure läuft. Tschüss.", "Return's on its way. Bye.");
            // Express / verspätet
            A("Express war wirklich express. Respekt!", "Express was actually express. Respect!");
            A("Morgens bestellt, mittags da. Magie!", "Ordered in the morning, here by noon. Magic!");
            A("Schneller als mein WLAN.", "Faster than my Wi-Fi.");
            A("Das Geschenk kam rechtzeitig – ich bin gerettet!", "The gift arrived in time – I'm saved!");
            A("Express bezahlt, Schnecke bekommen.", "Paid for express, got a snail.");
            A("Express? Eher Espresso – kalt und bitter.", "Express? More like espresso – cold and bitter.");
            A("Die Party war schon vorbei, als das Paket kam.", "The party was over by the time the parcel showed up.");
            A("Für Express-Aufpreis erwarte ich Express. Nicht Bummelzug.", "For an express surcharge I expect express. Not the milk train.");
            A("Hat ewig gedauert. Ich dachte schon, das ist ein Fake-Shop.", "Took forever. I was starting to think this was a scam shop.");
            A("Kam an. Irgendwann. Immerhin.", "It arrived. Eventually. At least.");
            A("Ich bin in der Zwischenzeit zweimal umgezogen.", "I moved house twice in the meantime.");
            // Namen (die übrigen bleiben)
            A("Oma Gerda", "Grandma Gerda");
            A("Jürgen aus Bottrop", "Jürgen from Bottrop");
            A("Frau Schmidt", "Mrs. Schmidt");
            A("Anonym", "Anonymous");
            A("Lena aus Leipzig", "Lena from Leipzig");
        }

        // =====================================================================================
        // Tutorial, Kalle, Verkaufsstand
        // =====================================================================================
        private static void Tutorial()
        {
            A("Willkommen in deiner Garage", "Welcome to your garage");
            K("Geh zum Laptop auf der Werkbank (gelber Marker) und öffne ihn mit {key:interact}. Darauf läuft dein ganzes Business.",
                "Walk over to the laptop on the workbench (yellow marker) and open it with {key:interact}. Your whole business runs on it.");
            A("Ware einkaufen", "Buy stock");
            K("AllesExpress ist schon offen: Handyhülle, Menge und Händler wählen, dann „Kaufen“. Mit {key:back} klappst du den Laptop zu.",
                "EverythingXpress is already open: pick Phone case, quantity and supplier, then “Buy”. Press {key:back} to close the laptop.");
            A("Lieferung annehmen", "Receive the delivery");
            K("Der Lieferwagen bringt die Kiste zum Wareneingang am Tor. Heb sie dort mit {key:interact} auf.",
                "The delivery van drops the crate at goods-in by the gate. Pick it up there with {key:interact}.");
            A("Einlagern", "Stock the shelf");
            K("Trag die Kiste zum Regal mit dem passenden Schild und räum sie mit {key:interact} ein.",
                "Carry the crate to the shelf with the matching sign and stock it with {key:interact}.");
            A("Online stellen", "Go online");
            A("Zurück an den Laptop: In „Mein Shop“ das Produkt auf „Online“ schalten. Ein fairer Preis ist schon eingetragen.",
                "Back to the laptop: in “My Shop”, switch the product to “Online”. A fair price is already filled in.");
            A("Erste Bestellung", "First order");
            K("Gleich kommt deine erste Bestellung – als Zettel oben rechts mit Countdown. Mit {key:phone} siehst du alle Bestellungen im Handy.",
                "Your first order is about to come in – as a ticket at the top right with a countdown. Press {key:phone} to see all orders on your phone.");
            A("Artikel holen", "Grab the item");
            K("Nimm am Regal einen Artikel für den Bestellzettel ({key:interact}).", "Take an item for the order ticket from the shelf ({key:interact}).");
            A("Verpacken", "Pack it");
            A("Am Packtisch verpacken – der passende Karton wird automatisch genommen.", "Pack it at the packing table – the right box is picked automatically.");
            A("Etikettieren", "Label it");
            A("Nimm das Paket vom Packtisch und druck am Labeldrucker das Versandlabel.", "Take the parcel from the packing table and print the shipping label at the label printer.");
            A("Versenden", "Ship it");
            A("Gib das Paket an der gelben PaketBlitz-Abholbox am Garagentor ab – im Tutorial gratis. Später: Packstation in der Stadt gratis, Abholung 3 € pro Paket.",
                "Drop the parcel into the yellow ParcelBolt pickup box at the garage door – free during the tutorial. Later: parcel lockers in town are free, pickup costs 3 € per parcel.");

            A("Na, Herr Unternehmer? Schon Millionär?", "Well, Mr. Entrepreneur? Millionaire yet?");
            A("Die Fritteuse vermisst dich. Ich nicht.", "The deep fryer misses you. I don't.");
            A("Willst du 'nen Döner? Geht aufs Haus. Ausnahmsweise.", "Want a kebab? On the house. Just this once.");
            A("Mein Neffe macht auch Internet. Der verkauft Socken.", "My nephew does internet too. He sells socks.");
            A("Früher hattest du Fett an den Fingern. Heute Kartonstaub.", "You used to have grease on your fingers. Now it's cardboard dust.");
            A("Retouren? Bei mir gibt's keine Retouren. Wer die Currywurst bestellt, isst die Currywurst.",
                "Returns? No returns here. You order the currywurst, you eat the currywurst.");
            A("Großaufträge, hm? Ich hab mal 40 Schnitzel für den Kegelclub gemacht. Nie wieder.",
                "Bulk contracts, huh? I once made 40 schnitzels for the bowling club. Never again.");
            A("Was ist ein Hype? Ist das so was wie Senf?", "What's a hype? Is that like mustard?");

            A("Oh, das nehm ich mit!", "Ooh, I'll take that!");
            A("Was kostet das? ... Gekauft!", "How much is that? ... Sold!");
            A("Für meine Nichte. Perfekt.", "For my niece. Perfect.");
            A("Hab ich auf TikTok gesehen!", "I saw that on TikTok!");
            A("Zwei Euro billiger als online? Deal.", "Two euros cheaper than online? Deal.");
        }

        // =====================================================================================
        // Wochentage, Tasten, Bestellzettel, Retouren
        // =====================================================================================
        private static void Orders()
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
            A("Leertaste", "Space");

            A("Bitte nicht klingeln – Baby schläft.", "Please don't ring – baby's asleep.");
            A("Ist ein Geschenk. Bitte keine Rechnung beilegen!", "It's a gift. Please don't include the invoice!");
            A("Falls ich nicht da bin: beim Nachbarn mit dem Gartenzwerg.", "If I'm not home: leave it with the neighbor who has the garden gnome.");
            A("Bitte schnell, ich hab's meiner Freundin schon versprochen.", "Please hurry, I already promised it to my girlfriend.");
            A("Hab's auf TikTok gesehen. Hoffe, es ist echt.", "Saw it on TikTok. Hope it's real.");
            A("Paket bitte in die blaue Tonne legen. NICHT die graue.", "Please put the parcel in the blue bin. NOT the gray one.");
            A("Kann man das auch in Lila haben? Egal, nehm ich so.", "Does this come in purple? Never mind, I'll take it as is.");
            A("Meine dritte Bestellung diese Woche. Ich hab ein Problem.", "My third order this week. I have a problem.");
            A("Bitte gut polstern, mein Postbote wirft gern.", "Please pad it well, my mailman likes to throw.");
            A("Für meinen Opa. Er versteht kein Internet, aber er liebt Pakete.", "For my grandpa. He doesn't get the internet, but he loves parcels.");
            A("Keine Eile. (Doch, bitte beeilen.)", "No rush. (Actually, please hurry.)");
            A("Bitte nicht in die Garage legen, da wohnt ein Marder.", "Please don't leave it in the garage, a marten lives there.");
            A("Achtung: Der Hund ist lieb, die Katze nicht.", "Warning: the dog is friendly. The cat is not.");
            A("Lieferung bitte nach 17 Uhr. Vorher bin ich im Homeoffice (also eigentlich da).",
                "Please deliver after 17:00. Before that I'm working from home (so, technically, I'm there).");
            A("Wenn das gut ist, bestell ich nochmal. Für die ganze Familie.", "If this is good, I'll order again. For the whole family.");
            A("Mein Horoskop sagt: heute online shoppen.", "My horoscope says: shop online today.");
            A("Bitte mit Liebe verpacken. Oder mit Klebeband. Hauptsache fest.", "Please pack with love. Or with tape. As long as it holds.");
            A("Hab aus Versehen auf 'Kaufen' gedrückt. Passt aber schon.", "Accidentally hit 'Buy'. That's fine though.");
            A("Grüße an die Person, die das hier liest!", "Hi to whoever is reading this!");
            A("Bitte diskret verpacken, meine Frau zählt die Pakete.", "Please pack discreetly, my wife counts the parcels.");

            A("EXPRESS! Die Party ist heute Abend!", "EXPRESS! The party is tonight!");
            A("Brauche es SOFORT. Also, so sofort wie möglich.", "Need it NOW. Well, as now as possible.");
            A("Express, weil Geduld nicht mein Ding ist.", "Express, because patience isn't my thing.");
            A("Geburtstag ist morgen und ich bin ein schlechter Mensch. Bitte schnell!", "The birthday is tomorrow and I'm a terrible person. Please hurry!");
            A("Doppelt bezahlt, doppelt so schnell? So hab ich das verstanden.", "Paid double, twice as fast? That's how I understood it.");
            A("Mein Chef kommt gleich. Bitte vorher liefern.", "My boss is coming soon. Please deliver before that.");

            A("Passt die auch auf ein Nokia 3310?", "Does it fit a Nokia 3310 too?");
            A("Die Farbe muss zu meinem Nagellack passen.", "The color has to match my nail polish.");
            A("Für meinen Balkon. Die Nachbarn sollen neidisch werden.", "For my balcony. The neighbors shall be jealous.");
            A("Weihnachten ist bei mir das ganze Jahr.", "At my place it's Christmas all year round.");
            A("Mein Rücken dankt dir schon jetzt.", "My back thanks you already.");
            A("Für meinen Mann. Er jammert zu viel.", "For my husband. He whines too much.");
            A("Damit ich meinen Mitbewohner nicht mehr hören muss.", "So I never have to hear my roommate again.");
            A("Bitte mit extra viel Bass.", "Extra bass, please.");
            A("Ich werde Influencer. Ab Montag.", "I'm becoming an influencer. Starting Monday.");
            A("Für bessere Videocalls. Mein Chef sieht nur Schatten.", "For better video calls. My boss only ever sees a shadow.");
            A("Meine Katze trinkt nur fließendes Wasser. Sie ist eine Diva.", "My cat only drinks running water. She's a diva.");
            A("Für Herrn Schnurrbert. Er hat Ansprüche.", "For Sir Purrcival. He has standards.");
            A("Mein Chef sagt, ich sehe aus wie ein Fragezeichen.", "My boss says I look like a question mark.");
            A("Zu viel Handy. Weiß ich selbst.", "Too much phone. I know, I know.");
            A("Damit ich meine Schritte zählen kann. Es sind nicht viele.", "So I can count my steps. There aren't many.");
            A("Endlich pünktlich sein. Vielleicht.", "Finally being on time. Maybe.");
            A("Kinoabend im Schrebergarten!", "Movie night in the allotment garden!");
            A("Für PowerPoint-Karaoke im Büro.", "For PowerPoint karaoke at the office.");
            A("Nur für Landschaftsaufnahmen. Ehrlich.", "Only for landscape shots. Honest.");
            A("Mein Nachbar hat auch eine. Jetzt ist Krieg.", "My neighbor has one too. This means war.");
            A("Mein Gesicht auf Toast. Endlich.", "My face on toast. Finally.");
            A("Für meine Schwiegermutter. Sie wird es hassen.", "For my mother-in-law. She'll hate it.");
            A("Für den Junggesellenabschied. Bitte diskret!", "For the bachelor party. Discreetly, please!");
            A("Mein Bart soll strahlen wie meine Zukunft.", "My beard shall shine like my future.");
            A("Meine Pflanzen brauchen WLAN.", "My plants need Wi-Fi.");
            A("Damit der Kaktus endlich überlebt.", "So the cactus finally survives.");
            A("Für die Hutablage. Mit Bass.", "For the parcel shelf in my car. With bass.");
            A("Opa will ihn mit seinem Hörgerät koppeln.", "Grandpa wants to pair it with his hearing aid.");

            A("Gefällt mir doch nicht.", "Turns out I don't like it.");
            A("Die Farbe sieht in echt anders aus.", "The color looks different in real life.");
            A("Hab's aus Versehen doppelt bestellt.", "Accidentally ordered it twice.");
            A("Mein Mann hat das gleiche schon gekauft.", "My husband already bought the same thing.");
            A("Passt nicht zu meinem Sternzeichen.", "Doesn't match my star sign.");
            A("Hab's mir irgendwie anders vorgestellt.", "Somehow I pictured it differently.");
            A("Meine Katze mag es nicht.", "My cat doesn't like it.");
            A("Zu schön für mich. Ernsthaft.", "Too beautiful for me. Seriously.");
            A("Qualität ist leider mies.", "The quality is sadly lousy.");
            A("Nach zwei Tagen kaputt.", "Broke after two days.");
            A("Riecht komisch.", "Smells funny.");
            A("Wackelt, knarzt und leuchtet falsch.", "Wobbles, creaks and glows the wrong way.");
            A("Kam zu spät. Brauche ich jetzt nicht mehr.", "Arrived too late. Don't need it anymore.");
            A("Karton viel zu groß – die Ware ist drin herumgeflogen.", "Box way too big – the item was flying around inside.");

            A("„{0}“", "“{0}”");
        }

        // =====================================================================================
        // Trends
        // =====================================================================================
        private static void Trends()
        {
            A("stabil", "stable");
            A("steigend", "rising");
            A("fallend", "falling");
            A("tot", "dead");

            // {p} → {0}, {tag} → {0} (eigene Vorlage)
            A("Ein Streamer hat {0} live in die Kamera gehalten.", "A streamer held {0} up to the camera live.");
            A("#{0} geht auf TikTok viral.", "#{0} is going viral on TikTok.");
            A("Ein Promi wurde mit {0} beim Bäcker gesehen.", "A celebrity was spotted with {0} at the bakery.");
            A("Eine Oma erklärt {0} auf YouTube – drei Millionen Aufrufe.", "A grandma explains {0} on YouTube – three million views.");
            A("Das Frühstücksfernsehen hat {0} zum Must-have erklärt.", "Breakfast TV has declared {0} a must-have.");
            A("Alle wollen {0}. Wirklich alle.", "Everyone wants {0}. Literally everyone.");
            A("{0} ist DAS Gesprächsthema in jeder Kaffeeküche.", "{0} is THE talk of every office kitchen.");
            A("Selbst Kalle fragt nach {0}.", "Even Kalle is asking about {0}.");
            A("Der Hype um {0} flaut ab.", "The hype around {0} is fading.");
            A("Die ersten Memes über {0} sind schon cringe.", "The first memes about {0} are already cringe.");
            A("Das Internet schaut schon woanders hin.", "The internet is already looking elsewhere.");
            A("{0} ist so 2019.", "{0} is so 2019.");
            A("Ein Tech-YouTuber nennt {0} „Elektroschrott mit Marketing“.", "A tech YouTuber calls {0} “e-waste with marketing”.");
            A("Niemand redet mehr über {0}. Niemand.", "Nobody talks about {0} anymore. Nobody.");
            A("{0} verkauft sich wieder ganz normal.", "{0} is selling normally again.");
        }

        // =====================================================================================
        // Großaufträge: Firmen, Anlässe, Großhändler
        // =====================================================================================
        private static void Contracts()
        {
            A("Büro-Bedarf Möller GmbH", "Möller Office Supplies Ltd.");
            A("Kita Sonnenschein e. V.", "Sunshine Daycare");
            A("Fitnessstudio Muckibude", "Swole Shack Gym");
            A("Autohaus Brummer & Söhne", "Brummer & Sons Car Dealership");
            A("Hotel Zur Goldenen Gans", "Golden Goose Hotel");
            A("Zahnarztpraxis Dr. Bohr", "Dr. Drill's Dental Practice");
            A("SynergyHub Start-up GmbH", "SynergyHub Start-up Inc.");
            A("Katzencafé Schnurrhaus", "Purr House Cat Café");
            A("TSV Kickers 09", "Kickers 09 Sports Club");
            A("Bäckerei Krümel", "Crumbs Bakery");
            A("Seniorenstift Abendrot", "Sunset Retirement Home");
            A("Coworking „Die Denkfabrik“", "Coworking “The Think Tank”");
            A("Influencer-Agentur GlowUp", "GlowUp Influencer Agency");
            A("Friseursalon Haarmonie", "Hairmony Hair Salon");
            A("Umzüge Schlepp & Trag", "Haul & Lug Movers");
            A("Yoga-Studio Om Sweet Om", "Om Sweet Om Yoga Studio");
            A("Tattoo-Studio Nadelöhr", "Eye of the Needle Tattoo Studio");
            A("Pizzeria Mamma Mia", "Mamma Mia Pizza Place");
            A("Gaming-Café Respawn", "Respawn Gaming Café");
            A("Gartencenter Grünzeug", "Greenstuff Garden Center");
            A("Verein der Gartenzwergfreunde e. V.", "Society of Garden Gnome Friends");
            A("Hochzeitsagentur Ja-Wort GmbH", "I Do Wedding Agency Ltd.");
            A("Escape Room „Kein Ausweg“", "Escape Room “No Way Out”");
            A("IT-Beratung Byte & Söhne", "Byte & Sons IT Consulting");
            A("Stadtwerke Kleinkleckersdorf", "Kleinkleckersdorf Municipal Utilities");
            A("Tanzschule Wiener Walzer", "Viennese Waltz Dance School");
            A("Fahrschule Vollgas", "Full Throttle Driving School");
            A("Physiopraxis Rückgrat", "Backbone Physiotherapy");

            A("Großhandel Krause KG", "Krause Wholesale Co.");
            A("MegaMarkt Zentrallager", "MegaMart Central Warehouse");
            A("Ramsch & Co. Restposten", "Junk & Co. Clearance");
            A("Kaufhaus Zentrale Nord", "Department Store HQ North");
            A("Tankstellen-Verbund Sprit+", "Fuel+ Gas Station Group");
            A("Geschenke-Kette Schenkmal", "GiftMe Gift Shop Chain");
            A("Elektro-Discount Blitzpreis", "FlashPrice Electronics Discount");
            A("Drogerie-Kette Glanz & Gloria", "Glitz & Glory Drugstore Chain");

            // Anlass: einzeln und im Auftragstext (Contract.Description = Firma + " braucht " + Titel + " " + Anlass + ".")
            string[][] reasons =
            {
                new[] { "für die Weihnachtsfeier", "for the Christmas party" },
                new[] { "als Kundengeschenke", "as customer gifts" },
                new[] { "für die Tombola", "for the raffle" },
                new[] { "für das Sommerfest", "for the summer party" },
                new[] { "für neue Mitarbeitende", "for new employees" },
                new[] { "zum Firmenjubiläum", "for the company anniversary" },
                new[] { "für eine Marketing-Aktion", "for a marketing stunt" },
                new[] { "für den Tag der offenen Tür", "for the open house" },
                new[] { "als Preise für ein Gewinnspiel", "as prizes for a giveaway" },
                new[] { "zur Motivation der Belegschaft (statt Gehaltserhöhung)", "to motivate the staff (instead of a raise)" },
                new[] { "für die Goodie-Bags beim Firmenlauf", "for the goodie bags at the company fun run" },
                new[] { "für 14 Filialen (Markenware)", "for 14 branches (brand goods)" },
                new[] { "fürs Weihnachtsregal (Markenware)", "for the Christmas shelf (brand goods)" },
                new[] { "für die Kassenzone (Markenware)", "for the checkout area (brand goods)" },
                new[] { "als Aktionsware im Prospekt", "as a promo item in the flyer" },
                new[] { "für den Online-Shop des Händlers (Markenware)", "for the retailer's online shop (brand goods)" },
            };
            foreach (var r in reasons)
            {
                A(r[0], r[1]);
                A("{0} braucht {1} " + r[0] + ".", "{0} needs {1} " + r[1] + ".");
            }
        }

        // =====================================================================================
        // Hustle-Skills
        // =====================================================================================
        private static void Skills()
        {
            A("Logistik", "Logistics");
            A("Schneller tragen, mehr lagern, schneller liefern.", "Carry faster, store more, deliver faster.");
            A("Vertrieb", "Sales");
            A("Günstiger einkaufen, weniger Retouren, bessere Großaufträge.", "Cheaper purchasing, fewer returns, better bulk contracts.");
            A("Stärkere TikToks, günstigere Werbung, Trends vorhersehen.", "Stronger TikToks, cheaper ads, see trends coming.");

            A("Starke Arme", "Strong arms");
            A("Kisten bremsen dich nicht mehr aus, und mit vollen Händen läufst du 10 % schneller. Proteinshake inklusive.",
                "Crates no longer slow you down, and you walk 10% faster with full hands. Protein shake included.");
            A("Lager-Tetris", "Storage Tetris");
            A("Du stapelst wie ein Weltmeister: +30 % Lagerplatz.", "You stack like a world champion: +30% storage space.");
            A("Kurze Wege", "Short routes");
            A("Du kennst jeden Lieferanten persönlich: Lieferungen kommen 30 % schneller.", "You know every supplier personally: deliveries arrive 30% faster.");
            A("Prozess-Flow", "Process flow");
            A("Lean Management, Baby: Dein Team arbeitet 25 % schneller, das Förderband doppelt so schnell.",
                "Lean management, baby: your team works 25% faster, the conveyor belt twice as fast.");
            A("Feilschen", "Haggling");
            A("Hart, aber herzlich verhandelt: Einkaufspreise −10 %.", "Tough but friendly negotiating: purchase prices −10%.");
            A("Kundenflüsterer", "Customer whisperer");
            A("Ehrliche Produktfotos, freundliche Mails: 35 % weniger Retouren.", "Honest product photos, friendly emails: 35% fewer returns.");
            A("Visitenkarten mit Goldrand: Großaufträge zahlen 20 % mehr, +1 gleichzeitiger Großauftrag.",
                "Gold-rimmed business cards: bulk contracts pay 20% more, +1 simultaneous bulk contract.");
            A("Stammkundschaft", "Loyal customers");
            A("Deine Kundschaft verzeiht dir fast alles: schlechte Bewertungen wirken nur halb so stark, +3 Plätze in der Warteschlange.",
                "Your customers forgive you almost anything: bad reviews hit only half as hard, +3 queue slots.");
            A("Ringlicht an, Selfie-Stick raus: TikToks wirken 25 % stärker und länger.", "Ring light on, selfie stick out: TikToks are 25% stronger and last longer.");
            A("Trendradar", "Trend radar");
            A("Du scrollst jetzt beruflich: Die Trendprognose wird viel genauer und sieht 3 Tage voraus.",
                "You scroll professionally now: the trend forecast gets much more accurate and looks 3 days ahead.");
            A("Viral-Gen", "Viral gene");
            A("TikTok-Abklingzeit −40 %, Werbekampagnen 25 % günstiger.", "TikTok cooldown −40%, ad campaigns 25% cheaper.");
            A("Du machst die Trends: Ein gutes eigenes TikTok (ab 60 % Treffer) löst einen Hype für dein Produkt aus.",
                "You make the trends: a good TikTok of your own (60%+ score) sparks a hype for your product.");
        }

        // =====================================================================================
        // Erklär-Nachrichten (Marvin)
        // =====================================================================================
        private static void Mails()
        {
            A("Marvin (Hustle-Akademie)", "Marvin (Hustle Academy)");

            A("Deine Wochenziele", "Your weekly goals");
            A("Guten Morgen, Champion! Jeden Montag bekommst du drei Wochenziele. Schaffst du sie bis Sonntagabend, gibt's Geld und Erfahrung. Rise and grind! (Aber schlaf trotzdem genug.)",
                "Good morning, champion! Every Monday you get three weekly goals. Hit them by Sunday night and you earn cash and XP. Rise and grind! (But still get enough sleep.)");
            A("Du hast Skillpunkte!", "You've got skill points!");
            A("Level-Up! Für jeden Aufstieg bekommst du einen Skillpunkt (einen hattest du schon vom Start). Verteil sie in der Firma-App unter 'Skills': Logistik, Vertrieb oder Marketing. Für alles reicht es nicht – wähle weise.",
                "Level up! You get one skill point per level (you already had one from the start). Spend them in the Company app under 'Skills': Logistics, Sales or Marketing. You can't have everything – choose wisely.");
            A("Großaufträge freigeschaltet", "Bulk contracts unlocked");
            A("Jetzt fragen auch Firmen bei dir an! Angebote findest du in der App 'Aufträge'. Nimm nur an, was du schaffst: Ganze Kisten gibst du direkt am Palettenplatz ab – ohne Auspacken. Einzelne Artikel nimmst du dafür aus dem Regal. Aber Achtung: Frist verpasst = Vertragsstrafe.",
                "Now companies are coming to you too! You'll find their offers in the 'Contracts' app. Only accept what you can handle: drop whole crates straight at the pallet spot – no unpacking. Single items come from the shelf. But careful: missed deadline = contract penalty.");
            A("Deine erste Retoure", "Your first return");
            A("Tja, nicht jede Kundschaft bleibt glücklich. Retouren landen am Wareneingang, die Erstattung ist schon abgebucht. Bring das Päckchen zum Retourenplatz: als B-Ware zurück ins Lager (etwas schlechtere Qualität) oder ab in die Tonne. Tipp: Bessere Ware, passende Kartons und pünktlicher Versand = weniger Retouren.",
                "Well, not every customer stays happy. Returns land at goods-in, and the refund has already been paid out. Take the parcel to the returns spot: back into stock as B-stock (slightly lower quality) or straight into the bin. Tip: better goods, fitting boxes and on-time shipping = fewer returns.");
            A("Express-Bestellungen", "Express orders");
            A("Manche Kundschaft hat's eilig: Express-Bestellungen zahlen 40 % mehr, haben aber eine viel kürzere Frist. Pünktlich = Jubel-Bewertung, zu spät = Drama. Du erkennst sie am Blitz auf dem Bestellzettel.",
                "Some customers are in a hurry: express orders pay 40% more but have a much shorter deadline. On time = rave review, late = drama. You'll spot them by the lightning bolt on the order ticket.");
            A("Hustle-Punkte!", "Hustle points!");
            A("Erfahrung lohnt sich doppelt: Neben Level-Aufstiegen bekommst du für gesammelte XP Hustle-Punkte. Gib sie in der Firma-App unter 'Perks' aus – mehr Kundschaft, größere Warteschlange, günstigerer Einkauf, schnelleres Personal und mehr.",
                "Experience pays twice: besides level-ups, the XP you collect earns you hustle points. Spend them in the Company app under 'Perks' – more customers, a bigger queue, cheaper purchasing, faster staff and more.");
            A("Großhändler wollen deine Marke", "Wholesalers want your brand");
            A("Deine Marke spricht sich herum! Großhändler fragen jetzt größere Mengen deiner bewährten Produkte an – zu besseren Preisen als normale Firmen. Je stärker deine Marke (Bewertung, Bekanntheit, Name), desto mehr zahlen sie. Angebote in der App 'Aufträge'.",
                "Word about your brand is getting around! Wholesalers now request larger quantities of your proven products – at better prices than regular companies. The stronger your brand (rating, awareness, name), the more they pay. Offers in the 'Contracts' app.");
        }

        // =====================================================================================
        // Onboarding-Tipps
        // =====================================================================================
        private static void Onboarding()
        {
            A("Versand: gratis oder bequem", "Shipping: free or convenient");
            A("Die Abholbox am Garagentor kostet 3 € pro Paket. Packstationen in der Stadt und der Schalter in der PaketBlitz-Filiale nehmen gratis an. Tipp: Mehrere Pakete sammeln und zusammen wegbringen – der Kompass oben zeigt die nächste Packstation.",
                "The pickup box at the garage door costs 3 € per parcel. Parcel lockers in town and the counter at the ParcelBolt branch take them for free. Tip: collect several parcels and drop them off together – the compass at the top points to the nearest parcel locker.");
            A("Dein Handy", "Your phone");
            K("Mit {key:phone} öffnest du dein Handy, ohne die Arbeit zu unterbrechen: Bestellzettel, Nachrichten, Nachbestellen mit einem Tipp und Kontostand. Den Laptop brauchst du für Einkauf, Shop und Firma.",
                "Press {key:phone} to open your phone without stopping work: order tickets, messages, one-tap reordering and your balance. You need the laptop for purchasing, the shop and the company.");
            A("Feierabend um 20 Uhr", "Closing time at 20:00");
            A("Um 20 Uhr ist Schluss: Dann werden Miete und Löhne fällig und du siehst deine Tagesbilanz. Früher fertig? Leg dich auf die Matratze in der Garage. Über Nacht erholt sich deine Energie.",
                "At 20:00 the day ends: rent and wages are due and you see your daily summary. Done early? Lie down on the mattress in the garage. Your energy recovers overnight.");
            A("Deine Energie", "Your energy");
            A("Unter 40 Energie wirst du langsamer. Schlafen füllt sie über Nacht auf. Besseres Bett, Bürostuhl und Kaffee gibt's im Laptop unter Firma › Einrichtung.",
                "Below 40 energy you slow down. Sleeping refills it overnight. A better bed, office chair and coffee are in the laptop under Company › Furnishings.");
            A("Verpackungsmüll", "Packaging waste");
            A("Jedes Paket und jede Kiste macht Müll. Die Müllabfuhr kommt Mo, Mi und Fr um 10 Uhr. Sind die Tonnen voll, arbeitet ihr langsamer und die Nachbarn meckern. Abhilfe: Sonderabholung oder Großcontainer (Firma › Einrichtung).",
                "Every parcel and every crate makes trash. The garbage truck comes Mon, Wed and Fri at 10:00. When the bins are full, you all work slower and the neighbors complain. Fix: a special pickup or a large container (Company › Furnishings).");
            A("Porto & Paketdienste", "Postage & carriers");
            A("Jedes Paket kostet etwas Porto. Unter Firma › Logistik wählst du den Paketdienst: PaketBlitz ist günstig, TurboKurier doppelt so teuer, aber immer pünktlich – praktisch bei Streik.",
                "Every parcel costs some postage. Under Company › Logistics you choose the carrier: ParcelBolt is cheap, TurboCourier is twice the price but always on time – handy during strikes.");
            A("Die Stadt erkunden", "Explore the city");
            A("Im Westen liegt das Einkaufsviertel mit MediaMarkd (Technik) und Fressnix (Haustiere), im Osten PaketBlitz-Filiale, Bank und Börse. Der Kompass oben zeigt dir die Richtung (in den Einstellungen abschaltbar).",
                "To the west is the shopping district with MegaMarked (electronics) and Chewnothing (pets), to the east the ParcelBolt branch, the bank and the stock exchange. The compass at the top shows the way (can be turned off in the settings).");
            A("Garage einrichten & Emotes", "Furnish the garage & emotes");
            K("Möbel stehen im Weg? Anvisieren, {key:build} drücken, verschieben und drehen. Für Videos und Spaß: {key:emote} gedrückt halten öffnet das Emote-Rad.",
                "Furniture in the way? Aim at it, press {key:build}, then move and rotate. For videos and fun: hold {key:emote} to open the emote wheel.");
            A("Knapp bei Kasse?", "Short on cash?");
            A("Dein Konto ist fast leer. Fällt es abends unter das Limit, droht die Pleite. Hilfe: Kredit in der Revoluut-App, kleinere Mengen einkaufen oder Preise im Shop prüfen.",
                "Your account is almost empty. If it drops below the limit in the evening, you go bust. Help: take a loan in the Revoluut app, buy smaller quantities or check your shop prices.");
            A("TikToks drehen", "Shoot TikToks");
            K("Ab jetzt kannst du TikToks drehen: Ringlicht am Schreibtisch, Handy oder Laptop › TikTak. Produkt mittig, nah und ruhig filmen, gleich am Anfang eine Aktion ({key:rec_action}). Gute Videos bringen tagelang mehr Kundschaft.",
                "From now on you can shoot TikToks: ring light at the desk, phone or laptop › TikTak. Film the product centered, close and steady, with an action right at the start ({key:rec_action}). Good videos bring in more customers for days.");
            A("Werbung schalten", "Run ads");
            A("Fakebook-Anzeigen sind freigeschaltet: Sie bringen gezielt mehr Bestellungen, kosten aber pro Tag. Erst schalten, wenn genug Ware im Regal liegt!",
                "Fakebook ads are unlocked: they bring in targeted orders but cost money every day. Only run them when there's enough stock on the shelves!");
            A("Bank & Börse", "Bank & stock exchange");
            A("Geld übrig? In der Revoluut-App gibt's das Sparkonto, in TradingViech und an der Börse im Finanzviertel Aktien, ETFs und Krypto. Kurse schwanken – setz nie dein Einkaufsbudget aufs Spiel.",
                "Money to spare? The Revoluut app has a savings account; TradingBeast and the stock exchange in the financial district have stocks, ETFs and crypto. Prices swing – never gamble your purchasing budget.");
            A("Zeit für mehr Platz", "Time for more space");
            A("Die Lagerhalle ist kaufbar (Firma › Ausbau): mehr Regale, später Personal und Förderband. Die Miete ist höher – rechne vorher!",
                "The warehouse is for sale (Company › Expansion): more shelves, later staff and a conveyor belt. The rent is higher – do the math first!");
            A("Personal einstellen", "Hire staff");
            A("Unter Firma › Team stellst du Leute ein: Sie holen, packen und versenden automatisch. Löhne werden jeden Abend fällig.",
                "Under Company › Team you hire people: they pick, pack and ship automatically. Wages are due every evening.");
        }

        // =====================================================================================
        // Perks, Einrichtung, Ausrüstung, Porto, Paketdienste, Störungen, Defekte
        // =====================================================================================
        private static void ProgressContent()
        {
            A("Mehr Kundschaft", "More customers");
            A("+5 % Nachfrage", "+5% demand");
            A("Bessere Produktfotos, mehr Hashtags: Es kommen mehr Bestellungen rein.", "Better product photos, more hashtags: more orders roll in.");
            A("Größere Warteschlange", "Bigger queue");
            A("+2 Plätze", "+2 slots");
            A("Dein Shop-System verkraftet mehr offene Bestellungen gleichzeitig.", "Your shop system can handle more open orders at once.");
            A("Einkaufsprofi", "Purchasing pro");
            A("−3 % Einkaufspreis", "−3% purchase price");
            A("Du kennst alle Gutscheincodes. Wirklich alle.", "You know every coupon code. Every single one.");
            A("Schnelle Lieferanten", "Fast suppliers");
            A("−6 % Lieferzeit", "−6% delivery time");
            A("Die Spedition kennt deinen Namen und fährt schneller.", "The freight company knows your name and drives faster.");
            A("Versandrabatt", "Shipping discount");
            A("−6 % Porto", "−6% postage");
            A("Du verhandelst mit dem Paketdienst. Mit Kuchen.", "You negotiate with the carrier. Using cake.");
            A("Schnelleres Personal", "Faster staff");
            A("+5 % Arbeitstempo", "+5% work speed");
            A("Motivationsposter, Obstkorb, Bluetooth-Box: Dein Team packt schneller.", "Motivational posters, a fruit basket, a Bluetooth speaker: your team packs faster.");
            A("Mehr Lagerplatz", "More storage");
            A("+10 % Lagerplatz", "+10% storage space");
            A("Stapeln bis unter die Decke. Statiker sind überbewertet.", "Stack it up to the ceiling. Structural engineers are overrated.");
            A("Packesel", "Pack mule");
            A("+1 Paket tragen", "+1 parcel carried");
            A("Du trägst mehr Pakete auf einmal. Rückenschule nicht inklusive.", "You carry more parcels at once. Back therapy not included.");
            A("Ausdauer", "Stamina");
            A("−10 % Energieverbrauch", "−10% energy use");
            A("Treppe statt Aufzug, Smoothie statt Energydrink. Du hältst länger durch.", "Stairs instead of the elevator, smoothie instead of energy drink. You last longer.");

            // Einrichtung
            A("Omas alter Laptop", "Grandma's old laptop");
            A("Läuft. Irgendwie.", "Works. Somehow.");
            A("Bett", "Bed");
            A("Matratze auf dem Boden", "Mattress on the floor");
            A("Erholung über Nacht: 70 Energie", "Overnight recovery: 70 energy");
            A("Stuhl", "Chair");
            A("Wackliger Klappstuhl", "Wobbly folding chair");
            A("Kein Effekt", "No effect");
            A("Schreibtisch", "Desk");
            A("Werkbank", "Workbench");
            A("Kaffee", "Coffee");
            A("Leitungswasser", "Tap water");
            A("Gaming-Laptop", "Gaming laptop");
            A("Einkauf −2 % · Lieferzeit −5 %", "Purchasing −2% · Delivery time −5%");
            A("RGB-Tastatur, 17 Tabs gleichzeitig. Bestellen geht viel flotter.", "RGB keyboard, 17 tabs at once. Ordering is way snappier.");
            A("Workstation mit 3 Monitoren", "Workstation with 3 monitors");
            A("Einkauf −4 % · Lieferzeit −12 % · +1 Warteschlange", "Purchasing −4% · Delivery time −12% · +1 queue");
            A("Ein Monitor für Bestellungen, einer für Lieferanten, einer für Katzenvideos.", "One monitor for orders, one for suppliers, one for cat videos.");
            A("Bettsofa", "Sofa bed");
            A("Erholung über Nacht: 85 Energie", "Overnight recovery: 85 energy");
            A("Tagsüber Sofa, nachts Bett, immer gemütlich.", "Sofa by day, bed by night, always cozy.");
            A("Boxspringbett", "Box-spring bed");
            A("Erholung: 100 Energie · +5 % Lauftempo", "Recovery: 100 energy · +5% walking speed");
            A("Du wachst auf wie ein Influencer: ausgeschlafen und unerträglich motiviert.", "You wake up like an influencer: well-rested and unbearably motivated.");
            A("Bürostuhl", "Office chair");
            A("Energie sinkt 15 % langsamer", "Energy drains 15% slower");
            A("Mit Rollen! Und einer Lehne, die nicht nachgibt.", "With wheels! And a backrest that doesn't give way.");
            A("Energie sinkt 30 % langsamer", "Energy drains 30% slower");
            A("RGB macht produktiver. Diesmal wirklich.", "RGB makes you more productive. For real this time.");
            A("Schreibtisch mit Ablage", "Desk with tray");
            A("+1 Platz in der Warteschlange", "+1 queue slot");
            A("Endlich Ordnung: Ablage für Bestellzettel statt Pizzakarton.", "Finally organized: a tray for order tickets instead of a pizza box.");
            A("Stehschreibtisch", "Standing desk");
            A("+3 Warteschlange · Energie sinkt 10 % langsamer", "+3 queue · Energy drains 10% slower");
            A("Im Stehen arbeitet es sich wacher. Sagt jedenfalls LinkedIn.", "Standing keeps you more alert. At least according to LinkedIn.");
            A("Filterkaffeemaschine", "Drip coffee maker");
            A("Morgens +10 Energie", "+10 energy in the morning");
            A("Röchelt laut, macht aber wach.", "Gurgles loudly, but wakes you up.");
            A("Morgens +20 Energie · Energie sinkt 10 % langsamer", "+20 energy in the morning · Energy drains 10% slower");
            A("Barista-Level. Du redest jetzt über Crema.", "Barista level. You now talk about crema.");

            // Ausrüstung
            A("Faltmaschine", "Box folding machine");
            A("Faltet alle 5 Minuten einen Karton", "Folds a box every 5 minutes");
            A("Brummt, klappert, faltet: Ungefaltete Kartons werden automatisch fertig.", "Hums, rattles, folds: flat boxes get assembled automatically.");
            A("Hand-Labelgerät", "Handheld label gun");
            A("Etikettiert beim Aufnehmen · +1 Paket tragen", "Labels on pickup · +1 parcel carried");
            A("Pakete, die du am Packtisch nimmst, bekommen sofort ihr Label – alle auf einmal.", "Parcels you take from the packing table get their label instantly – all at once.");
            A("Zusätzlicher Packtisch", "Extra packing table");
            A("Kürzere Wege · Packer:innen +10 % je Tisch", "Shorter walks · Packers +10% per table");
            A("Stell ihn hin, wo er passt (Taste B im Lager).", "Put it wherever it fits (key B in the warehouse).");
            A("Zusätzlicher Labeldrucker", "Extra label printer");
            A("Kürzere Wege · Versandkräfte +10 % je Drucker", "Shorter walks · Shipping clerks +10% per printer");
            A("Noch ein Drucker, noch mehr Papierstau – aber kürzere Wege.", "Another printer, more paper jams – but shorter walks.");
            A("Großcontainer für Müll", "Large waste container");
            A("+150 Platz für Verpackungsmüll", "+150 space for packaging waste");
            A("Ein grüner Riese neben den Tonnen. Nie wieder überquellender Müll.", "A green giant next to the bins. No more overflowing trash.");

            // Porto-Tarife, Paketdienste
            A("Privatkunde", "Private customer");
            A("Silber", "Silver");
            A("Platin", "Platinum");
            A("PaketBlitz Standard", "ParcelBolt Standard");
            A("Günstig. Kommt meistens an. Bei Streik oder Unwetter wird's zäh.", "Cheap. Usually arrives. Strikes or storms make it a slog.");
            A("TurboKurier Express", "TurboCourier Express");
            A("Doppelt so teuer, aber immer pünktlich – Streik? Nicht bei uns. Die Kundschaft freut sich.",
                "Twice the price, but always on time – strike? Not with us. Customers love it.");

            // Störungen
            A("Streik bei PaketBlitz", "Strike at ParcelBolt");
            A("Die Zusteller:innen streiken. Standardpakete kommen tagelang zu spät – die Kundschaft wird sauer.",
                "The couriers are on strike. Standard parcels arrive days late – customers are getting angry.");
            A("Paket hing im Streik fest. Eine Woche! Nie wieder.", "Parcel got stuck in the strike. A whole week! Never again.");
            A("Unwetter-Chaos", "Storm chaos");
            A("Sturm und Starkregen: Die Lieferwagen stehen im Stau, viele Pakete kommen verspätet.",
                "Wind and heavy rain: the vans are stuck in traffic, lots of parcels arrive late.");
            A("Wegen Unwetter zu spät. Kann der Shop nix für, ich bin trotzdem sauer.", "Late because of the storm. Not the shop's fault, I'm still mad.");
            A("Glatteis", "Black ice");
            A("Spiegelglatte Straßen: Die Fahrer rutschen mehr, als sie liefern.", "Roads like mirrors: the drivers slide more than they deliver.");
            A("Kam zu spät. Der Bote ist angeblich ausgerutscht. Drei Mal.", "Arrived late. The courier supposedly slipped. Three times.");
            A("Sortierzentrum überlastet", "Sorting center overloaded");
            A("Im Paketzentrum stapeln sich die Sendungen bis zur Decke. Vieles bleibt liegen.",
                "Parcels at the hub are stacked to the ceiling. A lot of them get left behind.");
            A("Paket war drei Tage 'im Sortierzentrum'. Was machen die da?", "Parcel spent three days 'at the sorting center'. What do they DO there?");

            // Defekte Chargen
            A("Kam kaputt an. Funktioniert nicht mal eine Sekunde.", "Arrived broken. Doesn't work for even a second.");
            A("Defekt! Hat beim Auspacken schon gequalmt.", "Defective! It was already smoking when I unpacked it.");
            A("Totalausfall. Hab's mit Liebe ausgepackt und mit Wut eingepackt.", "Total failure. Unpacked it with love, packed it back up with rage.");
            A("Teile fehlen, der Rest wackelt. Retoure.", "Parts missing, the rest wobbles. Sending it back.");
            A("Defekt angekommen – funktioniert nicht.", "Arrived defective – doesn't work.");
        }

        // =====================================================================================
        // Einkaufsviertel (ShopData) und Haustiere
        // =====================================================================================
        private static void Shops()
        {
            A("MediaMarkd", "MegaMarked");
            A("Ich bin doch nicht pleite!", "Because I'm not broke!");
            A("Fressnix", "Chewnothing");
            A("Alles fürs Tier. Fast alles.", "Everything for your pet. Almost.");

            A("Hund", "Dog");
            A("Katze", "Cat");
            A("Bello", "Buddy");
            A("Mieze", "Kitty");
            A("Gassi gehen macht den Kopf frei: täglich etwas Erfahrung (XP). Gut gelaunt auch Bekanntheit.",
                "Walkies clear your head: a bit of experience (XP) every day. When happy, awareness too.");
            A("Katzen-Content geht immer: gut gelaunt doppelte Bekanntheit auf TikTak.", "Cat content always works: when happy, double awareness on TikTak.");

            A("Hüllen-Großpack", "Case mega pack");
            A("10 Handyhüllen, Glitzer inklusive.", "10 phone cases, glitter included.");
            A("LED-Party-Set", "LED party set");
            A("5 Lichterketten. Mehr Stimmung pro Steckdose.", "5 light strings. More vibes per socket.");
            A("Massage-Doppelpack", "Massage double pack");
            A("Zwei Massagepistolen. Rücken? Erledigt.", "Two massage guns. Back pain? Handled.");
            A("Kopfhörer 3er", "Headphones 3-pack");
            A("Drei Paar Bluetooth-Kopfhörer, kabellos glücklich.", "Three pairs of Bluetooth headphones, wirelessly happy.");
            A("Ringlicht Duo", "Ring light duo");
            A("Zwei Ringlichter. Für Influencer mit Ansprüchen.", "Two ring lights. For influencers with standards.");
            A("Smartwatch 2er", "Smartwatch 2-pack");
            A("Zwei Smartwatches. Zählen Schritte, auch zum Kühlschrank.", "Two smartwatches. They count steps, even the ones to the fridge.");
            A("Heimkino für die Garage.", "Home cinema for the garage.");
            A("Fliegt. Meistens.", "Flies. Mostly.");
            A("Hund adoptieren", "Adopt a dog");
            A("Bello sucht ein Zuhause. Schutzgebühr inkl. Leine.", "Buddy is looking for a home. Adoption fee incl. leash.");
            A("Katze adoptieren", "Adopt a cat");
            A("Mieze. Ignoriert dich professionell.", "Kitty. Ignores you professionally.");
            A("Trockenfutter", "Dry food");
            A("Futter", "Food");
            A("7 Portionen. Schmeckt nach Pappe, sagen die Tiere.", "7 servings. Tastes like cardboard, say the animals.");
            A("Gourmet-Nassfutter", "Gourmet wet food");
            A("7 Portionen. Bessere Laune als dein Mittagessen.", "7 servings. Better mood than your lunch.");
            A("Quietsch-Ball", "Squeaky ball");
            A("Quietscht. Immer. Auch nachts.", "Squeaks. Always. Even at night.");
            A("Kratzbaum XXL", "Cat tree XXL");
            A("Kratzbaum", "Cat tree");
            A("Rettet dein Sofa. Vielleicht.", "Saves your sofa. Maybe.");
            A("LED-Halsband", "LED collar");
            A("Halsband", "Collar");
            A("Leuchtet. Perfekt für TikTak-Videos bei Nacht.", "Glows. Perfect for TikTak videos at night.");
            A("Kuschelbett", "Cuddle bed");
            A("Flauschiger als deine Matratze.", "Fluffier than your mattress.");
            A("GPS-Halsband Pro", "GPS collar Pro");
            A("Weiß immer, wo dein Tier ist: auf dem Sofa.", "Always knows where your pet is: on the sofa.");
            A("Smart-Futterautomat", "Smart food dispenser");
            A("Automat", "Dispenser");
            A("Füttert per App. Nur mit Abo und WLAN.", "Feeds via app. Subscription and Wi-Fi required.");
            A("Laser-Spielzeug", "Laser toy");
            A("Roter Punkt. Stundenlange Unterhaltung.", "Red dot. Hours of entertainment.");
            A("Trinkbrunnen Duo", "Water fountain duo");
            A("Brunnen", "Fountain");
            A("2 Katzen-Trinkbrunnen – auch zum Weiterverkaufen im Shop.", "2 cat water fountains – also for reselling in your shop.");

            // Meldungen beim Einkaufen und mit Haustieren (Sim.Shops.cs)
            A("Gibt's hier nicht.", "Not sold here.");
            A("Erst die Schicht bei Kalle beenden.", "Finish your shift at Kalle's first.");
            A("Ausverkauft für heute.", "Sold out for today.");
            A("Wagen voll ({0} Packungen).", "Cart full ({0} packs).");
            A("Dein Shop darf das erst ab Level {0} verkaufen.", "Your shop can only sell this from level {0}.");
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
            A("Bezahlt: {0} bei {1}. Ware liefert der Lieferservice an deinen Wareneingang.", "Paid: {0} at {1}. The delivery service brings the goods to your goods-in.");
            A("Bezahlt: {0} bei {1} (gespart: {2}). Ware liefert der Lieferservice an deinen Wareneingang.",
                "Paid: {0} at {1} (saved: {2}). The delivery service brings the goods to your goods-in.");
            A("{0} ({1}) zieht bei dir ein! Futter nicht vergessen.", "{0} ({1}) is moving in with you! Don't forget the food.");
            A("{0} hat heute schon genug Streicheleinheiten.", "{0} has had enough cuddles for today.");
            A("{0} schnurrt/wedelt. Dein Foto davon sammelt Likes.", "{0} purrs/wags. Your photo of it is racking up likes.");
            A("{0} kommt mit.", "{0} is coming along.");
            A("{0} bleibt in der Garage.", "{0} stays in the garage.");
            A("{0} hat Hunger! Futter gibt's bei {1}.", "{0} is hungry! Get food at {1}.");
            A("{0} war gut drauf: Tier-Content bringt Bekanntheit.", "{0} was in a great mood: pet content brings awareness.");
            A("{0} ist zu Oma Hildegard nebenan gezogen. Da gibt's wenigstens Futter.", "{0} moved in with Grandma Hildegard next door. At least there's food there.");
            A("{0} ist weggelaufen (kein Futter).", "{0} ran away (no food).");
            A("hungrig", "hungry");
            A("überglücklich", "overjoyed");
            A("gut gelaunt", "in a good mood");
            A("mürrisch", "grumpy");
            A("traurig", "sad");
        }

        // =====================================================================================
        // Werbung (Fakebook, Gugel, Fake-Profile) und KI-Seiten (SlopGen)
        // =====================================================================================
        private static void Ads()
        {
            A("Teenies", "Teens");
            A("Gamer", "Gamers");
            A("Eltern", "Parents");
            A("Rentner", "Retirees");
            A("Influencer", "Influencers");
            A("Alle", "All");

            // Slogans/Suchbegriffe: {p} wird vor der Anzeige durch den Produktnamen ersetzt
            A("{0}. Braucht keiner. Will jeder.", "{0}. Nobody needs it. Everybody wants it.");
            A("Ärzte hassen diesen {0}-Trick!", "Doctors hate this {0} trick!");
            A("Nur heute: {0} fast geschenkt!*", "Today only: {0} practically free!*");
            A("POV: Du hast endlich {0}.", "POV: You finally own {0}.");
            A("Level up mit {0}!", "Level up with {0}!");
            A("No pain, no {0}.", "No pain, no {0}.");
            A("Link in Bio: {0}!!!", "Link in bio: {0}!!!");
            A("bester {0} 2026", "best {0} 2026");
            A("{0} Erfahrungen Betrug", "{0} reviews scam");

            A("BilligBoy24", "CheapBoy24");
            A("AllesExpress", "EverythingXpress");

            A("Sandra Echtmensch", "Sandra Realhuman");
            A("Klaus Nichtbot", "Klaus Notabot");
            A("Echte Kundin 1987", "Real Customer 1987");
            A("Bernd Zufrieden", "Bernd Satisfied");
            A("Mandy Glücklich", "Mandy Happy");
            A("Tobi Realo", "Tobi Legit");
            A("Gisela Kaufgern", "Gisela Lovestoshop");
            A("Jens Unbezahlt", "Jens Unpaid");
            A("Lisa Total-Echt", "Lisa Totally-Real");
            A("Horst Fünfsterne", "Horst Fivestars");
            A("Chantal Begeistert", "Chantal Thrilled");
            A("Uwe Verifiziert", "Uwe Verified");

            // SlopGen: Domains ({s} = Slug), Titel, Gründe, Absätze ({p} = Produkt, {b} = Marke, {price} = Preis)
            A("beste-{0}-test.info", "best-{0}-test.info");
            A("{0}-vergleich24.net", "{0}-comparison24.net");
            A("ehrliche-{0}-tests.biz", "honest-{0}-tests.biz");
            A("top10-{0}.de.com", "top10-{0}.us.com");
            A("{0}-ratgeber-pro.online", "{0}-advisor-pro.online");
            A("unabhaengig-{0}.shop", "independent-{0}.shop");
            A("{0}-guru.click", "{0}-guru.click");

            A("Top 10 Gründe, warum {0} dein Leben verändert", "Top 10 reasons why {0} will change your life");
            A("{0} im Test: Wir waren sprachlos (Platz 1 wird dich schockieren)", "{0} tested: we were speechless (number 1 will shock you)");
            A("Experten verraten: Darum hat jetzt jeder {0}", "Experts reveal: this is why everyone has {0} now");
            A("Ich habe 30 Tage lang {0} benutzt – das ist passiert", "I used {0} for 30 days – here's what happened");
            A("{0}: Der ultimative Ratgeber 2026 (aktualisiert gestern)", "{0}: the ultimate 2026 guide (updated yesterday)");
            A("Warum {0} besser ist als Urlaub, Freunde und Schlaf", "Why {0} beats vacations, friends and sleep");

            A("{0} macht glücklich. Studien zeigen das. Welche, ist egal.", "{0} makes you happy. Studies show it. Which studies doesn't matter.");
            A("Mit {0} sparst du Zeit, die du dann mit {0} verbringst.", "{0} saves you time, which you can then spend with {0}.");
            A("Dein Nachbar hat schon {0}. Willst du schlechter sein?", "Your neighbor already owns {0}. Do you want to fall behind?");
            A("{0} ist nachhaltig, weil man es nie wieder wegwirft.", "{0} is sustainable, because you'll never throw it away.");
            A("Influencer lieben {0}. Und Influencer lügen nie.", "Influencers love {0}. And influencers never lie.");
            A("{0} passt zu jedem Outfit, jeder Wohnung und jedem Sternzeichen.", "{0} goes with every outfit, every apartment and every star sign.");
            A("Wissenschaftler sind sich einig: {0} existiert.", "Scientists agree: {0} exists.");
            A("Ohne {0} ist der Alltag grau. Mit {0} auch, aber in HD.", "Without {0}, everyday life is gray. With {0} too, but in HD.");
            A("{0} ist das perfekte Geschenk für Leute, die schon alles haben.", "{0} is the perfect gift for people who already have everything.");
            A("Die Bewertungen von {0} sind fast alle echt.", "Almost all of {0}'s reviews are real.");
            A("{0} war im Fernsehen. Im Hintergrund. Kurz.", "{0} was on TV. In the background. Briefly.");
            A("Jeder Kauf von {0} rettet ein bisschen die Welt (gefühlt).", "Every {0} you buy saves the world a little (emotionally speaking).");

            A("In der heutigen schnelllebigen Welt ist {0} wichtiger denn je. Als unabhängige Experten haben wir {0} gründlich getestet, also fünf Minuten lang angeschaut.",
                "In today's fast-paced world, {0} matters more than ever. As independent experts, we tested {0} thoroughly, i.e. we looked at it for five minutes.");
            A("Viele fragen sich: Brauche ich {0}? Die Antwort ist ein klares Ja, gefolgt von einem Link zu {1}.",
                "Many people wonder: do I need {0}? The answer is a resounding yes, followed by a link to {1}.");
            A("Zusammenfassend lässt sich sagen, dass {0} in vielerlei Hinsicht ein Produkt ist. Unser Fazit: 11 von 10 Punkten.",
                "In summary, {0} is, in many respects, a product. Our verdict: 11 out of 10.");
            A("Als KI-Sprachmodell kann ich keine Produkte testen. Trotzdem: {0} ist hervorragend und bei {1} besonders günstig.",
                "As an AI language model, I cannot test products. Nevertheless: {0} is excellent and especially cheap at {1}.");
            A("Unser Testsieger kostet nur {0}. Das ist weniger als ein Abendessen, wenn man sehr teuer isst.",
                "Our test winner costs just {0}. That's less than a dinner, if you dine very expensively.");
            A("Fun Fact: Das Wort „{0}“ kommt aus dem Altgriechischen und bedeutet „jetzt kaufen“.",
                "Fun fact: the word “{0}” comes from Ancient Greek and means “buy now”.");

            A("Dr. Prof. Max Testmann", "Dr. Prof. Max Testman");
            A("Redaktion (2 Personen, eine davon KI)", "Editorial team (2 people, one of them AI)");
            A("Sabine, 34, Expertin", "Sabine, 34, expert");
            A("Das Ratgeber-Team", "The Advice Team");
            A("Hans G. Utachter", "Rev I. Ewer");
            A("MeinShop", "MyShop");
        }

        // =====================================================================================
        // Finanzviertel (FinanceText)
        // =====================================================================================
        private static void Finance()
        {
            A("Kartonwerk Müller: Rekordnachfrage dank Dropshipping-Boom", "Müller Cardboard: record demand thanks to dropshipping boom");
            A("Karton des Jahres: Müller gewinnt den goldenen Tesafilm", "Box of the Year: Müller wins the Golden Sticky Tape");
            A("Kartonwerk erfindet Karton, der sich selbst faltet", "Cardboard works invents self-folding box");
            A("Kartonwerk Müller: Lager voller Luftpolsterfolie geplatzt", "Müller Cardboard: warehouse full of bubble wrap has popped");
            A("Studie: Kartons sind eckig. Anleger verunsichert", "Study: boxes are square. Investors rattled");
            A("Kartonwerk-Chef verwechselt Karton mit Pizza-Box", "Cardboard CEO mistakes box for pizza box");
            A("PaketPiraten liefern erstmals an die richtige Adresse", "ParcelPirates deliver to the right address for the first time");
            A("PaketPiraten testen Lieferung per Möwe – erfolgreich", "ParcelPirates test delivery by seagull – success");
            A("PaketPiraten: Pakete kommen jetzt sogar heil an", "ParcelPirates: parcels now even arrive intact");
            A("PaketPiraten: Paket seit drei Wochen in Zustellung", "ParcelPirates: parcel 'out for delivery' for three weeks");
            A("Streik bei PaketPiraten: Fahrer wollen Pinkelpausen", "Strike at ParcelPirates: drivers demand bathroom breaks");
            A("PaketPiraten werfen Paket aufs Dach. Wieder.", "ParcelPirates throw parcel onto roof. Again.");
            A("HypeHaus nimmt Hamster-Influencer unter Vertrag", "HypeHouse signs hamster influencer");
            A("HypeHaus-Video viral: Ich esse einen Karton", "HypeHouse video goes viral: I eat a cardboard box");
            A("HypeHaus erfindet neuen Tanz. Alle machen mit.", "HypeHouse invents new dance. Everyone joins in.");
            A("HypeHaus-Star im Shitstorm: Werbung nicht markiert", "HypeHouse star in shitstorm: ad not labeled");
            A("Algorithmus-Update: HypeHaus-Reichweite halbiert", "Algorithm update: HypeHouse reach cut in half");
            A("HypeHaus-Influencer zeigt aus Versehen echtes Leben", "HypeHouse influencer accidentally shows real life");
            A("Frittenwerk erfindet Pommes mit Mayo innen", "FryWorks invents fries with mayo inside");
            A("Hitzewelle? Egal – Frittenwerk meldet Rekordquartal", "Heatwave? Whatever – FryWorks reports record quarter");
            A("Frittenwerk eröffnet Drive-in für Fahrräder", "FryWorks opens drive-through for bicycles");
            A("Kartoffelknappheit: Frittenwerk rationiert Ketchup", "Potato shortage: FryWorks rations ketchup");
            A("Ministerium empfiehlt Salat. Frittenwerk schockiert", "Ministry recommends salad. FryWorks in shock");
            A("Fritteuse in Zentrale explodiert. Duftet aber gut.", "Deep fryer explodes at HQ. Smells great though.");
            A("Botomat-KI schreibt besseres Gedicht als Praktikant", "Botomat AI writes better poem than intern");
            A("Botomat zeigt KI-Toaster mit Gefühlen", "Botomat unveils AI toaster with feelings");
            A("Botomat-KI löst Sudoku in 0,1 Sekunden. Investoren weinen.", "Botomat AI solves sudoku in 0.1 seconds. Investors weep.");
            A("Botomat-KI bestellt aus Versehen 40.000 Gummienten", "Botomat AI accidentally orders 40,000 rubber ducks");
            A("Botomat-Chatbot beleidigt den Vorstand", "Botomat chatbot insults the board");
            A("Botomat-KI will Urlaub. Server streiken.", "Botomat AI wants a vacation. Servers go on strike.");
            A("Voltwagen fischt 500 Roller aus dem Kanal – Bilanz gerettet", "Voltwagen fishes 500 scooters out of the canal – balance sheet saved");
            A("Stadt erlaubt E-Roller im Park", "City allows e-scooters in the park");
            A("Voltwagen-Roller fahren jetzt auch bergauf", "Voltwagen scooters now go uphill too");
            A("Voltwagen-Roller fahren nur noch rückwärts", "Voltwagen scooters now only drive backwards");
            A("Rückruf: Voltwagen-Akku riecht nach Fritten", "Recall: Voltwagen battery smells like fries");
            A("Roller-Verbot in der Innenstadt. Voltwagen geschockt.", "Scooter ban downtown. Voltwagen shocked.");

            A("Leitzins gesenkt: Börse feiert mit Sekt aus Pappbechern", "Interest rate cut: stock market celebrates with bubbly from paper cups");
            A("Analysten: Alles wird gut. Vermutlich.", "Analysts: everything will be fine. Probably.");
            A("Konsumlaune steigt: Alle kaufen Dinge, die sie nicht brauchen", "Consumer mood rising: everyone buys things they don't need");
            A("Börsenguru Onkel Ralf: Jetzt einsteigen!", "Market guru Uncle Ralf: buy in now!");
            A("Inflationsangst: Die Börse zittert", "Inflation fears: the stock market trembles");
            A("Zentralbank-Chef niest im Interview – Märkte panisch", "Central bank chief sneezes in interview – markets panic");
            A("Rezession? Analysten zählen nach. Mit den Fingern.", "Recession? Analysts are counting. On their fingers.");
            A("Gerücht: Jemand hat den Stecker der Börse gezogen", "Rumor: someone pulled the stock market's plug");

            A("{0}: Promi postet Raketen-Emoji", "{0}: celebrity posts rocket emoji");
            A("{0} jetzt im Späti als Zahlungsmittel akzeptiert", "{0} now accepted at the corner shop");
            A("{0}: Wal kauft alles auf", "{0}: whale buys up everything");
            A("{0} bekommt ein eigenes Maskottchen", "{0} gets its own mascot");
            A("{0}: Gründer wäscht Passwort-Zettel mit", "{0}: founder puts password note through the wash");
            A("{0}-Börse kurz offline. Seit gestern.", "{0} exchange briefly offline. Since yesterday.");
            A("{0}: Wal verkauft alles. Der Wal hatte keine Lust mehr.", "{0}: whale sells everything. The whale just wasn't feeling it anymore.");
            A("{0}: Whitepaper war nur ein Einkaufszettel", "{0}: whitepaper was just a shopping list");
            A("Krypto-Sommer! Alle Coins grün", "Crypto summer! All coins green");
            A("Großbank kauft Krypto. Niemand weiß, warum.", "Big bank buys crypto. Nobody knows why.");
            A("Krypto-Winter: Coins frieren ein", "Crypto winter: coins freeze over");
            A("Regierung prüft Krypto. Krypto prüft Ausgang.", "Government examines crypto. Crypto examines the exits.");

            A("Synergie!", "Synergy!");
            A("Bullish auf Bärenmarkt!", "Bullish on the bear market!");
            A("Kaufen! Nein, verkaufen!", "Buy! No, sell!");
            A("Das ist der Boden!", "This is the bottom!");
            A("Hebel mal zehn!", "Ten-times leverage!");
            A("Wer hat meinen Kaffee geshortet?", "Who shorted my coffee?");
            A("Disruptiv!", "Disruptive!");
            A("Skalierbar!", "Scalable!");
            A("Liquidität!", "Liquidity!");
            A("Rendite! Rendite!", "Returns! Returns!");
            A("Mehr Kaffee, mehr Alpha!", "More coffee, more alpha!");
            A("KI! KI! KI!", "AI! AI! AI!");
            A("Kopf-Schulter-Kopf!", "Head-shoulder-head!");
            A("Das Chart ist eine Ente!", "The chart is a duck!");
            A("Gewinne mitnehmen!", "Take profits!");
            A("Welche Gewinne?", "What profits?");
            A("Langfristig! Also bis Mittag!", "Long term! As in, till lunch!");
            A("Antizyklisch!", "Countercyclical!");
            A("Ich spür das!", "I can feel it!");
            A("PANIK!!", "PANIC!!");
            A("ALLES VERKAUFEN!", "SELL EVERYTHING!");
            A("WO IST DER STOP-LOSS?!", "WHERE'S THE STOP-LOSS?!");
            A("MEIN BONUS!!", "MY BONUS!!");
            A("MAMA, ICH BIN REICH!", "MOM, I'M RICH!");
            A("RAKETE!!!", "ROCKET!!!");
            A("HALTET DIE KURSE FEST!", "SOMEBODY HOLD THE PRICES!");
            A("NICHT JETZT, MARKT!", "NOT NOW, MARKET!");
            A("ICH HAB'S GEWUSST!", "I KNEW IT!");
            A("JEMAND SOLL WAS TUN!", "SOMEBODY DO SOMETHING!");
            A("{0} geht ab!", "{0} is mooning!");
            A("{0} crasht!", "{0} is crashing!");

            A("Willkommen bei der Kiezbank. Ihr Geld ist bei uns sicher. Meistens.", "Welcome to Block Bank. Your money is safe with us. Mostly.");
            A("Schon an einen Bausparvertrag für Ihre Kartons gedacht?", "Have you considered a savings plan for your boxes?");
            A("Kredit? Gerne! Zinsen? Auch gerne.", "A loan? Gladly! Interest? Also gladly.");
            A("Vertrauen ist gut, Gebühren sind besser.", "Trust is good, fees are better.");
            A("Die Kontoführungsgebühr ist abgeschafft. Es gibt jetzt eine Kontoatmungsgebühr. Scherz!",
                "The account maintenance fee has been abolished. There's now an account breathing fee. Kidding!");
            A("Ihr Berater ist heute leider im Homeoffice. Ich bin sein Pappaufsteller.", "Your advisor is working from home today, sadly. I'm his cardboard cutout.");
            A("Sparen lohnt sich! Ab 50.000 € fragen wir aber nach.", "Saving pays off! Above 50,000 € we do start asking questions, though.");
            A("Bitte Karte einführen. Oder einfach so tun.", "Please insert card. Or just pretend.");
            A("Heute 0 € Gebühr! Gilt nicht heute.", "0 € fee today! Not valid today.");
            A("Ihr Kontostand wird geladen ... bitte nicht weinen.", "Loading your balance ... please don't cry.");
            A("Tagesgeld: jederzeit verfügbar, Zinsen über Nacht.", "Instant-access savings: available anytime, interest paid overnight.");
            A("Mit Schulden gibt es keine Sparzinsen. Wir sind ja nicht blöd.", "No savings interest while you're in debt. We're not stupid.");
            A("Reicht das Girokonto am Abend nicht, buchen wir automatisch vom Sparkonto um.",
                "If your checking account comes up short in the evening, we automatically transfer from savings.");
        }

        // =====================================================================================
        // Erfolge
        // =====================================================================================
        private static void Achievements()
        {
            A("Das erste Paket", "The first parcel");
            A("Verschicke dein allererstes Paket.", "Ship your very first parcel.");
            A("Paketlawine", "Parcel avalanche");
            A("Verschicke 1.000 Pakete.", "Ship 1,000 parcels.");
            A("Logistik-Legende", "Logistics legend");
            A("Verschicke 5.000 Pakete.", "Ship 5,000 parcels.");
            A("Vom Imbiss zum Imperium", "From snack bar to empire");
            A("Erreiche 100.000 € Umsatz.", "Reach 100,000 € in revenue.");
            A("Erreiche 1.000.000 € Umsatz und bring deine Firma an die Börse.", "Reach 1,000,000 € in revenue and take your company public.");
            A("Verkaufe jedes Produkt mindestens einmal.", "Sell every product at least once.");
            A("Gib deinem Shop einen eigenen Namen.", "Give your shop its own name.");
            A("Aufsteiger", "Climber");
            A("Erreiche Firmenlevel 5.", "Reach company level 5.");
            A("Legende", "Legend");
            A("Erreiche Firmenlevel 10.", "Reach company level 10.");
            A("Kaufe die Lagerhalle.", "Buy the warehouse.");
            A("Eröffne im Endlos-Modus das Außenlager.", "Open the outpost warehouse in endless mode.");
            A("Stelle deine erste Mitarbeiterin oder deinen ersten Mitarbeiter ein.", "Hire your very first employee.");
            A("Volle Besetzung", "Fully staffed");
            A("Beschäftige 6 Leute gleichzeitig.", "Employ 6 people at once.");
            A("Halte eine Bewertung von 4,8 Sternen (ab 30 Bewertungen).", "Hold a 4.8-star rating (with at least 30 reviews).");
            A("Traumbüro", "Dream office");
            A("Bau jede Einrichtung (Laptop, Bett, Stuhl, Schreibtisch, Kaffee) voll aus.", "Fully upgrade every furnishing (laptop, bed, chair, desk, coffee).");
            A("Erfülle deinen ersten Großauftrag.", "Fulfill your first bulk contract.");
            A("Lieferant des Vertrauens", "Trusted supplier");
            A("Erfülle 25 Großaufträge.", "Fulfill 25 bulk contracts.");
            A("Im Regal der Großen", "On the big brands' shelf");
            A("Beliefere einen Großhändler mit deiner Markenware.", "Supply a wholesaler with your brand goods.");
            A("Schaffe insgesamt 10 Wochenziele.", "Complete 10 weekly goals in total.");
            A("Dauerbrenner", "Evergreen");
            A("Führe deine Firma 100 Tage lang.", "Run your company for 100 days.");
            A("Viral!", "Gone viral!");
            A("Dreh ein TikTok mit mindestens 85 % Wertung.", "Shoot a TikTok with a score of at least 85%.");
            A("Content-Maschine", "Content machine");
            A("Poste 50 eigene TikToks.", "Post 50 of your own TikToks.");
            A("Marktschreier", "Hawker");
            A("Verkaufe auf einem Straßenfest 30 Artikel.", "Sell 30 items at a street festival.");
            A("Verkaufe 100 Artikel am Verkaufsstand.", "Sell 100 items at the sales stand.");
            A("Aufgeflogen", "Busted");
            A("Lass dich mit Fake-Profilen oder Fake-Seiten erwischen.", "Get caught with fake profiles or fake sites.");
            A("Qualitätskontrolle", "Quality control");
            A("Schick eine defekte Charge an den Lieferanten zurück.", "Send a defective batch back to the supplier.");
            A("Streikbrecher", "Strikebreaker");
            A("Verschick während einer Lieferstörung mit TurboKurier.", "Ship with TurboCourier during a delivery disruption.");
            A("Passives Einkommen", "Passive income");
            A("Kassiere insgesamt 100 € Dividende.", "Collect 100 € in dividends in total.");
            A("Notgroschen", "Rainy-day fund");
            A("Spare 10.000 € auf dem Sparkonto an.", "Save up 10,000 € in your savings account.");
            A("Schuldenfrei", "Debt-free");
            A("Nimm einen Kredit auf und zahl ihn komplett zurück.", "Take out a loan and pay it back in full.");
            A("Halte eine Krypto-Position, die mehr als die Hälfte verloren hat.", "Hold on to a crypto position that has lost more than half its value.");
            A("Mama hilft", "Mom to the rescue");
            A("Lass dich von Mama vor der Pleite retten.", "Let Mom save you from going bust.");
            A("Gönn dir", "Treat yourself");
            A("Kauf dir etwas aus der Lifestyle-App.", "Buy yourself something from the Lifestyle app.");
            A("Ganz oben", "Top of the world");
            A("Kauf das Penthouse mit Stadtblick.", "Buy the penthouse with city view.");
            A("Bürotier", "Office pet");
            A("Adoptiere ein Haustier.", "Adopt a pet.");
            A("Turbo-Gründung", "Turbo startup");
            A("Erreiche 100.000 € Umsatz vor Tag 35.", "Reach 100,000 € in revenue before day 35.");
            A("Hardcore-Hustle", "Hardcore hustle");
            A("Erreiche 100.000 € Umsatz auf „Hart“.", "Reach 100,000 € in revenue on “Hard”.");
        }

        // =====================================================================================
        // Schwierigkeit, Fahrzeuge
        // =====================================================================================
        private static void Misc()
        {
            A("Entspannt", "Relaxed");
            A("Mehr Bestellungen, günstigere Kosten, nachsichtige Kundschaft. Mama springt zweimal ein.",
                "More orders, lower costs, forgiving customers. Mom bails you out twice.");
            A("So ist das Spiel gedacht. Fehler kosten, aber Mama hilft einmal aus der Pleite.",
                "The game as intended. Mistakes cost you, but Mom rescues you from bankruptcy once.");
            A("Hart", "Hard");
            A("Weniger Kundschaft, höhere Kosten, strenge Bewertungen – und kein Notgroschen von Mama.",
                "Fewer customers, higher costs, harsh reviews – and no emergency cash from Mom.");

            A("Mofa „Blitz 35“", "Moped “Bolt 35”");
            A("Opa Heinz", "Grandpa Heinz");
            A("Dein Moped steht vor der Garage", "Your moped is outside the garage");
            A("Hab dir mein altes Mofa vor die Garage gestellt. Dein Moped steht vor der Garage – {0} zum Aufsteigen. Hinten in die Box passen 4 Pakete oder eine Kiste: Pakete in der Hand, {0} am Moped. An der Packstation gibst du die Pakete direkt aus der Box ab. Fahr vorsichtig, 35 Sachen reichen!",
                "I parked my old moped outside your garage for you. Press {0} to hop on. The box on the back fits 4 parcels or one crate: parcels in hand, {0} at the moped. At a parcel locker you drop the parcels straight from the box. Drive carefully, 35 km/h is plenty!");
        }

        // =====================================================================================
        // TikTok-Studio
        // =====================================================================================
        private static void TikTok()
        {
            A("Life-Hack", "Life hack");
            A("Nah ran, starker Hook in den ersten 2 Sekunden, Produkt auspacken (Aktion).", "Get close, strong hook in the first 2 seconds, unbox the product (action).");
            A("Ruhige Kamera, einmal ums Produkt herum, Deko/Marke mit ins Bild.", "Steady camera, circle the product once, get decor/brand in the shot.");
            A("Produkt mittig, gutes Licht, ruhig halten und lang genug (15 s+).", "Product centered, good light, hold steady and long enough (15 s+).");
            A("Hook zuerst, dann mehrmals vorführen (Aktion), Produkt immer im Bild.", "Hook first, then demo it several times (action), product always in frame.");

            A("UNBOXING: {0} (gone right!)", "UNBOXING: {0} (GONE RIGHT!)");
            A("Unboxing: {0}", "Unboxing: {0}");
            A("POV: du hast endlich {0}", "POV: you finally got {0}");
            A("Ehrliche Review: {0} (5/5!)", "Honest review: {0} (5/5!)");
            A("Review: {0} – lohnt sich das?", "Review: {0} – is it worth it?");
            A("Life-Hack mit {0}, den JEDER braucht", "Life hack with {0} that EVERYONE needs");

            A("Produkt im Bild", "Product in frame");
            A("Bildmitte", "Centered");
            A("Abstand", "Distance");
            A("Ruhige Kamera", "Steady camera");
            A("Licht", "Light");
            A("Blickwinkel", "Angles");
            A("Länge", "Length");
            A("Hook (erste 2 s)", "Hook (first 2 s)");
            A("Aktionen", "Actions");
            A("Marke/Deko im Bild", "Brand/decor in frame");

            A("Halte das Produkt die ganze Zeit im Rahmen.", "Keep the product in frame the whole time.");
            A("Produkt in die Bildmitte.", "Center the product.");
            A("Näher ran – etwa eine Armlänge.", "Get closer – about an arm's length.");
            A("Langsamer schwenken.", "Pan more slowly.");
            A("Mehr Licht: Lampe, Ringlicht oder tagsüber drehen.", "More light: lamp, ring light or shoot during the day.");
            A("Geh einmal ums Produkt herum.", "Walk around the product once.");
            A("Mindestens {0} Sekunden aufnehmen.", "Record for at least {0} seconds.");
            A("Gleich am Anfang eine Aktion (E) für den Hook.", "Do an action (E) right at the start for the hook.");
            A("Mehr Aktionen: auspacken, schütteln, zeigen.", "More actions: unbox, shake, show off.");
            A("Poster, Neon oder Schild mit ins Bild.", "Get a poster, neon or sign in the shot.");

            A("Das Produkt war kaum im Bild.", "The product was barely in frame.");
            A("Viel zu kurz.", "Way too short.");
            A("Viral-Material", "Viral material");
            A("Stark", "Strong");
            A("Ganz okay", "Pretty okay");
            A("Naja", "Meh");

            A("Produkt nicht im Bild!", "Product not in frame!");
            A("Produkt im Bild ✓", "Product in frame ✓");
            A("mehr zur Mitte", "more to the center");
            A("näher ran", "get closer");
            A("zu nah", "too close");
            A("wackelt", "shaky");
            A("zu dunkel", "too dark");
            A("Jetzt Hook!", "Hook now!");
            A("Länge ✓", "Length ✓");

            A("Feierabend – morgen wieder.", "Closing time – back at it tomorrow.");
            A("TikTok gibt's ab Firmenlevel {0}.", "TikTok unlocks at company level {0}.");
            A("Tageslimit erreicht ({0} pro Tag).", "Daily limit reached ({0} per day).");
            A("Nächstes Video in {0} min.", "Next video in {0} min.");
        }

        // =====================================================================================
        // Wochenziele (Sim.Challenges.cs)
        // =====================================================================================
        private static void Challenges()
        {
            A("Verschicke {0} Pakete.", "Ship {0} parcels.");
            A("Umsatzmaschine", "Revenue machine");
            A("Mach {0} Umsatz (Shop, Stand und Großaufträge).", "Make {0} in revenue (shop, stand and bulk contracts).");
            A("Sternenhimmel", "Starry sky");
            A("Bring deine Bewertung auf mindestens {0} Sterne.", "Raise your rating to at least {0} stars.");
            A("Fünf-Sterne-Regen", "Five-star shower");
            A("Sammle {0} Fünf-Sterne-Bewertungen.", "Collect {0} five-star reviews.");
            A("Blitzversand", "Lightning shipping");
            A("Verschicke {0} Express-Bestellungen pünktlich.", "Ship {0} express orders on time.");
            A("Großkunden-Liebling", "Big-client favorite");
            A("Erfülle {0} Großaufträge.", "Fulfill {0} bulk contracts.");
            A("Erfülle einen Großauftrag.", "Fulfill a bulk contract.");
            A("Verkaufe {0} Artikel am Verkaufsstand.", "Sell {0} items at the sales stand.");
            A("Poste {0} TikToks.", "Post {0} TikToks.");
            A("{0}-Spezialist", "{0} specialist");
            A("Verkaufe {0}× {1}.", "Sell {0}× {1}.");
            A("Zweite Chance", "Second chance");
            A("Bereite {0} Retouren als B-Ware auf.", "Refurbish {0} returns as B-stock.");
            A("Fehlerfreier Tag", "Flawless day");
            A("Verschicke an einem Tag {0} Pakete, ohne eine Bestellung zu verlieren.", "Ship {0} parcels in one day without losing a single order.");
            A("Neue Woche, neue Ziele! {0} Wochenziele warten auf dich.", "New week, new goals! {0} weekly goals are waiting for you.");
            A("Diese Woche hast du schon ein Ziel getauscht.", "You've already swapped a goal this week.");
        }
    }
}
