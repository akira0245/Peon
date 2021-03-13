using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Peon.Utility
{
    public enum MatchType : byte
    {
        Equal,
        Contains,
        StartsWith,
        EndsWith,
        RegexFull,
        RegexPartial,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CompareString
    {
        [FieldOffset(0)]
        private string _text;

        [FieldOffset(0)]
        private Regex? _regex;

        [FieldOffset(8)]
        public MatchType _type;


        public CompareString(string text, MatchType type)
        {
            _type  = type;
            _regex = null;
            _text  = text;
            if (_type == MatchType.RegexFull || _type == MatchType.RegexPartial)
                _regex = new Regex(text, RegexOptions.Compiled);
        }

        public string Text
        {
            get => _type == MatchType.RegexFull || _type == MatchType.RegexPartial
                ? _regex!.ToString()
                : _text;
            set
            {
                if (_type == MatchType.RegexFull || _type == MatchType.RegexPartial)
                    _regex = new Regex(value, RegexOptions.Compiled);
                else
                    _text = value;
            }
        }

        public Regex? Regex => _type == MatchType.RegexFull || _type == MatchType.RegexPartial
                ? _regex
                : null;

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public MatchType Type
        {
            get => _type;
            set
            {
                if (value == MatchType.RegexFull || value == MatchType.RegexPartial)
                {
                    if (_type != MatchType.RegexFull && _type != MatchType.RegexPartial)
                        _regex = new Regex(_text, RegexOptions.Compiled);
                }
                else if (_type == MatchType.RegexFull || _type == MatchType.RegexPartial)
                {
                    _text = _regex!.ToString();
                }

                _type = value;
            }
        }

        private bool FullRegexMatch(string text)
        {
            var match = _regex!.Match(text);
            return match.Success && match.Value.Length == text.Length;
        }

        public bool Matches(string text)
        {
            return _type switch
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
