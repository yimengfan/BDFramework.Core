
using System;
using System.Collections.Generic;
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
    unsafe class BDFramework_BDLauncher_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            FieldInfo field;
            Type[] args;
            Type type = typeof(BDFramework.BDLauncher);
            args = new Type[]{typeof(System.Action)};
            method = type.GetMethod("set_OnStart", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_OnStart_0);
            args = new Type[]{typeof(System.Action)};
            method = type.GetMethod("set_OnUpdate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_OnUpdate_1);
            args = new Type[]{typeof(System.Action)};
            method = type.GetMethod("set_OnLateUpdate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_OnLateUpdate_2);

            field = type.GetField("IsCodeHotfix", flag);
            app.RegisterCLRFieldGetter(field, get_IsCodeHotfix_0);
            app.RegisterCLRFieldSetter(field, set_IsCodeHotfix_0);


        }


        static StackObject* set_OnStart_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Action @value = (System.Action)typeof(System.Action).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            BDFramework.BDLauncher.OnStart = value;

            return __ret;
        }

        static StackObject* set_OnUpdate_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Action @value = (System.Action)typeof(System.Action).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            BDFramework.BDLauncher.OnUpdate = value;

            return __ret;
        }

        static StackObject* set_OnLateUpdate_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Action @value = (System.Action)typeof(System.Action).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            BDFramework.BDLauncher.OnLateUpdate = value;

            return __ret;
        }


        static object get_IsCodeHotfix_0(ref object o)
        {
            return ((BDFramework.BDLauncher)o).IsCodeHotfix;
        }
        static void set_IsCodeHotfix_0(ref object o, object v)
        {
            ((BDFramework.BDLauncher)o).IsCodeHotfix = (System.Boolean)v;
        }


    }
}
