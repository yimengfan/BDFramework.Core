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
    unsafe class UnityEngine_UI_AnimationTriggers_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.AnimationTriggers);
            args = new Type[]{};
            method = type.GetMethod("get_normalTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_normalTrigger_0);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_normalTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_normalTrigger_1);
            args = new Type[]{};
            method = type.GetMethod("get_highlightedTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_highlightedTrigger_2);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_highlightedTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_highlightedTrigger_3);
            args = new Type[]{};
            method = type.GetMethod("get_pressedTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_pressedTrigger_4);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_pressedTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_pressedTrigger_5);
            args = new Type[]{};
            method = type.GetMethod("get_disabledTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_disabledTrigger_6);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_disabledTrigger", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_disabledTrigger_7);



            app.RegisterCLRCreateDefaultInstance(type, () => new UnityEngine.UI.AnimationTriggers());
            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.AnimationTriggers[s]);

            args = new Type[]{};
            method = type.GetConstructor(flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Ctor_0);

        }


        static StackObject* get_normalTrigger_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.normalTrigger;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_normalTrigger_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.normalTrigger = value;

            return __ret;
        }

        static StackObject* get_highlightedTrigger_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.highlightedTrigger;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_highlightedTrigger_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.highlightedTrigger = value;

            return __ret;
        }

        static StackObject* get_pressedTrigger_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.pressedTrigger;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_pressedTrigger_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.pressedTrigger = value;

            return __ret;
        }

        static StackObject* get_disabledTrigger_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.disabledTrigger;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_disabledTrigger_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.AnimationTriggers instance_of_this_method = (UnityEngine.UI.AnimationTriggers)typeof(UnityEngine.UI.AnimationTriggers).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.disabledTrigger = value;

            return __ret;
        }




        static StackObject* Ctor_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);

            var result_of_this_method = new UnityEngine.UI.AnimationTriggers();

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }


    }
}
