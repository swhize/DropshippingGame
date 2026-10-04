using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Sucher für die echte TikTok-Aufnahme: 9:16-Rahmen in der Bildmitte (die Ränder links/rechts
    /// werden abgedunkelt), REC-Punkt, Timer mit Fortschritt (Markierung bei 10 s), Format/Produkt,
    /// Countdown und Live-Hinweise. Liegt als eigene Ebene direkt über dem HUD und fängt keine
    /// Klicks ab. Alle Stile inline, damit nichts von Game.uss abhängt.
    /// </summary>
    public sealed class TikTokViewfinder
    {
        private static readonly Color RecRed = new Color(1f, 0.17f, 0.33f);
        private static readonly Color Cyan = new Color(0.15f, 0.96f, 0.93f);
        private static readonly Color GoodGreen = new Color(0.4f, 0.92f, 0.55f);
        private static readonly Color WarnYellow = new Color(1f, 0.8f, 0.3f);

        private readonly VisualElement _root, _maskL, _maskR, _frame, _recDot, _fill, _minMark, _center, _hints, _keys, _trend;
        private readonly Label _time, _info, _countdown, _countdownSub, _recLabel;
        private string _hintSig = "", _keySig = "";
        private float _crop = -1f;

        /// <summary>Erstellt den Sucher über dem HUD. null, wenn es keine Oberfläche gibt.</summary>
        public static TikTokViewfinder Create()
        {
            var ui = Game.UI;
            if (ui == null || ui.Root == null) return null;
            try
            {
                return new TikTokViewfinder(ui.Root);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private TikTokViewfinder(VisualElement uiRoot)
        {
            _root = new VisualElement { name = "tiktok-viewfinder", pickingMode = PickingMode.Ignore };
            Abs(_root, 0f, 0f, 0f, 0f);
            // Direkt über dem HUD einsortieren (unter Handy, Fenstern, Toasts).
            var hud = uiRoot.Q<VisualElement>("hud");
            int idx = hud != null ? uiRoot.IndexOf(hud) : -1;
            if (idx >= 0 && idx + 1 <= uiRoot.childCount) uiRoot.Insert(idx + 1, _root);
            else uiRoot.Add(_root);

            var mask = new Color(0f, 0f, 0f, 0.74f);
            _maskL = Child(_root);
            _maskL.style.position = Position.Absolute;
            _maskL.style.left = 0;
            _maskL.style.top = 0;
            _maskL.style.bottom = 0;
            _maskL.style.backgroundColor = mask;
            _maskR = Child(_root);
            _maskR.style.position = Position.Absolute;
            _maskR.style.right = 0;
            _maskR.style.top = 0;
            _maskR.style.bottom = 0;
            _maskR.style.backgroundColor = mask;

            _frame = Child(_root);
            _frame.style.position = Position.Absolute;
            _frame.style.top = Length.Percent(1.5f);
            _frame.style.bottom = Length.Percent(1.5f);
            Border(_frame, 3f, new Color(1f, 1f, 1f, 0.85f), 22f);
            _frame.style.paddingLeft = 14;
            _frame.style.paddingRight = 14;
            _frame.style.paddingTop = 12;
            _frame.style.paddingBottom = 12;
            _frame.style.flexDirection = FlexDirection.Column;

            // ---- oben: REC + Zeit, Fortschritt, Format/Produkt -------------------------------
            var top = Child(_frame);
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            _recDot = Child(top);
            _recDot.style.width = 14;
            _recDot.style.height = 14;
            Round(_recDot, 7f);
            _recDot.style.backgroundColor = RecRed;
            _recDot.style.marginRight = 6;
            _recLabel = Lbl(top, "REC", 18f, Color.white, true);
            _recLabel.style.marginRight = 10;
            _time = Lbl(top, "00:00", 18f, Color.white, true);
            var spacer = Child(top);
            spacer.style.flexGrow = 1;
            Lbl(top, "max " + (int)TikTokScoring.MaxSeconds + " s", 13f, new Color(1f, 1f, 1f, 0.7f), false);

            var track = Child(_frame);
            track.style.height = 6;
            track.style.marginTop = 8;
            Round(track, 3f);
            track.style.backgroundColor = new Color(1f, 1f, 1f, 0.22f);
            _fill = Child(track);
            _fill.style.position = Position.Absolute;
            _fill.style.left = 0;
            _fill.style.top = 0;
            _fill.style.bottom = 0;
            _fill.style.width = Length.Percent(0f);
            Round(_fill, 3f);
            _fill.style.backgroundColor = RecRed;
            _minMark = Child(track);
            _minMark.style.position = Position.Absolute;
            _minMark.style.top = -3;
            _minMark.style.bottom = -3;
            _minMark.style.width = 2;
            _minMark.style.left = Length.Percent(TikTokScoring.MinSeconds / TikTokScoring.MaxSeconds * 100f);
            _minMark.style.backgroundColor = Color.white;

            var infoRow = Child(_frame);
            infoRow.style.flexDirection = FlexDirection.Row;
            infoRow.style.alignItems = Align.Center;
            infoRow.style.flexWrap = Wrap.Wrap;
            infoRow.style.marginTop = 8;
            _info = Lbl(infoRow, "", 15f, Color.white, true);
            _info.style.whiteSpace = WhiteSpace.Normal;
            _info.style.flexShrink = 1;
            _trend = Child(infoRow);
            _trend.style.marginLeft = 8;
            _trend.style.paddingLeft = 6;
            _trend.style.paddingRight = 6;
            _trend.style.paddingTop = 1;
            _trend.style.paddingBottom = 1;
            Round(_trend, 6f);
            _trend.style.backgroundColor = Cyan;
            Lbl(_trend, "TREND-FORMAT +" + Mathf.RoundToInt(TikTokScoring.TrendingFormatBonus * 100f) + " %", 12f, Color.black, true);
            UIX.Show(_trend, false);

            // ---- Mitte: Zielmarke + Countdown ------------------------------------------------
            _center = Child(_frame);
            _center.style.flexGrow = 1;
            _center.style.alignItems = Align.Center;
            _center.style.justifyContent = Justify.Center;
            // Zielmarke genau in der Bildschirmmitte (dort misst die Kamera "mittig").
            var reticle = Child(_root);
            reticle.style.position = Position.Absolute;
            reticle.style.left = Length.Percent(50f);
            reticle.style.top = Length.Percent(50f);
            reticle.style.width = 70;
            reticle.style.height = 70;
            reticle.style.marginLeft = -35;
            reticle.style.marginTop = -35;
            Border(reticle, 2f, new Color(1f, 1f, 1f, 0.45f), 10f);
            _countdown = Lbl(_center, "", 96f, Color.white, true);
            _countdown.style.unityTextAlign = TextAnchor.MiddleCenter;
            _countdownSub = Lbl(_center, "", 16f, Color.white, false);
            _countdownSub.style.unityTextAlign = TextAnchor.MiddleCenter;
            _countdownSub.style.whiteSpace = WhiteSpace.Normal;
            _countdownSub.style.maxWidth = 300;

            // ---- unten: Hinweise + Tasten ------------------------------------------------------
            _hints = Child(_frame);
            _hints.style.flexDirection = FlexDirection.Column;
            _hints.style.alignItems = Align.FlexStart;
            _keys = Child(_frame);
            _keys.style.flexDirection = FlexDirection.Row;
            _keys.style.flexWrap = Wrap.Wrap;
            _keys.style.marginTop = 8;

            Layout();
        }

        // =====================================================================================
        public void SetInfo(string format, string product, bool trending)
        {
            _info.text = (format ?? "") + " · " + (product ?? "");
            UIX.Show(_trend, trending);
        }

        /// <summary>Countdown vor der Aufnahme (0 = aus).</summary>
        public void SetCountdown(int n, string sub = null)
        {
            Layout();
            bool on = n > 0;
            _countdown.text = on ? n.ToString() : "";
            _countdownSub.text = on ? (sub ?? "") : "";
            UIX.Show(_countdown, on);
            UIX.Show(_countdownSub, on && !string.IsNullOrEmpty(sub));
            _recDot.style.opacity = on ? 0.35f : 1f;
            _recLabel.text = on ? "BEREIT" : "REC";
        }

        /// <summary>Laufzeit; canStop = Stopp schon erlaubt (für den Tastenhinweis).</summary>
        public void SetTime(float t, float max, bool canStop)
        {
            Layout();
            t = Mathf.Max(0f, t);
            int sec = Mathf.FloorToInt(t);
            _time.text = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
            float k = max > 0f ? Mathf.Clamp01(t / max) : 0f;
            _fill.style.width = Length.Percent(k * 100f);
            _fill.style.backgroundColor = t >= TikTokScoring.MinSeconds ? GoodGreen : RecRed;
            // REC-Punkt blinkt
            _recDot.style.opacity = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f ? 1f : 0.2f;
            _canStop = canStop;
        }

        private bool _canStop;

        /// <summary>Live-Hinweise (aus <see cref="TikTokScoring.FrameHints"/>) und Tastenleiste.</summary>
        public void SetHints(List<string> hints, string actionLabel)
        {
            string sig = hints != null ? string.Join("|", hints) : "";
            if (sig != _hintSig)
            {
                _hintSig = sig;
                _hints.Clear();
                if (hints != null)
                    foreach (var h in hints)
                        Hint(h);
            }
            string ks = (actionLabel ?? "") + _canStop + GameInput.UsingGamepad;
            if (ks != _keySig)
            {
                _keySig = ks;
                _keys.Clear();
                KeyHint(GameInput.KeyLabel("rec_action"), actionLabel ?? "Aktion");
                KeyHint(GameInput.KeyLabel("rec_stop"), _canStop ? "Stopp" : "Stopp ab " + (int)TikTokScoring.StopAfterSeconds + " s");
                KeyHint(GameInput.KeyLabel("rec_cancel"), "Abbrechen");
            }
        }

        private void Hint(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            bool good = text.EndsWith("✓");
            string clean = good ? text.Substring(0, text.Length - 1).TrimEnd() : text;
            bool bad = clean.EndsWith("!");
            var pill = Child(_hints);
            pill.style.flexDirection = FlexDirection.Row;
            pill.style.alignItems = Align.Center;
            pill.style.marginTop = 4;
            pill.style.paddingLeft = 8;
            pill.style.paddingRight = 10;
            pill.style.paddingTop = 3;
            pill.style.paddingBottom = 3;
            Round(pill, 10f);
            pill.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            var col = good ? GoodGreen : (bad ? RecRed : WarnYellow);
            if (good) UIX.Icon(pill, "check", 14f, col).style.marginRight = 5;
            else
            {
                var dot = Child(pill);
                dot.style.width = 8;
                dot.style.height = 8;
                Round(dot, 4f);
                dot.style.backgroundColor = col;
                dot.style.marginRight = 6;
            }
            Lbl(pill, clean, 15f, good ? Color.white : col, true);
        }

        private void KeyHint(string key, string text)
        {
            var r = Child(_keys);
            r.style.flexDirection = FlexDirection.Row;
            r.style.alignItems = Align.Center;
            r.style.marginRight = 12;
            r.style.marginTop = 4;
            UIX.Key(r, key ?? "").style.marginRight = 5;
            Lbl(r, text, 13f, Color.white, false);
        }

        /// <summary>Breite des 9:16-Ausschnitts an die Bildschirmgröße anpassen.</summary>
        private void Layout()
        {
            float crop = TikTokRecorder.CropWidth();
            if (Mathf.Abs(crop - _crop) < 0.001f) return;
            _crop = crop;
            float side = Mathf.Max(0f, (1f - crop) / 2f) * 100f;
            _maskL.style.width = Length.Percent(side);
            _maskR.style.width = Length.Percent(side);
            _frame.style.left = Length.Percent(side + 0.6f);
            _frame.style.right = Length.Percent(side + 0.6f);
        }

        public void Dispose()
        {
            _root?.RemoveFromHierarchy();
        }

        // ---- kleine Helfer ----------------------------------------------------------------------
        private static VisualElement Child(VisualElement parent)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            parent.Add(e);
            return e;
        }

        private static Label Lbl(VisualElement parent, string text, float size, Color color, bool bold)
        {
            var l = new Label(text ?? "") { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size;
            l.style.color = color;
            if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.marginLeft = 0;
            l.style.marginRight = 0;
            l.style.paddingLeft = 0;
            l.style.paddingRight = 0;
            parent.Add(l);
            return l;
        }

        private static void Abs(VisualElement e, float l, float t, float r, float b)
        {
            e.style.position = Position.Absolute;
            e.style.left = l;
            e.style.top = t;
            e.style.right = r;
            e.style.bottom = b;
        }

        private static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r;
            e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r;
            e.style.borderBottomRightRadius = r;
        }

        private static void Border(VisualElement e, float w, Color c, float radius)
        {
            e.style.borderLeftWidth = w;
            e.style.borderRightWidth = w;
            e.style.borderTopWidth = w;
            e.style.borderBottomWidth = w;
            e.style.borderLeftColor = c;
            e.style.borderRightColor = c;
            e.style.borderTopColor = c;
            e.style.borderBottomColor = c;
            Round(e, radius);
        }
    }
}
