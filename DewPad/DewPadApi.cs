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
    }

    public sealed class DewPadApi : IDewPadApi
    {
        private readonly ModEntry _mod;

        internal DewPadApi(ModEntry mod) => _mod = mod;

        public void DrawHints(SpriteBatch spriteBatch) => _mod.DrawHints(spriteBatch);

        public Rectangle HintBounds => _mod.HintBounds;

        public int HintCount => _mod.HintCount;
    }
}
