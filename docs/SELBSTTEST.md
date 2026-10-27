# Selbsttest – das Spiel prüft sich selbst

Der Selbsttest spielt das Spiel etwa **3 Minuten** lang automatisch durch (neues Spiel, Pakete packen und
verschicken, alle Laptop- und Handy-Seiten, Menüs, Moped, Stadtviertel, Wetter, Englisch, Speichern/Laden,
Feierabend, Bildrate) und schreibt einen **Bericht mit Screenshots**. Den Ordner schickst du einfach als ZIP an
den Entwickler – mehr ist nicht zu tun.

Deine eigenen Spielstände werden dabei **nicht** angefasst: Der Test benutzt einen eigenen Spielstand im
Berichtsordner. Läuft gerade ein Spiel, wird es vorher gespeichert.

## So geht's (ca. 5 Minuten)

1. Projekt in **Unity** öffnen (Unity Hub › Projekt anklicken) und warten, bis Unity fertig geladen hat.
2. Oben auf **▶ (Play)** drücken. Das Hauptmenü erscheint.
3. **F10** drücken. Es erscheint das Fenster „Selbsttest“.
   (Im laufenden Spiel: **F10** › Reiter **Allgemein** › Knopf **„Selbsttest starten“**.)
4. Auf **„Selbsttest starten“** klicken.
5. **Maus und Tastatur nicht anfassen** und ca. **3 Minuten warten.** Oben links steht, welcher Schritt gerade
   läuft (z. B. „SELBSTTEST 9/22 · Moped …“). Das Bild springt dabei wild durch die Stadt – das ist richtig so.
6. Am Ende geht das Spiel zurück ins Hauptmenü, ein Fenster zeigt das Ergebnis (z. B. **„20 von 22 Schritten OK“**)
   und der **Ordner mit dem Bericht öffnet sich** von selbst.
7. Im Datei-Explorer **eine Ebene höher** gehen (der Ordner heißt wie Datum und Uhrzeit, z. B.
   `2026-10-27_18-05-12`), **Rechtsklick** auf den Ordner › **Senden an** › **ZIP-komprimierter Ordner**.
8. Die ZIP-Datei an den Entwickler schicken. Fertig!

**Noch einfacher:** In Unity oben im Menü **Dropshipping › Selbsttest (Play Mode)** anklicken – das startet das
Spiel und den Test in einem Rutsch (Schritte 2–4 entfallen).

## Wo liegt der Bericht?

Windows: `C:\Users\<Name>\AppData\LocalLow\Hustle Games\Dropshipping Simulator\selftest\<Datum_Uhrzeit>\`
(in Unity: Menü **Dropshipping › Selbsttest-Berichte öffnen**). Darin:

| Datei | Inhalt |
|---|---|
| `report.md` | Der Bericht auf Deutsch: oben das Ergebnis, dann jeder Schritt mit OK / HINWEIS / FEHLER |
| `screenshots/` | Ein Bild pro geprüfter Seite und pro Stadtviertel |
| `report.json`, `selftest_log.txt`, `session.log` | Dasselbe für den Entwickler (alle Fehlermeldungen) |
| `missing_en.txt` | Texte, die im Englischen noch fehlen |
| `saves/` | Der Test-Spielstand |

Es werden nur die letzten 10 Berichte aufgehoben.

## Was bedeuten die Ergebnisse?

- **OK** – Schritt hat geklappt.
- **HINWEIS** – hat geklappt, aber etwas war auffällig (z. B. eine Warnung im Log). Zählt als OK.
- **FEHLER** – etwas ging schief. Genau dafür ist der Bericht da – einfach mitschicken.
- **ÜBERSPRUNGEN** – ging nicht, weil ein früherer Schritt (meist „Neues Spiel“) fehlgeschlagen ist.

Der Test bringt das Spiel nicht zum Absturz: Jeder Schritt hat ein Zeitlimit, Fehler werden nur notiert.
Falls doch einmal etwas hängen bleibt: ■ (Stop) drücken und den Ordner trotzdem schicken – der Bericht wird
beim Beenden geschrieben.

## Für Entwickler

- Code: `Assets/DropshippingGame/Scripts/Runtime/Testing/` (`SelfTest.cs` Ablauf/Bericht, `SelfTest.Steps.cs`
  die 22 Schritte, `SelfTestUI.cs` Einblendung/Fenster), Editor-Menü `Scripts/Editor/SelfTestMenu.cs`.
- Eingaben werden über Test-Hooks in `GameInput` simuliert (`TestActive`, `TestMove` – fährt auch das Moped –,
  `TestPressed`, `TestHeld`); Stationen werden wie vom Spieler angevisiert und per „Benutzen“ ausgelöst
  (`PlayerController.LookAt`). Trifft das Fadenkreuz nicht, ruft der Test `Interact` direkt auf und notiert einen HINWEIS.
- Auch als **PlayMode-Tests**: Unity › Window › General › **Test Runner** › Reiter **PlayMode** › **Run All**
  (`Assets/DropshippingGame/Tests/PlayMode`, ein Test pro Schritt, derselbe Bericht). Die Test-Assembly hat
  absichtlich keine Plattform-Einschränkung „nur Editor“ (sonst wären es EditMode-Tests ohne Play-Modus);
  sie wird über `UNITY_INCLUDE_TESTS` nur im Editor bzw. in Test-Builds kompiliert.
- Ohne Unity prüft `tools/compilecheck/build.sh` die PlayMode-Tests gegen NUnit und eine Nachbildung des
  Test-Frameworks (`PLAYMODE OK`) – die echten Test-Framework-Signaturen prüft nur Unity.
