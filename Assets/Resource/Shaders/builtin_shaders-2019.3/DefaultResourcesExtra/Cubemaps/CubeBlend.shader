// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/CubeBlend"
{
Properties
{
    [NoScaleOffset] _TexA ("Cubemap", Cube) = "grey" {}
    [NoScaleOffset] _TexB ("Cubemap", Cube) = "grey" {}
    _value ("Value", Range (0, 1)) = 0.5
}

CGINCLUDE

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

half4 _TexA_HDR;
half4 _TexB_HDR;

UNITY_DECLARE_TEXCUBE(_TexA);
UNITY_DECLARE_TEXCUBE(_TexB);

float _Level;
float _value;

struct appdata_t {
    float4 vertex : POSITION;
    float3 texcoord : TEXCOORD0;
};

struct v2f {
    float4 vertex : SV_POSITION;
    float3 texcoord : TEXCOORD0;
};

v2f vert (appdata_t v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.texcoord = v.texcoord;
    return o;
}

half4 frag (v2f i) : SV_Target
{
    half3 texA = DecodeHDR (UNITY_SAMPLE_TEXCUBE_LOD (_TexA, i.texcoord, _Level), _TexA_HDR);
    half3 texB = DecodeHDR (UNITY_SAMPLE_TEXCUBE_LOD (_TexB, i.texcoord, _Level), _TexB_HDR);

    half3 res = lerp(texA, texB, _value);
    return half4(res, 1.0);
}
ENDCG

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" }
    Cull back ZWrite Off ZTest Always Fog { Mode Off }
    Pass
    {
        CGPROGRAM
        #pragma target 3.0
        ENDCG
    }
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" }
    Cull back ZWrite Off ZTest Always Fog { Mode Off }

    Pass
    {
        CGPROGRAM
        #pragma target 2.0
        ENDCG
    }
}

Fallback Off

}
