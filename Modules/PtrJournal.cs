using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrJournal
    {
        public AddonJournalDetail* Pointer;

        public static implicit operator PtrJournal(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonJournalDetail>(ptr) };

        public static implicit operator bool(PtrJournal ptr)
            => ptr.Pointer != null;

        public string QuestTitle()
        {
            var node = (AtkTextNode*) Pointer->AtkUnitBase.UldManager.NodeList[19];
            return Module.TextNodeToString(node);
        }

        public void Accept()
        {
            Module.ClickAddon(Pointer, Pointer->AcceptButton, EventType.Change, 7);
        }
    }
}
