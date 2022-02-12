using System;
using System.Collections.Generic;
using BDFramework.UFlux;
using UnityEngine;

namespace BDFramework.Mgr
{
    public class ManagerAttribute : Attribute
    {
        /// <summary>
        /// int类型Tag
        /// </summary>
        public int IntTag { get; private set; } = -1;
        /// <summary>
        /// String类型tag
        /// </summary>
        public string Tag { get; private set; } = null;

        public ManagerAttribute(int intTag)
        {
            this.IntTag = intTag;
        }

        public ManagerAttribute(string tag)
        {
            this.Tag = tag;
        }
    }

    /// <summary>
    /// 所有管理器的基类
    /// </summary>
    /// <typeparam name="T">是管理器实例</typeparam>
    /// <typeparam name="V">标签属性</typeparam>
   abstract public class ManagerBase<T, V> : IMgr where T : IMgr, new() where V : ManagerAttribute
    {
        static private T i;

        static public T Inst
        {
            get
            {
                if (i == null)
                {
                    i = new T();
                }

                return i;
            }
        }

        private Dictionary<int, ClassData> ClassDataMap_IntKey { get; set; }
        private Dictionary<string, ClassData> ClassDataMap_StringKey { get; set; }

        protected ManagerBase()
        {
            this.ClassDataMap_IntKey = new Dictionary<int, ClassData>();
            this.ClassDataMap_StringKey = new Dictionary<string, ClassData>();
        }
        
        /// <summary>
        /// 检测类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attribute"></param>
        virtual public void CheckType(Type type, ManagerAttribute attribute)
        {
            //var vAttr = attribute as V;
            
            if (attribute is V vAttr)
            {
                
                if (vAttr.IntTag != -1)
                {
                    SaveAttribute(vAttr.IntTag, new ClassData() {Attribute = vAttr, Type = type});
                }
                else if (vAttr.Tag != null)
                {
                    SaveAttribute(vAttr.Tag, new ClassData() {Attribute = vAttr, Type = type});
                }
            }
        }


        virtual public void Init()
        {
        }

        virtual public void Start()
        {
        }

        /// <summary>
        /// 通过tag 获取class信息
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public ClassData GetClassData(int tag)
        {
            ClassData classData = null;
            this.ClassDataMap_IntKey.TryGetValue(tag, out classData);
            return classData;
        }

        /// <summary>
        /// 通过tag 获取class信息
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public ClassData GetClassData(string tag)
        {
            ClassData classData = null;
            this.ClassDataMap_StringKey.TryGetValue(tag, out classData);
            return classData;
        }


        /// <summary>
        /// 通过类型获取class信息
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ClassData GetClassData<TN>()
        {
            return GetClassData(typeof(TN));
        }

        /// <summary>
        /// 通过类型获取class信息
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ClassData GetClassData(Type type)
        {
            var classDatas = GetAllClassDatas();
            foreach (var value in classDatas)
            {
                if (value.Type == type)
                {
                    return value;
                }
            }

            return null;
        }


        /// <summary>
        /// 获取所有的ClassData
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClassData> GetAllClassDatas()
        {
            IEnumerable<ClassData> classDatas = new List<ClassData>();
            if (this.ClassDataMap_IntKey.Count > 0)
            {
                classDatas = this.ClassDataMap_IntKey.Values;
            }
            else if (this.ClassDataMap_StringKey.Count > 0)
            {
                classDatas = this.ClassDataMap_StringKey.Values;
            }

            return classDatas;
        }

        /// <summary>
        /// 保存属性
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        public void SaveAttribute(int tag, ClassData data)
        {
            this.ClassDataMap_IntKey[tag] = data;
        }

        /// <summary>
        /// 保存属性
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        public void SaveAttribute(string tag, ClassData data)
        {
            this.ClassDataMap_StringKey[tag] = data;
        }


        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="args"></param>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public T2 CreateInstance<T2>(ClassData cd, params object[] args) where T2 : class
        {
            if (cd.Type != null)
            {
                if (args.Length == 0)
                {
                    return Activator.CreateInstance(cd.Type) as T2;
                }
                else
                {
                    return Activator.CreateInstance(cd.Type, args) as T2;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="args"></param>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public T2 CreateInstance<T2>(int tag, params object[] args) where T2 : class
        {
            var cd = GetClassData(tag);
            if (cd == null)
            {
                BDebug.LogError("没有找到:" + tag + " -" + typeof(T2).Name);
                return null;
            }

            return CreateInstance<T2>(cd, args);
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="args"></param>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public T2 CreateInstance<T2>(string tag, params object[] args) where T2 : class
        {
            var cd = GetClassData(tag);
            if (cd == null)
            {
                BDebug.LogError("没有找到:" + tag + " -" + typeof(T2).Name);
                return null;
            }

            return CreateInstance<T2>(cd, args);
        }
    }
}