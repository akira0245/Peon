using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Internal.Gui;
using Dalamud.Plugin;

namespace Peon.Managers
{
    public enum WorkState
    {
        Error = -1,
        None  = 0,

        // Retainer
        RetainerListOpen       = 1,
        RetainerMenuOpen       = 2,
        RetainerTaskResultOpen = 3,
        RetainerTaskAskOpen    = 4,
        RetainerMenuOpen2      = 5,

        // Stables
        ChocoboMenuOpen = 1,
        StablesClean    = 2,
        StablesOpen     = 3,
        InventoryOpen   = 4,

        JobFinished = 100,
    }

    public class WorkManager
    {
        protected const    int              DefaultTimeOut = 3000;
        protected readonly TargetManager    _target;
        protected readonly AddonWatcher     _addons;
        protected readonly BotherHelper     _bothers;
        protected readonly InterfaceManager _interface;
        protected readonly ChatGui          _chat;

        protected WorkState _state;
        public    string    ErrorText = "";

        private static volatile   bool                     _jobRunning = false;
        protected static volatile CancellationTokenSource? CancelToken;

        public void Cancel()
        {
            if (CancelToken == null || CancelToken.IsCancellationRequested || CancelToken.Token.CanBeCanceled)
                return;

            CancelToken.Cancel();
            _chat.Print("Cancellation of current job requested.");
        }

        protected virtual WorkState SetInitialState()
            => throw new NotImplementedException();

        protected bool Failure(string text)
        {
            _state    = WorkState.Error;
            ErrorText = text;
            return false;
        }

        protected WorkManager(DalamudPluginInterface pluginInterface, TargetManager target, AddonWatcher addons, BotherHelper bothers,
            InterfaceManager                         iManager)
        {
            _chat      = pluginInterface.Framework.Gui.Chat;
            _target    = target;
            _addons    = addons;
            _bothers   = bothers;
            _interface = iManager;
        }

        protected void DoWork(Func<bool> stateHandler)
        {
            //if (_jobRunning)
            //{
            //    _chat.PrintError($"[{GetType().Name}] Can not start job. Job is already running.");
            //    return;
            //}

            try
            {
                _state      = SetInitialState();
                _jobRunning = true;
                CancelToken?.Dispose();
                CancelToken = new CancellationTokenSource();
                Task.Run(() => DoWorkTask(stateHandler), CancelToken.Token);
            }
            catch (Exception e)
            {
                _jobRunning = false;
                PluginLog.Error(e, "");
            }
        }

        protected void DoWorkTask(Func<bool> stateHandler)
        {
            while (_state != WorkState.JobFinished && !CancelToken!.IsCancellationRequested)
                if (_state == WorkState.Error)
                {
                    PluginLog.Error("Error during job: {Error:l}", ErrorText);
                    _chat.PrintError(ErrorText);
                    _state = WorkState.JobFinished;
                }
                else
                {
                    stateHandler();
                }

            _jobRunning = false;
        }
    }
}
