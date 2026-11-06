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

        /// <summary>True when the highlighted spot is a slot of that grid holding an item the game would act on. A slot the grid dims is not clickable, so it does not count.</summary>
        private static bool OnClickableItem(InventoryMenu? grid, ClickableComponent? spot)
        {
            if (grid is null || spot is null || grid.actualInventory is null || !grid.inventory.Contains(spot))
                return false;
            if (!int.TryParse(spot.name, out int slot) || slot < 0 || slot >= grid.actualInventory.Count)
                return false;
            var item = grid.actualInventory[slot];
            return item is not null && (grid.highlightMethod is null || grid.highlightMethod(item));
        }

        /// <summary>In an ordinary chest with an empty hand, A takes a chest item to the backpack and puts a backpack item in the chest (<c>ItemGrabMenu.receiveLeftClick</c>).</summary>
        private static SelectAction ChestAction(ItemGrabMenu grab, MenuKind kind)
        {
            if (kind != MenuKind.Chest || grab.heldItem is not null)
                return SelectAction.Select;
            var spot = grab.currentlySnappedComponent;
            if (grab.showReceivingMenu && OnClickableItem(grab.ItemsToGrabMenu, spot))
                return SelectAction.Take;
            if (grab.behaviorFunction is not null && OnClickableItem(grab.inventory, spot))
                return SelectAction.PutInChest;
            return SelectAction.Select;
        }

        /// <summary>In a shop, A on a row for sale buys one, and A on a backpack item it will take, with an empty hand, sells the whole stack (<c>ShopMenu.receiveLeftClick</c>).</summary>
        private static SelectAction ShopAction(ShopMenu shop)
        {
            var spot = shop.currentlySnappedComponent;
            if (spot is null)
                return SelectAction.Select;
            if (shop.forSaleButtons.Contains(spot))
                return SelectAction.Buy;
            if (!shop.readOnly && shop.heldItem is null && OnClickableItem(shop.inventory, spot))
                return SelectAction.SellStack;
            return SelectAction.Select;
        }

        /// <summary>Another menu on top, text entry, a dialogue box or an active chat box owns the buttons, so nothing is promised for the menu underneath.</summary>
        public static bool IsBlocked(IClickableMenu? menu) =>
            menu is null || menu.GetChildMenu() is not null || Game1.textEntry is not null || Game1.dialogueUp || Game1.chatBox?.isActive() == true;

        public static MenuFacts Read(IClickableMenu? menu)
        {
            if (IsBlocked(menu))
                return default;

            // a menu that another mod derives from the game's may handle the buttons differently, so only the game's own menus are described
            if (menu.GetType() != typeof(GameMenu) && menu.GetType() != typeof(ItemGrabMenu) && menu.GetType() != typeof(ShopMenu)
                && menu.GetType() != typeof(JunimoNoteMenu) && menu.GetType() != typeof(CarpenterMenu))
                return default;

            switch (menu)
            {
                case GameMenu game when !game.invisible:
                {
                    var page = game.currentTab >= 0 && game.currentTab < game.pages.Count ? game.pages[game.currentTab] : null;
                    bool canOrganize = page is InventoryPage inventory && inventory.GetType() == typeof(InventoryPage) && inventory.organizeButton is not null;
                    return new MenuFacts(MenuKind.GameMenu, game.readyToClose(), canOrganize, game.yPositionOnScreen + game.height);
                }
                case ItemGrabMenu grab:
                {
                    // the bumper jumps are only described for an ordinary chest, never for the shipping bin or a fishing treasure
                    var kind = !grab.shippingBin && grab.source == ItemGrabMenu.source_chest ? MenuKind.Chest : MenuKind.OtherItemGrab;
                    return new MenuFacts(kind, grab.readyToClose(), grab.organizeButton is not null, Lowest(grab.yPositionOnScreen + grab.height, grab.inventory, grab.okButton, grab.trashCan), ChestAction(grab, kind));
                }
                case ShopMenu shop:
                    return new MenuFacts(MenuKind.Shop, shop.readyToClose(), false, Lowest(shop.yPositionOnScreen + shop.height, shop.inventory, null, null), ShopAction(shop));
                case JunimoNoteMenu bundle:
                {
                    // the triggers act inside a bundle, and swap the room pages when the menu was opened from the game menu
                    var kind = bundle.specificBundlePage ? MenuKind.BundleIngredients : bundle.fromGameMenu ? MenuKind.BundleRooms : MenuKind.BundlePlain;
                    return new MenuFacts(kind, bundle.readyToClose(), false, bundle.yPositionOnScreen + bundle.height);
                }
                case CarpenterMenu carpenter when !carpenter.onFarm:
                    // only the building list, not the farm view where a building is placed
                    return new MenuFacts(MenuKind.Carpenter, carpenter.readyToClose(), false, carpenter.yPositionOnScreen + carpenter.height);
                default:
                    return default;
            }
        }
    }
}
