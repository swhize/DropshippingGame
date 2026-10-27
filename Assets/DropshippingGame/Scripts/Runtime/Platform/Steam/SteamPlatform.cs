// Steam-Anbindung über Steamworks.NET (https://github.com/rlabrecque/Steamworks.NET, MIT).
// Wird nur kompiliert, wenn das Scripting-Define DS_STEAM gesetzt ist (Menü Dropshipping › Build ›
// Steam-Integration an/aus) – zusätzlich hat die Assembly DropshippingGame.Steam die Define-Constraint DS_STEAM.
// Ohne Steamworks.NET und ohne DS_STEAM kompiliert das Projekt unverändert. Anleitung: docs/RELEASE.md.
#if DS_STEAM
using System;
using Steamworks;
using UnityEngine;

namespace DropshippingGame.Steam
{
    /// <summary>
    /// <see cref="IPlatformServices"/> für Steam: Erfolge (SetAchievement + StoreStats), Statistiken, Spielername.
    /// Benötigt Steamworks.NET ≥ 2025.161 (Steamworks SDK ≥ 1.61: Statistiken werden beim Start automatisch
    /// synchronisiert, kein RequestCurrentStats mehr nötig).
    /// </summary>
    public sealed class SteamPlatform : IPlatformServices
    {
        /// <summary>
        /// Steam-App-ID dieses Builds (Vollspiel oder Demo, eingetragen in <see cref="SteamIds"/>; solange dort 0 steht:
        /// 480 = Valves Testspiel „Spacewar“). Für lokale Tests zusätzlich steam_appid.txt neben die .exe legen.
        /// </summary>
        public static uint AppId => SteamIds.Current;

        /// <summary>Statistiken frühestens alle n Sekunden an Steam schicken (StoreStats ist gedrosselt).</summary>
        private const float StoreInterval = 2f;

        private bool _ready, _dirty;
        private float _storeTimer;

        public string Name => "Steam";
        public bool IsReady => _ready;

        public string PlayerName
        {
            get
            {
                if (!_ready) return "";
                try { return SteamFriends.GetPersonaName() ?? ""; }
                catch { return ""; }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Register()
        {
            // Außerhalb von Steam gestartet? Dann startet Steam das Spiel neu (mit Overlay, Erfolgen, DRM-frei).
            if (!Application.isEditor)
            {
                try
                {
                    if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
                    {
                        Application.Quit();
                        return;
                    }
                }
                catch (DllNotFoundException e)
                {
                    Debug.LogError("[Steam] steam_api64 fehlt im Build: " + e.Message);
                }
            }
            Platform.Register(new SteamPlatform());
        }

        public bool Init()
        {
            if (!Packsize.Test())
            {
                Debug.LogError("[Steam] Falsche Steamworks.NET-Variante für diese Plattform (Packsize).");
                return false;
            }
            if (!DllCheck.Test())
            {
                Debug.LogError("[Steam] steam_api-DLL hat die falsche Version.");
                return false;
            }
            try
            {
                _ready = SteamAPI.Init();
            }
            catch (DllNotFoundException e)
            {
                Debug.LogError("[Steam] steam_api nicht gefunden: " + e.Message);
                _ready = false;
            }
            if (!_ready) Debug.LogWarning("[Steam] SteamAPI.Init fehlgeschlagen – läuft Steam? Gibt es steam_appid.txt (nur Tests)?");
            return _ready;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!_ready) return;
            SteamAPI.RunCallbacks();
            if (_dirty)
            {
                _storeTimer -= unscaledDeltaTime;
                if (_storeTimer <= 0f) Store();
            }
        }

        public void Shutdown()
        {
            if (!_ready) return;
            if (_dirty) Store();
            SteamAPI.Shutdown();
            _ready = false;
        }

        public void UnlockAchievement(string id)
        {
            if (!_ready) return;
            if (SteamUserStats.SetAchievement(id)) MarkDirty();
            else Debug.LogWarning("[Steam] Unbekannter Erfolg (API-Name im Steamworks-Backend prüfen): " + id);
        }

        public bool IsAchievementUnlocked(string id)
        {
            if (!_ready) return false;
            return SteamUserStats.GetAchievement(id, out bool achieved) && achieved;
        }

        public void SetStat(string id, int value)
        {
            if (!_ready) return;
            if (SteamUserStats.SetStat(id, value)) MarkDirty();
        }

        public void ResetAll()
        {
            if (!_ready) return;
            SteamUserStats.ResetAllStats(true);
            MarkDirty();
        }

        private void MarkDirty()
        {
            if (!_dirty) _storeTimer = 0.25f; // erster Erfolg zeitnah, weitere gebündelt
            _dirty = true;
        }

        private void Store()
        {
            _dirty = false;
            _storeTimer = StoreInterval;
            if (!SteamUserStats.StoreStats()) Debug.LogWarning("[Steam] StoreStats fehlgeschlagen – wird beim nächsten Erfolg erneut versucht.");
        }
    }
}
#endif
