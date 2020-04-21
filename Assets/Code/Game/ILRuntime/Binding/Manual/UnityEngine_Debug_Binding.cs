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
using UnityEngine;

namespace ILRuntime.Runtime.Generated
{
    unsafe class UnityEngine_Debug_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[]     args;
            Type       type = typeof(UnityEngine.Debug);
            args = new Type[] { };

            args   = new Type[] {typeof(System.Object)};
            method = type.GetMethod("Log", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Log_11);
            args   = new Type[] {typeof(System.String), typeof(System.Object[])};
            method = type.GetMethod("LogFormat", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, LogFormat_13);
            args   = new Type[] {typeof(System.Object)};
            method = type.GetMethod("LogError", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, LogError_15);
            args   = new Type[] {typeof(System.String), typeof(System.Object[])};
            method = type.GetMethod("LogErrorFormat", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, LogErrorFormat_17);
            args   = new Type[] { };
            method = type.GetMethod("ClearDeveloperConsole", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ClearDeveloperConsole_19);
            args   = new Type[] {typeof(System.Object)};
            method = type.GetMethod("LogWarning", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, LogWarning_24);
            args   = new Type[] {typeof(System.String), typeof(System.Object[])};
            method = type.GetMethod("LogWarningFormat", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, LogWarningFormat_26);


            app.RegisterCLRCreateDefaultInstance(type, () => new UnityEngine.Debug());
            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.Debug[s]);

            args   = new Type[] { };
            method = type.GetConstructor(flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Ctor_0);
        }


        static StackObject* Log_11(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method,
                                   bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           ptr_of_this_method;
            StackObject*                           __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object @message =
                (System.Object) typeof(System.Object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain,
                                                                                         __mStack));
            __intp.Free(ptr_of_this_method);


            //通过ILRuntime的Debug接口获取调用热更DLL的堆栈
            string stackTrace = __domain.DebugService.GetStackTrace(__intp);
            Debug.Log(string.Format("{0}\n{1}", @message, stackTrace));

            return __ret;
        }

        static StackObject* LogFormat_13(ILIntepreter __intp,   StackObject* __esp, IList<object> __mStack,
                                         CLRMethod    __method, bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           ptr_of_this_method;
            StackObject*                           __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object[] @args =
                (System.Object[]) typeof(System.Object[]).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method,
                                                                                             __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.String @format =
                (System.String) typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain,
                                                                                         __mStack));
            __intp.Free(ptr_of_this_method);


            var content = string.Format(@format, @args);
            //通过ILRuntime的Debug接口获取调用热更DLL的堆栈
            string stackTrace = __domain.DebugService.GetStackTrace(__intp);
            Debug.Log(string.Format("{0}\n{1}", content, stackTrace));

            return __ret;
        }

        static StackObject* LogError_15(ILIntepreter __intp,   StackObject* __esp, IList<object> __mStack,
                                        CLRMethod    __method, bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           ptr_of_this_method;
            StackObject*                           __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object @message =
                (System.Object) typeof(System.Object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain,
                                                                                         __mStack));
            __intp.Free(ptr_of_this_method);


            //通过ILRuntime的Debug接口获取调用热更DLL的堆栈
            string stackTrace = __domain.DebugService.GetStackTrace(__intp);
            Debug.LogError(string.Format("{0}\n{1}", @message, stackTrace));

            return __ret;
        }

        static StackObject* LogErrorFormat_17(ILIntepreter __intp,   StackObject* __esp, IList<object> __mStack,
                                              CLRMethod    __method, bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           ptr_of_this_method;
            StackObject*                           __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object[] @args =
                (System.Object[]) typeof(System.Object[]).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method,
                                                                                             __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.String @format =
                (System.String) typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain,
                                                                                         __mStack));
            __intp.Free(ptr_of_this_method);


            //重定向打印堆栈
            var    content    = string.Format(@format, @args);
            string stackTrace = __domain.DebugService.GetStackTrace(__intp);
            Debug.LogErrorFormat(string.Format("{0}\n{1}", content, stackTrace));

            return __ret;
        }


        static StackObject* ClearDeveloperConsole_19(ILIntepreter __intp,   StackObject* __esp, IList<object> __mStack,
                                                     CLRMethod    __method, bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           __ret    = ILIntepreter.Minus(__esp, 0);


            UnityEngine.Debug.ClearDeveloperConsole();

            return __ret;
        }


        static StackObject* LogWarning_24(ILIntepreter __intp,   StackObject* __esp, IList<object> __mStack,
                                          CLRMethod    __method, bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           ptr_of_this_method;
            StackObject*                           __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object @message =
                (System.Object) typeof(System.Object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain,
                                                                                         __mStack));
            __intp.Free(ptr_of_this_method);


            //通过ILRuntime的Debug接口获取调用热更DLL的堆栈
            string stackTrace = __domain.DebugService.GetStackTrace(__intp);
            Debug.LogWarning(string.Format("{0}\n{1}", @message, stackTrace));

            return __ret;
        }


        static StackObject* LogWarningFormat_26(ILIntepreter __intp,   StackObject* __esp, IList<object> __mStack,
                                                CLRMethod    __method, bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           ptr_of_this_method;
            StackObject*                           __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object[] @args =
                (System.Object[]) typeof(System.Object[]).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method,
                                                                                             __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            System.String @format =
                (System.String) typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain,
                                                                                         __mStack));
            __intp.Free(ptr_of_this_method);


            //通过ILRuntime的Debug接口获取调用热更DLL的堆栈
            var    content    = string.Format(@format, @args);
            string stackTrace = __domain.DebugService.GetStackTrace(__intp);
            Debug.LogWarningFormat(string.Format("{0}\n{1}", content, stackTrace));

            return __ret;
        }


        static StackObject* Ctor_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method,
                                   bool         isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject*                           __ret    = ILIntepreter.Minus(__esp, 0);

            var result_of_this_method = new UnityEngine.Debug();

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }
    }
}