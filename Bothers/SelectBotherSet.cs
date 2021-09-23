using Newtonsoft.Json;

namespace Peon.Bothers
{
    public class SelectBotherSet
    {
        [JsonIgnore]
        private CompareString _mainString;

        [JsonIgnore]
        private CompareString _selectionString;

        public SelectSource Source;

        public int Index;

        public string MainText
        {
            get => _mainString.Text;
            set => _mainString.Text = value;
        }

        public MatchType MainMatchType
        {
            get => _mainString.Type;
            set => _mainString.Type = value;
        }

        public string SelectionText
        {
            get => _selectionString.Text;
            set => _selectionString.Text = value;
        }

        public MatchType SelectionMatchType
        {
            get => _selectionString.Type;
            set => _selectionString.Type = value;
        }

        public SelectBotherSet()
        {
            _mainString      = new CompareString();
            _selectionString = new CompareString();
            Source           = SelectSource.Disabled;
            Index            = 0;
        }

        public SelectBotherSet(string selectionText, MatchType selectionMatchType)
        {
            Source           = SelectSource.SelectionText;
            _mainString      = new CompareString(string.Empty,  MatchType.Contains);
            _selectionString = new CompareString(selectionText, selectionMatchType);
            Index            = 0;
        }

        public SelectBotherSet(string selectionText, MatchType selectionMatchType, int idx)
        {
            Source           = SelectSource.CheckIndexAndText;
            _mainString      = new CompareString(string.Empty,  MatchType.Contains);
            _selectionString = new CompareString(selectionText, selectionMatchType);
            Index            = idx;
        }

        public SelectBotherSet(string selectionText, MatchType selectionMatchType, string mainText, MatchType mainMatchType)
        {
            Source           = SelectSource.CheckMainAndSelectionText;
            _mainString      = new CompareString(mainText,      mainMatchType);
            _selectionString = new CompareString(selectionText, selectionMatchType);
            Index            = 0;
        }

        public SelectBotherSet(int idx, string mainText, MatchType mainMatchType)
        {
            Source           = SelectSource.CheckMainAndSelectionIndex;
            _mainString      = new CompareString(mainText,     mainMatchType);
            _selectionString = new CompareString(string.Empty, MatchType.Contains);
            Index            = idx;
        }

        public SelectBotherSet(string selectionText, MatchType selectionMatchType, int idx, string mainText, MatchType mainMatchType)
        {
            Source           = SelectSource.CheckAll;
            _mainString      = new CompareString(mainText,      mainMatchType);
            _selectionString = new CompareString(selectionText, selectionMatchType);
            Index            = idx;
        }

        public bool SelectionMatches(string text)
            => _selectionString.Matches(text);

        public bool MainMatches(string text)
            => _mainString.Matches(text);
    }

    public class AlternatingBotherSet
    {
        public SelectBotherSet[] Bothers;

        [JsonIgnore]
        public int CurrentSet;

        public AlternatingBotherSet(params SelectBotherSet[] sets)
        {
            Bothers    = sets;
            CurrentSet = 0;
        }
    }
}
