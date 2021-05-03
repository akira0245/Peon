using System;
using Dalamud.Plugin;
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
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode;

        private AtkComponentNode* ChocoboListNode
            => (AtkComponentNode*) Pointer->RootNode->ChildNode->PrevSiblingNode->ChildNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode;

        private bool SelectStable(int idx)
            => Module.ClickList(Pointer, StableListNode, idx, 2);

        private bool SelectChocobo(int idx)
            => Module.ClickList(Pointer, ChocoboListNode, idx, 3);


        private bool IsMaxLevelColor(ByteColor color)
            => color.R == 0xFF && color.G == 0xC5 && color.B == 0xE1 && color.A == 0xAA;

        private bool TrainableChocobo(AtkComponentListItemRenderer* renderer)
        {
            
            var rootNode   = renderer->AtkComponentButton.AtkComponentBase.UldManager.RootNode;
            var res1       = rootNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode->ChildNode->PrevSiblingNode;
            var statusTextNode = (AtkTextNode*) res1->ChildNode->PrevSiblingNode;
            var s = Module.TextNodeToString(statusTextNode);
            if (s != "Ready")
                return false;

            var levelTextNode = (AtkTextNode*) res1->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode->ChildNode->PrevSiblingNode;
            return !IsMaxLevelColor(levelTextNode->TextColor);
        }

        public bool SelectNextTrainableChocobo()
        {
            var list      = ChocoboListNode;
            var component = (AtkComponentList*) list->Component;

            // List is sometimes not up at the same time as module,
            // and buttons return errors even if they are setup correctly, thus the long wait.
            TaskExtension.WaitUntil(() => component->ItemRendererList != null, 5000, 1000);
            if (component->ItemRendererList == null)
                return false;

            for (var i = 0; i < component->ListLength; ++i)
            {
                var renderer = component->ItemRendererList[i].AtkComponentListItemRenderer;
                if (TrainableChocobo(renderer)) 
                    return Module.ClickList(Pointer, ChocoboListNode, i, 3);
            }

            return false;
        }

        public bool Select(int idx)
            => SelectChocobo(idx % 15);
    }
}
