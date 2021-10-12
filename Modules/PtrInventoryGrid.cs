using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrInventoryGrid
    {
        private const int StartIndex = 3;
        private const int MaxIndex   = 37;
        private const int DataOffset = 0xC0;

        public AtkUnitBase* Pointer;

        public static implicit operator PtrInventoryGrid(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrInventoryGrid ptr)
            => ptr.Pointer != null && ptr.Pointer->IsVisible;

        public bool FeedChocobo()
        {
            var list = Pointer->UldManager.NodeList;
            for (var i = MaxIndex; i >= StartIndex; --i)
            {
                var item      = list[i];
                var component = (AtkComponentDragDrop*)((AtkComponentNode*)item)->Component;
                var frame     = component->AtkComponentBase.UldManager.NodeList[1];
                var icon      = component->AtkComponentBase.UldManager.NodeList[2];
                if (frame->IsVisible && icon->IsVisible)
                {
                    Module.EventData data = new(item, (byte*)component + DataOffset);
                    Module.ClickAddon(Pointer, item, EventType.Unk37, MaxIndex - i, data.Data);
                    return true;
                }
            }

            return false;
        }
    }
}
