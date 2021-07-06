using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Peon.Bothers;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    [Flags]
    public enum RetainerMode
    {
        Venture       = 0x01,
        Resend        = 0x02,
        Gil           = 0x04,
        LootWithGil   = 0x05,
        ResendWithGil = 0x07,
    }

    public sealed class RetainerManager : WorkManager
    {
        private int _retainerIdx;

        private PtrRetainerList       _list;
        private PtrRetainerTaskAsk    _taskAsk;
        private PtrRetainerTaskResult _taskResult;
        private PtrSelectString       _retainerMenu;
        private PtrBank               _bank;

        public RetainerManager(DalamudPluginInterface pluginInterface, TargetManager target, AddonWatcher addons, BotherHelper bothers,
            InterfaceManager                          interfaceManager)
            : base(pluginInterface, target, addons, bothers, interfaceManager)
        { }

        protected override WorkState SetInitialState()
        {
            _list = Interface.RetainerList();
            if (_list)
                return WorkState.RetainerListOpen;

            _retainerMenu = Interface.SelectString();
            if (_retainerMenu)
                return WorkState.RetainerMenuOpen;

            _taskAsk = Interface.RetainerTaskAsk();
            if (_taskAsk)
                return WorkState.RetainerTaskAskOpen;

            _taskResult = Interface.RetainerTaskResult();
            if (_taskResult)
                return WorkState.RetainerTaskResultOpen;

            return WorkState.None;
        }

        public void DoAllRetainers(RetainerMode mode)
            => DoWork(PrependMulti(mode));

        public void DoSpecificRetainers(RetainerMode mode, params int[] which)
        {
            foreach (var idx in which)
            {
                _retainerIdx = idx;
                DoWork(ChooseFunc(mode));
            }
        }

        private Func<bool> ChooseFunc(RetainerMode mode)
        {
            return mode switch
            {
                RetainerMode.ResendWithGil => ResendOneRetainerWithGil,
                RetainerMode.Resend        => ResendOneRetainer,
                RetainerMode.LootWithGil   => LootOneRetainer,
                RetainerMode.Venture       => LootOneRetainerVenture,
                RetainerMode.Gil           => LootOneRetainerGil,
                _                          => throw new InvalidEnumArgumentException(),
            };
        }

        private Func<bool> PrependMulti(RetainerMode mode)
        {
            return () =>
            {
                switch (State)
                {
                    case WorkState.RetainerListOpen:
                        _retainerIdx = -1;
                        var data = _list.Info();
                        _retainerIdx = data.SelectFirstOr(d => d.Venture == VentureState.Complete && mode.HasFlag(RetainerMode.Venture)
                         || d.Gil > 0 && mode.HasFlag(RetainerMode.Gil), d => d.Index, -1);
                        if (_retainerIdx >= 0)
                            return ContactRetainer();

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

        private bool ResendOneRetainer()
        {
            return State switch
            {
                WorkState.None                   => OpenList(),
                WorkState.RetainerListOpen       => ContactRetainer(),
                WorkState.RetainerMenuOpen       => ViewReport(),
                WorkState.RetainerTaskResultOpen => Reassign(),
                WorkState.RetainerTaskAskOpen    => Assign(),
                WorkState.RetainerMenuOpenDone   => Quit(),
                _                                => Failure("Unknown state reached."),
            };
        }

        private bool ResendOneRetainerWithGil()
        {
            return State switch
            {
                WorkState.None                   => OpenList(),
                WorkState.RetainerListOpen       => ContactRetainer(),
                WorkState.RetainerMenuOpen       => OpenBank(),
                WorkState.RetainerBankOpen       => TakeGil(),
                WorkState.RetainerMenuOpenBank   => ViewReport(),
                WorkState.RetainerTaskResultOpen => Reassign(),
                WorkState.RetainerTaskAskOpen    => Assign(),
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
                WorkState.RetainerMenuOpen       => OpenBank(),
                WorkState.RetainerBankOpen       => TakeGil(),
                WorkState.RetainerMenuOpenBank   => ViewReport(),
                WorkState.RetainerTaskResultOpen => Accept(),
                WorkState.RetainerMenuOpenDone   => Quit(),
                _                                => Failure("Unknown state reached."),
            };
        }

        private bool LootOneRetainerVenture()
        {
            return State switch
            {
                WorkState.None                   => OpenList(),
                WorkState.RetainerListOpen       => ContactRetainer(),
                WorkState.RetainerMenuOpen       => ViewReport(),
                WorkState.RetainerTaskResultOpen => Accept(),
                WorkState.RetainerMenuOpenDone   => Quit(),
                _                                => Failure("Unknown state reached."),
            };
        }

        private bool LootOneRetainerGil()
        {
            return State switch
            {
                WorkState.None                 => OpenList(),
                WorkState.RetainerListOpen     => ContactRetainer(),
                WorkState.RetainerMenuOpen     => OpenBank(),
                WorkState.RetainerBankOpen     => TakeGil(),
                WorkState.RetainerMenuOpenBank => Quit(),
                _                              => Failure("Unknown state reached."),
            };
        }

        private bool OpenList()
        {
            var task = Interface.Add("RetainerList", true, DefaultTimeOut);

            var targetTask = Target.Interact("Summoning Bell", DefaultTimeOut / 6);
            Wait(targetTask);
            if (!targetTask.IsCompleted || targetTask.Result != TargetingState.Success)
                return Failure($"Targeting failed {targetTask.Result}.");

            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while opening RetainerList.");

            State = WorkState.RetainerListOpen;
            _list  = task.Result;
            return true;
        }

        private void SetNextRetainer(bool handleGil)
        {
            var data = _list.Info();
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

            State        = WorkState.RetainerMenuOpen;
            _list         = IntPtr.Zero;
            _retainerMenu = task.Result;
            return true;
        }

        private bool OpenBank()
        {
            if (!_retainerMenu.Select(new CompareString("Entrust or withdraw gil.", MatchType.Equal)))
                return Failure("Could not find bank button.");

            var task = Interface.Add("Bank", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while opening bank.");

            State        = WorkState.RetainerBankOpen;
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

            State        = WorkState.RetainerMenuOpenBank;
            _bank         = IntPtr.Zero;
            _retainerMenu = task.Result;
            return true;
        }


        private bool ViewReport()
        {
            if (!_retainerMenu.Select(new CompareString("report. (Complete)", MatchType.EndsWith)))
            {
                State = WorkState.RetainerMenuOpenDone;
                return true;
            }

            var task = Interface.Add("RetainerTaskResult", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while obtaining venture report.");

            State        = WorkState.RetainerTaskResultOpen;
            _retainerMenu = IntPtr.Zero;
            _taskResult   = task.Result;
            return true;
        }

        private bool Reassign()
        {
            _taskResult.Reassign();
            var task = Interface.Add("RetainerTaskAsk", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while obtaining new venture target.");

            State      = WorkState.RetainerTaskAskOpen;
            _taskResult = IntPtr.Zero;
            _taskAsk    = task.Result;
            return true;
        }

        private bool Accept()
        {
            _taskResult.Confirm();
            var task = Interface.Add("SelectString", true, DefaultTimeOut);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Timeout while accepting venture loot.");

            State        = WorkState.RetainerMenuOpenDone;
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

            State        = WorkState.RetainerMenuOpenDone;
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

            State        = WorkState.JobFinished;
            _retainerMenu = IntPtr.Zero;
            _list         = task.Result;
            return true;
        }
    }
}
