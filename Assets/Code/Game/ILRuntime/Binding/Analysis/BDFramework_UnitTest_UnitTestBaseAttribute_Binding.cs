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
            field = type.GetField("Des", flag);
            app.RegisterCLRFieldGetter(field, get_Des_1);
            app.RegisterCLRFieldSetter(field, set_Des_1);


        }



        static object get_Order_0(ref object o)
        {
            return ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Order;
        }
        static void set_Order_0(ref object o, object v)
        {
            ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Order = (System.Int32)v;
        }
        static object get_Des_1(ref object o)
        {
            return ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Des;
        }
        static void set_Des_1(ref object o, object v)
        {
            ((BDFramework.UnitTest.UnitTestBaseAttribute)o).Des = (System.String)v;
        }


    }
}
