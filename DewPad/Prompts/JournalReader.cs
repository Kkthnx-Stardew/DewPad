using System.Collections;
using DewPad.Core;
using StardewValley.Menus;

namespace DewPad.Prompts
{
    /// <summary>
    /// Reads the journal's page numbers, which the game keeps protected. In the list of quests the left trigger turns back a page when there is one
    /// before it and the right trigger turns on when there is one after it (<c>QuestLog.receiveGamePadButton</c>), and A opens the quest under the
    /// highlight. Inside a quest the same fields say so, and no hints are given there because B may go back or close the journal.
    /// </summary>
    internal static class JournalReader
    {
        private static readonly Func<QuestLog, int>? CurrentPage = ProtectedFields.Getter<QuestLog, int>("currentPage");
        private static readonly Func<QuestLog, int>? QuestPage = ProtectedFields.Getter<QuestLog, int>("questPage");
        private static readonly Func<QuestLog, IList>? Pages = ProtectedFields.Getter<QuestLog, IList>("pages");

        /// <summary>False when the fields cannot be read, and then the journal shows no hints.</summary>
        public static bool IsAvailable => CurrentPage is not null && QuestPage is not null && Pages is not null;

        public static MenuFacts Read(QuestLog log)
        {
            if (!IsAvailable || QuestPage!(log) != -1)
                return default;

            int page = CurrentPage!(log), count = Pages!(log).Count;
            bool onQuest = false;
            var spot = log.currentlySnappedComponent;
            if (spot is not null && log.questLogButtons is { } buttons)
            {
                int index = buttons.IndexOf(spot);
                // the entry exists only when the page has that many quests
                onQuest = index >= 0 && page >= 0 && page < count && PageSize(log, page) > index;
            }

            return new MenuFacts(MenuKind.Journal, log.readyToClose(), false, log.yPositionOnScreen + log.height,
                onQuest ? SelectAction.OpenQuest : SelectAction.Select, OneAction.None, PageBack: page > 0, PageForward: page < count - 1);
        }

        private static int PageSize(QuestLog log, int page) => Pages!(log)[page] is ICollection quests ? quests.Count : 0;
    }
}