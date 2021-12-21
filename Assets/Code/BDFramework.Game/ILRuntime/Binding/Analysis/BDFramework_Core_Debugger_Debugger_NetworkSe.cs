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
    unsafe class BDFramework_Core_Debugger_Debugger_NetworkServer_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(BDFramework.Core.Debugger.Debugger_NetworkServer);
            args = new Type[]{typeof(System.Int32), typeof(BDFramework.Core.Debugger.Debugger_NetworkServer.OnReceiveMsg)};
            method = type.GetMethod("AddLogicProcess", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, AddLogicProcess_0);


        }


        static StackObject* AddLogicProcess_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            BDFramework.Core.Debugger.Debugger_NetworkServer.OnReceiveMsg @callback = (BDFramework.Core.Debugger.Debugger_NetworkServer.OnReceiveMsg)typeof(BDFramework.Core.Debugger.Debugger_NetworkServer.OnReceiveMsg).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack), (CLR.Utils.Extensions.TypeFlags)8);
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.Int32 @id = ptr_of_this_method->Value;


            BDFramework.Core.Debugger.Debugger_NetworkServer.AddLogicProcess(@id, @callback);

            return __ret;
        }



    }
}
