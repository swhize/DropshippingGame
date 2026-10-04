using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Admin-Panel zum Testen (F10): Geld, Level, alles freischalten, Ware, Bestellungen,
    /// Ereignisse, Trends, Zeit und Teleport. Baut sich komplett im Code auf (keine USS nötig).
    /// </summary>
    public sealed class AdminPanel
    {
        public VisualElement Panel { get; private set; }
        public bool IsOpen { get; private set; }

        private readonly VisualElement _layer;
        private VisualElement _body;
        private VisualElement _tabBar;
        private Label _status;
        private string _tab = "general";
        private float _statusT;
        private int _deliverQty = 50;

        private static readonly Color Bg = new Color(0.07f, 0.07f, 0.09f, 0.97f);
        private static readonly Color Card = new Color(0.13f, 0.13f, 0.16f, 1f);
        private static readonly Color Accent = new Color(1f, 0.35f, 0.2f, 1f);
        private static readonly Color Text = new Color(0.93f, 0.93f, 0.95f, 1f);
        private static readonly Color Muted = new Color(0.6f, 0.6f, 0.66f, 1f);

        private static readonly string[] TabIds = { "general", "stock", "orders", "events", "trends", "world" };
        private static readonly string[] TabNames = { "Allgemein", "Ware", "Bestellungen", "Ereignisse", "Trends", "Welt & Zeit" };

        public AdminPanel(VisualElement layer)
        {
            _layer = layer;
            Build();
            SetOpen(false);
        }

        private static Sim S => Game.Sim;

        // =====================================================================================
        // Öffnen / Schließen
        // =====================================================================================
        public void Toggle() => SetOpen(!IsOpen);
        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        private void SetOpen(bool open)
        {
            if (open && S == null) return;
            IsOpen = open;
            if (Panel != null) Panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (Game.Root != null) Game.Root.Lock("admin", open);
            if (open) Rebuild();
        }

        public void Tick(float dt)
        {
            var sim = S;
            if (sim == null) return;
            try { sim.AdminTick(); } catch (Exception e) { Debug.LogException(e); }
            if (!IsOpen || _status == null) return;
            _statusT -= dt;
            if (_statusT > 0f) return;
            _statusT = 0.25f;
            int h = (int)(sim.TimeMinutes / 60f), m = (int)(sim.TimeMinutes % 60f);
            _status.text = "Geld " + sim.Money.ToString("N0") + " €   ·   Level " + sim.Level + " (" + sim.Xp + " XP)   ·   Tag " + sim.Day +
                           " " + h.ToString("00") + ":" + m.ToString("00") + "   ·   Bewertung " + sim.Reputation.ToString("0.0") +
                           "   ·   Schulden " + sim.Debt.ToString("N0") + " €   ·   Bestellungen " + sim.PendingCount() +
                           "   ·   Zeit ×" + sim.AdminTimeScale.ToString("0.#");
        }

        // =====================================================================================
        // Aufbau
        // =====================================================================================
        private void Build()
        {
            Panel = new VisualElement { name = "admin-panel" };
            var st = Panel.style;
            st.position = Position.Absolute;
            st.left = new Length(8, LengthUnit.Percent);
            st.right = new Length(8, LengthUnit.Percent);
            st.top = new Length(6, LengthUnit.Percent);
            st.bottom = new Length(6, LengthUnit.Percent);
            st.backgroundColor = Bg;
            Round(Panel, 14);
            Border(Panel, Accent, 2);
            st.paddingLeft = st.paddingRight = 18;
            st.paddingTop = st.paddingBottom = 14;
            st.flexDirection = FlexDirection.Column;
            Panel.pickingMode = PickingMode.Position;

            var head = Row();
            var title = new Label("ADMIN-PANEL");
            title.style.fontSize = 30;
            title.style.color = Accent;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexGrow = 1;
            head.Add(title);
            var hint = new Label("F10 / Esc schließen");
            hint.style.color = Muted;
            hint.style.fontSize = 16;
            hint.style.marginRight = 12;
            head.Add(hint);
            head.Add(Btn("X", Close, Accent));
            Panel.Add(head);

            _status = new Label("");
            _status.style.color = Text;
            _status.style.fontSize = 16;
            _status.style.marginTop = 6;
            _status.style.marginBottom = 8;
            _status.style.whiteSpace = WhiteSpace.Normal;
            Panel.Add(_status);

            _tabBar = Row();
            _tabBar.style.flexWrap = Wrap.Wrap;
            _tabBar.style.marginBottom = 10;
            Panel.Add(_tabBar);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            _body = scroll.contentContainer;
            Panel.Add(scroll);

            _layer.Add(Panel);
        }

        private void Rebuild()
        {
            _statusT = 0f;
            _tabBar.Clear();
            for (int i = 0; i < TabIds.Length; i++)
            {
                string id = TabIds[i];
                var b = Btn(TabNames[i], () =>
                {
                    _tab = id;
                    Rebuild();
                }, id == _tab ? Accent : Card);
                _tabBar.Add(b);
            }
            _body.Clear();
            if (S == null) return;
            try
            {
                switch (_tab)
                {
                    case "general": BuildGeneral(); break;
                    case "stock": BuildStock(); break;
                    case "orders": BuildOrders(); break;
                    case "events": BuildEvents(); break;
                    case "trends": BuildTrends(); break;
                    case "world": BuildWorld(); break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _body.Add(Info("Fehler beim Aufbau: " + e.Message));
            }
        }

        // =====================================================================================
        // Reiter
        // =====================================================================================
        private void BuildGeneral()
        {
            var sim = S;
            var big = Btn("ALLES FREISCHALTEN  (Max-Level, alle Upgrades, Lagerhalle, Skills, 1 Mio €)", () => Do(sim.AdminUnlockEverything), Accent);
            big.style.height = 54;
            big.style.fontSize = 20;
            _body.Add(big);

            _body.Add(Section("Geld"));
            var money = Row();
            money.style.flexWrap = Wrap.Wrap;
            foreach (int amount in new[] { 1000, 10000, 100000, 1000000 })
            {
                int a = amount;
                money.Add(Btn("+" + a.ToString("N0") + " €", () => Do(() => sim.AdminAddMoney(a))));
            }
            money.Add(Btn("−10.000 €", () => Do(() => sim.AdminAddMoney(-10000))));
            money.Add(Btn("Geld auf 0", () => Do(() => sim.AdminSetMoney(0))));
            money.Add(Btn("Schulden weg", () => Do(sim.AdminClearDebt)));
            _body.Add(money);
            _body.Add(ToggleRow("Unendlich Geld (nie unter 1 Mio)", sim.AdminInfiniteMoney, v => sim.AdminInfiniteMoney = v));
            _body.Add(ToggleRow("Keine Insolvenz", sim.AdminNoBankrupt, v => sim.AdminNoBankrupt = v));

            _body.Add(Section("Level & Skills"));
            var lv = Row();
            lv.style.flexWrap = Wrap.Wrap;
            for (int i = 1; i <= GameData.MaxLevel; i++)
            {
                int l = i;
                lv.Add(Btn("Lv " + l, () => Do(() => sim.AdminSetLevel(l)), sim.Level == l ? Accent : Card));
            }
            _body.Add(lv);
            var sk = Row();
            sk.style.flexWrap = Wrap.Wrap;
            sk.Add(Btn("+500 XP", () => Do(() => sim.AdminAddXp(500))));
            sk.Add(Btn("Alle Skills lernen", () => Do(sim.AdminLearnAllSkills)));
            sk.Add(Btn("+3 Skillpunkte", () => Do(() => sim.AddSkillPoints(3))));
            sk.Add(Btn("Alle Upgrades + Deko + Lifestyle", () => Do(sim.AdminUnlockUpgrades)));
            _body.Add(sk);

            _body.Add(Section("Firma"));
            var co = Row();
            co.style.flexWrap = Wrap.Wrap;
            co.Add(Btn("Bewertung 5,0", () => Do(() => sim.AdminSetReputation(5f))));
            co.Add(Btn("Bewertung 1,0", () => Do(() => sim.AdminSetReputation(1f))));
            co.Add(Btn("Bekanntheit max", () => Do(sim.AdminMaxAwareness)));
            co.Add(Btn("TikToks zurücksetzen", () => Do(sim.AdminResetTikTok)));
            co.Add(Btn("Intro überspringen", () => Do(sim.AdminSkipIntro)));
            co.Add(Btn("Speichern", () => Do(() => sim.SaveGame())));
            _body.Add(co);
        }

        private void BuildStock()
        {
            var sim = S;
            _body.Add(Info("Kiste landet sofort und gratis am Wareneingang."));
            var qty = Row();
            qty.Add(Info("Menge pro Kiste:"));
            foreach (int q in new[] { 10, 50, 200, 1000 })
            {
                int v = q;
                qty.Add(Btn(v.ToString(), () =>
                {
                    _deliverQty = v;
                    Rebuild();
                }, _deliverQty == v ? Accent : Card));
            }
            _body.Add(qty);

            var grid = Row();
            grid.style.flexWrap = Wrap.Wrap;
            foreach (var p in GameData.Products)
            {
                string pid = p.Id;
                grid.Add(Btn(p.Name, () => Do(() => sim.AdminDeliver(pid, _deliverQty))));
            }
            _body.Add(grid);

            _body.Add(Section("Sonstiges"));
            var r = Row();
            r.style.flexWrap = Wrap.Wrap;
            r.Add(Btn("Alle Lieferungen sofort da", () => Do(sim.AdminArriveDeliveries)));
            r.Add(Btn("+100 Kartons (jede Größe)", () => Do(() => sim.AdminAddPackaging(100))));
            r.Add(Btn("Alle Produkte im Shop listen", () => Do(sim.AdminListAll)));
            _body.Add(r);
        }

        private void BuildOrders()
        {
            var sim = S;
            var r = Row();
            r.style.flexWrap = Wrap.Wrap;
            r.Add(Btn("+1 Bestellung", () => Do(() => sim.AdminSpawnOrders(1, false))));
            r.Add(Btn("+5 Bestellungen", () => Do(() => sim.AdminSpawnOrders(5, false))));
            r.Add(Btn("+3 Express", () => Do(() => sim.AdminSpawnOrders(3, true))));
            r.Add(Btn("Großauftrag-Angebot", () => Do(sim.AdminContractOffer)));
            r.Add(Btn("Retourenwelle (3)", () => Do(() => sim.AdminReturnWave(3))));
            _body.Add(r);
            _body.Add(Info("Bestellungen nehmen gelistete Produkte (sonst zufällig). Offene Bestellungen: " + sim.PendingCount()));
        }

        private void BuildEvents()
        {
            var sim = S;
            _body.Add(Section("Mini-Event: Straßenfest"));
            _body.Add(Info("Status: " + sim.FestivalStatusText() + (sim.FestivalActive ? " · Stand: " + sim.FestivalTotal() + " Artikel · " + sim.FestivalStats.Sold + " verkauft" : "")));
            var fr = Row();
            fr.style.flexWrap = Wrap.Wrap;
            fr.Add(Btn("Straßenfest jetzt (mit Aufbau)", () => Do(() => sim.AdminStartFestival(false)), Accent));
            fr.Add(Btn("Straßenfest sofort live", () => Do(() => sim.AdminStartFestival(true))));
            fr.Add(Btn("Einladung für morgen", () => Do(() => sim.AdminAnnounceFestival())));
            fr.Add(Btn("Fest beenden", () => Do(sim.AdminEndFestival)));
            _body.Add(fr);
            _body.Add(Section("Zufallsereignisse"));
            _body.Add(Info("Klick löst das Ereignis sofort aus (Mail kommt aufs Handy/Laptop)."));
            foreach (var ev in EventData.All)
            {
                if (ev == null) continue;
                string id = ev.Id;
                var b = Btn(id + "   –   " + (ev.Title ?? ""), () => Do(() => sim.AdminTriggerEvent(id)));
                b.style.unityTextAlign = TextAnchor.MiddleLeft;
                _body.Add(b);
            }
        }

        private void BuildTrends()
        {
            var sim = S;
            string[] names = { "Normal", "Steigend", "HYPE", "Fallend", "Tot" };
            foreach (var p in GameData.Products)
            {
                string pid = p.Id;
                var r = Row();
                var l = new Label(p.Name + "  (" + sim.TrendLabel(pid) + ")");
                l.style.color = Text;
                l.style.width = 280;
                l.style.fontSize = 16;
                r.Add(l);
                for (int i = 0; i < names.Length; i++)
                {
                    var ph = (TrendPhase)i;
                    r.Add(Btn(names[i], () => Do(() => sim.AdminTriggerTrend(pid, ph)), i == 2 ? Accent : Card));
                }
                _body.Add(r);
            }
        }

        private void BuildWorld()
        {
            var sim = S;
            _body.Add(Section("Zeit"));
            var t = Row();
            t.style.flexWrap = Wrap.Wrap;
            foreach (float f in new[] { 0f, 1f, 3f, 10f })
            {
                float v = f;
                t.Add(Btn(v == 0f ? "Zeit anhalten" : "Zeit ×" + v, () =>
                {
                    sim.AdminTimeScale = v;
                    Rebuild();
                }, Mathf.Approximately(sim.AdminTimeScale, v) ? Accent : Card));
            }
            t.Add(Btn("+1 Stunde", () => Do(() => sim.AdminSkipMinutes(60f))));
            t.Add(Btn("Feierabend jetzt", () =>
            {
                Close();
                sim.EndDayNow();
            }));
            _body.Add(t);

            _body.Add(Section("Teleport"));
            var tp = Row();
            tp.style.flexWrap = Wrap.Wrap;
            tp.Add(Btn("Garage", () => Teleport(WorldBuilder.SpawnGarage)));
            tp.Add(Btn("Lagerhalle", () => Teleport(WorldBuilder.SpawnWarehouse)));
            tp.Add(Btn("Kalles Imbiss", () => Teleport(WorldBuilder.SpawnDiner)));
            _body.Add(tp);
            _body.Add(Info("Tipp: Die Lagerhalle steht erst nach „Alle Upgrades“ bzw. „Alles freischalten“."));
            _body.Add(Section("Stadt, Post & Emotes"));
            var city = Row();
            city.style.flexWrap = Wrap.Wrap;
            foreach (var a in CityDebug.Actions())
            {
                var act = a;
                city.Add(Btn(act.label, () =>
                {
                    if (act.close) Close();
                    Do(act.run);
                }));
            }
            _body.Add(city);
        }

        // =====================================================================================
        // Helfer
        // =====================================================================================
        private void Teleport(Vector3 pos)
        {
            if (Game.Player == null) return;
            Close();
            Game.Player.Teleport(pos, 180f);
        }

        private void Do(Action a)
        {
            try { a(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                S?.Notify("Admin-Fehler: " + e.Message, "bad");
            }
            if (IsOpen) Rebuild();
        }

        private static VisualElement Row()
        {
            var r = new VisualElement();
            r.style.flexDirection = FlexDirection.Row;
            r.style.alignItems = Align.Center;
            r.style.marginBottom = 4;
            return r;
        }

        private static Label Section(string text)
        {
            var l = new Label(text.ToUpperInvariant());
            l.style.color = Accent;
            l.style.fontSize = 15;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.marginTop = 14;
            l.style.marginBottom = 6;
            return l;
        }

        private static Label Info(string text)
        {
            var l = new Label(text);
            l.style.color = Muted;
            l.style.fontSize = 15;
            l.style.marginRight = 8;
            l.style.marginBottom = 6;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        private static Button Btn(string text, Action onClick, Color? bg = null)
        {
            var b = new Button(onClick) { text = text };
            var st = b.style;
            st.backgroundColor = bg ?? Card;
            st.color = Text;
            st.fontSize = 16;
            st.paddingLeft = st.paddingRight = 12;
            st.paddingTop = st.paddingBottom = 6;
            st.marginRight = 6;
            st.marginBottom = 6;
            st.marginLeft = 0;
            st.marginTop = 0;
            Round(b, 8);
            Border(b, new Color(1f, 1f, 1f, 0.08f), 1);
            return b;
        }

        private VisualElement ToggleRow(string label, bool value, Action<bool> set)
        {
            var b = Btn((value ? "[AN]  " : "[AUS]  ") + label, null, value ? Accent : Card);
            b.clicked += () =>
            {
                set(!value);
                Rebuild();
            };
            b.style.alignSelf = Align.FlexStart;
            return b;
        }

        private static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }

        private static void Border(VisualElement e, Color c, float w)
        {
            e.style.borderLeftWidth = e.style.borderRightWidth = w;
            e.style.borderTopWidth = e.style.borderBottomWidth = w;
            e.style.borderLeftColor = e.style.borderRightColor = c;
            e.style.borderTopColor = e.style.borderBottomColor = c;
        }
    }
}
