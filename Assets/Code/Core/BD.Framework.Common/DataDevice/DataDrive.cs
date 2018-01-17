using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


abstract public class ADataDrive
{
    /// <summary>
    /// 
    /// </summary>
    protected Dictionary<string, object> dataMap;
    //注册数据变动事件刷新
    protected Dictionary<string, Action<object>> whenDataChangeCallbackMap;

    public ADataDrive()
    {
        dataMap = new Dictionary<string, object>();
        whenDataChangeCallbackMap = new Dictionary<string, Action<object>>();
    }

    virtual public void InitData()
    {


    }
    /// <summary>
    /// 设置玩家数据
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    virtual public void SetData(string name, object value)
    {
        dataMap[name] = value;
        //调用数据改变
        if (whenDataChangeCallbackMap.ContainsKey(name))
        {
            whenDataChangeCallbackMap[name](value);
        }
    }

    /// <summary>
    /// 获取玩家数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    virtual public T GetData<T>(string name)
    {
        T t = default(T);
        if (dataMap.ContainsKey(name))
        {
            t = (T)dataMap[name];
        }
        return t;
    }

    /// <summary>
    /// 属性变动事件注册
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callback"></param>
    virtual  public void RegAction_WhenDataChange(string name, Action<object> callback)
    {
        whenDataChangeCallbackMap[name] = callback;
    }

    /// <summary>
    /// 移除属性变动事件注册
    /// </summary>
    /// <param name="name"></param>
    virtual public void RemoveAction_WhenPlayerDataChange(string name)
    {
        if (whenDataChangeCallbackMap.ContainsKey(name))
        {
            whenDataChangeCallbackMap.Remove(name);
        }
    }
}

