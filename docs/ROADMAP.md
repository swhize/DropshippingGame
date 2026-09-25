# Schlachtplan – Weg zum vollständigen Spiel

> Entstanden aus der Sprachnachricht vom 23.09.2026 ("richtig großes, vollständiges
> Spiel"). Am 25.09.2026 im großen Umbau (v1.0) fast komplett umgesetzt und danach
> komplett nach **Unity 6** portiert und erweitert (v2.0). Für Vision und Ton siehe
> `GDD.md`, hier steht, was gebaut ist und was als Nächstes kommt.

## Stand v2.0 – Unity-Umbau (25.09.2026)

Auftrag: "bau alles auf Unity um, sieht noch unprofessionell aus, inspirier dich an
Unity-Studios, BIG REWORK, bau es weiter aus" + 3 HTML-Entwürfe fürs Laptop mit Darkmode.

Vorbilder für Look & Bedienung: *Supermarket Simulator*, *TCG Card Shop Simulator*,
*Schedule I*, *PowerWash Simulator* (klare Ego-Interaktion mit Fadenkreuz-Pille, Outline
am anvisierten Objekt, ruhige HUD-Karten, ein "Betriebssystem" als zentrales Menü).

- [x] **Engine-Wechsel:** Godot 4.7/GDScript → Unity 6 LTS, C#, URP, UI Toolkit, Input System
- [x] **Spiellogik als reines C#** (`Scripts/Core`, ohne Unity-Abhängigkeit) + 38 NUnit-Tests
  (laufen mit `dotnet test` und im Unity Test Runner), Balance-Test über 25 Tage
- [x] **Automatische Projekteinrichtung** (`ProjectSetup.cs`): URP-Asset + Renderer mit SSAO,
  Linear-Farbraum, Vorlage-Materialien für Builds, Spielszene, Build-Einstellungen
- [x] **Grafik:** URP mit HDR, Bloom, ACES-Tonemapping, Vignette, SSAO, weiche Schatten,
  Reflection Probe, prozedurale Texturen mit Normal Maps, abgerundete Kanten (Bevel-Meshes),
  eigene Shader für Weltschrift und Hervorhebungs-Outline
- [x] **Neues UI (UI Toolkit):** HUD-Karten, Fadenkreuz mit Aktions-Pille, Toasts, Banner,
  Dialoge mit Schreibmaschinen-Effekt, Fenster, Pausemenü, Hauptmenü mit Kamerafahrt
- [x] **HustleOS in zwei Designs** ("Frachtbrief" und "Hype", je hell/dunkel) – umschaltbar
  in den Einstellungen; HTML-Entwürfe (3 Varianten) unter `docs/laptop-designs/`
- [x] **Neue Apps:** Übersicht (Aufgabenliste, Ziel, Kennzahlen), Bank (Kredite, Tilgung, Zinsen)
- [x] **Neue Mechaniken:** Verkaufsstand vor der Garage (Passanten kaufen direkt),
  Pitch Day als Minispiel (3 Juroren, echte Kennzahlen), 10 Produkte statt 6
- [x] **3 Spielstände** mit Vorschau, Überschreiben/Löschen, Auto-Save alle 90 s
- [x] **Controller-Unterstützung** (Input System), Tastenhinweise wechseln automatisch
- [x] Hilfe (F1), Screenshots (F12), Credits

Annahmen (beim Review zu bestätigen):
1. Im Spiel stecken die Entwürfe **A "Frachtbrief"** und **B "Hype"**; Entwurf **C
   "Schreibtisch"** gibt es nur als HTML. Sobald ein Favorit feststeht, wird er Standard.
2. Unity-Version: Unity 6 LTS (6000.x). Neuere 6.x-Versionen sollten ohne Änderungen laufen.

## Stand v1.0 – Godot (25.09.2026, inzwischen nach Unity portiert)

Getroffene Annahmen (vom Nutzer beim Review zu bestätigen oder zu korrigieren):

1. **Gastro-Job = kurzes, einmaliges Intro** (drei Teller austragen, Kündigung), danach
   bleibt Kalle als Nachbar mit Sprüchen und gelegentlichen Anrufen (Event).
2. **Eine einzige kleine, zusammenhängende Karte** ohne Ladebildschirme: Straße mit Kalles
   Imbiss, deiner Garage, der Lagerhalle, Park und Skyline.

### Phase A – Fundament ✅
- [x] Speichern/Laden (JSON, alle Systeme inkl. abgelegter Gegenstände, Spielerposition,
  gehaltenem Gegenstand, Markt, Postfach), Auto-Save alle 90 s, jeden Morgen und beim Schließen
- [x] Hauptmenü mit Kamerafahrt, Fortsetzen (mit Spielstand-Vorschau), Neues Spiel (3 Modi),
  Einstellungen, Credits
- [x] Pausemenü (Esc) mit Speichern, Einstellungen, Hilfe, Hauptmenü, Beenden
- [x] Audio: 19 Soundeffekte + 3 Musikstücke, komplett prozedural synthetisiert, gecached,
  Musik gedämpft im Pausemenü
- [x] Tutorial: 10 Schritte, zustandsbasiert, danach Ziel-Anzeige
- [x] Einstellungen: Lautstärken, Maus, Sichtfeld, Vollbild, Kopfwippen

### Phase B – Intro & Rahmenhandlung ✅
- [x] Kalles Imbiss: Schicht, drei Teller an die richtigen Tische, Dialog mit Entscheidung
- [x] Kündigung → Schwarzblende "Am nächsten Morgen ..." → Garage, 150 € Startkapital

### Phase C – Progression ✅
- [x] Firmenlevel 1–10 (XP aus Verkäufen) mit Freischaltungen (Produkte, Anbieter, Apps, Upgrades)
- [x] Garage → Lagerhalle (4.500 €, Level 4): Tor öffnet sich, 6 Hochregale, Personal möglich
- [x] Upgrades: Shop-Server, eigener Lieferwagen, Förderband (Pakete fahren sichtbar), Hochregal
- [x] 12 Ziele mit Geldbelohnung, Endziel "Imperium" (100.000 € Umsatz) mit Abschluss-Fenster

### Phase D – Ereignisse ✅
- [x] 20 Ereignisse (Black Friday, viral, Zoll, Transportschaden, Shitstorm, Krypto-Bro,
  Influencer, Pitch Day, Fake-Reviews, Preiskampf, Finanzamt, Oma, Kalle ruft an,
  Straßenfest, Wasserschaden, Krypto-Crash ...) mit Entscheidungen, Postfach, Popups

### Phase E – Personal ✅
- [x] Lagerist:in, Packer:in, Versandkraft, Social-Media-Praktikant:in – arbeiten automatisch,
  laufen sichtbar als Figuren in Warnwesten durch die Halle, Löhne jeden Abend

### Phase F – Konkurrenz & Markt ✅
- [x] 3 Konkurrenten mit täglich schwankenden Preisen, Marktpreis bestimmt die Nachfrage mit
- [x] Bewertungssystem (Sterne aus Tempo + Qualität), Rezensionen im Webshop

### Phase G – PC-OS & Grafik ✅
- [x] "HustleOS": Seitenleiste mit 12 Apps, Badges, Uhr, Kontostand, Diagramme, TikTok-Handy
- [x] Komplette Stadt-Kulisse mit prozeduralen Shadern (Ziegel, Beton, Asphalt, Fliesen,
  Holz, Blech, Fassaden mit nachts beleuchteten Fenstern), Tageslicht-Zyklus, Nebel, Bloom
- [x] NPCs (Kalle, Gäste, Passanten, Personal), Autoverkehr, Lieferwagen, Laternen

### Phase H – Trading ✅
- [x] DROPCOIN, GameShop-Aktie, Welt-ETF mit Live-Kursen, Gebühren, Portfolio

### Phase I – Koop/Multiplayer ⏳ offen
- [ ] Größter offener Brocken. Architektur ist vorbereitet (zentrale Autoloads,
  Stationen interagieren über einen Spieler-Parameter), aber Netzwerk fehlt komplett.

### Phase J – Win/Lose ✅
- [x] Insolvenz unter −500 € mit "Letzten Spielstand laden", Endziel mit Abschluss-Fenster

## Nächste Ideen (nach Review)
- Koop-Multiplayer (1–4 Spieler, z.B. Netcode for GameObjects)
- Echte Grafik-Assets statt Grundformen (z.B. CC0-Pakete oder Asset Store) –
  braucht dein OK zum Herunterladen
- Weitere Standorte, Kunden, die in den Laden kommen, Lieferanten-Verhandlungen
- Englische Übersetzung, Steam-Anbindung (Erfolge, Cloud-Saves)

## Werkzeuge
- `Assets/DropshippingGame/Tests/EditMode/CoreTests.cs` – 37 Logik-Tests
- `Assets/DropshippingGame/Tests/EditMode/BalanceTests.cs` – simuliert 25 Spieltage mit
  menschlichem Tempo (Lagerhalle ca. Tag 12, keine Pleite)
- In Unity: **Window → General → Test Runner → EditMode → Run All**
- Ohne Unity: kleines `dotnet`-Testprojekt, das `Scripts/Core` und die Tests einbindet
  (NUnit 3), `dotnet test`
- Menü **Dropshipping** im Editor: Projekt einrichten, Spiel starten, Spielstände-Ordner
