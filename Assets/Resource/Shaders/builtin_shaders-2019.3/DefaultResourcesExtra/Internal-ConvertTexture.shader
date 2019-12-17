// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/ConvertTexture" {
    SubShader {
        // Cubemap blit.  Takes a face index.
        Pass {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            samplerCUBE _MainTex;
            float4 _MainTex_ST;
            float _faceIndex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            static const float3 faceU[6] = { float3(0, 0, -1), float3(0, 0, 1), float3(1, 0, 0), float3(1, 0, 0), float3(1, 0, 0), float3(-1, 0, 0) };
            static const float3 faceV[6] = { float3(0, -1, 0), float3(0, -1, 0), float3(0, 0, 1), float3(0, 0, -1), float3(0, -1, 0), float3(0, -1, 0) };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                float2 uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                uv = uv * 2 - 1;

                int idx = (int)_faceIndex;
                float3 transformU = faceU[idx];
                float3 transformV = faceV[idx];

                float3 n = cross(transformV, transformU);
                o.texcoord = n + uv.x * transformU + uv.y * transformV;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return texCUBE(_MainTex, i.texcoord);
            }
            ENDCG

        }
    }
    Fallback Off
}
