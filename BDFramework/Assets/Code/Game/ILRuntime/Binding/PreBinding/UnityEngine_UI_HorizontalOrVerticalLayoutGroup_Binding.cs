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
    unsafe class UnityEngine_UI_HorizontalOrVerticalLayoutGroup_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup);
            args = new Type[]{};
            method = type.GetMethod("get_spacing", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_spacing_0);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_spacing", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_spacing_1);
            args = new Type[]{};
            method = type.GetMethod("get_childForceExpandWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_childForceExpandWidth_2);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_childForceExpandWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_childForceExpandWidth_3);
            args = new Type[]{};
            method = type.GetMethod("get_childForceExpandHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_childForceExpandHeight_4);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_childForceExpandHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_childForceExpandHeight_5);
            args = new Type[]{};
            method = type.GetMethod("get_childControlWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_childControlWidth_6);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_childControlWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_childControlWidth_7);
            args = new Type[]{};
            method = type.GetMethod("get_childControlHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_childControlHeight_8);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_childControlHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_childControlHeight_9);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.HorizontalOrVerticalLayoutGroup[s]);


        }


        static StackObject* get_spacing_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.spacing;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_spacing_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.spacing = value;

            return __ret;
        }

        static StackObject* get_childForceExpandWidth_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.childForceExpandWidth;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_childForceExpandWidth_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.childForceExpandWidth = value;

            return __ret;
        }

        static StackObject* get_childForceExpandHeight_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.childForceExpandHeight;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_childForceExpandHeight_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.childForceExpandHeight = value;

            return __ret;
        }

        static StackObject* get_childControlWidth_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.childControlWidth;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_childControlWidth_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.childControlWidth = value;

            return __ret;
        }

        static StackObject* get_childControlHeight_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.childControlHeight;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_childControlHeight_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.HorizontalOrVerticalLayoutGroup instance_of_this_method = (UnityEngine.UI.HorizontalOrVerticalLayoutGroup)typeof(UnityEngine.UI.HorizontalOrVerticalLayoutGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.childControlHeight = value;

            return __ret;
        }





    }
}
