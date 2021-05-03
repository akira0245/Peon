using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrBank
    {
        private const int ProceedButtonId  = 1;
        private const int CancelButtonId   = 2;
        private const int PlusButtonId     = 7;
        private const int NumberInputId    = 0;
        private const int WithdrawToggleId = 3;

        public AtkUnitBase* Pointer;

        public static implicit operator PtrBank(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrBank ptr)
            => ptr.Pointer != null;

        private AtkComponentNode* CancelButton
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode;

        private AtkComponentNode* ProceedButton
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode;

        private AtkComponentNode* NumberInput
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode
                ->PrevSiblingNode->PrevSiblingNode->ChildNode->PrevSiblingNode;

        public void Proceed()
            => Module.ClickAddon(Pointer, ProceedButton, EventType.Change, ProceedButtonId);

        public void Cancel()
            => Module.ClickAddon(Pointer, CancelButton, EventType.Change, CancelButtonId);

        public void Minus()
        {
            using Module.EventData data = new(int.MaxValue);
            Module.ClickAddon(Pointer, NumberInput, (EventType) 0x1B, NumberInputId, data.Data);
            Proceed();
        }
    }
}
