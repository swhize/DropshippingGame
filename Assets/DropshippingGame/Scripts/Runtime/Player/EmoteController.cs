using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DropshippingGame
{
    /// <summary>
    /// Emotes des Spielers: Emote-Rad (T bzw. Steuerkreuz links halten, mit Maus/rechtem Stick oder
    /// 1–6 wählen, loslassen = ausführen; kurz tippen = letztes Emote). Ego-Kamera wippt, Arme
    /// bewegen sich, Passanten in der Nähe reagieren mit Sprechblasen.
    /// Läuft vor dem PlayerController (Kamera-Versatz wird je Frame zurückgenommen und neu gesetzt).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class EmoteController : MonoBehaviour
    {
        public enum Emote { Wave, Dance, Facepalm, Flex, MoneyRain, Sit }

        private static readonly string[] Names = { "Winken", "Tanzen", "Facepalm", "Flexen", "Geldregen", "Hinsetzen" };
        private static readonly string[] IconNames = { "chat", "music", "angry", "bolt", "coin", "pause" };
        private static readonly float[] Durations = { 2.2f, 5f, 2.4f, 2.6f, 3.2f, 30f };

        private static readonly string[][] Reactions =
        {
            new[] { "Hallo!", "Hi! Kennen wir uns?", "*winkt zurück*", "Moin!" },
            new[] { "Yeah!", "Was für Moves!", "Tanzt der etwa?!", "Mach mit!" },
            new[] { "Läuft bei dir ...", "Kenn ich.", "Montag, oder?", "Kopf hoch!" },
            new[] { "Wow, Muckis!", "Beeindruckend ... nicht.", "Pakete schleppen hilft!", "Oha!" },
            new[] { "Ist das echt?!", "Hustle-Bro!", "Spielgeld, oder?", "Kann ich was haben?" },
            new[] { "Pause verdient.", "Gemütlich?", "Platz da noch frei?", "Chillig." },
        };

        /// <summary>true, solange das Rad offen ist (der PlayerController dreht dann die Kamera nicht).</summary>
        public static bool WheelOpen;

        private PlayerController _player;
        private Emote _last = Emote.Wave;
        private bool _wheel;
        private float _heldT;
        private Vector2 _sel;
        private int _hover = -1;
        private VisualElement _wheelUi;
        private readonly List<VisualElement> _slots = new List<VisualElement>();

        private bool _playing;
        private Emote _cur;
        private float _t;
        private Transform _arms;
        private Vector3 _lastPos;
        private Quaternion _lastRot = Quaternion.identity;
        private Vector3 _startPos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => WheelOpen = false;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        private void OnDestroy()
        {
            WheelOpen = false;
            if (_wheelUi != null) _wheelUi.RemoveFromHierarchy();
        }

        private bool Locked => Game.Root != null && Game.Root.InputLocked || (_player != null && _player.Mover.Active);

        private void Update()
        {
            var cam = _player != null ? _player.Cam : null;
            if (cam == null) return;
            // Versatz des letzten Frames zurücknehmen
            cam.transform.localRotation = cam.transform.localRotation * Quaternion.Inverse(_lastRot);
            cam.transform.localPosition -= _lastPos;
            _lastPos = Vector3.zero;
            _lastRot = Quaternion.identity;
            HandleWheel();
            if (_playing && _cur == Emote.Sit && (GameInput.Move.sqrMagnitude > 0.1f || GameInput.JumpDown)) Stop();
            if (_playing && (transform.position - _startPos).sqrMagnitude > 9f && _cur != Emote.Sit) Stop();
        }

        private void HandleWheel()
        {
            bool held = !Locked && GameInput.EmoteHeld;
            if (held && !_wheel)
            {
                _wheel = true;
                _heldT = 0f;
                _sel = Vector2.zero;
                _hover = -1;
                ShowWheel(true);
            }
            if (_wheel && held)
            {
                _heldT += Time.unscaledDeltaTime;
                Vector2 look = GameInput.Look;
                _sel += new Vector2(look.x, look.y) * 0.08f;
#if ENABLE_INPUT_SYSTEM
                var pad = Gamepad.current;
                if (pad != null)
                {
                    Vector2 rs = pad.rightStick.ReadValue();
                    if (rs.sqrMagnitude > 0.25f) _sel = rs * 2f;
                }
                var kb = Keyboard.current;
                if (kb != null)
                {
                    var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key };
                    for (int i = 0; i < keys.Length; i++)
                        if (keys[i].wasPressedThisFrame)
                        {
                            CloseWheel();
                            Play((Emote)i);
                            return;
                        }
                }
#endif
                if (_sel.magnitude > 2.5f) _sel = _sel.normalized * 2.5f;
                if (_sel.magnitude > 0.7f)
                {
                    // Slot 0 oben, im Uhrzeigersinn
                    float ang = Mathf.Atan2(_sel.x, _sel.y) * Mathf.Rad2Deg;
                    _hover = Mathf.RoundToInt(Mathf.Repeat(ang, 360f) / 60f) % 6;
                }
                UpdateHover();
            }
            if (_wheel && !held)
            {
                int pick = _hover;
                bool tap = _heldT < 0.25f && pick < 0;
                CloseWheel();
                if (Locked) return;
                if (pick >= 0) Play((Emote)pick);
                else if (tap) Play(_last);
            }
        }

        private void CloseWheel()
        {
            _wheel = false;
            ShowWheel(false);
        }

        private void LateUpdate()
        {
            var cam = _player != null ? _player.Cam : null;
            if (cam == null || !_playing) return;
            _t += Time.deltaTime;
            float dur = Durations[(int)_cur];
            if (_t >= dur)
            {
                Stop();
                return;
            }
            float k = Mathf.Clamp01(_t * 4f) * Mathf.Clamp01((dur - _t) * 4f);
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            switch (_cur)
            {
                case Emote.Wave:
                    rot = Quaternion.Euler(0, 0, Mathf.Sin(_t * 9f) * 2f * k);
                    AnimArm(0, new Vector3(0.35f, -0.1f + 0.25f * k, 0.55f), Mathf.Sin(_t * 12f) * 35f * k);
                    break;
                case Emote.Dance:
                    pos = new Vector3(Mathf.Sin(_t * 6f) * 0.12f, Mathf.Abs(Mathf.Sin(_t * 6f)) * 0.1f, 0) * k;
                    rot = Quaternion.Euler(0, 0, Mathf.Sin(_t * 6f) * 6f * k);
                    AnimArm(0, new Vector3(0.35f, -0.2f + Mathf.Abs(Mathf.Sin(_t * 6f)) * 0.4f, 0.5f), 20f);
                    AnimArm(1, new Vector3(-0.35f, -0.2f + Mathf.Abs(Mathf.Cos(_t * 6f)) * 0.4f, 0.5f), -20f);
                    break;
                case Emote.Facepalm:
                    rot = Quaternion.Euler(20f * k, 0, 0);
                    AnimArm(0, new Vector3(0.05f, -0.05f, 0.18f + 0.3f * (1f - k)), 0f);
                    break;
                case Emote.Flex:
                    rot = Quaternion.Euler(-8f * k, 0, 0);
                    float pump = Mathf.Abs(Mathf.Sin(_t * 5f));
                    AnimArm(0, new Vector3(0.45f, -0.15f + 0.1f * pump, 0.4f), 70f);
                    AnimArm(1, new Vector3(-0.45f, -0.15f + 0.1f * pump, 0.4f), -70f);
                    break;
                case Emote.MoneyRain:
                    rot = Quaternion.Euler(-15f * k, 0, 0);
                    AnimArm(0, new Vector3(0.3f, 0.1f * k, 0.5f), Mathf.Sin(_t * 14f) * 25f);
                    break;
                case Emote.Sit:
                    pos = new Vector3(0, -0.75f * Mathf.Clamp01(_t * 3f), 0);
                    break;
            }
            cam.transform.localPosition += pos;
            cam.transform.localRotation = cam.transform.localRotation * rot;
            _lastPos = pos;
            _lastRot = rot;
        }

        public void Play(Emote e)
        {
            if (_player == null || _player.Cam == null) return;
            Stop();
            _cur = e;
            _last = e;
            _t = 0f;
            _playing = true;
            _startPos = transform.position;
            BuildArms();
            if (e == Emote.MoneyRain) MoneyRain();
            if (e == Emote.Dance) Game.Sound("jingle_good", 0.05f, -8f);
            else Game.Sound("whoosh", 0.1f, -10f);
            ReactNpcs(e);
        }

        public void Stop()
        {
            _playing = false;
            if (_arms != null) Destroy(_arms.gameObject);
            _arms = null;
        }

        // ---- Arme ---------------------------------------------------------------------------------
        private readonly Transform[] _armT = new Transform[2];

        private void BuildArms()
        {
            if (_arms != null) Destroy(_arms.gameObject);
            _arms = new GameObject("EmoteArms").transform;
            _arms.SetParent(_player.Cam.transform, false);
            var sleeve = Mats.Std(new Color(0.2f, 0.22f, 0.28f), 0.8f);
            var skin = Mats.Std(new Color(0.93f, 0.76f, 0.62f), 0.7f);
            for (int i = 0; i < 2; i++)
            {
                var a = new GameObject("Arm" + i).transform;
                a.SetParent(_arms, false);
                a.localPosition = new Vector3(i == 0 ? 0.4f : -0.4f, -0.8f, 0.3f);
                Props.Box(a, new Vector3(0.1f, 0.5f, 0.1f), sleeve, new Vector3(0, -0.2f, 0), default, 0.02f, false);
                Props.Box(a, new Vector3(0.11f, 0.13f, 0.06f), skin, new Vector3(0, 0.1f, 0), default, 0.02f, false);
                _armT[i] = a;
            }
            foreach (var r in _arms.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void AnimArm(int i, Vector3 handPos, float roll)
        {
            var a = _armT[i];
            if (a == null) return;
            a.localPosition = Vector3.Lerp(a.localPosition, handPos, Mathf.Min(1f, Time.deltaTime * 14f));
            a.localRotation = Quaternion.Euler(-15f, 0, roll);
        }

        // ---- Geldregen (rein optisch, kostet nichts) -----------------------------------------------
        private void MoneyRain()
        {
            var parent = Game.World != null ? Game.World.transform : null;
            var green = Mats.Std(new Color(0.45f, 0.75f, 0.4f), 0.6f);
            for (int i = 0; i < 24; i++)
            {
                var bill = Props.Box(parent, new Vector3(0.16f, 0.005f, 0.08f), green,
                    transform.position + new Vector3(Random.Range(-1.6f, 1.6f), Random.Range(2.6f, 4.2f), Random.Range(-1.6f, 1.6f)) + transform.forward * 1.2f, default, 0f, false).transform;
                Vector3 p0 = bill.position;
                float spin = Random.Range(180f, 720f), drift = Random.Range(-0.5f, 0.5f);
                Anim.Run(Random.Range(1.8f, 2.8f), t =>
                {
                    if (bill == null) return;
                    bill.position = p0 + new Vector3(Mathf.Sin(t * 8f + drift) * 0.3f, -t * (p0.y - transform.position.y), drift * t);
                    bill.rotation = Quaternion.Euler(Mathf.Sin(t * 10f) * 50f, t * spin, Mathf.Cos(t * 9f) * 40f);
                }, () => { if (bill != null) Destroy(bill.gameObject); });
            }
            Game.Sound("coin", 0.1f, -4f);
        }

        // ---- Reaktionen ---------------------------------------------------------------------------
        private void ReactNpcs(Emote e)
        {
            int n = 0;
            NPC[] all;
            try { all = FindObjectsByType<NPC>(FindObjectsSortMode.None); }
            catch { return; }
            foreach (var npc in all)
            {
                if (npc == null || n >= 4) continue;
                Vector3 d = npc.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 10f * 10f || Random.value > 0.75f) continue;
                n++;
                float delay = Random.Range(0.3f, 1.2f);
                var target = npc;
                var lines = Reactions[(int)e];
                Anim.Delay(delay, () =>
                {
                    if (target == null) return;
                    target.Stop(2.5f, transform);
                    target.Say(lines[Random.Range(0, lines.Length)], 2.8f);
                    if (e == Emote.Dance)
                    {
                        var g = target.GetComponent<GoofyWalk>();
                        if (g != null) g.DanceFor(4f);
                    }
                });
            }
        }

        // ---- Rad (UI Toolkit, im Code gebaut) -----------------------------------------------------
        private void ShowWheel(bool on)
        {
            WheelOpen = on;
            if (on && _wheelUi == null) BuildWheel();
            if (_wheelUi != null) _wheelUi.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void BuildWheel()
        {
            var root = Game.UI != null ? Game.UI.Root : null;
            if (root == null) return;
            _wheelUi = new VisualElement { name = "emote-wheel", pickingMode = PickingMode.Ignore };
            var st = _wheelUi.style;
            st.position = Position.Absolute;
            st.width = 360;
            st.height = 360;
            st.left = new Length(50, LengthUnit.Percent);
            st.top = new Length(50, LengthUnit.Percent);
            st.marginLeft = -180;
            st.marginTop = -180;
            st.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.55f);
            st.borderTopLeftRadius = st.borderTopRightRadius = st.borderBottomLeftRadius = st.borderBottomRightRadius = 180;
            var center = new Label("EMOTES\n" + GameInput.KeyLabel("emote") + " loslassen");
            center.style.position = Position.Absolute;
            center.style.left = 120; center.style.top = 155; center.style.width = 120;
            center.style.unityTextAlign = TextAnchor.MiddleCenter;
            center.style.color = new Color(1f, 0.9f, 0.6f);
            center.style.fontSize = 15;
            _wheelUi.Add(center);
            _slots.Clear();
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var slot = new VisualElement { pickingMode = PickingMode.Ignore };
                var ss = slot.style;
                ss.position = Position.Absolute;
                ss.width = 96; ss.height = 84;
                ss.left = 180 + Mathf.Sin(a) * 125 - 48;
                ss.top = 180 - Mathf.Cos(a) * 125 - 42;
                ss.alignItems = Align.Center;
                ss.justifyContent = Justify.Center;
                ss.borderTopLeftRadius = ss.borderTopRightRadius = ss.borderBottomLeftRadius = ss.borderBottomRightRadius = 14;
                try { UI.UIX.Icon(slot, IconNames[i], 30f, Color.white); } catch { }
                var l = new Label((i + 1) + " " + Names[i]);
                l.style.color = Color.white;
                l.style.fontSize = 15;
                slot.Add(l);
                _wheelUi.Add(slot);
                _slots.Add(slot);
            }
            root.Add(_wheelUi);
        }

        private void UpdateHover()
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].style.backgroundColor = i == _hover ? new Color(1f, 0.55f, 0.2f, 0.85f) : new Color(1f, 1f, 1f, 0.08f);
        }
    }
}
