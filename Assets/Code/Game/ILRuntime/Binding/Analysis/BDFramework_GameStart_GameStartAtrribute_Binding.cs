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
    unsafe class BDFramework_GameStart_GameStartAtrribute_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            FieldInfo field;
            Type[] args;
            Type type = typeof(BDFramework.GameStart.GameStartAtrribute);

            field = type.GetField("Index", flag);
            app.RegisterCLRFieldGetter(field, get_Index_0);
            app.RegisterCLRFieldSetter(field, set_Index_0);


        }



        static object get_Index_0(ref object o)
        {
            return ((BDFramework.GameStart.GameStartAtrribute)o).Index;
        }
        static void set_Index_0(ref object o, object v)
        {
            ((BDFramework.GameStart.GameStartAtrribute)o).Index = (System.Int32)v;
        }


    }
}
