# Dropshipping Simulator – Vom Imbiss zum Imperium

Ein First-Person-Wirtschaftsspiel in **Unity 6** (URP, UI Toolkit, C#). Du startest als Aushilfe in
Kalles Imbiss, kündigst und baust aus deiner Garage heraus eine eigene Dropshipping-Marke auf:
Ware einkaufen, einlagern, verpacken, etikettieren, versenden – und über den Laptop „HustleOS“
Webshop, Marketing, Bank, Personal, Ausbau und Trading steuern.

Alle Modelle, Texturen, Symbole, Sounds und die Musik werden beim Start prozedural per Code erzeugt –
es gibt keine importierten Assets.

## Spielen – Schritt für Schritt

1. **Unity Hub** installieren: <https://unity.com/download>
2. Im Hub unter **Installs → Install Editor** eine **Unity 6** Version (LTS, „6000.x“) installieren.
   Zusatzmodule sind nicht nötig.
3. Dieses Projekt herunterladen (GitHub → Branch `claude/busy-knuth-yehyvb` → **Code → Download ZIP**)
   und entpacken. Wichtig ist der Ordner, in dem `Assets`, `Packages` und `ProjectSettings` liegen.
4. Im Hub: **Projects → Add → Add project from disk** und genau diesen Ordner auswählen.
5. Projekt öffnen. Fragt der Hub nach der Version, deine installierte Unity-6-Version wählen und
   bestätigen („Continue“ / „Change Version“).
6. Das erste Öffnen dauert ein paar Minuten (Unity lädt Pakete).
   - Erscheint die Frage zum **neuen Input System** („enable the new backends?“): **Yes** klicken.
     Unity startet dann einmal neu – das ist normal.
7. Es erscheint das Fenster **„Das Projekt ist eingerichtet!“** → oben in der Mitte auf **▶ (Play)** drücken.

Tipp: Im Game-Fenster oben rechts **„Maximize On Play“** einschalten, dann läuft das Spiel groß.

### Falls etwas hakt
- **Kein Fenster „eingerichtet“ oder alles lila/pink:** Menü oben **Dropshipping → Projekt einrichten**.
- **Play startet eine leere Szene:** Menü **Dropshipping → Spiel starten**.
- **Spielstände finden:** Menü **Dropshipping → Spielstände-Ordner öffnen**.

## Steuerung

| Aktion | Tastatur & Maus | Controller |
|---|---|---|
| Laufen / Umsehen | WASD / Maus | linker / rechter Stick |
| Sprinten / Ducken / Springen | Shift / Strg / Leertaste | linker Stick drücken / rechter Stick drücken / A |
| Benutzen | E | X |
| Ablegen | G | B |
| Laptop | Tab | Y |
| Pause | Esc | Start |
| Hilfe | F1 | Select |
| Screenshot | F12 | – |

## Was drin ist

- **Story:** Schicht in Kalles Imbiss (Teller an die richtigen Tische), Kündigung, eigene Garage
- **Kreislauf:** Einkauf → Kiste annehmen → Regal → Webshop → Bestellung → Kommissionieren →
  Verpacken → Label drucken → Versand
- **HustleOS-Laptop mit 14 Apps:** Übersicht (Aufgabenliste), Postfach, Einkauf, Verpackung, Webshop,
  Marketing (inkl. TikTok-Minispiel), Branding, Marktanalyse, Trading, **Bank (Kredite)**, Analytics,
  Personal, Ausbau, Ziele & Lifestyle
- **Zwei Laptop-Designs, jeweils hell oder dunkel:** „Frachtbrief“ und „Hype“ – umschaltbar unter
  **Pause → Einstellungen**. Die HTML-Entwürfe zum Vergleichen liegen in `docs/laptop-designs/`.
- **10 Produkte**, 3 Lieferanten, 4 Bestellgrößen, Kartons S/M/L (auch ungefaltet)
- **Verkaufsstand** vor der Garage: Passanten kaufen direkt
- **Pitch Day als Minispiel:** drei Juroren, deine echten Kennzahlen entscheiden
- Firmenlevel 1–10, Lagerhalle, Personal, Förderband, 20 Ereignisse, Konkurrenz, Bewertungen,
  Ziele, Insolvenz und Endziel (100.000 € Umsatz)
- Tageslicht-Zyklus, Straßenlaternen, NPCs, Verkehr, Bloom, Umgebungsverdeckung (SSAO)
- **3 Spielstände**, Auto-Save alle 90 Sekunden und jeden Morgen
- Controller-Unterstützung (Xbox/PlayStation)

## Für Entwickler

- `Assets/DropshippingGame/Scripts/Core` – komplette Spiellogik in reinem C# (ohne Unity),
  testbar mit `dotnet test` oder im Unity Test Runner (**Window → General → Test Runner → EditMode**)
- `Assets/DropshippingGame/Scripts/Runtime` – Welt, Spieler, Stationen, Rendering, Audio, UI
  (`GameRoot` startet alles automatisch)
- `Assets/DropshippingGame/Scripts/Editor/ProjectSetup.cs` – automatische Einrichtung (URP, Szene)
- `Assets/DropshippingGame/Resources` – Stylesheets (USS) und die beiden eigenen Shader
- `docs/` – Game Design Document, Roadmap, Laptop-Design-Entwürfe
