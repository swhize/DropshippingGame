# Art Direction – Dropshipping Simulator

Ziel: Das Spiel darf **nicht nach Unity-Standard aussehen** (Hauptkritik an Schedule I, siehe `MARKT_REFERENZEN.md` Punkt 5).
Wir haben keine Künstler-Pipeline, also entsteht der Look aus Code: Palette, Licht, Farbkorrektur, Materialregeln und
Schriften. Alles Einstellbare steht zentral in `Scripts/Runtime/Rendering/ArtStyle.cs`.

## 1. Leitbild: warmer, satirischer Comic-Realismus

- **Stilisiert, nicht fotorealistisch:** klare Formen, abgerundete Kanten (Bevel), lesbare Silhouetten, leicht überhöhte
  Farben. Realistische PBR-Oberflächen (Asphalt, Ziegel, Putz) nur als Untergrund – die Figuren und Requisiten bleiben
  Spielzeug-haft.
- **Warm:** Die Stadt ist eine sympathische, leicht abgenutzte Kleinstadt. Licht ist eher golden als klinisch-blau.
  Kühle Töne nur in Schatten, Nacht und Finanzviertel (Satire: dort ist es „kalt“).
- **Satirisch:** Werbung, Schilder und Marken sind laut und bunt (Ladenschilder, Plakatwand, Fake-Webseiten), die
  Umgebung darunter ist ruhig. Witz kommt aus Texten und Details, nicht aus grellen Farben überall.
- **Nichts ist neu:** Jede Fläche hat leichte Farbvariation, Fassaden haben Schmutzfahnen unter Fensterbänken,
  Straßen werden bei Regen nass. Perfekt einfarbige Flächen sind der typische „Default-Look“ – vermeiden.

## 2. Palette

### Marke & Oberfläche (identisch in Code und USS)

| Token | Farbe | Verwendung |
|---|---|---|
| `--brand-gold` / `ArtStyle.BrandGold` | `#FFBD42` | Hauptakzent: Aktionen, Fokus, Plakat im Hauptmenü, Hervorhebung beim Anvisieren |
| `--brand-ink` / `ArtStyle.Ink` | `#161616` | Comic-Tinte: Konturen, Schrift auf hellem Grund (nie `#000`) |
| `--brand-paper` / `ArtStyle.Paper` | `#FFFDF6` | Papier: helle Flächen, Kassenbon, Browser-Seiten (nie `#FFF` als Fläche) |
| `--brand-cream` / `--brand-sand` | `#F4F1EA` / `#E9E4D6` | Tablet-/Browser-Rahmen |
| `--brand-yellow` | `#FFD23F` | Hover/Highlights in den Fake-Webseiten |
| `--brand-red` / `ArtStyle.SignalRed` | `#E63312` | Badges, Sale, Warnungen |
| `--brand-money-pos` / `--brand-money-neg` | `#D8F5C8` / `#FFD0C8` | Geld-Chips (Kontostand) |
| `--good` / `--bad` / `--teal` | `#66DE8C` / `#FF6B6B` / `#52DBC7` | gut/schlecht/Info (Farbenblind-Modus: Blau/Orange, Klasse `.cb`) |

Regeln: höchstens **drei Akzentfarben** pro Ansicht (Gold = Aktion, Mint/Grün = gut, Rot = schlecht). Die
HustleOS-Themen (Laptop/Handy, `.os-theme.hype/.fb × .dark/.light`) definieren `--ink/--paper/--accent …` selbst, die
`--brand-*`-Tokens bleiben aber überall gleich. Fake-Webseiten dürfen eigene Markenfarben haben (Satire), nutzen für
Tinte, Papier, Badges und Geld-Chips aber die Tokens.

### Stadt je Viertel (`ArtStyle.Walls*`)

| Viertel | Stimmung | Wandfarben |
|---|---|---|
| Geschäftsstraße (Hauptstraße Nord/Süd) | Altbau, warm, belebt | Ocker, Terrakotta, Creme, Salbei, Lachs |
| Wohnviertel (Lindenweg) | Pastell, freundlich | Mint, Puderblau, Zitrone, Flieder, Pfirsich, Altweiß |
| Finanzviertel (Bank, Börse) | kühl, Stein und Glas | Schiefer, Sandstein, Stahlblau |
| Einkaufsviertel (Elektromarkt, Tierladen) | Marken-bunt, Flachbau | eigene Markenfarben der Läden |

Ladenschilder (`ArtStyle.SignColors`): kräftig, aber nicht rein gesättigt; nachts **hinterleuchtet** (`Mats.SignGlow`).
Dächer: Ziegelrot, Anthrazit, Braun, Schieferblau.

### Licht (`ArtStyle.Light*`)

| Licht | Farbe | Einsatz |
|---|---|---|
| `LightWarm` (~2700 K) | `1.00 / 0.80 / 0.58` | Imbiss, Wohnungen, Practicals |
| `LightWork` (~3500 K) | `1.00 / 0.90 / 0.76` | Garage, Lagerhalle (warm-neutral statt Büro-Blau) |
| `LightShop` (~4000 K) | `1.00 / 0.94 / 0.85` | Läden, Bank, Post |
| `LightStreet` | `1.00 / 0.78 / 0.50` | Straßenlaternen (Natrium-Anmutung) |

## 3. Lichtstimmungen über den Tag

Spieltag 08:00–20:00, Intro 17:30. Stützstellen in `ArtStyle.Moods`, dazwischen weich überblendet; Sonne flach über
`ArtStyle.SunElevation` (lange Goldene Stunde). Jede Stimmung setzt Sonne, Umgebungslicht (Trilight Himmel/Horizont/Boden),
Nebelfarbe und -dichte, Himmelstönung und die Farbkorrektur (Weißabgleich, kühle Schatten / warme Lichter, Sättigung).

| Zeit | Stimmung | Sonne | Nebel | Farbkorrektur |
|---|---|---|---|---|
| 06:30 Dämmerung | rosa-violett | flach, orange, schwach | dicht, rosig | leicht warm |
| 08:00 Morgen | frisch, leicht diesig | warmweiß 1,1 | hell, diesig | neutral-warm, Schatten kühl |
| 12:00 Mittag | klar, neutral | fast weiß 1,45 | dünn, bläulich | neutral, +10 Sättigung |
| 15:30 Nachmittag | sonnig, wärmer | gelblich 1,35 | dünn | +3 warm |
| 17:45 Goldene Stunde | **Hero-Stimmung** | tief, gold 1,25 | goldener Dunst | +8 warm, Schatten blau, Lichter gold |
| 19:00 Sonnenuntergang | rot-violett | rot, schwach | rosig | warm, satt |
| 20:00 Blaue Stunde | Laternen gehen an | Mondlicht | blau-violett | kühl (−6) |
| Nacht | Blau + Natrium-Inseln | Mond 0,2 | dunkelblau | kühl (−10), Lichter neutral |

Nacht (`ArtStyle.NightAmount`, ab ca. 19:10): Fenster und Ladenschilder leuchten, Laternen bekommen Lichtkreise am Boden
(additive Fläche statt echtem Licht für ferne Laternen), echte Punktlichter nur nahe der Kamera (`CityLights`).

### Wetter (`Rendering/Weather.cs`, nur Optik)

Pro Spieltag deterministisch aus Stadt-Seed + Tag: ca. 55 % sonnig (manchmal morgens noch nass), 25 % bewölkt,
20 % leichter Regen in einem Zeitfenster (1,5–4 h), danach trocknen die Straßen in ~2,5 h ab. Tag 1–2 und das
Hauptmenü sind immer sonnig. Bewölkt = Sonne schwächer und entsättigt, weiches graues Umgebungslicht, dichterer Nebel,
keine Sonnenscheibe, −14 Sättigung. Nass = dunklerer Belag, glänzender Wasserfilm, Pfützen, die Himmel und Laternen
spiegeln. Regen = ein Partikelsystem um die Kamera (nicht in Gebäuden). Abschaltbar: Einstellungen › Grafik ›
„Wechselndes Wetter“ (`Settings.Weather`).

## 4. Materialregeln (`Mats`, `TexGen`)

- **Kein reines Schwarz/Weiß:** Albedo-Kanäle 0,025–0,94 (`ArtStyle.SafeAlbedo`), wird in `Mats.Std` erzwungen.
  Reines Weiß brennt im ACES-Tonemapping aus, reines Schwarz frisst Formen.
- **Rauheit 0,08–0,97** (`ArtStyle.RoughnessMin/Max`). Richtwerte: Stoff/Putz/Karton 0,85–0,95; Holz 0,7–0,8;
  Lack/Plastik 0,4–0,6; Metall gebürstet 0,3–0,5 (metallic 0,6–0,85); Glas/Pfützen 0,03–0,1.
- **Subtile Farbvariation:** Jedes einfarbige `Mats.Std`-Material bekommt eine fast weiße Detailtextur
  (`TexGen.Grunge`, 2,5-m-Kachel in Weltmetern, ±6 % Helligkeit, ±1,5 % warm/kalt). Kleine Objekte bleiben dadurch
  ruhig, große Flächen wirken „benutzt“.
- **Kantenabnutzung:** Kanten sind über `Props.Box`-Bevel abgerundet und fangen Licht; Fassaden haben Schmutzfahnen
  und Flecken (`TexGen.FacadeWear`), Fenster teils Vorhänge, nachts warmes/neutrales/TV-blaues Licht.
- **Leuchten:** Emission nur für echte Lichtquellen (Lampen, Neon, Displays, Fenster nachts, Schilder nachts).
  Energie > 1 nur, wenn es im Bloom glühen soll.
- **Nässe:** nur über `Mats.SetWetness` (Asphalt/Gehweg dunkler, Wasserfilm, Pfützen) – keine eigenen Glanz-Hacks.

## 5. Typografie

| Rolle | Schrift | Datei |
|---|---|---|
| UI-Text | Inter Regular/Bold | `Fonts/UI-*` |
| Überschriften, Wortmarke | Bricolage Grotesque ExtraBold | `Fonts/Display-Bold` |
| Zahlen, Kassenbon, Code | JetBrains Mono | `Fonts/Mono-*` |
| Ladenschilder (3D, `SignFont`) | Bebas Neue (Condensed), Pacifico (Script), Saira Stencil, Bungee (Retro) | `Fonts/Sign-*` |
| Fake-Webseiten | Comic Neue, Bangers, Fredoka | `Fonts/Web-*` |

Regeln: höchstens zwei Schriften pro Ansicht (+ Mono für Zahlen); Schilder in der Welt immer Großbuchstaben oder
Script, nie UI-Schrift; Kontrast Text/Fläche mindestens 4,5 : 1 (`Theme.OnColor`).

## 6. Einheitliche Oberfläche

- HUD, Menüs, Fenster: dunkles Glas (`--glass`), Gold-Akzent, Radien 16/10/999 px, 4/8-px-Raster (`Game.uss`).
- Handy + Laptop („HustleOS“): eigene Themen, gleiche Struktur, Akzent Hype-Orange bzw. Frachtbrief-Gelb.
- Browser/Tablet (`Web.uss`, `Tablet.uss`): Comic-Look mit dicken Tinten-Rändern; gemeinsame Farben über `--brand-*`.
- Neue Farben **nicht** hart in USS schreiben, wenn es ein Token dafür gibt; neue gemeinsame Farben als `--brand-*` in
  `.ds-root` ergänzen.

## 7. Hauptmenü-Einstellung

Goldene Stunde (18:10), Sonne im Rücken der Kamera (`Atmosphere.SunYawOverride`), langsame Fahrt auf dem südlichen
Gehweg mit Blick über die Straße auf die Plakatwand (Hustle-Gold, „DROPSHIPPING – Vom Imbiss zum Imperium.“) im
rechten Bilddrittel; links liegt das Menü über dem Verlauf, im Hintergrund Park und Skyline.

## 8. Leistung (Ziel: Mittelklasse-PC, 60 fps)

- Farbkorrektur (Weißabgleich, Shadows/Midtones/Highlights) steckt in der Color-Grading-LUT: praktisch kostenlos.
- Wenige echte Lichter: Innenräume 2–6 Punktlichter ohne Schatten, sonst emissive Leuchtkörper; Laternen-Lichter nur
  im Umkreis von 48 m, ferne Laternen nur als Lichtkreis (ein Quad, additiv, Unlit ohne Zusatz-Varianten).
- Regen: ein Partikelsystem (max. 2000 Partikel, Rate je Qualitätsstufe 500/900/1400), folgt der Kamera.
- Nasse Straßen: eine Handvoll großer transparenter Flächen + ~46 Pfützen-Quads, nur sichtbar wenn nass, erst beim
  ersten Regen gebaut.
- Fassaden-Verwitterung wird pro Seed einmal berechnet und gecacht.
