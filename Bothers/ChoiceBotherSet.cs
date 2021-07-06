using Newtonsoft.Json;

namespace Peon.Bothers
{
    public class ChoiceBotherSet
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
        public YesNoSource Source;


        public bool Choice;

        public ChoiceBotherSet(string text, MatchType matchType, YesNoSource source, bool choice)
        {
            _string = new CompareString(text, matchType);
            Source  = source;
            Choice  = choice;
        }

        public bool Matches(string text)
            => _string.Matches(text);
    }
}
