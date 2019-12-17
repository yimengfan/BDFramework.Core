// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/VR/BlitTexArraySliceToDepth_MSAA" {
    Properties { _MainTex ("Texture", any) = "" {} }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite On  ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"

            Texture2DMSArray<float> _MainTex;

            uniform float4 _MainTex_ST;
            uniform float _ArraySliceIndex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            fixed4 frag (v2f i, out float oDepth : SV_Depth) : SV_Target
            {
#ifdef SHADER_API_D3D11
                uint width, height, sampleCount, arraySliceCount;

                _MainTex.GetDimensions(width,height,arraySliceCount,sampleCount);
                int3 coord = int3(width * i.texcoord.x, height * i.texcoord.y, _ArraySliceIndex);

                // Resolve using max depth
                float maxDepth = 0.0;
                for(uint curSample = 0; curSample < sampleCount; ++curSample)
                {
                    maxDepth = max(_MainTex.Load(coord, curSample).x, maxDepth);
                }
                oDepth = maxDepth;
#else
                oDepth = 0.9; // unsupported
#endif
                return fixed4(oDepth, 0, 0, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
