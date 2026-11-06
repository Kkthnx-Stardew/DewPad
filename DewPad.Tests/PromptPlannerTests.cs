using System.Text.Json;
using DewPad.Core;

namespace DewPad.Tests
{
    public class PromptPlannerTests
    {
        private static MenuFacts Facts(MenuKind kind, bool canClose = true, bool canOrganize = false) => new(kind, canClose, canOrganize);

        [Fact]
        public void AnUnknownMenuShowsNothing() => Assert.Empty(PromptPlanner.Plan(Facts(MenuKind.Unknown)));

        [Fact]
        public void TheGameMenuChangesTabsWithTheTriggersAndNotTheBumpers()
        {
            var plan = PromptPlanner.Plan(Facts(MenuKind.GameMenu));
            var tabs = Assert.Single(plan, p => p.LabelKey == PromptPlanner.SwitchTab);
            Assert.Equal(GamepadButton.LeftTrigger, tabs.First);
            Assert.Equal(GamepadButton.RightTrigger, tabs.Second);
            Assert.DoesNotContain(plan, p => p.First is GamepadButton.LeftBumper or GamepadButton.RightBumper);
        }

        [Fact]
        public void ACloseHintOnlyShowsWhenTheMenuCanClose()
        {
            Assert.Contains(PromptPlanner.Plan(Facts(MenuKind.Chest, canClose: true)), p => p.LabelKey == PromptPlanner.Close);
            Assert.DoesNotContain(PromptPlanner.Plan(Facts(MenuKind.Chest, canClose: false)), p => p.LabelKey == PromptPlanner.Close);
        }

        [Fact]
        public void AChestJumpsBetweenTheBackpackAndTheChestWithTheBumpers()
        {
            var plan = PromptPlanner.Plan(Facts(MenuKind.Chest));
            Assert.Contains(plan, p => p.First == GamepadButton.LeftBumper && p.LabelKey == PromptPlanner.ToBackpack);
            Assert.Contains(plan, p => p.First == GamepadButton.RightBumper && p.LabelKey == PromptPlanner.ToChest);
        }

        [Fact]
        public void AnItemGrabMenuThatIsNotAChestHasNoBumperJumps()
        {
            var plan = PromptPlanner.Plan(Facts(MenuKind.OtherItemGrab));
            Assert.DoesNotContain(plan, p => p.First is GamepadButton.LeftBumper or GamepadButton.RightBumper);
        }

        [Fact]
        public void AShopSwitchesBetweenTheShopAndTheBackpackWithTheTriggers()
        {
            var item = Assert.Single(PromptPlanner.Plan(Facts(MenuKind.Shop)), p => p.LabelKey == PromptPlanner.ShopOrBackpack);
            Assert.Equal(GamepadButton.LeftTrigger, item.First);
            Assert.Equal(GamepadButton.RightTrigger, item.Second);
        }

        [Theory]
        [InlineData(MenuKind.GameMenu)]
        [InlineData(MenuKind.Chest)]
        [InlineData(MenuKind.OtherItemGrab)]
        [InlineData(MenuKind.Shop)]
        public void OrganizeShowsOnlyWhereTheMenuHasIt(MenuKind kind)
        {
            Assert.DoesNotContain(PromptPlanner.Plan(Facts(kind, canOrganize: false)), p => p.LabelKey == PromptPlanner.Organize);
            var organize = Assert.Single(PromptPlanner.Plan(Facts(kind, canOrganize: true)), p => p.LabelKey == PromptPlanner.Organize);
            Assert.Equal(GamepadButton.Back, organize.First);
        }

        [Theory]
        [InlineData(MenuKind.GameMenu)]
        [InlineData(MenuKind.Chest)]
        [InlineData(MenuKind.OtherItemGrab)]
        [InlineData(MenuKind.Shop)]
        public void SelectComesFirstAndNoButtonIsUsedTwice(MenuKind kind)
        {
            var plan = PromptPlanner.Plan(Facts(kind, canClose: true, canOrganize: true));
            Assert.Equal(PromptPlanner.Select, plan[0].LabelKey);
            var buttons = plan.SelectMany(p => p.Second is null ? new[] { p.First } : new[] { p.First, p.Second.Value }).ToList();
            Assert.Equal(buttons.Count, buttons.Distinct().Count());
        }

        [Fact]
        public void TheSameFactsGiveTheSameHints()
        {
            Assert.Equal(PromptPlanner.Plan(Facts(MenuKind.Chest, true, true)), PromptPlanner.Plan(Facts(MenuKind.Chest, true, true)));
            Assert.NotEqual(Facts(MenuKind.Shop), Facts(MenuKind.Shop, canClose: false));
        }

        [Fact]
        public void EveryLabelKeyHasText()
        {
            var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "i18n", "default.json")))!;
            foreach (var key in new[]
            {
                PromptPlanner.Select, PromptPlanner.Close, PromptPlanner.SwitchTab, PromptPlanner.Organize,
                PromptPlanner.ToBackpack, PromptPlanner.ToChest, PromptPlanner.ShopOrBackpack
            })
                Assert.True(texts.ContainsKey(key), $"missing text for {key}");
        }
    }
}
