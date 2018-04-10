using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


abstract public class ADataDrive
{
    public delegate void CallBack(object o);
    /// <summary>
    /// 
    /// </summary>
    protected Dictionary<string, object> dataMap;
    //注册数据变动事件刷新
    protected Dictionary<string,CallBack> callbackMap;

    public ADataDrive()
    {
        dataMap = new Dictionary<string, object>();
        callbackMap = new Dictionary<string,CallBack>();
    }

    virtual public void InitData()
    {


    }
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    virtual public void SetData(string name, object value ,bool isUseCallback = true)
    {
        dataMap[name] = value;
        //调用数据改变
        if (isUseCallback && callbackMap.ContainsKey(name))
        {
            callbackMap[name](value);
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
        else
        {
            dataMap[name] = t;
        }
        return t;
    }

    /// <summary>
    /// 属性变动事件注册
    /// </summary>
    /// <param name="name"></param>
    /// <param name="callback"></param>
    virtual  public void RegAction(string name, CallBack callback)
    {
        CallBack cal = null;
        callbackMap.TryGetValue(name, out cal);
        if (cal == null)
        {
            callbackMap[name] = callback;
        }
        else
        {
            cal += callback;
        }
    }
    
    /// <summary>
    /// 移除属性变动事件注册
    /// </summary>
    /// <param name="name"></param>
    virtual public void RemoveAction(string name)
    {
        if (callbackMap.ContainsKey(name))
        {
            callbackMap.Remove(name);
        }
    }
    
    
    /// <summary>
    /// 移除属性变动事件注册
    /// </summary>
    /// <param name="name"></param>
    virtual public void RemoveAction(string name , CallBack callback)
    {
        CallBack cal = null;
        callbackMap.TryGetValue(name, out cal);
        if (cal != null)
        {
            cal -= callback;
        }
    }
}

