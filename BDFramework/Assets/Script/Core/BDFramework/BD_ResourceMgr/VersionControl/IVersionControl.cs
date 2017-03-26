using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public interface IVersionControl
{
    /// <summary>
    /// 是否是测试
    /// </summary>
    bool isTest 
    {
        get; 
        set; 
    }
    /// <summary>
    /// 服务器地址
    /// </summary>
    string mResServerAddress
    {
        get;
        set;
    }
    /// <summary>
    /// 本地存储地址
    /// </summary>
    string mLocalHotUpdateResPath
    {
        get;
        set;
    }
    /// <summary>
    /// 开始版本更新
    /// </summary>
    /// <param name="gameid">游戏id</param>
    /// <param name="localversion">当前版本</param>
    /// <param name="processCallback">回调</param>
    void Start(string gameid, string localversion,string sign, Action<float, string, bool> processCallback);


    /// <summary>
    /// 拷贝streaming asset
    /// </summary>
    /// <param name="appVersion"></param>
    void CopyStreamingAsset(float appVersion,Action callback);
}

