using DewPad.Core;

namespace DewPad.Tests
{
    public class PromptLayoutTests
    {
        [Fact]
        public void EverythingFitsWhenThereIsRoom() => Assert.Equal(3, PromptLayout.CountThatFit(new float[] { 100, 100, 100 }, 10, 400));

        [Fact]
        public void LaterHintsAreDroppedFirst() => Assert.Equal(2, PromptLayout.CountThatFit(new float[] { 100, 100, 100 }, 10, 250));

        [Fact]
        public void TheGapCountsBetweenHintsOnly()
        {
            Assert.Equal(2, PromptLayout.CountThatFit(new float[] { 100, 100 }, 10, 210));
            Assert.Equal(1, PromptLayout.CountThatFit(new float[] { 100, 100 }, 10, 209));
        }

        [Fact]
        public void NothingFitsInNoSpace()
        {
            Assert.Equal(0, PromptLayout.CountThatFit(new float[] { 100 }, 10, 50));
            Assert.Equal(0, PromptLayout.CountThatFit(ReadOnlySpan<float>.Empty, 10, 50));
        }

        [Fact]
        public void RowWidthAddsTheGaps()
        {
            Assert.Equal(220f, PromptLayout.RowWidth(new float[] { 100, 100, 100 }, 2, 20));
            Assert.Equal(0f, PromptLayout.RowWidth(new float[] { 100 }, 0, 20));
        }
    }
}
