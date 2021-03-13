using System;
using FFXIVClientStructs.Client.UI;

namespace Peon.Modules
{
    public unsafe struct PtrRecipeNote
    {
        private const int SynthesizeButtonId = 13;

        public AddonRecipeNote* Pointer;

        public static implicit operator PtrRecipeNote(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonRecipeNote>(ptr) };

        public static implicit operator bool(PtrRecipeNote ptr)
            => ptr.Pointer != null;

        public void Synthesize()
            => Module.ClickAddon(Pointer, Pointer->SynthesizeButton->AtkComponentBase.OwnerNode, EventType.Change, SynthesizeButtonId);
    }
}
