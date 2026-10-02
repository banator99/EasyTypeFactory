using System;
using System.Collections.Generic;

namespace EasyTypeFactory.Utils;

internal static class HashCodeExtensions
{
    extension(ref HashCode hashCode)
    {
        public void AddCollection<T>(IReadOnlyCollection<T>? items)
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

        public void AddDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue>? items)
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
}
