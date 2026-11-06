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
        [InlineData(MenuKind.BundleIngredients, GamepadButton.LeftTrigger, PromptPlanner.BackpackOrIngredients)]
        [InlineData(MenuKind.BundleRooms, GamepadButton.LeftTrigger, PromptPlanner.SwitchRoom)]
        [InlineData(MenuKind.Carpenter, GamepadButton.LeftTrigger, PromptPlanner.SwitchBuilding)]
        public void BundlesAndTheCarpenterUseTheTriggersAsAPair(MenuKind kind, GamepadButton first, string label)
        {
            var item = Assert.Single(PromptPlanner.Plan(Facts(kind)), p => p.LabelKey == label);
            Assert.Equal(first, item.First);
            Assert.Equal(GamepadButton.RightTrigger, item.Second);
        }

        [Fact]
        public void APlainBundleScreenHasNoTriggerHints()
        {
            var plan = PromptPlanner.Plan(Facts(MenuKind.BundlePlain));
            Assert.DoesNotContain(plan, p => p.First is GamepadButton.LeftTrigger or GamepadButton.RightTrigger);
            Assert.Contains(plan, p => p.LabelKey == PromptPlanner.Select);
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
        [InlineData(MenuKind.BundleIngredients)]
        [InlineData(MenuKind.BundleRooms)]
        [InlineData(MenuKind.BundlePlain)]
        [InlineData(MenuKind.Carpenter)]
        public void SelectComesFirstAndNoButtonIsUsedTwice(MenuKind kind)
        {
            var plan = PromptPlanner.Plan(Facts(kind, canClose: true, canOrganize: true));
            Assert.Equal(PromptPlanner.Select, plan[0].LabelKey);
            var buttons = plan.SelectMany(p => p.Second is null ? new[] { p.First } : new[] { p.First, p.Second.Value }).ToList();
            Assert.Equal(buttons.Count, buttons.Distinct().Count());
        }

        [Theory]
        [InlineData(SelectAction.Select, PromptPlanner.Select)]
        [InlineData(SelectAction.Take, PromptPlanner.Take)]
        [InlineData(SelectAction.PutInChest, PromptPlanner.PutInChest)]
        [InlineData(SelectAction.Buy, PromptPlanner.Buy)]
        [InlineData(SelectAction.SellStack, PromptPlanner.SellStack)]
        [InlineData(SelectAction.Place, PromptPlanner.Place)]
        [InlineData(SelectAction.OpenQuest, PromptPlanner.OpenQuest)]
        [InlineData(SelectAction.PickUp, PromptPlanner.PickUp)]
        public void TheAHintNamesWhatTheHighlightedSpotDoes(SelectAction action, string label)
        {
            var plan = PromptPlanner.Plan(new MenuFacts(MenuKind.Chest, true, false, 0, action));
            Assert.Equal(GamepadButton.A, plan[0].First);
            Assert.Equal(label, plan[0].LabelKey);
            Assert.Single(plan, p => p.First == GamepadButton.A);
        }

        [Theory]
        [InlineData(OneAction.None, null)]
        [InlineData(OneAction.TakeOne, PromptPlanner.TakeOne)]
        [InlineData(OneAction.PutOneInChest, PromptPlanner.PutOneInChest)]
        [InlineData(OneAction.PickUpOne, PromptPlanner.PickUpOne)]
        public void XIsListedOnlyWhenItSplitsOneOffAStack(OneAction one, string? label)
        {
            var plan = PromptPlanner.Plan(new MenuFacts(MenuKind.Chest, true, false, 0, SelectAction.Select, one));
            var x = plan.Where(p => p.First == GamepadButton.X).ToList();
            if (label is null)
                Assert.Empty(x);
            else
                Assert.Equal(label, Assert.Single(x).LabelKey);
        }

        [Theory]
        [InlineData(MenuKind.GameMenu, OneAction.PickUpOne)]
        [InlineData(MenuKind.Chest, OneAction.TakeOne)]
        [InlineData(MenuKind.Chest, OneAction.PutOneInChest)]
        public void NoButtonIsUsedTwiceWithTheOneHint(MenuKind kind, OneAction one)
        {
            var plan = PromptPlanner.Plan(new MenuFacts(kind, true, true, 0, SelectAction.Take, one));
            var buttons = plan.SelectMany(p => p.Second is null ? new[] { p.First } : new[] { p.First, p.Second.Value }).ToList();
            Assert.Equal(buttons.Count, buttons.Distinct().Count());
            Assert.Equal(GamepadButton.A, plan[0].First);
        }

        [Theory]
        [InlineData(false, false, 0)]
        [InlineData(true, false, 1)]
        [InlineData(false, true, 1)]
        [InlineData(true, true, 2)]
        public void TheJournalListsEachTriggerOnlyWhenThereIsAPageToTurnTo(bool back, bool forward, int triggerHints)
        {
            var plan = PromptPlanner.Plan(new MenuFacts(MenuKind.Journal, true, false, 0, SelectAction.Select, OneAction.None, back, forward));
            Assert.Equal(triggerHints, plan.Count(p => p.First is GamepadButton.LeftTrigger or GamepadButton.RightTrigger));
            Assert.Equal(back, plan.Any(p => p.First == GamepadButton.LeftTrigger && p.LabelKey == PromptPlanner.PreviousPage));
            Assert.Equal(forward, plan.Any(p => p.First == GamepadButton.RightTrigger && p.LabelKey == PromptPlanner.NextPage));
        }

        [Fact]
        public void AQuestUnderTheHighlightSaysOpen()
        {
            Assert.Equal(PromptPlanner.OpenQuest, PromptPlanner.Plan(new MenuFacts(MenuKind.Journal, true, false, 0, SelectAction.OpenQuest))[0].LabelKey);
            Assert.Equal(PromptPlanner.Select, PromptPlanner.Plan(new MenuFacts(MenuKind.Journal, true, false, 0, SelectAction.Select))[0].LabelKey);
        }

        [Fact]
        public void PickUpIsALabelOfItsOwn()
        {
            Assert.Equal(PromptPlanner.PickUp, PromptPlanner.Plan(new MenuFacts(MenuKind.GameMenu, true, false, 0, SelectAction.PickUp))[0].LabelKey);
        }

        [Fact]
        public void EveryActionHasADifferentLabel()
        {
            var labels = Enum.GetValues<SelectAction>().Select(a => PromptPlanner.Plan(new MenuFacts(MenuKind.Shop, true, false, 0, a))[0].LabelKey).ToList();
            Assert.Equal(labels.Count, labels.Distinct().Count());
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
                PromptPlanner.ToBackpack, PromptPlanner.ToChest, PromptPlanner.ShopOrBackpack,
                PromptPlanner.BackpackOrIngredients, PromptPlanner.SwitchRoom, PromptPlanner.SwitchBuilding,
                PromptPlanner.Take, PromptPlanner.PutInChest, PromptPlanner.Buy, PromptPlanner.SellStack,
                PromptPlanner.PickUp, PromptPlanner.TakeOne, PromptPlanner.PutOneInChest, PromptPlanner.PickUpOne,
                PromptPlanner.OpenQuest, PromptPlanner.PreviousPage, PromptPlanner.NextPage, PromptPlanner.Place
            })
                Assert.True(texts.ContainsKey(key), $"missing text for {key}");
        }
    }
}
