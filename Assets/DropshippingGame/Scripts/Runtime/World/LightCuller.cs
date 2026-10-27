using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Schaltet Punkt- und Spotlichter weit weg von der Kamera ab (Innenlampen der Läden, Bank, Post ...).
    /// URP rechnet sonst für jedes sichtbare Objekt alle Lichter in Reichweite durch – in der großen Stadt
    /// sind das viele. Abgeschaltet wird nur das eigene Licht-Objekt (GameObject mit nur Transform + Light),
    /// damit Tag/Nacht-Schaltungen über <c>Light.enabled</c> (Atmosphere, CityLights) unberührt bleiben.
    /// Die Liste wird regelmäßig neu eingesammelt (Stationen, Deko und Läden werden neu gebaut).
    /// </summary>
    public sealed class LightCuller : MonoBehaviour
    {
        /// <summary>Abstand (über die Reichweite des Lichts hinaus), ab dem ein Licht aus ist.</summary>
        public float CullDistance = 42f;

        private readonly List<Light> _lights = new List<Light>();
        private float _acc, _rescan;

        /// <summary>Aktuell eingeschaltete bzw. verwaltete Lichter (für das Perf-Overlay).</summary>
        public static int ActiveCount { get; private set; }
        public static int ManagedCount { get; private set; }

        private void OnDestroy()
        {
            ActiveCount = 0;
            ManagedCount = 0;
        }

        private void Update()
        {
            _acc -= Time.unscaledDeltaTime;
            if (_acc > 0f) return;
            _acc = 0.4f;
            try
            {
                _rescan -= 0.4f;
                if (_rescan <= 0f)
                {
                    _rescan = 4f;
                    Collect();
                }
                Apply();
            }
            catch (System.Exception e)
            {
                SafeLog.Warn("lights", e);
            }
        }

        private void Collect()
        {
            _lights.Clear();
            foreach (var l in GetComponentsInChildren<Light>(true))
            {
                if (l == null || l.type == LightType.Directional) continue;
                var go = l.gameObject;
                // Nur reine Licht-Objekte (sonst würden Lampenschirme o. Ä. mit verschwinden).
                if (go == gameObject || l.transform.childCount > 0) continue;
                if (go.GetComponents<Component>().Length != 2) continue;
                _lights.Add(l);
            }
            ManagedCount = _lights.Count;
        }

        private void Apply()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 cp = cam.transform.position;
            int active = 0;
            for (int i = _lights.Count - 1; i >= 0; i--)
            {
                var l = _lights[i];
                if (l == null)
                {
                    _lights.RemoveAt(i);
                    continue;
                }
                float reach = CullDistance + Mathf.Max(0f, l.range);
                bool on = (l.transform.position - cp).sqrMagnitude < reach * reach;
                var go = l.gameObject;
                if (go.activeSelf != on) go.SetActive(on);
                if (on && l.enabled) active++;
            }
            ActiveCount = active;
        }
    }
}
