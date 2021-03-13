using System;
using FFXIVClientStructs.Client.UI;
using FFXIVClientStructs.Component.GUI;

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
            => (AtkComponentNode*) Pointer->RootNode->PrevSiblingNode;

        private AtkComponentNode* ProceedButton
        {
            get
            {
                var root = Pointer->RootNode;
                var x    = root->ChildNode;
                var y    = x->PrevSiblingNode;
                var z    = y->PrevSiblingNode;
                return (AtkComponentNode*) z;
            }
        }

        private AtkComponentNode* NumberInput
        {
            get
            {
                var root = Pointer->RootNode;
                var x    = root->ChildNode;
                var y    = x->PrevSiblingNode;
                var z    = y->PrevSiblingNode;
                var a    = z->PrevSiblingNode;
                var b    = a->PrevSiblingNode;
                var c    = b->ChildNode;
                var d    = c->PrevSiblingNode;
                return (AtkComponentNode*) d;
            }
        }

        public void Proceed()
            => Module.ClickAddon(Pointer, ProceedButton, EventType.Change, ProceedButtonId);

        public void Cancel()
            => Module.ClickAddon(Pointer, CancelButton, EventType.Change, CancelButtonId);

        public void Minus()
        {
            using Module.EventData data = new(Int32.MaxValue);
            Module.ClickAddon(Pointer, NumberInput, (EventType) 0x1B, NumberInputId, data.Data);
            Proceed();
        }
    }
}
