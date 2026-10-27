# Dropshipping Simulator – Vom Imbiss zum Imperium

First-Person-Wirtschaftssimulation mit Satire von **Hustle Games**: Du kellnerst in Kalles Imbiss, kündigst und baust
aus der Garage heraus eine Dropshipping-Marke auf – Ware einkaufen, einräumen, packen, etikettieren, verschicken – und
steuerst über Handy und das Tablet „HustleOS“ Webshop, Marketing, Bank, Börse, Personal und Ausbau.
Ziel: 100.000 € Umsatz – und weiter, so lange du willst.

Unity 6.3 (6000.3.25f1) · URP · UI Toolkit · Input System · C# 9 · Zielplattform Steam (Windows zuerst).
Version: **1.0.0-rc1** (Release Candidate, Datei [`VERSION`](VERSION)) · Änderungen: [`CHANGELOG.md`](CHANGELOG.md) ·
Test-Checkliste: [`docs/KNOWN_ISSUES.md`](docs/KNOWN_ISSUES.md) · Fahrplan: [`docs/ROADMAP.md`](docs/ROADMAP.md) ·
Release-Planung: [`docs/RELEASE.md`](docs/RELEASE.md).

## Features (1.0)

- **Jedes Paket von Hand:** einkaufen, einräumen, kommissionieren, Karton falten, packen, etikettieren, verschicken –
  mit Bestellzetteln, Express, Retouren und Großaufträgen auf Paletten.
- **14 Produkte**, 3 Lieferanten-Stufen, Kartongrößen, Förderband, Personal, Lagerhalle und Außenlager.
- **Handy & Tablet „HustleOS“:** AllesExpress, Mein Shop, iWolke Mail, PaketBlitz, Stadtblatt, TikTak, Fakebook, Gugel,
  TradingViech, Revoluut, Markt & Trends, Aufträge, Retouren, Firma.
- **Marketing:** TikToks selbst drehen (Ringlicht, Formate, Wertung), Trends & Hype, Anzeigen, Influencer,
  Fake-Profile und KI-Fake-Seiten, eigenes Branding auf Karton, Van und Moped.
- **Eine ganze Stadt:** Wohn-, Einkaufs- und Finanzviertel, Verkehr, Post, Passanten, Haustiere, Straßenfest,
  Verkaufsstand, Pitch Day, Moped mit Heckbox, Emote-Rad, Möbel verschieben.
- **Fortschritt:** Level 1–10, Skills, Perks, Wochenziele, 30 Ereignisse, 40 Erfolge, Endlos-Modus bis zum Börsengang,
  drei Schwierigkeitsgrade.
- **Finanzen:** Aktien, ETFs, Krypto, Sparkonto, Kredite – nur Spielgeld.
- **Deutsch & Englisch** (live umschaltbar), volle Controller-Unterstützung, frei belegbare Tasten,
  Barrierefreiheit, Streamer-Modus, 3 Spielstände mit Auto-Save und Sicherungsdatei.

## Projekt öffnen

1. Unity Hub installieren, dort **Unity 6000.3.25f1** installieren (für Builds zusätzlich die Module
   „Windows Build Support (IL2CPP)“, optional Linux/Mac).
2. Repo klonen (Branch `claude/busy-knuth-yehyvb`) und im Hub **Add project from disk** → den Ordner mit
   `Assets`, `Packages`, `ProjectSettings` wählen.
3. Beim ersten Öffnen richtet sich das Projekt selbst ein (Grafik-Pipeline, Szene, Player-Einstellungen, Icon).
   Fragt Unity nach dem **neuen Input System** → **Yes** (Neustart ist normal).
4. **▶ Play** drücken. Hakt etwas: Menü **Dropshipping → Projekt einrichten** bzw. **→ Spiel starten**.

Spielstände: Menü **Dropshipping → Spielstände-Ordner öffnen** (im Spiel: `%USERPROFILE%\AppData\LocalLow\Hustle Games\Dropshipping Simulator`).

## Steuerung

| Aktion | Tastatur & Maus | Controller |
|---|---|---|
| Laufen / Umsehen | WASD / Maus | linker / rechter Stick |
| Sprinten / Ducken / Springen | Shift / Strg (C) / Leertaste | L3 / R3 / A |
| Benutzen (auch Tablet am Schreibtisch, Moped) / Ablegen | E / G | X / B |
| Möbel verschieben · drehen · abstellen | B · R / Mausrad · Linksklick | Steuerkreuz ↓ · LB / RB · X |
| Handy | Tab | Y |
| Emote-Rad | T | Steuerkreuz ← |
| Moped: Hupe · Ansicht | H · V | Steuerkreuz ↑ · → |
| Pause · Hilfe · Screenshot | Esc · F1 · F12 | Start · Select · – |

Nur im Editor und in Development-Builds: **F10** (oder F9 / ^) Admin-Panel, **Alt** Noclip. In Release-Builds
sind diese Tasten ohne Funktion.

## Bauen

Menü **Dropshipping → Build**:

| Eintrag | Ergebnis |
|---|---|
| Windows (Release) | `Builds/Windows-Release/` – für Steam, IL2CPP, ohne Entwickler-Werkzeuge |
| Windows (Development) | Development-Build mit Admin-Panel/Noclip (Define `DS_DEV`), Mono |
| Windows (Demo) | Demo fürs Steam Next Fest (Define `DS_DEMO`, 5 Spieltage) |
| Linux / macOS | Mono-Builds (macOS am besten auf einem Mac bauen und signieren) |
| Steam-Integration an/aus | setzt das Define `DS_STEAM` (braucht Steamworks.NET, siehe RELEASE.md) |

Kommandozeile (z. B. für CI):

```
Unity.exe -batchmode -nographics -projectPath . -executeMethod DropshippingGame.EditorTools.BuildTools.BuildFromCommandLine ^
  -dsTarget windows -dsConfig release -dsOut Builds/Steam/Windows -logFile build.log
```

Optionen: `-dsTarget windows|linux|macos`, `-dsConfig release|development|demo`, `-dsBackend il2cpp|mono`,
`-dsBuildNumber <n>`. Die Version kommt aus `VERSION` plus Build-Nummer (Anzahl Git-Commits), z. B. `1.0.0-rc1.412`
(Vorab-Kennungen wie `-rc1` sind erlaubt; macOS bekommt intern nur den Zahlenteil).
Eine Zusammenfassung (Größe, größte Assets, Fehler) steht in der Konsole und in `Builds/<Ziel>-<Variante>_report.txt`.

## Testen

- **Unity:** Window → General → Test Runner → EditMode.
- **Ohne Unity (Linux/CI):** `tools/compilecheck/setup.sh` (einmalig), dann
  `tools/compilecheck/build.sh .` (muss `CORE OK`, `RUNTIME OK`, `EDITOR OK` zeigen) und
  `dotnet test tools/compilecheck/Tests.csproj`.

## Ordner

```
Assets/DropshippingGame/
  Scripts/Core/      Spiellogik in reinem C# (ohne UnityEngine), NUnit-getestet
  Scripts/Runtime/   Unity-Schicht: World/, Player/, Rendering/, Audio/, UI/ (Laptop, Handy, HUD), Platform/ (Steam)
  Scripts/Editor/    Projekt-Einrichtung, Build-Werkzeuge, Branding, Import-Regeln
  Resources/         Modelle, Texturen, Audio, Schriften, USS, Shader, credits.json
  Branding/          App-Icon und Splash-Logo (erzeugt mit tools/branding/make_branding.py)
  ThirdParty/        Lizenztexte der Fremd-Assets
  Tests/EditMode/    NUnit-Tests der Spiellogik
docs/                GDD, Roadmap, Release-Checkliste, Asset- und API-Doku, Agenten-Regeln
tools/               Compile-Check ohne Unity, Branding-Generator
```

## Rechte

Spielcode, Texte und eigene Grafiken: © Hustle Games, alle Rechte vorbehalten. Fremd-Assets (CC0, CC BY 3.0,
SIL OFL 1.1) mit Quellen und Lizenzen: [`CREDITS.md`](CREDITS.md) und im Spiel unter **Credits**.
Das Spiel sammelt keine Telemetrie und sendet keine Daten.
