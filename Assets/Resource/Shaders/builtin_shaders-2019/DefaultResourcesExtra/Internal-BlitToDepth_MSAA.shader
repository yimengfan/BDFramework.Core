// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/BlitToDepth_MSAA" {
    Properties{ _MainTex("DepthTexture", any) = "" {} }
        SubShader{
            Pass {
                ZTest Always Cull Off ZWrite On ColorMask 0

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 5.0

                #include "UnityCG.cginc"

                UNITY_DECLARE_DEPTH_TEXTURE_MS(_MainTex);
                uniform float4 _MainTex_ST;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            float4 frag(v2f i, out float oDepth : SV_Depth) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#ifdef SHADER_API_D3D11
            uint width, height, sampleCount;

            #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
              uint arraySliceCount;
              _MainTex.GetDimensions(width, height, arraySliceCount, sampleCount);
              int3 coord = int3(width * i.texcoord.x, height * i.texcoord.y, unity_StereoEyeIndex);
            #else
              _MainTex.GetDimensions(width, height, sampleCount);
              int2 coord = int2(width * i.texcoord.x, height * i.texcoord.y);
            #endif

            // Resolve using max depth
            float maxDepth = 0.0;
            for(uint curSample = 0; curSample < sampleCount; ++curSample)
            {
              maxDepth = max(_MainTex.Load(coord, curSample).x, maxDepth);
            }
            oDepth = maxDepth;
#else
            oDepth = 0.9;
#endif
            return float4(oDepth, 0, 0, 1);
            }
            ENDCG

        }
    }
    Fallback Off
}
