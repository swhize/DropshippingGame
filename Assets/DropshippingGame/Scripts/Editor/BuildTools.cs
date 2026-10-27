using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DropshippingGame.EditorTools
{
    /// <summary>
    /// Fertige Spiel-Builds per Menü <b>Dropshipping › Build</b> oder Kommandozeile (siehe <see cref="BuildFromCommandLine"/>).
    ///
    /// Varianten:
    /// · <b>Release</b> – für Steam. Ohne Entwickler-Werkzeuge (Admin-Panel F10/F9/^, Noclip Alt, Vogelperspektive
    ///   sind per <see cref="DevTools"/> abgeschaltet), Windows mit IL2CPP (schneller, schwerer zu dekompilieren).
    /// · <b>Development</b> – Development-Build + Define DS_DEV: Admin-Panel und Noclip aktiv, Mono (schneller Build).
    /// · <b>Demo</b> – wie Release, plus Define DS_DEMO (5 Spieltage, siehe <see cref="DemoMode"/>), eigener
    ///   Produktname „… Demo“ (eigener Spielstand-Ordner).
    ///
    /// Scripting-Backend: IL2CPP braucht das Unity-Modul „Windows Build Support (IL2CPP)“ und Visual Studio mit
    /// „Desktop development with C++“ – und baut nur für das eigene Betriebssystem. Fehlt das Modul, wird mit Warnung
    /// auf Mono umgeschaltet. Linux/macOS bauen standardmäßig mit Mono (geht von Windows aus).
    /// Version: Datei <c>VERSION</c> im Repo-Wurzelordner (z. B. 0.9.0) + Build-Nummer (Git-Commits oder BUILD_NUMBER)
    /// → <c>Application.version</c> = „0.9.0.412“, Details in Resources/build_info.txt (nur während des Builds).
    /// </summary>
    public static class BuildTools
    {
        /// <summary>Produktname im Spiel, Fenstertitel und Spielstand-Ordner – überall dieser eine Wert.</summary>
        public const string ProductName = "Dropshipping Simulator";
        public const string CompanyName = "Hustle Games";
        /// <summary>Dateiname der ausführbaren Datei (ohne Leerzeichen).</summary>
        public const string ExeName = "DropshippingSimulator";

        public const string DefineDev = "DS_DEV";
        public const string DefineDemo = "DS_DEMO";
        public const string DefineSteam = "DS_STEAM";

        private const string BuildInfoPath = "Assets/DropshippingGame/Resources/build_info.txt";
        private const string ScenePath = "Assets/Scenes/Game.unity";

        public enum Target { Windows, Linux, MacOS }
        public enum Config { Release, Development, Demo }

        // ---- Menü ---------------------------------------------------------------------------------------
        [MenuItem("Dropshipping/Build/Windows (Release)", priority = 100)]
        public static void MenuWindowsRelease() => BuildInteractive(Target.Windows, Config.Release);

        [MenuItem("Dropshipping/Build/Windows (Development)", priority = 101)]
        public static void MenuWindowsDev() => BuildInteractive(Target.Windows, Config.Development);

        [MenuItem("Dropshipping/Build/Windows (Demo)", priority = 102)]
        public static void MenuWindowsDemo() => BuildInteractive(Target.Windows, Config.Demo);

        [MenuItem("Dropshipping/Build/Linux", priority = 120)]
        public static void MenuLinux() => BuildInteractive(Target.Linux, Config.Release);

        [MenuItem("Dropshipping/Build/macOS", priority = 121)]
        public static void MenuMac() => BuildInteractive(Target.MacOS, Config.Release);

        [MenuItem("Dropshipping/Build/Build-Ordner öffnen", priority = 140)]
        public static void MenuOpenBuilds()
        {
            string dir = Path.Combine(ProjectRoot, "Builds");
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        [MenuItem("Dropshipping/Build/Steam-Integration an/aus (DS_STEAM)", priority = 160)]
        public static void MenuToggleSteam()
        {
            var defs = GetDefines();
            bool on = defs.Contains(DefineSteam);
            if (!on && Type.GetType("Steamworks.SteamAPI, com.rlabrecque.steamworks.net") == null)
            {
                if (!EditorUtility.DisplayDialog("Steamworks.NET fehlt",
                        "Steamworks.NET ist nicht installiert. Ohne das Paket schlägt das Kompilieren mit DS_STEAM fehl.\n\n" +
                        "Installation: Window › Package Manager › + › Add package from git URL:\n" +
                        "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net\n\n" +
                        "Trotzdem einschalten?", "Trotzdem", "Abbrechen")) return;
            }
            if (on) defs.Remove(DefineSteam);
            else defs.Add(DefineSteam);
            SetDefines(defs);
            Debug.Log("[Build] Steam-Integration " + (on ? "AUS" : "AN") + ". Defines: " + string.Join(";", defs));
        }

        [MenuItem("Dropshipping/Build/Steam-Integration an/aus (DS_STEAM)", true)]
        private static bool MenuToggleSteamValidate()
        {
            Menu.SetChecked("Dropshipping/Build/Steam-Integration an/aus (DS_STEAM)", GetDefines().Contains(DefineSteam));
            return true;
        }

        private static void BuildInteractive(Target target, Config config)
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var report = Build(target, config, null, null, null);
            if (report != null && report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(report.summary.outputPath);
        }

        // ---- Kommandozeile ----------------------------------------------------------------------------------
        /// <summary>
        /// Für Batch-Builds (CI, Skripte). Beispiel (Windows, Release, IL2CPP):
        /// <code>Unity.exe -batchmode -nographics -projectPath . -executeMethod DropshippingGame.EditorTools.BuildTools.BuildFromCommandLine
        ///   -dsTarget windows -dsConfig release -dsOut Builds/Steam/Windows -logFile build.log</code>
        /// Argumente: <c>-dsTarget windows|linux|macos</c> (Standard windows), <c>-dsConfig release|development|demo</c>
        /// (Standard release), <c>-dsOut &lt;Ordner&gt;</c>, optional <c>-dsBackend il2cpp|mono</c>, <c>-dsBuildNumber &lt;n&gt;</c>.
        /// Beendet Unity mit Exit-Code 0 (Erfolg) bzw. 1.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            int code = 1;
            try
            {
                string t = Arg("-dsTarget", "windows").ToLowerInvariant();
                string c = Arg("-dsConfig", "release").ToLowerInvariant();
                var target = t.StartsWith("lin") ? Target.Linux : (t.StartsWith("mac") || t.StartsWith("osx") ? Target.MacOS : Target.Windows);
                var config = c.StartsWith("dev") ? Config.Development : (c.StartsWith("demo") ? Config.Demo : Config.Release);
                string backend = Arg("-dsBackend", null);
                ScriptingImplementation? be = null;
                if (!string.IsNullOrEmpty(backend)) be = backend.ToLowerInvariant().StartsWith("mono") ? ScriptingImplementation.Mono2x : ScriptingImplementation.IL2CPP;
                var report = Build(target, config, Arg("-dsOut", null), be, Arg("-dsBuildNumber", null));
                code = report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[Build] Abbruch: " + e);
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        private static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return fallback;
        }

        // ---- Build ------------------------------------------------------------------------------------------
        /// <summary>Baut das Spiel. <paramref name="outDir"/> null = Builds/&lt;Ziel&gt;-&lt;Variante&gt;.</summary>
        public static BuildReport Build(Target target, Config config, string outDir, ScriptingImplementation? backend, string buildNumber)
        {
            var named = NamedBuildTarget.Standalone;
            BuildTarget bt = target == Target.Windows ? BuildTarget.StandaloneWindows64 : (target == Target.Linux ? BuildTarget.StandaloneLinux64 : BuildTarget.StandaloneOSX);
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, bt))
            {
                Debug.LogError("[Build] Build-Modul für " + target + " ist nicht installiert (Unity Hub › Installs › Add modules).");
                return null;
            }

            // Lizenzen: Release/Demo gesperrt, solange "personal_only"-Assets enthalten sind (docs/ASSET_LIZENZEN.md)
            if (!LicenseGuard.AllowBuild(config == Config.Development, true, config.ToString())) return null;

            // Projekt sicher eingerichtet (Szene, Pipeline, Player-Einstellungen, Icon)
            if (!File.Exists(ScenePath)) ProjectSetup.Run(false);
            ProjectSetup.ApplyPlayerSettings();

            string version = ReadVersion();
            string build = !string.IsNullOrEmpty(buildNumber) ? buildNumber : (Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? GitBuildNumber());
            string commit = Git("rev-parse --short HEAD");
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");

            // Backend
            ScriptingImplementation be = backend ?? (target == Target.Windows && config != Config.Development ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
            if (be == ScriptingImplementation.IL2CPP && !Il2CppInstalled(bt))
            {
                Debug.LogWarning("[Build] IL2CPP-Modul für " + target + " nicht gefunden – baue mit Mono. (Unity Hub › Installs › Add modules › „" + target + " Build Support (IL2CPP)“)");
                be = ScriptingImplementation.Mono2x;
            }

            // Ausgabe
            string folder = target + "-" + config;
            string dir = string.IsNullOrEmpty(outDir) ? Path.Combine(ProjectRoot, "Builds", folder) : Path.GetFullPath(outDir);
            string productName = config == Config.Demo ? ProductName + " Demo" : ProductName;
            string exe = config == Config.Demo ? ExeName + "Demo" : ExeName;
            string location = target == Target.Windows ? Path.Combine(dir, exe + ".exe")
                : target == Target.Linux ? Path.Combine(dir, exe + ".x86_64")
                : Path.Combine(dir, productName + ".app");
            if (Directory.Exists(dir) && IsInsideBuildsFolder(dir)) Directory.Delete(dir, true); // alte Dateien raus (nur unter Builds/)
            Directory.CreateDirectory(dir);

            // Defines: Projekt-Defines ohne DS_DEV/DS_DEMO (die kommen nur über die Variante dazu)
            var oldDefines = PlayerSettings.GetScriptingDefineSymbols(named);
            var oldBackend = PlayerSettings.GetScriptingBackend(named);
            string oldProduct = PlayerSettings.productName;
            string oldVersion = PlayerSettings.bundleVersion;
            var defs = GetDefines();
            defs.Remove(DefineDev);
            defs.Remove(DefineDemo);
            var extra = new List<string>();
            if (config == Config.Development) extra.Add(DefineDev);
            if (config == Config.Demo) extra.Add(DefineDemo);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = location,
                target = bt,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.DetailedBuildReport | (config == Config.Development ? BuildOptions.Development : BuildOptions.None),
                extraScriptingDefines = extra.ToArray(),
            };

            BuildReport report = null;
            var sw = Stopwatch.StartNew();
            try
            {
                SetDefines(defs);
                PlayerSettings.SetScriptingBackend(named, be);
                PlayerSettings.productName = productName;
                PlayerSettings.bundleVersion = version + "." + build;
                PlayerSettings.macOS.buildNumber = build;
                WriteBuildInfo(version, build, date, commit, config);
                Debug.Log("[Build] Starte " + target + " " + config + " · v" + PlayerSettings.bundleVersion + " · " + be +
                          " · Defines: " + string.Join(";", defs.Concat(extra)) + " → " + location);
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                SetDefinesRaw(oldDefines);
                PlayerSettings.SetScriptingBackend(named, oldBackend);
                PlayerSettings.productName = oldProduct;
                PlayerSettings.bundleVersion = oldVersion;
                DeleteBuildInfo();
                AssetDatabase.SaveAssets();
            }
            sw.Stop();

            if (report != null && report.summary.result == BuildResult.Succeeded) MoveDoNotShipFolders(dir, target + "-" + config);
            LogSummary(report, target, config, version + "." + build, be, sw.Elapsed);
            return report;
        }

        /// <summary>Nur Ordner unterhalb von &lt;Projekt&gt;/Builds/ werden vor dem Build geleert – nie fremde Ordner.</summary>
        private static bool IsInsideBuildsFolder(string dir)
        {
            string full = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string builds = Path.GetFullPath(Path.Combine(ProjectRoot, "Builds")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.Length > builds.Length && full.StartsWith(builds + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string[] Scenes()
        {
            var list = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            return list.Length > 0 ? list : new[] { ScenePath };
        }

        /// <summary>IL2CPP legt Debug-Symbole/Backups in den Ausgabeordner – die gehören nicht ins Steam-Depot.</summary>
        private static void MoveDoNotShipFolders(string dir, string tag)
        {
            try
            {
                string sym = Path.Combine(ProjectRoot, "Builds", "_Symbols", tag + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm"));
                foreach (var d in Directory.GetDirectories(dir))
                {
                    string n = Path.GetFileName(d);
                    if (n.IndexOf("DoNotShip", StringComparison.OrdinalIgnoreCase) < 0 && n.IndexOf("DontShip", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Directory.CreateDirectory(sym);
                    Directory.Move(d, Path.Combine(sym, n));
                    Debug.Log("[Build] Nicht ausliefern – verschoben nach " + sym + ": " + n);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Build] DoNotShip-Ordner nicht verschoben (vor dem Upload von Hand entfernen!): " + e.Message);
            }
        }

        private static void LogSummary(BuildReport report, Target target, Config config, string version, ScriptingImplementation be, TimeSpan time)
        {
            var sb = new StringBuilder();
            if (report == null)
            {
                Debug.LogError("[Build] Kein Build-Report – Build nicht gestartet.");
                return;
            }
            var s = report.summary;
            bool ok = s.result == BuildResult.Succeeded;
            sb.AppendLine("[Build] " + (ok ? "ERFOLGREICH" : "FEHLGESCHLAGEN (" + s.result + ")") + " – " + ProductName + " " + version + " · " + target + " " + config + " · " + be);
            sb.AppendLine("  Ausgabe:   " + s.outputPath);
            sb.AppendLine("  Größe:     " + Mb(s.totalSize) + " (Player-Daten laut Report)" + (ok ? " · Ordner gesamt: " + Mb(FolderSize(Path.GetDirectoryName(s.outputPath))) : ""));
            sb.AppendLine("  Dauer:     " + time.ToString(@"mm\:ss") + " · Warnungen " + s.totalWarnings + " · Fehler " + s.totalErrors);
            sb.AppendLine("  Entwickler-Werkzeuge: " + (config == Config.Development ? "AN (DS_DEV)" : "aus") + (config == Config.Demo ? " · DEMO (" + DemoMode.DayLimit + " Tage)" : ""));
            try
            {
                var biggest = report.packedAssets.SelectMany(p => p.contents)
                    .GroupBy(c => c.sourceAssetPath)
                    .Select(g => new { Path = g.Key, Size = g.Sum(c => (long)c.packedSize) })
                    .OrderByDescending(x => x.Size).Take(10).ToList();
                if (biggest.Count > 0)
                {
                    sb.AppendLine("  Größte Assets:");
                    foreach (var b in biggest) sb.AppendLine("    " + Mb((ulong)b.Size).PadLeft(9) + "  " + (string.IsNullOrEmpty(b.Path) ? "(intern)" : b.Path));
                }
            }
            catch
            {
                // Detail-Report optional
            }
            if (!ok)
                foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type == LogType.Error || m.type == LogType.Exception)
                            sb.AppendLine("  ✗ " + m.content);
            string text = sb.ToString();
            if (ok) Debug.Log(text);
            else Debug.LogError(text);
            try
            {
                string rp = Path.Combine(ProjectRoot, "Builds", target + "-" + config + "_report.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(rp));
                File.WriteAllText(rp, text);
            }
            catch
            {
                // Report-Datei optional
            }
        }

        private static string Mb(ulong bytes) => (bytes / (1024.0 * 1024.0)).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";

        private static ulong FolderSize(string dir)
        {
            try
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return 0;
                ulong sum = 0;
                foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) sum += (ulong)new FileInfo(f).Length;
                return sum;
            }
            catch
            {
                return 0;
            }
        }

        private static bool Il2CppInstalled(BuildTarget bt)
        {
            try
            {
                string engine = BuildPipeline.GetPlaybackEngineDirectory(bt, BuildOptions.None);
                if (string.IsNullOrEmpty(engine)) return true; // unbekannt: Unity meldet es selbst
                string variations = Path.Combine(engine, "Variations");
                if (!Directory.Exists(variations)) return true;
                return Directory.GetDirectories(variations).Any(d => Path.GetFileName(d).IndexOf("il2cpp", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            catch
            {
                return true;
            }
        }

        // ---- Version ---------------------------------------------------------------------------------------
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        /// <summary>Inhalt der Datei VERSION (erste Zeile), Rückfall 0.0.0.</summary>
        public static string ReadVersion()
        {
            try
            {
                string p = Path.Combine(ProjectRoot, "VERSION");
                if (File.Exists(p))
                {
                    string v = File.ReadAllLines(p).Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0 && !l.StartsWith("#"));
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Build] VERSION nicht lesbar: " + e.Message);
            }
            return "0.0.0";
        }

        private static string GitBuildNumber()
        {
            string n = Git("rev-list --count HEAD");
            if (!string.IsNullOrEmpty(n) && int.TryParse(n, out _)) return n;
            // ohne Git: Minuten seit 2026-01-01 / 10 – steigt monoton
            return ((int)((DateTime.UtcNow - new DateTime(2026, 1, 1)).TotalMinutes / 10)).ToString();
        }

        private static string Git(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("git", args)
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return "";
                    string o = p.StandardOutput.ReadToEnd();
                    if (!p.WaitForExit(5000)) return "";
                    return p.ExitCode == 0 ? o.Trim() : "";
                }
            }
            catch
            {
                return "";
            }
        }

        private static void WriteBuildInfo(string version, string build, string date, string commit, Config config)
        {
            string cfg = config == Config.Development ? "Development" : (config == Config.Demo ? "Demo" : "Release");
            File.WriteAllText(BuildInfoPath, "version=" + version + "\nbuild=" + build + "\ndate=" + date + "\ncommit=" + commit + "\nconfig=" + cfg + "\n");
            AssetDatabase.ImportAsset(BuildInfoPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void DeleteBuildInfo()
        {
            if (File.Exists(BuildInfoPath)) AssetDatabase.DeleteAsset(BuildInfoPath);
        }

        // ---- Defines --------------------------------------------------------------------------------------
        private static List<string> GetDefines()
        {
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)
                .Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        }

        private static void SetDefines(List<string> defs) => SetDefinesRaw(string.Join(";", defs));

        private static void SetDefinesRaw(string defs)
        {
            if (PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone) != defs)
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, defs);
        }
    }
}
