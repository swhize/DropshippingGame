using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Spieler-HUD: Geld, Tag/Uhrzeit, Bewertung, Firmenlevel, Bestellungen, aktuelles Ziel,
    /// aktive Boosts, Fadenkreuz mit Aktionshinweis, gehaltener Gegenstand, Benachrichtigungen.
    /// </summary>
    public sealed class HudView
    {
        private readonly VisualElement _layer;
        private Label _money, _pop, _day, _clock, _rating, _level, _objTitle, _objText, _promptText, _promptTitle, _handsText, _hint, _banner, _bannerSub, _fps;
        private Label _orders, _dock, _travel, _packed, _promptKey;
        private VisualElement _stars, _xpBar, _objBar, _boosts, _toasts, _crosshair, _prompt, _hands, _handsSwatch, _orderDots, _left, _right, _statusRow;
        private int _lastMoney;
        private string _lastPrompt = "";
        private float _fpsAcc;
        private int _fpsFrames;

        public HudView(VisualElement layer)
        {
            _layer = layer;
            Build();
            UIX.Show(_layer, false);
        }

        public void SetVisible(bool on)
        {
            UIX.Show(_layer, on);
            if (on) Refresh();
        }

        private void Build()
        {
            // Links oben: Geld, Tag, Uhr, Bewertung, Level, Status
            _left = UIX.Col(_layer, 6f, "hud-panel", "hud-left");
            _left.pickingMode = PickingMode.Ignore;
            var mrow = UIX.Row(_left, 0f);
            _money = UIX.Text(mrow, "0 €", "hud-money");
            _pop = UIX.Text(mrow, "", "hud-pop");
            _pop.style.opacity = 0f;
            var trow = UIX.Row(_left, 12f);
            UIX.Icon(trow, "calendar", 16f, Theme.Muted);
            _day = UIX.Text(trow, "Tag 1", "hud-clock");
            UIX.Icon(trow, "clock", 16f, Theme.Muted);
            _clock = UIX.Text(trow, "08:00", "hud-clock");
            UIX.Spacer(trow);
            _stars = UIX.Row(trow, 1f);
            _rating = UIX.Text(trow, "3,0", "hud-clock", "hud-gold");
            var lrow = UIX.Row(_left, 10f);
            _level = UIX.Text(lrow, "Level 1", "hud-level");
            _xpBar = UIX.Bar(lrow, 0f, null, 7f);
            _statusRow = UIX.Row(_left, 14f);
            _statusRow.style.marginTop = 4;
            _orders = Status(_statusRow, "cart", "Bestellungen");
            _dock = Status(_statusRow, "box", "Eingang");
            _travel = Status(_statusRow, "truck", "Unterwegs");
            _packed = Status(_statusRow, "package", "Verpackt");
            _orderDots = UIX.Row(_left, 0f);
            _orderDots.style.flexWrap = Wrap.Wrap;

            // Rechts oben: Ziel + Boosts
            _right = UIX.Col(_layer, 6f, "hud-panel", "hud-right");
            _right.pickingMode = PickingMode.Ignore;
            _objTitle = UIX.Text(_right, "", "obj-title");
            _objText = UIX.Text(_right, "", "obj-text");
            _objBar = UIX.Bar(_right, 0f, Theme.Accent, 5f);
            _boosts = UIX.Col(_right, 5f);

            _toasts = UIX.Col(_layer, 0f, "toasts");
            _toasts.pickingMode = PickingMode.Ignore;

            // Mitte: Fadenkreuz + Hinweis
            _crosshair = UIX.Div(_layer, "crosshair");
            UIX.Div(_crosshair, "cross-ring").name = "ring";
            UIX.Div(_crosshair, "cross-dot");
            _prompt = UIX.Col(_layer, 0f, "prompt");
            _promptTitle = UIX.Text(_prompt, "", "prompt-title");
            var pill = UIX.Div(_prompt, "prompt-pill");
            _promptKey = UIX.Key(pill, "E");
            _promptText = UIX.Text(pill, "", "prompt-text");

            // Unten
            _hands = UIX.Row(_layer, 10f, "hud-panel", "hands");
            _handsSwatch = UIX.Swatch(_hands, Theme.Muted, 18f);
            _handsSwatch.style.borderTopLeftRadius = 5;
            _handsText = UIX.Text(_hands, "Hände frei");
            _hint = UIX.Text(UIX.Div(_layer, "hints"), "", "hint-text");

            var banner = UIX.Col(_layer, 2f, "banner");
            _banner = UIX.Text(banner, "", "banner-text");
            _bannerSub = UIX.Text(banner, "", "banner-sub");
            banner.style.opacity = 0f;
            banner.name = "banner";
            _fps = UIX.Text(_layer, "", "fps");

            foreach (var el in _layer.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
        }

        private Label Status(VisualElement parent, string icon, string title)
        {
            var c = UIX.Row(parent, 5f);
            UIX.Icon(c, icon, 15f, Theme.Muted);
            var v = UIX.Text(c, "0", "hud-status-value");
            c.tooltip = title;
            return v;
        }

        public void Tick(float dt)
        {
            var sim = Game.Sim;
            if (sim == null || _layer.style.display == DisplayStyle.None) return;
            _clock.text = Fmt.Clock(sim.TimeMinutes);
            _clock.EnableInClassList("hud-gold", sim.LifestyleOwned.Contains("uhr"));
            bool locked = Game.Root != null && Game.Root.InputLocked;
            UIX.Show(_crosshair, !locked);
            var player = Game.Player;
            string p = player != null && !locked ? player.PromptText : "";
            UIX.Show(_prompt, p != "");
            _crosshair.EnableInClassList("active", p != "");
            UIX.Show(_crosshair.Q("ring"), p != "");
            if (p != _lastPrompt)
            {
                _lastPrompt = p;
                _promptText.text = p;
                _promptTitle.text = player != null && player.Focus != null ? player.Focus.Title.ToUpperInvariant() : "";
                _promptKey.text = GameInput.KeyLabel("interact");
            }
            _left.style.opacity = sim.PcOpen ? 0.35f : 1f;
            RefreshBoosts(sim);
            _hint.text = GameInput.KeyLabel("interact") + " Interagieren · " + GameInput.KeyLabel("drop") + " Ablegen · " +
                         GameInput.KeyLabel("laptop") + " Laptop · " + GameInput.KeyLabel("pause") + " Menü · " + GameInput.KeyLabel("help") + " Hilfe";
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
            else _fps.text = "";
        }

        private string _boostSig = "";

        private void RefreshBoosts(Sim sim)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var b in sim.Boosts) sb.Append(b.Name).Append((int)Mathf.Max(0f, b.EndsAt - sim.BClock())).Append(';');
            bool offline = sim.BClock() < sim.ShopOfflineUntil;
            sb.Append(offline);
            string sig = sb.ToString();
            if (sig == _boostSig) return;
            _boostSig = sig;
            _boosts.Clear();
            foreach (var b in sim.Boosts)
            {
                int left = (int)Mathf.Max(0f, b.EndsAt - sim.BClock());
                UIX.Chip(_boosts, b.Name + " · ×" + Fmt.Dec(b.Mult, 1) + " · " + left + " min", "bolt", Theme.Teal, "boost-chip").style.alignSelf = Align.FlexStart;
            }
            if (offline) UIX.Chip(_boosts, "Shop offline", "warning", Theme.Bad).style.alignSelf = Align.FlexStart;
        }

        public void Refresh()
        {
            var sim = Game.Sim;
            if (sim == null) return;
            if (sim.Money != _lastMoney)
            {
                PopMoney(sim.Money - _lastMoney);
                _lastMoney = sim.Money;
            }
            _money.text = Fmt.Money(sim.Money);
            _money.style.color = sim.Money < 0 ? Theme.Bad : Theme.Text;
            _day.text = sim.StoryStage == "business" ? "Tag " + sim.Day : "Imbiss";
            _stars.Clear();
            UIX.Stars(_stars, sim.Reputation, 14f);
            _rating.text = Fmt.Rating(sim.Reputation);
            _level.text = "Level " + sim.Level;
            UIX.SetBar(_xpBar, sim.LevelProgress());
            bool business = sim.StoryStage == "business";
            UIX.Show(_statusRow, business);
            UIX.Show(_orderDots, business);
            if (business)
            {
                _orders.text = sim.PendingCount() + "/" + sim.QueueCapacity();
                _orders.style.color = sim.PendingCount() >= sim.QueueCapacity() ? Theme.Bad : Theme.Text;
                _dock.text = sim.DockCrates.Count.ToString();
                _travel.text = sim.TravelingDeliveries.Count.ToString();
                _packed.text = sim.PackedCount().ToString();
                _orderDots.Clear();
                int n = 0;
                foreach (var o in sim.OrderQueue)
                {
                    if (n++ >= 24) break;
                    var d = UIX.Div(_orderDots, "order-dot");
                    d.style.backgroundColor = GameData.Product(o.Product).Color.ToColor();
                    d.tooltip = GameData.Product(o.Product).Name;
                }
            }
            var obj = sim.CurrentObjective();
            _objTitle.text = obj.Title;
            _objText.text = obj.Text;
            UIX.Show(_objBar, obj.Progress >= 0f);
            UIX.SetBar(_objBar, Mathf.Max(0f, obj.Progress));
        }

        private void PopMoney(int delta)
        {
            if (delta == 0) return;
            _pop.text = (delta > 0 ? "+" : "") + Fmt.Money(delta);
            _pop.style.color = delta > 0 ? Theme.Good : Theme.Bad;
            _pop.style.opacity = 1f;
            Anim.Run(0.8f, t => _pop.style.opacity = 1f - t, null, Ease.Linear, true, _pop, 1.2f);
        }

        public void SetHeld(ItemData data)
        {
            if (data == null)
            {
                _handsText.text = "Hände frei";
                _handsSwatch.style.backgroundColor = new Color(0.4f, 0.42f, 0.48f);
                return;
            }
            string pname = string.IsNullOrEmpty(data.Product) ? "" : GameData.Product(data.Product).Name;
            _handsSwatch.style.backgroundColor = string.IsNullOrEmpty(data.Product) ? new Color(0.95f, 0.8f, 0.4f) : GameData.Product(data.Product).Color.ToColor();
            string drop = "   [" + GameInput.KeyLabel("drop") + "] ablegen";
            switch (data.Kind)
            {
                case ItemKind.Crate: _handsText.text = "Kiste: " + data.Quantity + "× " + pname + " (" + GameData.QualityName(data.Quality) + ")" + drop; break;
                case ItemKind.Item: _handsText.text = pname + " für Bestellung (" + Fmt.Money(data.Price) + ")" + drop; break;
                case ItemKind.Package: _handsText.text = "Paket: " + pname + " – noch ohne Label" + drop; break;
                case ItemKind.Labeled: _handsText.text = "Versandfertig: " + pname + drop; break;
                case ItemKind.Plate: _handsText.text = "Teller für Tisch " + data.Table; break;
                default: _handsText.text = "Hände frei"; break;
            }
        }

        public void ShowLevelUp(int level)
        {
            var banner = _layer.Q("banner");
            _banner.text = "LEVEL " + level;
            GameData.LevelUnlocks.TryGetValue(level, out string unlock);
            _bannerSub.text = unlock ?? "";
            Anim.Run(3.2f, t =>
            {
                float a = t < 0.08f ? t / 0.08f : (t > 0.78f ? 1f - (t - 0.78f) / 0.22f : 1f);
                banner.style.opacity = a;
                float s = t < 0.1f ? Mathf.Lerp(0.85f, 1f, Anim.Apply(Ease.OutBack, t / 0.1f)) : 1f;
                banner.style.scale = new Scale(new Vector3(s, s, 1f));
            }, null, Ease.Linear, true, banner);
        }

        public void ShowBanner(string title, string sub)
        {
            var banner = _layer.Q("banner");
            _banner.text = title;
            _bannerSub.text = sub;
            Anim.Run(2.6f, t => banner.style.opacity = t < 0.1f ? t / 0.1f : (t > 0.75f ? 1f - (t - 0.75f) / 0.25f : 1f), null, Ease.Linear, true, banner);
        }

        private readonly List<VisualElement> _toastList = new List<VisualElement>();

        public void AddToast(string text, string kind)
        {
            var t = UIX.Div(_toasts, "toast", "toast-" + kind);
            t.pickingMode = PickingMode.Ignore;
            UIX.Text(t, text, "toast-text").pickingMode = PickingMode.Ignore;
            _toastList.Add(t);
            while (_toastList.Count > 5)
            {
                _toastList[0].RemoveFromHierarchy();
                _toastList.RemoveAt(0);
            }
            t.style.opacity = 0f;
            t.style.translate = new Translate(20, 0, 0);
            Anim.Run(5.2f, x =>
            {
                float a = x < 0.04f ? x / 0.04f : (x > 0.88f ? 1f - (x - 0.88f) / 0.12f : 1f);
                t.style.opacity = a;
                t.style.translate = new Translate(x < 0.04f ? 20f * (1f - x / 0.04f) : 0f, 0, 0);
            }, () =>
            {
                t.RemoveFromHierarchy();
                _toastList.Remove(t);
            }, Ease.Linear, true);
        }
    }
}
