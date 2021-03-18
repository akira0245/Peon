using System;
using FFXIVClientStructs.Component.GUI;
using Peon.Utility;

namespace Peon.Modules
{
    public unsafe struct PtrRetainerList
    {
        private const int          ListOffset = 0xE0;
        public        AtkUnitBase* Pointer;

        public static implicit operator PtrRetainerList(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrRetainerList ptr)
            => ptr.Pointer != null;

        private AtkComponentNode* ListNode
            => *(AtkComponentNode**) ((byte*) Pointer + ListOffset);


        public int Count
            => ((AtkComponentList*) ListNode->Component)->ListLength;

        public RetainerData Info(int idx)
        {
            var list = (AtkComponentList*) ListNode->Component;
            if (idx >= 0 && idx < list->ListLength)
                return new RetainerData(idx, list->ItemRendererList[idx].AtkComponentListItemRenderer);

            throw new ArgumentOutOfRangeException();
        }

        public RetainerData[] Info()
        {
            var list = (AtkComponentList*) ListNode->Component;
            var ret  = new RetainerData[list->ListLength];
            for (var i = 0; i < list->ListLength; ++i)
                ret[i] = new RetainerData(i, list->ItemRendererList[i].AtkComponentListItemRenderer);
            return ret;
        }

        public bool Select(int idx)
            => Module.ClickList(Pointer, ListNode, idx, 1);

        private bool Select(Module.ListCallbackDelegate callback)
        {
            var list      = ListNode;
            var component = (AtkComponentList*) list->Component;

            for (var i = 0; i < component->ListLength; ++i)
            {
                var renderer = component->ItemRendererList[i].AtkComponentListItemRenderer;
                if (!callback(renderer))
                    continue;

                using var data = new Module.EventData(renderer, (ushort) i);
                Module.ClickAddon(Pointer, list, (EventType) 0x23, 1, data.Data);
                return true;
            }

            return false;
        }

        public bool SelectFirstComplete()
            => Module.ClickList(Pointer, ListNode, item => RetainerData.VentureStatus(item) == VentureState.Complete, 1);

        public bool Select(CompareString text)
            => Module.ClickList(Pointer, ListNode, item => text.Matches(Module.TextNodeToString(item->AtkComponentButton.ButtonTextNode)), 1);
    }
}
