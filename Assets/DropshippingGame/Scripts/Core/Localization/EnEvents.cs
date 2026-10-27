using System.Collections.Generic;
using System.Text;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Englische Übersetzungen: Ereignisse und Mails (EventData.cs, EventSystem.cs, Trends, Market-News, Festival, Endgame).
    /// Einträge: Add("Deutsch", "English"); Platzhalter {0}, {1} … für eingesetzte Werte (Zahlen, Namen).
    /// Ereignistexte dürfen hier die benannten Platzhalter aus <see cref="EventSystem.Fill"/> bzw.
    /// <see cref="TrendSystem.FillReason"/> enthalten ({product}, {tag}, {company}, {n}, {brand}, {p}) –
    /// <see cref="E"/> macht daraus Vorlagen mit {0}, {1} …, weil die Platzhalter vor der Anzeige ersetzt werden.
    /// </summary>
    internal static class EnEvents
    {
        private static readonly string[] Named = { "product", "tag", "company", "n", "brand", "p" };

        public static void Register()
        {
            System.Action<string, string> A = Lang.Add;
            Events(A);
            Effects(A);
            TrendReasons(A);
        }

        /// <summary>Eintrag mit benannten Platzhaltern ({product} …) → Vorlage mit {0}, {1} … in Reihenfolge des deutschen Texts.</summary>
        private static void E(string de, string en)
        {
            var order = new List<string>();
            string deT = Convert(de, order, true);
            string enT = Convert(en, order, false);
            Lang.Add(deT, enT);
        }

        private static string Convert(string s, List<string> order, bool collect)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('{') < 0) return s;
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '{')
                {
                    int close = s.IndexOf('}', i);
                    if (close > i)
                    {
                        string name = s.Substring(i + 1, close - i - 1);
                        if (System.Array.IndexOf(Named, name) >= 0)
                        {
                            int idx = order.IndexOf(name);
                            if (idx < 0 && collect)
                            {
                                order.Add(name);
                                idx = order.Count - 1;
                            }
                            if (idx >= 0)
                            {
                                sb.Append('{').Append(idx).Append('}');
                                i = close + 1;
                                continue;
                            }
                        }
                    }
                }
                sb.Append(s[i]);
                i++;
            }
            return sb.ToString();
        }

        // =====================================================================================
        // EventData.cs: Titel, Absender, Texte, Entscheidungen, Ergebnisse
        // =====================================================================================
        private static void Events(System.Action<string, string> A)
        {
            // Fill-Ersatzwerte
            A("deine Ware", "your goods");
            A("Eine Firma", "A company");
            A("DropshipChallenge", "DropshipChallenge");

            // black_friday
            A("Black Friday!", "Black Friday!");
            A("Shop-System", "Shop system");
            A("Heute drehen alle durch. Die Leute kaufen alles, was nicht festgeschraubt ist – drei Stunden lang mehr als doppelt so viele Bestellungen!",
                "Everyone's losing it today. People buy anything that isn't nailed down – more than twice the orders for three hours!");
            A("Black Friday", "Black Friday");
            // viral
            A("Du gehst viral!", "You're going viral!");
            A("TikTok", "TikTok");
            E("Ein Video mit {product} hat über Nacht 2 Millionen Aufrufe. Die Nachfrage explodiert – und der Hype fängt gerade erst an!",
                "A video featuring {product} got 2 million views overnight. Demand is exploding – and the hype is only just getting started!");
            A("Viraler Hit", "Viral hit");
            // supplier_down
            A("Billig-Fabrik geschlossen", "Cheapo Factory closed");
            A("Billig-Fabrik", "Cheapo Factory");
            A("Sehr geehrter Kunde, wegen 'Inspektion' (die Polizei war da) liefern wir 2 Tage nicht. Danke für Verständnis.",
                "Dear customer, due to 'inspection' (police came by) we no deliver for 2 days. Thank you for understanding.");
            // customs
            A("Zollkontrolle", "Customs check");
            A("Zollamt", "Customs Office");
            A("Eine deiner Lieferungen wird vom Zoll geprüft. Sie kommt zwei Stunden später an.",
                "Customs is inspecting one of your deliveries. It'll arrive two hours late.");
            // damaged
            A("Transportschaden", "Transport damage");
            A("Spedition Rumpel & Söhne", "Rumble & Sons Freight");
            A("Beim Verladen ist ein Gabelstapler 'leicht' gegen deine nächste Lieferung gefahren. Ein Viertel ist hin.",
                "During loading, a forklift 'lightly' drove into your next delivery. A quarter of it is toast.");
            // shitstorm
            A("Shitstorm!", "Shitstorm!");
            A("Social Media", "Social Media");
            A("Ein Kunde behauptet auf TikTok, dein Produkt hätte sein Handy 'geschmolzen'. 40.000 Kommentare. Was tust du?",
                "A customer claims on TikTok that your product 'melted' his phone. 40,000 comments. What do you do?");
            A("Entschuldigen + Gutscheine verteilen", "Apologise + hand out vouchers");
            A("Die Community findet's fair. Der Sturm legt sich.", "The community thinks that's fair. The storm dies down.");
            A("Ignorieren", "Ignore it");
            A("Der Sturm tobt weiter. Deine Bewertung leidet.", "The storm rages on. Your rating suffers.");
            A("Mit einem Meme antworten", "Reply with a meme");
            A("Legendär! Dein Meme geht viral – alle lieben dich.", "Legendary! Your meme goes viral – everybody loves you.");
            A("Meme-Hype", "Meme hype");
            A("Cringe. Das Internet vergisst nicht.", "Cringe. The internet never forgets.");
            // crypto_bro
            A("Bro, hör mir kurz zu", "Bro, hear me out");
            A("Maximilian (Krypto-Bro)", "Maximilian (crypto bro)");
            A("Bro. $HUSTLE-Coin. 100x garantiert. Nur heute. Investier 500 € und wir sehen uns auf den Malediven.",
                "Bro. $HUSTLE coin. 100x guaranteed. Today only. Invest 500 € and I'll see you in the Maldives.");
            A("All in, Bro", "All in, bro");
            A("UNFASSBAR. Der Coin ist wirklich explodiert: +3.000 €!", "UNBELIEVABLE. The coin actually mooned: +3,000 €!");
            A("Maximilians Profil ist gelöscht. Die 500 € sind weg.", "Maximilian's profile has been deleted. The 500 € are gone.");
            A("Nein danke", "No thanks");
            A("Maximilian: 'Have fun staying poor.' Du schläfst trotzdem gut.", "Maximilian: 'Have fun staying poor.' You sleep fine anyway.");
            // influencer
            A("Kooperationsanfrage", "Collab request");
            A("@lara.lifestyle (1,2 Mio.)", "@lara.lifestyle (1.2M)");
            A("Hey Babe! Ich würde dein Produkt in meiner Story zeigen. Kostet dich nur 300 €. Deal?",
                "Hey babe! I'd show your product in my story. Only costs you 300 €. Deal?");
            A("Deal!", "Deal!");
            A("Die Story ballert! Die Bestellungen fliegen nur so rein.", "The story slaps! Orders are flying in.");
            A("Influencer-Story", "Influencer story");
            A("Sie hat aus Versehen den Link der Konkurrenz gepostet. Ups.", "She accidentally posted your competitor's link. Oops.");
            A("Ablehnen", "Decline");
            A("'Okay, dann halt nicht' – Augenrollen inklusive.", "'Okay, whatever then' – eye roll included.");
            // pitch_day
            A("Einladung zum Pitch Day", "Invitation to Pitch Day");
            A("MegaMarkt Einkauf", "MegaMarkt Purchasing");
            A("Die Handelskette MegaMarkt sucht neue Marken für ihre Filialen. Drei Juroren, drei Fragen, fünf Minuten. Überzeugst du sie?",
                "The retail chain MegaMarkt is looking for new brands for its stores. Three judges, three questions, five minutes. Can you convince them?");
            A("Pitch halten (Minispiel)", "Give the pitch (minigame)");
            A("Absagen", "Decline");
            A("Du konzentrierst dich lieber auf deinen Shop.", "You'd rather focus on your shop.");
            // fake_reviews
            A("Unmoralisches Angebot", "Indecent proposal");
            A("ReviewBoost Agentur", "ReviewBoost Agency");
            A("50 Fünf-Sterne-Bewertungen für nur 200 €. Sehen garantiert echt aus. Niemand merkt was.",
                "50 five-star reviews for just 200 €. Guaranteed to look real. Nobody will notice a thing.");
            A("Kaufen", "Buy");
            A("Deine Bewertung schießt nach oben. Niemand merkt was ... noch.", "Your rating shoots up. Nobody notices ... yet.");
            A("Aufgeflogen! Ein Verbraucherportal berichtet über dich. Katastrophe.", "Busted! A consumer portal reports on you. Disaster.");
            A("Ehrlich währt am längsten. Deine Kunden merken das.", "Honesty is the best policy. Your customers notice.");
            // copycat
            A("Die Konkurrenz kopiert dich", "The competition is copying you");
            A("Marktbeobachtung", "Market Watch");
            A("BilligBoy24 verkauft plötzlich exakt dein Produkt – 25 % billiger. Die nächsten zwei Tage wird's hart.",
                "BilligBoy24 suddenly sells your exact product – 25 % cheaper. The next two days will be rough.");
            // server_down
            A("Webshop offline!", "Web shop offline!");
            A("Hosting-Anbieter", "Hosting provider");
            A("Unser Server hat eine Kaffeepause gemacht. Dein Shop ist eine Stunde lang offline. Sorry!",
                "Our server took a coffee break. Your shop is offline for an hour. Sorry!");
            // tax
            A("Post vom Finanzamt", "Letter from the tax office");
            A("Finanzamt", "Tax Office");
            A("Wir haben Fragen zu Ihren Einnahmen. Bitte reichen Sie sämtliche Unterlagen ein.",
                "We have questions about your income. Please submit all of your documents.");
            A("Steuerberater beauftragen", "Hire a tax advisor");
            A("Alles sauber. Der Berater hat sogar was zurückgeholt: +80 €.", "All clean. The advisor even got something back: +80 €.");
            A("Selbst machen", "Do it yourself");
            A("Glück gehabt. Alles in Ordnung.", "Lucky you. Everything's in order.");
            A("Formfehler! Nachzahlung: 10 % deines Kontostands.", "Paperwork error! Back payment: 10 % of your balance.");
            // oma
            A("Ein Brief von Oma", "A letter from Grandma");
            A("Oma Gerda", "Grandma Gerda");
            A("Mein Enkel, der Geschäftsmann! Ich hab dir 100 Euro beigelegt. Kauf dir was Warmes.",
                "My grandchild, the businessman! I've put 100 euros in the envelope. Buy yourself something warm.");
            // kalle_calls
            A("Kalle ruft an", "Kalle's calling");
            A("Kalle", "Kalle");
            A("Ey. Mein Aushilfskellner ist krank. Kannst du heute zwei Stunden aushelfen? 80 Euro. Bar auf die Hand.",
                "Hey. My temp waiter is sick. Can you help out for two hours today? 80 euros. Cash in hand.");
            A("Aushelfen (2 Stunden weg)", "Help out (away for 2 hours)");
            A("Alte Zeiten. Du riechst nach Fritteuse, aber 80 € sind 80 €.", "Old times. You smell like a deep fryer, but 80 € is 80 €.");
            A("Nie wieder", "Never again");
            A("Kalle: 'Pff. Undankbar.' *legt auf*", "Kalle: 'Pfft. Ungrateful.' *hangs up*");
            // street_fest
            A("Straßenfest vor der Tür", "Street festival outside");
            A("Stadtverwaltung", "City Hall");
            A("Heute ist Straßenfest! Ein Verkaufsstand kostet 50 € – du verkaufst direkt aus deinem Lager an Passanten.",
                "It's street festival day! A stall costs 50 € – you sell straight from your storage to passers-by.");
            A("Stand aufbauen", "Set up a stall");
            A("Die Leute lieben deinen Stand!", "People love your stall!");
            A("Keine Zeit", "No time");
            A("Du hörst die Musik nur von drinnen.", "You only hear the music from inside.");
            // water_damage
            A("Wasserschaden!", "Water damage!");
            A("Vermieter", "Landlord");
            A("Das Garagendach ist undicht. Ein Teil deiner Ware ist nass geworden.", "The garage roof is leaking. Some of your stock got wet.");
            // tiktok_algo
            A("Der Algorithmus liebt dich", "The algorithm loves you");
            A("Eines deiner alten Videos wird plötzlich wieder ausgespielt. Gratis-Reichweite!", "One of your old videos is suddenly being pushed again. Free reach!");
            // crypto
            A("Krypto-Crash!", "Crypto crash!");
            A("Finanznews", "Finance News");
            A("Ein Tech-Milliardär hat einen kritischen Post abgesetzt. DROPCOIN stürzt ab.", "A tech billionaire posted something critical. DROPCOIN crashes.");
            A("DROPCOIN to the moon", "DROPCOIN to the moon");
            A("Ein Tech-Milliardär hat ein Hunde-Meme mit DROPCOIN gepostet. Der Kurs explodiert.", "A tech billionaire posted a dog meme with DROPCOIN. The price explodes.");
            // retourenwelle
            A("Retourenwelle!", "Wave of returns!");
            A("Kundenservice-Postfach", "Customer service inbox");
            A("Ein Influencer erklärt in einem viralen Video, wie man „einfach alles zurückschickt, was nicht perfekt ist“. Ein paar deiner Kundinnen und Kunden haben zugeschaut.",
                "An influencer explains in a viral video how to “just send back everything that isn't perfect”. A few of your customers were watching.");
            A("Kulanz-Gutscheine verschicken", "Send goodwill vouchers");
            A("Die meisten behalten ihre Ware doch – und freuen sich über den Gutschein.", "Most of them keep their stuff after all – and enjoy the voucher.");
            A("Hinnehmen", "Accept it");
            A("Die Pakete sind schon auf dem Rückweg. Tja.", "The parcels are already on their way back. Oh well.");
            // grosskunde
            A("Großkunde fragt an", "Big client inquiry");
            E("Wir haben von Ihrem Shop gehört! Wir brauchen dringend {product} in größerer Menge – und zahlen 30 % über dem üblichen Preis. Interesse?",
                "We've heard about your shop! We urgently need {product} in bulk – and we'll pay 30 % above the usual price. Interested?");
            A("Sofort zusagen", "Accept right away");
            A("Handschlag per Mail. Ab zum Palettenplatz!", "Handshake by email. Off to the pallet spot!");
            A("Erst mal ansehen", "Take a look first");
            A("Das Angebot liegt in der App 'Aufträge' – gültig bis morgen Abend.", "The offer is in the 'Contracts' app – valid until tomorrow evening.");
            A("„Schade. Dann fragen wir halt bei BilligBoy24.“", "“Too bad. We'll ask BilligBoy24 then.”");
            // trend_alarm
            A("Trend-Alarm!", "Trend alert!");
            A("Trendradar", "Trend Radar");
            E("#{tag} trendet! Auf TikTok filmen sich gerade alle mit {product}. Das könnte richtig groß werden.",
                "#{tag} is trending! Everyone on TikTok is filming themselves with {product}. This could get really big.");
            // trend_crash
            A("Verriss im Netz", "Panned online");
            A("Tech-Blog „Gadget-Gurus“", "Tech blog “Gadget Gurus”");
            E("Ein bekannter Tech-YouTuber nennt {product} „Elektroschrott mit Ringlicht“. Die Kommentare eskalieren. Reagierst du?",
                "A well-known tech YouTuber calls {product} “e-waste with a ring light”. The comments are escalating. Do you respond?");
            A("Reaktionsvideo drehen", "Shoot a reaction video");
            E("Dein Video ist lustiger als seins. Plötzlich ist {product} wieder cool!", "Your video is funnier than his. Suddenly {product} is cool again!");
            A("Niemand schaut es. Der Hype ist tot.", "Nobody watches it. The hype is dead.");
            A("Aussitzen", "Wait it out");
            A("Du wartest ab. Die Nachfrage sackt erst mal ab.", "You wait it out. Demand drops for now.");
            // express_rush
            A("Express-Fieber", "Express fever");
            A("PaketBlitz", "ParcelBolt");
            A("Bei BilligBoy24 streiken die Paketboten. Die Leute wollen es heute schnell – und zahlen gern dafür. Drei Stunden lang kommen deutlich mehr Express-Bestellungen!",
                "BilligBoy24's couriers are on strike. People want it fast today – and happily pay for it. For three hours, way more express orders come in!");
            // eilauftrag
            A("Eilauftrag!", "Rush order!");
            E("Planänderung: Wir brauchen die Lieferung ({product}) einen Tag früher. Dafür legen wir 30 % drauf. Schaffen Sie das?",
                "Change of plans: we need the delivery ({product}) one day earlier. We'll add 30 % on top. Can you make it?");
            A("Klar, machen wir!", "Sure, we're on it!");
            A("Der Auftrag ist jetzt 30 % mehr wert – aber die Frist ist einen Tag kürzer.", "The order is now worth 30 % more – but the deadline is one day shorter.");
            A("Nein, es bleibt beim Termin", "No, we stick to the date");
            A("„Verstehe. Dann wie besprochen.“ Ein bisschen enttäuscht klingt es schon.", "“I see. As agreed then.” They do sound a little disappointed.");
            // kuriose_retoure
            A("Kuriose Retoure", "Weird return");
            A("Retourenabteilung", "Returns Department");
            E("In einem Rücksendepaket lag statt {product} ein benutzter Toaster. Dazu ein Zettel: „Passt schon.“ Was machst du damit?",
                "A return parcel contained a used toaster instead of {product}. Plus a note: “It's fine.” What do you do with it?");
            A("Bei eBay verkaufen", "Sell it on eBay");
            A("Ein Sammler zahlt 25 € für den „Vintage-Toaster“. Business ist Business.", "A collector pays 25 € for the “vintage toaster”. Business is business.");
            A("Foto posten", "Post a photo");
            A("„Kunde schickt Toaster zurück“ geht durch die Decke. Gratis-Werbung!", "“Customer returns toaster” goes through the roof. Free advertising!");
            A("Toaster-Meme", "Toaster meme");
            A("Drei Likes. Einer davon von deiner Mama.", "Three likes. One of them from your mom.");
            A("Dem Kunden zurückschicken", "Send it back to the customer");
            A("Der Kunde ist so gerührt, dass er dir fünf Sterne gibt.", "The customer is so touched he gives you five stars.");
            // seminar
            A("Hustle-Seminar", "Hustle seminar");
            A("Exklusives Wochenend-Seminar „Vom Pleitier zum Privatjet“ – nur 400 €, mit Zertifikat (selbst ausgedruckt). Bist du dabei?",
                "Exclusive weekend seminar “From Broke to Private Jet” – only 400 €, with certificate (self-printed). Are you in?");
            A("Anmelden", "Sign up");
            A("Überraschend gut! Du lernst tatsächlich was: +1 Skillpunkt.", "Surprisingly good! You actually learn something: +1 skill point.");
            A("Das Seminar war eine Werbeveranstaltung für ein weiteres Seminar. Immerhin gab es Kekse.", "The seminar was a sales pitch for another seminar. At least there were cookies.");
            A("Marvin: „Deine Konkurrenz sitzt jetzt im Seminar!“ Du sitzt lieber im Lager.", "Marvin: “Your competition is sitting in the seminar right now!” You'd rather sit in the warehouse.");
            // kalles_wette
            A("Kalles Wette", "Kalle's Bet");
            E("Ey, Unternehmer. Ich wette, du schaffst diese Woche keine {n} Pakete mehr. Wenn doch: Döner geht auf mich. Und ich leg noch was drauf.",
                "Hey, entrepreneur. I bet you can't ship {n} more parcels this week. If you do: kebab's on me. And I'll throw in a little extra.");
            A("Die Wette gilt!", "You're on!");
            A("Kalle: „Abgemacht. Und wehe, du schummelst.“ (Neues Wochenziel!)", "Kalle: “Deal. And don't you dare cheat.” (New weekly goal!)");
            A("Keine Zeit für Wetten", "No time for bets");
            A("Kalle: „Pff. Feigling.“ *legt auf*", "Kalle: “Pfft. Chicken.” *hangs up*");
            // messe
            A("Einladung zur Messe", "Trade fair invitation");
            A("Dropshipping-Expo Kleinkleckersdorf", "Dropshipping Expo Smallsville");
            A("Ein Stand auf der Dropshipping-Expo kostet 250 €. Dafür gibt es Kontakte zu Firmen, Kaffee aus Pappbechern und garantiert zwei Großanfragen.",
                "A booth at the Dropshipping Expo costs 250 €. In return you get company contacts, coffee in paper cups and two guaranteed bulk requests.");
            A("Hin da!", "Let's go!");
            A("Du verteilst 300 Visitenkarten und isst 14 Gratis-Kekse. Zwei Firmen melden sich!", "You hand out 300 business cards and eat 14 free cookies. Two companies get in touch!");
            A("Du hörst später, dass es dort Gratis-Kugelschreiber gab. Schade.", "You later hear there were free pens. Too bad.");
        }

        // =====================================================================================
        // EventSystem.cs: Wirkungs-Zeilen, Hinweise, Bonus-Wochenziel
        // =====================================================================================
        private static void Effects(System.Action<string, string> A)
        {
            A("Dafür fehlt dir das Geld.", "You can't afford that.");
            A("Keine Entscheidung getroffen.", "No decision made.");
            A("Bewertung +{0} Sterne", "Rating +{0} stars");
            A("Bewertung −{0} Sterne", "Rating −{0} stars");
            A("Bekanntheit steigt", "Awareness rises");
            A("×{0} Nachfrage für {1} min", "×{0} demand for {1} min");
            A("×{0} Nachfrage für {1} min ({2})", "×{0} demand for {1} min ({2})");
            A("{0} für {1} Tage gesperrt", "{0} blocked for {1} days");
            A("Lieferung +{0} min verspätet", "Delivery delayed +{0} min");
            A("Nächste Lieferung beschädigt", "Next delivery damaged");
            A("Preiskampf bei {0}", "Price war on {0}");
            A("Shop {0} min offline", "Shop offline for {0} min");
            A("{0} Stunden vergangen", "{0} hours passed");
            A("{0} Artikel verkauft: +{1}", "{0} items sold: +{1}");
            A("{0}× {1} verloren", "{0}× {1} lost");
            A("Gratis-TikTok-Boost", "Free TikTok boost");
            A("{0} +{1} %", "{0} +{1} %");
            A("{0} −{1} %", "{0} −{1} %");
            A("Hype startet: {0}", "Hype starting: {0}");
            A("Hype auf dem Höhepunkt: {0}", "Hype at its peak: {0}");
            A("Hype flaut ab: {0}", "Hype fading: {0}");
            A("Hype tot: {0}", "Hype dead: {0}");
            A("{0} läuft wieder normal", "{0} is back to normal");
            A("1 Retoure im Anmarsch", "1 return on its way");
            A("{0} Retouren im Anmarsch", "{0} returns on their way");
            A("Großauftrag läuft: {0} ({1})", "Bulk order running: {0} ({1})");
            A("Neues Angebot in 'Aufträge': {0} ({1})", "New offer in 'Contracts': {0} ({1})");
            A("{0}: jetzt {1}, Frist {2}", "{0}: now {1}, deadline {2}");
            A("Mehr Express-Bestellungen für {0} min", "More express orders for {0} min");
            A("+{0} Skillpunkt", "+{0} skill point");
            A("+{0} Skillpunkte", "+{0} skill points");
            A("Verschicke bis Sonntag noch {0} Pakete. Kalle zahlt {1} – und spendiert einen Döner.", "Ship {0} more parcels by Sunday. Kalle pays {1} – and buys you a kebab.");
            A("Neues Wochenziel: {0} Pakete bis Sonntag", "New weekly goal: {0} parcels by Sunday");
        }

        // =====================================================================================
        // Trend-Begründungen (GameData.TrendReasons*, {p} = Produkt, {tag} = Hashtag)
        // =====================================================================================
        private static void TrendReasons(System.Action<string, string> A)
        {
            E("Ein Streamer hat {p} live in die Kamera gehalten.", "A streamer held {p} up to the camera live.");
            E("#{tag} geht auf TikTok viral.", "#{tag} is going viral on TikTok.");
            E("Ein Promi wurde mit {p} beim Bäcker gesehen.", "A celebrity was spotted with {p} at the bakery.");
            E("Eine Oma erklärt {p} auf YouTube – drei Millionen Aufrufe.", "A grandma explains {p} on YouTube – three million views.");
            E("Das Frühstücksfernsehen hat {p} zum Must-have erklärt.", "Breakfast TV has declared {p} a must-have.");
            E("Alle wollen {p}. Wirklich alle.", "Everyone wants {p}. Literally everyone.");
            E("{p} ist DAS Gesprächsthema in jeder Kaffeeküche.", "{p} is THE talk of every office kitchen.");
            E("Selbst Kalle fragt nach {p}.", "Even Kalle is asking about {p}.");
            E("Der Hype um {p} flaut ab.", "The {p} hype is fading.");
            E("Die ersten Memes über {p} sind schon cringe.", "The first memes about {p} are already cringe.");
            A("Das Internet schaut schon woanders hin.", "The internet is already looking elsewhere.");
            E("{p} ist so 2019.", "{p} is so 2019.");
            E("Ein Tech-YouTuber nennt {p} „Elektroschrott mit Marketing“.", "A tech YouTuber calls {p} “e-waste with marketing”.");
            E("Niemand redet mehr über {p}. Niemand.", "Nobody talks about {p} anymore. Nobody.");
            E("{p} verkauft sich wieder ganz normal.", "{p} is selling normally again.");
            E("Dein TikTok hat einen Trend ausgelöst: #{tag}!", "Your TikTok started a trend: #{tag}!");
        }
    }
}
