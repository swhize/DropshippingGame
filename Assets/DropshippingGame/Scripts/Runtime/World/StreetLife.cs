using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Passanten-KI: Leute tauchen am Kartenrand auf, besuchen zufällig ein paar
    /// Orte (Schaufenster, Plakatwand, Parkbänke ...), bleiben stehen, schauen aufs
    /// Handy, quatschen miteinander und verschwinden wieder am Rand. Jede Figur hat
    /// eigenes Tempo und eigene Pausen, damit kein erkennbarer Loop entsteht.
    /// Laufwege folgen Gehwegen, Zebrastreifen und Parkwegen; Hindernisse umgeht <see cref="NpcNav"/>.
    /// </summary>
    public sealed class StreetLife : MonoBehaviour
    {
        // Zonen: 0 = Gehweg Süd (Läden), 1 = Gehweg Nord, 2 = Parkweg
        private const int South = 0, North = 1, ParkPath = 2;

        private static readonly float[] LaneMin = { -6.45f, 4.55f, 11.75f };
        private static readonly float[] LaneMax = { -4.55f, 6.45f, 13.25f };
        private static readonly float[] EdgeWest = { -60f, -60f, -56f };
        private static readonly float[] EdgeEast = { 52f, 52f, 47f };
        private static readonly float[] ParkConnectors = { -40f, -14f, 12f, 38f };
        private const float CrossMin = -37.2f, CrossMax = -32.8f;

        private struct Poi
        {
            public int Zone;
            public Vector3 Pos;
            public Vector3 Look; // Weltpunkt, den man anschaut
            public float MinPause, MaxPause;
        }

        private sealed class Step
        {
            public Vector3 Pos;
            public float Pause;
            public bool HasLook;
            public Vector3 Look;
            public bool Phone;
        }

        private sealed class Walker
        {
            public NPC Npc;
            public readonly Queue<Step> Steps = new Queue<Step>();
            public float BaseSpeed;
            public float ChatCooldown;
        }

        private static readonly string[] PhoneLines =
        {
            "Wo bleibt mein Paket?!", "Sendungsverfolgung sagt: morgen.", "Oh, Rabattcode!", "Noch 3 Stück auf Lager?!",
            "Hmm, 4,8 Sterne ...", "Wer schreibt mir da?", "Akku fast leer ...", "Schon wieder Werbung.",
        };

        private static readonly string[][] ChatLines =
        {
            new[] { "Hey! Lange nicht gesehen!", "Ja, echt! Alles gut?" },
            new[] { "Hast du den neuen Laden gesehen?", "Die verschicken voll schnell!" },
            new[] { "Schönes Wetter heute.", "Endlich mal!" },
            new[] { "Kennst du den Stand da?", "Klar, hab da was gekauft." },
            new[] { "Na, auch unterwegs?", "Muss noch was abholen." },
        };

        private static readonly string[] LookLines = { "Hübsch.", "Hmm ...", "Brauch ich das?", "Nicht schlecht!" };

        private readonly List<Poi> _pois = new List<Poi>();
        private readonly List<Walker> _walkers = new List<Walker>();
        private List<NPC> _registry;
        private Transform _parent;
        private System.Random _rng;
        private int _target = 9;
        private float _spawnTimer;
        private float _navTimer;
        private float _chatTimer;
        private Transform _navRoot;

        /// <summary>Startet die Passanten. <paramref name="registry"/> wird mit den aktiven NPCs gepflegt (Verkaufsstand-Logik).</summary>
        public void Setup(Transform parent, Transform navRoot, List<NPC> registry, int count)
        {
            _parent = parent;
            _navRoot = navRoot;
            _registry = registry ?? new List<NPC>();
            _target = Mathf.Max(1, count);
            _rng = new System.Random(unchecked(System.Environment.TickCount * 31 + 7));
            BuildPois();
            RebuildNav();
            _navTimer = 0.5f; // Stationen werden erst nach den Passanten gebaut
            // Anfangs schon mitten im Geschehen verteilt, damit die Straße nicht leer startet.
            for (int i = 0; i < _target; i++) Spawn(true);
            _spawnTimer = R(2f, 6f);
        }

        private float R(float a, float b) => a + (float)_rng.NextDouble() * (b - a);
        private bool Chance(float p) => _rng.NextDouble() < p;

        private void BuildPois()
        {
            _pois.Clear();
            // Schaufenster / Fassaden am südlichen Gehweg (Blick Richtung Gebäude)
            foreach (float x in new[] { -44f, -39f, -34.5f, -27f, -18f, -13.5f, -2f, 4f, 10f, 17f, 24f, 30f, 36f })
                AddPoi(South, new Vector3(x, 0, -6.35f), new Vector3(x, 1.5f, -9f), 2.5f, 7f);
            // Plakatwand (eigene Marke) vom Nordgehweg und vom Parkweg aus
            AddPoi(North, new Vector3(19f, 0, 6.3f), new Vector3(20f, 2f, 8.4f), 3f, 7f);
            AddPoi(North, new Vector3(21.5f, 0, 6.2f), new Vector3(20f, 2f, 8.4f), 3f, 6f);
            // Straße / Verkehr beobachten
            foreach (float x in new[] { -47f, -25f, -6f, 30f })
                AddPoi(North, new Vector3(x, 0, 4.7f), new Vector3(x, 1f, 0f), 2f, 5f);
            // Parkbänke und Parkblick
            foreach (float bx in new[] { -44f, -20f, 4f, 28f })
                AddPoi(ParkPath, new Vector3(bx + 0.4f, 0, 13.3f), new Vector3(bx, 1f, 8f), 4f, 10f);
            foreach (float x in new[] { -30f, -8f, 24f, 40f })
                AddPoi(ParkPath, new Vector3(x, 0, 12.9f), new Vector3(x, 1.2f, 20f), 3f, 8f);
        }

        private void AddPoi(int zone, Vector3 pos, Vector3 look, float minP, float maxP)
        {
            _pois.Add(new Poi { Zone = zone, Pos = pos, Look = look, MinPause = minP, MaxPause = maxP });
        }

        private void RebuildNav()
        {
            try { NpcNav.Rebuild(_navRoot, _parent); }
            catch (System.Exception e) { Debug.LogWarning("NpcNav: " + e.Message); }
        }

        private void Update()
        {
            if (_parent == null || _rng == null) return;
            float dt = Time.deltaTime;
            _navTimer -= dt;
            if (_navTimer <= 0f)
            {
                // Stationen/Deko können sich ändern (Umzug, Upgrades) -> Hindernisse regelmäßig auffrischen.
                _navTimer = 4f;
                RebuildNav();
            }
            for (int i = _walkers.Count - 1; i >= 0; i--)
            {
                var w = _walkers[i];
                if (w.Npc == null) { _walkers.RemoveAt(i); continue; }
                if (w.ChatCooldown > 0f) w.ChatCooldown -= dt;
                // Sicherheitsnetz: Figur ohne Ziel und ohne Pause -> weiter im Plan
                if (!w.Npc.HasDestination && !w.Npc.IsWaiting) Advance(w);
            }
            _registry.RemoveAll(n => n == null);
            if (_walkers.Count < _target)
            {
                _spawnTimer -= dt;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = R(1.5f, 9f);
                    Spawn(false);
                }
            }
            _chatTimer -= dt;
            if (_chatTimer <= 0f)
            {
                _chatTimer = 0.5f;
                TryChat();
            }
        }

        // ---------------------------------------------------------------------------------
        // Spawnen / Route
        // ---------------------------------------------------------------------------------
        private void Spawn(bool anywhere)
        {
            var go = Props.Node(_parent, "Pedestrian");
            var npc = go.AddComponent<NPC>();
            var look = CharacterKit.RandomLook(_rng);
            if (look != null) look.Sitting = false;
            var w = new Walker { Npc = npc, BaseSpeed = Chance(0.06f) ? R(2.2f, 2.6f) : R(0.9f, 1.55f), ChatCooldown = R(5f, 20f) };
            npc.Setup(look, null, w.BaseSpeed);
            int zone = PickZone();
            bool fromWest = Chance(0.5f);
            float x = anywhere ? R(EdgeWest[zone] + 8f, EdgeEast[zone] - 8f) : (fromWest ? EdgeWest[zone] : EdgeEast[zone]);
            Vector3 start = new Vector3(x, 0, R(LaneMin[zone], LaneMax[zone]));
            if (NpcNav.Blocked(start)) start.z = zone == South ? -4.6f : (LaneMin[zone] + LaneMax[zone]) * 0.5f;
            go.transform.localPosition = start;
            go.transform.localRotation = Quaternion.Euler(0, fromWest ? 90f : -90f, 0);
            PlanRoute(w, zone, x, fromWest);
            _walkers.Add(w);
            _registry.Add(npc);
            if (anywhere) npc.StopFacing(R(0f, 3f), start + new Vector3(R(-1f, 1f), 0, R(-1f, 1f)));
            Advance(w);
        }

        private int PickZone()
        {
            double r = _rng.NextDouble();
            return r < 0.45 ? South : r < 0.8 ? North : ParkPath;
        }

        private void PlanRoute(Walker w, int zone, float x, bool headingEast)
        {
            w.Steps.Clear();
            int stops = _rng.Next(0, 4);
            for (int i = 0; i < stops && _pois.Count > 0; i++)
            {
                var poi = _pois[_rng.Next(_pois.Count)];
                Vector3 p = poi.Pos + new Vector3(R(-0.7f, 0.7f), 0, R(-0.15f, 0.15f));
                Travel(w, ref zone, ref x, poi.Zone, p.x);
                var st = new Step { Pos = p, HasLook = true, Look = poi.Look + new Vector3(R(-1f, 1f), 0, 0), Pause = R(poi.MinPause, poi.MaxPause) };
                w.Steps.Enqueue(st);
                x = p.x;
            }
            // Zufällige Handy-Pause unterwegs
            if (Chance(0.35f))
            {
                float px = Mathf.Clamp(x + R(-12f, 12f), EdgeWest[zone] + 4f, EdgeEast[zone] - 4f);
                Travel(w, ref zone, ref x, zone, px);
                w.Steps.Enqueue(new Step { Pos = new Vector3(px, 0, R(LaneMin[zone], LaneMax[zone])), Pause = R(3f, 9f), Phone = true });
            }
            // Ausgang: meist in Laufrichtung weiter, manchmal umkehren, manchmal Zone wechseln
            int exitZone = Chance(0.7f) ? zone : PickZone();
            bool east = Chance(0.75f) ? headingEast : !headingEast;
            float ex = east ? EdgeEast[exitZone] + 2f : EdgeWest[exitZone] - 2f;
            Travel(w, ref zone, ref x, exitZone, ex);
        }

        private float Lane(int zone) => R(LaneMin[zone], LaneMax[zone]);

        /// <summary>Wegpunkte von (zone, x) nach (targetZone, targetX) über Zebrastreifen/Parkzugänge.</summary>
        private void Travel(Walker w, ref int zone, ref float x, int targetZone, float targetX)
        {
            int guard = 0;
            while (zone != targetZone && guard++ < 4)
            {
                if (zone == South || (zone == North && targetZone == South))
                {
                    // Straße nur am Zebrastreifen queren, vorher kurz umschauen
                    float cx = R(CrossMin, CrossMax);
                    Walk(w, zone, x, cx);
                    float curbFrom = zone == South ? -4.5f : 4.5f;
                    w.Steps.Enqueue(new Step { Pos = new Vector3(cx, 0, curbFrom), Pause = R(0.4f, 2f), HasLook = true, Look = new Vector3(cx + (Chance(0.5f) ? -8f : 8f), 1f, 0f) });
                    int to = zone == South ? North : South;
                    w.Steps.Enqueue(new Step { Pos = new Vector3(cx + R(-0.6f, 0.6f), 0, -curbFrom) });
                    zone = to;
                    x = cx;
                }
                else
                {
                    // Nord <-> Park über den nächstgelegenen Parkzugang
                    float mid = (x + targetX) * 0.5f;
                    float best = ParkConnectors[0];
                    foreach (float c in ParkConnectors)
                        if (Mathf.Abs(c - mid) < Mathf.Abs(best - mid)) best = c;
                    float cx = best + R(-0.5f, 0.5f);
                    int to = zone == North ? ParkPath : North;
                    Walk(w, zone, x, cx);
                    w.Steps.Enqueue(new Step { Pos = new Vector3(cx, 0, zone == North ? 6.4f : 11.8f) });
                    w.Steps.Enqueue(new Step { Pos = new Vector3(cx + R(-0.3f, 0.3f), 0, to == ParkPath ? 11.8f : 6.4f) });
                    zone = to;
                    x = cx;
                }
            }
            Walk(w, zone, x, targetX);
            x = targetX;
        }

        /// <summary>Entlang einer Zone laufen, mit leichtem Schlendern zwischen den Spurrändern.</summary>
        private void Walk(Walker w, int zone, float fromX, float toX)
        {
            float dist = Mathf.Abs(toX - fromX);
            int segs = Mathf.Max(1, Mathf.RoundToInt(dist / R(9f, 16f)));
            for (int i = 1; i <= segs; i++)
            {
                float px = Mathf.Lerp(fromX, toX, (float)i / segs);
                w.Steps.Enqueue(new Step { Pos = new Vector3(px, 0, Lane(zone)) });
            }
        }

        /// <summary>Nächsten Schritt starten; leerer Plan = Kartenrand erreicht -> verschwinden.</summary>
        private void Advance(Walker w)
        {
            if (w.Npc == null) return;
            if (w.Steps.Count == 0)
            {
                Despawn(w);
                return;
            }
            var st = w.Steps.Dequeue();
            w.Npc.Speed = w.BaseSpeed * R(0.9f, 1.1f);
            w.Npc.GoTo(st.Pos, n => OnArrive(w, st));
        }

        private void OnArrive(Walker w, Step st)
        {
            if (w.Npc == null) return;
            if (st.Pause > 0f)
            {
                if (st.Phone)
                {
                    var p = w.Npc.transform.position;
                    w.Npc.StopFacing(st.Pause, p + w.Npc.transform.forward * 2f + new Vector3(R(-1f, 1f), 0, R(-1f, 1f)));
                    if (Chance(0.45f)) w.Npc.Say(PhoneLines[_rng.Next(PhoneLines.Length)], Mathf.Min(st.Pause, 3.5f));
                }
                else
                {
                    Vector3 look = st.HasLook && _parent != null ? _parent.TransformPoint(st.Look) : w.Npc.transform.position + w.Npc.transform.forward;
                    w.Npc.StopFacing(st.Pause, look);
                    if (st.Pause > 3f && Chance(0.12f)) w.Npc.Say(LookLines[_rng.Next(LookLines.Length)], 2.5f);
                }
            }
            Advance(w);
        }

        private void Despawn(Walker w)
        {
            _walkers.Remove(w);
            if (w.Npc != null)
            {
                _registry.Remove(w.Npc);
                Destroy(w.Npc.gameObject);
            }
            w.Npc = null;
        }

        // ---------------------------------------------------------------------------------
        // Unterhaltungen
        // ---------------------------------------------------------------------------------
        private void TryChat()
        {
            for (int i = 0; i < _walkers.Count; i++)
            {
                var a = _walkers[i];
                if (a.Npc == null || a.ChatCooldown > 0f || !a.Npc.IsWalking) continue;
                for (int j = i + 1; j < _walkers.Count; j++)
                {
                    var b = _walkers[j];
                    if (b.Npc == null || b.ChatCooldown > 0f || !b.Npc.IsWalking) continue;
                    Vector3 d = a.Npc.transform.position - b.Npc.transform.position;
                    d.y = 0f;
                    if (d.sqrMagnitude > 1.8f * 1.8f) continue;
                    a.ChatCooldown = b.ChatCooldown = R(25f, 60f);
                    if (!Chance(0.3f)) continue;
                    float dur = R(3.5f, 7f);
                    a.Npc.Stop(dur, b.Npc.transform);
                    b.Npc.Stop(dur, a.Npc.transform);
                    var lines = ChatLines[_rng.Next(ChatLines.Length)];
                    a.Npc.Say(lines[0], 2.6f);
                    StartCoroutine(SayLater(b.Npc, lines[1], 1.8f));
                    return;
                }
            }
        }

        private System.Collections.IEnumerator SayLater(NPC npc, string text, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (npc != null) npc.Say(text, 2.6f);
        }
    }
}
