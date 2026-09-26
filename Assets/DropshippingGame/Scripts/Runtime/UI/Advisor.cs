using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Engpass-Assistent „Warum läuft's nicht?“ (GAME_IDEAS Idee 63 / Skizze 5.3).
    /// Liest nur den Spielzustand (keine Zufallszahlen, kein Zustand) und nennt die wichtigsten
    /// Engpässe mit passendem Ziel. Anzeige: Laptop-Übersicht (Karte) und Handy › Status.
    /// Liegt bewusst in der UI-Schicht; kann später 1:1 nach Core/Advisor.cs wandern.
    /// </summary>
    public static class Advisor
    {
        public sealed class Hint
        {
            public string Id;
            /// <summary>0 = Tipp, 1 = Warnung, 2 = dringend</summary>
            public int Severity;
            public string Icon, Text;
            /// <summary>Laptop-Ziel ("buy/ware" …) oder null.</summary>
            public string Laptop;
            /// <summary>Handy-App (0 Bestellungen, 1 Nachrichten, 2 Nachbestellen, 3 Status) oder −1.</summary>
            public int Phone = -1;
            public string Action;
        }

        public static List<Hint> Evaluate(Sim s)
        {
            var list = new List<Hint>();
            if (s == null || s.StoryStage != "business") return list;
            int order = 0;
            var orderOf = new Dictionary<Hint, int>();

            void Add(string id, int sev, string icon, string text, string laptop, int phone = -1, string action = null)
            {
                var h = new Hint { Id = id, Severity = sev, Icon = icon, Text = text, Laptop = laptop, Phone = phone, Action = action };
                orderOf[h] = order++;
                list.Add(h);
            }

            try
            {
                // R1 Shop leer
                bool anyListed = false;
                foreach (var p in GameData.Products)
                    if (s.IsListed(p.Id)) anyListed = true;
                if (!anyListed) Add("shop_empty", 2, "globe", "Dein Shop ist leer – stell ein Produkt online.", "shop/webshop", -1, "Zum Shop");

                // R2 ausverkauft
                foreach (var p in GameData.Products)
                {
                    if (!s.IsListed(p.Id)) continue;
                    if (s.StockQty(p.Id) + s.TravelingCountFor(p.Id) + s.DockCountFor(p.Id) > 0) continue;
                    Add("soldout_" + p.Id, 2, "warning", p.Name + " ist ausverkauft – nachbestellen.", "buy/ware", 2, "Nachbestellen");
                }

                // R3 Karton fehlt für eine wartende Bestellung
                var missing = new HashSet<int>();
                foreach (var o in s.OrderQueue)
                {
                    if (s.CartonFor(o.Product) >= 0) continue;
                    missing.Add(Mathf.Clamp(GameData.Product(o.Product).Size, 0, 2));
                }
                foreach (int size in missing)
                {
                    string n = GameData.SizeName(size);
                    if (s.FlatPackaging[size] > 0)
                        Add("carton_" + size, 2, "package", "Keine " + n + "-Kartons – am Falttisch falten (" + s.FlatPackaging[size] + " ungefaltet).", null);
                    else Add("carton_" + size, 2, "package", "Keine " + n + "-Kartons mehr – kaufen.", "buy/pack", -1, "Kartons kaufen");
                }

                // Großauftrag mit Frist heute
                foreach (var c in s.ActiveContracts())
                    if (s.ContractDaysLeft(c) <= 0 && c.Remaining > 0)
                        Add("contract_due_" + c.Id, 2, "pallet", "Großauftrag " + c.Company + ": Frist heute 20 Uhr, noch " + c.Remaining + " Stück.", "orders/active", -1, "Aufträge");

                // R4/R5 Fixkosten heute Abend
                int fixedCosts = s.FixedCostsPerDay();
                int after = s.Money - fixedCosts;
                if (after < GameData.BankruptLimit)
                    Add("bankrupt", 2, "bank", "Heute Abend droht die Pleite – dir fehlen " + Fmt.Money(GameData.BankruptLimit - after) + ".", "finance/bank", 3, "Bank");
                else if (after < 0 && fixedCosts > 0)
                    Add("fixed", 1, "bank", "Miete & Löhne heute Abend: " + Fmt.Money(fixedCosts) + " – du hast " + Fmt.Money(s.Money) + ".", "finance/bank", 3);

                // R6 Warteschlange fast voll / R7 verloren
                int cap = Math.Max(1, s.QueueCapacity());
                if (s.PendingCount() >= cap * 0.8f)
                    Add("queue", 1, "cart", "Warteschlange fast voll (" + s.PendingCount() + "/" + cap + ") – schneller packen oder Preis leicht erhöhen.", "shop/webshop", 0);
                if (s.Daily.Lost > 0)
                    Add("lost", 1, "angry", s.Daily.Lost == 1 ? "1 Bestellung heute verloren." : s.Daily.Lost + " Bestellungen heute verloren.", "shop/webshop", 0);

                // Retouren / Kisten am Wareneingang
                if (s.ReturnsAtDock > 0)
                    Add("returns", 1, "return", s.ReturnsAtDock + (s.ReturnsAtDock == 1 ? " Retoure wartet" : " Retouren warten") + " am Wareneingang – zum Retourenplatz.", "returns");
                if (s.DockCrates.Count > 0 && s.StockTotal() < s.Capacity())
                    Add("dock", 1, "box", s.DockCrates.Count + (s.DockCrates.Count == 1 ? " Kiste wartet" : " Kisten warten") + " am Wareneingang.", null);

                // R9 Preis / Bewertung
                foreach (var p in GameData.Products)
                {
                    if (!s.IsListed(p.Id)) continue;
                    float market = s.Market.MarketPrice(p.Id);
                    int price = s.CurrentSalePrice(p.Id);
                    if (market > 0 && price > market * 1.15f)
                        Add("price_" + p.Id, 1, "tag", p.Name + ": Preis " + Mathf.RoundToInt((price / market - 1f) * 100f) + " % über Markt – kaum Bestellungen.", "shop/webshop", -1, "Preise");
                }
                if (s.ReviewCount >= 3 && s.Reputation < 2.5f)
                    Add("rating", 1, "star", "Deine Bewertung (" + Fmt.Rating(s.Reputation) + ") schreckt Kundschaft ab – pünktlich liefern, bessere Ware.", "buy/ware");

                // R10 Lager voll
                if (s.StockTotal() >= s.Capacity() * 0.9f)
                    Add("storage", 1, "boxes", "Lager fast voll – weniger nachbestellen oder ausbauen.", "company/build");

                // R11 Entscheidung offen
                int pending = s.Events.PendingCount();
                if (pending > 0)
                    Add("decision", 1, "mail", pending == 1 ? "Eine Entscheidung wartet auf dem Handy." : pending + " Entscheidungen warten auf dem Handy.", "mail", 1, "Lesen");

                // Tipps
                var offers = s.ContractOffers();
                if (offers.Count > 0 && s.ActiveContractCount() < s.MaxActiveContracts())
                    Add("offers", 0, "pallet", offers.Count == 1 ? "Ein Großauftrag wartet auf deine Antwort." : offers.Count + " Großaufträge warten auf deine Antwort.", "orders/offers", 1);
                if (s.SkillPointsAvailable() > 0)
                    Add("skills", 0, "sparkle", s.SkillPointsAvailable() == 1 ? "1 Skillpunkt frei – Firma › Skills." : s.SkillPointsAvailable() + " Skillpunkte frei – Firma › Skills.", "company/skills");
                if (s.PackedCount() >= 3)
                    Add("label", 0, "truck", s.PackedCount() + " Pakete warten aufs Etikett.", null);
                if (s.TikTokAvailable() && s.ActiveBoost("tiktok") == null)
                    Add("tiktok", 0, "music", "Zeit für ein TikTok? (heute noch " + s.TikToksLeftToday() + ")", "shop/marketing");
                foreach (var c in s.Challenges)
                    if (!c.Done && c.Fraction >= 0.8f)
                    {
                        Add("challenge_" + c.Id, 0, "flag", "Fast geschafft: " + c.Title + " (" + c.ProgressText + ").", "company/goals", 3);
                        break;
                    }
                var g = s.NextGoal();
                if (g != null && s.TutorialStep < 0)
                {
                    float prog = Mathf.Clamp01(s.GoalValue(g) / Mathf.Max(g.Target, 0.001f));
                    if (prog >= 0.9f && prog < 1f) Add("goal", 0, "trophy", "Fast geschafft: „" + g.Title + "“ (" + UiFmt.Percent(prog) + ").", "company/goals");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            list.Sort((a, b) => a.Severity != b.Severity ? b.Severity.CompareTo(a.Severity) : orderOf[a].CompareTo(orderOf[b]));
            return list;
        }

        public static Color SeverityColor(int sev) => sev >= 2 ? Theme.LaptopBad : (sev == 1 ? Theme.LaptopAccent : Theme.LaptopTeal);
    }
}
