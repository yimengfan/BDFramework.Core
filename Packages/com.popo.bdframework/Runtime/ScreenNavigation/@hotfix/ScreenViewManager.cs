using System.Collections.Generic;
using BDFramework.Mgr;
using UnityEngine;

namespace BDFramework.ScreenView
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
    /// 导航作为逻辑层主调度，一般最后执行
    /// </summary>
    [ManagerOrder(Order = 99999)]
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
            //TODO 
            //一般情况下 打开unity.或者reloadAssembly 会重新初始化框架
            //但是ExitPlaymode后不会触发ReloadAssembly,所以有些静态对象会缓存
            if (MainLayer != null)
            {
                // Debug.LogError("已经执行过Awake -" + this.GetHashCode());
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
                sv.Name = attr.IntTag;
                MainLayer.RegisterScreen(sv);
                //
                //BDebug.Log("创建screen:" + attr.IntTag, "green");
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
