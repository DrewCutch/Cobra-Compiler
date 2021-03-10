using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompiler.Util
{
    static class IEnumerableExtensions
    {
        public static T OnlyOrDefault<T>(this IEnumerable<T> enumerable)
        {
            IEnumerator<T> enumerator = enumerable.GetEnumerator();
            try
            {
                if (!enumerator.MoveNext())
                    return default;
                
                T only = enumerator.Current;

                if (enumerator.MoveNext())
                    return default;

                return only;
            }
            finally
            {
                enumerator.Dispose();
            }
        }

        public static bool Empty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }

        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> enumerable)
        {
            int i = 0;
            foreach (T value in enumerable)
            {
                yield return (value, i);
                i += 1;
            }
        }
    }
}
