using System;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Peon.Modules
{
    public unsafe struct PtrRequest
    {
        public AddonRequest* Pointer;

        public static implicit operator PtrRequest(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonRequest>(ptr) };

        public static implicit operator bool(PtrRequest ptr)
            => ptr.Pointer != null;

        public void Fill()
        {
            Module.ClickAddon(Pointer, Pointer->AtkUnitBase.UldManager.NodeList[10], (EventType) 55, 12);
        }

        public void HandOver()
        {
            Module.ClickAddon(Pointer, Pointer->HandOverButton, EventType.Change, 7);
        }
    }
}
