using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Tageswetter (nur Optik, kein Einfluss auf die Simulation): pro Spieltag wird aus Stadt-Seed und Tag
    /// deterministisch „sonnig“, „bewölkt“ oder „leichter Regen“ gewählt. Regen läuft in einem Zeitfenster,
    /// danach trocknen die Straßen langsam ab. Liefert Bewölkung/Regen/Nässe an <see cref="Atmosphere"/>,
    /// steuert ein einziges Regen-Partikelsystem um die Kamera (Partikel werden vom System wiederverwendet)
    /// sowie Wasserfilm und Pfützen auf den Straßen. Abschaltbar über <see cref="Settings.Weather"/>.
    /// Die ersten beiden Spieltage und das Hauptmenü sind immer sonnig.
    /// </summary>
    public sealed class Weather : MonoBehaviour
    {
        public enum Kind { Clear, Cloudy, Rain }

        private struct Plan
        {
            public Kind Kind;
            public float Overcast;
            public float RainStart, RainEnd, RainStrength;
            public bool MorningWet;
        }

        /// <summary>Überdachte Bereiche (x, z, Breite, Tiefe), in denen es nicht regnet: Imbiss, Garage, Halle, Läden, Post, Bank, Börse.</summary>
        private static readonly Rect[] Covered =
        {
            new Rect(-44f, -19f, 16f, 12f), new Rect(-22f, -17f, 12f, 10f), new Rect(-2f, -31f, 36f, 24f),
            new Rect(-95f, -34f, 33f, 25f), new Rect(-115f, -27f, 16f, 18f), new Rect(-124f, -21f, 7f, 12f),
            new Rect(62f, -25f, 23f, 16f), new Rect(62f, 12f, 18f, 16f), new Rect(84f, 12f, 24f, 24f),
        };

        public Kind Today { get; private set; } = Kind.Clear;
        /// <summary>Bewölkung 0..1.</summary>
        public float Overcast { get; private set; }
        /// <summary>Regenstärke 0..1 (gerade fallender Regen).</summary>
        public float Rain { get; private set; }
        /// <summary>Nässe der Straßen 0..1 (bleibt nach dem Regen eine Weile).</summary>
        public float Wetness { get; private set; }

        /// <summary>Kurzer Text für Anzeigen (z. B. Handy): „Sonnig“, „Bewölkt“, „Regen“.</summary>
        public string Label => Today == Kind.Rain ? "Regen" : (Today == Kind.Cloudy ? "Bewölkt" : "Sonnig");

        private Plan _plan;
        private int _planDay = -1, _planSeed;
        private bool _planEnabled;
        private bool _menu;
        private ParticleSystem _rainPs;
        private Transform _rainTf;
        private readonly List<Renderer> _wetRenderers = new List<Renderer>();
        private bool _wetBuilt, _wetVisible;
        private float _coverAcc;
        private bool _covered;

        private void Awake()
        {
            var wb = GetComponent<WorldBuilder>();
            _menu = wb != null && wb.MenuMode;
        }

        // =====================================================================================
        // Tagesplan
        // =====================================================================================
        private static Plan MakePlan(int seed, int day)
        {
            var p = new Plan { Kind = Kind.Clear, Overcast = 0.05f };
            if (day <= 2) return p;
            var rng = new System.Random(unchecked(seed * 7919 + day * 104729));
            double r = rng.NextDouble();
            float F(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            if (r < 0.55)
            {
                p.Kind = Kind.Clear;
                p.Overcast = F(0f, 0.2f);
                p.MorningWet = rng.NextDouble() < 0.15;
            }
            else if (r < 0.8)
            {
                p.Kind = Kind.Cloudy;
                p.Overcast = F(0.55f, 0.8f);
            }
            else
            {
                p.Kind = Kind.Rain;
                p.Overcast = 0.65f;
                p.RainStart = F(9.5f, 15.5f);
                p.RainEnd = p.RainStart + F(1.5f, 4f);
                p.RainStrength = F(0.45f, 0.8f);
            }
            return p;
        }

        /// <summary>Wetter zur Uhrzeit berechnen (deterministisch, Zeitsprünge sind unproblematisch).</summary>
        public void Evaluate(float hours)
        {
            var sim = Game.Sim;
            bool enabled = Settings.Weather && !_menu && sim != null && sim.InGame;
            int day = sim != null ? sim.Day : 1;
            int seed = sim != null ? sim.CitySeed : 0;
            if (day != _planDay || seed != _planSeed || enabled != _planEnabled)
            {
                _planDay = day;
                _planSeed = seed;
                _planEnabled = enabled;
                _plan = enabled ? MakePlan(seed, day) : new Plan { Kind = Kind.Clear, Overcast = 0f };
                Today = _plan.Kind;
            }

            float rain = 0f, wet = 0f, overcast = _plan.Overcast;
            if (_plan.Kind == Kind.Rain)
            {
                const float ramp = 0.4f;
                float inR = Mathf.Clamp01((hours - (_plan.RainStart - ramp)) / ramp);
                float outR = Mathf.Clamp01(((_plan.RainEnd + ramp) - hours) / ramp);
                rain = Mathf.Min(inR, outR) * _plan.RainStrength;
                // Wolken ziehen vorher auf und verziehen sich danach langsam
                float near = Mathf.Min(Mathf.Clamp01((hours - (_plan.RainStart - 1.5f)) / 1.5f), Mathf.Clamp01(((_plan.RainEnd + 2f) - hours) / 2f));
                overcast = Mathf.Lerp(0.45f, 0.92f, near);
                if (hours >= _plan.RainStart && hours <= _plan.RainEnd) wet = Mathf.Clamp01((hours - _plan.RainStart) / 0.6f);
                else if (hours > _plan.RainEnd) wet = Mathf.Clamp01(1f - (hours - _plan.RainEnd) / 2.5f) * Mathf.Clamp01((_plan.RainEnd - _plan.RainStart) / 0.6f);
            }
            else if (_plan.MorningWet)
            {
                // Hat nachts geregnet: morgens nass, bis halb elf abgetrocknet
                wet = Mathf.Clamp01((10.5f - hours) / 2.5f) * 0.7f;
            }
            Overcast = overcast;
            Rain = rain;
            Wetness = wet;
            if (wet > 0.01f && !_wetBuilt) BuildWetSurfaces();
            SetWetVisible(wet > 0.01f);
        }

        // =====================================================================================
        // Regen-Partikel (ein System, folgt der Kamera)
        // =====================================================================================
        private void LateUpdate()
        {
            bool want = Rain > 0.01f;
            if (!want && _rainPs == null) return;
            var cam = Camera.main;
            if (want && _rainPs == null) BuildRain();
            if (_rainPs == null) return;
            if (cam == null)
            {
                SetEmission(0f);
                return;
            }
            Vector3 cp = cam.transform.position;
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            _rainTf.position = cp + Vector3.up * 10f + fwd.normalized * 5f;

            _coverAcc -= Time.unscaledDeltaTime;
            if (_coverAcc <= 0f)
            {
                _coverAcc = 0.25f;
                bool covered = IsCovered(cp);
                if (covered && !_covered) _rainPs.Clear();
                _covered = covered;
            }
            float max = Settings.Quality == 0 ? 500f : (Settings.Quality == 1 ? 900f : 1400f);
            SetEmission(_covered ? 0f : Rain * max);
        }

        private void SetEmission(float rate)
        {
            var em = _rainPs.emission;
            em.rateOverTime = rate;
        }

        private static bool IsCovered(Vector3 p)
        {
            foreach (var r in Covered)
                if (p.x > r.xMin && p.x < r.xMax && p.z > r.yMin && p.z < r.yMax && p.y < 12f) return true;
            // Sonstige Dächer/Vordächer mit Kollision
            return Physics.Raycast(p + Vector3.up * 0.3f, Vector3.up, 25f, ~0, QueryTriggerInteraction.Ignore);
        }

        private void BuildRain()
        {
            var mat = Mats.RainStreak();
            if (mat == null) return;
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            // +Z des Systems zeigt nach unten (leicht schräg = etwas Wind)
            go.transform.rotation = Quaternion.Euler(82f, 25f, 0f);
            _rainTf = go.transform;
            _rainPs = go.AddComponent<ParticleSystem>();
            _rainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _rainPs.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 5f;
            main.startLifetime = 1.15f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(13f, 16f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.03f);
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            main.scalingMode = ParticleSystemScalingMode.Local;
            var shape = _rainPs.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 34f, 0.5f);
            var em = _rainPs.emission;
            em.enabled = true;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 2f;
            r.velocityScale = 0.045f;
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 10f;
            _rainPs.Play();
        }

        // =====================================================================================
        // Nasse Straßen: Wasserfilm + Pfützen (erst beim ersten Regen gebaut)
        // =====================================================================================
        private void AddQuad(Transform parent, Material mat, Vector3 center, Vector2 size, float rotY = 0f)
        {
            var go = new GameObject("Wet");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = new Vector3(size.x, 1f, size.y);
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.QuadUp();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.enabled = _wetVisible;
            _wetRenderers.Add(r);
        }

        private void BuildWetSurfaces()
        {
            _wetBuilt = true;
            if (_menu) return;
            try
            {
                var root = new GameObject("WetSurfaces").transform;
                root.SetParent(transform, false);
                var sheen = Mats.WetSheen();
                var puddle = Mats.Puddle();
                // Wasserfilm knapp über Belag und Markierungen
                AddQuad(root, sheen, new Vector3(0f, 0.035f, 0f), new Vector2(400f, 8f));
                AddQuad(root, sheen, new Vector3(0f, 0.045f, -5.5f), new Vector2(400f, 3f));
                AddQuad(root, sheen, new Vector3(0f, 0.045f, 5.5f), new Vector2(400f, 3f));
                float hw = WorldLots.SideHalfWidth;
                float northEnd = WorldLots.ResidentialZ - WorldLots.ResidentialHalfWidth;
                foreach (float sx in WorldLots.SideStreetsX)
                    AddQuad(root, sheen, new Vector3(sx, 0.04f, (7f + northEnd) * 0.5f), new Vector2(hw * 2f, northEnd - 7f));
                foreach (float sx in WorldLots.SideStreetsSouthX)
                    AddQuad(root, sheen, new Vector3(sx, 0.04f, (WorldLots.SideSouthEndZ - 7f) * 0.5f), new Vector2(hw * 2f, -7f - WorldLots.SideSouthEndZ));
                float rx0 = WorldLots.ResidentialFromX, rx1 = WorldLots.ResidentialToX, rz = WorldLots.ResidentialZ, rhw = WorldLots.ResidentialHalfWidth;
                AddQuad(root, sheen, new Vector3((rx0 + rx1) * 0.5f, 0.04f, rz), new Vector2(rx1 - rx0, rhw * 2f));
                // Pfützen an Bordsteinen und auf Gehwegen (fester Seed: immer an denselben Stellen)
                var rng = new System.Random(777);
                float F(float a, float b) => a + (float)rng.NextDouble() * (b - a);
                for (int i = 0; i < 46; i++)
                {
                    int lane = rng.Next(5);
                    float x, z, y;
                    switch (lane)
                    {
                        case 0: x = F(-130f, 130f); z = F(-3.6f, -2.4f); y = 0.04f; break;
                        case 1: x = F(-130f, 130f); z = F(2.4f, 3.6f); y = 0.04f; break;
                        case 2: x = F(-130f, 130f); z = F(-6.4f, -4.6f); y = 0.05f; break;
                        case 3: x = F(-130f, 130f); z = F(4.6f, 6.4f); y = 0.05f; break;
                        default: x = F(rx0 + 2f, rx1 - 2f); z = rz + (rng.NextDouble() < 0.5 ? F(-4.4f, -3.2f) : F(3.2f, 4.4f)); y = 0.045f; break;
                    }
                    float s = F(0.9f, 2.4f);
                    AddQuad(root, puddle, new Vector3(x, y, z), new Vector2(s, s * F(0.55f, 0.9f)), F(0f, 180f));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Weather] Nasse Straßen: " + e.Message);
            }
        }

        private void SetWetVisible(bool on)
        {
            if (on == _wetVisible) return;
            _wetVisible = on;
            foreach (var r in _wetRenderers)
                if (r != null) r.enabled = on;
        }
    }
}
