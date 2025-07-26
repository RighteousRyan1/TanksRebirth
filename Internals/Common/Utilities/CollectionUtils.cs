using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class CollectionUtils
{
    public static Vector2 Closest(this IEnumerable<Vector2> positions, Vector2 source) {
        if (positions == null || !positions.Any())
            return source;

        float closestDistanceSquared = float.MaxValue;
        Vector2 closest = Vector2.Zero;

        foreach (var pos in positions) {
            float distanceSquared = Vector2.DistanceSquared(source, pos);
            if (distanceSquared < closestDistanceSquared) {
                closestDistanceSquared = distanceSquared;
                closest = pos;
            }
        }

        return closest;
    }
    public static int IndexClosest(this IList<Vector2> positions, Vector2 source) {
        if (positions == null || !positions.Any())
            return -1;

        float closestDistanceSquared = float.MaxValue;
        Vector2 closest = Vector2.Zero;

        foreach (var pos in positions) {
            float distanceSquared = Vector2.DistanceSquared(source, pos);
            if (distanceSquared < closestDistanceSquared) {
                closestDistanceSquared = distanceSquared;
                closest = pos;
            }
        }

        return positions.IndexOf(closest);
    }
    public static ICollection<T> Combine<T>(this ICollection<T> collection1, ICollection<T> mergeTo)
    {
        foreach (var item in collection1)
            mergeTo.Add(item);

        return mergeTo;
    }

    public static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, out bool found)
    {
        if (source == null)
        {
            throw new ArgumentNullException(source.ToString());
        }

        if (source is IList<TSource> list)
        {
            if (list.Count > 0)
            {
                found = true;
                return list[0];
            }
        }
        else
        {
            using IEnumerator<TSource> e = source.GetEnumerator();

            if (e.MoveNext())
            {
                found = true;
                return e.Current;
            }
        }

        found = false;
        return default!;
    }
    public static bool TryGetFirst<TSource>(this IEnumerable<TSource> source, out TSource value)
    {
        value = default!;
        if (source == null)
        {
            throw new ArgumentNullException(source.ToString());
        }

        if (source is IList<TSource> list)
        {
            if (list.Count > 0)
            {
                value = list[0];
                return true;
            }
        }
        else
        {
            using IEnumerator<TSource> e = source.GetEnumerator();

            if (e.MoveNext())
            {
                value = e.Current;
                return true;
            }
        }

        return false;
    }

    public static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
    {
        if (source == null)
        {
            throw new ArgumentNullException(source.ToString());
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(predicate.ToString());
        }

        foreach (TSource element in source)
        {
            if (predicate(element))
            {
                found = true;
                return element;
            }
        }

        found = false;
        return default!;
    }
    public static bool TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out TSource found)
    {
        found = default!;

        if (source == null)
        {
            throw new ArgumentNullException(source.ToString());
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(predicate.ToString());
        }

        foreach (TSource element in source)
        {
            if (predicate(element))
            {
                found = element;
                return true;
            }
        }

        return false;
    }
}
