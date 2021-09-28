using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using Peon.Modules;
using Peon.Utility;

namespace Peon.Managers
{
    public class ChocoboManager : WorkManager
    {
        private const int StableDelay = 500;
        public ChocoboManager(TargetManager target, AddonWatcher addons, BotherHelper bothers,
            InterfaceManager iManager)
            : base(target, addons, bothers, iManager)
        { }

        private PtrSelectString       _chocoboMenu;
        private PtrHousingChocoboList _stable;
        private PtrInventoryGrid[]    _inventory = new PtrInventoryGrid[4];

        protected override WorkState SetInitialState()
        {
            _chocoboMenu = Interface.SelectString();
            if (_chocoboMenu && _chocoboMenu.Description().Contains(StringId.ChocobosStabled.Value()))
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
                _                         => throw new InvalidEnumArgumentException(),
            };
        }

        private bool ContactStables()
        {
            var task = Interface.Add("SelectString", true, DefaultTimeOut,
                ptr => ((PtrSelectString) ptr).Description().Contains(StringId.ChocobosStabled.Value()));
            if (task.IsCompleted)
            {
                _chocoboMenu = task.Result;
                State        = WorkState.ChocoboMenuOpen;
                return true;
            }

            var targetTask = Targets.InteractWithoutKey(StringId.ChocoboStable.Value());
            switch (targetTask)
            {
                case TargetingState.ActorNotFound: return Failure("No chocobo stable in the vicinity.");
                case TargetingState.Unknown:
                case TargetingState.TimeOut:
                    return Failure("Unknown error.");
            }

            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not open Chocobo Menu.");

            State        = WorkState.ChocoboMenuOpen;
            _chocoboMenu = task.Result;
            return true;
        }

        private unsafe bool OpenStables()
        {
            _chocoboMenu.Select(StringId.TendChocobo.Cs());

            var task = Interface.Add("HousingChocoboList", true, DefaultTimeOut, ptr => ((PtrHousingChocoboList) ptr).ChocoboCount > 0);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not open stable menu.");

            State        = WorkState.StablesOpen;
            _chocoboMenu = IntPtr.Zero;
            _stable      = task.Result;
            Wait(Task.Delay(StableDelay));
            return true;
        }

        private bool CleanStables()
        {
            using var nextYesno = Bothers.SelectNextYesNo(true);
            if (_chocoboMenu.Description().Contains(StringId.StableStatusGood.Value()))
            {
                State = WorkState.StablesClean;
                return true;
            }

            _chocoboMenu.Select(StringId.CleanStable.Cs());

            var task = Interface.Add("SelectYesNo", false, DefaultTimeOut / 2);
            Wait(task);
            if (!task.IsCompleted || task.Result != IntPtr.Zero)
                return Failure("YesNo did not open for cleaning stables.");

            task = Interface.AddInverted("SelectYesNo", false, DefaultTimeOut / 2);
            Wait(task);
            if (!task.IsCompleted || task.Result != IntPtr.Zero || !_chocoboMenu.Description().Contains(StringId.StableStatusGood.Value()))
                return Failure("Could not clean stables.");

            _chocoboMenu = IntPtr.Zero;
            State        = WorkState.None;
            return true;
        }

        private unsafe void RestingBug(IntPtr plugin, IntPtr data)
        {
            PtrTextError error = plugin;
            if (error.Text().Contains(StringId.ChocoboIsResting.Value()))
            {
                Task.Run(() =>
                {
                    PluginLog.Debug("Error triggered, trying again to contact chocobo again.");
                    Wait(Task.Delay(StableDelay));
                    if (_stable.Pointer != null)
                        _stable.SelectNextTrainableChocobo();
                });
            }
        }

        private bool ContactChocobo()
        {
            Addons.OnTextErrorChange += RestingBug;
            if (!_stable.SelectNextTrainableChocobo())
            {
                Addons.OnTextErrorChange -= RestingBug;
                _stable                  =  IntPtr.Zero;
                State                    =  WorkState.JobFinished;
                return true;
            }

            var task = Interface.Add("InventoryGrid0E", true, DefaultTimeOut);
            Wait(task);
            Addons.OnTextErrorChange -= RestingBug;
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not train chocobo.");

            _stable       = IntPtr.Zero;
            _inventory[0] = task.Result;
            _inventory[1] = Interface.InventoryGrid(1);
            _inventory[2] = Interface.InventoryGrid(2);
            _inventory[3] = Interface.InventoryGrid(3);
            State         = WorkState.InventoryOpen;
            return true;
        }

        private bool FeedChocobo()
        {
            using var nextYesno = Bothers.SelectNextYesNo(true);
            Wait(Task.Delay(StableDelay));
            if (!_inventory.Any(inventory => inventory.FeedChocobo()))
                return Failure("No chocobo food available.");

            var task = Interface.Add("HousingChocoboList", true, DefaultTimeOut * 2);
            Wait(task);
            if (!task.IsCompleted || task.Result == IntPtr.Zero)
                return Failure("Could not feed chocobo.");

            State         = WorkState.StablesOpen;
            _inventory[0] = IntPtr.Zero;
            _stable       = task.Result;
            Wait(Task.Delay(StableDelay));
            return true;
        }
    }
}
