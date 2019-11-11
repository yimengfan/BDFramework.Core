// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/VideoDecodeAndroid"
{
GLSLINCLUDE
    #include "UnityCG.glslinc"
ENDGLSL

    SubShader
    {
        Pass
        {
            Name "RGBAExternal_To_RGBA"
            ZTest Always Cull Off ZWrite Off Blend Off

            GLSLPROGRAM

            #extension GL_OES_EGL_image_external : require
            #pragma glsl_es2

            uniform vec4 _MainTex_ST;

            #ifdef VERTEX

            varying vec2 textureCoord;
            void main()
            {
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                textureCoord = TRANSFORM_TEX_ST(gl_MultiTexCoord0, _MainTex_ST);
            }

            #endif

            #ifdef FRAGMENT

            vec4 AdjustForColorSpace(vec4 color)
            {
            #ifdef UNITY_COLORSPACE_GAMMA
                return color;
            #else
                // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
                vec3 sRGB = color.rgb;
                return vec4(sRGB * (sRGB * (sRGB * 0.305306011 + 0.682171111) + 0.012522878), color.a);
            #endif
            }

            varying vec2 textureCoord;
            uniform samplerExternalOES _MainTex;
            void main()
            {
                gl_FragColor = AdjustForColorSpace(textureExternal(_MainTex, textureCoord));
            }

            #endif

            ENDGLSL
        }

        Pass
        {
            Name "RGBASplitExternal_To_RGBA"
            ZTest Always Cull Off ZWrite Off Blend Off

            GLSLPROGRAM

            #extension GL_OES_EGL_image_external : require
            #pragma glsl_es2
            uniform vec4 _MainTex_ST;

            #ifdef VERTEX

            varying vec3 textureCoordSplit;
            void main()
            {
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                textureCoordSplit.xz = vec2(0.5 * gl_MultiTexCoord0.x * _MainTex_ST.x, gl_MultiTexCoord0.y * _MainTex_ST.y) + _MainTex_ST.zw;
                textureCoordSplit.y = textureCoordSplit.x + 0.5 * _MainTex_ST.x;
            }

            #endif

            #ifdef FRAGMENT

            vec4 AdjustForColorSpace(vec4 color)
            {
            #ifdef UNITY_COLORSPACE_GAMMA
                return color;
            #else
                // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
                vec3 sRGB = color.rgb;
                return vec4(sRGB * (sRGB * (sRGB * 0.305306011 + 0.682171111) + 0.012522878), color.a);
            #endif
            }

            varying vec3 textureCoordSplit;
            uniform samplerExternalOES _MainTex;
            void main()
            {
                gl_FragColor.rgb = AdjustForColorSpace(textureExternal(_MainTex, textureCoordSplit.xz)).rgb;
                gl_FragColor.a   = textureExternal(_MainTex, textureCoordSplit.yz).g;
            }

            #endif

            ENDGLSL
        }
    }

    FallBack Off
}
