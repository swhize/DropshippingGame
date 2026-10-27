using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>Gemeinsame Helfer des Feedback-Systems („Juice“): fehlertolerantes Ausführen, einmaliges Loggen.</summary>
    public static class Feel
    {
        /// <summary>Globaler Notschalter (z. B. für Tests oder falls ein Effekt auf einer Plattform Probleme macht).</summary>
        public static bool Enabled = true;

        private static readonly HashSet<string> Logged = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Enabled = true;
            Logged.Clear();
        }

        /// <summary>Führt einen Effekt aus; Fehler werden nur einmal pro Art geloggt und nie weitergereicht.</summary>
        public static void Safe(Action a)
        {
            if (a == null || !Enabled) return;
            try
            {
                a();
            }
            catch (Exception e)
            {
                LogOnce(e);
            }
        }

        public static void LogOnce(Exception e)
        {
            if (e == null) return;
            string key = e.GetType().Name + ":" + e.Message;
            if (Logged.Count > 64 || !Logged.Add(key)) return;
            Debug.LogWarning("[Feel] Effekt übersprungen: " + e);
        }

        /// <summary>Weltposition knapp vor der Kamera (für Effekte „beim Spieler“).</summary>
        public static Vector3 InFrontOfCamera(float distance = 1.1f, float down = 0.15f)
        {
            var p = Game.Player;
            var cam = p != null && p.Cam != null ? p.Cam.transform : (Camera.main != null ? Camera.main.transform : null);
            if (cam == null) return Vector3.zero;
            return cam.position + cam.forward * distance - cam.up * down;
        }

        /// <summary>Kamera-Entfernung für Kappung (weit entfernte Effekte werden ausgelassen).</summary>
        public static float CameraDistanceSqr(Vector3 pos)
        {
            var cam = Camera.main;
            if (cam == null) return 0f;
            return (cam.transform.position - pos).sqrMagnitude;
        }
    }

    /// <summary>Liest die Bewegungs-Einstellungen (Settings.ReducedMotion) für alle Effekte.</summary>
    public static class FeelSettings
    {
        /// <summary>Faktor für Kamera-Wackler, Federn und Schwanken: 1 normal, deutlich weniger bei „Weniger Bewegung“.</summary>
        public static float MotionScale => Settings.ReducedMotion ? 0.35f : 1f;

        /// <summary>Kamera-Shake und FOV-Kicks ganz aus bei „Weniger Bewegung“.</summary>
        public static bool CameraMotion => !Settings.ReducedMotion;
    }

    /// <summary>
    /// Ein unsichtbares Objekt, das alle Feedback-Systeme pro Frame antreibt (Tweens, Zahlen-Pops, UI-Konfetti).
    /// Wird beim ersten Gebrauch angelegt und überlebt Szenenwechsel.
    /// </summary>
    public sealed class FeelRunner : MonoBehaviour
    {
        private static FeelRunner _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        public static Transform Root => Ensure() != null ? _instance.transform : null;

        public static FeelRunner Ensure()
        {
            if (_instance != null) return _instance;
            if (!Application.isPlaying) return null;
            var go = new GameObject("Feel");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FeelRunner>();
            return _instance;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            try
            {
                FeelTween.Tick(dt);
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
            try
            {
                FeelPops.Tick(dt);
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
            try
            {
                UiConfetti.Tick(Time.unscaledDeltaTime);
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
            try
            {
                FeelAudio.Tick(Time.unscaledDeltaTime);
            }
            catch (Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
