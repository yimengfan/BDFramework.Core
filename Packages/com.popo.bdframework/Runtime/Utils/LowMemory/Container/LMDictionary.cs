using System;
using System.Collections.Generic;

namespace BDFramework.LowMemory.Container
{
    /// <summary>
    /// 低内存的字典容器，key 只能是int/string类型
    /// 这个不是线程安全的!!!!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LMDictionary<K,V> : IDisposable
    {
        public Dictionary<K, int> dict;
        public List<V> objectList;

        public LMDictionary(int capacity = 10)
        {
            // 运行时限制 K 只能为 int 或 string
            if (typeof(K) != typeof(int) && typeof(K) != typeof(string))
            {
                throw new InvalidOperationException("LMDictionary<K,V> only supports K = int or string");
            }

            this.dict = new Dictionary<K, int>(capacity);
            this.objectList = new List<V>(capacity);
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Add(K key, V value)
        {
            if (this.dict.ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists.", nameof(key));

            this.objectList.Add(value);
            var idx = this.objectList.Count - 1; // 使用 0-based 索引
            this.dict.Add(key, idx);
        }

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        public void Remove(K key)
        {
            if (!this.dict.TryGetValue(key, out var idx))
            {
                // key 不存在，直接返回
                return;
            }
            
             //元素小于100个，直接用移除. 大于100.

            // 移除元素并删除字典中的 key
            this.objectList.RemoveAt(idx);
            this.dict.Remove(key);

            // 更新字典中所有索引大于 idx 的值，减1
            var keysToUpdate = new List<K>();
            foreach (var kv in this.dict)
            {
                if (kv.Value > idx)
                    keysToUpdate.Add(kv.Key);
            }
            foreach (var k in keysToUpdate)
            {
                this.dict[k] = this.dict[k] - 1;
            }
        }


        public void Dispose()
        {
            this.dict.Clear();
            this.objectList.Clear();
            this.dict = null;
            this.objectList = null;
        }
    }
}