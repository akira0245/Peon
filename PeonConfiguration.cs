using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Configuration;
using Newtonsoft.Json;
using Peon.Utility;

namespace Peon
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ChoiceBotherSet
    {
        [FieldOffset(0)]
        public string Text;

        [FieldOffset(8)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public MatchType MatchType;

        [FieldOffset(9)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public BotherSet.Source StringType;

        [FieldOffset(10)]
        public bool YesNo;

        public void Deconstruct(out BotherSet set, out bool yesNo)
        {
            set   = new BotherSet(Text, MatchType, StringType);
            yesNo = YesNo;
        }
    }

    public class PeonConfiguration : IPluginConfiguration
    {
        public int  Version        { get; set; }
        public bool EnableNoBother { get; set; } = true;

        public readonly List<ChoiceBotherSet> BothersYesNo = new();
        public readonly List<BotherSet>       BothersTalk  = new();
    }
}
