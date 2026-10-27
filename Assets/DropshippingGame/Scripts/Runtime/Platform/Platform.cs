using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Plattform-Dienste (Steam, später evtl. GOG/Epic): Erfolge, Statistiken, Spielername.
    /// Eine Implementierung meldet sich über <see cref="Platform.Register"/> an (Steam: Platform/Steam/,
    /// nur mit Scripting-Define <c>DS_STEAM</c>). Ohne Anmeldung gilt <see cref="OfflinePlatform"/> (tut nichts).
    /// </summary>
    public interface IPlatformServices
    {
        /// <summary>Anzeigename, z. B. „Steam“ oder „Offline“.</summary>
        string Name { get; }
        /// <summary>true, wenn der Dienst verbunden ist und Aufrufe wirken.</summary>
        bool IsReady { get; }
        /// <summary>Name des angemeldeten Spielers oder leer.</summary>
        string PlayerName { get; }
        /// <summary>Verbindet den Dienst. false = nicht verfügbar (dann wird auf Offline umgeschaltet).</summary>
        bool Init();
        /// <summary>Einmal pro Frame (Callbacks, verzögertes Speichern der Statistiken).</summary>
        void Tick(float unscaledDeltaTime);
        /// <summary>Beim Beenden: offene Statistiken speichern, Verbindung schließen.</summary>
        void Shutdown();
        /// <summary>Erfolg freischalten (idempotent – mehrfaches Aufrufen schadet nicht).</summary>
        void UnlockAchievement(string id);
        /// <summary>true, wenn der Erfolg auf der Plattform bereits freigeschaltet ist.</summary>
        bool IsAchievementUnlocked(string id);
        /// <summary>Ganzzahlige Statistik setzen (z. B. für Fortschritts-Erfolge).</summary>
        void SetStat(string id, int value);
        /// <summary>Alle Erfolge und Statistiken zurücksetzen (nur Entwickler-Werkzeug).</summary>
        void ResetAll();
    }

    /// <summary>
    /// Zentraler Zugang für das Spiel: <c>Platform.UnlockAchievement("ACH_FIRST_SALE")</c>.
    /// Alle Aufrufe sind sicher – auch ohne Steam, vor der Initialisierung und bei Fehlern.
    /// IDs = „API-Name“ des Erfolgs im Steamworks-Backend (Großbuchstaben, z. B. ACH_FIRST_SALE).
    /// Tipp für das Erfolgssystem: nach dem Laden eines Spielstands alle dort freigeschalteten IDs erneut
    /// melden – so werden offline erspielte Erfolge nachgetragen.
    /// </summary>
    public static class Platform
    {
        private static IPlatformServices _registered;
        private static IPlatformServices _current;
        private static bool _initialized;

        /// <summary>Aktiver Dienst (nie null).</summary>
        public static IPlatformServices Current
        {
            get
            {
                if (!_initialized) Initialize();
                return _current ?? (_current = new OfflinePlatform());
            }
        }

        public static string Name => Current.Name;
        public static bool IsOnline => Current.IsReady && !(Current is OfflinePlatform);

        /// <summary>Wird beim Freischalten ausgelöst (für Toasts/Erfolgsanzeige im Spiel, falls gewünscht).</summary>
        public static event Action<string> AchievementUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _registered = null;
            _current = null;
            _initialized = false;
            AchievementUnlocked = null;
        }

        /// <summary>Plattform-Implementierung anmelden (vor dem Laden der ersten Szene).</summary>
        public static void Register(IPlatformServices services)
        {
            if (services == null) return;
            _registered = services;
            if (_initialized)
            {
                // spät angemeldet: sofort umschalten
                _initialized = false;
                Initialize();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!_initialized) Initialize();
        }

        private static void Initialize()
        {
            _initialized = true;
            _current = null;
            if (_registered != null)
            {
                bool ok = false;
                try { ok = _registered.Init(); }
                catch (Exception e) { Debug.LogWarning("[Platform] " + _registered.Name + " nicht verfügbar: " + e.Message); }
                if (ok)
                {
                    _current = _registered;
                    Debug.Log("[Platform] " + _current.Name + " verbunden" + (string.IsNullOrEmpty(_current.PlayerName) ? "" : " (" + _current.PlayerName + ")"));
                    PlatformDriver.Ensure();
                    return;
                }
                Debug.LogWarning("[Platform] " + _registered.Name + " konnte nicht starten – Spiel läuft offline weiter.");
            }
            _current = new OfflinePlatform();
        }

        public static void UnlockAchievement(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            try
            {
                if (Current.IsAchievementUnlocked(id)) return;
                Current.UnlockAchievement(id);
                AchievementUnlocked?.Invoke(id);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Platform] Erfolg " + id + " nicht gesetzt: " + e.Message);
            }
        }

        public static bool IsAchievementUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            try { return Current.IsAchievementUnlocked(id); }
            catch { return false; }
        }

        public static void SetStat(string id, int value)
        {
            if (string.IsNullOrEmpty(id)) return;
            try { Current.SetStat(id, value); }
            catch (Exception e) { Debug.LogWarning("[Platform] Statistik " + id + " nicht gesetzt: " + e.Message); }
        }

        /// <summary>Nur für Tests (Admin-Panel): setzt alle Erfolge zurück.</summary>
        public static void ResetAllAchievements()
        {
            if (!DevTools.Enabled) return;
            try { Current.ResetAll(); }
            catch (Exception e) { Debug.LogWarning("[Platform] Zurücksetzen fehlgeschlagen: " + e.Message); }
        }

        internal static void TickCurrent(float dt)
        {
            try { _current?.Tick(dt); }
            catch (Exception e) { Debug.LogWarning("[Platform] Tick: " + e.Message); }
        }

        internal static void ShutdownCurrent()
        {
            try { _current?.Shutdown(); }
            catch (Exception e) { Debug.LogWarning("[Platform] Shutdown: " + e.Message); }
        }
    }

    /// <summary>Standard ohne Plattform: merkt sich Erfolge nur für die laufende Sitzung (Persistenz liegt im Spielstand).</summary>
    public sealed class OfflinePlatform : IPlatformServices
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        public string Name => "Offline";
        public bool IsReady => true;
        public string PlayerName => "";
        public bool Init() => true;
        public void Tick(float unscaledDeltaTime) { }
        public void Shutdown() { }

        public void UnlockAchievement(string id)
        {
            if (_unlocked.Add(id) && DevTools.Enabled) Debug.Log("[Platform] Erfolg (offline): " + id);
        }

        public bool IsAchievementUnlocked(string id) => _unlocked.Contains(id);
        public void SetStat(string id, int value) { }
        public void ResetAll() => _unlocked.Clear();
    }

    /// <summary>Unsichtbares Objekt, das den aktiven Dienst pro Frame antreibt und beim Beenden schließt.</summary>
    internal sealed class PlatformDriver : MonoBehaviour
    {
        private static PlatformDriver _inst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _inst = null;

        internal static void Ensure()
        {
            if (_inst != null) return;
            var go = new GameObject("PlatformServices") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<PlatformDriver>();
        }

        private void Update() => Platform.TickCurrent(Time.unscaledDeltaTime);

        private void OnApplicationQuit() => Platform.ShutdownCurrent();
    }
}
