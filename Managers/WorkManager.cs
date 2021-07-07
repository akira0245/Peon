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
        RetainerListOpen             = 1,
        RetainerMenuOpen             = 2,
        RetainerBankOpen             = 3,
        RetainerMenuOpenBank         = 5,
        RetainerTaskResultOpen       = 6,
        RetainerTaskAskOpen          = 7,
        RetainerMenuOpenDone         = 8,
        RetainerSelectCategoryOpen   = 9,
        RetainerSelectLevelRangeOpen = 10,
        RetainerTaskListOpen         = 11,

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
        protected readonly TargetManager    Target;
        protected readonly AddonWatcher     Addons;
        protected readonly BotherHelper     Bothers;
        protected readonly InterfaceManager Interface;
        protected readonly ChatGui          Chat;

        protected WorkState State;
        public    string    ErrorText = "";

        private volatile   bool                     _jobRunning = false;
        protected volatile CancellationTokenSource? CancelToken;

        public void Cancel()
        {
            if (CancelToken == null || CancelToken.IsCancellationRequested || !CancelToken.Token.CanBeCanceled)
                return;

            CancelToken.Cancel();
            Chat.Print("Cancellation of current job requested.");
        }

        protected void Wait<T>(Task<T> task)
        {
            try
            {
                task.Wait(CancelToken!.Token);
            }
            catch (OperationCanceledException)
            { }
        }

        protected void Wait(Task task)
            => task.Wait(CancelToken!.Token);

        protected virtual WorkState SetInitialState()
            => throw new NotImplementedException();

        protected bool Failure(string text)
        {
            State     = WorkState.Error;
            ErrorText = text;
            return false;
        }

        protected WorkManager(DalamudPluginInterface pluginInterface, TargetManager target, AddonWatcher addons, BotherHelper bothers,
            InterfaceManager iManager)
        {
            Chat      = pluginInterface.Framework.Gui.Chat;
            Target    = target;
            Addons    = addons;
            Bothers   = bothers;
            Interface = iManager;
        }

        protected void DoWork(Func<bool> stateHandler)
        {
            if (_jobRunning)
            {
                Chat.PrintError($"[{GetType().Name}] Can not start job. Job is already running.");
                return;
            }

            try
            {
                State       = SetInitialState();
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
            try
            {
                while (State != WorkState.JobFinished && !CancelToken!.IsCancellationRequested)
                    if (State == WorkState.Error)
                    {
                        PluginLog.Error("Error during job: {Error:l}", ErrorText);
                        Chat.PrintError(ErrorText);
                        State = WorkState.JobFinished;
                    }
                    else
                    {
                        stateHandler();
                    }

                _jobRunning = false;
            }
            catch (Exception)
            {
                _jobRunning = false;
                throw;
            }
        }
    }
}
