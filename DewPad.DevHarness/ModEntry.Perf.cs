using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DewPad.DevHarness
{
    public sealed partial class ModEntry
    {
        /// <summary>The DewPad mod object, found by reflection because only a test tool needs it. Null when DewPad is not installed.</summary>
        private Mod? FindDewPad()
        {
            var info = Helper.ModRegistry.Get("Kkthnx.DewPad");
            return info?.GetType().GetProperty("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(info) as Mod;
        }

        /// <summary>Does what saving the settings page does, with the style changed, so the path that edits the game's sheet while the game runs is exercised.</summary>
        private bool ChangeStyle(Mod mod, string style)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var config = mod.GetType().GetField("_config", flags)?.GetValue(mod);
            var property = config?.GetType().GetProperty("PromptStyle");
            if (config is null || property is null)
                return false;
            property.SetValue(config, Enum.Parse(property.PropertyType, style));
            mod.GetType().GetMethod("OnSettingsSaved", flags)?.Invoke(mod, null);
            return true;
        }

        private double BandMatch(Texture2D sheet, int bandA, int bandB)
        {
            var pixels = new Color[sheet.Width * sheet.Height];
            sheet.GetData(pixels);
            int bandPixels = sheet.Width * (sheet.Height / 4);
            int same = 0;
            for (int i = 0; i < bandPixels; i++)
                if (pixels[bandA * bandPixels + i] == pixels[bandB * bandPixels + i])
                    same++;
            return Math.Round(100.0 * same / bandPixels, 2);
        }

        /// <summary>Changes the style while the game runs and reads the sheet the game holds, to prove the new art reaches the screen without a restart.</summary>
        private IEnumerable<int> RestyleScenario()
        {
            var info = new Dictionary<string, object?>();
            _results["restyle"] = info;
            var mod = FindDewPad();
            info["found"] = mod is not null;
            if (mod is null)
                yield break;

            yield return 60;
            foreach (var (style, band) in new[] { ("PlayStation", 1), ("Switch", 2), ("PlayStationCircleConfirm", 3), ("PlayStation", 1) })
            {
                info[$"set_{style}"] = ChangeStyle(mod, style);
                yield return 90;
                var sheet = Game1.controllerMaps;
                info[$"{style}_band0_matches_band{band}_percent"] = sheet is null ? -1 : BandMatch(sheet, 0, band);
                info[$"{style}_sheet_is_same_object_as_before"] = null;
            }
            info["texture_disposed"] = Game1.controllerMaps?.IsDisposed;
        }

        /// <summary>
        /// Times DewPad's drawing and counts the bytes it allocates, in a batch like the game's. The numbers that matter are microseconds per frame
        /// and bytes per frame, which should be near zero because the layout is cached.
        /// </summary>
        private IEnumerable<int> PerfScenario()
        {
            var info = new Dictionary<string, object?>();
            _results["perf"] = info;
            var api = Helper.ModRegistry.GetApi<IDewPadApi>("Kkthnx.DewPad");
            if (api is null)
            {
                info["error"] = "no api";
                yield break;
            }

            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            SetUiScale(1.0f);
            yield return 90;
            Game1.activeClickableMenu = new GameMenu(GameMenu.inventoryTab);
            yield return 40;

            var device = Game1.graphics.GraphicsDevice;
            var b = Game1.spriteBatch;
            int w = Game1.uiViewport.Width, h = Game1.uiViewport.Height;
            using var target = new RenderTarget2D(device, w, h);

            // one batch of draws, like a frame that holds many hint strips, so the cost of a single strip can be read from the total
            const int perBatch = 100;
            double Measure(int batches, out long bytes)
            {
                device.SetRenderTarget(target);
                long before = GC.GetAllocatedBytesForCurrentThread();
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < batches; i++)
                {
                    b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    for (int j = 0; j < perBatch; j++)
                        api.DrawHints(b);
                    b.End();
                }
                watch.Stop();
                bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                device.SetRenderTarget(null);
                return watch.Elapsed.TotalMilliseconds * 1000.0 / (batches * perBatch);
            }

            // the first draw builds the layout, which is the one time cost
            var first = Stopwatch.StartNew();
            device.SetRenderTarget(target);
            b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
            api.DrawHints(b);
            b.End();
            device.SetRenderTarget(null);
            first.Stop();
            info["first_draw_with_layout_ms"] = Math.Round(first.Elapsed.TotalMilliseconds, 3);
            info["hints"] = api.HintCount;

            Measure(5, out _);   // warm up, lets the sprite batch grow its buffer once
            double perDraw = Measure(40, out long bytes);
            info["draw_hints_microseconds_per_frame"] = Math.Round(perDraw, 2);
            info["draw_hints_bytes_allocated_per_frame"] = Math.Round(bytes / (40.0 * perBatch), 2);

            // the same call with the keyboard in use, which is what almost every frame of a keyboard player costs
            Game1.options.gamepadMode = Options.GamepadModes.ForceOff;
            yield return 40;
            double keyboard = Measure(40, out long keyboardBytes);
            info["keyboard_mode_microseconds_per_frame"] = Math.Round(keyboard, 3);
            info["keyboard_mode_bytes_allocated_per_frame"] = Math.Round(keyboardBytes / (40.0 * perBatch), 2);

            // the worst case for the new context reading, a chest with the highlight on an item
            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            Game1.exitActiveMenu();
            yield return 30;
            var chest = new StardewValley.Objects.Chest(true);
            chest.Items.Add(ItemRegistry.Create("(O)390", 12));
            Game1.player.currentLocation = Game1.getFarm();
            chest.ShowMenu();
            yield return 40;
            var grab = (ItemGrabMenu)Game1.activeClickableMenu!;
            grab.setCurrentlySnappedComponentTo(grab.ItemsToGrabMenu.inventory[0].myID);
            yield return 20;
            Measure(5, out _);
            double chestDraw = Measure(40, out long chestBytes);
            info["chest_on_item_microseconds_per_frame"] = Math.Round(chestDraw, 2);
            info["chest_on_item_bytes_allocated_per_frame"] = Math.Round(chestBytes / (40.0 * perBatch), 2);
            info["chest_on_item_first_hint"] = api.HintLabels.Length > 0 ? api.HintLabels[0] : "none";
            Game1.exitActiveMenu();
            yield return 30;
            Game1.activeClickableMenu = new GameMenu(GameMenu.inventoryTab);
            yield return 40;

            // the menu's own draw, for scale
            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            yield return 40;
            device.SetRenderTarget(target);
            var menu = Game1.activeClickableMenu!;
            var watch2 = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
            {
                b.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                menu.draw(b);
                b.End();
            }
            watch2.Stop();
            device.SetRenderTarget(null);
            info["game_menu_draw_microseconds_per_frame"] = Math.Round(watch2.Elapsed.TotalMilliseconds * 1000.0 / 200, 2);

            // opening and closing menus many times must not grow memory
            Game1.exitActiveMenu();
            yield return 30;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long memoryBefore = GC.GetTotalMemory(true);
            for (int i = 0; i < 300; i++)
            {
                Game1.activeClickableMenu = new GameMenu(GameMenu.inventoryTab);
                Measure(1, out _);
                Game1.exitActiveMenu();
                if (i % 20 == 0)
                    yield return 1;
            }
            yield return 30;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long memoryAfter = GC.GetTotalMemory(true);
            info["memory_before_kb"] = memoryBefore / 1024;
            info["memory_after_300_menu_opens_kb"] = memoryAfter / 1024;
            info["memory_change_kb"] = (memoryAfter - memoryBefore) / 1024;
        }
    }
}
