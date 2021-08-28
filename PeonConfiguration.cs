using System.Collections.Generic;
using Dalamud.Configuration;
using Peon.Bothers;
using Peon.Crafting;
using Peon.Utility;

namespace Peon
{
    public class PeonConfiguration : IPluginConfiguration
    {
        public          int                   Version            { get; set; }
        public          bool                  EnableNoBother     { get; set; } = true;
        public          bool                  EnableLoginButtons { get; set; } = true;
        public readonly List<ChoiceBotherSet> BothersYesNo   = new();
        public readonly List<TalkBotherSet>   BothersTalk    = new();
        public readonly List<SelectBotherSet> BothersSelect  = new();
        public readonly List<string>          CharacterNames = new();

        public readonly Dictionary<string, Macro> CraftingMacros = new();

        public void Save()
            => Dalamud.PluginInterface.SavePluginConfig(this);

        public static PeonConfiguration Load()
        {
            if (Dalamud.PluginInterface.GetPluginConfig() is PeonConfiguration cfg)
                return cfg;

            cfg = new PeonConfiguration();
            cfg.Save();
            return cfg;
        }
    }
}
