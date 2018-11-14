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
    unsafe class UnityEngine_EventSystems_AbstractEventData_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.EventSystems.AbstractEventData);
            args = new Type[]{};
            method = type.GetMethod("Reset", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Reset_0);
            args = new Type[]{};
            method = type.GetMethod("Use", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Use_1);
            args = new Type[]{};
            method = type.GetMethod("get_used", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_used_2);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.EventSystems.AbstractEventData[s]);


        }


        static StackObject* Reset_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.AbstractEventData instance_of_this_method = (UnityEngine.EventSystems.AbstractEventData)typeof(UnityEngine.EventSystems.AbstractEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Reset();

            return __ret;
        }

        static StackObject* Use_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.AbstractEventData instance_of_this_method = (UnityEngine.EventSystems.AbstractEventData)typeof(UnityEngine.EventSystems.AbstractEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Use();

            return __ret;
        }

        static StackObject* get_used_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.AbstractEventData instance_of_this_method = (UnityEngine.EventSystems.AbstractEventData)typeof(UnityEngine.EventSystems.AbstractEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.used;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }





    }
}
