using System;
using System.Collections.Generic;
using System.Linq;
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
    unsafe class BDFramework_ILRuntimeHelper_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            FieldInfo field;
            Type[] args;
            Type type = typeof(BDFramework.ILRuntimeHelper);
            args = new Type[]{};
            method = type.GetMethod("get_AppDomain", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_AppDomain_0);
            args = new Type[]{};
            method = type.GetMethod("get_IsRunning", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_IsRunning_1);
            args = new Type[]{};
            method = type.GetMethod("GetHotfixTypes", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, GetHotfixTypes_2);

            field = type.GetField("UIComponentTypes", flag);
            app.RegisterCLRFieldGetter(field, get_UIComponentTypes_0);
            app.RegisterCLRFieldSetter(field, set_UIComponentTypes_0);
            app.RegisterCLRFieldBinding(field, CopyToStack_UIComponentTypes_0, AssignFromStack_UIComponentTypes_0);


        }


        static StackObject* get_AppDomain_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = BDFramework.ILRuntimeHelper.AppDomain;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_IsRunning_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = BDFramework.ILRuntimeHelper.IsRunning;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* GetHotfixTypes_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = BDFramework.ILRuntimeHelper.GetHotfixTypes();

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }


        static object get_UIComponentTypes_0(ref object o)
        {
            return BDFramework.ILRuntimeHelper.UIComponentTypes;
        }

        static StackObject* CopyToStack_UIComponentTypes_0(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = BDFramework.ILRuntimeHelper.UIComponentTypes;
            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static void set_UIComponentTypes_0(ref object o, object v)
        {
            BDFramework.ILRuntimeHelper.UIComponentTypes = (System.Collections.Generic.Dictionary<System.String, System.Type>)v;
        }

        static StackObject* AssignFromStack_UIComponentTypes_0(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            System.Collections.Generic.Dictionary<System.String, System.Type> @UIComponentTypes = (System.Collections.Generic.Dictionary<System.String, System.Type>)typeof(System.Collections.Generic.Dictionary<System.String, System.Type>).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            BDFramework.ILRuntimeHelper.UIComponentTypes = @UIComponentTypes;
            return ptr_of_this_method;
        }



    }
}
