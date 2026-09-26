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
        /// <summary>Animiertes Modell (Kenney Mini Characters) - null, wenn die prozedurale Figur benutzt wird.</summary>
        public GameObject Model;
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
        private bool _modelWalking;
        private bool _sitting;
        private GameObject _bag;

        public void Setup(Look look, IList<Vector3> points = null, float speed = 1.4f)
        {
            _sitting = look != null && look.Sitting;
            Model = CharacterKit.BuildModel(transform, look);
            if (Model == null) Rig = CharacterKit.Build(transform, look);
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
                if (_bubbleTime <= 0f && _bubble != null) _bubble.gameObject.SetActive(false);
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
            if (Model != null) AnimateModel(moving);
            else CharacterKit.Animate(Rig, _phase, _walk, _t);
        }

        private void AnimateModel(bool moving)
        {
            if (_sitting) return;
            if (moving == _modelWalking) return;
            _modelWalking = moving;
            if (moving) AssetLib.PlayAnim(Model, "walk", 0.2f, Mathf.Clamp(Speed / 1.3f, 0.6f, 1.6f));
            else AssetLib.PlayAnim(Model, "idle", 0.25f);
        }

        /// <summary>Einkaufstüte in Markenfarbe in der rechten Hand (Marke sichtbar in der Welt).</summary>
        public void SetBag(bool on, Color brand)
        {
            if (!on)
            {
                if (_bag != null) Destroy(_bag);
                _bag = null;
                return;
            }
            if (_bag == null)
            {
                _bag = Props.Node(transform, "Bag", new Vector3(0.3f, 0.52f, 0.02f));
                Props.Box(_bag.transform, new Vector3(0.08f, 0.3f, 0.26f), Mats.Std(brand, 0.7f), Vector3.zero, default, 0.01f);
                Props.Box(_bag.transform, new Vector3(0.012f, 0.1f, 0.012f), Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.6f), new Vector3(0, 0.2f, -0.05f), default, 0f, false);
                Props.Box(_bag.transform, new Vector3(0.012f, 0.1f, 0.012f), Mats.Std(new Color(0.95f, 0.95f, 0.95f), 0.6f), new Vector3(0, 0.2f, 0.05f), default, 0f, false);
                Props.Box(_bag.transform, new Vector3(0.082f, 0.08f, 0.1f), Mats.Std(Color.white, 0.6f), new Vector3(0, 0.03f, 0), default, 0f, false);
            }
            else
            {
                var r = _bag.GetComponentInChildren<Renderer>();
                if (r != null) r.sharedMaterial = Mats.Std(brand, 0.7f);
            }
        }

        public bool HasBag => _bag != null;

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
