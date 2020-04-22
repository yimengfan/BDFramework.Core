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
    unsafe class UnityEngine_EventSystems_PointerInputModule_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            FieldInfo field;
            Type[] args;
            Type type = typeof(UnityEngine.EventSystems.PointerInputModule);
            args = new Type[]{typeof(System.Int32)};
            method = type.GetMethod("IsPointerOverGameObject", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, IsPointerOverGameObject_0);
            args = new Type[]{};
            method = type.GetMethod("ToString", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ToString_1);

            field = type.GetField("kMouseLeftId", flag);
            app.RegisterCLRFieldGetter(field, get_kMouseLeftId_0);
            app.RegisterCLRFieldBinding(field, CopyToStack_kMouseLeftId_0, null);
            field = type.GetField("kMouseRightId", flag);
            app.RegisterCLRFieldGetter(field, get_kMouseRightId_1);
            app.RegisterCLRFieldBinding(field, CopyToStack_kMouseRightId_1, null);
            field = type.GetField("kMouseMiddleId", flag);
            app.RegisterCLRFieldGetter(field, get_kMouseMiddleId_2);
            app.RegisterCLRFieldBinding(field, CopyToStack_kMouseMiddleId_2, null);
            field = type.GetField("kFakeTouchesId", flag);
            app.RegisterCLRFieldGetter(field, get_kFakeTouchesId_3);
            app.RegisterCLRFieldBinding(field, CopyToStack_kFakeTouchesId_3, null);


            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.EventSystems.PointerInputModule[s]);


        }


        static StackObject* IsPointerOverGameObject_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Int32 @pointerId = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.PointerInputModule instance_of_this_method = (UnityEngine.EventSystems.PointerInputModule)typeof(UnityEngine.EventSystems.PointerInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.IsPointerOverGameObject(@pointerId);

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* ToString_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PointerInputModule instance_of_this_method = (UnityEngine.EventSystems.PointerInputModule)typeof(UnityEngine.EventSystems.PointerInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.ToString();

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }


        static object get_kMouseLeftId_0(ref object o)
        {
            return UnityEngine.EventSystems.PointerInputModule.kMouseLeftId;
        }

        static StackObject* CopyToStack_kMouseLeftId_0(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = UnityEngine.EventSystems.PointerInputModule.kMouseLeftId;
            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static object get_kMouseRightId_1(ref object o)
        {
            return UnityEngine.EventSystems.PointerInputModule.kMouseRightId;
        }

        static StackObject* CopyToStack_kMouseRightId_1(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = UnityEngine.EventSystems.PointerInputModule.kMouseRightId;
            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static object get_kMouseMiddleId_2(ref object o)
        {
            return UnityEngine.EventSystems.PointerInputModule.kMouseMiddleId;
        }

        static StackObject* CopyToStack_kMouseMiddleId_2(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = UnityEngine.EventSystems.PointerInputModule.kMouseMiddleId;
            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static object get_kFakeTouchesId_3(ref object o)
        {
            return UnityEngine.EventSystems.PointerInputModule.kFakeTouchesId;
        }

        static StackObject* CopyToStack_kFakeTouchesId_3(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = UnityEngine.EventSystems.PointerInputModule.kFakeTouchesId;
            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }




    }
}
