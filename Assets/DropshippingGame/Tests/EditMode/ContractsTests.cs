using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>F4 Großaufträge (B2B): Angebote, Annahme, Palettenplatz, Erfüllung, Vertragsstrafe.</summary>
    public class ContractsTests
    {
        [Test]
        public void Offers_StartAtLevel3()
        {
            var gm = TestUtil.Fresh();
            gm.AddXp(GameData.LevelThreshold(2));
            Assert.IsFalse(gm.ContractsUnlocked, "Level 2: noch keine Großaufträge");
            Assert.AreEqual(0, gm.MaxActiveContracts());
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.AreEqual(0, gm.ContractOfferTimes.Count, "Keine Angebote eingeplant");
            var offered = new List<Contract>();
            gm.ContractOffered += c => offered.Add(c);
            gm.AddXp(GameData.LevelThreshold(3) - gm.Xp);
            Assert.AreEqual(3, gm.Level);
            Assert.AreEqual(1, offered.Count, "Beim Freischalten kommt sofort ein erstes Angebot");
            Assert.IsTrue(gm.Events.Mails.Exists(m => m.Title == GameData.MailContracts[0]), "Erklärung im Postfach");
            var o = offered[0];
            Assert.AreEqual(ContractState.Offered, o.State);
            Assert.IsTrue(o.Quantity >= 10 && o.Quantity % 5 == 0, "Menge in 5er-Schritten (" + o.Quantity + ")");
            Assert.IsTrue(o.Payment > 0 && o.Penalty > 0 && o.Penalty < o.Payment && o.Xp > 0, "Vergütung, Strafe, XP");
            Assert.IsTrue(o.Days >= 2 && o.Days <= 4, "Frist 2-4 Tage");
            Assert.IsTrue(gm.ProductAvailable(o.Product), "Nur verfügbare Produkte");
            Assert.IsFalse(string.IsNullOrEmpty(o.Company) || string.IsNullOrEmpty(o.Reason), "Firma und Anlass");
            Assert.IsTrue(o.Description.Contains(o.Company), "Beschreibung");
            Assert.AreEqual(1, gm.MaxActiveContracts(), "Ein Auftrag gleichzeitig auf Level 3");
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.AreEqual(1, gm.ContractOfferTimes.Count, "Werktags ein Angebot (Level 3)");
            for (int i = 0; i < 450 && gm.ContractOfferTimes.Count > 0; i++) gm.AdvanceMinutes(1f);
            Assert.AreEqual(2, offered.Count, "Angebot im Laufe des Tages eingetroffen");
        }

        [Test]
        public void Contract_AcceptDeliverCratesAndComplete()
        {
            var gm = TestUtil.Level3();
            var c = gm.ContractOffers()[0];
            int money0 = gm.Money, earned0 = gm.TotalEarned, xp0 = gm.Xp;
            float rep0 = gm.Reputation;
            var completed = new List<Contract>();
            var delivered = new List<int>();
            gm.ContractCompleted += x => completed.Add(x);
            gm.ContractDelivered += (x, n) => delivered.Add(n);
            Assert.IsTrue(gm.AcceptContract(c.Id), "Angenommen");
            Assert.AreEqual(ContractState.Active, c.State);
            Assert.AreEqual(gm.Day + c.Days - 1, c.DeadlineDay, "Frist");
            Assert.AreEqual(c.Quantity, gm.ContractUnitsNeeded(c.Product), "Offene Stückzahl");
            var crate = ItemData.Crate(c.Product, c.Quantity + 7, 1f);
            Assert.IsTrue(gm.ContractAccepts(crate), "Palettenplatz nimmt die Kiste");
            int units = gm.ContractDeliver(crate);
            Assert.AreEqual(c.Quantity, units, "Nur so viel wie bestellt");
            Assert.AreEqual(7, crate.Quantity, "Rest bleibt in der Kiste");
            Assert.AreEqual(ContractState.Completed, c.State, "Erfüllt");
            Assert.IsTrue(completed.Count == 1 && delivered.Count == 1, "Ereignisse gemeldet");
            int goalReward = System.Array.Find(GameData.Goals, g => g.Id == "first_contract").Reward;
            Assert.AreEqual(money0 + c.Payment + goalReward, gm.Money, "Vergütung + Ziel 'Business to Business'");
            Assert.AreEqual(earned0 + c.Payment, gm.TotalEarned, "Zählt zum Umsatz");
            Assert.IsTrue(gm.Daily.ContractIncome == c.Payment && gm.Daily.Revenue == c.Payment && gm.Daily.ContractsDone == 1, "Tagesstatistik");
            Assert.IsTrue(gm.Reputation > rep0, "Ruf steigt");
            Assert.IsTrue(gm.Xp >= xp0 + c.Xp, "Erfahrung");
            Assert.AreEqual(1, gm.TotalContractsDone);
            Assert.AreEqual(0, gm.ContractDeliver(ItemData.Crate(c.Product, 5, 1f)), "Kein laufender Auftrag mehr");
            Assert.AreEqual(c.Payment, c.PaidOut, "Ausgezahlt vermerkt");
            Assert.IsTrue(gm.ContractHistory().Contains(c), "Im Verlauf");
        }

        [Test]
        public void Contract_SingleItemsFromShelf()
        {
            var gm = TestUtil.Level3();
            var c = gm.ContractOffers()[0];
            gm.AcceptContract(c.Id);
            gm.Stock[c.Product].Qty = 30;
            var item = gm.PickItem(c.Product);
            Assert.IsNotNull(item, "Ohne Bestellung gibt das Regal einen Artikel für den Großauftrag");
            Assert.IsTrue(item.ContractId == c.Id && item.OrderId == 0, "Artikel gehört zum Auftrag");
            Assert.AreEqual(29, gm.StockQty(c.Product));
            Assert.IsFalse(gm.WrapItem(item), "Großauftrags-Artikel werden nicht verpackt");
            gm.ReturnItem(item);
            Assert.AreEqual(30, gm.StockQty(c.Product), "Zurück ins Regal");
            Assert.AreEqual(0, gm.PendingCount(), "Zurücklegen erzeugt keine Bestellung");
            item = gm.PickForContract(c.Product);
            Assert.AreEqual(1, gm.ContractDeliver(item), "Einzelartikel geliefert");
            Assert.AreEqual(1, c.Delivered);
            gm.SpawnOrder(c.Product, false);
            var orderItem = gm.PickItem(c.Product);
            Assert.IsTrue(orderItem.OrderId > 0, "Kundenbestellungen haben am Regal Vorrang");
            Assert.AreEqual(0, gm.ContractDeliver(orderItem), "Bestellartikel gehören nicht auf die Palette");
        }

        [Test]
        public void Contract_LimitAndQuality()
        {
            var gm = TestUtil.Level3();
            gm.GenerateContractOffer();
            var offers = gm.ContractOffers();
            Assert.AreEqual(2, offers.Count);
            Assert.IsTrue(gm.AcceptContract(offers[0].Id), "Erster Auftrag");
            Assert.IsFalse(gm.CanAcceptContract(offers[1], out string reason), "Limit erreicht");
            Assert.IsTrue(reason.Length > 0, "Mit Begründung");
            Assert.IsFalse(gm.AcceptContract(offers[1].Id), "Zweiter geht auf Level 3 nicht");
            var c = gm.ActiveContracts()[0];
            c.MinQuality = GameData.QualityStandard;
            var cheap = ItemData.Crate(c.Product, 10, 0.6f);
            Assert.IsFalse(gm.ContractAccepts(cheap), "Billig-Ware wird abgelehnt");
            Assert.AreEqual(0, gm.ContractDeliver(cheap));
            Assert.AreEqual(10, cheap.Quantity, "Kiste unverändert");
            var bstock = ItemData.Crate(c.Product, 10, 0.95f);
            Assert.AreEqual(System.Math.Min(10, c.Quantity), gm.ContractDeliver(bstock), "Leicht gemischte Standard-Ware zählt als Standard");
            string other = c.Product == "huelle" ? "led" : "huelle";
            Assert.AreEqual(0, gm.ContractDeliver(ItemData.Crate(other, 10, 1f)), "Falsches Produkt");
            Assert.IsTrue(gm.DeclineContract(offers[1].Id), "Ablehnen");
            Assert.AreEqual(ContractState.Declined, offers[1].State);
        }

        [Test]
        public void Contract_MissedDeadlineCostsPenaltyAndReputation()
        {
            var gm = TestUtil.Level3(5);
            var c = gm.ContractOffers()[0];
            gm.AcceptContract(c.Id);
            int half = c.Quantity / 2;
            gm.ContractDeliver(ItemData.Crate(c.Product, half, 1f));
            var failed = new List<Contract>();
            gm.ContractFailed += x => failed.Add(x);
            DaySummary last = null;
            gm.DayEnded += s => last = s;
            float rep0 = gm.Reputation;
            while (gm.Day < c.DeadlineDay)
            {
                gm.EndDayNow();
                Assert.AreEqual(ContractState.Active, c.State, "Läuft bis zur Frist");
                gm.StartNextDay();
            }
            int money0 = gm.Money;
            gm.EndDayNow();
            Assert.AreEqual(ContractState.Failed, c.State, "Frist verpasst");
            int partial = Mathx.RoundToInt(c.Payment * ((float)half / c.Quantity) * GameData.ContractPartialPay);
            Assert.AreEqual(money0 + partial - c.Penalty, gm.Money, "Teilzahlung minus Vertragsstrafe");
            Assert.IsTrue(last.Penalties == c.Penalty && last.ContractsFailed == 1, "Tagesabrechnung zeigt die Strafe");
            Assert.IsTrue(last.Notes.Exists(n => n.Contains(c.Company)), "Meldung auf dem Kassenbon");
            Assert.IsTrue(gm.Reputation < rep0, "Rufverlust");
            Assert.IsTrue(failed.Count == 1 && gm.TotalContractsFailed == 1, "ContractFailed + Statistik");
        }

        [Test]
        public void Offer_ExpiresAtEndOfNextDay()
        {
            var gm = TestUtil.Level3();
            var c = gm.ContractOffers()[0];
            Assert.AreEqual(gm.Day + 1, c.OfferExpiresDay);
            Assert.AreEqual("morgen bis 20:00", gm.ContractDeadlineText(c), "Angebot gilt bis morgen");
            gm.EndDayNow();
            Assert.AreEqual(ContractState.Offered, c.State, "Heute noch gültig");
            gm.StartNextDay();
            gm.EndDayNow();
            Assert.AreEqual(ContractState.Expired, c.State, "Verfallen");
        }

        [Test]
        public void RushEvent_ShortensDeadlineForMoreMoney()
        {
            var gm = TestUtil.Level3();
            var c = gm.ContractOffers()[0];
            c.Days = 4;
            gm.AcceptContract(c.Id);
            int pay0 = c.Payment, deadline0 = c.DeadlineDay;
            gm.Day = 2;
            Assert.IsTrue(gm.Events.Eligible(EventData.Find("eilauftrag")), "Eilauftrag möglich");
            var mail = gm.Events.Trigger("eilauftrag");
            Assert.AreEqual(c.Id, mail.CtxContract, "Betrifft den laufenden Auftrag");
            Assert.AreEqual(c.Company, mail.Sender, "Absender ist die Firma");
            gm.Events.Choose(mail.Id, 0);
            Assert.AreEqual(deadline0 - 1, c.DeadlineDay, "Einen Tag früher");
            Assert.IsTrue(c.Payment >= pay0 * 1.25f, "30 % mehr Geld");
        }

        [Test]
        public void GrosskundeEvent_CanAcceptRightAway()
        {
            var gm = TestUtil.Level3(7);
            var mail = gm.Events.Trigger("grosskunde");
            Assert.IsFalse(mail.Sender.Contains("{"), "Firmenname eingesetzt");
            gm.Events.Choose(mail.Id, 0);
            Assert.AreEqual(1, gm.ActiveContractCount(), "Sofort angenommen");
            var c = gm.ActiveContracts()[0];
            Assert.IsTrue(c.Special, "Sonderauftrag");
            Assert.AreEqual(mail.CtxCompany, c.Company, "Gleiche Firma wie in der Nachricht");
        }

        [Test]
        public void MesseEvent_BringsTwoOffers()
        {
            var gm = TestUtil.Level3(8);
            gm.Level = 4;
            gm.Money = 1000;
            int before = gm.ContractOffers().Count;
            var mail = gm.Events.Trigger("messe");
            gm.Events.Choose(mail.Id, 0);
            Assert.AreEqual(before + 2, gm.ContractOffers().Count, "Zwei neue Anfragen");
            Assert.AreEqual(750, gm.Money, "Standgebühr bezahlt");
        }

        [Test]
        public void Staff_StocksContractPalletsFromInventory()
        {
            var gm = TestUtil.Level3();
            gm.LocationStage = 1;
            gm.Upgrades.Add("warehouse");
            gm.Level = 5;
            var c = gm.ContractOffers()[0];
            gm.AcceptContract(c.Id);
            gm.Stock[c.Product].Qty = c.Quantity + GameData.ContractStockReserve + 5;
            Assert.IsTrue(gm.Hire("lager"));
            for (int i = 0; i < 40 && c.State == ContractState.Active; i++) gm.UpdateStaff(17f);
            Assert.AreEqual(ContractState.Completed, c.State, "Lagerist:in bestückt die Palette");
            Assert.AreEqual(GameData.ContractStockReserve + 5, gm.StockQty(c.Product), "Reserve bleibt im Regal");
        }
    }
}
