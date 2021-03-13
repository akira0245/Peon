using System;
using FFXIVClientStructs.Component.GUI;
using ImGuiScene;

namespace Peon.Modules
{
    public unsafe struct PtrCharaSelectListMenu
    {
        public AtkUnitBase* Pointer;

        public static implicit operator PtrCharaSelectListMenu(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrCharaSelectListMenu ptr)
            => ptr.Pointer != null;

        private int Value
            => *(int*) ((byte*) Pointer + 0x238);

        private AtkComponentNode* ListNode
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode;

        private AtkComponentList* List
            => (AtkComponentList*) ListNode->Component;

        public string[] CharacterNames()
        {
            var      list = List;
            string[] ret  = new string[list->ListLength];
            for (var i = 0; i < list->ListLength; ++i)
                ret[i] = Module.TextNodeToString(list->ItemRendererList[i].AtkComponentListItemRenderer->AtkComponentButton.ButtonTextNode);

            return ret;
        }

        public int CharacterIndex(string name)
        {
            var      list = List;
            string[] ret  = new string[list->ListLength];
            for (var i = 0; i < list->ListLength; ++i)
                if (name == Module.TextNodeToString(list->ItemRendererList[i].AtkComponentListItemRenderer->AtkComponentButton.ButtonTextNode))
                    return i;
            return -1;
        }

        public bool Select(int idx)
            => Module.ClickList(Pointer, ListNode, idx, Value);

        public bool Select(string name)
            => Module.ClickList(Pointer, ListNode, a => Module.TextNodeToString(a->AtkComponentButton.ButtonTextNode) == name);
    }
}
