using DewPad.Core;
using StardewValley;
using StardewValley.Menus;

namespace DewPad.Prompts
{
    /// <summary>
    /// Reads the few facts the hints depend on from the active menu. Only reads, never changes anything, and does not allocate, so it can run
    /// every frame. A menu it does not know, or one with another menu open on top of it, gives <see cref="MenuKind.Unknown"/> and so no hints.
    /// </summary>
    internal static class MenuFactsReader
    {
        /// <summary>The lowest row any of the menu's main parts reaches. The inventory grid and its buttons hang below the menu's own box in the chest and shop.</summary>
        private static int Lowest(int menuBottom, IClickableMenu? inventory, ClickableComponent? ok, ClickableComponent? trash)
        {
            int bottom = menuBottom;
            if (inventory is not null)
                bottom = Math.Max(bottom, inventory.yPositionOnScreen + inventory.height);
            if (ok is not null)
                bottom = Math.Max(bottom, ok.bounds.Bottom);
            if (trash is not null)
                bottom = Math.Max(bottom, trash.bounds.Bottom);
            return bottom;
        }

        public static MenuFacts Read(IClickableMenu? menu)
        {
            // another menu on top, text entry or a dialogue box owns the buttons, so nothing is promised
            if (menu is null || menu.GetChildMenu() is not null || Game1.textEntry is not null || Game1.dialogueUp)
                return default;

            switch (menu)
            {
                case GameMenu game when !game.invisible:
                {
                    var page = game.currentTab >= 0 && game.currentTab < game.pages.Count ? game.pages[game.currentTab] : null;
                    bool canOrganize = page is InventoryPage inventory && inventory.organizeButton is not null;
                    return new MenuFacts(MenuKind.GameMenu, game.readyToClose(), canOrganize, game.yPositionOnScreen + game.height);
                }
                case ItemGrabMenu grab:
                {
                    // the bumper jumps are only described for an ordinary chest, never for the shipping bin or a fishing treasure
                    var kind = !grab.shippingBin && grab.source == ItemGrabMenu.source_chest ? MenuKind.Chest : MenuKind.OtherItemGrab;
                    return new MenuFacts(kind, grab.readyToClose(), grab.organizeButton is not null, Lowest(grab.yPositionOnScreen + grab.height, grab.inventory, grab.okButton, grab.trashCan));
                }
                case ShopMenu shop:
                    return new MenuFacts(MenuKind.Shop, shop.readyToClose(), false, Lowest(shop.yPositionOnScreen + shop.height, shop.inventory, null, null));
                default:
                    return default;
            }
        }
    }
}
