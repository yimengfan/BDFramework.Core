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
    unsafe class BDFramework_DataListener_StatusListenerExtensionEnum_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(BDFramework.DataListener.StatusListenerExtensionEnum);
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Object), typeof(System.Boolean)};
            method = type.GetMethod("SetData", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, SetData_0);
            Dictionary<string, List<MethodInfo>> genericMethods = new Dictionary<string, List<MethodInfo>>();
            List<MethodInfo> lst = null;                    
            foreach(var m in type.GetMethods())
            {
                if(m.IsGenericMethodDefinition)
                {
                    if (!genericMethods.TryGetValue(m.Name, out lst))
                    {
                        lst = new List<MethodInfo>();
                        genericMethods[m.Name] = lst;
                    }
                    lst.Add(m);
                }
            }
            args = new Type[]{typeof(System.Int32)};
            if (genericMethods.TryGetValue("GetData", out lst))
            {
                foreach(var m in lst)
                {
                    if(m.MatchGenericParameters(args, typeof(System.Int32), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, GetData_1);

                        break;
                    }
                }
            }
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<System.Object>), typeof(System.Int32), typeof(System.Int32), typeof(System.Boolean)};
            method = type.GetMethod("AddListener", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, AddListener_2);
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<System.Object>), typeof(System.Int32), typeof(System.Boolean)};
            method = type.GetMethod("AddListenerOnce", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, AddListenerOnce_3);
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Object), typeof(System.Boolean)};
            method = type.GetMethod("TriggerEvent", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, TriggerEvent_4);
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum)};
            method = type.GetMethod("RemoveListener", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RemoveListener_5);
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum)};
            method = type.GetMethod("ClearListener", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ClearListener_6);
            args = new Type[]{typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)};
            if (genericMethods.TryGetValue("AddListener", out lst))
            {
                foreach(var m in lst)
                {
                    if(m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.String), typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>), typeof(System.Int32), typeof(System.Int32), typeof(System.Boolean)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, AddListener_7);

                        break;
                    }
                }
            }
            args = new Type[]{typeof(System.String)};
            if (genericMethods.TryGetValue("AddListener", out lst))
            {
                foreach(var m in lst)
                {
                    if(m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<System.String>), typeof(System.Int32), typeof(System.Int32), typeof(System.Boolean)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, AddListener_8);

                        break;
                    }
                }
            }
            args = new Type[]{typeof(System.Object)};
            if (genericMethods.TryGetValue("AddListener", out lst))
            {
                foreach(var m in lst)
                {
                    if(m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<System.Object>), typeof(System.Int32), typeof(System.Int32), typeof(System.Boolean)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, AddListener_9);

                        break;
                    }
                }
            }
            args = new Type[]{typeof(ILRuntime.Runtime.Intepreter.ILTypeInstance)};
            if (genericMethods.TryGetValue("AddListener", out lst))
            {
                foreach(var m in lst)
                {
                    if(m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>), typeof(System.Int32), typeof(System.Int32), typeof(System.Boolean)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, AddListener_10);

                        break;
                    }
                }
            }
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<System.Object>)};
            method = type.GetMethod("RemoveListener", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RemoveListener_11);


        }


        static StackObject* SetData_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 4);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCallback = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Object @value = (System.Object)typeof(System.Object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Enum @enum = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.SetData(@dl, @enum, @value, @isTriggerCallback);

            return __ret;
        }

        static StackObject* GetData_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = BDFramework.DataListener.StatusListenerExtensionEnum.GetData<System.Int32>(@dl, @name);

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* AddListener_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 6);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @triggerNum = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Int32 @order = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.Action<System.Object> @action = (System.Action<System.Object>)typeof(System.Action<System.Object>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.AddListener(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* AddListenerOnce_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 5);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @oder = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Action<System.Object> @callback = (System.Action<System.Object>)typeof(System.Action<System.Object>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.AddListenerOnce(@dl, @name, @callback, @oder, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* TriggerEvent_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 4);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCallback = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Object @value = (System.Object)typeof(System.Object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.TriggerEvent(@dl, @name, @value, @isTriggerCallback);

            return __ret;
        }

        static StackObject* RemoveListener_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.RemoveListener(@dl, @name);

            return __ret;
        }

        static StackObject* ClearListener_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.ClearListener(@dl, @name);

            return __ret;
        }

        static StackObject* AddListener_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 6);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @triggerNum = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Int32 @order = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance> @action = (System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>)typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            System.String @name = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.AddListener<ILRuntime.Runtime.Intepreter.ILTypeInstance>(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* AddListener_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 6);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @triggerNum = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Int32 @order = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.Action<System.String> @action = (System.Action<System.String>)typeof(System.Action<System.String>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.AddListener<System.String>(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* AddListener_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 6);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @triggerNum = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Int32 @order = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.Action<System.Object> @action = (System.Action<System.Object>)typeof(System.Action<System.Object>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.AddListener<System.Object>(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* AddListener_10(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 6);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @isTriggerCacheData = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @triggerNum = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            System.Int32 @order = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance> @action = (System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>)typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.AddListener<ILRuntime.Runtime.Intepreter.ILTypeInstance>(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* RemoveListener_11(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 3);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Action<System.Object> @callback = (System.Action<System.Object>)typeof(System.Action<System.Object>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Enum @name = (System.Enum)typeof(System.Enum).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.StatusListenerExtensionEnum.RemoveListener(@dl, @name, @callback);

            return __ret;
        }



    }
}
