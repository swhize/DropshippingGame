using System;
using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DropshippingGame.EditorTools
{
    /// <summary>
    /// Sperrt Release- und Demo-Builds, solange in Resources/credits.json ein Asset mit
    /// <c>"license_class": "personal_only"</c> steht (nur privat/nicht-kommerziell lizenziert, Lizenz noch nicht gekauft).
    /// Editor und Development-Builds bleiben erlaubt. Greift für <see cref="BuildTools"/> (mit Dialog) und – als
    /// Sicherheitsnetz – für jeden anderen Build über <see cref="IPreprocessBuildWithReport"/>.
    /// Regeln: <see cref="AssetLicenses"/>, Übersicht und Kaufliste: docs/ASSET_LIZENZEN.md.
    /// </summary>
    public sealed class LicenseGuard : IPreprocessBuildWithReport
    {
        public const string CreditsPath = "Assets/DropshippingGame/Resources/credits.json";

        public int callbackOrder => -100;

        /// <summary>
        /// true = Build darf starten. Bei Sperre: Fehler in der Konsole (Liste der Assets) und – wenn interactive und
        /// nicht im Batchmode – ein Dialog.
        /// </summary>
        public static bool AllowBuild(bool development, bool interactive, string label)
        {
            if (development) return true;
            if (AssetLicenses.ReleaseAllowed(ReadCredits(), false, out List<LicensedAsset> _, out string reason)) return true;
            Debug.LogError("[Build] " + label + "-Build GESPERRT – Lizenzprüfung:\n" + reason +
                           "\n(Development-Builds und der Editor bleiben erlaubt.)");
            if (interactive && !Application.isBatchMode)
                EditorUtility.DisplayDialog("Release gesperrt – Lizenzen fehlen",
                    reason + "\n\nDevelopment-Builds und der Editor bleiben erlaubt.", "OK");
            return false;
        }

        private static string ReadCredits()
        {
            try
            {
                return File.Exists(CreditsPath) ? File.ReadAllText(CreditsPath) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Build] credits.json nicht lesbar: " + e.Message);
                return null;
            }
        }

        /// <summary>Sicherheitsnetz für Builds über File › Build Profiles o. Ä. (ohne BuildTools).</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            bool dev = (report.summary.options & BuildOptions.Development) != 0;
            if (dev) return;
            if (!AssetLicenses.ReleaseAllowed(ReadCredits(), false, out List<LicensedAsset> _, out string reason))
                throw new BuildFailedException("Release-Build gesperrt (Lizenzprüfung):\n" + reason);
        }

        [MenuItem("Dropshipping/Build/Lizenzen prüfen", priority = 150)]
        public static void MenuCheck()
        {
            var all = AssetLicenses.Parse(ReadCredits());
            if (all == null)
            {
                EditorUtility.DisplayDialog("Lizenzprüfung", "Resources/credits.json fehlt oder ist kein gültiges JSON.", "OK");
                return;
            }
            var blocking = AssetLicenses.Blocking(all);
            string msg = blocking.Count == 0
                ? all.Count + " Asset-Quellen, alle kommerziell nutzbar (license_class \"free\"). Release-Builds sind erlaubt."
                : AssetLicenses.Report(blocking);
            Debug.Log("[Lizenzen] " + msg);
            EditorUtility.DisplayDialog(blocking.Count == 0 ? "Lizenzprüfung: OK" : "Lizenzprüfung: Release gesperrt", msg, "OK");
        }
    }
}
