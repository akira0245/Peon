using System;
using FFXIVClientStructs.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrInventoryGrid
    {
        private const int MaxIndex   = 34;
        private const int DataOffset = 0xC0;

        public AtkUnitBase* Pointer;

        public static implicit operator PtrInventoryGrid(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrInventoryGrid ptr)
            => ptr.Pointer != null && ptr.Pointer->IsVisible;

        public bool FeedChocobo()
        {
            var root = Pointer->RootNode;
            var grid = root->ChildNode->PrevSiblingNode;
            var item = grid->ChildNode;
            var idx  = MaxIndex;
            while (item != null)
            {
                var component = (AtkComponentDragDrop*) ((AtkComponentNode*) item)->Component;
                var frame     = component->AtkComponentBase.ULDData.NodeList[1];
                var icon      = component->AtkComponentBase.ULDData.NodeList[2];
                if (frame->IsVisible && icon->IsVisible)
                {
                    using Module.EventData data = new(item, (byte*) component + DataOffset);
                    Module.ClickAddon(Pointer, item, EventType.Unk37, idx, data.Data);
                    return true;
                }

                --idx;
                item = item->PrevSiblingNode;
            }

            return false;
        }
    }
}
