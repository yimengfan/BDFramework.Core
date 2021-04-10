using System;
using System.Collections;
using System.Collections.Generic;
using BDFramework.UFlux.Collections;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Scroll适配器
    /// 这里不适合无限滚动列表设计，只是做演示demo
    /// </summary>
    [ComponentBindAdaptor(typeof(ScrollRectAdaptor))]
    public class ComponentBindAdaptorScrollRect : AComponentBindAdaptor
    {
        public override void Init()
        {
            base.Init();
            this.setPropCustomLogicMap[nameof(ScrollRectAdaptor.ContentMap)] = SetProp_Contents;
        }

        /// <summary>
        /// 这里管理所有的这类组件，所以需要map
        /// </summary>
        Dictionary<Transform, ScrollRectAdaptor> srlogicMap = new Dictionary<Transform, ScrollRectAdaptor>();

        /// <summary>
        /// ScrollView的Content
        /// </summary>
        /// <param name="value">value必须是ComponentList 类型 </param>
        private void SetProp_Contents(Transform trans, object value)
        {
            ScrollRectAdaptor scrollRectAdaptor = null;
            if (!srlogicMap.TryGetValue(trans, out scrollRectAdaptor))
            {
                var sr = trans.GetComponent<ScrollRect>();
                scrollRectAdaptor = new ScrollRectAdaptor(sr);
                srlogicMap[trans] = scrollRectAdaptor;
            }

            //这里用协变能保证一定转型成功
            var ComtList = value as IComponentList<APropsBase>;
            /**********************增***********************/
            var list = ComtList.GetNewDatas();
            foreach (var props in list)
            {
                //创建component实例
                var com = Activator.CreateInstance(props.ComponentType) as IComponent;
                if (com != null)
                {
                    if (com.IsLoad) //同步加载
                    {
                        //添加组件 
                        scrollRectAdaptor.AddItem(props, com);
                        //更新组件
                        com.SetProps(props);
                    }
                }
                else
                {
                    BDebug.LogError("【类型出错】该类型不是组件:" + props.ComponentType);
                }
            }

            /**********************改***********************/
            list = ComtList.GetChangedDatas();
            foreach (var props in list)
            {
                //刷新节点
                var com = scrollRectAdaptor.GetItem(props);
                com.SetProps(props);
            }

            /**********************删***********************/
            list = ComtList.GetRemovedDatas();
            foreach (var props in list)
            {
                //删除节点
                scrollRectAdaptor.Destroy(props);
            }
        }
    }
}