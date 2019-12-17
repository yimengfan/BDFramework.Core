// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Reflective/Bumped VertexLit" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Spec Color", Color) = (1,1,1,1)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.1, 1)) = 0.7
    _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
    _MainTex ("Base (RGB) RefStrength (A)", 2D) = "white" {}
    _Cube ("Reflection Cubemap", Cube) = "" {}
    _BumpMap ("Normalmap", 2D) = "bump" {}
}

Category {
    Tags { "RenderType"="Opaque" }
    LOD 250
    SubShader {
        UsePass "Reflective/Bumped Unlit/BASE"

        Pass {
            Tags { "LightMode" = "Vertex" }
            Blend One One ZWrite Off
            Lighting On

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog

#include "UnityCG.cginc"

struct v2f {
    float2 uv : TEXCOORD0;
    UNITY_FOG_COORDS(1)
    fixed4 diff : COLOR0;
    float4 pos : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

uniform float4 _MainTex_ST;
uniform float4 _Color;

v2f vert (appdata_base v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
    float4 lighting = float4(ShadeVertexLightsFull(v.vertex, v.normal, 4, true),_Color.w);
    o.diff = lighting * _Color;
    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}

uniform sampler2D _MainTex;

fixed4 frag (v2f i) : SV_Target
{
    fixed4 temp = tex2D (_MainTex, i.uv);
    fixed4 c;
    c.xyz = (temp.xyz * i.diff.xyz);
    c.w = temp.w * i.diff.w;
    UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0)); // fog towards black due to our blend mode
    UNITY_OPAQUE_ALPHA(c.a);
    return c;
}
ENDCG

        }
    }
}

FallBack "Legacy Shaders/Reflective/VertexLit"
}
