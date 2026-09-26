# Spielideen & Bewertung – Dropshipping Simulator

> **Für wen:** dich (Besitzer) und die Umsetzungs-Agenten. **Von:** Game-Ideas-Agent (Game Design).
> **Stand:** 25.09.2026, während v3.0 „Das große Update“ läuft.
> **Grundlage:** der echte Code-Stand v2.0 auf dem Branch `claude/busy-knuth-yehyvb` (Spiellogik, Welt,
> Oberfläche), dazu `docs/REWORK_PLAN.md`, `docs/GDD.md`, `docs/ROADMAP.md`. Die laufenden Arbeiten der
> anderen Agenten (Welle 1) habe ich bewusst nicht angesehen – was dort schon erledigt ist, einfach abhaken.

## Das Wichtigste in 30 Sekunden

- **Der Kern funktioniert:** Packen, Etikettieren, Verschicken fühlt sich handfest an, und der Humor
  (FLÖRP, Krypto-Bro, Kalle) ist eigenständig. Darauf bauen wir.
- **Die größten Baustellen:** Der Handgriff ist immer gleich, viele Entscheidungen haben eine immer richtige
  Antwort (und Trading ist eine Geldmaschine: **+24 % pro Spieltag** beim ETF), der Tag hat keinen
  Höhepunkt, und das Spätspiel hat kein eigenes Ziel.
- **v3.0 geht in die richtige Richtung.** Für Welle 2 empfehle ich fünf kleine, sichere Ergänzungen:
  Balance-Fix, Engpass-Assistent, Paketstapel, Marke in der Welt, Nachtbestellungen.
- **Danach:** Tagesbogen mit Abholzeiten, mehrteilige Bestellungen und Kalles Geschichte (v3.1),
  Dropshipping-Modus und Kundenservice (v3.2), Produktlabor, Englisch und Steam (v4.0).
- **Fünf Entscheidungen liegen bei dir** (Abschnitt 6) – vor allem: Koop ja/nein und Englisch ja/nein.

**Lesehilfe:** Nur 5 Minuten? → Abschnitte 1 und 4. Entscheiden → Abschnitt 6. Bauen (Agenten) → Abschnitt 5.

**Legende**

| Zeichen | Bedeutung |
|---|---|
| Aufwand **S** | klein: ein Agent, wenige Stunden, 1–3 Dateien |
| Aufwand **M** | mittel: ein eigenes Arbeitspaket, mehrere Dateien, neue Tests |
| Aufwand **L** | groß: ein eigenes Update |
| Risiko **niedrig** | fast nur Spiellogik (reines C#), mit automatischen Tests prüfbar |
| Risiko **mittel** | Welt oder Steuerung betroffen – kompiliert blind, zeigt sich aber erst beim Play-Test |
| Risiko **hoch** | 3D, Physik, Animation oder Netzwerk – ohne laufendes Unity kaum prüfbar |
| Wirkung **★ bis ★★★★★** | wie stark es das Spielgefühl verbessert |

Wichtig für alle Einschätzungen: Während der Entwicklung kann niemand Unity starten. Alles wird blind
kompiliert und mit Tests geprüft, du testest per ▶ Play. Deshalb bevorzuge ich Ideen, die hauptsächlich
**Spiellogik** sind (die ist testbar) und bei denen die Welt nur **anzeigt**, was die Logik entschieden hat.

---

## 1. Kurzfazit

### Was heute schon Spaß macht

- **Die erste halbe Stunde fühlt sich gut an.** Ware bestellen, der gelbe PaketBlitz-Wagen hält vor der
  Garage, Kiste schleppen, einräumen, verpacken, der Labeldrucker rattert, die Kasse klingelt. Das ist das
  handfeste „Ich mach das selbst“-Gefühl, das Supermarket Simulator groß gemacht hat.
- **Der Ton sitzt.** Zimmerpflanze FLÖRP, Teppich LÅNGSAM, der Krypto-Bro mit „Have fun staying poor“,
  Oma Gerda mit 100 € im Brief, Kalle mit „Die Fritteuse vermisst dich. Ich nicht.“ – eigenständig und
  liebenswert.
- **Ein klarer Aufstiegstraum.** Imbiss → Garage → Lagerhalle → Team. „Vom Imbiss zum Imperium“ ist ein
  starker Aufhänger für Steam-Seite und Trailer.
- **Es ist schon ein komplettes Spiel.** Menüs, drei Spielstände, Auto-Save, Controller, 20 Ereignisse,
  Ziele, ein Ende. Viele Prototypen kommen nie so weit.

### Die größten Schwächen (ehrlich)

1. **Wiederholung ohne Steigerung.** Jede Bestellung ist gleich: 1 Artikel, 1 Karton, 1 Etikett, derselbe
   Weg. Nach 60–90 Minuten haben die Hände alles gesehen. Später übernimmt Personal – die eigene Arbeit wird
   nie *besser*, nur *abgeschafft*.
2. **Scheinentscheidungen und eine Geldmaschine.** Viele Entscheidungen haben eine klar beste Lösung
   (Premium-Lieferant, Preis-Knopf, Pitch-Antworten). Der Welt-ETF bringt im Schnitt **+24 % pro Spieltag**
   – mit einem Kredit (2 % Zinsen pro Tag) kann man sich reich „traden“, ohne ein Paket zu packen.
3. **Leerlauf und kein Tagesbogen.** Am Anfang kommt etwa **eine Bestellung pro echter Minute**, abarbeiten
   dauert **10–15 Sekunden**. Der Tag hat keinen Höhepunkt. Dazu kommen Pflicht-Klicks: Werbung läuft nur
   **1–3 echte Minuten**, TikTok geht **4–6-mal pro Tag** – immer dasselbe Minispiel.
4. **Das Spätspiel ist leer.** Mit Halle und Personal läuft der Laden von allein, danach heißt es nur noch
   „Zahl wird größer“. Nach 100.000 € Umsatz endet der rote Faden („mach einfach weiter“).
5. **Nebeneinander statt Miteinander.** Trading, Bank, Verkaufsstand und Pitch Day sind nette Einzelteile,
   greifen aber kaum ineinander. Gute Wirtschaftsspiele fühlen sich an wie ein Getriebe: Eine Entscheidung
   dreht an drei anderen.
6. **Die Seele ist dünn verteilt.** Kalle verschwindet nach dem Intro fast ganz, Ereignisse sind
   Einzelgags ohne Folgen, Online-Kundschaft ist unsichtbar, Personal hat nur Namen.
7. **Wenig Grund für einen zweiten Durchgang.** Keine Szenarien, keine Herausforderungen, kein Neustart-Bonus.
8. **Zwei offene Versprechen.** Das GDD verspricht Koop für 1–4 („Friendslop“ – chaotische Spiele mit
   Freunden), das Spiel ist solo. Und es gibt das Spiel nur auf Deutsch.

**Nebenbefund zum Namen:** Im Spiel betreibt man eigentlich **keinen Dropshipping-Shop** – beim echten
Dropshipping hat man kein Lager, der Lieferant verschickt direkt. Man betreibt einen Versandhandel mit
eigenem Lager. Das lässt sich wunderbar als Witz *und* als Spielmechanik nutzen (Idee 23).

### Was die Vorbilder besser machen

| Vorbild | Was dort fesselt | Stand bei uns | Was wir mitnehmen |
|---|---|---|---|
| Supermarket Simulator | Kundschaft ist ständig sichtbar; der Laden wächst in vielen kleinen Schritten | Online-Kundschaft unsichtbar; Wachstum in einem großen Sprung | Sichtbare Kundschaft (Selbstabholer, Markt); Halle in Abschnitten ausbauen |
| TCG Card Shop Simulator | Überraschung beim Öffnen, Sammeln | Jede Kiste ist gleich | Qualitätscheck am Wareneingang, Mystery-Boxen aus Retouren |
| Schedule I | Eigene Produkte „mischen“, Beziehungen zu Kunden, Koop | Feste Produkte, solo | Produktlabor, Stammkunden, Koop-Entscheidung |
| PowerWash Simulator | Ruhiger Fluss, bessere Werkzeuge, sichtbarer Fortschritt | Handarbeit wird nie besser | Tragehilfen, Scanner, „Falten im Fluss“ |
| Overcooked | Bestellzettel, Stoßzeiten, liebevolles Chaos | Kein Höhepunkt im Tag, kaum Fristen | Abholzeiten als tägliche Stoßzeit (zusammen mit den F1-Zetteln) |
| House Flipper | Vorher/Nachher, selbst einrichten | Deko an festen Plätzen, ohne Nutzen | Deko mit kleinem Nutzen; freies Platzieren erst viel später |
| Game Dev Tycoon / Startup Company | Entwickeln, Forschen, Investoren, Epochen | Marktentscheidungen sind gelöst | Produktlabor, Investorenrunde, Imperium-Phase |
| Big Ambitions | Mehrere Geschäfte, eine lebendige Stadt | Eine Straße, zwei Standorte | Dritter Standort erst spät (riskant) |
| Tiny Tower | Figuren mit Namen und Macken, Geduld statt Stress | Namen sind Deko | Personal mit Macken, Stammkunden |

### Die Zahlen hinter der Kritik (aus dem Code nachgerechnet)

- **Trading:** Der Welt-ETF steigt pro Spieltag (270 Kurs-Aktualisierungen) im Mittel um **×1,24**, an 98 %
  der Tage mit Gewinn; nach 10 Tagen **×8,6**. DROPCOIN (×1,21) und GameShop (×1,10) sind riskanter, im
  Mittel aber ebenfalls deutlich im Plus. (Simulation mit den Werten aus `Market.cs`.)
- **Lieferanten:** Premium kostet bei der Handyhülle **1,50 € mehr pro Stück** – bei 20 € Verkaufspreis.
  Dafür gibt es +1 Stern und halbe Lieferzeit. Premium ist also immer richtig, Billig immer falsch.
- **Margen:** Einkaufspreise liegen bei **8–14 % des Richtpreises**, Porto gibt es nicht. Nach den ersten
  Tagen ist Geld selten knapp – Druck kommt nur von den Fixkosten.
- **Preis:** Der Knopf „Knapp unter Markt“ setzt 97 % des Marktpreises – die Preisfrage ist mit einem Klick
  gelöst.
- **Werbung:** 90–240 Spielminuten = **67–180 echte Sekunden**, nur eine Kampagne gleichzeitig → ständiges
  Nachklicken.
- **Tempo:** Start-Nachfrage ≈ 1 Bestellung pro echter Minute, Arbeit pro Bestellung ≈ 10–15 Sekunden →
  in den ersten Tagen viel Warten (oder früh „Feierabend“ an der Matratze).

### Urteil in einem Satz

Ein charmantes, vollständiges Fundament mit starkem Ton – heute aber ein Spiel für zwei, drei gemütliche
Abende, noch nicht für 20+ Stunden. v3.0 packt viel Richtiges an (Bestellzettel, Trends, Großaufträge, Skills,
Wochenziele, Handy). Was danach am meisten fehlt: **Abwechslung im Handgriff, echte Entscheidungen, ein Bogen
im Tag und ein Spätspiel mit eigenem Ziel.**

---

## 2. Bestandsaufnahme – Behalten / Ändern / Streichen

### 2.1 Was es schon gibt

| System | Urteil | Warum | Konkrete Änderung |
|---|---|---|---|
| Imbiss-Intro (3 Teller, Kündigung) | **behalten** | Einzigartiger Aufhänger, kurz, bringt das Tragen bei | Das Hustle-Video („Mit 19 Millionär …“) als kurzen Handy-Moment zeigen; sonst so lassen |
| Kalle nach dem Intro | **ändern** | Stärkste Figur, danach nur 5 Sprüche und 1 Ereignis | Eigener Bogen in drei Akten (Idee 38), Imbiss mit Funktion (Idee 32), Aushilfsschicht spielbar (Idee 50) |
| Tagesablauf 08–20 Uhr, 9 echte Minuten, Abrechnung | **behalten + ändern** | Gute Sitzungslänge, die Abrechnung ist ein schönes Ritual | Tagesbogen: Nachtbestellungen, Abholzeiten, Morgen-Briefing (Ideen 11–13); Tageslänge wählbar (Idee 67) |
| Einkauf & drei Lieferanten | **ändern** | Abwägung nur auf dem Papier (Premium immer besser) | Echte Kostenunterschiede, Premium mit Kontingent, Billig mit Streuung (Idee 22, Skizze 5.1) |
| Express-Lieferung im Einkauf (+8 €) | **zusammenlegen** | Pauschal 8 € ist bei jeder Bestellgröße egal | Eilzuschlag 15 % des Warenwerts (Skizze 5.1) |
| Kisten, Wareneingang, Regale („falsches Regal“) | **behalten** | Handfest und verständlich | Füllstand am Regalschild; später Sortiment frei wählen (Idee 18) |
| Kommissionieren → Verpacken → Etikett → Versand | **behalten (Herzstück) + ändern** | Funktioniert, ist aber eintönig | Mehrteilige Bestellungen, Kundenwünsche, Paketstapel (Ideen 1–5) |
| Karton falten (ungefaltet = halber Preis) | **behalten + ändern** | Nette Spar-Kleinarbeit | Taste halten faltet am Stück (Idee 7) statt 50× einzeln drücken |
| Webshop: Preis, Rabattaktion, online/offline | **ändern** | Der 97-%-Knopf löst die Preisfrage | Prognose zeigen („≈ 14 Bestellungen/Tag, ≈ 230 € Gewinn“) statt Automatik; Rabatt pro Produkt und zeitlich (Skizze 5.1) |
| Bewertungen & Sterne | **behalten + ändern** | Guter Hebel aus Tempo und Qualität | Bewertung je Produkt; wiedererkennbare Namen (Idee 27); Antworten per Kundenservice (Idee 51) |
| Werbung (4 Stufen) | **ändern** | 1–3 echte Minuten Laufzeit = Klick-Pflicht | Tageskampagnen mit „Dauerauftrag“ (Skizze 5.1) |
| TikTok-Minispiel | **ändern** | 4–6× täglich dieselbe Leiste | Höchstens 3 Videos pro Tag, stärker, Trend-Bonus; später TikTok 2.0 (Idee 47) |
| Branding (Name, Logo, Farbe) | **behalten + ändern** | Schöne Identität, landet auf Kartons | Marke sichtbar in der Welt (Idee 34); mehr Logos, ein Slogan |
| Marktanalyse & 3 Konkurrenten | **ändern** | Preise wackeln zufällig, niemand reagiert | Konkurrenz mit Charakter (Idee 24); in „Markt & Trends“ aufgehen lassen (wie geplant) |
| Trading (Krypto, Meme-Aktie, ETF) | **ändern (stark entschärfen)** | Geldmaschine: +24 % pro Tag beim ETF | Neue Kurswerte (Skizze 5.1), an Ereignisse koppeln; siehe Frage 2 |
| Bank & Kredite | **behalten** | Sinnvoll für den Hallenkauf, einfach | Zinsen pro Tag und pro Woche klar zeigen; nach dem Trading-Fix wieder eine echte Abwägung |
| Verkaufsstand | **behalten + ändern** | Lebendig, sichtbare Kundschaft | Läuft mit 60 Stück Auslage fast von allein → kleinere Auslage, dafür B-Ware und Mystery-Boxen (Idee 29), samstags Markt (Idee 33) |
| Ereignisse & Postfach-Entscheidungen | **behalten + ändern** | Der Humor-Motor des Spiels | Ketten mit Folgen (Idee 43), fester Kalender (Idee 16), Hinweise statt Münzwurf (Idee 46); Entscheidungen aufs Handy statt Popup mitten in der Arbeit |
| Pitch Day (3 Juroren) | **zusammenlegen mit F4** | Das Minispiel ist gelöst: Man wählt die Antwort zum längsten Balken unter „Deine Stärken“ | Pitch schaltet einen Rahmenvertrag mit MegaMarkt frei (wiederkehrende Großaufträge); Juroren mit Vorlieben, Bluff-Antworten mit Risiko; Technik später für die Investorenrunde (Idee 31) |
| Personal (4 Rollen) | **behalten + ändern** | Wichtigster Fortschritt, sichtbar in der Halle | Eigenschaften, Erfahrung, Laune; neue Rollen Einkauf und Service (Ideen 42, 52, 53) |
| Förderband | **behalten** | Sichtbare Automatisierung | Später Sortieranlage; zählt für die Abholzeiten wie die Versandstelle (Skizze 5.2) |
| Upgrades & Lagerhalle | **behalten + ändern** | Der große Moment („das Tor geht auf“) ist gut | Mehr kleine Schritte dazwischen: Tragehilfen, Hallenabschnitte (Ideen 5, 17) |
| Einrichtung (Deko) | **ändern** | Rein kosmetisch | Kleiner Nutzen: Kaffeemaschine und Sofa heben die Team-Laune (Idee 52) |
| Lifestyle (Sneaker bis Penthouse) | **behalten** | Satire auf Statussymbole, schöne Sparziele | Kalle und Mama kommentieren Käufe (Idee 39) |
| Ziele & Firmenlevel 1–10 | **behalten + ändern** | Klarer Leitfaden | Ziele mit Story-Momenten verknüpfen; Level-Up als Fest (Idee 21) |
| Insolvenz (−500 €) & Ende (100.000 €) | **ändern** | Hartes Aus passt schlecht zu „cozy“; nach dem Ende fehlt ein Ziel | „Entspannt-Modus“ ohne Pleite (Idee 64); Kalle rettet einmal (Story); Imperium-Finale und Neustart (Ideen 56, 57) |
| Spielstände (3 Plätze, Auto-Save) | **behalten** | Solide | Firmenchronik (Idee 59), später Steam Cloud (Idee 72) |
| Tutorial (10 Schritte) | **behalten + ändern** | Funktioniert | Mit den geplanten Zielmarkern „zeigen statt erzählen“; Texte kürzen |
| Farbige Bestell-Punkte im HUD | **streichen** | Werden durch die Bestellzettel (F1) ersetzt | – |
| Zwei Laptop-Designs (Frachtbrief, Hype) | **behalten, aber einfrieren** | Wahlfreiheit ist nett | Nur weiterpflegen, solange neue Apps automatisch in beiden gut aussehen; sonst Frachtbrief einfrieren |
| Controller, Hilfe (F1), Screenshots | **behalten** | Gute Basis | Die Hilfe verweist auf den Engpass-Assistenten (Idee 63) |

### 2.2 Was v3.0 plant

| Geplant | Urteil | Warum | Konkrete Änderung / Ergänzung |
|---|---|---|---|
| **F1 Bestellzettel** (ID, Kunde, Notiz, Frist, Express) | **behalten – Top** | Macht Arbeit sichtbar und bringt Druck in Wellen | Frist später an Abholzeiten koppeln (Skizze 5.2). Express nur erzeugen, wenn die Warteschlange nicht voll ist. Im HUD höchstens 5 Zettel plus „+3 weitere“. Notizen mit Folgen (Idee 4) |
| **F2 Retouren** | **behalten + ändern** | Realistisch, neue Station | Nicht nur Strafe: prüfen → B-Ware → Grabbelkiste oder Mystery-Box (Idee 29). Rate niedrig halten, Grund anzeigen („Farbe gefällt nicht“) |
| **F3 Trends/Hype** | **behalten – Top** | Gibt Einkauf und Marketing endlich einen Sinn | Trends in der Welt spürbar machen: Kalles Gerüchte, Passanten-Sprüche, TikTok-Bonus. Totes Produkt = Lagerware verliert Wert |
| **F4 Großaufträge (B2B)** | **behalten + zusammenlegen** | Gibt der Halle Sinn und neue Handarbeit | Pitch Day wird der Einstieg (Rahmenvertrag). Gemischte Paletten als Knobelaufgabe (Idee 48) |
| **F5 Hustle-Skills** | **behalten + ändern** | Fortschrittsgefühl | Weniger „+5 %“, dafür am Ende jedes Asts ein Knoten, der das Spielen ändert (Idee 20); „Tragekraft“ als sichtbarer Paketstapel (Skizze 5.5) |
| **F6 Wochenziele** | **behalten** | Mittelfristige Motivation | Einmal pro Woche ein Ziel neu würfeln; Belohnungen auch als Deko oder Kosmetik |
| **F7 Inhalte & Balance** | **behalten + ergänzen** | Nötig | Trading-Fix, Lieferanten, Werbung und TikTok aus Skizze 5.1 mit aufnehmen |
| **Handy auf Tab** | **behalten – Top** | Weniger Wege zum Laptop | Zur Schaltzentrale für alles Kleine machen: Engpass-Tipp, Entscheidungen, Kundenchats, TikTok |
| **HustleOS mit ~9 Apps** | **behalten** | 14 Apps sind zu viel | Gesperrte Apps ausblenden statt Schloss-Symbol; „Neu“-Markierung nach Level-Up |
| Retourenplatz, Palettenplatz, Bestell-Monitor, Zonenschilder, Zielmarker | **behalten** | Klarheit in der Welt | Bodenmarkierungen ergänzen (Idee 66); der Monitor zeigt auch die nächste Abholzeit |

---

## 3. Neue Ideen

Die Nummern gelten im ganzen Dokument. Format: **Aufwand · Risiko** – worum es geht. *Spaß:* warum es sich
lohnt.

**Leitplanken für alle neuen Inhalte**
- Satire tritt nach oben (Hustle-Gurus, Tech-Milliardäre, Bürokratie), nie nach unten (Kundschaft, Personal,
  Herkunft). Keine echten Marken oder Personen – immer Parodie-Namen.
- Druck kommt in Wellen (Stoßzeiten, Ereignistage), nie dauerhaft.
- Jede neue Zahl braucht einen sichtbaren Ort (Zettel, Handy, Welt).
- Erst Spiellogik in `Scripts/Core` mit Tests – die Welt zeigt nur an.

### A. Kern-Kreislauf & Handarbeit

1. **Mehrteilige Bestellungen** – M · mittel. Ab Level 3 enthalten manche Bestellungen 2–3 Artikel, auch
   gemischt. Man sammelt alles am Packtisch und packt es gemeinsam. *Spaß:* Planen („erst beide holen“),
   Abwechslung, Overcooked-Gefühl – und ein Paket statt zwei spart Wege. → Skizze 5.4
2. **Kartonwahl mit Folgen** – S · niedrig. Das Spiel nimmt automatisch den kleinsten passenden Karton; fehlt
   er, den nächstgrößeren mit „Luftpolster“-Aufpreis. *Spaß:* Kartons planen wird eine echte Aufgabe, ohne den
   Handgriff zu bremsen.
3. **Porto & „versandkostenfrei“** – S–M · niedrig. Das Etikett kostet Porto je Kartongröße (z. B. S 3,49 €,
   M 4,99 €, L 6,99 €). Im Shop wählst du: Kundschaft zahlt den Versand, oder „versandkostenfrei“ (+20 %
   Nachfrage, du zahlst). *Spaß:* verständliche, echte Abwägung; kleine Kartons und Sammelbestellungen werden
   wertvoll („PaketBlitz erhöht die Preise – schon wieder“).
4. **Kundenwünsche am Packtisch** – S · niedrig. Zettel-Notizen wie „Bitte als Geschenk!“ oder „Vorsicht,
   Glas!“: ein Tastendruck für Geschenkpapier oder Zerbrechlich-Aufkleber (Cent-Beträge). Erfüllt → Trinkgeld
   und bessere Bewertung, vergessen → kleiner Abzug. *Spaß:* Aufmerksamkeit statt Autopilot; die F1-Notizen
   bekommen Gewicht.
5. **Paketstapel & Tragehilfen** – S–M · mittel. Tragegurt (2 Pakete), Sackkarre (2 Kisten), Rollwagen (bis
   6 Pakete). Etikettieren und Abgeben mit einem Tastendruck für den ganzen Stapel. *Spaß:* Die eigene Arbeit
   wird spürbar besser (PowerWash-Prinzip) – und sechs „+€“ hintereinander fühlen sich großartig an.
   → Skizze 5.5
6. **Scanner-Pistole** – S · niedrig. Beim Entnehmen piept es: „Bestellung #1042 ✓“. Später zeigt das Handy
   eine Pickliste in Laufreihenfolge. *Spaß:* befriedigendes Piepen, fühlt sich professionell an.
7. **Falten im Fluss** – S · niedrig. Taste halten faltet Karton um Karton mit kurzer Animation. *Spaß:*
   meditative Kleinarbeit statt Klick-Frust.
8. **Artikel-Varianten** – M · mittel. Manche Produkte gibt es in Farben (Handyhülle rosa/schwarz/klar); der
   Zettel sagt, welche. Falsche Farbe = Retoure. *Spaß:* echte Aufmerksamkeit, kleine Chaos-Momente (ideal für
   Koop). Risiko: mehr Regalplätze, mehr Oberfläche.
9. **Qualitätscheck am Wareneingang** – S–M · niedrig. Kisten vom Billig-Lieferanten kann man vor dem
   Einräumen stichprobenartig prüfen (kurzes Minispiel: 3 Teile ansehen, Mängel anklicken). Aussortieren senkt
   Retouren. *Spaß:* ein kleiner Auspack-Moment wie beim Kartenöffnen im TCG Card Shop Simulator.
10. **Selbstabholer am Garagentor** – M · mittel. Manche Kund:innen holen selbst ab: Eine Figur kommt ans Tor,
    du drückst ihr das Paket in die Hand (ohne Etikett), sie sagt etwas Lustiges. *Spaß:* Kundschaft wird
    sichtbar – genau das macht Supermarket Simulator so lebendig.

### B. Tagesablauf & Tempo

11. **Nachtbestellungen** – S · niedrig. Morgens liegen schon Bestellungen „von heute Nacht“ bereit (etwa 20 %
    eines Tages). *Spaß:* Der Tag beginnt mit Arbeit statt mit Warten. → Skizze 5.2
12. **Abholzeiten von PaketBlitz** – M · mittel. Der Paketwagen kommt um 12:00 und 17:00. Bestellungen
    versprechen „Versand heute“ – wer die 17-Uhr-Abholung verpasst, riskiert Sterne. *Spaß:* Jeder Tag bekommt
    einen Höhepunkt („Letzte Abholung in 10 Minuten!“), ohne Dauerstress. → Skizze 5.2
13. **Morgen-Briefing** – S · niedrig. Zum Tagesstart eine Handy-Karte: Trend des Tages, Nachtbestellungen,
    Termine (Großauftrag fällig), Abholzeiten. *Spaß:* Man plant wie eine Chefin; Rituale sind gemütlich.
14. **Zeitraffer bei Leerlauf** – S · niedrig. Wenn gerade nichts zu tun ist: Taste halten = Zeit ×4.
    *Spaß:* kein Zwangswarten.
15. **Wochenrhythmus** – S · niedrig. Montag Wochenziele (F6), Samstag Wochenmarkt, Sonntag holt PaketBlitz
    nicht ab (typisch deutsch!) – Sonntag ist ein kurzer Planungs- und Inventurtag. *Spaß:* Abwechslung im
    Kalender.
16. **Fester Jahreskalender** – S · niedrig. Black Friday, Weihnachten, Valentinstag, Sommer und der
    „Primel-Tag“ (Prime-Day-Parodie) kommen an festen Tagen und werden eine Woche vorher angekündigt.
    *Spaß:* Vorfreude und Vorbereitung („Lager voll machen!“).

### C. Fortschritt & Freischaltungen

17. **Halle in Abschnitten ausbauen** – M · mittel. Nach dem Hallenkauf werden Regalreihen, zweite
    Packstation, Büro und Pausenraum einzeln freigeschaltet (feste Plätze, kein freies Bauen). Optional ein
    Zwischenschritt „Container neben der Garage“ ab Level 3. *Spaß:* öfter „mein Laden wächst“; der Sprung
    Garage → Halle (4.500 €) wird sanfter.
18. **Sortiment selbst wählen** – M · mittel. Die Garage hat 3 Regale – du entscheidest, welche 3 Produkte
    darin stehen (das Schild wechselt). Neue Produkte sind Angebote, keine Pflicht. *Spaß:* echte
    Sortimentsstrategie.
19. **Produkte entdecken statt geschenkt bekommen** – M · mittel. Neue Produkte tauchen über eine
    Einkaufsmesse (Ereignis), das Trendradar oder Kalles Gerüchte auf; das Level schaltet nur mehr Plätze frei.
    *Spaß:* Überraschung und Wahl statt einer Liste.
20. **Skills, die das Spielen ändern** – S · niedrig. Jeder F5-Ast endet mit einem Knoten wie „Zwei Hände“
    (2 Artikel tragen), „Stammkunden-Magnet“ oder „Viral-Instinkt“ (breitere grüne Zone bei TikTok).
    *Spaß:* spürbar statt unsichtbarer +5 %.
21. **Level-Up als Fest** – S · niedrig. Konfetti in der Welt, Glückwunsch von Mama oder Kalle, das neue
    Produkt steht als Musterkiste auf der Werkbank. *Spaß:* Die Belohnung fühlt sich an.

### D. Wirtschaft & Strategie

22. **Balance-Paket „Echte Entscheidungen“** – S · niedrig. Trading entschärfen, Lieferanten-Abwägung echt
    machen, Werbung als Tageskampagne, TikTok höchstens 3× pro Tag, Preis-Prognose statt Automatik-Knopf.
    *Spaß:* Keine Antwort stimmt immer, und es gibt keine Abkürzung am Spiel vorbei. → Skizze 5.1
23. **Dropshipping-Modus pro Produkt** – M · niedrig. Für jedes Produkt wählbar: „selbst lagern“ (schnell,
    gute Bewertungen, Arbeit) oder „dropshippen“ (der Lieferant verschickt direkt: keine Handarbeit, aber
    halbe Marge, doppelte Retourenquote und −1 Stern wegen langer Lieferzeit). Ab Level 4, wenn es mehr
    Produkte als Regale gibt. Balance-Ziel: nie besser als selbst lagern, aber bequem. Optionaler Story-Moment:
    Der erste Versuch „wie im Video“ endet in wütenden Bewertungen – Kalle lacht. *Spaß:* Der Titel ergibt
    endlich Sinn, die Satire sitzt, und das Spätspiel wird breiter (Sortiment größer als die Halle).
24. **Konkurrenz mit Charakter** – M · niedrig. BilligBoy24 antwortet auf dauerhaftes Unterbieten mit einem
    Preiskampf, TrendHaus kopiert deine Trendprodukte, AliExpresso bleibt langsam und billig. Dazu eine
    Marktanteils-Anzeige. *Spaß:* Gegner, die man schlagen will, statt zufällig wackelnder Zahlen.
25. **Lieferanten-Beziehung & Verhandeln** – S–M · niedrig. Treue Bestellungen bringen Rabattstufen; einmal
    pro Woche kann man verhandeln (kurzer Chat mit drei Tonlagen: freundlich, hart, „Bro“). *Spaß:* eine kleine
    soziale Entscheidung mit Humor.
26. **Kunden-Zielgruppen** – M · mittel. Vier Gruppen (Schnäppchenjäger, Generation Oma, Gen Z,
    Technik-Fans) mit Vorlieben für Preis, Tempo, Qualität oder Trend. Werbekanäle erreichen verschiedene
    Gruppen (Flyer → Oma, TikTok → Gen Z, Google → alle). *Spaß:* Marketing wird Strategie statt Knopfdruck.
27. **Stammkunden mit Namen** – M · niedrig. Zufriedene Kund:innen kommen wieder, haben Namen und Vorlieben
    und schreiben persönliche Bewertungen („Oma Gerda, 12. Bestellung: Wie immer top!“). Sie sorgen für eine
    verlässliche Grundnachfrage. *Spaß:* Bindung wie an die Bewohner in Tiny Tower.
28. **Dauerauftrag (Auto-Nachbestellung)** – S · niedrig. Pro Produkt einen Mindestbestand einstellen; fällt
    das Lager darunter, wird automatisch nachbestellt (ab Level 5 oder mit Einkäufer:in). *Spaß:* Komfort im
    Spätspiel – planen statt klicken.
29. **Mystery-Boxen aus B-Ware** – S–M · niedrig. Geprüfte Retouren (F2) kommen in „Überraschungskisten“, die
    am Stand oder online verkauft werden. *Spaß:* Retouren werden eine Chance statt nur Strafe; Satire auf den
    Mystery-Box-Hype.
30. **Versicherungen** – S · niedrig. Transportversicherung (weniger Schaden), Server-Vertrag (kürzere
    Ausfälle), kleine Tageskosten. *Spaß:* Ereignisse werden planbar; eine ruhige Abwägung zwischen Sicherheit
    und Kosten.
31. **Investorenrunde „Die Haifisch-Runde“** – M · niedrig. Parodie auf „Die Höhle der Löwen“:
    Investor:innen bieten Geld gegen Firmenanteile. Wer Anteile abgibt, bekommt Umsatzziele und strenge Mails.
    Nutzt die Pitch-Technik weiter. *Spaß:* eine große, riskante Entscheidung mit Startup-Company-Gefühl.

### E. Welt & Orte

32. **Kalles Imbiss mit Funktion** – S–M · niedrig. Mittagspause bei Kalle: +10 % Lauftempo für zwei Stunden
    und ein Gerücht über den nächsten Trend (F3). *Spaß:* Die Welt bekommt Bedeutung, Kalle bleibt präsent.
33. **Wochenmarkt im Park** – M · mittel. Samstags mehr Passanten und ein Marktstand im Park; B-Ware
    verramschen und mit Kundschaft feilschen (kurzer Dialog „18 statt 20 €?“). *Spaß:* geselliger Höhepunkt
    der Woche, sichtbare Kundschaft.
34. **Marke in der Welt sichtbar** – S–M · niedrig. Je bekannter die Marke, desto mehr Passanten tragen
    Pakete oder Tüten in deiner Markenfarbe; ab bestimmten Stufen hängt ein Plakat mit deinem Logo, und der
    eigene Lieferwagen trägt dein Logo. *Spaß:* „Die Stadt kennt mich!“ – perfekt für Screenshots und Trailer.
35. **Nachbarschaft mit Running Gags** – S · niedrig. Frau Schmidt mit Dackel beschwert sich über den
    Lieferwagen, Kioskbesitzer Herr Petersen wettet mit Kalle auf deinen Erfolg. Kurze Sprüche an festen
    Orten, später kleine Ereignisse. *Spaß:* Die Straße fühlt sich bewohnt an.
36. **Wetter & Jahreszeiten** – M · mittel. Regen (weniger Laufkundschaft, mehr Online-Bestellungen), Schnee
    im Dezember, Hitzewelle (Ventilator-Trend). *Spaß:* Abwechslung im Bild und in der Nachfrage. Risiko: Regen
    und Schnee sind blind schwer zu prüfen → erst die Logik, die Optik später.
37. **Dritter Standort: Logistikzentrum** – L · hoch. Spätes Ziel ab Level 9: neue Kartenseite mit
    Sortieranlage. *Spaß:* Big-Ambitions-Wachstum. Risiko: viel 3D, sehr groß – frühestens v4.

### F. Figuren & Story

38. **Kalles Geschichte in drei Akten** – M · niedrig. Akt 1 (Garage): Kalle spottet. Akt 2 (Halle): Kalles
    Imbiss läuft schlecht – du hilfst ihm online, „Kalles Knoblauchsoße“ wird ein eigenes Produkt. Akt 3
    (Imperium): Kalle wird Partner oder Kantinenchef deiner Firma; das 100.000-€-Finale endet bei ihm.
    *Spaß:* ein emotionaler Bogen, ein Grund zum Weiterspielen, Trailer-Momente.
39. **Mama & Oma als Running Gag** – S · niedrig. Mama kommentiert Meilensteine („Hast du schon eine
    Rentenversicherung?“), Oma Gerda bestellt selbst und schreibt Bewertungen; Kalle und Mama reagieren auf
    Lifestyle-Käufe. *Spaß:* Herz und Humor mit wenig Aufwand.
40. **Der Rivale** – M · niedrig. Jannik von TrendHaus, Hustle-Influencer mit Podcast („Grindset“), kopiert
    dich, stichelt per Mail und fordert dich zum Pitch-Duell. *Spaß:* ein persönlicher Gegner; Satire auf
    Hustle-Bros.
41. **Hustle-Coach Marvin** – S · niedrig. Verkauft die „Mindset-Masterclass“ für 997 €. Wer kauft, bekommt …
    ein gerahmtes Zertifikat als Deko und sonst nichts. *Spaß:* bissige, freiwillige Falle.
42. **Personal mit Macken** – M · niedrig. Jede:r hat eine Eigenschaft (flink, gründlich, verplant,
    Kaffeejunkie) und eigene Sprüche. *Spaß:* Tiny-Tower-Charme und eine echte Wahl beim Einstellen.

### G. Ereignisse & Satire

43. **Ereignis-Ketten** – S–M · niedrig. Folgen statt Einzelgags: Der Krypto-Bro kommt mit NFTs zurück,
    Influencerin Lara will nochmal, der Shitstorm hat ein Nachspiel. *Spaß:* kleine Geschichten; Entscheidungen
    haben Folgen.
44. **Bürokratie-Satire** – S · niedrig. Verpackungsregister („Formular 17b“), Abmahnung wegen fehlendem
    Impressum, Rundfunkbeitrag für den Laptop, Brandschutzbegehung in der Halle. *Spaß:* sehr deutscher,
    wiedererkennbarer Humor.
45. **Absurde Trendprodukte** – S · niedrig. Zeitlich begrenzte Parodie-Produkte: Fidget-Kartoffel,
    40-Unzen-Megabecher, Pistazien-Knusperschokolade „garantiert nicht aus Dubai“. Keine echten Markennamen.
    *Spaß:* Aktualität und Lacher; passt zu F3.
46. **Hinweise statt Münzwurf** – S · niedrig. Bei Entscheidungen zeigt das Spiel, wovon der Ausgang abhängt
    („Chance steigt mit deiner Bewertung“). *Spaß:* Entscheidungen fühlen sich fair an, schlechte Ausgänge
    sind nachvollziehbar.

### H. Minispiele

47. **TikTok 2.0** – M · mittel. Vor der Timing-Leiste wählst du Produkt, Aufhänger („POV: …“) und
    Trend-Sound; passt alles zum aktuellen Trend (F3), gibt es einen Bonus. Später (v4) echtes Filmen in der
    3D-Welt mit dem Handy. *Spaß:* kreative Entscheidung plus Geschick statt immer derselben Leiste.
48. **Paletten-Tetris** – M · niedrig. Für Großaufträge (F4) Kisten auf einer Palette anordnen (2D-Raster im
    Bildschirm); stabil gepackt = Bonus. *Spaß:* Knobeln – und reine Logik, gut testbar.
49. **Live-Shopping-Stream** – M · niedrig. Einmal pro Woche live gehen: Chatfragen schnell passend
    beantworten, Rabattcode „droppen“. *Spaß:* kurze Hektik mit lustigen Chat-Kommentaren.
50. **Aushilfsschicht bei Kalle** – M · mittel. Das Ereignis „Kalle ruft an“ wird spielbar: zwei Minuten
    Teller austragen mit den Intro-Stationen; je schneller, desto mehr Trinkgeld. *Spaß:* Wiedersehen mit dem
    Anfang, Abwechslung.
51. **Kundenservice-Chats** – M · niedrig. Beschwerden landen auf dem Handy; du antwortest (Entschuldigung,
    Gutschein, Erstattung, Meme) – mit Folgen für Bewertung und Kasse. *Spaß:* Humor und kleine
    Entscheidungen; erfüllt die vierte Rolle aus dem GDD („Kundenservice“).

### I. Personal & Automatisierung

52. **Erfahrung, Pausen & Laune** – M · mittel. Personal wird mit Erfahrung schneller und braucht Pausen;
    Kaffeemaschine und Sofa (bisher reine Deko) heben die Laune; monatlich „Mitarbeiter:in des Monats“ mit Foto
    an der Wand. *Spaß:* Deko bekommt Sinn, das Team wächst ans Herz.
53. **Neue Rollen: Einkauf & Kundenservice** – S–M · niedrig. Einkäufer:in bestellt nach Mindestbestand
    (Idee 28), die Service-Kraft beantwortet Chats (Idee 51). *Spaß:* Das Spätspiel wird Führung statt
    Leerlauf.
54. **PackBot 3000** – M · mittel. Ein später Roboter, anfangs herrlich unfähig („Paket erkannt: Katze“), mit
    Updates immer besser. *Spaß:* Satire auf Tech-Versprechen, sichtbare Automatisierung.

### J. Langzeitmotivation & Spätspiel

55. **Produktlabor / Eigenmarke** – L · mittel. Ab Level 7 eigene Produkte entwickeln: Basisprodukt plus
    Merkmale (z. B. Handyhülle + Glitzer + Kartenfach), Entwicklungszeit, Testbewertungen, höhere Marge und ein
    eigener Hype. *Spaß:* das Game-Dev-Tycoon- und Schedule-I-Gefühl „meine Erfindung verkauft sich!“.
56. **Imperium-Finale & Börsengang** – M · niedrig. Nach 100.000 € Umsatz ein satirischer Börsengang
    (Glocke läuten, Aktionärsversammlung mit absurden Fragen), danach Quartalsziele als neue Langzeitziele.
    *Spaß:* ein richtiges Ende – und ein Grund, weiterzuspielen.
57. **Exit & Neustart („Serienunternehmer“)** – M · niedrig. Firma verkaufen und neu anfangen – mit einem
    kleinen Bonus (z. B. +1 Skillpunkt oder mehr Startkapital) und einem neuen Szenario. *Spaß:* Wiederspielwert
    mit Belohnung.
58. **Szenarien & Herausforderungen** – S–M · niedrig. „Start mit 2.000 € Schulden“, „Nur ein Produkt“,
    „Black-Friday-Woche“, „Ohne Personal“. *Spaß:* neue Ziele mit altem Wissen; gut für Streamer.
59. **Firmenchronik** – S–M · niedrig. Automatische Fotos bei Meilensteinen und ein Album mit Statistik
    („Tag 1: 3 Pakete. Tag 40: 212.“). *Spaß:* Stolz, teilbar.

### K. Koop & Mehrspieler

60. **Koop-fähige Grundlage** – S–M · niedrig. Jede Aktion läuft ohnehin über die Spiellogik (`Sim`);
    ergänzen: eine Spieler-Nummer an jeder Aktion, nirgends „der eine Spieler“ fest verdrahtet. *Nutzen:* für
    Spieler:innen unsichtbar – macht Koop später deutlich billiger.
61. **Couch-Koop + Steam „Remote Play Together“** – L · hoch. Zwei Personen an einem PC (geteilter
    Bildschirm); über Steam Remote Play Together spielen Freunde online mit, ganz ohne Netzwerkcode.
    *Spaß:* Overcooked-Chaos zu zweit.
62. **Online-Koop für 1–4** – L · hoch. Echte Netzwerkversion (z. B. Unity Netcode) mit Koop-Chaos-Ereignissen
    („Drucker klemmt – einer muss kurbeln“). *Spaß:* der Friendslop-Traum aus dem GDD. Risiko: ohne
    Testmöglichkeit extrem hoch.

### L. Komfort & Zugänglichkeit

63. **Engpass-Assistent** – S · niedrig. „Warum läuft's nicht?“ – das Handy nennt den wichtigsten Engpass mit
    Knopf zur Lösung („Keine M-Kartons mehr → kaufen“, „Preis 18 % über Markt“). *Spaß:* Frust wird zu einem
    klaren nächsten Schritt. → Skizze 5.3
64. **Entspannt-Modus** – S · niedrig. Keine Pleite, großzügige Fristen, sanftere Ereignisse. *Spaß:* für
    Cozy-Fans und Streamer, die nebenbei mit dem Chat reden.
65. **Farbenblind-Hilfe** – S · niedrig. Produkte werden heute vor allem über Farben unterschieden →
    zusätzlich Symbole auf Kisten, Regalen und Zetteln. *Nutzen:* niemand wird ausgeschlossen.
66. **Bodenmarkierungen & Wegweiser** – S · niedrig. Farbige Bodenlinien und Pfeile (Eingang → Regal →
    Packtisch → Versand), passend zu den geplanten Zonenschildern. *Nutzen:* Orientierung ohne Text.
67. **Tastenbelegung, Textgröße, Tageslänge** – M · niedrig. Frei belegbare Tasten, größere Texte,
    Tageslänge 7/9/12 Minuten wählbar. *Nutzen:* jede:r spielt im eigenen Tempo.

### M. „Juice“ (sattes Feedback)

68. **Münzregen** – S · niedrig. Beim Abgeben fliegen Münzen zum Kontostand oben links, der Zähler rattert
    hoch. *Spaß:* Jeder Verkauf fühlt sich nach Geld an.
69. **Etikett-Klatscher** – S · niedrig. Das Label klebt sichtbar mit Logo auf dem Paket, kurzes „Klatsch“,
    kleiner Wackler. *Spaß:* Der Drucker wird zur Lieblingsstation.
70. **Rekorde & Serien** – S · niedrig. Stempel auf dem Kassenbon („Rekordtag!“, „5 Tage ohne verlorene
    Bestellung“) und Serienzähler. *Spaß:* „Nur noch einen Tag“-Effekt.

### N. Steam-Funktionen

71. **Steam-Erfolge** – S–M · mittel. 30–40 Erfolge mit Witz („Oma wäre stolz“, „FLÖRP-Sammler“, „Grindset:
    10 Tage bis 20 Uhr gearbeitet“). *Nutzen:* Standard auf Steam, verlängert die Spielzeit.
72. **Steam Cloud & Rich Presence** – S · mittel. Spielstände in der Cloud; die Freundesliste zeigt „Packt
    Handyhüllen in der Garage (Tag 12)“. *Nutzen:* Komfort und kostenlose Werbung bei Freunden.
73. **Demo fürs Steam Next Fest** – S · niedrig. Intro plus Garage bis Tag 3, danach ein Wunschlisten-Hinweis.
    *Nutzen:* Eine gute Demo bringt mehr Wunschlisten als jede Einzelfunktion.
74. **Englische Version** – M–L · mittel. Alle Texte aus dem Code in Übersetzungstabellen, Wortwitze bewusst
    neu schreiben (FLÖRP bleibt FLÖRP). *Nutzen:* Ohne Englisch erreicht das Spiel nur einen kleinen Teil von
    Steam.
75. **Fotomodus** – M · mittel. Freie Kamera, Filter, Rahmen mit deinem Markenlogo. *Spaß:* Screenshots
    teilen – kostenlose Werbung.

---

## 4. Top 12 Empfehlungen

**Rang = Wert im Verhältnis zu Aufwand und Risiko** (nicht nur Wirkung). Deshalb stehen große Brocken wie das
Produktlabor trotz ★★★★★ weiter hinten.

| Rang | Empfehlung | Ideen | Wirkung | Aufwand | Risiko | Wann |
|---|---|---|---|---|---|---|
| 1 | Balance-Paket „Echte Entscheidungen“ | 22 | ★★★★★ | S | niedrig | **Welle 2** (Trading-Fix sofort) |
| 2 | Tagesrhythmus: Nachtbestellungen + Abholzeiten | 11, 12 (+13) | ★★★★★ | S + M | mittel | Nachtbestellungen **Welle 2**, Abholzeiten v3.1 |
| 3 | Engpass-Assistent | 63 | ★★★★ | S | niedrig | **Welle 2** |
| 4 | Mehrteilige Bestellungen & Kartonwahl | 1, 2 | ★★★★ | M | mittel | v3.1 (nach F1) |
| 5 | Paketstapel & Tragehilfen | 5 | ★★★★ | S–M | mittel | **Welle 2** |
| 6 | Kalles Geschichte in drei Akten | 38 (+32, 39, 50) | ★★★★ | M | niedrig | v3.1 (erste Sprüche schon Welle 2) |
| 7 | Dropshipping-Modus pro Produkt | 23 | ★★★★ | M | niedrig | v3.2 |
| 8 | Marke in der Welt sichtbar | 34 | ★★★ | S–M | niedrig | **Welle 2** |
| 9 | Ereignis-Ketten & fester Kalender | 43, 16 | ★★★ | S–M | niedrig | v3.1 |
| 10 | Kundenservice-Chats aufs Handy | 51 | ★★★ | M | niedrig | v3.2 |
| 11 | Produktlabor / Eigenmarke | 55 | ★★★★★ (langfristig) | L | mittel | v4.0 |
| 12 | Englisch + Steam-Grundausstattung | 71–74 | ★★★★★ (für den Verkauf) | M–L | mittel | vor dem Steam-Release |

**Warum genau diese:**
1. Beseitigt Geldmaschine und Scheinentscheidungen – nur Zahlen, voll testbar.
2. Gibt jedem Tag Anfang, Höhepunkt und Ruhe – das größte Plus fürs Spielgefühl pro Aufwand.
3. Größter Klarheitsgewinn; die To-do-Liste im Laptop (`AppHome.BuildTodos`) ist schon ein Anfang.
4. Macht den Handgriff abwechslungsreich; ohne das bleibt der Kern eintönig.
5. Die Hände werden besser statt nur ersetzt; F5 plant „Tragekraft“ ohnehin.
6. Gibt dem Spiel ein Herz und einen Grund, weiterzuspielen – fast nur Text, kaum Risiko.
7. Macht den Titel stimmig und das Spätspiel breiter – reine Logik.
8. Sichtbarer Erfolg in der Welt; passt zur laufenden Arbeit an Figuren und Assets.
9. Mehr Geschichten und Vorfreude – reine Daten.
10. Humor und Entscheidungen; verbindet Retouren und Bewertungen; nutzt das neue Handy.
11. Der Langzeit-Motor (wie Game Dev Tycoon und Schedule I) – groß, deshalb später.
12. Ohne Englisch nur ein Bruchteil der Steam-Kundschaft; Erfolge, Cloud und Demo sind Standard.

### 4.2 Vorschlag für die Reihenfolge

**Welle 2 (jetzt, im laufenden v3.0-Update)**
- **Balance-Paket** (Rang 1) als kleiner Nachtrag zur Spiellogik. Den **Trading-Fix bitte sofort** an den
  Core-Agenten geben – er gehört zu F7 und ist eher ein Fehler als eine Idee.
- **Nachtbestellungen** (Teil von Rang 2) – wenige Zeilen Logik, großer Effekt auf den Tagesstart.
- **Engpass-Assistent** (Rang 3) – Regeln in der Spiellogik, Karte auf dem Handy, Liste im Laptop.
- **Paketstapel & Tragehilfen** (Rang 5) – als Welt-Wirkung des F5-Skills „Tragekraft“ (Paket
  „Welt-Gameplay: Spieler-Effekte“).
- **Marke in der Welt** (Rang 8) – im Paket „Welt-Umgebung“, zusammen mit den neuen Figuren.
- **Mini-Inhalte ohne Risiko:** Kalle-Sprüche je Fortschritt, Mama-Nachrichten zu Meilensteinen (Teil von
  Rang 6).

**v3.1 „Tagesbogen & Herz“:** Abholzeiten, mehrteilige Bestellungen, Kalles drei Akten, Ereignis-Ketten und
Kalender. Dazu kleine Ideen: Kundenwünsche (4), Falten im Fluss (7), Morgen-Briefing (13), Level-Up als Fest (21).

**v3.2 „Tiefe“:** Dropshipping-Modus, Kundenservice-Chats, Personal mit Macken (42) sowie Erfahrung und Laune
(52), Stammkunden (27), Dauerauftrag (28), Konkurrenz mit Charakter (24).

**v4.0 „Imperium & Steam“:** Produktlabor, Imperium-Finale und Neustart (56, 57), Szenarien (58), Englisch,
Steam-Erfolge und Cloud, Demo. Koop je nach deiner Antwort auf Frage 1.

### 4.3 Bewusst nicht (jetzt)

- **Freies Bauen und Platzieren** (House-Flipper-Stil): sehr beliebt, aber Platzieren, Kollisionen und
  gespeicherte Layouts sind blind riskant. Zwischenschritt: feste Plätze in Abschnitten (Idee 17).
- **Selbst ausliefern mit dem Auto:** Fahrphysik lässt sich ohne Play-Tests nicht abstimmen.
- **Echtes 3D-Stapeln mit Physik:** gleiche Gründe; stattdessen 2D-Knobelei im Bildschirm (Idee 48).
- **Online-Koop jetzt:** siehe Frage 1.
- **Hunger- und Schlafbedürfnisse** wie in Big Ambitions: Pflichtarbeit ohne Spaß, passt nicht zu „cozy“.
- **Dritter Standort, Wetter-Optik:** später, wenn jemand regelmäßig mit Unity testen kann.

---

## 5. Umsetzungsskizzen (Top 5)

Hinweise für Agenten: Dateinamen und Funktionen beziehen sich auf den Stand v2.0. Nach dem laufenden
UI-Umbau heißen manche Apps anders – dann die entsprechende neue App nehmen. Für F1–F7 gilt die
API-Beschreibung des Core-Agenten (`docs/CORE_API.md`). Neue Felder im Spielstand immer mit Standardwert, damit
alte Spielstände laden. Tests unter `Assets/DropshippingGame/Tests/EditMode/`, mit festem Zufalls-Startwert
(„Seed“), damit sie immer gleich ausfallen.

### 5.1 Balance-Paket „Echte Entscheidungen“ (Idee 22)

**Ziel:** Keine Geldmaschine mehr, keine immer richtige Antwort, weniger Pflicht-Klicks. Nur Zahlen und
kleine Regeln – ideal für blindes Arbeiten.
**Wann:** Welle 2 als Nachtrag zur Spiellogik. Teil a) sofort (F7).

**a) Trading entschärfen** – `Scripts/Core/Market.cs` (`Market.Assets` und der Sprung in `Market.Tick`)

| Anlage | heute (Drift / Schwankung je Kurs-Tick) | neu | Wirkung pro Spieltag |
|---|---|---|---|
| Welt-ETF | 0,0008 / 0,006 | 0,00002 / 0,0006 | Mittel +0,5 %, Schwankung ±1 % |
| GameShop AG | 0,0004 / 0,025 | 0,00001 / 0,0035 | ±6–7 % |
| DROPCOIN | 0,0004 / 0,05, Sprung-Chance 1,2 % je Tick | −0,00008 / 0,009, Sprung-Chance 0,4 % je Tick (±25 %) | ±15 %, Erwartungswert ≈ 0 |

- 1 Spieltag = 270 Kurs-Ticks (alle 2 echten Sekunden). Nachgerechnet: ETF nach 30 Tagen ≈ ×1,18 – Kredit
  plus ETF lohnt sich nicht mehr (Zinsen 2 % pro Tag).
- Gebühr bleibt 1 %. Die Ereignisse „Krypto-Crash“ und „To the moon“ bleiben – sie sind der Spaß.
- Tests: 300 simulierte ETF-Tage → mittlere Tagesrendite zwischen 0 % und 1 %; Wahrscheinlichkeit für
  ETF ×1,5 nach 10 Tagen < 1 %; DROPCOIN-Median nach 10 Tagen zwischen ×0,8 und ×1,2.

**b) Lieferanten mit echter Abwägung** – `GameData.Products[].UnitCost`, `GameData.Suppliers`, `Sim.BuyBulk`,
`Sim.BulkCost`, `Sim.UpdateDeliveries`
- Einkaufspreise (Standard) auf 12–28 % des Richtpreises anheben. Startvorschlag: Hülle 3 €, LED 8 €,
  Massage 24 €, Kopfhörer 16 €, Ringlicht 13 €, Katzenbrunnen 18 €, Haltung 6 €, Smartwatch 32 €, Beamer 48 €,
  Drohne 70 €.
- Premium: Preisfaktor 2,0 (statt 1,75) und ein Kontingent von 1 Bestellung pro Produkt und Tag
  („Manufaktur – nur begrenzt lieferbar“). Ergebnis: Premium lohnt sich bei günstigen Produkten (Bewertung
  aufbauen), bei teuren halbiert es fast die Marge.
- Billig: Qualität je Kiste zufällig 0,4–0,8, mit 10 % Chance „Glücksgriff“ (1,0). Sichtbar erst beim
  Einräumen oder per Qualitätscheck (Idee 9).
- Express im Einkauf: 15 % des Warenwerts statt pauschal 8 € (`GameData.ExpressSurcharge`).
- Ziel für `BalanceTests`: Lagerhalle weiter zwischen Tag 10 und 14, Level 5 bis Tag 25, keine Pleite bei
  fleißigem Spiel. Die Startwerte daran feinjustieren.
- Tests: Bei der Drohne ist der Gewinn pro Stück mit Standard mindestens 50 % höher als mit Premium; bei der
  Hülle liegt der Unterschied bei höchstens 3 €; das Premium-Kontingent verhindert eine zweite
  Premium-Bestellung desselben Produkts am selben Tag; Billig-Qualität liegt immer zwischen 0,4 und 1,0.

**c) Werbung als Tageskampagne** – `GameData.AdTiers`, `Sim.StartAdCampaign`, `Sim.BoostMult`, Marketing-App

| Kanal | heute | neu |
|---|---|---|
| Flyer & Plakate | 15 € für 90 min, ×1,25 | 25 € pro Tag, ×1,15 bis Feierabend |
| Facebook-Ads | 60 € für 120 min, ×1,6 | 80 € pro Tag, ×1,35 |
| Google-Ads | 160 € für 180 min, ×2,0 | 200 € pro Tag, ×1,6 |
| Influencer-Kampagne | 650 € für 240 min, ×2,8 | 700 € einmalig, ×2,0 für 2 Tage plus Bekanntheit |

- Bis zu zwei Kanäle gleichzeitig. Schalter „Dauerauftrag“: morgens automatisch verlängern, wenn genug Geld
  da ist (steht dann auf dem Kassenbon).
- Sicherheitsnetz: Der Gesamt-Multiplikator aus allen Boosts ist höchstens ×4 (in `Sim.BoostMult`).
- Tests: Kampagne läuft bis 20:00 und endet; der Dauerauftrag bucht am nächsten Morgen ab bzw. pausiert bei
  zu wenig Geld; der Deckel ×4 greift.

**d) TikTok ohne Klick-Pflicht** – `GameData.TikTok*`, `Sim.TriggerTikTok`, `Sim.TikTokAvailable`
- Höchstens 3 Videos pro Tag (die Praktikant:in zählt mit); Abklingzeit 60 Spielminuten.
- Wirkung ×1,3–2,2 für 180–360 Spielminuten (statt ×1,2–2,4 für 60–180).
- Trend-Bonus: Ist das gezeigte Produkt gerade im Hype (F3, Wert ≥ 1,3), wirkt das Video ×1,25 stärker.
- Tests: Das 4. Video am selben Tag geht nicht; der Zähler setzt morgens zurück.

**e) Preis-Prognose statt Automatik** – neue Funktion `Sim.ForecastDay(productId, price)`, Shop-App
- Erwartete Bestellungen pro Tag für ein Produkt ≈ 10 × Nachfragewert (dieselbe Formel wie `DemandRate`, nur
  mit Wunschpreis); Gewinn pro Tag = Bestellungen × (Preis − Einkauf − Karton).
- Anzeige in der Produktzeile: „Bei 22 €: ≈ 14 Bestellungen/Tag · ≈ 230 € Gewinn“, daneben der Wert beim
  Marktpreis. Der Knopf „Knapp unter Markt“ wird zu „Empfehlung übernehmen“: Er empfiehlt höher, wenn heute
  schon Bestellungen verloren gingen (`Daily.Lost > 0`).
- Tests: Die Prognose steigt bei niedrigerem Preis; die Empfehlung liegt über dem Marktpreis, wenn heute
  Bestellungen verloren gingen.

**Prüfliste für dich (▶ Play):** Trading-App öffnen, ein paar Minuten warten → der ETF bewegt sich kaum.
Im Einkauf die Premium-Drohne ansehen → deutlich teurer. Eine Tageskampagne starten → läuft bis Feierabend.
Viermal TikTok versuchen → beim vierten Mal gesperrt.
**Risiko:** niedrig (nur Zahlen); die Balance-Tests zeigen sofort, ob das Spiel zu schwer wird.

### 5.2 Tagesrhythmus: Nachtbestellungen + Abholzeiten (Ideen 11, 12, 13)

**Ziel:** Jeder Tag bekommt einen Bogen: morgens gleich Arbeit, mittags eine erste und um 17 Uhr die große
Abholung, abends Ruhe für Planung.
**Wann:** Nachtbestellungen (S) schon in Welle 2; Abholzeiten (M) in v3.1, wenn F1 (Fristen) fertig ist.

**Regeln & Zahlen**
- **Nachtbestellungen:** In `Sim.StartNextDay()` nach `Market.NewDay()`: Anzahl = gerundet 20 % der
  erwarteten Tagesbestellungen (`BusinessMinutes / OrderIntervalMinutes()`), höchstens `QueueCapacity() − 2`,
  nur für gelistete Produkte mit Lager oder Lieferung unterwegs, nicht während des Tutorials. Erstellt mit
  `Created = BClock()` – sie zählen als frisch und schaden der Bewertung nicht. F1-Notiz z. B. „Bestellt um
  02:14 – konnte nicht schlafen“. Toast: „Über Nacht: 4 neue Bestellungen“.
- **Abholzeiten:** `GameData.PickupTimes = { 720f, 1020f }` (12:00 und 17:00). Upgrade „Spätabholung“
  (Level 4, 20 € pro Tag) ergänzt 19:00. Optional mit den F6-Wochentagen: sonntags keine Abholung.
- **Versprechen:** Jede Bestellung bekommt beim Entstehen ihre Abholung: die nächste, die mindestens 60
  Spielminuten entfernt ist (Express: 30 Minuten). Das ist die F1-Frist; der Zettel zeigt „Abholung 17:00“.
  Also: bestellt bis 11:00 → 12:00; bis 16:00 → 17:00; danach → morgen 12:00.
- **Pünktlichkeit:** Beim Abgeben an der Versandstelle (`Sim.ShipPackage`) steht fest, ob die versprochene
  Abholung schon vorbei ist. Die bisherige Alters-Regel für Sterne (90/200/360 Minuten) wird ersetzt:
  pünktlich = Basis 4 Sterne, innerhalb von 60 Spielminuten verschickt = +1, verpasst = −1, Express verpasst
  = −2 und der Express-Aufschlag wird erstattet. Qualität wirkt wie bisher.
- **Geld gibt es weiterhin sofort beim Abgeben** – das sofortige „+€“ ist wichtig fürs Gefühl.
- **Abholung:** Überschreitet die Uhr eine Abholzeit, feuert ein neues Ereignis `PickupArrived(int index)`;
  der Zähler „Pakete seit der letzten Abholung“ wird genullt. Förderband und Versandkraft zählen wie die
  Versandstelle.
- **Tagesabrechnung:** „Pünktlich verschickt: 34 von 36“.

**Oberfläche:** HUD-Hinweis ab 60 Spielminuten vorher: „Abholung 17:00 · noch 25 min · 6 im Käfig“, in den
letzten 10 Minuten pulsierend. Der Bestell-Monitor zeigt die nächste Abholung. Das Morgen-Briefing (Idee 13)
nennt Nachtbestellungen und Abholzeiten.
**Welt:** Bei `PickupArrived` fährt der PaketBlitz-Wagen vor (vorhandenes `WorldBuilder.SendVan()` mit einem
zweiten Haltepunkt an der Versandstelle), hupt, und der Käfig wird leer (`Station.FillShip` zeigt „Pakete seit
der letzten Abholung“ statt `Daily.Shipped`).
**Betroffene Dateien:** `Core/GameData.cs`, `Core/Sim.cs` (`StartNextDay`, `AdvanceMinutes`, `SpawnOrder`,
`ShipPackage`), `Core/Model.cs` (`DaySummary`), `Runtime/World/WorldBuilder.cs` (`SendVan`),
`Runtime/World/Station.cs` (`FillShip`), HUD und Handy.
**Tests:** keine Nachtbestellungen, wenn nichts gelistet ist; Obergrenze eingehalten; Versprechen für
08:30, 11:30, 15:30, 16:30 und für Express korrekt; zu spät abgegeben → Abzug und Erstattung;
`PickupArrived` feuert genau zweimal pro Tag; Speichern und Laden mitten am Tag.
**Prüfliste für dich:** Neuen Tag starten → Toast „Über Nacht …“ und Zettel sind da. Um 16:50 ein Paket
abgeben → pünktlich. Um 17:05 → Hinweis „zu spät“. Um 17:00 fährt der Wagen vor.
**Risiko:** mittel. Absicherung: ein Schalter `GameData.PickupsEnabled`, mit dem sich die Abholzeiten notfalls
abschalten lassen.

### 5.3 Engpass-Assistent (Idee 63)

**Ziel:** Nie mehr ratlos. Das Spiel sagt in einem Satz, was gerade am meisten bremst, und bietet den
passenden Knopf.
**Wann:** Welle 2 (klein; baut auf der vorhandenen To-do-Liste `AppHome.BuildTodos` auf).

**Spiellogik:** neue Datei `Scripts/Core/Advisor.cs` mit `List<Hint> Evaluate(Sim s)`;
`Hint { Id, Severity (0 = Tipp, 1 = Warnung, 2 = dringend), Text, Target }`. `Target` ist z. B. `"app:buy"`
oder `"station:Dock"`. Sortierung: Stufe absteigend, dann Reihenfolge der Regeln. Neues Feld
`Sim.LastOrderAt` (Zeitpunkt der letzten Bestellung, im Spielstand mit Standardwert).

| Regel | Bedingung | Stufe | Text (Beispiel) | Ziel |
|---|---|---|---|---|
| R1 | Kein Produkt online | 2 | „Dein Shop ist leer – stell ein Produkt online.“ | app:shop |
| R2 | Online-Produkt ohne Lager, nichts unterwegs, keine Kiste am Eingang | 2 | „LED-Lichterkette ausverkauft – nachbestellen.“ | app:buy |
| R3 | Offene Bestellung, passender Karton fehlt | 2 | „Keine M-Kartons: falten (12 ungefaltet)“ bzw. „… kaufen“ | station:Fold / app:pack |
| R4 | Kontostand minus Fixkosten heute Abend unter −400 € | 2 | „Heute Abend droht die Pleite – dir fehlen 84 €.“ | app:bank |
| R5 | Kontostand reicht nicht für die Fixkosten heute Abend | 1 | „Miete und Löhne heute Abend: 215 € – du hast 130 €.“ | app:bank |
| R6 | Warteschlange zu 80 % voll | 1 | „Warteschlange fast voll – Preis leicht erhöhen oder schneller packen.“ | app:shop |
| R7 | Heute Bestellungen verloren | 1 | „3 Bestellungen verloren, weil die Warteschlange voll war.“ | app:shop |
| R8 | Kisten am Wareneingang, Platz im Lager | 1 | „2 Kisten warten am Wareneingang.“ | station:Dock |
| R9 | Seit 90 Spielminuten keine Bestellung, obwohl online und auf Lager | 1 | Diagnose: Preis > Markt × 1,15 → „Preis 18 % über Markt“; Bewertung < 2,5 → „Deine Bewertung schreckt ab“; Shop offline → „Server offline“ | app:shop |
| R10 | Lager zu 90 % voll | 1 | „Lager fast voll – weniger nachbestellen oder ausbauen.“ | app:build |
| R11 | Entscheidung offen | 1 | „Eine Entscheidung wartet auf dem Handy.“ | app:mail |
| R12 | Personal wartet (Packer:in ohne Lager oder Kartons) | 1 | „Leonie wartet auf Kartons S.“ | app:pack |
| R13 | Mindestens 3 verpackte Pakete ohne Etikett | 0 | „3 Pakete warten aufs Etikett.“ | station:Label |
| R14 | TikTok bereit, kein Boost aktiv | 0 | „Zeit für ein TikTok?“ | app:marketing |
| R15 | Ein Ziel zu 90 % erreicht | 0 | „Fast geschafft: noch 2 Pakete bis ‚Läuft bei dir‘.“ | – |

**Oberfläche:** Handy-Startseite: oberste Karte „Tipp“ mit Knopf „Zeigen“ (öffnet die App oder setzt den
Zielmarker auf die Station). Laptop-Übersicht: ersetzt `BuildTodos`. HUD: nur Stufe 2, als kleine Zeile unter
der Zielkarte, und nur wenn sich der Hinweis *ändert* (kein Dauerblinken).
**Welt:** Ziele vom Typ `station:…` nutzen den geplanten Zielmarker (Welle 2).
**Betroffene Dateien:** neu `Core/Advisor.cs`; `Core/Sim.cs` (`LastOrderAt` in `SpawnOrder`), Spielstand;
Laptop-Übersicht, Handy, HUD.
**Tests:** je Regel ein aufgebauter Spielzustand → erwarteter erster Hinweis; „alles gut“ → kein Hinweis der
Stufe 2; Sortierung stimmt; Texte enthalten die richtigen Zahlen.
**Prüfliste für dich:** Neues Spiel mit „Profi-Start“, Produkt im Shop offline schalten → das Handy sagt
„Dein Shop ist leer“. Alle S-Kartons verbrauchen → „Keine S-Kartons“.
**Risiko:** niedrig.

### 5.4 Mehrteilige Bestellungen & Kartonwahl (Ideen 1, 2)

**Ziel:** Abwechslung im Handgriff und kleine Planungsentscheidungen, ohne den Ablauf zu verkomplizieren.
**Wann:** v3.1, nach F1 (das Bestellmodell mit IDs muss stehen).

**Regeln & Zahlen**
- **Anteil:** Level 1–2: 0 %. Level 3–4: 15 % der Bestellungen mit 2 Artikeln. Ab Level 5: 25 %, davon 30 %
  mit 3 Artikeln. Zweiter und dritter Artikel: zu 50 % dasselbe Produkt, sonst ein anderes gelistetes Produkt
  mit Lager (nach Nachfrage gewichtet).
- **Volumen:** Produktgröße S = 1, M = 2, L = 4 Punkte. Karton fasst S = 1, M = 3, L = 6 Punkte. Bestellungen
  über 6 Punkte werden nicht erzeugt. Beispiele: 2× Hülle → M; Hülle + LED → M; LED + Massagepistole → L.
- **Preis** = Summe der Einzelpreise; pünktlich verschickt → +10 % „Warenkorb-Trinkgeld“.
- **Ablauf:** Artikel entnehmen → das Spiel ordnet ihn der ältesten Bestellung zu, die diesen Artikel noch
  braucht (Hand-Anzeige „LED für #1042 (2/3)“). Am Packtisch ablegen. Sind alle Artikel da, nimmt
  „Verpacken“ den kleinsten passenden Karton; fehlt er, den nächstgrößeren plus 0,50 € „Luftpolster“ und
  einen Hinweis.
- **Wichtig:** Heute entfernt `Sim.PickItem` die Bestellung schon beim Entnehmen aus der Warteschlange. Künftig
  bleibt sie mit Status „in Arbeit“ drin, bis sie verpackt ist.
- Der Packtisch fasst höchstens 3 angefangene Bestellungen („Packtisch voll“).
- Artikel zurück ins Regal → Zuordnung wird gelöst. Artikel auf den Boden → bleibt zugeordnet.
- Packer:innen verarbeiten mehrteilige Bestellungen nur, wenn alle Artikel auf Lager sind; Dauer = Intervall ×
  Artikelzahl.
- Mit Porto (Idee 3): Eine M-Sendung ist billiger als zwei S-Sendungen → Sammelbestellungen lohnen sich doppelt.

**Oberfläche:** Der Zettel zeigt die Positionen mit Häkchen; HUD-Zettel „2 Artikel“. Hinweis am Packtisch:
„Ablegen (#1042: 2/3)“ bzw. „Verpacken (#1042 komplett · Karton M)“.
**Welt:** Abgelegte Artikel liegen sichtbar auf dem Packtisch, in drei Feldern (vorhandenes `ItemKit`).
**Betroffene Dateien:** `Core/Model.cs` (`Order`: Positionen und Status; `ItemData`: Bestell-ID),
`Core/Sim.cs` (`SpawnOrder`, `PickItem`, `ReturnItem`, `WrapItem`, `StaffWork("packer")`), neue Klasse
`PackTable` in `Scripts/Core`, `Runtime/World/Station.cs` (Packtisch, `FillPacked`), Zettel in HUD und Handy.
Alte Spielstände: Bestellungen ohne Positionen = eine Position.
**Tests:** Anteile je Level (2.000 Bestellungen, fester Seed, ±3 %); Karton-Tabelle; Zuordnung zur ältesten
Bestellung; Verpacken erst bei Vollständigkeit; Ersatzkarton kostet 0,50 €; Packer:in; Speichern und Laden
mit halb gepackter Bestellung.
**Prüfliste für dich:** Level 3 erreichen, auf einen Zettel mit 2 Artikeln warten, beide holen, ablegen,
verpacken → ein Paket, ein Etikett, ein „+€“ mit Trinkgeld.
**Risiko:** mittel – betrifft das Herz des Spiels; deshalb erst nach F1 und mit vielen Tests.

### 5.5 Paketstapel & Tragehilfen (Idee 5)

**Ziel:** Die eigene Handarbeit wird mit dem Fortschritt besser. Weniger Laufen, mehr „Wow, sechs Pakete auf
einmal!“.
**Wann:** Welle 2 – der geplante F5-Skill „Tragekraft“ braucht ohnehin eine Wirkung in der Welt.

**Regeln & Zahlen** (Spiellogik: `Sim.CarryCapacity(ItemKind kind)`)

| Was | Basis | Tragegurt (Level 2, 150 €) | F5-Skill „Tragekraft“ | Sackkarre (Level 3, 350 €) | Rollwagen (Level 5, 900 €, nur Halle) |
|---|---|---|---|---|---|
| Pakete (mit oder ohne Etikett) | 1 | 2 | +1 | – | 6 (fest) |
| Kisten | 1 | – | – | 2 | – |
| Einzelartikel | 1 | – | Endknoten „Zwei Hände“ → 2 | – | – |

- Lauftempo: 2 Kisten ×0,8 (heute 1 Kiste ×0,85), Rollwagen ×0,9.
- Gestapelt werden nur Pakete mit Paketen (mit und ohne Etikett gemischt) und Kisten mit Kisten.
- **Packtisch:** Ein Druck nimmt so viele fertige Pakete, wie passen („tock-tock-tock“).
- **Labeldrucker:** Ein Druck etikettiert alle Pakete ohne Etikett im Stapel (ein Druckgeräusch je Paket,
  0,15 s versetzt).
- **Versand / Förderband:** Ein Druck gibt alle etikettierten Pakete ab; jedes zeigt sein eigenes „+€“, leicht
  versetzt. Pakete ohne Etikett bleiben in der Hand.
- **Wareneingang / Regal:** Mit Sackkarre bis zu 2 Kisten aufnehmen; am Regal wird die passende eingeräumt, die
  andere bleibt in der Hand.
- **G (Ablegen)** legt nur das oberste Teil ab.

**Oberfläche:** Hand-Anzeige „Pakete 3/4 (2 versandfertig)“; Hinweise „Alle 3 etikettieren“, „3 Pakete
abgeben (+71 €)“. Die drei Tragehilfen stehen in der Ausbau-App.
**Welt:** Der Stapel erscheint in den Händen als Turm (vorhandenes `ItemKit`, versetzt übereinander); der
Rollwagen zunächst als einfacher Wagen vor der Kamera, ohne Physik.
**Betroffene Dateien:** `Core/GameData.cs` (3 neue `Upgrades`), `Core/Sim.cs` (`CarryCapacity`),
`Core/Model.cs` (`PlayerSave`: zusätzliche Liste `held_stack`, fehlt sie → leer),
`Runtime/Player/PlayerController.cs` (`Held` bleibt das oberste Teil, dazu eine Liste `Stack`; Tempo),
`Runtime/World/Station.cs` (Packtisch, Drucker, Versand, Förderband, Wareneingang, Regal),
`Runtime/UI/HudView.cs` (`SetHeld`).
**Tests:** Kapazität je Upgrade und Skill; Speichern und Laden eines Stapels; mehrere Pakete nacheinander
über die Sim-Funktionen etikettieren und verschicken (Geld und Zähler stimmen).
**Prüfliste für dich:** Tragegurt kaufen, zwei Bestellungen verpacken, beide Pakete nehmen, einmal E am
Drucker, einmal E an der Versandstelle → zwei „+€“.
**Risiko:** mittel – Welt-Code, blind kompiliert; die Logik ist aber klein und die Wirkung beim Play-Test
sofort sichtbar.

---

## 6. Fragen an dich

1. **Koop – ja oder nein, und wann?** Das GDD verspricht 1–4 Spieler. Echter Online-Koop ist mit Abstand das
   riskanteste Vorhaben, solange niemand zwei Spielinstanzen gleichzeitig testen kann.
   → *Meine Empfehlung:* **Ja, als großes Ziel für v4 oder v5** – Koop-Chaos ist auf Steam der stärkste Hebel
   für Aufmerksamkeit. Aber erst, wenn das Solo-Spiel rund ist. Bis dahin „koop-fähig“ bauen (Idee 60) und
   als Zwischenschritt Couch-Koop mit Steam Remote Play Together prüfen (Idee 61).

2. **Trading behalten?** Heute ist es eine Gelddruckmaschine.
   → *Meine Empfehlung:* **Behalten, aber klein und satirisch** – entschärft (Skizze 5.1) und mit den
   Krypto-Bro-Geschichten verknüpft. Kein Weg, am Spiel vorbei reich zu werden. Die Krypto-Gags passen zum Ton.

3. **Wie viel Geschichte, und welcher Ton?**
   → *Meine Empfehlung:* **Eine leichte Rahmenhandlung in drei Akten mit Kalle als Herz** (Idee 38): kurze
   Dialoge, alles überspringbar, warmherzig statt zynisch. Satire nur nach oben (Hustle-Gurus, Bürokratie,
   Tech-Milliardäre), nie gegen Kundschaft, Personal oder Herkunft.

4. **Wie „cozy“ soll es sein – Zeitdruck und Pleite?**
   → *Meine Empfehlung:* **Ruhige Tage mit zwei sanften Stoßzeiten** (Abholung um 12 und 17 Uhr) plus
   einzelne Chaos-Tage (Black Friday). Im normalen Modus bleibt die Pleite möglich, dazu kommt ein
   **„Entspannt-Modus“ ohne Pleite** (Idee 64). Einmal im Spiel rettet dich Kalle – als Story-Gag.

5. **Nur Deutsch oder auch Englisch?**
   → *Meine Empfehlung:* **Deutsch bleibt die Hauptsprache, Englisch kommt spätestens zum Steam-Release.**
   Dafür früh alle Texte aus dem Code in Tabellen ziehen – je später, desto teurer. Wortwitze gezielt neu
   schreiben lassen.

---

## Anhang: Kleines Wörterbuch

- **Kern-Kreislauf (Core Loop):** das, was man immer wieder tut – hier: einkaufen, einräumen, packen, verschicken.
- **Spiellogik / Core:** die Regeln des Spiels in reinem C# (`Scripts/Core`), ohne Grafik – automatisch testbar.
- **Welle 1 / Welle 2:** die Arbeitsschritte im v3.0-Update (siehe `docs/REWORK_PLAN.md`).
- **Balance:** die Abstimmung der Zahlen (Preise, Tempo, Belohnungen), damit es weder zu leicht noch zu schwer ist.
- **Geldmaschine (Exploit):** eine Lücke, mit der man das Spiel austricksen kann.
- **Juice:** sattes Feedback – Geräusche, kleine Animationen, fliegende Zahlen.
- **Kurs-Tick:** eine Kursänderung beim Trading, alle 2 echten Sekunden.
- **Drift / Schwankung:** wie stark ein Kurs im Schnitt steigt / wie stark er hin- und herspringt.
- **Median:** der mittlere Wert – die Hälfte der Fälle liegt darüber, die Hälfte darunter.
- **Seed:** ein fester Startwert für den Zufall, damit Tests immer gleich ausfallen.
- **Friendslop:** chaotische Koop-Spiele mit Freunden (z. B. Lethal Company, R.E.P.O., Schedule I).
- **B2B:** Geschäfte mit Firmen statt mit Privatkundschaft (hier: Großaufträge).
- **Hype / Trend:** wie angesagt ein Produkt gerade ist.
- **Neustart-Bonus (New Game+):** nochmal von vorn, aber mit einem kleinen Vorteil.
