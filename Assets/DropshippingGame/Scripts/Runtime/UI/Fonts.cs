using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Schriften der Oberfläche. Lädt die fünf Schnitte aus Resources/Fonts (feste Pfade laut
    /// Arbeitsplan: UI-Regular, UI-Bold, Display-Bold, Mono-Regular, Mono-Bold) und setzt sie als
    /// UI-Toolkit-Stil. Fehlt eine Datei, bleibt die Standardschrift (bzw. das künstliche Fett aus
    /// dem Stylesheet) – das Spiel läuft also immer.
    ///
    /// Rollen: "regular" (Fließtext, wird vom Wurzelelement geerbt), "bold" (Überschriften klein,
    /// Buttons), "display" (große Überschriften, Kennzahlen), "mono" / "mono_bold" (Geld, Uhrzeit,
    /// Tabellenzahlen – feste Zeichenbreite, damit Zahlen nicht springen).
    /// </summary>
    public static class Fonts
    {
        public const string Regular = "regular";
        public const string Bold = "bold";
        public const string Display = "display";
        public const string Mono = "mono";
        public const string MonoBold = "mono_bold";

        private static bool _loaded;
        private static Font _regular, _bold, _display, _mono, _monoBold;

        /// <summary>USS-Klasse → Schriftrolle. Wird beim Erzeugen von Texten ausgewertet.</summary>
        private static readonly Dictionary<string, string> ClassRoles = new Dictionary<string, string>
        {
            // große Überschriften & Kennzahlen
            { "h1", Display }, { "h2", Display }, { "display", Display }, { "big", Display }, { "app-title", Display },
            { "modal-title", Display }, { "wordmark", Display }, { "banner-text", Display }, { "stat-value", Display },
            { "hero-value", Display }, { "celebrate-level", Display }, { "phone-title", Display }, { "boot-logo", Display },
            { "mini-value", Display }, { "fader-text", Display }, { "empty-title", Display }, { "section-title", Display },
            // fett
            { "h3", Bold }, { "bold", Bold }, { "btn-label", Bold }, { "card-title", Bold }, { "tile-title", Bold },
            { "eyebrow", Bold }, { "section", Bold }, { "stat-key", Bold }, { "chip-text", Bold }, { "badge", Bold },
            { "keycap", Bold }, { "os-brand-text", Bold }, { "dlg-speaker", Bold }, { "ticket-name", Bold },
            { "prompt-title", Bold }, { "obj-title", Bold }, { "obj-eyebrow", Bold }, { "table-head", Bold },
            { "mail-title", Bold }, { "tab-label", Bold }, { "nav-label", Bold }, { "pill-text", Bold },
            { "kv-strong", Bold }, { "menu-btn-label", Bold }, { "hud-level", Bold }, { "toast-title", Bold },
            { "stamp", Bold }, { "slot-title", Bold }, { "phone-tab-label", Bold }, { "row-title", Bold },
            // Zahlen (feste Breite)
            { "mono", Mono }, { "num", Mono }, { "receipt-text", Mono }, { "ticket-time", Mono }, { "kv-num", Mono },
            { "hud-clock", Mono }, { "chart-label", Mono }, { "boot-log", Mono }, { "phone-time", Mono },
            { "num-b", MonoBold }, { "money", MonoBold }, { "hud-money", MonoBold }, { "hud-pop", MonoBold },
            { "ticket-price", MonoBold }, { "receipt-strong", MonoBold }, { "receipt-total", MonoBold },
            { "price", MonoBold }, { "kv-value", MonoBold },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            _regular = _bold = _display = _mono = _monoBold = null;
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _regular = TryLoad("Fonts/UI-Regular");
            _bold = TryLoad("Fonts/UI-Bold");
            _display = TryLoad("Fonts/Display-Bold");
            _mono = TryLoad("Fonts/Mono-Regular");
            _monoBold = TryLoad("Fonts/Mono-Bold");
            int n = (_regular != null ? 1 : 0) + (_bold != null ? 1 : 0) + (_display != null ? 1 : 0) + (_mono != null ? 1 : 0) + (_monoBold != null ? 1 : 0);
            if (n < 5) Debug.Log("UI-Schriften: " + n + " von 5 gefunden (Resources/Fonts). Fehlende nutzen die Standardschrift.");
        }

        private static Font TryLoad(string path)
        {
            try
            {
                return Resources.Load<Font>(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Schrift " + path + " konnte nicht geladen werden: " + e.Message);
                return null;
            }
        }

        /// <summary>Grundschrift für alles darunter (wird vererbt).</summary>
        public static void ApplyRoot(VisualElement root)
        {
            Load();
            if (root == null || _regular == null) return;
            try
            {
                root.style.unityFontDefinition = FontDefinition.FromFont(_regular);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Grundschrift nicht gesetzt: " + e.Message);
            }
        }

        /// <summary>Setzt die Schrift einer Rolle als Inline-Stil. Ohne passende Datei passiert nichts.</summary>
        public static void Set(VisualElement el, string role)
        {
            if (el == null || string.IsNullOrEmpty(role)) return;
            Load();
            Font f = null;
            bool realWeight = false; // true = Schrift ist selbst fett, künstliches Fett abschalten
            switch (role)
            {
                case Display:
                    if (_display != null)
                    {
                        f = _display;
                        realWeight = true;
                    }
                    else if (_bold != null)
                    {
                        f = _bold;
                        realWeight = true;
                    }
                    break;
                case Bold:
                    if (_bold != null)
                    {
                        f = _bold;
                        realWeight = true;
                    }
                    break;
                case MonoBold:
                    if (_monoBold != null)
                    {
                        f = _monoBold;
                        realWeight = true;
                    }
                    else f = _mono;
                    break;
                case Mono:
                    f = _mono;
                    break;
                case Regular:
                    f = _regular;
                    break;
            }
            if (f == null) return;
            try
            {
                el.style.unityFontDefinition = FontDefinition.FromFont(f);
                if (realWeight) el.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Schrift nicht gesetzt: " + e.Message);
            }
        }

        /// <summary>Rolle aus den USS-Klassen des Elements ableiten und setzen.</summary>
        public static void Auto(VisualElement el)
        {
            if (el == null) return;
            string role = null;
            foreach (var c in el.GetClasses())
            {
                if (!ClassRoles.TryGetValue(c, out string r)) continue;
                // Mono schlägt Fett/Display (Zahlen sollen immer feste Breite haben)
                if (role == null || r == MonoBold || r == Mono && role != MonoBold) role = r;
            }
            if (role != null) Set(el, role);
        }

        /// <summary>Klasse hinzufügen und Schrift neu bestimmen.</summary>
        public static void AddClass(VisualElement el, string cls)
        {
            if (el == null) return;
            el.AddToClassList(cls);
            Auto(el);
        }
    }
}
