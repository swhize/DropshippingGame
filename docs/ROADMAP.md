# Schlachtplan – Weg zum vollständigen Spiel

> Entstanden aus der Sprachnachricht vom 23.09.2026 ("richtig großes, vollständiges
> Spiel"). Am 25.09.2026 im großen Umbau (v1.0) fast komplett umgesetzt. Für Vision und
> Ton siehe `GDD.md`, hier steht, was gebaut ist und was als Nächstes kommt.

## Stand v1.0 (25.09.2026)

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
- Koop-Multiplayer (1–4 Spieler, ENet)
- Echte Grafik-Assets statt Grundformen (z.B. CC0-Pakete oder gekaufte Modelle) –
  braucht dein OK zum Herunterladen
- Mehr Produkte, weitere Standorte, Straßenverkauf als eigene Mechanik
- Pitch Day als Mini-Spiel statt Entscheidungsfenster
- Controller-Unterstützung, englische Übersetzung, Steam-Anbindung

## Werkzeuge
- `tests/compile_all.gd` – kompiliert alle Skripte
- `tests/run_tests.gd` – 121 Logik-Checks
- `tests/SelfTest.gd` – 49 Integrations-Checks in der echten Spielszene
  (`godot --headless --path . res://scenes/Main.tscn -- --selftest`)
- `tests/balance_sim.gd` – simuliert 25 Spieltage mit menschlichem Tempo
- `scripts/DebugHarness.gd` – Screenshots und Test-Szenarien per Kommandozeile
