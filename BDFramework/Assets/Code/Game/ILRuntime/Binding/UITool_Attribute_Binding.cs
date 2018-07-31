
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
    unsafe class UITool_Attribute_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            FieldInfo field;
            Type[] args;
            Type type = typeof(UITool_Attribute);

            field = type.GetField("AutoSetValueField", flag);
            app.RegisterCLRFieldGetter(field, get_AutoSetValueField_0);
            app.RegisterCLRFieldSetter(field, set_AutoSetValueField_0);


        }



        static object get_AutoSetValueField_0(ref object o)
        {
            return ((UITool_Attribute)o).ClassFieldName;
        }
        static void set_AutoSetValueField_0(ref object o, object v)
        {
            ((UITool_Attribute)o).ClassFieldName = (System.String)v;
        }


    }
}
