using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrJournalResult
    {
        public AddonJournalResult* Pointer;

        public static implicit operator PtrJournalResult(IntPtr ptr)
            => new() { Pointer = Module.Cast<AddonJournalResult>(ptr) };

        public static implicit operator bool(PtrJournalResult ptr)
            => ptr.Pointer != null;

        public string QuestName()
            => Module.TextNodeToString((AtkTextNode*) Pointer->AtkUnitBase.UldManager.NodeList[11]);

        public void Complete()
        {
            Module.ClickAddon(Pointer, Pointer->CompleteButton, EventType.Change, 1);
        }
    }
}
