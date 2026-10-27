using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Möbel verschieben: Raster, Drehung, Gültigkeit und Spielstand.</summary>
    public class FurnitureTests
    {
        private static readonly FloorRect Room = new FloorRect(0f, 0f, 10f, 8f);

        [Test]
        public void Snap_RoundsToGridAndHandlesNaN()
        {
            Assert.AreEqual(1.25f, FurnitureLayout.Snap(1.2f), 1e-4f);
            Assert.AreEqual(-1.25f, FurnitureLayout.Snap(-1.2f), 1e-4f);
            Assert.AreEqual(0f, FurnitureLayout.Snap(float.NaN));
        }

        [Test]
        public void Angles_AreNormalizedAndSnapped()
        {
            Assert.AreEqual(270f, FurnitureLayout.NormalizeAngle(-90f), 1e-4f);
            Assert.AreEqual(0f, FurnitureLayout.NormalizeAngle(360f), 1e-4f);
            Assert.AreEqual(0f, FurnitureLayout.SnapAngle(359f), 1e-4f);
            Assert.AreEqual(90f, FurnitureLayout.SnapAngle(93f), 1e-4f);
        }

        [Test]
        public void Overlaps_DetectsAxisAlignedAndRotated()
        {
            var a = new Footprint(2f, 2f, 1f, 0.5f, 0f);
            Assert.IsTrue(FurnitureLayout.Overlaps(a, new Footprint(3f, 2f, 1f, 0.5f, 0f)));
            Assert.IsFalse(FurnitureLayout.Overlaps(a, new Footprint(5f, 2f, 1f, 0.5f, 0f)));
            // Berühren an der Kante ist erlaubt
            Assert.IsFalse(FurnitureLayout.Overlaps(a, new Footprint(4f, 2f, 1f, 0.5f, 0f)));
            // Langes Brett um 90° gedreht reicht in z-Richtung weit hinaus
            var plank = new Footprint(2f, 4f, 2f, 0.2f, 90f);
            Assert.IsTrue(FurnitureLayout.Overlaps(a, plank));
            Assert.IsFalse(FurnitureLayout.Overlaps(a, new Footprint(2f, 4f, 2f, 0.2f, 0f)));
            // 45° gedrehtes Quadrat: Ecken ragen hinaus
            var diamond = new Footprint(4.2f, 2f, 1f, 1f, 45f);
            Assert.IsTrue(FurnitureLayout.Overlaps(a, diamond));
        }

        [Test]
        public void Validate_ChecksRoomDoorsAndNeighbours()
        {
            var keep = new List<FloorRect> { new FloorRect(4f, 7f, 6f, 8f) };
            var others = new List<Footprint> { new Footprint(8f, 2f, 0.5f, 0.5f, 0f, "Packtisch") };
            Assert.IsNull(FurnitureLayout.Validate(new Footprint(2f, 2f, 0.5f, 0.5f, 0f), Room, others, keep));
            StringAssert.Contains("Gebäude", FurnitureLayout.Validate(new Footprint(9.8f, 2f, 0.5f, 0.5f, 0f), Room, others, keep));
            StringAssert.Contains("Tür", FurnitureLayout.Validate(new Footprint(5f, 6.8f, 0.5f, 0.5f, 0f), Room, others, keep));
            StringAssert.Contains("Packtisch", FurnitureLayout.Validate(new Footprint(7.5f, 2f, 0.5f, 0.5f, 0f), Room, others, keep));
            // Teppich darf unter dem Packtisch liegen
            Assert.IsNull(FurnitureLayout.Validate(new Footprint(7.5f, 2f, 1f, 1f, 0f, "Teppich", true), Room, others, keep));
            Assert.IsNotNull(FurnitureLayout.Validate(new Footprint(float.NaN, 2f, 1f, 1f, 0f), Room, others, keep));
        }

        [Test]
        public void Save_RoundTripsPlacementsAndOldSavesLoadWithout()
        {
            var sim = TestUtil.Fresh();
            sim.SetFurniture("st:0:Regal:1", -12.5f, -13.25f, -90f);
            sim.SetFurniture("deco:sofa:1", 28f, -12f, 180f);
            sim.SetFurniture("", 1f, 1f, 0f);
            sim.SetFurniture("bad", float.NaN, 1f, 0f);
            var text = Json.Write(sim.ToJson());
            Assert.IsTrue(Json.TryParse(text, out object o));

            var loaded = TestUtil.Fresh();
            loaded.FromJson((Dictionary<string, object>)o);
            Assert.AreEqual(2, loaded.Furniture.Count);
            var r = loaded.GetFurniture("st:0:Regal:1");
            Assert.IsNotNull(r);
            Assert.AreEqual(-12.5f, r.X, 1e-4f);
            Assert.AreEqual(-13.25f, r.Z, 1e-4f);
            Assert.AreEqual(270f, r.RotY, 1e-4f);

            // Alter Spielstand ohne "furniture": alles am Standardplatz, alte Einträge werden verworfen
            var state = sim.ToJson();
            state.Remove("furniture");
            loaded.FromJson(state);
            Assert.AreEqual(0, loaded.Furniture.Count);

            // Kaputte Einträge werden übersprungen
            state["furniture"] = new Dictionary<string, object> { { "x", "kaputt" }, { "st:0:Pack:0", new Dictionary<string, object> { { "x", 1.0 } } } };
            loaded.FromJson(state);
            Assert.AreEqual(0, loaded.Furniture.Count);
        }

        [Test]
        public void ResetState_ClearsPlacements()
        {
            var sim = TestUtil.Fresh();
            sim.SetFurniture("st:0:Pack:0", 1f, 1f, 0f);
            sim.ResetState();
            Assert.AreEqual(0, sim.Furniture.Count);
        }
    }
}
