using System;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Synthetisierte Feedback-Sounds („Juice“): vorhanden, hörbar, nicht übersteuert, kurz.</summary>
    public class FeelAudioTests
    {
        private static readonly string[] FeelSounds =
        {
            "tape_rip", "print_whirr", "locker", "coin_cascade", "ding", "pop", "popper", "perk", "stick", "sparkle",
        };

        [Test]
        public void FeelSounds_AreRegisteredAndRender()
        {
            foreach (var n in FeelSounds)
            {
                Assert.Contains(n, AudioSynth.SfxNames, "Sound registriert: " + n);
                var b = AudioSynth.Render(n, false);
                Assert.IsTrue(b.Length >= AudioSynth.Rate / 20, "Mindestens 50 ms: " + n);
                Assert.IsTrue(b.Length <= AudioSynth.Rate * 1.5f, "Höchstens 1,5 s: " + n);
                float peak = 0f;
                foreach (float v in b) peak = Math.Max(peak, Math.Abs(v));
                TestContext.WriteLine(n + ": Spitze " + peak.ToString("0.00"));
                Assert.IsTrue(peak > 0.05f && peak <= 1f, "Pegel im Rahmen (" + peak + "): " + n);
            }
        }

        [Test]
        public void FeelSounds_AreDeterministic()
        {
            var a = AudioSynth.Render("coin_cascade", false);
            var b = AudioSynth.Render("coin_cascade", false);
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i += 97) Assert.AreEqual(a[i], b[i]);
        }
    }
}
