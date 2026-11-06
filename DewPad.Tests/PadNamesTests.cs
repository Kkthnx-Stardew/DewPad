using DewPad.Core;

namespace DewPad.Tests
{
    public class PadNamesTests
    {
        [Theory]
        [InlineData("PS5 Controller")]
        [InlineData("PS4 Controller")]
        [InlineData("DualSense Wireless Controller")]
        [InlineData("DUALSHOCK 4 Wireless Controller")]
        [InlineData("Sony Interactive Entertainment Wireless Controller")]
        [InlineData("PlayStation 5 Controller")]
        public void PlayStationNamesGivePlayStation(string name) => Assert.Equal(PromptStyle.PlayStation, PadNames.Classify(name));

        [Theory]
        [InlineData("Nintendo Switch Pro Controller")]
        [InlineData("Switch Pro Controller")]
        [InlineData("Nintendo Switch Joy-Con (L/R)")]
        [InlineData("Joy-Con (R)")]
        public void SwitchNamesGiveSwitch(string name) => Assert.Equal(PromptStyle.Switch, PadNames.Classify(name));

        [Theory]
        [InlineData("Xbox 360 Controller")]
        [InlineData("Xbox One Controller")]
        [InlineData("Xbox Series X Controller")]
        [InlineData("XInput Controller")]
        [InlineData("Steam Virtual Gamepad")]
        [InlineData("Steam Deck")]
        [InlineData("Logitech F310 Gamepad")]
        [InlineData("8BitDo Pro 2")]
        public void EverythingElseGivesXbox(string name) => Assert.Equal(PromptStyle.Xbox, PadNames.Classify(name));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NoNameGivesXbox(string? name) => Assert.Equal(PromptStyle.Xbox, PadNames.Classify(name));

        [Fact]
        public void TheAnswerIsNeverAuto()
        {
            foreach (var name in new[] { "PS5 Controller", "Switch Pro Controller", "Xbox One Controller", "", "Mystery" })
                Assert.NotEqual(PromptStyle.Auto, PadNames.Classify(name));
        }
    }
}
