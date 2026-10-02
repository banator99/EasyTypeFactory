using System;
using System.Collections.Generic;

namespace EasyTypeFactory.Utils;

internal static class HashCodeExtensions
{
    public static void AddCollection<T>(this ref HashCode hashCode, IReadOnlyCollection<T>? items)
    {
        if (items is null)
        {
            return;
        }

        foreach (var item in items)
        {
            hashCode.Add(item);
        }
    }

    public static void AddDictionary<TKey, TValue>(this ref HashCode hashCode, IReadOnlyDictionary<TKey, TValue>? items)
        where TKey : notnull
    {
        if (items is null)
        {
            return;
        }

        foreach (var (key, value) in items)
        {
            hashCode.Add(key);
            hashCode.Add(value);
        }
    }
}
