using System;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Einkaufsviertel: MediaMarkd (Ware fürs Lager), Fressnix (Tiere, Futter), Tagesaktionen, Spielstand.</summary>
    public class ShopsTests
    {
        private static Sim Shop(int seed = 1)
        {
            var sim = TestUtil.Fresh(seed);
            sim.ShopSeed = 4242;
            sim.Money = 5000;
            return sim;
        }

        [Test]
        public void Content_AllItemsValid()
        {
            var ids = new HashSet<string>();
            foreach (var it in ShopData.Items)
            {
                Assert.IsTrue(ids.Add(it.Id), "doppelte ID " + it.Id);
                Assert.IsTrue(it.Store == ShopData.Electro || it.Store == ShopData.PetShop, it.Id);
                Assert.Greater(it.Pack, 0, it.Id);
                Assert.Greater(it.DailyStock, 0, it.Id);
                if (it.Kind == "ware") Assert.IsTrue(GameData.IsProduct(it.Product), it.Id);
                if (it.Kind == "tier") Assert.IsNotNull(ShopData.PetSpecies(it.Product), it.Id);
            }
            Assert.Greater(ShopData.ItemsOf(ShopData.Electro).Count, 4);
            Assert.Greater(ShopData.ItemsOf(ShopData.PetShop).Count, 4);
        }

        [Test]
        public void Prices_NormalPriceAboveWholesale()
        {
            var sim = Shop();
            foreach (var it in ShopData.Items)
            {
                if (it.Kind != "ware") continue;
                Assert.Greater(sim.ShopBasePrice(it.Id), sim.ShopWholesalePackPrice(it.Id), it.Id);
            }
        }

        [Test]
        public void Deals_DeterministicAndSometimesCheaperThanWholesale()
        {
            var a = Shop();
            var b = Shop();
            for (int day = 1; day <= 10; day++)
            {
                var da = a.ShopDeals(ShopData.Electro, day);
                var db = b.ShopDeals(ShopData.Electro, day);
                Assert.AreEqual(da.Count, db.Count);
                for (int i = 0; i < da.Count; i++)
                {
                    Assert.AreEqual(da[i].ItemId, db[i].ItemId);
                    Assert.AreEqual(da[i].Discount, db[i].Discount, 0.0001f);
                }
                Assert.AreEqual(3, da.Count);
                Assert.IsTrue(da[0].Mega);
            }
            // Über einen Monat gibt es mindestens einen Tag, an dem Ware billiger ist als beim Großhändler,
            // aber nicht jede Aktion ist es.
            int cheaper = 0, deals = 0;
            for (int day = 1; day <= 30; day++)
            {
                a.Day = day;
                foreach (var d in a.ShopDealsToday(ShopData.Electro))
                {
                    deals++;
                    if (a.ShopCheaperThanWholesale(d.ItemId)) cheaper++;
                    Assert.Less(a.ShopPrice(d.ItemId), a.ShopBasePrice(d.ItemId));
                }
            }
            Assert.Greater(cheaper, 0);
            Assert.Less(cheaper, deals);
        }

        [Test]
        public void Deals_NeverDiscountPets()
        {
            var sim = Shop();
            for (int day = 1; day <= 40; day++)
                foreach (var d in sim.ShopDeals(ShopData.PetShop, day))
                    Assert.AreNotEqual("tier", ShopData.Item(d.ItemId).Kind);
        }

        [Test]
        public void Checkout_WareGoesToDockAsCrate()
        {
            var sim = Shop();
            int before = sim.Money;
            Assert.IsTrue(sim.CartAdd("mm_huelle"));
            Assert.IsTrue(sim.CartAdd("mm_huelle"));
            int total = sim.CartTotal(ShopData.Electro);
            Assert.AreEqual(sim.ShopPrice("mm_huelle") * 2, total);
            Assert.IsTrue(sim.ShopCheckout(ShopData.Electro));
            Assert.AreEqual(before - total, sim.Money);
            Assert.AreEqual(total, sim.Daily.Purchases);
            Assert.AreEqual(1, sim.DockCrates.Count);
            Assert.AreEqual("huelle", sim.DockCrates[0].Product);
            Assert.AreEqual(20, sim.DockCrates[0].Quantity);
            Assert.AreEqual(0, sim.CartCount(ShopData.Electro));
            // Kiste lässt sich wie jede Lieferung einräumen
            var crate = sim.PickupCrate();
            Assert.IsTrue(sim.UnboxCrate(crate.Product, crate.Quantity, crate.Quality));
            Assert.AreEqual(20, sim.StockQty("huelle"));
        }

        [Test]
        public void Checkout_NotEnoughMoney_ChangesNothing()
        {
            var sim = Shop();
            sim.CartAdd("mm_huelle");
            sim.Money = 1;
            Assert.IsFalse(sim.ShopCheckout(ShopData.Electro));
            Assert.AreEqual(1, sim.Money);
            Assert.AreEqual(0, sim.DockCrates.Count);
            Assert.AreEqual(1, sim.CartCount(ShopData.Electro));
        }

        [Test]
        public void Cart_RespectsLockedProductsAndDailyStock()
        {
            var sim = Shop();
            Assert.IsFalse(sim.CartAdd("mm_drohne"), "Drohne erst ab Level 10 + Lagerhalle");
            Assert.AreNotEqual("", sim.ShopBlockReason("mm_drohne"));
            int left = sim.ShopLeft("mm_huelle");
            Assert.Greater(left, 0);
            int added = 0;
            for (int i = 0; i < 40; i++)
                if (sim.CartAdd("mm_huelle")) added++;
            Assert.AreEqual(Math.Min(left, ShopData.CartCapacity), added);
            Assert.AreEqual(0, Math.Min(sim.ShopLeft("mm_huelle"), ShopData.CartCapacity - sim.CartCount(ShopData.Electro)));
            Assert.IsTrue(sim.CartRemove("mm_huelle"));
            Assert.AreEqual(added - 1, sim.CartQty("mm_huelle"));
        }

        [Test]
        public void DailyStock_ResetsNextDay_AndCartClears()
        {
            var sim = Shop();
            sim.NewGame("skip");
            sim.Money = 5000;
            int left = sim.ShopLeft("mm_led");
            sim.AddXp(GameData.LevelThreshold(2));
            for (int i = 0; i < left; i++) sim.CartAdd("mm_led");
            sim.ShopCheckout(ShopData.Electro);
            Assert.AreEqual(0, sim.ShopLeft("mm_led"));
            sim.CartAdd("mm_huelle");
            sim.EndDayNow();
            sim.StartNextDay();
            Assert.AreEqual(0, sim.StoreCart.Count);
            Assert.Greater(sim.ShopLeft("mm_led"), 0);
        }

        [Test]
        public void Cart_LeavingWithoutPaying_Clears()
        {
            var sim = Shop();
            sim.CartAdd("mm_huelle");
            sim.CartAdd("fn_brunnen"); // Katzenbrunnen gesperrt (Level 6) → kommt nicht rein
            Assert.AreEqual(1, sim.StoreCart.Count);
            sim.CartClear(ShopData.Electro);
            Assert.AreEqual(0, sim.StoreCart.Count);
        }

        [Test]
        public void PetShop_AdoptFeedAndBonus()
        {
            var sim = Shop();
            Assert.IsFalse(sim.CartAdd("fn_futter"), "Futter erst mit Tier");
            Assert.IsTrue(sim.CartAdd("fn_katze"));
            Assert.IsFalse(sim.CartAdd("fn_katze"), "nur eine Katze");
            Assert.IsTrue(sim.ShopCheckout(ShopData.PetShop));
            Assert.IsTrue(sim.HasPet("katze"));
            Assert.AreEqual(90, sim.Daily.Other);
            Assert.IsTrue(sim.CartAdd("fn_futter"));
            Assert.IsTrue(sim.CartAdd("fn_ball"));
            Assert.IsFalse(sim.CartAdd("fn_ball"), "Gadget nur einmal");
            Assert.IsTrue(sim.ShopCheckout(ShopData.PetShop));
            Assert.AreEqual(7, sim.PetFood);
            Assert.IsTrue(sim.PetGadgets.Contains("fn_ball"));

            float aw = sim.Awareness;
            sim.PetsNewDay();
            Assert.AreEqual(6, sim.PetFood);
            Assert.Greater(sim.Awareness, aw);
            Assert.AreEqual(0, sim.PetOf("katze").HungryDays);

            // Streicheln einmal pro Tag
            Assert.IsTrue(sim.PetPet("katze"));
            Assert.IsFalse(sim.PetPet("katze"));
        }

        [Test]
        public void Pets_RunAwayAfterDaysWithoutFood()
        {
            var sim = Shop();
            sim.AdoptPet("hund");
            Assert.AreEqual(1, sim.Pets.Count);
            for (int i = 0; i < ShopData.RunawayDays - 1; i++) sim.PetsNewDay();
            Assert.AreEqual(1, sim.Pets.Count);
            Assert.AreEqual(ShopData.RunawayDays - 1, sim.PetOf("hund").HungryDays);
            sim.PetsNewDay();
            Assert.AreEqual(0, sim.Pets.Count);
        }

        [Test]
        public void Pets_MaxTwoOneOfEach()
        {
            var sim = Shop();
            Assert.IsNotNull(sim.AdoptPet("hund"));
            Assert.IsNull(sim.AdoptPet("hund"));
            Assert.IsNotNull(sim.AdoptPet("katze"));
            Assert.IsNull(sim.AdoptPet("pferd"));
            Assert.AreEqual(2, sim.Pets.Count);
        }

        [Test]
        public void DogGivesXp_WhenHappyAndFed()
        {
            var sim = Shop();
            sim.AdoptPet("hund");
            sim.PetFood = 3;
            int xp = sim.Xp;
            sim.PetsNewDay();
            Assert.Greater(sim.Xp, xp);
        }

        [Test]
        public void Save_RoundTripKeepsPetsAndShopState()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds_tests_shops_" + Guid.NewGuid().ToString("N"));
            try
            {
                var gm = new Sim(3) { SaveDir = dir, Slot = 2 };
                gm.NewGame("skip");
                gm.ShopSeed = 999;
                gm.Money = 4000;
                gm.AdoptPet("katze");
                gm.PetOf("katze").Name = "Garfield";
                gm.PetSetFollow("katze", false);
                gm.PetFood = 5;
                gm.PetFoodPremium = 2;
                gm.PetGadgets.Add("fn_kratzbaum");
                gm.CartAdd("mm_huelle");
                gm.ShopCheckout(ShopData.Electro);
                Assert.IsTrue(gm.SaveGame(true));

                var g2 = new Sim(5) { SaveDir = dir };
                Assert.IsTrue(g2.LoadGame(2));
                Assert.AreEqual(999, g2.ShopSeed);
                Assert.AreEqual(1, g2.Pets.Count);
                Assert.AreEqual("Garfield", g2.Pets[0].Name);
                Assert.IsFalse(g2.Pets[0].Follow);
                Assert.AreEqual(5, g2.PetFood);
                Assert.AreEqual(2, g2.PetFoodPremium);
                Assert.IsTrue(g2.PetGadgets.Contains("fn_kratzbaum"));
                Assert.AreEqual(1, g2.ShopBought["mm_huelle"]);
                Assert.AreEqual(gm.ShopTotalSpent, g2.ShopTotalSpent);
                Assert.AreEqual(gm.ShopLeft("mm_huelle"), g2.ShopLeft("mm_huelle"));
            }
            finally
            {
                try
                {
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                }
                catch (Exception)
                {
                    // egal
                }
            }
        }

        [Test]
        public void Save_OldSaveWithoutShopsBlockLoads()
        {
            var gm = new Sim(2);
            gm.NewGame("skip");
            var json = gm.ToJson();
            json.Remove("shops");
            var g2 = new Sim(4);
            g2.ResetState();
            g2.FromJson(Json.Parse(Json.Write(json)) as Dictionary<string, object>);
            Assert.AreEqual(0, g2.Pets.Count);
            Assert.AreEqual(0, g2.PetFood);
            Assert.AreEqual(0, g2.StoreCart.Count);
            Assert.Greater(g2.ShopLeft("mm_huelle"), 0);
            // Kaputte Tiere werden ignoriert
            json["shops"] = new Dictionary<string, object>
            {
                { "pets", new List<object> { new Dictionary<string, object> { { "species", "drache" } }, "quatsch" } }, { "food", -5 },
            };
            var g3 = new Sim(4);
            g3.ResetState();
            g3.FromJson(Json.Parse(Json.Write(json)) as Dictionary<string, object>);
            Assert.AreEqual(0, g3.Pets.Count);
            Assert.AreEqual(0, g3.PetFood);
        }
    }
}
