using System;
using System.ComponentModel;
using System.Linq;

namespace Peon.Bothers
{
    public enum TalkSource : byte
    {
        Disabled,
        Text,
        Speaker,
    };

    public enum YesNoSource : byte
    {
        Disabled,
        Text,
        YesButton,
        NoButton,
    }

    public enum SelectSource : byte
    {
        Disabled,
        SelectionText,
        CheckMainAndSelectionText,
        CheckMainAndSelectionIndex,
        CheckIndexAndText,
        CheckAll,
    }

    public static class SourceExtensions
    {
        public static readonly string[] TalkStrings = Enum.GetValues(typeof(TalkSource)).Cast<TalkSource>().Select(ToString).ToArray();

        public static readonly string[] YesNoStrings = Enum.GetValues(typeof(YesNoSource)).Cast<YesNoSource>().Select(ToString).ToArray();

        public static readonly string[] SelectStrings = Enum.GetValues(typeof(SelectSource)).Cast<SelectSource>().Select(ToString).ToArray();

        public static string ToString(this TalkSource source)
        {
            return source switch
            {
                TalkSource.Disabled => "Disabled",
                TalkSource.Text     => "Text",
                TalkSource.Speaker  => "Speaker",
                _                   => throw new InvalidEnumArgumentException(),
            };
        }

        public static string ToString(this YesNoSource source)
        {
            return source switch
            {
                YesNoSource.Disabled  => "Disabled",
                YesNoSource.Text      => "Text",
                YesNoSource.YesButton => "Yes Button",
                YesNoSource.NoButton  => "No Button",
                _                     => throw new InvalidEnumArgumentException(),
            };
        }

        public static string ToString(this SelectSource source)
        {
            return source switch
            {
                SelectSource.Disabled                   => "Disabled",
                SelectSource.SelectionText              => "Text",
                SelectSource.CheckMainAndSelectionText  => "Main & Text",
                SelectSource.CheckMainAndSelectionIndex => "Main & Index",
                SelectSource.CheckIndexAndText          => "Text & Index",
                SelectSource.CheckAll                   => "All",
                _                                       => throw new InvalidEnumArgumentException(),
            };
        }
    }
}
