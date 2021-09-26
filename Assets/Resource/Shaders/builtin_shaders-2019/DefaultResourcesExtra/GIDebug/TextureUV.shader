// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/GIDebug/TextureUV" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
        Pass {
            Tags { "RenderType"="Opaque" }
            LOD 200

            CGPROGRAM
            #pragma vertex vert_surf
            #pragma fragment frag_surf
            #include "UnityCG.cginc"
            #include "UnityShaderVariables.cginc"

            struct v2f_surf
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            half4 _Decode_HDR;
            float _ConvertToLinearSpace;
            float _StaticUV1;
            float _Exposure;

            v2f_surf vert_surf (appdata_full v)
            {
                v2f_surf o;
                o.pos = UnityObjectToClipPos(v.vertex);

                if (_StaticUV1)
                    o.uv.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                else
                    o.uv.xy = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;

                return o;
            }

            float4 frag_surf (v2f_surf IN) : COLOR
            {
                float4 mainTexSampled = tex2D (_MainTex, IN.uv.xy);
                float3 result;

                if (_Decode_HDR.x > 0)
                    result = float4 (DecodeHDR(mainTexSampled, _Decode_HDR), 1);
                else
                    result = mainTexSampled.rgb;

                if (_ConvertToLinearSpace)
                    result = LinearToGammaSpace (result);

                return float4 (result * exp2(_Exposure), 1);
            }
            ENDCG
        }
    }
}
