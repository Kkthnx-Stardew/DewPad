using StardewValley;

namespace DewPad.DevHarness
{
    public sealed partial class ModEntry
    {
        /// <summary>Saves the game's own controller art sheet so its button rectangles can be read by eye.</summary>
        private IEnumerable<int> SheetScenario()
        {
            var sheet = Game1.controllerMaps;
            _results["sheet_size"] = sheet is null ? "none" : $"{sheet.Width}x{sheet.Height}";
            if (sheet is not null)
            {
                using var fs = File.Create(Path.Combine(OutDir, "controller-sheet.png"));
                sheet.SaveAsPng(fs, sheet.Width, sheet.Height);
            }
            yield return 30;
        }
    }
}
