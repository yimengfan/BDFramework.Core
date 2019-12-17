// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Internal-CombineDepthNormals" {
SubShader {

Pass {
    ZWrite Off ZTest Always Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct appdata {
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};
float4 _CameraNormalsTexture_ST;

v2f vert (appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord,_CameraNormalsTexture);
    return o;
}
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
sampler2D _CameraNormalsTexture;

fixed4 frag (v2f i) : SV_Target
{
    float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
    float3 n = tex2D (_CameraNormalsTexture, i.uv) * 2.0 - 1.0;
    d = Linear01Depth (d);
    n = mul ((float3x3)unity_WorldToCamera, n);
    n.z = -n.z;
    return (d < (1.0-1.0/65025.0)) ? EncodeDepthNormal (d, n.xyz) : float4(0.5,0.5,1.0,1.0);
}
ENDCG
}

}
Fallback Off
}
