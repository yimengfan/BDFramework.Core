using System;
using System.Collections;
using System.Collections.Generic;
using BDFramework.UFlux.View.Props;
using BDFramework.UI.Demo_ScreenRect;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BDFramework.UFlux
{
    /// <summary>
    /// 这里是UnityEngine的UI Scroll适配器
    /// 这里不适合无限滚动列表设计，只是做演示demo
    /// </summary>
    [ComponentBind(nameof(ScrollRectAdaptor))]
    public class ComponentAdaptor_ScrollRect : AComponentAdaptor
    {
        public override void Init()
        {
            base.Init();
            this.setPropCustomLogicMap[nameof(ScrollRectAdaptor.Contents)] = SetProp_Contents;
        }

        Dictionary<int, ScrollRectAdaptor> srlogicMap = new Dictionary<int, ScrollRectAdaptor>();

        /// <summary>
        /// ScrollView的Content
        /// </summary>
        /// <param name="value">value必须是list[AStateBase] 类型 </param>
        private void SetProp_Contents(Transform trans, object value)
        {
            ScrollRectAdaptor srlogic = null;
            if (!srlogicMap.TryGetValue(trans.GetHashCode(), out srlogic))
            {
                var sr = trans.GetComponent<ScrollRect>();
                srlogic = new ScrollRectAdaptor(sr);
                srlogicMap[trans.GetHashCode()] = srlogic;
            }
            //遍历Conent进行赋值操作
            var sourceList = value as IList;
            //这里做适配演示，
            //一般ScrollRect不会把所有的数据加载出来
            var count = Mathf.Max(srlogic.Contents.Count, sourceList.Count);
            for (int i = 0; i < count; i++)
            {
                if (i >= sourceList.Count) //现有列表大于新增列表
                {
                    //删除节点
                    srlogic.Contents[i].Destroy();
                }
                else if (i >= srlogic.Contents.Count) //新增模式
                {
                    var sourceProp = sourceList[i] as PropsBase;
                    //创建component实例
                    var com = Activator.CreateInstance(sourceProp.ComponentType) as IComponent;
                    if (com != null)
                    {
                        if (com.IsLoad) //同步加载
                        {
                            //添加组件 
                            srlogic.AddItem(com);
                            //更新组件
                            com.SetProps(sourceProp);
                        }
                    }
                    else
                    {
                        BDebug.LogError("【类型出错】该类型不是组件:" + sourceProp.ComponentType);
                    }
                }
                else //更新模式
                {
                    //TODO 这里更新模式一般不这么强制更新，
                    //最好自己做算法索引，动态调整item的位置，
                    //不要每个重新设置
                    var source = sourceList[i] as PropsBase;
                    var com = srlogic.GetItem(i);
                    com.SetProps(source);
                }
            }
            //移除无效component
            srlogic.RemoveUnInvalidComponent();
        }
    }
}