using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace DewPad.DevHarness
{
    public sealed partial class ModEntry
    {
        /// <summary>
        /// Moves the highlight onto different things in a chest and a shop and reads the first hint, the A button, each time, to prove the label
        /// follows what is highlighted.
        /// </summary>
        private IEnumerable<int> ContextScenario()
        {
            var info = new Dictionary<string, object?>();
            _results["context"] = info;
            var api = Helper.ModRegistry.GetApi<IDewPadApi>("Kkthnx.DewPad");
            if (api is null)
            {
                info["error"] = "no api";
                yield break;
            }

            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            SetUiScale(1.0f);
            yield return 90;

            // wood sells, tools do not, so the backpack has one of each
            Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)388", 40));
            // Pierre buys farm goods and not wood, so a parsnip is the sellable one for the shop
            Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)24", 5));
            int woodSlot = Game1.player.Items.ToList().FindIndex(i => i?.QualifiedItemId == "(O)388");
            int parsnipSlot = Game1.player.Items.ToList().FindIndex(i => i?.QualifiedItemId == "(O)24");
            info["parsnip_slot"] = parsnipSlot;
            int toolSlot = 0;
            info["wood_slot"] = woodSlot;
            info["tool_in_slot_0"] = Game1.player.Items[0]?.QualifiedItemId;

            string First(string name)
            {
                var strip = api.HintLabels;
                string a = strip.Length > 0 ? strip[0] : "(no strip)";
                info[name] = a;
                return a;
            }

            // ---- a chest ----
            var chest = new Chest(true);
            chest.Items.Add(ItemRegistry.Create("(O)390", 12));
            Game1.player.currentLocation = Game1.getFarm();
            chest.ShowMenu();
            yield return 40;
            var grab = (ItemGrabMenu)Game1.activeClickableMenu!;

            void SnapTo(IClickableMenu menu, ClickableComponent? spot)
            {
                if (spot is null)
                    return;
                menu.setCurrentlySnappedComponentTo(spot.myID);
                menu.snapCursorToCurrentSnappedComponent();
            }

            SnapTo(grab, grab.ItemsToGrabMenu.inventory[0]);
            yield return 20;
            foreach (int t in Shot("ctx-chest-chest-item")) yield return t;
            First("chest_on_chest_item");

            SnapTo(grab, grab.ItemsToGrabMenu.inventory[5]);
            yield return 20;
            foreach (int t in Shot("ctx-chest-empty-slot")) yield return t;
            First("chest_on_empty_chest_slot");

            SnapTo(grab, grab.inventory.inventory[woodSlot]);
            yield return 20;
            foreach (int t in Shot("ctx-chest-backpack-item")) yield return t;
            First("chest_on_backpack_item");

            SnapTo(grab, grab.organizeButton);
            yield return 20;
            foreach (int t in Shot("ctx-chest-sort-button")) yield return t;
            First("chest_on_sort_button");
            Game1.exitActiveMenu();
            yield return 40;

            // ---- a shop ----
            Utility.TryOpenShopMenu("SeedShop", "Pierre");
            yield return 40;
            var shop = (ShopMenu)Game1.activeClickableMenu!;
            SnapTo(shop, shop.forSaleButtons[0]);
            yield return 20;
            foreach (int t in Shot("ctx-shop-for-sale")) yield return t;
            First("shop_on_item_for_sale");

            SnapTo(shop, shop.inventory.inventory[parsnipSlot]);
            yield return 20;
            foreach (int t in Shot("ctx-shop-sellable")) yield return t;
            First("shop_on_sellable_parsnip");

            SnapTo(shop, shop.inventory.inventory[woodSlot]);
            yield return 20;
            foreach (int t in Shot("ctx-shop-wood")) yield return t;
            First("shop_on_wood_pierre_does_not_buy");

            SnapTo(shop, shop.inventory.inventory[toolSlot]);
            yield return 20;
            foreach (int t in Shot("ctx-shop-unsellable")) yield return t;
            First("shop_on_unsellable_tool");
            Game1.exitActiveMenu();
        }
    }
}
