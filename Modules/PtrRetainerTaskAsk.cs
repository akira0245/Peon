using System;
using FFXIVClientStructs.Component.GUI;

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

        public AtkComponentButton* ReassignButton
            => (AtkComponentButton*) ((byte*) Pointer + AssignOffset);

        public void Return()
            => Module.ClickAddon(Pointer, ConfirmButton->AtkComponentBase.OwnerNode, EventType.Change, ReturnButtonId);

        public void Assign()
            => Module.ClickAddon(Pointer, ReassignButton->AtkComponentBase.OwnerNode, EventType.Change, AssignButtonId);
    }
}
