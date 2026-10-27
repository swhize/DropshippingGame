# Roadmap – Dropshipping Simulator

Stand: 27.10.2026 · Version **1.0.0-rc1** (Release Candidate) · Hustle Games

Dieses Dokument zeigt, **was in 1.0 steckt** und **was nach dem Release kommt**. Änderungen pro Version:
[`CHANGELOG.md`](../CHANGELOG.md). Test vor dem Release: [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md). Steam-Fahrplan,
Store-Texte und Recht: [`RELEASE.md`](RELEASE.md). Design und Zahlen: [`GDD.md`](GDD.md).

Bisheriger Weg: Godot-Prototyp → Unity-6-Port → „Das große Update“ → Inhalts- und Polier-Wellen (Stadt, Läden,
Finanzviertel, Marketing, Moped, Englisch, Release-Technik) → **1.0 Release Candidate**.

---

## 1. Was in 1.0 steckt

### Kern: jedes Paket von Hand
- Intro als Aushilfe in **Kalles Imbiss** (Teller an die richtigen Tische, Kündigung), danach die eigene Marke in der Garage.
- Physische Kette: Einkauf am Tablet → Lieferwagen/Abholbox → Wareneingang → Regal → Kommissionieren → Karton falten →
  Packtisch → Labeldrucker → Packstation/Post. Förderband, Palettenplatz, Retourenplatz, Bestell-Monitor, Zonenschilder.
- **14 Produkte** mit eigenen 3D-Modellen, 3 Lieferanten-Stufen, Kartongrößen S/M/L, Verpackungsmüll, defekte Chargen.
- Bestellzettel mit Kundschaft, Frist und **Express**; **Retouren** (B-Ware oder Entsorgung); **Großaufträge (B2B)** auf Paletten.
- Tag von 8 bis 20 Uhr, Kassenbon-Abrechnung, Wochentage (samstags mehr Bestellungen), Energie und Feierabend.

### Business über Handy und Tablet
- **Handy** (Tab): Bestellzettel, Nachrichten mit Entscheidungen, Wochenziele, Status mit Engpass-Assistent, Wallet, Fakebook/Gugel.
- **Tablet „HustleOS“** am Schreibtisch mit Dock und Startseite. Web-Parodien: AllesExpress (Einkauf), Mein Shop
  (shopifly-Admin), iWolke Mail, PaketBlitz, Stadtblatt, TikTak, Fakebook, Gugel, TradingViech, Revoluut, dazu
  Markt & Trends, Aufträge, Retouren und Firma (Team, Skills, Perks, Ausbau, Einrichtung, Logistik, Erfolge,
  Statistik, Ziele, Branding, Verpackung, Lifestyle).
- **Marketing:** Flyer, Influencer, Fakebook- und Gugel-Anzeigen, Fake-Profile und KI-Fake-Seiten (mit Risiko),
  **Trends & Hype** mit Trendradar, **TikToks selbst drehen** (Ringlicht, Formate, Wertung aus Licht/Hook/Bildaufbau).
- **Branding:** Markenname, Logo, Farbe – auf Kartons, Van, Moped und Plakatwand.

### Eine ganze Stadt
- Garage, Lagerhalle, Außenlager; Wohnviertel, **Einkaufsviertel** (MediaMarkd, Fressnix mit Haustieren) und
  **Finanzviertel** (Bank, Börse mit Maklern, Nachrichten-Laufband).
- Verkehr mit Linienbus, Müllabfuhr, PaketBlitz-Post, Passanten (mit Untertiteln), Tunnelportale, Emote-Rad.
- **Moped** mit Arcade-Physik, Ego-/Verfolgerkamera, Hupe, Heckbox (4 Pakete oder 1 Kiste) und Abgabe direkt aus der Box.
- **Straßenfest/Flohmarkt** mit eigenem Stand, Verkaufsstand vor der Garage, **Pitch Day** vor drei Juroren.
- Möbel verschieben (Raster, Drehen, gespeichert), Haustier-Ecke, Einrichtung/Ausrüstung.

### Fortschritt & Endspiel
- Firmenlevel 1–10, Skillbaum (3 Äste × 4 Stufen), Hustle-Perks, Personal (eine Kraft je Rolle, ab Level 10 zwei).
- Ziele, Wochenziele, **30 Ereignisse** mit Entscheidungen, Kalle als Nachbar.
- Endziel 100.000 € Umsatz (6–9 h) → Endlos-Modus: Viertelmillion, Außenlager, Halbe Million, **Börsengang** bei 1 Mio., danach verdoppelnde Ziele.
- **Börse, ETFs, Krypto**, Sparkonto, Kredite, Schufi-Score – nur Spielgeld.
- Schwierigkeit Entspannt/Normal/Hart, Mama-Notkredit vor der Pleite, Insolvenz mit „Letzten Spielstand laden“.
- **40 Erfolge** (Steam-API-Namen `ACH_…`) und Gesamtstatistik.

### Präsentation & Technik
- Unity 6.3, URP mit Tagesstimmungen, Farbkorrektur, Wetter (sonnig/bewölkt/Regen, nasse Straßen), CC0-Modelle mit prozeduralen Rückfällen.
- Spielgefühl-System (Packtisch-Sequenz, Münzen, Konfetti, schwebende Zahlen, FOV-Kick, Synth-Sounds).
- **Deutsch und Englisch**, live umschaltbar. Komplett mit Controller spielbar, Tasten frei belegbar.
- Einstellungen: Grafik (Fenstermodus, Auflösung, VSync, FPS-Limit, Qualität, Renderskala), Audio, Steuerung,
  Barrierefreiheit (UI-Größe, Untertitel, Textgeschwindigkeit, Farbenblind-Modus, weniger Bewegung), Streamer-Modus.
- 3 Spielstände mit Vorschaubild, Auto-Save, Sicherung (`.bak`) mit Wiederherstellung, Sitzungsprotokoll, Fehlerbericht.
- Build-Menü (Windows Release/Development/Demo, Linux, macOS), Lizenz-Sperre, Demo-Modus (5 Tage), Steam-Schnittstelle.
- Über 320 NUnit-Tests inkl. Balance-Bot, Chaos-Tests und Lokalisierungs-Vollständigkeit.

### Bis 1.0 final noch zu tun
- [ ] Test-Checkliste [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md) im Editor und im Release-Build durchspielen, Funde beheben → `1.0.0-rc2` bzw. `1.0.0`.
- [ ] Steamworks: App-ID, Steam-Integration (`DS_STEAM`), Erfolge und Steam Cloud im Backend ([`RELEASE.md`](RELEASE.md)).
- [ ] Store-Seite, Kapseln, Trailer; Lizenz-Stichproben der Assets.

---

## 2. Nach dem Release

Reihenfolge nach Wirkung; Zeitangaben sind Ziele, keine Versprechen.

### 2.1 Woche 1–4: Pflege
- Hotfixes aus Fehlerberichten (Pausemenü › Feedback & Bug melden) und Steam-Reviews, Balance-Feinschliff.
- Komfort: Bildschirmtastatur auf dem Steam Deck („Verified“), PlayStation-Symbole bei DualSense, weitere Tastenhinweise.

### 2.2 Monat 2–3: Inhalts-Update „Tagesbogen“
- Feste Abholzeiten, Bestellungen mit mehreren Artikeln und Kartonwahl, Kalles Geschichte in drei Akten.
- Neue Produkte, Ereignis-Ketten, weitere Erfolge, Kundenservice-Chats auf dem Handy.
- Ideen-Pool: [`GAME_IDEAS.md`](GAME_IDEAS.md).

### 2.3 Großes Koop-Update (1–4 Spieler)
- Größter Wachstumstreiber im Genre. Der Spielzustand liegt schon zentral in `Sim` (reines C#), Stationen arbeiten
  über einen Spieler-Parameter.
- Plan: Host-autoritativ mit Netcode for GameObjects + Steam-Transport (Lobby, Einladen über Steam), Rollen frei
  wechselbar (Einkauf, Packen, Versand, Marketing), Personal füllt fehlende Hände, Spielstand gehört dem Host.
- Eigenes Marketing-Event (Trailer, Rabatt, Next-Fest-artige Demo-Aktion).

### 2.4 DLC-Idee: Krypto-Mining
- Mining-Rigs in Garage/Lagerhalle: Strom, Hitze und Lärm als neue Ressourcen, Lüfter/Kühlung als Ausbau.
- DROPCOIN-Schwankungen, „Mining-Pool“ mit Kalle, Satire auf Krypto-Bros; hängt an den vorhandenen Börsen-/Krypto-Systemen.
- Nur Spielgeld, keine Echtgeld- oder Blockchain-Anbindung.

### 2.5 Update-Idee: Casino
- Ein Casino im Finanzviertel als satirischer Abstecher (Spielautomat, Roulette, „Business-Poker“ mit NPC-Hustlern).
- Nur Spielgeld, kein Kauf von Spielwährung, keine Lootboxen – Altersfreigabe vorher prüfen (simuliertes Glücksspiel).

### 2.6 Mehr Stadt
- Neue Viertel: Hafen/Industriegebiet (Containerware, Zoll), Uni-Viertel (Studenten-Kundschaft), Altstadt mit Markt.
- Der vorbereitete **Klamottenladen** (verschlossene DLC-Tür im Einkaufsviertel) als kosmetisches Paket:
  Outfits, Caps, Merch der eigenen Marke – rein kosmetisch.
- Weitere Läden und Lieferdienste, Moped-Varianten, Lieferfahrten zu Großkunden.

### 2.7 Später
- Steam Workshop / Mods (Produkte, Ereignisse, Texte), weitere Sprachen, Soundtrack als eigenes Produkt.
