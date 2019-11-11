// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Nature/SpeedTree8"
{
    Properties
    {
        _MainTex ("Base (RGB) Transparency (A)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Toggle(EFFECT_HUE_VARIATION)] _HueVariationKwToggle("Hue Variation", Float) = 0
        _HueVariationColor ("Hue Variation Color", Color) = (1.0,0.5,0.0,0.1)

        [Toggle(EFFECT_BUMP)] _NormalMapKwToggle("Normal Mapping", Float) = 0
        _BumpMap ("Normalmap", 2D) = "bump" {}

        _ExtraTex ("Smoothness (R), Metallic (G), AO (B)", 2D) = "(0.5, 0.0, 1.0)" {}
        _Glossiness ("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0

        [Toggle(EFFECT_SUBSURFACE)] _SubsurfaceKwToggle("Subsurface", Float) = 0
        _SubsurfaceTex ("Subsurface (RGB)", 2D) = "white" {}
        _SubsurfaceColor ("Subsurface Color", Color) = (1,1,1,1)
        _SubsurfaceIndirect ("Subsurface Indirect", Range(0.0, 1.0)) = 0.25

        [Toggle(EFFECT_BILLBOARD)] _BillboardKwToggle("Billboard", Float) = 0
        _BillboardShadowFade ("Billboard Shadow Fade", Range(0.0, 1.0)) = 0.5

        [Enum(No,2,Yes,0)] _TwoSided ("Two Sided", Int) = 2 // enum matches cull mode
        [KeywordEnum(None,Fastest,Fast,Better,Best,Palm)] _WindQuality ("Wind Quality", Range(0,5)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "DisableBatching"="LODFading"
        }
        LOD 400
        Cull [_TwoSided]

        CGPROGRAM
            #pragma surface SpeedTreeSurf SpeedTreeSubsurface vertex:SpeedTreeVert dithercrossfade addshadow
            #pragma target 3.0
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma instancing_options assumeuniformscaling maxcount:50

            #pragma shader_feature _WINDQUALITY_NONE _WINDQUALITY_FASTEST _WINDQUALITY_FAST _WINDQUALITY_BETTER _WINDQUALITY_BEST _WINDQUALITY_PALM
            #pragma shader_feature EFFECT_BILLBOARD
            #pragma shader_feature EFFECT_HUE_VARIATION
            #pragma shader_feature EFFECT_SUBSURFACE
            #pragma shader_feature EFFECT_BUMP
            #pragma shader_feature EFFECT_EXTRA_TEX

            #define ENABLE_WIND
            #define EFFECT_BACKSIDE_NORMALS
            #include "SpeedTree8Common.cginc"

        ENDCG
    }

    // targeting SM2.0: Many effects are disabled for fewer instructions
    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "DisableBatching"="LODFading"
        }
        LOD 400
        Cull [_TwoSided]

        CGPROGRAM
            #pragma surface SpeedTreeSurf Standard vertex:SpeedTreeVert addshadow noinstancing
            #pragma multi_compile_vertex LOD_FADE_PERCENTAGE
            #pragma shader_feature EFFECT_BILLBOARD
            #pragma shader_feature EFFECT_EXTRA_TEX

            #include "SpeedTree8Common.cginc"

        ENDCG
    }

    FallBack "Transparent/Cutout/VertexLit"
    CustomEditor "SpeedTree8ShaderGUI"
}
