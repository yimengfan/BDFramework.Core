// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Internal-UIRDefault"
{
    Properties
    {
        // Establish sensible default values
        [HideInInspector] _MainTex("Atlas", 2D) = "white" {}
        [HideInInspector] _FontTex("Font", 2D) = "black" {}
        [HideInInspector] _CustomTex("Custom", 2D) = "black" {}
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)

        // The ortho matrix used to draw 2D UI causes the Y coordinate to flip which also reverses the winding order,
        // so our two-sided stencil is setup accordingly (front settings are swapped with back settings).
        // When drawing in perspective, then these settings are flipped.
        [HideInInspector] _StencilCompFront("__scf", Float) = 3.0   // Equal
        [HideInInspector] _StencilPassFront("__spf", Float) = 0.0   // Keep
        [HideInInspector] _StencilZFailFront("__szf", Float) = 1.0  // Zero
        [HideInInspector] _StencilFailFront("__sff", Float) = 0.0   // Keep

        [HideInInspector] _StencilCompBack("__scb", Float) = 8.0    // Always
        [HideInInspector] _StencilPassBack("__spb", Float) = 0.0    // Keep
        [HideInInspector] _StencilZFailBack("__szb", Float) = 2.0   // Replace
        [HideInInspector] _StencilFailBack("__sfb", Float) = 0.0    // Keep
    }

    Category
    {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        // Users pass depth between [Near,Far] = [-1,1]. This gets stored on the depth buffer in [Near,Far] [0,1] regardless of the underlying graphics API.
        Cull Off    // Two sided rendering is crucial for immediate clipping
        ZTest GEqual
        ZWrite Off
        Stencil
        {
            Ref         255 // 255 for ease of visualization in RenderDoc, but can be just one bit
            ReadMask    255
            WriteMask   255

            CompFront[_StencilCompFront]
            PassFront[_StencilPassFront]
            ZFailFront[_StencilZFailFront]
            FailFront[_StencilFailFront]

            CompBack[_StencilCompBack]
            PassBack[_StencilPassBack]
            ZFailBack[_StencilZFailBack]
            FailBack[_StencilFailBack]
        }

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        // SM3.5 version
        SubShader
        {
            Tags { "UIE_VertexTexturingIsAvailable" = "1" }
            Pass
            {
                CGPROGRAM
                #pragma target 3.5
                #pragma vertex vert
                #pragma fragment frag
                #pragma require samplelod
                #define UIE_SDF_TEXT
                #include "UnityUIE.cginc"
                ENDCG
            }
        }

        // SM2.0 version
        SubShader
        {
            Pass
            {
                CGPROGRAM
                #pragma target 2.0
                #pragma vertex vert
                #pragma fragment frag
                #define UIE_SDF_TEXT
                #include "UnityUIE.cginc"
                ENDCG
            }
        }
    } // Category
}
