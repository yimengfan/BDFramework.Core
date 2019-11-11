// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/Splatmap/Standard-BaseGen" {
    Properties
    {
        [HideInInspector] _Control("AlphaMap", 2D) = "" {}

        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] [Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 1.0

        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }
    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"
        sampler2D _Control;

        struct appdata_t {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        ENDCG

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _Splat0;
            sampler2D _Splat1;
            sampler2D _Splat2;
            sampler2D _Splat3;

            float _Smoothness0;
            float _Smoothness1;
            float _Smoothness2;
            float _Smoothness3;

            float4 _Control_TexelSize;
            float4 _Splat0_ST;
            float4 _Splat1_ST;
            float4 _Splat2_ST;
            float4 _Splat3_ST;

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                float2 texcoord3 : TEXCOORD3;
                float2 texcoord4 : TEXCOORD4;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // adjust splatUVs so the edges of the terrain tile lie on pixel centers
                float2 controlUV = (v.texcoord * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
                o.texcoord0 = controlUV;
                o.texcoord1 = TRANSFORM_TEX(v.texcoord, _Splat0);
                o.texcoord2 = TRANSFORM_TEX(v.texcoord, _Splat1);
                o.texcoord3 = TRANSFORM_TEX(v.texcoord, _Splat2);
                o.texcoord4 = TRANSFORM_TEX(v.texcoord, _Splat3);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 alpha = tex2D(_Control, i.texcoord0);
                float4 splat0 = tex2D(_Splat0, i.texcoord1);
                float4 splat1 = tex2D(_Splat1, i.texcoord2);
                float4 splat2 = tex2D(_Splat2, i.texcoord3);
                float4 splat3 = tex2D(_Splat3, i.texcoord4);

                splat0.a *= _Smoothness0;
                splat1.a *= _Smoothness1;
                splat2.a *= _Smoothness2;
                splat3.a *= _Smoothness3;

                float4 albedoSmoothness = splat0 * alpha.x;
                albedoSmoothness += splat1 * alpha.y;
                albedoSmoothness += splat2 * alpha.z;
                albedoSmoothness += splat3 * alpha.w;
                return albedoSmoothness;
            }
            ENDCG
        }

        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "R8"
                "Size" = "1/4"
                "EmptyColor" = "FF000000"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float _Metallic0;
            float _Metallic1;
            float _Metallic2;
            float _Metallic3;

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord0 = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 alpha = tex2D(_Control, i.texcoord0);

                float4 metallic = { _Metallic0 * alpha.x, 0, 0, 0 };
                metallic.r += _Metallic1 * alpha.y;
                metallic.r += _Metallic2 * alpha.z;
                metallic.r += _Metallic3 * alpha.w;
                return metallic;
            }
            ENDCG
        }
    }
    Fallback Off
}
