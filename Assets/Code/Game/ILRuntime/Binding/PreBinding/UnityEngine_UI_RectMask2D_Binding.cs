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
    unsafe class UnityEngine_UI_RectMask2D_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.RectMask2D);
            args = new Type[]{};
            method = type.GetMethod("get_canvasRect", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_canvasRect_0);
            args = new Type[]{};
            method = type.GetMethod("get_rectTransform", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_rectTransform_1);
            args = new Type[]{typeof(UnityEngine.Vector2), typeof(UnityEngine.Camera)};
            method = type.GetMethod("IsRaycastLocationValid", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, IsRaycastLocationValid_2);
            args = new Type[]{};
            method = type.GetMethod("PerformClipping", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, PerformClipping_3);
            args = new Type[]{typeof(UnityEngine.UI.IClippable)};
            method = type.GetMethod("AddClippable", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, AddClippable_4);
            args = new Type[]{typeof(UnityEngine.UI.IClippable)};
            method = type.GetMethod("RemoveClippable", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RemoveClippable_5);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.RectMask2D[s]);


        }


        static StackObject* get_canvasRect_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.RectMask2D instance_of_this_method = (UnityEngine.UI.RectMask2D)typeof(UnityEngine.UI.RectMask2D).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.canvasRect;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_rectTransform_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.RectMask2D instance_of_this_method = (UnityEngine.UI.RectMask2D)typeof(UnityEngine.UI.RectMask2D).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.rectTransform;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* IsRaycastLocationValid_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 3);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.Camera @eventCamera = (UnityEngine.Camera)typeof(UnityEngine.Camera).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.Vector2 @sp = (UnityEngine.Vector2)typeof(UnityEngine.Vector2).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            UnityEngine.UI.RectMask2D instance_of_this_method = (UnityEngine.UI.RectMask2D)typeof(UnityEngine.UI.RectMask2D).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.IsRaycastLocationValid(@sp, @eventCamera);

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* PerformClipping_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.RectMask2D instance_of_this_method = (UnityEngine.UI.RectMask2D)typeof(UnityEngine.UI.RectMask2D).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.PerformClipping();

            return __ret;
        }

        static StackObject* AddClippable_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.IClippable @clippable = (UnityEngine.UI.IClippable)typeof(UnityEngine.UI.IClippable).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.RectMask2D instance_of_this_method = (UnityEngine.UI.RectMask2D)typeof(UnityEngine.UI.RectMask2D).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.AddClippable(@clippable);

            return __ret;
        }

        static StackObject* RemoveClippable_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.IClippable @clippable = (UnityEngine.UI.IClippable)typeof(UnityEngine.UI.IClippable).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.RectMask2D instance_of_this_method = (UnityEngine.UI.RectMask2D)typeof(UnityEngine.UI.RectMask2D).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.RemoveClippable(@clippable);

            return __ret;
        }





    }
}
