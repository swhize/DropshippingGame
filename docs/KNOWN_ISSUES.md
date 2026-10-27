# Bekannte Punkte & Test-Checkliste – 1.0.0 Release Candidate

Stand: 27.10.2026 · Version `1.0.0-rc1` (Datei [`VERSION`](../VERSION))

Der Code ist ohne Unity geprüft: Core, Runtime und Editor kompilieren gegen die echten Unity-6.3-DLLs
(`tools/compilecheck/build.sh`, **0 Compiler-Warnungen** auch ohne `-nowarn`), alle NUnit-Tests der Spiellogik
sind grün (`dotnet test tools/compilecheck/Tests.csproj`). **Was ohne Unity nicht geprüft werden kann**, steht unten
als Checkliste: Darstellung, Physik, Shader, Layout, Eingabe, Ton und echte Builds. Diese Liste spielt der Inhaber
einmal komplett durch, bevor aus dem RC die 1.0 wird. Ergebnisse direkt hier abhaken; Fehler mit Screenshot und
Pausemenü › **Feedback & Bug melden** (kopiert Version + Systeminfos) melden.

Tipp: Im Editor/Development-Build öffnet **F10** das Admin-Panel (Geld, Level, Zeit, Wetter, Teleport, Ereignisse,
Vogelperspektive). Damit lassen sich späte Spielphasen schnell erreichen.

**Zuerst den Selbsttest laufen lassen** ([`SELBSTTEST.md`](SELBSTTEST.md)): ▶ Play › im Hauptmenü **F10** ›
„Selbsttest starten“ (oder Menü Dropshipping › Selbsttest (Play Mode)). Er prüft in ca. 3 Minuten automatisch einen
großen Teil dieser Liste (Kernschleife, alle Laptop-/Handy-Seiten, Menüs, Moped, Viertel, Wetter, Englisch,
Speichern/Laden, Feierabend, Bildrate) und legt einen Bericht mit Screenshots an – den Ordner als ZIP schicken.

Code-Kommentare mit TODO/FIXME/HACK: **keine** (Stand rc1). Offene Aufgaben stehen nur in dieser Datei und in
[`ROADMAP.md`](ROADMAP.md).

Punkte mit **(LP)** stammen aus dem „Playtest durch Lesen“ vom 27.10.2026: Der Code wurde Bereich für Bereich
nachverfolgt (Geometrie, USS, Shader, Eingabe, Lebenszyklus). Behobenes ist zum Nachprüfen aufgeführt, Unsicheres als
konkrete „Schau dir X an: sollte Y sein“-Prüfung.

---

## 0. Erster Start im Editor

- [ ] Projekt in Unity **6000.3.25f1** öffnen: keine roten Fehler in der Console (gelbe Warnungen notieren).
      Nach diesem Aufräum-Commit meldet Unity evtl. einmalig verwaiste `.meta`-Dateien gelöschter Skripte
      (`AppsFinance.cs`) – das ist normal, Unity räumt sie selbst weg.
- [ ] Menü **Dropshipping › Projekt einrichten** läuft ohne Fehler durch (URP-Asset, Renderer, SSAO, Szene, Player-Einstellungen).
- [ ] **(LP)** Danach liegen in `Resources/DSMaterials` zusätzlich `LitSurfaceGlossAo`, `LitSurfaceGloss`, `LitSurfaceAo`.
      Schau dir diese und `LitNormal`/`LitEmission` im Inspector (Debug-Modus, „Shader Keywords“) an: sollten
      `_NORMALMAP`/`_METALLICSPECGLOSSMAP`/`_OCCLUSIONMAP` bzw. `_EMISSION` enthalten. Sind sie leer (URP setzt Keywords
      bei Materialien ohne Texturen evtl. zurück), fehlen die Varianten im Build → Vorlagen dann mit Dummy-Texturen anlegen.
- [ ] **(LP)** Project Settings › Graphics › Shader Stripping › **Fog Modes = Custom** mit Linear/Exponential/Exp2 an
      (setzt „Projekt einrichten“/jeder Build automatisch). Sonst fehlt im Build der Dunst über der Skyline.
- [ ] ▶ Play: Hauptmenü erscheint mit Abendkulisse, rechts das Panel **„Neu in Version 1.0 – Das steckt drin“**,
      unten links „Version 1.0.0-rc1 (Build …)“ bzw. im Editor „… Editor“.
- [ ] URP-Nachbearbeitung wirkt wirklich (Bloom, Tonemapping, Vignette, Weißabgleich/Farbkorrektur je Tageszeit).
      Die URP-Klassen sind im Compile-Check nur nachgebildet (`tools/compilecheck/URPStub.cs`) – echte Signaturen prüft nur Unity.

## 1. Laptop / Tablet (HustleOS)

- [ ] Am Schreibtisch **E**: Tablet öffnet sich mit Rahmen, Kamera-Punkt, Statusleiste (Zurück, Start, Adresse, Uhr,
      Bewertung, Konto, Schließen) und Dock unten. Kein abgeschnittenes Dock bei 1920×1080, 2560×1440 und **1280×800** (Steam Deck).
- [ ] Startseite „Start“ (Uhr, Kennzahlen-Kacheln, Ziel, App-Kacheln): jede Kachel öffnet die richtige App.
- [ ] Jede Dock-App einmal öffnen: AllesExpress (Kauf, Verpackung), Mein Shop (Admin, Produktfotos, Preisvorschlag),
      iWolke Mail (Entscheidung treffen), PaketBlitz, Stadtblatt, TikTak, Fakebook, Gugel, TradingViech (ab Level 4),
      Revoluut (Kredit aufnehmen/tilgen), Markt & Trends, Aufträge (ab Level 3), Retouren, Firma (alle Reiter:
      Team, Skills, Perks, Ausbau, Einrichtung, Logistik, Erfolge, Statistik, Ziele, Branding, Verpackung, Lifestyle).
- [ ] Firma › **Statistik** zeigt die neue Statistik-Seite (die alte „Analytics“-Seite wurde entfernt – gleiche Stelle im Reiter).
- [ ] Einstellungen › Design „Hype“/„Frachtbrief“ und hell/dunkel umschalten: HustleOS-Seiten (Markt, Aufträge, Firma)
      und Handy wechseln sauber; nach dem Aufräumen ungenutzter USS-Regeln darf nichts „nackt“ (ohne Stil) aussehen.
- [ ] **(LP, behoben)** Design „Hype“: Buttons und Chips (Handy, HustleOS) sind Pillen mit halbrunden Enden – vorher
      `--radius-btn/--radius-chip: 999px`, was UI Toolkit als flache Ellipse zeichnet. Schau dir einen breiten Button
      (z. B. Handy › Status) an: sollte gerade Ober-/Unterkante mit runden Enden haben.
- [ ] Branding: Markenname tippen – während des Tippens lösen E/G/Tab/F10 nichts aus; Enter/Esc beendet das Tippen.
- [ ] Controller: LB/RB wechseln Apps/Reiter, A wählt, B schließt, rechter Stick scrollt.
- [ ] Tutorial-Leiste im Laptop während der ersten Stunde, Badges (Mail, PaketBlitz, Aufträge, Skillpunkte) aktualisieren sich.

## 2. HUD & Handy

- [ ] Bestellzettel oben rechts mit Countdown, Express-Markierung, überfällige verschwinden; Toasts darunter.
- [ ] Kompass oben (Garage, Packstation, Post, Bank, Börse, MediaMarkd, Fressnix, Ziel) – Richtung stimmt beim Drehen.
- [ ] Fadenkreuz-Pille mit Aktion, Outline am anvisierten Objekt, Tastenhinweise wechseln zwischen Tastatur und Controller.
- [ ] Untertitel bei Kalle/Passanten, Textgeschwindigkeit wirkt.
- [ ] Handy (**Tab**): alle Apps inkl. Fakebook/Gugel (nach Freischaltung), Wochenziele, Status/Engpass-Assistent.
- [ ] Tagesabrechnung als Kassenbon um 20 Uhr, „Weiter“ startet den nächsten Tag.
- [ ] UI-Größe 80 % / 140 % (Einstellungen › Barrierefreiheit): nichts überlappt oder verschwindet.
- [ ] Farbenblind-Modus: Gut/Schlecht in Blau/Orange.
- [ ] **(LP, behoben)** Möbel verschieben mit Controller: **A** stellt ab, ohne dass die Figur dabei hochspringt.

## 3. Moped

- [ ] Ab der Business-Phase steht das Moped vor der Garage; Hinweis-Mail kommt nach dem Tutorial.
- [ ] Auf-/Absteigen mit **E**, sicherer Ausstieg neben dem Moped (nie in Wänden/unter der Straße).
- [ ] Fahrgefühl: Beschleunigen, Bremsen, Schräglage, max. ~35 km/h, Rückwärtsschieben, Bordstein-Hüpfer,
      keine Fahrt in Innenräume; Kollision mit Häusern, Props und Verkehr.
- [ ] **V** wechselt Ego-Sicht (Lenker, Arme, Tacho) / Verfolgerkamera; **H** Hupe; Motorklang folgt dem Tempo; Licht nachts.
- [ ] Heckbox: 4 Pakete oder 1 Kiste einladen/herausnehmen; Abgabe an Packstation/Filiale direkt aus der Box.
- [ ] Modell: Vespa (CC0) richtig herum (Front nach vorn), Räder/Lenker drehen, Lack in Markenfarbe; ohne Modell prozeduraler Ersatz.
      **(LP)** Liegt das Vorderrad des importierten Modells hinten, dreht der Code es jetzt selbst um 180°. Schau dir an:
      Scheinwerferlicht vorn, Heckbox sitzt auf dem Gepäckträger hinter dem Sitz (nicht auf dem Beinschild), Kennzeichen hinten.
- [ ] **(LP, behoben)** „Weniger Bewegung“/Streamer-Modus: Ego-Sicht neigt sich in Kurven kaum noch mit, kein Tempo-Zoom (FOV).
- [ ] Speichern auf dem Moped / mit vollen Boxen → Laden: Position und Inhalt stimmen.

## 4. Stadt / Karte

- [ ] Hauptstraße mit Tunnelportalen: Autos und Passanten verschwinden im Tunnel, nicht am Kartenrand.
- [ ] Verkehr bremst vor Spieler und abgestelltem Moped; Linienbus fährt; Müllabfuhr kommt.
- [ ] Passanten laufen auf Gehwegen, weichen aus, kommen nicht in Wänden fest.
- [ ] Vogelperspektive (Admin › Welt & Zeit): keine Ampeln/Leitkegel/Schilder auf Fahrbahnen, Zebrastreifen außerhalb der Kreuzungen.
- [ ] Modelle aus den CC0-Paketen (Kenney, KayKit, Quaternius, Tiny Treats, Bus): Ausrichtung und Größe plausibel, nichts schwebt.
- [ ] **(LP, behoben)** Garage: Poster (Westwand), Whiteboard, Neon „HUSTLE“, Bestell-Monitor (Rückwand) und die Schilder
      **LAGER** (Ostwand) / **RETOUREN** (über dem Tor innen) steckten komplett in der 30 cm dicken Wand (Koordinaten aus dem
      Godot-Original bezogen sich auf die Wandmitte). Jetzt 2 cm vor der Innenseite: alle sichtbar, nichts flackert (Z-Fighting)?
      Matratze 25 cm nach vorn: der rote Wecker am Kopfende ist sichtbar.
- [ ] **(LP, behoben)** Lagerhalle: Werkbank und Kartons standen mitten in den Wandregalen der Produkte 11–14 (Westwand) und
      blockierten das Anvisieren. Jetzt an der Westwand zwischen Regalen und Büro (z −21,7 … −17,7). Alle vier Wandregale
      erreichbar? Feuerlöscher sichtbar (steckte im Sockel), Poster/Neon an der Wand sichtbar.
- [ ] **(LP, behoben)** Kalles Aufsteller vor dem Imbiss: Text liegt jetzt auf der geneigten Tafel (die untere Zeile steckte darin)
      und ist kleiner (passt auf 0,7 m).
- [ ] **(LP, neu)** Schilder (`Props.SignBoard`, Plakatwand) verkleinern zu lange Texte automatisch auf die Schildbreite
      (`Label3D.MaxWidth`). Schau dir „WARENEINGANG“, „B2B · SPEDITION“, „TIERHEIM-ECKE · ADOPTIEREN“, „AUDIO, VIDEO & DROHNEN“,
      „ZU VERKAUFEN · 4.500 €“, die Ladenschilder der Stadt und einen langen Markennamen auf der Plakatwand an – auch auf Englisch:
      Text bleibt auf dem Schild. Sind dagegen ALLE Schrift-Höhen deutlich zu groß/klein für ihre Schilder, stimmt die Größenannahme
      in `Label3D.Create` (Schriftgröße × 4 mm über `TextMesh.characterSize`) nicht – dann dort korrigieren, nicht an jedem Schild.
- [ ] **(LP)** Börse: Die 20-cm-Stufe vor den Säulen (x 84–108, z 9,6–12) hat keinen Collider – schau dir Spieler, Moped und
      Passanten dort an: Füße sollten nicht sichtbar in der Stufe versinken (sonst Stufe flacher machen).
- [ ] Figuren-Animationen (Gehen, Idle; „sit“-Clip war unsicher) – Gäste im Imbiss sitzen glaubwürdig.
- [ ] Leistung: Perf-Overlay (F10) – 60 FPS auf Mittel/Hoch in der Innenstadt, Grafik „Niedrig“ reduziert Passanten/Verkehr/Lichter.

## 5. Läden (Einkaufsviertel)

- [ ] MediaMarkd und Fressnix: Wagen schieben, Preisscanner, Artikel in den Wagen, Kasse → Kisten am Wareneingang.
- [ ] Tagesaktionen wechseln je Tag; Kassen-Fenster mit Controller bedienbar (B schließt).
- [ ] Haustiere kaufen: Tier folgt bzw. wohnt in der Garage, Napf-Füllstand = Futtervorrat, Tier-Ecke mit Gadgets.
- [ ] Verschlossene DLC-Tür (Klamottenladen) zeigt nur einen Hinweis, kein Absturz.

## 6. Finanzen

- [ ] Finanzviertel: Bank (Kredit, Sparkonto), Börse mit Maklern, Laufband mit Nachrichten.
- [ ] Aktien/ETF/Krypto kaufen/verkaufen, Kurse laufen live (TradingViech-Seite aktualisiert sich ohne Neuladen).
- [ ] Kontostand unter −500 € am Abend → Mama-Notkredit (Normal einmal) bzw. Insolvenz-Fenster mit „Letzten Spielstand laden“.
- [ ] Wallet-App auf dem Handy, Schufi-Score und Vermögen stimmen mit der Bank überein.

## 7. Straßenfest

- [ ] Fest wird angekündigt (ab Level 4), Aufbau sichtbar, Marktstand mit Ware befüllen, Besucher kaufen.
- [ ] Bilanz am Ende des Festtags; Besucher verschwinden danach, keine hängenbleibenden NPCs.
- [ ] Verkaufsstand vor der Garage: Passanten kaufen direkt.

## 8. TikTok

- [ ] Ringlicht aufstellen, Aufnahme starten: Sucher (Viewfinder) mit Rahmen, Format-Hinweis, Timer.
- [ ] Wertung reagiert auf Licht, Abstand, Ruhe, Winkel, Hook und Aktionen; Aufnahme mit Enter/Klick/RB beenden.
- [ ] Video erscheint in TikTak, Boost/Hype wirkt; max. Videos pro Tag werden eingehalten.
- [ ] Streamer-Modus: kein Blitzen beim Aufnehmen.

## 9. Feel-Effekte (Spielgefühl)

- [ ] Packtisch-Sequenz (Karton, Produkt fällt hinein, Laschen, Klebeband), Labeldrucker, Packstation-Fach mit Münzen.
- [ ] Schwebende Zahlen („+12 €“), Funken/Konfetti/Sterne (Shader `DS/Particle`, sonst Fallback) – keine rosa Quadrate.
- [ ] FOV-Kick/Kamerawackeln; mit „Weniger Bewegung“ oder Streamer-Modus komplett aus.
- [ ] Level-Up, Rekordtag, erstes Paket: Bildschirm-Konfetti + Stempel über der UI, verschwinden wieder.
- [ ] Neue Synth-Sounds (Klebeband, Drucker, Münzen, Ding, Pop) klingen sauber, nicht übersteuert.

## 10. Wetter & Look

- [ ] Tagesstimmungen (Morgen, Mittag, Goldene Stunde, Blaue Stunde, Nacht): weiche Übergänge, kein Flackern der Schatten.
- [ ] Wetter sonnig/bewölkt/Regen (Admin › Wetter erzwingen): Regen-Partikel um die Kamera, nasse Straßen mit Pfützen;
      Einstellung „Wechselndes Wetter“ aus = immer sonnig.
- [ ] Nachts: beleuchtete Fenster, hinterleuchtete Ladenschilder, Lichtkreise unter Laternen, Moped-Licht.
- [ ] Hauptmenü-Kulisse: Goldene Stunde mit Blick auf die Plakatwand.
- [ ] Weltschrift (Shader `Text3D`) und Hervorhebungs-Outline (`Highlight`) werden gerendert, auch im Build (Shader im Build enthalten).

## 11. Lokalisierung (Deutsch/Englisch)

- [ ] Einstellungen › Sprache auf **English** im Hauptmenü: Menü, Panel „New in version 1.0“, Einstellungen sofort englisch.
- [ ] Im laufenden Spiel umschalten: HUD, Handy, Tablet, 3D-Schilder (Label3D) wechseln; offene Fenster spätestens nach dem Neuöffnen.
- [ ] Zurück auf Deutsch: alle Texte wieder deutsch (LocSweeper stellt den Rohtext her).
- [ ] Zahlenformat Englisch „1,234 €“, Deutsch „1.234 €“.
- [ ] Admin › „Fehlende Übersetzungen speichern“ nach einer Stunde Englisch: `missing_en.txt` sollte (fast) leer sein.
- [ ] Lange englische Texte sprengen keine Buttons/Kacheln (Tablet-Dock, Einstellungen, Kassenbon).

## 12. Speichern / Laden

- [ ] 3 Spielstände mit Vorschaubild (PNG) und Markenkarte; Auto-Save alle 90 s, morgens und beim Beenden.
- [ ] **(LP)** Vorschaubild entsteht über `Camera.Render()` in eine RenderTexture (URP): schau dir ein Bild im Hauptmenü an –
      sollte das Spielbild mit Farbkorrektur zeigen, nicht schwarz, nicht flau (Gamma) und ohne HUD.
- [ ] Speichern während Feierabend wird abgelehnt (verhindert Doppel-Abrechnung) – Hinweis erscheint.
- [ ] Beenden mitten in Aktionen (Paket in der Hand, Moped, Möbel verschieben, TikTok-Aufnahme) → Laden ohne Fehler.
- [ ] Kaputte Datei simulieren (Spielstand-Datei leeren): Laden aus `.bak` mit Hinweis.
- [ ] Alte Einstellungen aus Vorabversionen: Design wird einmalig auf „Hype“ dunkel gesetzt, sonst bleibt alles erhalten.

## 13. Build

- [ ] **Dropshipping › Build › Windows (Release)** (IL2CPP, sonst Mono mit Warnung) baut ohne Fehler;
      Bericht `Builds/Windows-Release_report.txt` ansehen (Größe, größte Assets).
- [ ] Release-Build auf einem fremden Windows-PC: Start, neues Spiel, Imbiss, Garage, 3 Tage, speichern, beenden, laden.
- [ ] F10, F9, ^ und Alt tun im Release-Build **nichts**; Pausemenü ohne Admin-Knopf.
- [ ] Hauptmenü und Pausemenü zeigen `1.0.0-rc1 (Build …, Release)`; Fehlerbericht und `logs/session_*.log` ebenso.
- [ ] `*_DoNotShip`/`*_DontShip`-Ordner liegen nicht im Build-Ordner (werden nach `Builds/_Symbols/` verschoben).
- [ ] Lizenz-Sperre: **Dropshipping › Build › Lizenzen prüfen** meldet 0 `personal_only`-Assets.
- [ ] Demo-Build: endet nach Tag 5 mit „Auf die Wunschliste!“, eigener Spielstand-Ordner.
- [ ] Optional macOS-Build: `Application.version` ist dort bewusst nur numerisch (`1.0.0.<Build>`), Menüs zeigen trotzdem `1.0.0-rc1`.
- [ ] Alt-Tab, Auflösungs- und Fenstermoduswechsel, Controller ab-/anstecken im Build.
- [ ] **(LP)** Build gegen Editor vergleichen (gleicher Spielstand, gleiche Uhrzeit): Straßen/Gehwege mit gleicher Rauheit und
      Fugen-Schatten (PBR-Varianten), Nebel/Dunst über der Skyline, leuchtende Schilder nachts. Unterschiede = fehlende Shader-Varianten.
- [ ] **(LP)** `PostFX.ApplyQuality` schreibt Schatten/MSAA/Render-Scale ins URP-Asset: nach einer Play-Session im Editor zeigt die
      Versionsverwaltung evtl. Änderungen an `Settings/DS_URP.asset` – nicht mit einchecken. Grafik „Hoch“ = MSAA 4× **und** SMAA:
      Leistung prüfen.

---

## Bekannte Einschränkungen (kein Fehler, bewusst für 1.0)

- Nur Einzelspieler – Koop kommt als großes Update (siehe [`ROADMAP.md`](ROADMAP.md)).
- Steam-Integration ist vorbereitet, aber aus: Steamworks.NET installieren und `DS_STEAM` einschalten, App-IDs in
  `SteamIds` eintragen ([`RELEASE.md`](RELEASE.md) Abschnitt 4).
- Steam Deck: Bildschirmtastatur für die Markennamen-Eingabe fehlt noch (Controller-Namensvorschlag ist da).
- Controller-Symbole immer im Xbox-Stil (A/B/X/Y), auch bei DualSense.
- Low Poly Vespa (Moped-Modell): CC0-Angabe vor Release auf der Originalseite bestätigen ([`ASSET_LIZENZEN.md`](ASSET_LIZENZEN.md)).
- Asset-Lizenzen allgemein: Downloads kamen teils von GitHub-Spiegeln – Stichprobe auf den Originalseiten (siehe `CREDITS.md`).
- Design „Hype“/„Frachtbrief“ wirkt nur noch auf Handy und HustleOS-Seiten; die Web-Apps im Tablet haben ihr eigenes Design.
