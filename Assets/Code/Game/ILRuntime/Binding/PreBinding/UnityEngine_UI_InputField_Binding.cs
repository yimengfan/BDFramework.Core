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
    unsafe class UnityEngine_UI_InputField_Binding
    {
        public static void Register(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodBase method;
            Type[] args;
            Type type = typeof(UnityEngine.UI.InputField);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_shouldHideMobileInput", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_shouldHideMobileInput_0);
            args = new Type[]{};
            method = type.GetMethod("get_shouldHideMobileInput", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_shouldHideMobileInput_1);
            args = new Type[]{};
            method = type.GetMethod("get_text", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_text_2);
            args = new Type[]{typeof(System.String)};
            method = type.GetMethod("set_text", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_text_3);
            args = new Type[]{};
            method = type.GetMethod("get_isFocused", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_isFocused_4);
            args = new Type[]{};
            method = type.GetMethod("get_caretBlinkRate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_caretBlinkRate_5);
            args = new Type[]{typeof(System.Single)};
            method = type.GetMethod("set_caretBlinkRate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_caretBlinkRate_6);
            args = new Type[]{};
            method = type.GetMethod("get_caretWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_caretWidth_7);
            args = new Type[]{typeof(System.Int32)};
            method = type.GetMethod("set_caretWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_caretWidth_8);
            args = new Type[]{};
            method = type.GetMethod("get_textComponent", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_textComponent_9);
            args = new Type[]{typeof(UnityEngine.UI.Text)};
            method = type.GetMethod("set_textComponent", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_textComponent_10);
            args = new Type[]{};
            method = type.GetMethod("get_placeholder", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_placeholder_11);
            args = new Type[]{typeof(UnityEngine.UI.Graphic)};
            method = type.GetMethod("set_placeholder", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_placeholder_12);
            args = new Type[]{};
            method = type.GetMethod("get_caretColor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_caretColor_13);
            args = new Type[]{typeof(UnityEngine.Color)};
            method = type.GetMethod("set_caretColor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_caretColor_14);
            args = new Type[]{};
            method = type.GetMethod("get_customCaretColor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_customCaretColor_15);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_customCaretColor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_customCaretColor_16);
            args = new Type[]{};
            method = type.GetMethod("get_selectionColor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_selectionColor_17);
            args = new Type[]{typeof(UnityEngine.Color)};
            method = type.GetMethod("set_selectionColor", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_selectionColor_18);
            args = new Type[]{};
            method = type.GetMethod("get_onEndEdit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_onEndEdit_19);
            args = new Type[]{typeof(UnityEngine.UI.InputField.SubmitEvent)};
            method = type.GetMethod("set_onEndEdit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_onEndEdit_20);
            args = new Type[]{};
            method = type.GetMethod("get_onValueChanged", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_onValueChanged_21);
            args = new Type[]{typeof(UnityEngine.UI.InputField.OnChangeEvent)};
            method = type.GetMethod("set_onValueChanged", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_onValueChanged_22);
            args = new Type[]{};
            method = type.GetMethod("get_onValidateInput", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_onValidateInput_23);
            args = new Type[]{typeof(UnityEngine.UI.InputField.OnValidateInput)};
            method = type.GetMethod("set_onValidateInput", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_onValidateInput_24);
            args = new Type[]{};
            method = type.GetMethod("get_characterLimit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_characterLimit_25);
            args = new Type[]{typeof(System.Int32)};
            method = type.GetMethod("set_characterLimit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_characterLimit_26);
            args = new Type[]{};
            method = type.GetMethod("get_contentType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_contentType_27);
            args = new Type[]{typeof(UnityEngine.UI.InputField.ContentType)};
            method = type.GetMethod("set_contentType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_contentType_28);
            args = new Type[]{};
            method = type.GetMethod("get_lineType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_lineType_29);
            args = new Type[]{typeof(UnityEngine.UI.InputField.LineType)};
            method = type.GetMethod("set_lineType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_lineType_30);
            args = new Type[]{};
            method = type.GetMethod("get_inputType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_inputType_31);
            args = new Type[]{typeof(UnityEngine.UI.InputField.InputType)};
            method = type.GetMethod("set_inputType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_inputType_32);
            args = new Type[]{};
            method = type.GetMethod("get_touchScreenKeyboard", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_touchScreenKeyboard_33);
            args = new Type[]{};
            method = type.GetMethod("get_keyboardType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_keyboardType_34);
            args = new Type[]{typeof(UnityEngine.TouchScreenKeyboardType)};
            method = type.GetMethod("set_keyboardType", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_keyboardType_35);
            args = new Type[]{};
            method = type.GetMethod("get_characterValidation", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_characterValidation_36);
            args = new Type[]{typeof(UnityEngine.UI.InputField.CharacterValidation)};
            method = type.GetMethod("set_characterValidation", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_characterValidation_37);
            args = new Type[]{};
            method = type.GetMethod("get_readOnly", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_readOnly_38);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("set_readOnly", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_readOnly_39);
            args = new Type[]{};
            method = type.GetMethod("get_multiLine", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_multiLine_40);
            args = new Type[]{};
            method = type.GetMethod("get_asteriskChar", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_asteriskChar_41);
            args = new Type[]{typeof(System.Char)};
            method = type.GetMethod("set_asteriskChar", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_asteriskChar_42);
            args = new Type[]{};
            method = type.GetMethod("get_wasCanceled", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_wasCanceled_43);
            args = new Type[]{};
            method = type.GetMethod("get_caretPosition", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_caretPosition_44);
            args = new Type[]{typeof(System.Int32)};
            method = type.GetMethod("set_caretPosition", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_caretPosition_45);
            args = new Type[]{};
            method = type.GetMethod("get_selectionAnchorPosition", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_selectionAnchorPosition_46);
            args = new Type[]{typeof(System.Int32)};
            method = type.GetMethod("set_selectionAnchorPosition", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_selectionAnchorPosition_47);
            args = new Type[]{};
            method = type.GetMethod("get_selectionFocusPosition", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_selectionFocusPosition_48);
            args = new Type[]{typeof(System.Int32)};
            method = type.GetMethod("set_selectionFocusPosition", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, set_selectionFocusPosition_49);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("MoveTextEnd", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, MoveTextEnd_50);
            args = new Type[]{typeof(System.Boolean)};
            method = type.GetMethod("MoveTextStart", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, MoveTextStart_51);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData)};
            method = type.GetMethod("OnBeginDrag", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnBeginDrag_52);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData)};
            method = type.GetMethod("OnDrag", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnDrag_53);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData)};
            method = type.GetMethod("OnEndDrag", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnEndDrag_54);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData)};
            method = type.GetMethod("OnPointerDown", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnPointerDown_55);
            args = new Type[]{typeof(UnityEngine.Event)};
            method = type.GetMethod("ProcessEvent", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ProcessEvent_56);
            args = new Type[]{typeof(UnityEngine.EventSystems.BaseEventData)};
            method = type.GetMethod("OnUpdateSelected", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnUpdateSelected_57);
            args = new Type[]{};
            method = type.GetMethod("ForceLabelUpdate", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ForceLabelUpdate_58);
            args = new Type[]{typeof(UnityEngine.UI.CanvasUpdate)};
            method = type.GetMethod("Rebuild", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, Rebuild_59);
            args = new Type[]{};
            method = type.GetMethod("LayoutComplete", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, LayoutComplete_60);
            args = new Type[]{};
            method = type.GetMethod("GraphicUpdateComplete", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, GraphicUpdateComplete_61);
            args = new Type[]{};
            method = type.GetMethod("ActivateInputField", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, ActivateInputField_62);
            args = new Type[]{typeof(UnityEngine.EventSystems.BaseEventData)};
            method = type.GetMethod("OnSelect", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnSelect_63);
            args = new Type[]{typeof(UnityEngine.EventSystems.PointerEventData)};
            method = type.GetMethod("OnPointerClick", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnPointerClick_64);
            args = new Type[]{};
            method = type.GetMethod("DeactivateInputField", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, DeactivateInputField_65);
            args = new Type[]{typeof(UnityEngine.EventSystems.BaseEventData)};
            method = type.GetMethod("OnDeselect", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnDeselect_66);
            args = new Type[]{typeof(UnityEngine.EventSystems.BaseEventData)};
            method = type.GetMethod("OnSubmit", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, OnSubmit_67);
            args = new Type[]{};
            method = type.GetMethod("CalculateLayoutInputHorizontal", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CalculateLayoutInputHorizontal_68);
            args = new Type[]{};
            method = type.GetMethod("CalculateLayoutInputVertical", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, CalculateLayoutInputVertical_69);
            args = new Type[]{};
            method = type.GetMethod("get_minWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_minWidth_70);
            args = new Type[]{};
            method = type.GetMethod("get_preferredWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_preferredWidth_71);
            args = new Type[]{};
            method = type.GetMethod("get_flexibleWidth", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_flexibleWidth_72);
            args = new Type[]{};
            method = type.GetMethod("get_minHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_minHeight_73);
            args = new Type[]{};
            method = type.GetMethod("get_preferredHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_preferredHeight_74);
            args = new Type[]{};
            method = type.GetMethod("get_flexibleHeight", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_flexibleHeight_75);
            args = new Type[]{};
            method = type.GetMethod("get_layoutPriority", flag, null, args, null);
            app.RegisterCLRMethodRedirection(method, get_layoutPriority_76);



            app.RegisterCLRCreateArrayInstance(type, s => new UnityEngine.UI.InputField[s]);


        }


        static StackObject* set_shouldHideMobileInput_0(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.shouldHideMobileInput = value;

            return __ret;
        }

        static StackObject* get_shouldHideMobileInput_1(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.shouldHideMobileInput;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* get_text_2(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.text;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_text_3(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.String @value = (System.String)typeof(System.String).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.text = value;

            return __ret;
        }

        static StackObject* get_isFocused_4(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.isFocused;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* get_caretBlinkRate_5(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.caretBlinkRate;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_caretBlinkRate_6(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Single @value = *(float*)&ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.caretBlinkRate = value;

            return __ret;
        }

        static StackObject* get_caretWidth_7(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.caretWidth;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_caretWidth_8(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Int32 @value = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.caretWidth = value;

            return __ret;
        }

        static StackObject* get_textComponent_9(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.textComponent;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_textComponent_10(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Text @value = (UnityEngine.UI.Text)typeof(UnityEngine.UI.Text).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.textComponent = value;

            return __ret;
        }

        static StackObject* get_placeholder_11(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.placeholder;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_placeholder_12(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.Graphic @value = (UnityEngine.UI.Graphic)typeof(UnityEngine.UI.Graphic).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.placeholder = value;

            return __ret;
        }

        static StackObject* get_caretColor_13(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.caretColor;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_caretColor_14(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.Color @value = (UnityEngine.Color)typeof(UnityEngine.Color).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.caretColor = value;

            return __ret;
        }

        static StackObject* get_customCaretColor_15(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.customCaretColor;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_customCaretColor_16(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.customCaretColor = value;

            return __ret;
        }

        static StackObject* get_selectionColor_17(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.selectionColor;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_selectionColor_18(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.Color @value = (UnityEngine.Color)typeof(UnityEngine.Color).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.selectionColor = value;

            return __ret;
        }

        static StackObject* get_onEndEdit_19(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.onEndEdit;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_onEndEdit_20(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField.SubmitEvent @value = (UnityEngine.UI.InputField.SubmitEvent)typeof(UnityEngine.UI.InputField.SubmitEvent).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.onEndEdit = value;

            return __ret;
        }

        static StackObject* get_onValueChanged_21(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.onValueChanged;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_onValueChanged_22(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField.OnChangeEvent @value = (UnityEngine.UI.InputField.OnChangeEvent)typeof(UnityEngine.UI.InputField.OnChangeEvent).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.onValueChanged = value;

            return __ret;
        }

        static StackObject* get_onValidateInput_23(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.onValidateInput;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_onValidateInput_24(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField.OnValidateInput @value = (UnityEngine.UI.InputField.OnValidateInput)typeof(UnityEngine.UI.InputField.OnValidateInput).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.onValidateInput = value;

            return __ret;
        }

        static StackObject* get_characterLimit_25(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.characterLimit;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_characterLimit_26(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Int32 @value = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.characterLimit = value;

            return __ret;
        }

        static StackObject* get_contentType_27(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.contentType;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_contentType_28(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField.ContentType @value = (UnityEngine.UI.InputField.ContentType)typeof(UnityEngine.UI.InputField.ContentType).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.contentType = value;

            return __ret;
        }

        static StackObject* get_lineType_29(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.lineType;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_lineType_30(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField.LineType @value = (UnityEngine.UI.InputField.LineType)typeof(UnityEngine.UI.InputField.LineType).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.lineType = value;

            return __ret;
        }

        static StackObject* get_inputType_31(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.inputType;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_inputType_32(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField.InputType @value = (UnityEngine.UI.InputField.InputType)typeof(UnityEngine.UI.InputField.InputType).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.inputType = value;

            return __ret;
        }

        static StackObject* get_touchScreenKeyboard_33(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.touchScreenKeyboard;

            object obj_result_of_this_method = result_of_this_method;
            if(obj_result_of_this_method is CrossBindingAdaptorType)
            {    
                return ILIntepreter.PushObject(__ret, __mStack, ((CrossBindingAdaptorType)obj_result_of_this_method).ILInstance);
            }
            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* get_keyboardType_34(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.keyboardType;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_keyboardType_35(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.TouchScreenKeyboardType @value = (UnityEngine.TouchScreenKeyboardType)typeof(UnityEngine.TouchScreenKeyboardType).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.keyboardType = value;

            return __ret;
        }

        static StackObject* get_characterValidation_36(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.characterValidation;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }

        static StackObject* set_characterValidation_37(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField.CharacterValidation @value = (UnityEngine.UI.InputField.CharacterValidation)typeof(UnityEngine.UI.InputField.CharacterValidation).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.characterValidation = value;

            return __ret;
        }

        static StackObject* get_readOnly_38(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.readOnly;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* set_readOnly_39(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @value = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.readOnly = value;

            return __ret;
        }

        static StackObject* get_multiLine_40(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.multiLine;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* get_asteriskChar_41(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.asteriskChar;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = (int)result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_asteriskChar_42(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Char @value = (char)ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.asteriskChar = value;

            return __ret;
        }

        static StackObject* get_wasCanceled_43(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.wasCanceled;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method ? 1 : 0;
            return __ret + 1;
        }

        static StackObject* get_caretPosition_44(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.caretPosition;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_caretPosition_45(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Int32 @value = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.caretPosition = value;

            return __ret;
        }

        static StackObject* get_selectionAnchorPosition_46(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.selectionAnchorPosition;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_selectionAnchorPosition_47(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Int32 @value = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.selectionAnchorPosition = value;

            return __ret;
        }

        static StackObject* get_selectionFocusPosition_48(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.selectionFocusPosition;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* set_selectionFocusPosition_49(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Int32 @value = ptr_of_this_method->Value;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.selectionFocusPosition = value;

            return __ret;
        }

        static StackObject* MoveTextEnd_50(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @shift = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.MoveTextEnd(@shift);

            return __ret;
        }

        static StackObject* MoveTextStart_51(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Boolean @shift = ptr_of_this_method->Value == 1;

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.MoveTextStart(@shift);

            return __ret;
        }

        static StackObject* OnBeginDrag_52(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PointerEventData @eventData = (UnityEngine.EventSystems.PointerEventData)typeof(UnityEngine.EventSystems.PointerEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnBeginDrag(@eventData);

            return __ret;
        }

        static StackObject* OnDrag_53(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PointerEventData @eventData = (UnityEngine.EventSystems.PointerEventData)typeof(UnityEngine.EventSystems.PointerEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnDrag(@eventData);

            return __ret;
        }

        static StackObject* OnEndDrag_54(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PointerEventData @eventData = (UnityEngine.EventSystems.PointerEventData)typeof(UnityEngine.EventSystems.PointerEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnEndDrag(@eventData);

            return __ret;
        }

        static StackObject* OnPointerDown_55(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PointerEventData @eventData = (UnityEngine.EventSystems.PointerEventData)typeof(UnityEngine.EventSystems.PointerEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnPointerDown(@eventData);

            return __ret;
        }

        static StackObject* ProcessEvent_56(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.Event @e = (UnityEngine.Event)typeof(UnityEngine.Event).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.ProcessEvent(@e);

            return __ret;
        }

        static StackObject* OnUpdateSelected_57(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.BaseEventData @eventData = (UnityEngine.EventSystems.BaseEventData)typeof(UnityEngine.EventSystems.BaseEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnUpdateSelected(@eventData);

            return __ret;
        }

        static StackObject* ForceLabelUpdate_58(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.ForceLabelUpdate();

            return __ret;
        }

        static StackObject* Rebuild_59(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.CanvasUpdate @update = (UnityEngine.UI.CanvasUpdate)typeof(UnityEngine.UI.CanvasUpdate).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.Rebuild(@update);

            return __ret;
        }

        static StackObject* LayoutComplete_60(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.LayoutComplete();

            return __ret;
        }

        static StackObject* GraphicUpdateComplete_61(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.GraphicUpdateComplete();

            return __ret;
        }

        static StackObject* ActivateInputField_62(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.ActivateInputField();

            return __ret;
        }

        static StackObject* OnSelect_63(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.BaseEventData @eventData = (UnityEngine.EventSystems.BaseEventData)typeof(UnityEngine.EventSystems.BaseEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnSelect(@eventData);

            return __ret;
        }

        static StackObject* OnPointerClick_64(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.PointerEventData @eventData = (UnityEngine.EventSystems.PointerEventData)typeof(UnityEngine.EventSystems.PointerEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnPointerClick(@eventData);

            return __ret;
        }

        static StackObject* DeactivateInputField_65(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.DeactivateInputField();

            return __ret;
        }

        static StackObject* OnDeselect_66(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.BaseEventData @eventData = (UnityEngine.EventSystems.BaseEventData)typeof(UnityEngine.EventSystems.BaseEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnDeselect(@eventData);

            return __ret;
        }

        static StackObject* OnSubmit_67(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 2);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.EventSystems.BaseEventData @eventData = (UnityEngine.EventSystems.BaseEventData)typeof(UnityEngine.EventSystems.BaseEventData).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 2);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.OnSubmit(@eventData);

            return __ret;
        }

        static StackObject* CalculateLayoutInputHorizontal_68(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.CalculateLayoutInputHorizontal();

            return __ret;
        }

        static StackObject* CalculateLayoutInputVertical_69(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            instance_of_this_method.CalculateLayoutInputVertical();

            return __ret;
        }

        static StackObject* get_minWidth_70(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.minWidth;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_preferredWidth_71(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.preferredWidth;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_flexibleWidth_72(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.flexibleWidth;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_minHeight_73(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.minHeight;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_preferredHeight_74(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.preferredHeight;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_flexibleHeight_75(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.flexibleHeight;

            __ret->ObjectType = ObjectTypes.Float;
            *(float*)&__ret->Value = result_of_this_method;
            return __ret + 1;
        }

        static StackObject* get_layoutPriority_76(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            ILRuntime.Runtime.Enviorment.AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.UI.InputField instance_of_this_method = (UnityEngine.UI.InputField)typeof(UnityEngine.UI.InputField).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            var result_of_this_method = instance_of_this_method.layoutPriority;

            __ret->ObjectType = ObjectTypes.Integer;
            __ret->Value = result_of_this_method;
            return __ret + 1;
        }





    }
}
