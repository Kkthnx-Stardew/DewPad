using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DewPad.DevHarness
{
    public sealed partial class ModEntry
    {
        /// <summary>Opens the journal with one quest and then with many, and reads the strip with the highlight on a quest, off one, and on each page.</summary>
        private IEnumerable<int> JournalScenario()
        {
            var info = new Dictionary<string, object?>();
            _results["journal"] = info;
            var api = Helper.ModRegistry.GetApi<IDewPadApi>("Kkthnx.DewPad");
            if (api is null)
            {
                info["error"] = "no api";
                yield break;
            }

            Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
            SetUiScale(1.0f);
            yield return 90;

            string Strip() => api.HintLabels.Length == 0 ? "(no strip)" : string.Join(" | ", api.HintLabels);

            Game1.activeClickableMenu = new QuestLog();
            yield return 40;
            info["no_quests"] = Strip();
            Game1.exitActiveMenu();
            yield return 30;

            // many quests, so there is more than one page
            var added = new List<string>();
            foreach (string id in new[] { "9", "1", "2", "3", "4", "5", "6", "7", "8", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" })
            {
                try { Game1.player.addQuest(id); added.Add(id); } catch { }
            }
            info["quests_added"] = added.Count;
            info["quests_in_log"] = Game1.player.questLog.Count;

            Game1.activeClickableMenu = new QuestLog();
            yield return 40;
            var log = (QuestLog)Game1.activeClickableMenu!;
            void Snap(ClickableComponent? spot)
            {
                if (spot is null)
                    return;
                log.setCurrentlySnappedComponentTo(spot.myID);
                log.snapCursorToCurrentSnappedComponent();
            }

            Snap(log.questLogButtons[0]);
            yield return 20;
            foreach (int t in Shot("journal-page1")) yield return t;
            info["page_1_on_a_quest"] = Strip();

            Snap(log.forwardButton);
            yield return 20;
            info["page_1_on_the_forward_arrow"] = Strip();

            // turn to the next page the way the right trigger does, through the game's own handler
            log.receiveGamePadButton(Microsoft.Xna.Framework.Input.Buttons.RightTrigger);
            yield return 30;
            Snap(log.questLogButtons[0]);
            yield return 20;
            foreach (int t in Shot("journal-page2")) yield return t;
            info["page_2_on_a_quest"] = Strip();

            // open a quest, where no hints are given
            log.receiveLeftClick(log.questLogButtons[0].bounds.Center.X, log.questLogButtons[0].bounds.Center.Y);
            yield return 30;
            foreach (int t in Shot("journal-quest")) yield return t;
            info["inside_a_quest"] = Strip();
            Game1.exitActiveMenu();
        }
    }
}