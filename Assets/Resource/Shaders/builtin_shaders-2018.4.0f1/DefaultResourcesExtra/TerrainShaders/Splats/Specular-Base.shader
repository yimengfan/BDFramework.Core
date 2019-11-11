// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/Splatmap/Specular-Base" {
    Properties {
        _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}

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
        #pragma surface surf BlinnPhong vertex:SplatmapVert addshadow fullforwardshadows
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd

        #define TERRAIN_BASE_PASS
        #define TERRAIN_SURFACE_OUTPUT SurfaceOutput
        #include "TerrainSplatmapCommon.cginc"

        sampler2D _MainTex;
        half _Shininess;

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 tex = tex2D(_MainTex, IN.tc.xy);
            o.Albedo = tex.rgb;
            o.Gloss = tex.a;
            o.Alpha = 1.0f;
            o.Specular = _Shininess;
        }
        ENDCG

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
        UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
    }

    FallBack "Legacy Shaders/Specular"
}
