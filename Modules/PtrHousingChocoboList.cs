using System;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Peon.Utility;

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
            => (AtkComponentNode*) Pointer->UldManager.NodeList[20];

        private AtkComponentNode* ChocoboListNode
            => (AtkComponentNode*) Pointer->UldManager.NodeList[6];

        private bool SelectStable(int idx)
            => Module.ClickList(Pointer, StableListNode, idx, 2);

        private bool SelectChocobo(int idx)
            => Module.ClickList(Pointer, ChocoboListNode, idx, 3);

        public int ChocoboCount
            => ((AtkComponentList*) ChocoboListNode->Component)->ListLength;

        public int StableCount
            => ((AtkComponentList*) StableListNode->Component)->ListLength;

        private static bool IsMaxLevelColor(ByteColor color)
            => color.R == 0xF0 && color.G == 0x8E && color.B == 0x37 && color.A == 0xFF;

        private static bool TrainableChocobo(AtkComponentListItemRenderer* renderer)
        {
            var uld = ((AtkComponentBase*) renderer)->UldManager;
            if (!StringId.ChocoboIsReady.Equal(Module.TextNodeToString((AtkTextNode*) uld.NodeList[7])))
                return false;

            return !IsMaxLevelColor(((AtkTextNode*) uld.NodeList[11]->ChildNode->PrevSiblingNode)->TextColor);
        }

        public bool SelectNextTrainableChocobo()
            => Module.ClickList(Pointer, ChocoboListNode, TrainableChocobo, 3);

        public bool Select(int idx)
            => SelectChocobo(idx % 15);
    }
}
