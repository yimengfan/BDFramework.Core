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
    unsafe class UnityEngine_EventSystems_ExecuteEvents_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.EventSystems.ExecuteEvents);
            args = new Type[]{};
            method = type.GetMethod("get_pointerEnterHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_pointerEnterHandler_0);
            args = new Type[]{};
            method = type.GetMethod("get_pointerExitHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_pointerExitHandler_1);
            args = new Type[]{};
            method = type.GetMethod("get_pointerDownHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_pointerDownHandler_2);
            args = new Type[]{};
            method = type.GetMethod("get_pointerUpHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_pointerUpHandler_3);
            args = new Type[]{};
            method = type.GetMethod("get_pointerClickHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_pointerClickHandler_4);
            args = new Type[]{};
            method = type.GetMethod("get_initializePotentialDrag", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_initializePotentialDrag_5);
            args = new Type[]{};
            method = type.GetMethod("get_beginDragHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_beginDragHandler_6);
            args = new Type[]{};
            method = type.GetMethod("get_dragHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_dragHandler_7);
            args = new Type[]{};
            method = type.GetMethod("get_endDragHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_endDragHandler_8);
            args = new Type[]{};
            method = type.GetMethod("get_dropHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_dropHandler_9);
            args = new Type[]{};
            method = type.GetMethod("get_scrollHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_scrollHandler_10);
            args = new Type[]{};
            method = type.GetMethod("get_updateSelectedHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_updateSelectedHandler_11);
            args = new Type[]{};
            method = type.GetMethod("get_selectHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_selectHandler_12);
            args = new Type[]{};
            method = type.GetMethod("get_deselectHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_deselectHandler_13);
            args = new Type[]{};
            method = type.GetMethod("get_moveHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_moveHandler_14);
            args = new Type[]{};
            method = type.GetMethod("get_submitHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_submitHandler_15);
            args = new Type[]{};
            method = type.GetMethod("get_cancelHandler", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_cancelHandler_16);





        }


        static StackObject* get_pointerEnterHandler_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_pointerExitHandler_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_pointerDownHandler_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_pointerUpHandler_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_pointerClickHandler_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_initializePotentialDrag_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.initializePotentialDrag;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_beginDragHandler_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.beginDragHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_dragHandler_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.dragHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_endDragHandler_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.endDragHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_dropHandler_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.dropHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_scrollHandler_10(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.scrollHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_updateSelectedHandler_11(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.updateSelectedHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_selectHandler_12(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.selectHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_deselectHandler_13(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.deselectHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_moveHandler_14(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.moveHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_submitHandler_15(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.submitHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_cancelHandler_16(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* __ret = ILIntepreter.Minus(__esp, 0);


            var result_of_this_method = UnityEngine.EventSystems.ExecuteEvents.cancelHandler;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }





    }
}
