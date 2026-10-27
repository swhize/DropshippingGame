using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DropshippingGame.Vehicles
{
    /// <summary>
    /// Das eigene Moped: steht ab der Business-Phase vor der Garage, Aufsteigen/Absteigen mit der
    /// Benutzen-Taste, Arcade-Fahrphysik (kinematisch mit Box-Sweeps und Boden-Strahlen, Schräglage in Kurven,
    /// max. 35 km/h, Hüpfer an Bordsteinen, keine Innenräume), Ego-Sicht mit Lenker und Tacho oder
    /// Verfolgerkamera (V), Hupe (H), Motorklang nach Tempo, Licht bei Dunkelheit.
    /// Heckbox: bis zu 4 Pakete oder 1 Kiste (Regeln und Inhalt in Core: <see cref="VehicleRules"/>,
    /// <see cref="Sim.Moped"/>); an Packstation/Filiale werden etikettierte Pakete direkt aus der Box abgegeben.
    /// </summary>
    public sealed class Moped : MonoBehaviour, IInteractable
    {
        public static Moped Instance { get; private set; }

        private const int PlayerLayer = 2;
        private const int Mask = ~(1 << PlayerLayer);
        private const float Skin = 0.03f;
        /// <summary>Kollisionskörper: 0,31–1,19 m hoch (darunter klettern die Räder), vom Vorderrad bis hinter die Heckbox.</summary>
        private static readonly Vector3 BodyCenter = new Vector3(0f, 0.75f, -0.08f);
        private static readonly Vector3 BodySize = new Vector3(0.64f, 0.88f, 2.0f);
        private static readonly RaycastHit[] Hits = new RaycastHit[32];
        private static readonly Collider[] Overlaps = new Collider[32];
        private static readonly string[] PedLines = { "Ey!", "Ey, pass doch auf!", "Hey, das ist ein Gehweg!", "Langsam, du Raser!", "Boah, ey!", "Hallo?! Füße!" };
        private static readonly string[] HornLines = { "Jaja!", "Is ja gut!", "Hupen kann jeder!", "Hm?" };
        private static bool _tipShown;

        public MopedParts Parts;
        public PlayerController Rider => _rider;
        public bool IsRidden => _rider != null;
        public float Speed => _speed;

        private PlayerController _rider;
        private BoxCollider _col;
        private readonly Highlighter _hl = new Highlighter();
        private MopedAudio _audio;
        private readonly MopedHud _hud = new MopedHud();
        private float _yaw, _speed, _steer, _lean, _pitch, _vy, _wheelAngle, _throttle, _brake;
        private float _bumpY, _bumpV, _shake, _impactCd, _pedAcc, _saveAcc, _brandAcc, _hornCd;
        private float _lookYaw, _lookPitch, _lookIdle;
        private bool _wantExit, _thirdPerson, _tpInit;
        private int _mountFrame = -10, _exitAskFrame = -10;
        private Vector3 _tpCam;
        private int _cargoSig = int.MinValue;
        private string _brandSig = "";
        private readonly Dictionary<NPC, float> _pedCd = new Dictionary<NPC, float>();
        private readonly List<Dodge> _dodges = new List<Dodge>();

        private struct Dodge
        {
            public NPC Npc;
            public Vector3 Dir;
            public float Until;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            _tipShown = false;
        }

        private static float Vmax => VehicleRules.KmhToMs(Def.MaxKmh);
        private static VehicleDef Def => Game.Sim != null ? Game.Sim.MopedDef : VehicleRules.Def(null);

        // =====================================================================================
        // Aufbau
        // =====================================================================================
        public static Moped Spawn(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("Moped");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var m = go.AddComponent<Moped>();
            m.Init(yaw);
            return m;
        }

        private void Init(float yaw)
        {
            Instance = this;
            _yaw = yaw;
            _lean = -5f;
            _col = gameObject.AddComponent<BoxCollider>();
            _col.center = BodyCenter;
            _col.size = BodySize;
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            BuildModel();
            _audio = new MopedAudio(gameObject);
            SnapToGround();
        }

        private void BuildModel()
        {
            if (Parts != null && Parts.Body != null) Destroy(Parts.Body.gameObject);
            var sim = Game.Sim;
            Color brand = sim != null ? sim.BrandColor.ToColor() : new Color(0.95f, 0.45f, 0.15f);
            string name = sim != null ? sim.BrandName : "";
            _brandSig = name + "|" + ColorUtility.ToHtmlStringRGB(brand);
            Parts = MopedModel.Build(transform, brand, name);
            var box = Parts.Box.gameObject.AddComponent<MopedBox>();
            box.Setup(this);
            _hl.Collect(Parts.Shell);
            _cargoSig = int.MinValue;
            UpdateVisuals(0f);
        }

        private void OnDestroy()
        {
            if (_rider != null) Release(_rider);
            _hud.Destroy();
            if (Instance == this) Instance = null;
        }

        // =====================================================================================
        // IInteractable (Moped selbst: aufsteigen bzw. Pakete einladen)
        // =====================================================================================
        public string Title => Def.Name;

        public string Prompt(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null || _rider != null) return "";
            if (player.Held != null)
            {
                int n = CountLoadable(player);
                if (n > 0) return (n > 1 ? n + " Pakete" : What(player.Held)) + " in die Heckbox laden (" + sim.Moped.Box.Count + "/" + Def.ParcelSlots + ")";
                if (VehicleRules.Accepts(player.Held)) return "Heckbox voll – erst ausladen";
                return "Hände frei machen zum Aufsteigen";
            }
            string box = BoxText();
            return "Aufsteigen" + (sim.Moped.Box.Count > 0 ? "  ·  " + box : "");
        }

        public void Interact(PlayerController player)
        {
            if (player == null || _rider != null) return;
            if (player.Held != null)
            {
                if (CountLoadable(player) > 0) Load(player);
                else if (VehicleRules.Accepts(player.Held)) Game.Notify("Die Heckbox ist voll (4 Pakete oder 1 Kiste).", "info");
                else Game.Notify("Erst die Hände frei machen – " + GameInput.KeyLabel("drop") + " legt ab.", "info");
                return;
            }
            Mount(player);
        }

        public void SetHighlighted(bool on) => _hl.Set(on && _rider == null);

        private static string What(ItemData it)
        {
            if (it == null) return "";
            switch (it.Kind)
            {
                case ItemKind.Crate: return "Kiste";
                case ItemKind.Return: return "Retoure";
                case ItemKind.Labeled: return "Paket";
                default: return "Paket";
            }
        }

        /// <summary>Wie viele der getragenen Gegenstände (von oben) passen in die Box?</summary>
        public int CountLoadable(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null) return 0;
            var box = new List<ItemData>(sim.Moped.Box);
            var carried = player.Carried();
            int n = 0;
            for (int i = carried.Count - 1; i >= 0; i--)
            {
                if (!VehicleRules.CanLoad(box, carried[i], sim.MopedDef)) break;
                box.Add(carried[i]);
                n++;
            }
            return n;
        }

        /// <summary>Lädt getragene Pakete (von oben, solange sie passen) in die Heckbox.</summary>
        public void Load(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null) return;
            var carried = player.Carried();
            int loaded = 0;
            for (int i = carried.Count - 1; i >= 0; i--)
            {
                if (!sim.LoadIntoMoped(carried[i])) break;
                carried.RemoveAt(i);
                loaded++;
            }
            if (loaded == 0) return;
            player.SetCarried(carried);
            player.RefreshPrompt();
            _audio?.Play("moped_bump", 0.25f, 0.1f);
            Game.Sound("place", 0.08f, -4f);
            Game.Notify((loaded > 1 ? loaded + " Pakete" : What(sim.Moped.Box[sim.Moped.Box.Count - 1])) + " in der Heckbox (" + BoxText() + ").", "info");
        }

        /// <summary>Nimmt den obersten Gegenstand aus der Box in die Hände.</summary>
        public void TakeOne(PlayerController player)
        {
            var sim = Game.Sim;
            if (sim == null || player == null || sim.Moped.Box.Count == 0) return;
            var top = sim.Moped.Box[sim.Moped.Box.Count - 1];
            if (player.Held != null && !player.CanStack(top))
            {
                Game.Notify("Hände sind schon voll.", "info");
                return;
            }
            sim.TakeFromMoped();
            if (player.Held == null) player.Hold(top);
            else player.Push(top);
            player.RefreshPrompt();
            Game.Sound("pickup", 0.06f, -3f);
        }

        /// <summary>Kurzbeschreibung des Box-Inhalts, z. B. "Box 2/4 · 1 mit Label".</summary>
        public string BoxText()
        {
            var sim = Game.Sim;
            if (sim == null) return "";
            var box = sim.Moped.Box;
            if (box.Count == 0) return "Heckbox leer";
            if (VehicleRules.HasCrate(box)) return "Heckbox: 1 Kiste";
            int lab = sim.CountInMoped(ItemKind.Labeled);
            return "Heckbox " + box.Count + "/" + Def.ParcelSlots + (lab > 0 ? " · " + lab + " mit Label" : "");
        }

        // =====================================================================================
        // Auf- und Absteigen
        // =====================================================================================
        public void Mount(PlayerController p)
        {
            if (p == null || _rider != null || p.Riding != null) return;
            if (p.Held != null)
            {
                Game.Notify("Erst die Hände frei machen – Pakete in die Heckbox laden oder ablegen.", "info");
                return;
            }
            if (p.Mover.Active || p.Recorder.Active || PlayerController.Noclip) return;
            var emote = p.GetComponent<EmoteController>();
            if (emote != null) emote.Stop();
            _rider = p;
            p.Riding = this;
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            if (p.Focus != null && !(p.Focus is Object fo && fo == null)) p.Focus.SetHighlighted(false);
            p.Focus = null;
            _hl.Set(false);
            p.SetHandsVisible(false);
            p.PromptText = "";
            _lookYaw = 0f;
            _lookPitch = 9f;
            _lookIdle = 0f;
            _wantExit = false;
            _tpInit = false;
            _speed = 0f;
            _mountFrame = Time.frameCount; // dieselbe Taste soll nicht sofort wieder absteigen lassen
            _audio.SetRunning(true);
            _audio.Play("moped_bump", 0.2f, 0.1f);
            if (!_tipShown)
            {
                _tipShown = true;
                Game.Notify("Moped: " + (GameInput.UsingGamepad ? "RT/LT Gas/Bremse · Stick lenken" : "W/S Gas/Bremse · A/D lenken") + " · " +
                            GameInput.KeyLabel("horn") + " Hupe · " + GameInput.KeyLabel("vehcam") + " Ansicht · " + GameInput.KeyLabel("interact") + " absteigen", "info");
            }
            LateUpdate();
        }

        /// <summary>Absteigen: Spieler landet sicher neben dem Moped.</summary>
        public void Exit()
        {
            if (_rider == null) return;
            var p = _rider;
            var spot = ExitSpot();
            Release(p);
            p.Teleport(spot, _yaw);
            Game.Sound("step", 0.1f, -6f);
        }

        /// <summary>Vom Moped holen, ohne den Spieler zu versetzen (z. B. wenn er gerade teleportiert wird).</summary>
        public void ForceExit()
        {
            if (_rider != null) Release(_rider);
        }

        private void Release(PlayerController p)
        {
            _rider = null;
            _wantExit = false;
            if (p != null)
            {
                p.Riding = null;
                p.SetHandsVisible(true);
                p.PromptText = "";
                if (p.Cam != null)
                {
                    p.Cam.fieldOfView = Settings.Fov;
                    p.Cam.transform.localRotation = Quaternion.identity;
                }
                var cc = p.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = !PlayerController.Noclip;
            }
            _audio?.SetRunning(false);
            _hud.Tick(false, 0f, 1f, "");
            if (Parts != null)
            {
                if (Parts.Rider != null) Parts.Rider.gameObject.SetActive(false);
                if (Parts.ArmL != null) Parts.ArmL.gameObject.SetActive(false);
                if (Parts.ArmR != null) Parts.ArmR.gameObject.SetActive(false);
            }
            SaveState();
        }

        /// <summary>Freier Platz zum Absteigen: links (Ständerseite), rechts, hinten, vorne – sonst etwas weiter weg.</summary>
        public Vector3 ExitSpot()
        {
            var right = transform.right;
            var fwd = transform.forward;
            var basePos = transform.position;
            Vector3[] offs = { -right * 0.95f, right * 0.95f, -fwd * 1.55f, fwd * 1.55f, -right * 1.5f, right * 1.5f, -fwd * 2.3f, fwd * 2.3f };
            foreach (var o in offs)
            {
                var p = basePos + o;
                float g = Ground(p, basePos.y + 0.3f);
                if (float.IsNegativeInfinity(g) || g > basePos.y + 0.4f) continue;
                p.y = g;
                if (Free(p)) return p + Vector3.up * 0.05f;
            }
            return basePos - fwd * 2.3f + Vector3.up * 0.1f;
        }

        private bool Free(Vector3 p)
        {
            int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.33f, Overlaps, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = Overlaps[i];
                if (c == null || IgnoreCollider(c)) continue;
                if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) continue;
                return false;
            }
            return true;
        }

        // =====================================================================================
        // Fahren
        // =====================================================================================
        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            bool paused = Game.Root != null && Game.Root.Paused;
            if (_rider != null && !_rider) Release(null);
            UpdateCargo();
            _brandAcc -= Time.unscaledDeltaTime;
            if (_brandAcc <= 0f)
            {
                _brandAcc = 2f;
                CheckBrand();
            }
            if (dt <= 0f || paused)
            {
                _audio?.Tick(0f, 0f, 0f, true, false);
                return;
            }
            bool riding = _rider != null;
            bool locked = Game.Root != null && Game.Root.InputLocked;
            float steerIn = 0f;
            _throttle = 0f;
            _brake = 0f;
            if (riding)
            {
                if (_rider.Recorder.Active || _rider.Mover.Active || PlayerController.Noclip)
                {
                    Exit();
                    return;
                }
                if (!locked && !EmoteController.WheelOpen && !_wantExit)
                {
                    if (Time.frameCount > _mountFrame && GameInput.InteractDown) OnRideInteract();
                    if (GameInput.HornDown) Horn();
                    if (GameInput.VehicleCamDown) ToggleView();
                    ReadDriveInput(out _throttle, out _brake, out steerIn);
                }
                if (locked || _wantExit)
                {
                    _throttle = 0f;
                    _brake = 1f;
                }
                if (_wantExit && Mathf.Abs(_speed) < 0.7f && Time.frameCount > _exitAskFrame)
                {
                    Exit();
                    return;
                }
                if (_rider != null) _rider.PromptText = RidePrompt();
            }
            else if (Mathf.Abs(_speed) > 0.01f) _brake = 1f;

            Simulate(dt, steerIn, riding);
            UpdateVisuals(dt);
            float s01 = Mathf.Clamp01(Mathf.Abs(_speed) / Vmax);
            _audio?.Tick(dt, s01, _throttle, false, riding && !_thirdPerson);
            _hud.Tick(riding, VehicleRules.MsToKmh(_speed), Def.MaxKmh, BoxText());
            if (riding)
            {
                _pedAcc -= dt;
                if (_pedAcc <= 0f)
                {
                    _pedAcc = 0.12f;
                    WarnPedestrians();
                }
                _saveAcc -= dt;
                if (_saveAcc <= 0f)
                {
                    _saveAcc = 0.5f;
                    SaveState();
                }
            }
            TickDodges(dt);
            _impactCd -= dt;
            _hornCd -= dt;
        }

        private static void ReadDriveInput(out float throttle, out float brake, out float steer)
        {
            Vector2 mv = GameInput.Move;
            throttle = Mathf.Max(0f, mv.y);
            brake = Mathf.Max(0f, -mv.y);
            steer = mv.x;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
            {
                float rt = pad.rightTrigger.ReadValue(), lt = pad.leftTrigger.ReadValue();
                if (rt > 0.05f) throttle = Mathf.Max(throttle, rt);
                if (lt > 0.05f) brake = Mathf.Max(brake, lt);
            }
#endif
        }

        private void Simulate(float dt, float steerIn, bool riding)
        {
            var def = Def;
            float vmax = VehicleRules.KmhToMs(def.MaxKmh);
            bool parked = !riding && Mathf.Abs(_speed) < 0.001f && _vy == 0f;
            _speed = VehicleRules.StepSpeed(_speed, _throttle, _brake, dt, def);
            _steer = Mathf.MoveTowards(_steer, steerIn, (Mathf.Abs(steerIn) > 0.01f ? 3.2f : 4.5f) * dt);
            float steerDeg = _steer * VehicleRules.SteerLimitDeg(Mathf.Abs(_speed) / vmax);
            float yawRate = VehicleRules.YawRateDeg(_speed, steerDeg);
            float targetLean = riding || Mathf.Abs(_speed) > 0.3f ? VehicleRules.LeanDeg(Mathf.Max(0f, _speed), yawRate) : -5f;
            _lean = Mathf.Lerp(_lean, targetLean, 1f - Mathf.Exp(-6f * dt));
            if (parked) return;

            float oldYaw = _yaw;
            Vector3 oldPos = transform.position;
            _yaw = Mathf.Repeat(_yaw + yawRate * dt, 360f);
            var rot = Quaternion.Euler(0f, _yaw, 0f);
            var fwd = rot * Vector3.forward;
            var oldFwd = Quaternion.Euler(0f, oldYaw, 0f) * Vector3.forward;
            Vector3 frontOld = oldPos + oldFwd * MopedModel.AxleZ, rearOld = oldPos - oldFwd * MopedModel.AxleZ;

            var pos = SweepMove(oldPos, rot, fwd * (_speed * dt), fwd);
            pos = Depenetrate(pos, rot);

            // Innenräume, Autos, Fußgänger
            float dirSign = _speed >= 0f ? 1f : -1f;
            var nose = pos + fwd * dirSign * (MopedModel.AxleZ + 0.3f);
            bool wasIndoor = RideZones.IsIndoor(oldPos, 0.6f);
            bool indoor = !wasIndoor && (RideZones.IsIndoor(nose) || RideZones.IsIndoor(pos, 0.6f));
            bool car = CityTraffic.CarAt(nose, 0.3f) || CityTraffic.CarAt(pos, 0.35f);
            bool ped = Mathf.Abs(_speed) > 0.05f && PedestrianAhead(pos, fwd * dirSign);
            if (indoor || car || ped)
            {
                pos = oldPos;
                _yaw = oldYaw;
                rot = Quaternion.Euler(0f, _yaw, 0f);
                fwd = rot * Vector3.forward;
                if (ped) _speed = 0f;
                else Impact(1f);
            }

            // Boden: zwei Radpunkte, kleine Stufen hoch, sonst fallen
            var front = pos + fwd * MopedModel.AxleZ;
            var rear = pos - fwd * MopedModel.AxleZ;
            float hf = Ground(front, pos.y), hr = Ground(rear, pos.y);
            bool hasF = !float.IsNegativeInfinity(hf), hasR = !float.IsNegativeInfinity(hr);
            float g = hasF && hasR ? (hf + hr) * 0.5f : hasF ? hf : hasR ? hr : pos.y - 5f;
            if (g >= pos.y - 0.005f)
            {
                float rise = g - pos.y;
                if (rise > 0.02f) _bumpY -= Mathf.Min(rise, 0.3f);
                pos.y = g;
                _vy = 0f;
            }
            else
            {
                _vy -= 18f * dt;
                pos.y = Mathf.Max(g, pos.y + _vy * dt);
                if (pos.y <= g + 0.0001f)
                {
                    if (_vy < -3f) Hop(0.6f, 0.5f);
                    _vy = 0f;
                }
            }
            float targetPitch = hasF && hasR ? -Mathf.Atan2(hf - hr, 2f * MopedModel.AxleZ) * Mathf.Rad2Deg : 0f;
            _pitch = Mathf.Lerp(_pitch, Mathf.Clamp(targetPitch, -20f, 20f), 1f - Mathf.Exp(-10f * dt));

            // Bordsteine (nur Optik in der Welt): kleiner Hüpfer
            if (Mathf.Abs(_speed) > 0.4f)
            {
                if (RideZones.CrossesCurb(frontOld, front)) Hop(1f, Mathf.Abs(_speed) / vmax);
                else if (RideZones.CrossesCurb(rearOld, rear)) Hop(0.6f, Mathf.Abs(_speed) / vmax);
            }

            if (pos.y < -15f || float.IsNaN(pos.x) || float.IsNaN(pos.z))
            {
                MopedSpawner.Home(Game.Sim != null ? Game.Sim.LocationStage : 0, out pos, out float hy);
                _yaw = hy;
                rot = Quaternion.Euler(0f, _yaw, 0f);
                _speed = 0f;
                _vy = 0f;
            }
            transform.SetPositionAndRotation(pos, rot);
        }

        private Vector3 SweepMove(Vector3 pos, Quaternion rot, Vector3 delta, Vector3 fwd)
        {
            for (int i = 0; i < 3; i++)
            {
                float dist = delta.magnitude;
                if (dist < 1e-5f) break;
                var dir = delta / dist;
                if (!Cast(pos, rot, dir, dist + Skin, out var hit))
                {
                    pos += delta;
                    break;
                }
                float travel = Mathf.Max(0f, hit.distance - Skin);
                pos += dir * travel;
                var n = hit.normal;
                n.y = 0f;
                if (n.sqrMagnitude < 1e-4f) n = -dir;
                else n.Normalize();
                if (i == 0) Impact(Mathf.Clamp01(-Vector3.Dot(n, fwd * Mathf.Sign(_speed))));
                var rest = dir * (dist - travel);
                delta = Vector3.ProjectOnPlane(rest, n);
                delta.y = 0f;
                if (Mathf.Abs(_speed) < 0.05f) break;
            }
            return pos;
        }

        private bool Cast(Vector3 pos, Quaternion rot, Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            float bestD = float.MaxValue;
            bool found = false;
            var c = pos + rot * BodyCenter;
            int n = Physics.BoxCastNonAlloc(c, BodySize * 0.485f, dir, Hits, rot, dist, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = Hits[i];
                if (IgnoreCollider(h.collider)) continue;
                var body = h.rigidbody;
                if (body != null && !body.isKinematic)
                {
                    Push(body, dir);
                    continue;
                }
                if (h.distance <= 0f) continue; // Überlappung am Start: Depenetrate löst das
                if (h.distance < bestD)
                {
                    bestD = h.distance;
                    best = h;
                    found = true;
                }
            }
            return found;
        }

        private Vector3 Depenetrate(Vector3 pos, Quaternion rot)
        {
            if (_col == null) return pos;
            var c = pos + rot * BodyCenter;
            int n = Physics.OverlapBoxNonAlloc(c, BodySize * 0.5f, Overlaps, rot, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var col = Overlaps[i];
                if (col == null || IgnoreCollider(col)) continue;
                if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic) continue;
                if (Physics.ComputePenetration(_col, pos, rot, col, col.transform.position, col.transform.rotation, out var d, out float dist))
                {
                    d.y = 0f;
                    if (d.sqrMagnitude < 1e-6f) continue;
                    pos += d.normalized * Mathf.Min(dist, 0.25f);
                }
            }
            return pos;
        }

        /// <summary>Höchster Boden unter p (bis 0,45 m über y), −∞ wenn nichts.</summary>
        private float Ground(Vector3 p, float y)
        {
            float best = float.NegativeInfinity;
            var o = new Vector3(p.x, y + 0.45f, p.z);
            int n = Physics.RaycastNonAlloc(o, Vector3.down, Hits, 4f, Mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = Hits[i];
                if (IgnoreCollider(h.collider)) continue;
                if (h.rigidbody != null && !h.rigidbody.isKinematic) continue;
                if (h.point.y > best) best = h.point.y;
            }
            return best;
        }

        private bool IgnoreCollider(Collider c)
        {
            if (c == null || c.isTrigger) return true;
            var t = c.transform;
            if (t.IsChildOf(transform)) return true;
            if (_rider != null && t.IsChildOf(_rider.transform)) return true;
            return false;
        }

        private void Push(Rigidbody body, Vector3 dir)
        {
            float want = Mathf.Max(1.2f, Mathf.Abs(_speed) * 0.9f);
            float along = Vector3.Dot(body.linearVelocity, dir);
            if (along < want) body.AddForce(dir * (want - along) + Vector3.up * 0.4f, ForceMode.VelocityChange);
        }

        /// <summary>Anstoßen: frontal = abprallen (ab Schritttempo mit Rumms), streifend = abbremsen.</summary>
        private void Impact(float head)
        {
            float spd = Mathf.Abs(_speed);
            if (head > 0.7f)
            {
                if (spd > 1.5f)
                {
                    _speed = -_speed * 0.22f;
                    if (_impactCd <= 0f)
                    {
                        _impactCd = 0.35f;
                        float k = Mathf.Clamp01(spd / Vmax);
                        _audio?.Play("moped_bump", 0.35f + 0.65f * k, 0.08f);
                        _shake = Mathf.Max(_shake, 0.025f + 0.06f * k);
                        _bumpV += 0.5f + k;
                    }
                }
                else _speed = 0f;
            }
            else _speed *= 1f - 0.5f * head;
        }

        private void Hop(float strength, float speed01)
        {
            _bumpV += (0.45f + 0.5f * speed01) * strength;
            _shake = Mathf.Max(_shake, (0.006f + 0.02f * speed01) * strength);
            _speed *= 1f - 0.05f * strength;
            if (_impactCd <= 0f)
            {
                _impactCd = 0.18f;
                _audio?.Play("moped_bump", (0.12f + 0.25f * speed01) * strength, 0.12f);
            }
        }

        private void SnapToGround()
        {
            var pos = transform.position;
            float g = Ground(pos, pos.y + 0.5f);
            if (!float.IsNegativeInfinity(g)) pos.y = g;
            transform.position = pos;
        }

        // =====================================================================================
        // Optik & Kamera
        // =====================================================================================
        private void UpdateVisuals(float dt)
        {
            if (Parts == null || Parts.Body == null) return;
            bool riding = _rider != null;
            _bumpV += (-_bumpY * 140f - _bumpV * 13f) * dt;
            _bumpY = Mathf.Clamp(_bumpY + _bumpV * dt, -0.3f, 0.12f);
            float vib = riding && dt > 0f ? Mathf.Sin(Time.time * 95f) * 0.0018f * (1f - Mathf.Clamp01(Mathf.Abs(_speed) / Vmax) * 0.5f) : 0f;
            Parts.Body.localPosition = new Vector3(0f, _bumpY + vib, 0f);
            Parts.Body.localRotation = Quaternion.Euler(_pitch, 0f, -_lean);
            float vmax = Vmax;
            if (Parts.Steer != null)
                Parts.Steer.localRotation = Parts.SteerBase * Quaternion.AngleAxis(_steer * Mathf.Min(32f, VehicleRules.SteerLimitDeg(Mathf.Abs(_speed) / vmax) + 4f), Parts.SteerAxis);
            _wheelAngle = Mathf.Repeat(_wheelAngle + _speed / Mathf.Max(0.1f, Parts.WheelRadius) * Mathf.Rad2Deg * dt, 360f);
            if (Parts.FrontSpin != null) Parts.FrontSpin.localRotation = Parts.FrontBase * Quaternion.AngleAxis(_wheelAngle, Parts.FrontAxis);
            if (Parts.RearSpin != null) Parts.RearSpin.localRotation = Parts.RearBase * Quaternion.AngleAxis(_wheelAngle, Parts.RearAxis);
            if (Parts.Needle != null)
                Parts.Needle.localRotation = Quaternion.Euler(0f, -120f + 240f * Mathf.Clamp01(VehicleRules.MsToKmh(Mathf.Abs(_speed)) / 35f), 0f);
            bool parked = !riding && Mathf.Abs(_speed) < 0.1f;
            if (Parts.Kickstand != null && Parts.Kickstand.activeSelf != parked) Parts.Kickstand.SetActive(parked);
            MatSlot.SetAll(Parts.Lens, riding ? Parts.LensOn : null);
            MatSlot.SetAll(Parts.Tail, !riding ? null : (_brake > 0.1f && _speed > 0.3f ? Parts.TailBrake : Parts.TailOn));
            if (Parts.HeadLight != null)
            {
                bool on = riding && IsDark();
                if (Parts.HeadLight.enabled != on) Parts.HeadLight.enabled = on;
            }
            bool showRider = riding && _thirdPerson, showArms = riding && !_thirdPerson;
            if (Parts.Rider != null && Parts.Rider.gameObject.activeSelf != showRider) Parts.Rider.gameObject.SetActive(showRider);
            if (Parts.ArmL != null && Parts.ArmL.gameObject.activeSelf != showArms) Parts.ArmL.gameObject.SetActive(showArms);
            if (Parts.ArmR != null && Parts.ArmR.gameObject.activeSelf != showArms) Parts.ArmR.gameObject.SetActive(showArms);
        }

        private static bool IsDark()
        {
            var w = Game.World;
            if (w == null || w.Atmos == null) return false;
            float h = w.Atmos.Hours;
            return h >= 18.5f || h < 6.5f;
        }

        private void ToggleView()
        {
            _thirdPerson = !_thirdPerson;
            _tpInit = false;
            Game.Sound("click", 0.02f, -8f);
        }

        private void LateUpdate()
        {
            if (_rider == null || Parts == null || Parts.Body == null) return;
            var cam = _rider.Cam;
            if (cam == null) return;
            float dt = Time.deltaTime;
            bool locked = Game.Root != null && Game.Root.InputLocked;
            if (!locked && !EmoteController.WheelOpen && dt > 0f)
            {
                var look = GameInput.Look;
                _lookYaw += look.x;
                _lookPitch = Mathf.Clamp(_lookPitch - look.y, -55f, 70f);
                if (look.sqrMagnitude > 0.02f) _lookIdle = 0f;
            }
            _lookIdle += dt;
            if (_lookIdle > 1.2f && Mathf.Abs(_speed) > 2.5f)
            {
                _lookYaw = Mathf.MoveTowards(_lookYaw, 0f, 75f * dt);
                if (!_thirdPerson) _lookPitch = Mathf.MoveTowards(_lookPitch, 9f, 20f * dt);
            }
            _lookYaw = _thirdPerson ? Mathf.DeltaAngle(0f, _lookYaw) : Mathf.Clamp(_lookYaw, -140f, 140f);
            Vector3 sh = Vector3.zero;
            if (_shake > 0.0005f)
            {
                if (Settings.ShakeEnabled) sh = Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 0.25f);
            }
            float vmax = Vmax;
            if (!_thirdPerson)
            {
                var eye = Parts.Body.TransformPoint(MopedModel.EyeLocal);
                _rider.transform.SetPositionAndRotation(eye - Vector3.up * MopedModel.EyeLocal.y, Quaternion.Euler(0f, _yaw + _lookYaw, 0f));
                cam.transform.localPosition = new Vector3(0f, MopedModel.EyeLocal.y, 0f) + sh;
                cam.transform.localRotation = Quaternion.Euler(_lookPitch + _pitch, 0f, -_lean * 0.55f);
                if (Parts.GripL != null) MopedModel.AimArm(Parts.ArmL, Parts.Body, MopedModel.ShoulderL, Parts.GripL.position);
                if (Parts.GripR != null) MopedModel.AimArm(Parts.ArmR, Parts.Body, MopedModel.ShoulderR, Parts.GripR.position);
            }
            else
            {
                var pivot = transform.position + Vector3.up * 1.35f;
                float yaw = _yaw + _lookYaw;
                float pitch = Mathf.Clamp(_lookPitch + 4f, -15f, 60f);
                var back = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
                float dist = 3.9f;
                int n = Physics.SphereCastNonAlloc(pivot, 0.25f, back, Hits, dist, Mask, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < n; i++)
                {
                    var h = Hits[i];
                    if (IgnoreCollider(h.collider) || h.distance <= 0f) continue;
                    if (h.rigidbody != null && !h.rigidbody.isKinematic) continue;
                    if (h.distance - 0.05f < dist)
                    {
                        dist = Mathf.Max(0.6f, h.distance - 0.05f);
                        blocked = true;
                    }
                }
                var want = pivot + back * dist;
                if (!_tpInit || blocked) _tpCam = want;
                else _tpCam = Vector3.Lerp(_tpCam, want, 1f - Mathf.Exp(-14f * dt));
                _tpInit = true;
                _rider.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, yaw, 0f));
                cam.transform.position = _tpCam + sh;
                var to = pivot - _tpCam;
                if (to.sqrMagnitude > 1e-4f) cam.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
            }
            cam.fieldOfView = Settings.Fov + 7f * Mathf.Clamp01(Mathf.Abs(_speed) / vmax);
        }

        // =====================================================================================
        // Während der Fahrt: Packstation, Hupe, Fußgänger
        // =====================================================================================
        private Station NearbyShipStation()
        {
            Station best = null;
            float bestD = 5.5f * 5.5f;
            var p = transform.position;
            foreach (var st in Station.All)
            {
                if (st == null || !PostService.IsFreeDrop(st)) continue;
                var d = st.transform.position - p;
                d.y = 0f;
                float d2 = d.sqrMagnitude;
                if (d2 < bestD)
                {
                    bestD = d2;
                    best = st;
                }
            }
            return best;
        }

        private void OnRideInteract()
        {
            var sim = Game.Sim;
            var st = NearbyShipStation();
            if (st != null && sim != null && sim.CountInMoped(ItemKind.Labeled) > 0 && Mathf.Abs(_speed) < 2.5f)
            {
                try
                {
                    PostService.Interact(st, _rider);
                }
                catch (System.Exception e)
                {
                    SafeLog.Warn("moped.ship", e);
                }
                return;
            }
            _wantExit = true;
            _exitAskFrame = Time.frameCount;
        }

        private string RidePrompt()
        {
            if (_wantExit) return "Absteigen …";
            var sim = Game.Sim;
            if (sim == null || sim.CountInMoped(ItemKind.Labeled) == 0) return "";
            var st = NearbyShipStation();
            if (st == null) return "";
            try
            {
                return PostService.Prompt(st, _rider) + " – aus der Heckbox";
            }
            catch (System.Exception)
            {
                return "";
            }
        }

        private void Horn()
        {
            if (_hornCd > 0f) return;
            _hornCd = 0.35f;
            _audio?.Play("moped_horn", 0.9f, 0.02f);
            var world = Game.World;
            if (world == null) return;
            var p = transform.position;
            var fwd = transform.forward;
            foreach (var npc in world.Pedestrians)
            {
                if (npc == null) continue;
                var d = npc.transform.position - p;
                d.y = 0f;
                if (d.sqrMagnitude > 14f * 14f || Vector3.Dot(d, fwd) < -2f) continue;
                if (Random.value < 0.35f && CanSay(npc, 4f)) npc.Say(HornLines[Random.Range(0, HornLines.Length)], 2f);
                if (Vector3.Dot(d, fwd) > 0f && Mathf.Abs(Vector3.Dot(d, transform.right)) < 2f) AddDodge(npc, d);
            }
        }

        /// <summary>Fußgänger direkt vor der Nase (Moped hält an statt durchzufahren).</summary>
        private bool PedestrianAhead(Vector3 pos, Vector3 dir)
        {
            var world = Game.World;
            if (world == null) return false;
            var right = new Vector3(dir.z, 0f, -dir.x);
            foreach (var npc in world.Pedestrians)
            {
                if (npc == null) continue;
                var d = npc.transform.position - pos;
                d.y = 0f;
                float along = Vector3.Dot(d, dir);
                if (along < 0f || along > MopedModel.AxleZ + 0.65f) continue;
                if (Mathf.Abs(Vector3.Dot(d, right)) > 0.5f) continue;
                if (CanSay(npc, 3f)) npc.Say(PedLines[Random.Range(0, PedLines.Length)], 2.2f);
                AddDodge(npc, d);
                return true;
            }
            return false;
        }

        private void WarnPedestrians()
        {
            var world = Game.World;
            if (world == null || Mathf.Abs(_speed) < 1.2f) return;
            float sign = Mathf.Sign(_speed);
            var fwd = transform.forward * sign;
            var right = transform.right;
            var p = transform.position;
            float reach = 2.5f + Mathf.Abs(_speed) * 0.55f;
            foreach (var npc in world.Pedestrians)
            {
                if (npc == null) continue;
                var d = npc.transform.position - p;
                d.y = 0f;
                float along = Vector3.Dot(d, fwd);
                if (along < 0.3f || along > reach) continue;
                if (Mathf.Abs(Vector3.Dot(d, right)) > 1.15f) continue;
                if (CanSay(npc, 6f)) npc.Say(PedLines[Random.Range(0, PedLines.Length)], 2.2f);
                AddDodge(npc, d);
            }
        }

        private bool CanSay(NPC npc, float cooldown)
        {
            float now = Time.time;
            if (_pedCd.TryGetValue(npc, out float t) && now < t) return false;
            _pedCd[npc] = now + cooldown;
            if (_pedCd.Count > 64)
            {
                var dead = new List<NPC>();
                foreach (var kv in _pedCd)
                    if (kv.Key == null || kv.Value < now) dead.Add(kv.Key);
                foreach (var k in dead) _pedCd.Remove(k);
            }
            return true;
        }

        /// <summary>Passant macht einen Schritt zur Seite (weg von der Fahrlinie).</summary>
        private void AddDodge(NPC npc, Vector3 offsetFromMoped)
        {
            foreach (var dg in _dodges)
                if (dg.Npc == npc) return;
            var right = transform.right;
            float side = Vector3.Dot(offsetFromMoped, right);
            var dir = (side >= 0f ? right : -right);
            dir.y = 0f;
            _dodges.Add(new Dodge { Npc = npc, Dir = dir.normalized, Until = Time.time + 0.55f });
        }

        private void TickDodges(float dt)
        {
            for (int i = _dodges.Count - 1; i >= 0; i--)
            {
                var dg = _dodges[i];
                if (dg.Npc == null || Time.time > dg.Until)
                {
                    _dodges.RemoveAt(i);
                    continue;
                }
                var t = dg.Npc.transform;
                var next = t.position + dg.Dir * (2.4f * dt);
                if (RideZones.IsIndoor(next, 0f) || Physics.CheckSphere(next + Vector3.up * 0.6f, 0.22f, Mask, QueryTriggerInteraction.Ignore))
                {
                    _dodges.RemoveAt(i);
                    continue;
                }
                t.position = next;
            }
        }

        // =====================================================================================
        // Box, Marke, Spielstand
        // =====================================================================================
        private void UpdateCargo()
        {
            var sim = Game.Sim;
            if (sim == null || Parts == null) return;
            int sig = 17;
            unchecked
            {
                foreach (var it in sim.Moped.Box)
                    sig = sig * 31 + (it == null ? 0 : ((int)it.Kind * 7919 + it.OrderId * 13 + it.EffectivePackSize));
                sig = sig * 31 + sim.Moped.Box.Count;
            }
            if (sig == _cargoSig) return;
            _cargoSig = sig;
            MopedModel.BuildCargo(Parts.Cargo, sim.Moped.Box);
        }

        private void CheckBrand()
        {
            var sim = Game.Sim;
            if (sim == null || _rider != null) return;
            string sig = sim.BrandName + "|" + ColorUtility.ToHtmlStringRGB(sim.BrandColor.ToColor());
            if (sig != _brandSig) BuildModel();
        }

        /// <summary>Standort in den Spielstand (Core) schreiben.</summary>
        public void SaveState()
        {
            var sim = Game.Sim;
            if (sim == null) return;
            var p = transform.position;
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)) return;
            sim.Moped.Placed = true;
            sim.Moped.Pos = p.ToV3();
            sim.Moped.RotY = _yaw;
        }

        /// <summary>Moped an eine Stelle setzen (Stammplatz, Admin „Moped zu mir“).</summary>
        public void PlaceAt(Vector3 pos, float yaw)
        {
            if (_rider != null) return;
            _speed = 0f;
            _vy = 0f;
            _steer = 0f;
            _yaw = Mathf.Repeat(yaw, 360f);
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yaw, 0f));
            SnapToGround();
            SaveState();
        }

        /// <summary>Hook aus GameRoot.CollectWorldState: Standort sichern; fährt der Spieler, wird er neben das Moped gespeichert.</summary>
        public static void CollectState(Sim sim)
        {
            var m = Instance;
            if (sim == null || m == null) return;
            m.SaveState();
            if (m._rider != null && sim.PlayerState != null)
            {
                sim.PlayerState.Pos = m.ExitSpot().ToV3();
                sim.PlayerState.RotY = m._yaw;
            }
        }

        /// <summary>Admin-Panel: Moped vor den Spieler stellen.</summary>
        public static void AdminBring()
        {
            var p = Game.Player;
            var m = Instance;
            if (p == null) return;
            if (m == null)
            {
                Game.Notify("Das Moped gibt es erst nach Kalles Imbiss (Business-Phase).", "info");
                return;
            }
            if (m._rider != null) return;
            var fwd = p.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            m.PlaceAt(p.transform.position + fwd * 2.4f + Vector3.up * 0.3f, p.Yaw + 90f);
            Game.Notify("Moped steht vor dir.", "info");
        }

        /// <summary>Etikettierte Pakete in der Box, wenn das Moped höchstens 8 m von pos steht (Abgabe an der Packstation).</summary>
        public static int LabeledNear(Vector3 pos)
        {
            var m = Instance;
            var sim = Game.Sim;
            if (m == null || sim == null || !m.Near(pos)) return 0;
            return sim.CountInMoped(ItemKind.Labeled);
        }

        /// <summary>Warenwert der etikettierten Pakete in der Box (für den Hinweis an der Packstation).</summary>
        public static int LabeledValueNear(Vector3 pos)
        {
            var m = Instance;
            var sim = Game.Sim;
            if (m == null || sim == null || !m.Near(pos)) return 0;
            int sum = 0;
            foreach (var it in sim.Moped.Box)
                if (it != null && it.Kind == ItemKind.Labeled) sum += it.Price;
            return sum;
        }

        /// <summary>Entnimmt die etikettierten Pakete aus der Box, wenn das Moped in der Nähe steht.</summary>
        public static List<ItemData> TakeLabeledNear(Vector3 pos)
        {
            var m = Instance;
            var sim = Game.Sim;
            if (m == null || sim == null || !m.Near(pos)) return new List<ItemData>();
            return sim.TakeLabeledFromMoped();
        }

        private bool Near(Vector3 pos)
        {
            var d = transform.position - pos;
            d.y = 0f;
            return d.sqrMagnitude <= 8f * 8f;
        }

        /// <summary>Für den Verkehr: Position des abgestellten Mopeds (fährt der Spieler, zählt der Spieler selbst).</summary>
        public static bool ParkedObstacle(out Vector3 pos)
        {
            var m = Instance;
            pos = default;
            if (m == null || m._rider != null) return false;
            pos = m.transform.position;
            return true;
        }

        /// <summary>Kompass: wo steht das Moped (null beim Fahren oder ohne Moped)?</summary>
        public static Vector3? CompassPos()
        {
            var m = Instance;
            if (m == null || m._rider != null) return null;
            return m.transform.position;
        }
    }

    /// <summary>Die Heckbox als eigener Anvisier-Bereich: einladen, herausnehmen.</summary>
    public sealed class MopedBox : MonoBehaviour, IInteractable
    {
        private Moped _owner;
        private readonly Highlighter _hl = new Highlighter();

        public void Setup(Moped owner)
        {
            _owner = owner;
            if (owner != null && owner.Parts != null) _hl.Collect(transform, owner.Parts.Cargo);
        }

        public string Title => "Heckbox";

        public string Prompt(PlayerController player)
        {
            var sim = Game.Sim;
            if (_owner == null || sim == null || player == null || _owner.IsRidden) return "";
            var box = sim.Moped.Box;
            if (player.Held != null && _owner.CountLoadable(player) > 0)
                return "In die Heckbox laden (" + box.Count + "/" + sim.MopedDef.ParcelSlots + ")";
            if (box.Count == 0) return player.Held != null ? "Passt nicht – nur Pakete oder eine Kiste" : "Heckbox ist leer (Pakete in der Hand → einladen)";
            var top = box[box.Count - 1];
            if (player.Held == null || player.CanStack(top))
                return "Herausnehmen: " + top.Describe() + (box.Count > 1 ? " (" + box.Count + " drin)" : "");
            return "Hände voll";
        }

        public void Interact(PlayerController player)
        {
            if (_owner == null || player == null || _owner.IsRidden) return;
            if (player.Held != null && _owner.CountLoadable(player) > 0)
            {
                _owner.Load(player);
                return;
            }
            _owner.TakeOne(player);
        }

        public void SetHighlighted(bool on) => _hl.Set(on && _owner != null && !_owner.IsRidden);
    }
}
