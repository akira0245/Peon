using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrContextIconMenu
    {
        public AddonContextIconMenu* Pointer;

        public static implicit operator PtrContextIconMenu(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonContextIconMenu>(ptr) };

        public static implicit operator bool(PtrContextIconMenu ptr)
            => ptr.Pointer != null;

        public void Select(int idx)
        {
            Module.ClickList(Pointer, (AtkComponentNode*) Pointer->AtkUnitBase.UldManager.NodeList[2], idx);
        }
    }
}
