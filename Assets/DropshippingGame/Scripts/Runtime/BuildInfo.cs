using System;
using UnityEngine;

namespace DropshippingGame
{
    /// <summary>
    /// Versions- und Build-Infos des laufenden Spiels. Die Build-Werkzeuge (Editor/BuildTools.cs) schreiben
    /// vor jedem Build <c>Resources/build_info.txt</c> (Zeilen <c>schlüssel=wert</c>); fehlt die Datei
    /// (Editor, alter Build), gelten <see cref="Application.version"/> und „Editor“ als Rückfall.
    /// </summary>
    public static class BuildInfo
    {
        private static bool _loaded;
        private static string _version, _build, _date, _commit, _config;

        /// <summary>Spielversion aus der VERSION-Datei, z. B. „0.9.0“.</summary>
        public static string Version { get { Load(); return _version; } }
        /// <summary>Fortlaufende Build-Nummer (Anzahl Git-Commits oder BUILD_NUMBER), „0“ im Editor.</summary>
        public static string Build { get { Load(); return _build; } }
        /// <summary>Build-Datum (UTC, yyyy-MM-dd HH:mm).</summary>
        public static string Date { get { Load(); return _date; } }
        /// <summary>Kurzer Git-Commit-Hash oder leer.</summary>
        public static string Commit { get { Load(); return _commit; } }
        /// <summary>„Release“, „Development“ oder „Editor“.</summary>
        public static string Config { get { Load(); return _config; } }

        /// <summary>Kurzform für Menüs und Fehlerberichte, z. B. „0.9.0 (Build 412, Release)“.</summary>
        public static string Short => Version + " (Build " + Build + ", " + Config + ")";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _loaded = false;

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _version = string.IsNullOrEmpty(Application.version) ? "0.0.0" : Application.version;
            _build = "0";
            _date = "";
            _commit = "";
            _config = Application.isEditor ? "Editor" : (Debug.isDebugBuild ? "Development" : "Release");
            try
            {
                var ta = Resources.Load<TextAsset>("build_info");
                if (ta == null) return;
                foreach (var raw in ta.text.Split('\n'))
                {
                    string line = raw.Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    if (val.Length == 0) continue;
                    switch (key)
                    {
                        case "version": _version = val; break;
                        case "build": _build = val; break;
                        case "date": _date = val; break;
                        case "commit": _commit = val; break;
                        case "config": if (!Application.isEditor) _config = val; break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildInfo] build_info.txt nicht lesbar: " + e.Message);
            }
        }
    }
}
