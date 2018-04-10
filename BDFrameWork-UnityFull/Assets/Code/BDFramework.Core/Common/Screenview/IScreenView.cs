using System;
using System.Collections.Generic;
using System.Text;

namespace BDFramework.ScreenView
{
    /// <summary>
    /// IScreenView接口
    /// </summary>
    public interface IScreenView
    {
        /// <summary>
        /// IScreenView名称
        /// </summary>
        string Name { get;  }

        /// <summary>
        /// IScreenView是否加载(暂时只看到手动设置)
        /// </summary>
        bool IsLoad { get;  }

        //是否繁忙（在某个Begin操作中）
        bool IsBusy { get;  }

        //是否透明
        bool IsTransparent { get;}

        //ScreenView 有三种状态 未加载，隐藏，显示
        //于是就有四个相关的异步操作
        /// <summary>
        /// IScreenView 初始化
        /// </summary>
        /// <param name="onInit">回调函数</param>
        /// <param name="layer">相关的ScreenViewLayer</param>
        void BeginInit(Action<Exception> onInit, ScreenViewLayer layer); //加载

        /// <summary>
        /// IScreenView 退出
        /// </summary>
        /// <param name="onExit">回调函数</param>
        void BeginExit(Action<Exception> onExit); //卸载

        /// <summary>
        /// 销毁，如果需要立即释放出内存
        /// 正常情况下应该由BeginExit决定是缓存还是释放，但是要有特殊的切换时，要能释放所有的ScreenView
        /// </summary>
        void Destory();

        void Update(float delta); //当他处于Views中的可见位置时，这个会被调用


        void UpdateTask(float delta); //当他处于某个Task中时这个会被调用

        void FixedUpdate(float delta);
    }
}