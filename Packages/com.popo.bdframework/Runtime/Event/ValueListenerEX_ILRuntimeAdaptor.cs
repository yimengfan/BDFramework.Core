using System;
using System.Collections.Generic;
using System.Reflection;
using ILRuntime.CLR.Method;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using ILRuntime.CLR.Utils;

namespace ILRuntime.Runtime.Generated
{
    /// <summary>
    /// 值监听的ilr拓展
    /// </summary>
    unsafe class ValueListenerEX_ILRuntimeAdaptor
    {
        public static void RegisterCLRRedirection(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(BDFramework.DataListener.ValueListenerEx);
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
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum)};
            method = type.GetMethod("RemoveListener", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RemoveListener_4);
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum)};
            method = type.GetMethod("ClearListener", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ClearListener_5);
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Object), typeof(System.Boolean)};
            method = type.GetMethod("TriggerEvent", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, TriggerEvent_6);
            args = new Type[]{typeof(System.Object)};
            if (genericMethods.TryGetValue("AddListener", out lst))
            {
                foreach(var m in lst)
                {
                    if(m.MatchGenericParameters(args, typeof(void), typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<System.Object>), typeof(System.Int32), typeof(System.Int32), typeof(System.Boolean)))
                    {
                        method = m.MakeGenericMethod(args);
                        app.RegisterCLRMethodRedirection(method, AddListener_7);

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
                        app.RegisterCLRMethodRedirection(method, AddListener_8);

                        break;
                    }
                }
            }
            args = new Type[]{typeof(BDFramework.DataListener.AStatusListener), typeof(System.Enum), typeof(System.Action<System.Object>)};
            method = type.GetMethod("RemoveListener", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RemoveListener_9);


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
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.SetData(@dl,@name, @value, @isTriggerCallback);

            return __ret;
        }

        static StackObject* GetData_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = BDFramework.DataListener.ValueListenerEx.GetData<System.Int32>(@dl, @name);

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
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.AddListener(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

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
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.AddListenerOnce(@dl, @name, @callback, @oder, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* RemoveListener_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.RemoveListener(@dl, @name);

            return __ret;
        }

        static StackObject* ClearListener_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.ClearListener(@dl, @name);

            return __ret;
        }

        static StackObject* TriggerEvent_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
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
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 4);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.TriggerEvent(@dl, @name, @value, @isTriggerCallback);

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
            System.Action<System.Object> @action = (System.Action<System.Object>)typeof(System.Action<System.Object>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.AddListener<System.Object>(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

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
            System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance> @action = (System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>)typeof(System.Action<ILRuntime.Runtime.Intepreter.ILTypeInstance>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 5);
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 6);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.AddListener<ILRuntime.Runtime.Intepreter.ILTypeInstance>(@dl, @name, @action, @order, @triggerNum, @isTriggerCacheData);

            return __ret;
        }

        static StackObject* RemoveListener_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 3);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Action<System.Object> @callback = (System.Action<System.Object>)typeof(System.Action<System.Object>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            //这里劫持enum进行修改
            var enumObj = StackObject.ToObject(ptr_of_this_method, __domain, __mStack);
            string @name = enumObj.ToString();
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            BDFramework.DataListener.AStatusListener @dl = (BDFramework.DataListener.AStatusListener)typeof(BDFramework.DataListener.AStatusListener).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            __intp.Free(ptr_of_this_method);


            BDFramework.DataListener.ValueListenerEx.RemoveListener(@dl, @name, @callback);

            return __ret;
        }



    }
}
