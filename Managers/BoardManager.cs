using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Peon.Modules;

namespace Peon.Managers
{
    public class BoardManager : WorkManager
    {
        private new const int                 DefaultTimeOut = 1000;
        private           PtrHousingSignBoard _board;
        private           PtrSelectString     _select;
        private           bool                _buyCompany;
        private           bool                _killOnFailure;
        private           bool                _keepRetrying;
        private           int                 _minDelayMs;
        private           int                 _maxDelayMs;

        public BoardManager(TargetManager target, AddonWatcher addons, BotherHelper bothers, InterfaceManager iManager)
            : base(target, addons, bothers, iManager)
        { }

        protected override WorkState SetInitialState()
        {
            _board = Interface.HousingSignBoard();
            if (_board )
                return WorkState.BoardPlacardOpen;

            _select = Interface.SelectString();
            if (_select && _select.Description() == "Select a buyer.")
                return WorkState.BoardBuyerSelect;

            return WorkState.None;
        }

        public void StartBuying(int minDelayMs, int maxDelayMs, bool forCompany, bool killOnFailure = false, bool keepRetrying = true)
        {
            Debug.Assert(minDelayMs > 0);
            Debug.Assert(minDelayMs <= maxDelayMs);
            _minDelayMs    = minDelayMs;
            _maxDelayMs    = maxDelayMs;
            _buyCompany    = forCompany;
            _killOnFailure = killOnFailure;
            _keepRetrying  = keepRetrying;
            DoWork(TryBuy);
        }

        private bool Kill()
        {
            Wait(Task.Delay(5000));
            Process.GetCurrentProcess().Kill();
            return false;
        }

        private bool FailureOrRetry(string text)
        {
            if (_keepRetrying)
            {
                Targets.Target("Placard");
                return Retry();
            }

            return Failure(text);
        }

        private bool TryBuy()
        {
            return State switch
            {
                WorkState.None             => ContactPlacard(),
                WorkState.BoardPlacardOpen => Purchase(),
                WorkState.BoardBuyerSelect => SelectBuyer(),
                WorkState.BoardWait        => Wait(),
                _                          => throw new InvalidEnumArgumentException(),
            };
        }

        private bool Purchase()
        {
            if (!_board.IsPurchasable())
                return _killOnFailure ? Kill() : Failure("House not purchasable.");

            var task = Interface.Add("SelectString", true, DefaultTimeOut,
                ptr => ((PtrSelectString) ptr).Description() == "Select a buyer." && ((PtrSelectString)ptr).Count >= 2);
            _board.Click(true);
            Wait(task, DefaultTimeOut);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return FailureOrRetry("SelectString did not open for purchase.");

            _board  = IntPtr.Zero;
            _select = task.Result;
            State   = WorkState.BoardBuyerSelect;
            return true;
        }

        private bool SelectBuyer()
        {
            if (_select.Count > 2)
                return _killOnFailure ? Kill() : Failure("Already own a plot.");

            using var bother = Bothers.SelectNextYesNo(true);
            _select.Select(_buyCompany ? 1 : 0);

            var       task   = Interface.AddInverted("HousingSignBoard", false, DefaultTimeOut);
            Wait(task, DefaultTimeOut);
            if (!task.IsCompleted || task.Result != IntPtr.Zero)
                return FailureOrRetry("Could not select buyer.");

            _board  = IntPtr.Zero;
            _select = IntPtr.Zero;
            State   = WorkState.BoardWait;
            Targets.Target("Placard");
            return true;
        }

        private bool Wait()
        {
            Targets.Target("Placard");
            var milliseconds = RandomNumberGenerator.GetInt32(_minDelayMs, _maxDelayMs);
            PluginLog.Debug("Waiting for {Time} milliseconds.", milliseconds);
            var task = Task.Delay(milliseconds, CancelToken?.Token ?? new CancellationToken());
            Wait(task);
            if (!task.IsCompleted)
                return FailureOrRetry("Could not wait.");

            State = WorkState.None;
            return true;
        }

        private unsafe bool ContactPlacard()
        {
            var task = Interface.Add("HousingSignBoard", true, DefaultTimeOut * 3, ptr => ((PtrHousingSignBoard) ptr).IsReady());
            if (task.IsCompleted)
            {
                _board = task.Result;
                State  = WorkState.BoardPlacardOpen;
                return true;
            }

            var targetTask = Targets.InteractWithoutKey("Placard");
            switch (targetTask)
            {
                case TargetingState.ActorNotFound: return Failure("No placard in the vicinity.");
                case TargetingState.Unknown:
                case TargetingState.TimeOut:       return FailureOrRetry("Could not open Placard.");
            }

            Wait(task, DefaultTimeOut);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return FailureOrRetry("Unknown error.");

            State  = WorkState.BoardPlacardOpen;
            _board = task.Result;
            return true;
        }
    }
}
