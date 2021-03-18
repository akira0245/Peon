using System;
using FFXIVClientStructs.Client.UI;
using FFXIVClientStructs.Component.GUI;
using Peon.Utility;

namespace Peon.Modules
{
    // Also usable by SystemMenu
    public unsafe struct PtrSelectString
    {
        public AddonSelectString* Pointer;

        public static implicit operator PtrSelectString(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonSelectString>(ptr) };

        public static implicit operator bool(PtrSelectString ptr)
            => ptr.Pointer != null;

        public bool Select(int idx)
            => Module.ClickList(&Pointer->vtbl238, Pointer->AtkComponentList->AtkComponentBase.OwnerNode, idx);

        public string Description()
            => Module.TextNodeToString((AtkTextNode*) Pointer->AtkUnitBase.RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode);

        public int Count
            => Pointer->AtkComponentList->ListLength;

        public string ItemText(int idx)
            => Module.TextNodeToString(Pointer->AtkComponentList->ItemRendererList[idx]
               .AtkComponentListItemRenderer->AtkComponentButton.ButtonTextNode);

        public bool Select(CompareString text)
            => Module.ClickList(&Pointer->vtbl238, Pointer->AtkComponentList->AtkComponentBase.OwnerNode,
                item => text.Matches(Module.TextNodeToString(item->AtkComponentButton.ButtonTextNode)));
    }
}
