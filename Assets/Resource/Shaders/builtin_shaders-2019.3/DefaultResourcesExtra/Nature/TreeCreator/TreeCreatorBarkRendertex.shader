// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Nature/Tree Creator Bark Rendertex" {
Properties {
    _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
    _BumpSpecMap ("Normalmap (GA) Spec (R)", 2D) = "bump" {}
    _TranslucencyMap ("Trans (RGB) Gloss(A)", 2D) = "white" {}

    // These are here only to provide default values
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
}

SubShader {
    Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 color : TEXCOORD1;
    float2 params1: TEXCOORD2;
    float2 params2: TEXCOORD3;
    float2 params3: TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
};

CBUFFER_START(UnityTerrainImposter)
    float3 _TerrainTreeLightDirections[4];
    float4 _TerrainTreeLightColors[4];
CBUFFER_END

float2 CalcTreeLightingParams(float3 normal, float3 lightDir, float3 viewDir)
{
    float2 output;
    half nl = dot (normal, lightDir);
    output.r = max (0, nl);

    half3 h = normalize (lightDir + viewDir);
    float nh = max (0, dot (normal, h));
    output.g = nh;
    return output;
}

v2f vert (appdata_full v) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));

    /* We used to do a for loop and store params as a texcoord array[3].
     * HLSL compiler, however, unrolls this loop and opens up the uniforms
     * into 3 separate texcoords, but doesn't do it on fragment shader.
     * This discrepancy causes error on iOS, so do it manually. */
    o.params1 = CalcTreeLightingParams(v.normal, _TerrainTreeLightDirections[0], viewDir);
    o.params2 = CalcTreeLightingParams(v.normal, _TerrainTreeLightDirections[1], viewDir);
    o.params3 = CalcTreeLightingParams(v.normal, _TerrainTreeLightDirections[2], viewDir);

    o.color = v.color.a;

    return o;
}

sampler2D _MainTex;
sampler2D _BumpSpecMap;
sampler2D _TranslucencyMap;
fixed4 _SpecColor;

void ApplyTreeLighting(inout half3 light, half3 albedo, half gloss, half specular, half3 lightColor, float2 param)
{
    half nl = param.r;
    light += albedo * lightColor * nl;

    float nh = param.g;
    float spec = pow (nh, specular) * gloss;
    light += lightColor * _SpecColor.rgb * spec;
}

fixed4 frag (v2f i) : SV_Target
{
    fixed3 albedo = tex2D (_MainTex, i.uv).rgb * i.color;
    half gloss = tex2D(_TranslucencyMap, i.uv).a;
    half specular = tex2D (_BumpSpecMap, i.uv).r * 128.0;

    half3 light = UNITY_LIGHTMODEL_AMBIENT * albedo;

    ApplyTreeLighting(light, albedo, gloss, specular, _TerrainTreeLightColors[0], i.params1);
    ApplyTreeLighting(light, albedo, gloss, specular, _TerrainTreeLightColors[1], i.params2);
    ApplyTreeLighting(light, albedo, gloss, specular, _TerrainTreeLightColors[2], i.params3);

    fixed4 c;
    c.rgb = light;
    c.a = 1.0;
    UNITY_OPAQUE_ALPHA(c.a);
    return c;
}
ENDCG
    }
}

FallBack Off
}
