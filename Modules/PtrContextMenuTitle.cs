using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Peon.Utility;

namespace Peon.Modules
{
    public unsafe struct PtrContextMenuTitle
    {
        public const int          PopupOffset = 0x220;
        public       AtkUnitBase* Pointer;

        public static implicit operator PtrContextMenuTitle(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrContextMenuTitle ptr)
            => ptr.Pointer != null;

        public string Title
            => Module.TextNodeToString((AtkTextNode*) Pointer->UldManager.NodeList[4]);

        public AtkComponentList* List
            => (AtkComponentList*) Pointer->UldManager.NodeList[2];

        public bool Select(int idx)
            => Module.ClickList((byte*)Pointer + PopupOffset, List->AtkComponentBase.OwnerNode, idx);

        public int Count
            => List->ListLength;

        public string ItemText(int idx)
            => Module.TextNodeToString(List->ItemRendererList[idx].AtkComponentListItemRenderer->AtkComponentButton.ButtonTextNode);

        public bool Select(CompareString text)
            => Module.ClickList((byte*)Pointer + PopupOffset, (AtkComponentNode*) List,
                item => text.Matches(Module.TextNodeToString(item->AtkComponentButton.ButtonTextNode)));
    }
}
