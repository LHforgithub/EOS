using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES
{
    internal static class Utility
    {
        public static bool TryAdd<T>(this List<T> list, T item)
        {
            if (list.Contains(item))
            {
                return false;
            }
            list.Add(item);
            return true;
        }
        public static void InsertOrUpdateAt<T>(this List<T> list, int index, T item)
        {
            if (list is null)
                throw new NullReferenceException(nameof(list));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index < list.Count)
            {
                list[index] = item;
            }
            else
            {
                while (list.Count < index)
                {
                    list.Add(default);
                }
                list.Add(item);
            }
        }
        public static void AddOrUpdata<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            dic[key] = value;
        }
    }
}
