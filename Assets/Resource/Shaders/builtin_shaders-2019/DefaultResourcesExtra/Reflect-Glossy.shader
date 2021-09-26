// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Reflective/Specular" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
    _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
    _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
    _Cube ("Reflection Cubemap", Cube) = "_Skybox" {}
}
SubShader {
    LOD 300
    Tags { "RenderType"="Opaque" }

CGPROGRAM
#pragma surface surf BlinnPhong

sampler2D _MainTex;
samplerCUBE _Cube;

fixed4 _Color;
fixed4 _ReflectColor;
half _Shininess;

struct Input {
    float2 uv_MainTex;
    float3 worldRefl;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    fixed4 c = tex * _Color;
    o.Albedo = c.rgb;
    o.Gloss = tex.a;
    o.Specular = _Shininess;

    fixed4 reflcol = texCUBE (_Cube, IN.worldRefl);
    reflcol *= tex.a;
    o.Emission = reflcol.rgb * _ReflectColor.rgb;
    o.Alpha = reflcol.a * _ReflectColor.a;
}
ENDCG
}

FallBack "Legacy Shaders/Reflective/VertexLit"
}
