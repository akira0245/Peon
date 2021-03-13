using System;
using FFXIVClientStructs.Client.UI;

namespace Peon.Modules
{
    public unsafe struct PtrSelectYesno
    {
        private const int YesButtonId = 0;
        private const int NoButtonId  = 1;
        private const int CheckmarkId = 3;

        public AddonSelectYesno* Pointer;

        public static implicit operator PtrSelectYesno(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonSelectYesno>(ptr) };

        public static implicit operator bool(PtrSelectYesno ptr)
            => ptr.Pointer != null;

        public string YesText
            => Module.TextNodeToString(Pointer->YesButton->ButtonTextNode);

        public string NoText
            => Module.TextNodeToString(Pointer->NoButton->ButtonTextNode);

        public void Click(bool yesNo)
        {
            if (yesNo)
                Module.ClickAddon(Pointer, Pointer->YesButton->AtkComponentBase.OwnerNode, EventType.Change, YesButtonId);
            else
                Module.ClickAddon(Pointer, Pointer->NoButton->AtkComponentBase.OwnerNode, EventType.Change, NoButtonId);
        }

        public void ClickYes()
            => Click(true);

        public void ClickNo()
            => Click(false);
    }
}
