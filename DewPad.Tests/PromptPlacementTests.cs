using DewPad.Core;

namespace DewPad.Tests
{
    public class PromptPlacementTests
    {
        private const int Strip = 62;
        private const int Margin = 16;
        private const int Gap = 6;

        [Fact]
        public void AMenuHighOnTheScreenLeavesTheStripAtTheBottom()
        {
            // 1440 high, the menu ends at 1060, so the strip sits at the bottom with the usual margin
            Assert.Equal(1440 - Strip - Margin, PromptLayout.PlaceBelowMenu(1440, Strip, 1060, Margin, Gap));
        }

        [Fact]
        public void WhenTheBottomMarginWouldTouchTheMenuTheStripCentresInTheSpaceLeft()
        {
            // 960 high, the menu ends at 880, 80 are free, the strip needs 62 plus the gaps, and the bottom row would be 882 which is not clear enough
            int? top = PromptLayout.PlaceBelowMenu(960, Strip, 880, Margin, Gap);
            Assert.Equal(880 + (80 - Strip) / 2, top);
            Assert.True(top + Strip <= 960);
            Assert.True(top >= 880 + 1);
        }

        [Fact]
        public void NoRoomMeansNoStrip()
        {
            Assert.Null(PromptLayout.PlaceBelowMenu(720, Strip, 700, Margin, Gap));
            Assert.Null(PromptLayout.PlaceBelowMenu(960, Strip, 890, Margin, Gap));
        }

        [Fact]
        public void ExactlyEnoughRoomIsEnough()
        {
            int free = Strip + 2 * Gap;
            Assert.NotNull(PromptLayout.PlaceBelowMenu(1000, Strip, 1000 - free, Margin, Gap));
            Assert.Null(PromptLayout.PlaceBelowMenu(1000, Strip, 1000 - free + 1, Margin, Gap));
        }

        [Fact]
        public void ThePlacedStripIsAlwaysOnScreenAndClearOfTheMenu()
        {
            for (int screen = 600; screen <= 1600; screen += 20)
            for (int bottom = 300; bottom <= screen; bottom += 25)
            {
                int? top = PromptLayout.PlaceBelowMenu(screen, Strip, bottom, Margin, Gap);
                if (top is null)
                    continue;
                Assert.True(top >= bottom + Gap, $"covers the menu at screen {screen}, menu bottom {bottom}");
                Assert.True(top + Strip <= screen, $"off screen at screen {screen}, menu bottom {bottom}");
            }
        }
    }
}
