using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDFramework.ResourceMgr
{
    /// <summary>
    /// 每次成功下载资源都会调用一次
    /// </summary>
    /// <param name="processCallback"></param>
    public delegate void OnVersionContrlDownload(float curProcess, string returnInfo);
    public delegate void OnVersionContrlState(string returnInfo);
    public interface IVersionControl
    {
        event OnVersionContrlDownload OnDownLoading;
        event OnVersionContrlState OnError;
        event OnVersionContrlState OnSuccess;

        int AllFileCount { get; }
        /// <summary> 
        /// 开始版本更新
        /// </summary>
        /// <param name="gameid">游戏id</param>
        /// <param name="localversion">当前版本</param>
        /// <param name="processCallback">回调</param>
        void Start();
        /// <summary>
        /// 任务中断,继续执行
        /// </summary>
        void Continue();
    }

}