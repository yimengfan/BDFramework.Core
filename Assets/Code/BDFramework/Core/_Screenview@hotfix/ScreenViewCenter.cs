using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace BDFramework.ScreenView
{
    /// <summary>
    /// 导航层，一个导航层管理若干ScreenView,ScreenView之间可以多个同时出现，是一种层叠的关系
    /// </summary>
    public class ScreenViewLayer
    {
        /// <summary>
        /// 导航层构造函数
        /// </summary>
        /// <param name="layerid">导航层id</param>
        public ScreenViewLayer(int layerid)
        {
            this.layerid = layerid;
        }

        /// <summary>
        /// 导航层id
        /// </summary>
        public int layerid
        {
            get;
            private set;
        }

        /// <summary>
        /// 所有的view
        /// </summary>
        Dictionary<string, IScreenView> allViews = new Dictionary<string, IScreenView>();

        /// <summary>
        /// 显示中的IScreenView列表
        /// </summary>
        List<IScreenView> navViews = new List<IScreenView>();//显示栈，当前在显示栈中的界面
        /// <summary>
        /// 通过名称获取IScreenView
        /// </summary>
        /// <param name="svName">IScreenView名称</param>
        /// <returns></returns>
        public IScreenView GetScreenView(string svName)
        {
            IScreenView sv = null;
            //遍历显示中的IScreenView列表,找到直接跳出
            foreach (var _sv in navViews)
            {
                if (_sv.Name == svName)
                {
                    sv = _sv;
                    break;
                }
            }
            //如果没有找到
            if (sv == null)
            {
                //遍历未显示的IScreenView列表,找到直接跳出
                foreach (var _sv in allViews)
                {
                    if (_sv.Key == svName)
                    {
                        sv = _sv.Value;
                        break;
                    }
                }
            }
            return sv;
        }


        /// <summary>
        /// 增加一个Screen，Screen 虽然立即创建，
        /// Screen应设计为不执行BeginLoad不加载任何内容，完成后由回调通知
        /// <para>注册IScreenView,默认添加到未使用的IScreenView列表</para>
        /// </summary>
        /// <param name="creator"></param>
        public void RegScreen(IScreenView view)
        {
            allViews.Add(view.Name, view);
        }


        private IScreenView currentView = null;
        /// <summary>
        /// 灵魂功能，导航到一个指定名称的ScreenView，可能是向前，也可能是向后
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="onLoad"></param>
        public void BeginNavTo(string name)
        {
            //
            if (currentView != null && currentView.Name == name)
            {
                BDebug.LogError("别闹，当前就是" + name);
                return;
            }
            //
            IScreenView view = null;
            if (this.allViews.TryGetValue(name, out view))
            {
                view.BeginInit();
                if (currentView != null)
                {
                   currentView.BeginExit();
                }
                //
                currentView = view;
                currentViewIndex = -1;
                //
                this.navViews.Add(view);
                if (navViews.Count > 10)
                {
                    navViews.RemoveAt(0);
                }
            }
        }


        private int currentViewIndex = -1;
        /// <summary>
        /// ScreenViewLayer 向前导航
        /// </summary>
        /// <param name="name">IScreenView名称</param>
        /// <param name="onLoad">异常回调</param>
        public void BeginNavForward(string name)
        {
            if (currentViewIndex == -1 || currentViewIndex+1 >= navViews.Count)
            {
                BDebug.LogError("别闹，前方没有view");
                return;
            }

            currentViewIndex++;
            
            IScreenView view = navViews[currentViewIndex];
            view.BeginInit();
            if (currentView != null)
            {
                currentView.BeginExit();
            }
            //
            currentView = view;

        }

        /// <summary>
        /// ScreenViewLayer 向后导航
        /// </summary>
        /// <param name="onLoad">异常回调</param>
        public void BeginNavBack()
        {
            
            if (currentViewIndex == 0 || currentViewIndex-1 >= navViews.Count)
            {
                BDebug.LogError("别闹，前方没有view");
                return;
            }

            currentViewIndex--;
            
            IScreenView view = navViews[currentViewIndex];
            
            view.BeginInit();
            if (currentView != null)
            {
                currentView.BeginExit();
            }
            //
            currentView = view;         
        }
        
    }

}


