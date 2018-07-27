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
        string Name { get; }

        /// <summary>
        /// IScreenView是否加载(暂时只看到手动设置)
        /// </summary>
        bool IsLoad { get;  }

        //于是就有四个相关的异步操作
        /// <summary>
        /// IScreenView 初始化
        /// </summary>
        void BeginInit(); //加载

        /// <summary>
        /// IScreenView 退出
        /// </summary>
        void BeginExit(); //卸载



        void Update(float delta); //当他处于Views中的可见位置时，这个会被调用


        void FixedUpdate(float delta);
    }
}