// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/Lit/Bumped"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Specular ("Specular Color", Color) = (0,0,0,0)
        _MainTex ("Diffuse (RGB), Alpha (A)", 2D) = "white" {}
        [NoScaleOffset] _MainBump ("Diffuse Bump Map", 2D) = "bump" {}
        [PowerSlider(5.0)] _Shininess ("Shininess", Range(0.01, 1.0)) = 0.2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        LOD 400

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
            #pragma surface surf PPL alpha noshadow novertexlights nolightmap nofog vertex:vert

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                fixed4 color : COLOR;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Input
            {
                float2 uv_MainTex;
                fixed4 color : COLOR;
                float4 worldPosition;
            };

            sampler2D _MainTex;
            sampler2D _MainBump;

            fixed4 _Color;
            fixed4 _Specular;
            half _Shininess;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            void vert (inout appdata_t v, out Input o)
            {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                o.worldPosition = v.vertex;
                v.vertex = o.worldPosition;

                v.color = v.color * _Color;
            }

            void surf (Input IN, inout SurfaceOutput o)
            {
                fixed4 col = (tex2D(_MainTex, IN.uv_MainTex) + _TextureSampleAdd) * IN.color;
                half3 normal = UnpackNormal(tex2D(_MainBump, IN.uv_MainTex));

                o.Albedo = col.rgb;
                o.Normal = normalize(normal);
                o.Specular = _Specular.a;
                o.Gloss = _Shininess;
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
                half shininess = s.Gloss * 250.0 + 4.0;

            #ifndef USING_DIRECTIONAL_LIGHT
                lightDir = normalize(lightDir);
            #endif

                // Phong shading model
                half reflectiveFactor = max(0.0, dot(-viewDir, reflect(lightDir, s.Normal)));

                // Blinn-Phong shading model
                //half reflectiveFactor = max(0.0, dot(s.Normal, normalize(lightDir + viewDir)));

                half diffuseFactor = max(0.0, dot(s.Normal, lightDir));
                half specularFactor = pow(reflectiveFactor, shininess) * s.Specular;

                half4 c;
                c.rgb = (s.Albedo * diffuseFactor + _Specular.rgb * specularFactor) * _LightColor0.rgb;
                c.rgb *= atten;
                c.a = s.Alpha;
                return c;
            }
        ENDCG
    }
    Fallback "UI/Lit/Transparent"
}
