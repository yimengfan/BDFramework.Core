using System;
using System.Collections.Generic;
using UnityEngine;

namespace BDFramework.ResourceMgr
{
    public class GameObjectPoolManager : Singleton<GameObjectPoolManager>
    {
        public bool logStatus;

        /// <summary>
        /// prefab-pool的映射
        /// </summary>
        private Dictionary<GameObject, ObjectPool<GameObject>> prefabLookup;
        /// <summary>
        /// 实例化gameobj-pool的映射
        /// </summary>
        private Dictionary<GameObject, ObjectPool<GameObject>> instanceLookup;

        private bool dirty = false;

        void Awake()
        {
            prefabLookup = new Dictionary<GameObject, ObjectPool<GameObject>>();
            instanceLookup = new Dictionary<GameObject, ObjectPool<GameObject>>();
        }

        void Update()
        {
            if (logStatus && dirty)
            {
                PrintStatus();
                dirty = false;
            }
        }

        private void warmPool(GameObject prefab, int size)
        {
            if (prefabLookup.ContainsKey(prefab))
            {
                throw new Exception("Pool for prefab " + prefab.name + " has already been created");
            }

            var pool = new ObjectPool<GameObject>(//
                () =>//构造
            {
                return InstantiatePrefab(prefab);
            }, 
            (t) =>//删除
            {
                Destroy(t);
            }, size);
            prefabLookup[prefab] = pool;

            dirty = true;
        }

        private GameObject spawnObject(GameObject prefab)
        {
            return spawnObject(prefab, Vector3.zero, Quaternion.identity);
        }

        private GameObject spawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefabLookup.ContainsKey(prefab))
            {
                WarmPool(prefab, 1);
            }

            var pool = prefabLookup[prefab];

            var clone = pool.GetItem();
            clone.transform.SetPositionAndRotation(position, rotation);
            clone.SetActive(true);

            instanceLookup.Add(clone, pool);
            dirty = true;
            return clone;
        }

        private void releaseObject(GameObject clone)
        {
            clone.SetActive(false);

            if (instanceLookup.ContainsKey(clone))
            {
                instanceLookup[clone].ReleaseItem(clone);
                instanceLookup.Remove(clone);
                dirty = true;
            }
            else
            {
                Debug.LogWarning("No pool contains the object: " + clone.name);
            }
        }


        private GameObject InstantiatePrefab(GameObject prefab)
        {
            var go = Instantiate(prefab);
            go.transform.SetParent(this.transform);
            return go;
        }

        public void PrintStatus()
        {
            foreach (KeyValuePair<GameObject, ObjectPool<GameObject>> keyVal in prefabLookup)
            {
                Debug.Log(string.Format("Object Pool for Prefab: {0} In Use: {1} Total {2}", keyVal.Key.name, keyVal.Value.CountUsedItems, keyVal.Value.Count));
            }
        }

        #region Static API

        /// <summary>
        /// 初始化pool
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="size"></param>
        public static void WarmPool(GameObject prefab, int size)
        {
            Instance.warmPool(prefab, size);
        }
        
        /// <summary>
        /// 销毁一个pool
        /// </summary>
        public static void DestoryPool(GameObject prefab)
        {
            Instance.prefabLookup.TryGetValue(prefab, out var pool);
            if (pool != null)
            {
                pool.Destroy();
            }
            //删除key
            Instance.prefabLookup.Remove(prefab);
        }
        /// <summary>
        /// 获取一个object
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static GameObject SpawnObject(GameObject prefab)
        {
            return Instance.spawnObject(prefab);
        }
        /// <summary>
        /// 获取一个object
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Instance.spawnObject(prefab, position, rotation);
        }
        
        /// <summary>
        /// 归还一个object
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static void ReleaseObject(GameObject clone)
        {
            Instance.releaseObject(clone);
        }

        #endregion
    }
}
