# Dropshipping Simulator – Game Design Document (Entwurf v0.1)

> Arbeitstitel. Status: Konzeptphase, wird laufend erweitert.

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

## 15. E-Commerce/Marken-Kern ("Hustler Simulator"-Erweiterung)

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
- Koop-Multiplayer noch nicht umgesetzt (Einzelspieler)
- Alle Modelle sind stilisierte Grundformen (kein importiertes Art-Asset)

## 14. Offene Punkte (noch zu klären)

- [x] Progressions-Kurve: Level 1–10 (XP-Schwellen in `GameData.LevelXp`), Lagerhalle bei
  Level 4 / 4.500 €; per `BalanceTests.cs` geprüft (Lagerhalle ca. Tag 12 bei
  menschlichem Tempo, danach Wachstum mit Personal)
- [x] Event-Häufigkeit: 0–2 Ereignisse pro Tag ab Tag 2, gewichtete Auswahl mit Abklingzeit
- [x] Pitch Day: Minispiel mit drei Juroren; Antworten stützen sich auf echte Kennzahlen
- [x] Social Media: TikTok-Handy-Minispiel + Praktikant:in, der täglich postet
- [ ] Koop: Rollenverteilung und Netzwerk-Synchronisation

## 9. Tech-/Tool-Stack (Referenz)

Siehe Konzept-Chat für Details zu KI-Tools (3D: Meshy/Luma/Kaedim, Art: Midjourney, Animation:
Mixamo/Cascadeur, Audio: eigene Produktion statt KI). Wird bei Bedarf in eigenes `TOOLS.md`
ausgelagert.
