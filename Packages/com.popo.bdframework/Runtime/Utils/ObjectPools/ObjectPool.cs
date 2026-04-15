using System;
using System.Collections.Generic;
using UnityEngine;

namespace BDFramework.Utils
{
    /// <summary>
    /// 通用对象池基类，提供泛型对象的获取、释放和预热能力。
    /// 与 GameObject 特化池（<see cref="BDFramework.ResourceMgr.GameObjectPoolManager"/>）不同，
    /// 本类不依赖 Unity 资源体系，可被任意业务层复用。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T>
    {
        private List<ObjectPoolContainer<T>> list;
        private Dictionary<T, ObjectPoolContainer<T>> lookup;
        private Func<T> factoryFunc;
        private Action<T> destroyFunc;
        private int lastIndex = 0;

        public ObjectPool(Func<T> factoryFunc, int initialSize)
        {
            this.factoryFunc = factoryFunc;
            list = new List<ObjectPoolContainer<T>>(initialSize);
            lookup = new Dictionary<T, ObjectPoolContainer<T>>(initialSize);

            Warm(initialSize);
        }

        public ObjectPool(Func<T> factoryFunc, Action<T> destroyFunc, int initialSize)
        {
            this.factoryFunc = factoryFunc;
            this.destroyFunc = destroyFunc;
            list = new List<ObjectPoolContainer<T>>(initialSize);
            lookup = new Dictionary<T, ObjectPoolContainer<T>>(initialSize);

            Warm(initialSize);
        }

        private void Warm(int capacity)
        {
            for (int i = 0; i < capacity; i++)
            {
                CreateContainer();
            }
        }

        private ObjectPoolContainer<T> CreateContainer()
        {
            var container = new ObjectPoolContainer<T>();
            container.Item = factoryFunc();
            list.Add(container);
            return container;
        }

        /// <summary>
        /// 获取一个item
        /// </summary>
        /// <returns></returns>
        public T GetItem()
        {
            ObjectPoolContainer<T> container = null;
            for (int i = 0; i < list.Count; i++)
            {
                lastIndex++;
                if (lastIndex > list.Count - 1) lastIndex = 0;

                if (list[lastIndex].Used)
                {
                    continue;
                }
                else
                {
                    container = list[lastIndex];
                    break;
                }
            }

            //没有合适的则创建一个新的
            if (container == null)
            {
                container = CreateContainer();
            }
            container.Consume();
            lookup.Add(container.Item, container);
            return container.Item;
        }

        /// <summary>
        /// 释放一个item
        /// </summary>
        /// <param name="item"></param>
        public void ReleaseItem(object item)
        {
            ReleaseItem((T) item);
        }

        /// <summary>
        /// 释放一个item
        /// </summary>
        /// <param name="item"></param>
        public void ReleaseItem(T item)
        {
            if (lookup.ContainsKey(item))
            {
                var container = lookup[item];
                container.Release();
                lookup.Remove(item);
            }
            else
            {
                Debug.LogWarning("This object pool does not contain the item provided: " + item);
            }
        }

        public int Count
        {
            get { return list.Count; }
        }

        public int CountUsedItems
        {
            get { return lookup.Count; }
        }

        /// <summary>
        /// 销毁Pool
        /// </summary>
        public void Destroy()
        {
            foreach (var container in this.list)
            {
                this.destroyFunc(container.Item);
            }
            this.list.Clear();
        }
    }
}
