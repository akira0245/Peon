using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Peon.Modules
{
    public unsafe struct PtrHousingSignBoard
    {
        private const int PurchaseButtonId = 2;
        private const int CancelButtonId   = 3;

        public AtkUnitBase* Pointer;

        public static implicit operator PtrHousingSignBoard(IntPtr ptr)
            => new() { Pointer = (AtkUnitBase*) ptr };

        public static implicit operator bool(PtrHousingSignBoard ptr)
            => ptr.Pointer != null;

        public AtkResNode* PurchaseButton
            => Pointer->UldManager.NodeList[5];

        public AtkResNode* CancelButton
            => Pointer->UldManager.NodeList[4];

        public bool IsPurchasable()
            => Pointer->UldManager.NodeList[3]->IsVisible;

        public string PriceString
            => Module.TextNodeToString((AtkTextNode*) Pointer->UldManager.NodeList[18]);

        public long Price
            => long.Parse(PriceString.Replace(",", "").Replace(" Gil", ""));

        public bool IsReady()
        {
            var node   = ((AtkTextNode*) Pointer->UldManager.NodeList[18]);
            return node->NodeText.IsEmpty == 0;
        }

        public void Click(bool yesNo)
        {
            var button = Pointer->UldManager.NodeList[8];

            var       id     = yesNo ? PurchaseButtonId : CancelButtonId;
            using var helper = new Module.ClickHelper(Pointer, button);
            helper.Data[3] = (byte*) id;

            Module.ClickAddonHelper(Pointer, yesNo ? PurchaseButton : CancelButton, EventType.Change, id, helper.Data);
        }
    }
}
