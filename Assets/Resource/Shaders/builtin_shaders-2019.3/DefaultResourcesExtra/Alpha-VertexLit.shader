// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Transparent/VertexLit" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Spec Color", Color) = (1,1,1,0)
    _Emission ("Emissive Color", Color) = (0,0,0,0)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.1, 1)) = 0.7
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100

    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha
    ColorMask RGB

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
            Combine texture * primary DOUBLE, texture * primary
        }
    }

    // Lightmapped
    Pass
    {
        Tags{ "LIGHTMODE" = "VertexLM" "QUEUE" = "Transparent" "IGNOREPROJECTOR" = "true" "RenderType" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB

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
            half4 color : COLOR;
            float3 uv1 : TEXCOORD1;
            float3 uv0 : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            fixed4 color : COLOR0;
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
            float2 uv2 : TEXCOORD2;
#if USING_FOG
            fixed fog : TEXCOORD3;
#endif
            float4 pos : SV_POSITION;

            UNITY_VERTEX_OUTPUT_STEREO
        };

        // vertex shader
        v2f vert(appdata IN)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            half4 color = IN.color;
            float3 eyePos = UnityObjectToViewPos(IN.pos);
            half3 viewDir = 0.0;
            o.color = saturate(color);

            o.uv0 = IN.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
            o.uv1 = IN.uv1.xy * unity_Lightmap_ST.xy + unity_Lightmap_ST.zw;
            o.uv2 = IN.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

#if USING_FOG
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
            fixed4 col, tex;
            tex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.uv0.xy);
            col = fixed4(DecodeLightmap(tex), 1);
            col = col * _Color;
            tex = tex2D(_MainTex, IN.uv2.xy);
            col.rgb = tex * col;
            col.a = tex.a * IN.color.a;

    #if USING_FOG
            col.rgb = lerp(unity_FogColor.rgb, col.rgb, IN.fog);
    #endif
            return col;
        }

        ENDCG
    }
}

}
