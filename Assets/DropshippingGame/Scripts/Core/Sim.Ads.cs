using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Eine Werbekampagne bei Fakebook (Zielgruppe + Slogan + Budget) oder Gugel (Suchbegriff + Gebot + Budget).</summary>
    public sealed class AdCampaign
    {
        public int Id;
        /// <summary>"fakebook" oder "gugel".</summary>
        public string Platform = "fakebook";
        public string Product = "";
        /// <summary>Fakebook: Zielgruppe und Slogan (Indizes in GameData.AdTargets / AdSlogans).</summary>
        public int Target, Slogan;
        /// <summary>Gugel: Suchbegriff-Index (GameData.GugelKeywords) und Gebot pro Klick in Cent.</summary>
        public int Keyword, Bid;
        /// <summary>Gugel: Anzeigenplatz 1–4 beim Start.</summary>
        public int Position;
        /// <summary>Höchstbetrag in Euro.</summary>
        public int Budget;
        public float Spent, Impressions, Clicks, Sales, Revenue;
        /// <summary>Nachfrage-Faktor für das Produkt, solange die Kampagne läuft.</summary>
        public float Mult = 1f;
        public float StartedAt, EndsAt;
        public int Day;
        public bool Active;
        /// <summary>Warum beendet: "done", "budget", "stopped", "money".</summary>
        public string EndReason = "";

        public bool IsGugel => Platform == "gugel";
        public string PlatformName => IsGugel ? "Gugel" : "Fakebook";
        public float Ctr => Impressions > 0f ? Clicks / Impressions : 0f;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "id", Id }, { "platform", Platform }, { "product", Product }, { "target", Target }, { "slogan", Slogan },
            { "keyword", Keyword }, { "bid", Bid }, { "position", Position }, { "budget", Budget }, { "spent", (double)Spent },
            { "impressions", (double)Impressions }, { "clicks", (double)Clicks }, { "sales", (double)Sales }, { "revenue", (double)Revenue },
            { "mult", (double)Mult }, { "started_at", (double)StartedAt }, { "ends_at", (double)EndsAt }, { "day", Day }, { "active", Active },
            { "end_reason", EndReason },
        };

        public static AdCampaign FromJson(object o)
        {
            var d = J.Obj(o);
            return new AdCampaign
            {
                Id = J.I(d, "id"), Platform = J.S(d, "platform", "fakebook"), Product = J.S(d, "product", ""), Target = J.I(d, "target"),
                Slogan = J.I(d, "slogan"), Keyword = J.I(d, "keyword"), Bid = J.I(d, "bid"), Position = J.I(d, "position", 1),
                Budget = J.I(d, "budget"), Spent = J.F(d, "spent"), Impressions = J.F(d, "impressions"), Clicks = J.F(d, "clicks"),
                Sales = J.F(d, "sales"), Revenue = J.F(d, "revenue"), Mult = J.F(d, "mult", 1f), StartedAt = J.F(d, "started_at"),
                EndsAt = J.F(d, "ends_at"), Day = J.I(d, "day", 1), Active = J.B(d, "active"), EndReason = J.S(d, "end_reason", ""),
            };
        }
    }

    /// <summary>
    /// Marketing-Web-Apps: Fakebook-Anzeigen, Gugel-Anzeigen (Suchbegriff-Gebote), Fake-Profile
    /// („Social Proof“, kann auffliegen) und Fake-Webseiten (SEO, kann auffliegen). Die Wirkung läuft
    /// über normale <see cref="Boost"/>s (Quellen "ad:{id}", "proof", "seo:{produkt}"), damit HUD,
    /// Handy und Nachfrage (<see cref="DemandRate"/>) sie ohne Änderung kennen. Kosten werden laufend
    /// als "marketing" abgebucht. Eigener Zufall (<see cref="AdsReseed"/>) – andere Systeme bleiben
    /// deterministisch.
    /// </summary>
    public sealed partial class Sim
    {
        /// <summary>Kampagne beendet (Laufzeit um, Budget weg, gestoppt, Konto leer).</summary>
        public event Action<AdCampaign> AdCampaignEnded;
        /// <summary>Fake aufgeflogen: ("profiles" | "site", Produkt-ID bei Seiten).</summary>
        public event Action<string, string> FakeExposed;

        public List<AdCampaign> AdCampaigns = new List<AdCampaign>();
        public int NextAdId = 1;
        public int FakeProfiles;
        public List<FakeSite> FakeSites = new List<FakeSite>();
        public int FakeExposedTotal;
        private float _adBill;

        private Rng _adsRng;
        private Rng AdsRng => _adsRng ?? (_adsRng = new Rng());

        /// <summary>Für Tests: Zufall der Fake-Entlarvung festlegen.</summary>
        public void AdsReseed(int seed) => _adsRng = new Rng(seed);

        public bool FakebookUnlocked => Level >= GameData.FakebookLevel;
        public bool GugelUnlocked => Level >= GameData.GugelLevel;
        public bool FakeSitesUnlocked => Level >= GameData.FakeSiteLevel;

        // =====================================================================================
        // Abfragen (verbrauchen keinen Zufall)
        // =====================================================================================
        public List<AdCampaign> ActiveAds(string platform = null)
        {
            var l = new List<AdCampaign>();
            foreach (var c in AdCampaigns)
                if (c.Active && (platform == null || c.Platform == platform)) l.Add(c);
            return l;
        }

        /// <summary>Alle Kampagnen einer Plattform, neueste zuerst.</summary>
        public List<AdCampaign> AdHistory(string platform)
        {
            var l = new List<AdCampaign>();
            for (int i = AdCampaigns.Count - 1; i >= 0; i--)
                if (platform == null || AdCampaigns[i].Platform == platform) l.Add(AdCampaigns[i]);
            return l;
        }

        public int ActiveAdCount(string platform) => ActiveAds(platform).Count;

        public AdCampaign FindAd(int id)
        {
            foreach (var c in AdCampaigns)
                if (c.Id == id) return c;
            return null;
        }

        /// <summary>Passung Produkt ↔ Zielgruppe (0,45 … 1).</summary>
        public static float AdTargetFit(string pid, int target)
        {
            if (target < 0 || target >= GameData.AdTargets.Length) return 0.45f;
            var t = GameData.AdTargets[target];
            if (t.Flat > 0f) return t.Flat;
            foreach (var p in t.Products)
                if (p == pid) return 1f;
            return 0.45f;
        }

        private static AdSlogan SloganAt(int i) => GameData.AdSlogans[Mathx.Clamp(i, 0, GameData.AdSlogans.Length - 1)];

        /// <summary>Klickrate einer Fakebook-Anzeige (für Vorschau und Statistik).</summary>
        public float FakebookCtr(string pid, int target, int slogan)
        {
            var sl = SloganAt(slogan);
            float fit = AdTargetFit(pid, target);
            bool match = target >= 0 && target < GameData.AdTargets.Length && GameData.AdTargets[target].Id == sl.Target;
            float trend = GameData.IsProduct(pid) ? Mathx.Clamp(TrendMult(pid), 0.6f, 1.6f) : 1f;
            return GameData.FakebookBaseCtr * (0.5f + 0.5f * fit) * sl.Ctr * (match ? 1.2f : 1f) * trend;
        }

        /// <summary>Nachfrage-Faktor, den eine Fakebook-Kampagne bringen würde.</summary>
        public float FakebookMult(string pid, int target, int slogan, int budget)
        {
            var sl = SloganAt(slogan);
            float fit = AdTargetFit(pid, target);
            bool match = target >= 0 && target < GameData.AdTargets.Length && GameData.AdTargets[target].Id == sl.Target;
            float strength = GameData.FakebookPower * (float)Math.Sqrt(Math.Max(0, budget) / 50f) * fit * sl.Conv * (match ? 1.1f : 1f);
            return Mathx.Clamp(1f + strength, 1f, GameData.FakebookMaxMult);
        }

        /// <summary>Anzeigenplatz (1–4) für ein Gebot in Cent.</summary>
        public static int GugelPosition(string pid, int keyword, int bidCents)
        {
            int pos = 1;
            foreach (int b in GameData.GugelRivalBids(pid, keyword))
                if (b >= bidCents) pos++;
            return Math.Min(pos, GameData.GugelPosShare.Length);
        }

        /// <summary>Preis pro Klick (Cent): nächstniedrigeres Gebot + 1 Cent, höchstens das eigene Gebot.</summary>
        public static int GugelCpc(string pid, int keyword, int bidCents)
        {
            int below = GameData.GugelMinBid - 1;
            foreach (int b in GameData.GugelRivalBids(pid, keyword))
                if (b < bidCents && b > below) below = b;
            return Math.Max(1, Math.Min(bidCents, below + 1));
        }

        public static float GugelShare(string pid, int keyword, int bidCents) =>
            GameData.GugelPosShare[GugelPosition(pid, keyword, bidCents) - 1];

        /// <summary>Erwartete Klicks pro Geschäftstag.</summary>
        public static float GugelClicksPerDay(string pid, int keyword, int bidCents) =>
            GameData.GugelVolume(pid, keyword) * GugelShare(pid, keyword, bidCents);

        /// <summary>Nachfrage-Faktor einer Gugel-Anzeige.</summary>
        public static float GugelMult(string pid, int keyword, int bidCents)
        {
            var kw = GameData.GugelKeywords[Mathx.Clamp(keyword, 0, GameData.GugelKeywords.Length - 1)];
            float share = GugelShare(pid, keyword, bidCents) / GameData.GugelPosShare[0];
            return 1f + GameData.GugelPower * share * kw.Intent;
        }

        public float SocialProofMult() => 1f + GameData.FakeProfileProof * FakeProfiles;

        /// <summary>Chance pro Tag, dass die Fake-Profile auffliegen.</summary>
        public float FakeProfileRisk() => Math.Min(0.6f, GameData.FakeProfileRiskEach * FakeProfiles);

        /// <summary>Bewertungsverlust, wenn die Fake-Profile jetzt auffliegen.</summary>
        public float FakeProfileRepLoss() => FakeProfiles <= 0 ? 0f : GameData.FakeProfileRepBase + GameData.FakeProfileRepEach * FakeProfiles;

        public int FakeSiteCount(string pid)
        {
            int n = 0;
            foreach (var s in FakeSites)
                if (s.Product == pid) n++;
            return n;
        }

        public float SeoMult(string pid) => 1f + GameData.FakeSiteSeo * FakeSiteCount(pid);

        /// <summary>Chance pro Tag, dass mindestens eine Fake-Seite auffliegt.</summary>
        public float FakeSiteRiskTotal() => FakeSites.Count == 0 ? 0f : 1f - (float)Math.Pow(1f - GameData.FakeSiteRisk, FakeSites.Count);

        public string CanStartAd(string platform, string pid, int budget)
        {
            if (platform == "gugel" ? !GugelUnlocked : !FakebookUnlocked)
                return "Ab Level " + (platform == "gugel" ? GameData.GugelLevel : GameData.FakebookLevel);
            if (!GameData.IsProduct(pid) || !ProductUnlocked(pid)) return "Produkt gesperrt";
            if (!IsListed(pid)) return "Produkt ist offline";
            if (ActiveAdCount(platform) >= GameData.MaxAdsPerPlatform) return "Max. " + GameData.MaxAdsPerPlatform + " Kampagnen";
            foreach (var c in ActiveAds(platform))
                if (c.Product == pid) return "Läuft schon";
            if (budget <= 0) return "Kein Budget";
            if (Money < budget) return "Zu wenig Geld";
            return "";
        }

        // =====================================================================================
        // Aktionen
        // =====================================================================================
        public AdCampaign StartFakebookAd(string pid, int target, int slogan, int budget)
        {
            string why = CanStartAd("fakebook", pid, budget);
            if (why != "")
            {
                Notify("Fakebook: " + why + ".", "bad");
                return null;
            }
            target = Mathx.Clamp(target, 0, GameData.AdTargets.Length - 1);
            slogan = Mathx.Clamp(slogan, 0, GameData.AdSlogans.Length - 1);
            var c = NewCampaign("fakebook", pid, budget);
            c.Target = target;
            c.Slogan = slogan;
            c.Mult = FakebookMult(pid, target, slogan, budget);
            c.EndsAt = c.StartedAt + GameData.AdCampaignMinutes;
            AddAwareness(0.012f * (float)Math.Sqrt(budget / 50f));
            Notify("Fakebook-Anzeige läuft: " + GameData.Product(pid).Name + " ×" + Fmt.Dec(c.Mult, 2) + ".", "good");
            AdsSyncBoosts();
            RaiseEconomyChanged();
            return c;
        }

        public AdCampaign StartGugelAd(string pid, int keyword, int bidCents, int budget)
        {
            string why = CanStartAd("gugel", pid, budget);
            if (why != "")
            {
                Notify("Gugel Ads: " + why + ".", "bad");
                return null;
            }
            keyword = Mathx.Clamp(keyword, 0, GameData.GugelKeywords.Length - 1);
            bidCents = Mathx.Clamp(bidCents, GameData.GugelMinBid, GameData.GugelMaxBid);
            var c = NewCampaign("gugel", pid, budget);
            c.Keyword = keyword;
            c.Bid = bidCents;
            c.Position = GugelPosition(pid, keyword, bidCents);
            c.Mult = GugelMult(pid, keyword, bidCents);
            c.EndsAt = c.StartedAt + GameData.AdCampaignMinutes;
            Notify("Gugel-Anzeige auf Platz " + c.Position + ": „" + GameData.FillProduct(GameData.GugelKeywords[keyword].Text, pid) + "“.", "good");
            AdsSyncBoosts();
            RaiseEconomyChanged();
            return c;
        }

        private AdCampaign NewCampaign(string platform, string pid, int budget)
        {
            var c = new AdCampaign
            {
                Id = NextAdId++, Platform = platform, Product = pid, Budget = budget, Active = true, StartedAt = BClock(), Day = Day,
            };
            AdCampaigns.Add(c);
            TrimAdHistory();
            return c;
        }

        private void TrimAdHistory()
        {
            int finished = 0;
            foreach (var c in AdCampaigns)
                if (!c.Active) finished++;
            for (int i = 0; i < AdCampaigns.Count && finished > 16;)
            {
                if (!AdCampaigns[i].Active)
                {
                    AdCampaigns.RemoveAt(i);
                    finished--;
                }
                else i++;
            }
        }

        public bool StopAdCampaign(int id)
        {
            var c = FindAd(id);
            if (c == null || !c.Active) return false;
            EndCampaign(c, "stopped");
            AdsSyncBoosts();
            RaiseEconomyChanged();
            return true;
        }

        private void EndCampaign(AdCampaign c, string reason)
        {
            c.Active = false;
            c.EndReason = reason;
            c.EndsAt = Math.Min(c.EndsAt, BClock());
            string res = Fmt.Thousands(Mathx.RoundToInt(c.Impressions)) + " Views · " + Fmt.Thousands(Mathx.RoundToInt(c.Clicks)) + " Klicks · " +
                         Mathx.RoundToInt(c.Sales) + " Verkäufe";
            string why = reason == "budget" ? " (Budget weg)" : reason == "money" ? " (Konto leer)" : reason == "stopped" ? " (gestoppt)" : "";
            Notify(c.PlatformName + "-Anzeige " + (GameData.IsProduct(c.Product) ? GameData.Product(c.Product).Short : "") + " beendet" + why + ": " + res,
                reason == "money" ? "bad" : "info");
            AdCampaignEnded?.Invoke(c);
        }

        /// <summary>Fake-Profil anlegen (Social Proof). false = zu teuer / Maximum erreicht.</summary>
        public bool CreateFakeProfile()
        {
            if (FakeProfiles >= GameData.MaxFakeProfiles)
            {
                Notify("Mehr Fake-Profile fallen auf. Sogar Fakebook merkt das.", "info");
                return false;
            }
            if (Money < GameData.FakeProfileCost)
            {
                Notify("Nicht genug Geld für eine Prepaid-SIM.", "bad");
                return false;
            }
            Spend(GameData.FakeProfileCost, "marketing");
            FakeProfiles++;
            AdsSyncBoosts();
            RaiseEconomyChanged();
            return true;
        }

        /// <summary>Alle Fake-Profile freiwillig löschen (kein Bewertungsverlust).</summary>
        public void DeleteFakeProfiles()
        {
            if (FakeProfiles <= 0) return;
            FakeProfiles = 0;
            AdsSyncBoosts();
            Notify("Fake-Profile gelöscht. Spuren verwischt.", "info");
            RaiseEconomyChanged();
        }

        public string CanCreateFakeSite(string pid)
        {
            if (!FakeSitesUnlocked) return "Ab Level " + GameData.FakeSiteLevel;
            if (!GameData.IsProduct(pid) || !ProductUnlocked(pid)) return "Produkt gesperrt";
            if (FakeSites.Count >= GameData.MaxFakeSites) return "Max. " + GameData.MaxFakeSites + " Seiten";
            if (FakeSiteCount(pid) >= GameData.MaxFakeSitesPerProduct) return "Max. " + GameData.MaxFakeSitesPerProduct + " pro Produkt";
            if (Money < GameData.FakeSiteCost) return "Zu wenig Geld";
            return "";
        }

        /// <summary>KI-Ratgeberseite für ein Produkt erzeugen (SEO-Boost).</summary>
        public FakeSite CreateFakeSite(string pid)
        {
            string why = CanCreateFakeSite(pid);
            if (why != "")
            {
                Notify("Fake-Seite: " + why + ".", "bad");
                return null;
            }
            Spend(GameData.FakeSiteCost, "marketing");
            var site = new FakeSite { Product = pid, Day = Day, Seed = NextAdId * 31 + FakeSites.Count * 7 + Day * 131 + FakeExposedTotal };
            NextAdId++;
            FakeSites.Add(site);
            AdsSyncBoosts();
            Notify("Neue KI-Seite online: " + SlopGen.Domain(site), "good");
            RaiseEconomyChanged();
            return site;
        }

        public bool DeleteFakeSite(FakeSite site)
        {
            if (site == null || !FakeSites.Remove(site)) return false;
            AdsSyncBoosts();
            RaiseEconomyChanged();
            return true;
        }

        // =====================================================================================
        // Ablauf (aus AdvanceMinutes / NewDay / SpawnOrder)
        // =====================================================================================
        private void AdsReset()
        {
            AdCampaigns.Clear();
            NextAdId = 1;
            FakeProfiles = 0;
            FakeSites.Clear();
            FakeExposedTotal = 0;
            _adBill = 0f;
        }

        private void AdsUpdate(float m)
        {
            if (AdCampaigns.Count == 0 || m <= 0f) return;
            float now = BClock();
            bool changed = false;
            foreach (var c in AdCampaigns)
            {
                if (!c.Active) continue;
                float minutes = Math.Max(0f, Math.Min(m, c.EndsAt - (now - m)));
                float left = Math.Max(0f, c.Budget - c.Spent);
                float spend;
                if (c.IsGugel)
                {
                    float searches = GameData.GugelVolume(c.Product, c.Keyword) / GameData.AdCampaignMinutes * minutes;
                    float clicks = searches * GameData.GugelPosShare[Mathx.Clamp(c.Position, 1, GameData.GugelPosShare.Length) - 1];
                    float cpc = GugelCpc(c.Product, c.Keyword, c.Bid) / 100f;
                    spend = clicks * cpc;
                    if (spend > left && spend > 0f)
                    {
                        float k = left / spend;
                        clicks *= k;
                        searches *= k;
                        spend = left;
                    }
                    c.Impressions += searches;
                    c.Clicks += clicks;
                }
                else
                {
                    spend = Math.Min(left, c.Budget / GameData.AdCampaignMinutes * minutes);
                    float impr = spend * GameData.ImpressionsPerEuro * (0.8f + 0.4f * AdTargetFit(c.Product, c.Target));
                    c.Impressions += impr;
                    c.Clicks += impr * FakebookCtr(c.Product, c.Target, c.Slogan);
                }
                c.Spent += spend;
                _adBill += spend;
                if (c.IsGugel && c.Spent >= c.Budget - 0.01f)
                {
                    EndCampaign(c, "budget");
                    changed = true;
                }
                else if (now >= c.EndsAt - 0.001f)
                {
                    EndCampaign(c, "done");
                    changed = true;
                }
            }
            if (_adBill >= 1f)
            {
                int euros = (int)_adBill;
                _adBill -= euros;
                Spend(euros, "marketing");
            }
            if (Money < 0)
            {
                foreach (var c in AdCampaigns)
                    if (c.Active)
                    {
                        EndCampaign(c, "money");
                        changed = true;
                    }
            }
            if (changed)
            {
                AdsSyncBoosts();
                RaiseEconomyChanged();
            }
        }

        /// <summary>Tageswechsel: Fakes können auffliegen; Dauer-Boosts für den neuen Tag erneuern.</summary>
        private void AdsNewDay()
        {
            bool changed = false;
            if (FakeProfiles > 0 && AdsRng.Value() < FakeProfileRisk())
            {
                float loss = FakeProfileRepLoss();
                int n = FakeProfiles;
                FakeProfiles = 0;
                FakeExposedTotal++;
                ChangeReputation(-loss);
                Notify("Aufgeflogen! Fakebook löscht deine " + n + " Fake-Profile. Bewertung −" + Fmt.Dec(loss, 2) + ".", "bad");
                FakeExposed?.Invoke("profiles", "");
                changed = true;
            }
            for (int i = FakeSites.Count - 1; i >= 0; i--)
            {
                if (AdsRng.Value() >= GameData.FakeSiteRisk) continue;
                var site = FakeSites[i];
                FakeSites.RemoveAt(i);
                FakeExposedTotal++;
                ChangeReputation(-GameData.FakeSiteRep);
                Notify("Verbraucherschutz entlarvt " + SlopGen.Domain(site) + ". Bewertung −" + Fmt.Dec(GameData.FakeSiteRep, 2) + ".", "bad");
                FakeExposed?.Invoke("site", site.Product);
                changed = true;
            }
            AdsSyncBoosts();
            if (changed) RaiseEconomyChanged();
        }

        /// <summary>Neue Bestellung: anteilig den laufenden Kampagnen des Produkts zuschreiben.</summary>
        private void AdsOnOrder(Order order)
        {
            if (order == null || AdCampaigns.Count == 0) return;
            float total = Math.Max(1f, BoostMult(order.Product));
            foreach (var c in AdCampaigns)
            {
                if (!c.Active || c.Product != order.Product) continue;
                float share = Mathx.Clamp01((c.Mult - 1f) / total);
                c.Sales += share;
                c.Revenue += order.Price * share;
            }
        }

        /// <summary>Boosts passend zum Werbe-Zustand neu setzen (laufende Kampagnen, Social Proof, SEO).</summary>
        public void AdsSyncBoosts()
        {
            Boosts.RemoveAll(b => b.Source != null && (b.Source.StartsWith("ad:", StringComparison.Ordinal) || b.Source == "proof" ||
                                                       b.Source.StartsWith("seo:", StringComparison.Ordinal)));
            foreach (var c in AdCampaigns)
            {
                if (!c.Active) continue;
                Boosts.Add(new Boost { Source = "ad:" + c.Id, Name = c.PlatformName + "-Ad", Mult = c.Mult, EndsAt = c.EndsAt, Product = c.Product });
            }
            // Dauer-Boosts bis kurz nach Feierabend; AdsNewDay erneuert sie, bevor sie auslaufen.
            float dayEnd = Day * GameData.BusinessMinutes + 60f;
            if (FakeProfiles > 0)
                Boosts.Add(new Boost { Source = "proof", Name = "Social Proof", Mult = SocialProofMult(), EndsAt = dayEnd, Product = "" });
            var done = new HashSet<string>();
            foreach (var s in FakeSites)
            {
                if (!done.Add(s.Product) || !GameData.IsProduct(s.Product)) continue;
                Boosts.Add(new Boost { Source = "seo:" + s.Product, Name = "SEO", Mult = SeoMult(s.Product), EndsAt = dayEnd, Product = s.Product });
            }
        }

        // =====================================================================================
        // Speichern / Admin
        // =====================================================================================
        private Dictionary<string, object> AdsToJson()
        {
            var list = new List<object>();
            foreach (var c in AdCampaigns) list.Add(c.ToJson());
            var sites = new List<object>();
            foreach (var s in FakeSites) sites.Add(s.ToJson());
            return new Dictionary<string, object>
            {
                { "campaigns", list }, { "next_id", NextAdId }, { "profiles", FakeProfiles }, { "sites", sites },
                { "exposed", FakeExposedTotal }, { "bill", (double)_adBill },
            };
        }

        private void AdsFromJson(Dictionary<string, object> d)
        {
            AdsReset();
            if (d != null && d.Count > 0)
            {
                foreach (var o in J.A(d, "campaigns"))
                {
                    var c = AdCampaign.FromJson(o);
                    if (GameData.IsProduct(c.Product)) AdCampaigns.Add(c);
                }
                NextAdId = Math.Max(1, J.I(d, "next_id", 1));
                foreach (var c in AdCampaigns) NextAdId = Math.Max(NextAdId, c.Id + 1);
                FakeProfiles = Mathx.Clamp(J.I(d, "profiles"), 0, GameData.MaxFakeProfiles);
                foreach (var o in J.A(d, "sites"))
                {
                    var s = FakeSite.FromJson(o);
                    if (GameData.IsProduct(s.Product)) FakeSites.Add(s);
                }
                FakeExposedTotal = J.I(d, "exposed");
                _adBill = Mathx.Clamp(J.F(d, "bill"), 0f, 1f);
            }
            AdsSyncBoosts();
        }

        /// <summary>Admin: Entlarvung jetzt erzwingen (alle Fake-Profile und eine Fake-Seite).</summary>
        public void AdminExposeFakes()
        {
            if (FakeProfiles > 0)
            {
                float loss = FakeProfileRepLoss();
                FakeProfiles = 0;
                FakeExposedTotal++;
                ChangeReputation(-loss);
                Notify("ADMIN: Fake-Profile aufgeflogen (−" + Fmt.Dec(loss, 2) + ").", "bad");
                FakeExposed?.Invoke("profiles", "");
            }
            if (FakeSites.Count > 0)
            {
                var site = FakeSites[FakeSites.Count - 1];
                FakeSites.RemoveAt(FakeSites.Count - 1);
                FakeExposedTotal++;
                ChangeReputation(-GameData.FakeSiteRep);
                Notify("ADMIN: " + SlopGen.Domain(site) + " entlarvt.", "bad");
                FakeExposed?.Invoke("site", site.Product);
            }
            AdsSyncBoosts();
            RaiseEconomyChanged();
        }

        /// <summary>Admin: alle laufenden Kampagnen sofort beenden.</summary>
        public void AdminFinishAds()
        {
            foreach (var c in AdCampaigns)
                if (c.Active) EndCampaign(c, "done");
            AdsSyncBoosts();
            RaiseEconomyChanged();
        }
    }
}
