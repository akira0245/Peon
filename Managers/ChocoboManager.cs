using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            _chocoboMenu = Interface.SelectString();
            if (_chocoboMenu)
                return WorkState.ChocoboMenuOpen;

            _stable = Interface.HousingChocoboList();
            if (_stable)
                return WorkState.StablesOpen;

            _inventory[0] = Interface.InventoryGrid(0);
            if (!_inventory[0])
                return WorkState.None;

            _inventory[1] = Interface.InventoryGrid(1);
            _inventory[2] = Interface.InventoryGrid(2);
            _inventory[3] = Interface.InventoryGrid(3);
            return WorkState.InventoryOpen;

        }

        public void FeedAllChocobos()
            => DoWork(FeedAll);

        private bool FeedAll()
        {
            return State switch
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
            var task = Interface.Add("SelectString", false, DefaultTimeOut);
            if (task.IsCompleted)
            {
                _chocoboMenu = task.Result;
                State       = WorkState.ChocoboMenuOpen;
                return true;
            }

            var targetTask = Target.Interact("Chocobo Stable", DefaultTimeOut / 6);

            Wait(targetTask);
            switch (targetTask.IsCompleted ? targetTask.Result : TargetingState.TimeOut)
            {
                case TargetingState.ActorNotFound:   return Failure("No chocobo stable in the vicinity.");
                case TargetingState.ActorNotInRange: return Failure("Too far away from chocobo stable.");
                case TargetingState.TimeOut:         
                case TargetingState.Unknown:
                    return Failure("Unknown error.");
            }

            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not open Chocobo Menu.");

            State       = WorkState.ChocoboMenuOpen;
            _chocoboMenu = task.Result;
            return true;
        }

        private bool OpenStables()
        {
            var task = Interface.Add("HousingChocoboList", false, DefaultTimeOut);
            _chocoboMenu.Select(new CompareString("Tend to a Specified Chocobo", MatchType.Equal));
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not open stable menu.");

            State       = WorkState.StablesOpen;
            _chocoboMenu = IntPtr.Zero;
            _stable      = task.Result;
            return true;
        }

        private bool CleanStables()
        {
            using var nextYesno = Bothers.SelectNextYesNo (true);
            if (_chocoboMenu.Description().Contains("Good"))
            {
                State = WorkState.StablesClean;
                return true;
            }

            _chocoboMenu.Select(new CompareString("Clean Stable", MatchType.Equal));

            var task = Interface.AddInverted("SelectYesNo", false, DefaultTimeOut / 2);
            Wait(task);
            if (!task.IsCompleted || task.Result != IntPtr.Zero)
                return Failure("Could not clean stables.");

            _chocoboMenu             = IntPtr.Zero;
            State                   = WorkState.None;
            return true;
        }

        private bool ContactChocobo()
        {
            var task = Interface.Add("InventoryGrid0E", true, DefaultTimeOut);
            if (!_stable.SelectNextTrainableChocobo())
            {
                _stable = IntPtr.Zero;
                State  = WorkState.JobFinished;
                return true;
            }

            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not train chocobo.");

            _stable       = IntPtr.Zero;
            _inventory[0] = task.Result;
            _inventory[1] = Interface.InventoryGrid(1);
            _inventory[2] = Interface.InventoryGrid(2);
            _inventory[3] = Interface.InventoryGrid(3);
            State        = WorkState.InventoryOpen;
            return true;
        }

        private bool FeedChocobo()
        {
            using var nextYesno = Bothers.SelectNextYesNo(true);
            Wait(Task.Delay(100));
            if (!_inventory.Any(inventory => inventory.FeedChocobo()))
                return Failure("No chocobo food available.");

            var task = Interface.Add("HousingChocoboList", true, DefaultTimeOut * 2);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not feed chocobo.");

            State         = WorkState.StablesOpen;
            _inventory[0] = IntPtr.Zero;
            _stable       = task.Result;
            return true;

        }
    }
}
