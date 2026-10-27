using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame
{
    /// <summary>
    /// Bildschirm-Konfetti und Funken über der gesamten Oberfläche (UI Toolkit): Level-Up, Rekordtag,
    /// TikTok gepostet, Perk/Skill freigeschaltet. Fester Pool aus kleinen VisualElements (keine Allokationen
    /// pro Frame, nur translate/rotate/scale/opacity), läuft in Echtzeit (auch bei Pause/Modals) und nimmt
    /// keine Klicks an. Optional ein großer „Stempel“-Text (z. B. NEUER REKORD!).
    /// </summary>
    public static class UiConfetti
    {
        private const int Capacity = 110;

        private static VisualElement _layer;
        private static Label _badge;
        private static float _badgeT = -1f, _badgeDur;
        private static readonly VisualElement[] Pieces = new VisualElement[Capacity];
        private static readonly float[] X = new float[Capacity], Y = new float[Capacity], Vx = new float[Capacity], Vy = new float[Capacity];
        private static readonly float[] Rot = new float[Capacity], Vr = new float[Capacity], Life = new float[Capacity], MaxLife = new float[Capacity];
        private static readonly float[] Flip = new float[Capacity], Delay = new float[Capacity];
        private static readonly bool[] Live = new bool[Capacity];
        private static int _next, _alive;

        public static readonly Color[] Hearts = { new Color(1f, 0.25f, 0.45f), new Color(1f, 0.45f, 0.6f), new Color(0.2f, 0.95f, 0.95f), Color.white };
        public static readonly Color[] GoldSet = { new Color(1f, 0.82f, 0.3f), new Color(1f, 0.92f, 0.6f), Color.white, new Color(1f, 0.7f, 0.2f) };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _layer = null;
            _badge = null;
            _badgeT = -1f;
            for (int i = 0; i < Capacity; i++)
            {
                Pieces[i] = null;
                Live[i] = false;
            }
            _next = 0;
            _alive = 0;
        }

        /// <summary>Schuss von einem Punkt (0..1 Bildschirmkoordinaten, y nach unten). palette null = bunt.</summary>
        public static void Burst(Vector2 origin01, int count, Color[] palette = null, float power = 1f, float delay = 0f)
        {
            if (!Feel.Enabled) return;
            try
            {
                if (!EnsureLayer()) return;
                float w = Mathf.Max(320f, _layer.resolvedStyle.width), h = Mathf.Max(240f, _layer.resolvedStyle.height);
                if (float.IsNaN(w) || float.IsNaN(h))
                {
                    w = 1600f;
                    h = 900f;
                }
                if (Settings.ReducedMotion) count = Mathf.Max(6, count / 2);
                var pal = palette ?? FeelFx.ConfettiPalette;
                for (int n = 0; n < count; n++)
                {
                    int i = Take();
                    float ang = UnityEngine.Random.Range(-160f, -20f) * Mathf.Deg2Rad;
                    float sp = UnityEngine.Random.Range(380f, 980f) * power;
                    Spawn(i, origin01.x * w, origin01.y * h, Mathf.Cos(ang) * sp, Mathf.Sin(ang) * sp, pal, delay + UnityEngine.Random.Range(0f, 0.06f));
                }
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        /// <summary>Konfetti-Regen von oben über die ganze Breite.</summary>
        public static void Rain(int count, Color[] palette = null, float delay = 0f)
        {
            if (!Feel.Enabled) return;
            try
            {
                if (!EnsureLayer()) return;
                float w = _layer.resolvedStyle.width;
                if (float.IsNaN(w) || w < 100f) w = 1600f;
                if (Settings.ReducedMotion) count = Mathf.Max(8, count / 2);
                var pal = palette ?? FeelFx.ConfettiPalette;
                for (int n = 0; n < count; n++)
                {
                    int i = Take();
                    Spawn(i, UnityEngine.Random.Range(0f, w), UnityEngine.Random.Range(-140f, -10f), UnityEngine.Random.Range(-60f, 60f),
                        UnityEngine.Random.Range(40f, 220f), pal, delay + UnityEngine.Random.Range(0f, 1.2f));
                    MaxLife[i] += 1.2f;
                }
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        /// <summary>Großer schräger Stempel-Text in der Bildmitte (ploppt auf, bleibt kurz, blendet aus).</summary>
        public static void Badge(string text, Color color, float delay = 0f, float duration = 2.4f)
        {
            if (!Feel.Enabled || string.IsNullOrEmpty(text)) return;
            try
            {
                if (!EnsureLayer()) return;
                if (_badge == null)
                {
                    _badge = new Label { pickingMode = PickingMode.Ignore };
                    _badge.style.position = Position.Absolute;
                    _badge.style.left = new Length(0f, LengthUnit.Percent);
                    _badge.style.right = new Length(0f, LengthUnit.Percent);
                    _badge.style.top = new Length(18f, LengthUnit.Percent);
                    _badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                    _badge.style.fontSize = 64;
                    _badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                    _badge.style.textShadow = new TextShadow { offset = new Vector2(0f, 4f), blurRadius = 8f, color = new Color(0f, 0f, 0f, 0.55f) };
                    _layer.Add(_badge);
                }
                _badge.text = text;
                _badge.style.color = color;
                _badge.style.opacity = 0f;
                _badgeT = -Mathf.Max(0f, delay);
                _badgeDur = Mathf.Max(0.6f, duration);
                _layer.BringToFront();
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        private static int Take()
        {
            for (int k = 0; k < Capacity; k++)
            {
                int i = (_next + k) % Capacity;
                if (!Live[i])
                {
                    _next = (i + 1) % Capacity;
                    return i;
                }
            }
            int j = _next;
            _next = (_next + 1) % Capacity;
            return j; // ältestes überschreiben
        }

        private static void Spawn(int i, float x, float y, float vx, float vy, Color[] pal, float delay)
        {
            var el = Pieces[i];
            if (el == null)
            {
                el = new VisualElement { pickingMode = PickingMode.Ignore };
                el.style.position = Position.Absolute;
                el.style.left = 0;
                el.style.top = 0;
                Pieces[i] = el;
                _layer.Add(el);
            }
            bool round = UnityEngine.Random.value < 0.3f;
            float size = UnityEngine.Random.Range(7f, 13f);
            el.style.width = size;
            el.style.height = round ? size : size * UnityEngine.Random.Range(1.4f, 2.1f);
            float rad = round ? size / 2f : 2f;
            el.style.borderTopLeftRadius = rad;
            el.style.borderTopRightRadius = rad;
            el.style.borderBottomLeftRadius = rad;
            el.style.borderBottomRightRadius = rad;
            el.style.backgroundColor = pal[UnityEngine.Random.Range(0, pal.Length)];
            el.style.opacity = 0f;
            el.style.display = DisplayStyle.Flex;
            X[i] = x;
            Y[i] = y;
            Vx[i] = vx;
            Vy[i] = vy;
            Rot[i] = UnityEngine.Random.Range(0f, 360f);
            Vr[i] = UnityEngine.Random.Range(-540f, 540f);
            Flip[i] = UnityEngine.Random.Range(4f, 11f);
            Life[i] = 0f;
            MaxLife[i] = UnityEngine.Random.Range(1.6f, 2.6f);
            Delay[i] = delay;
            if (!Live[i]) _alive++;
            Live[i] = true;
            _layer.BringToFront();
        }

        private static bool EnsureLayer()
        {
            var ui = Game.UI;
            if (ui == null || ui.Root == null) return false;
            FeelRunner.Ensure();
            if (_layer != null && _layer.parent == ui.Root) return true;
            for (int i = 0; i < Capacity; i++)
            {
                Pieces[i] = null;
                Live[i] = false;
            }
            _alive = 0;
            _badge = null;
            _layer = new VisualElement { name = "feel-confetti", pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            _layer.style.left = 0;
            _layer.style.top = 0;
            _layer.style.right = 0;
            _layer.style.bottom = 0;
            _layer.style.overflow = Overflow.Hidden;
            ui.Root.Add(_layer);
            return true;
        }

        internal static void Tick(float dt)
        {
            if (_layer == null) return;
            if (dt > 0.1f) dt = 0.1f;
            if (_alive > 0)
            {
                float h = _layer.resolvedStyle.height;
                if (float.IsNaN(h) || h < 100f) h = 900f;
                for (int i = 0; i < Capacity; i++)
                {
                    if (!Live[i]) continue;
                    var el = Pieces[i];
                    if (el == null)
                    {
                        Live[i] = false;
                        _alive--;
                        continue;
                    }
                    if (Delay[i] > 0f)
                    {
                        Delay[i] -= dt;
                        continue;
                    }
                    Life[i] += dt;
                    float t = Life[i] / MaxLife[i];
                    if (t >= 1f || Y[i] > h + 40f)
                    {
                        Live[i] = false;
                        _alive--;
                        el.style.display = DisplayStyle.None;
                        continue;
                    }
                    // Luftwiderstand + Schwerkraft + Flattern
                    float drag = Mathf.Exp(-2.4f * dt);
                    Vx[i] *= drag;
                    Vy[i] = Vy[i] * drag + 900f * dt;
                    if (Vy[i] > 260f) Vy[i] = 260f;
                    X[i] += (Vx[i] + Mathf.Sin(Life[i] * Flip[i]) * 40f) * dt;
                    Y[i] += Vy[i] * dt;
                    Rot[i] += Vr[i] * dt;
                    el.style.translate = new Translate(X[i], Y[i]);
                    el.style.rotate = new Rotate(new Angle(Rot[i], AngleUnit.Degree));
                    el.style.scale = new Scale(new Vector3(Mathf.Cos(Life[i] * Flip[i]), 1f, 1f));
                    el.style.opacity = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
                }
                if (_alive < 0) _alive = 0;
            }
            if (_badge != null && _badgeT > -100f)
            {
                _badgeT += dt;
                if (_badgeT < 0f) return;
                float p = _badgeT / _badgeDur;
                if (p >= 1f)
                {
                    _badge.style.opacity = 0f;
                    _badgeT = -1000f;
                    return;
                }
                float pop = p < 0.12f ? FeelTween.Eval(FeelEase.OutBack, p / 0.12f) : 1f;
                float s = Mathf.LerpUnclamped(1.8f, 1f, pop);
                _badge.style.scale = new Scale(new Vector3(s, s, 1f));
                _badge.style.rotate = new Rotate(new Angle(-6f, AngleUnit.Degree));
                _badge.style.opacity = p < 0.12f ? p / 0.12f : (p > 0.8f ? 1f - (p - 0.8f) / 0.2f : 1f);
            }
        }
    }
}
