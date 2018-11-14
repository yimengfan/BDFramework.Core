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
    unsafe class UnityEngine_UI_CanvasScaler_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.CanvasScaler);
            args = new Type[]{};
            method = type.GetMethod("get_uiScaleMode", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_uiScaleMode_0);
            args = new Type[]{typeof(UnityEngine.UI.CanvasScaler.ScaleMode)};
            method = type.GetMethod("set_uiScaleMode", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_uiScaleMode_1);
            args = new Type[]{};
            method = type.GetMethod("get_referencePixelsPerUnit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_referencePixelsPerUnit_2);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_referencePixelsPerUnit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_referencePixelsPerUnit_3);
            args = new Type[]{};
            method = type.GetMethod("get_scaleFactor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_scaleFactor_4);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_scaleFactor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_scaleFactor_5);
            args = new Type[]{};
            method = type.GetMethod("get_referenceResolution", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_referenceResolution_6);
            args = new Type[]{typeof(UnityEngine.Vector2)};
            method = type.GetMethod("set_referenceResolution", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_referenceResolution_7);
            args = new Type[]{};
            method = type.GetMethod("get_screenMatchMode", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_screenMatchMode_8);
            args = new Type[]{typeof(UnityEngine.UI.CanvasScaler.ScreenMatchMode)};
            method = type.GetMethod("set_screenMatchMode", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_screenMatchMode_9);
            args = new Type[]{};
            method = type.GetMethod("get_matchWidthOrHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_matchWidthOrHeight_10);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_matchWidthOrHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_matchWidthOrHeight_11);
            args = new Type[]{};
            method = type.GetMethod("get_physicalUnit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_physicalUnit_12);
            args = new Type[]{typeof(UnityEngine.UI.CanvasScaler.Unit)};
            method = type.GetMethod("set_physicalUnit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_physicalUnit_13);
            args = new Type[]{};
            method = type.GetMethod("get_fallbackScreenDPI", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_fallbackScreenDPI_14);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_fallbackScreenDPI", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_fallbackScreenDPI_15);
            args = new Type[]{};
            method = type.GetMethod("get_defaultSpriteDPI", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_defaultSpriteDPI_16);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_defaultSpriteDPI", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_defaultSpriteDPI_17);
            args = new Type[]{};
            method = type.GetMethod("get_dynamicPixelsPerUnit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_dynamicPixelsPerUnit_18);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_dynamicPixelsPerUnit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_dynamicPixelsPerUnit_19);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.CanvasScaler[s]);


        }


        static StackObject* get_uiScaleMode_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.uiScaleMode;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_uiScaleMode_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler.ScaleMode @value = (UnityEngine.UI.CanvasScaler.ScaleMode)typeof(UnityEngine.UI.CanvasScaler.ScaleMode).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.uiScaleMode = value;

            return __ret;
        }

        static StackObject* get_referencePixelsPerUnit_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.referencePixelsPerUnit;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_referencePixelsPerUnit_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.referencePixelsPerUnit = value;

            return __ret;
        }

        static StackObject* get_scaleFactor_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.scaleFactor;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_scaleFactor_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.scaleFactor = value;

            return __ret;
        }

        static StackObject* get_referenceResolution_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.referenceResolution;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_referenceResolution_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.Vector2 @value = (UnityEngine.Vector2)typeof(UnityEngine.Vector2).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.referenceResolution = value;

            return __ret;
        }

        static StackObject* get_screenMatchMode_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.screenMatchMode;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_screenMatchMode_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler.ScreenMatchMode @value = (UnityEngine.UI.CanvasScaler.ScreenMatchMode)typeof(UnityEngine.UI.CanvasScaler.ScreenMatchMode).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.screenMatchMode = value;

            return __ret;
        }

        static StackObject* get_matchWidthOrHeight_10(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.matchWidthOrHeight;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_matchWidthOrHeight_11(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.matchWidthOrHeight = value;

            return __ret;
        }

        static StackObject* get_physicalUnit_12(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.physicalUnit;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_physicalUnit_13(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler.Unit @value = (UnityEngine.UI.CanvasScaler.Unit)typeof(UnityEngine.UI.CanvasScaler.Unit).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.physicalUnit = value;

            return __ret;
        }

        static StackObject* get_fallbackScreenDPI_14(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.fallbackScreenDPI;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_fallbackScreenDPI_15(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.fallbackScreenDPI = value;

            return __ret;
        }

        static StackObject* get_defaultSpriteDPI_16(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.defaultSpriteDPI;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_defaultSpriteDPI_17(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.defaultSpriteDPI = value;

            return __ret;
        }

        static StackObject* get_dynamicPixelsPerUnit_18(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.dynamicPixelsPerUnit;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_dynamicPixelsPerUnit_19(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.CanvasScaler instance_of_this_method = (UnityEngine.UI.CanvasScaler)typeof(UnityEngine.UI.CanvasScaler).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.dynamicPixelsPerUnit = value;

            return __ret;
        }





    }
}
