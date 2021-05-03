using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrRetainerTaskResult
    {
        private const int          ReassignOffset   = 0x240;
        private const int          ConfirmOffset    = 0x248;
        private const int          ConfirmButtonId  = 2;
        private const int          ReassignButtonId = 3;
        public        AtkUnitBase* Pointer;

        public static implicit operator PtrRetainerTaskResult(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrRetainerTaskResult ptr)
            => ptr.Pointer != null;

        public AtkComponentButton* ConfirmButton
            => (AtkComponentButton*) ((byte*) Pointer + ConfirmOffset);

        public AtkComponentButton* ReassignButton
            => (AtkComponentButton*) ((byte*) Pointer + ReassignOffset);

        public void Confirm()
            => Module.ClickAddon(Pointer, ConfirmButton->AtkComponentBase.OwnerNode, EventType.Change, ConfirmButtonId);

        public void Reassign()
            => Module.ClickAddon(Pointer, ReassignButton->AtkComponentBase.OwnerNode, EventType.Change, ReassignButtonId);
    }
}
