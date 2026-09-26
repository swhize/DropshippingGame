using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    public enum ItemKind
    {
        None = 0,
        Crate = 1,
        Item = 2,
        Package = 3,
        Labeled = 4,
        Plate = 5,
        /// <summary>v3.0: Retourenpaket. Kommt am Wareneingang an (<see cref="Sim.DockReturns"/>) und wird am Retourenplatz bearbeitet.</summary>
        Return = 6,
    }

    /// <summary>
    /// Ein Gegenstand: Kiste, Einzelartikel, Paket, etikettiertes Paket, Teller oder Retoure.
    /// Wird in der Hand gehalten, liegt am Boden, im Regal-Eingang oder im Pakete-Stapel.
    /// Einzelartikel und Pakete tragen die Daten ihres Bestellzettels mit sich (OrderId, Kundschaft, Fälligkeit).
    /// </summary>
    public sealed class ItemData
    {
        public ItemKind Kind;
        public string Product = "";
        public int Quantity;
        public float Quality = 1f;
        public int Price;
        public float Created;
        public RGBA Color = new RGBA(0.9f, 0.3f, 0.3f);
        public int Logo;
        public int Table;

        // ---- v3.0: Bestellzettel, Retouren, Großaufträge --------------------------------------
        /// <summary>Nummer des Bestellzettels (0 = keiner, z. B. Kiste oder Gegenstand aus einem alten Spielstand).</summary>
        public int OrderId;
        /// <summary>Kundschaft laut Bestellzettel, z. B. "Sabine K." (auch für das Versandlabel).</summary>
        public string Customer = "";
        /// <summary>Wohnort der Kundschaft, z. B. "Bottrop".</summary>
        public string City = "";
        /// <summary>Express-Bestellung (kürzere Frist, höherer Preis, stärkerer Einfluss auf die Bewertung).</summary>
        public bool Express;
        /// <summary>Fälligkeit auf der Geschäftsuhr (<see cref="Sim.BClock"/>). 0 = unbekannt (alter Spielstand).</summary>
        public float DueAt;
        /// <summary>Großauftrag, für den dieser Einzelartikel aus dem Regal genommen wurde (0 = keiner).</summary>
        public int ContractId;
        /// <summary>Tatsächlich benutzte Kartongröße 0-2 (S/M/L) oder -1 = passend zum Produkt.</summary>
        public int PackSize = -1;
        /// <summary>Freitext, bei Retouren der Rücksendegrund.</summary>
        public string Note = "";

        public ItemData Clone() => (ItemData)MemberwiseClone();

        /// <summary>Kartongröße, in der das Paket steckt (0-2). Ohne Produkt: M.</summary>
        public int EffectivePackSize => PackSize >= 0 ? PackSize : (Product != "" ? GameData.Product(Product).Size : 1);

        /// <summary>Paket steckt in einem zu großen Karton (höheres Retourenrisiko).</summary>
        public bool Oversized => Product != "" && PackSize > GameData.Product(Product).Size;

        /// <summary>Übernimmt die Bestellzettel-Daten (OrderId, Kundschaft, Express, Fälligkeit, Großauftrag).</summary>
        public ItemData CopyTicketFrom(ItemData o)
        {
            if (o == null) return this;
            OrderId = o.OrderId;
            Customer = o.Customer ?? "";
            City = o.City ?? "";
            Express = o.Express;
            DueAt = o.DueAt;
            ContractId = o.ContractId;
            return this;
        }

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "kind", (int)Kind }, { "product", Product }, { "quantity", Quantity }, { "quality", (double)Quality },
            { "price", Price }, { "created", (double)Created }, { "color", Color.ToJson() }, { "logo", Logo }, { "table", Table },
            { "order_id", OrderId }, { "customer", Customer ?? "" }, { "city", City ?? "" }, { "express", Express },
            { "due_at", (double)DueAt }, { "contract_id", ContractId }, { "pack_size", PackSize }, { "note", Note ?? "" },
        };

        public static ItemData FromJson(object o)
        {
            var d = J.Obj(o);
            return new ItemData
            {
                Kind = (ItemKind)J.I(d, "kind", 0),
                Product = J.S(d, "product", ""),
                Quantity = J.I(d, "quantity"),
                Quality = J.F(d, "quality", 1f),
                Price = J.I(d, "price"),
                Created = J.F(d, "created"),
                Color = RGBA.FromJson(J.Get(d, "color"), new RGBA(0.9f, 0.3f, 0.3f)),
                Logo = J.I(d, "logo"),
                Table = J.I(d, "table"),
                OrderId = J.I(d, "order_id"),
                Customer = J.S(d, "customer", ""),
                City = J.S(d, "city", ""),
                Express = J.B(d, "express"),
                DueAt = J.F(d, "due_at"),
                ContractId = J.I(d, "contract_id"),
                PackSize = Mathx.Clamp(J.I(d, "pack_size", -1), -1, 2),
                Note = J.S(d, "note", ""),
            };
        }

        public static ItemData Crate(string product, int quantity, float quality) =>
            new ItemData { Kind = ItemKind.Crate, Product = product, Quantity = quantity, Quality = quality };

        public string Describe()
        {
            string pname = Product != "" ? GameData.Product(Product).Name : "";
            switch (Kind)
            {
                case ItemKind.Crate: return $"Kiste {Quantity}× {pname}";
                case ItemKind.Item: return ContractId > 0 ? $"{pname} (Großauftrag)" : pname;
                case ItemKind.Package: return $"Paket ({pname})";
                case ItemKind.Labeled: return $"Versandfertiges Paket ({pname})";
                case ItemKind.Plate: return "Teller";
                case ItemKind.Return: return $"Retoure ({pname})";
            }
            return "Gegenstand";
        }
    }

    public sealed class Delivery
    {
        public string Product;
        public int Quantity;
        public float Quality;
        public float ArriveAt;
        public bool VanSent;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "product", Product }, { "quantity", Quantity }, { "quality", (double)Quality }, { "arrive_at", (double)ArriveAt }, { "van_sent", VanSent },
        };

        public static Delivery FromJson(object o)
        {
            var d = J.Obj(o);
            return new Delivery
            {
                Product = J.S(d, "product", "huelle"), Quantity = J.I(d, "quantity"), Quality = J.F(d, "quality", 1f),
                ArriveAt = J.F(d, "arrive_at"), VanSent = J.B(d, "van_sent"),
            };
        }
    }

    public sealed class StockEntry
    {
        public int Qty;
        public float Quality = 1f;
    }

    /// <summary>Bearbeitungsstand eines Bestellzettels.</summary>
    public enum OrderStage
    {
        /// <summary>Wartet in der Warteschlange (<see cref="Sim.OrderQueue"/>).</summary>
        Queued = 0,
        /// <summary>Artikel aus dem Regal genommen (liegt in <see cref="Sim.OrdersInWork"/>).</summary>
        Picked = 1,
        /// <summary>Verpackt.</summary>
        Packed = 2,
        /// <summary>Versandlabel gedruckt.</summary>
        Labeled = 3,
        /// <summary>Liegt auf dem Förderband.</summary>
        Conveyor = 4,
    }

    /// <summary>
    /// Ein Bestellzettel: Produkt, Preis, Kundschaft, Notiz, Fälligkeit und optional Express.
    /// Alle Zeiten auf der Geschäftsuhr (<see cref="Sim.BClock"/>).
    /// </summary>
    public sealed class Order
    {
        public string Product;
        public int Price;
        public float Created;

        // ---- v3.0 Bestellzettel ---------------------------------------------------------------
        /// <summary>Fortlaufende Nummer (ab 1001), angezeigt als <see cref="Number"/>.</summary>
        public int Id;
        public string Customer = "";
        public string City = "";
        /// <summary>Flavor-Notiz der Kundschaft ("Bitte nicht klingeln – Baby schläft.").</summary>
        public string Note = "";
        public bool Express;
        /// <summary>Fälligkeit: bis dahin sollte das Paket verschickt sein.</summary>
        public float DueAt;
        public OrderStage Stage = OrderStage.Queued;

        /// <summary>Zeitpunkt, an dem eine noch wartende Bestellung storniert wird.</summary>
        public float ExpiresAt => Created + (Express ? GameData.ExpressExpireMinutes : GameData.OrderExpireMinutes);

        /// <summary>Länge der Frist (Fälligkeit minus Eingang) in Minuten.</summary>
        public float DueWindow => DueAt > Created ? DueAt - Created : (Express ? GameData.ExpressDueMinutes : GameData.OrderDueMinutes);

        /// <summary>true, sobald der Artikel aus dem Regal genommen wurde.</summary>
        public bool InWork => Stage != OrderStage.Queued;

        /// <summary>Anzeige-Nummer, z. B. "#1042".</summary>
        public string Number => "#" + Id;

        /// <summary>Artikel für diesen Bestellzettel (Einzelartikel mit allen Zettel-Daten).</summary>
        public ItemData ToItem(float quality) => new ItemData
        {
            Kind = ItemKind.Item, Product = Product, Price = Price, Quality = quality, Created = Created,
            OrderId = Id, Customer = Customer ?? "", City = City ?? "", Express = Express, DueAt = DueAt,
        };

        /// <summary>Bestellzettel aus einem Artikel/Paket rekonstruieren (z. B. Zurücklegen ins Regal).</summary>
        public static Order FromItem(ItemData item) => new Order
        {
            Id = item.OrderId, Product = item.Product, Price = item.Price, Created = item.Created, Customer = item.Customer ?? "",
            City = item.City ?? "", Express = item.Express,
            DueAt = item.DueAt > 0f ? item.DueAt : item.Created + (item.Express ? GameData.ExpressDueMinutes : GameData.OrderDueMinutes),
        };

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "product", Product }, { "price", Price }, { "created", (double)Created },
            { "id", Id }, { "customer", Customer ?? "" }, { "city", City ?? "" }, { "note", Note ?? "" }, { "express", Express },
            { "due_at", (double)DueAt }, { "stage", (int)Stage },
        };

        public static Order FromJson(object o)
        {
            var d = J.Obj(o);
            var order = new Order
            {
                Product = J.S(d, "product", "huelle"), Price = J.I(d, "price"), Created = J.F(d, "created"),
                Id = J.I(d, "id"), Customer = J.S(d, "customer", ""), City = J.S(d, "city", ""), Note = J.S(d, "note", ""),
                Express = J.B(d, "express"), DueAt = J.F(d, "due_at"), Stage = (OrderStage)Mathx.Clamp(J.I(d, "stage"), 0, 4),
            };
            if (!GameData.IsProduct(order.Product)) order.Product = "huelle";
            if (order.DueAt <= 0f) order.DueAt = order.Created + (order.Express ? GameData.ExpressDueMinutes : GameData.OrderDueMinutes);
            return order;
        }
    }

    public sealed class Boost
    {
        public string Source, Name, Product = "";
        public float Mult, EndsAt;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "source", Source }, { "name", Name }, { "mult", (double)Mult }, { "ends_at", (double)EndsAt }, { "product", Product },
        };

        public static Boost FromJson(object o)
        {
            var d = J.Obj(o);
            return new Boost
            {
                Source = J.S(d, "source"), Name = J.S(d, "name"), Mult = J.F(d, "mult", 1f),
                EndsAt = J.F(d, "ends_at"), Product = J.S(d, "product", ""),
            };
        }
    }

    public sealed class StaffMember
    {
        public string Role, Name;
        public float Progress;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "role", Role }, { "name", Name }, { "progress", (double)Progress },
        };

        public static StaffMember FromJson(object o)
        {
            var d = J.Obj(o);
            return new StaffMember { Role = J.S(d, "role", "packer"), Name = J.S(d, "name", "Aushilfe"), Progress = J.F(d, "progress") };
        }
    }

    public sealed class ConveyorEntry
    {
        public int Id;
        public ItemData Pkg;
        public float DoneAt;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "id", Id }, { "pkg", Pkg.ToJson() }, { "done_at", (double)DoneAt },
        };

        public static ConveyorEntry FromJson(object o)
        {
            var d = J.Obj(o);
            return new ConveyorEntry { Id = J.I(d, "id"), Pkg = ItemData.FromJson(J.Get(d, "pkg")), DoneAt = J.F(d, "done_at") };
        }
    }

    public sealed class Review
    {
        public int Stars, Day;
        public string Text, Name, Product;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "stars", Stars }, { "text", Text }, { "name", Name }, { "product", Product }, { "day", Day },
        };

        public static Review FromJson(object o)
        {
            var d = J.Obj(o);
            return new Review
            {
                Stars = J.I(d, "stars", 3), Text = J.S(d, "text"), Name = J.S(d, "name"),
                Product = J.S(d, "product", "huelle"), Day = J.I(d, "day", 1),
            };
        }
    }

    public sealed class HistoryEntry
    {
        public int Day, Revenue, Profit, Shipped;
        /// <summary>v3.0: Retouren und Großaufträge des Tages.</summary>
        public int Returns, Contracts;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "day", Day }, { "revenue", Revenue }, { "profit", Profit }, { "shipped", Shipped }, { "returns", Returns }, { "contracts", Contracts },
        };

        public static HistoryEntry FromJson(object o)
        {
            var d = J.Obj(o);
            return new HistoryEntry
            {
                Day = J.I(d, "day"), Revenue = J.I(d, "revenue"), Profit = J.I(d, "profit"), Shipped = J.I(d, "shipped"),
                Returns = J.I(d, "returns"), Contracts = J.I(d, "contracts"),
            };
        }
    }

    /// <summary>Tageswerte für die Abrechnung um 20 Uhr.</summary>
    public sealed class DailyStats
    {
        public int Revenue, Purchases, Packaging, Marketing, Other, IncomeOther, Shipped, Lost, Orders, Trading, Stand, XpStart;
        public float RepStart;

        // ---- v3.0 ------------------------------------------------------------------------------
        /// <summary>Verschickte Express-Pakete / verspätet verschickte Pakete / stornierte (abgelaufene) Bestellungen.</summary>
        public int Express, Late, Expired;
        /// <summary>Eingetroffene Retouren, deren Erstattungen (€), als B-Ware eingelagert, entsorgt.</summary>
        public int Returns, Refunds, ReturnsRestocked, ReturnsDisposed;
        /// <summary>Großaufträge: Einnahmen (sind auch im Umsatz enthalten), Vertragsstrafen (€), erfüllt, geplatzt.</summary>
        public int ContractIncome, Penalties, ContractsDone, ContractsFailed;
        /// <summary>Wochenziele heute geschafft und deren Geldbelohnung (ist auch in IncomeOther enthalten).</summary>
        public int ChallengesDone, ChallengeRewards;
        /// <summary>Heute gepostete TikToks (max. <see cref="GameData.TikTokPerDay"/>).</summary>
        public int TikToks;
        /// <summary>Kurze Meldungen des Tages für den Kassenbon.</summary>
        public List<string> Notes = new List<string>();

        public void Add(string category, int amount)
        {
            switch (category)
            {
                case "revenue": Revenue += amount; break;
                case "purchases": Purchases += amount; break;
                case "packaging": Packaging += amount; break;
                case "marketing": Marketing += amount; break;
                case "other": Other += amount; break;
                case "income_other": IncomeOther += amount; break;
                case "trading": Trading += amount; break;
                case "stand": Stand += amount; break;
                case "refunds": Refunds += amount; break;
                case "penalties": Penalties += amount; break;
            }
        }

        /// <summary>Ergebnis des Tages ohne Fixkosten (Umsatz + Sonstiges − variable Kosten).</summary>
        public int VariableProfit() => Revenue + IncomeOther - Purchases - Packaging - Marketing - Other - Refunds - Penalties;

        public Dictionary<string, object> ToJson()
        {
            var notes = new List<object>();
            foreach (var n in Notes) notes.Add(n);
            return new Dictionary<string, object>
            {
                { "revenue", Revenue }, { "purchases", Purchases }, { "packaging", Packaging }, { "marketing", Marketing },
                { "other", Other }, { "income_other", IncomeOther }, { "shipped", Shipped }, { "lost", Lost }, { "orders", Orders },
                { "trading", Trading }, { "stand", Stand }, { "rep_start", (double)RepStart }, { "xp_start", XpStart },
                { "express", Express }, { "late", Late }, { "expired", Expired }, { "returns", Returns }, { "refunds", Refunds },
                { "returns_restocked", ReturnsRestocked }, { "returns_disposed", ReturnsDisposed }, { "contract_income", ContractIncome },
                { "penalties", Penalties }, { "contracts_done", ContractsDone }, { "contracts_failed", ContractsFailed },
                { "challenges_done", ChallengesDone }, { "challenge_rewards", ChallengeRewards }, { "notes", notes }, { "tiktoks", TikToks },
            };
        }

        public static DailyStats FromJson(object o, DailyStats fallback)
        {
            var d = o as Dictionary<string, object>;
            if (d == null || d.Count == 0) return fallback;
            var s = new DailyStats
            {
                Revenue = J.I(d, "revenue"), Purchases = J.I(d, "purchases"), Packaging = J.I(d, "packaging"),
                Marketing = J.I(d, "marketing"), Other = J.I(d, "other"), IncomeOther = J.I(d, "income_other"),
                Shipped = J.I(d, "shipped"), Lost = J.I(d, "lost"), Orders = J.I(d, "orders"), Trading = J.I(d, "trading"),
                Stand = J.I(d, "stand"), RepStart = J.F(d, "rep_start", fallback.RepStart), XpStart = J.I(d, "xp_start", fallback.XpStart),
                Express = J.I(d, "express"), Late = J.I(d, "late"), Expired = J.I(d, "expired"), Returns = J.I(d, "returns"),
                Refunds = J.I(d, "refunds"), ReturnsRestocked = J.I(d, "returns_restocked"), ReturnsDisposed = J.I(d, "returns_disposed"),
                ContractIncome = J.I(d, "contract_income"), Penalties = J.I(d, "penalties"), ContractsDone = J.I(d, "contracts_done"),
                ContractsFailed = J.I(d, "contracts_failed"), ChallengesDone = J.I(d, "challenges_done"),
                ChallengeRewards = J.I(d, "challenge_rewards"), TikToks = J.I(d, "tiktoks"),
            };
            foreach (var n in J.A(d, "notes"))
                if (n is string ns) s.Notes.Add(ns);
            return s;
        }
    }

    /// <summary>Tagesabrechnung, die beim Feierabend angezeigt wird.</summary>
    public sealed class DaySummary
    {
        public int Day, Revenue, IncomeOther, Purchases, Packaging, Marketing, Other, Rent, Wages, Upkeep, Interest;
        public int Shipped, Lost, Trading, Stand, Profit, MoneyAfter, XpGained, Portfolio, Debt;
        public float RepStart, RepEnd;
        public bool Bankrupt;

        // ---- v3.0 ------------------------------------------------------------------------------
        /// <summary>Wochentag des abgerechneten Tages (0 = Montag ... 6 = Sonntag).</summary>
        public int Weekday;
        public string WeekdayName = "";
        /// <summary>Express-Pakete, verspätete Pakete, stornierte Bestellungen.</summary>
        public int Express, Late, Expired;
        /// <summary>Retouren: eingetroffen, Erstattungen (€, Kosten), als B-Ware eingelagert, entsorgt, noch unterwegs.</summary>
        public int Returns, Refunds, ReturnsRestocked, ReturnsDisposed, ReturnsIncoming;
        /// <summary>Großaufträge: Einnahmen (bereits im Umsatz enthalten), Vertragsstrafen (€, Kosten), erfüllt, geplatzt, noch aktiv.</summary>
        public int ContractIncome, Penalties, ContractsDone, ContractsFailed, ContractsActive;
        /// <summary>Wochenziele heute geschafft, deren Geldbelohnung (bereits in IncomeOther enthalten).</summary>
        public int ChallengesDone, ChallengeRewards;
        /// <summary>Heute gepostete TikToks (max. <see cref="GameData.TikTokPerDay"/>).</summary>
        public int TikToks;
        /// <summary>Sonntag: Wochenbilanz der Wochenziele.</summary>
        public bool WeekEnded;
        public int WeekChallengesDone, WeekChallengesTotal;
        /// <summary>Kurze deutsche Meldungen für den Kassenbon (Großaufträge, Wochenziele, Trends ...).</summary>
        public List<string> Notes = new List<string>();
    }

    /// <summary>Abgelegter Gegenstand in der Welt (nur für Speichern/Laden).</summary>
    public sealed class WorldItemSave
    {
        public ItemData Item;
        public V3 Pos;
        public float RotY;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "item", Item.ToJson() }, { "pos", Pos.ToJson() }, { "rot", (double)RotY },
        };

        public static WorldItemSave FromJson(object o)
        {
            var d = J.Obj(o);
            return new WorldItemSave { Item = ItemData.FromJson(J.Get(d, "item")), Pos = V3.FromJson(J.Get(d, "pos")), RotY = J.F(d, "rot") };
        }
    }

    public sealed class PlayerSave
    {
        public V3 Pos;
        public float RotY;
        public ItemData Held;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "pos", Pos.ToJson() }, { "rot", (double)RotY }, { "held", Held != null ? Held.ToJson() : null },
        };

        public static PlayerSave FromJson(object o)
        {
            var d = o as Dictionary<string, object>;
            if (d == null || !d.ContainsKey("pos")) return null;
            var held = J.Get(d, "held");
            return new PlayerSave { Pos = V3.FromJson(J.Get(d, "pos")), RotY = J.F(d, "rot"), Held = held != null ? ItemData.FromJson(held) : null };
        }
    }

    public sealed class SaveSummary
    {
        public int Slot, Day, Money, Level;
        public string Brand, Story;
        public long SavedUnix;
        /// <summary>v3.0: Spielstand-Version, Wochentag (0 = Montag), Bewertung, Standort (0 = Garage, 1 = Lagerhalle).</summary>
        public int Version, Weekday, Stage;
        public float Rating;
    }
}
