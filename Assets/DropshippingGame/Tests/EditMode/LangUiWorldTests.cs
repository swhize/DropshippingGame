using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>
    /// Englische Tabellen für Oberfläche (EnUi) und 3D-Welt (EnWorld): zusammengesetzte Hinweise, Kettenvorlagen und
    /// übersetzte Format-Vorlagen (Loc.F) müssen vollständig übersetzt werden.
    /// </summary>
    public class LangUiWorldTests
    {
        [SetUp]
        public void Setup()
        {
            Lang.ResetForTests();
            Lang.Set("en");
        }

        [TearDown]
        public void Reset() => Lang.ResetForTests();

        [Test]
        public void ComposedHudAndWorldPrompts()
        {
            Assert.AreEqual("Order monitor: 4 open · 1 express · 2 overdue", Lang.T("Bestell-Monitor: 4 offen · 1 Express · 2 überfällig"));
            Assert.AreEqual("Order monitor: 4 open · 2 overdue", Lang.T("Bestell-Monitor: 4 offen · 2 überfällig"));
            Assert.AreEqual("Stock 5 · 3 in transit · 2 at goods-in · 1 open · 4 for bulk orders",
                Lang.T("Lager 5 · 3 unterwegs · 2 am Eingang · 1 offen · 4 für Aufträge"));
            Assert.AreEqual("Get on  ·  Top box 2/4 · 1 labelled", Lang.T("Aufsteigen  ·  Heckbox 2/4 · 1 mit Label"));
            Assert.AreEqual("Drop off parcel (+12 € · free) – from the top box", Lang.T("Paket abgeben (+12 € · gratis) – aus der Heckbox"));
            Assert.AreEqual("EMOTES\nrelease T", Lang.T("EMOTES\nT loslassen"));
            Assert.AreEqual("Day 1. Your garage. Your business. Let's go!", Lang.T("Tag 1. Deine Garage. Dein Business. Los geht's!"));
        }

        [Test]
        public void FormatTemplatesKeepPlaceholders()
        {
            // Loc.F übersetzt die deutsche Vorlage selbst – die Platzhalter müssen erhalten bleiben.
            Assert.AreEqual("Into the trolley: {0} · {1} left", Lang.T("In den Wagen: {0} · noch {1}"));
            Assert.AreEqual(" ({0} pcs)", Lang.T(" ({0} Stk)"));
            Assert.AreEqual("{0} ({1}/pc · wholesale {2})", Lang.T("{0} ({1}/Stk · Großhandel {2})"));
            Assert.AreEqual("{0} · {1} in cart", Lang.T("{0} · {1} drin"));
        }

        [Test]
        public void MainMenuVersionPanel()
        {
            Assert.AreEqual("NEW IN VERSION 1.0", Lang.T("NEU IN VERSION 1.0"));
            Assert.AreEqual("Phone (Tab) & tablet", Lang.T("Handy (Tab) & Tablet"));
            Assert.AreEqual("German & English", Lang.T("Deutsch & Englisch"));
        }

        [Test]
        public void GermanStaysUntouched()
        {
            Lang.Set("de");
            Assert.AreEqual("Bestell-Monitor: 4 offen", Lang.T("Bestell-Monitor: 4 offen"));
        }
    }
}
