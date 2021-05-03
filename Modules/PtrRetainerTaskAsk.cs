using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrRetainerTaskAsk
    {
        private const int AssignOffset   = 0x2A8;
        private const int ReturnOffset   = 0x2B0;
        private const int ReturnButtonId = 2;
        private const int AssignButtonId = 1;

        public AtkUnitBase* Pointer;

        public static implicit operator PtrRetainerTaskAsk(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrRetainerTaskAsk ptr)
            => ptr.Pointer != null;

        public AtkComponentButton* ConfirmButton
            => (AtkComponentButton*) ((byte*) Pointer + ReturnOffset);

        public AtkComponentButton* AssignButton
            => (AtkComponentButton*) ((byte*) Pointer + AssignOffset);

        public void Return()
            => Module.ClickAddon(Pointer, ConfirmButton->AtkComponentBase.OwnerNode, EventType.Change, ReturnButtonId);

        public void Assign()
            => Module.ClickAddon(Pointer, AssignButton->AtkComponentBase.OwnerNode, EventType.Change, AssignButtonId);
    }
}
