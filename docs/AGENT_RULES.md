# Regeln für Agenten (Claude) in diesem Projekt

- Projekt: Unity 6.3 (6000.3.25f1, URP 17.3, UI Toolkit, Input System 1.20, C# 9), First-Person-„Hustle Simulator“ (Dropshipping-Satire, Zielpreis 6–8 € auf Steam). Code in `Assets/DropshippingGame/`. Arbeits-Branch: `claude/busy-knuth-yehyvb` (nie direkt auf `main`).
- Start jeder Arbeit: `git fetch origin claude/busy-knuth-yehyvb && git reset --hard origin/claude/busy-knuth-yehyvb` (Worktrees starten manchmal auf altem Godot-Stand 744c71b).
- Aufbau: `Scripts/Core` = reine Logik ohne UnityEngine (Namespace `DropshippingGame.Core`), `Scripts/Runtime` = Unity-Schicht (World/, Player/, Rendering/, UI/ inkl. Laptop/Web/*, PhoneView, HudView, AdminPanel = Dev-Modus F10), `Scripts/Editor` = Projekt-Setup. Doku: `docs/CORE_API.md`, `docs/ASSETS.md`, `docs/GDD.md`, `CREDITS.md`.
- Regeln: C# 9 (kein `record`/`init`), keine veralteten Unity-APIs, mehrdeutige Namen voll qualifizieren (z. B. `UnityEngine.ShadowQuality`), defensiv coden, immer prozeduraler Fallback wenn ein Asset fehlt, Assets nur CC0/CC-BY/OFL mit Eintrag in `CREDITS.md` + `Resources/credits.json`. Core-Regeln mit NUnit-Tests (`Tests/EditMode`), Speicherstand-Erweiterungen abwärtskompatibel (+ Test).
- Prüfen ohne Unity (Pflicht vor jedem Commit):
  - einmalig `tools/compilecheck/setup.sh`
  - `tools/compilecheck/build.sh <repo-pfad>` → muss `CORE OK`, `RUNTIME OK`, `EDITOR OK` zeigen
  - `dotnet test tools/compilecheck/Tests.csproj` → alle Tests grün
- Parallelarbeit: neue Features in NEUEN Dateien, in gemeinsamen Dateien (WorldBuilder, Sim, Sim.Save, GameData, LaptopView, PhoneView, GameRoot, GameInput, AdminPanel) nur kleine Hooks, nie umformatieren.
- Commits: deutsche Nachricht, endet mit
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` und `Claude-Session: https://claude.ai/code/session_012JKQQ9oUzF3akk9boUr5S8`. Nicht pushen, wenn man als Sub-Agent arbeitet (der Haupt-Agent führt zusammen).
- Ziel jetzt: fertiges, professionelles, verkaufbares Spiel. Qualität vor Menge: reparieren, polieren, vereinfachen; keine halbfertigen Features.
