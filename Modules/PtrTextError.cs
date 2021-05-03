using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrTextError
    {
        public AtkUnitBase* Pointer;
        public static implicit operator PtrTextError(IntPtr ptr)
            => new() { Pointer = Module.Cast<AtkUnitBase>(ptr) };

        public static implicit operator bool(PtrTextError ptr)
            => ptr.Pointer != null;

        public string Text()
            => Module.TextNodeToString((AtkTextNode*) Pointer->RootNode->ChildNode);
    }
}
