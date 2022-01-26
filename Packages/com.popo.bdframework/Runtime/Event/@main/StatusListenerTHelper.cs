using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ILRuntime.CLR.TypeSystem;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using ILRuntime.Reflection;
using ILRuntime.CLR.Utils;

namespace ILRuntime.Runtime.Generated
{
    unsafe class StatusListenerTHelper
    {
        public static void RegisterCLRRedirection(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            MethodBase method;
            Type[] args;
            Type type = typeof(BDFramework.DataListener.StatusListenerExtensionT);
            Dictionary<string, List<MethodInfo>> genericMethods = new Dictionary<string, List<MethodInfo>>();
            List<MethodInfo> lst = null;
            foreach (var m in type.GetMethods())
            {
                if (m.IsGenericMethodDefinition)
                {
                    if (!genericMethods.TryGetValue(m.Name, out lst))
                    {
                        lst = new List<MethodInfo>();
                        genericMethods[m.Name] = lst;
                    }

                    lst.Add(m);
                }
            }


            args = new Type[] {typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)};
            if (genericMethods.TryGetValue("AddListener", out lst))
            {
                foreach (var m in lst)
                {
                    if (m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>), typeof(System.Int32), typeof(System.Int32), typeof(System.Boolean)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, AddListener_2);

                        break;
                    }
                }
            }

            args = new Type[] {typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)};
            if (genericMethods.TryGetValue("AddListenerOnce", out lst))
            {
                foreach (var m in lst)
                {
                    if (m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>), typeof(System.Int32), typeof(System.Boolean)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, AddListenerOnce_3);

                        break;
                    }
                }
            }

            args = new Type[] {typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)};
            if (genericMethods.TryGetValue("RemoveListener", out lst))
            {
                foreach (var m in lst)
                {
                    if (m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, RemoveListener_4);

                        break;
                    }
                }
            }

            args = new Type[] {typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)};
            if (genericMethods.TryGetValue("ClearListener", out lst))
            {
                foreach (var m in lst)
                {
                    if (m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, ClearListener_5);

                        break;
                    }
                }
            }

            args = new Type[] {typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)};
            if (genericMethods.TryGetValue("TriggerEvent", out lst))
            {
                foreach (var m in lst)
                {
                    if (m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, TriggerEvent_6);

                        break;
                    }
                }
            }
        }

        static StackObject* AddListener_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 5);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @triggerNum = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Int32 @order = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance> @action =
                (System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>) typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener) typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 0);
            __intp.Free(ptr_of_this_method);


            //拿到T触发
            var t0 = __method.GenericArguments[0];
            var typename = t0.ReflectionType.FullName;
            @dl.AddListener(typename, @action, @order, @triggerNum, @isTriggerCacheData);
            return __ret;
        }

        static StackObject* AddListenerOnce_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 4);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @order = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance> @callback =
                (System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>) typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener) typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 0);
            __intp.Free(ptr_of_this_method);

            

            //拿到T触发
            var t0 = __method.GenericArguments[0];
            var typename = t0.ReflectionType.FullName;
            @dl.AddListener(typename, @callback, @order, 1, @isTriggerCacheData);
            
            return __ret;
        }

        static StackObject* RemoveListener_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance> @callback =
                (System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>) typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener) typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 0);
            __intp.Free(ptr_of_this_method);

            
            
            //拿到T触发
            var t0 = __method.GenericArguments[0];
            var typename = t0.ReflectionType.FullName;
            @dl.RemoveListener(typename,@callback);
          

            return __ret;
        }

        static StackObject* ClearListener_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener) typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 0);
            __intp.Free(ptr_of_this_method);

            
            //拿到T触发
            var t0 = __method.GenericArguments[0];
            var typename = t0.ReflectionType.FullName;
            @dl.ClearListener(typename);

            return __ret;
        }

        static StackObject* TriggerEvent_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            ILRuntime.Runtime.Intepreter.ILTypeInstance @value = (ILRuntime.Runtime.Intepreter.ILTypeInstance) typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener) typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags) 0);
            __intp.Free(ptr_of_this_method);

            //拿到T触发
            var t0 = __method.GenericArguments[0];
            @dl.TriggerEvent(t0.ReflectionType.FullName, @value);

            return __ret;
        }
    }
}