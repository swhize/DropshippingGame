using System;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Kurven für <see cref="FeelTween"/> (ergänzt <see cref="Ease"/> um Rückfedern und Hüpfen).</summary>
    public enum FeelEase
    {
        Linear,
        InQuad,
        OutQuad,
        InOutSine,
        OutCubic,
        OutBack,
        InBack,
        OutElastic,
        OutBounce,
    }

    /// <summary>
    /// „Juice“-Tweens ohne Coroutinen und ohne Allokationen pro Frame: feste Slot-Tabelle (struct-Array),
    /// ein gemeinsamer Runner (<see cref="FeelRunner"/>). Squash &amp; Stretch, Punch, Shake, Bewegen,
    /// Drehen, Skalieren, Ein-/Ausblenden und verzögerte Aufrufe. Läuft in Spielzeit (Pause friert ein).
    /// Wiederholte Squash/Punch/Shake auf dasselbe Objekt behalten die Grundform (kein Aufschaukeln).
    /// Ist die Tabelle voll, wird der Endzustand sofort gesetzt – Effekte sind immer optional.
    /// </summary>
    public static class FeelTween
    {
        private enum Kind : byte
        {
            None,
            Squash,
            Punch,
            Shake,
            Move,
            Rotate,
            Scale,
            Show,
            Hide,
            Destroy,
            Call,
        }

        private struct Slot
        {
            public Transform Tr;
            public Kind Kind;
            public FeelEase Ease;
            public bool Started;
            public float Delay, T, Dur, Amp;
            public Vector3 A, B, Axis;
            public Quaternion QA, QB;
            public Action Done;
        }

        public const int Capacity = 192;
        private static readonly Slot[] Slots = new Slot[Capacity];
        private static int _high;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (int i = 0; i < Capacity; i++) Slots[i] = default;
            _high = 0;
        }

        public static int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _high; i++)
                    if (Slots[i].Kind != Kind.None) n++;
                return n;
            }
        }

        // ---- Kurven ------------------------------------------------------------------------------
        public static float Eval(FeelEase e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case FeelEase.InQuad: return t * t;
                case FeelEase.OutQuad: return 1f - (1f - t) * (1f - t);
                case FeelEase.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case FeelEase.OutCubic:
                {
                    float u = 1f - t;
                    return 1f - u * u * u;
                }
                case FeelEase.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                case FeelEase.InBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }
                case FeelEase.OutElastic:
                {
                    if (t <= 0f || t >= 1f) return t;
                    const float c4 = 2f * Mathf.PI / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case FeelEase.OutBounce:
                {
                    const float n1 = 7.5625f, d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1)
                    {
                        t -= 1.5f / d1;
                        return n1 * t * t + 0.75f;
                    }
                    if (t < 2.5f / d1)
                    {
                        t -= 2.25f / d1;
                        return n1 * t * t + 0.9375f;
                    }
                    t -= 2.625f / d1;
                    return n1 * t * t + 0.984375f;
                }
                default: return t;
            }
        }

        // ---- Öffentliche API -----------------------------------------------------------------------
        /// <summary>
        /// Squash &amp; Stretch: gedämpftes Federn um die Grundskalierung (Volumen bleibt ungefähr erhalten).
        /// amount 0.1 = dezent, 0.3 = cartoonig.
        /// </summary>
        public static void Squash(Transform tr, float amount = 0.18f, float duration = 0.38f, float delay = 0f)
        {
            if (tr == null) return;
            amount *= FeelSettings.MotionScale;
            int i = Find(tr, Kind.Squash);
            Vector3 baseScale = i >= 0 ? Slots[i].A : tr.localScale;
            int scaleSlot = Find(tr, Kind.Scale);
            if (scaleSlot >= 0) baseScale = Slots[scaleSlot].B;
            if (i < 0) i = Alloc();
            if (i < 0) return;
            Slots[i] = new Slot { Tr = tr, Kind = Kind.Squash, Dur = Mathf.Max(0.05f, duration), Delay = delay, Amp = amount, A = baseScale, Started = true };
        }

        /// <summary>Punch: Stoß in eine Richtung (lokal), der zurückfedert.</summary>
        public static void Punch(Transform tr, Vector3 localOffset, float duration = 0.3f, float delay = 0f)
        {
            if (tr == null) return;
            localOffset *= FeelSettings.MotionScale;
            int i = Find(tr, Kind.Punch);
            Vector3 basePos = i >= 0 ? Slots[i].A : tr.localPosition;
            if (i < 0) i = Alloc();
            if (i < 0) return;
            Slots[i] = new Slot { Tr = tr, Kind = Kind.Punch, Dur = Mathf.Max(0.05f, duration), Delay = delay, Axis = localOffset, A = basePos, Started = true };
        }

        /// <summary>Zittern (lokal, weich über Perlin-Rauschen), klingt linear aus.</summary>
        public static void Shake(Transform tr, float amplitude = 0.01f, float duration = 0.3f, float delay = 0f)
        {
            if (tr == null) return;
            amplitude *= FeelSettings.MotionScale;
            int i = Find(tr, Kind.Shake);
            Vector3 basePos = i >= 0 ? Slots[i].A : tr.localPosition;
            if (i < 0) i = Alloc();
            if (i < 0) return;
            float seed = (tr.GetInstanceID() & 1023) * 0.37f;
            Slots[i] = new Slot { Tr = tr, Kind = Kind.Shake, Dur = Mathf.Max(0.05f, duration), Delay = delay, Amp = amplitude, A = basePos, Axis = new Vector3(seed, seed + 17.3f, seed + 41.9f), Started = true };
        }

        /// <summary>Bewegt lokal zum Ziel; arc &gt; 0 = Bogen nach oben (Wurf).</summary>
        public static void Move(Transform tr, Vector3 to, float duration, FeelEase ease = FeelEase.OutCubic, float delay = 0f, float arc = 0f)
        {
            if (tr == null) return;
            int i = Find(tr, Kind.Move);
            if (i < 0) i = Alloc();
            if (i < 0)
            {
                tr.localPosition = to;
                return;
            }
            Slots[i] = new Slot { Tr = tr, Kind = Kind.Move, Dur = Mathf.Max(0.01f, duration), Delay = delay, B = to, Ease = ease, Amp = arc };
        }

        public static void Rotate(Transform tr, Quaternion to, float duration, FeelEase ease = FeelEase.OutCubic, float delay = 0f)
        {
            if (tr == null) return;
            int i = Find(tr, Kind.Rotate);
            if (i < 0) i = Alloc();
            if (i < 0)
            {
                tr.localRotation = to;
                return;
            }
            Slots[i] = new Slot { Tr = tr, Kind = Kind.Rotate, Dur = Mathf.Max(0.01f, duration), Delay = delay, QB = to, Ease = ease };
        }

        public static void Scale(Transform tr, Vector3 to, float duration, FeelEase ease = FeelEase.OutCubic, float delay = 0f)
        {
            if (tr == null) return;
            int i = Find(tr, Kind.Scale);
            if (i < 0) i = Alloc();
            if (i < 0)
            {
                tr.localScale = to;
                return;
            }
            Slots[i] = new Slot { Tr = tr, Kind = Kind.Scale, Dur = Mathf.Max(0.01f, duration), Delay = delay, B = to, Ease = ease };
        }

        /// <summary>Auftauchen: sofort unsichtbar klein, nach delay mit Überschwingen auf die Zielgröße.</summary>
        public static void PopIn(Transform tr, float duration = 0.25f, float delay = 0f, Vector3? target = null)
        {
            if (tr == null) return;
            int s = Find(tr, Kind.Scale);
            Vector3 to = target ?? (s >= 0 ? Slots[s].B : tr.localScale);
            if (to.sqrMagnitude < 1e-6f) to = Vector3.one;
            tr.localScale = Vector3.zero;
            Scale(tr, to, duration, FeelEase.OutBack, delay);
        }

        public static void Show(Transform tr, float delay)
        {
            if (tr == null) return;
            if (delay <= 0f)
            {
                tr.gameObject.SetActive(true);
                return;
            }
            Instant(tr, Kind.Show, delay, null);
        }

        public static void Hide(Transform tr, float delay)
        {
            if (tr == null) return;
            if (delay <= 0f)
            {
                tr.gameObject.SetActive(false);
                return;
            }
            Instant(tr, Kind.Hide, delay, null);
        }

        public static void DestroyAfter(Transform tr, float delay)
        {
            if (tr == null) return;
            Instant(tr, Kind.Destroy, delay, null);
        }

        /// <summary>Ruft action nach delay Sekunden Spielzeit auf (Fehler werden geschluckt).</summary>
        public static void Call(float delay, Action action)
        {
            if (action == null) return;
            if (delay <= 0f)
            {
                Feel.Safe(action);
                return;
            }
            Instant(null, Kind.Call, delay, action);
        }

        /// <summary>Stoppt alle Tweens eines Objekts. complete = Endzustand setzen (Grundform wiederherstellen).</summary>
        public static void Kill(Transform tr, bool complete = true)
        {
            if (tr == null) return;
            for (int i = 0; i < _high; i++)
            {
                if (Slots[i].Kind == Kind.None || !ReferenceEquals(Slots[i].Tr, tr)) continue;
                if (complete) Finish(ref Slots[i]);
                Slots[i] = default;
            }
        }

        // ---- Intern --------------------------------------------------------------------------------
        private static void Instant(Transform tr, Kind kind, float delay, Action done)
        {
            int i = Alloc();
            if (i < 0)
            {
                var tmp = new Slot { Tr = tr, Kind = kind, Done = done };
                Finish(ref tmp);
                return;
            }
            Slots[i] = new Slot { Tr = tr, Kind = kind, Delay = delay, Dur = 0f, Done = done };
        }

        private static int Find(Transform tr, Kind kind)
        {
            for (int i = 0; i < _high; i++)
                if (Slots[i].Kind == kind && ReferenceEquals(Slots[i].Tr, tr)) return i;
            return -1;
        }

        private static int Alloc()
        {
            FeelRunner.Ensure();
            for (int i = 0; i < Capacity; i++)
            {
                if (Slots[i].Kind != Kind.None) continue;
                if (i >= _high) _high = i + 1;
                return i;
            }
            return -1;
        }

        private static bool NeedsTransform(Kind k) => k != Kind.Call;

        internal static void Tick(float dt)
        {
            for (int i = 0; i < _high; i++)
            {
                if (Slots[i].Kind == Kind.None) continue;
                ref var s = ref Slots[i];
                if (NeedsTransform(s.Kind) && s.Tr == null)
                {
                    s = default;
                    continue;
                }
                if (s.Delay > 0f)
                {
                    s.Delay -= dt;
                    if (s.Delay > 0f) continue;
                }
                if (!s.Started)
                {
                    s.Started = true;
                    if (s.Kind == Kind.Move) s.A = s.Tr.localPosition;
                    else if (s.Kind == Kind.Rotate) s.QA = s.Tr.localRotation;
                    else if (s.Kind == Kind.Scale) s.A = s.Tr.localScale;
                }
                s.T += dt;
                if (s.T >= s.Dur)
                {
                    var copy = s;
                    s = default;
                    Finish(ref copy);
                    continue;
                }
                Apply(ref s, s.T / s.Dur);
            }
            // Callbacks dürfen neue Tweens anlegen – Obergrenze erst danach einkürzen.
            int h = _high;
            while (h > 0 && Slots[h - 1].Kind == Kind.None) h--;
            _high = h;
        }

        private static void Apply(ref Slot s, float p)
        {
            var tr = s.Tr;
            switch (s.Kind)
            {
                case Kind.Squash:
                {
                    float d = 1f - p;
                    float k = s.Amp * d * d * Mathf.Sin(p * Mathf.PI * 3f);
                    tr.localScale = new Vector3(s.A.x * (1f + k * 0.5f), s.A.y * (1f - k), s.A.z * (1f + k * 0.5f));
                    break;
                }
                case Kind.Punch:
                {
                    float d = 1f - p;
                    tr.localPosition = s.A + s.Axis * (d * d * Mathf.Sin(p * Mathf.PI * 3f));
                    break;
                }
                case Kind.Shake:
                {
                    float d = (1f - p) * s.Amp;
                    float f = s.T * 28f;
                    var off = new Vector3(Mathf.PerlinNoise(s.Axis.x, f) - 0.5f, Mathf.PerlinNoise(s.Axis.y, f) - 0.5f, Mathf.PerlinNoise(s.Axis.z, f) - 0.5f);
                    tr.localPosition = s.A + off * (2f * d);
                    break;
                }
                case Kind.Move:
                {
                    float e = Eval(s.Ease, p);
                    var pos = Vector3.LerpUnclamped(s.A, s.B, e);
                    if (s.Amp != 0f) pos.y += s.Amp * 4f * p * (1f - p);
                    tr.localPosition = pos;
                    break;
                }
                case Kind.Rotate:
                    tr.localRotation = Quaternion.SlerpUnclamped(s.QA, s.QB, Eval(s.Ease, p));
                    break;
                case Kind.Scale:
                    tr.localScale = Vector3.LerpUnclamped(s.A, s.B, Eval(s.Ease, p));
                    break;
            }
        }

        private static void Finish(ref Slot s)
        {
            try
            {
                var tr = s.Tr;
                switch (s.Kind)
                {
                    case Kind.Squash:
                        if (tr != null) tr.localScale = s.A;
                        break;
                    case Kind.Punch:
                    case Kind.Shake:
                        if (tr != null) tr.localPosition = s.A;
                        break;
                    case Kind.Move:
                        if (tr != null) tr.localPosition = s.B;
                        break;
                    case Kind.Rotate:
                        if (tr != null) tr.localRotation = s.QB;
                        break;
                    case Kind.Scale:
                        if (tr != null) tr.localScale = s.B;
                        break;
                    case Kind.Show:
                        if (tr != null) tr.gameObject.SetActive(true);
                        break;
                    case Kind.Hide:
                        if (tr != null) tr.gameObject.SetActive(false);
                        break;
                    case Kind.Destroy:
                        if (tr != null) UnityEngine.Object.Destroy(tr.gameObject);
                        break;
                    case Kind.Call:
                        s.Done?.Invoke();
                        break;
                }
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }
    }
}
