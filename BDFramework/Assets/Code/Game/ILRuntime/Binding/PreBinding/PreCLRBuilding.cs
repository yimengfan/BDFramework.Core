using System;
using System.Collections.Generic;
using System.Reflection;

namespace ILRuntime.Runtime.Generated
{
    class PreCLRBuilding
    {
        /// <summary>
        /// Initialize the CLR binding, please invoke this AFTER CLR Redirection registration
        /// </summary>
        public static void Initialize(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
            UnityEngine_EventSystems_EventSystem_Binding.Register(app);
            UnityEngine_EventSystems_EventTrigger_Binding.Register(app);
            UnityEngine_EventSystems_ExecuteEvents_Binding.Register(app);
            UnityEngine_EventSystems_UIBehaviour_Binding.Register(app);
            UnityEngine_EventSystems_AxisEventData_Binding.Register(app);
            UnityEngine_EventSystems_AbstractEventData_Binding.Register(app);
            UnityEngine_EventSystems_BaseEventData_Binding.Register(app);
            UnityEngine_EventSystems_PointerEventData_Binding.Register(app);
            UnityEngine_EventSystems_BaseInput_Binding.Register(app);
            UnityEngine_EventSystems_BaseInputModule_Binding.Register(app);
            UnityEngine_EventSystems_PointerInputModule_Binding.Register(app);
            UnityEngine_EventSystems_StandaloneInputModule_Binding.Register(app);
            UnityEngine_EventSystems_BaseRaycaster_Binding.Register(app);
            UnityEngine_EventSystems_Physics2DRaycaster_Binding.Register(app);
            UnityEngine_EventSystems_PhysicsRaycaster_Binding.Register(app);
            UnityEngine_UI_AnimationTriggers_Binding.Register(app);
            UnityEngine_UI_CanvasUpdateRegistry_Binding.Register(app);
            UnityEngine_UI_DefaultControls_Binding.Register(app);
            UnityEngine_UI_Dropdown_Binding.Register(app);
            UnityEngine_UI_FontData_Binding.Register(app);
            UnityEngine_UI_FontUpdateTracker_Binding.Register(app);
            UnityEngine_UI_Graphic_Binding.Register(app);
            UnityEngine_UI_GraphicRaycaster_Binding.Register(app);
            UnityEngine_UI_GraphicRebuildTracker_Binding.Register(app);
            UnityEngine_UI_GraphicRegistry_Binding.Register(app);
            UnityEngine_UI_InputField_Binding.Register(app);
            UnityEngine_UI_Mask_Binding.Register(app);
            UnityEngine_UI_MaskableGraphic_Binding.Register(app);
            UnityEngine_UI_MaskUtilities_Binding.Register(app);
            UnityEngine_UI_RawImage_Binding.Register(app);
            UnityEngine_UI_RectMask2D_Binding.Register(app);
            UnityEngine_UI_ScrollRect_Binding.Register(app);
            UnityEngine_UI_Selectable_Binding.Register(app);
            UnityEngine_UI_StencilMaterial_Binding.Register(app);
            UnityEngine_UI_ToggleGroup_Binding.Register(app);
            UnityEngine_UI_ClipperRegistry_Binding.Register(app);
            UnityEngine_UI_Clipping_Binding.Register(app);
            UnityEngine_UI_AspectRatioFitter_Binding.Register(app);
            UnityEngine_UI_CanvasScaler_Binding.Register(app);
            UnityEngine_UI_ContentSizeFitter_Binding.Register(app);
            UnityEngine_UI_GridLayoutGroup_Binding.Register(app);
            UnityEngine_UI_HorizontalLayoutGroup_Binding.Register(app);
            UnityEngine_UI_HorizontalOrVerticalLayoutGroup_Binding.Register(app);
            UnityEngine_UI_LayoutElement_Binding.Register(app);
            UnityEngine_UI_LayoutGroup_Binding.Register(app);
            UnityEngine_UI_LayoutRebuilder_Binding.Register(app);
            UnityEngine_UI_LayoutUtility_Binding.Register(app);
            UnityEngine_UI_VerticalLayoutGroup_Binding.Register(app);
            UnityEngine_UI_VertexHelper_Binding.Register(app);
            UnityEngine_UI_BaseMeshEffect_Binding.Register(app);
            UnityEngine_UI_Outline_Binding.Register(app);
            UnityEngine_UI_PositionAsUV1_Binding.Register(app);
            UnityEngine_UI_Shadow_Binding.Register(app);
        }

        /// <summary>
        /// Release the CLR binding, please invoke this BEFORE ILRuntime Appdomain destroy
        /// </summary>
        public static void Shutdown(ILRuntime.Runtime.Enviorment.AppDomain app)
        {
        }
    }
}
