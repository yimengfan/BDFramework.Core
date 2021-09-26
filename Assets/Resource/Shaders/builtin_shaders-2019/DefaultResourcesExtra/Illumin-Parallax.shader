// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Self-Illumin/Parallax Diffuse" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _Parallax ("Height", Range (0.005, 0.08)) = 0.02
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _Illum ("Illumin (A)", 2D) = "white" {}
    _BumpMap ("Normalmap", 2D) = "bump" {}
    _ParallaxMap ("Heightmap (A)", 2D) = "black" {}
    _Emission ("Emission (Lightmapper)", Float) = 1.0
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 500

CGPROGRAM
#pragma surface surf Lambert
#pragma target 3.0

sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _ParallaxMap;
sampler2D _Illum;
fixed4 _Color;
float _Parallax;
fixed _Emission;

struct Input {
    float2 uv_MainTex;
    float2 uv_BumpMap;
    float2 uv_Illum;
    float3 viewDir;
};

void surf (Input IN, inout SurfaceOutput o) {
    half h = tex2D (_ParallaxMap, IN.uv_BumpMap).w;
    float2 offset = ParallaxOffset (h, _Parallax, IN.viewDir);
    IN.uv_MainTex += offset;
    IN.uv_BumpMap += offset;
    IN.uv_Illum += offset;

    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    o.Albedo = c.rgb;
    o.Emission = c.rgb * tex2D(_Illum, IN.uv_Illum).a;
#if defined (UNITY_PASS_META)
    o.Emission *= _Emission.rrr;
#endif
    o.Alpha = c.a;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
}
ENDCG
}
FallBack "Legacy Shaders/Self-Illumin/Bumped Diffuse"
CustomEditor "LegacyIlluminShaderGUI"

}
