using System;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Peon.Modules
{
    public unsafe struct PtrTalk
    {
        public AddonTalk* Pointer;

        public static implicit operator PtrTalk(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonTalk>(ptr) };

        public static implicit operator bool(PtrTalk ptr)
            => ptr.Pointer != null;

        public bool IsVisible
            => Pointer->AtkUnitBase.IsVisible;

        public void Click()
            => Module.ClickAddon(Pointer, Pointer->AtkStage, EventType.Click, 0);

        public string Speaker()
            => Module.TextNodeToString(Pointer->AtkTextNode220);

        public string Text()
            => Module.TextNodeToString(Pointer->AtkTextNode228);
    }
}
