// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/Splatmap/Standard-BaseGen"
{
    Properties
    {
        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }
    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"
        sampler2D _Control;
        float4 _Control_ST;
        float4 _Control_TexelSize;

        float4 _Splat0_ST;
        float4 _Splat1_ST;
        float4 _Splat2_ST;
        float4 _Splat3_ST;

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        float2 ComputeControlUV(float2 uv)
        {
            // adjust splatUVs so the edges of the terrain tile lie on pixel centers
            return (uv * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
        }

        ENDCG

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "RGBA32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
            float _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;

            struct v2f
            {
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
                float2 uv = TRANSFORM_TEX(v.texcoord, _Control);
                o.texcoord0 = ComputeControlUV(uv);
                o.texcoord1 = TRANSFORM_TEX(uv, _Splat0);
                o.texcoord2 = TRANSFORM_TEX(uv, _Splat1);
                o.texcoord3 = TRANSFORM_TEX(uv, _Splat2);
                o.texcoord4 = TRANSFORM_TEX(uv, _Splat3);
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
            // _NormalMap pass will get ignored by terrain basemap generation code. Put here so that the VTC can use it to generate cache for normal maps.
            Tags
            {
                "Name" = "_NormalMap"
                "Format" = "A2R10G10B10"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
            float _NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3;

            struct v2f
            {
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
                float2 uv = TRANSFORM_TEX(v.texcoord, _Control);
                o.texcoord0 = ComputeControlUV(uv);
                o.texcoord1 = TRANSFORM_TEX(uv, _Splat0);
                o.texcoord2 = TRANSFORM_TEX(uv, _Splat1);
                o.texcoord3 = TRANSFORM_TEX(uv, _Splat2);
                o.texcoord4 = TRANSFORM_TEX(uv, _Splat3);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 alpha = tex2D(_Control, i.texcoord0);

                float3 normal;
                normal = UnpackNormalWithScale(tex2D(_Normal0, i.texcoord1), _NormalScale0) * alpha.x;
                normal += UnpackNormalWithScale(tex2D(_Normal1, i.texcoord2), _NormalScale1) * alpha.y;
                normal += UnpackNormalWithScale(tex2D(_Normal2, i.texcoord3), _NormalScale2) * alpha.z;
                normal += UnpackNormalWithScale(tex2D(_Normal3, i.texcoord4), _NormalScale3) * alpha.w;
                return float4(normal.xyz * 0.5f + 0.5f, 1.0f);
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
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            float _Metallic0, _Metallic1, _Metallic2, _Metallic3;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord0 = ComputeControlUV(TRANSFORM_TEX(v.texcoord, _Control));
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
