using System;
using System.Collections.Generic;

namespace Peon.Utility
{
    public static class LinqExtension
    {
        public static T FirstOr<T>(this IEnumerable<T> collection, Predicate<T> pred, T defaultValue)
        {
            foreach (var x in collection)
            {
                if (pred(x))
                    return x;
            }

            return defaultValue;
        }

        public static U SelectFirstOr<T, U>(this IEnumerable<T> collection, Predicate<T> pred, Func<T,U> select, U defaultValue)
        {
            foreach (var x in collection)
            {
                if (pred(x))
                    return select(x);
            }

            return defaultValue;
        }
    }
}
