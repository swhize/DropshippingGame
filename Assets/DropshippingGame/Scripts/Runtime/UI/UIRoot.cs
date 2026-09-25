using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DropshippingGame.UI
{
    /// <summary>
    /// Erstellt die komplette Oberfläche (ein UIDocument mit Ebenen) zur Laufzeit.
    /// Ebenen von unten nach oben: HUD, Handy, Laptop, Level-Up-Feier, Dialog, Fenster,
    /// großes Fenster, Pausemenü, Hauptmenü, Toasts, Schwarzblende.
    /// Kümmert sich außerdem um Schriften, UI-Größe und den Controller-Fokus.
    /// </summary>
    public sealed class UIRoot : MonoBehaviour
    {
        public VisualElement Root;
        public HudView Hud;
        public PhoneView Phone;
        public LaptopView Laptop;
        public DialogueView Dialogue;
        public ModalView Modal;
        public ModalView BigModal;
        public PauseView Pause;
        public MainMenuView Menu;
        public FaderView Fader;

        private UIDocument _doc;
        private PanelSettings _panelSettings;
        private float _focusT;

        public static UIRoot Create(Transform parent)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var ui = go.AddComponent<UIRoot>();
            ui._doc = go.AddComponent<UIDocument>();
            ui._panelSettings = CreatePanelSettings();
            ui._doc.panelSettings = ui._panelSettings;
            go.SetActive(true);
            ui.Build();
            EnsureEventSystem(parent);
            Settings.Changed += ui.ApplyScale;
            ui.ApplyScale();
            return ui;
        }

        private static PanelSettings CreatePanelSettings()
        {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = "DS Panel";
            var theme = Resources.Load<ThemeStyleSheet>("UI/DefaultTheme");
            if (theme != null) ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 10;
            return ps;
        }

        /// <summary>UI-Größe aus den Einstellungen (0,8–1,3) auf das Panel anwenden.</summary>
        private void ApplyScale()
        {
            if (_panelSettings == null) return;
            float s = Mathf.Clamp(Settings.UiScale, Settings.UiScaleMin, Settings.UiScaleMax);
            if (Mathf.Abs(_panelSettings.scale - s) > 0.001f) _panelSettings.scale = s;
        }

        private void OnDestroy()
        {
            Settings.Changed -= ApplyScale;
        }

        /// <summary>UI Toolkit bekommt Maus/Tastatur/Controller über ein EventSystem (neues oder altes Input-System).</summary>
        private static void EnsureEventSystem(Transform parent)
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.transform.SetParent(parent, false);
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private void Build()
        {
            var docRoot = _doc.rootVisualElement;
            var sheet = Resources.Load<StyleSheet>("UI/Game");
            if (sheet != null) docRoot.styleSheets.Add(sheet);
            else Debug.LogWarning("UI-Stylesheet Resources/UI/Game.uss nicht gefunden.");
            Root = new VisualElement { name = "ds-root", pickingMode = PickingMode.Ignore };
            Root.AddToClassList("ds-root");
            docRoot.Add(Root);
            Fonts.ApplyRoot(Root);

            var hud = Layer("hud");
            var phone = Layer("phone");
            var laptop = Layer("laptop");
            var celebrate = Layer("celebrate");
            var dialogue = Layer("dialogue");
            var modal = Layer("modal");
            var bigmodal = Layer("bigmodal");
            var pause = Layer("pause");
            var menu = Layer("menu");
            var toast = Layer("toast");
            var fader = Layer("fader");

            Hud = new HudView(hud, toast, celebrate);
            Phone = new PhoneView(phone);
            Laptop = new LaptopView(laptop);
            Dialogue = new DialogueView(dialogue);
            Modal = new ModalView(modal, "modal");
            BigModal = new ModalView(bigmodal, "bigmodal");
            Pause = new PauseView(pause);
            Menu = new MainMenuView(menu);
            Fader = new FaderView(fader);
        }

        private VisualElement Layer(string name)
        {
            var l = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            l.AddToClassList("layer");
            Root.Add(l);
            return l;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            try { GameInput.PollDevice(); } catch (Exception e) { Debug.LogException(e); }
            // Jede Ebene einzeln absichern: ein Fehler in einer Ansicht darf die anderen nicht stoppen.
            try { Hud?.Tick(dt); } catch (Exception e) { Debug.LogException(e); }
            try { Phone?.Tick(dt); } catch (Exception e) { Debug.LogException(e); }
            try { Laptop?.Tick(dt); } catch (Exception e) { Debug.LogException(e); }
            try { Dialogue?.Tick(dt); } catch (Exception e) { Debug.LogException(e); }
            try { Menu?.Tick(dt); } catch (Exception e) { Debug.LogException(e); }
            try { UpdateFocus(); } catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Oberstes Fenster, das gerade bedient wird (für den Controller-Fokus), sonst null.</summary>
        public VisualElement TopInteractive()
        {
            if (Fader != null && Fader.Busy) return null;
            if (Menu != null && Menu.IsOpen) return Menu.Content;
            if (Pause != null && Pause.IsOpen) return Pause.Card;
            if (BigModal != null && BigModal.IsOpen) return BigModal.Card;
            if (Modal != null && Modal.IsOpen) return Modal.Card;
            if (Dialogue != null && Dialogue.ChoicesShown) return Dialogue.Panel;
            if (Laptop != null && Laptop.IsOpen) return Laptop.Booting ? null : Laptop.Screen;
            if (Phone != null && Phone.IsOpen) return Phone.Screen;
            return null;
        }

        /// <summary>
        /// Controller: hält den Fokus im obersten Fenster (erstes Element, falls nichts fokussiert ist).
        /// Ohne offenes Fenster wird ein verirrter Fokus gelöst, damit Enter/Leertaste im Spiel
        /// keine versteckten Buttons auslösen.
        /// </summary>
        private void UpdateFocus()
        {
            var fc = Root != null && Root.panel != null ? Root.panel.focusController : null;
            if (fc == null) return;
            var cur = fc.focusedElement as VisualElement;
            var top = TopInteractive();
            if (top == null)
            {
                bool anyOpen = (Laptop != null && Laptop.IsOpen) || (Dialogue != null && Dialogue.Active) || (Fader != null && Fader.Busy);
                if (cur != null && !anyOpen) cur.Blur();
                return;
            }
            if (!GameInput.UsingGamepad) return;
            if (cur != null && UIX.IsInside(top, cur)) return;
            _focusT += Time.unscaledDeltaTime;
            if (_focusT < 0.08f) return;
            _focusT = 0f;
            UIX.FocusFirst(top);
        }

        /// <summary>Liegt gerade ein Fenster über dem Spiel, das Eingaben braucht?</summary>
        public bool AnyWindowOpen => (Modal != null && Modal.IsOpen) || (BigModal != null && BigModal.IsOpen) ||
                                     (Dialogue != null && Dialogue.Active) || (Pause != null && Pause.IsOpen) ||
                                     (Phone != null && Phone.IsOpen);
    }
}
