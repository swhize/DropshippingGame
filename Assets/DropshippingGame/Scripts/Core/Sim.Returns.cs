using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Eine Rücksendung, die gerade zurück zum Wareneingang unterwegs ist.</summary>
    public sealed class PendingReturn
    {
        /// <summary>Das Retourenpaket (<see cref="ItemKind.Return"/>), Price = Erstattungsbetrag.</summary>
        public ItemData Item;
        /// <summary>Ankunft auf der Geschäftsuhr.</summary>
        public float ArriveAt;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "item", Item.ToJson() }, { "arrive_at", (double)ArriveAt },
        };

        public static PendingReturn FromJson(object o)
        {
            var d = J.Obj(o);
            var item = ItemData.FromJson(J.Get(d, "item"));
            item.Kind = ItemKind.Return;
            if (!GameData.IsProduct(item.Product)) item.Product = "huelle";
            return new PendingReturn { Item = item, ArriveAt = J.F(d, "arrive_at") };
        }
    }

    /// <summary>
    /// F2 Retouren: Ein Teil der verschickten Pakete kommt zurück. Die Wahrscheinlichkeit hängt von
    /// der Warenqualität, dem Karton (zu groß), Verspätung und Skills ab (plus etwas Zufall).
    /// Bei Ankunft wird erstattet und das Retourenpaket liegt am Wareneingang (<see cref="DockReturns"/>).
    /// Am Retourenplatz: als B-Ware einlagern (Qualität sinkt) oder entsorgen.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Ein Retourenpaket ist am Wareneingang angekommen (Erstattung wurde abgebucht).</summary>
        public event Action<ItemData> ReturnArrived;
        /// <summary>Retoure bearbeitet: (Retoure, true = als B-Ware eingelagert / false = entsorgt).</summary>
        public event Action<ItemData, bool> ReturnProcessed;
        /// <summary>Listen der Retouren (unterwegs / am Wareneingang) haben sich geändert.</summary>
        public event Action ReturnsChanged;

        /// <summary>Rücksendungen auf dem Weg zurück.</summary>
        public List<PendingReturn> ReturnsIncoming = new List<PendingReturn>();
        /// <summary>Retourenpakete am Wareneingang (älteste zuerst).</summary>
        public List<ItemData> DockReturns = new List<ItemData>();
        public int TotalReturns, TotalRefunds, TotalReturnsRestocked, TotalReturnsDisposed;
        public Dictionary<string, int> ReturnsPerProduct = new Dictionary<string, int>();

        // =====================================================================================
        // Wahrscheinlichkeit
        // =====================================================================================
        /// <summary>
        /// Rücksende-Wahrscheinlichkeit eines Pakets (ohne Zufallsstreuung): Basis 3 %, Billig-Ware bis ×3,5,
        /// Premium ×0,3, zu großer Karton ×1,6, verspätet ×1,5, Skill "Kundenflüsterer" ×0,65. Max. 35 %.
        /// </summary>
        public float ReturnChance(ItemData pkg, bool late = false)
        {
            if (pkg == null) return 0f;
            float c = GameData.ReturnBaseChance * Mathx.Clamp(1f + (1f - pkg.Quality) * 5f, 0.3f, 3.5f);
            if (pkg.Oversized) c *= GameData.ReturnOversizeMult;
            if (late) c *= GameData.ReturnLateMult;
            c *= ReturnRateMult();
            return Mathx.Clamp(c, 0f, GameData.ReturnMaxChance);
        }

        /// <summary>Erwartete Retourenquote für ein Produkt mit aktuellem Lager (für Anzeigen).</summary>
        public float ExpectedReturnRate(string id)
        {
            var probe = new ItemData { Kind = ItemKind.Package, Product = id, Quality = StockQuality(id) };
            return ReturnChance(probe);
        }

        /// <summary>Tatsächliche Retourenquote bisher (Retouren / Verkäufe) für ein Produkt.</summary>
        public float ReturnRate(string id)
        {
            int r = ReturnsPerProduct.TryGetValue(id, out int v) ? v : 0;
            int s = ShippedPerProduct.TryGetValue(id, out int w) ? w : 0;
            return s > 0 ? (float)r / s : 0f;
        }

        /// <summary>Tatsächliche Retourenquote über alle Pakete.</summary>
        public float ReturnRateTotal() => TotalShipped > 0 ? (float)TotalReturns / TotalShipped : 0f;

        /// <summary>Qualität, mit der eine Retoure als B-Ware ins Lager käme.</summary>
        public float BStockQuality(ItemData ret) => ret == null ? 0.5f : Mathx.Clamp(ret.Quality * GameData.BStockQualityMult, 0.3f, 2f);

        public int ReturnsAtDock => DockReturns.Count;

        // =====================================================================================
        // Entstehen & Ankunft
        // =====================================================================================
        private void MaybeScheduleReturn(ItemData pkg, bool late)
        {
            float chance = ReturnChance(pkg, late) * Rng.Range(0.8f, 1.2f);
            if (Rng.Value() >= chance) return;
            ScheduleReturn(pkg, Rng.Range(GameData.ReturnDelayMin, GameData.ReturnDelayMax), PickReturnReason(pkg, late));
        }

        /// <summary>Plant eine Rücksendung für ein verschicktes Paket (auch für Ereignisse und Tests).</summary>
        public ItemData ScheduleReturn(ItemData pkg, float delayMinutes, string reason = "")
        {
            if (pkg == null || !GameData.IsProduct(pkg.Product)) return null;
            var ret = new ItemData
            {
                Kind = ItemKind.Return, Product = pkg.Product, Quantity = 1, Quality = pkg.Quality, Price = Math.Max(0, pkg.Price),
                Color = pkg.Color, Logo = pkg.Logo, PackSize = pkg.PackSize,
                Note = string.IsNullOrEmpty(reason) ? Rng.Pick(GameData.ReturnReasons) : reason,
            };
            ret.CopyTicketFrom(pkg);
            ret.ContractId = 0;
            float at = BClock() + Math.Max(0f, delayMinutes);
            ret.Created = at;
            ReturnsIncoming.Add(new PendingReturn { Item = ret, ArriveAt = at });
            ReturnsChanged?.Invoke();
            return ret;
        }

        private string PickReturnReason(ItemData pkg, bool late)
        {
            if (late && Rng.Value() < 0.5f) return GameData.ReturnReasonLate;
            if (pkg.Oversized && Rng.Value() < 0.5f) return GameData.ReturnReasonOversize;
            if (pkg.Quality < 0.8f && Rng.Value() < 0.6f) return Rng.Pick(GameData.ReturnReasonsQuality);
            return Rng.Pick(GameData.ReturnReasons);
        }

        /// <summary>Retourenwelle (Ereignis): count Retouren aus zuletzt verkauften Produkten treffen bald ein.</summary>
        public int TriggerReturnWave(int count)
        {
            int made = 0;
            int total = 0;
            foreach (var p in GameData.Products) total += ShippedPerProduct.TryGetValue(p.Id, out int n) ? n : 0;
            if (total <= 0) return 0;
            for (int i = 0; i < count; i++)
            {
                int r = Rng.Index(total);
                string pid = GameData.Products[0].Id;
                foreach (var p in GameData.Products)
                {
                    int n = ShippedPerProduct.TryGetValue(p.Id, out int v) ? v : 0;
                    if (r < n)
                    {
                        pid = p.Id;
                        break;
                    }
                    r -= n;
                }
                var pkg = new ItemData
                {
                    Kind = ItemKind.Labeled, Product = pid, Price = CurrentSalePrice(pid), Quality = StockQuality(pid),
                    Customer = RandomCustomer(), City = Rng.Pick(GameData.Cities),
                };
                if (ScheduleReturn(pkg, Rng.Range(20f, 140f), "Hab ein Video gesehen: Man kann einfach alles zurückschicken!") != null) made++;
            }
            RaiseEconomyChanged();
            return made;
        }

        private void UpdateReturns()
        {
            if (ReturnsIncoming.Count == 0) return;
            float now = BClock();
            bool changed = false;
            for (int i = 0; i < ReturnsIncoming.Count;)
            {
                if (now < ReturnsIncoming[i].ArriveAt)
                {
                    i++;
                    continue;
                }
                var ret = ReturnsIncoming[i].Item;
                ReturnsIncoming.RemoveAt(i);
                ReturnArrive(ret);
                changed = true;
            }
            if (changed)
            {
                ReturnsChanged?.Invoke();
                RaiseEconomyChanged();
            }
        }

        private void ReturnArrive(ItemData ret)
        {
            int refund = Math.Max(0, ret.Price);
            Money -= refund;
            Daily.Refunds += refund;
            Daily.Returns++;
            TotalReturns++;
            TotalRefunds += refund;
            ReturnsPerProduct[ret.Product] = (ReturnsPerProduct.TryGetValue(ret.Product, out int n) ? n : 0) + 1;
            ret.Created = BClock();
            DockReturns.Add(ret);
            string who = string.IsNullOrEmpty(ret.Customer) ? "Kundschaft" : ret.Customer + (string.IsNullOrEmpty(ret.City) ? "" : " (" + ret.City + ")");
            Notify("Retoure von " + who + ": " + GameData.Product(ret.Product).Name + " – " + Fmt.Money(refund) + " erstattet. „" + ret.Note + "“", "bad");
            Sound("bad", 0.04f, -8f);
            Explain("returns", GameData.MailReturns, "box");
            ReturnArrived?.Invoke(ret);
        }

        // =====================================================================================
        // Wareneingang & Retourenplatz
        // =====================================================================================
        /// <summary>Nimmt das älteste Retourenpaket vom Wareneingang (oder null).</summary>
        public ItemData PickupReturn()
        {
            if (DockReturns.Count == 0)
            {
                Notify("Keine Retouren am Wareneingang.", "info");
                return null;
            }
            var r = DockReturns[0];
            DockReturns.RemoveAt(0);
            ReturnsChanged?.Invoke();
            RaiseEconomyChanged();
            return r;
        }

        /// <summary>Legt ein Retourenpaket zurück an den Wareneingang.</summary>
        public void PutBackReturn(ItemData ret)
        {
            if (ret == null) return;
            ret.Kind = ItemKind.Return;
            DockReturns.Insert(0, ret);
            ReturnsChanged?.Invoke();
            RaiseEconomyChanged();
        }

        /// <summary>
        /// Retourenplatz: restock = true lagert die Retoure als B-Ware ein (Qualität × 0,75, braucht
        /// Lagerplatz), false entsorgt sie. Gibt false zurück, wenn nichts passiert ist.
        /// </summary>
        public bool ProcessReturn(ItemData ret, bool restock) => ProcessReturnInternal(ret, restock, false);

        private bool ProcessReturnInternal(ItemData ret, bool restock, bool silent)
        {
            if (ret == null || ret.Kind != ItemKind.Return || !GameData.IsProduct(ret.Product)) return false;
            string pname = GameData.Product(ret.Product).Name;
            if (restock)
            {
                if (StockTotal() + 1 > Capacity())
                {
                    if (!silent)
                    {
                        Notify("Lager voll – die B-Ware passt nicht mehr rein. Entsorgen oder Platz schaffen.", "bad");
                        Sound("error");
                    }
                    return false;
                }
                float q = BStockQuality(ret);
                AddToStock(ret.Product, 1, q);
                Daily.ReturnsRestocked++;
                TotalReturnsRestocked++;
                if (!silent)
                {
                    Notify("B-Ware eingelagert: 1× " + pname + " (" + GameData.QualityName(q) + ").", "good");
                    Sound("place");
                }
                AddXp(GameData.ReturnRestockXp);
                ChallengeProgress("restock", 1f);
                AddWaste(GameData.WastePerReturn);
            }
            else
            {
                Daily.ReturnsDisposed++;
                TotalReturnsDisposed++;
                if (!silent)
                {
                    Notify("Retoure entsorgt: " + pname + ".", "info");
                    Sound("drop");
                }
                AddXp(GameData.ReturnDisposeXp);
            }
            ReturnProcessed?.Invoke(ret, restock);
            ReturnsChanged?.Invoke();
            RaiseEconomyChanged();
            return true;
        }

        /// <summary>Lagerist:in bearbeitet eine Retoure am Wareneingang (Einlagern, wenn die Qualität reicht).</summary>
        private bool StaffProcessReturn()
        {
            if (DockReturns.Count == 0) return false;
            var r = DockReturns[0];
            bool restock = BStockQuality(r) >= 0.45f && StockTotal() + 1 <= Capacity();
            DockReturns.RemoveAt(0);
            if (!ProcessReturnInternal(r, restock, true))
            {
                DockReturns.Insert(0, r);
                return false;
            }
            return true;
        }
    }
}
