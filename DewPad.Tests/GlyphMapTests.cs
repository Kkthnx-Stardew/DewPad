using DewPad.Core;

namespace DewPad.Tests
{
    public class GlyphMapTests
    {
        private static readonly PromptStyle[] Styles =
            { PromptStyle.Xbox, PromptStyle.PlayStation, PromptStyle.PlayStationCircleConfirm, PromptStyle.Switch, PromptStyle.SteamDeck };

        private static int Column(PromptStyle style, GamepadButton button, SwitchFaceLayout layout = SwitchFaceLayout.GameArt)
        {
            var glyph = GlyphMap.For(style, button, layout);
            Assert.True(glyph.IsSheetGlyph, $"{style} {button} should come from the sheet");
            return glyph.SheetColumn;
        }

        [Fact]
        public void XboxFaceButtonsAreInTheSheetsOwnOrder()
        {
            Assert.Equal(GlyphMap.ColumnX, Column(PromptStyle.Xbox, GamepadButton.X));
            Assert.Equal(GlyphMap.ColumnA, Column(PromptStyle.Xbox, GamepadButton.A));
            Assert.Equal(GlyphMap.ColumnB, Column(PromptStyle.Xbox, GamepadButton.B));
            Assert.Equal(GlyphMap.ColumnY, Column(PromptStyle.Xbox, GamepadButton.Y));
        }

        [Fact]
        public void AutoIsXbox()
        {
            foreach (var button in Enum.GetValues<GamepadButton>())
                Assert.Equal(GlyphMap.For(PromptStyle.Xbox, button), GlyphMap.For(PromptStyle.Auto, button));
        }

        [Fact]
        public void PlayStationKeepsEachButtonInPlace()
        {
            // square on the left, cross at the bottom, circle on the right, triangle on top, the same positions as the Xbox buttons
            foreach (var button in new[] { GamepadButton.X, GamepadButton.A, GamepadButton.B, GamepadButton.Y })
                Assert.Equal(Column(PromptStyle.Xbox, button), Column(PromptStyle.PlayStation, button));
        }

        [Fact]
        public void CircleConfirmSwapsTheCrossAndTheCircleOnly()
        {
            Assert.Equal(GlyphMap.ColumnB, Column(PromptStyle.PlayStationCircleConfirm, GamepadButton.A));
            Assert.Equal(GlyphMap.ColumnA, Column(PromptStyle.PlayStationCircleConfirm, GamepadButton.B));
            Assert.Equal(GlyphMap.ColumnX, Column(PromptStyle.PlayStationCircleConfirm, GamepadButton.X));
            Assert.Equal(GlyphMap.ColumnY, Column(PromptStyle.PlayStationCircleConfirm, GamepadButton.Y));
        }

        [Fact]
        public void SwitchFollowsTheGameArtOrThePosition()
        {
            // the Switch row reads Y, B, A, X by position. The game's art makes Check the Switch A, which is the third glyph
            Assert.Equal(GlyphMap.ColumnB, Column(PromptStyle.Switch, GamepadButton.A, SwitchFaceLayout.GameArt));
            Assert.Equal(GlyphMap.ColumnA, Column(PromptStyle.Switch, GamepadButton.B, SwitchFaceLayout.GameArt));
            // by position the bottom button is the Switch B, the second glyph
            Assert.Equal(GlyphMap.ColumnA, Column(PromptStyle.Switch, GamepadButton.A, SwitchFaceLayout.Position));
            Assert.Equal(GlyphMap.ColumnB, Column(PromptStyle.Switch, GamepadButton.B, SwitchFaceLayout.Position));
            // left and top are the Y and the X either way
            foreach (var layout in Enum.GetValues<SwitchFaceLayout>())
            {
                Assert.Equal(GlyphMap.ColumnX, Column(PromptStyle.Switch, GamepadButton.X, layout));
                Assert.Equal(GlyphMap.ColumnY, Column(PromptStyle.Switch, GamepadButton.Y, layout));
            }
        }

        [Fact]
        public void TheSteamDeckHasTheXboxFaceButtonsAndItsOwnShoulderLabels()
        {
            foreach (var button in new[] { GamepadButton.X, GamepadButton.A, GamepadButton.B, GamepadButton.Y, GamepadButton.Back, GamepadButton.Start })
                Assert.Equal(GlyphMap.For(PromptStyle.Xbox, button), GlyphMap.For(PromptStyle.SteamDeck, button));
            Assert.Equal("L1", GlyphMap.For(PromptStyle.SteamDeck, GamepadButton.LeftBumper).Label);
            Assert.Equal("R1", GlyphMap.For(PromptStyle.SteamDeck, GamepadButton.RightBumper).Label);
            Assert.Equal("L2", GlyphMap.For(PromptStyle.SteamDeck, GamepadButton.LeftTrigger).Label);
            Assert.Equal("R2", GlyphMap.For(PromptStyle.SteamDeck, GamepadButton.RightTrigger).Label);
        }

        [Fact]
        public void BackAndStartAreTheLastTwoGlyphsInEveryStyle()
        {
            foreach (var style in Styles)
            {
                Assert.Equal(GlyphMap.ColumnBack, Column(style, GamepadButton.Back));
                Assert.Equal(GlyphMap.ColumnStart, Column(style, GamepadButton.Start));
            }
        }

        [Fact]
        public void ShouldersAndTriggersAreLabelledPillsInTheirOwnFamilyNames()
        {
            Assert.Equal("LB", GlyphMap.For(PromptStyle.Xbox, GamepadButton.LeftBumper).Label);
            Assert.Equal("RT", GlyphMap.For(PromptStyle.Xbox, GamepadButton.RightTrigger).Label);
            Assert.Equal("L1", GlyphMap.For(PromptStyle.PlayStation, GamepadButton.LeftBumper).Label);
            Assert.Equal("R2", GlyphMap.For(PromptStyle.PlayStationCircleConfirm, GamepadButton.RightTrigger).Label);
            Assert.Equal("L", GlyphMap.For(PromptStyle.Switch, GamepadButton.LeftBumper).Label);
            Assert.Equal("ZR", GlyphMap.For(PromptStyle.Switch, GamepadButton.RightTrigger).Label);
            foreach (var style in Styles)
            foreach (var button in new[] { GamepadButton.LeftBumper, GamepadButton.RightBumper, GamepadButton.LeftTrigger, GamepadButton.RightTrigger })
            {
                var glyph = GlyphMap.For(style, button);
                Assert.False(glyph.IsSheetGlyph);
                Assert.NotEqual("", glyph.Label);
            }
        }

        [Fact]
        public void EveryButtonInEveryStyleHasADistinctSymbol()
        {
            foreach (var style in Styles)
            foreach (var layout in Enum.GetValues<SwitchFaceLayout>())
            {
                var glyphs = Enum.GetValues<GamepadButton>().Select(b => GlyphMap.For(style, b, layout)).ToList();
                Assert.Equal(glyphs.Count, glyphs.Distinct().Count());
            }
        }

        [Fact]
        public void EverySheetGlyphLiesInsideItsBand()
        {
            foreach (var style in Styles)
            foreach (var layout in Enum.GetValues<SwitchFaceLayout>())
            foreach (var button in Enum.GetValues<GamepadButton>())
            {
                var glyph = GlyphMap.For(style, button, layout);
                if (!glyph.IsSheetGlyph)
                    continue;
                int band = SheetBands.BandFor(style);
                Assert.InRange(glyph.SheetColumn, 0, SheetBands.GlyphColumns - 1);
                Assert.True(SheetBands.GlyphX(glyph.SheetColumn) + SheetBands.GlyphSize <= SheetBands.SheetWidth);
                Assert.True(SheetBands.GlyphY(band) >= band * SheetBands.BandHeight);
                Assert.True(SheetBands.GlyphY(band) + SheetBands.GlyphSize <= (band + 1) * SheetBands.BandHeight);
            }
        }
    }
}
