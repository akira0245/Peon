using System;
using System.Runtime.InteropServices;
using Peon.SeFunctions;

namespace Peon.Managers
{
    public unsafe class TimerManager
    {
        private readonly InterfaceManager   _interface;
        private readonly PeonTimers         _timers;
        private readonly RetainerContainer* _retainers;
        private          Retainer*          _retainerList;

        public TimerManager(InterfaceManager i)
        {
            _interface = i;
            _timers    = Peon.Timers;
            _retainers = (RetainerContainer*) new StaticRetainerContainer(Dalamud.SigScanner).Address;
        }

        private bool UpdateRetainer()
        {
            if (_retainers == null || _retainers->Ready != 1)
                return false;
            _retainerList = (Retainer*) _retainers->Retainers;

            var playerName =
                $"{Dalamud.ClientState.LocalPlayer!.Name} ({Dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name})";

            var changes = false;
            for (var i = 0; i < _retainers->RetainerCount; ++i)
            {
                var retainer = _retainerList[i];
                var name     = Marshal.PtrToStringUTF8((IntPtr) retainer.Name)!;
                var time     = new DateTime((retainer.VentureComplete + 62135596800) * TimeSpan.TicksPerSecond , DateTimeKind.Utc);
                changes |= _timers.AddOrUpdateRetainer(playerName, name, time);
            }
            return changes;
        }

        private unsafe bool UpdateMachines()
        {
            var selectString = _interface.SelectString();
            if (!selectString || !InterfaceManager.IsReady(&selectString.Pointer->AtkUnitBase))
                return false;
            var desc         = selectString.Description();
            if (!(desc.EndsWith("Select an airship.") || desc.EndsWith("Select a submersible.")))
                return false;

            var fcName = $"{Dalamud.ClientState.LocalPlayer!.CompanyTag} ({Dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name})";
            return false;
        }

        public void Update(bool retainer, bool machines)
        {
            if (Dalamud.ClientState.LocalPlayer == null)
                return;

            var saveConfig = retainer && UpdateRetainer();
            saveConfig |= machines && UpdateMachines();
            if (saveConfig)
                _timers.Save();
        }
    }
}
