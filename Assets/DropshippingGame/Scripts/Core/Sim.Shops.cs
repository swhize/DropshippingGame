using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    // =========================================================================================
    // Einkaufsviertel: Elektromarkt "MediaMarkd", Tierbedarf "Fressnix", Haustiere.
    // Reine Logik (ohne UnityEngine). Die Welt (ShoppingDistrict.cs) und die Zeitung
    // (WebStadtblatt.cs) lesen nur hierüber.
    // =========================================================================================

    /// <summary>Ein Artikel in einem Laden der Einkaufsstraße.</summary>
    public sealed class StoreItemDef
    {
        public string Id, Store, Name, Short, Icon, Desc;
        /// <summary>"ware" (Produkt für den eigenen Shop → Lager), "futter", "gadget", "tier".</summary>
        public string Kind;
        /// <summary>Bei Ware: Produkt-ID (GameData.Products). Bei Tieren: Tierart ("hund", "katze").</summary>
        public string Product = "";
        /// <summary>Stück pro Packung (Ware) bzw. Portionen (Futter). Sonst 1.</summary>
        public int Pack = 1;
        /// <summary>Normalpreis pro Packung in € (bei Ware 0 = aus dem Einkaufspreis berechnet).</summary>
        public int Price;
        /// <summary>Wie viele Packungen der Laden pro Tag ohne Aktion hat.</summary>
        public int DailyStock = 8;
        public RGBA Color = new RGBA(0.8f, 0.8f, 0.85f);
    }

    /// <summary>Tagesaktion: Rabatt auf einen Artikel, nur solange der (kleinere) Vorrat reicht.</summary>
    public sealed class ShopDeal
    {
        public string ItemId;
        /// <summary>Rabatt 0..1 (0,4 = 40 % billiger).</summary>
        public float Discount;
        /// <summary>Packungen zum Aktionspreis pro Tag.</summary>
        public int Limit;
        /// <summary>Knaller des Tages (größter Rabatt, Titelseite der Zeitung).</summary>
        public bool Mega;
    }

    /// <summary>Position im Einkaufswagen / Korb.</summary>
    public sealed class CartLine
    {
        public string ItemId;
        public int Qty;
        /// <summary>Preis pro Packung beim Einlegen (Aktion gilt bis zur Kasse).</summary>
        public int UnitPrice;
    }

    /// <summary>Ein adoptiertes Haustier.</summary>
    public sealed class Pet
    {
        public string Species = "hund";
        public string Name = "Bello";
        /// <summary>Laune 0..1 (gefüttert, gestreichelt, Spielzeug).</summary>
        public float Happiness = 0.7f;
        /// <summary>Tage in Folge ohne Futter.</summary>
        public int HungryDays;
        public int AdoptedDay = 1;
        /// <summary>Zuletzt gestreichelt (Tag).</summary>
        public int PettedDay;
        /// <summary>true = läuft dem Spieler hinterher, false = wohnt in der Garage.</summary>
        public bool Follow;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "species", Species }, { "name", Name }, { "happiness", (double)Happiness }, { "hungry_days", HungryDays },
            { "adopted_day", AdoptedDay }, { "petted_day", PettedDay }, { "follow", Follow },
        };

        public static Pet FromJson(object o)
        {
            var d = J.Obj(o);
            string sp = J.S(d, "species", "");
            if (ShopData.PetSpecies(sp) == null) return null;
            float h = J.F(d, "happiness", 0.7f);
            if (float.IsNaN(h) || float.IsInfinity(h)) h = 0.7f;
            return new Pet
            {
                Species = sp, Name = J.S(d, "name", ShopData.PetSpecies(sp).DefaultName), Happiness = Mathx.Clamp01(h),
                HungryDays = Math.Max(0, J.I(d, "hungry_days")), AdoptedDay = J.I(d, "adopted_day", 1), PettedDay = J.I(d, "petted_day"),
                Follow = J.B(d, "follow"),
            };
        }
    }

    /// <summary>Tierart (Hund, Katze) mit Eigenschaften.</summary>
    public sealed class PetSpeciesDef
    {
        public string Id, Name, DefaultName, Bonus, Icon;
        public RGBA Color;
    }

    /// <summary>Feste Inhalte der Einkaufsstraße (Läden, Sortiment, Tiere, Regeln).</summary>
    public static class ShopData
    {
        public const string Electro = "elektro";
        public const string PetShop = "tier";

        public const string ElectroName = "MediaMarkd";
        public const string ElectroSlogan = "Ich bin doch nicht pleite!";
        public const string PetShopName = "Fressnix";
        public const string PetShopSlogan = "Alles fürs Tier. Fast alles.";
        public const string ClothesName = "Hype & Hoodie";

        /// <summary>Normalpreis im Laden = Großhandels-Stückpreis × diesen Faktor (Ladenmiete, Beratung, Kaffee).</summary>
        public const float RetailMarkup = 1.5f;
        /// <summary>Qualität der Ware aus dem Laden (Markenware, wie Standard-Großhändler).</summary>
        public const float RetailQuality = 1.0f;
        /// <summary>Höchstens so viele Packungen passen in Wagen/Korb.</summary>
        public const int CartCapacity = 12;
        /// <summary>Höchstens so viele Haustiere (je Art eins).</summary>
        public const int MaxPets = 2;
        /// <summary>Nach so vielen Tagen ohne Futter zieht ein Tier zu den Nachbarn.</summary>
        public const int RunawayDays = 3;
        /// <summary>Tägliche Bekanntheit durch ein gut gelauntes Tier (Tier-Content auf TikTak).</summary>
        public const float PetAwarenessPerDay = 0.006f;
        /// <summary>Streicheln (1× pro Tag und Tier): Bekanntheit und etwas Laune.</summary>
        public const float PetPetAwareness = 0.004f;
        public const int DogXpPerDay = 6;

        public static readonly PetSpeciesDef[] Species =
        {
            new PetSpeciesDef { Id = "hund", Name = "Hund", DefaultName = "Bello", Icon = "heart", Color = new RGBA(0.72f, 0.5f, 0.3f),
                Bonus = "Gassi gehen macht den Kopf frei: täglich etwas Erfahrung (XP). Gut gelaunt auch Bekanntheit." },
            new PetSpeciesDef { Id = "katze", Name = "Katze", DefaultName = "Mieze", Icon = "heart", Color = new RGBA(0.95f, 0.6f, 0.25f),
                Bonus = "Katzen-Content geht immer: gut gelaunt doppelte Bekanntheit auf TikTak." },
        };

        public static readonly StoreItemDef[] Items =
        {
            // ---- MediaMarkd: Ware für den eigenen Shop (geht ins Lager) ----
            W("mm_huelle", "huelle", 10, "Hüllen-Großpack", "10 Handyhüllen, Glitzer inklusive."),
            W("mm_led", "led", 5, "LED-Party-Set", "5 Lichterketten. Mehr Stimmung pro Steckdose."),
            W("mm_massage", "massage", 2, "Massage-Doppelpack", "Zwei Massagepistolen. Rücken? Erledigt."),
            W("mm_kopfhoerer", "kopfhoerer", 3, "Kopfhörer 3er", "Drei Paar Bluetooth-Kopfhörer, kabellos glücklich."),
            W("mm_ringlicht", "ringlicht", 2, "Ringlicht Duo", "Zwei Ringlichter. Für Influencer mit Ansprüchen."),
            W("mm_smartwatch", "smartwatch", 2, "Smartwatch 2er", "Zwei Smartwatches. Zählen Schritte, auch zum Kühlschrank."),
            W("mm_beamer", "beamer", 1, "Mini-Beamer", "Heimkino für die Garage."),
            W("mm_drohne", "drohne", 1, "Kamera-Drohne", "Fliegt. Meistens."),
            // ---- Fressnix: Tiere, Futter, Spielzeug, Ware ----
            new StoreItemDef { Id = "fn_hund", Store = PetShop, Kind = "tier", Product = "hund", Name = "Hund adoptieren", Short = "Hund", Icon = "heart",
                Price = 120, DailyStock = 1, Color = new RGBA(0.72f, 0.5f, 0.3f), Desc = "Bello sucht ein Zuhause. Schutzgebühr inkl. Leine." },
            new StoreItemDef { Id = "fn_katze", Store = PetShop, Kind = "tier", Product = "katze", Name = "Katze adoptieren", Short = "Katze", Icon = "heart",
                Price = 90, DailyStock = 1, Color = new RGBA(0.95f, 0.6f, 0.25f), Desc = "Mieze. Ignoriert dich professionell." },
            new StoreItemDef { Id = "fn_futter", Store = PetShop, Kind = "futter", Name = "Trockenfutter", Short = "Futter", Icon = "box",
                Pack = 7, Price = 12, DailyStock = 10, Color = new RGBA(0.85f, 0.65f, 0.3f), Desc = "7 Portionen. Schmeckt nach Pappe, sagen die Tiere." },
            new StoreItemDef { Id = "fn_premium", Store = PetShop, Kind = "futter", Name = "Gourmet-Nassfutter", Short = "Gourmet", Icon = "star",
                Pack = 7, Price = 22, DailyStock = 6, Color = new RGBA(0.75f, 0.25f, 0.4f), Desc = "7 Portionen. Bessere Laune als dein Mittagessen." },
            new StoreItemDef { Id = "fn_ball", Store = PetShop, Kind = "gadget", Name = "Quietsch-Ball", Short = "Ball", Icon = "sparkle",
                Price = 8, DailyStock = 3, Color = new RGBA(0.95f, 0.3f, 0.3f), Desc = "Quietscht. Immer. Auch nachts." },
            new StoreItemDef { Id = "fn_kratzbaum", Store = PetShop, Kind = "gadget", Name = "Kratzbaum XXL", Short = "Kratzbaum", Icon = "building",
                Price = 39, DailyStock = 2, Color = new RGBA(0.75f, 0.68f, 0.55f), Desc = "Rettet dein Sofa. Vielleicht." },
            new StoreItemDef { Id = "fn_halsband", Store = PetShop, Kind = "gadget", Name = "LED-Halsband", Short = "Halsband", Icon = "bulb",
                Price = 15, DailyStock = 3, Color = new RGBA(0.3f, 0.8f, 0.95f), Desc = "Leuchtet. Perfekt für TikTak-Videos bei Nacht." },
            new StoreItemDef { Id = "fn_bett", Store = PetShop, Kind = "gadget", Name = "Kuschelbett", Short = "Bett", Icon = "moon",
                Price = 29, DailyStock = 2, Color = new RGBA(0.55f, 0.45f, 0.85f), Desc = "Flauschiger als deine Matratze." },
            new StoreItemDef { Id = "fn_gps", Store = PetShop, Kind = "gadget", Name = "GPS-Halsband Pro", Short = "GPS", Icon = "signal",
                Price = 49, DailyStock = 2, Color = new RGBA(0.2f, 0.25f, 0.3f), Desc = "Weiß immer, wo dein Tier ist: auf dem Sofa." },
            new StoreItemDef { Id = "fn_automat", Store = PetShop, Kind = "gadget", Name = "Smart-Futterautomat", Short = "Automat", Icon = "phone",
                Price = 59, DailyStock = 2, Color = new RGBA(0.92f, 0.92f, 0.95f), Desc = "Füttert per App. Nur mit Abo und WLAN." },
            new StoreItemDef { Id = "fn_laser", Store = PetShop, Kind = "gadget", Name = "Laser-Spielzeug", Short = "Laser", Icon = "bolt",
                Price = 9, DailyStock = 4, Color = new RGBA(0.95f, 0.2f, 0.25f), Desc = "Roter Punkt. Stundenlange Unterhaltung." },
            new StoreItemDef { Id = "fn_brunnen", Store = PetShop, Kind = "ware", Product = "katzenbrunnen", Pack = 2, Name = "Trinkbrunnen Duo", Short = "Brunnen",
                Icon = "drop", DailyStock = 4, Color = new RGBA(0.36f, 0.78f, 0.84f), Desc = "2 Katzen-Trinkbrunnen – auch zum Weiterverkaufen im Shop." },
        };

        private static StoreItemDef W(string id, string product, int pack, string name, string desc)
        {
            var p = GameData.Product(product);
            return new StoreItemDef
            {
                Id = id, Store = Electro, Kind = "ware", Product = product, Pack = pack, Name = name, Short = p.Short, Icon = p.Icon,
                DailyStock = 8, Color = p.Color, Desc = desc,
            };
        }

        public static StoreItemDef Item(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var i in Items)
                if (i.Id == id) return i;
            return null;
        }

        public static List<StoreItemDef> ItemsOf(string store)
        {
            var l = new List<StoreItemDef>();
            foreach (var i in Items)
                if (i.Store == store) l.Add(i);
            return l;
        }

        public static PetSpeciesDef PetSpecies(string id)
        {
            foreach (var s in Species)
                if (s.Id == id) return s;
            return null;
        }

        public static string StoreName(string store) => store == PetShop ? PetShopName : ElectroName;

        /// <summary>Stabiler Hash (unabhängig von .NET-Version), für die Tagesaktionen.</summary>
        public static int Hash(int seed, int day, string salt)
        {
            unchecked
            {
                int h = (int)2166136261;
                h = (h ^ seed) * 16777619;
                h = (h ^ day) * 16777619;
                if (salt != null)
                    foreach (char c in salt)
                        h = (h ^ c) * 16777619;
                return h & 0x7fffffff;
            }
        }
    }

    public sealed partial class Sim
    {
        /// <summary>Zufallsbasis der Tagesaktionen (pro Spielstand, gespeichert).</summary>
        public int ShopSeed = 7331;
        /// <summary>Tag, für den <see cref="ShopBought"/> gilt.</summary>
        public int ShopDay;
        /// <summary>Heute gekaufte Packungen je Artikel (für den Tagesvorrat).</summary>
        public Dictionary<string, int> ShopBought = new Dictionary<string, int>();
        /// <summary>Einkaufswagen / Korb (beide Läden, Zeilen mit Laden über den Artikel). Nicht gespeichert.</summary>
        public List<CartLine> StoreCart = new List<CartLine>();
        public List<Pet> Pets = new List<Pet>();
        /// <summary>Futter-Portionen (normal / Gourmet).</summary>
        public int PetFood, PetFoodPremium;
        /// <summary>Gekauftes Tierspielzeug (Artikel-IDs).</summary>
        public HashSet<string> PetGadgets = new HashSet<string>();
        /// <summary>Summen für Statistik / Zeitung.</summary>
        public int ShopTotalSpent, ShopTotalSaved;

        /// <summary>Einkaufswagen oder Haustiere geändert (Welt: Wagen-Inhalt, Tiere neu aufbauen).</summary>
        public event Action ShopChanged;
        /// <summary>Haustier adoptiert / weggelaufen (Welt: Tier erzeugen / entfernen).</summary>
        public event Action PetsChanged;

        private readonly Dictionary<string, List<ShopDeal>> _dealCache = new Dictionary<string, List<ShopDeal>>();

        private void ResetShops()
        {
            ShopSeed = new Random().Next(1, 1000000);
            ShopDay = 0;
            ShopBought.Clear();
            StoreCart.Clear();
            Pets.Clear();
            PetFood = 0;
            PetFoodPremium = 0;
            PetGadgets.Clear();
            ShopTotalSpent = 0;
            ShopTotalSaved = 0;
            _dealCache.Clear();
        }

        private void RaiseShopChanged()
        {
            ShopChanged?.Invoke();
            RaiseEconomyChanged();
        }

        private void EnsureShopDay()
        {
            if (ShopDay == Day) return;
            ShopDay = Day;
            ShopBought.Clear();
        }

        // ---- Preise & Aktionen ---------------------------------------------------------------------
        /// <summary>Großhandelspreis (Standard-Großhändler, kleine Menge) für eine Packung dieses Artikels. 0 = kein Produkt.</summary>
        public float ShopWholesalePackPrice(string itemId)
        {
            var it = ShopData.Item(itemId);
            if (it == null || it.Kind != "ware" || !GameData.IsProduct(it.Product)) return 0f;
            return GameData.Product(it.Product).UnitCost * it.Pack * GameData.Suppliers[1].PriceMult * PurchasePriceMult();
        }

        /// <summary>Normalpreis pro Packung (ohne Aktion).</summary>
        public int ShopBasePrice(string itemId)
        {
            var it = ShopData.Item(itemId);
            if (it == null) return 0;
            if (it.Price > 0) return it.Price;
            if (it.Kind == "ware" && GameData.IsProduct(it.Product))
                return Math.Max(1, (int)Math.Ceiling(GameData.Product(it.Product).UnitCost * it.Pack * ShopData.RetailMarkup - 0.001f));
            return 1;
        }

        /// <summary>Tagesaktionen eines Ladens für einen Tag (deterministisch aus <see cref="ShopSeed"/>, verbraucht keinen Zufall der Sim).</summary>
        public List<ShopDeal> ShopDeals(string store, int day)
        {
            string key = store + ":" + day + ":" + ShopSeed;
            if (_dealCache.TryGetValue(key, out var cached)) return cached;
            var items = ShopData.ItemsOf(store);
            var rnd = new Random(ShopData.Hash(ShopSeed, day, store));
            var deals = new List<ShopDeal>();
            // Tiere selbst gibt es nie im Angebot (Schutzgebühr), alles andere schon.
            var pool = new List<StoreItemDef>();
            foreach (var i in items)
                if (i.Kind != "tier") pool.Add(i);
            int count = Math.Min(pool.Count, store == ShopData.Electro ? 3 : 2);
            float[] discounts = { 0.2f, 0.25f, 0.3f, 0.35f, 0.4f, 0.45f };
            for (int n = 0; n < count && pool.Count > 0; n++)
            {
                int idx = rnd.Next(pool.Count);
                var it = pool[idx];
                pool.RemoveAt(idx);
                bool mega = n == 0;
                float disc = mega ? 0.5f + (float)rnd.Next(0, 3) * 0.05f : discounts[rnd.Next(discounts.Length)];
                int limit = Math.Max(1, Math.Min(it.DailyStock, mega ? 3 + rnd.Next(0, 2) : 4 + rnd.Next(0, 3)));
                deals.Add(new ShopDeal { ItemId = it.Id, Discount = disc, Limit = limit, Mega = mega });
            }
            if (_dealCache.Count > 32) _dealCache.Clear();
            _dealCache[key] = deals;
            return deals;
        }

        public List<ShopDeal> ShopDealsToday(string store) => ShopDeals(store, Day);

        public ShopDeal ShopDealFor(string itemId, int day)
        {
            var it = ShopData.Item(itemId);
            if (it == null) return null;
            foreach (var d in ShopDeals(it.Store, day))
                if (d.ItemId == itemId) return d;
            return null;
        }

        public ShopDeal ShopDealFor(string itemId) => ShopDealFor(itemId, Day);

        /// <summary>Heutiger Preis pro Packung (mit Aktion).</summary>
        public int ShopPrice(string itemId)
        {
            int b = ShopBasePrice(itemId);
            var d = ShopDealFor(itemId);
            if (d == null) return b;
            return Math.Max(1, Mathx.RoundToInt(b * (1f - d.Discount)));
        }

        /// <summary>Ware heute billiger als beim Standard-Großhändler (pro Stück)?</summary>
        public bool ShopCheaperThanWholesale(string itemId)
        {
            float w = ShopWholesalePackPrice(itemId);
            return w > 0f && ShopPrice(itemId) < w - 0.001f;
        }

        /// <summary>Gibt es heute irgendwo Ware unter Großhandelspreis? (Badge der Zeitung)</summary>
        public bool ShopCheaperThanWholesaleAny()
        {
            foreach (var store in new[] { ShopData.Electro, ShopData.PetShop })
                foreach (var d in ShopDealsToday(store))
                    if (ShopCheaperThanWholesale(d.ItemId)) return true;
            return false;
        }

        /// <summary>Preis pro Stück heute (Ware) – für Scanner und Zeitung.</summary>
        public float ShopUnitPrice(string itemId)
        {
            var it = ShopData.Item(itemId);
            if (it == null) return 0f;
            return ShopPrice(itemId) / (float)Math.Max(1, it.Pack);
        }

        /// <summary>Packungen, die heute noch zu haben sind (Tagesvorrat − gekauft − im Wagen).</summary>
        public int ShopLeft(string itemId)
        {
            var it = ShopData.Item(itemId);
            if (it == null) return 0;
            EnsureShopDay();
            var d = ShopDealFor(itemId);
            int limit = d != null ? d.Limit : it.DailyStock;
            ShopBought.TryGetValue(itemId, out int bought);
            return Math.Max(0, limit - bought - CartQty(itemId));
        }

        // ---- Einkaufswagen -----------------------------------------------------------------------
        public int CartQty(string itemId)
        {
            int n = 0;
            foreach (var l in StoreCart)
                if (l.ItemId == itemId) n += l.Qty;
            return n;
        }

        public int CartCount(string store)
        {
            int n = 0;
            foreach (var l in StoreCart)
            {
                var it = ShopData.Item(l.ItemId);
                if (it != null && it.Store == store) n += l.Qty;
            }
            return n;
        }

        public int CartTotal(string store)
        {
            int sum = 0;
            foreach (var l in StoreCart)
            {
                var it = ShopData.Item(l.ItemId);
                if (it != null && it.Store == store) sum += l.UnitPrice * l.Qty;
            }
            return sum;
        }

        /// <summary>Ersparnis gegenüber Normalpreis im Wagen.</summary>
        public int CartSavings(string store)
        {
            int sum = 0;
            foreach (var l in StoreCart)
            {
                var it = ShopData.Item(l.ItemId);
                if (it != null && it.Store == store) sum += Math.Max(0, ShopBasePrice(l.ItemId) - l.UnitPrice) * l.Qty;
            }
            return sum;
        }

        public List<CartLine> CartLines(string store)
        {
            var list = new List<CartLine>();
            foreach (var l in StoreCart)
            {
                var it = ShopData.Item(l.ItemId);
                if (it != null && it.Store == store) list.Add(l);
            }
            return list;
        }

        /// <summary>Warum kann der Artikel nicht in den Wagen? "" = geht.</summary>
        public string ShopBlockReason(string itemId)
        {
            var it = ShopData.Item(itemId);
            if (it == null) return "Gibt's hier nicht.";
            if (StoryStage != "business") return "Erst die Schicht bei Kalle beenden.";
            if (ShopLeft(itemId) <= 0) return "Ausverkauft für heute.";
            if (CartCount(it.Store) >= ShopData.CartCapacity) return "Wagen voll (" + ShopData.CartCapacity + " Packungen).";
            switch (it.Kind)
            {
                case "ware":
                    if (!GameData.IsProduct(it.Product)) return "Gibt's hier nicht.";
                    if (!ProductUnlocked(it.Product)) return "Dein Shop darf das erst ab Level " + GameData.Product(it.Product).UnlockLevel + " verkaufen.";
                    if (!ProductHasShelf(it.Product)) return "Dafür hast du noch kein Regal (Lagerhalle).";
                    break;
                case "gadget":
                    if (PetGadgets.Contains(itemId) || CartQty(itemId) > 0) return "Hast du schon.";
                    if (Pets.Count == 0) return "Erst ein Tier adoptieren.";
                    break;
                case "futter":
                    if (Pets.Count == 0) return "Erst ein Tier adoptieren.";
                    break;
                case "tier":
                    if (HasPet(it.Product) || CartQty(itemId) > 0) return it.Product == "hund" ? "Du hast schon einen Hund." : "Du hast schon eine Katze.";
                    if (Pets.Count + CartPets() >= ShopData.MaxPets) return "Mehr Tiere passen nicht in die Garage.";
                    break;
            }
            return "";
        }

        private int CartPets()
        {
            int n = 0;
            foreach (var l in StoreCart)
            {
                var it = ShopData.Item(l.ItemId);
                if (it != null && it.Kind == "tier") n += l.Qty;
            }
            return n;
        }

        /// <summary>Eine Packung in den Wagen legen (Preis von heute wird gemerkt).</summary>
        public bool CartAdd(string itemId)
        {
            string why = ShopBlockReason(itemId);
            if (why != "")
            {
                Notify(why, "bad");
                Sound("error");
                return false;
            }
            int price = ShopPrice(itemId);
            var line = StoreCart.Find(l => l.ItemId == itemId && l.UnitPrice == price);
            if (line != null) line.Qty++;
            else StoreCart.Add(new CartLine { ItemId = itemId, Qty = 1, UnitPrice = price });
            Sound("pickup", 0.08f, -6f);
            RaiseShopChanged();
            return true;
        }

        /// <summary>Eine Packung zurück ins Regal.</summary>
        public bool CartRemove(string itemId)
        {
            for (int i = StoreCart.Count - 1; i >= 0; i--)
            {
                if (StoreCart[i].ItemId != itemId) continue;
                StoreCart[i].Qty--;
                if (StoreCart[i].Qty <= 0) StoreCart.RemoveAt(i);
                RaiseShopChanged();
                return true;
            }
            return false;
        }

        /// <summary>Wagen eines Ladens leeren (Ware zurück ins Regal, z. B. beim Verlassen ohne Bezahlen).</summary>
        public void CartClear(string store)
        {
            int before = StoreCart.Count;
            StoreCart.RemoveAll(l =>
            {
                var it = ShopData.Item(l.ItemId);
                return it == null || it.Store == store;
            });
            if (StoreCart.Count != before) RaiseShopChanged();
        }

        /// <summary>
        /// Kasse: bezahlt alles aus diesem Laden. Ware kommt als Kiste an den Wareneingang (Lieferservice),
        /// Futter in den Napf-Vorrat, Spielzeug zu den Tieren, Tiere werden adoptiert. false bei zu wenig Geld / leerem Wagen.
        /// </summary>
        public bool ShopCheckout(string store)
        {
            var lines = CartLines(store);
            if (lines.Count == 0)
            {
                Notify("Der Wagen ist leer.", "info");
                return false;
            }
            int total = CartTotal(store);
            if (Money < total)
            {
                Notify("Karte abgelehnt! Dir fehlen " + Fmt.Money(total - Money) + ".", "bad");
                Sound("error");
                return false;
            }
            EnsureShopDay();
            int saved = CartSavings(store);
            int wareCost = 0, otherCost = 0;
            var crates = new Dictionary<string, int>();
            foreach (var l in lines)
            {
                var it = ShopData.Item(l.ItemId);
                if (it == null) continue;
                ShopBought.TryGetValue(it.Id, out int b);
                ShopBought[it.Id] = b + l.Qty;
                int cost = l.UnitPrice * l.Qty;
                switch (it.Kind)
                {
                    case "ware":
                        wareCost += cost;
                        crates.TryGetValue(it.Product, out int q);
                        crates[it.Product] = q + it.Pack * l.Qty;
                        break;
                    case "futter":
                        otherCost += cost;
                        if (it.Id == "fn_premium") PetFoodPremium += it.Pack * l.Qty;
                        else PetFood += it.Pack * l.Qty;
                        break;
                    case "gadget":
                        otherCost += cost;
                        PetGadgets.Add(it.Id);
                        break;
                    case "tier":
                        otherCost += cost;
                        AdoptPet(it.Product);
                        break;
                }
            }
            if (wareCost > 0) Spend(wareCost, "purchases");
            if (otherCost > 0) Spend(otherCost, "other");
            foreach (var kv in crates) DockCrates.Add(ItemData.Crate(kv.Key, kv.Value, ShopData.RetailQuality));
            StoreCart.RemoveAll(l => lines.Contains(l));
            ShopTotalSpent += total;
            ShopTotalSaved += saved;
            string msg = "Bezahlt: " + Fmt.Money(total) + " bei " + ShopData.StoreName(store) + (saved > 0 ? " (gespart: " + Fmt.Money(saved) + ")" : "") + ".";
            if (crates.Count > 0) msg += " Ware liefert der Lieferservice an deinen Wareneingang.";
            Notify(msg, "good");
            Sound("register");
            RaiseShopChanged();
            return true;
        }

        // ---- Haustiere -----------------------------------------------------------------------------
        public bool HasPet(string species)
        {
            foreach (var p in Pets)
                if (p.Species == species) return true;
            return false;
        }

        public Pet PetOf(string species)
        {
            foreach (var p in Pets)
                if (p.Species == species) return p;
            return null;
        }

        /// <summary>Tier aufnehmen (Kasse ruft das auf; Admin/Tests direkt). null, wenn nicht möglich.</summary>
        public Pet AdoptPet(string species)
        {
            var sp = ShopData.PetSpecies(species);
            if (sp == null || HasPet(species) || Pets.Count >= ShopData.MaxPets) return null;
            var pet = new Pet { Species = species, Name = sp.DefaultName, Happiness = 0.7f, AdoptedDay = Day, Follow = true };
            Pets.Add(pet);
            Notify(pet.Name + " (" + sp.Name + ") zieht bei dir ein! Futter nicht vergessen.", "good");
            PetsChanged?.Invoke();
            return pet;
        }

        /// <summary>Admin/Test: alle Tiere entfernen.</summary>
        public void RemoveAllPets()
        {
            Pets.Clear();
            PetsChanged?.Invoke();
            RaiseShopChanged();
        }

        public int PetFoodTotal() => PetFood + PetFoodPremium;

        /// <summary>Bonus-Faktor durch Spielzeug (1 + 0,25 je Gadget).</summary>
        public float PetGadgetMult() => 1f + 0.25f * PetGadgets.Count;

        public bool PetHappy(Pet p) => p != null && p.Happiness >= 0.5f;

        public string PetMoodText(Pet p)
        {
            if (p == null) return "";
            if (p.HungryDays > 0) return "hungrig";
            if (p.Happiness >= 0.85f) return "überglücklich";
            if (p.Happiness >= 0.5f) return "gut gelaunt";
            if (p.Happiness >= 0.25f) return "mürrisch";
            return "traurig";
        }

        /// <summary>Streicheln: einmal pro Tag und Tier etwas Laune und Bekanntheit (Foto für TikTak).</summary>
        public bool PetPet(string species)
        {
            var p = PetOf(species);
            if (p == null) return false;
            if (p.PettedDay == Day)
            {
                Notify(p.Name + " hat heute schon genug Streicheleinheiten.", "info");
                return false;
            }
            p.PettedDay = Day;
            p.Happiness = Mathx.Clamp01(p.Happiness + 0.1f);
            AddAwareness(ShopData.PetPetAwareness * PetGadgetMult());
            Notify(p.Name + " schnurrt/wedelt. Dein Foto davon sammelt Likes.", "good");
            Sound("coin", 0.1f, -8f);
            RaiseShopChanged();
            return true;
        }

        /// <summary>Tier wohnt in der Garage oder läuft mit.</summary>
        public void PetSetFollow(string species, bool follow)
        {
            var p = PetOf(species);
            if (p == null || p.Follow == follow) return;
            p.Follow = follow;
            Notify(follow ? p.Name + " kommt mit." : p.Name + " bleibt in der Garage.", "info");
            PetsChanged?.Invoke();
            RaiseShopChanged();
        }

        /// <summary>
        /// Tageswechsel: jedes Tier frisst eine Portion (Gourmet zuerst), Laune ändert sich, gut gelaunte
        /// Tiere bringen Bekanntheit (Katze doppelt) bzw. XP (Hund). Nach <see cref="ShopData.RunawayDays"/>
        /// Tagen ohne Futter zieht ein Tier zu den Nachbarn.
        /// </summary>
        public void PetsNewDay()
        {
            if (Pets.Count == 0) return;
            var gone = new List<Pet>();
            float gadget = PetGadgetMult();
            foreach (var p in Pets)
            {
                if (PetFoodPremium > 0)
                {
                    PetFoodPremium--;
                    p.HungryDays = 0;
                    p.Happiness = Mathx.Clamp01(p.Happiness + 0.25f);
                }
                else if (PetFood > 0)
                {
                    PetFood--;
                    p.HungryDays = 0;
                    p.Happiness = Mathx.Clamp01(p.Happiness + 0.12f);
                }
                else
                {
                    p.HungryDays++;
                    p.Happiness = Mathx.Clamp01(p.Happiness - 0.3f);
                }
                p.Happiness = Mathx.Clamp01(p.Happiness + 0.02f * PetGadgets.Count - 0.04f);
                if (p.HungryDays >= ShopData.RunawayDays)
                {
                    gone.Add(p);
                    continue;
                }
                if (p.HungryDays > 0)
                {
                    Notify(p.Name + " hat Hunger! Futter gibt's bei " + ShopData.PetShopName + ".", "bad");
                    continue;
                }
                if (PetHappy(p))
                {
                    float aw = ShopData.PetAwarenessPerDay * gadget * (p.Species == "katze" ? 2f : 1f);
                    AddAwareness(aw);
                    if (p.Species == "hund") AddXp(ShopData.DogXpPerDay);
                    Daily.Notes.Add(p.Name + " war gut drauf: Tier-Content bringt Bekanntheit.");
                }
            }
            foreach (var p in gone)
            {
                Pets.Remove(p);
                Notify(p.Name + " ist zu Oma Hildegard nebenan gezogen. Da gibt's wenigstens Futter.", "bad");
                Daily.Notes.Add(p.Name + " ist weggelaufen (kein Futter).");
            }
            if (gone.Count > 0) PetsChanged?.Invoke();
        }

        /// <summary>Tageswechsel der Läden: Wagen leeren, Tagesvorrat zurücksetzen, Tiere versorgen.</summary>
        private void ShopsNewDay()
        {
            StoreCart.Clear();
            ShopDay = Day;
            ShopBought.Clear();
            PetsNewDay();
            ShopChanged?.Invoke();
        }

        // ---- Speichern -------------------------------------------------------------------------
        private Dictionary<string, object> ShopsToJson()
        {
            var bought = new Dictionary<string, object>();
            foreach (var kv in ShopBought) bought[kv.Key] = kv.Value;
            return new Dictionary<string, object>
            {
                { "seed", ShopSeed }, { "day", ShopDay }, { "bought", bought }, { "pets", JList(Pets, p => p.ToJson()) },
                { "food", PetFood }, { "food_premium", PetFoodPremium }, { "gadgets", Flags(PetGadgets) },
                { "spent", ShopTotalSpent }, { "saved", ShopTotalSaved },
            };
        }

        /// <summary>Fehlt der Block (alter Spielstand), bleiben die Standardwerte aus ResetState.</summary>
        private void ShopsFromJson(Dictionary<string, object> d)
        {
            _dealCache.Clear();
            StoreCart.Clear();
            if (d == null || d.Count == 0) return;
            ShopSeed = J.I(d, "seed", ShopSeed);
            ShopDay = J.I(d, "day");
            ShopBought.Clear();
            foreach (var kv in J.O(d, "bought"))
                if (ShopData.Item(kv.Key) != null) ShopBought[kv.Key] = Math.Max(0, J.I(kv.Value));
            Pets.Clear();
            foreach (var o in J.A(d, "pets"))
            {
                var p = Pet.FromJson(o);
                if (p != null && !HasPet(p.Species) && Pets.Count < ShopData.MaxPets) Pets.Add(p);
            }
            PetFood = Math.Max(0, J.I(d, "food"));
            PetFoodPremium = Math.Max(0, J.I(d, "food_premium"));
            ReadFlags(d, "gadgets", PetGadgets);
            PetGadgets.RemoveWhere(id => ShopData.Item(id) == null || ShopData.Item(id).Kind != "gadget");
            ShopTotalSpent = Math.Max(0, J.I(d, "spent"));
            ShopTotalSaved = Math.Max(0, J.I(d, "saved"));
        }
    }
}
