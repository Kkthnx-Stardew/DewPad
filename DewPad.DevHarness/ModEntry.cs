using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DewPad.DevHarness
{
    /// <summary>
    /// Runs DewPad scenarios inside the real game on a throwaway single player farm and writes the results to the file named by DPH_OUT,
    /// with pictures next to it. Does nothing when DPH_OUT is not set.
    /// </summary>
    public sealed partial class ModEntry : Mod
    {
        private string? _out;
        private IEnumerator<int>? _run;
        private int _wait, _ticks, _exitAt = -1;
        private readonly Dictionary<string, object?> _results = new();

        private static string Only => Environment.GetEnvironmentVariable("DPH_ONLY") ?? "";

        public override void Entry(IModHelper helper)
        {
            _out = Environment.GetEnvironmentVariable("DPH_OUT");
            if (string.IsNullOrWhiteSpace(_out))
            {
                Monitor.Log("DPH_OUT is not set, so the harness is idle.", LogLevel.Info);
                return;
            }
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private string OutDir => Path.GetDirectoryName(_out!)!;

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            _ticks++;

            // the game skips its update while its window is inactive, which would ruin every timed step. Runtime only, not saved
            if (Game1.options != null && Game1.options.pauseWhenOutOfFocus)
                Game1.options.pauseWhenOutOfFocus = false;
            if (Game1.musicCategory != null && _ticks % 30 == 0)
            {
                Game1.musicCategory.SetVolume(0f);
                Game1.soundCategory?.SetVolume(0f);
                Game1.ambientCategory?.SetVolume(0f);
                Game1.footstepCategory?.SetVolume(0f);
            }

            if (_exitAt >= 0)
            {
                if (_ticks >= _exitAt)
                    Environment.Exit(0);
                return;
            }

            if (_run is null)
            {
                if (Game1.activeClickableMenu is TitleMenu title && _ticks > 240)
                {
                    Game1.player.Name = "Tester";
                    Game1.player.displayName = "Tester";
                    Game1.player.farmName.Value = "DPTest";
                    Game1.player.favoriteThing.Value = "Testing";
                    Game1.player.isCustomized.Value = true;
                    Game1.player.ConvertClothingOverrideToClothesItems();
                    Game1.multiplayerMode = 0;
                    title.createdNewCharacter(skipIntro: true);
                    _run = Main().GetEnumerator();
                }
                else if (_ticks > 3600)
                    Finish("timeout waiting for the title menu");
                return;
            }

            if (_wait > 0)
            {
                _wait--;
                return;
            }

            try
            {
                if (!_run.MoveNext())
                    Finish(null);
                else
                    _wait = Math.Max(0, _run.Current - 1);
            }
            catch (Exception ex)
            {
                _results["harness_error"] = ex.ToString();
                Finish("harness error");
            }
        }

        private void Finish(string? note)
        {
            _results["note"] = note;
            _results["mods"] = Helper.ModRegistry.GetAll().Select(m => m.Manifest.UniqueID + " " + m.Manifest.Version).ToList();
            File.WriteAllText(_out!, JsonSerializer.Serialize(_results, new JsonSerializerOptions { WriteIndented = true }));
            Monitor.Log($"Harness results written to {_out}.", LogLevel.Info);
            Game1.quit = true;
            _exitAt = _ticks + 300;
        }

        private IEnumerable<int> Main()
        {
            for (int t = 0; t < 3600 && !(Context.IsWorldReady && Context.CanPlayerMove && !Game1.fadeToBlack); t++)
                yield return 1;
            if (!Context.IsWorldReady)
            {
                _results["world"] = "never became ready";
                yield break;
            }
            yield return 120;
            _results["world"] = $"ready, viewport {Game1.uiViewport.Width}x{Game1.uiViewport.Height}, zoom {Game1.options.zoomLevel}, uiScale {Game1.options.uiScale}";

            switch (Only)
            {
                case "restyle":
                    foreach (int w in RestyleScenario()) yield return w;
                    break;
                case "perf":
                    foreach (int w in PerfScenario()) yield return w;
                    break;
                case "menus":
                    foreach (int w in MenusScenario()) yield return w;
                    break;
                case "icons":
                    foreach (int w in IconsScenario()) yield return w;
                    break;
                case "sheet":
                    foreach (int w in SheetScenario()) yield return w;
                    break;
                default:
                    _results["error"] = "unknown scenario " + Only;
                    break;
            }
        }
    }
}
