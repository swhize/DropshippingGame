# Übergabe-Prompt für neue Sessions

Kopier den Block unten als erste Nachricht in eine neue Session ohne Vorwissen.

---

Du übernimmst die Weiterentwicklung eines Godot-4.7-Spiels unter `D:\DropshippingGame`.
Ich (der Nutzer) kann nicht programmieren – du schreibst den kompletten Code selbst,
testest ihn und sagst mir nur, was ich tun soll (meist F5 im Editor).

## Zuerst lesen
1. `docs/GDD.md` (Vision, Ton, Art-Stil) und `docs/ROADMAP.md` (was gebaut ist, was offen ist)
2. Alle Skripte unter `scripts/` (Unterordner `ui/`, `pc/`, `pc/apps/`) und `tests/`

## Technische Leitplanken
- Godot 4.7, nur **GDScript**. Alles wird zur Laufzeit per Code gebaut, `.tscn` bleiben minimal.
- **Keine importierten Assets**: Modelle (`Props.gd`, `StationKit.gd`, `ItemKit.gd`,
  `CharacterKit.gd`), Materialien/Shader (`Mats.gd`), UI-Theme (`UITheme.gd`) und
  Sounds/Musik (`AudioManager.gd`) sind prozedural.
- Autoloads (Reihenfolge wichtig): `Settings`, `Audio`, `Market`, `Events`, `GameManager`.
  `GameManager` ist die zentrale Spiellogik, `GameData.gd` enthält alle festen Daten.
- Szenen: `scenes/MainMenu.tscn` (Startszene) → `scenes/Main.tscn` (Spielsitzung, `Main.gd`).
- **Neue `class_name`-Skripte** werden erst nach einem Editor-Scan erkannt:
  `godot --headless --editor --path D:\DropshippingGame --quit-after 30`
- Godot: `D:\Godot_v4.7.2-stable_win64.exe`

## Verifikation nach jeder Änderung
```
godot --headless --path D:\DropshippingGame -s tests/compile_all.gd
godot --headless --path D:\DropshippingGame -s tests/run_tests.gd
godot --headless --path D:\DropshippingGame res://scenes/Main.tscn -- --selftest
godot --headless --path D:\DropshippingGame --check-only --quit
godot --headless --path D:\DropshippingGame --quit-after 600
```
Visuelle Prüfung per Screenshot (mit Fenster, siehe Kopf von `scripts/DebugHarness.gd`):
```
godot --path D:\DropshippingGame --resolution 1280x720 -- --start=skip --demo --stage=1 --staff --time=13 --screenshot=C:\pfad\bild.png
```
Test-Spielstände landen im echten Nutzerordner
(`%APPDATA%\Godot\app_userdata\Dropshipping Simulator\savegame.json`) – nach Tests löschen.

## Aufgabe
Arbeite die offenen Punkte aus `docs/ROADMAP.md` ab, entscheide selbst wo sinnvoll und
notiere Annahmen im Roadmap-Dokument. Nach jeder größeren Änderung kurz erklären, was
ich im Spiel ausprobieren soll.

---
