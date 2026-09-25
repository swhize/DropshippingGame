using System;
using System.Collections.Generic;
using System.IO;

namespace DropshippingGame.Core
{
    /// <summary>Speichern / Laden als JSON. Drei Spielstände (Slots 1-3).</summary>
    public sealed partial class Sim
    {
        /// <summary>Wird vor dem Speichern aufgerufen, damit die Welt Spielerposition und abgelegte Gegenstände eintragen kann.</summary>
        public Action CollectWorldState;

        public string SavePath(int slot) => Path.Combine(SaveDir, "savegame_" + slot + ".json");

        public bool HasSave(int slot) => !string.IsNullOrEmpty(SaveDir) && File.Exists(SavePath(slot));

        public bool HasAnySave()
        {
            for (int s = 1; s <= SaveSlots; s++)
                if (HasSave(s)) return true;
            return false;
        }

        /// <summary>Slot mit dem jüngsten Spielstand oder 0.</summary>
        public int MostRecentSlot()
        {
            int best = 0;
            long bestTime = -1;
            for (int s = 1; s <= SaveSlots; s++)
            {
                var sum = ReadSummary(s);
                if (sum != null && sum.SavedUnix > bestTime)
                {
                    best = s;
                    bestTime = sum.SavedUnix;
                }
            }
            return best;
        }

        private Dictionary<string, object> ReadSaveFile(int slot)
        {
            if (!HasSave(slot)) return null;
            try
            {
                string text = File.ReadAllText(SavePath(slot));
                return Json.TryParse(text, out object o) ? o as Dictionary<string, object> : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public SaveSummary ReadSummary(int slot)
        {
            var s = ReadSaveFile(slot);
            if (s == null) return null;
            int day = J.I(s, "day", 1);
            return new SaveSummary
            {
                Slot = slot, Day = day, Money = J.I(s, "money"), Brand = J.S(s, "brand_name", ""),
                Level = J.I(s, "level", 1), Story = J.S(s, "story_stage", "business"), SavedUnix = (long)J.F(s, "saved_unix"),
                Version = J.I(s, "version", 1), Weekday = GameData.WeekdayOf(day), Rating = J.F(s, "reputation", 3f),
                Stage = J.I(s, "location_stage"),
            };
        }

        private static List<object> JList<T>(IEnumerable<T> items, Func<T, object> f)
        {
            var l = new List<object>();
            foreach (var i in items) l.Add(f(i));
            return l;
        }

        private static Dictionary<string, object> Flags(IEnumerable<string> set)
        {
            var d = new Dictionary<string, object>();
            foreach (var k in set) d[k] = true;
            return d;
        }

        private static void ReadFlags(Dictionary<string, object> d, string key, HashSet<string> into)
        {
            into.Clear();
            foreach (var kv in J.O(d, key))
                if (kv.Value is bool b && b) into.Add(kv.Key);
        }

        public Dictionary<string, object> ToJson()
        {
            var stock = new Dictionary<string, object>();
            foreach (var kv in Stock) stock[kv.Key] = new Dictionary<string, object> { { "qty", kv.Value.Qty }, { "quality", (double)kv.Value.Quality } };
            var listed = new Dictionary<string, object>();
            foreach (var kv in Listed) listed[kv.Key] = kv.Value;
            var prices = new Dictionary<string, object>();
            foreach (var kv in ShopPrices) prices[kv.Key] = kv.Value;
            var shippedPer = new Dictionary<string, object>();
            foreach (var kv in ShippedPerProduct) shippedPer[kv.Key] = kv.Value;
            var blocked = new Dictionary<string, object>();
            foreach (var kv in BlockedSuppliers) blocked[kv.Key.ToString()] = kv.Value;
            var stand = new Dictionary<string, object>();
            foreach (var kv in StandStock) stand[kv.Key] = kv.Value;
            var brand = new List<object>();
            foreach (var b in PackagingBrand) brand.Add(new Dictionary<string, object> { { "color", b.Color.ToJson() }, { "logo", b.Logo } });

            var state = new Dictionary<string, object>
            {
                { "version", SaveVersion }, { "money", Money }, { "day", Day }, { "time_minutes", (double)TimeMinutes },
                { "story_stage", StoryStage }, { "intro_step", IntroStep }, { "tutorial_step", TutorialStep },
                { "tut_flags", Flags(TutFlags) }, { "location_stage", LocationStage }, { "upgrades", Flags(Upgrades) },
                { "decor_owned", Flags(DecorOwned) }, { "lifestyle_owned", Flags(LifestyleOwned) }, { "reputation", (double)Reputation },
                { "review_count", ReviewCount }, { "reviews", JList(Reviews, r => r.ToJson()) }, { "xp", Xp }, { "level", Level },
                { "awareness", (double)Awareness }, { "goals_done", Flags(GoalsDone) }, { "brand_named", BrandNamed },
                { "ending_seen", EndingSeen }, { "brand_name", BrandName }, { "brand_logo_index", BrandLogoIndex },
                { "brand_color", BrandColor.ToJson() }, { "traveling_deliveries", JList(TravelingDeliveries, d => d.ToJson()) },
                { "dock_crates", JList(DockCrates, c => c.ToJson()) }, { "stock", stock }, { "express_delivery", ExpressDelivery },
                { "blocked_suppliers", blocked }, { "damaged_next_crate", (double)DamagedNextCrate },
                { "order_queue", JList(OrderQueue, o => o.ToJson()) }, { "packed_packages", JList(PackedPackages, p => p.ToJson()) },
                { "packaging", new List<object> { Packaging[0], Packaging[1], Packaging[2] } },
                { "flat_packaging", new List<object> { FlatPackaging[0], FlatPackaging[1], FlatPackaging[2] } },
                { "packaging_brand", brand }, { "listed", listed }, { "shop_prices", prices }, { "promo_active", PromoActive },
                { "shop_offline_until", (double)ShopOfflineUntil }, { "boosts", JList(Boosts, b => b.ToJson()) },
                { "tiktok_ready_at", (double)TikTokReadyAt }, { "staff", JList(Staff, s => s.ToJson()) },
                { "social_posted_day", SocialPostedDay }, { "conveyor_queue", JList(ConveyorQueue, c => c.ToJson()) },
                { "world_items", JList(WorldItems, w => w.ToJson()) }, { "player_state", PlayerState != null ? PlayerState.ToJson() : null },
                { "debt", Debt }, { "stand_stock", stand }, { "stand_sold_total", StandSoldTotal },
                { "total_shipped", TotalShipped }, { "total_earned", TotalEarned }, { "total_lost_orders", TotalLostOrders },
                { "shipped_per_product", shippedPer }, { "daily", Daily.ToJson() }, { "history", JList(History, h => h.ToJson()) },
                { "market", Market.ToJson() }, { "events", Events.ToJson() },
                { "saved_unix", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            };
            AddV4State(state);
            return state;
        }

        /// <summary>v4 (v3.0-Update): Bestellzettel, Retouren, Trends, Großaufträge, Skills, Wochenziele.</summary>
        private void AddV4State(Dictionary<string, object> state)
        {
            var launch = new Dictionary<string, object>();
            foreach (var kv in LaunchOrders) launch[kv.Key] = (double)kv.Value;
            var lastBuy = new Dictionary<string, object>();
            foreach (var kv in LastPurchase)
                if (kv.Value != null && kv.Value.Length >= 2) lastBuy[kv.Key] = new List<object> { kv.Value[0], kv.Value[1] };
            var retPer = new Dictionary<string, object>();
            foreach (var kv in ReturnsPerProduct) retPer[kv.Key] = kv.Value;
            var offerTimes = new List<object>();
            foreach (var t in ContractOfferTimes) offerTimes.Add((double)t);

            state["explained"] = Flags(Explained);
            state["orders_in_work"] = JList(OrdersInWork, o => o.ToJson());
            state["next_order_id"] = NextOrderId;
            state["launch_orders"] = launch;
            state["express_boost_until"] = (double)ExpressBoostUntil;
            state["express_boost_chance"] = (double)ExpressBoostChance;
            state["last_purchase"] = lastBuy;
            state["returns_incoming"] = JList(ReturnsIncoming, r => r.ToJson());
            state["dock_returns"] = JList(DockReturns, r => r.ToJson());
            state["total_returns"] = TotalReturns;
            state["total_refunds"] = TotalRefunds;
            state["total_returns_restocked"] = TotalReturnsRestocked;
            state["total_returns_disposed"] = TotalReturnsDisposed;
            state["returns_per_product"] = retPer;
            state["trends"] = Trends.ToJson();
            state["contracts"] = JList(Contracts, c => c.ToJson());
            state["next_contract_id"] = NextContractId;
            state["total_contracts_done"] = TotalContractsDone;
            state["total_contracts_failed"] = TotalContractsFailed;
            state["contract_offer_times"] = offerTimes;
            state["skills"] = Flags(Skills);
            state["bonus_skill_points"] = BonusSkillPoints;
            state["challenges"] = JList(Challenges, c => c.ToJson());
            state["challenge_week"] = ChallengeWeek;
            state["total_challenges_done"] = TotalChallengesDone;
        }

        /// <summary>Speichert in den aktuellen Slot. Das Imbiss-Intro wird nicht gespeichert.</summary>
        public bool SaveGame(bool silent = false)
        {
            if (StoryStage == "diner" || string.IsNullOrEmpty(SaveDir)) return false;
            CollectWorldState?.Invoke();
            var state = ToJson();
            try
            {
                Directory.CreateDirectory(SaveDir);
                string path = SavePath(Slot);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, Json.Write(state));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception)
            {
                Notify("Speichern fehlgeschlagen!", "bad");
                return false;
            }
            LastSavedUnix = (long)state["saved_unix"];
            if (!silent) Notify("Spiel gespeichert (Spielstand " + Slot + ").", "good");
            return true;
        }

        public bool LoadGame(int slot)
        {
            var s = ReadSaveFile(slot);
            if (s == null) return false;
            ResetState();
            Slot = slot;
            FromJson(s);
            return true;
        }

        public void FromJson(Dictionary<string, object> s)
        {
            Money = J.I(s, "money", Money);
            Day = J.I(s, "day", 1);
            TimeMinutes = J.F(s, "time_minutes", GameData.DayStart);
            StoryStage = J.S(s, "story_stage", "business");
            IntroStep = J.I(s, "intro_step", 3);
            TutorialStep = J.I(s, "tutorial_step", -1);
            ReadFlags(s, "tut_flags", TutFlags);
            LocationStage = Mathx.Clamp(J.I(s, "location_stage", 0), 0, 1);
            ReadFlags(s, "upgrades", Upgrades);
            ReadFlags(s, "decor_owned", DecorOwned);
            ReadFlags(s, "lifestyle_owned", LifestyleOwned);
            Reputation = J.F(s, "reputation", 3f);
            ReviewCount = J.I(s, "review_count");
            Reviews.Clear();
            foreach (var r in J.A(s, "reviews")) Reviews.Add(Review.FromJson(r));
            Xp = J.I(s, "xp");
            Level = Mathx.Clamp(J.I(s, "level", 1), 1, GameData.MaxLevel);
            Awareness = J.F(s, "awareness");
            ReadFlags(s, "goals_done", GoalsDone);
            BrandNamed = J.B(s, "brand_named");
            EndingSeen = J.B(s, "ending_seen");
            BrandName = J.S(s, "brand_name", "MeinShop");
            BrandLogoIndex = J.I(s, "brand_logo_index");
            BrandColor = RGBA.FromJson(J.Get(s, "brand_color"), BrandColor);
            TravelingDeliveries.Clear();
            foreach (var d in J.A(s, "traveling_deliveries")) TravelingDeliveries.Add(Delivery.FromJson(d));
            DockCrates.Clear();
            foreach (var c in J.A(s, "dock_crates")) DockCrates.Add(ItemData.FromJson(c));
            var stock = J.O(s, "stock");
            var listed = J.O(s, "listed");
            var prices = J.O(s, "shop_prices");
            var shippedPer = J.O(s, "shipped_per_product");
            var stand = J.O(s, "stand_stock");
            foreach (var p in GameData.Products)
            {
                if (stock.TryGetValue(p.Id, out object so))
                {
                    var sd = J.Obj(so);
                    Stock[p.Id] = new StockEntry { Qty = J.I(sd, "qty"), Quality = J.F(sd, "quality", 1f) };
                }
                if (listed.TryGetValue(p.Id, out object lo) && lo is bool lb) Listed[p.Id] = lb;
                if (prices.TryGetValue(p.Id, out object po)) ShopPrices[p.Id] = J.I(po, ShopPrices[p.Id]);
                if (shippedPer.TryGetValue(p.Id, out object spo)) ShippedPerProduct[p.Id] = J.I(spo);
                if (stand.TryGetValue(p.Id, out object sto)) StandStock[p.Id] = J.I(sto);
            }
            ExpressDelivery = J.B(s, "express_delivery");
            BlockedSuppliers.Clear();
            foreach (var kv in J.O(s, "blocked_suppliers"))
                if (int.TryParse(kv.Key, out int idx)) BlockedSuppliers[idx] = J.I(kv.Value);
            DamagedNextCrate = J.F(s, "damaged_next_crate");
            OrderQueue.Clear();
            foreach (var o in J.A(s, "order_queue")) OrderQueue.Add(Order.FromJson(o));
            PackedPackages.Clear();
            foreach (var p in J.A(s, "packed_packages")) PackedPackages.Add(ItemData.FromJson(p));
            var pack = J.A(s, "packaging");
            var flat = J.A(s, "flat_packaging");
            for (int i = 0; i < 3; i++)
            {
                if (i < pack.Count) Packaging[i] = J.I(pack[i]);
                if (i < flat.Count) FlatPackaging[i] = J.I(flat[i]);
            }
            var pb = J.A(s, "packaging_brand");
            if (pb.Count == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    var bd = J.Obj(pb[i]);
                    PackagingBrand[i] = new PrintDesign { Color = RGBA.FromJson(J.Get(bd, "color"), BrandColor), Logo = J.I(bd, "logo") };
                }
            }
            PromoActive = J.B(s, "promo_active");
            ShopOfflineUntil = J.F(s, "shop_offline_until", -1f);
            Boosts.Clear();
            foreach (var b in J.A(s, "boosts")) Boosts.Add(Boost.FromJson(b));
            TikTokReadyAt = J.F(s, "tiktok_ready_at");
            Staff.Clear();
            foreach (var st in J.A(s, "staff")) Staff.Add(StaffMember.FromJson(st));
            SocialPostedDay = J.I(s, "social_posted_day");
            ConveyorQueue.Clear();
            foreach (var c in J.A(s, "conveyor_queue")) ConveyorQueue.Add(ConveyorEntry.FromJson(c));
            foreach (var c in ConveyorQueue) _conveyorId = Math.Max(_conveyorId, c.Id);
            WorldItems.Clear();
            foreach (var w in J.A(s, "world_items")) WorldItems.Add(WorldItemSave.FromJson(w));
            PlayerState = PlayerSave.FromJson(J.Get(s, "player_state"));
            Debt = Math.Max(0, J.I(s, "debt"));
            StandSoldTotal = J.I(s, "stand_sold_total");
            TotalShipped = J.I(s, "total_shipped");
            TotalEarned = J.I(s, "total_earned");
            TotalLostOrders = J.I(s, "total_lost_orders");
            History.Clear();
            foreach (var h in J.A(s, "history")) History.Add(HistoryEntry.FromJson(h));
            Daily = DailyStats.FromJson(J.Get(s, "daily"), new DailyStats { RepStart = Reputation, XpStart = Xp });
            LastSavedUnix = (long)J.F(s, "saved_unix");
            Market.FromJson(J.O(s, "market"));
            Events.FromJson(J.O(s, "events"));
            DayOver = false;
            if (TimeMinutes >= GameData.DayEnd) TimeMinutes = GameData.DayEnd - 1f;
            ReadV4State(s);
            RaiseEconomyChanged();
        }

        /// <summary>Liest die v4-Felder. Fehlen sie (Spielstand v3), gelten sinnvolle Standardwerte.</summary>
        private void ReadV4State(Dictionary<string, object> s)
        {
            ReadFlags(s, "explained", Explained);
            OrdersInWork.Clear();
            foreach (var o in J.A(s, "orders_in_work"))
            {
                var ord = Order.FromJson(o);
                if (ord.Stage == OrderStage.Queued) ord.Stage = OrderStage.Picked;
                OrdersInWork.Add(ord);
            }
            NextOrderId = Math.Max(GameData.FirstOrderId, J.I(s, "next_order_id", GameData.FirstOrderId));
            LaunchOrders.Clear();
            foreach (var kv in J.O(s, "launch_orders"))
                if (GameData.IsProduct(kv.Key)) LaunchOrders[kv.Key] = J.F(kv.Value);
            ExpressBoostUntil = J.F(s, "express_boost_until", -1f);
            ExpressBoostChance = J.F(s, "express_boost_chance");
            LastPurchase.Clear();
            foreach (var kv in J.O(s, "last_purchase"))
                if (GameData.IsProduct(kv.Key) && kv.Value is List<object> l && l.Count >= 2)
                    LastPurchase[kv.Key] = new[] { J.I(l[0]), J.I(l[1]) };

            ReturnsIncoming.Clear();
            foreach (var r in J.A(s, "returns_incoming")) ReturnsIncoming.Add(PendingReturn.FromJson(r));
            DockReturns.Clear();
            foreach (var r in J.A(s, "dock_returns"))
            {
                var item = ItemData.FromJson(r);
                item.Kind = ItemKind.Return;
                if (GameData.IsProduct(item.Product)) DockReturns.Add(item);
            }
            TotalReturns = J.I(s, "total_returns");
            TotalRefunds = J.I(s, "total_refunds");
            TotalReturnsRestocked = J.I(s, "total_returns_restocked");
            TotalReturnsDisposed = J.I(s, "total_returns_disposed");
            ReturnsPerProduct.Clear();
            foreach (var kv in J.O(s, "returns_per_product"))
                if (GameData.IsProduct(kv.Key)) ReturnsPerProduct[kv.Key] = J.I(kv.Value);

            Trends.FromJson(J.O(s, "trends"));

            Contracts.Clear();
            foreach (var c in J.A(s, "contracts")) Contracts.Add(Contract.FromJson(c));
            NextContractId = J.I(s, "next_contract_id", 1);
            foreach (var c in Contracts) NextContractId = Math.Max(NextContractId, c.Id + 1);
            TotalContractsDone = J.I(s, "total_contracts_done");
            TotalContractsFailed = J.I(s, "total_contracts_failed");
            ContractOfferTimes.Clear();
            foreach (var t in J.A(s, "contract_offer_times")) ContractOfferTimes.Add(J.F(t));
            ContractOfferTimes.Sort();

            ReadFlags(s, "skills", Skills);
            Skills.RemoveWhere(id => GameData.Skill(id) == null);
            BonusSkillPoints = Math.Max(0, J.I(s, "bonus_skill_points"));

            Challenges.Clear();
            foreach (var c in J.A(s, "challenges")) Challenges.Add(WeeklyChallenge.FromJson(c));
            ChallengeWeek = J.I(s, "challenge_week");
            TotalChallengesDone = J.I(s, "total_challenges_done");

            FixupLegacyOrders();
            ReconcileOrdersInWork();
            // Alte Spielstände (v3) bekommen sofort Wochenziele für die laufende Woche (anteilig).
            int version = J.I(s, "version", 1);
            if (StoryStage == "business" && (version < 4 || (ChallengeWeek > 0 && ChallengeWeek != Week))) StartWeek(false);
        }

        public void DeleteSave(int slot)
        {
            try
            {
                if (HasSave(slot)) File.Delete(SavePath(slot));
            }
            catch (Exception)
            {
                // Löschen ist nicht kritisch
            }
        }
    }
}
