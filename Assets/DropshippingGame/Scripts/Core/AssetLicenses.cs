using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DropshippingGame.Core
{
    /// <summary>Ein Eintrag aus Resources/credits.json, reduziert auf das, was für die Lizenzprüfung zählt.</summary>
    public sealed class LicensedAsset
    {
        public string Id = "";
        public string Name = "";
        public string License = "";
        public string Files = "";
        public string Url = "";
        /// <summary><see cref="AssetLicenses.Free"/> oder <see cref="AssetLicenses.PersonalOnly"/> (bzw. unbekannt).</summary>
        public string LicenseClass = "";
        /// <summary>Preis der kommerziellen Lizenz in Euro (nur bei personal_only sinnvoll, 0 = unbekannt).</summary>
        public float CommercialPriceEur;
        /// <summary>Darf in einem verkauften Build (Release/Demo) enthalten sein.</summary>
        public bool CommercialOk;
    }

    /// <summary>
    /// Lizenz-Regeln für Drittanbieter-Assets (siehe docs/ASSET_LIZENZEN.md):
    /// jeder Eintrag in Resources/credits.json trägt <c>"license_class": "free"</c> (CC0/CC-BY/OFL/MIT – kommerziell
    /// erlaubt) oder <c>"personal_only"</c> (nur privat/nicht-kommerziell frei, Lizenz noch nicht gekauft).
    /// Solange ein personal_only-Asset enthalten ist, sind Release- und Demo-Builds gesperrt
    /// (Editor und Development-Builds bleiben erlaubt). Fehlt das Feld, wird am Lizenznamen erkannt
    /// (NC/NonCommercial/personal → gesperrt). Unlesbare credits.json sperrt ebenfalls – ohne Credits
    /// fehlen auch die Pflicht-Namensnennungen.
    /// </summary>
    public static class AssetLicenses
    {
        public const string Free = "free";
        public const string PersonalOnly = "personal_only";

        /// <summary>Liest alle Einträge. null = JSON fehlt oder ist kaputt.</summary>
        public static List<LicensedAsset> Parse(string creditsJson)
        {
            if (string.IsNullOrWhiteSpace(creditsJson)) return null;
            if (!Json.TryParse(creditsJson, out object root)) return null;
            List<object> items = null;
            if (root is Dictionary<string, object> d)
            {
                foreach (var key in new[] { "credits", "assets", "items", "entries" })
                {
                    items = J.A(d, key);
                    if (items.Count > 0) break;
                }
            }
            else if (root is List<object> l) items = l;
            if (items == null) return null;

            var res = new List<LicensedAsset>();
            foreach (object o in items)
            {
                if (!(o is Dictionary<string, object> e)) continue;
                var a = new LicensedAsset
                {
                    Id = J.S(e, "id"),
                    Name = J.S(e, "name"),
                    License = J.S(e, "license"),
                    Files = J.S(e, "files"),
                    Url = J.S(e, "url"),
                    LicenseClass = J.S(e, "license_class").Trim().ToLowerInvariant(),
                    CommercialPriceEur = Math.Max(0f, J.F(e, "commercial_price_eur")),
                };
                a.CommercialOk = IsCommercialOk(a.LicenseClass, a.License);
                res.Add(a);
            }
            return res;
        }

        /// <summary>Kommerziell nutzbar? Mit Klasse: nur "free". Ohne Klasse: Lizenzname ohne NC/personal-Hinweis.</summary>
        public static bool IsCommercialOk(string licenseClass, string license)
        {
            if (!string.IsNullOrEmpty(licenseClass)) return licenseClass == Free;
            string l = (license ?? "").ToLowerInvariant();
            if (l.Length == 0) return false;
            return !(l.Contains("-nc") || l.Contains(" nc") || l.Contains("noncommercial") || l.Contains("non-commercial") ||
                     l.Contains("personal") || l.Contains("privat"));
        }

        /// <summary>Alle Einträge, die einen Release-/Demo-Build blockieren.</summary>
        public static List<LicensedAsset> Blocking(List<LicensedAsset> all)
        {
            var res = new List<LicensedAsset>();
            if (all == null) return res;
            foreach (var a in all)
                if (!a.CommercialOk)
                    res.Add(a);
            return res;
        }

        /// <summary>
        /// Darf ein verkaufter Build (Release/Demo) gebaut werden? development = true erlaubt immer.
        /// reason enthält bei false eine lesbare Liste der blockierenden Assets.
        /// </summary>
        public static bool ReleaseAllowed(string creditsJson, bool development, out List<LicensedAsset> blocking, out string reason)
        {
            blocking = new List<LicensedAsset>();
            reason = "";
            if (development) return true;
            var all = Parse(creditsJson);
            if (all == null)
            {
                reason = "Resources/credits.json fehlt oder ist kein gültiges JSON – ohne Lizenzliste wird kein Release gebaut.";
                return false;
            }
            blocking = Blocking(all);
            if (blocking.Count == 0) return true;
            reason = Report(blocking);
            return false;
        }

        /// <summary>Summe der kommerziellen Lizenzpreise (Euro) der übergebenen Assets.</summary>
        public static float TotalPriceEur(List<LicensedAsset> assets)
        {
            float sum = 0f;
            if (assets != null)
                foreach (var a in assets)
                    sum += a.CommercialPriceEur;
            return sum;
        }

        /// <summary>Mehrzeiliger Text für Dialog/Konsole.</summary>
        public static string Report(List<LicensedAsset> blocking)
        {
            var sb = new StringBuilder();
            sb.Append(blocking.Count).Append(blocking.Count == 1 ? " Asset ist" : " Assets sind")
              .Append(" nur für privaten/nicht-kommerziellen Gebrauch lizenziert:\n");
            foreach (var a in blocking)
            {
                sb.Append("• ").Append(string.IsNullOrEmpty(a.Name) ? a.Id : a.Name)
                  .Append(" [").Append(a.Id).Append("] – ").Append(string.IsNullOrEmpty(a.License) ? "Lizenz unbekannt" : a.License);
                if (a.CommercialPriceEur > 0f)
                    sb.Append(" – kommerzielle Lizenz ").Append(a.CommercialPriceEur.ToString("0.00", CultureInfo.InvariantCulture)).Append(" €");
                if (!string.IsNullOrEmpty(a.Files)) sb.Append("\n    Dateien: ").Append(a.Files);
                sb.Append('\n');
            }
            float total = TotalPriceEur(blocking);
            if (total > 0f) sb.Append("Summe kommerzielle Lizenzen: ").Append(total.ToString("0.00", CultureInfo.InvariantCulture)).Append(" €\n");
            sb.Append("Lizenzen kaufen (docs/ASSET_LIZENZEN.md) und \"license_class\" auf \"free\" setzen – oder die Assets entfernen.");
            return sb.ToString();
        }
    }
}
