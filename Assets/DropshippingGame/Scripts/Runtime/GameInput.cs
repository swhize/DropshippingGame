using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DropshippingGame
{
    /// <summary>
    /// Einheitlicher Zugriff auf Tastatur, Maus und Controller. Funktioniert mit dem neuen
    /// Input System (bevorzugt) und mit dem alten Input Manager - je nachdem, was im Projekt aktiv ist.
    ///
    /// Belegung Tastatur: WASD laufen · Maus umsehen · E interagieren · G ablegen · Leertaste springen
    /// · Shift sprinten · Strg/C ducken · Tab Handy · Esc Pause/Zurück · F1 Hilfe · F12 Screenshot
    /// · 1–4 Handy-App wählen
    /// Controller: linker Stick laufen · rechter Stick umsehen · X/Quadrat interagieren · B/Kreis ablegen
    /// bzw. zurück · A/Kreuz springen · L3 sprinten · R3 ducken · Y/Dreieck Handy · LB/RB App wechseln
    /// · Start Pause · Select Hilfe
    /// Den vollen Laptop „HustleOS“ gibt es nur am Schreibtisch (Station benutzen).
    /// </summary>
    public static class GameInput
    {
        public const float MouseDegreesPerPixel = 0.15f;
        public const float PadDegreesPerSecond = 170f;

        /// <summary>true, wenn zuletzt ein Controller benutzt wurde (für die Tastenhinweise).</summary>
        public static bool UsingGamepad { get; private set; }

        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                }
                var pad = Gamepad.current;
                if (pad != null)
                {
                    Vector2 s = pad.leftStick.ReadValue();
                    if (s.sqrMagnitude > 0.04f)
                    {
                        v += s;
                        UsingGamepad = true;
                    }
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
#endif
                if (v.sqrMagnitude > 0.001f && !PadActiveThisFrame()) UsingGamepad = false;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>Blickänderung in Grad für diesen Frame (x = Gieren, y = Nicken, positiv = nach oben).</summary>
        public static Vector2 Look
        {
            get
            {
                Vector2 d = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    Vector2 md = mouse.delta.ReadValue();
                    if (md.sqrMagnitude > 0.01f) UsingGamepad = false;
                    d += md * (MouseDegreesPerPixel * Settings.MouseSensitivity);
                }
                var pad = Gamepad.current;
                if (pad != null)
                {
                    Vector2 s = pad.rightStick.ReadValue();
                    if (s.sqrMagnitude > 0.02f)
                    {
                        UsingGamepad = true;
                        // Leichte Kurve für feines Zielen
                        s = s.normalized * Mathf.Pow(s.magnitude, 1.6f);
                        d += s * (PadDegreesPerSecond * Settings.PadSensitivity * Time.unscaledDeltaTime);
                    }
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                Vector2 lm = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
                d += lm * (MouseDegreesPerPixel * Settings.MouseSensitivity);
#endif
                if (Settings.InvertY) d.y = -d.y;
                return d;
            }
        }

        /// <summary>
        /// Einmal pro Frame (aus der Oberfläche) aufrufen: erkennt das zuletzt benutzte Gerät auch dann,
        /// wenn gerade niemand Laufen/Umsehen abfragt (Menüs, Handy, Laptop).
        /// </summary>
        public static void PollDevice()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.ReadValue().sqrMagnitude > 0.25f || pad.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                    pad.rightStick.ReadValue().sqrMagnitude > 0.25f || pad.buttonSouth.wasPressedThisFrame ||
                    pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                    pad.startButton.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)
                    UsingGamepad = true;
            }
            var mouse = Mouse.current;
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 9f || mouse.leftButton.wasPressedThisFrame)) UsingGamepad = false;
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) UsingGamepad = false;
#endif
        }

        /// <summary>Rechter Stick (vertikal) zum Scrollen in Handy, Laptop und Menüs. Positiv = nach unten.</summary>
        public static float UiScroll
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var pad = Gamepad.current;
                if (pad == null) return 0f;
                float y = pad.rightStick.ReadValue().y;
                if (Mathf.Abs(y) < 0.2f) return 0f;
                UsingGamepad = true;
                return -y;
#else
                return 0f;
#endif
            }
        }

        public static bool InteractDown => Key(KeyId.E) || PadDown(PadButton.West);
        public static bool DropDown => Key(KeyId.G) || PadDown(PadButton.East);
        public static bool JumpDown => Key(KeyId.Space) || PadDown(PadButton.South);
        public static bool SprintHeld => Held(KeyId.Shift) || PadHeld(PadButton.LeftStick);
        public static bool CrouchHeld => Held(KeyId.Ctrl) || Held(KeyId.C) || PadHeld(PadButton.RightStick);
        /// <summary>Handy öffnen/schließen (Tab / Y). Schließt auch den Laptop.</summary>
        public static bool PhoneDown => Key(KeyId.Tab) || PadDown(PadButton.North);
        /// <summary>Alter Name, bleibt für Kompatibilität erhalten (= <see cref="PhoneDown"/>).</summary>
        public static bool LaptopDown => PhoneDown;
        public static bool PauseDown => Key(KeyId.Escape) || PadDown(PadButton.Start);
        public static bool CancelDown => Key(KeyId.Escape) || PadDown(PadButton.East) || PadDown(PadButton.Start);
        public static bool HelpDown => Key(KeyId.F1) || PadDown(PadButton.Select);
        public static bool ScreenshotDown => Key(KeyId.F12);
        /// <summary>F10, F9 oder ^ (links neben der 1): Admin-Panel (Testmodus).</summary>
        public static bool AdminDown => Key(KeyId.F10) || Key(KeyId.F9) || Key(KeyId.Backquote);
        /// <summary>Vorherige App / vorheriger Reiter (LB).</summary>
        public static bool PrevTabDown => PadDown(PadButton.LeftShoulder);
        /// <summary>Nächste App / nächster Reiter (RB).</summary>
        public static bool NextTabDown => PadDown(PadButton.RightShoulder);

        /// <summary>Zifferntaste 1–4 gedrückt (für die Handy-Apps), sonst 0.</summary>
        public static int NumberDown
        {
            get
            {
                if (Key(KeyId.D1)) return 1;
                if (Key(KeyId.D2)) return 2;
                if (Key(KeyId.D3)) return 3;
                if (Key(KeyId.D4)) return 4;
                return 0;
            }
        }

        /// <summary>Weiter im Dialog: E, Leertaste, Enter, linke Maustaste oder A/X am Controller.</summary>
        public static bool AdvanceDown =>
            Key(KeyId.E) || Key(KeyId.Space) || Key(KeyId.Enter) || MouseLeftDown || PadDown(PadButton.South) || PadDown(PadButton.West);

        public static bool MouseLeftDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetMouseButtonDown(0);
#else
                return false;
#endif
            }
        }

        /// <summary>Beschriftung der Taste für eine Aktion – passend zum zuletzt benutzten Gerät.</summary>
        public static string KeyLabel(string action) => UsingGamepad ? PadLabel(action) : KeyboardLabel(action);

        public static string KeyboardLabel(string action)
        {
            switch (action)
            {
                case "interact": return "E";
                case "drop": return "G";
                case "phone":
                case "laptop": return "Tab";
                case "pause": return "Esc";
                case "back": return "Esc";
                case "help": return "F1";
                case "jump": return "Leertaste";
                case "sprint": return "Shift";
                case "crouch": return "Strg";
                case "tabs": return "1–4";
                case "screenshot": return "F12";
                case "confirm": return "Enter";
            }
            return action;
        }

        public static string PadLabel(string action)
        {
            switch (action)
            {
                case "interact": return "X";
                case "drop": return "B";
                case "phone":
                case "laptop": return "Y";
                case "pause": return "Start";
                case "back": return "B";
                case "help": return "Select";
                case "jump": return "A";
                case "sprint": return "L3";
                case "crouch": return "R3";
                case "tabs": return "LB/RB";
                case "screenshot": return "–";
                case "confirm": return "A";
            }
            return action;
        }

        /// <summary>Tabelle für den Hilfe-Bildschirm: Aktion, Tastatur &amp; Maus, Controller.</summary>
        public static readonly string[][] HelpTable =
        {
            new[] { "Laufen", "W A S D", "Linker Stick" },
            new[] { "Umsehen", "Maus", "Rechter Stick" },
            new[] { "Sprinten", "Shift", "L3" },
            new[] { "Ducken", "Strg / C", "R3" },
            new[] { "Springen", "Leertaste", "A" },
            new[] { "Benutzen / Aufheben", "E", "X" },
            new[] { "Ablegen", "G", "B" },
            new[] { "Handy öffnen / schließen", "Tab", "Y" },
            new[] { "Handy-App wechseln", "1 2 3 4", "LB / RB" },
            new[] { "Laptop (am Schreibtisch)", "E", "X" },
            new[] { "Laptop-App wechseln", "Maus", "LB / RB" },
            new[] { "Auswählen", "Linksklick / Enter", "A" },
            new[] { "Zurück / Schließen", "Esc", "B" },
            new[] { "Scrollen", "Mausrad", "Rechter Stick" },
            new[] { "Pause-Menü", "Esc", "Start" },
            new[] { "Hilfe", "F1", "Select" },
            new[] { "Screenshot", "F12", "–" },
        };

        // ---- Intern -----------------------------------------------------------------------------------
        private enum KeyId
        {
            E, G, Space, Shift, Ctrl, C, Tab, Escape, F1, F9, F10, Backquote, F12, Enter, D1, D2, D3, D4,
        }

        private enum PadButton
        {
            South, East, West, North, Start, Select, LeftStick, RightStick, LeftShoulder, RightShoulder,
        }

        private static bool Key(KeyId k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            bool down;
            switch (k)
            {
                case KeyId.E: down = kb.eKey.wasPressedThisFrame; break;
                case KeyId.G: down = kb.gKey.wasPressedThisFrame; break;
                case KeyId.Space: down = kb.spaceKey.wasPressedThisFrame; break;
                case KeyId.Shift: down = kb.leftShiftKey.wasPressedThisFrame; break;
                case KeyId.Ctrl: down = kb.leftCtrlKey.wasPressedThisFrame; break;
                case KeyId.C: down = kb.cKey.wasPressedThisFrame; break;
                case KeyId.Tab: down = kb.tabKey.wasPressedThisFrame; break;
                case KeyId.Escape: down = kb.escapeKey.wasPressedThisFrame; break;
                case KeyId.F1: down = kb.f1Key.wasPressedThisFrame; break;
                case KeyId.F9: down = kb.f9Key.wasPressedThisFrame; break;
                case KeyId.F10: down = kb.f10Key.wasPressedThisFrame; break;
                case KeyId.Backquote: down = kb.backquoteKey.wasPressedThisFrame; break;
                case KeyId.F12: down = kb.f12Key.wasPressedThisFrame; break;
                case KeyId.Enter: down = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame; break;
                case KeyId.D1: down = kb.digit1Key.wasPressedThisFrame; break;
                case KeyId.D2: down = kb.digit2Key.wasPressedThisFrame; break;
                case KeyId.D3: down = kb.digit3Key.wasPressedThisFrame; break;
                case KeyId.D4: down = kb.digit4Key.wasPressedThisFrame; break;
                default: down = false; break;
            }
            if (down) UsingGamepad = false;
            return down;
#elif ENABLE_LEGACY_INPUT_MANAGER
            bool down = Input.GetKeyDown(ToKeyCode(k));
            if (k == KeyId.Enter && !down) down = Input.GetKeyDown(KeyCode.KeypadEnter);
            if (down) UsingGamepad = false;
            return down;
#else
            return false;
#endif
        }

        private static bool Held(KeyId k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case KeyId.Shift: return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                case KeyId.Ctrl: return kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                case KeyId.C: return kb.cKey.isPressed;
                default: return false;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (k)
            {
                case KeyId.Shift: return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                case KeyId.Ctrl: return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                case KeyId.C: return Input.GetKey(KeyCode.C);
                default: return false;
            }
#else
            return false;
#endif
        }

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        private static KeyCode ToKeyCode(KeyId k)
        {
            switch (k)
            {
                case KeyId.E: return KeyCode.E;
                case KeyId.G: return KeyCode.G;
                case KeyId.Space: return KeyCode.Space;
                case KeyId.Shift: return KeyCode.LeftShift;
                case KeyId.Ctrl: return KeyCode.LeftControl;
                case KeyId.C: return KeyCode.C;
                case KeyId.Tab: return KeyCode.Tab;
                case KeyId.Escape: return KeyCode.Escape;
                case KeyId.F1: return KeyCode.F1;
                case KeyId.F9: return KeyCode.F9;
                case KeyId.F10: return KeyCode.F10;
                case KeyId.Backquote: return KeyCode.BackQuote;
                case KeyId.F12: return KeyCode.F12;
                case KeyId.Enter: return KeyCode.Return;
                case KeyId.D1: return KeyCode.Alpha1;
                case KeyId.D2: return KeyCode.Alpha2;
                case KeyId.D3: return KeyCode.Alpha3;
                case KeyId.D4: return KeyCode.Alpha4;
            }
            return KeyCode.None;
        }
#endif

        private static bool PadActiveThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            return pad != null && pad.leftStick.ReadValue().sqrMagnitude > 0.04f;
#else
            return false;
#endif
        }

        private static bool PadDown(PadButton b)
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad == null) return false;
            bool down;
            switch (b)
            {
                case PadButton.South: down = pad.buttonSouth.wasPressedThisFrame; break;
                case PadButton.East: down = pad.buttonEast.wasPressedThisFrame; break;
                case PadButton.West: down = pad.buttonWest.wasPressedThisFrame; break;
                case PadButton.North: down = pad.buttonNorth.wasPressedThisFrame; break;
                case PadButton.Start: down = pad.startButton.wasPressedThisFrame; break;
                case PadButton.Select: down = pad.selectButton.wasPressedThisFrame; break;
                case PadButton.LeftStick: down = pad.leftStickButton.wasPressedThisFrame; break;
                case PadButton.RightStick: down = pad.rightStickButton.wasPressedThisFrame; break;
                case PadButton.LeftShoulder: down = pad.leftShoulder.wasPressedThisFrame; break;
                case PadButton.RightShoulder: down = pad.rightShoulder.wasPressedThisFrame; break;
                default: down = false; break;
            }
            if (down) UsingGamepad = true;
            return down;
#else
            return false;
#endif
        }

        private static bool PadHeld(PadButton b)
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad == null) return false;
            switch (b)
            {
                case PadButton.LeftStick: return pad.leftStickButton.isPressed;
                case PadButton.RightStick: return pad.rightStickButton.isPressed;
                default: return false;
            }
#else
            return false;
#endif
        }
    }
}
