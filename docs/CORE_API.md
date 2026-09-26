# Core-API v3.0 – Vertrag für UI und Welt (Welle 2)

> Stand: v3.0 „Das große Update“, Spielstand-Version **4**. Alles hier ist reines C# in
> `Assets/DropshippingGame/Scripts/Core` (Namespace `DropshippingGame.Core`, ohne UnityEngine).
> Dieses Dokument listet **jede neue öffentliche Klasse, jedes neue Mitglied und jedes neue
> Ereignis** mit Signatur, Bedeutung und konkreten Hinweisen, welche App / welches HUD-Element /
> welche Station was braucht. Bestehende API bleibt vollständig erhalten (nur erweitert).
> Ausführlich getestet: `Assets/DropshippingGame/Tests/EditMode/*.cs` (103 Tests).

## Inhalt

1. [Grundregeln für die Unity-Schicht](#1-grundregeln-für-die-unity-schicht)
2. [Zeit, Wochentage, Formatierung](#2-zeit-wochentage-formatierung)
3. [F1 Bestellzettel](#3-f1-bestellzettel)
4. [F2 Retouren](#4-f2-retouren)
5. [F3 Trends / Hype](#5-f3-trends--hype)
6. [F4 Großaufträge (B2B)](#6-f4-großaufträge-b2b)
7. [F5 Hustle-Skills](#7-f5-hustle-skills)
8. [F6 Wochenziele](#8-f6-wochenziele)
9. [F7 Tagesabrechnung, Ereignisse, Texte, Ziele](#9-f7-tagesabrechnung-ereignisse-texte-ziele)
10. [Speichern / Laden (v4)](#10-speichern--laden-v4)
11. [Geänderte bestehende API und Design-Änderungen](#11-geänderte-bestehende-api-und-design-änderungen)
12. [Checkliste UI-Agent](#12-checkliste-ui-agent)
13. [Checkliste Welt-Agent](#13-checkliste-welt-agent)
14. [Nicht aus der UI aufrufen](#14-nicht-aus-der-ui-aufrufen)

---

## 1. Grundregeln für die Unity-Schicht

* **Zeit:** Alle Zeitpunkte (Fälligkeit, Retouren-Ankunft, Boosts …) laufen auf der
  Geschäftsuhr `Sim.BClock()` (Minuten seit Spielbeginn, nur 08–20 Uhr zählen, 720 pro Tag).
  Umrechnen in Uhrzeit: `sim.BClockText(b)` → `"11:30"` bzw. `"Di 11:30"`.
* **`EconomyChanged` bleibt der Sammel-Auslöser:** Jede Zustandsänderung ruft weiterhin
  `RaiseEconomyChanged()`. Wer nur „alles neu zeichnen“ will, braucht keine neuen Ereignisse.
  Die neuen, spezifischen Ereignisse (unten je System) sind für **Animationen, Sounds, Banner,
  Badges und Welt-Objekte** gedacht.
* **Zufall:** Die `Sim` hat einen seedbaren `Rng`. Alle Abfragen für Anzeigen (Prognosen,
  Countdowns, Kosten, Zustände) verbrauchen **keinen** Zufall. Methoden, die Zufall verbrauchen
  oder Spielzustand erzeugen (Angebote, Retouren, Bestellungen …), stehen in
  [Abschnitt 14](#14-nicht-aus-der-ui-aufrufen) – bitte nicht aus der UI aufrufen.
* **Texte:** Alle Texte der Core sind Deutsch und fertig formatiert (`Fmt.Money`, `Fmt.Rating`,
  `Fmt.Duration` …). Icons, die die Core liefert, existieren alle in `UI/Icons.cs`.
* **Toasts:** Die Core meldet wichtige Dinge selbst über `Sim.Toast` (`Notify(text, kind)`, kind =
  `"info"`, `"good"`, `"bad"`). Die UI muss für die neuen Systeme keine eigenen Toasts bauen.

---

## 2. Zeit, Wochentage, Formatierung

**Tag 1 ist ein Montag.** Wochentage beeinflussen leicht die Nachfrage (Sa +10 %, Mo/Di −5 %).

| Mitglied | Bedeutung |
|---|---|
| `int Sim.Weekday` | 0 = Montag … 6 = Sonntag |
| `int Sim.Week` | Wochennummer ab 1 |
| `string Sim.WeekdayName()` / `string Sim.WeekdayShort()` | `"Montag"` / `"Mo"` |
| `int Sim.DaysLeftInWeek()` | Tage nach heute bis Sonntag (Sonntag = 0) |
| `float Sim.WeekdayDemand()` | Nachfrage-Faktor des Tages (in `DemandRate` enthalten) |
| `static int GameData.WeekdayOf(int day)` | Wochentag eines beliebigen Tages |
| `static string GameData.WeekdayName(int day)` / `WeekdayShort(int day)` | Namen für beliebige Tage |
| `static readonly string[] GameData.WeekdayNames`, `WeekdayShorts` | `"Montag"…"Sonntag"`, `"Mo"…"So"` |
| `static readonly float[] GameData.WeekdayDemand` | `{0.95, 0.95, 1, 1, 1.05, 1.1, 1}` |
| `static string Fmt.Duration(float minutes)` | Spieldauer: `"45 min"`, `"1:05 h"` |
| `string Sim.BClockText(float b)` | Geschäftsuhr → `"12:00"` (heute) oder `"Di 08:30"` |

**UI:** HUD oben links: `Tag 12 · Fr · 14:35` (`sim.Day`, `sim.WeekdayShort()`, `Fmt.Clock(sim.TimeMinutes)`).
Tagesbanner: `DayStarted` wie bisher, am Montag zusätzlich `WeekStarted` (siehe F6).

---

## 3. F1 Bestellzettel

Jede Bestellung ist ein **Zettel** mit Nummer, Kundschaft, Ort, Notiz, Fälligkeit und optional
**Express** (ab Level 2, 16–25 % der Bestellungen, +40 % Preis, Frist 100 statt 240 min,
Storno nach 360 statt 720 min, wird häufiger und 1,6-fach gewichtet bewertet).
Wartende Zettel liegen in `Sim.OrderQueue`, angefangene in `Sim.OrdersInWork`. **Artikel und Pakete
tragen die Zettel-Daten selbst** (`ItemData.OrderId`, `Customer`, `City`, `Express`, `DueAt`).

### Typen

```csharp
public enum OrderStage { Queued = 0, Picked = 1, Packed = 2, Labeled = 3, Conveyor = 4 }

public sealed class Order
{
    // bestehend
    public string Product; public int Price; public float Created;
    // neu
    public int Id;                 // ab 1001, fortlaufend
    public string Customer, City;  // "Sabine K.", "Bottrop"
    public string Note;            // Flavor-Notiz der Kundschaft
    public bool Express;
    public float DueAt;            // Fälligkeit (BClock)
    public OrderStage Stage;       // Bearbeitungsstand
    public float ExpiresAt { get; }   // Storno-Zeitpunkt, solange wartend
    public float DueWindow { get; }   // Länge der Frist in Minuten
    public bool InWork { get; }       // Stage != Queued
    public string Number { get; }     // "#1042"
    public ItemData ToItem(float quality);          // Artikel mit allen Zettel-Daten
    public static Order FromItem(ItemData item);    // Zettel aus Artikel/Paket rekonstruieren
}
```

`ItemData` (neue Felder, alle gespeichert, `Clone()` kopiert sie mit):

| Feld | Bedeutung |
|---|---|
| `int OrderId` | Zettel-Nummer (0 = keiner, z. B. Kiste oder alter Spielstand) |
| `string Customer`, `City` | fürs Versandlabel („An: Sabine K., Bottrop“) |
| `bool Express` | Express-Paket (Label rot/Blitz) |
| `float DueAt` | Fälligkeit (0 = unbekannt) |
| `int ContractId` | Einzelartikel für einen Großauftrag (siehe F4) |
| `int PackSize` | benutzte Kartongröße 0–2, −1 = passend zum Produkt |
| `string Note` | bei Retouren: Rücksendegrund |
| `int EffectivePackSize { get; }` | tatsächliche Kartongröße (für `ItemKit.PackageSizes[...]`) |
| `bool Oversized { get; }` | Karton zu groß (höheres Retourenrisiko) |
| `ItemData CopyTicketFrom(ItemData o)` | Zettel-Daten übernehmen |

### Sim-Mitglieder

| Signatur | Bedeutung |
|---|---|
| `List<Order> OrdersInWork` | angefangene Zettel (entnommen, verpackt, etikettiert, auf dem Band) |
| `int NextOrderId` | nächste Nummer |
| `List<Order> Tickets()` | **alle offenen Zettel** (wartend + in Arbeit), nach Fälligkeit sortiert – neue Liste pro Aufruf |
| `Order FindOrder(int orderId)` | Zettel suchen (wartend oder in Arbeit) |
| `float OrderTimeLeft(Order o)` | Minuten bis Fälligkeit (negativ = überfällig) |
| `float OrderUrgency(Order o)` | 0 frisch … 1 jetzt fällig … bis 3 überfällig (Farbverlauf/Balken) |
| `bool OrderOverdue(Order o)` | überfällig? |
| `string OrderTimeLeftText(Order o)` | `"45 min"`, `"1:30 h"`, `"überfällig (12 min)"` |
| `int ExpressPendingCount()` / `int OverdueCount()` | Zähler für Badges |
| `bool ExpressUnlocked` / `float ExpressChance()` | Express ab Level 2 / aktuelle Chance |
| `Order SpawnOrder(string id, bool express)` | neue Bestellung mit festem Express-Status (null = Warteschlange voll) |
| `void OnLabeled(ItemData pkg)` | **Labeldrucker** (neu, mit Paket): setzt Stand „etikettiert“ und ruft das alte `OnLabeled()` |
| `void DiscardItem(ItemData item)` | Gegenstand unwiederbringlich verloren → Zettel storniert (zählt als verloren) |
| `int CartonFor(string pid)` | welcher Karton benutzt würde (passend, sonst nächstgrößer; −1 = keiner) |
| `ItemData PickupCrateAt(int index)` | bestimmte Kiste vom Wareneingang nehmen |
| `int[] QuickReorderPlan(string pid)` | `{Mengen-Index, Lieferant}` für Schnell-Nachbestellen (letzter Einkauf, sonst 50 Stk. Standard) |
| `int QuickReorderCost(string pid)` | Kosten dafür |
| `bool QuickReorder(string pid)` | **Handy: Schnell-Nachbestellen** (gleiche Menge/Lieferant wie zuletzt) |
| `Dictionary<string,int[]> LastPurchase` | letzter Einkauf je Produkt |
| `Dictionary<string,float> LaunchOrders` | geplante „Neu im Shop“-Bestellungen |
| `float ExpressBoostUntil`, `ExpressBoostChance` | Ereignis „Express-Fieber“ |

### Ereignisse

| Ereignis | Wann | Nutzen |
|---|---|---|
| `event Action OrdersChanged` | Warteschlange oder Stand eines Zettels geändert | HUD-Zettel, Handy-Liste, Bestell-Monitor neu zeichnen |
| `event Action<Order> OrderReceived` | neuer Zettel | Zettel oben rechts einfliegen lassen, Sound (Express: auffälliger) |
| `event Action<Order> OrderExpired` | wartender Zettel storniert (oder Gegenstand verloren) | Zettel rot zerreißen/abfallen lassen |
| `event Action<string> OrderLost` | Warteschlange voll, Bestellung verloren (Produkt-ID) | kurzer Hinweis/Wackeln der Zettel-Leiste |
| `event Action<Order,int,bool> OrderShipped` | Paket zu Zettel verschickt (Zettel, Erlös, pünktlich) | Zettel abhaken, „Pünktlich!“ / „Zu spät“ |

`PackageShipped(ItemData, int, V3)` gibt es weiterhin (Welt: schwebender Betrag).

### Hinweise UI

* **HUD oben rechts – Bestellzettel:** `sim.Tickets()` (max. ~6 zeigen, Rest „+3“). Pro Zettel:
  Produkt-Icon/Farbe (`GameData.Product(o.Product)`), `o.Number`, `o.Customer`, Countdown
  `sim.OrderTimeLeftText(o)`, Farbe nach `sim.OrderUrgency(o)`, Blitz bei `o.Express`, kleiner
  Stand (`o.Stage`: Häkchen „entnommen“, Karton „verpackt“, Label „etikettiert“, Band). Neu
  zeichnen bei `OrdersChanged` und jede Sekunde für den Countdown.
* **Handy – Bestellungen:** Liste wie oben plus `o.Note` und Fälligkeit `sim.BClockText(o.DueAt)`.
  **Schnell-Nachbestellen:** je gelistetem Produkt Button „Nachbestellen (`Fmt.Money(sim.QuickReorderCost(id))`)“ → `sim.QuickReorder(id)`.
* `HudView.SetHeld`: Für `ItemKind.Item` mit `OrderId > 0` „für #1042 · Sabine K.“, bei
  `data.Express` Blitz; `ContractId > 0` „für Großauftrag“ (statt Preis 0 €); `ItemKind.Return`
  „Retoure: … – zum Retourenplatz“.

### Hinweise Welt

* **Labeldrucker:** `gm.OnLabeled(labeled)` statt `gm.OnLabeled()` aufrufen (Stand „etikettiert“).
  Das Label kann `Customer`/`City` und bei Express einen roten Streifen zeigen.
* **Pakete:** Größe über `data.EffectivePackSize` statt Produktgröße (Fallback-Karton ist größer).
* **Bestell-Monitor an der Wand:** `sim.Tickets()` + `OrdersChanged`.
* **Regal:** `PickItem` gibt jetzt den **dringendsten** Zettel des Produkts (Express zuerst) –
  keine Änderung am Aufruf nötig.
* Fällt ein Gegenstand aus der Welt und wird zerstört: `gm.DiscardItem(data)`.

---

## 4. F2 Retouren

Ein Teil der verschickten Pakete kommt zurück: Basis 3 %, Billig-Ware bis ×3,5, Premium ×0,3,
zu großer Karton ×1,6, verspätet ×1,5, Skill „Kundenflüsterer“ ×0,65, ±20 % Zufall, max. 35 %.
Die Rücksendung trifft 150–480 Geschäftsminuten nach dem Versand ein; **dann** wird erstattet und
das Retourenpaket liegt am Wareneingang (`Sim.DockReturns`). Am **Retourenplatz**: als B-Ware
einlagern (Qualität × 0,75) oder entsorgen. Die Lagerist:in bearbeitet Retouren automatisch.
*Die alte sofortige „Rücksendung!“-Erstattung bei Billig-Ware entfällt (ersetzt durch dieses System).*

### Typen

```csharp
public enum ItemKind { None, Crate, Item, Package, Labeled, Plate, Return = 6 }   // Return ist neu

public sealed class PendingReturn   // Rücksendung unterwegs
{
    public ItemData Item;     // Kind = Return, Price = Erstattungsbetrag, Note = Grund, Customer/City
    public float ArriveAt;    // BClock
}
```

### Sim-Mitglieder

| Signatur | Bedeutung |
|---|---|
| `List<PendingReturn> ReturnsIncoming` | unterwegs (Retouren-App: „kommen noch“) |
| `List<ItemData> DockReturns` | **Retourenpakete am Wareneingang** (älteste zuerst) |
| `int ReturnsAtDock` | Anzahl am Wareneingang |
| `ItemData PickupReturn()` | ältestes Retourenpaket aufnehmen (null + Hinweis, wenn keins da) |
| `void PutBackReturn(ItemData ret)` | zurück an den Wareneingang |
| `bool ProcessReturn(ItemData ret, bool restock)` | **Retourenplatz:** `true` = als B-Ware einlagern (braucht Lagerplatz), `false` = entsorgen; `false` zurück = nichts passiert (z. B. Lager voll – dann bleibt die Retoure in der Hand) |
| `float BStockQuality(ItemData ret)` | Qualität, mit der sie als B-Ware ins Lager käme (für den Prompt „B-Ware (Billig)“) |
| `float ReturnChance(ItemData pkg, bool late = false)` | Rücksende-Wahrscheinlichkeit eines Pakets |
| `float ExpectedReturnRate(string id)` | erwartete Quote mit aktuellem Lager (Einkauf/Retouren-App) |
| `float ReturnRate(string id)` / `float ReturnRateTotal()` | tatsächliche Quote bisher |
| `int TotalReturns, TotalRefunds, TotalReturnsRestocked, TotalReturnsDisposed` | Gesamtstatistik |
| `Dictionary<string,int> ReturnsPerProduct` | Retouren je Produkt |

Tageswerte: `Daily.Returns`, `Daily.Refunds` (€), `Daily.ReturnsRestocked`, `Daily.ReturnsDisposed`.

### Ereignisse

| Ereignis | Nutzen |
|---|---|
| `event Action<ItemData> ReturnArrived` | Welt: Retourenpaket am Wareneingang erscheinen lassen; UI: Badge Retouren-App |
| `event Action<ItemData,bool> ReturnProcessed` | (Retoure, eingelagert?) – Welt: Paket ins Regal/in die Tonne animieren |
| `event Action ReturnsChanged` | Listen geändert (unterwegs/am Wareneingang) |

### Hinweise Welt

* **Wareneingang:** Retourenpakete zusätzlich zu `DockCrates` zeigen (`gm.DockReturns`, z. B.
  eigenes kleines Ablagefach „RETOUREN“). Aufnehmen: `gm.PickupReturn()`; mit Retoure in der Hand am
  Fach: `gm.PutBackReturn(held)`.
* **`ItemKit.Build` braucht einen Fall für `ItemKind.Return`** (sonst unsichtbar!): ramponierter
  Karton in Produktgröße mit rotem „RETOURE“-Aufkleber. `Bounds` ebenso.
* **Retourenplatz (neue Station):** zwei Aktionen, z. B. Prüftisch links „Als B-Ware einlagern
  (`GameData.QualityName(gm.BStockQuality(held))`)“ → `gm.ProcessReturn(held, true)` und Container
  rechts „Entsorgen“ → `gm.ProcessReturn(held, false)`. Bei `true` Hände leeren.
* Tragen: wie ein Paket (eine Hand oder zwei – Welt entscheidet).

### Hinweise UI

* **App „Retouren“:** unterwegs (`ReturnsIncoming`: Produkt, Kundschaft, Ankunft `sim.BClockText(r.ArriveAt)`),
  am Wareneingang (`DockReturns`: Produkt, `Note` = Grund, Erstattung `Price`), Quote je Produkt
  (`ReturnRate`, `ExpectedReturnRate`), Tipps (Billig-Ware, zu große Kartons, Verspätung).
* Tagesabrechnung: Erstattungen als Kostenzeile (`DaySummary.Refunds`).

---

## 5. F3 Trends / Hype

Jedes Produkt hat einen **Hype-Faktor 0,5–2,0**, der die Nachfrage multipliziert (in
`DemandRate`/`DemandLabel` enthalten). Zyklus über Tage: **stabil → steigend (2–3 T) → Peak
(1–2 T) → fallend (2–3 T) → tot (0–2 T) → stabil (5–14 T)**, höchstens 2 Produkte gleichzeitig
im Hype. Innerhalb eines Tages gleitet der Wert vom Start- zum Zielwert. Ereignisse und der Skill
„Trendsetter“ können Hypes auslösen.

### Typen

```csharp
public enum TrendPhase { Normal = 0, Rising = 1, Peak = 2, Falling = 3, Dead = 4 }

public sealed class TrendState
{
    public string Product; public TrendPhase Phase;
    public int DaysLeft, PhaseDays;          // Rest/Länge der Phase in Tagen
    public float Start, StartAt, Hype, From; // Verlauf heute: Start (ab StartAt) → Hype (20 Uhr)
    public float Peak, Floor;                // geplante Höhe / Tiefpunkt
    public int RiseDays, PeakDays, FallDays, DeadDays;   // Plan des Zyklus
    public string Reason;                    // "Ein Streamer hat LED-Lichterkette live in die Kamera gehalten."
    public List<float> History;              // Hype am Ende der letzten Tage (max. 14, ältester zuerst)
}

public sealed class TrendForecast
{
    public string Product; public TrendPhase Phase;
    public float Now;              // aktueller Hype
    public float[] Values;         // Prognose Ende morgen, übermorgen, in 3 Tagen
    public float[] Low, High;      // Unsicherheitsband je Tag
    public float Accuracy;         // 0,45 ohne / 0,85 mit Skill "Trendradar"
    public string Label;           // "stabil", "Hype im Anflug", "erholt sich", "steigend", "Hype!", "fallend", "tot"
    public string Hint;            // Handlungstipp (Deutsch)
    public string Icon;            // "trend", "fire", "trend_down", "warning", "dot"
}
```

### API

| Signatur | Bedeutung |
|---|---|
| `readonly TrendSystem Sim.Trends` | das Trend-System |
| `float Sim.TrendMult(string id)` | aktueller Hype-Faktor |
| `string Sim.TrendLabel(string id)` | `"stabil"`, `"steigend"`, `"Hype!"`, `"fallend"`, `"tot"` |
| `TrendState TrendSystem.State(string id)` | Zustand inkl. `Reason` und `History` (Diagramm) |
| `TrendPhase TrendSystem.Phase(string id)` | Phase |
| `float TrendSystem.Mult(string id)` | = `Sim.TrendMult` |
| `string TrendSystem.Label(string id)` | = `Sim.TrendLabel` |
| `int TrendSystem.HypedCount()` | Produkte im Aufwind/Peak |
| `TrendForecast TrendSystem.Forecast(string id, int days = 3)` | **Trendradar** – verbraucht keinen Zufall, pro Tag stabil |
| `static string TrendSystem.HashTag(string id)` | `"LEDChallenge"` |
| `static string GameData.TrendPhaseNames[]` | Anzeigenamen je Phase |
| `int Sim.TrendSightDays()` / `float Sim.TrendForecastAccuracy()` | Sichtweite (1 bzw. 3 Tage) / Genauigkeit |

Nur Core/Ereignisse: `TrendSystem.Trigger(string id, TrendPhase phase, float peak = 0f, string reason = "")`,
`NewDay()`, `Reset()`.

### Ereignisse

| Ereignis | Nutzen |
|---|---|
| `event Action<string, TrendPhase> Sim.TrendChanged` | Produkt wechselt Phase (Toast macht die Core bei gelisteten Produkten) – Badge „Markt & Trends“, Flammen-Icon im Shop |
| `event Action Sim.TrendsUpdated` | neue Tageswerte (Tageswechsel) oder Ereignis-Hype |

### Hinweise UI

* **App „Markt & Trends“:** je Produkt: aktueller Hype (`TrendMult`, als ×1,4 oder Balken),
  `TrendLabel` + Icon, `State(id).Reason`, 14-Tage-Linie aus `History` + heutiger Wert, Prognose
  `Forecast(id)` als 3 Punkte mit Band `Low`/`High`, `Hint`. Ohne Skill „Trendradar“ Hinweis
  „Prognose ungenau – Skill Trendradar (Marketing)“.
* **Webshop:** kleines Flammen-/Pfeil-Icon neben Produkten mit Hype (`TrendPhase.Rising/Peak`).

---

## 6. F4 Großaufträge (B2B)

Ab **Level 3** kommen Anfragen von Firmen: werktags 1 (Level 3–4), 1–2 (Level 5–7), 1–3 (ab
Level 8), am Wochenende 0–1; beim Erreichen von Level 3 sofort eine erste Anfrage. Angebote
gelten bis Ende des nächsten Tages. Gleichzeitig laufend: 1 (L3–4), 2 (L5–7), 3 (ab L8), +1 mit
Skill „Networking“. Frist 2–4 Tage (inkl. Annahmetag, bis 20 Uhr). Geliefert wird am
**Palettenplatz**: ganze **Kisten** (ohne Auspacken, Rest bleibt in der Kiste) oder
**Einzelartikel** aus dem Regal. Erfüllt → Vergütung (zählt als Umsatz), XP, Ruf +;
Frist verpasst → gelieferte Stück zu 50 % bezahlt, **Vertragsstrafe (25 %)**, Ruf −0,18.
Manche Firmen wollen mind. Standard- (Qualität ≥ 0,8) oder Premium-Qualität (≥ 1,3).
Die **Lagerist:in** bestückt Paletten automatisch aus dem Lager (10 Stück je Arbeitsgang, 20 bleiben
als Reserve).

### Typen

```csharp
public enum ContractState { Offered = 0, Active = 1, Completed = 2, Failed = 3, Declined = 4, Expired = 5 }

public sealed class Contract
{
    public int Id;
    public string Company, Reason, Product;           // "Kita Sonnenschein e. V.", "für das Sommerfest", "led"
    public int Quantity, Delivered, Payment, Penalty, Xp;
    public float RepBonus, RepPenalty, MinQuality;    // MinQuality: 0 egal, 0,8 Standard, 1,3 Premium
    public int Days;                                  // Frist ab Annahme
    public int OfferDay, OfferExpiresDay, AcceptedDay, DeadlineDay, ClosedDay;
    public ContractState State;
    public bool Special;                              // aus einem Ereignis (Sonderkonditionen)
    public int PaidOut;                               // nach Abschluss: ausgezahlt (Misserfolg: Teilzahlung − Strafe)
    public int Remaining { get; }  public float Progress { get; }
    public bool IsOffer { get; }   public bool IsActive { get; }  public bool IsClosed { get; }
    public string Title { get; }        // "40× LED-Lichterkette"
    public string QualityText { get; }  // "Qualität egal" / "mind. Standard-Qualität" / "nur Premium-Qualität"
    public string Description { get; }  // "Kita Sonnenschein e. V. braucht 40× LED-Lichterkette für das Sommerfest."
}
```

### Sim-Mitglieder

| Signatur | Bedeutung |
|---|---|
| `bool ContractsUnlocked` | ab Level 3 |
| `int MaxActiveContracts()` | aktuelles Limit |
| `List<Contract> ContractOffers()` | offene Angebote |
| `List<Contract> ActiveContracts()` | laufende, früheste Frist zuerst |
| `List<Contract> ContractHistory()` | abgeschlossene (neueste zuerst, max. 12) |
| `int ActiveContractCount()` | Anzahl laufend |
| `Contract FindContract(int id)` | suchen |
| `bool CanAcceptContract(Contract c, out string reason)` | Button aktiv? `reason` = deutscher Grund für Tooltip |
| `bool AcceptContract(int id)` / `bool DeclineContract(int id)` | **App-Buttons** |
| `int ContractDaysLeft(Contract c)` | Tage bis Frist (laufend) bzw. bis Angebot verfällt (0 = heute) |
| `string ContractDeadlineText(Contract c)` | `"heute bis 20:00"`, `"morgen bis 20:00"`, `"bis Do, 20:00 (3 Tage)"` |
| `int ContractUnitsNeeded(string pid)` | offene Stück eines Produkts über alle laufenden Aufträge (Regal-Prompt) |
| `Contract ContractWanting(string pid)` | laufender Auftrag, der das Produkt noch braucht |
| `Contract ContractFor(ItemData item)` / `bool ContractAccepts(ItemData item)` | würde der Palettenplatz diesen Gegenstand nehmen? (Prompt) |
| `int ContractDeliver(ItemData item)` | **Palettenplatz:** Kiste oder Einzelartikel abgeben; Rückgabe = gelieferte Stück (0 = abgelehnt, Core gibt Hinweis) |
| `ItemData PickForContract(string id)` | Einzelartikel für einen Auftrag aus dem Regal (macht `PickItem` automatisch, wenn keine Bestellung offen ist) |
| `List<Contract> Contracts` | alle (Angebote, laufend, Verlauf) |
| `int TotalContractsDone, TotalContractsFailed` | Statistik |

Tageswerte: `Daily.ContractIncome` (auch im Umsatz enthalten), `Daily.Penalties`, `Daily.ContractsDone`,
`Daily.ContractsFailed`.

### Ereignisse

| Ereignis | Nutzen |
|---|---|
| `event Action ContractsChanged` | Aufträge-App/Handy neu zeichnen, Palette aktualisieren |
| `event Action<Contract> ContractOffered` | Badge „Aufträge“, Handy-Benachrichtigung |
| `event Action<Contract> ContractAccepted` | Welt: leere Palette mit Firmenschild bereitstellen |
| `event Action<Contract,int> ContractDelivered` | (Auftrag, Stück) – Palette wächst, Sound |
| `event Action<Contract> ContractCompleted` | **Spedition holt die Palette ab** (LKW-Animation), Banner „Großauftrag erfüllt“ |
| `event Action<Contract> ContractFailed` | Palette verschwindet, roter Hinweis |

### Hinweise Welt

* **Palettenplatz (neue Station):** Prompt mit Kiste: `gm.ContractAccepts(held)` ?
  „Auf die Palette (`ContractFor(held).Company`, noch `Remaining`)“ : Grund. Aktion:
  `int n = gm.ContractDeliver(held)`; bei Kisten Hände nur leeren, wenn `held.Quantity <= 0`
  (sonst `player.Hold(held)` wie beim Verkaufsstand), bei Einzelartikeln Hände leeren, wenn `n > 0`.
  Sichtbarer Stapel: `gm.ActiveContracts()` → je Auftrag `Delivered/Quantity` als Kartonstapel,
  Schild mit `Company`.
* **Regal:** Ohne offene Bestellung liefert `PickItem(pid)` automatisch einen Großauftrags-Artikel
  (`ContractId > 0`), wenn `gm.ContractUnitsNeeded(pid) > 0`. Prompt dann z. B.
  „Für Großauftrag entnehmen (noch 25)“. Zurücklegen wie gewohnt mit `ReturnItem` (legt nur ins
  Lager zurück, erzeugt keine Bestellung).
* **Packtisch** lehnt Großauftrags-Artikel ab (Core-Hinweis).

### Hinweise UI

* **App „Aufträge“:** Tabs *Angebote* (Description, `QualityText`, Menge, Vergütung, Strafe, XP,
  `ContractDeadlineText`, Buttons Annehmen/Ablehnen – Annehmen deaktiviert mit Tooltip aus
  `CanAcceptContract`), *Laufend* (Fortschritt `Delivered/Quantity`, Frist, Lagerbestand
  `sim.StockQty(c.Product)` + unterwegs, Schnell-Nachbestellen), *Verlauf* (`PaidOut`, Status).
  Oben: `ActiveContractCount()/MaxActiveContracts()`, `TotalContractsDone`.
* Handy: Angebote als Nachricht mit Annehmen/Ablehnen.

---

## 7. F5 Hustle-Skills

**1 Skillpunkt zum Start + 1 je Level-Aufstieg** (+ Bonuspunkte aus Ereignissen, z. B. Seminar).
3 Äste × 4 Stufen; Stufe n braucht Stufe n−1 im selben Ast und Firmenlevel 1/3/5/7. Mit 10
Punkten (Level 10) lassen sich nicht alle 12 Skills lernen – bewusste Wahl. Umschulung
(alles zurücksetzen) kostet `150 € × Level`.

| Ast | Stufe 1 (L1) | Stufe 2 (L3) | Stufe 3 (L5) | Stufe 4 (L7) |
|---|---|---|---|---|
| **Logistik** (`truck`) | `l_arme` Starke Arme: Kiste bremst nicht mehr, +10 % Tempo beim Tragen | `l_tetris` Lager-Tetris: +30 % Lagerplatz | `l_wege` Kurze Wege: −30 % Lieferzeit | `l_flow` Prozess-Flow: Personal +25 % Tempo, Förderband 3 statt 6 min |
| **Vertrieb** (`coin`) | `v_feilschen` Feilschen: Einkauf −10 % | `v_kulanz` Kundenflüsterer: −35 % Retouren | `v_netzwerk` Networking: Großaufträge +20 %, +1 gleichzeitig | `v_stamm` Stammkundschaft: schlechte Bewertungen halb so stark, +3 Warteschlange |
| **Marketing** (`mega`) | `m_content` Content Creator: TikToks +25 % stärker und länger | `m_radar` Trendradar: Prognose genauer (0,85) und 3 Tage Sicht | `m_viral` Viral-Gen: TikTok-Abklingzeit −40 %, Werbung −25 % | `m_trendsetter` Trendsetter: eigenes TikTok ≥ 60 % startet einen Hype |

### Typen & Daten

```csharp
public sealed class SkillDef       { public string Id, Branch, Name, Desc, Icon; public int Tier, Level; }
public sealed class SkillBranchDef { public string Id, Name, Icon, Desc; public RGBA Color; }
GameData.Skills (SkillDef[12]), GameData.SkillBranches (3), GameData.SkillTierLevels {1,3,5,7},
GameData.Skill(id), GameData.SkillBranch(id), GameData.StartSkillPoints = 1, GameData.RespecCostPerLevel = 150
```

### Sim-Mitglieder

| Signatur | Bedeutung |
|---|---|
| `HashSet<string> Skills` | gelernte Skill-IDs |
| `int BonusSkillPoints` | Extra-Punkte aus Ereignissen |
| `bool HasSkill(string id)` | gelernt? |
| `int SkillPointsTotal()` / `int SkillPointsAvailable()` | gesamt / frei (Badge!) |
| `string SkillState(string id)` | `"learned"`, `"available"`, `"level"` (Level zu niedrig), `"requires"` (Vorstufe fehlt), `"points"` (kein Punkt), `"unknown"` |
| `bool LearnSkill(string id)` | lernen (Core gibt Hinweise bei Fehlschlag) |
| `static SkillDef PreviousSkill(SkillDef s)` / `static List<SkillDef> SkillsOfBranch(string branch)` | Baum-Aufbau |
| `int RespecCost()` / `bool ResetSkills()` | Umschulung |
| `void AddSkillPoints(int n)` | Bonuspunkte (Ereignisse) |

**Effekt-Abfragen** (von Formeln benutzt, für Anzeigen lesbar):
`CapacityMult()`, `LeadTimeMult()`, `StaffSpeedMult()`, `ConveyorMinutes()`, `PurchasePriceMult()`,
`ReturnRateMult()`, `ContractPayMult()`, `ContractSlotBonus()`, `QueueBonus()`, `BadReviewWeightMult()`,
`TikTokPowerMult()`, `TikTokCooldownMinutes()`, `AdCostMult()`, **`int AdCost(int tierIndex)`**,
`TrendForecastAccuracy()`, `TrendSightDays()`, `bool TikTokStartsTrends`.

**Welt-Flag:** `float CarrySpeedMult(ItemKind kind)` – Faktor fürs Lauftempo mit Gegenstand in der
Hand: ohne Skill Kiste 0,85 / sonst 1,0; mit „Starke Arme“ 1,1 für alles; leere Hände 1,0.

### Ereignisse

| Ereignis | Nutzen |
|---|---|
| `event Action<SkillDef> SkillLearned` | Freischalt-Animation im Skillbaum, Sound |
| `event Action SkillsChanged` | Punkte/Stand geändert (auch bei jedem Level-Aufstieg) – Badge aktualisieren |

### Hinweise

* **Welt (PlayerController):** `if (Held != null && Held.Kind == ItemKind.Crate) speed *= 0.85f;`
  ersetzen durch `speed *= Game.Sim.CarrySpeedMult(Held?.Kind ?? ItemKind.None);`.
* **UI – App „Firma“ › Skills:** drei Spalten (Äste, `SkillBranch.Color`), je 4 Knoten; Zustand aus
  `SkillState` (gelernt / lernbar / „ab Level X“ / „erst Vorstufe“ / „kein Punkt“); oben
  „`SkillPointsAvailable()` Skillpunkte frei“; Button „Umschulung (`Fmt.Money(RespecCost())`)“.
  Level-Up-Feier: „+1 Skillpunkt“.
* **UI – Marketing-App:** Kosten der Werbeformen mit `sim.AdCost(i)` statt `GameData.AdTiers[i].Cost`
  anzeigen; TikTok-Abklingzeit kommt schon über `TikTokReadyAt`. Optional: Produkt fürs TikTok
  wählen und `sim.TriggerTikTok(score, false, productId)` aufrufen (für „Trendsetter“).

---

## 8. F6 Wochenziele

Jeden **Montag** 3 Wochenziele (immer ein Durchsatz-Ziel „Pakete“ oder „Umsatz“ + 2 weitere),
bemessen an den letzten Tagen; Start mitten in der Woche (alter Spielstand) → anteilig kleiner.
Belohnung Geld + XP. Unfertige Ziele verfallen Sonntagabend. Ereignisse können Bonusziele
hinzufügen (z. B. „Kalles Wette“).

Typen (`WeeklyChallenge.Type`): `ship` (Pakete), `revenue` (Umsatz inkl. Stand/Großaufträge),
`rating` (Bewertung ≥ x, mind. 5 Bewertungen), `stars5` (Fünf-Sterne-Bewertungen), `express`
(pünktliche Express-Pakete), `contract` (Großaufträge), `stand` (Stand-Verkäufe), `tiktok`
(eigene TikToks), `product` (Verkäufe eines Produkts), `restock` (Retouren als B-Ware),
`perfect_day` (x Pakete an einem Tag ohne verlorene Bestellung).

```csharp
public sealed class WeeklyChallenge
{
    public string Id, Type, Title, Desc, Icon, Product;   // Icon existiert in UI/Icons.cs
    public float Target, Progress;
    public int Reward, Xp, Week;
    public bool Done, Bonus;
    public string Sponsor;                 // bei Bonuszielen z. B. "Kalle"
    public float Fraction { get; }         // 0..1
    public string ProgressText { get; }    // "12 / 40", "1.234 € / 2.000 €", "4,1 / 4,3 ★"
}
```

| Signatur | Bedeutung |
|---|---|
| `List<WeeklyChallenge> Sim.Challenges` | Ziele der Woche (inkl. Bonus) |
| `int Sim.ChallengeWeek` | Woche, für die sie gelten |
| `int Sim.ChallengesDoneThisWeek()` | geschafft diese Woche |
| `int Sim.TotalChallengesDone` | gesamt (Ziel „Wochenheld“) |
| `event Action<WeeklyChallenge> ChallengeCompleted` | Banner „Wochenziel geschafft“ (Toast macht die Core) |
| `event Action ChallengesChanged` | Fortschritt/Liste geändert |
| `event Action<int> WeekStarted` | Montag, neue Ziele (Wochennummer) – Banner „Woche 3“ |

Nur Core/Ereignisse/Tests: `StartWeek(bool announce = true)`, `AddBonusChallenge(...)`,
`ChallengeProgress(...)`, `CheckChallenges()`.

**UI:** HUD-Zielkarte kann zwischen aktuellem Ziel (`CurrentObjective()`) und Wochenzielen
wechseln; Handy „Status“ und App „Firma › Ziele“: Liste mit Icon, Titel, Desc, Balken `Fraction`,
`ProgressText`, Belohnung `Fmt.Money(Reward)` + `Xp` XP, Bonusziele mit `Sponsor` hervorheben,
„noch `DaysLeftInWeek()` Tage“.

---

## 9. F7 Tagesabrechnung, Ereignisse, Texte, Ziele

### DaySummary (neue Felder, `DayEnded` wie bisher)

| Feld | Bedeutung / Kassenbon-Zeile |
|---|---|
| `int Weekday`, `string WeekdayName` | „Freitag, Tag 12“ |
| `int Express, Late, Expired` | Express-Pakete, verspätet, storniert (Expired ⊆ Lost) |
| `int Returns, Refunds, ReturnsRestocked, ReturnsDisposed, ReturnsIncoming` | Retouren heute, **Erstattungen (Kosten)**, B-Ware, entsorgt, noch unterwegs |
| `int ContractIncome, Penalties, ContractsDone, ContractsFailed, ContractsActive` | Großaufträge: **Einnahmen (schon im Umsatz, als „davon“ zeigen)**, **Vertragsstrafen (Kosten)** |
| `int ChallengesDone, ChallengeRewards` | Wochenziele heute (Belohnung schon in `IncomeOther`, als „davon“ zeigen) |
| `bool WeekEnded`, `int WeekChallengesDone, WeekChallengesTotal` | Sonntag: Wochenbilanz |
| `List<string> Notes` | kurze Meldungen („Großauftrag erfüllt: …“, „Wochenziel geschafft: …“, „Wochenbilanz: 2 von 3 …“) |

**Profit** = Umsatz + Sonstiges − Einkauf − Verpackung − Marketing − Sonstiges − **Erstattungen −
Strafen** − Miete − Löhne − Strom − Zinsen (`DailyStats.VariableProfit()` − Fixkosten).
`HistoryEntry` hat zusätzlich `Returns` und `Contracts` (Analytics-Diagramme).

### Ereignisse (Postfach)

Neue Ereignisse (alle mit Entscheidungen außer `trend_alarm`/`express_rush`):
`retourenwelle` (Retouren), `grosskunde` (Großauftrag sofort/als Angebot), `trend_alarm` (Hype),
`trend_crash` (Hype-Verriss, Reaktionsvideo), `express_rush` (mehr Express), `eilauftrag`
(Auftrag früher, +30 %), `kuriose_retoure` (Toaster-Retoure), `seminar` (+1 Skillpunkt),
`kalles_wette` (Bonus-Wochenziel), `messe` (2 Großanfragen). `viral` startet jetzt zusätzlich einen
Hype für dasselbe Produkt. Insgesamt 30 Ereignisse.

* **Platzhalter:** Titel/Absender/Text/Ergebnis können `{product}`, `{tag}`, `{company}`, `{n}`,
  `{brand}` enthalten – die Core ersetzt sie beim Auslösen; `Mail.Title/Sender/Text/Result` sind fertig.
* `Mail` hat neue Felder `CtxProduct`, `CtxCompany`, `CtxContract`, `CtxNumber` (Kontext; die UI kann
  z. B. das Produkt-Icon zeigen).
* `Effects` hat neue Felder: `HypeEffect Hype` (`Phase`, `Peak`, `Product`), `int? ReturnWave`,
  `string Contract` (`"offer"`/`"accept"`), `ContractBonus`, `ContractCount`, `float? ContractRush`,
  `float? ExpressBoost` + `ExpressBoostMinutes`, `int? SkillPoints`, `bool BonusChallenge`.
* `EventSystem`: `List<string> Apply(Effects e, Mail ctx)` (alte Überladung bleibt),
  `Contract RushableContract()`, `int BonusShipTarget()`, `string Fill(string text, Mail ctx)`.
  Neue `Needs`-Werte: `shipped15`, `returns`, `contract_rushable`, `early_week`.
* **Einmalige Erklär-Nachrichten** (Absender `GameData.CoachName` „Marvin (Hustle-Akademie)“):
  erste Retoure, erster Express-Zettel, erster Level-Aufstieg (Skills), Level 3 (Großaufträge),
  Wochenziele (nach dem Tutorial). `Sim.Explained` merkt sich, was schon kam; `Sim.Explain(key, mail, icon)`.

### Inhalte in `GameData` (Datei `GameData.Content.cs`)

`CustomerFirstNames` (50), `CustomerInitials`, `Cities` (35), `OrderNotes` (20), `ExpressNotes`,
`ProductNotes` (je Produkt), `ReturnReasons`, `ReturnReasonsQuality`, `ReturnReasonLate`,
`ReturnReasonOversize`, `Companies` (28 lustige Firmennamen), `ContractReasons`, `TrendReasons*`,
`ReviewTextsExpressGood/ExpressLate/Late` (+ mehr `ReviewTexts`), `Mail*`-Texte, alle
Balancing-Konstanten der neuen Systeme (siehe Datei, jeweils kommentiert).

### Tutorial & Tastenhinweise

* `GameData.Tutorial[i].Text` enthält jetzt Platzhalter `{key:interact}` / `{key:phone}`.
  **Immer `GameData.TutorialText(i)` oder `sim.CurrentObjective().Text` benutzen** (bereits ersetzt).
* `static Func<string,string> GameData.KeyLabel` – die UI setzt z. B.
  `GameData.KeyLabel = GameInput.KeyLabel;` sobald `GameInput` die Aktion `"phone"` kennt. Liefert
  der Resolver nichts oder den Aktionsnamen selbst, gilt `GameData.DefaultKey` (`interact`→E,
  `drop`→G, `phone`→Tab, `laptop`→Tab, …). `GameData.Key(action)`, `GameData.FillKeys(text)`.
* Texte: Bestellzettel „oben rechts“, **Handy mit {key:phone}**, HustleOS-Laptop an der Werkbank.

### Ziele

`GoalDef.Xp` (neu): Ziele geben jetzt auch Erfahrung. Neue Ziele: `first_contract` („Business to
Business“), `challenges3` („Wochenheld“); `GoalValue` kennt `"contracts"` und `"challenges"`.

### Sonstiges

* `SaveSummary` (Menü-Karten): neu `Version`, `Weekday`, `Stage`, `Rating`.
* `GameData.QualityStandard = 0.8f`, `GameData.QualityPremium = 1.3f` (Stufen von `QualityName`).
* Pitch Day: Frau Weber fragt jetzt auch nach der Retourenquote (Kennzahl `"returns"`).

---

## 10. Speichern / Laden (v4)

* `Sim.SaveVersion = 4`. Neu gespeichert: Zettel in Arbeit, nächste Nummer, Neuheiten-Bestellungen,
  Express-Fieber, letzter Einkauf, Retouren (unterwegs, am Wareneingang, Statistik), Trends
  (kompletter Zustand + Verlauf), Großaufträge (inkl. Verlauf, geplante Angebote), Skills,
  Bonus-Skillpunkte, Wochenziele (inkl. Fortschritt), Erklär-Flags; `ItemData`/`Order`/`Mail`/
  `DailyStats`/`HistoryEntry` mit neuen Feldern.
* **v3-Spielstände laden weiter:** Zettel bekommen Nummer, Kundschaft, Ort, Notiz und Standard-Frist;
  alte Artikel/Pakete ohne Nummer funktionieren (Versand, Zurücklegen); Trends starten frisch;
  keine Skills (alle Punkte frei: Level = Punkte); Wochenziele für die laufende Woche (anteilig);
  keine Retouren/Aufträge. Beim nächsten Speichern wird v4 geschrieben. Getestet mit echtem v3-JSON.
* Nach dem Laden werden Zettel „in Arbeit“ entfernt, zu denen kein Gegenstand mehr existiert
  (Paketstapel, Band, Welt-Gegenstände, Hand).

---

## 11. Geänderte bestehende API und Design-Änderungen

Signaturen bleiben kompatibel; nur Verhalten/Balance ändern sich:

| Mitglied / Wert | Neu |
|---|---|
| `SpawnOrder(string)` | würfelt ab Level 2 Express; Ereignisse `OrderReceived`/`OrdersChanged`/`OrderLost` |
| `PickItem(string)` | nimmt den **dringendsten** Zettel; ohne Zettel, aber mit Großauftrag → Auftrags-Artikel |
| `ReturnItem(ItemData)` | stellt **denselben** Zettel wieder her; Auftrags-Artikel nur zurück ins Lager |
| `WrapItem(ItemData)` | fehlt der passende Karton, wird der **nächstgrößere** genommen (Hinweis, höheres Retourenrisiko) statt abzubrechen; Auftrags-Artikel werden abgelehnt |
| `ShipPackage(...)` | Bewertung relativ zur Frist (≤ 40 % der Frist +1, verspätet −1, > doppelt −2; Express ±1 zusätzlich, 70 % Bewertungschance, Gewicht 1,6); Retouren statt sofortiger Billig-Erstattung |
| `TriggerTikTok(float score, bool silent = false, string product = "")` | neuer optionaler Parameter; Skill-Stärke/Abklingzeit; eigene TikToks zählen für Wochenziele |
| `StartAdCampaign(int)` | Kosten = `AdCost(i)` (Skill) |
| `Capacity()`, `QueueCapacity()`, `BulkCost(...)`, `LeadMinutes(...)` | Skill-Faktoren |
| `DemandRate(string)` | × `TrendMult` × `WeekdayDemand` |
| `SetListed(id, true)` | noch nie verkauftes Produkt → erste Bestellung in 20–45 Spielminuten (≈ 15–35 echte Sekunden) |
| `NewGame("skip")` | wie oben für die Handyhülle; alle Modi starten die Wochenziele |
| `CurrentObjective()` | Tutorial-Text mit ersetzten Tasten |
| `AddXp` | Toast „· +1 Skillpunkt“; Level 3 → erstes Großauftrags-Angebot |
| `CheckGoals` | Ziele geben XP |
| Personal | Lagerist:in bestückt Paletten und bearbeitet Retouren; Packer:in/Versand nehmen den dringendsten Zettel; Packer:in nutzt notfalls größere Kartons; Tempo-Skill |
| Förderband | `ConveyorMinutes()` (6, mit Skill 3) |
| `GameData.LevelXp` | `{0, 150, 800, 1700, 3000, 5000, 8000, 13000, 20000, 30000}` (vorher 500/1200/2500/4500/7500/12000/18000/26000) |
| Lagerhalle | **6.000 €** (vorher 4.500 €) |
| `GameData.LevelUnlocks` | nennen Express (L2), Großaufträge (L3), Skill-Stufen |
| Straßenfest-Icon | `stand` (vorher nicht existierendes `tent`) |

**Balance-Ergebnis** (`BalanceTests`, fleißiger Bot mit menschlichem Tempo, Retouren, Aufträgen, Skills):
erste Bestellung 22 Spielminuten nach dem Online-Stellen (≈ 16 s echt), erstes Level an Tag 1,
Lagerhalle an Tag 11 (4 weitere Seeds: Tag 11–12), Level 10 um Tag 24, 24 Großaufträge
in 25 Tagen ohne verpasste Frist, Retourenquote ≈ 2 %, 9 Wochenziele. Halbes Tempo ohne Aufträge/Skills:
keine Pleite (Level 5 nach 20 Tagen). Pleite nur bei grobem Fehlverhalten (Kredit verprassen, nichts
verkaufen → nach 4 Wochen, Tag 29).

---

## 12. Checkliste UI-Agent

1. **HUD oben rechts:** Bestellzettel aus `Tickets()` mit Countdown/Express/Stand; `OrdersChanged`, `OrderReceived`, `OrderExpired`, `OrderShipped`.
2. **HUD oben links:** Wochentag (`WeekdayShort()`), sonst wie bisher.
3. **Handy (Tab):** Bestellungen (Zettel + Notiz), Nachrichten (Postfach inkl. Entscheidungen, Großauftrags-Angebote mit Annehmen/Ablehnen), **Schnell-Nachbestellen** (`QuickReorder`, `QuickReorderCost`), Status (Geld, Wochenziele, Skillpunkte, Retouren am Wareneingang).
4. **HustleOS-Apps:** *Markt & Trends* (Abschnitt 5), *Aufträge* (6), *Retouren* (4), *Firma › Skills* (7) und *› Ziele* (8), Marketing-Kosten über `AdCost(i)`.
5. **Badges:** `SkillPointsAvailable()`, `ContractOffers().Count`, `ReturnsAtDock`, `Events.PendingCount()`, Wochenziele geschafft.
6. **Tagesabrechnung (Kassenbon):** neue Felder aus Abschnitt 9, `Notes` als Liste, Sonntag Wochenbilanz.
7. **Banner:** `WeekStarted`, `ChallengeCompleted`, `ContractCompleted`, `SkillLearned`, Level-Up mit „+1 Skillpunkt“.
8. `HudView.SetHeld` für Retouren, Zettel-Artikel und Großauftrags-Artikel (Abschnitt 3).
9. `GameData.KeyLabel = GameInput.KeyLabel;` sobald die Aktion `"phone"` existiert (sonst Standard „Tab“).
10. Tutorial-/Zieltexte nur über `CurrentObjective()` bzw. `GameData.TutorialText(i)` anzeigen.

## 13. Checkliste Welt-Agent

1. **`ItemKit`:** Visual + `Bounds` für `ItemKind.Return`; Pakete in `EffectivePackSize`; optional Express-Label rot, Label mit `Customer`/`City`.
2. **Labeldrucker:** `gm.OnLabeled(labeled)` (neue Überladung).
3. **Wareneingang:** Retourenfach (`DockReturns`, `PickupReturn`, `PutBackReturn`), Refresh über `ReturnsChanged`/`ReturnArrived`.
4. **Retourenplatz (neu):** zwei Aktionen → `ProcessReturn(held, true/false)`.
5. **Palettenplatz (neu):** `ContractAccepts`/`ContractFor`/`ContractDeliver`; Stapel aus `ActiveContracts()`; Spedition bei `ContractCompleted`.
6. **Regal:** Prompt „Für Großauftrag entnehmen“ wenn keine Bestellung, aber `ContractUnitsNeeded(pid) > 0`.
7. **Bestell-Monitor (neu):** `Tickets()`.
8. **PlayerController:** Tempo mit `Game.Sim.CarrySpeedMult(Held?.Kind ?? ItemKind.None)`.
9. Zerstörte Gegenstände (aus der Welt gefallen): `gm.DiscardItem(data)`.

## 14. Nicht aus der UI aufrufen

Diese öffentlichen Methoden verbrauchen Zufall oder erzeugen Spielzustand; sie sind für die Core,
Ereignisse und Tests gedacht: `CreateOrder`, `SpawnOrder` (außer Debug), `RandomCustomer`,
`ScheduleLaunchOrder`, `ScheduleReturn`, `TriggerReturnWave`, `GenerateContractOffer`,
`RushContract`, `StartWeek`, `AddBonusChallenge`, `ChallengeProgress`, `CheckChallenges`,
`AddSkillPoints`, `AddExpressBoost`, `Trends.Trigger`, `Trends.NewDay`, `Trends.Reset`,
`Explain`, `OnTrendPhaseChanged`, `RaiseTrendsUpdated`, `RaiseOrdersChanged`,
`Events.Apply`, `Events.Fill`, `Events.BonusShipTarget`.

---

## 15. Nachtrag: Balance-Paket „Echte Entscheidungen“ (GAME_IDEAS 5.1)

| Thema | Neu | API |
|---|---|---|
| Trading | ETF Drift 0,00002 / Vol 0,0006 je Tick (≈ +0,5 %/Tag), GameShop 0,00001 / 0,0035, DROPCOIN −0,00008 / 0,009 mit 0,4 % Sprungchance ±25 % – Kredit + ETF lohnt nicht mehr | `Market.Assets` |
| Premium-Lieferant | Preisfaktor 2,0 und **1 Bestellung pro Produkt und Tag** | `bool Sim.PremiumQuotaLeft(string pid)`, `Dictionary<string,int> PremiumBoughtDay` (gespeichert); `BuyBulk` lehnt ab → Einkauf-App: Button deaktivieren |
| Werbung | Tageskampagnen bis 20 Uhr: Flyer 25 € ×1,15, Facebook 80 € ×1,35, Google 200 € ×1,6, Influencer 700 € ×2,0 heute + morgen | `StartAdCampaign` wie bisher; `AdTier.Minutes` = 720 / 1440 (Anzeige „Tageskampagne“) |
| Boost-Deckel | Gesamt-Multiplikator höchstens ×4 | `GameData.MaxBoostMult` |
| TikTok | max. **3 pro Tag** (Praktikant:in zählt mit), Abklingzeit 60 min, ×1,3–2,2 für 180–360 min, ×1,25 stärker für ein Produkt mit Hype ≥ 1,3 | `int Sim.TikToksLeftToday()`, `TikTokAvailable()` prüft das Limit, `Daily.TikToks` |
| Express | nur, solange die Warteschlange nicht (fast) voll ist | – |
| Retourengrund | steht in `ItemData.Note` der Retoure (UI zeigen) | – |
| Wochenziel tauschen | 1× pro Woche ein offenes, normales Ziel tauschen | `bool CanRerollChallenge(string id)`, `bool RerollChallenge(string id)`, `int ChallengeRerollWeek` (gespeichert) |

Die Stufe-4-Skills (Prozess-Flow, Stammkundschaft, Trendsetter) verändern das Spiel bereits spürbar.
Nicht umgesetzt aus 5.1 (größerer Balance-Umbau, eigener Schritt): neue Einkaufspreise,
Billig-Streuung, Express-Einkauf 15 %, Dauerauftrag, Preis-Prognose. Balance danach: Lagerhalle an
Tag 11 (Seeds 11–12), 110 Tests grün.

## 14. Mini-Event Straßenfest/Flohmarkt (`Sim.Festival.cs`)

Ablauf: Einladung per Mail (ab Level 2, ~14 %/Tag, mind. 4 Tage Abstand) → am Festtag
**Aufbau** 10:00–12:30 (`FestivalPhase.Prep`) → **Fest** 12:30–16:00 (`Live`) → Bilanz (Mail,
`FestivalEnded`), Restware zurück ins Lager. Tagesende beendet ein laufendes Fest.

| API | Zweck |
|---|---|
| `FestivalPhase Festival`, `int FestivalDay`, `string FestivalName` | Zustand |
| `ItemData FestivalPackCrate(pid)` / `string FestivalNextPackProduct(after)` | Packtisch: bis 10 Stück aus dem Lager in eine Kiste |
| `int FestivalAddCrate(ItemData crate)` | Kiste (Fest- oder Lieferkiste) am Marktstand auslegen (max. 120) |
| `FestivalVisit FestivalVisitor()` | Ein Besucher entscheidet: `NoInterest`, `TooExpensive`, `HaggleFailed`, `Bought`, `HaggledBought` (Preis vs. Marktpreis, Hype, Bewertung) |
| `FestivalStats`, `LastFestival`, `FestivalCountdown()`, `FestivalStatusText()` | HUD/Bilanz |
| `AdminStartFestival(skipPrep)`, `AdminAnnounceFestival()`, `AdminEndFestival()` | Admin-Panel (Tab „Ereignisse“) |
| `event FestivalChanged / FestivalSale / FestivalEnded` | Welt/HUD |

Welt: `Runtime/World/StreetFestival.cs` (Banner, Marktstand mit Markise, Packtisch vor der Tür,
Besucher; Autos pausieren über `StreetFestival.BlocksTraffic`). Gespeichert unter `"festival"`.
