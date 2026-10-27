using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Sounds für Feedback-Effekte: über <see cref="AudioManager"/> (Aufnahme vor Synth-Fallback aus
    /// <see cref="Core.AudioSynth"/>), mit Tonhöhen-Streuung gegen Wiederholung, Mindestabstand pro Name
    /// (kein Aufsummieren bei Stapeln) und „Tonleiter“ für Serien (Münzen, Pakete hintereinander).
    /// </summary>
    public static class FeelAudio
    {
        private static readonly Dictionary<string, float> Last = new Dictionary<string, float>();
        private static float _now;
        private static float _ladderAt = -10f;
        private static int _ladderStep;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Last.Clear();
            _now = 0f;
            _ladderAt = -10f;
            _ladderStep = 0;
        }

        internal static void Tick(float unscaledDt) => _now += unscaledDt;

        /// <summary>
        /// Spielt einen Effekt. pitch = Grund-Tonhöhe, pitchVar = Zufallsstreuung (±), minGap = gleicher Name
        /// frühestens nach so vielen Sekunden erneut. delay in Spielzeit.
        /// </summary>
        public static void Play(string name, float pitch = 1f, float pitchVar = 0.06f, float volumeDb = 0f, float delay = 0f, float minGap = 0.04f)
        {
            if (string.IsNullOrEmpty(name) || !Feel.Enabled) return;
            if (delay > 0f)
            {
                FeelTween.Call(delay, () => Play(name, pitch, pitchVar, volumeDb, 0f, minGap));
                return;
            }
            FeelRunner.Ensure();
            if (Last.TryGetValue(name, out float at) && _now - at < minGap) return;
            Last[name] = _now;
            var a = Game.Audio;
            if (a == null) return;
            try
            {
                a.PlayPitched(name, pitch, pitchVar, volumeDb);
            }
            catch (System.Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        /// <summary>
        /// Steigende Tonhöhe für schnelle Serien: jede Aktion innerhalb von window Sekunden klingt einen
        /// Halbton höher (max. eine Quinte), danach beginnt die Leiter von vorn.
        /// </summary>
        public static float Ladder(float window = 1.4f)
        {
            FeelRunner.Ensure();
            if (_now - _ladderAt > window) _ladderStep = 0;
            else _ladderStep = Mathf.Min(_ladderStep + 1, 7);
            _ladderAt = _now;
            return Mathf.Pow(2f, _ladderStep / 12f);
        }
    }
}
