namespace DewPad.Core
{
    /// <summary>
    /// The game's controller sheet (LooseSprites/ControllerMaps) is four bands of the same screens, one per layout, stacked from the top. The
    /// game only ever draws the first band, the Xbox one. Showing another band's pixels in the first is what makes the game's own tutorial
    /// screens, fishing and arcade help and button icons show that layout, in the game's own art.
    /// </summary>
    public static class SheetBands
    {
        public const int SheetWidth = 841;
        public const int SheetHeight = 1152;
        public const int BandCount = 4;
        public const int BandHeight = SheetHeight / BandCount;

        // Each band has a row of six button glyphs, 28 pixels square, which the game's own menus draw from the first band (the emote menu, the
        // local co-op join menu). The offsets are the ones those menus use: the emote menu draws the fifth glyph at x 625, y 260.
        public const int GlyphSize = 28;
        public const int GlyphRowX = 513;
        public const int GlyphRowY = 260;
        public const int GlyphColumns = 6;

        public static int GlyphX(int column) => GlyphRowX + column * GlyphSize;

        public static int GlyphY(int band) => GlyphRowY + band * BandHeight;

        /// <summary>The band that holds the art for a style, counted from 0 at the top. Xbox is band 0, the one the game already draws.</summary>
        public static int BandFor(PromptStyle style) => GlyphMap.Resolve(style) switch
        {
            PromptStyle.PlayStation => 1,
            PromptStyle.Switch => 2,
            PromptStyle.PlayStationCircleConfirm => 3,
            _ => 0
        };

        /// <summary>The sheet is only patched when it is exactly the size this layout was read from, so a mod that changed the sheet is left alone.</summary>
        public static bool IsExpectedSize(int width, int height) => width == SheetWidth && height == SheetHeight;
    }
}
