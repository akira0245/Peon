using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrGrandCompanySupplyReward
    {
        public AtkUnitBase* Pointer;

        public static implicit operator PtrGrandCompanySupplyReward(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrGrandCompanySupplyReward ptr)
            => ptr.Pointer != null;

        private AtkComponentNode* CancelButton
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode->ChildNode;

        private AtkComponentNode* DeliverButton
            => (AtkComponentNode*) ((AtkResNode*) CancelButton)->PrevSiblingNode;

        public void Deliver()
            => Module.ClickAddon(Pointer, DeliverButton, EventType.Change, 0);

        public void Cancel()
            => Module.ClickAddon(Pointer, CancelButton, EventType.Change, 0);
    }
}
