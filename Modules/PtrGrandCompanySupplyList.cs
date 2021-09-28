using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrGrandCompanySupplyList
    {
        public AtkUnitBase* Pointer;

        public static implicit operator PtrGrandCompanySupplyList(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrGrandCompanySupplyList ptr)
            => ptr.Pointer != null;

        private AtkComponentNode* ListNode
            => (AtkComponentNode*) Pointer->UldManager.NodeList[5];

        public int Count
            => ((AtkComponentList*) ListNode->Component)->ListLength;

        public bool Select(int idx)
            => Module.ClickList(Pointer, ListNode, idx, 1);
    }
}
