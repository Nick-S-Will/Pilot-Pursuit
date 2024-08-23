using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class IEnumerableExtensions
{
    public static Vector3 Average(this IEnumerable<Vector3> source)
    {
        if (source == null) throw new NullReferenceException();

        var average = Vector3.zero;
        foreach (var vector in source) average += vector;
        average /= source.Count();

        return average;
    }

    #region Select Index
    /// <summary>
    /// Determines the index of the item of <paramref name="source"/> with the smallest <paramref name="selector"/> value.
    /// </summary>
    /// <param name="selector">Function to compare items</param>
    /// <returns>The index with the smallest <paramref name="selector"/> or -1 if all items are null.</returns>
    public static int? MinIndex<T>(this IEnumerable<T> source, Func<T, float> selector)
    {
        return CompareForIndex(source, selector, (a, b) => a < b, float.PositiveInfinity);
    }

    /// <summary>
    /// Determines the index of the item of <paramref name="source"/> with the largest <paramref name="selector"/> value.
    /// </summary>
    /// <param name="selector">Function to compare items</param>
    /// <returns>The index with the largest <paramref name="selector"/> or -1 if all items are null.</returns>
    public static int? MaxIndex<T>(this IEnumerable<T> source, Func<T, float> selector)
    {
        return CompareForIndex(source, selector, (a, b) => a > b, float.NegativeInfinity);
    }

    private static int? CompareForIndex<T>(this IEnumerable<T> source, Func<T, float> selector, Func<float, float, bool> replaceComparer, float initial)
    {
        if (source == null) throw new NullReferenceException();
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (replaceComparer == null) throw new ArgumentNullException(nameof(replaceComparer));

        var current = initial;
        int? compareIndex = null;
        int index = 0;
        foreach (var item in source)
        {
            var selectorValue = selector(item);
            if (item != null && replaceComparer(selectorValue, current))
            {
                current = selectorValue;
                compareIndex = index;
            }

            index++;
        }

        return compareIndex;
    }

    public static int? IndexOf<T>(this IEnumerable<T> source, T item) where T : class
    {
        if (source == null) throw new NullReferenceException();

        int index = 0;
        foreach (var t in source)
        {
            if (item == t) break;

            index++;
        }

        return index < source.Count() ? index : null;
    }
    #endregion
}