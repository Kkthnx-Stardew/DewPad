namespace DewPad.Core
{
    /// <summary>The menus DewPad knows how to describe. Everything else is <see cref="Unknown"/> and shows nothing.</summary>
    public enum MenuKind
    {
        Unknown = 0,
        GameMenu,
        Chest,
        OtherItemGrab,
        Shop
    }

    /// <summary>
    /// What was read from a menu, and nothing else, so the same facts always give the same hints. A struct with value equality, so the caller can
    /// tell that a menu changed by comparing two of them without allocating. <c>Bottom</c> is the lowest screen row the menu draws on, in UI pixels,
    /// so the strip can stay clear of it.
    /// </summary>
    public readonly record struct MenuFacts(MenuKind Kind, bool CanClose, bool CanOrganize, int Bottom = 0);

    /// <summary>
    /// The hints for a menu. Each rule below was read from the 1.6.15 game code and is repeated in docs/RESEARCH.md with the method it came from.
    /// A hint is only listed when its button does that in that menu, so the strip never promises something that will not happen.
    /// </summary>
    public static class PromptPlanner
    {
        public const string Select = "prompt.select";
        public const string Close = "prompt.close";
        public const string SwitchTab = "prompt.switch-tab";
        public const string Organize = "prompt.organize";
        public const string ToBackpack = "prompt.to-backpack";
        public const string ToChest = "prompt.to-chest";
        public const string ShopOrBackpack = "prompt.shop-or-backpack";

        public static IReadOnlyList<PromptItem> Plan(MenuFacts facts)
        {
            var items = new List<PromptItem>(5);
            if (facts.Kind == MenuKind.Unknown)
                return items;

            // A clicks the highlighted item in every menu the game moves a highlight in
            items.Add(new PromptItem(GamepadButton.A, Select));

            // B and Start close a menu once it is ready to close, for example not while an item is held
            if (facts.CanClose)
                items.Add(new PromptItem(GamepadButton.B, Close));

            switch (facts.Kind)
            {
                case MenuKind.GameMenu:
                    // the triggers, not the bumpers, change the tab of the game menu
                    items.Add(new PromptItem(GamepadButton.LeftTrigger, GamepadButton.RightTrigger, SwitchTab));
                    break;
                case MenuKind.Chest:
                    items.Add(new PromptItem(GamepadButton.LeftBumper, ToBackpack));
                    items.Add(new PromptItem(GamepadButton.RightBumper, ToChest));
                    break;
                case MenuKind.Shop:
                    items.Add(new PromptItem(GamepadButton.LeftTrigger, GamepadButton.RightTrigger, ShopOrBackpack));
                    break;
            }

            // the Back button, called View on an Xbox pad, sorts the backpack where the menu has the sort button
            if (facts.CanOrganize)
                items.Add(new PromptItem(GamepadButton.Back, Organize));

            return items;
        }
    }
}
