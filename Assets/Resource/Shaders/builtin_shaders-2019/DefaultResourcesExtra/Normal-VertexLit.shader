// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/VertexLit" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Spec Color", Color) = (1,1,1,1)
    _Emission ("Emissive Color", Color) = (0,0,0,0)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.01, 1)) = 0.7
    _MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 100

    // Non-lightmapped
    Pass {
        Tags { "LightMode" = "Vertex" }

        Material {
            Diffuse [_Color]
            Ambient [_Color]
            Shininess [_Shininess]
            Specular [_SpecColor]
            Emission [_Emission]
        }
        Lighting On
        SeparateSpecular On
        SetTexture [_MainTex] {
            constantColor (1,1,1,1)
            Combine texture * primary DOUBLE, constant // UNITY_OPAQUE_ALPHA_FFP
        }
    }

    // Lightmapped
    Pass
    {
        Tags{ "LIGHTMODE" = "VertexLM" "RenderType" = "Opaque" }

        CGPROGRAM

        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        #include "UnityCG.cginc"
        #pragma multi_compile_fog
        #define USING_FOG (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))

        float4 unity_Lightmap_ST;
        float4 _MainTex_ST;

        struct appdata
        {
            float3 pos : POSITION;
            float3 uv1 : TEXCOORD1;
            float3 uv0 : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float2 uv2 : TEXCOORD2;
#if USING_FOG
            fixed fog : TEXCOORD3;
#endif
            float4 pos : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert(appdata IN)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.uv0 = IN.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
            o.uv1 = IN.uv1.xy * unity_Lightmap_ST.xy + unity_Lightmap_ST.zw;
            o.uv2 = IN.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

#if USING_FOG
            float3 eyePos = UnityObjectToViewPos(IN.pos);
            float fogCoord = length(eyePos.xyz);
            UNITY_CALC_FOG_FACTOR_RAW(fogCoord);
            o.fog = saturate(unityFogFactor);
#endif

            o.pos = UnityObjectToClipPos(IN.pos);
            return o;
        }

        sampler2D _MainTex;
        fixed4 _Color;

        fixed4 frag(v2f IN) : SV_Target
        {
            fixed4 col;

            fixed4 tex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.uv0.xy);
            half4 bakedColor = half4(DecodeLightmap(tex), 1.0);

            col = bakedColor * _Color;

            tex = tex2D(_MainTex, IN.uv2.xy);
            col.rgb = tex.rgb * col.rgb;

            col.a = 1.0f;

            #if USING_FOG
                    col.rgb = lerp(unity_FogColor.rgb, col.rgb, IN.fog);
            #endif

            return col;
        }

        ENDCG
    }

    // Pass to render object as a shadow caster
    Pass {
        Name "ShadowCaster"
        Tags { "LightMode" = "ShadowCaster" }

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 2.0
#pragma multi_compile_shadowcaster
#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
#include "UnityCG.cginc"

struct v2f {
    V2F_SHADOW_CASTER;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert( appdata_base v )
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
    return o;
}

float4 frag( v2f i ) : SV_Target
{
    SHADOW_CASTER_FRAGMENT(i)
}
ENDCG

    }

}

}
