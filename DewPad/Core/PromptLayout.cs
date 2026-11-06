namespace DewPad.Core
{
    /// <summary>The arithmetic of laying hints in a row. Pure, so the rules for what fits are tested without the game.</summary>
    public static class PromptLayout
    {
        /// <summary>How many of the hints, taken in order from the first, fit in <paramref name="maxWidth"/> with <paramref name="gap"/> between them.</summary>
        public static int CountThatFit(ReadOnlySpan<float> widths, float gap, float maxWidth)
        {
            float used = 0;
            for (int i = 0; i < widths.Length; i++)
            {
                float next = used + (i > 0 ? gap : 0) + widths[i];
                if (next > maxWidth)
                    return i;
                used = next;
            }
            return widths.Length;
        }

        /// <summary>
        /// The top row for a strip that must not cover the menu. It sits at the bottom of the screen when that is clear of the menu, otherwise in the
        /// middle of whatever space is left under the menu, and nowhere (null) when the space under the menu is too small. A hint that covers part
        /// of a menu is worse than no hint.
        /// </summary>
        public static int? PlaceBelowMenu(int screenHeight, int stripHeight, int menuBottom, int edgeMargin, int minGap)
        {
            int free = screenHeight - menuBottom;
            if (free < stripHeight + 2 * minGap)
                return null;

            int atBottom = screenHeight - stripHeight - edgeMargin;
            if (atBottom >= menuBottom + minGap)
                return atBottom;
            return menuBottom + (free - stripHeight) / 2;
        }

        /// <summary>The width of the first <paramref name="count"/> hints in a row.</summary>
        public static float RowWidth(ReadOnlySpan<float> widths, int count, float gap)
        {
            float total = 0;
            for (int i = 0; i < count && i < widths.Length; i++)
                total += widths[i] + (i > 0 ? gap : 0);
            return total;
        }
    }
}
