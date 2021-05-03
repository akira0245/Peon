using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrTitleMenu
    {
        private const int          ValueOffset = 0x230;
        public        AtkUnitBase* Pointer;

        public static implicit operator PtrTitleMenu(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrTitleMenu ptr)
            => ptr.Pointer != null;

        public void Start()
        {
            var value     = *(int*) ((byte*) Pointer + ValueOffset);
            var buttons   = Pointer->RootNode->ChildNode->PrevSiblingNode->ChildNode;
            var node      = buttons->ChildNode;
            while (node != null)
            {
                if (node->Y == 0)
                    Module.ClickAddon(Pointer, node, EventType.Change, value);
                node = node->PrevSiblingNode;
            }
        }
    }
}
