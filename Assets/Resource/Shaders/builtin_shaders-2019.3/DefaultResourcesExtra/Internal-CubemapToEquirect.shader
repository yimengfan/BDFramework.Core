// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Internal-CubemapToEquirect" {
    Properties
    {
        _MainTex("Texture", cube) = "" {}
    }
    SubShader {
        Pass{
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform float4 _MainTex_ST;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            samplerCUBE _MainTex;

            float3 computeRayDir(float2 tc)
            {
                //based on google's omni-directional stereo rendering thread
                const float PI = 3.14159265;
                float4 scaleOffset = float4(2.0f * PI, PI, -PI, -PI*0.5);
                float2 angles = tc*scaleOffset.xy + scaleOffset.zw;
                float2 angleCos = cos(angles);
                float2 angleSin = sin(angles);
                return float3(angleSin.x * angleCos.y, angleSin.y, angleCos.x * angleCos.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = computeRayDir(i.uv);
                return texCUBE(_MainTex, dir);
            }
            ENDCG

        }
    }
    Fallback Off
}
