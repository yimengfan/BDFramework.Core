using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using BDFramework.Mgr;
using BDFramework.Reflection;
using BDFramework.UFlux.View.Props;
using LitJson;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 每个component的值缓存
    /// </summary>
    public class ComponentValueCahce
    {
        public UIBehaviour UIBehaviour { get; set; }
        public Transform Transform { get; set; }
        public ComponentValueBindAttribute ValueBindAttribute { get; set; }
        public object LastValue { get; set; }
    }

    /// <summary>
    /// 组件绑定逻辑 管理器
    /// </summary>
    public class ComponentBindAdaptorManager : ManagerBase<ComponentBindAdaptorManager, ComponentBindAdaptorAttribute>
    {
        Dictionary<Type, AComponentBindAdaptor> adaptorMap = new Dictionary<Type, AComponentBindAdaptor>();


        public override void Start()
        {
            base.Start();
            var clsList = this.GetAllClassDatas();
            foreach (var cd in clsList)
            {
                var attr = cd.Attribute as ComponentBindAdaptorAttribute;
                var inst = CreateInstance<AComponentBindAdaptor>(cd);
                adaptorMap[attr.BindType] = inst;
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
            var type = this.adaptorMap.Keys.FirstOrDefault((key) => key.FullName == name);

            return type;
        }

        /// <summary>
        /// 值缓存map
        /// </summary>
        Dictionary<int, Dictionary<string, ComponentValueCahce>> componentValueCacheMap =
            new Dictionary<int, Dictionary<string, ComponentValueCahce>>();

        /// <summary>
        /// 设置组件绑定值
        /// </summary>
        /// <param name="t"></param>
        /// <param name="aState"></param>
        public void SetComponentBindValue(Transform t, AStateBase aState)
        {
            //第一次进行缓存绑定后，就不再重新解析了，
            //所以使用者要保证每次的 state尽量是一致的
            Dictionary<string, ComponentValueCahce> map = null;
            var key = t.GetInstanceID();
            if (!componentValueCacheMap.TryGetValue(key, out map))
            {
                map = TransformStateBind(t, aState);
                componentValueCacheMap[key] = map;
            }

            while (true)
            {
                var field = aState.GetChangedProperty();
                if (field == null)
                {
                    break;
                }

                //开始
                ComponentValueCahce cvc = null;
                if (map.TryGetValue(field, out cvc))
                {
                    var newValue = aState.GetValue(field);
                    // if (cvc.LastValue == null || !cvc.LastValue.Equals(newValue))
                    // {
                        cvc.LastValue = newValue;
                        //执行赋值操作
                        if (cvc.UIBehaviour != null) //UI操作
                        {
                            var componentAdaptor = adaptorMap[cvc.ValueBindAttribute.UIComponentType];
                            componentAdaptor.SetData(cvc.UIBehaviour, cvc.ValueBindAttribute.FieldName, newValue);
                        }
                        else if (cvc.Transform) //自定义逻辑管理
                        {
                            var componentAdaptor = adaptorMap[cvc.ValueBindAttribute.UIComponentType];
                            componentAdaptor.SetData(cvc.Transform, cvc.ValueBindAttribute.FieldName, newValue);
                        }
                    //}
                }
                else
                {
                    BDebug.LogError("State不存在字段:" + field);
                }
            }
        }


        /// <summary>
        /// bind Tansform和State的值，防止每次都修改
        /// </summary>
        /// <param name="t"></param>
        /// <param name="aState"></param>
        /// <returns></returns>
        private Dictionary<string, ComponentValueCahce> TransformStateBind(Transform t, AStateBase aState)
        {
            Dictionary<string, ComponentValueCahce> retMap = new Dictionary<string, ComponentValueCahce>();
            //所有的成员信息
            var memberInfos = StateFactory.GetCache(aState.GetType());
            foreach (var mi in memberInfos.Values)
            {
                //先寻找节点 
                Transform transform = null;
                {
                    var attr = mi.GetAttributeInILRuntime<TransformPathAttribute>();
                    if (attr != null)
                    {
                        transform = t.Find(attr.Path);
                        if (!transform)
                        {
                            BDebug.LogError("节点不存在:" + attr.Path + "  -" + aState.GetType().Name);
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                //再进行值绑定
                {
                    var cvc = new ComponentValueCahce();
                    var cvb = mi.GetAttributeInILRuntime<ComponentValueBindAttribute>();
                    //
                    if (cvb != null)
                    {
                        if (cvb.UIComponentType == null)
                        {
                            Debug.LogErrorFormat("is null: {0} - {1}", cvb.UIComponentType, cvb.FieldName);
                        }

                        var ret = cvb.UIComponentType.IsSubclassOf(typeof(UIBehaviour));
                        if (ret)
                        {
                            cvc.UIBehaviour = transform.GetComponent(cvb.UIComponentType) as UIBehaviour;
                        }
                        else
                        {
                            cvc.Transform = transform;
                        }

                        cvc.ValueBindAttribute = cvb;
                    }
                    else //如果只有Transform 没有ComponentValueBind标签，处理默认逻辑
                    {
                        Type type = null;

                        if (mi is FieldInfo)
                        {
                            type = ((FieldInfo) mi).FieldType;
                        }
                        else if (mi is PropertyInfo)
                        {
                            type = ((PropertyInfo) mi).PropertyType;
                        }


                        if (type.IsSubclassOf(typeof(PropsBase)))
                        {
                            //填充 子节点赋值逻辑
                            cvc.ValueBindAttribute = new ComponentValueBindAttribute(typeof(UFluxAutoLogic),
                                nameof(UFluxAutoLogic.SetChildValue));
                        }
                        else
                        {
                            cvc.ValueBindAttribute = new ComponentValueBindAttribute(typeof(UFluxAutoLogic),
                                nameof(UFluxAutoLogic.ForeachSetChildValue));

#if UNITY_EDITOR
                            //props 数组
                            if (type.IsArray && !type.GetElementType().IsSubclassOf(typeof(PropsBase))) //数组
                            {
                                Debug.LogError("自动适配节点逻辑失败，类型元素不是Props!!!");
                            }
                            //泛型
                            else if (type.IsGenericType &&
                                     !type.GetGenericArguments()[0].IsSubclassOf(typeof(PropsBase))) //泛型
                            {
                                Debug.LogError("自动适配节点逻辑失败，类型元素不是Props!!!");
                            }
#endif

                            //list t或者array
                            if (type.IsArray || type.IsGenericType) //数组
                            {
                                cvc.ValueBindAttribute = new ComponentValueBindAttribute(typeof(UFluxAutoLogic),
                                    nameof(UFluxAutoLogic.ForeachSetChildValue));
                            }
                        }

                        cvc.Transform = transform;
                    }

                    //缓存
                    retMap[mi.Name] = cvc;
                }
            }

            return retMap;
        }


        /// <summary>
        /// 清理cache
        /// </summary>
        /// <returns></returns>
        private IEnumerator IE_ClearCache(float time)
        {
            while (true)
            {
                var keys = componentValueCacheMap.Keys.ToList();
                for (int j = 0; j < keys.Count; j++)
                {
                    var key = keys[j];
                    var cvcMap = componentValueCacheMap[key];
                    //遍历cache
                    var cvcMapkeys = cvcMap.Keys.ToList();
                    foreach (var ckey in cvcMapkeys)
                    {
                        var cvc = cvcMap[ckey];
                        if (!cvc.UIBehaviour && !cvc.Transform)
                        {
                            cvcMap.Remove(ckey);
                        }
                    }

                    if (cvcMap.Count == 0)
                    {
                        componentValueCacheMap.Remove(key);
                    }
                }

                //每n s 清理一次
                yield return new WaitForSeconds(time);
            }
        }
    }
}