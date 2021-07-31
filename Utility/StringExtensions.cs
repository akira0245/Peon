using System.Linq;

namespace Peon.Utility
{
    public static class StringExtensions
    {
        public static string RemoveNonSimple(this string s)
        {
            return string.Concat(s.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c)));
        }
    }
}
