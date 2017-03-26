using System;
using System.Collections.Generic;

using System.Text;

namespace BDFramework.ScreenView
{
    public interface IScreenView
    {
        string name
        {
            get;
        }
        bool isLoad//是否加载
        {
            get;
        }
        //bool isHide//是否隐藏
        //{
        //    get;
        //}
        bool isBusy//是否繁忙（在某个Begin操作中）
        {
            get;
        }
        bool isTransparent//是否透明
        {
            get;
        }
        //ScreenView 有三种状态 未加载，隐藏，显示
        //于是就有四个相关的异步操作
        void BeginInit(Action<Exception> onInit, ScreenViewLayer layer);//加载

        void BeginExit(Action<Exception> onExit);//卸载

        /// <summary>
        /// 销毁，如果需要立即释放出内存
        /// 正常情况下应该由BeginExit决定是缓存还是释放，但是要有特殊的切换时，要能释放所有的ScreenView
        /// </summary>
        void Destory();

        void Update(float delta);//当他处于Views中的可见位置时，这个会被调用


        void UpdateTask(float delta);//当他处于某个Task中时这个会被调用
    }
}
