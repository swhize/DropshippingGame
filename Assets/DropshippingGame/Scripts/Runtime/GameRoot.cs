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
    /// Eingabesperren und Speichern.
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
            s.DayStarted += day => _ui.Hud.ShowBanner("Tag " + day, DayTagline());
            s.LevelUp += level => _ui.Hud.ShowLevelUp(level);
            s.GoalCompleted += g => _ui.Hud.ShowBanner("Ziel erreicht", g.Title + " · +" + Fmt.Money(g.Reward));
            s.StoryChanged += _ => _ui.Hud.Refresh();
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
        }

        private string DayTagline()
        {
            string[] lines =
            {
                "Neuer Tag, neue Pakete.", "Kaffee an, Laptop auf.", "Die Kunden warten schon.", "Heute wird ein guter Tag.",
                "Dein Imperium wächst.",
            };
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
                _sim.Notify("Willkommen zurück bei " + _sim.BrandName + ". Tag " + _sim.Day + ".", "info");
            else _ui.Hud.ShowBanner("Tag 1", "Deine Garage. Dein Business.");
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
            bool pause = GameInput.PauseDown;
            bool cancel = GameInput.CancelDown;

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
                if (cancel || GameInput.LaptopDown) _sim.ClosePc();
                return;
            }
            if (_ui.BigModal.IsOpen || _ui.Modal.IsOpen)
            {
                var m = _ui.BigModal.IsOpen ? _ui.BigModal : _ui.Modal;
                if (cancel && (m.Current == "help" || m.Current == "credits" || m.Current == "confirm" || m.Current == "event_result")) m.Close();
                return;
            }
            if (_ui.Dialogue.Active) return;
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
            if (GameInput.LaptopDown)
            {
                if (_sim.StoryStage == "business") _sim.OpenPc();
                else _sim.Notify("Dein Handy hat gerade keinen Empfang. Kalle guckt.", "info");
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
            _sim.SaveGame(true);
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
            }, true, "confirm");
            _ui.Modal.Text(body, "Offene Bestellungen bleiben bis morgen liegen. Miete, Löhne und Zinsen werden abgerechnet.", "muted");
        }

        private void ShowSummary(DaySummary s)
        {
            if (_sim.PcOpen) _sim.ClosePc();
            _ui.Modal.Close();
            Game.Sound("notify");
            var body = _ui.BigModal.Open("Feierabend – Tag " + s.Day, "Zeit für die Abrechnung.", 560, new[]
            {
                new ModalButton("Nächster Tag", NextDay, "accent", "arrow_right"),
            }, true, "summary");
            var m = _ui.BigModal;
            var top = UIX.Row(body, 10f);
            UIX.Stat(top, "PAKETE", s.Shipped.ToString());
            UIX.Stat(top, "VERLOREN", s.Lost.ToString(), s.Lost > 0 ? Theme.Bad : (Color?)null);
            UIX.Stat(top, "ERFAHRUNG", "+" + s.XpGained + " XP", Theme.Accent);
            UIX.Separator(body);
            m.KV(body, "Umsatz", Fmt.Money(s.Revenue), Theme.Good);
            if (s.Stand > 0) m.KV(body, "  davon Verkaufsstand", Fmt.Money(s.Stand), Theme.Good);
            if (s.IncomeOther != 0) m.KV(body, "Sonstige Einnahmen", Fmt.Money(s.IncomeOther), Theme.Good);
            void Cost(string label, int v)
            {
                if (v != 0) m.KV(body, label, Fmt.Money(-v), Theme.Bad);
            }
            Cost("Wareneinkauf", s.Purchases);
            Cost("Verpackung", s.Packaging);
            Cost("Marketing", s.Marketing);
            Cost("Sonstiges", s.Other);
            Cost("Miete", s.Rent);
            Cost("Löhne", s.Wages);
            Cost("Strom Förderband", s.Upkeep);
            Cost("Kreditzinsen", s.Interest);
            UIX.Separator(body);
            m.KV(body, "Tagesergebnis (Cashflow)", Fmt.SignedMoney(s.Profit), s.Profit >= 0 ? Theme.Good : Theme.Bad);
            if (s.Trading != 0) m.KV(body, "Trading (Käufe/Verkäufe)", Fmt.SignedMoney(s.Trading));
            m.KV(body, "Kontostand danach", Fmt.Money(s.MoneyAfter), s.MoneyAfter < 0 ? Theme.Bad : (Color?)null);
            if (s.Debt > 0) m.KV(body, "Offener Kredit", Fmt.Money(s.Debt), Theme.Bad);
            float dr = s.RepEnd - s.RepStart;
            m.KV(body, "Bewertung", Fmt.Rating(s.RepEnd) + " ★  (" + (dr >= 0 ? "+" : "−") + Fmt.Dec(Mathf.Abs(dr), 2) + ")", Theme.Accent);
            if (s.Bankrupt)
                UIX.Chip(body, "Dein Konto fällt unter " + Fmt.Money(GameData.BankruptLimit) + " – das ist die Insolvenz!", "warning", Theme.Bad);
            else if (s.MoneyAfter < 0)
                UIX.Chip(body, "Du bist im Minus. Ab " + Fmt.Money(GameData.BankruptLimit) + " ist Schluss – verkauf mehr, spar Kosten oder nimm einen Kredit.", "warning", Theme.Accent);
        }

        private void NextDay() => _ui.Fader.Transition("Tag " + (_sim.Day + 1), DoNextDay, 0.9f);

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
            }, true, "gameover");
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
            }, true, "ending");
            _ui.BigModal.Text(body, "Vor " + _sim.Day + " Tagen hast du noch Teller bei Kalle getragen. Heute gehört dir eine Marke mit " +
                                    Fmt.Rating(_sim.Reputation) + " Sternen, " + Fmt.Thousands(_sim.TotalShipped) + " verschickten Paketen und einem Team, das für dich arbeitet.");
            _ui.BigModal.Text(body, "Das Spiel ist hier nicht zu Ende – bau dein Imperium weiter aus, so lange du willst.", "muted");
        }

        private void ShowCredits()
        {
            var body = _ui.Modal.Open("Credits", "Dropshipping Simulator – Vom Imbiss zum Imperium", 540, new[]
            {
                new ModalButton("Schließen", null, "accent"),
            }, true, "credits");
            _ui.Modal.Text(body, "Idee & Game Design: du\nCode, 3D-Welt, Shader, Texturen, Sounds & Musik: Claude\nEngine: Unity 6 (URP, UI Toolkit)\n\n" +
                                 "Kein einziges Asset wurde importiert – jedes Modell, jede Textur, jedes Symbol und jeder Ton entsteht beim Start aus Code.");
        }

        private void ShowHelp()
        {
            _ui.Pause.Close();
            string k(string a) => "<color=#FFBD42><b>" + GameInput.KeyLabel(a) + "</b></color>";
            var body = _ui.Modal.Open("Steuerung & Spielablauf", null, 700, new[]
            {
                new ModalButton("Verstanden", null, "accent", "check"),
            }, true, "help");
            _ui.Modal.Text(body, "<b>Steuerung</b>", "h3");
            _ui.Modal.Text(body,
                "WASD / linker Stick laufen · Shift sprinten · Strg ducken · Leertaste springen\n" +
                "Maus / rechter Stick umsehen · " + k("interact") + " benutzen · " + k("drop") + " ablegen\n" +
                k("laptop") + " Laptop · " + k("pause") + " Pause · " + k("help") + " diese Hilfe · F12 Screenshot\n" +
                "Controller wird automatisch erkannt (Xbox/PlayStation).");
            _ui.Modal.Text(body, "<b>Der Kreislauf</b>", "h3");
            _ui.Modal.Text(body,
                "1. Im Laptop unter <b>Einkauf</b> Ware bestellen\n" +
                "2. Kiste am <b>Wareneingang</b> holen und ins passende <b>Regal</b> räumen\n" +
                "3. Produkt im <b>Webshop</b> online stellen – Bestellungen erscheinen oben links\n" +
                "4. Artikel aus dem Regal nehmen und am <b>Packtisch</b> verpacken\n" +
                "5. Am <b>Labeldrucker</b> ein Versandlabel drucken\n" +
                "6. Paket zur <b>Versand-Box</b> bringen – Geld kassieren");
            _ui.Modal.Text(body, "<b>Wachsen</b>", "h3");
            _ui.Modal.Text(body,
                "Gute Preise, schnelle Lieferung und Qualität bringen gute Bewertungen und mehr Kunden. Marketing erhöht die Reichweite, " +
                "der Verkaufsstand bringt Laufkundschaft, die Bank hilft über Engpässe. Mit Erfahrung steigt dein Firmenlevel und schaltet " +
                "Produkte, Lagerhalle, Personal und mehr frei. Um 20 Uhr ist Feierabend – dann werden Miete, Löhne und Zinsen fällig.", "muted");
        }

        // =====================================================================================
        // Ereignisse & Minispiel
        // =====================================================================================
        private void ShowEventChoice(Mail mail)
        {
            if (mail == null || !mail.Pending) return;
            if (_ui.Modal.IsOpen || _ui.BigModal.IsOpen || _ui.Dialogue.Active || _ui.Pause.IsOpen || _inMenu) return;
            var buttons = new List<ModalButton>();
            for (int i = 0; i < mail.Choices.Count; i++)
            {
                var c = mail.Choices[i];
                int ci = i;
                string txt = c.Label + (c.Cost > 0 ? "  ·  " + Fmt.Money(c.Cost) : "");
                buttons.Add(new ModalButton(txt, () => Decide(mail.Id, ci), i == 0 ? "accent" : "", string.IsNullOrEmpty(c.Minigame) ? null : "gamepad"));
            }
            buttons.Add(new ModalButton("Später (Postfach)", null, "ghost"));
            var body = _ui.Modal.Open(mail.Title, "Von: " + mail.Sender, 660, buttons, true, "event");
            _ui.Modal.Text(body, mail.Text);
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
            var body = _ui.Modal.Open("Ergebnis", mail != null ? mail.Title : null, 540, new[] { new ModalButton("OK", null, "accent") }, true, "event_result");
            _ui.Modal.Text(body, res);
        }

        private void OnMinigame(Mail mail, string kind)
        {
            if (kind != "pitch") return;
            if (_sim.PcOpen) _sim.ClosePc();
            _ui.Modal.Close();
            PitchView.Run(_ui.BigModal, _sim, mail, null);
        }
    }
}
