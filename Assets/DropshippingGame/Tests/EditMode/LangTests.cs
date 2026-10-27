using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Übersetzung zur Anzeigezeit (Lang): exakt, Vorlagen mit verschachtelten Teilen, mehrzeilig.</summary>
    public class LangTests
    {
        [TearDown]
        public void Reset() => Lang.ResetForTests();

        [Test]
        public void GermanIsUnchanged()
        {
            Lang.ResetForTests();
            Lang.Add("Feierabend", "Closing time");
            Assert.AreEqual("Feierabend", Lang.T("Feierabend"));
        }

        [Test]
        public void ExactTemplateNestedMultiline()
        {
            Lang.ResetForTests();
            Lang.T("x"); // Tabellen laden (leer oder befüllt), danach eigene Testeinträge
            Lang.Add("Handyhülle", "Phone case");
            Lang.Add("Feierabend", "Closing time");
            Lang.Add("Lieferung angekommen: {0}× {1} am Wareneingang.", "Delivery arrived: {0}× {1} at goods-in.");
            Lang.Add("{0} gekauft!", "{0} bought!");
            Lang.Set("en");
            Assert.AreEqual("Closing time", Lang.T("Feierabend"));
            Assert.AreEqual("  Closing time ", Lang.T("  Feierabend "));
            Assert.AreEqual("Delivery arrived: 50× Phone case at goods-in.", Lang.T("Lieferung angekommen: 50× Handyhülle am Wareneingang."));
            Assert.AreEqual("Phone case bought!", Lang.T("Handyhülle gekauft!"));
            Assert.AreEqual("Closing time\nPhone case bought!", Lang.T("Feierabend\nHandyhülle gekauft!"));
            Assert.AreEqual("1.234 €", Lang.T("1.234 €"));
            Lang.T("Etwas Unbekanntes");
            CollectionAssert.Contains(Lang.Missing, "Etwas Unbekanntes");
        }
    }
}
