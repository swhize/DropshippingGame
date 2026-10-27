using System.Collections.Generic;
using System.IO;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Lizenz-Sperre für Release-Builds (personal_only-Assets) und Konsistenz von credits.json.</summary>
    public class AssetLicensesTests
    {
        private const string FreeOnly =
            "{\"credits\":[{\"id\":\"a\",\"name\":\"Pack A\",\"license\":\"CC0 1.0\",\"license_class\":\"free\"}," +
            "{\"id\":\"b\",\"name\":\"Font B\",\"license\":\"SIL OFL 1.1\",\"license_class\":\"free\"}]}";

        private const string WithPersonal =
            "{\"credits\":[{\"id\":\"a\",\"name\":\"Pack A\",\"license\":\"CC0 1.0\",\"license_class\":\"free\"}," +
            "{\"id\":\"p\",\"name\":\"Fancy Moped\",\"license\":\"CC BY-NC 4.0\",\"license_class\":\"personal_only\"," +
            "\"commercial_price_eur\":24.5,\"files\":\"Models/fancy\"}]}";

        [Test]
        public void FreeAssets_AllowRelease()
        {
            Assert.IsTrue(AssetLicenses.ReleaseAllowed(FreeOnly, false, out var blocking, out string reason));
            Assert.AreEqual(0, blocking.Count);
            Assert.AreEqual("", reason);
        }

        [Test]
        public void PersonalOnly_BlocksReleaseAndDemo_ButNotDevelopment()
        {
            Assert.IsFalse(AssetLicenses.ReleaseAllowed(WithPersonal, false, out var blocking, out string reason));
            Assert.AreEqual(1, blocking.Count);
            Assert.AreEqual("p", blocking[0].Id);
            StringAssert.Contains("Fancy Moped", reason);
            StringAssert.Contains("24.50", reason);
            StringAssert.Contains("Models/fancy", reason);

            Assert.IsTrue(AssetLicenses.ReleaseAllowed(WithPersonal, true, out var none, out _));
            Assert.AreEqual(0, none.Count);
        }

        [Test]
        public void MissingOrBrokenCredits_BlockRelease()
        {
            Assert.IsFalse(AssetLicenses.ReleaseAllowed(null, false, out _, out string r1));
            StringAssert.Contains("credits.json", r1);
            Assert.IsFalse(AssetLicenses.ReleaseAllowed("{kaputt", false, out _, out _));
        }

        [Test]
        public void MissingClass_FallsBackToLicenseName()
        {
            Assert.IsTrue(AssetLicenses.IsCommercialOk("", "CC0 1.0"));
            Assert.IsTrue(AssetLicenses.IsCommercialOk(null, "CC BY 3.0"));
            Assert.IsTrue(AssetLicenses.IsCommercialOk("", "SIL OFL 1.1"));
            Assert.IsFalse(AssetLicenses.IsCommercialOk("", "CC BY-NC 4.0"));
            Assert.IsFalse(AssetLicenses.IsCommercialOk("", "Free for personal use"));
            Assert.IsFalse(AssetLicenses.IsCommercialOk("", ""));
            Assert.IsFalse(AssetLicenses.IsCommercialOk("unknown", "CC0 1.0"));        }

        [Test]
        public void Parser_NormalizesClassAndSumsPrices()
        {
            var all = AssetLicenses.Parse(WithPersonal.Replace("personal_only", " Personal_Only "));
            Assert.AreEqual(2, all.Count);
            Assert.AreEqual(AssetLicenses.PersonalOnly, all[1].LicenseClass);
            Assert.AreEqual(24.5f, AssetLicenses.TotalPriceEur(AssetLicenses.Blocking(all)), 1e-4f);
        }

        // ---- echte Projektdateien -------------------------------------------------------------------------

        private static string FindRepoFile(string rel)
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    string p = Path.Combine(dir.FullName, rel);
                    if (File.Exists(p)) return p;
                    dir = dir.Parent;
                }
            }
            return null;
        }

        [Test]
        public void ProjectCredits_EveryEntryClassifiedAndLicenseFilePresent()
        {
            string path = FindRepoFile("Assets/DropshippingGame/Resources/credits.json");
            if (path == null) Assert.Ignore("credits.json nicht gefunden");
            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(path))));
            var dict = Json.Parse(File.ReadAllText(path)) as Dictionary<string, object>;
            Assert.IsNotNull(dict);
            var ids = new HashSet<string>();
            foreach (object o in J.A(dict, "credits"))
            {
                var e = J.Obj(o);
                string id = J.S(e, "id");
                Assert.IsTrue(ids.Add(id), "doppelte Credit-ID " + id);
                string cls = J.S(e, "license_class");
                Assert.IsTrue(cls == AssetLicenses.Free || cls == AssetLicenses.PersonalOnly, id + ": license_class fehlt/ungültig");
                string lf = J.S(e, "licenseFile");
                Assert.IsFalse(string.IsNullOrEmpty(lf), id + ": licenseFile fehlt");
                Assert.IsTrue(File.Exists(Path.Combine(root, lf)), id + ": Lizenzdatei fehlt: " + lf);
            }

            string man = FindRepoFile("Assets/DropshippingGame/Resources/asset_manifest.json");
            if (man == null) return;
            var m = Json.Parse(File.ReadAllText(man)) as Dictionary<string, object>;
            foreach (object o in J.A(m, "models"))
            {
                string credit = J.S(J.Obj(o), "credit");
                if (credit.Length > 0) Assert.IsTrue(ids.Contains(credit), "Manifest verweist auf unbekannte Credit-ID " + credit);
            }
        }
    }
}
