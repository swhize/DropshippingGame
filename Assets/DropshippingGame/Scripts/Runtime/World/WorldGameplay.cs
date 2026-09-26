using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// v3.0 Bestell-Monitor an der Wand: zeigt die offenen Bestellzettel (<see cref="Sim.Tickets"/>)
    /// mit Countdown, Express und Überfällig-Farbe. Aktualisiert sich bei <see cref="Sim.OrdersChanged"/>
    /// und alle paar Sekunden (Countdowns).
    /// </summary>
    public sealed class OrderMonitor : MonoBehaviour
    {
        private Transform _content;
        private int _stage;
        private Sim _sim;
        private float _acc;
        private readonly List<Label3D> _lines = new List<Label3D>();
        private Label3D _header;
        private string _sig = "";

        private int MaxLines => _stage == 0 ? 6 : 7;

        public void Setup(Transform content, int stage)
        {
            _content = content;
            _stage = stage;
            float h = stage == 0 ? 0.78f : 1.35f;
            float lineH = h / (MaxLines + 1.6f);
            float size = lineH * 0.62f / 0.004f;
            _header = Label3D.Create(_content, "BESTELLUNGEN", size * 1.1f, new Color(0.55f, 0.8f, 1f), new Vector3(0, h / 2f - lineH * 0.75f, 0), false, 0f, false, 1.6f);
            for (int i = 0; i < MaxLines; i++)
            {
                var l = Label3D.Create(_content, "", size, Color.white, new Vector3(0, h / 2f - lineH * (1.95f + i), 0), false, 0f, false, 1.4f);
                _lines.Add(l);
            }
            _sim = Game.Sim;
            if (_sim != null) _sim.OrdersChanged += Redraw;
            Redraw();
        }

        private void OnDestroy()
        {
            if (_sim != null) _sim.OrdersChanged -= Redraw;
        }

        private void Update()
        {
            _acc += Time.deltaTime;
            if (_acc < 2f) return;
            _acc = 0f;
            Redraw();
        }

        private void Redraw()
        {
            if (this == null || _content == null) return;
            var sim = Game.Sim;
            if (sim == null) return;
            List<Order> tickets;
            try
            {
                tickets = sim.Tickets();
            }
            catch (System.Exception)
            {
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append(tickets.Count);
            int shown = Mathf.Min(tickets.Count, MaxLines);
            bool more = tickets.Count > MaxLines;
            if (more) shown = MaxLines - 1;
            var texts = new string[MaxLines];
            var cols = new Color[MaxLines];
            for (int i = 0; i < shown; i++)
            {
                var o = tickets[i];
                string pname = GameData.IsProduct(o.Product) ? GameData.Product(o.Product).Short : o.Product;
                string stage = _stage == 0 ? "" : StageShort(o.Stage);
                string line = o.Number + (o.Express ? " EXPRESS " : " ") + pname + " · " + sim.OrderTimeLeftText(o) + (stage != "" ? " · " + stage : "");
                int maxChars = _stage == 0 ? 34 : 46;
                texts[i] = line.Length > maxChars ? line.Substring(0, maxChars - 3) + "..." : line;
                float u = sim.OrderUrgency(o);
                cols[i] = sim.OrderOverdue(o) ? new Color(1f, 0.35f, 0.3f) : (u > 0.7f ? new Color(1f, 0.75f, 0.3f) : (o.Express ? new Color(1f, 0.6f, 0.55f) : new Color(0.85f, 0.95f, 0.88f)));
            }
            if (more)
            {
                texts[MaxLines - 1] = "+ " + (tickets.Count - shown) + " weitere";
                cols[MaxLines - 1] = new Color(0.7f, 0.75f, 0.85f);
            }
            if (tickets.Count == 0)
            {
                texts[0] = "Keine offenen Bestellungen";
                cols[0] = new Color(0.6f, 0.7f, 0.8f);
            }
            for (int i = 0; i < MaxLines; i++) sb.Append('|').Append(texts[i]);
            string sig = sb.ToString();
            if (sig == _sig) return;
            _sig = sig;
            _header.SetText("BESTELLUNGEN (" + tickets.Count + ")");
            for (int i = 0; i < MaxLines; i++)
            {
                _lines[i].SetText(texts[i] ?? "");
                _lines[i].SetColor(cols[i]);
            }
        }

        private static string StageShort(OrderStage s)
        {
            switch (s)
            {
                case OrderStage.Picked: return "entnommen";
                case OrderStage.Packed: return "verpackt";
                case OrderStage.Labeled: return "etikettiert";
                case OrderStage.Conveyor: return "auf dem Band";
            }
            return "";
        }
    }

    /// <summary>
    /// v3.0 Ziel-Markierung: schwebender Diamant mit Pfeil und Entfernung über der Station, die für den
    /// aktuellen Tutorial-Schritt bzw. den getragenen Gegenstand gebraucht wird. Unauffällig: nach dem
    /// Tutorial nur, solange man etwas trägt; ausgeblendet bei offenem Laptop/Handy/Menü und in der Nähe.
    /// </summary>
    public sealed class ObjectiveMarker : MonoBehaviour
    {
        private PlayerController _player;
        private Transform _visual, _gem;
        private Label3D _dist;
        private Station _target;
        private float _retarget, _t;

        public static ObjectiveMarker Create(Transform parent, PlayerController player)
        {
            var go = new GameObject("ObjectiveMarker");
            go.transform.SetParent(parent, false);
            go.layer = 2; // Ignore Raycast
            var m = go.AddComponent<ObjectiveMarker>();
            m._player = player;
            m.Build();
            return m;
        }

        private void Build()
        {
            _visual = Props.Node(transform, "Visual").transform;
            _gem = Props.Node(_visual, "Gem").transform;
            var glow = Mats.Emit(new Color(1f, 0.78f, 0.15f), 2.2f);
            Props.Box(_gem, new Vector3(0.2f, 0.2f, 0.2f), glow, Vector3.zero, new Vector3(45f, 0f, 45f), 0f, false);
            Props.Cyl(_visual, 0.1f, 0.0f, 0.18f, glow, new Vector3(0, -0.3f, 0), default, 12);
            _dist = Label3D.Create(_visual, "", 34f, new Color(1f, 0.9f, 0.55f), new Vector3(0, 0.3f, 0), true, 0f, true, 1.5f);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
            _visual.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            var sim = Game.Sim;
            bool hide = sim == null || _player == null || Game.World == null || Game.World.MenuMode || sim.PcOpen ||
                        (Game.Root != null && Game.Root.InputLocked);
            _retarget -= Time.deltaTime;
            if (!hide && _retarget <= 0f)
            {
                _retarget = 0.3f;
                _target = FindTarget(sim, _player);
            }
            if (hide || _target == null || _target.Kit == null)
            {
                if (_visual.gameObject.activeSelf) _visual.gameObject.SetActive(false);
                return;
            }
            var cam = _player.Cam != null ? _player.Cam.transform.position : _player.transform.position;
            var basePos = _target.transform.position + new Vector3(0, Mathf.Max(1.2f, _target.Kit.LabelH) + 0.55f, 0);
            float d = Vector3.Distance(new Vector3(cam.x, 0, cam.z), new Vector3(basePos.x, 0, basePos.z));
            if (d < 2.4f)
            {
                if (_visual.gameObject.activeSelf) _visual.gameObject.SetActive(false);
                return;
            }
            if (!_visual.gameObject.activeSelf) _visual.gameObject.SetActive(true);
            _t += Time.deltaTime;
            float scale = Mathf.Clamp(d / 9f, 0.8f, 2.4f);
            _visual.position = basePos + new Vector3(0, Mathf.Sin(_t * 2.4f) * 0.08f * scale, 0);
            _visual.localScale = Vector3.one * scale;
            _gem.localRotation = Quaternion.Euler(0, _t * 90f, 0);
            _dist.SetText(Mathf.RoundToInt(d) + " m");
        }

        // ---- Welche Station ist gerade gefragt? --------------------------------------------------------
        public static Station FindTarget(Sim sim, PlayerController p)
        {
            if (sim == null || p == null) return null;
            var held = p.Held;
            var kind = held?.Kind ?? ItemKind.None;
            if (sim.StoryStage == "diner")
            {
                if (sim.IntroStep == 1)
                {
                    if (kind == ItemKind.Plate) return Nearest(p, s => s.Type == StationType.DinerTable && s.Opts.TableId == held.Table);
                    return Nearest(p, s => s.Type == StationType.DinerPass);
                }
                return Nearest(p, s => s.Type == StationType.NpcTalk);
            }
            if (sim.StoryStage != "business") return null;
            int step = sim.TutorialStep;
            if (step >= 0 && step < GameData.Tutorial.Length)
            {
                switch (step)
                {
                    case 0:
                    case 1:
                    case 4:
                        return Nearest(p, s => s.Type == StationType.Pc);
                    case 2:
                    case 3:
                        if (kind == ItemKind.Crate) return RegalFor(p, held.Product);
                        return Nearest(p, s => s.Type == StationType.Dock);
                    case 5:
                        return null;
                    case 6:
                        if (kind == ItemKind.None) return Nearest(p, s => s.Type == StationType.Regal && sim.PendingCountFor(s.ProductId) > 0 && sim.StockQty(s.ProductId) > 0);
                        return ForHeld(sim, p);
                    default:
                        return ForHeld(sim, p);
                }
            }
            return ForHeld(sim, p);
        }

        private static Station ForHeld(Sim sim, PlayerController p)
        {
            var held = p.Held;
            if (held == null) return null;
            switch (held.Kind)
            {
                case ItemKind.Crate:
                    return RegalFor(p, held.Product);
                case ItemKind.Item:
                    if (held.ContractId > 0 || (held.OrderId <= 0 && sim.ContractAccepts(held))) return Nearest(p, s => s.Type == StationType.Pallet);
                    return Nearest(p, s => s.Type == StationType.Pack);
                case ItemKind.Package:
                    return Nearest(p, s => s.Type == StationType.Label);
                case ItemKind.Labeled:
                    if (p.CountCarried(ItemKind.Package) > 0) return Nearest(p, s => s.Type == StationType.Label);
                    return Nearest(p, s => s.Type == StationType.Ship || s.Type == StationType.Conveyor);
                case ItemKind.Return:
                    return Nearest(p, s => s.Type == StationType.ReturnDesk);
                case ItemKind.Plate:
                    return Nearest(p, s => s.Type == StationType.DinerTable && s.Opts.TableId == held.Table);
            }
            return null;
        }

        private static Station RegalFor(PlayerController p, string pid) =>
            Nearest(p, s => s.Type == StationType.Regal && s.ProductId == pid);

        private static Station Nearest(PlayerController p, System.Func<Station, bool> pred)
        {
            Station best = null;
            float bestD = float.MaxValue;
            var pos = p.transform.position;
            for (int i = Station.All.Count - 1; i >= 0; i--)
            {
                var s = Station.All[i];
                if (s == null)
                {
                    Station.All.RemoveAt(i);
                    continue;
                }
                bool ok;
                try
                {
                    ok = pred(s);
                }
                catch (System.Exception)
                {
                    ok = false;
                }
                if (!ok) continue;
                float d = (s.transform.position - pos).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }
    }

    /// <summary>v3.0: Spedition holt eine fertige Großauftrags-Palette ab (LKW auf der Straße).</summary>
    public static class SpeditionTruck
    {
        private const float RoadZ = -2.2f;

        /// <summary>Bewegt die Palette (ghost) an den Straßenrand, ein LKW hält, lädt und fährt davon.</summary>
        public static void Pickup(Transform ghost, Vector3 from, string company)
        {
            if (ghost == null || Game.World == null) return;
            var world = Game.World.transform;
            var curb = new Vector3(from.x, 0, -5.2f);
            var truck = Props.Van(world, new Color(0.18f, 0.35f, 0.7f), "Spedition").transform;
            truck.localScale = new Vector3(1.15f, 1.1f, 1.05f);
            float stopX = from.x;
            float startX = stopX + 70f;
            truck.position = new Vector3(startX, 0, RoadZ);
            truck.rotation = Quaternion.Euler(0, 180, 0);
            Label3D.Create(ghost, "Abholung: " + company, 34f, new Color(0.7f, 0.85f, 1f), new Vector3(0, 1.7f, 0), true, 30f, false, 1.2f, 20);
            // Die Palette steht (vom Hubwagen gebracht) am Straßenrand bereit.
            ghost.position = curb;
            Anim.Run(3.2f, t =>
            {
                if (truck != null) truck.position = new Vector3(Mathf.Lerp(startX, stopX, t), 0, RoadZ);
            }, () =>
            {
                if (truck == null) return;
                if (Game.Audio != null) Game.Audio.PlayAt("truck", truck.position, -2f);
                Anim.Run(0.8f, t =>
                {
                    if (ghost != null) ghost.position = Vector3.Lerp(curb, new Vector3(stopX - 0.6f, 0.9f, RoadZ), t);
                }, () =>
                {
                    if (ghost != null) Object.Destroy(ghost.gameObject);
                }, Ease.OutCubic, false, null, 0.6f);
                Anim.Run(4.5f, t =>
                {
                    if (truck != null) truck.position = new Vector3(Mathf.Lerp(stopX, stopX - 90f, t), 0, RoadZ);
                }, () =>
                {
                    if (truck != null) Object.Destroy(truck.gameObject);
                }, Ease.InQuad, false, null, 2.0f);
            }, Ease.OutQuad);
        }
    }
}
