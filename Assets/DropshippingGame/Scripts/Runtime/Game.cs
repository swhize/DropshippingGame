using DropshippingGame.Core;
using DropshippingGame.UI;

namespace DropshippingGame
{
    /// <summary>Zentraler Zugriff auf die laufenden Systeme (wie die Autoloads in Godot).</summary>
    public static class Game
    {
        public static Sim Sim;
        public static GameRoot Root;
        public static AudioManager Audio;
        public static WorldBuilder World;
        public static PlayerController Player;
        public static UIRoot UI;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Sim = null;
            Root = null;
            Audio = null;
            World = null;
            Player = null;
            UI = null;
        }

        public static void Sound(string name, float pitchVar = 0.06f, float volumeDb = 0f)
        {
            if (Audio != null) Audio.Play(name, pitchVar, volumeDb);
        }

        public static void Notify(string text, string kind = "info") => Sim?.Notify(text, kind);
    }

    /// <summary>Alles, was der Spieler mit dem Fadenkreuz anvisieren und benutzen kann.</summary>
    public interface IInteractable
    {
        string Title { get; }
        string Prompt(PlayerController player);
        void Interact(PlayerController player);
        void SetHighlighted(bool on);
    }
}
