// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/Splatmap/Standard-Base" {
    Properties {
        _MainTex ("Base (RGB) Smoothness (A)", 2D) = "white" {}
        _MetallicTex ("Metallic (R)", 2D) = "white" {}

        // used in fallback on old cards
        _Color ("Main Color", Color) = (1,1,1,1)
    }

    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-100"
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:SplatmapVert addshadow fullforwardshadows
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
        #pragma target 3.0
        // needs more than 8 texcoords
        #pragma exclude_renderers gles

        #define TERRAIN_BASE_PASS
        #define TERRAIN_INSTANCED_PERPIXEL_NORMAL
        #include "TerrainSplatmapCommon.cginc"
        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex;
        sampler2D _MetallicTex;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            half4 c = tex2D (_MainTex, IN.tc.xy);
            o.Albedo = c.rgb;
            o.Alpha = 1;
            o.Smoothness = c.a;
            o.Metallic = tex2D (_MetallicTex, IN.tc.xy).r;

            #if defined(INSTANCING_ON) && defined(SHADER_TARGET_SURFACE_ANALYSIS) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)
                o.Normal = float3(0, 0, 1); // make sure that surface shader compiler realizes we write to normal, as UNITY_INSTANCING_ENABLED is not defined for SHADER_TARGET_SURFACE_ANALYSIS.
            #endif

            #if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)
                o.Normal = normalize(tex2D(_TerrainNormalmapTexture, IN.tc.zw).xyz * 2 - 1).xzy;
            #endif
        }

        ENDCG

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
        UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
    }

    FallBack "Diffuse"
}
