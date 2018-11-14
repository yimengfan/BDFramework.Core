
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
            args = new Type[]{};
            method = type.GetMethod("get_OnUpdate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_OnUpdate_0);
            args = new Type[]{typeof(BDFramework.BDLauncher.OnLife)};
            method = type.GetMethod("set_OnUpdate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_OnUpdate_1);
            args = new Type[]{};
            method = type.GetMethod("get_OnLateUpdate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_OnLateUpdate_2);
            args = new Type[]{typeof(BDFramework.BDLauncher.OnLife)};
            method = type.GetMethod("set_OnLateUpdate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_OnLateUpdate_3);

            field = type.GetField("IsCodeHotfix", flag);
            app.RegisterCLRFieldGetter(field, get_IsCodeHotfix_0);
            app.RegisterCLRFieldSetter(field, set_IsCodeHotfix_0);


        }


        static StackObject* get_OnUpdate_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = BDFramework.BDLauncher.OnUpdate;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_OnUpdate_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            BDFramework.BDLauncher.OnLife @value = (BDFramework.BDLauncher.OnLife)typeof(BDFramework.BDLauncher.OnLife).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            BDFramework.BDLauncher.OnUpdate = value;

            return __ret;
        }

        static StackObject* get_OnLateUpdate_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = BDFramework.BDLauncher.OnLateUpdate;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_OnLateUpdate_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            BDFramework.BDLauncher.OnLife @value = (BDFramework.BDLauncher.OnLife)typeof(BDFramework.BDLauncher.OnLife).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
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
