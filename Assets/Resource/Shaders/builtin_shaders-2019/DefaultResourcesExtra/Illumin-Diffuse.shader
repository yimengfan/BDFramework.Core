// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Self-Illumin/Diffuse" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _Illum ("Illumin (A)", 2D) = "white" {}
    _Emission ("Emission (Lightmapper)", Float) = 1.0
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200

CGPROGRAM
#pragma surface surf Lambert

sampler2D _MainTex;
sampler2D _Illum;
fixed4 _Color;
fixed _Emission;

struct Input {
    float2 uv_MainTex;
    float2 uv_Illum;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    fixed4 c = tex * _Color;
    o.Albedo = c.rgb;
    o.Emission = c.rgb * tex2D(_Illum, IN.uv_Illum).a;
#if defined (UNITY_PASS_META)
    o.Emission *= _Emission.rrr;
#endif
    o.Alpha = c.a;
}
ENDCG
}
FallBack "Legacy Shaders/Self-Illumin/VertexLit"
CustomEditor "LegacyIlluminShaderGUI"
}
