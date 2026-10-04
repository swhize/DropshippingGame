using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Baut die gekaufte Ausrüstung in Garage/Lagerhalle: zusätzliche Packtische und Labeldrucker
    /// (normale <see cref="Station"/>s), Faltmaschine und Mülltonnen (<see cref="EquipmentProp"/>).
    /// Alles ist mit Taste B verschiebbar (<see cref="Movable"/>, Schlüssel wie die Stationen).
    /// Wird am Ende von WorldBuilder.RebuildStations aufgerufen und baut bei Käufen neu.
    /// </summary>
    public sealed class EquipmentWorld : MonoBehaviour
    {
        private static readonly Vector3[] GaragePack = { new Vector3(-18.2f, 0, -12.6f) };
        private static readonly Vector3[] GarageLabel = { new Vector3(-17.6f, 0, -14.6f) };
        private static readonly Vector3[] WarePack = { new Vector3(22f, 0, -20f), new Vector3(8f, 0, -16.8f) };
        private static readonly Vector3[] WareLabel = { new Vector3(26f, 0, -20f), new Vector3(13f, 0, -16.8f) };
        private static readonly Vector3 GarageFold = new Vector3(-21.3f, 0, -14.6f);
        private static readonly Vector3 WareFold = new Vector3(5f, 0, -20f);

        /// <summary>Wo die Mülltonnen stehen (für den Müllwagen der Welt).</summary>
        public static Vector3 BinPosition(int stage) => stage >= 1 ? new Vector3(33.2f, 0, -11.5f) : new Vector3(-8.6f, 0, -7.6f);

        private Sim _sim;

        public static void Build(Transform stations, int stage)
        {
            var sim = Game.Sim;
            if (stations == null || sim == null) return;
            try
            {
                var host = Props.Node(stations, "EquipmentWorld");
                host.AddComponent<EquipmentWorld>().Hook(sim);
                var pack = stage >= 1 ? WarePack : GaragePack;
                var label = stage >= 1 ? WareLabel : GarageLabel;
                for (int i = 0; i < sim.EquipmentActive("packtisch") && i < pack.Length; i++)
                    AddStation(stations, StationType.Pack, pack[i], stage, i + 1);
                for (int i = 0; i < sim.EquipmentActive("labeldrucker") && i < label.Length; i++)
                    AddStation(stations, StationType.Label, label[i], stage, i + 1);
                if (sim.HasEquipment("faltmaschine"))
                    AddProp(stations, EquipmentProp.Kind.FoldMachine, stage >= 1 ? WareFold : GarageFold, stage >= 1 ? 0f : 90f, stage, "eq:" + stage + ":falt");
                AddProp(stations, EquipmentProp.Kind.Bins, BinPosition(stage), stage >= 1 ? -90f : 0f, stage, "");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void AddStation(Transform parent, StationType type, Vector3 pos, int stage, int index)
        {
            var go = Props.Node(parent, type + "_extra" + index, pos, 0f);
            var st = go.AddComponent<Station>();
            st.Setup(type, new StationOpts { Stage = stage, ProductIndex = index });
            string key = WorldBuilder.StationKey(stage, type, index);
            ApplyPlacement(go.transform, key);
            if (st.Kit != null) go.AddComponent<Movable>().Setup(key, st.Title + " " + (index + 1), stage, false, st.Kit.Size, st.Kit.Center);
        }

        private static void AddProp(Transform parent, EquipmentProp.Kind kind, Vector3 pos, float rot, int stage, string key)
        {
            var go = Props.Node(parent, kind.ToString(), pos, rot);
            var p = go.AddComponent<EquipmentProp>();
            p.Setup(kind);
            if (string.IsNullOrEmpty(key)) return; // Tonnen stehen draußen fest (Müllwagen)
            ApplyPlacement(go.transform, key);
            go.AddComponent<Movable>().Setup(key, p.Title, stage, false, p.Size, new Vector3(0, p.Size.y / 2f, 0));
        }

        private static void ApplyPlacement(Transform t, string key)
        {
            var p = Game.Sim?.GetFurniture(key);
            if (p == null || t == null) return;
            t.localPosition = new Vector3(p.X, t.localPosition.y, p.Z);
            t.localRotation = Quaternion.Euler(0f, p.RotY, 0f);
        }

        private void Hook(Sim sim)
        {
            _sim = sim;
            _sim.EquipmentChanged += OnEquipment;
        }

        private void OnEquipment()
        {
            if (Game.World != null) Game.World.RebuildStations();
        }

        private void OnDestroy()
        {
            if (_sim != null) _sim.EquipmentChanged -= OnEquipment;
        }
    }

    /// <summary>Faltmaschine oder Mülltonnen: einfache, anvisierbare Ausrüstung mit Statustext.</summary>
    public sealed class EquipmentProp : MonoBehaviour, IInteractable
    {
        public enum Kind
        {
            FoldMachine,
            Bins,
        }

        public Kind Type;
        public Vector3 Size = Vector3.one;
        private readonly Highlighter _hl = new Highlighter();
        private Transform _fill;
        private Sim _sim;
        private float _confirmUntil = -10f;

        public void Setup(Kind kind)
        {
            Type = kind;
            var visual = Props.Node(transform, "Visual").transform;
            if (kind == Kind.FoldMachine) BuildFoldMachine(visual);
            else BuildBins(visual);
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = Size;
            col.center = new Vector3(0, Size.y / 2f, 0);
            _hl.Collect(visual);
            _sim = Game.Sim;
            if (_sim != null && kind == Kind.Bins) _sim.WasteChanged += RefreshFill;
            RefreshFill();
        }

        private void OnDestroy()
        {
            if (_sim != null) _sim.WasteChanged -= RefreshFill;
        }

        private void BuildFoldMachine(Transform v)
        {
            Size = new Vector3(1.1f, 1.3f, 0.9f);
            var body = Mats.Std(new Color(0.85f, 0.55f, 0.15f), 0.5f, 0.3f);
            var dark = Mats.Std(new Color(0.18f, 0.18f, 0.2f), 0.6f, 0.4f);
            Props.Box(v, new Vector3(1.1f, 0.9f, 0.9f), body, new Vector3(0, 0.45f, 0));
            Props.Box(v, new Vector3(0.9f, 0.08f, 0.6f), dark, new Vector3(0, 0.94f, 0));
            Props.Box(v, new Vector3(0.6f, 0.3f, 0.05f), Mats.Std(new Color(0.75f, 0.6f, 0.4f)), new Vector3(0, 1.1f, -0.2f), new Vector3(-20, 0, 0));
            Props.Cyl(v, 0.06f, 0.06f, 0.95f, dark, new Vector3(0, 0.75f, 0.47f), new Vector3(0, 0, 90));
            Label3D.Create(v, "FALTOMAT 3000", 18f, Color.white, new Vector3(0, 0.6f, 0.46f), false);
        }

        private void BuildBins(Transform v)
        {
            Size = new Vector3(2.2f, 1.3f, 0.9f);
            var green = Mats.Std(new Color(0.2f, 0.45f, 0.25f), 0.7f);
            var blue = Mats.Std(new Color(0.15f, 0.3f, 0.6f), 0.7f);
            for (int i = 0; i < 2; i++)
            {
                float x = -0.55f + i * 1.1f;
                if (Props.AssetAt(v, i == 0 ? "street.trash_a" : "street.trash_b", new Vector3(x, 0, 0), 0f, 1.1f) == null)
                {
                    Props.Box(v, new Vector3(0.8f, 1.05f, 0.8f), i == 0 ? blue : green, new Vector3(x, 0.52f, 0));
                    Props.Box(v, new Vector3(0.86f, 0.08f, 0.86f), i == 0 ? blue : green, new Vector3(x, 1.08f, 0));
                }
            }
            // Säcke neben den Tonnen zeigen den Füllstand (wachsen über 60 %).
            _fill = Props.Node(v, "Fill").transform;
            var bag = Mats.Std(new Color(0.08f, 0.08f, 0.09f), 0.35f);
            Props.Sphere(_fill, 0.35f, bag, new Vector3(0, 0.3f, 0.75f));
            Props.Sphere(_fill, 0.3f, bag, new Vector3(0.5f, 0.26f, 0.8f));
            Props.Sphere(_fill, 0.28f, bag, new Vector3(-0.5f, 0.25f, 0.78f));
            if (Game.Sim != null && Game.Sim.HasEquipment("container"))
            {
                if (Props.AssetAt(v, "street.dumpster", new Vector3(2.1f, 0, 0), 0f, 1.8f) == null)
                    Props.Box(v, new Vector3(1.8f, 1.3f, 1.1f), green, new Vector3(2.1f, 0.65f, 0));
            }
        }

        private void RefreshFill()
        {
            if (_fill == null || Game.Sim == null) return;
            float f = Game.Sim.WasteFill;
            float s = Mathf.Clamp01((f - 0.6f) / 0.6f);
            _fill.gameObject.SetActive(s > 0.01f);
            _fill.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.2f, s);
        }

        public string Title => Type == Kind.FoldMachine ? "Faltmaschine" : "Mülltonnen";

        public string Prompt(PlayerController player)
        {
            var s = Game.Sim;
            if (s == null) return "";
            if (Type == Kind.FoldMachine)
                return s.FlatTotal() > 0 ? "Faltmaschine: faltet automatisch (" + s.FlatTotal() + " ungefaltet)" : "Faltmaschine: keine ungefalteten Kartons";
            string st = "Müll " + s.Waste + "/" + s.WasteCapacity() + (s.WasteFull ? " – VOLL!" : "") + " · Abholung " + s.NextGarbagePickupText();
            if (s.GarbagePending) return st + " · Müllwagen kommt";
            if (s.WasteFill >= 0.5f) return st + " · " + GameInput.KeyLabel("interact") + ": Sonderabholung (" + Fmt.Money(s.SpecialPickupCost()) + ")";
            return st;
        }

        public void Interact(PlayerController player)
        {
            var s = Game.Sim;
            if (s == null) return;
            if (Type == Kind.FoldMachine)
            {
                if (player != null && player.Held == null && s.FlatTotal() > 0) s.FoldCarton();
                else s.Notify("Die Faltmaschine arbeitet von allein. Ungefaltete Kartons kaufst du günstiger bei AllesExpress.", "info");
                return;
            }
            if (s.WasteFill < 0.5f || s.GarbagePending)
            {
                s.Notify("Müll: " + s.Waste + "/" + s.WasteCapacity() + ". Nächste Abholung: " + s.NextGarbagePickupText() + ".", "info");
                return;
            }
            if (Time.unscaledTime > _confirmUntil)
            {
                _confirmUntil = Time.unscaledTime + 3f;
                s.Notify("Nochmal " + GameInput.KeyLabel("interact") + " = Sonderabholung für " + Fmt.Money(s.SpecialPickupCost()) + ".", "info");
                return;
            }
            _confirmUntil = -10f;
            s.OrderGarbagePickup();
        }

        public void SetHighlighted(bool on) => _hl.Set(on);
    }
}
