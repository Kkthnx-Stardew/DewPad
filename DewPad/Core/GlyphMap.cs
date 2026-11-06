namespace DewPad.Core
{
    /// <summary>
    /// One button symbol. Face, Back and Start buttons are glyphs the game already has in its controller sheet, named here by column in that
    /// sheet's row of six. Shoulder and trigger buttons have no glyph in the sheet, so they are drawn as a dark pill with a label in the game's font.
    /// </summary>
    public readonly record struct Glyph(int SheetColumn, string Label)
    {
        public bool IsSheetGlyph => SheetColumn >= 0;

        public static Glyph FromSheet(int column) => new(column, "");

        public static Glyph Pill(string label) => new(-1, label);
    }

    /// <summary>
    /// Which symbol draws a button in a style. The six columns of the game's glyph row are, in every layout, the pad's left, bottom, right and top
    /// face buttons, then Back, then Start, which is the order of XInput's X, A, B, Y. The Switch row is the exception, its art labels each position
    /// with the Switch name, so its columns read Y, B, A, X, minus, plus.
    /// </summary>
    public static class GlyphMap
    {
        public const int ColumnX = 0;
        public const int ColumnA = 1;
        public const int ColumnB = 2;
        public const int ColumnY = 3;
        public const int ColumnBack = 4;
        public const int ColumnStart = 5;

        public static PromptStyle Resolve(PromptStyle style) => style == PromptStyle.Auto ? PromptStyle.Xbox : style;

        public static Glyph For(PromptStyle style, GamepadButton button, SwitchFaceLayout layout = SwitchFaceLayout.GameArt)
        {
            style = Resolve(style);
            return button switch
            {
                GamepadButton.A => Glyph.FromSheet(FaceColumn(style, GamepadButton.A, layout)),
                GamepadButton.B => Glyph.FromSheet(FaceColumn(style, GamepadButton.B, layout)),
                GamepadButton.X => Glyph.FromSheet(FaceColumn(style, GamepadButton.X, layout)),
                GamepadButton.Y => Glyph.FromSheet(FaceColumn(style, GamepadButton.Y, layout)),
                GamepadButton.Back => Glyph.FromSheet(ColumnBack),
                GamepadButton.Start => Glyph.FromSheet(ColumnStart),
                GamepadButton.LeftBumper => Glyph.Pill(Shoulder(style, left: true, trigger: false)),
                GamepadButton.RightBumper => Glyph.Pill(Shoulder(style, left: false, trigger: false)),
                GamepadButton.LeftTrigger => Glyph.Pill(Shoulder(style, left: true, trigger: true)),
                _ => Glyph.Pill(Shoulder(style, left: false, trigger: true))
            };
        }

        private static int FaceColumn(PromptStyle style, GamepadButton face, SwitchFaceLayout layout)
        {
            // the sheet's columns for the four face buttons, by the position they sit in on the pad
            int position = face switch
            {
                GamepadButton.X => ColumnX,
                GamepadButton.A => ColumnA,
                GamepadButton.B => ColumnB,
                _ => ColumnY
            };

            // in the Circle to confirm layout the circle, on the right, is Check and the cross, at the bottom, is Cancel
            if (style == PromptStyle.PlayStationCircleConfirm && face == GamepadButton.A)
                return ColumnB;
            if (style == PromptStyle.PlayStationCircleConfirm && face == GamepadButton.B)
                return ColumnA;

            // the game's Switch art shows Check as the Switch A, which sits on the right, so A and B trade places unless the labels follow the positions
            if (style == PromptStyle.Switch && layout == SwitchFaceLayout.GameArt && face == GamepadButton.A)
                return ColumnB;
            if (style == PromptStyle.Switch && layout == SwitchFaceLayout.GameArt && face == GamepadButton.B)
                return ColumnA;

            return position;
        }

        private static string Shoulder(PromptStyle style, bool left, bool trigger) => style switch
        {
            PromptStyle.PlayStation or PromptStyle.PlayStationCircleConfirm or PromptStyle.SteamDeck => trigger ? (left ? "L2" : "R2") : (left ? "L1" : "R1"),
            PromptStyle.Switch => trigger ? (left ? "ZL" : "ZR") : (left ? "L" : "R"),
            _ => trigger ? (left ? "LT" : "RT") : (left ? "LB" : "RB")
        };
    }
}
