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
    unsafe class UnityEngine_UI_Toggle_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            FieldInfo field;
            Type[] args;
            Type type = typeof(UnityEngine.UI.Toggle);
            args = new Type[]{};
            method = type.GetMethod("get_group", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_group_0);
            args = new Type[]{typeof(UnityEngine.UI.ToggleGroup)};
            method = type.GetMethod("set_group", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_group_1);
            args = new Type[]{typeof(UnityEngine.UI.CanvasUpdate)};
            method = type.GetMethod("Rebuild", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Rebuild_2);
            args = new Type[]{};
            method = type.GetMethod("LayoutComplete", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, LayoutComplete_3);
            args = new Type[]{};
            method = type.GetMethod("GraphicUpdateComplete", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, GraphicUpdateComplete_4);
            args = new Type[]{};
            method = type.GetMethod("get_isOn", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_isOn_5);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_isOn", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_isOn_6);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData)};
            method = type.GetMethod("OnPointerClick", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnPointerClick_7);
            args = new Type[]{typeof(UnityEngine.EventSystems.BaseEventData)};
            method = type.GetMethod("OnSubmit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnSubmit_8);

            field = type.GetField("toggleTransition", flag);
            app.RegisterCLRFieldGetter(field, get_toggleTransition_0);
            app.RegisterCLRFieldSetter(field, set_toggleTransition_0);
            app.RegisterCLRFieldBinding(field, CopyToStack_toggleTransition_0, AssignFromStack_toggleTransition_0);
            field = type.GetField("graphic", flag);
            app.RegisterCLRFieldGetter(field, get_graphic_1);
            app.RegisterCLRFieldSetter(field, set_graphic_1);
            app.RegisterCLRFieldBinding(field, CopyToStack_graphic_1, AssignFromStack_graphic_1);
            field = type.GetField("onValueChanged", flag);
            app.RegisterCLRFieldGetter(field, get_onValueChanged_2);
            app.RegisterCLRFieldSetter(field, set_onValueChanged_2);
            app.RegisterCLRFieldBinding(field, CopyToStack_onValueChanged_2, AssignFromStack_onValueChanged_2);


            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.Toggle[s]);


        }


        static StackObject* get_group_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.group;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_group_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.ToggleGroup @value = (UnityEngine.UI.ToggleGroup)typeof(UnityEngine.UI.ToggleGroup).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.group = value;

            return __ret;
        }

        static StackObject* Rebuild_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasUpdate @executing = (UnityEngine.UI.CanvasUpdate)typeof(UnityEngine.UI.CanvasUpdate).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Rebuild(@executing);

            return __ret;
        }

        static StackObject* LayoutComplete_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.LayoutComplete();

            return __ret;
        }

        static StackObject* GraphicUpdateComplete_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.GraphicUpdateComplete();

            return __ret;
        }

        static StackObject* get_isOn_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.isOn;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_isOn_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.isOn = value;

            return __ret;
        }

        static StackObject* OnPointerClick_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PointerEventData @eventData = (UnityEngine.EventSystems.PointerEventData)typeof(UnityEngine.EventSystems.PointerEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnPointerClick(@eventData);

            return __ret;
        }

        static StackObject* OnSubmit_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.BaseEventData @eventData = (UnityEngine.EventSystems.BaseEventData)typeof(UnityEngine.EventSystems.BaseEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.Toggle instance_of_this_method = (UnityEngine.UI.Toggle)typeof(UnityEngine.UI.Toggle).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnSubmit(@eventData);

            return __ret;
        }


        static object get_toggleTransition_0(ref object o)
        {
            return ((UnityEngine.UI.Toggle)o).toggleTransition;
        }

        static StackObject* CopyToStack_toggleTransition_0(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((UnityEngine.UI.Toggle)o).toggleTransition;
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static void set_toggleTransition_0(ref object o, object v)
        {
            ((UnityEngine.UI.Toggle)o).toggleTransition = (UnityEngine.UI.Toggle.ToggleTransition)v;
        }

        static StackObject* AssignFromStack_toggleTransition_0(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            UnityEngine.UI.Toggle.ToggleTransition @toggleTransition = (UnityEngine.UI.Toggle.ToggleTransition)typeof(UnityEngine.UI.Toggle.ToggleTransition).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            ((UnityEngine.UI.Toggle)o).toggleTransition = @toggleTransition;
            return ptr_of_this_method;
        }

        static object get_graphic_1(ref object o)
        {
            return ((UnityEngine.UI.Toggle)o).graphic;
        }

        static StackObject* CopyToStack_graphic_1(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((UnityEngine.UI.Toggle)o).graphic;
            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static void set_graphic_1(ref object o, object v)
        {
            ((UnityEngine.UI.Toggle)o).graphic = (UnityEngine.UI.Graphic)v;
        }

        static StackObject* AssignFromStack_graphic_1(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            UnityEngine.UI.Graphic @graphic = (UnityEngine.UI.Graphic)typeof(UnityEngine.UI.Graphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            ((UnityEngine.UI.Toggle)o).graphic = @graphic;
            return ptr_of_this_method;
        }

        static object get_onValueChanged_2(ref object o)
        {
            return ((UnityEngine.UI.Toggle)o).onValueChanged;
        }

        static StackObject* CopyToStack_onValueChanged_2(ref object o, ILIntepreter __intp, StackObject* __ret, IList<object> __mStack)
        {
            var result_of_this_method = ((UnityEngine.UI.Toggle)o).onValueChanged;
            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static void set_onValueChanged_2(ref object o, object v)
        {
            ((UnityEngine.UI.Toggle)o).onValueChanged = (UnityEngine.UI.Toggle.ToggleEvent)v;
        }

        static StackObject* AssignFromStack_onValueChanged_2(ref object o, ILIntepreter __intp, StackObject* ptr_of_this_method, IList<object> __mStack)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            UnityEngine.UI.Toggle.ToggleEvent @onValueChanged = (UnityEngine.UI.Toggle.ToggleEvent)typeof(UnityEngine.UI.Toggle.ToggleEvent).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            ((UnityEngine.UI.Toggle)o).onValueChanged = @onValueChanged;
            return ptr_of_this_method;
        }




    }
}
