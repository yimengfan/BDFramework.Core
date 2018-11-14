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
    unsafe class UnityEngine_EventSystems_PhysicsRaycaster_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.EventSystems.PhysicsRaycaster);
            args = new Type[]{};
            method = type.GetMethod("get_eventCamera", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_eventCamera_0);
            args = new Type[]{};
            method = type.GetMethod("get_depth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_depth_1);
            args = new Type[]{};
            method = type.GetMethod("get_finalEventMask", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_finalEventMask_2);
            args = new Type[]{};
            method = type.GetMethod("get_eventMask", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_eventMask_3);
            args = new Type[]{typeof(UnityEngine.LayerMask)};
            method = type.GetMethod("set_eventMask", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_eventMask_4);
            args = new Type[]{};
            method = type.GetMethod("get_maxRayIntersections", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_maxRayIntersections_5);
            args = new Type[]{typeof(System.Int32)};
            method = type.GetMethod("set_maxRayIntersections", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_maxRayIntersections_6);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData), typeof(System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>)};
            method = type.GetMethod("Raycast", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Raycast_7);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.EventSystems.PhysicsRaycaster[s]);


        }


        static StackObject* get_eventCamera_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.eventCamera;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_depth_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.depth;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_finalEventMask_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.finalEventMask;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_eventMask_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.eventMask;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_eventMask_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.LayerMask @value = (UnityEngine.LayerMask)typeof(UnityEngine.LayerMask).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.eventMask = value;

            return __ret;
        }

        static StackObject* get_maxRayIntersections_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.maxRayIntersections;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_maxRayIntersections_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Int32 @value = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.maxRayIntersections = value;

            return __ret;
        }

        static StackObject* Raycast_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
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
            UnityEngine.EventSystems.PhysicsRaycaster instance_of_this_method = (UnityEngine.EventSystems.PhysicsRaycaster)typeof(UnityEngine.EventSystems.PhysicsRaycaster).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Raycast(@eventData, @resultAppendList);

            return __ret;
        }





    }
}
