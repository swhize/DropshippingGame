using System.Collections.Generic;
using DropshippingGame.Core;
using DropshippingGame.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace DropshippingGame
{
    /// <summary>
    /// Echte TikTok-Aufnahme in Ego-Perspektive (wie <see cref="FurnitureMover"/>): Das gewählte
    /// Produkt wird auf einem Karton vor dem Spieler aufgestellt, der Bildschirm zeigt einen
    /// 9:16-Sucher (<see cref="TikTokViewfinder"/>). Jeden Frame wird gemessen (Produkt im Bild und
    /// mittig, Abstand, ruhige Kamera, Licht, Blickwinkel, Marke/Deko, Aktionen) – die Wertung
    /// selbst macht <see cref="TikTokScoring"/> in der Core.
    /// E/X = Aktion (in den ersten 2 s: Hook), Enter/Linksklick/RB = Stopp (ab 3 s),
    /// Esc/B/Start = abbrechen (GameRoot leitet das um, damit kein Pausemenü aufgeht).
    /// </summary>
    public sealed class TikTokRecorder
    {
        private const int IgnoreRaycastLayer = 2;
        private const int RayMask = ~(1 << IgnoreRaycastLayer);
        private const float CountdownSeconds = 3f;
        private const float SetScale = 1.6f;
        private const float HopDuration = 0.6f;
        private const float ActionCooldown = 0.7f;
        private const float StandHeight = 0.42f;
        private static readonly Vector3 StandSize = new Vector3(0.5f, StandHeight, 0.42f);

        private enum Phase
        {
            None, Countdown, Recording,
        }

        private Phase _phase = Phase.None;
        private string _product = "", _format = "";
        private float _t, _countdown;
        private GameObject _set;
        private Transform _item;
        private GameObject _boxVis, _productVis;
        private float _boxHalf = 0.1f, _productHalf = 0.1f;
        private bool _unboxed = true;
        private Vector3 _itemBase;
        private float _hopT;
        private Light _ring;
        private bool _hudHidden;
        private PlayerController _handsOf;
        private readonly List<Light> _lights = new List<Light>();
        private readonly List<Transform> _brands = new List<Transform>();
        private TikTokViewfinder _view;

        // Messwerte
        private float _visT, _centerSum, _distOkT, _steadyT, _lightSum, _brandT, _measuredT;
        private float _yawMin, _yawMax, _yawLast, _yawUnwrapped;
        private bool _yawInit, _hook;
        private int _actions;
        private float _lastActionT = -10f;
        private Quaternion _prevRot;
        private bool _hasPrevRot;
        private float _rotSpeed;
        private float _lightCache = 0.5f, _lightAcc = 1f;

        public bool Active => _phase != Phase.None;
        public bool Recording => _phase == Phase.Recording;
        public string Product => _product;
        public string Format => _format;

        private static bool Allowed()
        {
            var sim = Game.Sim;
            return sim != null && sim.StoryStage == "business" && !sim.DayOver;
        }

        /// <summary>Stellt das Produkt auf und startet den Countdown. false, wenn es nicht geht.</summary>
        public bool Begin(PlayerController player, string product, string format)
        {
            if (Active || player == null || player.Cam == null) return false;
            var sim = Game.Sim;
            if (!Allowed() || !GameData.IsProduct(product)) return false;
            string block = sim.TikTokBlocker();
            if (block != null)
            {
                sim.Notify(block, "info");
                return false;
            }
            if (player.Mover != null && player.Mover.Active) player.Mover.Cancel();
            _product = product;
            _format = TikTokFormats.IsFormat(format) ? format : TikTokFormats.Review;
            ResetMeasures();
            try
            {
                if (!BuildSet(player))
                {
                    Cleanup();
                    return false;
                }
                CollectScene();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Cleanup();
                return false;
            }
            _phase = Phase.Countdown;
            _countdown = CountdownSeconds;
            _t = 0f;

            // Kamera-Modus: HUD und Hände weg, Sucher drauf.
            var ui = Game.UI;
            if (ui != null && ui.Hud != null)
            {
                ui.Hud.SetVisible(false);
                _hudHidden = true;
            }
            player.SetHandsVisible(false);
            _handsOf = player;
            _view = TikTokViewfinder.Create();
            if (_view != null)
            {
                _view.SetInfo(TikTokFormats.Name(_format), GameData.Product(_product).Name, sim.TrendingTikTokFormat() == _format);
                _view.SetCountdown(Mathf.CeilToInt(CountdownSeconds), CountdownText());
                _view.SetHints(null, ActionLabel());
            }
            Game.Sound("click");
            return true;
        }

        private string CountdownText()
        {
            switch (_format)
            {
                case TikTokFormats.Unboxing: return "Produkt anvisieren – gleich zu Beginn auspacken!";
                case TikTokFormats.Pov: return "Ruhig bleiben und einmal ums Produkt gehen.";
                case TikTokFormats.Hack: return "Hook zuerst, dann mehrmals vorführen.";
            }
            return "Produkt mittig, ruhig halten, gutes Licht.";
        }

        private void ResetMeasures()
        {
            _visT = _centerSum = _distOkT = _steadyT = _lightSum = _brandT = _measuredT = 0f;
            _yawMin = _yawMax = _yawLast = _yawUnwrapped = 0f;
            _yawInit = false;
            _hook = false;
            _actions = 0;
            _lastActionT = -10f;
            _hasPrevRot = false;
            _rotSpeed = 0f;
            _hopT = 0f;
            _lightAcc = 1f;
            _unboxed = true;
        }

        /// <summary>Jeden Frame vom Spieler. locked = ein Fenster liegt darüber (Aufnahme hält an).</summary>
        public void Tick(PlayerController player, bool locked)
        {
            if (!Active) return;
            if (player == null || player.Cam == null || !Allowed() || _item == null)
            {
                Cancel();
                return;
            }
            float dt = Time.deltaTime;
            if (dt <= 0f || locked)
            {
                _hasPrevRot = false;
                return;
            }
            var cam = player.Cam;

            // Kamera-Ruhe (geglättete Drehgeschwindigkeit in Grad/s)
            var rot = cam.transform.rotation;
            float speed = _hasPrevRot ? Quaternion.Angle(_prevRot, rot) / dt : 0f;
            _prevRot = rot;
            _hasPrevRot = true;
            _rotSpeed = Mathf.Lerp(_rotSpeed, speed, 1f - Mathf.Exp(-10f * dt));

            AnimateItem(dt);

            if (_phase == Phase.Countdown)
            {
                _countdown -= dt;
                if (_view != null) _view.SetCountdown(Mathf.CeilToInt(Mathf.Max(0f, _countdown)), CountdownText());
                if (_countdown <= 0f)
                {
                    _phase = Phase.Recording;
                    _t = 0f;
                    if (_view != null) _view.SetCountdown(0);
                    Game.Sound("notify");
                }
                return;
            }

            _t += dt;
            if (GameInput.RecordActionDown && _t - _lastActionT >= ActionCooldown) DoAction();

            Measure(cam, dt, out bool visible, out float center, out float dist, out float light);
            if (_view != null)
            {
                _view.SetTime(_t, TikTokScoring.MaxSeconds, _t >= TikTokScoring.StopAfterSeconds);
                _view.SetHints(TikTokScoring.FrameHints(visible, center, dist, _rotSpeed, light, _t, _hook), ActionLabel());
            }

            if (_t >= TikTokScoring.MaxSeconds || (_t >= TikTokScoring.StopAfterSeconds && GameInput.RecordStopDown))
                Finish();
        }

        private void DoAction()
        {
            _lastActionT = _t;
            _actions++;
            if (_t <= TikTokScoring.HookSeconds) _hook = true;
            _hopT = HopDuration;
            if (!_unboxed)
            {
                _unboxed = true;
                if (_boxVis != null) _boxVis.SetActive(false);
                if (_productVis != null) _productVis.SetActive(true);
                Game.Sound("tape", 0.1f, -4f);
            }
            else Game.Sound("pickup", 0.15f, -4f);
        }

        private string ActionLabel()
        {
            switch (_format)
            {
                case TikTokFormats.Unboxing: return _unboxed ? "Zeigen" : "Auspacken";
                case TikTokFormats.Pov: return "Zeigen";
                case TikTokFormats.Hack: return "Vorführen";
            }
            return "Drehen";
        }

        // ---- Messung ----------------------------------------------------------------------------------
        private float ItemHalf => _unboxed ? _productHalf : _boxHalf;
        private Vector3 ItemCenter => _item != null ? _item.position + Vector3.up * ItemHalf : Vector3.zero;

        /// <summary>Breite des 9:16-Ausschnitts als Anteil der Bildschirmbreite.</summary>
        public static float CropWidth()
        {
            float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
            return Mathf.Clamp01(h * 9f / 16f / w);
        }

        private void Measure(Camera cam, float dt, out bool visible, out float center, out float dist, out float light)
        {
            var c = ItemCenter;
            var camPos = cam.transform.position;
            dist = Vector3.Distance(camPos, c);
            float halfW = CropWidth() / 2f;
            var vp = cam.WorldToViewportPoint(c);
            visible = vp.z > 0.05f && Mathf.Abs(vp.x - 0.5f) <= halfW * 0.95f && vp.y > 0.03f && vp.y < 0.97f;
            if (visible && dist > 0.2f && Occluded(camPos, c, dist)) visible = false;
            center = 0f;
            if (visible)
            {
                float nx = Mathf.Abs(vp.x - 0.5f) / Mathf.Max(0.01f, halfW);
                float ny = Mathf.Abs(vp.y - 0.5f) / 0.5f;
                center = 1f - Mathf.Clamp01(Mathf.Max(nx, ny));
            }

            _lightAcc += dt;
            if (_lightAcc >= 0.25f)
            {
                _lightAcc = 0f;
                _lightCache = EstimateLight(c);
            }
            light = _lightCache;

            _measuredT += dt;
            _lightSum += light * dt;
            if (_rotSpeed <= TikTokScoring.SteadyDegPerSec) _steadyT += dt;
            if (BrandVisible(cam)) _brandT += dt;
            if (!visible) return;
            _visT += dt;
            _centerSum += center * dt;
            if (dist >= TikTokScoring.GoodDistMin && dist <= TikTokScoring.GoodDistMax) _distOkT += dt;

            // Blickwinkel ums Produkt (Gieren, entfaltet – einmal herum = 360°)
            var flat = new Vector3(camPos.x - c.x, 0f, camPos.z - c.z);
            if (flat.sqrMagnitude > 0.0001f)
            {
                float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                if (!_yawInit)
                {
                    _yawInit = true;
                    _yawLast = yaw;
                    _yawUnwrapped = 0f;
                    _yawMin = _yawMax = 0f;
                }
                else
                {
                    _yawUnwrapped += Mathf.DeltaAngle(_yawLast, yaw);
                    _yawLast = yaw;
                    _yawMin = Mathf.Min(_yawMin, _yawUnwrapped);
                    _yawMax = Mathf.Max(_yawMax, _yawUnwrapped);
                }
            }
        }

        /// <summary>Steht etwas Festes zwischen Kamera und Produkt? (Collider, in denen das Produkt selbst steckt, zählen nicht.)</summary>
        private bool Occluded(Vector3 camPos, Vector3 c, float dist)
        {
            var dir = (c - camPos) / dist;
            float len = dist - 0.15f * SetScale;
            if (len <= 0.05f) return false;
            var hits = Physics.RaycastAll(camPos, dir, len, RayMask, QueryTriggerInteraction.Ignore);
            if (hits == null) return false;
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null) continue;
                if (col.GetComponentInParent<CharacterController>() != null) continue;
                var b = col.bounds;
                b.Expand(0.15f);
                if (b.Contains(c)) continue;
                return true;
            }
            return false;
        }

        private float EstimateLight(Vector3 at)
        {
            float l = 0.22f;
            var world = Game.World;
            var sun = world != null && world.Atmos != null ? world.Atmos.Sun : null;
            if (sun != null && sun.isActiveAndEnabled)
            {
                float s = Mathf.Clamp01(sun.intensity / 1.2f);
                // Direktes Sonnenlicht nur, wenn nichts darüber ist; drinnen kommt nur etwas durch Tor/Fenster.
                bool outside = !Physics.Raycast(at + Vector3.up * 0.3f, -sun.transform.forward, 60f, RayMask, QueryTriggerInteraction.Ignore);
                l += s * (outside ? 0.5f : 0.18f);
            }
            foreach (var li in _lights)
            {
                if (li == null || !li.isActiveAndEnabled || li.type == LightType.Directional || li.intensity <= 0.01f) continue;
                float d = Vector3.Distance(li.transform.position, at);
                float range = Mathf.Max(0.1f, li.range);
                if (d >= range) continue;
                float f = 1f - d / range;
                l += li.intensity * f * f * 0.3f;
            }
            return Mathf.Clamp01(l);
        }

        private bool BrandVisible(Camera cam)
        {
            float halfW = CropWidth() / 2f;
            var camPos = cam.transform.position;
            foreach (var b in _brands)
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                if ((b.position - camPos).sqrMagnitude > 100f) continue;
                var vp = cam.WorldToViewportPoint(b.position);
                if (vp.z > 0.1f && Mathf.Abs(vp.x - 0.5f) <= halfW && vp.y > 0f && vp.y < 1f) return true;
            }
            return false;
        }

        private void CollectScene()
        {
            _lights.Clear();
            _brands.Clear();
            var world = Game.World;
            if (world == null) return;
            _lights.AddRange(world.GetComponentsInChildren<Light>(false));
            if (_ring != null && !_lights.Contains(_ring)) _lights.Add(_ring);
            var origin = _set != null ? _set.transform.position : Vector3.zero;
            foreach (var t in world.GetComponentsInChildren<Transform>(false))
            {
                if (t == null) continue;
                string n = t.name;
                bool brand = n == "Poster" || n == "Neon" || n == "Sign" || t.GetComponent<TikTokRingLight>() != null;
                if (!brand) continue;
                if ((t.position - origin).sqrMagnitude > 400f) continue;
                _brands.Add(t);
            }
            foreach (var st in Station.All)
                if (st != null && st.Type == StationType.Sign && !_brands.Contains(st.transform) && (st.transform.position - origin).sqrMagnitude <= 400f)
                    _brands.Add(st.transform);
        }

        // ---- Aufbau -----------------------------------------------------------------------------------
        /// <summary>Sucht vor dem Spieler einen freien Bodenplatz für Karton + Produkt. false = kein Platz.</summary>
        private bool FindSpot(PlayerController player, out Vector3 pos, out Vector3 fwd)
        {
            var cam = player.Cam.transform;
            fwd = new Vector3(cam.forward.x, 0f, cam.forward.z);
            if (fwd.sqrMagnitude < 0.0001f) fwd = player.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();
            var basePos = player.transform.position;
            var eye = cam.position;
            float[] dists = { 1.2f, 1.0f, 1.4f, 0.85f, 1.7f };
            float[] angles = { 0f, -25f, 25f, -50f, 50f };
            var half = new Vector3(StandSize.x / 2f + 0.04f, 0.32f, StandSize.z / 2f + 0.04f);
            foreach (float a in angles)
            {
                var dir = Quaternion.Euler(0f, a, 0f) * fwd;
                foreach (float d in dists)
                {
                    var p = basePos + dir * d;
                    // Boden unter dem Platz (von Hüfthöhe nach unten)
                    if (!Physics.Raycast(p + Vector3.up * 1.0f, Vector3.down, out var ground, 2.5f, RayMask, QueryTriggerInteraction.Ignore)) continue;
                    if (ground.point.y > basePos.y + 0.3f || ground.point.y < basePos.y - 0.5f) continue; // kein Tisch/Loch
                    var center = new Vector3(p.x, ground.point.y + 0.42f, p.z);
                    if (Physics.CheckBox(center, half, Quaternion.LookRotation(dir), RayMask, QueryTriggerInteraction.Ignore)) continue;
                    // Freie Sicht vom Auge (keine Wand dazwischen)
                    var target = center + Vector3.up * 0.2f;
                    var to = target - eye;
                    if (Physics.Raycast(eye, to.normalized, to.magnitude, RayMask, QueryTriggerInteraction.Ignore)) continue;
                    pos = new Vector3(p.x, ground.point.y, p.z);
                    fwd = dir;
                    return true;
                }
            }
            pos = Vector3.zero;
            return false;
        }

        private bool BuildSet(PlayerController player)
        {
            if (!FindSpot(player, out var pos, out var fwd)) return false;
            var sim = Game.Sim;

            _set = new GameObject("TikTokSet") { layer = IgnoreRaycastLayer };
            if (Game.World != null) _set.transform.SetParent(Game.World.transform, true);
            _set.transform.position = pos;
            _set.transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);

            // Karton als kleine Bühne
            Props.Box(_set.transform, StandSize, Mats.Cardboard(), new Vector3(0f, StandHeight / 2f, 0f), default, 0.01f);
            Props.Box(_set.transform, new Vector3(StandSize.x + 0.004f, 0.004f, StandSize.z * 0.22f), Mats.Std(new Color(0.8f, 0.7f, 0.45f), 0.35f),
                new Vector3(0f, StandHeight + 0.002f, 0f), default, 0f, false);

            var holder = new GameObject("Item").transform;
            holder.SetParent(_set.transform, false);
            holder.localPosition = new Vector3(0f, StandHeight, 0f);
            _item = holder;
            _itemBase = holder.localPosition;

            var data = new ItemData { Kind = ItemKind.Item, Product = _product, Quantity = 1, Quality = sim != null ? sim.StockQuality(_product) : 1f };
            _productVis = BuildVisual(holder, data, out _productHalf);
            if (_format == TikTokFormats.Unboxing)
            {
                // Erst ein Paket in Markenfarbe – die erste Aktion packt aus.
                var pkg = new ItemData { Kind = ItemKind.Package, Product = _product, Quantity = 1 };
                if (sim != null)
                {
                    pkg.Color = sim.BrandColor;
                    pkg.Logo = sim.BrandLogoIndex;
                }
                _boxVis = BuildVisual(holder, pkg, out _boxHalf);
                if (_boxVis != null && _productVis != null)
                {
                    _unboxed = false;
                    _productVis.SetActive(false);
                }
            }

            bool ringLight = sim != null && (sim.StockQty("ringlicht") > 0 || sim.HasSkill("m_content"));
            if (ringLight) BuildRingLight(_set.transform);

            foreach (var lbl in _set.GetComponentsInChildren<Label3D>(true)) Object.Destroy(lbl.gameObject);
            foreach (var c in _set.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            foreach (var t in _set.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = IgnoreRaycastLayer;
            return _productVis != null;
        }

        /// <summary>Baut einen vergrößerten Gegenstand auf den Halter (Fallback: bunter Würfel).</summary>
        private GameObject BuildVisual(Transform holder, ItemData data, out float halfHeight)
        {
            var size = ItemKit.Bounds(data) * SetScale;
            halfHeight = size.y / 2f;
            GameObject vis = null;
            try
            {
                vis = ItemKit.Build(holder, data, false);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (vis != null) Object.Destroy(vis);
                vis = null;
            }
            if (vis == null)
            {
                var col = GameData.IsProduct(_product) ? GameData.Product(_product).Color.ToColor() : Color.magenta;
                vis = Props.Box(holder, ItemKit.ItemSize, Mats.Std(col, 0.5f), Vector3.zero, default, 0.01f, false);
                halfHeight = ItemKit.ItemSize.y * SetScale / 2f;
            }
            vis.transform.localScale = Vector3.one * SetScale;
            vis.transform.localPosition = new Vector3(0f, halfHeight, 0f);
            return vis;
        }

        private void BuildRingLight(Transform parent)
        {
            var n = new GameObject("RingLight").transform;
            n.SetParent(parent, false);
            n.localPosition = new Vector3(0.55f, 0f, 0.35f);
            n.localRotation = Quaternion.Euler(0f, -35f, 0f);
            var metal = Mats.Std(new Color(0.12f, 0.12f, 0.14f), 0.4f);
            Props.Box(n, new Vector3(0.03f, 1.1f, 0.03f), metal, new Vector3(0f, 0.55f, 0f), default, 0f, false);
            Props.Box(n, new Vector3(0.35f, 0.02f, 0.35f), metal, new Vector3(0f, 0.01f, 0f), default, 0f, false);
            var glow = Mats.Emit(new Color(1f, 0.97f, 0.92f), 3f);
            const int seg = 14;
            const float radius = 0.17f;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var p = new Vector3(Mathf.Cos(a) * radius, 1.25f + Mathf.Sin(a) * radius, 0f);
                var b = Props.Box(n, new Vector3(0.08f, 0.025f, 0.025f), glow, p, new Vector3(0f, 0f, a * Mathf.Rad2Deg + 90f), 0f, false);
                var r = b != null ? b.GetComponent<MeshRenderer>() : null;
                if (r != null) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            _ring = Props.PointLight(n, new Vector3(0f, 1.25f, 0.2f), new Color(1f, 0.96f, 0.9f), 2.2f, 3f);
        }

        private void AnimateItem(float dt)
        {
            if (_item == null) return;
            if (_hopT > 0f)
            {
                _hopT = Mathf.Max(0f, _hopT - dt);
                float k = 1f - _hopT / HopDuration;
                _item.localPosition = _itemBase + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.25f);
                _item.localRotation = Quaternion.Euler(0f, k * 360f, 0f);
            }
            else
            {
                _item.localPosition = _itemBase;
                _item.localRotation = Quaternion.identity;
            }
        }

        // ---- Abschluss --------------------------------------------------------------------------------
        private void Finish()
        {
            var sim = Game.Sim;
            var take = new TikTokTake
            {
                Duration = _t,
                VisibleFrac = _measuredT > 0f ? _visT / _measuredT : 0f,
                Centered = _visT > 0f ? _centerSum / _visT : 0f,
                DistanceFrac = _visT > 0f ? _distOkT / _visT : 0f,
                SteadyFrac = _measuredT > 0f ? _steadyT / _measuredT : 0f,
                Light = _measuredT > 0f ? _lightSum / _measuredT : 0f,
                AngleDegrees = Mathf.Min(360f, _yawMax - _yawMin),
                Hook = _hook,
                Actions = _actions,
                BrandFrac = _measuredT > 0f ? _brandT / _measuredT : 0f,
                Hype = sim != null ? sim.TrendMult(_product) : 1f,
            };
            string product = _product, format = _format;
            var rating = TikTokScoring.Rate(take, format, sim != null ? sim.TrendingTikTokFormat() : "");
            Cleanup();
            Game.Sound("place");
            TikTokStudio.ShowResult(product, format, rating);
        }

        /// <summary>Bricht ab (Esc/B, Tagesende, Menü) und räumt auf. silent = ohne Hinweis.</summary>
        public void Cancel(bool silent = false)
        {
            if (!Active) return;
            Cleanup();
            if (!silent) Game.Sim?.Notify("Aufnahme abgebrochen.", "info");
        }

        private void Cleanup()
        {
            _phase = Phase.None;
            if (_set != null) Object.Destroy(_set);
            _set = null;
            _item = null;
            _boxVis = null;
            _productVis = null;
            _ring = null;
            _lights.Clear();
            _brands.Clear();
            if (_view != null) _view.Dispose();
            _view = null;
            if (_hudHidden)
            {
                _hudHidden = false;
                var ui = Game.UI;
                if (ui != null && ui.Hud != null && Game.Player != null) ui.Hud.SetVisible(true);
            }
            if (_handsOf != null) _handsOf.SetHandsVisible(true);
            _handsOf = null;
        }
    }
}
