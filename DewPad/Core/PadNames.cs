namespace DewPad.Core
{
    /// <summary>
    /// Picks a layout from the name SDL gives a connected pad. The game runs on MonoGame's SDL backend, where <c>GamePadCapabilities.DisplayName</c>
    /// is SDL's own controller name. A pad that Steam Input or a driver presents as an Xbox pad is named like one, and then the answer is Xbox,
    /// because nothing reveals what is behind it. Only a name that plainly says PlayStation or Nintendo changes the answer, so a wrong guess
    /// needs a pad that names itself wrongly, and the setting can always be fixed to a layout.
    /// </summary>
    public static class PadNames
    {
        private static readonly string[] PlayStationWords = { "ps5", "ps4", "ps3", "dualsense", "dualshock", "playstation", "sony" };
        private static readonly string[] SwitchWords = { "switch", "nintendo", "joy-con", "joycon" };

        public static PromptStyle Classify(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return PromptStyle.Xbox;

            if (ContainsAny(displayName, PlayStationWords))
                return PromptStyle.PlayStation;
            if (ContainsAny(displayName, SwitchWords))
                return PromptStyle.Switch;
            return PromptStyle.Xbox;
        }

        /// <summary>
        /// The style for Auto. A pad that names itself PlayStation or Nintendo wins, even on a Steam Deck with one plugged in. Otherwise a Steam Deck
        /// gets its own style, and everything else is Xbox.
        /// </summary>
        public static PromptStyle Choose(string? displayName, bool onSteamDeck)
        {
            var named = Classify(displayName);
            return named == PromptStyle.Xbox && onSteamDeck ? PromptStyle.SteamDeck : named;
        }

        private static bool ContainsAny(string name, string[] words)
        {
            foreach (var word in words)
            {
                if (name.Contains(word, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
