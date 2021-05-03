using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrMainCommand
    {
        public        AtkUnitBase* Pointer;

        public static implicit operator PtrMainCommand(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrMainCommand ptr)
            => ptr.Pointer != null;

        public void System()
        {
            var button = Pointer->UldManager.NodeList[8];
            Module.ClickAddon(Pointer, button, EventType.Change, 7);
        }
    }
}
