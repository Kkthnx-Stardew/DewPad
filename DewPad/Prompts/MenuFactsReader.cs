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

        /// <summary>The item in the highlighted slot of that grid, or null. A slot the grid dims is not clickable, so it gives null.</summary>
        private static Item? ItemAt(InventoryMenu? grid, ClickableComponent? spot)
        {
            if (grid is null || spot is null || grid.actualInventory is null || !grid.inventory.Contains(spot))
                return null;
            if (!int.TryParse(spot.name, out int slot) || slot < 0 || slot >= grid.actualInventory.Count)
                return null;
            var item = grid.actualInventory[slot];
            return item is not null && (grid.highlightMethod is null || grid.highlightMethod(item)) ? item : null;
        }

        private static bool OnClickableItem(InventoryMenu? grid, ClickableComponent? spot) => ItemAt(grid, spot) is not null;

        /// <summary>A right click takes one from a stack of more than one that can be split, where a left click takes the whole stack (<c>InventoryMenu.rightClick</c>).</summary>
        private static bool CanSplit(Item? item) => item is not null && item.Stack > 1 && item.maximumStackSize() != -1;
        /// <summary>In an ordinary chest with an empty hand, A takes a chest item to the backpack and puts a backpack item in the chest (<c>ItemGrabMenu.receiveLeftClick</c>).</summary>
        private static SelectAction ChestAction(ItemGrabMenu grab, MenuKind kind)
        {
            if (kind != MenuKind.Chest)
                return SelectAction.Select;
            var spot = grab.currentlySnappedComponent;

            // a held item, for example after taking one with a full backpack, which keeps the menu from closing until it is put down. A on the chest
            // side puts it in the chest (behaviorFunction) and A on a backpack slot puts it there (MenuWithInventory.receiveLeftClick)
            if (grab.heldItem is not null)
            {
                if (spot is not null && grab.ItemsToGrabMenu is { } chestGrid && chestGrid.inventory.Contains(spot) && grab.behaviorFunction is not null)
                    return SelectAction.PutInChest;
                return spot is not null && grab.inventory.inventory.Contains(spot) ? SelectAction.Place : SelectAction.Select;
            }

            if (grab.showReceivingMenu && OnClickableItem(grab.ItemsToGrabMenu, spot))
                return SelectAction.Take;
            if (grab.behaviorFunction is not null && OnClickableItem(grab.inventory, spot))
                return SelectAction.PutInChest;
            return SelectAction.Select;
        }

        /// <summary>In an ordinary chest with an empty hand, X moves one item of a stack the same way A moves the whole stack (<c>ItemGrabMenu.receiveRightClick</c>).</summary>
        private static OneAction ChestOne(ItemGrabMenu grab, MenuKind kind)
        {
            if (kind != MenuKind.Chest || grab.heldItem is not null)
                return OneAction.None;
            var spot = grab.currentlySnappedComponent;
            if (grab.showReceivingMenu && CanSplit(ItemAt(grab.ItemsToGrabMenu, spot)))
                return OneAction.TakeOne;
            if (grab.behaviorFunction is not null && CanSplit(ItemAt(grab.inventory, spot)))
                return OneAction.PutOneInChest;
            return OneAction.None;
        }

        /// <summary>In the backpack page of the game menu, A picks up the stack under the highlight and X picks up one (<c>InventoryPage</c> and <c>InventoryMenu.rightClick</c>). Only with an empty hand.</summary>
        private static (SelectAction, OneAction) BackpackActions(IClickableMenu? page)
        {
            if (page is not InventoryPage backpack || page.GetType() != typeof(InventoryPage))
                return (SelectAction.Select, OneAction.None);

            // with an item in the hand, A puts it down on a backpack slot, swapping with what is there
            if (Game1.player.CursorSlotItem is not null)
                return (backpack.currentlySnappedComponent is { } held && backpack.inventory.inventory.Contains(held) ? SelectAction.Place : SelectAction.Select, OneAction.None);
            var item = ItemAt(backpack.inventory, backpack.currentlySnappedComponent);
            if (item is null)
                return (SelectAction.Select, OneAction.None);
            return (SelectAction.PickUp, CanSplit(item) ? OneAction.PickUpOne : OneAction.None);
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
                && menu.GetType() != typeof(JunimoNoteMenu) && menu.GetType() != typeof(CarpenterMenu) && menu.GetType() != typeof(QuestLog))
                return default;

            switch (menu)
            {
                case GameMenu game when !game.invisible:
                {
                    var page = game.currentTab >= 0 && game.currentTab < game.pages.Count ? game.pages[game.currentTab] : null;
                    bool canOrganize = page is InventoryPage inventory && inventory.GetType() == typeof(InventoryPage) && inventory.organizeButton is not null;
                    var (action, one) = BackpackActions(page);
                    return new MenuFacts(MenuKind.GameMenu, game.readyToClose(), canOrganize, game.yPositionOnScreen + game.height, action, one);
                }
                case ItemGrabMenu grab:
                {
                    // the bumper jumps are only described for an ordinary chest, never for the shipping bin or a fishing treasure
                    var kind = !grab.shippingBin && grab.source == ItemGrabMenu.source_chest ? MenuKind.Chest : MenuKind.OtherItemGrab;
                    return new MenuFacts(kind, grab.readyToClose(), grab.organizeButton is not null, Lowest(grab.yPositionOnScreen + grab.height, grab.inventory, grab.okButton, grab.trashCan), ChestAction(grab, kind), ChestOne(grab, kind));
                }
                case ShopMenu shop:
                    return new MenuFacts(MenuKind.Shop, shop.readyToClose(), false, Lowest(shop.yPositionOnScreen + shop.height, shop.inventory, null, null), ShopAction(shop));
                case JunimoNoteMenu bundle:
                {
                    // the triggers act inside a bundle, and swap the room pages when the menu was opened from the game menu
                    var kind = bundle.specificBundlePage ? MenuKind.BundleIngredients : bundle.fromGameMenu ? MenuKind.BundleRooms : MenuKind.BundlePlain;
                    return new MenuFacts(kind, bundle.readyToClose(), false, bundle.yPositionOnScreen + bundle.height);
                }
                case QuestLog journal:
                    return JournalReader.Read(journal);
                case CarpenterMenu carpenter when !carpenter.onFarm:
                    // only the building list, not the farm view where a building is placed
                    return new MenuFacts(MenuKind.Carpenter, carpenter.readyToClose(), false, carpenter.yPositionOnScreen + carpenter.height);
                default:
                    return default;
            }
        }
    }
}
