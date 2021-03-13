using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Peon.Utility
{
    [StructLayout(LayoutKind.Explicit)]
    public struct BotherSet
    {
        public enum Source : byte
        {
            Main,
            Speaker,
            YesButton,
            NoButton,
        }

        public string Text
        {
            get => _matchType == MatchType.RegexFull || _matchType == MatchType.RegexPartial
                ? _regex!.ToString()
                : _text;
            set
            {
                if (_matchType == MatchType.RegexFull || _matchType == MatchType.RegexPartial)
                    _regex = new Regex(value, RegexOptions.Compiled);
                else
                    _text = value;
            }
        }

        [FieldOffset(0)]
        [JsonIgnore]
        private string _text;

        [FieldOffset(0)]
        [JsonIgnore]
        private Regex? _regex;

        [FieldOffset(8)]
        private MatchType _matchType;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public MatchType MatchType
        {
            get => _matchType;
            set
            {
                if (value == MatchType.RegexFull || value == MatchType.RegexPartial)
                {
                    if (_matchType != MatchType.RegexFull && _matchType != MatchType.RegexPartial)
                        _regex = new Regex(_text, RegexOptions.Compiled);
                }
                else if (_matchType == MatchType.RegexFull || _matchType == MatchType.RegexPartial)
                {
                    _text = _regex!.ToString();
                }

                _matchType = value;
            }
        }

        [FieldOffset(9)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public Source StringType;

        public BotherSet(string text, MatchType matchType, Source stringType)
        {
            if (matchType == MatchType.RegexPartial || matchType == MatchType.RegexFull)
            {
                _text  = text;
                _regex = new Regex(text, RegexOptions.Compiled);
            }
            else
            {
                _regex = null;
                _text  = text;
            }

            _matchType = matchType;
            StringType = stringType;
        }

        private bool FullRegexMatch(string text)
        {
            var match = _regex!.Match(text);
            return match.Success && match.Value.Length == text.Length;
        }

        public bool Matches(string text)
        {
            return _matchType switch
            {
                MatchType.Equal        => text.Equals(_text),
                MatchType.Contains     => text.Contains(_text),
                MatchType.StartsWith   => text.StartsWith(_text),
                MatchType.EndsWith     => text.EndsWith(_text),
                MatchType.RegexFull    => FullRegexMatch(text),
                MatchType.RegexPartial => _regex!.IsMatch(text),
                _                      => throw new InvalidEnumArgumentException(),
            };
        }
    }
}
