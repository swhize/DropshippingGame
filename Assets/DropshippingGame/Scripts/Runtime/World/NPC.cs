using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Nicht-Spieler-Figur: läuft Wegpunkte ab (oder steht), animiert Beine/Arme,
    /// zeigt Sprechblasen und kann kurz stehen bleiben (z.B. am Verkaufsstand).
    /// Keine Physik - reine Atmosphäre.
    /// </summary>
    public sealed class NPC : MonoBehaviour
    {
        public CharacterRig Rig;
        public readonly List<Vector3> Waypoints = new List<Vector3>();
        public float Speed = 1.4f;
        public float PauseAtPoints;
        public bool PingPong;
        public Transform LookAtTarget;
        public float LastStandVisit = -100f;

        private int _idx;
        private int _dir = 1;
        private float _wait;
        private float _phase;
        private float _t;
        private float _walk;
        private Label3D _bubble;
        private float _bubbleTime;
        private Transform _faceTarget;

        public void Setup(Look look, IList<Vector3> points = null, float speed = 1.4f)
        {
            Rig = CharacterKit.Build(transform, look);
            if (points != null) Waypoints.AddRange(points);
            Speed = speed;
            _t = Random.value * 10f;
            _bubble = Label3D.Create(transform, "", 40f, Color.white, new Vector3(0, 2.25f, 0), true, 14f, false, 1f, 26);
            _bubble.gameObject.SetActive(false);
            if (Waypoints.Count > 0) transform.localPosition = Waypoints[0];
        }

        public void Say(string text, float duration = 4f)
        {
            if (_bubble == null) return;
            _bubble.SetText(text);
            _bubble.gameObject.SetActive(true);
            _bubbleTime = duration;
        }

        /// <summary>Kurz stehen bleiben und optional etwas anschauen.</summary>
        public void Stop(float seconds, Transform faceTarget = null)
        {
            _wait = Mathf.Max(_wait, seconds);
            _faceTarget = faceTarget;
        }

        public bool IsWalking => _walk > 0.5f;

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            if (_bubbleTime > 0f)
            {
                _bubbleTime -= dt;
                if (_bubbleTime <= 0f) _bubble.gameObject.SetActive(false);
            }
            bool moving = false;
            if (Waypoints.Count > 1)
            {
                if (_wait > 0f)
                {
                    _wait -= dt;
                    if (_faceTarget != null) FaceTowards(_faceTarget.position, 5f, dt);
                    if (_wait <= 0f) _faceTarget = null;
                }
                else
                {
                    Vector3 target = Waypoints[_idx];
                    Vector3 to = target - transform.localPosition;
                    to.y = 0f;
                    float dist = to.magnitude;
                    if (dist < 0.08f)
                    {
                        NextPoint();
                        _wait = PauseAtPoints;
                    }
                    else
                    {
                        float step = Mathf.Min(dist, Speed * dt);
                        transform.localPosition += to / dist * step;
                        float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0, yaw, 0), Mathf.Min(1f, dt * 8f));
                        moving = true;
                    }
                }
            }
            else if (LookAtTarget != null)
            {
                Vector3 d = LookAtTarget.position - transform.position;
                if (d.sqrMagnitude < 36f) FaceTowards(LookAtTarget.position, 3f, dt);
            }
            _walk = Mathf.MoveTowards(_walk, moving ? 1f : 0f, dt * 4f);
            _phase += dt * Speed * 4.2f * _walk;
            CharacterKit.Animate(Rig, _phase, _walk, _t);
        }

        private void FaceTowards(Vector3 worldPos, float speed, float dt)
        {
            Vector3 d = worldPos - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Mathf.Min(1f, dt * speed));
        }

        private void NextPoint()
        {
            if (PingPong)
            {
                if (_idx + _dir >= Waypoints.Count || _idx + _dir < 0) _dir = -_dir;
                _idx += _dir;
            }
            else _idx = (_idx + 1) % Waypoints.Count;
        }
    }
}
