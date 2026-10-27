# Changelog – Dropshipping Simulator

Alle nennenswerten Änderungen, neueste zuerst. Versionsnummer aus [`VERSION`](VERSION), im Spiel unten im
Hauptmenü und im Pausemenü (`BuildInfo`). Hustle Games.

## [1.0.0-rc1] – 27.10.2026 · Release Candidate

Erster Release Candidate der Vollversion. Inhaltlich fertig; vor 1.0 wird die Test-Checkliste
[`docs/KNOWN_ISSUES.md`](docs/KNOWN_ISSUES.md) im Editor und im Release-Build durchgespielt.

### Das steckt in 1.0
- **Vom Imbiss zum Imperium:** Intro in Kalles Imbiss, Kündigung, eigene Dropshipping-Marke aus der Garage,
  später Lagerhalle und Außenlager. Endziel 100.000 € Umsatz (6–9 Stunden), danach Endlos-Modus bis zum Börsengang.
- **Jedes Paket von Hand:** einkaufen, einräumen, kommissionieren, Karton falten, packen, etikettieren, verschicken.
  Bestellzettel mit Frist und Express, Retouren, Großaufträge auf Paletten, Förderband, Personal.
- **Handy und Tablet „HustleOS“:** AllesExpress, Mein Shop, iWolke Mail, PaketBlitz, Stadtblatt, TikTak, Fakebook,
  Gugel, TradingViech, Revoluut, Markt & Trends, Aufträge, Retouren, Firma.
- **Marketing:** TikToks selbst drehen (Ringlicht, Formate, Wertung), Trends & Hype mit Trendradar, Flyer,
  Influencer, Anzeigen, Fake-Profile und KI-Fake-Seiten (mit Risiko), Branding auf Karton, Van und Moped.
- **Eine ganze Stadt:** Wohn-, Einkaufs- (MediaMarkd, Fressnix mit Haustieren) und Finanzviertel (Bank, Börse),
  Verkehr, Linienbus, Müllabfuhr, Post, Passanten, Straßenfest, Verkaufsstand, Pitch Day, Emote-Rad.
- **Moped** mit Heckbox und Abgabe direkt an der Packstation.
- **Fortschritt:** Level 1–10, Skills, Perks, Einrichtung, Ausrüstung, Wochenziele, 30 Ereignisse, 40 Erfolge,
  Statistik, drei Schwierigkeitsgrade, Mama-Notkredit.
- **Finanzen:** Aktien, ETFs, Krypto, Sparkonto, Kredite – nur Spielgeld.
- **Look & Gefühl:** Tagesstimmungen, Wetter mit nassen Straßen, Feedback-Effekte für jede Kernhandlung.
- **Deutsch und Englisch** (live umschaltbar), volle Controller-Unterstützung, frei belegbare Tasten,
  Barrierefreiheit (UI-Größe, Untertitel, Farbenblind-Modus, weniger Bewegung), Streamer-Modus.
- **Technik:** 3 Spielstände mit Vorschaubild, Auto-Save, Sicherungsdatei mit Wiederherstellung, Fehlerbericht,
  Build-Menü (Release/Development/Demo), Steam-Schnittstelle für Erfolge, Lizenz-Sperre für Assets.

### Geändert in diesem RC (gegenüber 0.9.0)
- Version **1.0.0-rc1**; die Build-Werkzeuge übernehmen die Vorab-Kennung (macOS intern nur der Zahlenteil).
- Hauptmenü: Panel „Neu in Version 1.0 – Das steckt drin“ ersetzt den alten „Was ist neu“-Text.
- Version im Sitzungsprotokoll kommt jetzt wie in Menüs und Fehlerbericht aus `BuildInfo`.
- Aufgeräumt: alte, nicht mehr erreichbare Laptop-Seiten (alter Webshop, alte Marketing-, Einkaufs-, Bank-,
  Trading-, Übersichts-, Analytics- und Mail-Seiten), ungenutzte Hilfsfunktionen und rund 370 Zeilen ungenutzter
  USS-Regeln entfernt. Verhalten unverändert.
- Einstellungen: nie gelesener Altwert `ds.fullscreen` wird nicht mehr geschrieben (Fenstermodus regelt das).
- Dokumentation: Roadmap, GDD-Überblick, README, Release-Plan aktualisiert; neue Test-Checkliste `docs/KNOWN_ISSUES.md`.

### Bekannt
- Nur Einzelspieler; Koop folgt als großes Update. Steam-Integration vorbereitet, aber noch nicht aktiviert.
  Details: [`docs/KNOWN_ISSUES.md`](docs/KNOWN_ISSUES.md).

---

## English summary

### [1.0.0-rc1] – 2026-10-27 · Release Candidate
First release candidate of the full game, content-complete. Quit your diner job, build a dropshipping brand from
your garage up to a warehouse and an IPO – picking, packing, labelling and shipping every parcel by hand.
Phone and the "HustleOS" tablet with parody web apps, self-filmed TikToks, trends, ads, bulk contracts, returns,
a whole town with shopping and finance districts, a moped with a top box, street festival, pitch day, stocks,
ETFs and crypto (play money only), 30 events, 40 achievements, three difficulties, German and English, full
controller support, accessibility options and a streamer mode.

Changes in this RC: version 1.0.0-rc1, new "New in version 1.0" main-menu panel, version shown from `BuildInfo`
everywhere, removal of unreachable legacy laptop pages and unused styles (no behaviour change), updated docs and a
play-test checklist in `docs/KNOWN_ISSUES.md`. Single-player only for now; co-op is planned as a major update.
