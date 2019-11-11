// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/Lit/Refraction Detail"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Specular ("Specular Color", Color) = (0,0,0,0)
        _MainTex ("Diffuse (RGB), Alpha (A)", 2D) = "white" {}
        _MainBump ("Diffuse Bump Map", 2D) = "bump" {}
        _Mask ("Mask (Specularity, Shininess, Refraction)", 2D) = "white" {}
        _DetailTex ("Detail (RGB)", 2D) = "white" {}
        _DetailBump ("Detail Bump Map", 2D) = "bump" {}
        _DetailMask ("Detail Mask (Spec, Shin, Ref)", 2D) = "white" {}
        [PowerSlider(5.0)] _Shininess ("Shininess", Range(0.01, 1.0)) = 0.2
        _Focus ("Focus", Range(-100.0, 100.0)) = -100.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    // SM 3.0
    SubShader
    {
        LOD 400

        GrabPass
        {
            Name "BASE"
            Tags { "LightMode" = "Always" }
        }

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType"="Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        CGPROGRAM
            #pragma target 3.0
            #pragma surface surf PPL alpha noshadow novertexlights nolightmap vertex:vert nofog

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord1 : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                fixed4 color : COLOR;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Input
            {
                float4 texcoord1;
                float4 texcoord2;
                float2 texcoord3;
                float4 proj;
                fixed4 color : COLOR;
                float4 worldPosition;
            };

            sampler2D _GrabTexture;
            sampler2D _MainTex;
            sampler2D _MainBump;
            sampler2D _Mask;
            sampler2D _DetailTex;
            sampler2D _DetailBump;
            sampler2D _DetailMask;

            float4 _MainTex_ST;
            float4 _MainBump_ST;
            float4 _Mask_ST;
            float4 _DetailTex_ST;
            float4 _DetailBump_ST;
            float4 _DetailMask_ST;
            float4 _DetailTex_TexelSize;
            float4 _DetailBump_TexelSize;
            float4 _DetailMask_TexelSize;
            half4 _GrabTexture_TexelSize;

            fixed4 _Color;
            fixed4 _Specular;
            half _Shininess;
            half _Focus;

            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            void vert (inout appdata_t v, out Input o)
            {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                o.worldPosition = v.vertex;
                v.vertex = o.worldPosition;
                v.color = v.color * _Color;

                o.texcoord1.xy  = TRANSFORM_TEX(v.texcoord1, _MainTex);
                o.texcoord1.zw  = TRANSFORM_TEX(v.texcoord1, _MainBump);
                o.texcoord2.xy  = TRANSFORM_TEX(v.texcoord2 * _DetailTex_TexelSize.xy, _DetailTex);
                o.texcoord2.zw  = TRANSFORM_TEX(v.texcoord2 * _DetailBump_TexelSize.xy, _DetailBump);
                o.texcoord3     = TRANSFORM_TEX(v.texcoord2 * _DetailMask_TexelSize.xy, _DetailMask);

            #if UNITY_UV_STARTS_AT_TOP
                o.proj.xy = (float2(v.vertex.x, -v.vertex.y) + v.vertex.w) * 0.5;
            #else
                o.proj.xy = (float2(v.vertex.x, v.vertex.y) + v.vertex.w) * 0.5;
            #endif
                o.proj.zw = v.vertex.zw;
            }

            void surf (Input IN, inout SurfaceOutput o)
            {
                fixed4 col = tex2D(_MainTex, IN.texcoord1.xy) + _TextureSampleAdd;
                fixed4 detail = tex2D(_DetailTex, IN.texcoord2.xy);

                half3 normal = UnpackNormal(tex2D(_MainBump, IN.texcoord1.zw)) +
                               UnpackNormal(tex2D(_DetailBump, IN.texcoord2.zw));

                half3 mask = tex2D(_Mask, IN.texcoord1.xy) *
                             tex2D(_DetailMask, IN.texcoord3);

                float2 offset = normal.xy * _GrabTexture_TexelSize.xy * _Focus;
                IN.proj.xy = offset * IN.proj.z + IN.proj.xy;
                half4 ref = tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(IN.proj));

                col.rgb = lerp(col.rgb, ref.rgb, mask.b);
                col.rgb = lerp(col.rgb, col.rgb * detail.rgb, detail.a);
                col *= IN.color;

                o.Albedo = col.rgb;
                o.Normal = normalize(normal);
                o.Specular = _Specular.a * mask.r;
                o.Gloss = _Shininess * mask.g;
                o.Alpha = col.a;

                #ifdef UNITY_UI_CLIP_RECT
                o.Alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (o.Alpha - 0.001);
                #endif
            }

            half4 LightingPPL (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
            {
                half3 nNormal = normalize(s.Normal);
                half shininess = s.Gloss * 250.0 + 4.0;

            #ifndef USING_DIRECTIONAL_LIGHT
                lightDir = normalize(lightDir);
            #endif

                // Phong shading model
                half reflectiveFactor = max(0.0, dot(-viewDir, reflect(lightDir, nNormal)));

                // Blinn-Phong shading model
                //half reflectiveFactor = max(0.0, dot(nNormal, normalize(lightDir + viewDir)));

                half diffuseFactor = max(0.0, dot(nNormal, lightDir));
                half specularFactor = pow(reflectiveFactor, shininess) * s.Specular;

                half4 c;
                c.rgb = (s.Albedo * diffuseFactor + _Specular.rgb * specularFactor) * _LightColor0.rgb;
                c.rgb *= atten;
                c.a = s.Alpha;
                return c;
            }
        ENDCG
    }
    Fallback "UI/Lit/Detail"
}
