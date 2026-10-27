using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Kamera-Mikrofeedback: kurzer FOV-Kick und weiches Wackeln (Position + etwas Rollen).
    /// Wird additiv in LateUpdate aufgetragen und im nächsten Frame wieder abgezogen, solange niemand
    /// sonst die Kamera verändert hat – stört also weder Kopfwippen noch Blicksteuerung.
    /// Bei Settings.ReducedMotion komplett aus.
    /// </summary>
    public sealed class CameraFeel : MonoBehaviour
    {
        private static CameraFeel _active;

        private Camera _cam;
        private float _kick, _kickVel;
        private float _trauma, _traumaDur = 0.3f, _traumaT;
        private float _seed;

        private bool _applied;
        private Vector3 _appliedPos, _setPos;
        private Quaternion _appliedRot = Quaternion.identity, _setRot;
        private float _appliedFov, _setFov;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _active = null;

        /// <summary>FOV-Kick in Grad (positiv = weiter, negativ = „Zoom“-Ruck). Federt zurück.</summary>
        public static void Kick(float fovDegrees)
        {
            if (!FeelSettings.CameraMotion || !Feel.Enabled) return;
            var c = Get();
            if (c == null) return;
            c._kickVel += fovDegrees * 22f;
        }

        /// <summary>Kurzes Wackeln. strength 0..1 (0.15 = Abstellen, 0.4 = Kiste, 0.7 = großer Moment).</summary>
        public static void Shake(float strength, float duration = 0.25f)
        {
            if (!FeelSettings.CameraMotion || !Feel.Enabled) return;
            var c = Get();
            if (c == null) return;
            c._trauma = Mathf.Clamp01(Mathf.Max(c._trauma * (1f - c._traumaT / Mathf.Max(0.01f, c._traumaDur)), strength));
            c._traumaDur = Mathf.Max(0.05f, duration);
            c._traumaT = 0f;
        }

        private static CameraFeel Get()
        {
            if (_active != null && _active.isActiveAndEnabled) return _active;
            var p = Game.Player;
            if (p == null || p.Cam == null) return null;
            _active = p.Cam.GetComponent<CameraFeel>();
            if (_active == null) _active = p.Cam.gameObject.AddComponent<CameraFeel>();
            return _active;
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _seed = Random.value * 100f;
        }

        private void OnDisable() => Undo();

        private void Undo()
        {
            if (!_applied) return;
            _applied = false;
            var tr = transform;
            if (tr.localPosition == _setPos) tr.localPosition = _setPos - _appliedPos;
            if (tr.localRotation == _setRot) tr.localRotation = _setRot * Quaternion.Inverse(_appliedRot);
            if (_cam != null && Mathf.Approximately(_cam.fieldOfView, _setFov)) _cam.fieldOfView = _setFov - _appliedFov;
        }

        private void LateUpdate()
        {
            Undo();
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            // FOV-Feder (kritisch gedämpft)
            const float k = 260f, d = 26f;
            _kickVel += (-k * _kick - d * _kickVel) * dt;
            _kick += _kickVel * dt;
            if (Mathf.Abs(_kick) < 0.001f && Mathf.Abs(_kickVel) < 0.01f)
            {
                _kick = 0f;
                _kickVel = 0f;
            }
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            if (_trauma > 0f)
            {
                _traumaT += dt;
                float p = Mathf.Clamp01(_traumaT / _traumaDur);
                float s = _trauma * (1f - p);
                s *= s;
                float f = Time.time * 32f;
                pos = new Vector3(Mathf.PerlinNoise(_seed, f) - 0.5f, Mathf.PerlinNoise(_seed + 7f, f) - 0.5f, 0f) * (0.05f * s);
                rot = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(_seed + 13f, f) - 0.5f) * 2.4f * s);
                if (p >= 1f) _trauma = 0f;
            }
            bool reduced = !FeelSettings.CameraMotion;
            if (reduced)
            {
                _kick = 0f;
                _kickVel = 0f;
                _trauma = 0f;
                return;
            }
            float fov = Mathf.Clamp(_kick, -8f, 8f);
            if (pos == Vector3.zero && fov == 0f && rot == Quaternion.identity) return;
            var tr = transform;
            tr.localPosition += pos;
            tr.localRotation *= rot;
            if (_cam != null) _cam.fieldOfView += fov;
            _applied = true;
            _appliedPos = pos;
            _appliedRot = rot;
            _appliedFov = fov;
            _setPos = tr.localPosition;
            _setRot = tr.localRotation;
            _setFov = _cam != null ? _cam.fieldOfView : 0f;
        }
    }
}
