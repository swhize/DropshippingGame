using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Smartphone-Overlay (Tab / Y): fährt unten rechts ins Bild, die Welt läuft weiter.
    /// Vier Mini-Apps: Bestellungen (offene Zettel mit Countdown), Nachrichten (Postfach –
    /// Entscheidungen direkt beantworten), Nachbestellen (ein Tipp pro Produkt mit dem zuletzt
    /// genutzten Lieferanten und der letzten Menge) und Status (Konto, Bewertung, Level, Umsatz,
    /// Fixkosten heute Abend). Im Imbiss-Intro: „Kein Empfang“.
    /// Bedienung: Maus, Tastatur (1–4, Esc) oder Controller (LB/RB, Steuerkreuz, A, B).
    /// Erweiterungspunkt (Phase B): <see cref="ExtraApps"/> für weitere Handy-Apps.
    /// </summary>
    public sealed class PhoneView
    {
        /// <summary>Zusätzliche App (Phase B), z. B. „Aufträge“ oder „Trends“.</summary>
        public sealed class PhoneApp
        {
            public string Title, TabLabel, Icon;
            public Func<bool> Visible;
            public Func<string> Badge;
            /// <summary>Baut den Inhalt: (Kopf-Container, Inhalts-Container)</summary>
            public Action<PhoneView, VisualElement> Build;
        }

        /// <summary>Weitere Apps nach den vier Standard-Apps (Phase B).</summary>
        public static readonly List<PhoneApp> ExtraApps = new List<PhoneApp>();

        private static readonly string[] Titles = { "Bestellungen", "Nachrichten", "Nachbestellen", "Status" };
        private static readonly string[] TabLabels = { "Bestellungen", "Nachrichten", "Nachkauf", "Status" };
        private static readonly string[] TabIcons = { "cart", "chat", "refresh", "bars" };

        private static readonly string[] NoSignal =
        {
            "Kalle hat den Router hinter der Fritteuse versteckt. Erst die Schicht, dann das Business.",
            "Der Imbiss ist ein Funkloch. Angeblich wegen der Dunstabzugshaube.",
            "Einziges Netz hier: das Frittierfett-Sieb.",
        };

        private sealed class LiveOrder
        {
            public Order Order;
            public VisualElement Bar;
            public Label Time;
        }

        private readonly VisualElement _layer;
        private VisualElement _phone, _screen, _head, _content, _tabsBar, _signalIcon;
        private ScrollView _scroll;
        private Label _time;
        private readonly List<Button> _tabs = new List<Button>();
        private readonly List<Label> _badges = new List<Label>();
        private readonly List<int> _tabApps = new List<int>();
        private readonly List<LiveOrder> _live = new List<LiveOrder>();
        private int _app;
        private int _mailId = -1;
        private bool _dirty;
        private float _dirtyT, _liveT, _badgeT;
        private string _themeSig = "", _badgeSig = "", _tabSig = "";
        private int _animToken;
        private int _lastMinute = -1;
        private string _joke = "";

        public bool IsOpen { get; private set; }
        /// <summary>Bildschirm des Handys (für die Fokus-Steuerung).</summary>
        public VisualElement Screen => _screen;

        public PhoneView(VisualElement layer)
        {
            _layer = layer;
            _layer.pickingMode = PickingMode.Ignore;
            Build();
            UIX.Show(_layer, false);
            Settings.Changed += () =>
            {
                if (IsOpen) ApplyTheme();
            };
        }

        private int AppCount => 4 + VisibleExtraCount();

        private static int VisibleExtraCount()
        {
            int n = 0;
            foreach (var a in ExtraApps)
                if (a != null && (a.Visible == null || SafeBool(a.Visible)))
                    n++;
            return n;
        }

        private static bool SafeBool(Func<bool> f)
        {
            try
            {
                return f();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        // =====================================================================================
        // Aufbau
        // =====================================================================================
        private void Build()
        {
            _phone = UIX.Col(_layer, 0f, "smartphone", "os-theme");
            _phone.pickingMode = PickingMode.Position;
            _screen = UIX.Col(_phone, 0f, "phone-screen");
            var status = UIX.Row(_screen, 0f, "phone-status");
            _time = UIX.Num(status, "08:00", true, "phone-time");
            UIX.Spacer(status);
            _signalIcon = UIX.Icon(status, "signal", 14f);
            UIX.Icon(status, "wifi", 14f);
            UIX.Icon(status, "battery", 17f);
            var notch = UIX.Div(_screen, "phone-notch");
            notch.style.position = Position.Absolute;
            notch.pickingMode = PickingMode.Ignore;
            _head = UIX.Col(_screen, 0f, "phone-head");
            _scroll = UIX.Scroll(_screen, "phone-body");
            _content = UIX.Col(_scroll.contentContainer, 0f, "phone-content");
            _tabsBar = UIX.Row(_screen, 0f, "phone-tabs");
            UIX.Div(_screen, "phone-home").pickingMode = PickingMode.Ignore;
            ApplyTheme();
        }

        private void BuildTabs()
        {
            // Standard-Apps + sichtbare Zusatz-Apps
            var sig = new System.Text.StringBuilder();
            _tabApps.Clear();
            for (int i = 0; i < 4; i++) _tabApps.Add(i);
            for (int i = 0; i < ExtraApps.Count; i++)
            {
                var a = ExtraApps[i];
                if (a == null || (a.Visible != null && !SafeBool(a.Visible))) continue;
                _tabApps.Add(4 + i);
            }
            foreach (int a in _tabApps) sig.Append(a).Append(',');
            string s = sig.ToString();
            if (s == _tabSig && _tabs.Count > 0) return;
            _tabSig = s;
            _tabsBar.Clear();
            _tabs.Clear();
            _badges.Clear();
            foreach (int appIndex in _tabApps)
            {
                int idx = appIndex;
                string label = idx < 4 ? TabLabels[idx] : (ExtraApps[idx - 4].TabLabel ?? ExtraApps[idx - 4].Title);
                string icon = idx < 4 ? TabIcons[idx] : ExtraApps[idx - 4].Icon;
                var b = UIX.PressCol(_tabsBar, 0f, () => SelectApp(idx), "phone-tab");
                b.focusable = false;
                UIX.Icon(b, icon, 20f);
                UIX.Text(b, label, "phone-tab-label");
                var badge = UIX.Badge(b, "", "phone-tab-badge");
                badge.style.position = Position.Absolute;
                UIX.Show(badge, false);
                UIX.PassThrough(b);
                _tabs.Add(b);
                _badges.Add(badge);
            }
        }

        private void ApplyTheme()
        {
            string sig = Settings.LaptopDesign + Settings.LaptopDark;
            if (sig == _themeSig) return;
            _themeSig = sig;
            bool hype = Settings.LaptopDesign != "frachtbrief";
            _phone.EnableInClassList("hype", hype);
            _phone.EnableInClassList("fb", !hype);
            _phone.EnableInClassList("dark", Settings.LaptopDark);
            _phone.EnableInClassList("light", !Settings.LaptopDark);
        }

        // =====================================================================================
        // Öffnen / Schließen / Navigation
        // =====================================================================================
        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open(int app = -1)
        {
            if (IsOpen) return;
            IsOpen = true;
            if (app >= 0) _app = app;
            _mailId = -1;
            _joke = NoSignal[UnityEngine.Random.Range(0, NoSignal.Length)];
            ApplyTheme();
            UIX.Show(_layer, true);
            Game.Root?.Lock("phone", true);
            Rebuild(false);
            int token = ++_animToken;
            _phone.style.opacity = 1f;
            _phone.style.translate = new Translate(0, 760f, 0);
            Anim.Run(0.2f, t =>
            {
                if (token == _animToken) _phone.style.translate = new Translate(0, 760f * (1f - t), 0);
            }, null, Ease.OutCubic, true, _phone);
            Game.Sound("whoosh", 0.05f, -9f);
        }

        public void Close(bool instant = false)
        {
            if (!IsOpen) return;
            IsOpen = false;
            _mailId = -1;
            Game.Root?.Lock("phone", false);
            int token = ++_animToken;
            if (instant)
            {
                Anim.Stop(_phone);
                UIX.Show(_layer, false);
                return;
            }
            Game.Sound("whoosh", 0.05f, -12f);
            Anim.Run(0.14f, t =>
            {
                if (token == _animToken) _phone.style.translate = new Translate(0, 760f * t, 0);
            }, () =>
            {
                if (token == _animToken && !IsOpen) UIX.Show(_layer, false);
            }, Ease.InQuad, true, _phone);
        }

        /// <summary>Esc / B: aus einer Nachricht zurück zur Liste. true = verarbeitet.</summary>
        public bool Back()
        {
            if (!IsOpen) return false;
            if (_app == 1 && _mailId >= 0)
            {
                _mailId = -1;
                Game.Sound("click", 0.05f, -8f);
                Rebuild(false);
                return true;
            }
            return false;
        }

        public void SelectApp(int app)
        {
            if (app < 0) return;
            if (app >= 4 && (app - 4 >= ExtraApps.Count)) return;
            if (_app == app && _mailId < 0) return;
            _app = app;
            _mailId = -1;
            Game.Sound("click", 0.05f, -8f);
            Rebuild(false);
            UIX.FadeSlideIn(_content, 6f, 0.14f);
        }

        /// <summary>LB/RB: nächste / vorige App.</summary>
        public void CycleApp(int dir)
        {
            if (_tabApps.Count == 0) BuildTabs();
            int pos = _tabApps.IndexOf(_app);
            if (pos < 0) pos = 0;
            int n = _tabApps.Count;
            SelectApp(_tabApps[((pos + dir) % n + n) % n]);
        }

        public void MarkDirty() => _dirty = true;

        // =====================================================================================
        // Laufende Aktualisierung
        // =====================================================================================
        public void Tick(float dt)
        {
            if (!IsOpen) return;
            var sim = Game.Sim;
            if (sim == null) return;
            int minute = (int)sim.TimeMinutes;
            if (minute != _lastMinute)
            {
                _lastMinute = minute;
                _time.text = Fmt.Clock(sim.TimeMinutes);
            }
            UIX.PadScroll(_scroll, dt);
            _dirtyT += dt;
            if (_dirty && _dirtyT > 0.35f)
            {
                _dirty = false;
                _dirtyT = 0f;
                Rebuild(true);
            }
            _liveT += dt;
            if (_liveT >= 0.25f)
            {
                _liveT = 0f;
                foreach (var l in _live)
                {
                    var v = OrderInfo.Get(sim, l.Order);
                    UIX.SetBar(l.Bar, v.Fill, OrderInfo.ToneColor(v.Tone));
                    l.Time.text = v.Tone == "late" ? "überfällig " + v.TimeText : "noch " + v.TimeText;
                }
            }
            _badgeT += dt;
            if (_badgeT >= 0.5f)
            {
                _badgeT = 0f;
                UpdateBadges(sim);
            }
        }

        private void UpdateBadges(Sim sim)
        {
            bool business = sim.StoryStage == "business";
            int pending = business ? sim.PendingCount() : 0;
            int unread = business ? sim.Events.UnreadCount() : 0;
            int urgentReorder = 0;
            if (business)
                foreach (var p in GameData.Products)
                    if (sim.ProductAvailable(p.Id) && sim.IsListed(p.Id) && sim.StockQty(p.Id) + sim.TravelingCountFor(p.Id) + sim.DockCountFor(p.Id) == 0)
                        urgentReorder++;
            string sig = pending + "|" + unread + "|" + urgentReorder + "|" + _app + "|" + business;
            if (sig == _badgeSig) return;
            _badgeSig = sig;
            for (int i = 0; i < _tabs.Count && i < _tabApps.Count; i++)
            {
                int app = _tabApps[i];
                string badge = "";
                if (business)
                {
                    if (app == 0 && pending > 0) badge = pending.ToString();
                    else if (app == 1 && unread > 0) badge = unread.ToString();
                    else if (app == 2 && urgentReorder > 0) badge = "!";
                    else if (app >= 4)
                    {
                        var extra = ExtraApps[app - 4];
                        if (extra.Badge != null)
                        {
                            try
                            {
                                badge = extra.Badge() ?? "";
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                    }
                }
                _badges[i].text = badge;
                UIX.Show(_badges[i], badge != "");
                _badges[i].EnableInClassList("badge-bad", app == 2);
                _tabs[i].EnableInClassList("active", app == _app);
                _tabs[i].SetEnabled(business);
            }
            _signalIcon.style.backgroundImage = new StyleBackground(Icons.Get(business ? "signal" : "signal_off"));
        }

        /// <summary>Inhalt neu aufbauen (Fokus bleibt an gleicher Stelle, wichtig für Controller).</summary>
        public void Rebuild(bool keepScroll = true)
        {
            if (!IsOpen) return;
            var sim = Game.Sim;
            int focus = GameInput.UsingGamepad ? UIX.FocusIndex(_screen) : -1;
            var offset = _scroll.scrollOffset;
            _live.Clear();
            _head.Clear();
            _content.Clear();
            BuildTabs();
            _badgeSig = "";
            if (sim == null) return;
            _time.text = Fmt.Clock(sim.TimeMinutes);
            try
            {
                if (sim.StoryStage != "business") BuildNoSignal();
                else
                {
                    switch (_app)
                    {
                        case 0: BuildOrders(sim); break;
                        case 1: BuildMessages(sim); break;
                        case 2: BuildReorder(sim); break;
                        case 3: BuildStatus(sim); break;
                        default: BuildExtra(sim); break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _content.Clear();
                UIX.Empty(_content, "warning", "Ups.", "Diese App ist kurz abgestürzt. Einfach nochmal öffnen.");
            }
            UpdateBadges(sim);
            if (keepScroll) _scroll.schedule.Execute(() => _scroll.scrollOffset = offset);
            else _scroll.scrollOffset = Vector2.zero;
            if (focus >= 0) _scroll.schedule.Execute(() => UIX.RestoreFocus(_screen, focus));
        }

        private void Head(string title, string sub, bool back = false)
        {
            if (back)
            {
                var b = UIX.PressRow(_head, 4f, () => Back(), "phone-back");
                UIX.Icon(b, "chevron_left", 14f);
                UIX.Text(b, "Zurück", "phone-back-text");
                UIX.PassThrough(b);
            }
            if (!string.IsNullOrEmpty(title)) UIX.Text(_head, title, "phone-title");
            if (!string.IsNullOrEmpty(sub)) UIX.Text(_head, sub, "phone-sub");
        }

        private VisualElement Hint(string icon, string text)
        {
            var h = UIX.Row(_content, 0f, "phone-hint");
            UIX.Icon(h, icon, 16f);
            UIX.Text(h, text, "phone-hint-text");
            return h;
        }

        private static VisualElement Pill(VisualElement parent, string icon, string text, Color? tint = null)
        {
            var p = UIX.Row(parent, 5f, "phone-pill");
            UIX.Icon(p, icon, 13f, tint);
            UIX.Text(p, text, "phone-pill-text");
            return p;
        }

        // =====================================================================================
        // Apps
        // =====================================================================================
        private void BuildNoSignal()
        {
            Head(null, null);
            var box = UIX.Col(_content, 0f, "nosignal");
            box.style.marginTop = 70;
            var ic = UIX.Div(box, "nosignal-icon");
            UIX.Icon(ic, "signal_off", 40f);
            UIX.Text(box, "Kein Empfang", "nosignal-title", "display");
            UIX.Text(box, _joke, "nosignal-text");
            UIX.Text(box, "SOS · NUR NOTRUFE", "nosignal-sos");
        }

        private void BuildOrders(Sim sim)
        {
            int pending = sim.PendingCount(), cap = sim.QueueCapacity();
            Head("Bestellungen", pending + " offen · Platz für " + cap);
            var sum = UIX.Row(_content, 0f, "phone-summary");
            sum.style.flexWrap = Wrap.Wrap;
            Pill(sum, "box", sim.DockCrates.Count + " Eingang");
            Pill(sum, "truck", sim.TravelingDeliveries.Count + " unterwegs");
            Pill(sum, "package", sim.PackedCount() + " verpackt");
            if (pending >= cap)
            {
                var full = Hint("warning", "Warteschlange voll – neue Bestellungen gehen verloren!");
                full.style.marginBottom = 8;
            }
            if (sim.OrderQueue.Count == 0)
            {
                bool anyListed = false;
                foreach (var p in GameData.Products)
                    if (sim.IsListed(p.Id)) anyListed = true;
                UIX.Empty(_content, "cart", "Keine offenen Bestellungen",
                    anyListed ? "Neue Bestellungen erscheinen hier – und oben rechts als Zettel." : "Stell am Laptop unter „Shop“ ein Produkt online.");
                return;
            }
            int n = 0;
            foreach (var o in sim.OrderQueue)
            {
                if (++n > 40) break;
                var v = OrderInfo.Get(sim, o);
                var item = UIX.Row(_content, 10f, "phone-item");
                UIX.Round(UIX.Swatch(item, v.Product.Color.ToColor(), 38f, v.Product.Icon), 12f);
                var mid = UIX.Col(item, 1f);
                mid.style.flexGrow = 1;
                mid.style.flexShrink = 1;
                UIX.Ellipsis(UIX.Text(mid, v.Product.Name, "phone-item-title"));
                int stock = sim.StockQty(o.Product);
                var sub = UIX.Text(mid, stock > 0 ? "Im Regal: " + stock : "Nicht auf Lager!", "phone-item-sub");
                if (stock <= 0) sub.AddToClassList("bad-text");
                var bar = UIX.Bar(mid, v.Fill, OrderInfo.ToneColor(v.Tone), 4f);
                bar.style.marginTop = 5;
                var right = UIX.Col(item, 2f);
                right.style.alignItems = Align.FlexEnd;
                UIX.Num(right, Fmt.Money(v.Price), true, "phone-order-price");
                var time = UIX.Num(right, v.Tone == "late" ? "überfällig " + v.TimeText : "noch " + v.TimeText, false, "phone-order-time");
                _live.Add(new LiveOrder { Order = o, Bar = bar, Time = time });
            }
            if (sim.PackedCount() > 0)
                Hint("truck", sim.PackedCount() == 1 ? "1 Paket wartet auf Label & Versand." : sim.PackedCount() + " Pakete warten auf Label & Versand.");
        }

        private void BuildMessages(Sim sim)
        {
            var ev = sim.Events;
            var mail = _mailId >= 0 ? ev.Find(_mailId) : null;
            if (mail != null)
            {
                BuildMessage(sim, mail);
                return;
            }
            _mailId = -1;
            int pending = ev.PendingCount(), unread = ev.UnreadCount();
            Head("Nachrichten", pending > 0 ? (pending == 1 ? "1 Entscheidung offen" : pending + " Entscheidungen offen") : (unread > 0 ? unread + " ungelesen" : "Alles gelesen"));
            if (ev.Mails.Count == 0)
            {
                UIX.Empty(_content, "chat", "Keine Nachrichten", "Hier landen Angebote, Ereignisse und Post von Mama.");
                return;
            }
            var ordered = new List<Mail>();
            foreach (var m in ev.Mails)
                if (m.Pending) ordered.Add(m);
            foreach (var m in ev.Mails)
                if (!m.Pending) ordered.Add(m);
            int n = 0;
            foreach (var m in ordered)
            {
                if (++n > 25) break;
                int id = m.Id;
                var item = UIX.PressRow(_content, 10f, () =>
                {
                    _mailId = id;
                    Rebuild(false);
                }, "phone-item");
                var sw = UIX.Div(item, "swatch");
                sw.style.width = 36;
                sw.style.height = 36;
                sw.style.flexShrink = 0;
                UIX.Round(sw, 12f);
                sw.style.backgroundColor = m.Pending ? Theme.LaptopAccent : new Color(0.5f, 0.55f, 0.65f, 0.25f);
                UIX.Icon(sw, AppMail.MailIcon(m.Icon), 18f, m.Pending ? Color.white : (Color?)null);
                var mid = UIX.Col(item, 1f);
                mid.style.flexGrow = 1;
                mid.style.flexShrink = 1;
                var title = UIX.Ellipsis(UIX.Text(mid, m.Title, "phone-item-title"));
                if (m.Read && !m.Pending) title.style.unityFontStyleAndWeight = FontStyle.Normal;
                UIX.Ellipsis(UIX.Text(mid, (m.Pending ? "Entscheidung · " : "") + m.Sender + " · Tag " + m.Day, "phone-item-sub"));
                if (m.Pending || !m.Read) UIX.Div(item, "pending-dot").EnableInClassList("urgent", m.Pending);
                UIX.Icon(item, "chevron", 12f);
                UIX.PassThrough(item);
            }
            if (pending > 0) Hint("clock", "Offene Entscheidungen verfallen um 20 Uhr – dann gilt die vorsichtigste Antwort.");
        }

        private void BuildMessage(Sim sim, Mail mail)
        {
            Head(null, null, true);
            sim.Events.MarkRead(mail.Id);
            UIX.Text(_content, mail.Title, "phone-msg-title", "display");
            UIX.Text(_content, "Von " + mail.Sender + " · Tag " + mail.Day + ", " + Fmt.Clock(mail.Time) + " Uhr", "phone-item-sub").style.marginTop = 2;
            UIX.Text(_content, mail.Text, "phone-msg-text");
            if (mail.Pending)
            {
                UIX.Text(_content, "DEINE ENTSCHEIDUNG", "phone-section").style.marginTop = 14;
                for (int i = 0; i < mail.Choices.Count; i++)
                {
                    var c = mail.Choices[i];
                    int ci = i;
                    int id = mail.Id;
                    string txt = c.Label + (c.Cost > 0 ? "  ·  " + Fmt.Money(c.Cost) : "") + (string.IsNullOrEmpty(c.Minigame) ? "" : "  ·  Minispiel");
                    var b = UIX.Button(_content, txt, () =>
                    {
                        string res = Game.Sim != null ? Game.Sim.Events.Choose(id, ci) : "";
                        if (res != "" && IsOpen) Rebuild(false);
                    }, i == 0 ? "accent" : "", c.Cost > sim.Money, string.IsNullOrEmpty(c.Minigame) ? null : "gamepad");
                    b.AddToClassList("btn-left");
                    b.AddToClassList("phone-choice");
                }
                Hint("clock", "Unbeantwortet gilt um 20 Uhr die vorsichtigste Antwort.");
            }
            else if (!string.IsNullOrEmpty(mail.Result))
            {
                var rc = UIX.Col(_content, 4f, "phone-result");
                UIX.Text(rc, "ERGEBNIS", "phone-section").style.marginTop = 0;
                if (!string.IsNullOrEmpty(mail.Chosen)) UIX.Text(rc, "Du hast gewählt: " + mail.Chosen, "phone-item-sub");
                UIX.Text(rc, mail.Result, "phone-msg-text").style.marginTop = 2;
            }
        }

        private void BuildReorder(Sim sim)
        {
            Head("Nachbestellen", "Letzter Lieferant, letzte Menge – ein Tipp genügt.");
            if (sim.ExpressDelivery)
            {
                var ex = UIX.Row(_content, 0f, "phone-summary");
                Pill(ex, "bolt", "Express an: +" + Fmt.Money(GameData.ExpressSurcharge) + " je Bestellung", Theme.LaptopAccent);
            }
            int shown = 0;
            for (int pi = 0; pi < GameData.Products.Length; pi++)
            {
                var p = GameData.Products[pi];
                if (!sim.ProductAvailable(p.Id)) continue;
                int stock = sim.StockQty(p.Id), travel = sim.TravelingCountFor(p.Id), dock = sim.DockCountFor(p.Id), open = sim.PendingCountFor(p.Id);
                if (!sim.IsListed(p.Id) && stock == 0 && travel == 0 && dock == 0 && open == 0) continue;
                shown++;
                int sup = ReorderMemory.GetSupplier(sim, p.Id);
                int bulk = ReorderMemory.GetBulk(sim, p.Id);
                int cost = sim.BulkCost(pi, bulk, sup);
                var bo = GameData.BulkOptions[bulk];

                var item = UIX.Col(_content, 8f, "phone-item");
                item.style.alignItems = Align.Stretch;
                var top = UIX.Row(item, 10f);
                UIX.Round(UIX.Swatch(top, p.Color.ToColor(), 36f, p.Icon), 12f);
                var mid = UIX.Col(top, 1f);
                mid.style.flexGrow = 1;
                mid.style.flexShrink = 1;
                UIX.Ellipsis(UIX.Text(mid, p.Name, "phone-item-title"));
                string info = "Lager " + stock + (travel > 0 ? " · " + travel + " unterwegs" : "") + (dock > 0 ? " · " + dock + " am Eingang" : "") + (open > 0 ? " · " + open + " offen" : "");
                var sub = UIX.Text(mid, info, "phone-item-sub");
                if (stock == 0 && travel == 0 && dock == 0) sub.AddToClassList("bad-text");

                var actions = UIX.Row(item, 0f);
                var size = UIX.PressRow(actions, 4f, () =>
                {
                    ReorderMemory.CycleBulk(Game.Sim, p.Id);
                    Rebuild();
                }, "reorder-size");
                UIX.Text(size, bo.Quantity + " Stk", "reorder-size-text");
                UIX.Icon(size, "chevron_down", 11f);
                UIX.PassThrough(size);
                UIX.Ellipsis(UIX.Text(actions, GameData.Suppliers[sup].Name, "phone-item-sub")).style.flexGrow = 1;
                int prodIndex = pi;
                int supIdx = sup, bulkIdx = bulk;
                var buy = UIX.PressRow(actions, 0f, () =>
                {
                    var s = Game.Sim;
                    if (s == null) return;
                    if (s.BuyBulk(prodIndex, bulkIdx, supIdx)) ReorderMemory.Remember(GameData.Products[prodIndex].Id, supIdx, bulkIdx);
                }, "reorder-main");
                UIX.Icon(buy, "cart", 13f);
                UIX.Num(buy, Fmt.Money(cost), true, "reorder-main-text");
                UIX.PassThrough(buy);
                buy.SetEnabled(sim.Money >= cost);
            }
            if (shown == 0)
            {
                UIX.Empty(_content, "box", "Noch nichts im Sortiment", "Kaufe am Laptop unter „Einkauf“ deine erste Ware – danach geht's hier mit einem Tipp.");
                return;
            }
            Hint("screen", "Andere Lieferanten, Mengen & neue Produkte: Laptop am Schreibtisch » Einkauf.");
        }

        private void BuildStatus(Sim sim)
        {
            Head("Status", UiFmt.DayLong(sim.Day) + " · " + Fmt.Clock(sim.TimeMinutes) + " Uhr");
            var hero = UIX.Col(_content, 2f, "status-hero");
            UIX.Text(hero, "KONTOSTAND", "status-tile-key");
            var money = UIX.Num(hero, Fmt.Money(sim.Money), true, "status-money");
            if (sim.Money < 0) money.AddToClassList("bad-text");
            var hr = UIX.Row(hero, 6f);
            hr.style.marginTop = 4;
            var chip = UIX.Chip(hr, "+" + Fmt.Money(sim.Daily.Revenue) + " heute", "trend", Theme.LaptopGood, "delta");
            chip.style.alignSelf = Align.FlexStart;
            if (sim.Debt > 0) UIX.Chip(hr, "Kredit " + Fmt.Money(sim.Debt), "bank", Theme.LaptopBad);
            var values = new List<float>();
            int from = Math.Max(0, sim.History.Count - 9);
            for (int i = from; i < sim.History.Count; i++) values.Add(sim.History[i].Revenue);
            values.Add(sim.Daily.Revenue);
            if (values.Count >= 2)
            {
                var spark = new Sparkline(values, Theme.LaptopAccent);
                spark.style.height = 56;
                hero.Add(spark);
            }

            var grid = UIX.Div(_content, "status-grid");
            Tile(grid, "BEWERTUNG", Fmt.Rating(sim.Reputation), t => UIX.Stars(t, sim.Reputation, 11f, Theme.LaptopAccent), sim.ReviewCount + " Bewertungen");
            Tile(grid, "FIRMENLEVEL", "Level " + sim.Level, t => UIX.Bar(t, sim.LevelProgress(), Theme.LaptopAccent, 5f).style.marginTop = 6,
                sim.Level >= GameData.MaxLevel ? "Maximum erreicht" : UiFmt.Percent(sim.LevelProgress()) + " bis Level " + (sim.Level + 1));
            Tile(grid, "UMSATZ HEUTE", Fmt.Money(sim.Daily.Revenue), null, sim.Daily.Shipped + " Pakete verschickt");
            Tile(grid, "FIXKOSTEN 20 UHR", "−" + Fmt.Money(sim.FixedCostsPerDay()), null, "Miete, Löhne, Zinsen", true);
            Tile(grid, "BESTELLUNGEN", sim.PendingCount() + " / " + sim.QueueCapacity(), null, sim.Daily.Lost > 0 ? sim.Daily.Lost + " heute verloren" : "keine verloren");
            Tile(grid, "LAGER", Fmt.Thousands(sim.StockTotal()) + " / " + Fmt.Thousands(sim.Capacity()), null, GameData.StageNames[Mathf.Clamp(sim.LocationStage, 0, GameData.StageNames.Length - 1)]);

            if (sim.Boosts.Count > 0)
            {
                UIX.Text(_content, "AKTIVE BOOSTS", "phone-section");
                foreach (var b in sim.Boosts)
                {
                    var r = UIX.Row(_content, 8f, "phone-item");
                    UIX.Icon(r, "bolt", 16f, Theme.LaptopAccent);
                    UIX.Ellipsis(UIX.Text(r, b.Name, "phone-item-title")).style.flexGrow = 1;
                    UIX.Num(r, "×" + Fmt.Dec(b.Mult, 1) + " · " + UiFmt.Duration(b.EndsAt - sim.BClock()), false, "phone-item-sub");
                }
            }
            Hint("clock", UiFmt.UntilClosing(sim) + ". Dann wird abgerechnet.");
        }

        private void Tile(VisualElement grid, string key, string value, Action<VisualElement> extra, string sub, bool bad = false)
        {
            var t = UIX.Col(grid, 1f, "status-tile");
            UIX.Text(t, key, "status-tile-key");
            var v = UIX.Num(t, value, true, "status-tile-value");
            if (bad) v.AddToClassList("bad-text");
            extra?.Invoke(t);
            if (!string.IsNullOrEmpty(sub)) UIX.Text(t, sub, "phone-item-sub");
        }

        private void BuildExtra(Sim sim)
        {
            int i = _app - 4;
            if (i < 0 || i >= ExtraApps.Count || ExtraApps[i] == null)
            {
                _app = 0;
                BuildOrders(sim);
                return;
            }
            var a = ExtraApps[i];
            Head(a.Title, null);
            a.Build?.Invoke(this, _content);
        }
    }
}
