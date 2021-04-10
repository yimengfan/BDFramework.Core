using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using BDFramework.Mgr;
using BDFramework.Reflection;
using BDFramework.ResourceMgr;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using LitJson;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 每个component的值缓存
    /// </summary>
    public class ComponentFieldCahce
    {
        /// <summary>
        /// ui behaviour
        /// </summary>
        public UIBehaviour UIBehaviour { get; set; }

        /// <summary>
        /// Transform
        /// </summary>
        public Transform Transform { get; set; }

        /// <summary>
        /// Attribute
        /// </summary>
        public ComponentValueBindAttribute Attribute { get; set; }

        /// <summary>
        /// 值缓存
        /// </summary>
        public object LastValue { get; set; }
    }

    /// <summary>
    /// Props字段相关缓存
    /// </summary>
    public class TransformBindCache
    {
        /// <summary>
        /// Transform
        /// </summary>
        public Transform Transform { get; set; }

        /// <summary>
        /// 组件属性缓存表
        /// 字段名-》Attribute相关
        /// </summary>
        public Dictionary<string, ComponentFieldCahce> FieldCacheMap = new Dictionary<string, ComponentFieldCahce>();
    }

    /// <summary>
    /// 组件绑定逻辑 管理器
    /// </summary>
    public class ComponentBindAdaptorManager : ManagerBase<ComponentBindAdaptorManager, ComponentBindAdaptorAttribute>
    {
        /// <summary>
        /// 组件绑定包装逻辑map
        /// </summary>
        Dictionary<Type, AComponentBindAdaptor> componentBindAdaptorMap = new Dictionary<Type, AComponentBindAdaptor>();


        public override void Start()
        {
            base.Start();
            var clsList = this.GetAllClassDatas();
            foreach (var cd in clsList)
            {
                var attr = cd.Attribute as ComponentBindAdaptorAttribute;
                var inst = CreateInstance<AComponentBindAdaptor>(cd);
                componentBindAdaptorMap[attr.BindType] = inst;
            }


            //执行30s清理一次的cache
            IEnumeratorTool.StartCoroutine(this.IE_ClearCache(30));
        }


        /// <summary>
        /// 获取绑定组件的类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Type GetBindComponentType(string name)
        {
            var type = this.componentBindAdaptorMap.Keys.FirstOrDefault((key) => key.FullName == name);

            return type;
        }

        /// <summary>
        /// 值缓存map
        /// </summary>
        Dictionary<Transform, TransformBindCache> globalTransformBindCacheMap =
            new Dictionary<Transform, TransformBindCache>();

        /// <summary>
        /// 设置组件绑定值
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="newProps"></param>
        public void SetTransformProps(Transform transform, APropsBase newProps)
        {
            //第一次进行缓存绑定后，就不再重新解析了，
            //所以使用者要保证每次的 state尽量是一致的
            TransformBindCache transformBindCache;
         
            //获取是否有缓存
            if (!globalTransformBindCacheMap.TryGetValue(transform, out transformBindCache))
            {
                transformBindCache = new TransformBindCache();
                transformBindCache.Transform = transform;
                //生成Bind信息
                transformBindCache.FieldCacheMap = BindTransformProps(transform, newProps);
                globalTransformBindCacheMap[transform] = transformBindCache;
            }

            //修改field
            List<string> changedFieldList = AnalysisPropsChanged(transformBindCache, newProps);
            //
            for (int j = 0; j < changedFieldList.Count; j++)
            {
                var fieldName = changedFieldList[j];
                var cf = transformBindCache.FieldCacheMap[fieldName];
                //TODO 这里考虑优化多次获取值的性能
                var newFieldValue = newProps.GetValue(fieldName);
                //执行赋值操作
                var comBindAdaptor = componentBindAdaptorMap[cf.Attribute.Type];
                if (cf.UIBehaviour != null) //UI操作
                {
                    comBindAdaptor.SetData(cf.UIBehaviour, cf.Attribute.FunctionName, newFieldValue);
                }
                else if (cf.Transform) //自定义逻辑管理
                {
                    comBindAdaptor.SetData(cf.Transform, cf.Attribute.FunctionName, newFieldValue);
                }
            }
        }

        
                /// <summary>
        /// bind Tansform和State的值，防止每次都修改
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        private Dictionary<string, ComponentFieldCahce> BindTransformProps(Transform transform, APropsBase props)
        {
            var componentFieldCacheMap = new Dictionary<string, ComponentFieldCahce>();
            //先初始化Props的成员信息
            var type = props.GetType();
            if (props.MemberinfoMap == null)
            {
                var memberInfoMap = StateFactory.GetMemberinfoCache(type);
                if (memberInfoMap == null)
                {
                    memberInfoMap = new Dictionary<string, MemberInfo>();
                    List<MemberInfo> memberInfoList = new List<MemberInfo>();
                    var flag = BindingFlags.Instance | BindingFlags.Public;
                    memberInfoList.AddRange(type.GetFields(flag));
                    memberInfoList.AddRange(type.GetProperties(flag));
                    //缓存所有属性
                    foreach (var mi in memberInfoList)
                    {
                        var attr = mi.GetAttributeInILRuntime<ComponentValueBindAttribute>();
                        if (attr != null)
                        {
                            memberInfoMap[mi.Name] = mi;
                        }
                    }
                    StateFactory.AddMemberinfoCache(type, memberInfoMap);
                }
                props.MemberinfoMap = memberInfoMap;
            }

            //进行值绑定
            foreach (var mi in props.MemberinfoMap.Values)
            {
                var cf = new ComponentFieldCahce();
                var attribute = mi.GetAttributeInILRuntime<ComponentValueBindAttribute>();
                if (attribute.Type == null)
                {
                    Debug.LogErrorFormat("is null: {0} - {1}", attribute.Type, attribute.FunctionName);
                    continue;
                }

                cf.Transform = transform.Find(attribute.TransformPath);
                if (attribute.Type.IsSubclassOf(typeof(UIBehaviour)))
                {
                    cf.UIBehaviour = cf.Transform.GetComponent(attribute.Type) as UIBehaviour;
                }

                cf.Attribute = attribute;
                //缓存
                componentFieldCacheMap[mi.Name] = cf;
            }

            return componentFieldCacheMap;
        }
        
        /// <summary>
        /// 分析Props修改
        /// </summary>
        /// <param name="transformBindCache"></param>
        /// <param name="newProps"></param>
        /// <returns></returns>
        List<string> AnalysisPropsChanged(TransformBindCache transformBindCache, APropsBase newProps)
        {
            List<string> changedFiledList = new List<string>();
            //手动设置field模式
            var fieldNames = newProps.GetChangedPropertise();
            if (fieldNames.Length > 0)
            {
                changedFiledList.AddRange(fieldNames);
            }
            else
            {
                //自动判断模式
                foreach (var item in transformBindCache.FieldCacheMap)
                {
                    var feildName = item.Key;
                    var newFieldValue = newProps.GetValue(feildName);
                    //旧数据为null 直接加入
                    if (item.Value.LastValue == null)
                    {
                        item.Value.LastValue = newFieldValue;
                        changedFiledList.Add(feildName);
                        continue;
                    }

                    var newValueType = newFieldValue.GetType();
                    //开始新的对比判断
                    if (newValueType.IsValueType || newValueType == typeof(string)) //内置类型处理
                    {
                        if (!newProps.Equals(item.Value.LastValue))
                        {
                            changedFiledList.Add(feildName);
                        }
                    }
                    else if (newFieldValue is APropsBase) //成员属性尽量使用手动版本设置改变
                    {
                        var props = newFieldValue as APropsBase;
                        if (props.IsChanged)
                        {
                            changedFiledList.Add(feildName);
                        }
                        else
                        {
                            //globalTransformBindCacheMap.ContainsValue()
                        }
                    }
                    else if (newFieldValue is IComponentList<APropsBase>)
                    {
                        var comList = newFieldValue as IComponentList<APropsBase>;
                        if (comList.IsChanged)
                        {
                            changedFiledList.Add(feildName);
                        }
                    }
                    else
                    {
                        BDebug.LogError("不支持的Props类型:" + newValueType.FullName);
                    }

                    item.Value.LastValue = newFieldValue;
                }
            }

            return changedFiledList;
        }





        /// <summary>
        /// 清理cache
        /// </summary>
        /// <returns></returns>
        private IEnumerator IE_ClearCache(float time)
        {
            while (true)
            {
                var keys = globalTransformBindCacheMap.Keys.ToList();
                for (int j = 0; j < keys.Count; j++)
                {
                    var key = keys[j];
                    if (!key)
                    {
                        globalTransformBindCacheMap.Remove(key);
                    }
                }

                //每n s 清理一次
                yield return new WaitForSeconds(time);
            }
        }
    }
}