
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
            Type type = typeof(global::UITool_Attribute);

            field = type.GetField("ToolTag_FieldName", flag);
            app.RegisterCLRFieldGetter(field, get_ToolTag_FieldName_0);
            app.RegisterCLRFieldSetter(field, set_ToolTag_FieldName_0);


        }



        static object get_ToolTag_FieldName_0(ref object o)
        {
            return ((global::UITool_Attribute)o).ToolTag_FieldName;
        }
        static void set_ToolTag_FieldName_0(ref object o, object v)
        {
            ((global::UITool_Attribute)o).ToolTag_FieldName = (System.String)v;
        }


    }
}
