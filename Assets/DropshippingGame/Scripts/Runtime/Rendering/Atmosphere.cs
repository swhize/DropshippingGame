using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Tageslicht und Stimmung: Sonnenstand und -farbe nach Uhrzeit, prozeduraler Himmel,
    /// Umgebungslicht, Nebel, Straßenlaternen und leuchtende Fenster bei Nacht,
    /// Reflexionen und Post-Processing.
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        public Light Sun;
        public readonly List<Light> StreetLights = new List<Light>();
        private Material _sky;
        private ReflectionProbe _probe;
        private float _lastProbeHour = -100f;
        private float _lastEnvHour = -100f;
        private float _hours = 12f;

        public float Hours => _hours;

        public void Setup(Transform root)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            Sun = sunGo.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.82f;
            Sun.shadowBias = 0.04f;
            Sun.shadowNormalBias = 0.3f;
            RenderSettings.sun = Sun;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                _sky = new Material(skyShader);
                _sky.SetFloat("_SunDisk", 2f);
                _sky.SetFloat("_SunSize", 0.035f);
                _sky.SetFloat("_SunSizeConvergence", 6f);
                RenderSettings.skybox = _sky;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0032f;

            var probeGo = new GameObject("Reflections");
            probeGo.transform.SetParent(root, false);
            probeGo.transform.localPosition = new Vector3(-4f, 4f, -8f);
            _probe = probeGo.AddComponent<ReflectionProbe>();
            _probe.mode = ReflectionProbeMode.Realtime;
            _probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            _probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            _probe.size = new Vector3(140f, 50f, 90f);
            _probe.resolution = 128;
            _probe.boxProjection = false;
            _probe.importance = 1;
            _probe.intensity = 0.8f;

            PostFX.Create(root);
        }

        /// <summary>Tageslicht: Sonnenstand, Farben, Straßenlaternen und Fensterlicht nach Uhrzeit (0-24).</summary>
        public void SetTimeOfDay(float hours)
        {
            _hours = hours;
            float t = Mathf.Clamp01((hours - 6f) / 15f);
            float elevation = Mathf.Sin(t * Mathf.PI) * 62f + 3f;
            float azimuth = Mathf.Lerp(-110f, 110f, t);
            float night = Mathf.Clamp01((hours - 18.6f) / 1.4f);
            if (Sun != null)
            {
                Sun.transform.rotation = Quaternion.Euler(elevation, azimuth + 180f, 0f);
                float warm = 1f - Mathf.Clamp01((elevation - 6f) / 30f);
                Sun.color = Color.Lerp(new Color(1f, 0.96f, 0.9f), new Color(1f, 0.58f, 0.32f), warm);
                Sun.intensity = Mathf.Lerp(0.55f, 1.45f, Mathf.Clamp01(elevation / 40f)) * (1f - night * 0.55f);

                Color skyTop = Color.Lerp(new Color(0.34f, 0.55f, 0.85f), new Color(0.3f, 0.32f, 0.55f), warm);
                Color horizon = Color.Lerp(new Color(0.72f, 0.8f, 0.9f), new Color(0.98f, 0.64f, 0.42f), warm);
                skyTop = Color.Lerp(skyTop, new Color(0.06f, 0.08f, 0.16f), night * 0.8f);
                horizon = Color.Lerp(horizon, new Color(0.35f, 0.22f, 0.25f), night * 0.6f);
                RenderSettings.ambientSkyColor = skyTop * Mathf.Lerp(1.05f, 0.7f, warm);
                RenderSettings.ambientEquatorColor = horizon * Mathf.Lerp(0.9f, 0.62f, warm);
                RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.22f) * Mathf.Lerp(1f, 0.6f, warm);
                RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.6f, night);
                RenderSettings.fogColor = Color.Lerp(Color.Lerp(new Color(0.72f, 0.77f, 0.84f), new Color(0.9f, 0.62f, 0.5f), warm),
                    new Color(0.18f, 0.16f, 0.22f), night * 0.7f);
                if (_sky != null)
                {
                    _sky.SetColor("_SkyTint", Color.Lerp(new Color(0.5f, 0.5f, 0.52f), new Color(0.62f, 0.45f, 0.42f), warm));
                    _sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.9f, 1.55f, warm));
                    _sky.SetFloat("_Exposure", Mathf.Lerp(1.25f, 0.95f, warm) * (1f - night * 0.55f));
                    _sky.SetColor("_GroundColor", Color.Lerp(new Color(0.37f, 0.38f, 0.4f), new Color(0.25f, 0.2f, 0.2f), warm));
                }
            }
            Mats.SetNight(night);
            foreach (var l in StreetLights)
            {
                if (l == null) continue;
                l.enabled = night > 0.02f;
                l.intensity = night * 5f;
            }
            if (Mathf.Abs(hours - _lastEnvHour) > 0.5f)
            {
                _lastEnvHour = hours;
                DynamicGI.UpdateEnvironment();
            }
            if (Mathf.Abs(hours - _lastProbeHour) > 1f) RenderReflections();
        }

        public void RenderReflections()
        {
            _lastProbeHour = _hours;
            if (_probe != null && Settings.Quality > 0) _probe.RenderProbe();
        }
    }
}
