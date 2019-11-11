// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Nature/Terrain/Diffuse" {
    Properties {
        // used in fallback on old cards & base map
        [HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
    }

    CGINCLUDE
        #pragma surface surf Lambert vertex:SplatmapVert finalcolor:SplatmapFinalColor finalprepass:SplatmapFinalPrepass finalgbuffer:SplatmapFinalGBuffer addshadow fullforwardshadows
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
        #pragma multi_compile_fog
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
            "RenderType" = "Opaque"
        }
        // TODO: Seems like "#pragma target 3.0 _NORMALMAP" can't fallback correctly on less capable devices?
        // Use two sub-shaders to simulate different features for different targets and still fallback correctly.
        SubShader { // for sm3.0+ targets
            CGPROGRAM
                #pragma target 3.0
                #pragma multi_compile __ _NORMALMAP
            ENDCG

            UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
            UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
        }
        SubShader { // for sm2.0 targets
            CGPROGRAM
            ENDCG
        }
    }

    Dependency "AddPassShader"    = "Hidden/TerrainEngine/Splatmap/Diffuse-AddPass"
    Dependency "BaseMapShader"    = "Hidden/TerrainEngine/Splatmap/Diffuse-Base"
    Dependency "BaseMapGenShader" = "Hidden/TerrainEngine/Splatmap/Diffuse-BaseGen"
    Dependency "Details0"         = "Hidden/TerrainEngine/Details/Vertexlit"
    Dependency "Details1"         = "Hidden/TerrainEngine/Details/WavingDoublePass"
    Dependency "Details2"         = "Hidden/TerrainEngine/Details/BillboardWavingDoublePass"
    Dependency "Tree0"            = "Hidden/TerrainEngine/BillboardTree"

    Fallback "Diffuse"
}
