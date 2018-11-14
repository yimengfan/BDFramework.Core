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
    unsafe class UnityEngine_UI_GraphicRaycaster_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.GraphicRaycaster);
            args = new Type[]{};
            method = type.GetMethod("get_sortOrderPriority", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_sortOrderPriority_0);
            args = new Type[]{};
            method = type.GetMethod("get_renderOrderPriority", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_renderOrderPriority_1);
            args = new Type[]{};
            method = type.GetMethod("get_ignoreReversedGraphics", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_ignoreReversedGraphics_2);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_ignoreReversedGraphics", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_ignoreReversedGraphics_3);
            args = new Type[]{};
            method = type.GetMethod("get_blockingObjects", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_blockingObjects_4);
            args = new Type[]{typeof(UnityEngine.UI.GraphicRaycaster.BlockingObjects)};
            method = type.GetMethod("set_blockingObjects", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_blockingObjects_5);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData), typeof(System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>)};
            method = type.GetMethod("Raycast", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Raycast_6);
            args = new Type[]{};
            method = type.GetMethod("get_eventCamera", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_eventCamera_7);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.GraphicRaycaster[s]);


        }


        static StackObject* get_sortOrderPriority_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.sortOrderPriority;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_renderOrderPriority_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.renderOrderPriority;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_ignoreReversedGraphics_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.ignoreReversedGraphics;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_ignoreReversedGraphics_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.ignoreReversedGraphics = value;

            return __ret;
        }

        static StackObject* get_blockingObjects_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.blockingObjects;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_blockingObjects_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.GraphicRaycaster.BlockingObjects @value = (UnityEngine.UI.GraphicRaycaster.BlockingObjects)typeof(UnityEngine.UI.GraphicRaycaster.BlockingObjects).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.blockingObjects = value;

            return __ret;
        }

        static StackObject* Raycast_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 3);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> @resultAppendList = (System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>)typeof(System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.PointerEventData @eventData = (UnityEngine.EventSystems.PointerEventData)typeof(UnityEngine.EventSystems.PointerEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 3);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Raycast(@eventData, @resultAppendList);

            return __ret;
        }

        static StackObject* get_eventCamera_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.GraphicRaycaster instance_of_this_method = (UnityEngine.UI.GraphicRaycaster)typeof(UnityEngine.UI.GraphicRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.eventCamera;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }





    }
}
