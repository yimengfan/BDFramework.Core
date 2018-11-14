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
    unsafe class UnityEngine_UI_ContentSizeFitter_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.ContentSizeFitter);
            args = new Type[]{};
            method = type.GetMethod("get_horizontalFit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_horizontalFit_0);
            args = new Type[]{typeof(UnityEngine.UI.ContentSizeFitter.FitMode)};
            method = type.GetMethod("set_horizontalFit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_horizontalFit_1);
            args = new Type[]{};
            method = type.GetMethod("get_verticalFit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_verticalFit_2);
            args = new Type[]{typeof(UnityEngine.UI.ContentSizeFitter.FitMode)};
            method = type.GetMethod("set_verticalFit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_verticalFit_3);
            args = new Type[]{};
            method = type.GetMethod("SetLayoutHorizontal", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, SetLayoutHorizontal_4);
            args = new Type[]{};
            method = type.GetMethod("SetLayoutVertical", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, SetLayoutVertical_5);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.ContentSizeFitter[s]);


        }


        static StackObject* get_horizontalFit_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ContentSizeFitter instance_of_this_method = (UnityEngine.UI.ContentSizeFitter)typeof(UnityEngine.UI.ContentSizeFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.horizontalFit;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_horizontalFit_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ContentSizeFitter.FitMode @value = (UnityEngine.UI.ContentSizeFitter.FitMode)typeof(UnityEngine.UI.ContentSizeFitter.FitMode).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.ContentSizeFitter instance_of_this_method = (UnityEngine.UI.ContentSizeFitter)typeof(UnityEngine.UI.ContentSizeFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.horizontalFit = value;

            return __ret;
        }

        static StackObject* get_verticalFit_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ContentSizeFitter instance_of_this_method = (UnityEngine.UI.ContentSizeFitter)typeof(UnityEngine.UI.ContentSizeFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.verticalFit;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_verticalFit_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ContentSizeFitter.FitMode @value = (UnityEngine.UI.ContentSizeFitter.FitMode)typeof(UnityEngine.UI.ContentSizeFitter.FitMode).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.ContentSizeFitter instance_of_this_method = (UnityEngine.UI.ContentSizeFitter)typeof(UnityEngine.UI.ContentSizeFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.verticalFit = value;

            return __ret;
        }

        static StackObject* SetLayoutHorizontal_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ContentSizeFitter instance_of_this_method = (UnityEngine.UI.ContentSizeFitter)typeof(UnityEngine.UI.ContentSizeFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.SetLayoutHorizontal();

            return __ret;
        }

        static StackObject* SetLayoutVertical_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ContentSizeFitter instance_of_this_method = (UnityEngine.UI.ContentSizeFitter)typeof(UnityEngine.UI.ContentSizeFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.SetLayoutVertical();

            return __ret;
        }





    }
}
