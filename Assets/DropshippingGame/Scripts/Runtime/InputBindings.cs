using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace DropshippingGame
{
    /// <summary>
    /// Frei belegbare Tasten für die Hauptaktionen (Tastatur und Controller), gespeichert in den
    /// PlayerPrefs ("ds.bind.kb.*", "ds.bind.pad.*"). <see cref="GameInput"/> fragt die Aktionen hier ab.
    /// Neu belegen: <see cref="BeginCapture"/> → nächste gedrückte Taste/Taste am Controller wird
    /// übernommen (Esc bzw. Start bricht ab). Ist die Taste schon vergeben, tauschen die Aktionen.
    /// Bewegung (WASD/Pfeile, Sticks), Esc/Start und die Menü-Navigation bleiben fest.
    /// </summary>
    public static class InputBindings
    {
        public sealed class ActionDef
        {
            public string Id, Name, Key, Pad;
        }

        /// <summary>Belegbare Aktionen mit Standardbelegung (Namen aus UnityEngine.InputSystem.Key / GamepadButton).</summary>
        public static readonly ActionDef[] Actions =
        {
            new ActionDef { Id = "interact", Name = "Benutzen / Aufheben", Key = "E", Pad = "West" },
            new ActionDef { Id = "drop", Name = "Ablegen", Key = "G", Pad = "East" },
            new ActionDef { Id = "jump", Name = "Springen", Key = "Space", Pad = "South" },
            new ActionDef { Id = "sprint", Name = "Sprinten (halten)", Key = "LeftShift", Pad = "LeftStick" },
            new ActionDef { Id = "crouch", Name = "Ducken (halten)", Key = "LeftCtrl", Pad = "RightStick" },
            new ActionDef { Id = "phone", Name = "Handy öffnen / schließen", Key = "Tab", Pad = "North" },
            new ActionDef { Id = "build", Name = "Möbel verschieben", Key = "B", Pad = "DpadDown" },
            new ActionDef { Id = "emote", Name = "Emote-Rad (halten)", Key = "T", Pad = "DpadLeft" },
            new ActionDef { Id = "help", Name = "Hilfe", Key = "F1", Pad = "Select" },
        };

        private static readonly Dictionary<string, string> _kb = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _pad = new Dictionary<string, string>();
        private static bool _loaded;

        // ---- Neu belegen ---------------------------------------------------------------------------
        private static string _captureAction;
        private static bool _capturePad;
        private static int _captureStartFrame, _captureEndFrame = -10;
        private static Action<bool> _captureDone;

        /// <summary>Wartet gerade auf eine neue Taste? (Dann liefert <see cref="GameInput"/> keine Aktionen.)</summary>
        public static bool Capturing => _captureAction != null || Time.frameCount <= _captureEndFrame + 1;
        public static string CaptureAction => _captureAction;
        public static bool CapturePad => _capturePad;

        /// <summary>Belegung wurde geändert (Tastenhinweise neu zeichnen).</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            _kb.Clear();
            _pad.Clear();
            _captureAction = null;
            _captureDone = null;
            _captureEndFrame = -10;
            Changed = null;
        }

        public static ActionDef Def(string id)
        {
            foreach (var a in Actions)
                if (a.Id == id) return a;
            return null;
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            foreach (var a in Actions)
            {
                string k = a.Key, p = a.Pad;
                try
                {
                    k = PlayerPrefs.GetString("ds.bind.kb." + a.Id, a.Key);
                    p = PlayerPrefs.GetString("ds.bind.pad." + a.Id, a.Pad);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                _kb[a.Id] = ValidKey(k) ? k : a.Key;
                _pad[a.Id] = ValidPad(p) ? p : a.Pad;
            }
        }

        private static void Save()
        {
            try
            {
                foreach (var a in Actions)
                {
                    PlayerPrefs.SetString("ds.bind.kb." + a.Id, KeyName(a.Id));
                    PlayerPrefs.SetString("ds.bind.pad." + a.Id, PadName(a.Id));
                }
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void ResetAll()
        {
            Load();
            foreach (var a in Actions)
            {
                _kb[a.Id] = a.Key;
                _pad[a.Id] = a.Pad;
            }
            Save();
        }

        public static string KeyName(string id)
        {
            Load();
            if (_kb.TryGetValue(id ?? "", out string k)) return k;
            var d = Def(id);
            return d != null ? d.Key : "";
        }

        public static string PadName(string id)
        {
            Load();
            if (_pad.TryGetValue(id ?? "", out string p)) return p;
            var d = Def(id);
            return d != null ? d.Pad : "";
        }

        public static bool IsDefault(string id)
        {
            var d = Def(id);
            return d == null || (KeyName(id) == d.Key && PadName(id) == d.Pad);
        }

        // =====================================================================================
        // Abfragen
        // =====================================================================================
        /// <summary>Aktion in diesem Frame gedrückt (Tastatur oder Controller).</summary>
        public static bool Down(string id) => KeyDown(id) || PadDown(id);

        /// <summary>Aktion gehalten (Tastatur oder Controller).</summary>
        public static bool Held(string id)
        {
            if (Capturing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = KeyControl(KeyName(id));
            if (k != null && k.isPressed) return true;
            var b = PadControl(PadName(id));
            return b != null && b.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return LegacyKey(KeyName(id), false);
#else
            return false;
#endif
        }

        public static bool KeyDown(string id)
        {
            if (Capturing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = KeyControl(KeyName(id));
            return k != null && k.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return LegacyKey(KeyName(id), true);
#else
            return false;
#endif
        }

        public static bool PadDown(string id)
        {
            if (Capturing) return false;
#if ENABLE_INPUT_SYSTEM
            var b = PadControl(PadName(id));
            return b != null && b.wasPressedThisFrame;
#else
            return false;
#endif
        }

        // =====================================================================================
        // Beschriftung
        // =====================================================================================
        public static string KeyLabel(string id) => KeyDisplay(KeyName(id));
        public static string PadLabel(string id) => PadDisplay(PadName(id));

        public static string KeyDisplay(string key)
        {
            switch (key)
            {
                case "LeftShift":
                case "RightShift": return "Shift";
                case "LeftCtrl":
                case "RightCtrl": return "Strg";
                case "LeftAlt": return "Alt";
                case "RightAlt": return "AltGr";
                case "Space": return "Leertaste";
                case "Escape": return "Esc";
                case "Enter":
                case "NumpadEnter": return "Enter";
                case "Backspace": return "Rücktaste";
                case "Tab": return "Tab";
                case "CapsLock": return "Feststell";
                case "UpArrow": return "↑";
                case "DownArrow": return "↓";
                case "LeftArrow": return "←";
                case "RightArrow": return "→";
            }
            if (string.IsNullOrEmpty(key)) return "–";
            if (key.StartsWith("Digit") && key.Length == 6) return key.Substring(5);
            if (key.StartsWith("Numpad")) return "Num " + key.Substring(6);
#if ENABLE_INPUT_SYSTEM
            // Tastaturlayout beachten (QWERTZ: Y/Z, Umlaute …)
            try
            {
                var k = KeyControl(key);
                if (k != null && !string.IsNullOrEmpty(k.displayName) && k.displayName.Length <= 3) return k.displayName.ToUpperInvariant();
            }
            catch (Exception)
            {
                // Anzeige ist nicht kritisch
            }
#endif
            return key;
        }

        public static string PadDisplay(string b)
        {
            switch (b)
            {
                case "South": return "A";
                case "East": return "B";
                case "West": return "X";
                case "North": return "Y";
                case "LeftShoulder": return "LB";
                case "RightShoulder": return "RB";
                case "LeftTrigger": return "LT";
                case "RightTrigger": return "RT";
                case "LeftStick": return "L3";
                case "RightStick": return "R3";
                case "Start": return "Start";
                case "Select": return "Select";
                case "DpadUp": return "Steuerkreuz ↑";
                case "DpadDown": return "Steuerkreuz ↓";
                case "DpadLeft": return "Steuerkreuz ←";
                case "DpadRight": return "Steuerkreuz →";
            }
            return string.IsNullOrEmpty(b) ? "–" : b;
        }

        // =====================================================================================
        // Neu belegen
        // =====================================================================================
        /// <summary>Wartet auf die nächste Taste (pad = Controller). done(true) = übernommen, false = abgebrochen.</summary>
        public static void BeginCapture(string id, bool pad, Action<bool> done)
        {
            if (Def(id) == null) return;
            CancelCapture();
            _captureAction = id;
            _capturePad = pad;
            _captureStartFrame = Time.frameCount;
            _captureDone = done;
        }

        public static void CancelCapture()
        {
            if (_captureAction == null) return;
            Finish(false);
        }

        private static void Finish(bool ok)
        {
            _captureAction = null;
            _captureEndFrame = Time.frameCount;
            var cb = _captureDone;
            _captureDone = null;
            try
            {
                cb?.Invoke(ok);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Einmal pro Frame (UIRoot): wartet auf die neue Taste.</summary>
        public static void TickCapture()
        {
            if (_captureAction == null || Time.frameCount <= _captureStartFrame) return;
#if ENABLE_INPUT_SYSTEM
            if (!_capturePad)
            {
                var kb = Keyboard.current;
                if (kb == null) return;
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    Finish(false);
                    return;
                }
                foreach (var k in kb.allKeys)
                {
                    if (k == null || !k.wasPressedThisFrame) continue;
                    string name = k.keyCode.ToString();
                    if (!Assignable(name)) continue;
                    Assign(_captureAction, name, false);
                    Finish(true);
                    return;
                }
                return;
            }
            var gp = Gamepad.current;
            if (gp == null) return;
            if (gp.startButton.wasPressedThisFrame)
            {
                Finish(false);
                return;
            }
            foreach (var name in PadCandidates)
            {
                var b = PadControl(name);
                if (b == null || !b.wasPressedThisFrame) continue;
                Assign(_captureAction, name, true);
                Finish(true);
                return;
            }
#else
            Finish(false);
#endif
        }

        private static readonly string[] PadCandidates =
        {
            "South", "East", "West", "North", "LeftShoulder", "RightShoulder", "LeftTrigger", "RightTrigger",
            "LeftStick", "RightStick", "Select", "DpadUp", "DpadDown", "DpadLeft", "DpadRight",
        };

        /// <summary>Tasten, die nicht belegt werden dürfen (Bewegung, Menü, Zifferntasten fürs Handy).</summary>
        private static bool Assignable(string key)
        {
            switch (key)
            {
                case "W":
                case "A":
                case "S":
                case "D":
                case "UpArrow":
                case "DownArrow":
                case "LeftArrow":
                case "RightArrow":
                case "Escape":
                case "Enter":
                case "NumpadEnter":
                case "F9":
                case "F10":
                case "F12":
                case "Backquote":
                case "Digit1":
                case "Digit2":
                case "Digit3":
                case "Digit4":
                case "None":
                case "IMESelected":
                    return false;
            }
            return true;
        }

        /// <summary>Neue Taste setzen; ist sie schon vergeben, bekommt die andere Aktion die alte Taste.</summary>
        public static void Assign(string id, string value, bool pad)
        {
            Load();
            var map = pad ? _pad : _kb;
            if (!map.TryGetValue(id, out string old)) old = "";
            foreach (var a in Actions)
            {
                if (a.Id == id) continue;
                if (map.TryGetValue(a.Id, out string other) && other == value) map[a.Id] = old;
            }
            map[id] = value;
            Save();
        }

#if ENABLE_INPUT_SYSTEM
        private static readonly Dictionary<string, Key> _keyCache = new Dictionary<string, Key>();

        private static KeyControl KeyControl(string name)
        {
            var kb = Keyboard.current;
            if (kb == null || string.IsNullOrEmpty(name)) return null;
            if (!_keyCache.TryGetValue(name, out Key key))
            {
                if (!Enum.TryParse(name, out key)) key = Key.None;
                _keyCache[name] = key;
            }
            if (key == Key.None) return null;
            try
            {
                return kb[key];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ButtonControl PadControl(string name)
        {
            var pad = Gamepad.current;
            if (pad == null || string.IsNullOrEmpty(name)) return null;
            if (!Enum.TryParse(name, out GamepadButton b)) return null;
            try
            {
                return pad[b];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool ValidKey(string name) => !string.IsNullOrEmpty(name) && Enum.TryParse(name, out Key k) && k != Key.None;
        private static bool ValidPad(string name) => !string.IsNullOrEmpty(name) && Enum.TryParse(name, out GamepadButton _);
#else
        private static bool ValidKey(string name) => !string.IsNullOrEmpty(name);
        private static bool ValidPad(string name) => !string.IsNullOrEmpty(name);
#endif

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        private static bool LegacyKey(string name, bool down)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name == "LeftCtrl") name = "LeftControl";
            if (name == "RightCtrl") name = "RightControl";
            if (!Enum.TryParse(name, out KeyCode kc)) return false;
            return down ? Input.GetKeyDown(kc) : Input.GetKey(kc);
        }
#endif
    }
}
