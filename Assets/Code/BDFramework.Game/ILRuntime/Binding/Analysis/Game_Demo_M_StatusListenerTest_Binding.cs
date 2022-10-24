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
    unsafe class Game_Demo_M_StatusListenerTest_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            FieldInfo field;
            Type[] args;
            Type type = typeof(Game.Demo.M_StatusListenerTest);

            field = type.GetField("TestStr", flag);
            app.RegisterCLRFieldGetter(field, get_TestStr_0);
            app.RegisterCLRFieldSetter(field, set_TestStr_0);
            app.RegisterCLRFieldBinding(field, CopyToStack_TestStr_0, AssignFromStack_TestStr_0);

            args = new Type[]{};
            method = type.GetConstructor(flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Ctor_0);

        }



        static object get_TestStr_0(ref object o)
        {
            return ((Game.Demo.M_StatusListenerTest)o).TestStr;
        }

        static StackObject* CopyToStack_TestStr_0(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((Game.Demo.M_StatusListenerTest)o).TestStr;
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static void set_TestStr_0(ref object o, object v)
        {
            ((Game.Demo.M_StatusListenerTest)o).TestStr = (System.String)v;
        }

        static StackObject* AssignFromStack_TestStr_0(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            System.String @TestStr = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)0);
            ((Game.Demo.M_StatusListenerTest)o).TestStr = @TestStr;
            return ptr_of_this_method;
        }


        static StackObject* Ctor_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);

            var result_of_this_method = new Game.Demo.M_StatusListenerTest();

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }


    }
}
