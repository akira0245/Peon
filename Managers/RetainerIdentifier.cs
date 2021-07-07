using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;

namespace Peon.Managers
{
    [Flags]
    public enum RetainerJob : byte
    {
        Miner    = 0x01,
        Botanist = 0x02,
        Fisher   = 0x04,
        Hunter   = 0x08,
    }

    public class RetainerIdentifier
    {
        public struct RetainerTaskInfo
        {
            public byte Category;
            public byte LevelRange;
            public byte Item;
        }

        public readonly Dictionary<RetainerJob, Dictionary<string, RetainerTaskInfo>> Tasks = new()
        {
            [RetainerJob.Botanist] = new Dictionary<string, RetainerTaskInfo>(),
            [RetainerJob.Miner]    = new Dictionary<string, RetainerTaskInfo>(),
            [RetainerJob.Fisher]   = new Dictionary<string, RetainerTaskInfo>(),
            [RetainerJob.Hunter]   = new Dictionary<string, RetainerTaskInfo>(),
        };

        private static RetainerJob FromClassJobCategory(byte classJobCategory)
        {
            return classJobCategory switch
            {
                34  => RetainerJob.Hunter,
                32  => RetainerJob.Botanist | RetainerJob.Miner | RetainerJob.Fisher,
                18  => RetainerJob.Botanist,
                17  => RetainerJob.Miner,
                19  => RetainerJob.Fisher,
                154 => RetainerJob.Botanist | RetainerJob.Miner,
                _   => 0,
            };
        }

        private static Dictionary<uint, byte> ExplorationsToItem(DalamudPluginInterface pi)
        {
            var  randomTasks      = pi.Data.GetExcelSheet<RetainerTaskRandom>(ClientLanguage.English);
            var  ret              = new Dictionary<uint, byte>((int) randomTasks.RowCount);
            byte watersideCounter = 0;
            byte woodlandCounter  = 0;
            byte highlandCounter  = 0;
            byte fieldCounter     = 0;

            foreach (var task in randomTasks.Reverse())
            {
                var name = task.Name.ToString();
                if (name.StartsWith("Water"))
                    ret.Add(task.RowId, watersideCounter++);
                else if (name.StartsWith("Wood"))
                    ret.Add(task.RowId, woodlandCounter++);
                else if (name.StartsWith("High"))
                    ret.Add(task.RowId, highlandCounter++);
                else if (name.StartsWith("Field"))
                    ret.Add(task.RowId, fieldCounter++);
                else if (name.StartsWith("Quick"))
                    continue;
                else
                    PluginLog.Error($"Random exploration {name} could not be corresponded to a job exploration.");
            }

            return ret;
        }

        public bool Identify(string name, RetainerJob job, out RetainerTaskInfo info)
        {
            if (!Tasks.TryGetValue(job, out var tasks))
            {
                info = default;
                PluginLog.Error("Invalid retainer job requested.");
                return false;
            }

            name = name.ToLowerInvariant();
            return tasks.TryGetValue(name, out info);
        }

        public RetainerIdentifier(DalamudPluginInterface pi)
        {
            var tasks       = pi.Data.GetExcelSheet<RetainerTask>();
            var normalTasks = pi.Data.GetExcelSheet<RetainerTaskNormal>();
            var randomTasks = pi.Data.GetExcelSheet<RetainerTaskRandom>(pi.ClientState.ClientLanguage);
            var items       = pi.Data.GetExcelSheet<Item>(pi.ClientState.ClientLanguage);
            var levelRanges = pi.Data.GetExcelSheet<RetainerTaskLvRange>();

            var explorations = ExplorationsToItem(pi);

            var counters = new Dictionary<RetainerJob, byte[]>(4)
            {
                [RetainerJob.Botanist] = new byte[(int) levelRanges.RowCount - 1],
                [RetainerJob.Miner]    = new byte[(int) levelRanges.RowCount - 1],
                [RetainerJob.Fisher]   = new byte[(int) levelRanges.RowCount - 1],
                [RetainerJob.Hunter]   = new byte[(int) levelRanges.RowCount - 1],
            };

            foreach (var task in tasks.Where(t => t.Task != 0))
            {
                var jobs = FromClassJobCategory((byte) task.ClassJobCategory.Row);
                if (jobs == 0)
                    continue;

                var taskInfo = new RetainerTaskInfo();

                string name;
                if (task.Task < 30000)
                {
                    taskInfo.Category   = 0;
                    taskInfo.LevelRange = (byte) ((task.RetainerLevel - 1) / 5);
                    name                = items.GetRow(normalTasks.GetRow(task.Task).Item.Row).Name.ToString().ToLowerInvariant();
                    foreach (var flag in counters.Where(flag => jobs.HasFlag(flag.Key)))
                    {
                        taskInfo.Item = flag.Value[taskInfo.LevelRange];
                        ++flag.Value[taskInfo.LevelRange];
                        Tasks[flag.Key].Add(name, taskInfo);
                    }
                }
                else
                {
                    name                = randomTasks.GetRow(task.Task).Name.ToString().ToLowerInvariant();
                    taskInfo.Category   = (byte) (task.Task == 30053 ? 2 : 1);
                    taskInfo.LevelRange = 0;
                    taskInfo.Item       = (byte) (task.Task == 30053 ? 0 : explorations[task.Task]);

                    Tasks[jobs].Add(name, taskInfo);
                }
            }
        }
    }
}
