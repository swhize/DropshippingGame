using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    public enum Ease
    {
        Linear,
        InOutSine,
        OutQuad,
        InQuad,
        OutBack,
        OutCubic,
    }

    /// <summary>
    /// Kleines Animationssystem (ersetzt die Godot-Tweens). Läuft über Coroutinen auf einem
    /// unsichtbaren Objekt. unscaled = true läuft auch, wenn das Spiel pausiert ist (für Menüs).
    /// </summary>
    public sealed class Anim : MonoBehaviour
    {
        private static Anim _runner;
        private readonly Dictionary<object, Coroutine> _keyed = new Dictionary<object, Coroutine>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _runner = null;

        private static Anim Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("Anim");
                    DontDestroyOnLoad(go);
                    _runner = go.AddComponent<Anim>();
                }
                return _runner;
            }
        }

        public static float Apply(Ease e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InQuad: return t * t;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                default: return t;
            }
        }

        /// <summary>Ruft step(0..1) über die Dauer auf. key: gleicher Schlüssel bricht die vorige Animation ab.</summary>
        public static Coroutine Run(float duration, Action<float> step, Action done = null, Ease ease = Ease.Linear,
            bool unscaled = false, object key = null, float delay = 0f)
        {
            var r = Runner;
            if (key != null) Stop(key);
            var co = r.StartCoroutine(r.Routine(duration, step, done, ease, unscaled, key, delay));
            if (key != null) r._keyed[key] = co;
            return co;
        }

        public static void Stop(object key)
        {
            if (_runner == null || key == null) return;
            if (_runner._keyed.TryGetValue(key, out var co))
            {
                if (co != null) _runner.StopCoroutine(co);
                _runner._keyed.Remove(key);
            }
        }

        public static Coroutine Delay(float seconds, Action then, bool unscaled = false, object key = null) =>
            Run(0f, null, then, Ease.Linear, unscaled, key, seconds);

        public static Coroutine Start(IEnumerator routine) => Runner.StartCoroutine(routine);

        private IEnumerator Routine(float duration, Action<float> step, Action done, Ease ease, bool unscaled, object key, float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            t = 0f;
            while (t < duration)
            {
                step?.Invoke(Apply(ease, t / duration));
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            step?.Invoke(1f);
            if (key != null) _keyed.Remove(key);
            done?.Invoke();
        }

        /// <summary>Bewegt ein Objekt zur Zielposition (lokal).</summary>
        public static Coroutine MoveLocal(Transform tr, Vector3 to, float duration, Ease ease = Ease.Linear, Action done = null)
        {
            Vector3 from = tr.localPosition;
            return Run(duration, t =>
            {
                if (tr != null) tr.localPosition = Vector3.LerpUnclamped(from, to, t);
            }, done, ease, false, tr);
        }
    }
}
