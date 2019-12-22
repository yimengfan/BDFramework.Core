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
    unsafe class Game_ILRuntime_ILTypeHelper_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            FieldInfo field;
            Type[] args;
            Type type = typeof(Game.ILRuntime.ILTypeHelper);

            field = type.GetField("UIComponentTypes", flag);
            app.RegisterCLRFieldGetter(field, get_UIComponentTypes_0);
            app.RegisterCLRFieldSetter(field, set_UIComponentTypes_0);


        }



        static object get_UIComponentTypes_0(ref object o)
        {
            return Game.ILRuntime.ILTypeHelper.UIComponentTypes;
        }
        static void set_UIComponentTypes_0(ref object o, object v)
        {
            Game.ILRuntime.ILTypeHelper.UIComponentTypes = (System.Collections.Generic.Dictionary<System.String, System.Type>)v;
        }


    }
}
