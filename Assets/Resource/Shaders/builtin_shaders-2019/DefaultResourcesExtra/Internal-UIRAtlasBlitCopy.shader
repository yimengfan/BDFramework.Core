// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Internal-UIRAtlasBlitCopy"
{
    Properties
    {
        _MainTex0("Texture", any) = "" {}
        _MainTex1("Texture", any) = "" {}
        _MainTex2("Texture", any) = "" {}
        _MainTex3("Texture", any) = "" {}
        _MainTex4("Texture", any) = "" {}
        _MainTex5("Texture", any) = "" {}
        _MainTex6("Texture", any) = "" {}
        _MainTex7("Texture", any) = "" {}
    }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off Blend Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform sampler2D _MainTex0;
            uniform float4 _MainTex0_ST;

            uniform sampler2D _MainTex1;
            uniform float4 _MainTex1_ST;

            uniform sampler2D _MainTex2;
            uniform float4 _MainTex2_ST;

            uniform sampler2D _MainTex3;
            uniform float4 _MainTex3_ST;

            uniform sampler2D _MainTex4;
            uniform float4 _MainTex4_ST;

            uniform sampler2D _MainTex5;
            uniform float4 _MainTex5_ST;

            uniform sampler2D _MainTex6;
            uniform float4 _MainTex6_ST;

            uniform sampler2D _MainTex7;
            uniform float4 _MainTex7_ST;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
                float4 tint : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                float4 tint : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                switch (v.texcoord.z)
                {
                case 0:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex0);
                    break;
                case 1:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex1);
                    break;
                case 2:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex2);
                    break;
                case 3:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex3);
                    break;
                case 4:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex4);
                    break;
                case 5:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex5);
                    break;
                case 6:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex6);
                    break;
                case 7:
                    o.texcoord.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex7);
                    break;
                default:
                    o.texcoord.xy = float2(0, 0);
                    break;
                }
                o.texcoord.z = v.texcoord.z;
                o.tint = v.tint;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = fixed4(1, 1, 1, 1);
                switch (i.texcoord.z)
                {
                case 0:
                    color = tex2D(_MainTex0, i.texcoord.xy);
                    break;
                case 1:
                    color = tex2D(_MainTex1, i.texcoord.xy);
                    break;
                case 2:
                    color = tex2D(_MainTex2, i.texcoord.xy);
                    break;
                case 3:
                    color = tex2D(_MainTex3, i.texcoord.xy);
                    break;
                case 4:
                    color = tex2D(_MainTex4, i.texcoord.xy);
                    break;
                case 5:
                    color = tex2D(_MainTex5, i.texcoord.xy);
                    break;
                case 6:
                    color = tex2D(_MainTex6, i.texcoord.xy);
                    break;
                case 7:
                    color = tex2D(_MainTex7, i.texcoord.xy);
                    break;
                }
                return color * i.tint;
            }
            ENDCG
        }
    }
    Fallback Off
}
