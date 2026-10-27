# Asset-Lizenzen, Budget & Kaufliste

Stand: 2026-10-27 · Budget für spätere Lizenzkäufe: **500 €** · aktuell verplant: **0 €**

Kurzfassung: Alles, was im Spiel steckt, ist kommerziell nutzbar (CC0, CC BY, SIL OFL, Public Domain).
Kein einziges „nur privat“-Asset ist enthalten, deshalb sind Release- und Demo-Builds erlaubt und die Kaufliste ist leer.
Kostenpflichtige Packs mit Gratis-Version für den Privatgebrauch wurden recherchiert, aber nicht eingebaut – die
Download-Seiten (itch.io, Sketchfab, Unity Asset Store) sind aus der Build-Umgebung nicht erreichbar und verlangen
ein Konto bzw. das Akzeptieren von Bedingungen, was nur der Inhaber selbst tun darf (Details in Abschnitt 3).

## 1. Regeln (für Menschen und Agenten)

- **Gruppe a – frei (`"license_class": "free"`):** CC0 / Public Domain / CC BY / SIL OFL / MIT. Direkt einbauen, Eintrag in
  `CREDITS.md`, `Resources/credits.json` und Lizenztext in `Assets/DropshippingGame/ThirdParty/Licenses/<id>.txt`.
- **Gruppe b – nur privat (`"license_class": "personal_only"`):** Assets, deren Gratis-Version ausdrücklich nur für
  private/nicht-kommerzielle Nutzung erlaubt ist (z. B. itch.io „free for personal use“, Sketchfab CC BY-NC,
  Lite-/Trial-Versionen). Nur einbauen, wenn die Lizenz das ausdrücklich erlaubt – nie Paywalls umgehen, nie
  Bedingungen im Namen des Inhabers akzeptieren, die ein Konto oder eine Zahlung verlangen. Zusätzliche Felder im
  credits.json-Eintrag: `"commercial_price_eur": <Preis der kommerziellen Lizenz>`, `"commercial_license_url": "<Kaufseite>"`.
  Danach in Abschnitt 4 (Kaufliste) eintragen – die Summe muss im Budget von 500 € bleiben.
- **Release-Sperre:** Solange ein `personal_only`-Eintrag in `Resources/credits.json` steht, brechen **Release- und
  Demo-Builds** ab – mit Dialog (Menü-Build) bzw. Konsolenfehler (Batch-Build) und einer Liste der betroffenen Assets
  samt Preis. **Editor und Development-Builds bleiben erlaubt.** Fehlt `license_class`, wird am Lizenznamen erkannt
  (`NC`/`NonCommercial`/`personal` → gesperrt); eine fehlende oder kaputte credits.json sperrt ebenfalls.
  - Code: `Scripts/Core/AssetLicenses.cs` (Regeln, NUnit-Tests in `Tests/EditMode/AssetLicensesTests.cs`),
    `Scripts/Editor/LicenseGuard.cs` (Hook in `BuildTools.Build` + `IPreprocessBuildWithReport` als Sicherheitsnetz
    für Builds über das Unity-Build-Fenster). Prüfen von Hand: **Dropshipping › Build › Lizenzen prüfen**.
- **Nach dem Kauf** einer Lizenz: Rechnung/Lizenz-PDF ablegen (nicht im Repo, z. B. im Firmen-Drive), im
  credits.json-Eintrag `"license_class": "free"` setzen und `"license"` auf die gekaufte Lizenz ändern,
  Lizenzdatei in `ThirdParty/Licenses/` ersetzen, Zeile hier in Abschnitt 2 verschieben.

## 2. Was im Spiel steckt (alle kommerziell erlaubt)

Spalte „Preis kommerzielle Lizenz“ = was zusätzlich bezahlt werden müsste (bei freien Lizenzen 0).
„Wo verwendet“ = Ordner unter `Assets/DropshippingGame/Resources/` (Anzahl der Keys in `asset_manifest.json`).

| Asset | Autor | Quelle | Lizenz | kommerziell erlaubt? | Preis kommerzielle Lizenz (€) | Wo verwendet |
|---|---|---|---|---|---|---|
| **Tiny Treats – Collection 1 (11 Sets)** · neu v3.2 | Isa Lousberg (isalousberg.com) | https://tinytreats.itch.io (Dateien: github.com/series-ai/jam-ready-assets) | CC0 1.0 | ja | 0 | `Models/tiny_treats` (42 Keys: Briefkasten, Paket, Paketwaage, Pflanzen, Retro-Elektronik, Brunnen …) |
| **Free Low Poly Vehicles Pack (Bus)** · neu v3.2 | Raphael Gonçalves (Rgsdev) | https://rgsdev.itch.io/free-low-poly-vehicles-pack | CC0 1.0 | ja | 0 | `Models/rgsdev_vehicles/bus.fbx` (`vehicle.bus`, Stadtverkehr) |
| **Low Poly Vespa** · neu v3.2 | poki3d | https://poki3d.itch.io/low-poly-vespa | CC0 1.0 ⚠ vor Release auf der Originalseite bestätigen | ja | 0 | `Models/cc0_vespa/scooter_vespa.fbx` (`vehicle.moped`, `vehicle.scooter`) |
| Inter 4.1 | Rasmus Andersson | https://github.com/rsms/inter/releases/tag/v4.1 | SIL OFL 1.1 | ja | 0 | `Fonts/UI-Regular.ttf, Fonts/UI-Bold.ttf` |
| Bricolage Grotesque 1.001 | Mathieu Triay | https://github.com/ateliertriay/bricolage | SIL OFL 1.1 | ja | 0 | `Fonts/Display-Bold.ttf (ExtraBold)` |
| JetBrains Mono 2.304 | JetBrains | https://github.com/JetBrains/JetBrainsMono/releases/tag/v2.304 | SIL OFL 1.1 | ja | 0 | `Fonts/Mono-Regular.ttf, Fonts/Mono-Bold.ttf` |
| Comic Neue 2.51 | Craig Rozynski / Hrant Papazian | https://github.com/crozynski/comicneue | SIL OFL 1.1 | ja | 0 | `Fonts/Web-Comic-Regular.ttf, Fonts/Web-Comic-Bold.ttf` |
| Fredoka (variable, 2021) | Milena Brandão / Hafontia | https://github.com/hafontia/Fredoka-One | SIL OFL 1.1 | ja | 0 | `Fonts/Web-Round-Regular.ttf, Fonts/Web-Round-Bold.ttf` |
| Bangers 1.0 | Vernon Adams | https://github.com/googlefonts/bangers | SIL OFL 1.1 | ja | 0 | `Fonts/Web-Comic-Title.ttf` |
| Bebas Neue 2.000 | Ryoichi Tsunekawa (Dharma Type) | https://github.com/dharmatype/Bebas-Neue | SIL OFL 1.1 | ja | 0 | `Fonts/Sign-Condensed.ttf` |
| Pacifico 3.001 | Vernon Adams u. a. | https://github.com/googlefonts/Pacifico | SIL OFL 1.1 | ja | 0 | `Fonts/Sign-Script.ttf` |
| Saira Stencil One 1.004 | Hector Gatti, Omnibus-Type | https://github.com/Omnibus-Type/Saira | SIL OFL 1.1 | ja | 0 | `Fonts/Sign-Stencil.ttf` |
| Bungee 2.000 | David Jonathan Ross | https://github.com/djrrb/Bungee | SIL OFL 1.1 | ja | 0 | `Fonts/Sign-Retro.ttf` |
| Interface Sounds | Kenney | https://kenney.nl/assets/interface-sounds | CC0 1.0 | ja | 0 | `Audio/SFX (click, hover, notify, order, error, bad, phone_*, scanner)` |
| Impact Sounds | Kenney | https://kenney.nl/assets/impact-sounds | CC0 1.0 | ja | 0 | `Audio/SFX (drop, place, plate, pallet, metal_clank, step*)` |
| RPG Audio | Kenney | https://kenney.nl/assets/rpg-audio | CC0 1.0 | ja | 0 | `Audio/SFX (pickup, cash, coin, fold, door*, box_close, stamp, latch, step_tile/asphalt)` |
| Foley Sounds | Kenney | https://kenney.nl/assets/foley-sounds | CC0 1.0 | ja | 0 | `Audio/SFX (whoosh*)` |
| Casino Audio | Kenney | https://kenney.nl/assets/casino-audio | CC0 1.0 | ja | 0 | `Audio/SFX (box_open*, coin_2)` |
| Music Jingles | Kenney | https://kenney.nl/assets/music-jingles | CC0 1.0 | ja | 0 | `Audio/SFX (levelup*, jingle_*)` |
| Music Loops | Kenney | https://kenney.nl/assets/music-loops | CC0 1.0 | ja | 0 | `Audio/Music/diner.ogg` |
| Tape Rip 5 | Geoff-Bremner-Audio | https://freesound.org/s/788639/ | CC0 1.0 | ja | 0 | `Audio/SFX/tape.ogg` |
| printer.wav | lezaarth | https://freesound.org/s/232728/ | CC0 1.0 | ja | 0 | `Audio/SFX/printer.ogg` |
| Keyboard Typing | Trollarch2 | https://freesound.org/s/331656/ | CC0 1.0 | ja | 0 | `Audio/SFX/typing*.ogg` |
| Cash Register Purchase | zott820 | https://freesound.org/s/209578/ | CC0 1.0 | ja | 0 | `Audio/SFX/register.ogg` |
| NYC_street leve02l.wav | gezortenplotz | https://freesound.org/s/44796/ | CC BY 3.0 (Namensnennung Pflicht – steht in den Credits) | ja | 0 | `Audio/Ambience/street.ogg` |
| Restaurant Ambiance | stephan (SoundBible) | https://soundbible.com/1664-Restaurant-Ambiance.html | Public Domain | ja | 0 | `Audio/Ambience/diner.ogg` |
| Air Conditioner Fan Hum | LilMati | https://freesound.org/s/515859/ | CC0 1.0 | ja | 0 | `Audio/Ambience/warehouse.ogg` |
| Crickets Chirping At Night | Lisa Redfern (SoundBible) | https://soundbible.com/2083-Crickets-Chirping-At-Night.html | Public Domain | ja | 0 | `Audio/Ambience/night.ogg` |
| Birds | kvgarlic | https://freesound.org/s/156826/ | CC0 1.0 | ja | 0 | `Audio/Ambience/park.ogg` |
| Laundry On The Wire (Lazy Summer Lofi) | HoliznaCC0 | https://freemusicarchive.org/music/holiznacc0/ | CC0 1.0 | ja | 0 | `Audio/Music/menu.ogg` |
| Winter Lofi (4 Titel) | HoliznaCC0 | https://freemusicarchive.org/music/holiznacc0/winter-lofi | CC0 1.0 | ja | 0 | `Audio/Music/work_1..3.ogg, diner_2.ogg` |
| Warm Piano | Cody Bromley (Blankie) | https://freesound.org/s/859607/ | CC0 1.0 | ja | 0 | `Audio/Music/evening.ogg` |
| Poly Haven Texturen | Poly Haven | https://polyhaven.com/textures | CC0 1.0 | ja | 0 | `Textures/Surfaces/{asphalt,sidewalk,concrete_floor,shutter}` |
| ambientCG Texturen | ambientCG (Lennart Demes) | https://ambientcg.com | CC0 1.0 | ja | 0 | `Textures/Surfaces/<übrige>` |
| Poly Haven HDRIs | Poly Haven | https://polyhaven.com/hdris | CC0 1.0 | ja | 0 | `HDRI/day.hdr, sunset.hdr, night.hdr` |
| Car Kit 3.1 | Kenney | https://kenney.nl/assets/car-kit | CC0 1.0 | ja | 0 | `Models/kenney_car_kit` (30 Keys) |
| Mini Characters 1.0 | Kenney | https://kenney.nl/assets/mini-characters | CC0 1.0 | ja | 0 | `Models/kenney_mini_characters` (15 Keys) |
| Factory Kit | Kenney | https://kenney.nl/assets/factory-kit | CC0 1.0 | ja | 0 | `Models/kenney_factory` (19 Keys) |
| Mini Market | Kenney | https://kenney.nl/assets/mini-market | CC0 1.0 | ja | 0 | `Models/kenney_mini_market` (31 Keys) |
| Food Kit | Kenney | https://kenney.nl/assets/food-kit | CC0 1.0 | ja | 0 | `Models/kenney_food` (57 Keys) |
| City Kit (Commercial) | Kenney | https://kenney.nl/assets/city-kit-commercial | CC0 1.0 | ja | 0 | `Models/kenney_city_commercial` (41 Keys) |
| City Kit (Suburban) | Kenney | https://kenney.nl/assets/city-kit-suburban | CC0 1.0 | ja | 0 | `Models/kenney_city_suburban` (40 Keys) |
| City Kit (Roads) | Kenney | https://kenney.nl/assets/city-kit-roads | CC0 1.0 | ja | 0 | `Models/kenney_city_roads` (12 Keys) |
| City Kit (Industrial) | Kenney | https://kenney.nl | CC0 1.0 | ja | 0 | `Models/kenney_city_industrial` (25 Keys) |
| Mini Arcade | Kenney | https://kenney.nl | CC0 1.0 | ja | 0 | `Models/kenney_mini_arcade` (13 Keys) |
| Furniture Kit | Kenney | https://kenney.nl | CC0 1.0 | ja | 0 | `Models/kenney_furniture_kit` (73 Keys) |
| Cube Pets 2.0 | Kenney | https://kenney.nl | CC0 1.0 | ja | 0 | `Models/kenney_cube_pets` (13 Keys) |
| Modular Buildings | Kenney | https://kenney.nl | CC0 1.0 | ja | 0 | `Models/kenney_modular_buildings` (11 Keys) |
| KayKit City Builder Bits | Kay Lousberg | https://kaylousberg.itch.io/city-builder-bits | CC0 1.0 | ja | 0 | `Models/kaykit_city` (42 Keys) |
| KayKit Furniture Bits | Kay Lousberg | https://kaylousberg.itch.io/furniture-bits | CC0 1.0 | ja | 0 | `Models/kaykit_furniture` (56 Keys) |
| KayKit Restaurant Bits | Kay Lousberg | https://kaylousberg.itch.io/restaurant-bits | CC0 1.0 | ja | 0 | `Models/kaykit_restaurant` (41 Keys) |
| KayKit Prototype Bits | Kay Lousberg | https://kaylousberg.itch.io/prototype-bits | CC0 1.0 | ja | 0 | `Models/kaykit_prototype` (17 Keys) |

**Gruppe b (nur privat) im Spiel:** keine. **Summe kommerzieller Lizenzen: 0 €.**

## 3. Recherche Gruppe b (bezahlte Packs mit Gratis-Version) – Ergebnis

| Kandidat | Gratis-Version | Kommerzielle Lizenz | Stil passend? | Status |
|---|---|---|---|---|
| Synty POLYGON City Pack (https://syntystore.com/products/polygon-city-pack, Unity Asset Store) | keine Gratis-/Privat-Version | Unity Asset Store: 20 $ Listenpreis (im Okt. 2026 im Sale für 10 $); Synty-Store per Abo ab 30 $/Monat | nur bedingt (härterer Low-Poly-Look als Kenney/KayKit) | nicht eingebaut – keine Gratis-Version, Download nur mit Konto |
| Sketchfab-Modelle „CC Attribution-NonCommercial“ (z. B. Low-Poly-Fahrzeuge) | ja (nicht-kommerziell) | kein fester Preis, muss mit jedem Autor einzeln verhandelt werden | uneinheitlich | nicht eingebaut – sketchfab.com aus der Build-Umgebung nicht erreichbar, Download braucht Konto |
| itch.io-Packs „free for personal use“ (Fahrzeuge/Stadt) | je nach Pack | je nach Pack | – | Suche fand kein passendes Pack mit ausdrücklicher Privat-Klausel; die meisten Treffer sind ohnehin CC0/CC BY (Gruppe a). itch.io aus der Build-Umgebung nicht erreichbar |
| Mixamo-Animationen (Adobe) | kostenlos, auch kommerziell – aber nur mit Adobe-Konto | 0 € | Charaktere müssten auf Mixamo-Rig umgebaut werden | nicht eingebaut – Konto/Bedingungen müsste der Inhaber selbst akzeptieren |

Fazit: Für den aktuellen Stil (Kenney, KayKit, Tiny Treats – alles CC0 aus derselben „Toy“-Familie) bringt kein
bezahltes Pack einen klaren Qualitätssprung, der Stilbrüche rechtfertigt. Wer eines ausprobieren möchte: Gratis-Version
selbst herunterladen (nicht ins Repo committen, solange nicht klar ist, dass es bleibt), Entscheidung treffen, dann nach
Abschnitt 1 eintragen.

## 4. Kaufliste (Empfehlung)

| Asset | Preis kommerzielle Lizenz (€) | Begründung |
|---|---|---|
| – (nichts nötig) | 0 | Alle eingebauten Assets sind bereits kommerziell nutzbar. |
| **Summe** | **0 € von 500 €** | |

Freiwillig, nicht nötig (Dank an die CC0-Autoren, die den Großteil der Welt liefern): Spende/„Name your price“ an
Kenney (kenney.nl), Kay Lousberg (kaylousberg.itch.io) und Isa Lousberg (tinytreats.itch.io) – Höhe frei wählbar.

## 5. Offene Prüfpunkte vor Release

- [ ] **Low Poly Vespa (poki3d):** CC0-Angabe auf https://poki3d.itch.io/low-poly-vespa bestätigen (die Seite war aus der
      Build-Umgebung nicht erreichbar; die Angabe stammt aus der README des GitHub-Spiegels). Falls nicht CC0:
      `license_class` auf `personal_only` setzen (sperrt Release) und Ersatz suchen.
- [ ] **Rgsdev Bus:** Lizenzfeld auf https://rgsdev.itch.io/free-low-poly-vehicles-pack ist laut Suche „Creative Commons
      Zero v1.0 Universal“; die mitgelieferte LICENSE.txt sagt dasselbe – Stichprobe genügt.
- [ ] Stichprobe der übrigen Originalseiten wie in `CREDITS.md` beschrieben.

## 6. Nicht gefunden (bleibt prozedural)

Lastenrad, deutscher Briefkasten (gelb, `street.post_box`), Paketstation, Bushaltestelle, Smartphone-Modell,
PC-Tower, Mofa-/Roller-Motorgeräusch (CC0-Quellen wie freesound.org nicht erreichbar). Kandidaten aus Spiegeln mit
unklarer Herkunft (Asset-Store-Kopien in fremden GitHub-Repos, Microsoft-Remix-3D-Modelle, ein Sketchfab-„Standard“-
E-Bike) wurden bewusst **nicht** übernommen.
