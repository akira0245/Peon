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

        private const ulong SystemFlags = 1840019; // Unknown, but consistent

        public void System()
        {
            var button = Pointer->UldManager.NodeList[8];

            using var helper = new Module.ClickHelper(Pointer, button);
            helper.Data[3] = (byte*) 7;
            Module.ClickAddonHelper(Pointer, button, EventType.Change, 7, helper.Data);
        }
    }
}
