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
    }

    /// <summary>
    /// Ein Gegenstand: Kiste, Einzelartikel, Paket, etikettiertes Paket oder Teller.
    /// Wird in der Hand gehalten, liegt am Boden, im Regal-Eingang oder im Pakete-Stapel.
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

        public ItemData Clone() => (ItemData)MemberwiseClone();

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "kind", (int)Kind }, { "product", Product }, { "quantity", Quantity }, { "quality", (double)Quality },
            { "price", Price }, { "created", (double)Created }, { "color", Color.ToJson() }, { "logo", Logo }, { "table", Table },
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
                case ItemKind.Item: return pname;
                case ItemKind.Package: return $"Paket ({pname})";
                case ItemKind.Labeled: return $"Versandfertiges Paket ({pname})";
                case ItemKind.Plate: return "Teller";
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

    public sealed class Order
    {
        public string Product;
        public int Price;
        public float Created;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "product", Product }, { "price", Price }, { "created", (double)Created },
        };

        public static Order FromJson(object o)
        {
            var d = J.Obj(o);
            return new Order { Product = J.S(d, "product", "huelle"), Price = J.I(d, "price"), Created = J.F(d, "created") };
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

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "day", Day }, { "revenue", Revenue }, { "profit", Profit }, { "shipped", Shipped },
        };

        public static HistoryEntry FromJson(object o)
        {
            var d = J.Obj(o);
            return new HistoryEntry { Day = J.I(d, "day"), Revenue = J.I(d, "revenue"), Profit = J.I(d, "profit"), Shipped = J.I(d, "shipped") };
        }
    }

    /// <summary>Tageswerte für die Abrechnung um 20 Uhr.</summary>
    public sealed class DailyStats
    {
        public int Revenue, Purchases, Packaging, Marketing, Other, IncomeOther, Shipped, Lost, Orders, Trading, Stand, XpStart;
        public float RepStart;

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
            }
        }

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "revenue", Revenue }, { "purchases", Purchases }, { "packaging", Packaging }, { "marketing", Marketing },
            { "other", Other }, { "income_other", IncomeOther }, { "shipped", Shipped }, { "lost", Lost }, { "orders", Orders },
            { "trading", Trading }, { "stand", Stand }, { "rep_start", (double)RepStart }, { "xp_start", XpStart },
        };

        public static DailyStats FromJson(object o, DailyStats fallback)
        {
            var d = o as Dictionary<string, object>;
            if (d == null || d.Count == 0) return fallback;
            return new DailyStats
            {
                Revenue = J.I(d, "revenue"), Purchases = J.I(d, "purchases"), Packaging = J.I(d, "packaging"),
                Marketing = J.I(d, "marketing"), Other = J.I(d, "other"), IncomeOther = J.I(d, "income_other"),
                Shipped = J.I(d, "shipped"), Lost = J.I(d, "lost"), Orders = J.I(d, "orders"), Trading = J.I(d, "trading"),
                Stand = J.I(d, "stand"), RepStart = J.F(d, "rep_start", fallback.RepStart), XpStart = J.I(d, "xp_start", fallback.XpStart),
            };
        }
    }

    /// <summary>Tagesabrechnung, die beim Feierabend angezeigt wird.</summary>
    public sealed class DaySummary
    {
        public int Day, Revenue, IncomeOther, Purchases, Packaging, Marketing, Other, Rent, Wages, Upkeep, Interest;
        public int Shipped, Lost, Trading, Stand, Profit, MoneyAfter, XpGained, Portfolio, Debt;
        public float RepStart, RepEnd;
        public bool Bankrupt;
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
    }
}
