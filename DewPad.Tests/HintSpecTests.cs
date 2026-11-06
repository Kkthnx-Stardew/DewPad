using DewPad.Core;

namespace DewPad.Tests
{
    public class HintSpecTests
    {
        [Fact]
        public void ASingleButtonHintIsRead()
        {
            Assert.True(HintSpec.TryParse("A=Select", out var item));
            Assert.Equal(GamepadButton.A, item.First);
            Assert.Null(item.Second);
            Assert.Equal("Select", item.LabelKey);
            Assert.True(item.IsLiteral);
        }

        [Fact]
        public void APairIsRead()
        {
            Assert.True(HintSpec.TryParse("LeftTrigger,RightTrigger=Switch page", out var item));
            Assert.Equal(GamepadButton.LeftTrigger, item.First);
            Assert.Equal(GamepadButton.RightTrigger, item.Second);
            Assert.Equal("Switch page", item.LabelKey);
        }

        [Fact]
        public void ButtonNamesIgnoreCaseAndSpaces()
        {
            Assert.True(HintSpec.TryParse(" lefttrigger , RIGHTTRIGGER = Next ", out var item));
            Assert.Equal(GamepadButton.RightTrigger, item.Second);
            Assert.Equal("Next", item.LabelKey);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("A")]
        [InlineData("A=")]
        [InlineData("=Select")]
        [InlineData("Bogus=Select")]
        [InlineData("A,Bogus=Select")]
        [InlineData("A,A=Select")]
        [InlineData("A,B,X=Select")]
        [InlineData("42=Select")]
        [InlineData("A=   ")]
        public void BadHintsAreRejected(string? spec) => Assert.False(HintSpec.TryParse(spec, out _));

        [Fact]
        public void TheTextHasALimit()
        {
            Assert.True(HintSpec.TryParse("A=" + new string('x', HintSpec.MaxTextLength), out _));
            Assert.False(HintSpec.TryParse("A=" + new string('x', HintSpec.MaxTextLength + 1), out _));
        }

        [Fact]
        public void ParseAllSkipsBadAndRepeatedButtonsAndKeepsTheOrder()
        {
            var items = HintSpec.ParseAll(new[] { "A=Select", "Bogus=No", "B=Back", "A=Again", "LeftTrigger,B=Clash", "Y=More" }, out int skipped);
            Assert.Equal(new[] { "Select", "Back", "More" }, items.Select(i => i.LabelKey));
            Assert.Equal(3, skipped);
        }

        [Fact]
        public void ParseAllStopsAtTheLimit()
        {
            var specs = new[] { "A=1", "B=2", "X=3", "Y=4", "LeftBumper=5", "RightBumper=6", "LeftTrigger=7", "RightTrigger=8", "Start=9", "Back=10" };
            var items = HintSpec.ParseAll(specs, out int skipped);
            Assert.Equal(HintSpec.MaxHints, items.Length);
            Assert.Equal(2, skipped);
        }

        [Fact]
        public void NothingGivesNothing()
        {
            Assert.Empty(HintSpec.ParseAll(null, out int skipped));
            Assert.Equal(0, skipped);
        }

        [Fact]
        public void ARegisteredMenuShowsExactlyItsHints()
        {
            var items = HintSpec.ParseAll(new[] { "A=Pick", "B=Leave" }, out _);
            var plan = PromptPlanner.Plan(new MenuFacts(MenuKind.Registered, true, true), items);
            Assert.Equal(items, plan);
            Assert.Empty(PromptPlanner.Plan(new MenuFacts(MenuKind.Registered, true, false), null));
        }
    }
}