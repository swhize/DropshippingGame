using System;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using DropshippingGame.UI;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Herzstück der Unity-Schicht. Startet automatisch (auch in einer leeren Szene), erzeugt
    /// Simulation, Audio und Oberfläche, zeigt das Hauptmenü mit der Welt als Kulisse und steuert
    /// die Spielsitzung: Story mit Kalle, Tagesabschluss, Ereignisse, Pitch-Minispiel, Pause,
    /// Handy (Tab / Y), Eingabesperren und Speichern. Den vollen Laptop öffnet die Schreibtisch-
    /// Station (Sim.OpenPc), nicht mehr die Tab-Taste.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private readonly HashSet<string> _locks = new HashSet<string>();
        private readonly HashSet<string> _pauses = new HashSet<string>();
        private Sim _sim;
        private UIRoot _ui;
        private WorldBuilder _world;
        private PlayerController _player;
        private Camera _menuCam;
        private float _menuT, _lightAcc, _autosaveAcc;
        private bool _inMenu = true;

        /// <summary>true, solange Menü, Laptop, Fenster oder Dialog die Steuerung übernehmen.</summary>
        public bool InputLocked => _inMenu || _locks.Count > 0;
        public bool Paused => _pauses.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (FindFirstObjectByType<GameRoot>() != null) return;
            new GameObject("Dropshipping Game").AddComponent<GameRoot>();
        }

        // =====================================================================================
        // Start
        // =====================================================================================
        private void Awake()
        {
            if (Game.Root != null && Game.Root != this)
            {
                Destroy(gameObject);
                return;
            }
            Game.Root = this;
            DisableForeignSceneObjects();
            Settings.Load();
            _sim = new Sim { SaveDir = Path.Combine(Application.persistentDataPath, "saves") };
            Game.Sim = _sim;
            Game.Audio = AudioManager.Create(transform);
            _ui = UIRoot.Create(transform);
            Game.UI = _ui;
            Settings.ApplyDisplay();
            Settings.Changed += PostFX.ApplyQuality;
            HookSim();
            HookUi();
        }

        /// <summary>
        /// Kamera, Licht und AudioListener aus einer Standard-Szene abschalten – die Welt bringt
        /// ihre eigenen mit. So läuft das Spiel auch, wenn man ▶ in einer neuen, leeren Szene drückt.
        /// </summary>
        private void DisableForeignSceneObjects()
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.transform.root != transform) c.gameObject.SetActive(false);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.transform.root != transform) l.gameObject.SetActive(false);
            foreach (var a in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (a.transform.root != transform) a.enabled = false;
        }

        private void Start()
        {
            // Beim allerersten Start: kurz schwarz, dann ins Menü blenden.
            _ui.Fader.SetBlack();
            EnterMenu();
            _ui.Fader.FadeIn(1.2f);
        }

        private void OnDestroy()
        {
            Settings.Changed -= PostFX.ApplyQuality;
            if (Game.Root == this) Game.Root = null;
            Time.timeScale = 1f;
        }

        private void OnApplicationQuit()
        {
            if (!_inMenu && CanAutosave()) _sim.SaveGame(true);
        }

        private void HookUi()
        {
            _ui.Menu.OnNewGame = (mode, slot) => _ui.Fader.FadeOut(() => StartNewGame(mode, slot));
            _ui.Menu.OnLoad = slot => _ui.Fader.FadeOut(() => LoadGame(slot));
            _ui.Menu.OnQuit = Quit;
            _ui.Pause.OnMainMenu = ToMenu;
            _ui.Pause.OnQuit = Quit;
            _ui.Pause.OnHelp = ShowHelp;
        }

        private void HookSim()
        {
            var s = _sim;
            s.EconomyChanged += OnEconomyChanged;
            s.Toast += (text, kind) => _ui.Hud.AddToast(text, kind);
            s.PcToggled += OnPcToggled;
            s.DayEnded += ShowSummary;
            s.DayStarted += day => _ui.Hud.ShowBanner("Tag " + day, DayTagline(), UiFmt.Weekday(day).ToUpperInvariant());
            s.LevelUp += level => _ui.Hud.ShowLevelUp(level);
            s.GoalCompleted += g => _ui.Hud.ShowBanner("Ziel erreicht", g.Title + " · +" + Fmt.Money(g.Reward), "MEILENSTEIN");
            s.StoryChanged += _ =>
            {
                _ui.Hud.Refresh();
                _ui.Phone.MarkDirty();
            };
            s.ObjectiveChanged += () => _ui.Hud.Refresh();
            s.LocationChanged += OnLocationChanged;
            s.DeliveryIncoming += _ =>
            {
                if (_world != null) _world.SendVan();
            };
            s.PackageShipped += (pkg, reward, where) =>
            {
                if (_world != null && !where.IsZero) _world.SpawnFloatText(where.ToVector3() + Vector3.up * 0.4f, "+" + Fmt.Money(reward), Theme.Good);
            };
            s.StandSale += (pid, price) =>
            {
                if (_world != null) _world.SpawnFloatText(WorldBuilder.StandPos + new Vector3(0, 1.6f, 0), "+" + Fmt.Money(price), Theme.Good);
            };
            s.DialogueRequested += OnDialogue;
            s.EndDayRequested += ConfirmEndDay;
            s.GameOver += ShowGameOver;
            s.GameWon += ShowEnding;
            s.WorldChanged += () =>
            {
                if (_world != null) _world.RefreshWorld(true);
            };
            s.StaffChanged += () =>
            {
                if (_world != null) _world.RefreshStaff();
            };
            s.ConveyorChanged += () =>
            {
                if (_world != null) _world.SyncConveyor();
            };
            s.SoundRequested += (name, pitch, vol) => Game.Sound(name, pitch, vol);
            s.Events.ChoiceEvent += ShowEventChoice;
            s.Events.MailChanged += () =>
            {
                if (_ui.Laptop.IsOpen)
                {
                    _ui.Laptop.UpdateBadges();
                    _ui.Laptop.MarkDirty();
                }
                _ui.Phone.MarkDirty();
                _ui.Hud.Refresh();
            };
            s.Events.MinigameRequested += OnMinigame;
            s.Market.TradingChanged += () => _ui.Laptop.OnTradingTick();
            s.CollectWorldState = CollectWorldState;
        }

        private void OnEconomyChanged()
        {
            _ui.Hud.Refresh();
            if (_ui.Laptop.IsOpen)
            {
                _ui.Laptop.MarkDirty();
                _ui.Laptop.UpdateBadges();
            }
            if (_ui.Phone.IsOpen) _ui.Phone.MarkDirty();
        }

        private string DayTagline()
        {
            string[] lines =
            {
                "Neuer Tag, neue Pakete.", "Kaffee an, Laptop auf.", "Die Kunden warten schon.", "Heute wird ein guter Tag.",
                "Dein Imperium wächst.", "Zettel checken: " + GameInput.KeyLabel("phone") + " fürs Handy.",
            };
            if (_sim.Day % 7 == 1) return "Neue Woche, neues Glück.";
            return lines[_sim.Day % lines.Length];
        }

        // =====================================================================================
        // Sperren & Pause
        // =====================================================================================
        public void Lock(string key, bool on)
        {
            if (on) _locks.Add(key);
            else _locks.Remove(key);
            // Zurück im Spiel: Fokus von UI-Buttons lösen, sonst lösen Leertaste/Enter
            // (Springen) versteckte Buttons erneut aus.
            if (!on && _locks.Count == 0) BlurUi();
            ApplyCursor();
        }

        private void BlurUi()
        {
            var focused = _ui != null && _ui.Root != null ? _ui.Root.panel?.focusController?.focusedElement : null;
            focused?.Blur();
        }

        public void SetPaused(string key, bool on)
        {
            if (on) _pauses.Add(key);
            else _pauses.Remove(key);
            Time.timeScale = Paused ? 0f : 1f;
            if (Game.Audio != null) Game.Audio.SetMuffled(_pauses.Contains("pause"));
        }

        private void ClearLocks()
        {
            _locks.Clear();
            _pauses.Clear();
            Time.timeScale = 1f;
            ApplyCursor();
        }

        private void ApplyCursor()
        {
            bool free = InputLocked;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        public void OnHeldChanged()
        {
            if (_ui != null && _player != null) _ui.Hud.SetHeld(_player.Held);
        }

        // =====================================================================================
        // Menü, Neues Spiel, Laden
        // =====================================================================================
        private void EnterMenu()
        {
            _ui.Phone.Close(true);
            DestroyGame();
            _inMenu = true;
            _sim.InGame = false;
            _sim.PcOpen = false;
            ClearLocks();
            BuildWorld(true);
            var camGo = new GameObject("MenuCamera");
            camGo.transform.SetParent(_world.transform, false);
            camGo.tag = "MainCamera";
            _menuCam = camGo.AddComponent<Camera>();
            _menuCam.fieldOfView = 58f;
            _menuCam.nearClipPlane = 0.1f;
            _menuCam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            PostFX.SetupCamera(_menuCam);
            PostFX.SetMenuLook(true);
            PostFX.SetDimmed(0f);
            _menuT = 0f;
            UpdateMenuCamera(0f);
            _world.Atmos.SetTimeOfDay(19.35f);
            _world.Atmos.RenderReflections();
            _ui.Hud.SetVisible(false);
            _ui.Menu.Open();
            Game.Audio.PlayMusic("menu");
        }

        private void UpdateMenuCamera(float dt)
        {
            if (_menuCam == null) return;
            _menuT += dt;
            float x = -18f + Mathf.Sin(_menuT * 0.045f) * 20f;
            _menuCam.transform.position = new Vector3(x, 2.9f + Mathf.Sin(_menuT * 0.11f) * 0.25f, 12.3f);
            _menuCam.transform.LookAt(new Vector3(x * 0.7f - 6f, 2.7f, -9f));
        }

        private void StartNewGame(string mode, int slot)
        {
            _sim.Slot = Mathf.Clamp(slot, 1, Sim.SaveSlots);
            _sim.NewGame(mode);
            EnterGame(false);
        }

        private void LoadGame(int slot)
        {
            if (!_sim.LoadGame(slot))
            {
                _ui.Fader.FadeIn(0.5f);
                _sim.Notify("Spielstand " + slot + " konnte nicht geladen werden.", "bad");
                return;
            }
            EnterGame(true);
        }

        private void BuildWorld(bool menuMode)
        {
            var go = new GameObject("World");
            _world = go.AddComponent<WorldBuilder>();
            Game.World = _world;
            _world.Build(menuMode);
        }

        /// <summary>Welt und Spieler sofort entfernen (vor dem Neuaufbau im selben Frame).</summary>
        private void DestroyGame()
        {
            if (_player != null) DestroyImmediate(_player.gameObject);
            _player = null;
            Game.Player = null;
            if (_world != null) DestroyImmediate(_world.gameObject);
            _world = null;
            Game.World = null;
            _menuCam = null;
            DroppedItem.All.RemoveAll(d => d == null);
        }

        private void EnterGame(bool fromSave)
        {
            _ui.Menu.Close();
            _ui.Modal.Close();
            _ui.BigModal.Close();
            _ui.Phone.Close(true);
            _ui.Laptop.ResetBoot();
            DestroyGame();
            _inMenu = false;
            ClearLocks();
            _player = PlayerController.Create(null);
            Game.Player = _player;
            BuildWorld(false);
            _world.RefreshStaff();
            PlacePlayer(fromSave);
            RestoreWorldItems();
            _sim.InGame = true;
            _autosaveAcc = 0f;
            PostFX.SetMenuLook(false);
            PostFX.SetDimmed(0f);
            _world.Atmos.SetTimeOfDay(_sim.TimeMinutes / 60f);
            _world.Atmos.RenderReflections();
            _ui.Hud.SetVisible(true);
            _ui.Hud.SetHeld(_player.Held);
            Game.Audio.PlayMusic(_sim.StoryStage == "diner" ? "diner" : "work");
            ApplyCursor();
            _ui.Fader.FadeIn(1f);
            if (_sim.StoryStage == "diner")
                _sim.Notify("Kalles Imbiss, 17:30 Uhr. Deine Schicht beginnt.", "info");
            else if (fromSave)
                _sim.Notify("Willkommen zurück bei " + _sim.BrandName + ". " + UiFmt.DayLong(_sim.Day) + ".", "info");
            else
            {
                _ui.Hud.ShowBanner("Tag 1", "Deine Garage. Dein Business.", UiFmt.Weekday(1).ToUpperInvariant());
                PhoneTipLater();
            }
        }

        /// <summary>Einmaliger Hinweis auf das Handy (ein paar Sekunden nach dem Start).</summary>
        private void PhoneTipLater()
        {
            var sim = _sim;
            Anim.Delay(7f, () =>
            {
                if (_inMenu || sim != _sim || sim.StoryStage != "business") return;
                sim.Notify("Tipp: " + GameInput.KeyLabel("phone") + " öffnet dein Handy – Bestellungen, Nachrichten, Nachbestellen.", "info");
            }, true);
        }

        private void PlacePlayer(bool useSaved)
        {
            var ps = _sim.PlayerState;
            if (useSaved && ps != null && _sim.StoryStage == "business" && !ps.Pos.IsZero)
            {
                _player.Teleport(ps.Pos.ToVector3() + new Vector3(0, 0.05f, 0), ps.RotY);
                if (ps.Held != null) _player.Hold(ps.Held);
            }
            else _player.Teleport(_world.SpawnPoint(), _world.SpawnYaw());
            _sim.PlayerState = null;
        }

        private void RestoreWorldItems()
        {
            foreach (var w in _sim.WorldItems)
                if (w.Item != null) _player.SpawnDropped(w.Item, w.Pos.ToVector3(), w.RotY);
            _sim.WorldItems.Clear();
        }

        private void CollectWorldState()
        {
            _sim.WorldItems.Clear();
            foreach (var d in DroppedItem.All)
            {
                if (d == null || d.Data == null) continue;
                _sim.WorldItems.Add(new WorldItemSave { Item = d.Data.Clone(), Pos = d.transform.position.ToV3(), RotY = d.transform.eulerAngles.y });
            }
            _sim.PlayerState = _player != null ? _player.ToSave() : null;
        }

        private bool CanAutosave() => _sim.StoryStage == "business" && !_sim.DayOver && _player != null;

        private void ToMenu()
        {
            if (CanAutosave()) _sim.SaveGame(true);
            _ui.Pause.Close();
            _ui.Phone.Close(true);
            _sim.ClosePc();
            _ui.Fader.FadeOut(() =>
            {
                _ui.Modal.Close();
                _ui.BigModal.Close();
                EnterMenu();
                _ui.Fader.FadeIn(0.8f);
            });
        }

        private void Quit()
        {
            if (!_inMenu && CanAutosave()) _sim.SaveGame(true);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // =====================================================================================
        // Schleife & Eingabe
        // =====================================================================================
        private void Update()
        {
            float udt = Time.unscaledDeltaTime;
            if (_inMenu)
            {
                UpdateMenuCamera(udt);
                if (!_ui.Fader.Busy && GameInput.CancelDown && _ui.Menu.IsOpen) _ui.Menu.Back();
                return;
            }
            if (_player == null || _world == null) return;
            if (!Paused) _sim.Tick(Time.deltaTime);
            HandleInput();

            _lightAcc += Time.deltaTime;
            if (_lightAcc >= 0.2f)
            {
                _lightAcc = 0f;
                _world.Atmos.SetTimeOfDay(_sim.TimeMinutes / 60f);
            }
            if (_player.transform.position.y < -20f) _player.Teleport(_world.SpawnPoint(), _world.SpawnYaw());

            // Automatisch speichern (alle 90 Sekunden, nur wenn gerade nichts offen ist).
            _autosaveAcc += udt;
            if (_autosaveAcc > 90f && !InputLocked && CanAutosave())
            {
                _autosaveAcc = 0f;
                _sim.SaveGame(true);
            }
        }

        private void HandleInput()
        {
            if (_ui.Fader.Busy) return;
            // Alle Tasten genau einmal pro Frame abfragen (wasPressedThisFrame).
            bool pause = GameInput.PauseDown;
            bool cancel = GameInput.CancelDown;
            bool phoneKey = GameInput.PhoneDown;
            bool prevTab = GameInput.PrevTabDown;
            bool nextTab = GameInput.NextTabDown;
            int number = GameInput.NumberDown;

            if (GameInput.ScreenshotDown) TakeScreenshot();

            if (_ui.Pause.IsOpen)
            {
                if (cancel) _ui.Pause.Back();
                return;
            }
            if (_sim.PcOpen)
            {
                if (AppBranding.Typing)
                {
                    if (pause) _ui.Root.panel?.focusController?.focusedElement?.Blur();
                    return;
                }
                if (_ui.BigModal.IsOpen || _ui.Modal.IsOpen)
                {
                    // Fenster über dem Laptop (z. B. Ereignis): erst das Fenster bedienen.
                    HandleModalCancel(cancel);
                    return;
                }
                if (cancel || phoneKey)
                {
                    _sim.ClosePc();
                    return;
                }
                if (prevTab) _ui.Laptop.CycleApp(-1);
                else if (nextTab) _ui.Laptop.CycleApp(1);
                return;
            }
            if (_ui.BigModal.IsOpen || _ui.Modal.IsOpen)
            {
                HandleModalCancel(cancel);
                return;
            }
            if (_ui.Dialogue.Active) return;
            if (_ui.Phone.IsOpen)
            {
                if (phoneKey)
                {
                    _ui.Phone.Close();
                    return;
                }
                if (cancel)
                {
                    if (!_ui.Phone.Back()) _ui.Phone.Close();
                    return;
                }
                if (prevTab) _ui.Phone.CycleApp(-1);
                else if (nextTab) _ui.Phone.CycleApp(1);
                else if (number > 0) _ui.Phone.SelectApp(number - 1);
                return;
            }
            if (pause)
            {
                _ui.Pause.Open();
                return;
            }
            if (GameInput.HelpDown)
            {
                ShowHelp();
                return;
            }
            if (phoneKey) _ui.Phone.Open();
        }

        /// <summary>Esc/B in Fenstern: Hilfe, Credits, Bestätigungen und Ergebnisse schließen; Ereignis = „Später“.</summary>
        private void HandleModalCancel(bool cancel)
        {
            if (!cancel) return;
            var m = _ui.BigModal.IsOpen ? _ui.BigModal : _ui.Modal;
            switch (m.Current)
            {
                case "help":
                case "credits":
                case "confirm":
                case "event_result":
                case "event":
                    m.Close();
                    break;
            }
        }

        private void TakeScreenshot()
        {
            string dir = Path.Combine(Application.persistentDataPath, "screenshots");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path);
            _sim.Notify("Screenshot gespeichert.", "info");
        }

        private void OnPcToggled(bool open)
        {
            if (open)
            {
                if (_player != null && _player.Focus != null) _player.Focus.SetHighlighted(false);
                _ui.Phone.Close(true);
                Lock("pc", true);
                _ui.Laptop.Open();
                PostFX.SetDimmed(1f);
            }
            else
            {
                AppBranding.Typing = false;
                _ui.Laptop.Close();
                Lock("pc", false);
                PostFX.SetDimmed(0f);
            }
        }

        private void OnLocationChanged(int stage)
        {
            if (_world == null) return;
            _world.RebuildStations();
            _world.RefreshWorld(true);
            _world.RefreshStaff();
            _world.Atmos.RenderReflections();
            _sim.Events.AddMail("Immobilien Schulze", "Willkommen in der Lagerhalle!",
                "Die Schlüssel liegen unter der Fußmatte, das Rolltor ist offen. Viel Erfolg mit dem Laden! PS: Das Förderband gibt's gegen Aufpreis.", "building");
        }

        // =====================================================================================
        // Story: Kalle
        // =====================================================================================
        private static DialogueView.Line L(string speaker, string text) => new DialogueView.Line(speaker, text);

        private void OnDialogue(string id)
        {
            if (id != "kalle") return;
            var d = _ui.Dialogue;
            if (_sim.StoryStage == "diner")
            {
                switch (_sim.IntroStep)
                {
                    case 0:
                        d.Start(new[]
                        {
                            L("Kalle", "Da bist du ja endlich! Fünf Minuten zu spät. Wie immer."),
                            L("Kalle", "Tisch 2, 3 und 4 warten auf ihr Essen. Die Teller stehen da vorne an der Durchreiche."),
                            L("Kalle", "Zack zack! Und Handy weg, sonst zieh ich's dir vom Lohn ab."),
                        }, null, _ => _sim.StartShift());
                        break;
                    case 1:
                        d.Start(new[] { L("Kalle", "Was stehst du hier rum?! Die Teller werden kalt!") });
                        break;
                    default:
                        d.Start(new[]
                        {
                            L("Kalle", "Na endlich. Hat ja ewig gedauert."),
                            L("Kalle", "Und glaub nicht, ich hätte nicht gesehen, wie du vorhin wieder auf dein Handy gestarrt hast."),
                            L("Du", "(Dieses Video ... „Mit 19 Millionär – dank Dropshipping“. Und die Garage gegenüber steht leer ...)"),
                        }, new[] { "Ich kündige.", "Ich mach mein eigenes Ding. Online-Shop." }, OnQuitChoice);
                        break;
                }
                return;
            }
            string text = GameData.KalleIdle[UnityEngine.Random.Range(0, GameData.KalleIdle.Length)];
            if (_sim.EndingSeen) text = "Hunderttausend Umsatz?! Kleiner ... hast du vielleicht 'nen Job für mich?";
            else if (_sim.LocationStage >= 1 && UnityEngine.Random.value < 0.5f)
                text = "Ich hab gehört, du hast jetzt 'ne ganze Lagerhalle. Respekt. Sag's aber keinem, dass ich das gesagt hab.";
            d.Start(new[] { L("Kalle", text) });
        }

        private void OnQuitChoice(int choice)
        {
            string reply = choice == 1
                ? "Online-Shop? DU? Hahaha! ... Na gut. Viel Glück mit deinem Internet-Kram."
                : "Du ... WAS? Nach allem, was ich für dich getan hab? Pff. Na dann geh doch!";
            _ui.Dialogue.Start(new[]
            {
                L("Kalle", reply),
                L("Kalle", "Hier. 150 Euro, dein letzter Lohn. Mach was draus – und komm nicht angekrochen."),
            }, null, _ => _ui.Fader.Transition("Am nächsten Morgen ...", FinishIntro, 1.6f));
        }

        private void FinishIntro()
        {
            _sim.FinishIntro();
            _player.ClearHands();
            _world.RebuildStations();
            _world.RefreshWorld(false);
            _player.Teleport(_world.SpawnPoint(), _world.SpawnYaw());
            _world.Atmos.SetTimeOfDay(_sim.TimeMinutes / 60f);
            _world.Atmos.RenderReflections();
            Game.Audio.PlayMusic("work");
            _sim.Notify("Tag 1. Deine Garage. Dein Business. Los geht's!", "good");
            _ui.Hud.ShowBanner("Tag 1", "Deine Garage. Dein Business.", UiFmt.Weekday(1).ToUpperInvariant());
            _sim.SaveGame(true);
            PhoneTipLater();
        }

        // =====================================================================================
        // Tagesablauf
        // =====================================================================================
        private void ConfirmEndDay()
        {
            if (!_sim.IsOpen) return;
            var body = _ui.Modal.Open("Feierabend machen?", "Es ist " + Fmt.Clock(_sim.TimeMinutes) + " Uhr.", 480, new[]
            {
                new ModalButton("Ja, Tag beenden", () => _sim.EndDayNow(), "accent", "moon"),
                new ModalButton("Weiterarbeiten", null, "ghost"),
            }, true, "confirm", null, "moon");
            _ui.Modal.Text(body, "Offene Bestellungen bleiben bis morgen liegen. Miete, Löhne und Zinsen (" + Fmt.Money(_sim.FixedCostsPerDay()) +
                                 ") werden abgerechnet.", "muted");
        }

        private void ShowSummary(DaySummary s)
        {
            if (_sim.PcOpen) _sim.ClosePc();
            _ui.Phone.Close(true);
            _ui.Modal.Close();
            Game.Sound("notify");
            SummaryView.Show(_ui.BigModal, _sim, s, NextDay);
        }

        private void NextDay()
        {
            int next = _sim.Day + 1;
            _ui.Fader.Transition("Tag " + next, DoNextDay, 0.9f, UiFmt.Weekday(next).ToUpperInvariant());
        }

        private void DoNextDay()
        {
            _player.Teleport(_world.SpawnPoint(), _world.SpawnYaw());
            _sim.StartNextDay();
            if (_sim.DayOver) return;
            _world.RefreshWorld(false);
            _world.RebuildStations();
            _world.Atmos.SetTimeOfDay(_sim.TimeMinutes / 60f);
            _world.Atmos.RenderReflections();
        }

        private void ShowGameOver(string reason)
        {
            Game.Sound("bad");
            var body = _ui.BigModal.Open("Pleite!", "Dein Konto ist tief im Minus. Die Bank zieht den Stecker.", 540, new[]
            {
                new ModalButton("Letzten Spielstand laden", () => _ui.Fader.FadeOut(() => LoadGame(_sim.Slot)), "accent", "save"),
                new ModalButton("Hauptmenü", ToMenu, "ghost", "home"),
            }, true, "gameover", null, "trend_down");
            _ui.BigModal.Text(body, "Du hast " + _sim.Day + " Tage durchgehalten, " + _sim.TotalShipped + " Pakete verschickt und " +
                                    Fmt.Money(_sim.TotalEarned) + " umgesetzt. Nicht schlecht – aber das Geld ist weg.");
            if (!_sim.HasSave(_sim.Slot)) _ui.BigModal.Text(body, "Hinweis: Für diesen Spielstand gibt es noch keine Speicherung.", "muted");
        }

        private void ShowEnding()
        {
            Game.Sound("levelup");
            var body = _ui.BigModal.Open("Du hast es geschafft!", "100.000 € Umsatz. Vom Imbiss zum Imperium.", 580, new[]
            {
                new ModalButton("Weiterspielen", null, "accent", "play"),
                new ModalButton("Credits", ShowCredits, "ghost", "heart"),
            }, true, "ending", null, "trophy");
            _ui.BigModal.Text(body, "Vor " + _sim.Day + " Tagen hast du noch Teller bei Kalle getragen. Heute gehört dir eine Marke mit " +
                                    Fmt.Rating(_sim.Reputation) + " Sternen, " + Fmt.Thousands(_sim.TotalShipped) + " verschickten Paketen und einem Team, das für dich arbeitet.");
            _ui.BigModal.Text(body, "Das Spiel ist hier nicht zu Ende – bau dein Imperium weiter aus, so lange du willst.", "muted");
        }

        private void ShowCredits()
        {
            var body = _ui.Modal.Open("Credits & Quellen", "Dropshipping Simulator – Vom Imbiss zum Imperium", 720, new[]
            {
                new ModalButton("Schließen", null, "accent", "check"),
            }, true, "credits", null, "heart");
            CreditsView.Build(body);
        }

        private void ShowHelp()
        {
            _ui.Pause.Close();
            var body = _ui.Modal.Open("Steuerung & Hilfe", "Tastatur, Maus und Controller – und wie dein Business läuft.", 820, new[]
            {
                new ModalButton("Verstanden", null, "accent", "check"),
            }, true, "help", null, "help");
            HelpView.Build(body);
        }

        // =====================================================================================
        // Ereignisse & Minispiel
        // =====================================================================================
        private void ShowEventChoice(Mail mail)
        {
            if (mail == null || !mail.Pending) return;
            if (_ui.Modal.IsOpen || _ui.BigModal.IsOpen || _ui.Dialogue.Active || _ui.Pause.IsOpen || _inMenu) return;
            // Handy offen: nicht dazwischenfunken – die Entscheidung steht dort unter „Nachrichten“.
            if (_ui.Phone.IsOpen)
            {
                _ui.Phone.MarkDirty();
                return;
            }
            EventView.Show(_ui.Modal, _sim, mail, ci => Decide(mail.Id, ci));
        }

        private void Decide(int mailId, int choice)
        {
            string res = _sim.Events.Choose(mailId, choice);
            var mail = _sim.Events.Find(mailId);
            if (res == "")
            {
                // Zu wenig Geld oder Minispiel gestartet: Fenster bleibt verfügbar.
                if (mail != null && mail.Pending && !_ui.Modal.IsOpen && !_ui.BigModal.IsOpen) ShowEventChoice(mail);
                return;
            }
            EventView.ShowResult(_ui.Modal, mail, res);
        }

        private void OnMinigame(Mail mail, string kind)
        {
            if (kind != "pitch") return;
            if (_sim.PcOpen) _sim.ClosePc();
            _ui.Phone.Close(true);
            _ui.Modal.Close();
            PitchView.Run(_ui.BigModal, _sim, mail, null);
        }
    }
}
