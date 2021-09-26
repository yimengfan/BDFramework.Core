// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Nature/Tree Creator Albedo Rendertex" {
Properties {
    _TranslucencyColor("Translucency Color", Color) = (0.73,0.85,0.41,1) // (187,219,106,255)
    _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
    _HalfOverCutoff("0.5 / alpha cutoff", Range(0,1)) = 1.0
    _TranslucencyViewDependency("View dependency", Range(0,1)) = 0.7

    _MainTex("Base (RGB) Alpha (A)", 2D) = "white" {}
    _BumpSpecMap("Normalmap (GA) Spec (R) Shadow Offset (B)", 2D) = "bump" {}
    _TranslucencyMap("Trans (B) Gloss(A)", 2D) = "white" {}
}

SubShader {

    Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "UnityBuiltin3xTreeLibrary.cginc"

struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 color : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

CBUFFER_START(UnityTerrainImposter)
    float3 _TerrainTreeLightDirections[4];
    float4 _TerrainTreeLightColors[4];
CBUFFER_END

v2f vert (appdata_full v) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    ExpandBillboard (UNITY_MATRIX_IT_MV, v.vertex, v.normal, v.tangent);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    o.color = v.color.a;
    return o;
}

sampler2D _MainTex;
sampler2D _BumpSpecMap;
sampler2D _TranslucencyMap;
fixed _Cutoff;

fixed4 frag (v2f i) : SV_Target {
    fixed4 col = tex2D (_MainTex, i.uv);
    clip (col.a - _Cutoff);
    fixed4 translucency = tex2D(_TranslucencyMap, i.uv);
    col.a = translucency.b;
    return col;
}
ENDCG
    }
}

FallBack Off
}
