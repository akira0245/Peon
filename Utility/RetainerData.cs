using FFXIVClientStructs.FFXIV.Component.GUI;
using Peon.Modules;

namespace Peon.Utility
{
    public enum VentureState
    {
        Unknown,
        Available,
        InProgress,
        Complete,
    }

    public enum RetainerCity : byte
    {
        Unknown,
        LimsaLominsa,
        Gridania,
        Uldah,
        Ishgard,
        Kugane,
        Crystarium,
    }

    public readonly struct RetainerData
    {
        public string       Name      { get; }
        public long         Gil       { get; }
        public byte         Inventory { get; }
        public byte         Selling   { get; }
        public byte         Level     { get; }
        public VentureState Venture   { get; }
        public RetainerCity Location  { get; }
        public byte         Job       { get; }
        public byte         Index     { get; }

        public static unsafe VentureState VentureStatus(AtkComponentListItemRenderer* list)
            => ToVenture((AtkTextNode*) FirstInfoNode(list));

        private static unsafe AtkResNode* FirstInfoNode(AtkComponentListItemRenderer* item)
        {
            var rootNode = item->AtkComponentButton.AtkComponentBase.UldManager.RootNode;
            return rootNode->PrevSiblingNode->PrevSiblingNode->PrevSiblingNode->ChildNode;
        }

        private static unsafe VentureState ToVenture(AtkTextNode* node)
        {
            return Module.TextNodeToString(node) switch
            {
                "Complete"         => VentureState.Complete,
                "None in progress" => VentureState.Available,
                _                  => VentureState.InProgress,
            };
        }

        private static unsafe byte ToSelling(AtkTextNode* node)
        {
            var text = Module.TextNodeToString(node);
            if (text[0] == 'N')
                return 0;

            text = text.Substring(8, 2).TrimEnd();
            return byte.Parse(text);
        }

        private static unsafe RetainerCity ToLocation(AtkImageNode* node)
        {
            return Module.ImageNodeToTexture(node) switch
            {
                "ui/icon/060000/060881.tex" => RetainerCity.LimsaLominsa,
                "ui/icon/060000/060882.tex" => RetainerCity.Gridania,
                "ui/icon/060000/060883.tex" => RetainerCity.Uldah,
                "ui/icon/060000/060884.tex" => RetainerCity.Ishgard,
                "ui/icon/060000/060885.tex" => RetainerCity.Kugane,
                "ui/icon/060000/060886.tex" => RetainerCity.Crystarium,
                _                           => RetainerCity.Unknown,
            };
        }

        private static unsafe long ToGil(AtkTextNode* node)
            => long.Parse(Module.TextNodeToString(node).Replace(",", ""));

        private static unsafe byte ToDigit(AtkTextNode* node)
            => byte.Parse(Module.TextNodeToString(node));

        private static unsafe byte ToJob(AtkImageNode* node)
            => byte.Parse(Module.ImageNodeToTexture(node).Substring(19, 2));

        public unsafe RetainerData(int idx, AtkComponentListItemRenderer* item)
        {
            Index = (byte) idx;

            var node = FirstInfoNode(item);
            Venture = ToVenture((AtkTextNode*) node);

            node    = node->PrevSiblingNode;
            Selling = ToSelling((AtkTextNode*) node);

            node     = node->PrevSiblingNode;
            Location = ToLocation((AtkImageNode*) node);

            node = node->PrevSiblingNode;
            Gil  = ToGil((AtkTextNode*) node);

            node      = node->PrevSiblingNode->PrevSiblingNode;
            Inventory = ToDigit((AtkTextNode*) node);

            node  = node->PrevSiblingNode->PrevSiblingNode;
            Level = ToDigit((AtkTextNode*) node);

            node = node->PrevSiblingNode;
            Job  = ToJob((AtkImageNode*) node);

            node = node->PrevSiblingNode;
            Name = Module.TextNodeToString((AtkTextNode*) node);
        }
    }
}
