using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Feedback an den Händen (Ego-Ansicht): Aufnehmen lässt das Gehaltene von unten hereinfedern,
    /// ein Etikett „klatscht“ mit Squash auf das Paket, getragene Stapel schwanken leicht beim Drehen/Laufen.
    /// Angesteuert über <see cref="OnHeldChanged"/> (ein Haken in PlayerController.HeldChanged).
    /// </summary>
    public static class HandFeel
    {
        private static int _lastCount;
        private static ItemKind _lastKind = ItemKind.None;
        private static Transform _held;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _lastCount = 0;
            _lastKind = ItemKind.None;
            _held = null;
        }

        /// <summary>Aktuelles Hand-Modell (oder null).</summary>
        public static Transform Held => _held != null ? _held : null;

        /// <summary>Weltposition des obersten gehaltenen Gegenstands (für Effekte „auf das Paket“).</summary>
        public static Vector3 HeldTop()
        {
            if (_held != null)
            {
                // Oberkante aller getragenen Gegenstände (ohne Arme)
                bool any = false;
                var b = new Bounds(_held.position, Vector3.zero);
                foreach (var r in _held.GetComponentsInChildren<Renderer>())
                {
                    if (r == null || r.transform.parent == null || r.transform.parent.name == "Arm") continue;
                    if (!any) b = r.bounds;
                    else b.Encapsulate(r.bounds);
                    any = true;
                }
                return any ? new Vector3(b.center.x, b.max.y, b.center.z) : _held.position;
            }
            return Feel.InFrontOfCamera(0.7f, 0.3f);
        }

        public static void OnHeldChanged(PlayerController p, GameObject visual)
        {
            if (!Feel.Enabled || p == null) return;
            try
            {
                int count = p.CarryCount;
                var kind = p.Held?.Kind ?? ItemKind.None;
                _held = visual != null ? visual.transform : null;
                if (_held != null)
                {
                    bool gained = count > _lastCount || (count == _lastCount && count > 0 && kind != _lastKind && kind != ItemKind.Labeled);
                    if (gained)
                    {
                        // Von unten hereinfedern (neuer Gegenstand / Stapel wächst)
                        float m = FeelSettings.MotionScale;
                        _held.localPosition = new Vector3(0f, -0.2f * m, -0.06f * m);
                        _held.localRotation = Quaternion.Euler(-14f * m, 0f, 4f * m);
                        FeelTween.Move(_held, Vector3.zero, 0.3f, FeelEase.OutBack);
                        FeelTween.Rotate(_held, Quaternion.identity, 0.34f, FeelEase.OutBack);
                    }
                    if (count >= 1 && PlayerController.IsStackable(p.Held))
                    {
                        var sway = _held.GetComponent<StackSway>();
                        if (sway == null) sway = _held.gameObject.AddComponent<StackSway>();
                        sway.Setup(p);
                    }
                }
                _lastCount = count;
                _lastKind = kind;
            }
            catch (System.Exception e)
            {
                Feel.LogOnce(e);
            }
        }

        /// <summary>Etikett klebt: kurzer Squash auf dem gehaltenen Stapel.</summary>
        public static void StickSquash(float amount = 0.22f)
        {
            if (_held == null) return;
            FeelTween.Squash(_held, amount, 0.32f);
        }
    }

    /// <summary>
    /// Leichtes Schwanken eines getragenen Paketstapels: obere Pakete neigen sich stärker gegen Drehung und
    /// Beschleunigung (Feder-Dämpfer, keine Allokationen pro Frame).
    /// </summary>
    public sealed class StackSway : MonoBehaviour
    {
        private readonly List<Transform> _items = new List<Transform>();
        private readonly List<Quaternion> _baseRot = new List<Quaternion>();
        private readonly List<Vector3> _basePos = new List<Vector3>();
        private PlayerController _player;
        private Vector2 _tilt, _vel;
        private float _lastYaw;
        private Vector3 _lastPos, _lastVel;
        private bool _primed;

        public void Setup(PlayerController p)
        {
            _player = p;
            _items.Clear();
            _baseRot.Clear();
            _basePos.Clear();
            foreach (Transform c in transform)
            {
                if (c.name == "Arm") continue;
                _items.Add(c);
                _baseRot.Add(c.localRotation);
                _basePos.Add(c.localPosition);
            }
            _primed = false;
        }

        private void LateUpdate()
        {
            if (_player == null || _items.Count == 0) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float yaw = _player.transform.eulerAngles.y;
            var pos = _player.transform.position;
            if (!_primed)
            {
                _primed = true;
                _lastYaw = yaw;
                _lastPos = pos;
                _lastVel = Vector3.zero;
                return;
            }
            float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
            var vel = (pos - _lastPos) / dt;
            var acc = (vel - _lastVel) / dt;
            _lastYaw = yaw;
            _lastPos = pos;
            _lastVel = vel;
            // Beschleunigung in Spieler-Koordinaten: vorwärts/seitlich
            var local = _player.transform.InverseTransformDirection(acc);
            var force = new Vector2(-yawRate * 0.012f - local.x * 0.25f, -local.z * 0.25f);
            force = Vector2.ClampMagnitude(force, 40f);
            const float k = 90f, d = 11f;
            _vel += (force - _tilt * k - _vel * d) * dt;
            _tilt += _vel * dt;
            _tilt = Vector2.ClampMagnitude(_tilt, 9f);
            float m = FeelSettings.MotionScale;
            int n = _items.Count;
            for (int i = 0; i < n; i++)
            {
                var tr = _items[i];
                if (tr == null) continue;
                float f = (i + 1f) / n * (n > 1 ? 1f : 0.4f) * m;
                tr.localRotation = _baseRot[i] * Quaternion.Euler(_tilt.y * f, 0f, _tilt.x * f);
                tr.localPosition = _basePos[i] + new Vector3(-_tilt.x * f * 0.0025f * (i + 1), 0f, 0f);
            }
        }
    }
}
