using System.Collections.Generic;
using BDFramework.Mgr;
using UnityEngine;

namespace BDFramework.Hotfix.ScreenView
{
    /// <summary>
    /// 导航管理器标签
    /// </summary>
    public class ScreenViewAttribute : ManagerAttribute
    {
        public ScreenViewAttribute(int intTag) : base(intTag)
        {
        }
    }

    /// <summary>
    /// 导航管理器
    /// </summary>
    public class ScreenViewManager : ManagerBase<ScreenViewManager, ScreenViewAttribute>
    {
        /// <summary>
        /// 所有的导航列表
        /// </summary>
        public List<ScreenViewLayer> screenViewList = new List<ScreenViewLayer>();

        /// <summary>
        /// 主层级
        /// </summary>
        public ScreenViewLayer MainLayer { get; private set; }

        /// <summary>
        /// 默认导航到
        /// </summary>
        private int defaultScreenTag = 0;

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
            foreach (var classData in this.GetAllClassDatas())
            {
                var attr = classData.Attribute as ScreenViewAttribute;

                var sv = CreateInstance<IScreenView>(attr.IntTag);
                //设置name属性
                var t = sv.GetType();
                t.GetProperty("Name").SetValue(sv, attr.IntTag, null);
                MainLayer.RegisterScreen(sv);
                //
                BDebug.Log("创建screen:" + attr.IntTag, "green");
            }
        }

        public override void Start()
        {
            MainLayer.BeginNavTo(this.defaultScreenTag);
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