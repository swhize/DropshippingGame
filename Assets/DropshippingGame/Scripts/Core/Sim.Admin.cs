using System;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Admin-/Testfunktionen (Admin-Panel, Taste F10). Alles hier umgeht bewusst die normalen Regeln
    /// (Geld, Level, Voraussetzungen). Die Schalter werden nicht gespeichert.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Zeitfaktor für den Spieltag (0 = Zeit angehalten, 1 = normal, bis 10).</summary>
        public float AdminTimeScale = 1f;
        /// <summary>Geld fällt nie unter 1 Mio.</summary>
        public bool AdminInfiniteMoney;
        /// <summary>Keine Insolvenz am Tagesende.</summary>
        public bool AdminNoBankrupt;

        public const int AdminMoneyFloor = 1000000;

        /// <summary>Einmal pro Frame aus dem Admin-Panel aufrufen.</summary>
        public void AdminTick()
        {
            if (AdminInfiniteMoney && Money < AdminMoneyFloor)
            {
                Money = AdminMoneyFloor;
                RaiseEconomyChanged();
            }
        }

        public void AdminAddMoney(int amount)
        {
            Money += amount;
            RaiseEconomyChanged();
        }

        public void AdminSetMoney(int amount)
        {
            Money = amount;
            RaiseEconomyChanged();
        }

        /// <summary>Setzt das Firmenlevel (1–Max). Aufstiege lösen die normalen Freischaltungen aus.</summary>
        public void AdminSetLevel(int level)
        {
            level = Mathx.Clamp(level, 1, GameData.MaxLevel);
            if (level > Level)
            {
                AddXp(Math.Max(0, GameData.LevelThreshold(level) - Xp));
            }
            else if (level < Level)
            {
                Level = level;
                Xp = GameData.LevelThreshold(level);
                SkillsChanged?.Invoke();
            }
            RaiseEconomyChanged();
        }

        public void AdminAddXp(int n) => AddXp(Math.Max(0, n));

        /// <summary>Überspringt Kalles Imbiss und startet direkt im Business.</summary>
        public void AdminSkipIntro()
        {
            if (StoryStage == "diner") FinishIntro();
            TutorialStep = -1;
            BrandNamed = true;
            RaiseEconomyChanged();
        }

        /// <summary>Kauft alle Upgrades (inkl. Lagerhalle), Deko und Lifestyle gratis.</summary>
        public void AdminUnlockUpgrades()
        {
            foreach (var u in GameData.Upgrades)
            {
                if (u == null || Upgrades.Contains(u.Id)) continue;
                Upgrades.Add(u.Id);
                if (u.Id == "warehouse" && LocationStage < 1)
                {
                    LocationStage = 1;
                    LocationChanged?.Invoke(1);
                }
            }
            foreach (var d in GameData.Decor) if (d != null) DecorOwned.Add(d.Id);
            foreach (var l in GameData.Lifestyle) if (l != null) LifestyleOwned.Add(l.Id);
            WorldChanged?.Invoke();
            StaffChanged?.Invoke();
            ConveyorChanged?.Invoke();
            RaiseEconomyChanged();
        }

        /// <summary>Lernt alle Skills (gibt dafür genug Bonus-Punkte).</summary>
        public void AdminLearnAllSkills()
        {
            foreach (var s in GameData.Skills)
            {
                if (s == null || Skills.Contains(s.Id)) continue;
                Skills.Add(s.Id);
            }
            int missing = Skills.Count - SkillPointsTotal();
            if (missing > 0) BonusSkillPoints += missing;
            SkillsChanged?.Invoke();
            RaiseEconomyChanged();
        }

        /// <summary>Alles freischalten: Intro weg, Max-Level, alle Upgrades, Skills, Geld, keine Schulden.</summary>
        public void AdminUnlockEverything()
        {
            AdminSkipIntro();
            AdminSetLevel(GameData.MaxLevel);
            AdminUnlockUpgrades();
            AdminLearnAllSkills();
            Debt = 0;
            if (Money < AdminMoneyFloor) Money = AdminMoneyFloor;
            Reputation = 5f;
            for (int i = 0; i < Packaging.Length; i++) Packaging[i] = Math.Max(Packaging[i], 200);
            Notify("ADMIN: Alles freigeschaltet.", "good");
            RaiseEconomyChanged();
        }

        /// <summary>Legt sofort eine Kiste mit Ware an den Wareneingang (gratis, ohne Lieferzeit).</summary>
        public void AdminDeliver(string productId, int quantity, float quality = 1f)
        {
            if (quantity <= 0 || !IsProductId(productId)) return;
            DockCrates.Add(ItemData.Crate(productId, quantity, quality));
            DeliveryArrived?.Invoke(productId);
            RaiseEconomyChanged();
        }

        /// <summary>Lässt alle unterwegs befindlichen Lieferungen sofort ankommen.</summary>
        private static bool IsProductId(string id)
        {
            foreach (var p in GameData.Products) if (p.Id == id) return true;
            return false;
        }

        public void AdminArriveDeliveries()
        {
            float now = BClock();
            foreach (var d in TravelingDeliveries) d.ArriveAt = now;
            UpdateDeliveries();
        }

        /// <summary>Erzeugt n Bestellungen für gelistete (sonst beliebige) Produkte.</summary>
        public int AdminSpawnOrders(int n, bool express)
        {
            int made = 0;
            for (int i = 0; i < n; i++)
            {
                string pid = null;
                var listed = new System.Collections.Generic.List<string>();
                foreach (var p in GameData.Products) if (IsListed(p.Id)) listed.Add(p.Id);
                pid = listed.Count > 0 ? Rng.Pick(listed) : Rng.Pick(GameData.Products).Id;
                if (SpawnOrder(pid, express) != null) made++;
            }
            RaiseEconomyChanged();
            return made;
        }

        /// <summary>Listet alle Produkte im Webshop.</summary>
        public void AdminListAll()
        {
            foreach (var p in GameData.Products) Listed[p.Id] = true;
            RaiseEconomyChanged();
        }

        public void AdminClearDebt()
        {
            Debt = 0;
            RaiseEconomyChanged();
        }

        public void AdminSetReputation(float r)
        {
            Reputation = Mathx.Clamp(r, 0.5f, 5f);
            RaiseEconomyChanged();
        }

        public void AdminMaxAwareness()
        {
            Awareness = 1.5f;
            RaiseEconomyChanged();
        }

        public void AdminResetTikTok()
        {
            Daily.TikToks = 0;
            TikTokReadyAt = 0f;
            RaiseEconomyChanged();
        }

        public void AdminAddPackaging(int n)
        {
            for (int i = 0; i < Packaging.Length; i++) Packaging[i] += n;
            RaiseEconomyChanged();
        }

        /// <summary>Spult die Uhr vor (nur im laufenden Geschäftstag).</summary>
        public void AdminSkipMinutes(float minutes)
        {
            if (!IsOpen || minutes <= 0f) return;
            float left = GameData.DayEnd - TimeMinutes;
            AdvanceMinutes(Math.Min(minutes, Math.Max(0f, left - 0.01f)));
            RaiseEconomyChanged();
        }

        public Mail AdminTriggerEvent(string id) => Events.Trigger(id);

        public void AdminTriggerTrend(string productId, TrendPhase phase) => Trends.Trigger(productId, phase, 0f, "Admin");

        public void AdminContractOffer()
        {
            GenerateContractOffer();
            RaiseEconomyChanged();
        }

        public int AdminReturnWave(int n) => TriggerReturnWave(n);
    }
}
