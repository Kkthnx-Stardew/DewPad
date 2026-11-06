using DewPad.Core;

namespace DewPad.Tests
{
    public class SheetBandsTests
    {
        [Theory]
        [InlineData(PromptStyle.Auto, 0)]
        [InlineData(PromptStyle.Xbox, 0)]
        [InlineData(PromptStyle.PlayStation, 1)]
        [InlineData(PromptStyle.Switch, 2)]
        [InlineData(PromptStyle.PlayStationCircleConfirm, 3)]
        public void EachStyleHasItsBand(PromptStyle style, int band) => Assert.Equal(band, SheetBands.BandFor(style));

        [Fact]
        public void EveryStyleHasADifferentBandExceptAutoAndXbox()
        {
            var bands = new[] { PromptStyle.Xbox, PromptStyle.PlayStation, PromptStyle.Switch, PromptStyle.PlayStationCircleConfirm }.Select(SheetBands.BandFor).ToList();
            Assert.Equal(bands.Count, bands.Distinct().Count());
        }

        [Fact]
        public void TheBandsFillTheSheetExactly()
        {
            Assert.Equal(SheetBands.SheetHeight, SheetBands.BandHeight * SheetBands.BandCount);
            foreach (var style in Enum.GetValues<PromptStyle>())
                Assert.InRange(SheetBands.BandFor(style), 0, SheetBands.BandCount - 1);
        }

        [Fact]
        public void OnlyTheExpectedSheetSizeIsPatched()
        {
            Assert.True(SheetBands.IsExpectedSize(841, 1152));
            Assert.False(SheetBands.IsExpectedSize(841, 288));
            Assert.False(SheetBands.IsExpectedSize(1682, 2304));
        }
    }
}
