using System;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Leistungsanzeige für Tests (Admin-Panel › Allgemein): FPS mit schlechtestem Frame, Draw-Calls
    /// (im Entwickler-Build echt, sonst sichtbare Renderer als Näherung), GC-Speicher pro Frame,
    /// NPCs, Fahrzeuge, aktive Lichter, Speicher und Fehlerzähler. Kostet nur etwas, solange sie sichtbar ist.
    /// </summary>
    public sealed class PerfOverlay : MonoBehaviour
    {
        private const string PrefKey = "ds.perfoverlay";
        private static PerfOverlay _inst;

        private Label _label;
        private float _acc, _slowAcc = 99f;
        private int _frames;
        private float _worst;
        private long _allocStart;
        private int _gcStart;
        private ProfilerRecorder _gcRec, _drawRec, _batchRec, _setPassRec, _trisRec;
        private bool _recorders;
        private long _gcFrameMax, _gcFrameSum;
        private int _renderers, _visible, _npcs;
        private readonly StringBuilder _sb = new StringBuilder(512);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _inst = null;

        /// <summary>Ist die Anzeige an?</summary>
        public static bool Visible => _inst != null && _inst.enabled && _inst._label != null && _inst._label.style.display != DisplayStyle.None;

        /// <summary>Beim Start: war die Anzeige zuletzt an, gleich wieder einschalten.</summary>
        public static void RestoreFromPrefs()
        {
            int on = 0;
            try { on = PlayerPrefs.GetInt(PrefKey, 0); }
            catch (Exception) { on = 0; }
            if (on == 1) SetVisible(true);
        }

        public static void SetVisible(bool on)
        {
            try { PlayerPrefs.SetInt(PrefKey, on ? 1 : 0); }
            catch (Exception) { /* egal */ }
            if (on)
            {
                if (_inst == null)
                {
                    var host = Game.UI != null ? Game.UI.gameObject : null;
                    if (host == null) return;
                    _inst = host.AddComponent<PerfOverlay>();
                }
                _inst.enabled = true;
                _inst.Show(true);
            }
            else if (_inst != null)
            {
                _inst.Show(false);
                _inst.enabled = false;
            }
        }

        public static void Toggle() => SetVisible(!Visible);

        private void Show(bool on)
        {
            if (on && _label == null) Build();
            if (_label != null) _label.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (on) StartRecorders();
            else StopRecorders();
            ResetWindow();
        }

        private void Build()
        {
            var root = Game.UI != null ? Game.UI.Root : null;
            if (root == null) return;
            _label = new Label("") { name = "perf-overlay", pickingMode = PickingMode.Ignore };
            var st = _label.style;
            st.position = Position.Absolute;
            // Oben mittig unter dem Kompass (links sitzt die Statuskarte, rechts die Bestellzettel).
            st.left = Length.Percent(50);
            st.top = 64;
            st.translate = new Translate(Length.Percent(-50), 0);
            st.paddingLeft = st.paddingRight = 10;
            st.paddingTop = st.paddingBottom = 6;
            st.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
            st.color = new Color(0.75f, 1f, 0.75f);
            st.fontSize = 14;
            st.whiteSpace = WhiteSpace.Normal;
            st.borderTopLeftRadius = st.borderTopRightRadius = st.borderBottomLeftRadius = st.borderBottomRightRadius = 6;
            root.Add(_label);
        }

        private void StartRecorders()
        {
            if (_recorders) return;
            _recorders = true;
            try
            {
                _gcRec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
                _drawRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
                _batchRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
                _setPassRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
                _trisRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            }
            catch (Exception e)
            {
                SafeLog.Warn("perf.recorder", e);
            }
        }

        private void StopRecorders()
        {
            if (!_recorders) return;
            _recorders = false;
            try
            {
                _gcRec.Dispose();
                _drawRec.Dispose();
                _batchRec.Dispose();
                _setPassRec.Dispose();
                _trisRec.Dispose();
            }
            catch (Exception)
            {
                // egal
            }
        }

        private void OnDestroy()
        {
            StopRecorders();
            if (_label != null) _label.RemoveFromHierarchy();
            if (_inst == this) _inst = null;
        }

        private void ResetWindow()
        {
            _acc = 0f;
            _frames = 0;
            _worst = 0f;
            _gcFrameMax = 0;
            _gcFrameSum = 0;
            _gcStart = GC.CollectionCount(0);
            _allocStart = AllocatedBytes();
        }

        private static long AllocatedBytes()
        {
            try { return GC.GetAllocatedBytesForCurrentThread(); }
            catch (Exception) { return 0; }
        }

        private void Update()
        {
            if (_label == null) return;
            float dt = Time.unscaledDeltaTime;
            _acc += dt;
            _frames++;
            if (dt > _worst) _worst = dt;
            if (_recorders && _gcRec.Valid)
            {
                long g = _gcRec.LastValue;
                _gcFrameSum += g;
                if (g > _gcFrameMax) _gcFrameMax = g;
            }
            _slowAcc += dt;
            if (_slowAcc >= 2f)
            {
                _slowAcc = 0f;
                CountScene();
            }
            if (_acc < 0.5f) return;
            try
            {
                Render();
            }
            catch (Exception e)
            {
                SafeLog.Warn("perf.overlay", e);
            }
            ResetWindow();
        }

        /// <summary>Szene zählen (teuer, daher nur alle 2 s und nur bei sichtbarer Anzeige).</summary>
        private void CountScene()
        {
            try
            {
                var rs = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                _renderers = rs.Length;
                int vis = 0;
                foreach (var r in rs)
                    if (r != null && r.enabled && r.isVisible) vis++;
                _visible = vis;
                _npcs = FindObjectsByType<NPC>(FindObjectsSortMode.None).Length;
            }
            catch (Exception e)
            {
                SafeLog.Warn("perf.count", e);
            }
        }

        private void Render()
        {
            float fps = _frames / Mathf.Max(0.0001f, _acc);
            var sb = _sb;
            sb.Length = 0;
            sb.Append("FPS ").Append(Mathf.RoundToInt(fps)).Append("  ·  Ø ").Append((1000f / Mathf.Max(1f, fps)).ToString("0.0"))
                .Append(" ms  ·  max ").Append((_worst * 1000f).ToString("0.0")).Append(" ms\n");

            if (_recorders && _drawRec.Valid && _drawRec.LastValue > 0)
            {
                sb.Append("Draw Calls ").Append(_drawRec.LastValue);
                if (_batchRec.Valid) sb.Append("  ·  Batches ").Append(_batchRec.LastValue);
                if (_setPassRec.Valid) sb.Append("  ·  SetPass ").Append(_setPassRec.LastValue);
                if (_trisRec.Valid) sb.Append("  ·  Tris ").Append((_trisRec.LastValue / 1000).ToString()).Append("k");
                sb.Append('\n');
            }
            sb.Append("Renderer sichtbar ").Append(_visible).Append(" / ").Append(_renderers).Append(" (Draw-Call-Näherung)\n");

            if (_recorders && _gcRec.Valid)
                sb.Append("GC/Frame Ø ").Append(Kb(_gcFrameSum / Math.Max(1, _frames))).Append("  ·  max ").Append(Kb(_gcFrameMax));
            else
                sb.Append("GC/Frame Ø ").Append(Kb((AllocatedBytes() - _allocStart) / Math.Max(1, _frames))).Append(" (Hauptthread)");
            int gcs = GC.CollectionCount(0) - _gcStart;
            if (gcs > 0) sb.Append("  ·  ").Append(gcs).Append("× GC");
            sb.Append("  ·  Heap ").Append((GC.GetTotalMemory(false) / (1024 * 1024)).ToString()).Append(" MB\n");

            sb.Append("NPCs ").Append(_npcs).Append("  ·  Fahrzeuge ").Append(CityTraffic.Count)
                .Append("  ·  Lichter ").Append(LightCuller.ActiveCount).Append(" / ").Append(LightCuller.ManagedCount)
                .Append("  ·  Pakete am Boden ").Append(DroppedItem.All.Count).Append('\n');
            sb.Append("Fehler ").Append(SessionLog.ErrorCount).Append("  ·  abgefangen ").Append(SafeLog.TotalReported)
                .Append("  ·  Qualität ").Append(Settings.QualityNames[Mathf.Clamp(Settings.Quality, 0, 2)]);
            _label.text = sb.ToString();
            var parent = _label.parent;
            if (parent != null && parent.IndexOf(_label) != parent.childCount - 1) _label.BringToFront();
        }

        private static string Kb(long bytes) => bytes < 1024 ? bytes + " B" : (bytes / 1024f).ToString("0.0") + " KB";
    }
}
