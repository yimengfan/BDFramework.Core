// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit shader. Simplest possible textured shader.
// - SUPPORTS lightmap
// - no lighting
// - no per-material color

Shader "Mobile/Unlit (Supports Lightmap)" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 100

    // Non-lightmapped
    Pass {
        Tags { "LightMode" = "Vertex" }
        Lighting Off
        SetTexture [_MainTex] {
            constantColor (1,1,1,1)
            combine texture, constant // UNITY_OPAQUE_ALPHA_FFP
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

        // uniforms
        float4 _MainTex_ST;

        // vertex shader input data
        struct appdata
        {
            float3 pos : POSITION;
            float3 uv1 : TEXCOORD1;
            float3 uv0 : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // vertex-to-fragment interpolators
        struct v2f
        {
            float2 uv0 : TEXCOORD0;
            float2 uv1 : TEXCOORD1;
#if USING_FOG
            fixed fog : TEXCOORD2;
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

            // compute texture coordinates
            o.uv0 = IN.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
            o.uv1 = IN.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

            // fog
#if USING_FOG
            float3 eyePos = UnityObjectToViewPos(float4(IN.pos, 1));
            float fogCoord = length(eyePos.xyz);  // radial fog distance
            UNITY_CALC_FOG_FACTOR_RAW(fogCoord);
            o.fog = saturate(unityFogFactor);
#endif

            // transform position
            o.pos = UnityObjectToClipPos(IN.pos);
            return o;
        }

        // textures
        sampler2D _MainTex;

        // fragment shader
        fixed4 frag(v2f IN) : SV_Target
        {
            fixed4 col, tex;

            // Fetch lightmap
            half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.uv0.xy);
            col.rgb = DecodeLightmap(bakedColorTex);

            // Fetch color texture
            tex = tex2D(_MainTex, IN.uv1.xy);
            col.rgb = tex.rgb * col.rgb;
            col.a = 1;

            // fog
    #if USING_FOG
            col.rgb = lerp(unity_FogColor.rgb, col.rgb, IN.fog);
    #endif
            return col;
        }

        ENDCG
    }
}
}
