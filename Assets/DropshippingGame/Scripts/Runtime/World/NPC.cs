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
        // Freies Ziel (Passanten-KI) statt Wegpunktliste
        private bool _hasFacePoint;
        private Vector3 _facePoint;
        private bool _hasDest;
        private Vector3 _dest;
        private System.Action<NPC> _onArrive;
        // Hindernisumgehung
        private Vector3 _steer;
        private bool _steerValid;
        private float _steerTimer;
        private float _stuckTimer;
        private Vector3 _lastPos;

        /// <summary>Hat gerade ein frei gesetztes Ziel (Passanten-Modus).</summary>
        public bool HasDestination => _hasDest;
        public bool IsWaiting => _wait > 0f;

        /// <summary>Läuft zu einem Punkt (lokal zum Parent), umgeht Hindernisse und ruft danach <paramref name="onArrive"/>.</summary>
        public void GoTo(Vector3 localPos, System.Action<NPC> onArrive = null)
        {
            _dest = localPos;
            _hasDest = true;
            _onArrive = onArrive;
            _steerValid = false;
        }

        public void ClearDestination()
        {
            _hasDest = false;
            _onArrive = null;
            _steerValid = false;
        }

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
            _hasFacePoint = false;
        }

        /// <summary>Kurz stehen bleiben und in Richtung eines Weltpunkts schauen (Schaufenster, Plakat ...).</summary>
        public void StopFacing(float seconds, Vector3 worldPoint)
        {
            _wait = Mathf.Max(_wait, seconds);
            _faceTarget = null;
            _facePoint = worldPoint;
            _hasFacePoint = true;
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
            bool navigating = _hasDest || Waypoints.Count > 1;
            if (navigating)
            {
                if (_wait > 0f)
                {
                    _wait -= dt;
                    if (_faceTarget != null) FaceTowards(_faceTarget.position, 5f, dt);
                    else if (_hasFacePoint) FaceTowards(_facePoint, 3f, dt);
                    if (_wait <= 0f) { _faceTarget = null; _hasFacePoint = false; }
                }
                else
                {
                    Vector3 target = _hasDest ? _dest : Waypoints[Mathf.Clamp(_idx, 0, Waypoints.Count - 1)];
                    Vector3 to = target - transform.localPosition;
                    to.y = 0f;
                    float dist = to.magnitude;
                    if (dist < 0.08f)
                    {
                        _steerValid = false;
                        if (_hasDest)
                        {
                            _hasDest = false;
                            var cb = _onArrive;
                            _onArrive = null;
                            if (cb != null) cb(this);
                        }
                        else
                        {
                            NextPoint();
                            _wait = PauseAtPoints;
                        }
                    }
                    else
                    {
                        Vector3 aim = SteerPoint(target, dt);
                        Vector3 toAim = aim - transform.localPosition;
                        toAim.y = 0f;
                        float aimDist = toAim.magnitude;
                        if (aimDist < 0.1f)
                        {
                            // Umgehungsecke erreicht: sofort neu planen
                            _steerValid = false;
                            aim = SteerPoint(target, dt);
                            toAim = aim - transform.localPosition;
                            toAim.y = 0f;
                            aimDist = toAim.magnitude;
                        }
                        if (aimDist > 0.0001f)
                        {
                            float step = Mathf.Min(aimDist, Speed * dt);
                            transform.localPosition += toAim / aimDist * step;
                            float yaw = Mathf.Atan2(toAim.x, toAim.z) * Mathf.Rad2Deg;
                            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0, yaw, 0), Mathf.Min(1f, dt * 8f));
                            moving = true;
                        }
                        StuckCheck(dt);
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

        /// <summary>Zwischenziel mit Hindernisumgehung (lokal zum Parent), alle ~0,3 s neu berechnet.</summary>
        private Vector3 SteerPoint(Vector3 targetLocal, float dt)
        {
            _steerTimer -= dt;
            if (_steerValid && _steerTimer > 0f) return _steer;
            _steerTimer = 0.25f + Random.value * 0.15f;
            Vector3 result = targetLocal;
            try
            {
                var parent = transform.parent;
                Vector3 fromW = parent != null ? parent.TransformPoint(transform.localPosition) : transform.localPosition;
                Vector3 toW = parent != null ? parent.TransformPoint(targetLocal) : targetLocal;
                Vector3 s = NpcNav.Steer(fromW, toW);
                result = parent != null ? parent.InverseTransformPoint(s) : s;
                result.y = targetLocal.y;
            }
            catch (System.Exception)
            {
                result = targetLocal;
            }
            _steer = result;
            _steerValid = true;
            return _steer;
        }

        /// <summary>Hängt der NPC fest (z.B. zwischen zwei Hindernissen), notfalls direkt aufs Ziel.</summary>
        private void StuckCheck(float dt)
        {
            if ((transform.localPosition - _lastPos).sqrMagnitude > 0.04f)
            {
                _lastPos = transform.localPosition;
                _stuckTimer = 0f;
                return;
            }
            _stuckTimer += dt;
            if (_stuckTimer > 4f)
            {
                _stuckTimer = 0f;
                _steerValid = false;
                if (_hasDest) transform.localPosition = Vector3.MoveTowards(transform.localPosition, _dest, 0.5f);
            }
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
