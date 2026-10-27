using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Tutorial-Leiste im Laptop: Das HUD ist hinter dem Laptop ausgeblendet, deshalb zeigt eine schmale
    /// Leiste über dem Dock den aktuellen Tutorial-Schritt (und wann man den Laptop wieder zuklappt).
    /// </summary>
    public sealed partial class LaptopView
    {
        private VisualElement _coach;
        private Label _coachEyebrow, _coachText;
        private string _coachSig = "";

        private void UpdateCoach()
        {
            if (_screen == null) return;
            var sim = Game.Sim;
            int step = sim != null ? sim.TutorialStep : -1;
            bool show = sim != null && sim.StoryStage == "business" && step >= 0 && step < GameData.Tutorial.Length;
            string sig = show ? step + "|" + GameInput.UsingGamepad + "|" + Lang.Current : "";
            if (sig == _coachSig && (_coach == null || _coach.parent == _screen)) return;
            _coachSig = sig;
            try
            {
                if (_coach == null || _coach.parent != _screen)
                {
                    _coach?.RemoveFromHierarchy();
                    _coach = new VisualElement { pickingMode = PickingMode.Ignore };
                    var st = _coach.style;
                    st.position = Position.Absolute;
                    st.left = 24;
                    st.right = 24;
                    st.bottom = 96;
                    st.flexDirection = FlexDirection.Row;
                    st.alignItems = Align.Center;
                    st.backgroundColor = new Color(1f, 0.74f, 0.26f, 0.96f);
                    st.borderTopLeftRadius = 12;
                    st.borderTopRightRadius = 12;
                    st.borderBottomLeftRadius = 12;
                    st.borderBottomRightRadius = 12;
                    st.paddingLeft = 14;
                    st.paddingRight = 14;
                    st.paddingTop = 8;
                    st.paddingBottom = 8;
                    var ink = new Color(0.11f, 0.08f, 0.02f);
                    UIX.Icon(_coach, "flag", 18f, ink).style.marginRight = 10;
                    var col = new VisualElement { pickingMode = PickingMode.Ignore };
                    col.style.flexShrink = 1;
                    col.style.flexGrow = 1;
                    _coach.Add(col);
                    _coachEyebrow = UIX.Text(col, "", "eyebrow");
                    _coachEyebrow.style.color = ink;
                    _coachText = UIX.Text(col, "");
                    _coachText.style.color = ink;
                    _coachText.style.whiteSpace = WhiteSpace.Normal;
                    _coachText.style.fontSize = 14;
                    foreach (var el in _coach.Query<VisualElement>().ToList()) el.pickingMode = PickingMode.Ignore;
                    _screen.Add(_coach);
                }
                UIX.Show(_coach, show);
                if (!show) return;
                _coach.BringToFront();
                var t = GameData.Tutorial[step];
                _coachEyebrow.text = "TUTORIAL · SCHRITT " + (step + 1) + "/" + GameData.Tutorial.Length + " · " + Lang.T(t.Title).ToUpperInvariant();
                bool laptopStep = step == 0 || step == 1 || step == 4;
                string close = GameInput.UsingGamepad ? "B" : GameInput.KeyboardLabel("back");
                _coachText.text = laptopStep ? GameData.TutorialText(step) : "Klapp den Laptop zu (" + close + ") – " + GameData.TutorialText(step);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
