namespace DewPad.Core
{
    /// <summary>The menus DewPad knows how to describe. Everything else is <see cref="Unknown"/> and shows nothing.</summary>
    public enum MenuKind
    {
        Unknown = 0,
        GameMenu,
        Chest,
        OtherItemGrab,
        Shop,
        BundleIngredients,
        BundleRooms,
        BundlePlain,
        Carpenter,

        /// <summary>A menu of another mod that registered its own hints through the API.</summary>
        Registered
    }

    /// <summary>
    /// What the A button does at the highlighted spot, when the game's code makes that certain. Everything else is <see cref="Select"/>, which is
    /// just a click on whatever is highlighted.
    /// </summary>
    public enum SelectAction
    {
        Select = 0,

        /// <summary>A backpack item in the game menu, which is picked up whole into the hand.</summary>
        PickUp,

        /// <summary>A chest item, which moves to the backpack.</summary>
        Take,

        /// <summary>A backpack item while a chest is open, which moves into the chest.</summary>
        PutInChest,

        /// <summary>A shop item for sale, which buys one.</summary>
        Buy,

        /// <summary>A backpack item the shop will take, which sells the whole stack at once, with no confirmation.</summary>
        SellStack
    }

    /// <summary>
    /// What the X button does at the highlighted spot, when it does something the A button does not. X is a right click, which takes ONE item from a
    /// stack where A takes the whole stack, so it is only listed for a stack of more than one that can be split.
    /// </summary>
    public enum OneAction
    {
        None = 0,

        /// <summary>A chest stack, one of which moves to the backpack.</summary>
        TakeOne,

        /// <summary>A backpack stack while a chest is open, one of which moves into the chest.</summary>
        PutOneInChest,

        /// <summary>A backpack stack in the game menu, one of which is picked up into the hand.</summary>
        PickUpOne
    }

    /// <summary>
    /// What was read from a menu, and nothing else, so the same facts always give the same hints. A struct with value equality, so the caller can
    /// tell that a menu changed by comparing two of them without allocating. <c>Bottom</c> is the lowest screen row the menu draws on, in UI pixels,
    /// so the strip can stay clear of it.
    /// </summary>
    public readonly record struct MenuFacts(MenuKind Kind, bool CanClose, bool CanOrganize, int Bottom = 0, SelectAction Action = SelectAction.Select, OneAction One = OneAction.None);

    /// <summary>
    /// The hints for a menu. Each rule below was read from the 1.6.15 game code and is repeated in docs/RESEARCH.md with the method it came from.
    /// A hint is only listed when its button does that in that menu, so the strip never promises something that will not happen.
    /// </summary>
    public static class PromptPlanner
    {
        public const string Select = "prompt.select";
        public const string PickUp = "prompt.pick-up";
        public const string TakeOne = "prompt.take-one";
        public const string PutOneInChest = "prompt.put-one-in-chest";
        public const string PickUpOne = "prompt.pick-up-one";
        public const string Take = "prompt.take";
        public const string PutInChest = "prompt.put-in-chest";
        public const string Buy = "prompt.buy";
        public const string SellStack = "prompt.sell-stack";
        public const string Close = "prompt.close";
        public const string SwitchTab = "prompt.switch-tab";
        public const string Organize = "prompt.organize";
        public const string ToBackpack = "prompt.to-backpack";
        public const string ToChest = "prompt.to-chest";
        public const string ShopOrBackpack = "prompt.shop-or-backpack";
        public const string BackpackOrIngredients = "prompt.backpack-or-ingredients";
        public const string SwitchRoom = "prompt.switch-room";
        public const string SwitchBuilding = "prompt.switch-building";

        public static IReadOnlyList<PromptItem> Plan(MenuFacts facts, IReadOnlyList<PromptItem>? registered = null)
        {
            // the hints a mod registered for its own menu are shown exactly as registered
            if (facts.Kind == MenuKind.Registered)
                return registered ?? Array.Empty<PromptItem>();

            var items = new List<PromptItem>(5);
            if (facts.Kind == MenuKind.Unknown)
                return items;

            // A clicks the highlighted item in every menu the game moves a highlight in, and in a chest or a shop what that click does depends on what is
            // highlighted, which is worth saying because selling a stack in a shop is immediate
            items.Add(new PromptItem(GamepadButton.A, facts.Action switch
            {
                SelectAction.PickUp => PickUp,
                SelectAction.Take => Take,
                SelectAction.PutInChest => PutInChest,
                SelectAction.Buy => Buy,
                SelectAction.SellStack => SellStack,
                _ => Select
            }));

            // X splits one off a stack, which A cannot do, so it is the second hint wherever it applies
            switch (facts.One)
            {
                case OneAction.TakeOne:
                    items.Add(new PromptItem(GamepadButton.X, TakeOne));
                    break;
                case OneAction.PutOneInChest:
                    items.Add(new PromptItem(GamepadButton.X, PutOneInChest));
                    break;
                case OneAction.PickUpOne:
                    items.Add(new PromptItem(GamepadButton.X, PickUpOne));
                    break;
            }

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
                case MenuKind.BundleIngredients:
                    // inside a bundle the right trigger jumps to the first empty ingredient slot and the left one back to the backpack
                    items.Add(new PromptItem(GamepadButton.LeftTrigger, GamepadButton.RightTrigger, BackpackOrIngredients));
                    break;
                case MenuKind.BundleRooms:
                    items.Add(new PromptItem(GamepadButton.LeftTrigger, GamepadButton.RightTrigger, SwitchRoom));
                    break;
                case MenuKind.Carpenter:
                    items.Add(new PromptItem(GamepadButton.LeftTrigger, GamepadButton.RightTrigger, SwitchBuilding));
                    break;
            }

            // the Back button, called View on an Xbox pad, sorts the backpack where the menu has the sort button
            if (facts.CanOrganize)
                items.Add(new PromptItem(GamepadButton.Back, Organize));

            return items;
        }
    }
}
