using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame.Vehicles
{
    /// <summary>
    /// Hängt an der Spielwelt (nicht im Menü): stellt das Moped ab der Business-Phase hin (gespeicherter
    /// Standort oder Stammplatz vor der Garage bzw. Halle), schickt nach dem Tutorial einmal die
    /// Hinweis-Nachricht und holt das Moped zu Tagesbeginn an den Stammplatz zurück, wenn es weit weg steht.
    /// </summary>
    public sealed class MopedSpawner : MonoBehaviour
    {
        private float _acc, _hintWait;
        private Sim _sim;

        /// <summary>Stammplatz: Gehweg vor der Garage (neben der Abholbox) bzw. vor der Lagerhalle.</summary>
        public static void Home(int locationStage, out Vector3 pos, out float yaw)
        {
            pos = locationStage >= 1 ? new Vector3(10.6f, 0f, -5.7f) : new Vector3(-8.3f, 0f, -5.7f);
            yaw = 72f;
        }

        private void Start()
        {
            _sim = Game.Sim;
            if (_sim != null) _sim.DayStarted += OnDayStarted;
        }

        private void OnDestroy()
        {
            if (_sim != null) _sim.DayStarted -= OnDayStarted;
            if (Moped.Instance != null && Moped.Instance.transform.IsChildOf(transform)) Moped.Instance.ForceExit();
        }

        private void Update()
        {
            _acc -= Time.unscaledDeltaTime;
            if (_acc > 0f) return;
            _acc = 0.5f;
            var sim = Game.Sim;
            var world = Game.World;
            if (sim == null || world == null || world.MenuMode || !sim.InGame) return;
            if (sim.StoryStage != "business")
            {
                if (Moped.Instance != null) Destroy(Moped.Instance.gameObject);
                return;
            }
            if (Moped.Instance == null)
            {
                try
                {
                    Spawn(sim, world.transform);
                }
                catch (System.Exception e)
                {
                    SafeLog.Warn("moped.spawn", e);
                    _acc = 10f;
                }
            }
            // Hinweis-Nachricht nach dem Tutorial (kurz warten, damit sie nicht mit der Tutorial-Feier zusammenfällt)
            if (sim.MopedHintDue)
            {
                _hintWait += 0.5f;
                if (_hintWait >= 20f) sim.SendMopedHint(GameInput.KeyLabel("interact"));
            }
            else _hintWait = 0f;
        }

        private static void Spawn(Sim sim, Transform parent)
        {
            Vector3 pos;
            float yaw;
            var st = sim.Moped;
            var saved = st.Pos.ToVector3();
            if (st.Placed && Finite(saved) && saved.y > -10f && !RideZones.IsIndoor(saved, 0f))
            {
                pos = saved;
                yaw = st.RotY;
            }
            else FreeHome(sim.LocationStage, out pos, out yaw);
            Moped.Spawn(parent, pos, yaw);
        }

        /// <summary>Stammplatz, notfalls etwas daneben, falls dort gerade etwas steht.</summary>
        private static void FreeHome(int stage, out Vector3 pos, out float yaw)
        {
            Home(stage, out pos, out yaw);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            foreach (float dx in new[] { 0f, 1.6f, -1.6f, 3.2f, -3.2f })
            {
                var p = pos + new Vector3(dx, 0f, 0f);
                if (!Physics.CheckBox(p + new Vector3(0f, 0.75f, 0f), new Vector3(0.3f, 0.42f, 0.9f), rot, ~(1 << 2), QueryTriggerInteraction.Ignore))
                {
                    pos = p;
                    return;
                }
            }
        }

        private static bool Finite(Vector3 v) =>
            !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        /// <summary>Neuer Tag: Moped, das weit weg vom Stammplatz steht, steht wieder vor der Tür (man ist ja heimgefahren).</summary>
        private void OnDayStarted(int day)
        {
            var m = Moped.Instance;
            var sim = Game.Sim;
            if (m == null || sim == null || m.IsRidden) return;
            Home(sim.LocationStage, out var home, out float yaw);
            var d = m.transform.position - home;
            d.y = 0f;
            if (d.sqrMagnitude < 30f * 30f) return;
            FreeHome(sim.LocationStage, out home, out yaw);
            m.PlaceAt(home, yaw);
        }
    }
}
