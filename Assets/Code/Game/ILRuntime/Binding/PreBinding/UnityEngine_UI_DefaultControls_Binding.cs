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
    unsafe class UnityEngine_UI_DefaultControls_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.DefaultControls);
            args = new Type[]{};
            method = type.GetMethod("get_factory", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_factory_0);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.IFactoryControls)};
            method = type.GetMethod("set_factory", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_factory_1);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreatePanel", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreatePanel_2);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateButton", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateButton_3);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateText", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateText_4);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateImage", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateImage_5);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateRawImage", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateRawImage_6);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateSlider", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateSlider_7);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateScrollbar", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateScrollbar_8);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateToggle", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateToggle_9);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateInputField", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateInputField_10);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateDropdown", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateDropdown_11);
            args = new Type[]{typeof(UnityEngine.UI.DefaultControls.Resources)};
            method = type.GetMethod("CreateScrollView", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CreateScrollView_12);





        }


        static StackObject* get_factory_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.UI.DefaultControls.factory;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_factory_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.IFactoryControls @value = (UnityEngine.UI.DefaultControls.IFactoryControls)typeof(UnityEngine.UI.DefaultControls.IFactoryControls).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            UnityEngine.UI.DefaultControls.factory = value;

            return __ret;
        }

        static StackObject* CreatePanel_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreatePanel(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateButton_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateButton(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateText_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateText(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateImage_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateImage(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateRawImage_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateRawImage(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateSlider_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateSlider(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateScrollbar_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateScrollbar(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateToggle_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateToggle(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateInputField_10(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateInputField(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateDropdown_11(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateDropdown(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* CreateScrollView_12(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.DefaultControls.Resources @resources = (UnityEngine.UI.DefaultControls.Resources)typeof(UnityEngine.UI.DefaultControls.Resources).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);


            var result_of_this_method = UnityEngine.UI.DefaultControls.CreateScrollView(@resources);

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }





    }
}
