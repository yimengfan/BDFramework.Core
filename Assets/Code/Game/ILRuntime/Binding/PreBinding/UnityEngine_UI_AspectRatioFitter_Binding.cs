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
    unsafe class UnityEngine_UI_AspectRatioFitter_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.AspectRatioFitter);
            args = new Type[]{};
            method = type.GetMethod("get_aspectMode", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_aspectMode_0);
            args = new Type[]{typeof(UnityEngine.UI.AspectRatioFitter.AspectMode)};
            method = type.GetMethod("set_aspectMode", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_aspectMode_1);
            args = new Type[]{};
            method = type.GetMethod("get_aspectRatio", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_aspectRatio_2);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_aspectRatio", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_aspectRatio_3);
            args = new Type[]{};
            method = type.GetMethod("SetLayoutHorizontal", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, SetLayoutHorizontal_4);
            args = new Type[]{};
            method = type.GetMethod("SetLayoutVertical", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, SetLayoutVertical_5);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.AspectRatioFitter[s]);


        }


        static StackObject* get_aspectMode_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AspectRatioFitter instance_of_this_method = (UnityEngine.UI.AspectRatioFitter)typeof(UnityEngine.UI.AspectRatioFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.aspectMode;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_aspectMode_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AspectRatioFitter.AspectMode @value = (UnityEngine.UI.AspectRatioFitter.AspectMode)typeof(UnityEngine.UI.AspectRatioFitter.AspectMode).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.AspectRatioFitter instance_of_this_method = (UnityEngine.UI.AspectRatioFitter)typeof(UnityEngine.UI.AspectRatioFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.aspectMode = value;

            return __ret;
        }

        static StackObject* get_aspectRatio_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AspectRatioFitter instance_of_this_method = (UnityEngine.UI.AspectRatioFitter)typeof(UnityEngine.UI.AspectRatioFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.aspectRatio;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_aspectRatio_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.AspectRatioFitter instance_of_this_method = (UnityEngine.UI.AspectRatioFitter)typeof(UnityEngine.UI.AspectRatioFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.aspectRatio = value;

            return __ret;
        }

        static StackObject* SetLayoutHorizontal_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.AspectRatioFitter instance_of_this_method = (UnityEngine.UI.AspectRatioFitter)typeof(UnityEngine.UI.AspectRatioFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
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
            UnityEngine.UI.AspectRatioFitter instance_of_this_method = (UnityEngine.UI.AspectRatioFitter)typeof(UnityEngine.UI.AspectRatioFitter).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.SetLayoutVertical();

            return __ret;
        }





    }
}
