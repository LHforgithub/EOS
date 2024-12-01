using System;
using System.Collections.Generic;
using System.Linq;


namespace EOS.Tools
{
    /// <summary>
    /// 对部分方法的拓展使用
    /// </summary>
    public static class Extentions
    {
        /// <summary>
        /// 转换enum对象至int
        /// </summary>
        public static int Int32(this Enum value)
        {
            return Convert.ToInt32(value);
        }
        /// <summary>
        /// 合并字典。当存在重复键时，默认优先使用新的字典的值。
        /// </summary>
        /// <param name="originalDic">原字典</param>
        /// <param name="mergeDic">要合并的字典</param>
        /// <param name="originalFirst">优先保留原字典的值。不推荐，效率低下。</param>
        /// <returns>返回修改后的字典。</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="KeyNotFoundException"/>
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> originalDic, IDictionary<TKey, TValue> mergeDic, bool originalFirst = false)
        {
            if (originalFirst)
            {
                foreach (var keyValuePair in mergeDic)
                {
                    if (originalDic.ContainsKey(keyValuePair.Key))
                    {
                        continue;
                    }
                    originalDic[keyValuePair.Key] = keyValuePair.Value;
                }
            }
            else
            {
                foreach (var keyValuePair in mergeDic)
                {
                    originalDic[keyValuePair.Key] = keyValuePair.Value;
                }
            }
            return originalDic;
        }
        /// <inheritdoc cref="Merge"/>
        /// <param name="mergeDics">合并的字典集合</param>
        public static IDictionary<TKey, TValue> MergeRange<TKey, TValue>(this IDictionary<TKey, TValue> originalDic, IEnumerable<IDictionary<TKey, TValue>> mergeDics, bool originalFirst = true)
        {
            var newlist = new List<IDictionary<TKey, TValue>>(mergeDics);
            foreach (var dic in newlist)
            {
                originalDic = originalDic.Merge(dic, originalFirst);
            }
            return originalDic;
        }
        /// <summary>将<see cref="IDictionary{TKey, TValue}"/>转为<see cref="Dictionary{TKey, TValue}"/>。</summary>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDictionary<TKey, TValue> keyValuePairs)
        {
            return keyValuePairs.ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// 向字典添加值如果其不存在，或者更新字典中的键值。
        /// </summary>
        /// <param name="originalDic">原字典</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="KeyNotFoundException"/>
        public static void AddOrUpdata<TKey, TValue>(this IDictionary<TKey, TValue> originalDic, TKey key, TValue value)
        {
            originalDic[key] = value;
        }

        /// <summary>
        /// 尝试向列表添加元素，如果已有相同元素则不添加。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">原列表</param>
        /// <param name="value">尝试添加的元素</param>
        public static bool TryAddWithOutMultiple<T>(this List<T> list, T value)
        {
            if (list.Contains(value))
            {
                return false;
            }
            list.Add(value);
            return true;
        }
    }
}
