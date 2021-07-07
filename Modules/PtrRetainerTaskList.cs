using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrRetainerTaskList
    {
        public AtkUnitBase* Pointer;

        public static implicit operator PtrRetainerTaskList(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrRetainerTaskList ptr)
            => ptr.Pointer != null;

        public AtkComponentNode* List
            => (AtkComponentNode*) (Pointer->UldManager.NodeList[2]->IsVisible
                ? Pointer->UldManager.NodeList[2]
                : Pointer->UldManager.NodeList[3]);

        public bool Select(int idx)
            => Module.ClickList(Pointer, List, idx);

        public int Count
            => ((AtkComponentList*) List->Component)->ListLength;
    }
}
