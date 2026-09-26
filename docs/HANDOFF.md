# Übergabe: Dropshipping Simulator – Stand v3.0 (26.09.2026)

> Diese Datei als erste Nachricht in einen neuen Chat geben. Sie enthält alles Wissen aus der
> bisherigen Arbeit.

## Wer ich bin / wie wir arbeiten
- Ich (Besitzer) bin Deutsch, **kann nicht programmieren**. Du schreibst allen Code, testest ihn und
  sagst mir nur in einfachen Worten, was ich tun soll (meist: in Unity ▶ Play drücken).
- Antworten bitte auf Deutsch, kurz und verständlich. Ich teste in Unity und schicke Screenshots
  oder Console-Fehler.
- Große Aufgaben gerne auf mehrere Agenten verteilen (klappt gut), danach zusammenführen und prüfen.

## Projekt
- GitHub: `swhize/DropshippingGame`, Arbeits-Branch **`claude/busy-knuth-yehyvb`**
  (PR #1 → `main`; `main` enthält noch die alte Godot-Version). Nie direkt auf `main` pushen.
- Commits enden mit den Attributions-Zeilen, die die Session vorgibt.
- Engine: **Unity 6.3 LTS (6000.3.25f1)**, URP 17.3, UI Toolkit, Input System 1.20, C#.
  Ich habe genau diese Version installiert (Projekt liegt bei mir auf Laufwerk D:, da C: voll ist).
- Früher Godot 4.7 (v1.0) → v2.0 Unity-Port → **v3.0 „Das große Update“** (aktuell, noch nicht in
  Unity getestet).

## Das Spiel
First-Person-Wirtschaftssim, Ton: lockere Satire auf Hustle-Kultur. Start als Aushilfe in Kalles
Imbiss → kündigen → Dropshipping-Marke aus der Garage, später Lagerhalle. Kreislauf: Einkauf am Laptop
→ Lieferwagen bringt Kisten → ins Regal → im Webshop listen → Bestellungen → Kommissionieren →
Packen → Label → Versand. Level 1–10, Personal, Förderband, Marketing/TikTok, Branding, Trading,
Bank/Kredite, Verkaufsstand, 30 Ereignisse, Pitch-Day-Minispiel, Ziele, Insolvenz, Endziel 100.000 €.

**Neu in v3.0:** Bestellzettel mit Express, Retouren (Retourenplatz), Trends/Hype + Trendradar,
B2B-Großaufträge (Palettenplatz, Spedition), Skillbaum (3 Äste × 4), Wochenziele, 10 neue Events,
Balance-Fixes (kein Kredit+ETF-Trick, Premium mit Abwägung, Tageskampagnen, max 3 TikToks/Tag).
UI: Handy auf `Tab` (Laptop „HustleOS“ nur am Schreibtisch), neues HUD mit Tickets oben rechts,
Laptop mit Tabs (Design „Hype“ dunkel), Assistent „Warum läuft's nicht?“, Kassenbon, Zielpfeil,
Bestell-Monitor, Zonenschilder, Pakete stapeln. Welt: echte CC0-Assets (Kenney, KayKit, Poly Haven,
ambientCG), PBR-Materialien, animierte Figuren, echte Sounds/Musik, Marke auf Van & Plakat.

## Code-Aufbau (`Assets/DropshippingGame/`)
- `Scripts/Core` – reine Spiellogik ohne UnityEngine (Sim*.cs, GameData, Market, Trends, Events …).
- `Scripts/Runtime` – Unity-Schicht: `GameRoot` (startet alles automatisch), World/, Player/,
  Rendering/ (inkl. `AssetLib` für Modelle/Sounds/Schriften), Audio/, UI/ (HUD, Phone, Laptop-Apps).
- `Scripts/Editor/ProjectSetup.cs` – richtet URP/Szene beim ersten Öffnen automatisch ein
  (Menü „Dropshipping“), `AssetImportRules.cs` – Import-Einstellungen.
- `Resources/` – USS, Schriften, Modelle (FBX), Texturen, Audio, `asset_manifest.json`, `credits.json`.
- `Tests/EditMode` – 110 NUnit-Tests.
- Doku: `docs/REWORK_PLAN.md`, `docs/CORE_API.md`, `docs/ASSETS.md`, `docs/GAME_IDEAS.md`
  (75 Ideen, Top 12), `docs/GDD.md`, `docs/ROADMAP.md`, `CREDITS.md` (alle Asset-Quellen/Lizenzen).

## Regeln
- C# 9, keine `record`/`init`, keine veralteten Unity-APIs, mehrdeutige Namen voll qualifizieren
  (z. B. `UnityEngine.ShadowQuality` – daran ist Unity schon einmal gescheitert).
- Core bleibt ohne UnityEngine; neue Regeln mit Tests absichern.
- Immer prozeduraler Fallback, wenn ein Asset fehlt. Defensiv coden – niemand kann hier Unity starten.
- Assets nur CC0 / CC-BY / OFL, Quellen in `CREDITS.md` + `credits.json` eintragen.

## Prüfen ohne Unity
- In der alten Cloud-Umgebung gab es ein Prüfskript gegen die echten Unity-6.3-DLLs
  (`scratchpad/unity63/check.sh <repo> --test`, gebaut aus den GameCI-Docker-Images
  `unityci/editor:ubuntu-6000.3.25f1-…`, weil Unity-Server blockiert waren). In einem neuen Chat
  ist das weg – bei Bedarf neu aufbauen (Anleitung: Docker-Hub-Layer streamen, nur
  `Editor/Data/Managed`, NetStandard, BuiltInPackages extrahieren; pro asmdef kompilieren).
- Core-Tests: kleines net8-Projekt mit NUnit 3, das `Scripts/Core` + `Tests/EditMode` einbindet → `dotnet test`.

## Offene Punkte / nächste Schritte
1. **Erster echter Test in Unity** durch mich – erwartbar sind Korrekturen an Ausrichtung/Größe
   von Modellen, Figuren-Animationen („sit“-Clip unsicher), Layout von Handy/HUD/Skillbaum.
2. Laut `docs/GAME_IDEAS.md` für v3.1: feste Abholzeiten, Bestellungen mit mehreren Artikeln,
   Kalles Geschichte in 3 Akten, Dropshipping-Modus pro Produkt, Kundenservice-Chats,
   Sackkarre/Rollwagen, Ereignis-Ketten.
3. Offene Fragen an mich: Koop-Modus (Empfehlung: später), Trading behalten (ja, als Nebenspiel),
   Englische Version (vor Steam).
4. Asset-Lizenzen vor einer Veröffentlichung auf den Originalseiten stichprobenartig prüfen
   (Downloads kamen von GitHub-Spiegeln).
5. PR #1 mergen, sobald das Spiel in Unity läuft.
