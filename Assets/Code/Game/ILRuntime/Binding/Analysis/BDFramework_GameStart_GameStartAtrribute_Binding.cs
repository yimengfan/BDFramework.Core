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
            app.RegisterCLRFieldBinding(field, CopyToStack_Index_0, AssignFromStack_Index_0);


        }



        static object get_Index_0(ref object o)
        {
            return ((BDFramework.GameStart.GameStartAtrribute)o).Index;
        }

        static StackObject* CopyToStack_Index_0(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((BDFramework.GameStart.GameStartAtrribute)o).Index;
            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static void set_Index_0(ref object o, object v)
        {
            ((BDFramework.GameStart.GameStartAtrribute)o).Index = (System.Int32)v;
        }

        static StackObject* AssignFromStack_Index_0(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            System.Int32 @Index = ptr_of_this_method->Value;
            ((BDFramework.GameStart.GameStartAtrribute)o).Index = @Index;
            return ptr_of_this_method;
        }



    }
}
