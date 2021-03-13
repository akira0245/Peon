using System;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public enum RetainerState
    {
        Error = -1,
        None  = 0,
        RetainerListOpen,
        RetainerMenuOpen,
        RetainerTaskResultOpen,
        RetainerTaskAskOpen,
        RetainerMenuOpen2,

        TaskFinished,
    }

    public class RetainerManager
    {
        private const    int              DefaultTimeOut = 3000;
        private readonly AddonWatcher     _addons;
        private readonly BotherHelper     _bothers;
        private readonly InterfaceManager _interface;

        private RetainerState _state = RetainerState.None;
        private int           _retainerIdx;

        private PtrRetainerList       _list;
        private PtrRetainerTaskAsk    _taskAsk;
        private PtrRetainerTaskResult _taskResult;
        private PtrSelectString       _retainerMenu;

        private IntPtr _window;


        public string ErrorText = "";

        public RetainerManager(AddonWatcher addons, BotherHelper bothers, InterfaceManager interfaceManager)
        {
            _addons    = addons;
            _bothers   = bothers;
            _interface = interfaceManager;
        }

        private RetainerState GetInitialState()
        {
            _list = _interface.RetainerList();
            if (_list)
                return RetainerState.RetainerListOpen;

            _retainerMenu = _interface.SelectString();
            if (_retainerMenu)
                return RetainerState.RetainerMenuOpen;

            _taskAsk = _interface.RetainerTaskAsk();
            if (_taskAsk)
                return RetainerState.RetainerTaskAskOpen;

            _taskResult = _interface.RetainerTaskResult();
            if (_taskResult)
                return RetainerState.RetainerTaskResultOpen;

            return RetainerState.None;
        }


        public void DoFullRetainer(int idx)
        {
            try
            {
                Task.Run(() =>
                {
                    DoFullRetainerTask(idx);
                    ResetState();
                });
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "");
            }
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
            while (_state != RetainerState.TaskFinished)
                NextStep();
        }

        private void DoAllRetainerTask()
        {
            OpenList();
            if (_state != RetainerState.RetainerListOpen)
            {
                NextStep();
            }
            else
            {
                PtrRetainerList list  = _window;
                var             count = list.Count;
                for (var i = 0; i < count; ++i)
                    if (_state == RetainerState.TaskFinished || _state == RetainerState.RetainerListOpen)
                    {
                        _state = RetainerState.RetainerListOpen;
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
                case RetainerState.Error:
                    PluginLog.Information(ErrorText);
                    _state = RetainerState.TaskFinished;
                    return false;
                case RetainerState.None:                   return OpenList();
                case RetainerState.RetainerListOpen:       return ContactRetainer();
                case RetainerState.RetainerMenuOpen:       return ViewReport();
                case RetainerState.RetainerTaskResultOpen: return Reassign();
                case RetainerState.RetainerTaskAskOpen:    return Assign();
                case RetainerState.RetainerMenuOpen2:      return Quit();
                case RetainerState.TaskFinished:           return true;
                default:                                   return Failure("Unknown state reached.");
            }
        }

        private void ResetState()
        {
            _state       = RetainerState.None;
            _retainerIdx = 0;
            _window      = IntPtr.Zero;
            ErrorText    = string.Empty;
        }

        private bool Failure(string text)
        {
            _state    = RetainerState.Error;
            ErrorText = text;
            return false;
        }

        private bool OpenList()
        {
            var task = _interface.Add("RetainerList", true, DefaultTimeOut);

            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while opening RetainerList.");
                return false;
            }

            _state  = RetainerState.RetainerListOpen;
            _window = task.Result;
            return true;
        }

        private bool ContactRetainer()
        {
            _bothers.SkipNextTalk = true;

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

            _state  = RetainerState.RetainerMenuOpen;
            _window = task.Result;
            return true;
        }

        private bool ViewReport()
        {
            if (!((PtrSelectString) _window).Select(new CompareString("report. (Complete)", MatchType.EndsWith)))
            {
                _state = RetainerState.RetainerMenuOpen2;
                return false;
            }

            var task = _interface.Add("RetainerTaskResult", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while obtaining venture report.");
                return false;
            }

            _state  = RetainerState.RetainerTaskResultOpen;
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

            _state  = RetainerState.RetainerTaskAskOpen;
            _window = task.Result;
            return true;
        }

        private bool Assign()
        {
            _bothers.SkipNextTalk = true;
            ((PtrRetainerTaskAsk) _window).Assign();
            var task = _interface.Add("SelectString", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while assigning venture.");
                return false;
            }

            _state  = RetainerState.RetainerMenuOpen2;
            _window = task.Result;
            return true;
        }

        private bool Quit()
        {
            _bothers.SkipNextTalk = true;
            PtrSelectString select = _window;
            select.Select(select.Count - 1);

            var task = _interface.Add("RetainerList", true, DefaultTimeOut);
            task.SafeWait();
            if (task.IsCanceled)
            {
                Failure("Timeout while quitting retainer.");
                return false;
            }

            _state  = RetainerState.TaskFinished;
            _window = task.Result;
            return true;
        }
    }
}
