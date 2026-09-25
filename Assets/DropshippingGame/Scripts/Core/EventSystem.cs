using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    public sealed class Mail
    {
        public sealed class ChoiceInfo
        {
            public string Label, Minigame;
            public int Cost;
        }

        public int Id, Day;
        public float Time;
        public string EventId = "", Title, Sender, Icon, Text, Result = "", Chosen = "";
        public bool Read, Pending;
        public List<ChoiceInfo> Choices = new List<ChoiceInfo>();

        // ---- v3.0: Kontext des Ereignisses (für Platzhalter und Wirkungen) --------------------------
        /// <summary>Produkt, um das es geht ({product}).</summary>
        public string CtxProduct = "";
        /// <summary>Firma ({company}).</summary>
        public string CtxCompany = "";
        /// <summary>Betroffener Großauftrag (Eilauftrag) oder 0.</summary>
        public int CtxContract;
        /// <summary>Zahl für {n} (z. B. Pakete bei Kalles Wette).</summary>
        public int CtxNumber;

        public Dictionary<string, object> ToJson()
        {
            var ch = new List<object>();
            foreach (var c in Choices)
                ch.Add(new Dictionary<string, object> { { "label", c.Label }, { "cost", c.Cost }, { "minigame", c.Minigame ?? "" } });
            return new Dictionary<string, object>
            {
                { "id", Id }, { "event", EventId }, { "title", Title }, { "sender", Sender }, { "icon", Icon }, { "text", Text },
                { "day", Day }, { "time", (double)Time }, { "read", Read }, { "pending", Pending }, { "result", Result },
                { "chosen", Chosen }, { "choices", ch }, { "ctx_product", CtxProduct ?? "" }, { "ctx_company", CtxCompany ?? "" },
                { "ctx_contract", CtxContract }, { "ctx_number", CtxNumber },
            };
        }

        public static Mail FromJson(object o)
        {
            var d = J.Obj(o);
            var m = new Mail
            {
                Id = J.I(d, "id"), EventId = J.S(d, "event"), Title = J.S(d, "title"), Sender = J.S(d, "sender"),
                Icon = J.S(d, "icon", "mail"), Text = J.S(d, "text"), Day = J.I(d, "day", 1), Time = J.F(d, "time"),
                Read = J.B(d, "read"), Pending = J.B(d, "pending"), Result = J.S(d, "result"), Chosen = J.S(d, "chosen"),
                CtxProduct = J.S(d, "ctx_product", ""), CtxCompany = J.S(d, "ctx_company", ""), CtxContract = J.I(d, "ctx_contract"),
                CtxNumber = J.I(d, "ctx_number"),
            };
            foreach (var c in J.A(d, "choices"))
            {
                var cd = J.Obj(c);
                string mg = J.S(cd, "minigame", "");
                m.Choices.Add(new ChoiceInfo { Label = J.S(cd, "label"), Cost = J.I(cd, "cost"), Minigame = mg == "" ? null : mg });
            }
            return m;
        }
    }

    /// <summary>
    /// Ereignisse &amp; Postfach. Jeden Geschäftstag werden 0-2 zufällige Ereignisse eingeplant.
    /// Automatische Ereignisse wirken sofort, Entscheidungs-Ereignisse öffnen ein Popup
    /// (oder warten im Postfach bis Feierabend, dann gilt die Standardwahl).
    /// </summary>
    public sealed class EventSystem
    {
        public sealed class Scheduled
        {
            public string Id;
            public float At;
        }

        private readonly Sim _sim;
        public List<Mail> Mails = new List<Mail>();
        public List<Scheduled> Schedule = new List<Scheduled>();
        public int NextId = 1;
        public Dictionary<string, int> LastSeen = new Dictionary<string, int>();

        public event Action<Mail> MailReceived;
        public event Action<Mail> ChoiceEvent;
        public event Action MailChanged;
        /// <summary>Eine Entscheidung verlangt ein Minispiel: (Mail, Minispiel-Name)</summary>
        public event Action<Mail, string> MinigameRequested;

        public EventSystem(Sim sim)
        {
            _sim = sim;
        }

        private Rng Rng => _sim.Rng;

        public void Reset()
        {
            Mails.Clear();
            Schedule.Clear();
            NextId = 1;
            LastSeen.Clear();
        }

        public void Process()
        {
            if (Schedule.Count == 0) return;
            float t = _sim.TimeMinutes;
            for (int i = Schedule.Count - 1; i >= 0; i--)
            {
                if (i >= Schedule.Count) continue;
                if (t >= Schedule[i].At)
                {
                    string id = Schedule[i].Id;
                    Schedule.RemoveAt(i);
                    Trigger(id);
                }
            }
        }

        public void NewDay()
        {
            Schedule.Clear();
            if (_sim.TutorialStep >= 0 || _sim.Day < 2) return;
            int count = 1;
            float r = Rng.Value();
            if (r < 0.15f) count = 0;
            else if (r > 0.7f) count = 2;
            var pool = new List<EventDef>();
            foreach (var ev in EventData.All)
                if (Eligible(ev)) pool.Add(ev);
            for (int n = 0; n < count && pool.Count > 0; n++)
            {
                int idx = WeightedIndex(pool);
                var ev = pool[idx];
                pool.RemoveAt(idx);
                Schedule.Add(new Scheduled { Id = ev.Id, At = Rng.Range(570f, 1080f) });
            }
        }

        public bool Eligible(EventDef ev)
        {
            var s = _sim;
            if (s.Level < ev.MinLevel) return false;
            if (s.Day < ev.MinDay) return false;
            if (ev.MinMoney.HasValue && s.Money < ev.MinMoney.Value) return false;
            if (ev.MaxMoney.HasValue && s.Money > ev.MaxMoney.Value) return false;
            if (ev.Stage.HasValue && s.LocationStage != ev.Stage.Value) return false;
            if (LastSeen.TryGetValue(ev.Id, out int seen) && s.Day - seen < ev.Cooldown) return false;
            switch (ev.Needs)
            {
                case "deliveries": return s.TravelingDeliveries.Count > 0;
                case "stock": return s.StockTotal() > 10;
                case "listed": return ListedProducts().Count > 0;
                case "shipped15": return s.TotalShipped >= 15;
                case "returns": return s.TotalReturns >= 1;
                case "contract_rushable": return RushableContract() != null;
                case "early_week": return s.Weekday <= 3;
            }
            return true;
        }

        /// <summary>Laufender Großauftrag, dessen Frist sich noch um einen Tag verkürzen lässt.</summary>
        public Contract RushableContract()
        {
            foreach (var c in _sim.ActiveContracts())
                if (c.DeadlineDay - _sim.Day >= 1 && c.Remaining > 0) return c;
            return null;
        }

        private int WeightedIndex(List<EventDef> pool)
        {
            float total = 0f;
            foreach (var e in pool) total += e.Weight;
            float r = Rng.Value() * total;
            for (int i = 0; i < pool.Count; i++)
            {
                r -= pool[i].Weight;
                if (r <= 0f) return i;
            }
            return pool.Count - 1;
        }

        public List<string> ListedProducts()
        {
            var outList = new List<string>();
            foreach (var p in GameData.Products)
                if (_sim.IsListed(p.Id) && _sim.ProductAvailable(p.Id)) outList.Add(p.Id);
            return outList;
        }

        /// <summary>Löst ein Ereignis sofort aus (auch für Tests/Debug nutzbar).</summary>
        public Mail Trigger(string id)
        {
            var ev = EventData.Find(id);
            if (ev == null) return null;
            LastSeen[id] = _sim.Day;
            var mail = new Mail
            {
                Id = NextId++, EventId = id, Icon = ev.Icon ?? "mail", Day = _sim.Day, Time = _sim.TimeMinutes, Pending = ev.HasChoices,
            };
            BuildContext(ev, mail);
            mail.Title = Fill(ev.Title, mail);
            mail.Sender = Fill(ev.Sender, mail);
            mail.Text = Fill(ev.Text, mail);
            if (ev.HasChoices)
            {
                foreach (var c in ev.Choices)
                    mail.Choices.Add(new Mail.ChoiceInfo { Label = c.Label, Cost = c.Cost, Minigame = c.Minigame });
            }
            else
            {
                mail.Result = string.Join("\n", Apply(ev.Effects, mail));
            }
            Mails.Insert(0, mail);
            if (Mails.Count > 40) Mails.RemoveAt(Mails.Count - 1);
            _sim.Sound("notify");
            _sim.Notify(mail.Title, "info");
            MailReceived?.Invoke(mail);
            MailChanged?.Invoke();
            if (mail.Pending) ChoiceEvent?.Invoke(mail);
            _sim.RaiseEconomyChanged();
            return mail;
        }

        public void AddMail(string sender, string title, string text, string icon = "chat")
        {
            var mail = new Mail
            {
                Id = NextId++, Title = title, Sender = sender, Icon = icon, Text = text, Day = _sim.Day, Time = _sim.TimeMinutes,
            };
            Mails.Insert(0, mail);
            if (Mails.Count > 40) Mails.RemoveAt(Mails.Count - 1);
            MailReceived?.Invoke(mail);
            MailChanged?.Invoke();
        }

        public Mail Find(int mailId)
        {
            foreach (var m in Mails)
                if (m.Id == mailId) return m;
            return null;
        }

        public void MarkRead(int mailId)
        {
            var m = Find(mailId);
            if (m != null && !m.Read)
            {
                m.Read = true;
                MailChanged?.Invoke();
            }
        }

        public int UnreadCount()
        {
            int n = 0;
            foreach (var m in Mails)
                if (!m.Read || m.Pending) n++;
            return n;
        }

        public int PendingCount()
        {
            int n = 0;
            foreach (var m in Mails)
                if (m.Pending) n++;
            return n;
        }

        /// <summary>
        /// Entscheidung treffen. Gibt den Ergebnistext zurück ("" wenn nicht möglich oder wenn
        /// die Wahl ein Minispiel startet - dann kommt <see cref="MinigameRequested"/>).
        /// </summary>
        public string Choose(int mailId, int choiceIndex)
        {
            var m = Find(mailId);
            if (m == null || !m.Pending) return "";
            var ev = EventData.Find(m.EventId);
            if (ev == null || !ev.HasChoices || choiceIndex < 0 || choiceIndex >= ev.Choices.Length) return "";
            var choice = ev.Choices[choiceIndex];
            if (choice.Cost > 0 && _sim.Money < choice.Cost)
            {
                _sim.Notify("Dafür fehlt dir das Geld.", "bad");
                _sim.Sound("error");
                return "";
            }
            if (!string.IsNullOrEmpty(choice.Minigame))
            {
                MinigameRequested?.Invoke(m, choice.Minigame);
                return "";
            }
            if (choice.Cost > 0) _sim.AdjustMoney(-choice.Cost, "other");
            var outcomes = choice.Outcomes;
            var probs = Probabilities(outcomes);
            float r = Rng.Value();
            Outcome picked = outcomes[outcomes.Length - 1];
            for (int i = 0; i < outcomes.Length; i++)
            {
                r -= probs[i];
                if (r <= 0f)
                {
                    picked = outcomes[i];
                    break;
                }
            }
            var lines = new List<string> { Fill(picked.Text, m) };
            lines.AddRange(Apply(picked.Effects, m));
            m.Pending = false;
            m.Read = true;
            m.Chosen = choice.Label;
            m.Result = string.Join("\n", lines);
            MailChanged?.Invoke();
            _sim.RaiseEconomyChanged();
            return m.Result;
        }

        /// <summary>Schließt eine Minispiel-Entscheidung mit dem erzielten Ergebnis ab.</summary>
        public string ResolveMinigame(int mailId, string label, string text, Effects effects)
        {
            var m = Find(mailId);
            if (m == null || !m.Pending) return "";
            var lines = new List<string> { text };
            lines.AddRange(Apply(effects, m));
            m.Pending = false;
            m.Read = true;
            m.Chosen = label;
            m.Result = string.Join("\n", lines);
            MailChanged?.Invoke();
            _sim.RaiseEconomyChanged();
            return m.Result;
        }

        public float[] Probabilities(Outcome[] outcomes)
        {
            var s = _sim;
            var probs = new float[outcomes.Length];
            float fixedSum = 0f;
            for (int i = 0; i < outcomes.Length; i++)
            {
                float p = outcomes[i].P;
                float v;
                if (Math.Abs(p - Outcome.ByReputation) < 0.001f) v = Mathx.Clamp(0.15f + s.Reputation * 0.12f, 0.15f, 0.85f);
                else if (Math.Abs(p - Outcome.ByLevel) < 0.001f) v = Mathx.Clamp(0.1f + s.Level * 0.09f, 0.15f, 0.9f);
                else if (p < 0f) v = -1f;
                else v = p;
                probs[i] = v;
                if (v >= 0f) fixedSum += v;
            }
            for (int i = 0; i < probs.Length; i++)
                if (probs[i] < 0f) probs[i] = Math.Max(0f, 1f - fixedSum);
            return probs;
        }

        public void ResolvePendingChoices()
        {
            foreach (var m in Mails.ToArray())
            {
                if (!m.Pending) continue;
                var ev = EventData.Find(m.EventId);
                if (ev == null || !ev.HasChoices)
                {
                    m.Pending = false;
                    m.Result = "Keine Entscheidung getroffen.";
                    continue;
                }
                int def = ev.DefaultChoice;
                string result = string.IsNullOrEmpty(ev.Choices[def].Minigame) ? Choose(m.Id, def) : "";
                if (result == "")
                {
                    m.Pending = false;
                    m.Result = "Keine Entscheidung getroffen.";
                }
            }
            MailChanged?.Invoke();
        }

        // ---- Wirkungen ----------------------------------------------------------------------------
        public List<string> Apply(Effects e) => Apply(e, null);

        /// <summary>Wendet Wirkungen an. ctx (Mail) liefert Produkt/Firma/Auftrag aus dem Ereignis-Kontext.</summary>
        public List<string> Apply(Effects e, Mail ctx)
        {
            var s = _sim;
            var outLines = new List<string>();
            if (e == null) return outLines;
            if (e.Money.HasValue)
            {
                int v = e.Money.Value;
                s.AdjustMoney(v, v > 0 ? "income_other" : "other");
                outLines.Add((v > 0 ? "+" : "") + Fmt.Money(v));
            }
            if (e.MoneyPct.HasValue)
            {
                int amount = Mathx.RoundToInt(Math.Max(s.Money, 0) * e.MoneyPct.Value);
                s.AdjustMoney(amount, "other");
                outLines.Add(Fmt.Money(amount));
            }
            if (e.Rep.HasValue)
            {
                s.ChangeReputation(e.Rep.Value);
                outLines.Add("Bewertung " + (e.Rep.Value >= 0 ? "+" : "−") + Fmt.Dec(Math.Abs(e.Rep.Value), 1) + " Sterne");
            }
            if (e.Awareness.HasValue)
            {
                s.AddAwareness(e.Awareness.Value);
                outLines.Add("Bekanntheit steigt");
            }
            if (e.Boost != null)
            {
                string pid = e.Boost.Product ?? "";
                if (pid == "random_listed")
                {
                    pid = ctx != null && GameData.IsProduct(ctx.CtxProduct) && s.IsListed(ctx.CtxProduct) ? ctx.CtxProduct : "";
                    if (pid == "")
                    {
                        var lp = ListedProducts();
                        pid = lp.Count > 0 ? lp[Rng.Index(lp.Count)] : "";
                    }
                }
                s.AddBoost("event", e.Boost.Name, e.Boost.Mult, e.Boost.Minutes, pid);
                outLines.Add("×" + Fmt.Dec(e.Boost.Mult, 1) + " Nachfrage für " + (int)e.Boost.Minutes + " min" +
                             (pid != "" ? " (" + GameData.Product(pid).Name + ")" : ""));
            }
            if (e.BlockSupplier.HasValue)
            {
                int idx = e.BlockSupplier.Value;
                s.BlockedSuppliers[idx] = s.Day + e.BlockDays - 1;
                outLines.Add(GameData.Suppliers[idx].Name + " für " + e.BlockDays + " Tage gesperrt");
            }
            if (e.DelayDelivery.HasValue && s.TravelingDeliveries.Count > 0)
            {
                var d = s.TravelingDeliveries[0];
                d.ArriveAt += e.DelayDelivery.Value;
                d.VanSent = false;
                outLines.Add("Lieferung +" + (int)e.DelayDelivery.Value + " min verspätet");
            }
            if (e.DamageNext.HasValue)
            {
                s.DamagedNextCrate = e.DamageNext.Value;
                outLines.Add("Nächste Lieferung beschädigt");
            }
            if (e.PriceWarMult.HasValue)
            {
                var lp = ListedProducts();
                if (lp.Count > 0)
                {
                    string target = lp[Rng.Index(lp.Count)];
                    s.Market.ApplyPriceWar(target, e.PriceWarMult.Value, e.PriceWarDays);
                    outLines.Add("Preiskampf bei " + GameData.Product(target).Name);
                }
            }
            if (e.Offline.HasValue)
            {
                s.ShopOfflineUntil = s.BClock() + e.Offline.Value;
                outLines.Add("Shop " + (int)e.Offline.Value + " min offline");
            }
            if (e.TimeSkip.HasValue)
            {
                s.AdvanceMinutes(e.TimeSkip.Value);
                outLines.Add((int)(e.TimeSkip.Value / 60f) + " Stunden vergangen");
            }
            if (e.SellStockUnits.HasValue)
            {
                int sold = 0, earned = 0, units = e.SellStockUnits.Value;
                foreach (var p in GameData.Products)
                {
                    while (units > 0 && s.StockQty(p.Id) > 0)
                    {
                        s.Stock[p.Id].Qty -= 1;
                        units--;
                        sold++;
                        earned += Mathx.RoundToInt(p.RefPrice * e.SellStockPriceMult);
                    }
                }
                if (sold > 0)
                {
                    s.AdjustMoney(earned, "income_other");
                    s.TotalEarned += earned;
                }
                outLines.Add(sold + " Artikel verkauft: +" + Fmt.Money(earned));
            }
            if (e.StockLoss.HasValue)
            {
                string best = "";
                foreach (var p in GameData.Products)
                    if (best == "" || s.StockQty(p.Id) > s.StockQty(best)) best = p.Id;
                int lost = Mathx.RoundToInt(s.StockQty(best) * e.StockLoss.Value);
                s.Stock[best].Qty -= lost;
                outLines.Add(lost + "× " + GameData.Product(best).Name + " verloren");
            }
            if (e.TikTok.HasValue)
            {
                s.TriggerTikTok(e.TikTok.Value, true);
                outLines.Add("Gratis-TikTok-Boost");
            }
            if (!string.IsNullOrEmpty(e.AssetId))
            {
                s.Market.Shock(e.AssetId, e.AssetMult);
                outLines.Add(Market.Asset(e.AssetId).Name + " " + Fmt.Pct(e.AssetMult - 1f));
            }
            ApplyV3(e, ctx, outLines);
            s.RaiseEconomyChanged();
            return outLines;
        }

        /// <summary>Wirkungen der v3.0-Systeme (Trends, Retouren, Großaufträge, Express, Skills, Wochenziele).</summary>
        private void ApplyV3(Effects e, Mail ctx, List<string> outLines)
        {
            var s = _sim;
            if (e.Hype != null)
            {
                string pid = e.Hype.Product ?? "";
                if (pid == "" || pid == "random_listed") pid = ctx != null ? ctx.CtxProduct ?? "" : "";
                if (!GameData.IsProduct(pid))
                {
                    var lp = ListedProducts();
                    pid = lp.Count > 0 ? lp[Rng.Index(lp.Count)] : "";
                }
                if (pid != "")
                {
                    s.Trends.Trigger(pid, e.Hype.Phase, e.Hype.Peak);
                    string name = GameData.Product(pid).Name;
                    switch (e.Hype.Phase)
                    {
                        case TrendPhase.Rising: outLines.Add("Hype startet: " + name); break;
                        case TrendPhase.Peak: outLines.Add("Hype auf dem Höhepunkt: " + name); break;
                        case TrendPhase.Falling: outLines.Add("Hype flaut ab: " + name); break;
                        case TrendPhase.Dead: outLines.Add("Hype tot: " + name); break;
                        default: outLines.Add(name + " läuft wieder normal"); break;
                    }
                }
            }
            if (e.ReturnWave.HasValue && e.ReturnWave.Value > 0)
            {
                int n = s.TriggerReturnWave(e.ReturnWave.Value);
                outLines.Add(n == 1 ? "1 Retoure im Anmarsch" : n + " Retouren im Anmarsch");
            }
            if (!string.IsNullOrEmpty(e.Contract))
            {
                string pid = ctx != null && GameData.IsProduct(ctx.CtxProduct) && s.ProductAvailable(ctx.CtxProduct) ? ctx.CtxProduct : "";
                string company = ctx != null ? ctx.CtxCompany ?? "" : "";
                for (int i = 0; i < Math.Max(1, e.ContractCount); i++)
                {
                    var c = s.GenerateContractOffer(e.ContractBonus, true, i == 0 ? pid : "", i == 0 ? company : "");
                    if (c == null) continue;
                    if (e.Contract == "accept" && s.CanAcceptContract(c, out _))
                    {
                        s.AcceptContract(c.Id);
                        outLines.Add("Großauftrag läuft: " + c.Title + " (" + Fmt.Money(c.Payment) + ")");
                    }
                    else outLines.Add("Neues Angebot in 'Aufträge': " + c.Title + " (" + Fmt.Money(c.Payment) + ")");
                }
            }
            if (e.ContractRush.HasValue)
            {
                var c = ctx != null ? s.FindContract(ctx.CtxContract) : null;
                if (c == null || c.State != ContractState.Active) c = RushableContract();
                if (c != null && s.RushContract(c, e.ContractRush.Value))
                    outLines.Add(c.Company + ": jetzt " + Fmt.Money(c.Payment) + ", Frist " + s.ContractDeadlineText(c));
            }
            if (e.ExpressBoost.HasValue)
            {
                s.AddExpressBoost(e.ExpressBoost.Value, e.ExpressBoostMinutes);
                outLines.Add("Mehr Express-Bestellungen für " + (int)e.ExpressBoostMinutes + " min");
            }
            if (e.SkillPoints.HasValue && e.SkillPoints.Value != 0)
            {
                s.AddSkillPoints(e.SkillPoints.Value);
                outLines.Add("+" + e.SkillPoints.Value + " Skillpunkt" + (e.SkillPoints.Value == 1 ? "" : "e"));
            }
            if (e.BonusChallenge)
            {
                int n = ctx != null && ctx.CtxNumber > 0 ? ctx.CtxNumber : BonusShipTarget();
                int reward = Math.Max(50, Mathx.RoundToInt((100 + 25 * s.Level) / 5f) * 5);
                s.AddBonusChallenge("ship", n, reward, 40 + 10 * s.Level, "Kalle", "Kalles Wette",
                    "Verschicke bis Sonntag noch " + n + " Pakete. Kalle zahlt " + Fmt.Money(reward) + " – und spendiert einen Döner.", "phone");
                outLines.Add("Neues Wochenziel: " + n + " Pakete bis Sonntag");
            }
        }

        // ---- Kontext & Platzhalter ------------------------------------------------------------------
        /// <summary>Paketziel für Kalles Wette: gut die Hälfte dessen, was bis Sonntag realistisch ist.</summary>
        public int BonusShipTarget()
        {
            var s = _sim;
            int n = 0, sum = 0;
            for (int i = s.History.Count - 1; i >= 0 && n < 5; i--, n++) sum += s.History[i].Shipped;
            float perDay = n > 0 ? Math.Max(4f, sum / (float)n) : 3f + s.Level * 3f;
            int target = Mathx.RoundToInt(perDay * (s.DaysLeftInWeek() + 1) * 0.55f / 5f) * 5;
            return Math.Max(5, target);
        }

        private void BuildContext(EventDef ev, Mail mail)
        {
            var s = _sim;
            if (ev.Needs == "contract_rushable")
            {
                var c = RushableContract();
                if (c != null)
                {
                    mail.CtxContract = c.Id;
                    mail.CtxProduct = c.Product;
                    mail.CtxCompany = c.Company;
                }
            }
            if (string.IsNullOrEmpty(mail.CtxProduct))
            {
                var lp = ListedProducts();
                if (lp.Count > 0) mail.CtxProduct = lp[Rng.Index(lp.Count)];
                else
                {
                    foreach (var p in GameData.Products)
                    {
                        if (!s.ProductAvailable(p.Id)) continue;
                        mail.CtxProduct = p.Id;
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(mail.CtxCompany)) mail.CtxCompany = Rng.Pick(GameData.Companies);
            mail.CtxNumber = BonusShipTarget();
        }

        /// <summary>Ersetzt {product}, {tag}, {company}, {n} und {brand} mit dem Kontext der Mail.</summary>
        public string Fill(string text, Mail ctx)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text ?? "";
            string pid = ctx != null ? ctx.CtxProduct ?? "" : "";
            bool hasProduct = GameData.IsProduct(pid);
            return text
                .Replace("{product}", hasProduct ? GameData.Product(pid).Name : "deine Ware")
                .Replace("{tag}", hasProduct ? TrendSystem.HashTag(pid) : "DropshipChallenge")
                .Replace("{company}", ctx != null && !string.IsNullOrEmpty(ctx.CtxCompany) ? ctx.CtxCompany : "Eine Firma")
                .Replace("{n}", (ctx != null ? ctx.CtxNumber : 0).ToString())
                .Replace("{brand}", _sim.BrandName ?? "");
        }

        public Dictionary<string, object> ToJson()
        {
            var mails = new List<object>();
            foreach (var m in Mails) mails.Add(m.ToJson());
            var sched = new List<object>();
            foreach (var s in Schedule) sched.Add(new Dictionary<string, object> { { "id", s.Id }, { "at", (double)s.At } });
            var seen = new Dictionary<string, object>();
            foreach (var kv in LastSeen) seen[kv.Key] = kv.Value;
            return new Dictionary<string, object> { { "mails", mails }, { "scheduled", sched }, { "next_id", NextId }, { "last_seen", seen } };
        }

        public void FromJson(Dictionary<string, object> d)
        {
            if (d == null || d.Count == 0) return;
            Mails.Clear();
            foreach (var m in J.A(d, "mails")) Mails.Add(Mail.FromJson(m));
            Schedule.Clear();
            foreach (var s in J.A(d, "scheduled"))
            {
                var sd = J.Obj(s);
                Schedule.Add(new Scheduled { Id = J.S(sd, "id"), At = J.F(sd, "at") });
            }
            NextId = J.I(d, "next_id", 1);
            LastSeen.Clear();
            foreach (var kv in J.O(d, "last_seen")) LastSeen[kv.Key] = J.I(kv.Value);
        }
    }
}
