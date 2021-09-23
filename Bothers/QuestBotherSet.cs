using Newtonsoft.Json;

namespace Peon.Bothers
{
    public class QuestBotherSet
    {
        [JsonIgnore]
        private CompareString _string;

        public string Text
        {
            get => _string.Text;
            set => _string.Text = value;
        }


        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public MatchType MatchType
        {
            get => _string.Type;
            set => _string.Type = value;
        }


        public bool Enabled;

        public QuestBotherSet(string text, MatchType matchType, bool enabled)
        {
            _string = new CompareString(text, matchType);
            Enabled = enabled;
        }

        public bool Matches(string text)
            => _string.Matches(text);
    }
}
