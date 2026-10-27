# Dropshipping Simulator – Game Design Document

> Stand: **1.0.0-rc1** (27.10.2026) · Hustle Games. Abschnitt 0 beschreibt das Spiel, wie es in 1.0 ist.
> Die Abschnitte 1–16 sind die ursprüngliche Vision aus der Konzeptphase und bleiben als Referenz stehen;
> Abweichungen stehen in Abschnitt 0. Zahlen und Balance: Abschnitt 17. Fahrplan: `docs/ROADMAP.md`.

## 0. Feature-Überblick 1.0 (Ist-Stand)

**Genre & Ton:** First-Person-Wirtschaftssimulation, Einzelspieler, lockere Satire auf Hustle- und Dropshipping-Kultur.
Zielpreis 6–8 € auf Steam (Windows zuerst). Spielzeit bis zum Endziel 6–9 h, danach Endlos-Modus.

**Kernschleife (ein Tag, 8–20 Uhr):** Bestellungen kommen über den eigenen Shop → Ware am Tablet einkaufen (AllesExpress,
Läden in der Stadt) → Kisten am Wareneingang → Regal → Kommissionieren → Karton falten → Packtisch → Label → Packstation,
Post oder Moped-Heckbox. Abends Kassenbon-Abrechnung, Fixkosten, Ereignis-Entscheidungen, Gewinn reinvestieren.

| Bereich | In 1.0 |
|---|---|
| Einstieg | Intro-Schicht in Kalles Imbiss, Kündigung, Garage mit Startkapital; Tutorial der ersten Stunde, Erklär-Nachrichten |
| Logistik | 14 Produkte, 3 Lieferanten, Kartons S/M/L (fertig oder ungefaltet), Bestellzettel mit Frist und Express, Retouren, Großaufträge (B2B) auf Paletten, Förderband, Hochregal, Verpackungsmüll, defekte Chargen, Porto & Paketdienste, Lieferstörungen |
| Orte | Garage → Lagerhalle (Level 4) → Außenlager (Level 10); Wohn-, Einkaufs- und Finanzviertel; Imbiss, Post, Bank, Börse, MediaMarkd, Fressnix |
| Bedienung | Handy (Bestellzettel, Nachrichten, Wochenziele, Status/Engpass-Assistent, Wallet) und Tablet „HustleOS“ mit Web-Parodien und Firmen-Apps |
| Marketing | Eigener Shop mit Preisen und Aktionen, Branding, Flyer, Influencer, Fakebook/Gugel-Anzeigen, Fake-Profile, KI-Fake-Seiten, Trends & Hype mit Trendradar, selbst gedrehte TikToks |
| Nebenspiele | Straßenfest mit Stand, Verkaufsstand vor der Garage, Pitch Day (drei Juroren), Börse/ETF/Krypto/Sparkonto, Haustiere |
| Fortschritt | Firmenlevel 1–10, Skillbaum (3 × 4), Hustle-Perks, Einrichtung/Energie, Ausrüstung, Personal, Ziele, Wochenziele, 30 Ereignisse, 40 Erfolge, Statistik |
| Endspiel | 100.000 € Umsatz → Viertelmillion, Außenlager, Halbe Million, Börsengang bei 1 Mio., danach verdoppelnde Ziele |
| Fairness | Schwierigkeit Entspannt/Normal/Hart, Mama-Notkredit, Insolvenz unter −500 € mit Laden des letzten Stands |
| Bewegung | Laufen, Sprinten, Ducken, Moped (Arcade-Physik, Heckbox, Ego-/Verfolgerkamera), Möbel verschieben, Emote-Rad |
| Präsentation | Unity 6.3 URP, CC0-Modelle mit prozeduralen Rückfällen, Tagesstimmungen, Wetter, Feedback-Effekte, Synth- und CC0-Audio |
| Komfort | Deutsch/Englisch live, Controller, Tastenbelegung, Barrierefreiheit, Streamer-Modus, 3 Spielstände, Auto-Save, `.bak`-Sicherung |

**Abweichungen von der ursprünglichen Vision (Abschnitte 1–16):**
- **Koop 1–4 Spieler** ist nicht in 1.0, sondern das erste große Update nach dem Release. 1.0 ist Einzelspieler;
  die Rolle der Mitspieler übernimmt das NPC-Personal.
- **Art-Stil:** statt Strichmännchen und PS1-Look ein warmer, satirischer Comic-Realismus mit CC0-Low-Poly-Modellen
  (siehe `docs/ART_DIRECTION.md`).
- **Musik:** CC0-Stücke statt Eigenproduktion (streamer-sicher); ein eigener Soundtrack ist für später notiert.
- **Monetarisierung:** keine Käufe im Spiel. Kosmetische DLCs (Klamottenladen ist vorbereitet) und weitere Ideen
  (Krypto-Mining, Casino, neue Viertel) stehen in `docs/ROADMAP.md`.

## 1. High Concept

Ein cozy Koop-Business-Sim in voll begehbarer 3D-Welt. 1–4 Spieler führen gemeinsam eine
Dropshipping-Firma – von der improvisierten Garage bis zum automatisierten Warehouse.
Art-Referenz: *R.V. There Yet?*, *How to Fish* (painterly, cel-shaded, liebevoll detailliert,
"Friendslop"-Stimmung). Gameplay-Referenz: *Overcooked* (physische Koordination, Rollenaufteilung)
trifft *Game Dev Tycoon* / *Startup Company* (Langzeit-Progression, Wirtschaftssimulation).

## 2. Plattform & Team

- **Ziel:** Steam (Windows, ggf. später weitere Plattformen)
- **Engine:** Unity 6 LTS (C#, URP, UI Toolkit, Input System) – seit v2.0; vorher Godot 4.7.
  Alles wird zur Laufzeit per Code gebaut, ein Editor-Skript richtet das Projekt automatisch ein,
  sodass kein manueller Editor-Aufwand nötig ist ("KI schreibt den kompletten Code")
- **Team:** Freundesgruppe, kein bestehendes Coding-Know-how im Team – Code wird vollständig
  KI-gestützt (Claude Code) entwickelt
- **Umfang:** Langzeit-Progression, Dutzende Spielstunden

## 3. Spieleranzahl & Skalierung

- Skalierbar **1–4 Spieler**, vollwertig solo spielbar
- Fehlende Rollen werden von anheuerbaren **NPC-Mitarbeitern** übernommen
- NPC-Personal ist gleichzeitig zentrale Progressionsmechanik: teuer, aber automatisiert Aufgaben
  → doppelter Nutzen (Solo-Enabler + Lategame-Ziel für alle Teamgrößen)

## 4. Rollen (frei wechselbar, nicht fest zugewiesen)

| Rolle | Aufgabe |
|---|---|
| **Einkauf/Sourcing** | Lieferanten-Terminal durchsuchen, Preise verhandeln, Wareneingänge bestellen |
| **Logistik** | Pakete physisch annehmen, einpacken, sortieren, versenden – Kernstück der physischen Interaktion |
| **Marketing** | Anzeigen/Social-Posts erstellen, auf Trends reagieren, Kundenbewertungen managen |
| **Kundenservice/Finanzen** | Beschwerden bearbeiten, Rückerstattungen, Buchhaltung, Skalierungsentscheidungen |

## 5. Core Loop (ein "Tag")

1. **Morgens:** Bestellungen sichten, Prioritäten setzen
2. **Tagsüber:** Pakete abarbeiten (physische Logistik) parallel zu Einkaufs-/Marketing-Entscheidungen
3. **Events (unregelmäßig):** Lieferant fällt aus, viraler Trend pusht ein Produkt, Zollkontrolle,
   eskalierende schlechte Bewertung
4. **Abends:** Auswertung, Gewinn reinvestieren (Ausrüstung, größeres Lager, Personal)

## 6. Zeitdruck-Design

- **Normale Tage:** entspanntes, cozy Tempo – kein harter Timer
- **Event-Tage** (Black-Friday-artige Spitzen, virale Momente): kurzzeitig echter Overcooked-artiger
  Chaos-Druck, danach zurück zu ruhigem Alltag
- Ziel: Spannungsbogen statt Dauerstress – passt zum Cozy-Anspruch, behält aber Koop-Nervenkitzel

## 7. Progression

- **Location-Stages:** Garage (chaotisch, improvisiert) → kleines Warehouse → ausgebautes Warehouse
  mit Förderbändern/Automatisierung → ggf. mehrere Standorte
- Sichtbares physisches Wachstum der Welt als Hauptfortschrittsindikator
- Wirtschaftliche Tiefe: Produktkategorien, Lieferantenbeziehungen, Marktrends, Konkurrenz

## 8. Art-Stil

- **Referenz: Schedule I** – geerdeter Low-Poly-Stil (PS1/PS2-artig), einfache Polygon-NPCs,
  keine aufwendigen Cel-Shading-Shader nötig → reduziert Asset-Aufwand/-Kosten spürbar gegenüber
  einem painterly Look
- **Spielercharaktere = Strichmännchen** ("Stick Figures"), heben sich bewusst optisch von den
  Polygon-NPCs ab – markantes visuelles Wiedererkennungsmerkmal, einfach zu animieren
- Farbpalette/Beleuchtung: geerdet/realistisch angehaucht statt knallig-cartoonig, aber klar
  stilisiert, kein Photorealismus

## 9. Ton & Humor

Mix aus warmherzig-cozy (Grundstimmung, freundliche NPCs, Chaos bleibt liebevoll) und
trocken-absurder Satire bei Events/Produkten (Internet-Kultur-Gags, dubiose Charaktere,
überzogene Business-Momente).

## 10. Musik

Lo-fi/Chillhop – entspannte Beats für den Alltag, kontrastiert gut mit hektischeren Event-Tagen.
Eigene Produktion statt KI-generiert (siehe Abschnitt 12).

## 11. Event-Katalog (Startliste, wird erweitert)

| Kategorie | Beispiele |
|---|---|
| Nachfrage-Spitzen | Black-Friday-Wellen, Feiertags-Ansturm, Produkt geht viral |
| **Social-Media-Hype-Plattform** (TikTok-artig, In-Game) | Eigenes Mini-Social-Network, Trends entstehen/verpuffen sichtbar, Spieler können gezielt pushen |
| **Pitch Day** | Ein Unternehmen lädt die Spieler zu einem Verkaufsgespräch ein – Verhandlungs-/Präsentations-Mechanik, Belohnung bei Erfolg |
| **Straßenverkauf** | Produkte direkt an NPCs auf der Straße anbieten – optionale physische Zusatz-Einnahmequelle, passt zum begehbaren 3D-Kern |
| Lieferketten-Probleme | Lieferant fällt aus, Zoll hält Ware fest, Ware kommt beschädigt an |
| Kunden-/Reputationskrisen | Shitstorm wegen Bewertung, Fake-Review-Skandal, eskalierender Kunde |
| Absurde/Satire-Events | Krypto-Bro will investieren, dubiose Konkurrenz kopiert Produkt, Influencer-Kooperation läuft schief |

## 12. Onboarding

Klassisches Tutorial-Overlay/Popups bei erstem Kontakt mit jeder Mechanik, jederzeit im Menü
nachschlagbar (kein rein diegetisches Tutorial).

## 13. Monetarisierung nach Release

Rein kosmetische Erweiterungen ohne Gameplay-Einfluss: Kleidung/Caps für die Strichmännchen-Spieler,
Themen-Packs (z.B. Krypto-Bro-Shirts) – passend zum satirischen Ton. Zusätzlich OST als separater
Verkauf (Steam/Spotify) wie besprochen.

## 15. E-Commerce/Marken-Kern (Marke statt nur Logistik)

Der Kern ist nicht nur Logistik, sondern eine **echte Marke aufbauen**:

- **Multi-Anbieter-Einkauf:** mehrere Lieferanten pro Produkt, jeweils unterschiedliche Qualität,
  Preis und Lieferzeit – bewusste Trade-off-Entscheidung statt einem generischen Kauf-Button
  (billig/langsam/schlecht vs. teuer/schnell/gut)
- **Branding:** eigener Markenname + Logo + Farbschema, erscheint sichtbar auf Verpackung und
  Versand-Label – macht das eigene Business optisch erkennbar
- **Eigener Online-Shop (In-Game-Webseite):** Produkte selbst listen (Titel, Preis, Lieferzeit,
  Rabattaktionen) – treibt die Nachfrage aktiv, statt nur zufällig generierter Bestellungen
- **Marketing-Suite:** TikTok-Videos drehen (Handy-Minigame), bezahlte Werbung (Google/Facebook,
  Budget-Allocation), zu Beginn auch analoge Werbung (Flyer/Plakate) als günstiger Einstieg
- **Lifestyle/Erfolg-Meta:** sichtbarer persönlicher Fortschritt (Wohnung, Auto, Ausstattung) als
  Belohnung für Geschäftserfolg – das "Hustler"-Gefühl on top der reinen Wirtschaftssimulation

## 16. Bau-Reihenfolge (Roadmap)

> Ab Phase 7 wird der konkrete Schlachtplan für den restlichen Ausbau zum
> vollständigen Spiel separat in `docs/ROADMAP.md` geführt (Speichern,
> Hauptmenü, Gastro-Job-Intro, Progression, Events, Personal, Konkurrenz,
> Trading, Koop). Dieser Abschnitt hier bleibt das Protokoll der bereits
> fertigen Phasen.

1. **Phase 1 (fertig):** Physische Fulfillment-Kette (Einkauf → Dock → Regal → Verpacken → Label →
   Versand), Tragen/Halten-System, Basis-Wirtschaft, PC-Grundgerüst
2. **Phase 2 (fertig):** Branding-System – Markenname + Logo + Farbe wählbar, wird beim Kauf von
   Verpackungsmaterial auf die Kartons "gedruckt" (Snapshot zum Kaufzeitpunkt, keine rückwirkende
   Umfärbung bereits gekaufter Ware), sichtbar auf jedem getragenen Paket/Label
3. **Phase 3 (fertig):** Multi-Anbieter-Sourcing – 3 Anbieter (Billig/Standard/Premium) mit
   Qualität/Preisfaktor/Lieferzeit-Tradeoff; Qualität fließt gewichtet in Lagerbestand und
   Verkaufspreis (Bonus/Malus beim Versand) ein
4. **Phase 4 (fertig):** Webseite-App – eigener Verkaufspreis + Rabattaktion steuern aktiv die
   Bestellrate (niedriger Preis = mehr, aber kleinere Bestellungen)
5. **Phase 5 (fertig):** Marketing-Suite – TikTok-Video-Timing-Minigame (Trefferquote bestimmt
   Boost-Stärke/-Dauer), Werbung-App mit 3 Stufen (analoge Flyer → Facebook → Google Ads)
6. **Phase 6 (fertig, einfache Version):** Lifestyle-App – kosmetische Erfolgs-Käufe
   (Sneaker/Auto/Wohnung/Uhr), rein Status-Anzeige ohne Gameplay-Effekt
7. **Phase 7 (fertig):** Produktkatalog & Kartongrößen – 3 eigene Produkte (Handyhülle/S,
   LED-Lichterkette/M, Massagepistole/L) mit eigenem Einkaufspreis, Richtwert-Verkaufspreis und
   Beliebtheit; je ein eigenes Regal pro Produkt in der Welt (falsches Produkt in falsches Regal
   stellen wird verweigert); Webseite listet/entlistet Produkte einzeln mit eigenem Preis, die
   Gesamt-Bestellrate ergibt sich aus der Summe aller gelisteten Produkte; Verpackungsmaterial gibt
   es jetzt in 3 Kartongrößen (S/M/L, passend zur Produktgröße), zusätzlich als günstigere
   "ungefaltete" Variante, die erst am neuen Falttisch (eigene Station) gefaltet werden muss, bevor
   sie am Packtisch nutzbar ist – setzt die im Konzept-Chat vorgemerkte "ungefaltet bestellen"-Idee um

8. **v1.0 – großer Umbau (25.09.2026):** komplettes Spiel mit Hauptmenü, Imbiss-Intro,
   Tagesablauf (08–20 Uhr, Abrechnung), 6 Produkten, Level 1–10, Lagerhalle, Personal,
   Förderband, 20 Ereignissen, Konkurrenz, Trading, Bewertungen, Zielen, Insolvenz und
   Endziel. Neue Stadt-Kulisse, prozedurale Shader, Tageslicht, NPCs, Verkehr,
   prozedurale Musik und Soundeffekte, neues UI ("HustleOS"). Details: `docs/ROADMAP.md`.

9. **v2.0 – Unity-Umbau (25.09.2026):** komplette Portierung nach Unity 6 mit URP-Grafik
   (SSAO, Bloom, weiche Schatten), neuem UI Toolkit-Interface, HustleOS in zwei Designs
   (hell/dunkel), neuen Apps (Übersicht, Bank), Verkaufsstand, Pitch-Day-Minispiel,
   10 Produkten, 3 Spielständen und Controller-Unterstützung. Details: `docs/ROADMAP.md`.

### Bewusst vereinfacht / nächste Ausbaustufe
- Koop-Multiplayer noch nicht umgesetzt (Einzelspieler) – geplant als großes Update nach 1.0
- Ab v3.0 kamen echte CC0-Modelle, -Texturen und -Sounds dazu; die Phasen danach (Stadt, Läden, Finanzviertel,
  Marketing, Moped, Englisch, Release-Technik) fasst Abschnitt 0 zusammen.

## 17. Zahlen & Balance (Kampagne 6–10 h, Stand Okt. 2026)

Gemessen mit dem Balance-Bot (`Tests/EditMode/BalanceBot.cs`, Tests in `BalanceTests.cs`, `MetaTests.cs`,
`FinanceBalanceTests.cs`). Der Bot spielt über die öffentliche Sim-API mit menschlichem Tempo und nutzt alle
Systeme: Perks, Einrichtung/Energie, Müll, Porto (Packstation gratis vs. Abholgebühr), Lieferstörungen
(TurboKurier), Defekte (Rückruf), Großaufträge & Großhändler, Werbung (Flyer/Influencer, Fakebook, Gugel,
Fake-Profile, KI-Seiten), TikTok (Wertung echt aus `TikTokScoring.Rate`, Ø-Aufnahme ≈ 0,6), Straßenfest,
Verkaufsstand, MediaMarkd/Fressnix & Haustiere, Börse/ETF/Krypto/Sparkonto, Kredite, Personal, Förderband.

**Echtzeit-Annahme:** 1 Geschäftsminute = 0,75 s (Tag = 9 min) + 45 s je Tageswechsel (Abrechnung,
Ereignis-Dialoge). Hand-Tempo: vorsichtig 46, Durchschnitt 40, gierig 34 Spielminuten je Bestellung
(Lagerhalle ×1,3 wegen längerer Wege), Müdigkeit bremst.

### Kurve (Median aus 4 Seeds, Schwierigkeit „Normal“)

| Spielertyp | 1. Level | Lagerhalle | 100.000 € Umsatz | vorher (v3-Zahlen) |
|---|---|---|---|---|
| Vorsichtig (Standard-Ware, Packstation, Sparkonto, keine Tricks) | 5–11 min | 2,6–3,1 h (Tag 17–20) | **7,0–7,4 h** (Tag 44–46) | 3,4–4,3 h |
| Durchschnitt (etwas Werbung, 2 TikToks, Fest, ETF, Katze) | 5–11 min | 1,9–2,4 h (Tag 13–16) | **5,6–5,9 h** (Tag 35–37) | 2,7–3,3 h |
| Gierig (Billig-Ware, Wucher, Werbung stapeln, Fakes, Krypto, Kredit) | 12–25 min | 1,6–2,1 h (Tag 11–14) | **5,0–5,6 h** (Tag 31–35) | 2,4–2,8 h |

Bot-Zeiten sind eine Untergrenze (perfekte Information, keine Pausen) – echte Spieler liegen eher bei 6–9 h.
Belohnungen (Level, Ziel, Wochenziel, Auftrag, Fest, Hustle-Punkt, Erfolg): Ø alle 2–3 min, längste Lücke in
den ersten 3 h ≤ 25 min (Test `Pacing_FrequentRewardsInFirstHours`).

### Phasen

1. **Garage (0–2 h):** Hände sind der Engpass (≈ 18–20 Pakete/Tag). Freischaltungen gestaffelt statt alles
   auf einmal: L2 Massagepistole/Bart-Glitzer, Express, TikTok, Stand · L3 Großaufträge, Fakebook,
   Premium, Großbestellung · L4 Lagerhalle (7.000 €), Kopfhörer/Ringlicht, Straßenfeste, Börse.
2. **Lagerhalle (2–4 h):** je Rolle **eine** Kraft (Packer:in 48 min/Paket, Versand 22, Lager 24; Löhne
   75/90/80 €), dazu der Spieler – ≈ 40 Pakete/Tag, 3–4,5 k€ Umsatz/Tag. L5 Personal, Gugel, KI-Seiten,
   Palette, Lieferwagen · L6 Förderband, 2 Großaufträge · L7 Smartwatch, Influencer.
3. **Ausbau (4–6 h):** teurere Produkte (L8 Beamer, L9 Drohne), Hochregal, Großhändler-Nachbestellungen.
4. **Endlos (ab 100.000 €):** Abspann → Viertelmillion → **Außenlager** (L10, 40.000 €, +3.000 Plätze,
   +6 Warteschlange, Personal +20 %, Miete +150 €) → Halbe Million → **Börsengang** bei 1 Mio. (Modal +
   Mail) → danach Umsatzziele 2/4/8/… Mio. mit 1 % Belohnung. Ab **L10 zweite Kraft je Rolle**.

### Wichtige Zahlen

| Bereich | Wert |
|---|---|
| Level-XP | 150 · 700 · 1.600 · 3.200 · 6.000 · 12.000 · 24.000 · 38.000 · 56.000 |
| Nachfrage | 90 min / Σ Nachfrage; Menge (Produkte, Boosts) gedämpft mit Exponent 0,65, Preis wirkt voll |
| Preis | bis Markenpreis keine Einbuße; darüber Elastizität 2,6; ab ×1,25 −1 Stern & Retouren ×1,5, ab ×1,6 −2 Sterne |
| Boosts | Gesamtfaktor höchstens ×2,5 (vorher ×4) |
| Verlorene Bestellung | 30 % Chance auf 2-Sterne-Bewertung (vorher 60 % auf 1 Stern) |
| Großaufträge | Wert (50 + 32·Level) × 1,1 in der Halle, Zahlung 0,27–0,36 × Ref-Preis; Slots L3: 1, L6: 2, L9: 3 |
| Großhändler | erst mit Lagerhalle, Menge ×1,5, Zahlung 0,28–0,33, Chance 12 % + 15 % × Markenstärke |
| Straßenfest/Stand | Spontankauf-Budget 45 €: teure Ware verkauft sich seltener (Faktor 45/Preis, min. 0,3) |
| Personal-Perk / Zusatzstationen | +5 % je Stufe / +10 % je Tisch (vorher 8 % / 15 %) |
| Kredit | 2 %/Tag; keine Anlage schlägt das (Sparplan 45 Tage: ETF Ø ×1,0–1,1, Krypto Ø ×0,7–1,15, Spanne ×0,2–×3) |

### Exploits (30 Tage, 3 Seeds, Durchschnitt + genau ein Trick; Test `Exploits_AreCapped`)

Deckel: höchstens +30 % Umsatz und +50 % Nettovermögen gegenüber dem schlichten Spieler; Krypto ≤ +10 %,
Kredit+Anlage ≤ ±0, MediaMarkd ≤ +10 %, volles Fest ≤ +15 % Umsatz. Geschlossen: **Wucher + Werbe-Stapel**
(Nachfrage-Dämpfung hatte Preise unelastisch gemacht → Preis-Elastizität getrennt, Boost-Deckel ×2,5,
Sterne-Abzug), **Straßenfest/Stand mit teurer Ware** (vorher 5× Tagesumsatz an einem Festtag),
**Großhändler in der Garage**, **Personal-Multiplikatoren**.

### Sackgassen & Rettung

* Pleite nur durch grobe Fehler (Kredit verprassen, nichts verkaufen). Warnung im Advisor und in der
  Tagesabrechnung; **Mama-Notkredit**: einmal (Entspannt: zweimal, Hart: nie) überweist Mama so viel, dass nach
  den Fixkosten 500 € bleiben – zinslos, wird ab 1.500 € Kontostand mit 20 % des Überschusses abgestottert.
* Schwierigkeit beim neuen Spiel: **Entspannt** (Nachfrage ×1,15, Kosten ×0,85, schlechte Bewertungen ×0,7,
  300 € Start, 2× Mama) · **Normal** · **Hart** (×0,88, ×1,15, ×1,35, 100 € Start, keine Mama).

### Erfolge & Statistik

40 Steam-Erfolge (`AchievementData`, API-Namen `ACH_…`), im Spielstand gespeichert; Runtime meldet an
`Platform.UnlockAchievement` und nach dem Laden erneut. Firma › Erfolge (mit Fortschritt, geheime erst nach
dem Freischalten) und Firma › Statistik (Gesamtzahlen, nächstes Ziel).

## 14. Offene Punkte (noch zu klären)

- [x] Progressions-Kurve: siehe Abschnitt 17 „Zahlen & Balance“ (per `BalanceTests.cs` gesichert)
- [x] Event-Häufigkeit: 0–2 Ereignisse pro Tag ab Tag 2, gewichtete Auswahl mit Abklingzeit
- [x] Pitch Day: Minispiel mit drei Juroren; Antworten stützen sich auf echte Kennzahlen
- [x] Social Media: TikTok-Handy-Minispiel + Praktikant:in, der täglich postet
- [ ] Koop: Rollenverteilung und Netzwerk-Synchronisation (nach 1.0, siehe `docs/ROADMAP.md`)

## 9. Tech-/Tool-Stack (Referenz)

Siehe Konzept-Chat für Details zu KI-Tools (3D: Meshy/Luma/Kaedim, Art: Midjourney, Animation:
Mixamo/Cascadeur, Audio: eigene Produktion statt KI). Wird bei Bedarf in eigenes `TOOLS.md`
ausgelagert.
