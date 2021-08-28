using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Peon.Bothers;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public enum RetainerMode
    {
        LootWithGil           = 0x01,
        ResendWithGil         = 0x02,
        LootNewVentureWithGil = 0x04,
    }

    public enum RetainerType
    {
        Name          = 0,
        FirstBotanist = 1,
        AllBotanist   = 2,
        FirstMiner    = 3,
        AllMiner      = 4,
        FirstFisher   = 5,
        AllFisher     = 6,
        FirstHunter   = 7,
        AllHunter     = 8,
        FirstGatherer = 9,
        AllGatherer   = 10,
        First         = 11,
        All           = 12,
        Indices       = 13,
    }

    public sealed class RetainerManager : WorkManager
    {
        public const    int                MaxRetainers = 10;
        public readonly RetainerIdentifier Identifier;

        private int _retainerIdx;

        private PtrRetainerList       _list;
        private PtrRetainerTaskAsk    _taskAsk;
        private PtrRetainerTaskList   _taskList;
        private PtrRetainerTaskResult _taskResult;
        private PtrSelectString       _retainerMenu;
        private PtrBank               _bank;

        private RetainerType                        _retainerType = RetainerType.Name;
        private CompareString                       _retainerName;
        private int[]                               _retainerIndices = Array.Empty<int>();
        private RetainerIdentifier.RetainerTaskInfo _retainerInfo;
        private int                                 _currentRetainerIdx = -1;

        public RetainerManager(TargetManager target, AddonWatcher addons, BotherHelper bothers,
            InterfaceManager interfaceManager)
            : base(target, addons, bothers, interfaceManager)
            => Identifier = new RetainerIdentifier();

        public void SetRetainer(CompareString name)
        {
            if (!_jobRunning)
            {
                _retainerType = RetainerType.Name;
                _retainerName = name;
            }
        }

        public void SetRetainer(RetainerType type)
        {
            if (!_jobRunning)
            {
                if (type == RetainerType.Name)
                    throw new InvalidEnumArgumentException();

                _retainerName = default;
                _retainerType = type;
            }
        }

        public void SetRetainers(params int[] indices)
        {
            if (!_jobRunning)
            {
                _retainerType    = RetainerType.Indices;
                _retainerIndices = indices;
            }
        }

        protected override WorkState SetInitialState()
        {
            _list = Interface.RetainerList();
            if (_list)
                return WorkState.RetainerListOpen;

            _retainerMenu = Interface.SelectString();
            if (_retainerMenu)
            {
                var description = _retainerMenu.Description();
                if (StringId.SelectCategory.Equal(description))
                    return WorkState.RetainerSelectCategoryOpen;
                if (description.Contains(StringId.SelectOption.Value()))
                    return WorkState.RetainerMenuOpen;

                return WorkState.RetainerSelectLevelRangeOpen;
            }

            _taskAsk = Interface.RetainerTaskAsk();
            if (_taskAsk)
                return WorkState.RetainerTaskAskOpen;

            _taskResult = Interface.RetainerTaskResult();
            if (_taskResult)
                return WorkState.RetainerTaskResultOpen;

            _taskList = Interface.RetainerTaskList();
            if (_taskList)
                return WorkState.RetainerTaskListOpen;

            return WorkState.None;
        }

        public void DoAllRetainers(RetainerMode mode)
            => DoWork(PrependMulti(mode));

        public void DoAllRetainers(RetainerMode mode, string item)
            => DoWork(PrependMulti(mode, item));

        private Func<bool> ChooseFunc(RetainerMode mode)
        {
            return mode switch
            {
                RetainerMode.ResendWithGil         => ResendOneRetainerWithGil,
                RetainerMode.LootWithGil           => LootOneRetainer,
                RetainerMode.LootNewVentureWithGil => AssignNewVenture,
                _                                  => throw new InvalidEnumArgumentException(),
            };
        }

        private int SelectRetainer(RetainerData[] retainers, RetainerJob flags)
        {
            switch (_retainerType)
            {
                case RetainerType.Name:
                    return retainers.IndexOf(r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress && _retainerName.Matches(r.Name),
                        _currentRetainerIdx + 1);
                case RetainerType.FirstBotanist:
                case RetainerType.AllBotanist:
                    return retainers.IndexOf(r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress && r.Job == RetainerJob.Botanist,
                        _currentRetainerIdx + 1);
                case RetainerType.FirstMiner:
                case RetainerType.AllMiner:
                    return retainers.IndexOf(r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress && r.Job == RetainerJob.Miner,
                        _currentRetainerIdx + 1);
                case RetainerType.FirstFisher:
                case RetainerType.AllFisher:
                    return retainers.IndexOf(r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress && r.Job == RetainerJob.Fisher,
                        _currentRetainerIdx + 1);
                case RetainerType.FirstHunter:
                case RetainerType.AllHunter:
                    return retainers.IndexOf(r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress && r.Job == RetainerJob.Hunter,
                        _currentRetainerIdx + 1);
                case RetainerType.FirstGatherer:
                case RetainerType.AllGatherer:
                    return retainers.IndexOf(r
                            => flags.HasFlag(r.Job)
                         && r.Venture != VentureState.InProgress
                         && (RetainerJob.Botanist | RetainerJob.Miner).HasFlag(r.Job),
                        _currentRetainerIdx + 1);
                case RetainerType.First:
                    return retainers.IndexOf(r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress, _currentRetainerIdx + 1);
                case RetainerType.All:
                    return retainers.IndexOf(r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress, _currentRetainerIdx + 1);
                case RetainerType.Indices:
                    return retainers.IndexOf(
                        r => flags.HasFlag(r.Job) && r.Venture != VentureState.InProgress && _retainerIndices.Contains(r.Index),
                        _currentRetainerIdx + 1);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateRetainerCounter()
        {
            _currentRetainerIdx = _retainerType switch
            {
                RetainerType.Name          => _retainerIdx,
                RetainerType.FirstBotanist => MaxRetainers,
                RetainerType.AllBotanist   => _retainerIdx,
                RetainerType.FirstMiner    => MaxRetainers,
                RetainerType.AllMiner      => _retainerIdx,
                RetainerType.FirstFisher   => MaxRetainers,
                RetainerType.AllFisher     => _retainerIdx,
                RetainerType.FirstHunter   => MaxRetainers,
                RetainerType.AllHunter     => _retainerIdx,
                RetainerType.FirstGatherer => MaxRetainers,
                RetainerType.AllGatherer   => _retainerIdx,
                RetainerType.First         => MaxRetainers,
                RetainerType.All           => _retainerIdx,
                RetainerType.Indices       => _retainerIdx,
                _                          => throw new InvalidEnumArgumentException(),
            };
        }

        private Dictionary<RetainerJob, RetainerIdentifier.RetainerTaskInfo>? GetTaskInfos(string item)
        {
            if (item == string.Empty)
                return null;

            if (item == new LazyString(StringId.QuickExploration))
            {
                _retainerInfo = new RetainerIdentifier.RetainerTaskInfo { Category = 2 };
                return null;
            }

            var flags = _retainerType switch
            {
                RetainerType.Name          => RetainerIdentifier.AllJobs,
                RetainerType.FirstBotanist => RetainerIdentifier.Botanist,
                RetainerType.AllBotanist   => RetainerIdentifier.Botanist,
                RetainerType.FirstMiner    => RetainerIdentifier.Miner,
                RetainerType.AllMiner      => RetainerIdentifier.Miner,
                RetainerType.FirstFisher   => RetainerIdentifier.Fisher,
                RetainerType.AllFisher     => RetainerIdentifier.Fisher,
                RetainerType.FirstHunter   => RetainerIdentifier.Hunter,
                RetainerType.AllHunter     => RetainerIdentifier.Hunter,
                RetainerType.FirstGatherer => RetainerIdentifier.Gatherers,
                RetainerType.AllGatherer   => RetainerIdentifier.Gatherers,
                RetainerType.First         => RetainerIdentifier.AllJobs,
                RetainerType.All           => RetainerIdentifier.AllJobs,
                RetainerType.Indices       => RetainerIdentifier.AllJobs,
                _                          => throw new InvalidEnumArgumentException(),
            };
            Dictionary<RetainerJob, RetainerIdentifier.RetainerTaskInfo> indices = new();

            foreach (var flag in flags)
                if (Identifier.Identify(item, flag, out var info))
                    indices[flag] = info;

            return indices;
        }

        private Func<bool> PrependMulti(RetainerMode mode, string item = "")
        {
            var indices = GetTaskInfos(item);
            if (indices != null && indices.Count == 0)
                Dalamud.Chat.Print($"Item {item} could not be identified for the given jobs.");

            var flags = indices?.Keys.Aggregate((RetainerJob) 0, (j1, j2) => j1 | j2)
             ?? RetainerJob.Botanist | RetainerJob.Fisher | RetainerJob.Hunter | RetainerJob.Miner;

            _currentRetainerIdx = -1;
            return () =>
            {
                switch (State)
                {
                    case WorkState.RetainerListOpen:
                        var data = _list.Info();
                        _retainerIdx = SelectRetainer(data, flags);
                        if (_retainerIdx >= 0)
                        {
                            UpdateRetainerCounter();
                            _retainerInfo = indices?[data[_retainerIdx].Job] ?? _retainerInfo;
                            return ContactRetainer();
                        }

                        State = WorkState.JobFinished;
                        return true;

                    case WorkState.RetainerMenuOpenDone:
                        var ret = Quit();
                        if (ret)
                            State = WorkState.RetainerListOpen;
                        return ret;
                    default: return ChooseFunc(mode)();
                }
            };
        }

        public bool AssignNewVenture()
        {
            return State switch
            {
                WorkState.None                         => OpenList(),
                WorkState.RetainerListOpen             => ContactRetainer(),
                WorkState.RetainerMenuOpenBank         => OpenBank(),
                WorkState.RetainerBankOpen             => TakeGil(),
                WorkState.RetainerMenuOpenReport       => ViewReport(),
                WorkState.RetainerTaskResultOpen       => Accept(),
                WorkState.RetainerMenuOpen             => AssignNew(),
                WorkState.RetainerSelectCategoryOpen   => SelectCategory(),
                WorkState.RetainerSelectLevelRangeOpen => SelectLevelRange(),
                WorkState.RetainerTaskListOpen         => SelectItem(),
                WorkState.RetainerTaskAskOpen          => Assign(),
                WorkState.RetainerMenuOpenDone         => Quit(),
                _                                      => Failure("Unknown state reached."),
            };
        }


        private bool ResendOneRetainerWithGil()
        {
            return State switch
            {
                WorkState.None                   => OpenList(),
                WorkState.RetainerListOpen       => ContactRetainer(),
                WorkState.RetainerMenuOpenBank   => OpenBank(),
                WorkState.RetainerBankOpen       => TakeGil(),
                WorkState.RetainerMenuOpenReport => ViewReport(),
                WorkState.RetainerTaskResultOpen => Reassign(),
                WorkState.RetainerTaskAskOpen    => Assign(),
                WorkState.RetainerMenuOpen       => MenuOpenToDone(),
                WorkState.RetainerMenuOpenDone   => Quit(),
                _                                => Failure("Unknown state reached."),
            };
        }

        private bool LootOneRetainer()
        {
            return State switch
            {
                WorkState.None                   => OpenList(),
                WorkState.RetainerListOpen       => ContactRetainer(),
                WorkState.RetainerMenuOpenBank   => OpenBank(),
                WorkState.RetainerBankOpen       => TakeGil(),
                WorkState.RetainerMenuOpenReport => ViewReport(),
                WorkState.RetainerTaskResultOpen => Accept(),
                WorkState.RetainerMenuOpen       => MenuOpenToDone(),
                WorkState.RetainerMenuOpenDone   => Quit(),
                _                                => Failure("Unknown state reached."),
            };
        }

        private bool MenuOpenToDone()
        {
            State = WorkState.RetainerMenuOpenDone;
            return true;
        }

        private bool OpenList()
        {
            var task = Interface.Add("RetainerList", true, DefaultTimeOut);

            var targetTask = Targets.Interact(StringId.SummoningBell.Value(), DefaultTimeOut / 6);
            Wait(targetTask);
            if (!targetTask.IsCompleted || targetTask.Result != TargetingState.Success)
                return Failure($"Targeting failed {targetTask.Result}.");

            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while opening RetainerList.");

            State = WorkState.RetainerListOpen;
            _list = task.Result;
            return true;
        }

        private static WorkState RetainerMenuState(IEnumerable<string> texts)
        {
            foreach (var text in texts)
            {
                if (StringId.RetainerTaskComplete.Equal(text))
                    return WorkState.RetainerMenuOpenReport;
                if (StringId.RetainerTaskInProgress.Equal(text))
                    return WorkState.RetainerMenuOpenDone;
                if (StringId.RetainerTaskAvailable.Equal(text))
                    return WorkState.RetainerMenuOpen;
            }

            return WorkState.RetainerMenuOpenDone;
        }

        private bool ContactRetainer()
        {
            using var nextTalk = Bothers.SkipNextTalk();

            var task = Interface.Add("SelectString", true, DefaultTimeOut);

            if (!_list.Select(_retainerIdx))
                return Failure("Invalid index for RetainerList.");

            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while contacting retainer.");

            _list         = IntPtr.Zero;
            _retainerMenu = task.Result;
            State         = WorkState.RetainerMenuOpenBank;
            return true;
        }

        private bool OpenBank()
        {
            if (!_retainerMenu.Select(StringId.EntrustGil.Cs()))
                return Failure("Could not find bank button.");

            var task = Interface.Add("Bank", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while opening bank.");

            State         = WorkState.RetainerBankOpen;
            _retainerMenu = IntPtr.Zero;
            _bank         = task.Result;
            return true;
        }

        private bool TakeGil()
        {
            _bank.Minus();
            _bank.Proceed();

            var task = Interface.Add("SelectString", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while obtaining gil.");

            _bank         = IntPtr.Zero;
            _retainerMenu = task.Result;
            State         = RetainerMenuState(_retainerMenu.EnumerateTexts());
            return true;
        }

        private bool ViewReport()
        {
            if (!_retainerMenu.Select(StringId.RetainerTaskComplete.Cs()))
            {
                State = WorkState.RetainerMenuOpenDone;
                return true;
            }

            var task = Interface.Add("RetainerTaskResult", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while obtaining venture report.");

            State         = WorkState.RetainerTaskResultOpen;
            _retainerMenu = IntPtr.Zero;
            _taskResult   = task.Result;
            return true;
        }

        private bool AssignNew()
        {
            if (!_retainerMenu.Select(StringId.RetainerTaskAvailable.Cs()))
            {
                State = WorkState.RetainerMenuOpenDone;
                return true;
            }

            var task = Interface.Add("SelectString", true, DefaultTimeOut,
                ptr => StringId.SelectCategory.Equal(((PtrSelectString) ptr).Description()));
            if (task.IsCompleted && task.Result != IntPtr.Zero)
                return Failure("Timeout while assigning new venture.");

            State         = WorkState.RetainerSelectCategoryOpen;
            _retainerMenu = task.Result;
            return true;
        }

        private bool SelectCategory()
        {
            switch (_retainerInfo.Category)
            {
                case 0:
                {
                    if (!_retainerMenu.Select(0))
                        goto default;

                    var task = Interface.Add("SelectString", true, DefaultTimeOut,
                        ptr => ((PtrSelectString) ptr).Count > 3);
                    Wait(task);
                    if (!task.IsCompleted || task.Result == IntPtr.Zero)
                        return Failure("Timeout while selecting category.");

                    _retainerMenu = task.Result;
                    State         = WorkState.RetainerSelectLevelRangeOpen;
                    return true;
                }
                case 1:
                {
                    if (!_retainerMenu.Select(1))
                        goto default;
                    var task = Interface.Add("RetainerTaskList", true, DefaultTimeOut, ptr => ((PtrRetainerTaskList) ptr).Count > 0);
                    Wait(task);
                    if (!task.IsCompleted || task.Result == IntPtr.Zero)
                        return Failure("Timeout while selecting category.");

                    State         = WorkState.RetainerTaskListOpen;
                    _taskList     = task.Result;
                    _retainerMenu = IntPtr.Zero;
                    return true;
                }
                case 2:
                {
                    if (!_retainerMenu.Select(2))
                        goto default;
                    var task = Interface.Add("RetainerTaskAsk", true, DefaultTimeOut);
                    Wait(task);
                    if (!task.IsCompleted || task.Result == IntPtr.Zero)
                        return Failure("Timeout while selecting category.");

                    State         = WorkState.RetainerTaskAskOpen;
                    _taskAsk      = task.Result;
                    _retainerMenu = IntPtr.Zero;
                    return true;
                }
                default:
                {
                    if (!_retainerMenu.Select(StringId.RetainerReturn.Cs()))
                        return Failure("Could not return from category select.");

                    var task = Interface.Add("SelectString", true, DefaultTimeOut,
                        ptr => ((PtrSelectString) ptr).Description() != "Select a category.");
                    Wait(task);
                    if (!task.IsCompleted || task.Result == IntPtr.Zero)
                        return Failure("Timeout while returning from category select.");

                    State         = WorkState.RetainerMenuOpen;
                    _retainerMenu = task.Result;
                    return true;
                }
            }
        }

        private bool SelectLevelRange()
        {
            if (!_retainerMenu.Select(_retainerInfo.LevelRange))
                return Failure("Level Range index not available.");

            var task = Interface.Add("RetainerTaskList", true, DefaultTimeOut, ptr => ((PtrRetainerTaskList) ptr).Count > 0);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while selecting level range.");

            State         = WorkState.RetainerTaskListOpen;
            _taskList     = task.Result;
            _retainerMenu = IntPtr.Zero;
            return true;
        }

        private bool SelectItem()
        {
            if (!_taskList.Select(_retainerInfo.Item))
                return Failure($"Item index {_retainerInfo.Item} not available.");

            var task = Interface.Add("RetainerTaskAsk", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while selecting item.");

            State     = WorkState.RetainerTaskAskOpen;
            _taskList = IntPtr.Zero;
            _taskAsk  = task.Result;
            return true;
        }


        private bool Reassign()
        {
            _taskResult.Reassign();
            var task = Interface.Add("RetainerTaskAsk", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while obtaining new venture target.");

            State       = WorkState.RetainerTaskAskOpen;
            _taskResult = IntPtr.Zero;
            _taskAsk    = task.Result;
            return true;
        }

        private bool Accept()
        {
            using var nextTalk = Bothers.SkipNextTalk();
            _taskResult.Confirm();
            var task = Interface.Add("SelectString", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while accepting venture loot.");

            State         = WorkState.RetainerMenuOpen;
            _taskResult   = IntPtr.Zero;
            _retainerMenu = task.Result;
            return true;
        }

        private bool Assign()
        {
            using var nextTalk = Bothers.SkipNextTalk();
            _taskAsk.Assign();
            var task = Interface.Add("SelectString", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while assigning venture.");

            State         = WorkState.RetainerMenuOpenDone;
            _taskAsk      = IntPtr.Zero;
            _retainerMenu = task.Result;
            return true;
        }

        private bool Quit()
        {
            using var nextTalk = Bothers.SkipNextTalk();
            _retainerMenu.Select(_retainerMenu.Count - 1);

            var task = Interface.Add("RetainerList", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while quitting retainer.");

            State         = WorkState.JobFinished;
            _retainerMenu = IntPtr.Zero;
            _list         = task.Result;
            return true;
        }
    }
}
