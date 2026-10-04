using System;
using System.Collections.Generic;
using System.Text;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Spieler-HUD (v3.0):
    /// oben links Statuskarte (Geld mit Änderungs-Pop, Wochentag/Tag, Uhr, Bewertung, Level + XP)
    /// mit Ziel-Karte und aktiven Boosts darunter; oben rechts Bestellzettel mit Countdown
    /// (max. 5 sichtbar + „+N“); unten mittig die Interaktions-Pille, unten links der gehaltene
    /// Gegenstand, unten rechts dezente Tastenhinweise. Dazu Toasts, Tages-Banner und die
    /// Level-Up-Feier (eigene Ebenen, damit sie auch über dem Laptop sichtbar sind).
    /// </summary>
    public sealed class HudView
    {
        private const int MaxTickets = 4;

        private sealed class TicketUi
        {
            public Order Order;
            public VisualElement Root, Bar;
            public Label Time;
            public int Tone = -1;
        }

        private readonly VisualElement _layer, _toastLayer, _celeLayer;
        private VisualElement _left, _status, _stars, _xpBar, _dayFill, _obj, _objBar, _boosts;
        private Label _money, _pop, _day, _clock, _rating, _level, _xpText, _objEyebrow, _objTitle, _objText, _objPct, _objReward;
        private VisualElement _tickets, _ticketsRow, _miniDock, _miniTravel, _miniPacked;
        private Label _ticketsCount, _dockText, _travelText, _packedText;
        private VisualElement _crosshair, _ring, _prompt, _hands, _handsSwatch, _handsDrop, _hints, _banner;
        private Label _promptTitle, _promptText, _promptKey, _handsEyebrow, _handsText, _handsSub, _bannerEyebrow, _bannerText, _bannerSub, _fps;
        private VisualElement _toasts;
        private VisualElement _fest;
        private Label _festEyebrow, _festTitle, _festText;
        private string _festSig = "";

        private readonly List<TicketUi> _ticketUis = new List<TicketUi>();
        private readonly List<Order> _shownOrders = new List<Order>();
        private string _ticketSig = "";
        private int _lastMoney = int.MinValue;
        private float _moneyShown;
        private int _popSum;
        private float _popAt = -10f;
        private string _lastPrompt = "";
        private string _boostSig = "", _hintSig = "", _toastPlace = "";
        private float _fpsAcc;
        private int _fpsFrames;
        private float _slowAcc;
        private float _hudAlpha = 1f;
        private int _lastMore = -1;
        private int _lastMinute = -1;
        private int _lastStars = -1;

        public HudView(VisualElement layer, VisualElement toastLayer, VisualElement celebrateLayer)
        {
            _layer = layer;
            _toastLayer = toastLayer;
            _celeLayer = celebrateLayer;
            Build();
            UIX.Show(_layer, false);
            UIX.Show(_celeLayer, false);
        }

        public void SetVisible(bool on)
        {
            UIX.Show(_layer, on);
            if (on)
            {
                _lastMoney = int.MinValue;
                _ticketSig = "";
                _lastMinute = -1;
                _lastStars = -1;
                _boostSig = "";
                _hintSig = "";
                Refresh();
            }
        }

        // =====================================================================================
        // Aufbau
        // =====================================================================================
        private void Build()
        {
            // ---- Links oben: Status, Ziel, Boosts --------------------------------------------
            _left = UIX.Col(_layer, 0f, "hud-left");
            _status = UIX.Col(_left, 0f, "hud-card", "status-card");
            var mrow = UIX.Div(_status, "hud-money-row");
            _money = UIX.Text(mrow, "0 €", "hud-money");
            _pop = UIX.Text(mrow, "", "hud-pop");
            _pop.style.opacity = 0f;

            var meta = UIX.Row(_status, 6f, "hud-meta");
            UIX.Icon(meta, "calendar", 14f);
            _day = UIX.Text(meta, "Mo · Tag 1", "hud-meta-text");
            UIX.Icon(meta, "clock", 14f).style.marginLeft = 8;
            _clock = UIX.Text(meta, "08:00", "hud-clock");
            UIX.Spacer(meta);
            _stars = UIX.Row(meta, 1f);
            _rating = UIX.Text(meta, "3,0", "hud-rating");

            var lrow = UIX.Row(_status, 8f, "hud-level-row");
            var badge = UIX.Div(lrow, "level-badge");
            _level = UIX.Text(badge, "LV 1", "level-badge-text");
            _xpBar = UIX.Bar(lrow, 0f, null, 5f);
            _xpText = UIX.Text(lrow, "0 %", "hud-xp-text");
            var track = UIX.Div(_status, "day-track");
            track.style.position = Position.Absolute;
            _dayFill = UIX.Div(track, "day-fill");
            _dayFill.style.width = Length.Percent(0);

            _obj = UIX.Col(_left, 0f, "hud-card", "obj-card");
            _objEyebrow = UIX.Text(_obj, "", "obj-eyebrow");
            _objTitle = UIX.Text(_obj, "", "obj-title");
            _objText = UIX.Text(_obj, "", "obj-text");
            var foot = UIX.Row(_obj, 8f, "obj-foot");
            _objBar = UIX.Bar(foot, 0f, Theme.Accent, 5f);
            _objPct = UIX.Text(foot, "", "obj-pct");
            _objReward = UIX.Text(foot, "", "obj-reward");

            // Mini-Event (Straßenfest): Phase + Countdown, nur sichtbar wenn geplant/aktiv.
            _fest = UIX.Col(_left, 0f, "hud-card", "obj-card");
            _festEyebrow = UIX.Text(_fest, "MINI-EVENT", "obj-eyebrow");
            _festTitle = UIX.Text(_fest, "", "obj-title");
            _festText = UIX.Text(_fest, "", "obj-text");
            UIX.Show(_fest, false);

            _boosts = UIX.Col(_left, 0f, "boost-list");

            // ---- Rechts oben: Bestellzettel -----------------------------------------------------
            _tickets = UIX.Col(_layer, 0f, "tickets-panel");
            var head = UIX.Row(_tickets, 6f, "tickets-head");
            _miniDock = Mini(head, "box", out _dockText, "am Wareneingang");
            _miniTravel = Mini(head, "truck", out _travelText, "unterwegs");
            _miniPacked = Mini(head, "package", out _packedText, "versandbereit");
            UIX.Text(head, "BESTELLUNGEN", "tickets-title").style.marginLeft = 10;
            _ticketsCount = UIX.Text(head, "0/6", "tickets-count");
            _ticketsRow = UIX.Div(_tickets, "tickets-row");

            // ---- Mitte: Fadenkreuz + Interaktion -------------------------------------------------
            _crosshair = UIX.Div(_layer, "crosshair");
            _ring = UIX.Div(_crosshair, "cross-ring");
            UIX.Div(_crosshair, "cross-dot");
            _prompt = UIX.Col(_layer, 0f, "prompt");
            _promptTitle = UIX.Text(_prompt, "", "prompt-title");
            var pill = UIX.Div(_prompt, "prompt-pill");
            _promptKey = UIX.Key(pill, "E");
            _promptText = UIX.Text(pill, "", "prompt-text");
            UIX.Show(_prompt, false);

            // ---- Unten links: gehaltener Gegenstand ----------------------------------------------
            _hands = UIX.Div(_layer, "hands");
            _handsSwatch = UIX.Swatch(_hands, Theme.Muted, 38f, "dot");
            _handsSwatch.AddToClassList("hands-swatch");
            var hv = UIX.Col(_hands, 0f);
            hv.style.flexShrink = 1;
            _handsEyebrow = UIX.Text(hv, "HÄNDE FREI", "hands-eyebrow");
            _handsText = UIX.Text(hv, "Nichts in der Hand", "hands-text");
            _handsSub = UIX.Text(hv, "", "hands-sub");
            _handsDrop = UIX.Row(_hands, 6f, "hands-drop");

            // ---- Unten rechts: Tastenhinweise ---------------------------------------------------
            _hints = UIX.Div(_layer, "hints");

            // ---- Banner & FPS -------------------------------------------------------------------
            _banner = UIX.Col(_layer, 2f, "banner");
            _bannerEyebrow = UIX.Text(_banner, "", "banner-eyebrow");
            _bannerText = UIX.Text(_banner, "", "banner-text", "display");
            _bannerSub = UIX.Text(_banner, "", "banner-sub");
            _banner.style.opacity = 0f;
            _fps = UIX.Text(_layer, "", "fps");

            // ---- Toasts (eigene Ebene über Laptop und Fenstern) --------------------------------
            _toasts = UIX.Col(_toastLayer, 0f, "toasts");
            _toastLayer.pickingMode = PickingMode.Ignore;
            _toasts.pickingMode = PickingMode.Ignore;

            foreach (var el in _layer.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
            _layer.pickingMode = PickingMode.Ignore;
            if (_celeLayer != null) _celeLayer.pickingMode = PickingMode.Ignore;
            SetHeld(null);
        }

        private static VisualElement Mini(VisualElement parent, string icon, out Label text, string tooltip)
        {
            var c = UIX.Row(parent, 4f, "hud-mini");
            UIX.Icon(c, icon, 13f);
            text = UIX.Num(c, "0", true, "hud-mini-text");
            c.tooltip = tooltip;
            return c;
        }

        // =====================================================================================
        // Laufende Aktualisierung
        // =====================================================================================
        public void Tick(float dt)
        {
            PlaceToasts();
            var sim = Game.Sim;
            if (sim == null || _layer.style.display == DisplayStyle.None) return;

            // Hinter dem Laptop ausblenden (der zeigt Geld & Co. selbst), sonst wirkt es unruhig.
            float targetOpacity = sim.PcOpen ? 0f : 1f;
            if (Mathf.Abs(_hudAlpha - targetOpacity) > 0.001f)
            {
                _hudAlpha = Mathf.MoveTowards(_hudAlpha, targetOpacity, dt * 7f);
                _layer.style.opacity = _hudAlpha;
            }

            int minute = (int)sim.TimeMinutes;
            if (minute != _lastMinute)
            {
                _lastMinute = minute;
                _clock.text = Fmt.Clock(sim.TimeMinutes);
                _clock.EnableInClassList("gold", sim.LifestyleOwned.Contains("uhr"));
                _dayFill.style.width = Length.Percent(UiFmt.DayProgress(sim) * 100f);
            }

            var root = Game.Root;
            bool locked = root != null && root.InputLocked;
            UIX.Show(_crosshair, !locked);
            var player = Game.Player;
            string p = player != null && !locked ? UiFmt.Safe(player.PromptText ?? "") : "";
            UIX.Show(_prompt, p != "");
            _crosshair.EnableInClassList("active", p != "");
            UIX.Show(_ring, p != "");
            if (p != _lastPrompt)
            {
                _lastPrompt = p;
                _promptText.text = p;
                string title = "";
                try
                {
                    if (player != null && player.Focus != null) title = (player.Focus.Title ?? "").ToUpperInvariant();
                }
                catch (Exception)
                {
                    title = "";
                }
                _promptTitle.text = title;
                UIX.Show(_promptTitle, title != "");
                _promptKey.text = GameInput.KeyLabel("interact");
                if (p != "") UIX.FadeSlideIn(_prompt, 6f, 0.12f);
            }

            TickTickets(sim);
            _slowAcc += dt;
            if (_slowAcc >= 0.25f)
            {
                _slowAcc = 0f;
                RefreshBoosts(sim);
                RefreshFestival(sim);
                RefreshHints(sim, locked);
            }

            if (Settings.ShowFps)
            {
                _fpsAcc += dt;
                _fpsFrames++;
                if (_fpsAcc >= 0.5f)
                {
                    _fps.text = Mathf.RoundToInt(_fpsFrames / _fpsAcc) + " FPS";
                    _fpsAcc = 0f;
                    _fpsFrames = 0;
                }
            }
            else if (_fps.text != "") _fps.text = "";
        }

        /// <summary>Karte für das Straßenfest: Phase, Countdown, Stand-Bestand und Verkäufe.</summary>
        private void RefreshFestival(Sim sim)
        {
            if (_fest == null) return;
            bool show = sim.StoryStage == "business" && sim.Festival != FestivalPhase.None && sim.FestivalDay <= sim.Day + 1;
            string eyebrow = "", title = "", text = "";
            if (show)
            {
                switch (sim.Festival)
                {
                    case FestivalPhase.Announced:
                        eyebrow = "MINI-EVENT · ANGEKÜNDIGT";
                        title = sim.FestivalName + (sim.FestivalDay == sim.Day ? " heute" : " morgen");
                        text = sim.FestivalDay == sim.Day
                            ? "Aufbau ab " + Fmt.Clock(sim.FestivalPrepStart) + " (in " + sim.FestivalCountdown() + ")"
                            : "Aufbau ab " + Fmt.Clock(sim.FestivalPrepStart) + " · Lager auffüllen!";
                        break;
                    case FestivalPhase.Prep:
                        eyebrow = "AUFBAU · START IN " + sim.FestivalCountdown().ToUpperInvariant();
                        title = sim.FestivalName + ": Stand bestücken";
                        text = "Stand: " + sim.FestivalTotal() + "/" + GameData.FestivalCapacity + " · Festkisten am Packtisch packen";
                        break;
                    case FestivalPhase.Live:
                        eyebrow = "LÄUFT · NOCH " + sim.FestivalCountdown().ToUpperInvariant();
                        title = sim.FestivalName;
                        text = sim.FestivalStats.Sold + " verkauft · " + Fmt.Money(sim.FestivalStats.Revenue) + " · Stand: " + sim.FestivalTotal();
                        break;
                }
            }
            string sig = show + eyebrow + title + text;
            if (sig == _festSig) return;
            _festSig = sig;
            UIX.Show(_fest, show);
            _festEyebrow.text = UiFmt.Safe(eyebrow);
            _festTitle.text = UiFmt.Safe(title);
            _festText.text = UiFmt.Safe(text);
        }

        private void RefreshBoosts(Sim sim)
        {
            var sb = new StringBuilder();
            float now = sim.BClock();
            foreach (var b in sim.Boosts) sb.Append(b.Name).Append((int)Mathf.Max(0f, b.EndsAt - now)).Append(';');
            bool offline = now < sim.ShopOfflineUntil;
            sb.Append(offline);
            var chips = sim.StatusChips();
            foreach (var c in chips) sb.Append(c[0]).Append(';');
            string sig = sb.ToString();
            if (sig == _boostSig) return;
            _boostSig = sig;
            _boosts.Clear();
            foreach (var b in sim.Boosts)
            {
                int left = (int)Mathf.Max(0f, b.EndsAt - now);
                string what = string.IsNullOrEmpty(b.Product) ? "" : " (" + GameData.Product(b.Product).Short + ")";
                var c = UIX.Chip(_boosts, b.Name + what + "  ×" + Fmt.Dec(b.Mult, 1) + " · " + UiFmt.Duration(left), "bolt", Theme.Teal, "boost-chip");
                if (b.Mult < 1f) c.AddToClassList("bad");
            }
            foreach (var c in chips)
            {
                var chip = UIX.Chip(_boosts, c[0], Icons.Has(c[1]) ? c[1] : "warning", c[2] == "bad" ? Theme.Bad : Theme.Teal, "boost-chip");
                if (c[2] == "bad") chip.AddToClassList("bad");
            }
            if (offline)
                UIX.Chip(_boosts, "Shop offline · noch " + UiFmt.Duration(sim.ShopOfflineUntil - now), "warning", Theme.Bad, "boost-chip").AddToClassList("bad");
            foreach (var el in _boosts.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
        }

        private void RefreshHints(Sim sim, bool locked)
        {
            bool business = sim.StoryStage == "business";
            int unread = business ? sim.Events.UnreadCount() : 0;
            bool show = Settings.ShowHints && !locked;
            string sig = show + "|" + GameInput.UsingGamepad + "|" + unread;
            if (sig == _hintSig) return;
            _hintSig = sig;
            _hints.Clear();
            UIX.Show(_hints, show);
            if (!show) return;
            var phone = UIX.KeyHint(_hints, GameInput.KeyLabel("phone"), "Handy");
            if (unread > 0) UIX.Badge(phone, unread.ToString(), "hint-badge");
            UIX.KeyHint(_hints, GameInput.KeyLabel("pause"), "Menü");
            UIX.KeyHint(_hints, GameInput.KeyLabel("help"), "Hilfe");
            foreach (var el in _hints.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
        }

        /// <summary>Nach jeder Wirtschaftsänderung: Geld, Tag, Bewertung, Level, Zettel, Ziel.</summary>
        public void Refresh()
        {
            var sim = Game.Sim;
            if (sim == null) return;
            if (_lastMoney == int.MinValue)
            {
                _lastMoney = sim.Money;
                _moneyShown = sim.Money;
                _money.text = Fmt.Money(sim.Money);
            }
            else if (sim.Money != _lastMoney)
            {
                PopMoney(sim.Money - _lastMoney);
                AnimateMoney(_lastMoney, sim.Money);
                _lastMoney = sim.Money;
            }
            _money.EnableInClassList("neg", sim.Money < 0);

            bool business = sim.StoryStage == "business";
            _day.text = business ? UiFmt.DayShort(sim.Day) : "Kalles Imbiss";
            int starCount = Mathf.RoundToInt(sim.Reputation);
            if (starCount != _lastStars)
            {
                _lastStars = starCount;
                _stars.Clear();
                UIX.Stars(_stars, sim.Reputation, 12f);
            }
            _rating.text = Fmt.Rating(sim.Reputation);
            _level.text = "LV " + sim.Level;
            float lp = sim.LevelProgress();
            UIX.SetBar(_xpBar, lp);
            _xpText.text = sim.Level >= GameData.MaxLevel ? "MAX" : UiFmt.Percent(lp);

            RefreshTickets(sim);
            RefreshObjective(sim);
            foreach (var el in _status.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
        }

        private void AnimateMoney(int from, int to)
        {
            float start = _moneyShown;
            Anim.Run(0.35f, t =>
            {
                _moneyShown = Mathf.Lerp(start, to, t);
                _money.text = Fmt.Money(Mathf.RoundToInt(_moneyShown));
            }, () =>
            {
                _moneyShown = to;
                _money.text = Fmt.Money(to);
            }, Ease.OutCubic, true, _money);
        }

        private void PopMoney(int delta)
        {
            if (delta == 0) return;
            if (Time.unscaledTime - _popAt < 1.4f && Math.Sign(delta) == Math.Sign(_popSum)) _popSum += delta;
            else _popSum = delta;
            _popAt = Time.unscaledTime;
            _pop.text = (_popSum > 0 ? "+" : "") + Fmt.Money(_popSum);
            _pop.style.color = _popSum > 0 ? Theme.Good : Theme.Bad;
            Anim.Run(1.6f, t =>
            {
                _pop.style.opacity = t < 0.08f ? t / 0.08f : (t > 0.7f ? 1f - (t - 0.7f) / 0.3f : 1f);
                _pop.style.translate = new Translate(0, t < 0.12f ? 6f * (1f - t / 0.12f) : 0f, 0);
            }, () => _pop.style.opacity = 0f, Ease.Linear, true, _pop);
        }

        // ---- Bestellzettel ----------------------------------------------------------------------
        private void RefreshTickets(Sim sim)
        {
            bool business = sim.StoryStage == "business";
            bool diner = sim.StoryStage == "diner" && sim.IntroStep >= 1;
            UIX.Show(_tickets, business || diner);
            if (diner)
            {
                RefreshDinerTickets(sim);
                return;
            }
            if (!business) return;

            int pending = sim.PendingCount(), cap = sim.QueueCapacity();
            _ticketsCount.text = pending + "/" + cap;
            _ticketsCount.EnableInClassList("full", pending >= cap);
            _dockText.text = sim.DockCrates.Count.ToString();
            _travelText.text = sim.TravelingDeliveries.Count.ToString();
            _packedText.text = sim.PackedCount().ToString();
            UIX.Show(_miniDock, sim.DockCrates.Count > 0);
            UIX.Show(_miniTravel, sim.TravelingDeliveries.Count > 0);
            UIX.Show(_miniPacked, sim.PackedCount() > 0);

            // Sichtbare Bestellungen (dringendste zuerst, Sim.Tickets() sortiert nach Fälligkeit) – nur bei Änderung neu bauen
            var all = sim.Tickets();
            var visible = new List<Order>();
            for (int i = 0; i < all.Count && visible.Count < MaxTickets; i++) visible.Add(all[i]);
            int moreCount = Math.Max(0, all.Count - MaxTickets);
            var sb = new System.Text.StringBuilder("orders|");
            sb.Append(moreCount).Append('|');
            foreach (var o in visible) sb.Append(o.Id).Append(':').Append((int)o.Stage).Append(o.Express ? "x" : "").Append(',');
            string sig = sb.ToString();
            if (sig == _ticketSig) return;
            _ticketSig = sig;
            _lastMore = moreCount;

            var before = new HashSet<Order>(_shownOrders);
            _ticketsRow.Clear();
            _ticketUis.Clear();
            _shownOrders.Clear();
            if (visible.Count == 0)
            {
                var e = UIX.Row(_ticketsRow, 8f, "ticket-empty");
                UIX.Icon(e, "cart", 14f, Theme.Muted);
                bool anyListed = false;
                foreach (var p in GameData.Products)
                    if (sim.IsListed(p.Id)) anyListed = true;
                UIX.Text(e, anyListed ? "Warte auf Bestellungen …" : "Nichts online – Laptop » Shop", "ticket-empty-text");
            }
            foreach (var o in visible)
            {
                var ui = BuildTicket(sim, o);
                _ticketUis.Add(ui);
                _shownOrders.Add(o);
                if (!before.Contains(o)) UIX.FadeSlideIn(ui.Root, -10f, 0.18f);
            }
            int more = all.Count - visible.Count;
            if (more > 0)
            {
                var m = UIX.Col(_ticketsRow, 0f, "ticket-more");
                UIX.Text(m, "+" + more, "ticket-more-text");
                UIX.Text(m, "weitere", "ticket-more-sub");
            }
            foreach (var el in _ticketsRow.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
            TickTickets(sim);
        }

        private static readonly string[] StepNames = { "Holen", "Packen", "Label", "Versand" };
        private static readonly string[] StepIcons = { "box", "package", "tag", "truck" };

        /// <summary>Zeitampel auf dem hellen Zettel: grün / gelb / rot.</summary>
        private static Color TicketToneColor(int tone)
        {
            switch (tone)
            {
                case 0: return new Color(0.12f, 0.55f, 0.31f);
                case 1: return new Color(0.80f, 0.52f, 0.05f);
                default: return new Color(0.78f, 0.22f, 0.18f);
            }
        }

        /// <summary>
        /// Kompakter Bestellzettel: Nummer + Menge/Produkt, Fortschritt Holen → Packen → Label →
        /// Versand, nächster Schritt mit Ort, Restzeit mit Ampelfarbe und Express-Kennzeichnung.
        /// </summary>
        private TicketUi BuildTicket(Sim sim, Order o)
        {
            var v = OrderInfo.Get(sim, o);
            int step = Mathf.Clamp(Sim.OrderStepIndex(o), 0, 4);
            var t = UIX.Col(_ticketsRow, 0f, "ticket");
            if (v.Express) t.AddToClassList("express");

            // Kopf: Produktfarbe, Icon, Nummer, Express
            var band = UIX.Row(t, 4f, "ticket-band");
            var pc = v.Product.Color.ToColor();
            band.style.backgroundColor = pc;
            var onColor = Theme.OnColor(pc);
            UIX.Icon(band, v.Product.Icon, 14f, onColor);
            var num = UIX.Text(band, v.Number, "ticket-number");
            num.style.color = onColor;
            UIX.Spacer(band);
            if (v.Express)
            {
                var ex = UIX.Row(band, 2f, "ticket-express");
                UIX.Icon(ex, "bolt", 9f, Color.white);
                UIX.Text(ex, "EXPRESS", "ticket-express-text");
            }

            var body = UIX.Col(t, 0f, "ticket-body");
            UIX.Ellipsis(UIX.Text(body, "1× " + v.Product.Short, "ticket-name"));

            // Fortschritt: vier Segmente
            var steps = UIX.Row(body, 2f, "ticket-steps");
            for (int i = 0; i < StepNames.Length; i++)
            {
                var seg = UIX.Div(steps, "ticket-step");
                if (i < step) seg.AddToClassList("done");
                else if (i == step) seg.AddToClassList("now");
            }

            // Nächster Schritt + Ort
            var next = UIX.Row(body, 3f, "ticket-next");
            UIX.Icon(next, StepIcons[Mathf.Min(step, 3)], 10f, Theme.PaperInk);
            UIX.Ellipsis(UIX.Text(next, Sim.OrderNextStep(o), "ticket-next-text"));
            UIX.Ellipsis(UIX.Text(body, "» " + Sim.OrderNextPlace(o), "ticket-where"));

            int tone = sim.OrderTimeTone(o);
            var bar = UIX.Bar(body, v.Fill, TicketToneColor(tone), 4f);
            bar.AddToClassList("ticket-bar");
            var time = UIX.Num(body, v.TimeText, false, "ticket-time");
            time.style.color = TicketToneColor(tone);
            t.tooltip = v.Number + " · " + v.Product.Name + " · " + v.Customer + " · " + Fmt.Money(v.Price) +
                        " · nächster Schritt: " + Sim.OrderNextStep(o) + " (" + Sim.OrderNextPlace(o) + ")";
            return new TicketUi { Order = o, Root = t, Bar = bar, Time = time, Tone = tone };
        }

        private void TickTickets(Sim sim)
        {
            if (sim.StoryStage != "business") return;
            foreach (var ui in _ticketUis)
            {
                if (ui == null || ui.Order == null || ui.Root == null) continue;
                var v = OrderInfo.Get(sim, ui.Order);
                int tone = sim.OrderTimeTone(ui.Order);
                var col = TicketToneColor(tone);
                UIX.SetBar(ui.Bar, v.Fill, col);
                if (ui.Time.text != v.TimeText) ui.Time.text = v.TimeText;
                if (ui.Tone != tone)
                {
                    ui.Tone = tone;
                    ui.Time.style.color = col;
                }
                bool late = sim.OrderOverdue(ui.Order);
                if (late != ui.Root.ClassListContains("late")) ui.Root.EnableInClassList("late", late);
                ui.Root.style.opacity = late ? 0.78f + 0.22f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) : 1f;
            }
        }

        private void RefreshDinerTickets(Sim sim)
        {
            _ticketsCount.text = sim.IntroServed.Count + "/" + Sim.IntroTables.Length;
            _ticketsCount.EnableInClassList("full", false);
            UIX.Show(_miniDock, false);
            UIX.Show(_miniTravel, false);
            UIX.Show(_miniPacked, false);
            var held = Game.Player != null ? Game.Player.Held : null;
            var sb = new StringBuilder();
            foreach (int t in Sim.IntroTables) sb.Append(t).Append(sim.DinerTableState(t)).Append(held != null && held.Table == t ? "h" : "");
            string sig = "diner" + sb;
            if (sig == _ticketSig) return;
            _ticketSig = sig;
            _ticketsRow.Clear();
            _ticketUis.Clear();
            _shownOrders.Clear();
            string[] dishes = { "Currywurst", "Pommes rot-weiß", "Döner-Teller" };
            for (int i = 0; i < Sim.IntroTables.Length; i++)
            {
                int table = Sim.IntroTables[i];
                int state = sim.DinerTableState(table);
                bool inHand = held != null && held.Kind == ItemKind.Plate && held.Table == table;
                var t = UIX.Col(_ticketsRow, 0f, "ticket", "diner");
                if (state == 2) t.AddToClassList("done");
                var band = UIX.Div(t, "ticket-band");
                UIX.Icon(band, state == 2 ? "check" : "plate", 18f, Color.white);
                var body = UIX.Col(t, 0f, "ticket-body");
                UIX.Text(body, "Tisch " + table, "ticket-name");
                UIX.Text(body, dishes[i % dishes.Length], "ticket-time");
                var bar = UIX.Bar(body, state == 2 ? 1f : (inHand ? 0.6f : 0.25f), state == 2 ? Theme.Teal : Theme.Accent, 4f);
                bar.AddToClassList("ticket-bar");
                UIX.Text(body, state == 2 ? "serviert" : (inHand ? "in der Hand" : "wartet"), "ticket-time");
            }
            foreach (var el in _ticketsRow.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
        }

        // ---- Ziel-Karte ----------------------------------------------------------------------
        private void RefreshObjective(Sim sim)
        {
            Objective obj;
            try
            {
                obj = sim.CurrentObjective();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
            string eyebrow, title = obj.Title ?? "", reward = "";
            if (sim.StoryStage == "diner") eyebrow = "SCHICHT IM IMBISS";
            else if (sim.TutorialStep >= 0 && sim.TutorialStep < GameData.Tutorial.Length)
            {
                eyebrow = "TUTORIAL · SCHRITT " + (sim.TutorialStep + 1) + "/" + GameData.Tutorial.Length;
                title = GameData.Tutorial[sim.TutorialStep].Title;
            }
            else if (title.StartsWith("Ziel: "))
            {
                eyebrow = "NÄCHSTES ZIEL";
                title = title.Substring(6);
                var g = sim.NextGoal();
                if (g != null) reward = "+" + Fmt.Money(g.Reward);
            }
            else eyebrow = "ZIEL";
            _objEyebrow.text = eyebrow;
            _objTitle.text = title;
            // Die Bestellzettel stehen jetzt oben rechts (Core-Text sagt noch „oben links“).
            _objText.text = obj.Text ?? "";
            bool hasBar = obj.Progress >= 0f;
            UIX.Show(_objBar, hasBar);
            UIX.Show(_objPct, hasBar);
            UIX.SetBar(_objBar, Mathf.Max(0f, obj.Progress));
            _objPct.text = hasBar ? UiFmt.Percent(obj.Progress) : "";
            _objReward.text = reward;
            UIX.Show(_objReward, reward != "");
        }

        // =====================================================================================
        // Gehaltener Gegenstand
        // =====================================================================================
        public void SetHeld(ItemData data)
        {
            _handsDrop.Clear();
            if (data == null || data.Kind == ItemKind.None)
            {
                _hands.AddToClassList("empty");
                _handsEyebrow.text = "HÄNDE FREI";
                _handsText.text = "Nichts in der Hand";
                _handsSub.text = "";
                UIX.Show(_handsSub, false);
                var empty = new Color(0.3f, 0.33f, 0.4f);
                _handsSwatch.style.backgroundColor = empty;
                SetSwatchIcon("dot", empty);
                return;
            }
            _hands.RemoveFromClassList("empty");
            bool hasProduct = !string.IsNullOrEmpty(data.Product) && GameData.IsProduct(data.Product);
            var pd = hasProduct ? GameData.Product(data.Product) : null;
            string pname = pd != null ? pd.Name : "";
            Color bg = pd != null ? pd.Color.ToColor() : new Color(0.95f, 0.8f, 0.4f);
            _handsEyebrow.text = "IN DER HAND";
            string sub = "";
            string icon;
            switch (data.Kind)
            {
                case ItemKind.Crate:
                    _handsText.text = "Kiste · " + data.Quantity + "× " + pname;
                    sub = GameData.QualityName(data.Quality) + "-Qualität · ins Regal oder auf den Stand";
                    icon = "box";
                    break;
                case ItemKind.Item:
                    _handsText.text = (data.Express ? "Express · " : "") + pname;
                    if (data.ContractId > 0)
                    {
                        var c = Game.Sim?.FindContract(data.ContractId);
                        sub = "Für Großauftrag" + (c != null ? " · " + c.Company : "") + " · zum Palettenplatz";
                    }
                    else if (data.OrderId > 0)
                        sub = "für #" + data.OrderId + (string.IsNullOrEmpty(data.Customer) ? "" : " · " + data.Customer) + " · zum Packtisch";
                    else sub = "Für eine Bestellung (" + Fmt.Money(data.Price) + ") · zum Packtisch";
                    icon = pd != null ? pd.Icon : "tag";
                    break;
                case ItemKind.Package:
                    _handsText.text = (data.Express ? "Express-Paket · " : "Paket · ") + pname;
                    sub = (data.OrderId > 0 ? "#" + data.OrderId + (string.IsNullOrEmpty(data.Customer) ? "" : " · " + data.Customer) + " · " : "") + "zum Labeldrucker";
                    icon = "package";
                    break;
                case ItemKind.Labeled:
                    _handsText.text = (data.Express ? "Express · versandfertig · " : "Versandfertig · ") + pname;
                    sub = (string.IsNullOrEmpty(data.Customer) ? "" : "An: " + data.Customer + (string.IsNullOrEmpty(data.City) ? "" : ", " + data.City) + " · ") + "ab in die Versand-Box";
                    icon = "truck";
                    break;
                case ItemKind.Return:
                    _handsEyebrow.text = "RETOURE";
                    _handsText.text = "Retoure: " + pname;
                    sub = (string.IsNullOrEmpty(data.Note) ? "" : "„" + data.Note + "“ · ") + "zum Retourenplatz";
                    bg = new Color(0.78f, 0.22f, 0.18f);
                    icon = "return";
                    break;
                case ItemKind.Plate:
                    _handsText.text = "Teller für Tisch " + data.Table;
                    sub = "Bring ihn an den richtigen Tisch";
                    bg = new Color(0.89f, 0.34f, 0.23f);
                    icon = "plate";
                    break;
                default:
                    _handsText.text = data.Describe();
                    icon = "dot";
                    break;
            }
            _handsSwatch.style.backgroundColor = bg;
            SetSwatchIcon(icon, bg);
            _handsSub.text = sub;
            UIX.Show(_handsSub, sub != "");
            if (data.Kind != ItemKind.Plate)
            {
                UIX.Key(_handsDrop, GameInput.KeyLabel("drop"), "keycap-sm");
                UIX.Text(_handsDrop, "Ablegen", "keyhint-text");
            }
            foreach (var el in _hands.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
            UIX.PopIn(_hands, 0.14f);
            _ticketSig = ""; // Imbiss-Zettel zeigen „in der Hand“
            if (Game.Sim != null && Game.Sim.StoryStage == "diner") RefreshTickets(Game.Sim);
        }

        private void SetSwatchIcon(string icon, Color bg)
        {
            _handsSwatch.Clear();
            UIX.Icon(_handsSwatch, icon, 20f, Theme.OnColor(bg));
        }

        // =====================================================================================
        // Banner, Level-Up, Toasts
        // =====================================================================================
        public void ShowBanner(string title, string sub, string eyebrow = null)
        {
            _bannerEyebrow.text = eyebrow ?? "";
            UIX.Show(_bannerEyebrow, !string.IsNullOrEmpty(eyebrow));
            _bannerText.text = title;
            _bannerSub.text = sub ?? "";
            Anim.Run(2.8f, t =>
            {
                _banner.style.opacity = t < 0.07f ? t / 0.07f : (t > 0.78f ? 1f - (t - 0.78f) / 0.22f : 1f);
                float s = t < 0.07f ? Mathf.Lerp(0.94f, 1f, t / 0.07f) : 1f;
                _banner.style.scale = new Scale(new Vector3(s, s, 1f));
            }, () => _banner.style.opacity = 0f, Ease.Linear, true, _banner);
        }

        /// <summary>Level-Up-Feier: großes Abzeichen mit Glühen und den Freischaltungen.</summary>
        public void ShowLevelUp(int level)
        {
            if (_celeLayer == null) return;
            _celeLayer.Clear();
            UIX.Show(_celeLayer, true);
            var wrap = UIX.Div(_celeLayer, "layer", "celebrate");
            var glow = UIX.Div(wrap, "cele-glow");
            glow.style.backgroundImage = new StyleBackground(UiTex.Radial());
            var card = UIX.Col(wrap, 0f, "cele-card");
            UIX.Text(card, "LEVEL UP", "cele-eyebrow");
            var badge = UIX.Div(card, "cele-badge");
            UIX.Text(badge, level.ToString(), "celebrate-level");
            UIX.Text(card, "Firmenlevel " + level + " erreicht", "cele-title", "display");
            GameData.LevelUnlocks.TryGetValue(level, out string unlock);
            if (!string.IsNullOrEmpty(unlock))
            {
                UIX.Text(card, "NEU FREIGESCHALTET", "cele-sub");
                var chips = UIX.Div(card, "cele-unlocks");
                foreach (var part in unlock.Split('·'))
                {
                    string s = part.Trim();
                    if (s != "") UIX.Chip(chips, s, "sparkle", Theme.Accent, "cele-chip");
                }
            }
            UIX.Chip(card, "+1 Skillpunkt · Laptop » Firma » Skills", "sparkle", Theme.Accent, "cele-chip").style.marginTop = 10;
            foreach (var el in _celeLayer.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
            wrap.style.opacity = 0f;
            Anim.Run(4.2f, t =>
            {
                float a = t < 0.05f ? t / 0.05f : (t > 0.88f ? 1f - (t - 0.88f) / 0.12f : 1f);
                wrap.style.opacity = a;
                float bt = Mathf.Clamp01(t / 0.09f);
                float s = Mathf.LerpUnclamped(0.55f, 1f, Anim.Apply(Ease.OutBack, bt));
                badge.style.scale = new Scale(new Vector3(s, s, 1f));
                float gs = 0.9f + 0.1f * Mathf.Sin(t * 9f);
                glow.style.scale = new Scale(new Vector3(gs, gs, 1f));
            }, () =>
            {
                _celeLayer.Clear();
                UIX.Show(_celeLayer, false);
            }, Ease.Linear, true, _celeLayer);
        }

        private readonly List<VisualElement> _toastList = new List<VisualElement>();

        public void AddToast(string text, string kind)
        {
            if (string.IsNullOrEmpty(text)) return;
            string k = kind == "good" || kind == "bad" ? kind : "info";
            var t = UIX.Div(_toasts, "toast", "toast-" + k);
            string icon = k == "good" ? "check_circle" : (k == "bad" ? "warning" : "info");
            Color tint = k == "good" ? Theme.Good : (k == "bad" ? Theme.Bad : Theme.Teal);
            UIX.Icon(t, icon, 16f, tint, "toast-icon");
            UIX.Text(t, UiFmt.Safe(text), "toast-text");
            foreach (var el in t.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
            _toastList.Add(t);
            while (_toastList.Count > 4)
            {
                _toastList[0].RemoveFromHierarchy();
                _toastList.RemoveAt(0);
            }
            t.style.opacity = 0f;
            Anim.Run(5.4f, x =>
            {
                float a = x < 0.035f ? x / 0.035f : (x > 0.9f ? 1f - (x - 0.9f) / 0.1f : 1f);
                t.style.opacity = a;
                t.style.translate = new Translate(x < 0.035f ? 18f * (1f - x / 0.035f) : 0f, 0, 0);
            }, () =>
            {
                t.RemoveFromHierarchy();
                _toastList.Remove(t);
            }, Ease.Linear, true);
        }

        /// <summary>Toasts rechts unter den Zetteln – bei Laptop/Menü unten mittig, bei offenem Handy links daneben.</summary>
        private void PlaceToasts()
        {
            var sim = Game.Sim;
            var ui = Game.UI;
            string place;
            if (sim == null || !sim.InGame || sim.PcOpen || (ui != null && ui.Menu != null && ui.Menu.IsOpen)) place = "bottom";
            else if (ui != null && ui.Phone != null && ui.Phone.IsOpen) place = "phone";
            else place = "right";
            if (place == _toastPlace) return;
            _toastPlace = place;
            switch (place)
            {
                case "bottom":
                    _toasts.style.top = StyleKeyword.Auto;
                    _toasts.style.bottom = 28;
                    _toasts.style.right = StyleKeyword.Auto;
                    _toasts.style.left = Length.Percent(50);
                    _toasts.style.marginLeft = -174;
                    break;
                case "phone":
                    _toasts.style.top = 196;
                    _toasts.style.bottom = StyleKeyword.Auto;
                    _toasts.style.left = StyleKeyword.Auto;
                    _toasts.style.right = 420;
                    _toasts.style.marginLeft = 0;
                    break;
                default:
                    _toasts.style.top = 196;
                    _toasts.style.bottom = StyleKeyword.Auto;
                    _toasts.style.left = StyleKeyword.Auto;
                    _toasts.style.right = 24;
                    _toasts.style.marginLeft = 0;
                    break;
            }
        }
    }
}
