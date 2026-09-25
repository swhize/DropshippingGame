using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace DropshippingGame.UI
{
    /// <summary>
    /// Erstellt die komplette Oberfläche (ein UIDocument mit Ebenen) zur Laufzeit:
    /// HUD, Laptop, Dialog, Fenster, Pausemenü, Hauptmenü und Schwarzblende.
    /// </summary>
    public sealed class UIRoot : MonoBehaviour
    {
        public VisualElement Root;
        public HudView Hud;
        public LaptopView Laptop;
        public DialogueView Dialogue;
        public ModalView Modal;
        public ModalView BigModal;
        public PauseView Pause;
        public MainMenuView Menu;
        public FaderView Fader;

        private UIDocument _doc;

        public static UIRoot Create(Transform parent)
        {
            var go = new GameObject("UI");
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            var ui = go.AddComponent<UIRoot>();
            ui._doc = go.AddComponent<UIDocument>();
            ui._doc.panelSettings = CreatePanelSettings();
            go.SetActive(true);
            ui.Build();
            EnsureEventSystem(parent);
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

        /// <summary>UI Toolkit bekommt Maus/Tastatur/Controller über ein EventSystem (neues oder altes Input-System).</summary>
        private static void EnsureEventSystem(Transform parent)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
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

            Hud = new HudView(Layer("hud"));
            Laptop = new LaptopView(Layer("laptop"));
            Dialogue = new DialogueView(Layer("dialogue"));
            Modal = new ModalView(Layer("modal"), "modal");
            BigModal = new ModalView(Layer("bigmodal"), "bigmodal");
            Pause = new PauseView(Layer("pause"));
            Menu = new MainMenuView(Layer("menu"));
            Fader = new FaderView(Layer("fader"));
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
            Hud?.Tick(dt);
            Laptop?.Tick(dt);
            Dialogue?.Tick(dt);
            Menu?.Tick(dt);
        }

        /// <summary>Liegt gerade ein Fenster über dem Spiel, das Eingaben braucht?</summary>
        public bool AnyWindowOpen => (Modal != null && Modal.IsOpen) || (BigModal != null && BigModal.IsOpen) ||
                                     (Dialogue != null && Dialogue.Active) || (Pause != null && Pause.IsOpen);
    }
}
