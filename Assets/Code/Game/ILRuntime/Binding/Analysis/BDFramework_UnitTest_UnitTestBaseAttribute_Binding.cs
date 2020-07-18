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
    unsafe class BDFramework_UnitTest_UnitTestBaseAttribute_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            FieldInfo field;
            Type[] args;
            Type type = typeof(BDFramework.UnitTest.UnitTestBaseAttribute);

            field = type.GetField("Order", flag);
            app.RegisterCLRFieldGetter(field, get_Order_0);
            app.RegisterCLRFieldSetter(field, set_Order_0);
            app.RegisterCLRFieldBinding(field, CopyToStack_Order_0, AssignFromStack_Order_0);
            field = type.GetField("Des", flag);
            app.RegisterCLRFieldGetter(field, get_Des_1);
            app.RegisterCLRFieldSetter(field, set_Des_1);
            app.RegisterCLRFieldBinding(field, CopyToStack_Des_1, AssignFromStack_Des_1);


        }



        static object get_Order_0(ref object o)
        {
            return ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Order;
        }

        static StackObject* CopyToStack_Order_0(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Order;
            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static void set_Order_0(ref object o, object v)
        {
            ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Order = (System.Int32)v;
        }

        static StackObject* AssignFromStack_Order_0(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            System.Int32 @Order = ptr_of_this_method->Value;
            ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Order = @Order;
            return ptr_of_this_method;
        }

        static object get_Des_1(ref object o)
        {
            return ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Des;
        }

        static StackObject* CopyToStack_Des_1(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Des;
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static void set_Des_1(ref object o, object v)
        {
            ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Des = (System.String)v;
        }

        static StackObject* AssignFromStack_Des_1(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            System.String @Des = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Des = @Des;
            return ptr_of_this_method;
        }



    }
}
