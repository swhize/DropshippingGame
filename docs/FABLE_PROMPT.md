# Übergabe-Prompt für neue Sessions

Kopier den Block unten als erste Nachricht in eine neue Session ohne Vorwissen.

---

Du übernimmst die Weiterentwicklung eines **Unity-6-Spiels** (C#, URP, UI Toolkit, Input System)
in diesem Repository. Ich (der Nutzer) kann nicht programmieren – du schreibst den kompletten Code
selbst, testest ihn und sagst mir nur, was ich tun soll (meist: Unity öffnen und ▶ Play drücken).

## Zuerst lesen
1. `README.md` (Start, Steuerung), `docs/GDD.md` (Vision, Ton, Art-Stil) und `docs/ROADMAP.md`
   (was gebaut ist, was offen ist)
2. `Assets/DropshippingGame/Scripts/Core` – die komplette Spiellogik in reinem C#:
   `Sim.cs` (zentrale Simulation, Events), `Sim.Save.cs` (Spielstände), `GameData.cs` (alle festen
   Daten), `Market.cs`, `EventSystem.cs`/`EventData.cs`, `PitchGame.cs`, `AudioSynth.cs`
3. `Assets/DropshippingGame/Scripts/Runtime` – Unity-Schicht: `GameRoot.cs` (Start, Menü,
   Tagesablauf, Story), `World/` (Welt, Stationen, NPCs), `Player/`, `Rendering/` (Meshes,
   prozedurale Texturen, Materialien, Post-FX), `UI/` (HUD, Fenster, Menüs, `Laptop/` mit allen Apps)

## Technische Leitplanken
- **Keine importierten Assets:** Modelle (`Props`, `StationKit`, `ItemKit`, `CharacterKit`,
  `MeshKit`), Texturen (`TexGen`), Materialien (`Mats`), Symbole (`Icons`), Sounds/Musik
  (`AudioSynth`) entstehen zur Laufzeit. Aussehen der Oberfläche steht in `Resources/UI/Game.uss`.
- `Scripts/Core` hat **keine** Unity-Abhängigkeit (`noEngineReferences`) – neue Spielregeln dort
  einbauen und mit Tests absichern. Die Unity-Schicht hört nur auf `Sim`-Events.
- `GameRoot` startet automatisch (auch in einer leeren Szene). `ProjectSetup.cs` richtet URP,
  Szene und Build-Einstellungen beim ersten Öffnen ein.
- C# 9 (Unity-Grenze): keine neueren Sprachfeatures. Keine Unity-6-veralteten APIs
  (`Rigidbody.velocity`, `PhysicMaterial`, `FindObjectOfType` …).

## Verifikation nach jeder Änderung
- Logik-Tests ohne Unity: kleines `dotnet`-Projekt (net8.0, NUnit 3), das
  `Scripts/Core/**/*.cs` und `Tests/EditMode/**/*.cs` einbindet → `dotnet test`
- Kompilierprüfung der Unity-Schicht ohne Unity: `netstandard2.1`-Projekt gegen die
  Unity-Referenz-DLLs (NuGet `UnityEngine.Modules`, für Editor-Code `Unity3D.SDK`) plus Stubs für URP,
  Input System und UGUI; einmal mit `ENABLE_INPUT_SYSTEM`, einmal mit `ENABLE_LEGACY_INPUT_MANAGER`
- In Unity: **Window → General → Test Runner → EditMode → Run All**, dann ▶ Play

## Aufgabe
Arbeite die offenen Punkte aus `docs/ROADMAP.md` ab, entscheide selbst wo sinnvoll und notiere
Annahmen im Roadmap-Dokument. Nach jeder größeren Änderung kurz erklären, was ich im Spiel
ausprobieren soll.

---
