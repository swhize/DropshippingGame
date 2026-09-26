using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DropshippingGame.EditorTools
{
    /// <summary>
    /// Holt neue Versionen vom GitHub-Branch automatisch ins Projekt (git fetch + pull), damit man
    /// nicht selbst in GitHub Desktop klicken muss. Menü „Dropshipping/Update holen“, automatisch
    /// beim Start von Unity und alle paar Minuten (abschaltbar). Nie während Play.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoUpdate
    {
        private const string PrefAuto = "DS_AutoUpdate";
        private const string MenuAuto = "Dropshipping/Automatisch updaten";
        private const double IntervalSeconds = 180;

        private static double _next;
        private static volatile bool _busy;
        private static string _result;
        private static bool _resultChanged;
        private static bool _resultManual;
        private static readonly object Gate = new object();

        static AutoUpdate()
        {
            _next = EditorApplication.timeSinceStartup + 15; // kurz nach dem Start
            EditorApplication.update += Poll;
        }

        private static bool Auto
        {
            get => EditorPrefs.GetBool(PrefAuto, true);
            set => EditorPrefs.SetBool(PrefAuto, value);
        }

        [MenuItem("Dropshipping/Update holen", priority = 2)]
        private static void ManualUpdate() => Start(true);

        [MenuItem(MenuAuto, priority = 3)]
        private static void ToggleAuto()
        {
            Auto = !Auto;
            Debug.Log("[Update] Automatisch updaten: " + (Auto ? "AN" : "AUS"));
        }

        [MenuItem(MenuAuto, true)]
        private static bool ToggleAutoValidate()
        {
            Menu.SetChecked(MenuAuto, Auto);
            return true;
        }

        private static void Poll()
        {
            string res = null;
            bool manual = false;
            lock (Gate)
            {
                if (_resultChanged)
                {
                    res = _result;
                    manual = _resultManual;
                    _resultChanged = false;
                }
            }
            if (res != null) Finish(res, manual);

            if (!Auto || _busy || EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + IntervalSeconds;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            Start(false);
        }

        private static void Start(bool manual)
        {
            if (_busy) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (manual) EditorUtility.DisplayDialog("Update", "Bitte zuerst Play beenden.", "OK");
                return;
            }
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            string git = FindGit();
            if (git == null)
            {
                if (manual)
                    EditorUtility.DisplayDialog("Update", "Git wurde nicht gefunden.\nBitte GitHub Desktop installieren (bringt Git mit) oder Git für Windows.", "OK");
                return;
            }
            _busy = true;
            Task.Run(() =>
            {
                string msg;
                try { msg = Work(git, root); }
                catch (Exception e) { msg = "ERR:" + e.Message; }
                lock (Gate)
                {
                    _result = msg;
                    _resultManual = manual;
                    _resultChanged = true;
                }
                _busy = false;
            });
        }

        /// <summary>Läuft im Hintergrund. Rückgabe: "NEW:…", "SAME", "ERR:…".</summary>
        private static string Work(string git, string root)
        {
            string branch = Run(git, root, "rev-parse --abbrev-ref HEAD", out int c1).Trim();
            if (c1 != 0) return "ERR:Kein Git-Projekt in " + root;
            Run(git, root, "fetch origin " + branch, out int c2);
            if (c2 != 0) return "ERR:Keine Verbindung zu GitHub (fetch).";
            string behind = Run(git, root, "rev-list --count HEAD..origin/" + branch, out _).Trim();
            if (behind == "0" || behind.Length == 0) return "SAME";
            string log = Run(git, root, "log --format=%s HEAD..origin/" + branch, out _).Trim();
            string pull = Run(git, root, "pull --ff-only --autostash origin " + branch, out int c3);
            if (c3 != 0) return "ERR:Update ging nicht automatisch:\n" + pull.Trim();
            return "NEW:" + behind + "\n" + log;
        }

        private static void Finish(string res, bool manual)
        {
            if (res == "SAME")
            {
                if (manual) EditorUtility.DisplayDialog("Update", "Du hast schon die neueste Version.", "OK");
                return;
            }
            if (res.StartsWith("ERR:"))
            {
                string m = res.Substring(4);
                Debug.LogWarning("[Update] " + m);
                if (manual) EditorUtility.DisplayDialog("Update", m, "OK");
                return;
            }
            string body = res.Substring(4);
            Debug.Log("[Update] Neue Version geladen:\n" + body);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EditorUtility.DisplayDialog("Update", "Neue Version geladen!\n\n" + body + "\n\nUnity lädt kurz neu. Danach einfach ▶ Play drücken.", "OK");
        }

        private static string Run(string exe, string dir, string args, out int code)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                WorkingDirectory = dir, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                if (p == null)
                {
                    code = -1;
                    return "";
                }
                string o = p.StandardOutput.ReadToEnd();
                string e = p.StandardError.ReadToEnd();
                if (!p.WaitForExit(120000))
                {
                    try { p.Kill(); } catch (Exception) { }
                    code = -1;
                    return "Zeitüberschreitung";
                }
                code = p.ExitCode;
                return o + e;
            }
        }

        /// <summary>git aus dem PATH, sonst das von GitHub Desktop mitgebrachte.</summary>
        private static string FindGit()
        {
            try
            {
                Run("git", Directory.GetCurrentDirectory(), "--version", out int c);
                if (c == 0) return "git";
            }
            catch (Exception) { }
            try
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string gd = Path.Combine(local, "GitHubDesktop");
                if (Directory.Exists(gd))
                {
                    string best = null;
                    foreach (var app in Directory.GetDirectories(gd, "app-*"))
                    {
                        string exe = Path.Combine(app, "resources", "app", "git", "cmd", "git.exe");
                        if (File.Exists(exe) && (best == null || string.CompareOrdinal(app, best) > 0)) best = exe;
                    }
                    if (best != null) return best;
                }
                string pf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe");
                if (File.Exists(pf)) return pf;
            }
            catch (Exception) { }
            return null;
        }
    }
}
