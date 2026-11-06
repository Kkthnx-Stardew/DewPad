using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DewPad
{
    /// <summary>What other mods and test tools can ask of DewPad through <c>IModRegistry.GetApi</c>. Read only.</summary>
    public interface IDewPadApi
    {
        /// <summary>Draws the hint strip for the open menu where the game would draw it. Draws nothing when no hints apply.</summary>
        void DrawHints(SpriteBatch spriteBatch);

        /// <summary>Where the hint strip was last drawn, in UI pixels. Empty when nothing is shown.</summary>
        Rectangle HintBounds { get; }

        /// <summary>How many hints are in the strip now.</summary>
        int HintCount { get; }

        /// <summary>
        /// Shows hints for a menu class of your mod, in the same strip, glyphs and placement as the game's own menus. <paramref name="menuTypeFullName"/>
        /// is the full name of the exact class, so a derived class needs its own call. Each hint is a button name, or two joined by a comma, an equals
        /// sign and the words, such as <c>A=Select</c> or <c>LeftTrigger,RightTrigger=Switch page</c>. Button names are A, B, X, Y, LeftBumper,
        /// RightBumper, LeftTrigger, RightTrigger, Start and Back. Up to 8 hints, no button twice, 40 characters of text each, and bad ones are skipped
        /// with a warning in the log. The words are shown as given, so translate them first. Returns false when nothing valid was registered.
        /// Only register a button that really does that in your menu. DewPad only draws the hint, it cannot check it.
        /// </summary>
        bool RegisterMenu(string menuTypeFullName, string[] hints);

        /// <summary>Removes the hints registered for a menu class.</summary>
        void UnregisterMenu(string menuTypeFullName);

        /// <summary>The text of each hint in the strip, in order. Allocates, so it is for tests and diagnostics, not for every frame.</summary>
        string[] HintLabels { get; }
    }

    public sealed class DewPadApi : IDewPadApi
    {
        private readonly ModEntry _mod;
        private readonly string _consumer;

        internal DewPadApi(ModEntry mod, string consumer)
        {
            _mod = mod;
            _consumer = consumer;
        }

        public void DrawHints(SpriteBatch spriteBatch) => _mod.DrawHints(spriteBatch);

        public Rectangle HintBounds => _mod.HintBounds;

        public bool RegisterMenu(string menuTypeFullName, string[] hints) => _mod.RegisterMenu(menuTypeFullName, hints, _consumer);

        public void UnregisterMenu(string menuTypeFullName) => _mod.UnregisterMenu(menuTypeFullName);

        public int HintCount => _mod.HintCount;

        public string[] HintLabels => _mod.HintLabels;
    }
}
