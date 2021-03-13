using System;
using FFXIVClientStructs.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrHousingChocoboList
    {
        public AtkUnitBase* Pointer;

        public static implicit operator PtrHousingChocoboList(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrHousingChocoboList ptr)
            => ptr.Pointer != null;

        private AtkComponentNode* StableListNode
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode;

        private AtkComponentNode* ChocoboListNode
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode->ChildNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode;

        private bool SelectStable(int idx)
            => Module.ClickList(Pointer, StableListNode, idx, 2);

        private bool SelectChocobo(int idx)
            => Module.ClickList(Pointer, ChocoboListNode, idx, 3);

        public bool Select(int idx)
            => SelectChocobo(idx % 15);
    }
}
