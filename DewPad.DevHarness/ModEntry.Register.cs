using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DewPad.DevHarness
{
    public sealed partial class ModEntry
    {
        /// <summary>Registers hints for a menu through the API, as another mod would, and checks they show, are replaced and are removed.</summary>
        private IEnumerable<int> RegisterScenario()
        {
            var info = new Dictionary<string, object?>();
            _results["register"] = info;
            var api = Helper.ModRegistry.GetApi<IDewPadApi>("Kkthnx.DewPad");
            if (api is null)
            {
                info["error"] = "no api";
                yield break;
            }

            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            SetUiScale(1.0f);
            yield return 90;

            const string journal = "StardewValley.Menus.QuestLog";

            // the journal is not a menu DewPad describes, so without a registration it shows nothing
            Game1.activeClickableMenu = new QuestLog();
            yield return 40;
            foreach (int t in Shot("reg-before")) yield return t;
            info["before_registering"] = string.Join(" | ", api.HintLabels);
            Game1.exitActiveMenu();
            yield return 30;

            // one bad entry in the list, which must be skipped with a warning and not stop the rest
            info["register_result"] = api.RegisterMenu(journal, new[] { "A=Select", "LeftTrigger,RightTrigger=Switch page", "Bogus=No", "B=Back" });
            Game1.activeClickableMenu = new QuestLog();
            yield return 40;
            foreach (int t in Shot("reg-after")) yield return t;
            info["after_registering"] = string.Join(" | ", api.HintLabels);
            Game1.exitActiveMenu();
            yield return 30;

            // registering again replaces the earlier hints
            api.RegisterMenu(journal, new[] { "A=Only this" });
            Game1.activeClickableMenu = new QuestLog();
            yield return 40;
            foreach (int t in Shot("reg-replaced")) yield return t;
            info["after_replacing"] = string.Join(" | ", api.HintLabels);
            Game1.exitActiveMenu();
            yield return 30;

            // a list with nothing valid registers nothing and removes the old hints
            info["empty_register_result"] = api.RegisterMenu(journal, new[] { "Nope", "A" });
            Game1.activeClickableMenu = new QuestLog();
            yield return 40;
            info["after_empty_registration"] = string.Join(" | ", api.HintLabels);
            Game1.exitActiveMenu();
            yield return 30;

            api.RegisterMenu(journal, new[] { "A=Again" });
            api.UnregisterMenu(journal);
            Game1.activeClickableMenu = new QuestLog();
            yield return 40;
            info["after_unregistering"] = string.Join(" | ", api.HintLabels);
            Game1.exitActiveMenu();
        }
    }
}