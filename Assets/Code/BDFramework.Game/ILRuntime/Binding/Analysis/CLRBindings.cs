using System;
using System.Collections.Generic;
using System.Reflection;

namespace ILRuntime.Runtime.Generated
{
    class CLRBindings
    {

//will auto register in unity
#if UNITY_5_3_OR_NEWER
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        static private void RegisterBindingAction()
        {
            ILRuntime.Runtime.CLRBinding.CLRBindingUtils.RegisterBindingAction(Initialize);
        }

        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector2> s_UnityEngine_Vector2_Binding_Binder = null;
        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector3> s_UnityEngine_Vector3_Binding_Binder = null;
        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Vector4> s_UnityEngine_Vector4_Binding_Binder = null;
        internal static ILRuntime.Runtime.Enviorment.ValueTypeBinder<UnityEngine.Quaternion> s_UnityEngine_Quaternion_Binding_Binder = null;

        /// <summary>
        /// Initialize the CLR binding, please invoke this AFTER CLR Redirection registration
        /// </summary>
        public static void Initialize(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            System_Runtime_CompilerServices_AsyncVoidMethodBuilder_Binding.Register(app);
            LitJson_JsonMapper_Binding.Register(app);
            System_Object_Binding.Register(app);
            System_Action_1_String_Binding.Register(app);
            System_Action_2_Int32_Int32_Binding.Register(app);
            UnityEngine_Debug_Binding.Register(app);
            BDFramework_ILRuntimeHelper_Binding.Register(app);
            System_Collections_Generic_List_1_Int32_Binding.Register(app);
            BDFramework_Sql_SqliteHelper_Binding.Register(app);
            BDFramework_Sql_SqliteHelper_Binding_SQLiteService_Binding.Register(app);
            SQLite_TableQueryForILRuntime_Binding.Register(app);
            System_Collections_Generic_List_1_Hero_Binding.Register(app);
            System_Collections_Generic_List_1_Hero_Binding_Enumerator_Binding.Register(app);
            System_IDisposable_Binding.Register(app);
            System_Collections_Generic_List_1_GameObject_Binding.Register(app);
            BDFramework_ResourceMgr_BResources_Binding.Register(app);
            UnityEngine_Object_Binding.Register(app);
            System_Collections_Generic_List_1_String_Binding.Register(app);
            IEnumeratorTool_Binding.Register(app);
            UnityEngine_Application_Binding.Register(app);
            System_Collections_Generic_IEnumerable_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_IEnumerator_1_ILTypeInstance_Binding.Register(app);
            System_Type_Binding.Register(app);
            System_String_Binding.Register(app);
            System_Collections_IEnumerator_Binding.Register(app);
            BDebug_Binding.Register(app);
            System_Collections_Generic_IEnumerable_1_KeyValuePair_2_String_Object_Binding.Register(app);
            System_Collections_Generic_IEnumerator_1_KeyValuePair_2_String_Object_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_String_Object_Binding.Register(app);
            System_Collections_Generic_List_1_GameObject_Binding_Enumerator_Binding.Register(app);
            BDFramework_DataListener_StatusListenerServer_Binding.Register(app);
            BDFramework_DataListener_ValueListenerEx_Binding.Register(app);
            UnityEngine_UI_Button_Binding.Register(app);
            UnityEngine_Events_UnityEvent_Binding.Register(app);
            BDFramework_DataListener_EventListenerEx_Binding.Register(app);
            Game_Client_Binding.Register(app);
            UnityEngine_UI_Text_Binding.Register(app);
            Game_Demo_M_StatusListenerTest_Binding.Register(app);
            DotNetExtension_DateTimeEx_Binding.Register(app);
            System_Int64_Binding.Register(app);
            System_Int32_Binding.Register(app);
            System_DateTime_Binding.Register(app);
            System_Reflection_MemberInfo_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Type_Binding.Register(app);
            System_Collections_Generic_List_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_List_1_ILTypeInstance_Binding_Enumerator_Binding.Register(app);
            BDFramework_Mgr_ManagerInstHelper_Binding.Register(app);
            System_Activator_Binding.Register(app);
            BDFramework_BDLauncher_Binding.Register(app);
            System_Array_Binding.Register(app);
            UnityEngine_Transform_Binding.Register(app);
            UnityEngine_Random_Binding.Register(app);
            UnityEngine_WaitForSeconds_Binding.Register(app);
            System_NotSupportedException_Binding.Register(app);
            Google_Protobuf_ProtoPreconditions_Binding.Register(app);
            Google_Protobuf_CodedOutputStream_Binding.Register(app);
            Google_Protobuf_CodedInputStream_Binding.Register(app);
            Google_Protobuf_MessageParser_1_IMessageAdapter_Binding_Adaptor_Binding.Register(app);
            Google_Protobuf_Collections_RepeatedField_1_IMessageAdapter_Binding_Adaptor_Binding.Register(app);
            Google_Protobuf_FieldCodec_Binding.Register(app);
            System_Collections_Generic_IEnumerable_1_Type_Binding.Register(app);
            System_Collections_Generic_IEnumerator_1_Type_Binding.Register(app);
            System_Reflection_PropertyInfo_Binding.Register(app);
            System_Reflection_CustomAttributeExtensions_Binding.Register(app);
            BDFramework_Mgr_ManagerOrder_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_Int32_ILTypeInstance_Binding.Register(app);
            BDFramework_Core_Debugger_Debugger_NetworkServer_Binding.Register(app);
            BDFramework_ResourceMgr_IResMgr_Binding.Register(app);
            BDFramework_DataListener_AStatusListener_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_Object_Binding.Register(app);
            System_Net_WebClient_Binding.Register(app);
            System_Threading_Tasks_Task_1_String_Binding.Register(app);
            System_Runtime_CompilerServices_TaskAwaiter_1_String_Binding.Register(app);
            Com_Test_Item_one_item_Binding.Register(app);
            Com_Test_Item_rsp_getItem_Binding.Register(app);
            Google_Protobuf_Collections_RepeatedField_1_one_item_Binding.Register(app);
            Google_Protobuf_MessageExtensions_Binding.Register(app);
            Google_Protobuf_MessageParser_1_rsp_getItem_Binding.Register(app);
            System_IO_MemoryStream_Binding.Register(app);
            System_IO_Stream_Binding.Register(app);
            System_IO_IPath_Binding.Register(app);
            BDFramework_Sql_SqliteLoder_Binding.Register(app);
            System_Diagnostics_Stopwatch_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_List_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_List_1_Type_Binding.Register(app);
            System_Reflection_Assembly_Binding.Register(app);
            System_Linq_Enumerable_Binding.Register(app);
            System_Collections_Generic_List_1_Type_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_List_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_List_1_ILTypeInstance_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_Type_List_1_ILTypeInstance_Binding.Register(app);
            System_Reflection_MethodBase_Binding.Register(app);
            System_Exception_Binding.Register(app);
            BDFramework_Core_Tools_BApplication_Binding.Register(app);
            System_Collections_Generic_List_1_Single_Binding.Register(app);
            System_Collections_Generic_List_1_Double_Binding.Register(app);
            System_Collections_Generic_List_1_Boolean_Binding.Register(app);
            System_Single_Binding.Register(app);
            System_Double_Binding.Register(app);
            System_Boolean_Binding.Register(app);
            UnityEngine_Component_Binding.Register(app);
            System_Reflection_FieldInfo_Binding.Register(app);
            System_Action_Binding.Register(app);
            ILRuntime_Reflection_ILRuntimeFieldInfo_Binding.Register(app);
            ILRuntime_CLR_TypeSystem_IType_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_String_IType_Binding.Register(app);
            System_Collections_IList_Binding.Register(app);
            System_Action_1_Boolean_Binding.Register(app);
            UnityEngine_UI_Toggle_Binding.Register(app);
            UnityEngine_Events_UnityEvent_1_Boolean_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_ILTypeInstance_Binding.Register(app);
            UnityEngine_Events_UnityEventBase_Binding.Register(app);
            UnityEngine_UI_Selectable_Binding.Register(app);
            BDFramework_UFlux_IButton_Binding.Register(app);
            BDFramework_UFlux_IComponentOnClick_Binding.Register(app);
            UnityEngine_UI_Image_Binding.Register(app);
            UnityEngine_UI_Graphic_Binding.Register(app);
            UnityEngine_GameObject_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Transform_ILTypeInstance_Binding.Register(app);
            UnityEngine_UI_ScrollRect_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_ILTypeInstance_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_MemberInfo_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_Dictionary_2_String_MemberInfo_Binding.Register(app);
            ILRuntime_Mono_Cecil_MemberReference_Binding.Register(app);
            UnityEngine_Behaviour_Binding.Register(app);
            ILRuntime_Runtime_Enviorment_AppDomain_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_IType_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_List_1_MemberInfo_Binding.Register(app);
            System_Collections_Generic_List_1_MemberInfo_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_MemberInfo_Binding_ValueCollection_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_MemberInfo_Binding_ValueCollection_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_String_ILTypeInstance_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_KeyValuePair_2_String_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_List_1_Transform_Binding.Register(app);
            System_Collections_Generic_List_1_Int32_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_List_1_Enum_Binding.Register(app);
            System_Collections_Generic_List_1_Enum_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding_ValueCollection_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_ILTypeInstance_Binding_ValueCollection_Binding_Enumerator_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Int32_List_1_ILTypeInstance_Binding.Register(app);
            System_Reflection_MethodInfo_Binding.Register(app);
            System_Reflection_ParameterInfo_Binding.Register(app);
            System_Collections_Generic_List_1_Object_Binding.Register(app);
            UnityEngine_Color_Binding.Register(app);
            System_Runtime_CompilerServices_AsyncTaskMethodBuilder_1_ILTypeInstance_Binding.Register(app);
            System_Uri_Binding.Register(app);
            System_Net_DownloadStringCompletedEventArgs_Binding.Register(app);
            System_Action_1_ILTypeInstance_Binding.Register(app);
            System_Collections_Generic_Dictionary_2_Type_MethodInfo_Binding.Register(app);
            BDFramework_DataListener_StatusListenerService_Binding.Register(app);
            System_Action_2_Int32_ILTypeInstance_Binding.Register(app);
            Cysharp_Text_ZString_Binding.Register(app);
            System_Collections_Generic_Queue_1_ILTypeInstance_Binding.Register(app);

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
