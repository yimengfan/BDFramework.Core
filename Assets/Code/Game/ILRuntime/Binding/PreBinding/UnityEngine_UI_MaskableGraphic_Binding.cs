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
    unsafe class UnityEngine_UI_MaskableGraphic_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.MaskableGraphic);
            args = new Type[]{};
            method = type.GetMethod("get_onCullStateChanged", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_onCullStateChanged_0);
            args = new Type[]{typeof(UnityEngine.UI.MaskableGraphic.CullStateChangedEvent)};
            method = type.GetMethod("set_onCullStateChanged", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_onCullStateChanged_1);
            args = new Type[]{};
            method = type.GetMethod("get_maskable", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_maskable_2);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_maskable", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_maskable_3);
            args = new Type[]{typeof(UnityEngine.Material)};
            method = type.GetMethod("GetModifiedMaterial", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, GetModifiedMaterial_4);
            args = new Type[]{typeof(UnityEngine.Rect), typeof(System.Boolean)};
            method = type.GetMethod("Cull", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Cull_5);
            args = new Type[]{typeof(UnityEngine.Rect), typeof(System.Boolean)};
            method = type.GetMethod("SetClipRect", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, SetClipRect_6);
            args = new Type[]{};
            method = type.GetMethod("RecalculateClipping", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RecalculateClipping_7);
            args = new Type[]{};
            method = type.GetMethod("RecalculateMasking", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RecalculateMasking_8);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.MaskableGraphic[s]);


        }


        static StackObject* get_onCullStateChanged_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.onCullStateChanged;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_onCullStateChanged_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.MaskableGraphic.CullStateChangedEvent @value = (UnityEngine.UI.MaskableGraphic.CullStateChangedEvent)typeof(UnityEngine.UI.MaskableGraphic.CullStateChangedEvent).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.onCullStateChanged = value;

            return __ret;
        }

        static StackObject* get_maskable_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.maskable;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_maskable_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.maskable = value;

            return __ret;
        }

        static StackObject* GetModifiedMaterial_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.Material @baseMaterial = (UnityEngine.Material)typeof(UnityEngine.Material).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.GetModifiedMaterial(@baseMaterial);

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* Cull_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 3);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @validRect = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.Rect @clipRect = (UnityEngine.Rect)typeof(UnityEngine.Rect).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Cull(@clipRect, @validRect);

            return __ret;
        }

        static StackObject* SetClipRect_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 3);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @validRect = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.Rect @clipRect = (UnityEngine.Rect)typeof(UnityEngine.Rect).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.SetClipRect(@clipRect, @validRect);

            return __ret;
        }

        static StackObject* RecalculateClipping_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.RecalculateClipping();

            return __ret;
        }

        static StackObject* RecalculateMasking_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.MaskableGraphic instance_of_this_method = (UnityEngine.UI.MaskableGraphic)typeof(UnityEngine.UI.MaskableGraphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.RecalculateMasking();

            return __ret;
        }





    }
}
