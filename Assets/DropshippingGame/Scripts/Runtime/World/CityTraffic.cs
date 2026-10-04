using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Verkehr der großen Stadt: Autos, Busse, Lieferwagen, ab und zu Polizei – auf der Hauptstraße und
    /// gelegentlich in der Wohnstraße. Fahrzeuge folgen einander mit Abstand und bremsen für den Spieler.
    /// Modelle über "vehicle.*" (AssetLib), sonst prozedural. Ersetzt das alte SpawnCar des WorldBuilder.
    /// </summary>
    public sealed class CityTraffic : MonoBehaviour
    {
        public static bool Active;

        private sealed class Vehicle
        {
            public Transform Tr;
            public int Lane;
            public float Dir, X, Z, Speed, Cruise, Length, Honk;
            public GameObject Siren;
        }

        // Spuren: (z, Richtung, xStart, xEnde)
        private static readonly (float z, float dir, float x0, float x1)[] Lanes =
        {
            (WorldLots.LaneEastZ, 1f, -175f, 175f), (WorldLots.LaneWestZ, -1f, 175f, -175f),
            (WorldLots.ResidentialZ + 1.75f, 1f, WorldLots.ResidentialFromX + 1f, WorldLots.ResidentialToX - 1f),
            (WorldLots.ResidentialZ - 1.75f, -1f, WorldLots.ResidentialToX - 1f, WorldLots.ResidentialFromX + 1f),
        };

        private static readonly string[] CarIds =
        {
            "vehicle.traffic_1", "vehicle.traffic_2", "vehicle.traffic_3", "vehicle.traffic_4", "vehicle.traffic_5", "vehicle.car_sedan",
            "vehicle.car_hatchback", "vehicle.car_stationwagon", "vehicle.taxi", "vehicle.suv", "vehicle.sedan_sports", "vehicle.suv_luxury",
        };

        private readonly List<Vehicle> _cars = new List<Vehicle>();
        private Transform _root;
        private float _acc, _resAcc;

        private void OnEnable() => Active = true;
        private void OnDisable() => Active = false;

        public void Setup(Transform root)
        {
            _root = root;
            _acc = 1f;
            _resAcc = 8f;
        }

        private void Update()
        {
            if (_root == null) return;
            float dt = Time.deltaTime;
            _acc -= dt;
            if (_acc <= 0f)
            {
                _acc = Random.Range(2.5f, 6f);
                if (!StreetFestival.BlocksTraffic && _cars.Count < 14) Spawn(Random.value < 0.5f ? 0 : 1, null);
            }
            _resAcc -= dt;
            if (_resAcc <= 0f)
            {
                _resAcc = Random.Range(14f, 30f);
                if (_cars.Count < 14) Spawn(Random.value < 0.5f ? 2 : 3, null);
            }
            Vector3 pp = Game.Player != null ? Game.Player.transform.position : new Vector3(9999, 0, 9999);
            for (int i = _cars.Count - 1; i >= 0; i--)
            {
                var v = _cars[i];
                if (v.Tr == null) { _cars.RemoveAt(i); continue; }
                float target = v.Cruise;
                // Vordermann
                foreach (var o in _cars)
                {
                    if (o == v || o.Lane != v.Lane || o.Tr == null) continue;
                    float gap = (o.X - v.X) * v.Dir;
                    if (gap > 0f && gap < v.Length * 0.5f + o.Length * 0.5f + 6f) target = Mathf.Min(target, Mathf.Max(0f, o.Speed * (gap < v.Length + 2f ? 0f : 1f)));
                }
                // Spieler auf der Fahrbahn vor dem Auto
                float ahead = (pp.x - v.X) * v.Dir;
                bool blocked = Mathf.Abs(pp.z - v.Z) < 2.2f && ahead > 0f && ahead < v.Length * 0.5f + 7f;
                if (blocked)
                {
                    target = 0f;
                    v.Honk += dt;
                    if (v.Honk > 2.5f)
                    {
                        v.Honk = -4f;
                        if (Game.World != null) Game.World.SpawnFloatText(v.Tr.position + new Vector3(0, 2.4f, 0), Random.value < 0.5f ? "HUUUP!" : "Ey, Straße!", new Color(1f, 0.85f, 0.3f));
                    }
                }
                else v.Honk = Mathf.Min(v.Honk, 0f);
                v.Speed = Mathf.MoveTowards(v.Speed, target, (target < v.Speed ? 14f : 4f) * dt);
                v.X += v.Speed * v.Dir * dt;
                v.Tr.localPosition = new Vector3(v.X, 0f, v.Z);
                if (v.Siren != null) v.Siren.SetActive(Mathf.Repeat(Time.time * 3f, 1f) < 0.5f);
                var lane = Lanes[v.Lane];
                if ((v.X - lane.x1) * v.Dir > 0f)
                {
                    Destroy(v.Tr.gameObject);
                    _cars.RemoveAt(i);
                }
            }
        }

        /// <summary>Spawnt ein Fahrzeug. kind: null = zufällig, sonst "car", "bus", "van", "police", "truck".</summary>
        public void Spawn(int lane, string kind)
        {
            if (_root == null) return;
            lane = Mathf.Clamp(lane, 0, Lanes.Length - 1);
            var l = Lanes[lane];
            foreach (var o in _cars)
                if (o.Lane == lane && Mathf.Abs(o.X - l.x0) < 12f) return;
            if (kind == null)
            {
                float r = Random.value;
                kind = r < 0.62f ? "car" : r < 0.74f ? "bus" : r < 0.9f ? "van" : r < 0.95f ? "truck" : "police";
                if (lane >= 2 && kind == "bus") kind = "car";
            }
            GameObject go;
            float length = 4.4f, cruise = Random.Range(7f, 10f);
            GameObject siren = null;
            switch (kind)
            {
                case "bus": go = Bus(); length = 10f; cruise = 7f; break;
                case "van":
                    go = Random.value < 0.4f
                        ? Props.Van(_root, new Color(0.98f, 0.78f, 0.1f), "PaketBlitz", new Color(0.85f, 0.12f, 0.1f))
                        : (Props.Asset(_root, "Van", "vehicle.delivery", 5f, 90f) ?? Props.Van(_root, new Color(0.9f, 0.9f, 0.9f), "Blitzblank\nReinigung"));
                    length = 5.2f;
                    break;
                case "truck": go = Props.Asset(_root, "Truck", "vehicle.truck", 7f, 90f) ?? Props.Van(_root, new Color(0.3f, 0.35f, 0.4f), "Spedition"); length = 7f; cruise = 7f; break;
                case "police":
                    go = Props.Asset(_root, "Police", Random.value < 0.5f ? "vehicle.police" : "vehicle.car_police", -1f, 90f) ?? Props.Car(_root, new Color(0.15f, 0.3f, 0.7f));
                    siren = Props.Box(go.transform, new Vector3(0.3f, 0.15f, 0.8f), Mats.Emit(new Color(0.2f, 0.4f, 1f), 4f), new Vector3(0, 1.75f, 0), default, 0f, false);
                    cruise = 11f;
                    break;
                default:
                    string id = CarIds[Random.Range(0, CarIds.Length)];
                    go = Props.Car(_root, Random.ColorHSV(0f, 1f, 0.3f, 0.9f, 0.3f, 0.95f), false, AssetLib.HasModel(id) ? id : null);
                    break;
            }
            if (lane >= 2) cruise *= 0.55f;
            go.transform.localPosition = new Vector3(l.x0, 0, l.z);
            go.transform.localRotation = Quaternion.Euler(0, l.dir > 0 ? 0f : 180f, 0);
            _cars.Add(new Vehicle { Tr = go.transform, Lane = lane, Dir = l.dir, X = l.x0, Z = l.z, Speed = cruise, Cruise = cruise, Length = length, Siren = siren });
        }

        /// <summary>Linienbus (Front +X): Modell "vehicle.bus", sonst prozedural.</summary>
        private GameObject Bus()
        {
            var a = Props.Asset(_root, "Bus", "vehicle.bus", 10f, 90f);
            if (a != null) return a;
            var n = Props.Node(_root, "Bus");
            var t = n.transform;
            Props.Box(t, new Vector3(10f, 2.6f, 2.5f), Mats.Std(new Color(0.95f, 0.85f, 0.2f), 0.4f, 0.2f), new Vector3(0, 1.75f, 0), default, 0.1f);
            Props.Box(t, new Vector3(8.6f, 0.9f, 2.52f), Mats.Std(new Color(0.12f, 0.16f, 0.22f), 0.08f, 0.5f), new Vector3(-0.4f, 2.2f, 0), default, 0f);
            Props.Box(t, new Vector3(0.05f, 1.3f, 2.2f), Mats.Std(new Color(0.12f, 0.16f, 0.22f), 0.08f, 0.5f), new Vector3(5.01f, 2f, 0), default, 0f);
            foreach (float fx in new[] { 3.4f, -3.2f })
            foreach (float fz in new[] { 1.1f, -1.1f })
                Props.Cyl(t, 0.5f, 0.5f, 0.3f, Mats.Std(new Color(0.06f, 0.06f, 0.07f), 0.8f), new Vector3(fx, 0.5f, fz), new Vector3(90, 0, 0), 14);
            foreach (float side in new[] { 1f, -1f })
            {
                var h = Props.Node(t, "Side", new Vector3(0, 1.1f, side * 1.27f), side > 0 ? 0f : 180f);
                Label3D.Create(h.transform, "Linie 7 · Hustle-Express", 90f, new Color(0.15f, 0.15f, 0.2f), Vector3.zero, false);
            }
            return n;
        }
    }

    /// <summary>
    /// Müllabfuhr: Der Müllwagen fährt regelmäßig (09:30 und 15:00 Uhr Spielzeit, oder per Admin) die
    /// Hauptstraße ab, hält an den Mülleimern, hebt sie an und leert sie. Löst
    /// <see cref="CityServices.GarbageCollected"/> je Eimer aus.
    /// </summary>
    public sealed class GarbageTruck : MonoBehaviour
    {
        public static GarbageTruck Instance;
        private Transform _root;
        private bool _running;
        private float _lastMinutes = -1f;
        private static readonly float[] Times = { 9.5f * 60f, 15f * 60f };

        public void Setup(Transform root)
        {
            _root = root;
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            var sim = Game.Sim;
            if (sim == null || !sim.InGame || _running) return;
            float m = sim.TimeMinutes;
            if (_lastMinutes >= 0f)
                foreach (float t in Times)
                    if (_lastMinutes < t && m >= t) StartRound();
            _lastMinutes = m;
        }

        public void StartRound()
        {
            if (_running || _root == null) return;
            StartCoroutine(Round());
        }

        private GameObject BuildTruck()
        {
            var a = Props.Asset(_root, "GarbageTruck", "vehicle.garbage_truck", 7f, 90f);
            if (a != null) return a;
            var n = Props.Node(_root, "GarbageTruck");
            var t = n.transform;
            var orange = Mats.Std(new Color(0.95f, 0.5f, 0.1f), 0.5f);
            Props.Box(t, new Vector3(4.6f, 2.6f, 2.4f), orange, new Vector3(-1f, 1.7f, 0), default, 0.1f);
            Props.Box(t, new Vector3(1.8f, 2f, 2.3f), Mats.Std(new Color(0.9f, 0.9f, 0.88f), 0.4f), new Vector3(2.4f, 1.4f, 0), default, 0.1f);
            Props.Box(t, new Vector3(0.05f, 0.8f, 2f), Mats.Std(new Color(0.12f, 0.16f, 0.22f), 0.08f, 0.5f), new Vector3(3.31f, 1.8f, 0), default, 0f);
            foreach (float fx in new[] { 2.3f, -2.4f })
            foreach (float fz in new[] { 1.05f, -1.05f })
                Props.Cyl(t, 0.5f, 0.5f, 0.3f, Mats.Std(new Color(0.06f, 0.06f, 0.07f), 0.8f), new Vector3(fx, 0.5f, fz), new Vector3(90, 0, 0), 14);
            foreach (float side in new[] { 1f, -1f })
            {
                var h = Props.Node(t, "Side", new Vector3(-1f, 1.8f, side * 1.22f), side > 0 ? 0f : 180f);
                Label3D.Create(h.transform, "Stadtreinigung\n\"Wir holen alles ab\"", 80f, Color.white, Vector3.zero, false);
            }
            return n;
        }

        private System.Collections.IEnumerator Round()
        {
            _running = true;
            int emptied = 0;
            foreach (float dir in new[] { 1f, -1f })
            {
                var truck = BuildTruck();
                if (truck == null) break;
                var tr = truck.transform;
                float z = dir > 0 ? WorldLots.LaneEastZ : WorldLots.LaneWestZ;
                float x = -dir * 170f, end = dir * 170f;
                tr.localPosition = new Vector3(x, 0, z);
                tr.localRotation = Quaternion.Euler(0, dir > 0 ? 0f : 180f, 0);
                var stops = new List<TrashBinInfo>();
                foreach (var b in CityServices.Bins)
                    if (b.Node != null && Mathf.Abs(b.Position.z) < 10f && Mathf.Sign(b.Position.z) == Mathf.Sign(z)) stops.Add(b);
                stops.Sort((p, q) => (p.Position.x * dir).CompareTo(q.Position.x * dir));
                foreach (var b in stops)
                {
                    float sx = b.Position.x;
                    while (tr != null && (sx - x) * dir > 0.2f)
                    {
                        float speed = (sx - x) * dir < 6f ? 3f : 9f;
                        if (Game.Player != null)
                        {
                            var pp = Game.Player.transform.position;
                            float ahead = (pp.x - x) * dir;
                            if (Mathf.Abs(pp.z - z) < 2.2f && ahead > 0f && ahead < 8f) speed = 0f;
                        }
                        x += speed * dir * Time.deltaTime;
                        tr.localPosition = new Vector3(x, 0, z);
                        yield return null;
                    }
                    if (tr == null) break;
                    yield return Empty(b);
                    emptied++;
                }
                while (tr != null && (end - x) * dir > 0f)
                {
                    x += 10f * dir * Time.deltaTime;
                    tr.localPosition = new Vector3(x, 0, z);
                    yield return null;
                }
                if (tr != null) Destroy(tr.gameObject);
            }
            CityServices.RaiseRoundFinished(emptied);
            _running = false;
        }

        private System.Collections.IEnumerator Empty(TrashBinInfo b)
        {
            if (b == null || b.Node == null) yield break;
            var n = b.Node;
            Vector3 p0 = n.localPosition;
            Quaternion r0 = n.localRotation;
            if (Game.Audio != null) Game.Audio.PlayAt("metal_clank", n.position, -6f);
            for (float t = 0f; t < 1f; t += Time.deltaTime * 1.6f)
            {
                if (n == null) yield break;
                float k = Mathf.Sin(t * Mathf.PI);
                n.localPosition = p0 + new Vector3(0, k * 1.2f, 0);
                n.localRotation = r0 * Quaternion.Euler(k * 70f, 0, 0);
                yield return null;
            }
            if (n == null) yield break;
            n.localPosition = p0;
            n.localRotation = r0;
            float amount = b.Fill;
            b.Fill = 0f;
            CityServices.UpdateVisual(b);
            CityServices.RaiseCollected(b, amount);
        }
    }
}
