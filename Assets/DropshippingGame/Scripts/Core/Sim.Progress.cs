using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Fortschritt &amp; Wirtschaftstiefe: Hustle-Perks (Erfahrung ausgeben), Einrichtung &amp; Energie,
    /// Ausrüstung, Mengenrabatt, Porto &amp; Paketdienste, Lieferstörungen, fehlerhafte Chargen,
    /// Verpackungsmüll, Großhändler und Markenstärke. Daten in <c>GameData.Progress.cs</c>.
    /// Wird über wenige Haken aus <c>Sim.cs</c> aufgerufen (ProgressReset/Update/NewDay/EndDay/Ship).
    /// </summary>
    public sealed partial class Sim
    {
        // ---- Ereignisse ------------------------------------------------------------------------
        /// <summary>Perks, Einrichtung oder Ausrüstung haben sich geändert.</summary>
        public event Action ProgressChanged;
        /// <summary>Ausrüstung gekauft (Welt baut Stationen neu).</summary>
        public event Action EquipmentChanged;
        /// <summary>
        /// Müllabfuhr fällig (true = bezahlte Sonderabholung). Der Müllwagen der Welt fährt hin und ruft
        /// <see cref="CollectGarbage"/>. Hängt niemand dran, leert der Kern sofort; sonst spätestens nach
        /// <see cref="GameData.GarbageFallbackMinutes"/>.
        /// </summary>
        public event Action<bool> GarbagePickupDue;
        /// <summary>Tonnen geleert: entsorgte Menge.</summary>
        public event Action<int> GarbageCollected;
        /// <summary>Füllstand der Tonnen hat sich geändert.</summary>
        public event Action WasteChanged;
        /// <summary>Lieferstörung beginnt (Def) bzw. endet (null).</summary>
        public event Action<DisruptionDef> DisruptionChanged;
        /// <summary>Fehlerhafte Charge eingetroffen: (Produkt, defekte Stück).</summary>
        public event Action<string, int> DefectiveBatch;

        // ---- Zustand ---------------------------------------------------------------------------
        public Dictionary<string, int> PerkRanks = new Dictionary<string, int>();
        /// <summary>Gekaufte Einrichtungs-Stufe je Platz ("laptop", "bed" ...).</summary>
        public Dictionary<string, int> HomeTiers = new Dictionary<string, int>();
        public float Energy = GameData.EnergyMax;
        public Dictionary<string, int> EquipmentOwned = new Dictionary<string, int>();
        /// <summary>Insgesamt eingekaufte Stück je Produkt (Mengenrabatt).</summary>
        public Dictionary<string, int> PurchasedUnits = new Dictionary<string, int>();
        /// <summary>Gewählter Paketdienst (Index in <see cref="GameData.Carriers"/>).</summary>
        public int Carrier;
        /// <summary>Noch nicht abgebuchte Porto-Cent-Bruchteile (€).</summary>
        public float PortoCarry;
        public int TotalPorto;
        public string DisruptionId = "";
        /// <summary>Störung gilt bis einschließlich dieses Tages.</summary>
        public int DisruptionUntilDay;
        public int TotalDelayed;
        /// <summary>Defekte Stück je Produkt (im Lager oder in Kisten am Wareneingang).</summary>
        public Dictionary<string, int> DefectUnits = new Dictionary<string, int>();
        public int TotalDefectsShipped;
        public int Waste;
        public bool GarbagePending;
        public float GarbagePendingSince;
        public int TotalWasteCollected;
        public float WholesaleOfferAt = -1f;
        public string ReorderCompany = "", ReorderProduct = "";
        public int ReorderDay;

        private float _foldProgress;
        private float _reviewStarBonus;
        private bool _tiredWarned, _wasteWarned, _delayToastDay;
        private int _lastGarbageDay;

        private void ProgressReset()
        {
            PerkRanks.Clear();
            HomeTiers.Clear();
            Energy = GameData.EnergyMax;
            EquipmentOwned.Clear();
            PurchasedUnits.Clear();
            Carrier = 0;
            PortoCarry = 0f;
            TotalPorto = 0;
            DisruptionId = "";
            DisruptionUntilDay = 0;
            TotalDelayed = 0;
            DefectUnits.Clear();
            TotalDefectsShipped = 0;
            Waste = 0;
            GarbagePending = false;
            GarbagePendingSince = 0f;
            TotalWasteCollected = 0;
            WholesaleOfferAt = -1f;
            ReorderCompany = ReorderProduct = "";
            ReorderDay = 0;
            _foldProgress = 0f;
            _reviewStarBonus = 0f;
            _tiredWarned = _wasteWarned = _delayToastDay = false;
            _lastGarbageDay = 0;
            _perkPointsSeen = -1;
        }

        private void RaiseProgress()
        {
            ProgressChanged?.Invoke();
            RaiseEconomyChanged();
        }

        // =====================================================================================
        // 1. Hustle-Perks
        // =====================================================================================
        public int PerkRank(string id) => PerkRanks.TryGetValue(id ?? "", out int r) ? r : 0;
        public int PerkPointsTotal() => GameData.PerkPointsForXp(Xp);

        public int PerkPointsSpent()
        {
            int n = 0;
            foreach (var kv in PerkRanks) n += kv.Value;
            return n;
        }

        public int PerkPointsAvailable() => Math.Max(0, PerkPointsTotal() - PerkPointsSpent());

        /// <summary>XP bis zum nächsten Hustle-Punkt.</summary>
        public int XpToNextPerkPoint() => Math.Max(0, GameData.PerkXpThreshold(PerkPointsTotal() + 1) - Xp);

        /// <summary>"max", "level", "points", "available" oder "unknown".</summary>
        public string PerkState(string id)
        {
            var p = GameData.Perk(id);
            if (p == null) return "unknown";
            if (PerkRank(id) >= p.MaxRank) return "max";
            if (Level < p.Level) return "level";
            if (PerkPointsAvailable() <= 0) return "points";
            return "available";
        }

        public bool BuyPerk(string id)
        {
            var p = GameData.Perk(id);
            switch (PerkState(id))
            {
                case "available": break;
                case "level": Notify("Dafür brauchst du Firmenlevel " + p.Level + ".", "bad"); return false;
                case "points": Notify("Kein Hustle-Punkt frei. Nächster in " + XpToNextPerkPoint() + " XP.", "bad"); return false;
                default: return false;
            }
            PerkRanks[id] = PerkRank(id) + 1;
            Notify("Perk: " + p.Name + " Stufe " + PerkRank(id) + " (" + p.Effect + ")", "good");
            Sound("levelup", 0.02f, -6f);
            RaiseProgress();
            return true;
        }

        private int _perkPointsSeen = -1;

        private void CheckPerkPoints()
        {
            int total = PerkPointsTotal();
            if (_perkPointsSeen < 0) _perkPointsSeen = total;
            if (total <= _perkPointsSeen) return;
            _perkPointsSeen = total;
            if (StoryStage != "business") return;
            Notify("+1 Hustle-Punkt! Ausgeben: Laptop › Firma › Perks", "good");
            Explain("perks", GameData.MailPerks, "sparkle");
            ProgressChanged?.Invoke();
        }

        /// <summary>Nachfrage-Faktor aus Perks.</summary>
        public float DemandPerkMult() => 1f + 0.05f * PerkRank("p_kunden");

        // =====================================================================================
        // 2. Einrichtung & Energie
        // =====================================================================================
        public int HomeTier(string slot)
        {
            int t = HomeTiers.TryGetValue(slot ?? "", out int v) ? v : 0;
            // Bereits über Deko/Lifestyle gekaufte Gegenstände zählen als Stufe 2.
            if (slot == "chair" && LifestyleOwned.Contains("gamingstuhl")) t = Math.Max(t, 2);
            if (slot == "coffee" && DecorOwned.Contains("kaffee")) t = Math.Max(t, 2);
            return t;
        }

        /// <summary>Nächste kaufbare Stufe eines Platzes oder null (alles gekauft).</summary>
        public HomeItemDef NextHomeItem(string slot) => GameData.HomeItem(slot, HomeTier(slot) + 1);

        public bool BuyHomeItem(string slot)
        {
            var h = NextHomeItem(slot);
            if (h == null) return false;
            if (Level < h.Level)
            {
                Notify("Ab Firmenlevel " + h.Level + ".", "bad");
                return false;
            }
            if (Money < h.Cost)
            {
                Notify("Nicht genug Geld!", "bad");
                Sound("error");
                return false;
            }
            Spend(h.Cost, "other");
            HomeTiers[slot] = h.Tier;
            if (h.Grants == "gamingstuhl") LifestyleOwned.Add("gamingstuhl");
            else if (h.Grants == "kaffee") DecorOwned.Add("kaffee");
            Notify(h.Name + " gekauft: " + h.Effect, "good");
            Sound("levelup", 0.02f, -6f);
            WorldChanged?.Invoke();
            RaiseProgress();
            return true;
        }

        /// <summary>Faktor auf den Energieverbrauch (Stuhl, Schreibtisch, Kaffee, Perk "Ausdauer").</summary>
        public float EnergyDrainMult()
        {
            float m = 1f;
            int chair = HomeTier("chair");
            if (chair == 1) m *= 0.85f;
            else if (chair >= 2) m *= 0.7f;
            if (HomeTier("desk") >= 2) m *= 0.9f;
            if (HomeTier("coffee") >= 2) m *= 0.9f;
            m *= 1f - 0.1f * PerkRank("p_ausdauer");
            return Math.Max(0.2f, m);
        }

        /// <summary>Energie am Morgen (Bett + Kaffee).</summary>
        public float MorningEnergy() =>
            Math.Min(GameData.EnergyMax, GameData.BedRest[Mathx.Clamp(HomeTier("bed"), 0, 2)] + GameData.CoffeeMorning[Mathx.Clamp(HomeTier("coffee"), 0, 2)]);

        public bool Tired => Energy < GameData.EnergyTiredBelow;

        /// <summary>
        /// Welt-Flag: Lauftempo-Faktor des Spielers. Müdigkeit bremst bis −20 %, das Boxspringbett gibt +5 %,
        /// volle Mülltonnen −8 % (Säcke im Weg).
        /// </summary>
        public float WalkSpeedMult()
        {
            float m = 1f;
            if (Energy < GameData.EnergyTiredBelow)
                m *= Mathx.Lerp(GameData.EnergyMinSpeed, 1f, Mathx.Clamp01(Energy / GameData.EnergyTiredBelow));
            if (HomeTier("bed") >= 2) m *= 1.05f;
            if (WasteFull) m *= GameData.WasteFullWalkMult;
            return m;
        }

        private void UpdateEnergy(float m)
        {
            Energy = Math.Max(0f, Energy - m * GameData.EnergyDrainPerMinute * EnergyDrainMult());
            if (!_tiredWarned && Energy < 25f && StoryStage == "business")
            {
                _tiredWarned = true;
                Notify("Du wirst müde und langsamer. Besseres Bett, Kaffee oder Stuhl helfen (Firma › Einrichtung).", "info");
            }
        }

        /// <summary>Kaufpreis-Faktor aus der Einrichtung (Laptop).</summary>
        private float HomePurchaseMult() => HomeTier("laptop") >= 2 ? 0.96f : (HomeTier("laptop") == 1 ? 0.98f : 1f);
        private float HomeLeadMult() => HomeTier("laptop") >= 2 ? 0.88f : (HomeTier("laptop") == 1 ? 0.95f : 1f);
        private int HomeQueueBonus() => (HomeTier("laptop") >= 2 ? 1 : 0) + (HomeTier("desk") >= 2 ? 3 : (HomeTier("desk") == 1 ? 1 : 0));

        // ---- Sammel-Faktoren für Sim.Skills (Perks + Einrichtung) ----------------------------
        private float ProgressCapacityMult() => 1f + 0.1f * PerkRank("p_lager");
        private float ProgressLeadMult() => (1f - 0.06f * PerkRank("p_liefer")) * HomeLeadMult();
        private float ProgressPurchaseMult() => (1f - 0.03f * PerkRank("p_einkauf")) * HomePurchaseMult();
        private int ProgressQueueBonus() => 2 * PerkRank("p_queue") + HomeQueueBonus();
        private float ProgressStaffMult() => (1f + 0.08f * PerkRank("p_team")) * (WasteFull ? GameData.WasteFullStaffMult : 1f);

        /// <summary>Zusätzlich tragbare Pakete (Perk "Packesel", Hand-Labelgerät).</summary>
        public int CarryBonus() => PerkRank("p_tragen") + (HasEquipment("labelgun") ? 1 : 0);

        // =====================================================================================
        // 3. Ausrüstung
        // =====================================================================================
        public int EquipmentCount(string id) => EquipmentOwned.TryGetValue(id ?? "", out int n) ? n : 0;

        public int EquipmentMax(string id)
        {
            var e = GameData.EquipmentDef(id);
            if (e == null) return 0;
            return LocationStage >= 1 ? e.MaxWarehouse : e.MaxGarage;
        }

        /// <summary>Aufgestellte (wirksame) Stückzahl am aktuellen Standort.</summary>
        public int EquipmentActive(string id) => Math.Min(EquipmentCount(id), EquipmentMax(id));

        public bool HasEquipment(string id) => EquipmentActive(id) > 0;

        /// <summary>"max", "level", "available".</summary>
        public string EquipmentState(string id)
        {
            var e = GameData.EquipmentDef(id);
            if (e == null) return "unknown";
            if (EquipmentCount(id) >= Math.Max(e.MaxGarage, e.MaxWarehouse) || EquipmentCount(id) >= EquipmentMax(id)) return "max";
            if (Level < e.Level) return "level";
            return "available";
        }

        public bool BuyEquipment(string id)
        {
            var e = GameData.EquipmentDef(id);
            string st = EquipmentState(id);
            if (st == "level")
            {
                Notify("Ab Firmenlevel " + e.Level + ".", "bad");
                return false;
            }
            if (st != "available") return false;
            if (Money < e.Cost)
            {
                Notify("Nicht genug Geld!", "bad");
                Sound("error");
                return false;
            }
            Spend(e.Cost, "other");
            EquipmentOwned[id] = EquipmentCount(id) + 1;
            Notify(e.Name + " gekauft: " + e.Effect, "good");
            Sound("levelup", 0.02f, -6f);
            EquipmentChanged?.Invoke();
            WasteChanged?.Invoke();
            RaiseProgress();
            return true;
        }

        /// <summary>Tempo-Faktor je Rolle (zusätzliche Packtische/Labeldrucker).</summary>
        public float StaffRoleMult(string role)
        {
            if (role == "packer") return 1f + GameData.ExtraStationStaffBonus * EquipmentActive("packtisch");
            if (role == "versand") return 1f + GameData.ExtraStationStaffBonus * EquipmentActive("labeldrucker");
            return 1f;
        }

        /// <summary>Hand-Labelgerät: Pakete werden beim Aufnehmen am Packtisch etikettiert.</summary>
        public bool AutoLabelOnPickup => HasEquipment("labelgun");

        private void UpdateFoldingMachine(float m)
        {
            if (!HasEquipment("faltmaschine") || FlatTotal() <= 0)
            {
                _foldProgress = 0f;
                return;
            }
            _foldProgress += m / GameData.FoldMachineMinutes;
            bool changed = false;
            while (_foldProgress >= 1f && FlatTotal() > 0)
            {
                _foldProgress -= 1f;
                int best = -1;
                for (int s = 0; s < 3; s++)
                    if (FlatPackaging[s] > 0 && (best < 0 || Packaging[s] < Packaging[best])) best = s;
                if (best < 0) break;
                FlatPackaging[best]--;
                Packaging[best]++;
                changed = true;
            }
            if (changed) RaiseEconomyChanged();
        }

        // =====================================================================================
        // 5. Mengenrabatt
        // =====================================================================================
        public int PurchasedOf(string pid) => PurchasedUnits.TryGetValue(pid ?? "", out int n) ? n : 0;

        /// <summary>Mengenrabatt (0..0,15) aus der insgesamt gekauften Stückzahl.</summary>
        public float VolumeDiscount(string pid)
        {
            int units = PurchasedOf(pid);
            float d = 0f;
            foreach (var t in GameData.VolumeTiers)
                if (units >= t.Units) d = t.Discount;
            return d;
        }

        /// <summary>Händlerstatus-Rabatt aus dem Firmenlevel.</summary>
        public float LevelDiscount() => Math.Min(0.045f, Math.Max(0, Level - 1) * GameData.LevelDiscountPerLevel);

        /// <summary>Preisfaktor für Einkäufe aus Menge und Level (Skaleneffekte).</summary>
        public float ScaleDiscountMult(string pid) => 1f - VolumeDiscount(pid) - LevelDiscount();

        /// <summary>Stück bis zur nächsten Rabattstufe (0 = höchste erreicht).</summary>
        public int UnitsToNextVolumeTier(string pid)
        {
            int units = PurchasedOf(pid);
            foreach (var t in GameData.VolumeTiers)
                if (t.Units > units) return t.Units - units;
            return 0;
        }

        private void TrackPurchase(string pid, int qty) => PurchasedUnits[pid] = PurchasedOf(pid) + Math.Max(0, qty);

        // =====================================================================================
        // 5./7. Porto & Paketdienst
        // =====================================================================================
        public CarrierDef CurrentCarrier => GameData.Carriers[Mathx.Clamp(Carrier, 0, GameData.Carriers.Length - 1)];

        public PortoTier CurrentPortoTier()
        {
            var tier = GameData.PortoTiers[0];
            foreach (var t in GameData.PortoTiers)
                if (TotalShipped >= t.Shipped) tier = t;
            return tier;
        }

        public PortoTier NextPortoTier()
        {
            foreach (var t in GameData.PortoTiers)
                if (TotalShipped < t.Shipped) return t;
            return null;
        }

        /// <summary>Porto je Paket in € (Größe 0-2) für einen Paketdienst (-1 = aktueller).</summary>
        public float PortoFor(int size, int carrier = -1)
        {
            var c = carrier < 0 ? CurrentCarrier : GameData.Carriers[Mathx.Clamp(carrier, 0, GameData.Carriers.Length - 1)];
            float p = GameData.PortoBySize[Mathx.Clamp(size, 0, 2)] * CurrentPortoTier().Mult * (1f - 0.06f * PerkRank("p_porto"));
            return p * c.PortoMult + c.PortoExtra;
        }

        public void SetCarrier(int index)
        {
            index = Mathx.Clamp(index, 0, GameData.Carriers.Length - 1);
            if (index == Carrier) return;
            Carrier = index;
            Notify("Versand ab jetzt mit " + CurrentCarrier.Name + ".", "info");
            RaiseProgress();
        }

        private void ChargePorto(ItemData pkg)
        {
            PortoCarry += PortoFor(pkg.EffectivePackSize);
            int whole = (int)Math.Floor(PortoCarry);
            if (whole <= 0) return;
            PortoCarry -= whole;
            Spend(whole, "shipping");
            TotalPorto += whole;
        }

        // =====================================================================================
        // 7. Lieferstörungen
        // =====================================================================================
        public DisruptionDef ActiveDisruption => DisruptionId != "" && Day <= DisruptionUntilDay ? GameData.Disruption(DisruptionId) : null;

        public void StartDisruption(string id, int days = 1)
        {
            var d = GameData.Disruption(id) ?? GameData.Disruptions[0];
            DisruptionId = d.Id;
            DisruptionUntilDay = Day + Math.Max(1, days) - 1;
            _delayToastDay = false;
            Notify(d.Name + "! " + d.Text + " Tipp: PaketBlitz-App › TurboKurier.", "bad");
            Events.AddMail("PaketBlitz Störungsmeldung", d.Name, d.Text + " Voraussichtlich bis " + (DisruptionUntilDay == Day ? "heute Abend" : GameData.WeekdayName(DisruptionUntilDay)) +
                ". Für pünktliche Lieferung kannst du auf TurboKurier Express umstellen (teurer).", d.Icon);
            Daily.Notes.Add("Lieferstörung: " + d.Name);
            DisruptionChanged?.Invoke(d);
            RaiseProgress();
        }

        public void EndDisruption()
        {
            if (DisruptionId == "") return;
            DisruptionId = "";
            Notify("Der Paketdienst läuft wieder normal.", "good");
            DisruptionChanged?.Invoke(null);
            RaiseProgress();
        }

        // =====================================================================================
        // 8. Fehlerhafte Chargen
        // =====================================================================================
        public int DefectsOf(string pid) => DefectUnits.TryGetValue(pid ?? "", out int n) ? n : 0;

        private int DockQtyFor(string pid)
        {
            int n = 0;
            foreach (var c in DockCrates)
                if (c.Product == pid) n += c.Quantity;
            return n;
        }

        /// <summary>Anteil defekter Ware im Bestand (Lager + Wareneingang) eines Produkts.</summary>
        public float DefectShare(string pid)
        {
            int all = StockQty(pid) + DockQtyFor(pid);
            return all > 0 ? Mathx.Clamp01((float)DefectsOf(pid) / all) : 0f;
        }

        /// <summary>Produkte mit gemeldeten Defekten.</summary>
        public List<string> DefectiveProducts()
        {
            var l = new List<string>();
            foreach (var p in GameData.Products)
                if (DefectsOf(p.Id) > 0) l.Add(p.Id);
            return l;
        }

        /// <summary>Lieferung ist angekommen: kleine Chance auf eine fehlerhafte Charge (je nach Lieferant).</summary>
        private void OnDeliveryArrived(Delivery d, int qty)
        {
            if (Day < GameData.DefectMinDay || TutorialStep >= 0 || StoryStage != "business" || qty <= 0) return;
            float chance = d.Quality >= GameData.QualityPremium ? GameData.DefectChancePremium
                : (d.Quality < GameData.QualityStandard ? GameData.DefectChanceCheap : GameData.DefectChanceStandard);
            if (Rng.Value() >= chance) return;
            int bad = Math.Max(1, Mathx.RoundToInt(qty * Rng.Range(GameData.DefectShareMin, GameData.DefectShareMax)));
            AddDefectBatch(d.Product, bad);
        }

        /// <summary>Meldet eine fehlerhafte Charge (auch für Tests/Admin).</summary>
        public void AddDefectBatch(string pid, int units)
        {
            if (!GameData.IsProduct(pid) || units <= 0) return;
            DefectUnits[pid] = DefectsOf(pid) + units;
            string name = GameData.Product(pid).Name;
            Notify("Qualitätsalarm: " + units + "× " + name + " aus der letzten Lieferung sind defekt! Rückruf im Laptop (Firma › Logistik) – sonst drohen Retouren.", "bad");
            Events.AddMail("Qualitätskontrolle", "Fehlerhafte Charge: " + name,
                "Bei der Wareneingangsprüfung ist uns aufgefallen: Etwa " + units + " Stück " + name + " sind defekt. Verkaufst du sie, kommen viele zurück und es hagelt 1-Stern-Bewertungen. " +
                "Du kannst die defekten Stück an den Lieferanten zurückschicken (volle Erstattung).", "warning");
            Daily.Notes.Add("Fehlerhafte Charge: " + units + "× " + name);
            DefectiveBatch?.Invoke(pid, units);
            RaiseProgress();
        }

        /// <summary>Rückruf: defekte Stück aus Lager und Kisten entfernen, Lieferant erstattet. Gibt die Stückzahl zurück.</summary>
        public int RecallDefects(string pid)
        {
            int want = DefectsOf(pid);
            if (want <= 0) return 0;
            int removed = Math.Min(want, StockQty(pid));
            if (removed > 0) Stock[pid].Qty -= removed;
            for (int i = DockCrates.Count - 1; i >= 0 && removed < want; i--)
            {
                var c = DockCrates[i];
                if (c.Product != pid) continue;
                int take = Math.Min(c.Quantity, want - removed);
                c.Quantity -= take;
                removed += take;
                if (c.Quantity <= 0) DockCrates.RemoveAt(i);
            }
            DefectUnits.Remove(pid);
            int refund = Mathx.RoundToInt(removed * GameData.Product(pid).UnitCost * GameData.DefectRefundShare);
            if (refund > 0)
            {
                Money += refund;
                Daily.IncomeOther += refund;
            }
            Notify(removed + "× " + GameData.Product(pid).Name + " zurückgeschickt. Erstattung: " + Fmt.Money(refund) + ".", "good");
            RaiseProgress();
            return removed;
        }

        private void ClampDefects()
        {
            List<string> fix = null;
            foreach (var kv in DefectUnits)
            {
                int avail = StockQty(kv.Key) + DockQtyFor(kv.Key);
                if (kv.Value > avail)
                {
                    if (fix == null) fix = new List<string>();
                    fix.Add(kv.Key);
                }
            }
            if (fix == null) return;
            foreach (var k in fix)
            {
                int avail = StockQty(k) + DockQtyFor(k);
                if (avail <= 0) DefectUnits.Remove(k);
                else DefectUnits[k] = avail;
            }
        }

        // =====================================================================================
        // Versand-Haken (aus ShipPackage)
        // =====================================================================================
        /// <summary>
        /// Porto abbuchen, Lieferstörung und Defekte würfeln. Verspätet die Störung das Paket, wird
        /// onTime falsch und reviewNow nach hinten verschoben (schlechtere Bewertung). Gibt true zurück,
        /// wenn der Artikel defekt war (dann gibt es statt der normalen Bewertung Ärger).
        /// </summary>
        private bool ProgressShip(ItemData pkg, ref bool onTime, ref float reviewNow, float dueWindow)
        {
            ChargePorto(pkg);
            var c = CurrentCarrier;
            _reviewStarBonus = c.StarBonusChance;
            var dis = ActiveDisruption;
            if (dis != null && !c.Reliable && Rng.Value() < dis.DelayChance)
            {
                onTime = false;
                reviewNow += Math.Max(60f, dueWindow) * 1.2f;
                TotalDelayed++;
                if (!_delayToastDay)
                {
                    _delayToastDay = true;
                    Notify(dis.Name + ": Pakete kommen zu spät an – Kundschaft ist sauer. TurboKurier ist immun.", "bad");
                }
                if (Rng.Value() < 0.35f)
                {
                    AddReview(pkg.Product, Rng.Value() < 0.5f ? 1 : 2, dis.Review);
                    _reviewStarBonus = -99f; // bereits bewertet
                }
            }
            int defects = DefectsOf(pkg.Product);
            if (defects <= 0) return false;
            float share = (float)defects / Math.Max(1, StockQty(pkg.Product) + 1 + DockQtyFor(pkg.Product));
            if (Rng.Value() >= Mathx.Clamp01(share)) return false;
            DefectUnits[pkg.Product] = defects - 1;
            if (DefectUnits[pkg.Product] <= 0) DefectUnits.Remove(pkg.Product);
            TotalDefectsShipped++;
            AddReview(pkg.Product, Rng.Value() < 0.7f ? 1 : 2, Rng.Pick(GameData.DefectReviews), 1.2f);
            if (Rng.Value() < GameData.DefectReturnChance)
                ScheduleReturn(pkg, Rng.Range(GameData.ReturnDelayMin * 0.5f, GameData.ReturnDelayMax * 0.6f), GameData.DefectReturnReason);
            return true;
        }

        /// <summary>Zusatzstern in der Bewertung (TurboKurier). Wird von ReviewShipment abgefragt.</summary>
        private int ConsumeStarBonus()
        {
            float c = _reviewStarBonus;
            _reviewStarBonus = 0f;
            return c > 0f && Rng.Value() < c ? 1 : 0;
        }

        /// <summary>true = Paket wurde wegen Lieferstörung schon bewertet (keine zweite Bewertung).</summary>
        private bool ReviewAlreadyGiven()
        {
            bool given = _reviewStarBonus < -1f;
            if (given) _reviewStarBonus = 0f;
            return given;
        }

        // =====================================================================================
        // 6. Verpackungsmüll
        // =====================================================================================
        public int WasteCapacity() => GameData.WasteCapacityByStage[Mathx.Clamp(LocationStage, 0, 1)] + (HasEquipment("container") ? GameData.WasteContainerBonus : 0);
        public bool WasteFull => Waste >= WasteCapacity();
        public float WasteFill => Mathx.Clamp((float)Waste / Math.Max(1, WasteCapacity()), 0f, 2f);

        public void AddWaste(int n)
        {
            if (n <= 0 || StoryStage != "business") return;
            bool wasFull = WasteFull;
            Waste = Math.Min(Waste + n, WasteCapacity() * 2);
            if (!wasFull && WasteFull)
                Notify("Mülltonnen voll! Personal wird langsamer, du stolperst über Säcke, die Nachbarn meckern. Sonderabholung oder Container (Firma › Einrichtung).", "bad");
            else if (!_wasteWarned && WasteFill >= 0.8f && !WasteFull)
            {
                _wasteWarned = true;
                Notify("Mülltonnen fast voll (" + Waste + "/" + WasteCapacity() + "). Nächste Abholung: " + NextGarbagePickupText() + ".", "info");
            }
            WasteChanged?.Invoke();
        }

        /// <summary>Müllabfuhr leert alle Tonnen (vom Müllwagen der Welt aufgerufen). Gibt die Menge zurück.</summary>
        public int CollectGarbage()
        {
            int n = Waste;
            Waste = 0;
            GarbagePending = false;
            _wasteWarned = false;
            TotalWasteCollected += n;
            if (n > 0 && StoryStage == "business") Notify("Müllabfuhr war da: " + n + " Ladungen Kartons und Folie weg.", "good");
            GarbageCollected?.Invoke(n);
            WasteChanged?.Invoke();
            RaiseEconomyChanged();
            return n;
        }

        private void RequestGarbagePickup(bool special)
        {
            if (GarbagePickupDue == null)
            {
                CollectGarbage();
                return;
            }
            GarbagePending = true;
            GarbagePendingSince = BClock();
            GarbagePickupDue.Invoke(special);
        }

        public int SpecialPickupCost() => GameData.GarbageSpecialCost[Mathx.Clamp(LocationStage, 0, 1)];

        /// <summary>Bezahlte Sonderabholung (sofort).</summary>
        public bool OrderGarbagePickup()
        {
            if (GarbagePending)
            {
                Notify("Der Müllwagen ist schon unterwegs.", "info");
                return false;
            }
            int cost = SpecialPickupCost();
            if (Money < cost)
            {
                Notify("Nicht genug Geld für die Sonderabholung.", "bad");
                return false;
            }
            Spend(cost, "other");
            Notify("Sonderabholung bestellt (" + Fmt.Money(cost) + ").", "info");
            RequestGarbagePickup(true);
            RaiseEconomyChanged();
            return true;
        }

        public bool IsPickupDay(int day) => Array.IndexOf(GameData.GarbagePickupWeekdays, GameData.WeekdayOf(day)) >= 0;

        public string NextGarbagePickupText()
        {
            for (int d = Day; d < Day + 8; d++)
            {
                if (!IsPickupDay(d)) continue;
                if (d == Day && (TimeMinutes > GameData.GarbagePickupMinute || _lastGarbageDay == Day)) continue;
                return (d == Day ? "heute" : GameData.WeekdayShort(d)) + " " + Fmt.Clock(GameData.GarbagePickupMinute);
            }
            return "bald";
        }

        private void UpdateGarbage()
        {
            if (IsPickupDay(Day) && _lastGarbageDay != Day && TimeMinutes >= GameData.GarbagePickupMinute)
            {
                _lastGarbageDay = Day;
                if (!GarbagePending) RequestGarbagePickup(false);
            }
            if (GarbagePending && BClock() - GarbagePendingSince > GameData.GarbageFallbackMinutes) CollectGarbage();
        }

        // =====================================================================================
        // 9./10. Markenstärke & Großhändler
        // =====================================================================================
        /// <summary>Markenstärke 0..1 aus Bewertung, Anzahl Bewertungen, Bekanntheit, Markenname und Verkäufen.</summary>
        public float BrandStrength()
        {
            float s = 0.35f * Mathx.Clamp01((Reputation - 3f) / 2f) + 0.2f * Mathx.Clamp01(ReviewCount / 150f) +
                      0.2f * Mathx.Clamp01(Awareness) + (BrandNamed ? 0.1f : 0f) + 0.15f * Mathx.Clamp01(TotalShipped / 1000f);
            return Mathx.Clamp01(s);
        }

        /// <summary>Wie viel mehr als den Marktpreis die Kundschaft für deine Marke zahlt (1,0-1,3).</summary>
        public float BrandPriceMult() => 1f + GameData.BrandMaxPremium * BrandStrength();

        /// <summary>Preisvorschlag mit Markenaufschlag (knapp unter dem, was die Marke trägt).</summary>
        public int SuggestedPrice(string pid) => Math.Max(1, Mathx.RoundToInt(Market.MarketPrice(pid) * BrandPriceMult() * 0.97f));

        /// <summary>
        /// Bezugspreis für die Nachfrage: bis zum Markenpreis (Markt × Markenfaktor) kostet ein Aufpreis
        /// keine Kundschaft, darüber sinkt die Nachfrage wie gewohnt.
        /// </summary>
        public float DemandReferencePrice(string pid, float market, float price) =>
            price <= market ? market : Math.Min(price, market * BrandPriceMult());

        public bool WholesaleUnlocked => Level >= GameData.WholesaleLevel && BrandNamed;

        private List<string> WholesaleProducts()
        {
            var l = new List<string>();
            foreach (var p in GameData.Products)
                if (ProductAvailable(p.Id) && (ShippedPerProduct.TryGetValue(p.Id, out int n) ? n : 0) >= GameData.WholesaleMinSold) l.Add(p.Id);
            return l;
        }

        /// <summary>Großhändler-Anfrage für deine Markenware (größer und besser bezahlt als normale Aufträge).</summary>
        public Contract GenerateWholesaleOffer(string company = "", string product = "", float payBonus = 1f)
        {
            var pool = WholesaleProducts();
            if (pool.Count == 0) return null;
            string pid = !string.IsNullOrEmpty(product) && pool.Contains(product) ? product : pool[Rng.Index(pool.Count)];
            var pd = GameData.Product(pid);
            int qty = Mathx.Clamp(Mathx.RoundToInt(ContractQuantity(pd) * GameData.WholesaleQtyMult / 10f) * 10, 30, 400);
            float unit = pd.RefPrice * Rng.Range(GameData.WholesalePayMin, GameData.WholesalePayMax) * BrandPriceMult() * ContractPayMult() * payBonus;
            int pay = RoundTo5(qty * unit);
            var c = new Contract
            {
                Id = NextContractId++, Company = string.IsNullOrEmpty(company) ? Rng.Pick(GameData.Wholesalers) : company,
                Reason = Rng.Pick(GameData.WholesaleReasons), Product = pid, Quantity = qty, Payment = pay,
                Penalty = RoundTo5(pay * GameData.ContractPenaltyFactor), Xp = Math.Max(30, Mathx.RoundToInt(pay * GameData.ContractXpFactor)),
                RepBonus = GameData.ContractRepBonus + 0.04f, RepPenalty = GameData.ContractRepPenalty, MinQuality = GameData.QualityStandard,
                Days = qty <= 100 ? 3 : (qty <= 200 ? 4 : 5), OfferDay = Day, OfferExpiresDay = Day + 1, State = ContractState.Offered,
                Special = payBonus > 1f, Wholesale = true,
            };
            Contracts.Add(c);
            Notify("Großhändler-Anfrage: " + c.Company + " will " + c.Title + " deiner Marke (" + Fmt.Money(c.Payment) + ").", "info");
            Explain("wholesale", GameData.MailWholesale, "factory");
            Sound("notify", 0.03f, -6f);
            ContractOffered?.Invoke(c);
            ContractsChanged?.Invoke();
            RaiseEconomyChanged();
            return c;
        }

        /// <summary>Aus CompleteContract: zufriedene Großhändler bestellen nach.</summary>
        private void OnContractCompletedProgress(Contract c)
        {
            if (c == null || !c.Wholesale) return;
            ReorderCompany = c.Company;
            ReorderProduct = c.Product;
            ReorderDay = Day + Rng.RangeInt(2, 3);
        }

        // =====================================================================================
        // Ablauf-Haken
        // =====================================================================================
        private void ProgressUpdate(float m)
        {
            UpdateEnergy(m);
            UpdateFoldingMachine(m);
            UpdateGarbage();
            CheckPerkPoints();
            if (WholesaleOfferAt >= 0f && TimeMinutes >= WholesaleOfferAt)
            {
                WholesaleOfferAt = -1f;
                if (WholesaleUnlocked && ContractOffers().Count < MaxOpenOffers) GenerateWholesaleOffer();
            }
        }

        private void ProgressNewDay()
        {
            Energy = MorningEnergy();
            _tiredWarned = false;
            ClampDefects();
            if (DisruptionId != "" && Day > DisruptionUntilDay) EndDisruption();
            if (StoryStage != "business") return;
            if (DisruptionId == "" && Day >= GameData.DisruptionMinDay && TutorialStep < 0 && Rng.Value() < GameData.DisruptionDailyChance)
                StartDisruption(Rng.Pick(GameData.Disruptions).Id, Rng.Value() < 0.35f ? 2 : 1);
            WholesaleOfferAt = -1f;
            if (WholesaleUnlocked && Weekday < 6 && Rng.Value() < 0.2f + 0.2f * BrandStrength()) WholesaleOfferAt = Rng.Range(540f, 840f);
            if (ReorderDay > 0 && Day >= ReorderDay)
            {
                ReorderDay = 0;
                if (WholesaleUnlocked) GenerateWholesaleOffer(ReorderCompany, ReorderProduct, GameData.WholesaleReorderBonus);
            }
        }

        private void ProgressEndDay(DaySummary s)
        {
            s.Shipping = Daily.Shipping;
            if (WasteFull && StoryStage == "business")
            {
                float pen = GameData.WasteFullRepPenalty * (WasteFill >= 1.25f ? 2f : 1f);
                Reputation = Mathx.Clamp(Reputation - pen, 0.5f, 5f);
                s.RepEnd = Reputation;
                s.Notes.Add("Müll quillt über – die Nachbarn beschweren sich (Bewertung −" + Fmt.Dec(pen, 2) + ").");
            }
        }

        /// <summary>Kurze Status-Hinweise für das HUD: {Text, Icon, "bad"/"info"}.</summary>
        public List<string[]> StatusChips()
        {
            var l = new List<string[]>();
            if (StoryStage != "business") return l;
            if (Tired) l.Add(new[] { "Müde · " + (int)Energy + " Energie", "battery", "bad" });
            if (WasteFull) l.Add(new[] { "Müll voll " + Waste + "/" + WasteCapacity(), "warning", "bad" });
            else if (WasteFill >= 0.8f) l.Add(new[] { "Müll " + Waste + "/" + WasteCapacity(), "boxes", "info" });
            var d = ActiveDisruption;
            if (d != null) l.Add(new[] { d.Name + (CurrentCarrier.Reliable ? " (TurboKurier)" : ""), d.Icon, CurrentCarrier.Reliable ? "info" : "bad" });
            if (DefectiveProducts().Count > 0) l.Add(new[] { "Defekte Charge im Lager", "warning", "bad" });
            return l;
        }

        // =====================================================================================
        // Speichern / Laden
        // =====================================================================================
        private static Dictionary<string, object> IntDict(Dictionary<string, int> d)
        {
            var o = new Dictionary<string, object>();
            foreach (var kv in d) o[kv.Key] = kv.Value;
            return o;
        }

        private static void ReadIntDict(Dictionary<string, object> s, string key, Dictionary<string, int> into)
        {
            into.Clear();
            foreach (var kv in J.O(s, key))
            {
                int v = J.I(kv.Value);
                if (v > 0) into[kv.Key] = v;
            }
        }

        private void AddProgressState(Dictionary<string, object> state)
        {
            state["perk_ranks"] = IntDict(PerkRanks);
            state["home_tiers"] = IntDict(HomeTiers);
            state["energy"] = (double)Energy;
            state["equipment"] = IntDict(EquipmentOwned);
            state["purchased_units"] = IntDict(PurchasedUnits);
            state["carrier"] = Carrier;
            state["porto_carry"] = (double)PortoCarry;
            state["total_porto"] = TotalPorto;
            state["disruption_id"] = DisruptionId ?? "";
            state["disruption_until"] = DisruptionUntilDay;
            state["total_delayed"] = TotalDelayed;
            state["defect_units"] = IntDict(DefectUnits);
            state["total_defects_shipped"] = TotalDefectsShipped;
            state["waste"] = Waste;
            state["total_waste_collected"] = TotalWasteCollected;
            state["garbage_day"] = _lastGarbageDay;
            state["wholesale_offer_at"] = (double)WholesaleOfferAt;
            state["reorder_company"] = ReorderCompany ?? "";
            state["reorder_product"] = ReorderProduct ?? "";
            state["reorder_day"] = ReorderDay;
        }

        /// <summary>Fehlen die Felder (älterer Spielstand), gelten die Startwerte.</summary>
        private void ReadProgressState(Dictionary<string, object> s)
        {
            ReadIntDict(s, "perk_ranks", PerkRanks);
            var drop = new List<string>();
            foreach (var kv in PerkRanks)
                if (GameData.Perk(kv.Key) == null) drop.Add(kv.Key);
            foreach (var k in drop) PerkRanks.Remove(k);
            foreach (var p in GameData.Perks)
                if (PerkRank(p.Id) > p.MaxRank) PerkRanks[p.Id] = p.MaxRank;
            ReadIntDict(s, "home_tiers", HomeTiers);
            foreach (var k in new List<string>(HomeTiers.Keys)) HomeTiers[k] = Mathx.Clamp(HomeTiers[k], 0, 2);
            Energy = Mathx.Clamp(J.F(s, "energy", GameData.EnergyMax), 0f, GameData.EnergyMax);
            ReadIntDict(s, "equipment", EquipmentOwned);
            ReadIntDict(s, "purchased_units", PurchasedUnits);
            Carrier = Mathx.Clamp(J.I(s, "carrier"), 0, GameData.Carriers.Length - 1);
            PortoCarry = Mathx.Clamp(J.F(s, "porto_carry"), 0f, 1f);
            TotalPorto = J.I(s, "total_porto");
            DisruptionId = J.S(s, "disruption_id", "");
            if (GameData.Disruption(DisruptionId) == null) DisruptionId = "";
            DisruptionUntilDay = J.I(s, "disruption_until");
            TotalDelayed = J.I(s, "total_delayed");
            ReadIntDict(s, "defect_units", DefectUnits);
            TotalDefectsShipped = J.I(s, "total_defects_shipped");
            Waste = Math.Max(0, J.I(s, "waste"));
            TotalWasteCollected = J.I(s, "total_waste_collected");
            _lastGarbageDay = J.I(s, "garbage_day");
            WholesaleOfferAt = J.F(s, "wholesale_offer_at", -1f);
            ReorderCompany = J.S(s, "reorder_company", "");
            ReorderProduct = J.S(s, "reorder_product", "");
            ReorderDay = J.I(s, "reorder_day");
            _perkPointsSeen = PerkPointsTotal();
        }
    }
}
