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
    unsafe class UnityEngine_UI_CanvasUpdateRegistry_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.CanvasUpdateRegistry);
            args = new Type[]{};
            method = type.GetMethod("get_instance", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_instance_0);
            args = new Type[]{typeof(UnityEngine.UI.ICanvasElement)};
            method = type.GetMethod("RegisterCanvasElementForLayoutRebuild", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RegisterCanvasElementForLayoutRebuild_1);
            args = new Type[]{typeof(UnityEngine.UI.ICanvasElement)};
            method = type.GetMethod("TryRegisterCanvasElementForLayoutRebuild", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, TryRegisterCanvasElementForLayoutRebuild_2);
            args = new Type[]{typeof(UnityEngine.UI.ICanvasElement)};
            method = type.GetMethod("RegisterCanvasElementForGraphicRebuild", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, RegisterCanvasElementForGraphicRebuild_3);
            args = new Type[]{typeof(UnityEngine.UI.ICanvasElement)};
            method = type.GetMethod("TryRegisterCanvasElementForGraphicRebuild", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, TryRegisterCanvasElementForGraphicRebuild_4);
            args = new Type[]{typeof(UnityEngine.UI.ICanvasElement)};
            method = type.GetMethod("UnRegisterCanvasElementForRebuild", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, UnRegisterCanvasElementForRebuild_5);
            args = new Type[]{};
            method = type.GetMethod("IsRebuildingLayout", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, IsRebuildingLayout_6);
            args = new Type[]{};
            method = type.GetMethod("IsRebuildingGraphics", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, IsRebuildingGraphics_7);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.CanvasUpdateRegistry[s]);


        }


        static StackObject* get_instance_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.UI.CanvasUpdateRegistry.instance;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* RegisterCanvasElementForLayoutRebuild_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ICanvasElement @element = (UnityEngine.UI.ICanvasElement)typeof(UnityEngine.UI.ICanvasElement).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            UnityEngine.UI.CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(@element);

            return __ret;
        }

        static StackObject* TryRegisterCanvasElementForLayoutRebuild_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ICanvasElement @element = (UnityEngine.UI.ICanvasElement)typeof(UnityEngine.UI.ICanvasElement).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(@element);

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* RegisterCanvasElementForGraphicRebuild_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ICanvasElement @element = (UnityEngine.UI.ICanvasElement)typeof(UnityEngine.UI.ICanvasElement).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            UnityEngine.UI.CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(@element);

            return __ret;
        }

        static StackObject* TryRegisterCanvasElementForGraphicRebuild_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ICanvasElement @element = (UnityEngine.UI.ICanvasElement)typeof(UnityEngine.UI.ICanvasElement).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.CanvasUpdateRegistry.TryRegisterCanvasElementForGraphicRebuild(@element);

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* UnRegisterCanvasElementForRebuild_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ICanvasElement @element = (UnityEngine.UI.ICanvasElement)typeof(UnityEngine.UI.ICanvasElement).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            UnityEngine.UI.CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(@element);

            return __ret;
        }

        static StackObject* IsRebuildingLayout_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingLayout();

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* IsRebuildingGraphics_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingGraphics();

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }





    }
}
