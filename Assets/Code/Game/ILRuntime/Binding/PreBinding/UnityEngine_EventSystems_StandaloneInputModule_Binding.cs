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
    unsafe class UnityEngine_EventSystems_StandaloneInputModule_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.EventSystems.StandaloneInputModule);
            args = new Type[]{};
            method = type.GetMethod("get_forceModuleActive", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_forceModuleActive_0);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_forceModuleActive", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_forceModuleActive_1);
            args = new Type[]{};
            method = type.GetMethod("get_inputActionsPerSecond", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_inputActionsPerSecond_2);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_inputActionsPerSecond", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_inputActionsPerSecond_3);
            args = new Type[]{};
            method = type.GetMethod("get_repeatDelay", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_repeatDelay_4);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_repeatDelay", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_repeatDelay_5);
            args = new Type[]{};
            method = type.GetMethod("get_horizontalAxis", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_horizontalAxis_6);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_horizontalAxis", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_horizontalAxis_7);
            args = new Type[]{};
            method = type.GetMethod("get_verticalAxis", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_verticalAxis_8);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_verticalAxis", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_verticalAxis_9);
            args = new Type[]{};
            method = type.GetMethod("get_submitButton", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_submitButton_10);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_submitButton", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_submitButton_11);
            args = new Type[]{};
            method = type.GetMethod("get_cancelButton", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_cancelButton_12);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_cancelButton", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_cancelButton_13);
            args = new Type[]{};
            method = type.GetMethod("UpdateModule", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, UpdateModule_14);
            args = new Type[]{};
            method = type.GetMethod("IsModuleSupported", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, IsModuleSupported_15);
            args = new Type[]{};
            method = type.GetMethod("ShouldActivateModule", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ShouldActivateModule_16);
            args = new Type[]{};
            method = type.GetMethod("ActivateModule", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ActivateModule_17);
            args = new Type[]{};
            method = type.GetMethod("DeactivateModule", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, DeactivateModule_18);
            args = new Type[]{};
            method = type.GetMethod("Process", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Process_19);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.EventSystems.StandaloneInputModule[s]);


        }


        static StackObject* get_forceModuleActive_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.forceModuleActive;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_forceModuleActive_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.forceModuleActive = value;

            return __ret;
        }

        static StackObject* get_inputActionsPerSecond_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.inputActionsPerSecond;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_inputActionsPerSecond_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.inputActionsPerSecond = value;

            return __ret;
        }

        static StackObject* get_repeatDelay_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.repeatDelay;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_repeatDelay_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.repeatDelay = value;

            return __ret;
        }

        static StackObject* get_horizontalAxis_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.horizontalAxis;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_horizontalAxis_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.horizontalAxis = value;

            return __ret;
        }

        static StackObject* get_verticalAxis_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.verticalAxis;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_verticalAxis_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.verticalAxis = value;

            return __ret;
        }

        static StackObject* get_submitButton_10(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.submitButton;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_submitButton_11(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.submitButton = value;

            return __ret;
        }

        static StackObject* get_cancelButton_12(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.cancelButton;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_cancelButton_13(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.cancelButton = value;

            return __ret;
        }

        static StackObject* UpdateModule_14(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.UpdateModule();

            return __ret;
        }

        static StackObject* IsModuleSupported_15(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.IsModuleSupported();

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* ShouldActivateModule_16(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.ShouldActivateModule();

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* ActivateModule_17(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.ActivateModule();

            return __ret;
        }

        static StackObject* DeactivateModule_18(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.DeactivateModule();

            return __ret;
        }

        static StackObject* Process_19(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.StandaloneInputModule instance_of_this_method = (UnityEngine.EventSystems.StandaloneInputModule)typeof(UnityEngine.EventSystems.StandaloneInputModule).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Process();

            return __ret;
        }





    }
}
