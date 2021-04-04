
using System;
using System.Collections;
using System.Collections.Generic;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;

public class IGameStartAdapter:CrossBindingAdaptor
{
public override Type BaseCLRType
{
    get
    {
        return typeof(BDFramework.GameStart.IGameStart);//这是你想继承的那个类
    }
}
public override Type AdaptorType
{
    get
    {
        return typeof(Adaptor);//这是实际的适配器类
    }
}
public override object CreateCLRInstance(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
{
    return new Adaptor(appdomain, instance);//创建一个新的实例
}
//实际的适配器类需要继承你想继承的那个类，并且实现CrossBindingAdaptorType接口
public class Adaptor : BDFramework.GameStart.IGameStart, CrossBindingAdaptorType
{
    ILTypeInstance instance;
    ILRuntime.Runtime.Enviorment.AppDomain appdomain;
    //缓存这个数组来避免调用时的GC Alloc
    object[] param1 = new object[1];
    public Adaptor()
    {

    }
    public Adaptor(ILRuntime.Runtime.Enviorment.AppDomain appdomain, ILTypeInstance instance)
    {
        this.appdomain = appdomain;
        this.instance = instance;
    }
    public ILTypeInstance ILInstance { get { return instance; } }
bool m_bStartGot = false;
IMethod m_Start = null;
public  void Start ()
{
   if(!m_bStartGot)
   {
       m_Start = instance.Type.GetMethod("Start",0);
       m_bStartGot = true;
   }
          if(m_Start != null)
       {
            appdomain.Invoke(m_Start, instance,null);
        }
       else
       {
           
       } 
}
bool m_bUpdateGot = false;
IMethod m_Update = null;
public  void Update ()
{
   if(!m_bUpdateGot)
   {
       m_Update = instance.Type.GetMethod("Update",0);
       m_bUpdateGot = true;
   }
          if(m_Update != null)
       {
            appdomain.Invoke(m_Update, instance,null);
        }
       else
       {
           
       } 
}
bool m_bLateUpdateGot = false;
IMethod m_LateUpdate = null;
public  void LateUpdate ()
{
   if(!m_bLateUpdateGot)
   {
       m_LateUpdate = instance.Type.GetMethod("LateUpdate",0);
       m_bLateUpdateGot = true;
   }
          if(m_LateUpdate != null)
       {
            appdomain.Invoke(m_LateUpdate, instance,null);
        }
       else
       {
           
       } 
}
}
}