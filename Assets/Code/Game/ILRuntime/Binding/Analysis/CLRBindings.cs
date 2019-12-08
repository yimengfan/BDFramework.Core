using System;
using System.Collections.Generic;
using System.Reflection;

namespace ILRuntime.Runtime.Generated
{
    class CLRBindings
    {

        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector2> s_UnityEngine_Vector2_Binding_Binder = null;
        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector3> s_UnityEngine_Vector3_Binding_Binder = null;
        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector4> s_UnityEngine_Vector4_Binding_Binder = null;
        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Quaternion> s_UnityEngine_Quaternion_Binding_Binder = null;

        /// <summary>
        /// Initialize the CLR binding, please invoke this AFTER CLR Redirection registration
        /// </summary>
        public static void Initialize(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            System_Boolean_Binding.Register(app);
            System_String_Binding.Register(app);
            BDebug_Binding.Register(app);
            System_Collections_Generic_List_1_Type_Binding.Register(app);
            BDFramework_ILRuntimeHelper_Binding.Register(app);
            ILRuntime_Runtime_Enviorment_AppDomain_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_IType_Binding.Register(app);
            System_Linq_Enumerable_Binding.Register(app);
            System_Collections_Generic_List_1_IType_Binding.Register(app);
            System_Collections_Generic_List_1_IType_Binding_Enumerator_Binding.Register(app);
            ILRuntime_CLR_TypeSystem_IType_Binding.Register(app);
            System_IDisposable_Binding.Register(app);
            System_Reflection_Assembly_Binding.Register(app);
            UnityEngine_Debug_Binding.Register(app);
            System_Collections_Generic_List_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_List_1_Type_Binding_Enumerator_Binding.Register(app);
            System_Object_Binding.Register(app);
            System_Activator_Binding.Register(app);
            System_Collections_Generic_List_1_ILTypeInstance_Binding_Enumerator_Binding.Register(app);
            BDFramework_GameStart_IGameStart_Binding.Register(app);
            BDFramework_BDLauncher_Binding.Register(app);
            UnityEngine_Transform_Binding.Register(app);
            UnityEngine_Object_Binding.Register(app);
            System_Runtime_CompilerServices_AsyncVoidMethodBuilder_Binding.Register(app);
            LitJson_JsonMapper_Binding.Register(app);
            System_Action_1_String_Binding.Register(app);
            System_Action_2_Int32_Int32_Binding.Register(app);
            System_Int32_Binding.Register(app);
            UnityEngine_UI_Button_Binding.Register(app);
            UnityEngine_Events_UnityEvent_Binding.Register(app);
            UnityEngine_Random_Binding.Register(app);
            BDFramework_ResourceMgr_BResources_Binding.Register(app);
            System_Collections_Generic_List_1_String_Binding.Register(app);
            System_Collections_Generic_IEnumerable_1_KeyValuePair_2_String_Object_Binding.Register(app);
            System_Collections_Generic_IEnumerator_1_KeyValuePair_2_String_Object_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_String_Object_Binding.Register(app);
            System_Collections_IEnumerator_Binding.Register(app);
            UnityEngine_Application_Binding.Register(app);
            BDFramework_VersionContrller_VersionContorller_Binding.Register(app);
            UnityEngine_Camera_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Object_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Object_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_String_Object_Binding_t.Register(app);
            UnityEngine_UI_Text_Binding.Register(app);
            System_Single_Binding.Register(app);
            DG_Tweening_ShortcutExtensions_Binding.Register(app);
            UnityEngine_Vector3_Binding.Register(app);
            DG_Tweening_TweenSettingsExtensions_Binding.Register(app);
            BDFramework_DataListener_DataListenerServer_Binding.Register(app);
            System_Array_Binding.Register(app);
            IEnumeratorTool_Binding.Register(app);
            UnityEngine_WaitForSeconds_Binding.Register(app);
            System_NotSupportedException_Binding.Register(app);
            BDFramework_DataListener_ADataListener_Binding.Register(app);
            SQLite4Unity3d_SQLiteConnection_Binding.Register(app);
            SQLite4Unity3d_SQLiteCommand_Binding.Register(app);
            System_Collections_Generic_List_1_Object_Binding.Register(app);
            System_Collections_Generic_List_1_Object_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_List_1_Int32_Binding.Register(app);
            System_Collections_Generic_List_1_Int32_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_MemberInfo_Binding.Register(app);
            System_Collections_Generic_List_1_MemberInfo_Binding.Register(app);
            System_Collections_Generic_List_1_MemberInfo_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_Dictionary_2_String_MemberInfo_Binding.Register(app);
            ILRuntime_Mono_Cecil_MemberReference_Binding.Register(app);
            Game_ILRuntime_ILTypeHelper_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Type_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Action_2_UIBehaviour_Object_Binding.Register(app);
            System_Action_2_UIBehaviour_Object_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Action_2_Transform_Object_Binding.Register(app);
            System_Action_2_Transform_Object_Binding.Register(app);
            UnityEngine_Behaviour_Binding.Register(app);
            UnityEngine_GameObject_Binding.Register(app);
            UnityEngine_Events_UnityEventBase_Binding.Register(app);
            System_Action_Binding.Register(app);
            BDFramework_UFlux_IButton_Binding.Register(app);
            BDFramework_UFlux_IComponentOnClick_Binding.Register(app);
            UnityEngine_UI_Image_Binding.Register(app);
            UnityEngine_UI_Graphic_Binding.Register(app);
            System_Collections_IEnumerable_Binding.Register(app);
            System_Collections_Generic_IEnumerable_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_IEnumerator_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_Dictionary_2_String_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_MemberInfo_Binding_ValueCollection_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_MemberInfo_Binding_ValueCollection_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_List_1_String_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_List_1_Enum_Binding.Register(app);
            System_Collections_Generic_List_1_Enum_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_List_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding_ValueCollection_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding_ValueCollection_Binding_Enumerator_Binding.Register(app);
            System_Collections_ICollection_Binding.Register(app);
            UnityEngine_Mathf_Binding.Register(app);
            System_Collections_IList_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_Object_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Action_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_ILTypeInstance_Binding_ValueCollection_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_ILTypeInstance_Binding_ValueCollection_Binding_Enumerator_Binding.Register(app);
            System_Action_1_ILTypeInstance_Binding.Register(app);
            UnityEngine_UI_Scrollbar_Binding.Register(app);
            UnityEngine_Events_UnityEvent_1_Single_Binding.Register(app);
            UnityEngine_UI_Slider_Binding.Register(app);
            UnityEngine_UI_Toggle_Binding.Register(app);
            UnityEngine_UI_ScrollRect_Binding.Register(app);
            BDFramework_Sql_SqliteLoder_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_Int32_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Transform_Dictionary_2_String_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_List_1_UITool_Attribute_Binding.Register(app);
            UITool_Attribute_Binding.Register(app);
            UnityEngine_WaitForSecondsRealtime_Binding.Register(app);
            System_Collections_Generic_List_1_Transform_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_Type_Binding.Register(app);
            BDFramework_Core_Debugger_Debugger_NetworkServer_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_MethodInfo_Binding.Register(app);

            ILRuntime.CLR.TypeSystem.CLRType __clrType = null;
            __clrType = (ILRuntime.CLR.TypeSystem.CLRType)app.GetType (typeof(UnityEngine.Vector2));
            s_UnityEngine_Vector2_Binding_Binder = __clrType.ValueTypeBinder as ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector2>;
            __clrType = (ILRuntime.CLR.TypeSystem.CLRType)app.GetType (typeof(UnityEngine.Vector3));
            s_UnityEngine_Vector3_Binding_Binder = __clrType.ValueTypeBinder as ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector3>;
            __clrType = (ILRuntime.CLR.TypeSystem.CLRType)app.GetType (typeof(UnityEngine.Vector4));
            s_UnityEngine_Vector4_Binding_Binder = __clrType.ValueTypeBinder as ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector4>;
            __clrType = (ILRuntime.CLR.TypeSystem.CLRType)app.GetType (typeof(UnityEngine.Quaternion));
            s_UnityEngine_Quaternion_Binding_Binder = __clrType.ValueTypeBinder as ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Quaternion>;
        }

        /// <summary>
        /// Release the CLR binding, please invoke this BEFORE ILRuntime Appdomain destroy
        /// </summary>
        public static void Shutdown(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            s_UnityEngine_Vector2_Binding_Binder = null;
            s_UnityEngine_Vector3_Binding_Binder = null;
            s_UnityEngine_Vector4_Binding_Binder = null;
            s_UnityEngine_Quaternion_Binding_Binder = null;
        }
    }
}
