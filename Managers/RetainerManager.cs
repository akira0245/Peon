using System;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public sealed class RetainerManager : WorkManager
    {
        private int _retainerIdx;

        private PtrRetainerList       _list;
        private PtrRetainerTaskAsk    _taskAsk;
        private PtrRetainerTaskResult _taskResult;
        private PtrSelectString       _retainerMenu;

        private IntPtr _window;

        public RetainerManager(DalamudPluginInterface pluginInterface, TargetManager target, AddonWatcher addons, BotherHelper bothers,
            InterfaceManager                          interfaceManager)
            : base(pluginInterface, target, addons, bothers, interfaceManager)
        { }

        protected override WorkState SetInitialState()
        {
            _list = _interface.RetainerList();
            if (_list)
                return WorkState.RetainerListOpen;

            _retainerMenu = _interface.SelectString();
            if (_retainerMenu)
                return WorkState.RetainerMenuOpen;

            _taskAsk = _interface.RetainerTaskAsk();
            if (_taskAsk)
                return WorkState.RetainerTaskAskOpen;

            _taskResult = _interface.RetainerTaskResult();
            if (_taskResult)
                return WorkState.RetainerTaskResultOpen;

            return WorkState.None;
        }

        public void DoFullRetainer(int idx)
        {
            _retainerIdx = idx;
            DoWork(NextStep);
        }

        public void DoAllRetainers()
        {
            try
            {
                Task.Run(DoAllRetainerTask);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "");
            }
        }

        private void DoFullRetainerTask(int idx)
        {
            _retainerIdx = idx;
            while (_state != WorkState.JobFinished)
                NextStep();
        }

        private void DoAllRetainerTask()
        {
            OpenList();
            if (_state != WorkState.RetainerListOpen)
            {
                NextStep();
            }
            else
            {
                PtrRetainerList list  = _window;
                var             count = list.Count;
                for (var i = 0; i < count; ++i)
                    if (_state == WorkState.JobFinished || _state == WorkState.RetainerListOpen)
                    {
                        _state = WorkState.RetainerListOpen;
                        DoFullRetainerTask(i);
                    }
                    else
                    {
                        ResetState();
                        return;
                    }
            }

            ResetState();
        }

        private bool NextStep()
        {
            switch (_state)
            {
                case WorkState.Error:
                    PluginLog.Information(ErrorText);
                    _state = WorkState.JobFinished;
                    return false;
                case WorkState.None:                   return OpenList();
                case WorkState.RetainerListOpen:       return ContactRetainer();
                case WorkState.RetainerMenuOpen:       return ViewReport();
                case WorkState.RetainerTaskResultOpen: return Reassign();
                case WorkState.RetainerTaskAskOpen:    return Assign();
                case WorkState.RetainerMenuOpen2:      return Quit();
                case WorkState.JobFinished:            return true;
                default:                               return Failure("Unknown state reached.");
            }
        }

        private void ResetState()
        {
            _state       = WorkState.None;
            _retainerIdx = 0;
            _window      = IntPtr.Zero;
            ErrorText    = string.Empty;
        }

        private bool OpenList()
        {
            var task = _interface.Add("RetainerList", true, DefaultTimeOut);

            var targetTask = _target.Interact(300, a => a.Name == "Summoning Bell");
            targetTask.Wait();
            if (targetTask.Result != TargetingState.Success)
                return Failure($"Targeting failed {targetTask.Result}.");

            task.SafeWait();
            if (task.IsCanceled)
                return Failure("Timeout while opening RetainerList.");

            _state  = WorkState.RetainerListOpen;
            _window = task.Result;
            return true;
        }

        private bool ContactRetainer()
        {
            using var nextYesno = _bothers.SelectNextYesNo(true);

            if (!((PtrRetainerList) _window).Select(_retainerIdx))
            {
                Failure("Invalid index for RetainerList.");
                return false;
            }

            var task = _interface.Add("SelectString", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while contacting retainer.");
                return false;
            }

            _state  = WorkState.RetainerMenuOpen;
            _window = task.Result;
            return true;
        }

        private bool ViewReport()
        {
            if (!((PtrSelectString) _window).Select(new CompareString("report. (Complete)", MatchType.EndsWith)))
            {
                _state = WorkState.RetainerMenuOpen2;
                return false;
            }

            var task = _interface.Add("RetainerTaskResult", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while obtaining venture report.");
                return false;
            }

            _state  = WorkState.RetainerTaskResultOpen;
            _window = task.Result;
            return true;
        }

        private bool Reassign()
        {
            ((PtrRetainerTaskResult) _window).Reassign();
            var task = _interface.Add("RetainerTaskAsk", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while obtaining new venture target.");
                return false;
            }

            _state  = WorkState.RetainerTaskAskOpen;
            _window = task.Result;
            return true;
        }

        private bool Assign()
        {
            using var nextYesno = _bothers.SelectNextYesNo(true);
            ((PtrRetainerTaskAsk) _window).Assign();
            var task = _interface.Add("SelectString", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while assigning venture.");
                return false;
            }

            _state  = WorkState.RetainerMenuOpen2;
            _window = task.Result;
            return true;
        }

        private bool Quit()
        {
            using var       nextYesno = _bothers.SelectNextYesNo(true);
            PtrSelectString select    = _window;
            select.Select(select.Count - 1);

            var task = _interface.Add("RetainerList", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while quitting retainer.");
                return false;
            }

            _state  = WorkState.JobFinished;
            _window = task.Result;
            return true;
        }
    }
}
