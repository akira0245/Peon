using System;
using System.ComponentModel;
using System.Threading;
using Dalamud.Plugin;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public class ChocoboManager : WorkManager
    {
        public ChocoboManager(DalamudPluginInterface pluginInterface, TargetManager target, AddonWatcher addons, BotherHelper bothers,
            InterfaceManager                         iManager)
            : base(pluginInterface, target, addons, bothers, iManager)
        { }

        private PtrSelectString       _chocoboMenu;
        private PtrHousingChocoboList _stable;
        private PtrInventoryGrid[]    _inventory = new PtrInventoryGrid[4];

        protected override WorkState SetInitialState()
        {
            _chocoboMenu = _interface.SelectString();
            if (_chocoboMenu)
                return WorkState.ChocoboMenuOpen;

            _stable = _interface.HousingChocoboList();
            if (_stable)
                return WorkState.StablesOpen;

            _inventory[0] = _interface.InventoryGrid(0);
            if (!_inventory[0])
                return WorkState.None;

            _inventory[1] = _interface.InventoryGrid(1);
            _inventory[2] = _interface.InventoryGrid(2);
            _inventory[3] = _interface.InventoryGrid(3);
            return WorkState.InventoryOpen;

        }

        public void FeedAllChocobos()
            => DoWork(FeedAll);

        private bool FeedAll()
        {
            return _state switch
            {
                WorkState.None            => ContactStables(),
                WorkState.ChocoboMenuOpen => CleanStables(),
                WorkState.StablesClean    => OpenStables(),
                WorkState.StablesOpen     => ContactChocobo(),
                WorkState.InventoryOpen   => FeedChocobo(),
                _                         => throw new InvalidEnumArgumentException()
            };
        }

        private bool ContactStables()
        {
            var task = _interface.Add("SelectString", false, DefaultTimeOut);
            if (task.IsCompleted)
            {
                _chocoboMenu = task.Result;
                _state       = WorkState.ChocoboMenuOpen;
                return true;
            }

            var targetTask = _target.Interact("Chocobo Stable", DefaultTimeOut / 6);

            targetTask.Wait(CancelToken!.Token);
            switch (targetTask.IsCompleted ? targetTask.Result : TargetingState.TimeOut)
            {
                case TargetingState.ActorNotFound:   return Failure("No chocobo stable in the vicinity.");
                case TargetingState.ActorNotInRange: return Failure("Too far away from chocobo stable.");
                case TargetingState.TimeOut:         
                case TargetingState.Unknown:
                    return Failure("Unknown error.");
            }

            task.Wait(CancelToken!.Token);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not open Chocobo Menu.");

            _state       = WorkState.ChocoboMenuOpen;
            _chocoboMenu = task.Result;
            return true;
        }

        private bool OpenStables()
        {
            var task = _interface.Add("HousingChocoboList", false, DefaultTimeOut);
            _chocoboMenu.Select(new CompareString("Tend to a Specified Chocobo", MatchType.Equal));
            task.Wait(CancelToken!.Token);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not open stable menu.");

            _state       = WorkState.StablesOpen;
            _chocoboMenu = IntPtr.Zero;
            _stable      = task.Result;
            return true;
        }

        private bool CleanStables()
        {
            using var nextYesno = _bothers.SelectNextYesNo (true);
            if (_chocoboMenu.Description().Contains("Good"))
            {
                _state = WorkState.StablesClean;
                return true;
            }

            _chocoboMenu.Select(new CompareString("Clean Stable", MatchType.Equal));

            var task = _interface.AddInverted("SelectYesNo", false, DefaultTimeOut / 2);
            task.Wait(CancelToken!.Token);
            if (!task.IsCompleted || task.Result != IntPtr.Zero)
                return Failure("Could not clean stables.");

            _chocoboMenu             = IntPtr.Zero;
            _state                   = WorkState.None;
            return true;
        }

        private bool ContactChocobo()
        {
            var task = _interface.Add("InventoryGrid0E", true, DefaultTimeOut);
            if (!_stable.SelectNextTrainableChocobo())
            {
                _stable = IntPtr.Zero;
                _state  = WorkState.JobFinished;
                return true;
            }
            task.Wait(CancelToken!.Token);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not train chocobo.");

            _stable       = IntPtr.Zero;
            _inventory[0] = task.Result;
            _inventory[1] = _interface.InventoryGrid(1);
            _inventory[2] = _interface.InventoryGrid(2);
            _inventory[3] = _interface.InventoryGrid(3);
            _state        = WorkState.InventoryOpen;
            return true;
        }

        private bool FeedChocobo()
        {
            using var nextYesno = _bothers.SelectNextYesNo(true);
            foreach (var inventory in _inventory)
            {
                if (!inventory.FeedChocobo())
                    continue;
                var task = _interface.Add("HousingChocoboList", false, DefaultTimeOut * 2);
                task.Wait(CancelToken!.Token);
                if (!task.IsCompleted || task.Result == IntPtr.Zero)
                    return Failure("Could not feed chocobo.");

                _state        = WorkState.StablesOpen;
                _inventory[0] = IntPtr.Zero;
                _stable       = task.Result;
                return true;
            }

            return Failure("No chocobo food available.");
        }
    }
}
