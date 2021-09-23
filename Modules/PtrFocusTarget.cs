using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrFocusTarget
    {
        public AtkUnitBase* Pointer;

        public static implicit operator PtrFocusTarget(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrFocusTarget ptr)
            => ptr.Pointer != null;

        public string TargetName()
            => Module.TextNodeToString((AtkTextNode*)Pointer->UldManager.NodeList[10]);

        public void Interact()
        {
            var       target = Pointer->UldManager.NodeList[9];
            using var helper = new Module.ClickHelper(Pointer, target);
            helper.Data[5] = (byte*) 0x184003;
            helper.Data[7] = (byte*) target;
            Module.ClickAddonHelper(Pointer, target, (EventType) 3, 0, helper.Data);
        }
    }
}
