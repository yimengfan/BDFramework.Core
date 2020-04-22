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
    unsafe class BDFramework_GameConfig_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            FieldInfo field;
            Type[] args;
            Type type = typeof(BDFramework.GameConfig);

            field = type.GetField("IsExcuteHotfixUnitTest", flag);
            app.RegisterCLRFieldGetter(field, get_IsExcuteHotfixUnitTest_0);
            app.RegisterCLRFieldSetter(field, set_IsExcuteHotfixUnitTest_0);
            app.RegisterCLRFieldBinding(field, CopyToStack_IsExcuteHotfixUnitTest_0, AssignFromStack_IsExcuteHotfixUnitTest_0);


        }



        static object get_IsExcuteHotfixUnitTest_0(ref object o)
        {
            return ((BDFramework.GameConfig)o).IsExcuteHotfixUnitTest;
        }

        static StackObject* CopyToStack_IsExcuteHotfixUnitTest_0(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((BDFramework.GameConfig)o).IsExcuteHotfixUnitTest;
            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static void set_IsExcuteHotfixUnitTest_0(ref object o, object v)
        {
            ((BDFramework.GameConfig)o).IsExcuteHotfixUnitTest = (System.Boolean)v;
        }

        static StackObject* AssignFromStack_IsExcuteHotfixUnitTest_0(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            System.Boolean @IsExcuteHotfixUnitTest = ptr_of_this_method->Value == 1;
            ((BDFramework.GameConfig)o).IsExcuteHotfixUnitTest = @IsExcuteHotfixUnitTest;
            return ptr_of_this_method;
        }



    }
}
