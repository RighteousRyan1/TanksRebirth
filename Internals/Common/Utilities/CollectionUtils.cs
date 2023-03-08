using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class CollectionUtils
{
    public static ICollection<T> Combine<T>(this IEnumerable<T> enumerable, ICollection<T> mergeTo) {
        foreach (var item in enumerable)
            mergeTo.Add(item);
        return mergeTo;
    }
    public static bool TryGetFirst<TSource>(this IEnumerable<TSource> source, out TSource value) {
        value = default!;
        switch (source) {
            case null:
                throw new ArgumentNullException(nameof(source), "Source IEnumerable can not be null!");
            case IList<TSource> list: {
                if (list.Count > 0) {
                    value = list[0];
                    return true;
                }
                break;
            }
            default: {
                using var enumerator = source.GetEnumerator();

                if (enumerator.MoveNext()) {
                    value = enumerator.Current;
                    return true;
                }

                break;
            }
        }

        return false;
    }
    public static bool TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out TSource found) {
        found = default!;

        if (source == null)
            throw new ArgumentNullException(nameof(source), "Source IEnumerable can not be null!");

        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate), "The predicate used to find the first element in the source IEnumerable can not be null!");

        foreach (var element in source) {
            if (!predicate(element)) continue;
            found = element;
            return true;
        }

        return false;
    }
}
