using System.Collections.Generic;
using Dalamud.Configuration;
using Peon.Bothers;
using Peon.Utility;

namespace Peon
{
    public class PeonConfiguration : IPluginConfiguration
    {
        public int  Version        { get; set; }
        public bool EnableNoBother { get; set; } = true;

        public readonly List<ChoiceBotherSet> BothersYesNo  = new();
        public readonly List<TalkBotherSet>   BothersTalk   = new();
        public readonly List<SelectBotherSet> BothersSelect = new();
    }
}
