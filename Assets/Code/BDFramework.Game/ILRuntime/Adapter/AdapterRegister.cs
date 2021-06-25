using System;
using System.Collections;
using System.Collections.Generic;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;

public class AdapterRegister
{
    public static void RegisterCrossBindingAdaptor(ILRuntime.Runtime.Enviorment.AppDomain domain)
    {

        //这几条是手写的
        domain.RegisterCrossBindingAdaptor(new MonoBehaviourAdapter());
        domain.RegisterCrossBindingAdaptor(new CoroutineAdapter());
        domain.RegisterCrossBindingAdaptor(new IMessageAdapter());
        domain.DelegateManager.RegisterFunctionDelegate<IMessageAdapter.Adaptor>();

        //以下是自动生成的
        domain.RegisterCrossBindingAdaptor(new ScriptableObjectAdapter());
        domain.RegisterCrossBindingAdaptor(new ExceptionAdapter());
        domain.RegisterCrossBindingAdaptor(new IEnumerableAdapter());
        domain.RegisterCrossBindingAdaptor(new IAsyncStateMachineAdapter());
    }
}