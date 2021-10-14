using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrRetainerTaskAsk
    {
        private const int ReturnButtonId = 2;
        private const int AssignButtonId = 1;

        public AtkUnitBase* Pointer;

        public static implicit operator PtrRetainerTaskAsk(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrRetainerTaskAsk ptr)
            => ptr.Pointer != null;

        public AtkComponentNode* ReturnButton
            => (AtkComponentNode*) Pointer->UldManager.NodeList[3];

        public AtkComponentNode* AssignButton
            => (AtkComponentNode*) Pointer->UldManager.NodeList[4];

        public bool IsReady
            => AssignButton != null && (AssignButton->AtkResNode.Flags & (1 << 5)) != 0;

        public void Return()
            => Module.ClickAddon(Pointer, ReturnButton, EventType.Change, ReturnButtonId);

        public void Assign()
            => Module.ClickAddon(Pointer, AssignButton, EventType.Change, AssignButtonId);
    }
}
