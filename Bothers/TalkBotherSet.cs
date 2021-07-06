using Newtonsoft.Json;

namespace Peon.Bothers
{
    public class TalkBotherSet
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


        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public TalkSource Source;

        public TalkBotherSet(string text, MatchType matchType, TalkSource source)
        {
            _string = new CompareString(text, matchType);
            Source  = source;
        }

        public bool Matches(string text)
            => _string.Matches(text);
    }
}
