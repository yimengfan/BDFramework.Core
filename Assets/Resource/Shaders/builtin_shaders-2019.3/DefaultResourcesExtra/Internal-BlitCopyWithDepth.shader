// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/BlitCopyWithDepth" {
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _Color("Multiplicative color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader {
        Pass{
            ZTest Always Cull Off ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_DepthTex);

            uniform float4 _MainTex_ST;
            uniform float4 _Color;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            fixed4 frag(v2f i, out float oDepth : SV_Depth
            ) : SV_Target
            {
                oDepth = SAMPLE_RAW_DEPTH_TEXTURE(_DepthTex, i.texcoord);
                return tex2D(_MainTex, i.texcoord) * _Color;
            }
            ENDCG

        }

    }
    Fallback Off
}
