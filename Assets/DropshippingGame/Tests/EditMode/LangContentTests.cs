using System.Collections.Generic;
using System.Reflection;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Englische Inhalte (EnContent): Jeder Name-/Beschreibungs-/Text-Inhalt aus GameData*, ShopData, Ads.Content
    /// (inkl. SlopGen), FinanceText, Achievements, Difficulty, Vehicles und TikTokStudio muss bei aktivem Englisch
    /// übersetzt werden – so, wie er zur Laufzeit angezeigt wird (mit eingesetzten Produkten, Tasten, Firmen).
    /// Ausnahmen nur über die Liste <see cref="Same"/> (Eigennamen, Marken, Wörter, die im Englischen gleich sind).
    /// </summary>
    public class LangContentTests
    {
        /// <summary>Texte, die im Englischen absichtlich gleich bleiben.</summary>
        private static readonly HashSet<string> Same = new HashSet<string>
        {
            // Wörter, die im Englischen gleich sind
            "Garage", "Smartwatch", "Watch", "LED", "Massage", "Toaster", "S", "M", "L", "Standard", "Premium", "Laptop",
            "Marketing", "Networking", "Content Creator", "Trendsetter", "Bronze", "Gold", "Hype!", "Normal", "Fitness",
            "Unboxing", "POV", "Review", "Video", "Cringe", "Gourmet", "Ball", "GPS", "Laser", "Diamond Hands",
            // Marken / Eigennamen
            "Amazonas", "Hype & Hoodie",
            // schon englisches Makler-Kauderwelsch
            "Short the dip!", "Diamond Hands!", "Buy the rumor!", "Sell the news!", "To the moon!", "Margin Call?!", "Blockchain!",
            // Slogan ist schon englisch
            "No pain, no {p}.",
            // Bewertungsnamen (Personennamen bleiben)
            "Sabine K.", "Dennis", "xX_Gamer_Xx", "Laura M.", "Tobi", "Ben", "Kevin H.", "Mareike", "Uschi (72)", "Mehmet Y.",
            "ShoppingQueen99", "Dr. Pfeiffer", "Rüdiger",
        };

        private List<string> _fails;

        [SetUp]
        public void Setup()
        {
            Lang.ResetForTests();
            Lang.Set("en");
            _fails = new List<string>();
        }

        [TearDown]
        public void Reset() => Lang.ResetForTests();

        /// <summary>Prüft, dass <paramref name="shown"/> übersetzt wird. <paramref name="raw"/> = Rohtext für die Ausnahmeliste.</summary>
        private void Check(string ctx, string shown, string raw = null)
        {
            if (string.IsNullOrEmpty(shown)) return;
            string en = Lang.T(shown);
            if (en == shown && !Same.Contains(raw ?? shown)) _fails.Add(ctx + ": " + shown);
        }

        private void AssertNoFails()
        {
            Assert.IsEmpty(_fails, "Nicht übersetzt (" + _fails.Count + "):\n" + string.Join("\n", _fails));
        }

        [Test]
        public void GameData_Catalog_Translated()
        {
            foreach (var p in GameData.Products)
            {
                Check("product", p.Name);
                Check("product.short", p.Short);
            }
            foreach (var b in GameData.BulkOptions) Check("bulk", b.Name);
            foreach (var s in GameData.Suppliers)
            {
                Check("supplier", s.Name);
                Check("supplier.desc", s.Desc);
            }
            foreach (var p in GameData.PackagingSizes) Check("packaging", p.Name);
            foreach (var a in GameData.AdTiers) Check("adtier", a.Name);
            foreach (var s in GameData.StageNames) Check("stage", s);
            foreach (var s in GameData.LogoNames) Check("logo", s);
            foreach (var kv in GameData.LevelUnlocks) Check("unlock " + kv.Key, kv.Value);
            foreach (var u in GameData.Upgrades)
            {
                Check("upgrade", u.Name);
                Check("upgrade.desc", u.Desc);
            }
            foreach (var d in GameData.Decor) Check("decor", d.Name);
            foreach (var l in GameData.Lifestyle)
            {
                Check("lifestyle", l.Name);
                Check("lifestyle.desc", l.Desc);
            }
            foreach (var g in GameData.Goals)
            {
                Check("goal", g.Title);
                Check("goal.desc", g.Desc);
            }
            foreach (var r in GameData.StaffRoles)
            {
                Check("staff", r.Name);
                Check("staff.desc", r.Desc);
            }
            foreach (float q in new[] { 0.5f, 1f, 1.5f }) Check("quality", GameData.QualityName(q));
            AssertNoFails();
        }

        [Test]
        public void GameData_TextsAndTutorial_Translated()
        {
            foreach (var kv in GameData.ReviewTexts)
                foreach (var t in kv.Value) Check("review " + kv.Key, t);
            foreach (var t in GameData.ReviewTextsExpressGood) Check("review.express", t);
            foreach (var t in GameData.ReviewTextsExpressLate) Check("review.express_late", t);
            foreach (var t in GameData.ReviewTextsLate) Check("review.late", t);
            foreach (var n in GameData.ReviewNames) Check("review.name", n);
            for (int i = 0; i < GameData.Tutorial.Length; i++)
            {
                Check("tutorial.title", GameData.Tutorial[i].Title);
                Check("tutorial.raw", GameData.Tutorial[i].Text);
                Check("tutorial", GameData.TutorialText(i));
            }
            foreach (var t in GameData.KalleIdle) Check("kalle", t);
            foreach (var t in GameData.StandShouts) Check("stand", t);
            foreach (var t in GameData.WeekdayNames) Check("weekday", t);
            foreach (var t in GameData.WeekdayShorts) Check("weekday.short", t);
            Check("key", GameData.Key("jump"));
            AssertNoFails();
        }

        [Test]
        public void GameData_Content_Translated()
        {
            foreach (var t in GameData.OrderNotes) Check("note", t);
            foreach (var t in GameData.ExpressNotes) Check("note.express", t);
            foreach (var kv in GameData.ProductNotes)
                foreach (var t in kv.Value) Check("note." + kv.Key, t);
            Check("note.quoted", "„" + GameData.OrderNotes[0] + "“");
            foreach (var t in GameData.ReturnReasons) Check("return", t);
            foreach (var t in GameData.ReturnReasonsQuality) Check("return.quality", t);
            Check("return.late", GameData.ReturnReasonLate);
            Check("return.oversize", GameData.ReturnReasonOversize);
            foreach (var t in GameData.TrendPhaseNames) Check("trend.phase", t);

            var pools = new List<string>();
            pools.AddRange(GameData.TrendReasonsRising);
            pools.AddRange(GameData.TrendReasonsPeak);
            pools.AddRange(GameData.TrendReasonsFalling);
            pools.AddRange(GameData.TrendReasonsDead);
            pools.Add(GameData.TrendReasonNormal);
            foreach (var p in GameData.Products)
                foreach (var t in pools) Check("trend." + p.Id, TrendSystem.FillReason(t, p.Id), t);

            foreach (var c in GameData.Companies) Check("company", c);
            foreach (var c in GameData.Wholesalers) Check("wholesaler", c);
            var reasons = new List<string>(GameData.ContractReasons);
            reasons.AddRange(GameData.WholesaleReasons);
            foreach (var r in reasons)
            {
                Check("contract.reason", r);
                foreach (var p in GameData.Products)
                {
                    var c = new Contract { Company = GameData.Companies[0], Reason = r, Product = p.Id, Quantity = 50 };
                    if (Lang.T(p.Name) != p.Name) Check("contract.title", c.Title); // "50× Smartwatch" bleibt gleich
                    Check("contract.desc", c.Description);
                }
            }
            foreach (float q in new[] { 0f, 0.8f, 1.3f }) Check("contract.quality", new Contract { MinQuality = q }.QualityText);

            foreach (var b in GameData.SkillBranches)
            {
                Check("branch", b.Name);
                Check("branch.desc", b.Desc);
            }
            foreach (var s in GameData.Skills)
            {
                Check("skill", s.Name);
                Check("skill.desc", s.Desc);
            }
            Check("coach", GameData.CoachName);
            foreach (var m in new[] { GameData.MailWeek, GameData.MailSkills, GameData.MailContracts, GameData.MailReturns, GameData.MailExpress, GameData.MailPerks, GameData.MailWholesale })
                foreach (var t in m) Check("mail", t);
            foreach (var tip in GameData.OnboardingTips)
            {
                Check("onboarding.title", tip.Title);
                Check("onboarding", GameData.FillKeys(tip.Text));
            }
            AssertNoFails();
        }

        [Test]
        public void GameData_Progress_Translated()
        {
            foreach (var p in GameData.Perks)
            {
                Check("perk", p.Name);
                Check("perk.desc", p.Desc);
                Check("perk.effect", p.Effect);
            }
            foreach (var s in GameData.HomeSlots)
            {
                Check("home.slot", s.Name);
                Check("home.base", s.BaseName);
                Check("home.baseeffect", s.BaseEffect);
            }
            foreach (var h in GameData.HomeItems)
            {
                Check("home", h.Name);
                Check("home.desc", h.Desc);
                Check("home.effect", h.Effect);
            }
            foreach (var e in GameData.Equipment)
            {
                Check("equipment", e.Name);
                Check("equipment.desc", e.Desc);
                Check("equipment.effect", e.Effect);
            }
            foreach (var t in GameData.PortoTiers) Check("porto", t.Name);
            foreach (var c in GameData.Carriers)
            {
                Check("carrier", c.Name);
                Check("carrier.desc", c.Desc);
            }
            foreach (var d in GameData.Disruptions)
            {
                Check("disruption", d.Name);
                Check("disruption.text", d.Text);
                Check("disruption.review", d.Review);
            }
            foreach (var t in GameData.DefectReviews) Check("defect", t);
            Check("defect.return", GameData.DefectReturnReason);
            foreach (var d in GameData.Difficulties)
            {
                Check("difficulty", d.Name);
                Check("difficulty.desc", d.Desc);
            }
            foreach (var v in VehicleRules.Defs) Check("vehicle", v.Name);
            var sim = TestUtil.Fresh();
            Assert.IsTrue(sim.SendMopedHint("E"));
            var mail = sim.Events.Mails[0];
            Check("moped.sender", mail.Sender);
            Check("moped.title", mail.Title);
            Check("moped.text", mail.Text);
            AssertNoFails();
        }

        [Test]
        public void ShopData_Translated()
        {
            Check("store", ShopData.ElectroName);
            Check("store", ShopData.ElectroSlogan);
            Check("store", ShopData.PetShopName);
            Check("store", ShopData.PetShopSlogan);
            Check("store", ShopData.ClothesName);
            foreach (var s in ShopData.Species)
            {
                Check("species", s.Name);
                Check("species.default", s.DefaultName);
                Check("species.bonus", s.Bonus);
            }
            foreach (var i in ShopData.Items)
            {
                Check("item", i.Name);
                Check("item.short", i.Short);
                Check("item.desc", i.Desc);
            }
            Check("shop.msg", "Bezahlt: 120 € bei MediaMarkd (gespart: 30 €). Ware liefert der Lieferservice an deinen Wareneingang.");
            Check("shop.msg", "Bello (Hund) zieht bei dir ein! Futter nicht vergessen.");
            Check("shop.msg", "Mieze hat Hunger! Futter gibt's bei Fressnix.");
            AssertNoFails();
        }

        [Test]
        public void Ads_And_SlopGen_Translated()
        {
            foreach (var t in GameData.AdTargets) Check("adtarget", t.Name);
            foreach (var r in GameData.GugelRivals) Check("rival", r);
            foreach (var n in GameData.FakeProfileNames) Check("fakeprofile", n);
            foreach (var p in GameData.Products)
            {
                foreach (var s in GameData.AdSlogans) Check("slogan." + p.Id, GameData.FillProduct(s.Text, p.Id), s.Text);
                foreach (var k in GameData.GugelKeywords) Check("keyword." + p.Id, GameData.FillProduct(k.Text, p.Id), k.Text);
            }

            // Alle Vorlagen der KI-Seiten (privat) – mit jedem Produkt, Standardmarke und Preis gefüllt
            string[] Arr(string name) => (string[])typeof(SlopGen).GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (var p in GameData.Products)
            {
                string slug = SlopGen.Slug(p.Name);
                foreach (var name in new[] { "Domains", "Titles", "Reasons", "Paragraphs", "Authors" })
                    foreach (var t in Arr(name))
                    {
                        string filled = t.Replace("{p}", p.Name).Replace("{b}", "MeinShop").Replace("{price}", "25 €").Replace("{s}", slug);
                        // Domains mit unverändertem Slug (z. B. "smartwatch") nur einmal mit der Handyhülle prüfen
                        if (name == "Domains" && p.Id != "huelle") continue;
                        Check("slop." + name + "." + p.Id, filled, t);
                    }
            }
            var site = new FakeSite { Product = "kopfhoerer", Seed = 7, Day = 1 };
            Check("slop.domain", SlopGen.Domain(site));
            Check("slop.title", SlopGen.Title(site));
            foreach (var r in SlopGen.Reasons10(site, "")) Check("slop.reason", r);
            foreach (var b in SlopGen.Body(site, "Mein Shop", 69)) Check("slop.body", b);
            AssertNoFails();
        }

        [Test]
        public void FinanceText_Translated()
        {
            var stock = (Dictionary<string, string[][]>)typeof(FinanceText).GetField("Stock", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (var kv in stock)
                foreach (var set in kv.Value)
                    foreach (var t in set) Check("stock." + kv.Key, t);
            foreach (var arr in new[] { FinanceText.MarketGood, FinanceText.MarketBad, FinanceText.CryptoGood, FinanceText.CryptoBad, FinanceText.BrokerBabble, FinanceText.BrokerPanic, FinanceText.BankerLines, FinanceText.AtmLines, FinanceText.SavingsTips })
                foreach (var t in arr) Check("finance", t);
            foreach (var t in FinanceText.CoinGood) Check("coin", string.Format(t, "HodlCoin"), t);
            foreach (var t in FinanceText.CoinBad) Check("coin", string.Format(t, "HodlCoin"), t);
            Check("broker", string.Format(FinanceText.BrokerUp, "KART"));
            Check("broker", string.Format(FinanceText.BrokerDown, "KART"));
            AssertNoFails();
        }

        [Test]
        public void Achievements_Translated()
        {
            foreach (var a in AchievementData.All)
            {
                Check("achievement", a.Name);
                Check("achievement.desc", a.Desc);
            }
            AssertNoFails();
        }

        [Test]
        public void TikTok_Translated()
        {
            var tipFor = typeof(TikTokScoring).GetMethod("TipFor", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var f in TikTokFormats.All)
            {
                Check("format", TikTokFormats.Name(f));
                Check("format.tip", TikTokFormats.Tip(f));
                foreach (var p in GameData.Products)
                {
                    Check("format.title", TikTokFormats.Title(f, p.Name, 0.95f));
                    if (Lang.T(p.Name) != p.Name || f != TikTokFormats.Unboxing) Check("format.title", TikTokFormats.Title(f, p.Name, 0.3f)); // "Unboxing: Smartwatch"
                }
                Check("format.title", TikTokFormats.Title(f, "", 0.3f));
            }
            foreach (var id in TikTokScoring.CriterionIds)
            {
                Check("criterion", TikTokScoring.Label(id));
                Check("criterion.tip", (string)tipFor.Invoke(null, new object[] { id }));
            }
            foreach (float s in new[] { 0.9f, 0.7f, 0.5f, 0.3f, 0.1f }) Check("grade", TikTokScoring.Grade(s));
            foreach (var h in TikTokScoring.FrameHints(false, 0f, 9f, 999f, 0f, 0f, false)) Check("hint", h);
            foreach (var h in TikTokScoring.FrameHints(true, 0f, 9f, 999f, 0f, 0f, false)) Check("hint", h);
            foreach (var h in TikTokScoring.FrameHints(true, 1f, 0.01f, 0f, 1f, 999f, true)) Check("hint", h);
            AssertNoFails();
        }

        [Test]
        public void WeeklyChallenges_Translated()
        {
            string[] samples =
            {
                "Paketlawine", "Verschicke 35 Pakete.", "Umsatzmaschine", "Mach 1.250 € Umsatz (Shop, Stand und Großaufträge).",
                "Sternenhimmel", "Bring deine Bewertung auf mindestens 4,5 Sterne.", "Fünf-Sterne-Regen", "Sammle 6 Fünf-Sterne-Bewertungen.",
                "Blitzversand", "Verschicke 4 Express-Bestellungen pünktlich.", "Großkunden-Liebling", "Erfülle 2 Großaufträge.", "Erfülle einen Großauftrag.",
                "Marktschreier", "Verkaufe 20 Artikel am Verkaufsstand.", "Content-Maschine", "Poste 3 TikToks.", "Zweite Chance",
                "Bereite 3 Retouren als B-Ware auf.", "Fehlerfreier Tag", "Verschicke an einem Tag 12 Pakete, ohne eine Bestellung zu verlieren.",
            };
            foreach (var s in samples) Check("challenge", s);
            foreach (var p in GameData.Products)
            {
                Check("challenge.product", p.Short + "-Spezialist");
                Check("challenge.product", "Verkaufe 25× " + p.Name + ".");
            }
            AssertNoFails();
        }

        [Test]
        public void Samples_ReadAsExpected()
        {
            Assert.AreEqual("Phone case", Lang.T("Handyhülle"));
            Assert.AreEqual("A celebrity was spotted with Smart watering can with app at the bakery.",
                Lang.T(TrendSystem.FillReason("Ein Promi wurde mit {p} beim Bäcker gesehen.", "giesskanne")));
            Assert.AreEqual("#CaseChallenge is going viral on TikTok.", Lang.T(TrendSystem.FillReason("#{tag} geht auf TikTok viral.", "huelle")));
            var c = new Contract { Company = "Bäckerei Krümel", Reason = "für die Tombola", Product = "giesskanne", Quantity = 40 };
            Assert.AreEqual("Crumbs Bakery needs 40× Smart watering can with app for the raffle.", Lang.T(c.Description));
            Assert.AreEqual("buy Bluetooth headphones", Lang.T(GameData.FillProduct("{p} kaufen", "kopfhoerer")));
            Assert.AreEqual("best-phone-case-test.info", Lang.T("beste-handyhuelle-test.info"));
            Assert.AreEqual("Press Tab to open your phone without stopping work: order tickets, messages, one-tap reordering and your balance. You need the laptop for purchasing, the shop and the company.",
                Lang.T(GameData.FillKeys(GameData.OnboardingTips[1].Text)));
            Assert.AreEqual("Hold Space", Lang.T("Hold Space")); // Unbekanntes bleibt
        }
    }
}
