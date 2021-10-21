using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Logging;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Peon.SeFunctions;

namespace Peon.Managers
{
    public class QuestManager
    {
        public static readonly IReadOnlySet<uint> RemovedQuests = new SortedSet<uint>()
        {
            67752,
            67819,
        };

        private readonly IsQuestCompleted  _quest;
        private readonly ExcelSheet<Quest> _sheet;

        public QuestManager()
        {
            _quest = new IsQuestCompleted(Dalamud.SigScanner);
            _sheet = Dalamud.GameData.GetExcelSheet<Quest>()!;
        }

        private bool Check(uint questId)
            => RemovedQuests.Contains(questId) || _quest.Check((ushort) questId);

        private string FormatQuest(Quest q)
            => $"{q.RowId}\t{Check(q.RowId)}\t{q.Name}\t{q.Expansion.Value?.Name ?? ""}\t{q.IssuerLocation.Value?.Territory.Value?.PlaceName.Value?.Name ?? ""}\t{q.JournalGenre.Value?.Name ?? ""}\t{q.PlaceName.Value?.Name ?? ""}\t{q.ClassJobLevel0}\t{q.ClassJobLevel1}\t{q.LevelMax}";

        public void Export(FileInfo file, bool onlyUnfinished)
        {
            StringBuilder sb   = new(512 * (int) _sheet.RowCount);
            var           iter = _sheet.Where(q => q.Name.RawString.Any() && !q.IsRepeatable);
            if (onlyUnfinished)
                iter = iter.Where(q => !Check(q.RowId));
            sb.AppendLine("Row\tFinished\tName\tExpansion\tQuest Giver Map\tGenre\tMap\tLevel\tLevel2\tLevelMax");
            foreach (var quest in iter)
            {
                sb.AppendLine(FormatQuest(quest));
            }

            try
            {
                File.WriteAllText(file.FullName, sb.ToString());
            }
            catch (Exception e)
            {
                PluginLog.Error($"Could not export quests:\n{e}");
                Dalamud.Chat.PrintError($"Exporting quests failed.");
            }
        }
    }
}
