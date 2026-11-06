using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace DewPad.DevHarness
{
    /// <summary>The slice of the DewPad API the scenarios use. SMAPI matches it to the real one by member names.</summary>
    public interface IDewPadApi
    {
        void DrawHints(SpriteBatch spriteBatch);
        Rectangle HintBounds { get; }
        int HintCount { get; }
    }

    public sealed partial class ModEntry
    {
        /// <summary>
        /// Draws the open menu and then DewPad's hints into a render target the size of the UI, in the order the game draws them, and saves it.
        /// Also records where the strip sits and where the menu ends, so overlap can be checked by number and not only by eye.
        /// </summary>
        private IEnumerable<int> Shot(string name)
        {
            var api = Helper.ModRegistry.GetApi<IDewPadApi>("Kkthnx.DewPad");
            var menu = Game1.activeClickableMenu;
            int w = Game1.uiViewport.Width, h = Game1.uiViewport.Height;
            var device = Game1.graphics.GraphicsDevice;
            var b = Game1.spriteBatch;

            var target = new RenderTarget2D(device, w, h);
            device.SetRenderTarget(target);
            device.Clear(new Color(45, 60, 45));
            b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
            menu?.draw(b);
            api?.DrawHints(b);
            b.End();
            device.SetRenderTarget(null);
            using (var fs = File.Create(Path.Combine(OutDir, $"frame-{name}.png")))
                target.SaveAsPng(fs, w, h);
            target.Dispose();

            var shots = (Dictionary<string, object?>)(_results.TryGetValue("shots", out var existing) ? existing! : _results["shots"] = new Dictionary<string, object?>());
            var bounds = api?.HintBounds ?? Rectangle.Empty;
            shots[name] = new Dictionary<string, object?>
            {
                ["ui"] = $"{w}x{h}",
                ["menu"] = menu?.GetType().Name,
                ["menu_box"] = menu is null ? "none" : $"x {menu.xPositionOnScreen} y {menu.yPositionOnScreen} w {menu.width} h {menu.height} bottom {menu.yPositionOnScreen + menu.height}",
                ["hints"] = api?.HintCount ?? -1,
                ["strip"] = bounds.IsEmpty ? "none" : $"x {bounds.X} y {bounds.Y} w {bounds.Width} h {bounds.Height}",
                ["strip_top_minus_menu_bottom"] = bounds.IsEmpty || menu is null ? null : bounds.Y - (menu.yPositionOnScreen + menu.height)
            };
            yield return 1;
        }
        /// <summary>The game's own UI scale setting, which shrinks the UI viewport the way a smaller screen does, and its own resize path to apply it.</summary>
        private void SetUiScale(float scale)
        {
            Game1.options.desiredUIScale = scale;
            var pp = Game1.graphics.GraphicsDevice.PresentationParameters;
            Game1.game1.SetWindowSize(pp.BackBufferWidth, pp.BackBufferHeight);
        }
        private string Sizes => $"{Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth}x{Game1.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight} ui {Game1.uiViewport.Width}x{Game1.uiViewport.Height}";

        /// <summary>Opens each supported menu with a controller in use, at two window sizes, and takes a real frame of each.</summary>
        private IEnumerable<int> MenusScenario()
        {
            var info = new Dictionary<string, object?>();
            _results["menus"] = info;
            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            yield return 30;
            info["gamepad_mode"] = Game1.options.gamepadControls;

            foreach (var scale in new[] { 2.0f, 1.8f, 1.5f, 1.0f })
            {
                SetUiScale(scale);
                yield return 90;
                string size = $"ui{scale:0.0}";
                info[$"size_{size}"] = Sizes;

                // the game menu on the inventory tab, with the sort button
                Game1.activeClickableMenu = new GameMenu(GameMenu.inventoryTab);
                yield return 40;
                foreach (int t in Shot($"{size}-gamemenu")) yield return t;
                info[$"{size}_gamemenu"] = Game1.activeClickableMenu?.GetType().Name;
                Game1.exitActiveMenu();
                yield return 30;

                // a chest
                var chest = new Chest(true);
                chest.Items.Add(ItemRegistry.Create("(O)388", 25));
                Game1.player.currentLocation = Game1.getFarm();
                chest.ShowMenu();
                yield return 40;
                foreach (int t in Shot($"{size}-chest")) yield return t;
                info[$"{size}_chest"] = Game1.activeClickableMenu?.GetType().Name;
                Game1.exitActiveMenu();
                yield return 30;

                // a shop
                Utility.TryOpenShopMenu("SeedShop", "Pierre");
                yield return 40;
                foreach (int t in Shot($"{size}-shop")) yield return t;
                info[$"{size}_shop"] = Game1.activeClickableMenu?.GetType().Name;
                Game1.exitActiveMenu();
                yield return 30;

                // a menu that DewPad does not describe, which must show nothing
                Game1.activeClickableMenu = new QuestLog();
                yield return 40;
                foreach (int t in Shot($"{size}-unknown")) yield return t;
                Game1.exitActiveMenu();
                yield return 30;
            }

            // keyboard and mouse in use, so no hints
            Game1.options.gamepadMode = Options.GamepadModes.ForceOff;
            yield return 30;
            info["keyboard_mode"] = Game1.options.gamepadControls;
            Game1.activeClickableMenu = new GameMenu(GameMenu.inventoryTab);
            yield return 40;
            foreach (int t in Shot("keyboard-gamemenu")) yield return t;
            Game1.exitActiveMenu();
        }

        /// <summary>The game's own controls tutorial, so the controller art that the settings choose can be seen.</summary>
        private IEnumerable<int> IconsScenario()
        {
            var info = new Dictionary<string, object?>();
            _results["icons"] = info;
            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            SetUiScale(1.0f);
            yield return 90;

            var sheet = Game1.controllerMaps;
            info["sheet"] = sheet is null ? "none" : $"{sheet.Width}x{sheet.Height}";
            if (sheet is not null)
            {
                // compares the first band with the other three, to say which band the game now draws
                var pixels = new Color[sheet.Width * sheet.Height];
                sheet.GetData(pixels);
                int bandPixels = sheet.Width * (sheet.Height / 4);
                for (int band = 1; band < 4; band++)
                {
                    int same = 0;
                    for (int i = 0; i < bandPixels; i++)
                        if (pixels[i] == pixels[band * bandPixels + i])
                            same++;
                    info[$"band0_matches_band{band}_percent"] = Math.Round(100.0 * same / bandPixels, 2);
                }
            }

            Game1.activeClickableMenu = new ButtonTutorialMenu(ButtonTutorialMenu.move_run_check);
            yield return 40;
            foreach (int t in Shot("tutorial-move")) yield return t;
            Game1.exitActiveMenu();
            yield return 30;
            Game1.activeClickableMenu = new ButtonTutorialMenu(ButtonTutorialMenu.useTool_menu);
            yield return 40;
            foreach (int t in Shot("tutorial-tool")) yield return t;
            Game1.exitActiveMenu();
        }
    }
}
