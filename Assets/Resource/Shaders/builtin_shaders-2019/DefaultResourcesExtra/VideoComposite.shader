// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/VideoComposite"
{
    Properties
    {
        _MainTex ("_MainTex (A)", 2D) = "black"
    }

    CGINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float  _AlphaParam;
        float4 _RightEyeUVOffset;
        float4 _MainTex_ST;

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

        v2f vertexDirect(appdata_t v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex) + unity_StereoEyeIndex * _RightEyeUVOffset.xy;
            return o;
        }

        fixed4 fragmentBlit(v2f i) : SV_Target
        {
            fixed4 col = tex2D(_MainTex, i.texcoord);
            return fixed4(col.rgb, col.a * _AlphaParam);
        }

    ENDCG

    SubShader
    {
        Tags{ "Queue" = "Transparent" }

        Pass
        {
            Name "Default"
            Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vertexDirect
            #pragma fragment fragmentBlit
            ENDCG
        }
    }

    FallBack Off
}
