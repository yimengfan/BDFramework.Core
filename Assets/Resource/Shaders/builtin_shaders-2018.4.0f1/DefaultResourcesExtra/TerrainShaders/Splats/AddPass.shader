// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass" {
    CGINCLUDE
        #pragma surface surf Lambert decal:add vertex:SplatmapVert finalcolor:SplatmapFinalColor finalprepass:SplatmapFinalPrepass finalgbuffer:SplatmapFinalGBuffer fullforwardshadows nometa
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
        #pragma multi_compile_fog

        #define TERRAIN_SPLAT_ADDPASS
        #include "TerrainSplatmapCommon.cginc"

        void surf(Input IN, inout SurfaceOutput o)
        {
            half4 splat_control;
            half weight;
            fixed4 mixedDiffuse;
            SplatmapMix(IN, splat_control, weight, mixedDiffuse, o.Normal);
            o.Albedo = mixedDiffuse.rgb;
            o.Alpha = weight;
        }
    ENDCG

    Category {
        Tags {
            "Queue" = "Geometry-99"
            "IgnoreProjector"="True"
            "RenderType" = "Opaque"
        }
        // TODO: Seems like "#pragma target 3.0 _NORMALMAP" can't fallback correctly on less capable devices?
        // Use two sub-shaders to simulate different features for different targets and still fallback correctly.
        SubShader { // for sm3.0+ targets
            CGPROGRAM
                #pragma target 3.0
                #pragma multi_compile __ _NORMALMAP
            ENDCG
        }
        SubShader { // for sm2.0 targets
            CGPROGRAM
            ENDCG
        }
    }

    Fallback off
}
