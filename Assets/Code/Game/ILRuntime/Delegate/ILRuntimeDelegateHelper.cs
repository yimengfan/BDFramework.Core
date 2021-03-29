using System.Collections;
using System.Collections.Generic;
using ILRuntime.Runtime.Enviorment;
using UnityEngine;
using System;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

public class ILRuntimeDelegateHelper
{
    static public void Register(AppDomain appdomain)
    {
        appdomain.DelegateManager.RegisterMethodDelegate<System.Object>();
        appdomain.DelegateManager
            .RegisterFunctionDelegate<ILRuntime.Runtime.Intepreter.ILTypeInstance, System.Boolean>();
        appdomain.DelegateManager.RegisterMethodDelegate<System.Collections.Generic.List<System.Object>>();
        appdomain.DelegateManager.RegisterMethodDelegate<System.Collections.Generic.IDictionary<System.String, UnityEngine.Object>>();
        appdomain.DelegateManager.RegisterMethodDelegate<System.Boolean>();
        appdomain.DelegateManager.RegisterMethodDelegate<System.Single>();
        appdomain.DelegateManager.RegisterFunctionDelegate<System.Object, System.Boolean>();
        appdomain.DelegateManager.RegisterFunctionDelegate<System.Attribute, System.Boolean>();
        appdomain.DelegateManager.RegisterFunctionDelegate<System.Object, ILRuntime.Runtime.Adapters.AttributeAdapter.Adapter>();

        appdomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction>((act) =>
        {
            return new UnityEngine.Events.UnityAction(() => { ((Action) act)(); });
        });
        appdomain.DelegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<System.Single>>((act) =>
        {
            return new UnityEngine.Events.UnityAction<System.Single>((arg0) =>
            {
                ((Action<System.Single>) act)(arg0);
            });
        });
        appdomain.DelegateManager.RegisterDelegateConvertor<System.Predicate<System.Object>>((act) =>
        {
            return new System.Predicate<System.Object>((obj) =>
            {
                return ((Func<System.Object, System.Boolean>) act)(obj);
            });
        });

        appdomain.DelegateManager.RegisterMethodDelegate<System.Boolean, UnityEngine.GameObject>();

        appdomain.DelegateManager.RegisterMethodDelegate<System.Int32, System.Int32>();

        appdomain.DelegateManager.RegisterMethodDelegate<System.String>();

        appdomain.DelegateManager.RegisterMethodDelegate<ILRuntime.Runtime.Intepreter.ILTypeInstance>();
        appdomain.DelegateManager.RegisterMethodDelegate<UnityEngine.GameObject>();

        appdomain.DelegateManager.RegisterMethodDelegate<UnityEngine.EventSystems.UIBehaviour, System.Object>();

        appdomain.DelegateManager.RegisterMethodDelegate<UnityEngine.Transform, System.Object>();

        appdomain.DelegateManager
            .RegisterDelegateConvertor<System.Predicate<ILRuntime.Runtime.Intepreter.ILTypeInstance>>((act) =>
            {
                return new System.Predicate<ILRuntime.Runtime.Intepreter.ILTypeInstance>((obj) =>
                {
                    return ((Func<ILRuntime.Runtime.Intepreter.ILTypeInstance, System.Boolean>) act)(obj);
                });
            });

//[insert]
    }
}