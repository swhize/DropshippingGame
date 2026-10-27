# Release-Plan: Steam (Windows zuerst)

Stand: 27.10.2026 · Version laut [`VERSION`](../VERSION): **0.9.0** · Zielpreis 6–8 €
Bezug: [`MARKT_REFERENZEN.md`](MARKT_REFERENZEN.md) (Vermarktung, Streamer-Tauglichkeit, Koop als Update).

Legende: ✅ erledigt (im Repo) · 🔧 Technik offen · 👤 muss der Inhaber selbst tun (Konto, Geld, Recht, Entscheidung)

---

## 1. Checkliste

### Name & Marke
- 👤 **Titel festlegen.** Im Spiel, in GDD und Build heißt es „Dropshipping Simulator – Vom Imbiss zum Imperium“
  (`BuildTools.ProductName`). Intern taucht „Hustle Simulator“ als Arbeitstitel auf. Vor der Steam-Seite:
  Steam und Google nach gleichnamigen Spielen durchsuchen („Dropshipping Simulator“ ist ein naheliegender Name –
  Verwechslungsgefahr und schlechte Auffindbarkeit). Einen eindeutigen Namen nimmt man am besten **vor** der
  Store-Seite. Ändern heißt: `BuildTools.ProductName`, Wortmarke im Hauptmenü, Splash-Logo (`tools/branding`).
  **Achtung:** Produkt- und Firmenname bestimmen den Spielstand-Ordner – nach dem Release nicht mehr ändern.
- 👤 Parodie-Marken im Spiel prüfen (z. B. MediaMarkd, Revoluut, AllesExpress, TikTak, shopifly, PaketBlitz):
  Namen deutlich verfremdet, keine echten Logos/Farbkombinationen nachbauen. Im Zweifel kurz juristisch prüfen lassen.
- 👤 Icon und Splash-Logo ansehen (`Assets/DropshippingGame/Branding/`) – sie sind prozedural erzeugt
  (`tools/branding/make_branding.py`) und ein guter Platzhalter. Für die Steam-Kapseln lohnt sich ein Illustrator.

### Technik (im Repo)
- ✅ Build-Menü **Dropshipping › Build** (Windows Release/Development/Demo, Linux, macOS) + Kommandozeile (README).
- ✅ Version aus `VERSION` + Build-Nummer → `Application.version` (z. B. `0.9.0.412`); Details im Fehlerbericht.
- ✅ Release-Builds ohne Entwickler-Werkzeuge: Admin-Panel (F10/F9/^), Noclip (Alt), Vogelperspektive, Teleports
  sind nur im Editor, in Development-Builds oder mit Define `DS_DEV` aktiv (`Runtime/DevTools.cs`).
- ✅ IL2CPP x64 für Windows-Release, Mono für Development/Linux/macOS (Begründung in `BuildTools.cs`).
  IL2CPP braucht das Hub-Modul „Windows Build Support (IL2CPP)“ und Visual Studio 2022 mit „Desktop development
  with C++“. Fehlt es, baut das Werkzeug mit Warnung in Mono – das ist für Steam auch in Ordnung.
- ✅ Player-Einstellungen per Code (`ProjectSetup.ApplyPlayerSettings`): Firma „Hustle Games“, Produktname,
  Icon, Splash (dunkler Hintergrund + Wortmarke), randloses Vollbild in nativer Auflösung, Run in Background aus,
  Single Instance, Linear, Input System, Stacktraces nur für Fehler, IL2CPP Release, Stripping „Minimal“.
- ✅ Pausemenü › **Feedback & Bug melden**: kopiert Version + Systeminfos + letzte Fehler in die Zwischenablage,
  öffnet den Log-Ordner (Player.log). Kontaktziel in `BugReport.Contact` – 👤 ggf. Discord/E-Mail eintragen.
- ✅ Plattform-Schnittstelle für Erfolge (`Platform.UnlockAchievement(id)`), Steam-Implementierung hinter `DS_STEAM`.
- ✅ Demo-Modus `DS_DEMO` (5 Spieltage, Abschluss-Bildschirm „Auf die Wunschliste!“).
- 🔧 **Standard-Vollbild:** `Settings.Fullscreen` startet derzeit mit `false` (Fenster 1600×900) und überschreibt die
  Player-Einstellung. Für Steam empfohlen: Standard Vollbild, wenn noch keine Einstellung gespeichert ist.
- 🔧 **Englische Lokalisierung.** Das Spiel ist nur auf Deutsch. Der englischsprachige Markt ist ein Vielfaches
  größer – Englisch vor dem Release (mindestens UI + Story + Ereignisse) ist die wirkungsvollste Einzelmaßnahme.
  Bis dahin auf Steam ehrlich nur „Deutsch“ als unterstützte Sprache angeben.
- 🔧 Steam Deck: Controller-Bedienung ist da. Für „Verified“ fehlen: Bildschirmtastatur bei Texteingaben
  (Markenname → `SteamUtils.ShowGamepadTextInput` bzw. Floating Keyboard), Lesbarkeit der UI auf 1280×800 prüfen.
- 🔧 Optional: PlayStation-Symbole statt „A/B/X/Y“, wenn ein DualSense erkannt wird.
- 🔧 Streamer-Modus (siehe Abschnitt 6).

### QA vor jedem Upload
- Release-Build auf einem frischen Windows-Konto starten (kein Unity installiert): Start, neues Spiel, Imbiss-Schicht,
  Garage, 3 Tage spielen, speichern, beenden, laden. Alt-Tab, Auflösungswechsel, Controller ab-/anstecken.
- Prüfen, dass F10, F9, ^ und Alt **nichts** tun und das Pausemenü keinen „Admin-Panel“-Knopf zeigt.
- Mindest-Hardware testen (siehe Systemanforderungen), Player.log auf Fehler prüfen.
- Ordner `*_BackUpThisFolder_ButDontShipItWithYourGame` / `*_BurstDebugInformation_DoNotShip` dürfen nicht im
  Build-Ordner liegen (BuildTools verschiebt sie nach `Builds/_Symbols/` – dort für Crash-Analysen aufheben).

### Steamworks (👤)
1. Steamworks-Partnerkonto anlegen (partner.steamgames.com), Steuer- und Bankdaten, Identitätsprüfung.
2. Steam-Direct-Gebühr (100 US-$) pro Spiel zahlen → App-ID. Danach frühestens nach 30 Tagen veröffentlichen.
3. App-IDs in `Runtime/DemoMode.cs` → `SteamIds.FullGame` / `SteamIds.Demo` eintragen.
4. Store-Seite ausfüllen (Texte unten, Kapseln, Screenshots, Trailer), zur Prüfung einreichen (einige Werktage).
5. „Coming Soon“ möglichst früh live schalten – Wunschlisten sammeln (Steam verlangt mind. 2 Wochen vor Release).
6. Inhaltsfragebogen (Content Survey) ausfüllen; Altersfreigabe siehe Abschnitt 5.
7. Depots/Launch-Optionen anlegen (Abschnitt 3), Build hochladen, Release-Check durch Valve.
8. Erfolge im Backend anlegen (API-Namen = IDs im Code), Steam Cloud konfigurieren (Abschnitt 4).
9. Preis (6–8 €) und Regionalpreise (Valve-Empfehlung übernehmen), Launch-Rabatt 10–20 %.

---

## 2. Store-Texte (Entwurf)

### Deutsch

**Kurzbeschreibung (max. 300 Zeichen):**
> Vom Tellerträger zum Online-Mogul: Kündige im Imbiss, bau in deiner Garage ein Dropshipping-Business auf und
> pack, kleb und verschick jedes Paket selbst. Webshop, TikTok-Hypes, Börse und Krypto – eine bissige
> Wirtschafts-Satire in der Ego-Perspektive.

**Beschreibung:**
> Kalles Imbiss, Spätschicht, Teller Nummer 400. Genug. Du kündigst, räumst die Garage frei und gründest deine
> eigene Marke. Ein Laptop, ein Packtisch, ein Labeldrucker – und ein Kopf voller Hustle.
>
> **Pack selbst an.** Ware bestellen, Kisten annehmen, Regale einräumen, Bestellungen kommissionieren, den richtigen
> Karton falten, Klebeband ratschen, Label drucken und ab zur Packstation. Jedes Paket ist ein kleiner Sieg.
>
> **Führ dein Business über HustleOS.** Webshop mit Preisen und Branding, Marketing und TikTok-Videos, die du selbst
> drehst, Lieferanten, Bank und Kredite, Personal, Ausbau bis zur Lagerhalle – alles auf deinem Laptop.
>
> **Reite die Trends – oder geh unter.** Produkte werden heiß und wieder kalt, Großkunden wollen Paletten, Retouren
> stapeln sich, ein Shitstorm rollt an. Und wer zu viel Geld übrig hat, verzockt es an der Börse oder in Krypto.
>
> **Vom Imbiss zum Imperium.** Firmenlevel, Skills, Wochenziele, Ereignisse, Straßenfest und Pitch Day vor drei
> Juroren. Schaffst du 100.000 € Umsatz – und hört der Hustle dann überhaupt auf?

**Feature-Liste:**
- Echte Handgriffe statt Menüs: kommissionieren, verpacken, etikettieren, versenden
- HustleOS-Laptop mit Webshop, Marketing, Bank, Trading, Personal und Ausbau
- Selbst gedrehte TikTok-Videos – gutes Licht, Hook und Deko entscheiden über den Hype
- Trends, Großaufträge, Retouren, Shitstorms und über 20 Ereignisse
- Börse und Krypto im Finanzviertel (nur Spielgeld)
- Lebendige Stadt mit Verkehr, Passanten, Müllabfuhr, Tag-Nacht-Wechsel
- Satirisch, bunt, nimmt sich selbst nicht ernst
- Komplett mit Controller spielbar · 3 Spielstände · Auto-Save
- Streamer-freundlich: alle Musik CC0/gemeinfrei

### English

**Short description:**
> From diner waiter to online mogul: quit your job, turn your garage into a dropshipping business and pick, pack,
> tape and ship every parcel yourself. Web shop, TikTok hypes, stocks and crypto – a cheeky first-person business satire.

**Description:**
> Kalle's Diner, late shift, plate number 400. Enough. You quit, clear out the garage and start your own brand.
> One laptop, one packing table, one label printer – and a head full of hustle.
>
> **Do the work.** Order stock, receive crates, fill your shelves, pick orders, fold the right box, rip the tape,
> print the label and drop it off. Every parcel is a small victory.
>
> **Run it all on HustleOS.** Your web shop with prices and branding, marketing and TikTok videos you film yourself,
> suppliers, bank and loans, staff and upgrades all the way to a warehouse – right on your laptop.
>
> **Ride the trends – or go under.** Products get hot and go cold, business clients want pallets, returns pile up,
> a shitstorm is coming. And if there's cash left over, gamble it on stocks or crypto.
>
> **From diner to empire.** Company levels, skills, weekly goals, events, a street festival and a pitch day in front
> of three judges. Can you reach €100,000 in revenue – and does the hustle ever stop?

**Features:**
- Hands-on work instead of menus: pick, pack, label, ship
- HustleOS laptop: web shop, marketing, banking, trading, staff, upgrades
- Film your own TikTok videos – light, hook and props make the hype
- Trends, bulk contracts, returns, shitstorms and 20+ events
- Stocks and crypto in the finance district (in-game money only)
- A living town with traffic, pedestrians, garbage trucks and a day-night cycle
- Satirical, colourful, never takes itself seriously
- Full controller support · 3 save slots · autosave
- Streamer-friendly: all music is CC0 / public domain

(Englische Texte erst verwenden, wenn das Spiel auf Englisch spielbar ist – sonst „Sprachen: Deutsch“ klar angeben.)

**Tags (max. 20, wichtigste zuerst):** Simulation, Management, Economy, First-Person, Singleplayer, Funny, Satire,
Capitalism, Shop Keeper, Life Sim, Trading, Casual, Colorful, Stylized, 3D, Sandbox, Building, Comedy, Relaxing,
Controller.

### Systemanforderungen (Schätzung – vor Release auf echter Hardware prüfen)

| | Minimum | Empfohlen |
|---|---|---|
| OS | Windows 10 64-bit | Windows 10/11 64-bit |
| CPU | Intel Core i5-4590 / AMD Ryzen 3 1200 | Intel Core i5-8400 / AMD Ryzen 5 2600 |
| RAM | 8 GB | 16 GB |
| Grafik | GTX 960 (2 GB) / RX 470, DirectX 11 | GTX 1060 6 GB / RX 580 8 GB |
| Speicher | 2 GB | 2 GB |

Mit Grafik-Einstellungen „Niedrig“ (Schatten/SSAO reduziert) testen, bevor das Minimum veröffentlicht wird.

### Grafiken für Steam (Pflichtgrößen, Stand Steamworks-Doku 2025 – vor Upload dort gegenprüfen)

| Asset | Größe | Hinweis |
|---|---|---|
| Header Capsule | 920 × 430 | Logo gut lesbar, oben auf der Store-Seite |
| Small Capsule | 462 × 174 | Logo muss klein lesbar sein |
| Main Capsule | 1232 × 706 | Startseite/Empfehlungen |
| Vertical Capsule | 748 × 896 | Saisonale Aktionen |
| Page Background | 1438 × 810 | optional, ruhiges Motiv |
| Library Capsule | 600 × 900 | Bibliothek (Hochformat) |
| Library Header | 920 × 430 | |
| Library Hero | 3840 × 1240 | ohne Text/Logo |
| Library Logo | 1280 × 720 (max.) | transparentes PNG, liegt über dem Hero |
| Screenshots | mind. 5, 1920 × 1080 | echte Spielszenen, kein UI-Overlay-Spam |
| Trailer | 1920 × 1080, MP4 | die ersten 5 Sekunden zeigen die Kernhandlung |
| Community-Icon | 184 × 184 | JPG |
| Client-Icon | .ico (16/32/48/256) | aus `Branding/AppIcon.png` |
| Erfolgs-Icons | 256 × 256 (je freigeschaltet + gesperrt) | |
| Event-/News-Bilder | 800 × 450 (Cover), 1920 × 622 (Header) | |

Screenshot-Motive: Packtisch mit Klebeband, Laptop-HustleOS, TikTok-Dreh mit Ringlicht, Lagerhalle mit Personal,
Börsencrash, Straßenfest, Imbiss-Chaos, Abendstimmung in der Stadt.

---

## 3. Build & Upload (SteamPipe)

1. **Dropshipping › Build › Windows (Release)** → `Builds/Windows-Release/DropshippingSimulator.exe`
   (oder per Kommandozeile mit `-dsOut`). Die Konsole zeigt Größe, Dauer, größte Assets.
2. Steamworks SDK (ContentBuilder) herunterladen; `app_build_<APPID>.vdf` anlegen:

```
"AppBuild"
{
    "AppID" "<APPID>"
    "Desc" "0.9.0.<Build>"
    "ContentRoot" "<Repo>\Builds\Windows-Release\"
    "BuildOutput" "<Repo>\Builds\_steampipe\"
    "Depots"
    {
        "<DEPOTID>"
        {
            "FileMapping" { "LocalPath" "*" "DepotPath" "." "recursive" "1" }
            "FileExclusion" "*.pdb"
            "FileExclusion" "*DoNotShip*"
            "FileExclusion" "*DontShip*"
        }
    }
}
```

3. `steamcmd +login <user> +run_app_build <pfad>\app_build_<APPID>.vdf +quit`
4. Im Backend den Build auf den Branch `beta` setzen, selbst testen, dann auf `default`.
5. Launch-Option: `DropshippingSimulator.exe`, Betriebssystem Windows, 64-bit.
6. Demo: eigener Build **Windows (Demo)** (`DropshippingSimulatorDemo.exe`), eigene App-ID/Depot.

Branches: `default` (öffentlich), `beta` (Tester), optional `dev` mit Development-Build (Passwort).

---

## 4. Steamworks-Integration

Ziel: Erfolge, Overlay, Steam Cloud – ohne dass das Projekt ohne Steam kaputtgeht.

**Aufbau (im Repo):**
- `Runtime/Platform/Platform.cs` – `IPlatformServices` + statische Fassade `Platform`:
  `Platform.UnlockAchievement("ACH_FIRST_SALE")`, `Platform.SetStat(id, wert)`, `Platform.IsAchievementUnlocked(id)`,
  Ereignis `Platform.AchievementUnlocked`. Ohne Steam gilt `OfflinePlatform` (tut nichts, merkt sich die Sitzung).
  Alle Aufrufe sind sicher (auch vor Initialisierung, ohne Steam, bei Fehlern).
- `Runtime/Platform/Steam/SteamPlatform.cs` + `DropshippingGame.Steam.asmdef` – nur mit Define `DS_STEAM`
  (Define-Constraint der Assembly + `#if DS_STEAM`). Ruft `RestartAppIfNecessary`, `SteamAPI.Init`, `RunCallbacks`
  pro Frame, `SetAchievement` + gebündeltes `StoreStats`, `Shutdown` beim Beenden.

**Für das Erfolgssystem (Balance-Agent):** Erfolge im eigenen Spielstand speichern und beim Freischalten
`Platform.UnlockAchievement(id)` aufrufen; nach dem Laden eines Spielstands alle freigeschalteten IDs noch einmal
melden (holt offline erspielte Erfolge nach). IDs: `ACH_` + Großbuchstaben, identisch mit dem API-Namen im Backend.

**Einschalten (👤, wenn die App-ID da ist):**
1. Window › Package Manager › + › *Add package from git URL*:
   `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net` (Version ≥ 2025.161 –
   ältere SDKs bräuchten noch `RequestCurrentStats`).
2. Menü **Dropshipping › Build › Steam-Integration an/aus (DS_STEAM)**.
3. App-ID in `SteamIds.FullGame` eintragen; für Tests im Editor `steam_appid.txt` mit der ID in den Projektordner
   (nicht ins Depot!). Steam muss laufen.
4. Lizenz: Steamworks.NET ist MIT → Lizenztext nach `ThirdParty/Licenses/steamworks_net.txt` und Eintrag in
   `CREDITS.md` + `Resources/credits.json`. Die Steamworks-SDK-DLLs fallen unter das Steamworks-SDK-Abkommen.

**Steam Cloud (Auto-Cloud, kein Code nötig):** Backend › Steam Cloud › Root `WinAppDataLocalLow`,
Unterordner `Hustle Games/Dropshipping Simulator/saves`, Muster `savegame_*.json`. Root-Overrides:
Linux `LinuxXdgConfigHome` + `unity3d/Hustle Games/Dropshipping Simulator/saves`,
macOS `MacAppSupport` + `Hustle Games/Dropshipping Simulator/saves`. Kontingent z. B. 10 Dateien / 20 MB. (Root-Namen im Backend-Dropdown gegenprüfen.)
Einstellungen liegen in PlayerPrefs (Registry) und werden bewusst nicht synchronisiert.

**Erfolgs-Ideen (Vorschlag, 15–25 Stück):** erstes Paket, 100/1.000/10.000 Pakete, erste 5 Sterne, Kündigung bei
Kalle, Lagerhalle, erster Mitarbeiter, TikTok viral, Großauftrag pünktlich, Shitstorm überlebt, Börsencrash überlebt,
Krypto-Millionär (Spielgeld), Pitch Day gewonnen, 100.000 € Umsatz, Pleite gegangen (humorvoll), Straßenfest-Sieger.

---

## 5. Recht & Datenschutz

- **Credits/Lizenzen:** `CREDITS.md`, `Resources/credits.json` und `ThirdParty/Licenses/` sind deckungsgleich
  (geprüft: jede Credits-ID hat eine Lizenzdatei, JSON und Tabelle stimmen überein). Credits-Bildschirm im Spiel
  liest `credits.json` (Quelle/Lizenz verlinkt). Pflicht-Nennung CC BY 3.0 (Straßen-Ambiente) ist enthalten.
  Vor Release: Stichprobe der Originalseiten (siehe Hinweis in CREDITS.md), Steamworks.NET ergänzen, wenn genutzt.
  Eigene Schrift-Nutzung im Logo (Bricolage Grotesque, Inter – OFL) ist erlaubt.
- **Engine:** Unity-Lizenz prüfen (Personal bis 200.000 $ Umsatz/Jahr). Ob der „Made with Unity“-Splash
  abschaltbar ist, hängt von Lizenz und Version ab – im Editor unter Player › Splash Image nachsehen.
  Die Build-Werkzeuge lassen die Unity-Logo-Einstellung unverändert.
- **Datenschutz:** Das Spiel sendet keine Daten (kein Analytics-Paket, keine Web-Anfragen; Fehlerberichte nur
  manuell über die Zwischenablage). Kurze Datenschutzerklärung für Store/Website: „Das Spiel erhebt und überträgt
  keine personenbezogenen Daten. Kauf, Erfolge und Cloud-Speicher laufen über Steam (Datenschutz von Valve).“
  👤 Unity-Dashboard: Cloud Diagnostics/Analytics für das Projekt aus lassen.
- **EULA:** Für Steam genügt das Steam-Abonnementvertrag-Standard-EULA. Eigene EULA nur, wenn wirklich nötig
  (z. B. Mods). 👤 Impressum auf der eigenen Website/Presskit (Pflicht bei gewerblichem Angebot in DE).
- **Altersfreigabe / Inhaltsfragebogen:** Satire auf Dropshipping/Influencer-Kultur, gelegentlich derbe Sprache,
  Rauchen/Bier nur als Erwähnung. **Börse und Krypto** sind Wirtschaftssimulation mit Spielgeld – kein Echtgeld,
  keine Lootboxen, keine Käufe im Spiel: im Fragebogen bei „simuliertes Glücksspiel“ ehrlich angeben, falls Fragen
  zu Zufalls-/Spekulationsmechaniken kommen, und im Store-Text „nur Spielgeld“ betonen. Erwartung: USK 6–12 /
  PEGI 7–12. 👤 Für Deutschland eine IARC/USK-Einstufung über das Steamworks-Backend beantragen, falls angeboten.

---

## 6. Vermarktung & Launch-Plan

Leitlinie aus der Marktrecherche: Steam ist voll mit Simulator-Klonen; Erfolg kommt über früh gesammelte
Wunschlisten, eine Demo im Next Fest, Streamer-Tauglichkeit und regelmäßige Updates.

| Wann | Was |
|---|---|
| **sofort** | Name klären, Steamworks-Konto, App-ID, Store-Seite „Coming Soon“ mit 5+ Screenshots und kurzem Gameplay-Trailer |
| laufend | Wunschlisten sammeln: kurze Clips (Packen, TikTok-Dreh, Börsencrash, Shitstorm) auf TikTok/Shorts/Reddit (r/simulationgames, r/IndieGaming), Devlog-Posts auf Steam |
| -10 Wochen vor Next Fest | Anmeldung zum Next Fest (Fristen auf partner.steamgames.com), Demo-Build hochladen und prüfen lassen |
| -4 Wochen | Demo vorab freischalten (Feedback + Wunschlisten), Presse/Streamer-Mails mit Steam-Keys der Demo |
| **Steam Next Fest** | Februar, Juni oder Oktober – **jedes Spiel darf nur einmal teilnehmen**, also erst mit polierter Demo. Realistisch: Juni 2027 |
| 1–3 Monate danach | Release (oder Early Access), Launch-Rabatt, Streamer-Keys |
| nach Release | Update-Rhythmus (unten), Rabattaktionen ab ~3 Monaten |

**Demo (`DS_DEMO`, umgesetzt):** Menü **Build › Windows (Demo)**. Begrenzung: die ersten 5 Spieltage
(`DemoMode.DayLimit`), danach Abschluss-Bildschirm mit Bilanz und „Auf die Wunschliste!“ (öffnet die Store-Seite,
im Steam-Client per `steam://store/<id>`). Eigener Produktname „Dropshipping Simulator Demo“ → eigener
Spielstand-Ordner. Weitere Speicherungen nach Tag 5 gibt es nicht, weil der nächste Tag nie startet.
Zu prüfen 👤: Sind 5 Tage genug „Aha“ (Garage, erste Bestellungen, TikTok)? Ggf. 6–7 Tage.

**Early Access – abwägen:**
- Für: früher Umsatz und Feedback, Genre-Spieler akzeptieren EA, Koop später als großes Update passt ins EA-Modell.
- Gegen: kleines Team, Erwartung regelmäßiger Updates; schlechte EA-Reviews hängen lange nach; der Kern ist
  bereits vollständig (Ziel 100.000 €) – ein fertiger 1.0-Release mit klarer Update-Roadmap ist ehrlicher.
- Empfehlung: **Vollversion 1.0** auf Deutsch+Englisch, wenn Englisch rechtzeitig fertig ist; sonst EA mit
  „Englisch kommt in Update X“ und offen kommunizierter Roadmap.

**Roadmap nach Release (öffentlich auf der Store-Seite):**
1. Woche 1–2: Hotfixes aus Fehlerberichten (Pausemenü › Feedback), Balance.
2. Monat 1: Komfort-Update (Wünsche aus Reviews), Steam Deck „Verified“.
3. Monat 2–3: Inhalts-Update (Endlos-Modus/Endgame-Ziele, neue Produkte/Events, weitere Erfolge).
4. **Großes Koop-Update (1–4 Spieler)** – größter Wachstumstreiber im Genre. Spielzustand liegt schon zentral in
   `Sim`; Netzwerk z. B. Netcode for GameObjects + Steam-Transport. Eigenes Marketing-Event (Trailer, Rabatt).
5. Später: Workshop/Mods, weitere Sprachen.

**Streamer-Modus (Spezifikation, noch nicht umgesetzt – gehört in die Einstellungen):**
- Schalter „Streamer-Modus“ unter Einstellungen › Oberfläche.
- Musik: Alle Stücke sind CC0/gemeinfrei, also unkritisch. Trotzdem Option „nur prozedurale Musik/keine Musik“,
  falls Content-ID-Systeme fälschlich anschlagen (CC0-Tracks werden manchmal von Dritten registriert).
- Namen: Parodie-Marken optional durch neutrale Namen ersetzen; vom Spieler eingegebene Namen (Marke) bleiben.
- Steam-Spielername und Spielstand-Pfade nirgends anzeigen (Fehlerbericht-Seite im Streamer-Modus einklappen).
- Keine Einblendungen mit externen Links/Overlays während Aufnahme.

**Presskit (👤):** eine Seite mit Logo (SplashLogo.png), Icon, 8 Screenshots, Trailer, Kurztext DE/EN,
Kontakt, Impressum. Steam-Keys über das Steamworks-Key-Tool, Anfragen über Keymailer/Lurkit prüfen.

---

## 7. Was der Inhaber selbst tun muss (Kurzfassung)

1. Titel endgültig festlegen und auf Kollisionen prüfen.
2. Steamworks-Konto, Steuer/Bank, 100 $ Gebühr, App-IDs (Vollspiel + Demo) in `SteamIds` eintragen.
3. Icon/Logo prüfen, Steam-Kapseln + Trailer erstellen (lassen).
4. Englisch-Lokalisierung beauftragen/entscheiden; bis dahin Sprache „Deutsch“.
5. Unity 6.3 mit IL2CPP-Modul + Visual Studio C++ installieren, Release-Build bauen, auf fremdem PC testen.
6. Steamworks.NET installieren, `DS_STEAM` einschalten, Erfolge + Steam Cloud im Backend anlegen.
7. Next-Fest-Termin wählen, Demo bauen und anmelden.
8. Datenschutztext + Impressum, Altersfragebogen, Preis.
