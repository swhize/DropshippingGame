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
| Sprinten / Ducken / Springen | Shift / Strg (C) / Leertaste | linker Stick drücken / rechter Stick drücken / A |
| Benutzen (auch Laptop am Schreibtisch) | E | X |
| Ablegen | G | B |
| **Handy** (Bestellzettel, Nachrichten, Nachbestellen, Wochenziele, Status) | Tab | Y |
| Handy-App bzw. Laptop-Reiter direkt wählen | 1–4 | – |
| App bzw. Reiter wechseln (Handy & Laptop) | Maus | LB / RB |
| Zurück / Schließen | Esc | B |
| Pause | Esc | Start |
| Hilfe | F1 | Select |
| Screenshot | F12 | – |

Die Bestellzettel stehen **oben rechts** im HUD. Den vollen HustleOS-Laptop gibt es nur am Schreibtisch
(hingehen, E / X). Menüs lassen sich komplett mit dem Controller bedienen; die UI-Größe stellst du unter
**Pause → Einstellungen** ein.

## Was drin ist

- **Story:** Schicht in Kalles Imbiss (Teller an die richtigen Tische), Kündigung, eigene Garage
- **Kreislauf:** Einkauf → Kiste annehmen → Regal → Webshop → Bestellung → Kommissionieren →
  Verpacken → Label drucken → Versand
- **Handy (Tab / Y):** Bestellzettel mit Kundschaft, Frist und Express, Nachrichten mit Entscheidungen und
  Großauftrags-Angeboten, Schnell-Nachbestellen, Wochenziele, Status mit Engpass-Assistent
- **HustleOS-Laptop mit Apps und Reitern:** Übersicht („Warum läuft's nicht?“), Postfach, Einkauf (Ware,
  Verpackung), Retouren, Shop (Webshop, Marketing inkl. TikTok, Branding), Markt & Trends (Marktanalyse,
  Trendradar), Aufträge (Angebote, Laufend, Verlauf), Finanzen (Bank, Analytics, Trading), Firma (Team,
  Skills, Ausbau, Ziele & Wochenziele, Lifestyle)
- **v3.0:** Bestellzettel & Express, Retouren, Trends/Hype, Großaufträge (B2B), Hustle-Skills, Wochenziele,
  Kassenbon mit Wochentagen
- **Zwei Laptop-Designs, jeweils hell oder dunkel:** „Hype“ (Standard) und „Frachtbrief“ – umschaltbar unter
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
