# v3.0 „Das große Update“ – Arbeitsplan

> Auftrag des Besitzers (25.09.2026): „Nimm Assets aus dem Netz mit freien Lizenzen – ich brauche
> auf jeden Fall die Quellen. Man kann an der Idee einiges ändern, entfernen, hinzufügen, Layouts
> anpassen. Tob dich aus und mach alles fertig. BIG REWORK – ich will ein massives Update sehen!“
>
> Dieses Dokument ist die gemeinsame Grundlage für alle Agenten, die parallel am Update arbeiten.
> Wer davon abweicht, notiert die Abweichung und den Grund im eigenen Abschlussbericht.

## 1. Ziel und Leitlinien

**Ziel:** Aus dem funktionierenden, aber grafisch rohen Prototyp wird ein Spiel, das man stolz
zeigen kann: echte 3D-Modelle, Materialien, Sounds, Musik und Schriften aus freien Quellen, eine
klar strukturierte Oberfläche, mehr spielerische Tiefe und bessere Führung für neue Spieler.

**Vorbilder:** *Supermarket Simulator*, *TCG Card Shop Simulator*, *Schedule I*, *PowerWash Simulator*,
*Overcooked* (Bestellzettel). Gemütlich-lebendige Welt, klare Ego-Interaktion, ein Handy/PC als
zentrales Werkzeug, befriedigendes Feedback (Geräusche, Zahlen, kleine Animationen).

**Art Direction:** „Cozy stylized“ – leicht stilisierte Low-Poly-Modelle (Kenney/Quaternius-Stil),
dazu hochwertige PBR-Materialien für Böden/Wände/Straße, warmes Licht, weiche Schatten, SSAO,
Bloom. Keine Stil-Mischung aus fotorealistischen Einzelobjekten und Klötzchen: pro Kategorie ein
einheitlicher Stil.

**Ton:** Deutsch, locker, satirisch über Hustle-Kultur („Mit 19 Millionär – dank Dropshipping“),
aber nie gemein. Alle Texte im Spiel auf Deutsch.

**Besitzer kann nicht programmieren:** Alles muss nach dem Öffnen in Unity automatisch laufen
(Projekt-Setup per Editor-Skript, keine manuellen Schritte, keine Szenen-Bastelei).

## 2. Neue Spielmechaniken (Core, reines C#, mit Tests)

Alle Regeln leben in `Scripts/Core` (ohne UnityEngine) und werden mit NUnit-Tests abgesichert.
Bestehende öffentliche API bleibt erhalten (nur erweitern, nicht umbenennen). Spielstand-Version
wird erhöht; alte Spielstände (v3) müssen weiter laden (fehlende Felder = sinnvolle Standardwerte).

**F1 Bestellzettel.** Jede Bestellung bekommt ID, Kundin/Kunde (Name + Ort), Notiz (Flavor),
Fälligkeit und optional **Express** (ab Level 2, ca. 15–25 %, kürzere Frist, +40 % Preis,
mehr Einfluss auf die Bewertung). Die Oberfläche zeigt Bestellungen als Zettel mit Countdown.

**F2 Retouren.** Ein Teil verschickter Pakete kommt zurück (Basis ca. 3 %, mehr bei Billig-Ware,
weniger bei Premium und mit Skill). Die Erstattung wird beim Eintreffen abgebucht. Retouren liegen
als eigener Gegenstand (neue `ItemKind`) am Wareneingang; am **Retourenplatz** entscheidet man:
als B-Ware zurück ins Lager (Qualität sinkt) oder entsorgen. Statistik pro Tag.

**F3 Trends/Hype.** Jedes Produkt hat einen Hype-Wert (ca. 0,5–2,0) mit Zyklen (steigend, Peak,
fallend, tot) über mehrere Tage; wirkt auf die Nachfrage. Vorhersage mit Unsicherheit
(„Trendradar“), Ereignisse können Hype auslösen (viraler TikTok-Trend usw.).

**F4 Großaufträge (B2B).** Ab Level 3 täglich 1–3 Angebote von Firmen (lustige deutsche Namen):
Produkt, Menge, Frist in Tagen, Vergütung, Bewertungseffekt. Angenommene Aufträge werden am
**Palettenplatz** erfüllt: ganze Kisten (ohne Auspacken!) oder Einzelartikel abgeben. Erfüllt →
Geld, XP, Ruf; Frist verpasst → Vertragsstrafe und Rufverlust.

**F5 Hustle-Skills.** Skillpunkte (1 pro Level-Aufstieg, 1 zum Start). Skillbaum mit drei Ästen
(Logistik, Vertrieb, Marketing) à 4 Stufen, z. B. Tragekraft/Tempo beim Tragen, mehr Lagerplatz,
kürzere Lieferzeit, bessere Einkaufspreise, höhere B2B-Vergütung, stärkere TikToks, günstigere
Werbung, genauere Trendprognose, weniger Retouren. Effekte werden in den Core-Formeln abgefragt
(Welt-Effekte wie Lauftempo liest die Welt über `Sim`).

**F6 Wochenziele.** Tag 1 ist Montag, Wochentage im HUD. Jeden Montag 3 Wochenziele
(z. B. 40 Pakete, 1.500 € Umsatz, Bewertung ≥ 4,2, ein Großauftrag, 15 Stand-Verkäufe) mit
Belohnung (Geld + XP).

**F7 Inhalte & Balance.** Mindestens 8 neue Ereignisse, die die neuen Systeme nutzen
(Retourenwelle, Großkunde fragt an, Trend-Alarm …), Kunden- und Firmennamen, Tagesabrechnung um
Retouren/Aufträge/Wochenziele erweitern. Balance: erste Bestellung innerhalb von ca. 1 echten
Minute nach dem Online-Stellen, erstes Level am ersten Tag, Lagerhalle ca. Tag 10–14, Pleite nur
bei grobem Fehlverhalten. `BalanceTests` erweitern.

Die genaue API (Namen, Events, Nutzung für UI und Welt) dokumentiert der Core-Agent in
`docs/CORE_API.md`.

## 3. Oberfläche (UI Toolkit)

- **Handy statt Laptop-überall:** `Tab` öffnet ein Smartphone-Overlay (schnell, klein):
  Bestellzettel, Nachrichten mit Entscheidungen, Schnell-Nachbestellen, Status. Das volle
  **HustleOS** gibt es nur am Schreibtisch-Laptop (Station `Pc`, existiert in Garage und Halle).
- **HustleOS aufräumen:** Standard-Design „Hype“ dunkel (Frachtbrief bleibt als Alternative in den
  Einstellungen). Weniger, dafür reichere Apps mit Tabs (Vorschlag ~9 Apps: Übersicht, Postfach,
  Einkauf+Verpackung, Shop+Marketing+Branding, Markt & Trends, Aufträge, Retouren, Finanzen
  (Bank/Analytics/Trading), Firma (Team/Skills/Ausbau/Ziele/Lifestyle)). Boot-Animation,
  saubere Übergänge.
- **HUD neu:** oben links kompakt Geld/Tag/Wochentag/Uhr/Bewertung/Level; oben rechts
  Bestellzettel mit Countdown und Express-Markierung; Ziel-Karte; Toasts; Interaktions-Pille.
- **Menüs:** Hauptmenü mit Logo, Spielstand-Karten, „Was ist neu in v3.0“-Panel; Pausemenü;
  Einstellungen inkl. UI-Größe; Hilfe mit Tastenbelegung; Tagesabrechnung als Kassenbon;
  Level-Up-Feier; Credits mit **allen Asset-Quellen** (aus `Resources/credits.json`).
- **Schriften** (fester Pfad, siehe 6.): `Resources/Fonts/UI-Regular`, `UI-Bold`, `Display-Bold`,
  `Mono-Regular`, `Mono-Bold` – immer mit Fallback, falls eine Datei fehlt.

## 4. Welt

- **Assets einbauen** (über `AssetLib`, immer mit prozeduralem Fallback): Fahrzeuge (Lieferwagen,
  Verkehr, Lifestyle-Autos), Stadtkulisse, Straßenmöbel, Pflanzen/Bäume, Möbel und Deko, Imbiss-
  Einrichtung und Essen, Lager-Einrichtung, animierte Figuren (Passanten, Kundschaft, Personal,
  Kalle), PBR-Materialien auf Böden/Wänden/Straßen, echte Geräusche und Musik.
- **Neue Stationen:** Retourenplatz, Palettenplatz (sichtbarer Stapel, Spedition holt ab),
  Bestell-Monitor an der Wand, Beschilderung der Zonen (WARENEINGANG, VERSAND …).
- **Führung:** Ziel-Markierungen (schwebender Pfeil/Diamant mit Entfernung) für Tutorial und
  aktuelles Ziel.
- **Licht & Stimmung:** Innenleuchten, Nacht-Atmosphäre, optional HDRI für Reflexionen,
  Umgebungsgeräusche (Straße, Imbiss, Halle).

## 5. Assets aus dem Netz – Regeln

- **Erlaubt:** CC0 / Public Domain (bevorzugt: Kenney, Poly Haven, ambientCG, Quaternius),
  CC-BY 3.0/4.0 (mit Namensnennung), SIL OFL (Schriften). Pro Asset Lizenz auf der Quellseite
  prüfen.
- **Nicht erlaubt:** NC, ND, CC-BY-SA, „frei, aber nicht weiterverteilen“ (Pixabay, Sonniss,
  Mixamo, Asset-Store-Rips), unklare Lizenzen, Assets hinter Logins.
- **Quellen sind Pflicht:** `CREDITS.md` (Repo-Wurzel) mit Name, Autor, Quelle-URL, Lizenz,
  Lizenz-URL, Dateien, Änderungen; Lizenztexte unter `Assets/DropshippingGame/ThirdParty/Licenses/`;
  maschinenlesbar `Assets/DropshippingGame/Resources/credits.json` für die Credits im Spiel.
- **Größe:** insgesamt möglichst < 300 MB, jede Datei < 50 MB (GitHub-Limit 100 MB). Texturen
  1K (höchstens 2K für große Flächen), Audio als OGG, Modelle als FBX (Unity-nativ; GLB nur wenn
  das offizielle glTFast-Paket eingebunden wird).
- **Prüfen vor Übernahme:** Vorschaubilder rendern und ansehen (z. B. Blender per `pip install
  bpy`), Maße/Ausrichtung messen und im Manifest festhalten.

## 6. Feste Schnittstellen zwischen den Paketen

**Pfade in `Assets/DropshippingGame/Resources/`:**

| Was | Pfad / Name |
|---|---|
| Schriften | `Fonts/UI-Regular`, `Fonts/UI-Bold`, `Fonts/Display-Bold`, `Fonts/Mono-Regular`, `Fonts/Mono-Bold` (.ttf/.otf) |
| Soundeffekte | `Audio/SFX/<name>` plus Varianten `<name>_2`, `<name>_3` … Namen wie in `AudioSynth.SfxNames` (click, hover, pickup, drop, place, cash, notify, order, error, tape, printer, levelup, step, fold, truck, door, whoosh, plate, bad) plus neue (z. B. step_concrete, step_wood, step_tile, step_grass, box_open, scanner, phone_open, phone_close, typing, coin …) |
| Umgebung (Loops) | `Audio/Ambience/<name>` (street, diner, warehouse, night …) |
| Musik | `Audio/Music/<name>` (menu, work_1, work_2 …, diner, evening) |
| Modelle | laut `asset_manifest.json` (Kategorie + ID) |
| Oberflächen/PBR | `Textures/Surfaces/<id>/<id>_basecolor|_normal|_roughness|_ao` |
| Manifest | `asset_manifest.json`, `credits.json` |

**`AssetLib` (Scripts/Runtime/Rendering/AssetLib.cs)** – alle Aufrufe geben `null`/`false`
zurück, wenn etwas fehlt, damit die Welt prozedural weiterbauen kann:

```csharp
public static class AssetLib
{
    public static bool HasModel(string id);
    // fit > 0: größte Kante wird auf fit Meter skaliert; sonst Maße aus dem Manifest
    public static GameObject Model(string id, Transform parent, Vector3 localPos, float rotY = 0f,
                                   float fit = -1f, bool collider = false, bool castShadows = true);
    public static Bounds ModelBounds(string id);
    public static IReadOnlyList<string> Ids(string category);
    public static Material Surface(string id, float tileMeters = 1f);   // URP-Lit mit PBR-Maps
    public static Texture2D Texture(string path);
    public static AudioClip Sfx(string name);        // zufällige Variante
    public static AudioClip Ambience(string name);
    public static AudioClip Music(string name);
    public static Font Font(string key);             // "ui", "ui_bold", "display", "mono", "mono_bold"
}
```

## 7. Technische Regeln (für alle)

- Unity **6.3 LTS (6000.3.25f1)** – das hat der Besitzer installiert. URP, UI Toolkit, Input System.
- **C# 9**, keine `record`s und keine `init`-Setter (fehlendes `IsExternalInit` in Unity).
- Keine veralteten Unity-APIs (`FindObjectOfType`, `Rigidbody.velocity`, `PhysicMaterial` …).
  Mehrdeutige Namen voll qualifizieren (z. B. `UnityEngine.ShadowQuality` vs. URP-`ShadowQuality`).
- `Scripts/Core` ohne `UnityEngine`. Tests: `dotnet test` (siehe Agenten-Anweisungen).
- Jede Änderung muss kompilieren: Stub-Kompilierprüfung und – sobald vorhanden – die echte
  Unity-6.3-Prüfung (`scratchpad/unity63/check.sh <repo>`).
- Commit-Nachrichten enden mit:
  ```
  Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01AsYRN9VhqLUAvaJk52MNJ9
  ```

## 8. Arbeitspakete und Dateibesitz

| Welle | Paket | Besitzt (darf ändern) |
|---|---|---|
| 1 | **Toolchain** – echte Unity-6.3-Kompilierprüfung, Kompilierfehler beheben, Pakete/Version aktualisieren | `Packages/`, `ProjectSettings/`, gezielte Fixes überall |
| 1 | **Assets** – Beschaffung, Lizenzen, Quellen, Manifest, `AssetLib`, Import-Regeln | `Resources/` (neue Unterordner), `ThirdParty/`, `CREDITS.md`, `docs/ASSETS.md`, `Rendering/AssetLib.cs`, `Editor/AssetImportRules.cs` |
| 1 | **Core** – F1–F7, Tests, `docs/CORE_API.md` | `Scripts/Core/`, `Tests/` |
| 1+2 | **UI** – Handy, HUD, HustleOS, Menüs, Schriften; in Welle 2 die UI der neuen Mechaniken | `Scripts/Runtime/UI/`, `Resources/UI/`, `GameRoot.cs`, `GameInput.cs`, `Settings.cs` |
| 2 | **Welt-Umgebung** – Assets einbauen, Materialien, Licht, Figuren, Audio | `Rendering/` (außer AssetLib), `World/Props.cs`, `World/CharacterKit.cs`, `World/NPC.cs`, `World/ItemKit.cs`, `Audio/`, Umgebungsteile von `World/WorldBuilder.cs` |
| 2 | **Welt-Gameplay** – neue Stationen, Markierungen, Zonen, Spieler-Effekte | `World/Station.cs`, `World/StationKit.cs`, `World/DroppedItem.cs`, `World/Highlighter.cs`, `Player/`, `StationDefs()` in `WorldBuilder.cs`, neue Dateien |
| 3 | **Integration & QA** – Zusammenführen, Prüfen, Doku, Push | alles |
