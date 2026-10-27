using System.IO;
using DropshippingGame.Testing;
using UnityEditor;
using UnityEngine;

namespace DropshippingGame.EditorTools
{
    /// <summary>
    /// Menü „Dropshipping › Selbsttest (Play Mode)“: startet das Spiel und danach automatisch den Selbsttest
    /// (Runtime/Testing/SelfTest.cs). Läuft das Spiel schon, startet der Test sofort. Der Bericht landet in
    /// <c>persistentDataPath/selftest/&lt;Zeitstempel&gt;/</c> – „Selbsttest-Berichte öffnen“ zeigt den Ordner.
    /// Anleitung für den Inhaber: docs/SELBSTTEST.md.
    /// </summary>
    [InitializeOnLoad]
    public static class SelfTestMenu
    {
        private const string PendingKey = "ds.selftest.pending";

        static SelfTestMenu()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Dropshipping/Selbsttest (Play Mode)", priority = 30)]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                SelfTest.Launch();
                return;
            }
            SessionState.SetBool(PendingKey, true);
            ProjectSetup.PlayGame();
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) SessionState.EraseBool(PendingKey);
        }

        [MenuItem("Dropshipping/Selbsttest-Berichte öffnen", priority = 31)]
        public static void OpenReports()
        {
            string dir = Path.Combine(Application.persistentDataPath, "selftest");
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.EraseBool(PendingKey);
                SelfTest.Launch();
            }
            else if (change == PlayModeStateChange.EnteredEditMode) SessionState.EraseBool(PendingKey);
        }
    }
}
