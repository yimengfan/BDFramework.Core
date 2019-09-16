using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BDFramework.Mgr;
//using UnityEditor.Graphs;
using UnityEngine;

namespace BDFramework.ScreenView
{
    public class ScreenViewManager : ManagerBase<ScreenViewManager, ScreenViewAttribute>
    {
        public List<ScreenViewLayer> screenViewList = new List<ScreenViewLayer>();

        /// <summary>
        /// 主层级
        /// </summary>
        public ScreenViewLayer MainLayer { get; private set; }

        private string defaultScreenName = null;

        #region Mgr管理

        /// <summary>
        ///初始化
        /// </summary>
        public override void Init()
        {
            if (MainLayer != null)
            {
                Debug.LogError("已经执行过Awake");
                return;
            }

            base.Init();
            //

            MainLayer = this.AddLayer();
            //
            foreach (var classData in this.ClassDataMap.Values)
            {
                var attr = classData.Attribute as ScreenViewAttribute;

                var sv = CreateInstance<IScreenView>(attr.Tag);
                //设置name属性
                var t = sv.GetType();
                t.GetProperty("Name").SetValue(sv, attr.Tag, null);
                MainLayer.RegScreen(sv);
                //
                BDebug.Log("创建screen:" + attr.Tag, "green");
                //
                if (attr.IsDefault && string.IsNullOrEmpty(defaultScreenName) == true)
                {
                    defaultScreenName = attr.Tag;
                }
            }
        }

        public override void Start()
        {
            if (string.IsNullOrEmpty(this.defaultScreenName) == false)
            {
                MainLayer.BeginNavTo(this.defaultScreenName);
            }
            else
            {
                BDebug.Log("没有默认导航的ScreenView");
            }
        }
        

        #endregion


        #region Screen管理

        public ScreenViewLayer AddLayer()
        {
            ScreenViewLayer layer = null;
            layer = new ScreenViewLayer(this.screenViewList.Count);
            this.screenViewList.Add(layer);
            return layer;
        }

        #endregion
    }
}