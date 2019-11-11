// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Compositing"
{
    Properties
    {
        _MainTex ("_MainTex (A)", 2D) = "black"
        _SecondTex ("_SecondTex (A)", 2D) = "black"
    }

    CGINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        sampler2D _SecondTex;
        float  _AlphaParam;
        float  _SecondAlphaParam;
        float4 _MainTex_ST;

        struct appdata_t {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        v2f vertexDirect(appdata_t v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
            return o;
        }

        fixed4 fragmentMix(v2f i) : SV_Target
        {
            fixed4 mainCol = tex2D(_MainTex, i.texcoord);
            fixed4 secondCol = tex2D(_SecondTex, i.texcoord);

            return fixed4(_AlphaParam * mainCol.a * mainCol.rgb + _SecondAlphaParam * secondCol.a * secondCol.rgb, 1.0f);
        }

    ENDCG

    SubShader
    {
        Pass
        {
            Name "Mix_RGBA_To_RGBA"
            Cull Off ZWrite On Blend Off
            CGPROGRAM
            #pragma vertex vertexDirect
            #pragma fragment fragmentMix
            ENDCG
        }
    }

    FallBack Off
}
