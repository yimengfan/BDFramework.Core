// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/VideoDecodeOSX"
{
    SubShader
    {
        Pass
        {
            Name "Flip_RGBARect_To_RGBA"
            ZTest Always Cull Off ZWrite Off Blend Off

            GLSLPROGRAM

            uniform sampler2DRect _MainTex;

            #ifdef VERTEX
            varying vec2 textureCoord;
            void main()
            {
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                textureCoord = vec2(gl_MultiTexCoord0.x, 1.0 - gl_MultiTexCoord0.y) * vec2(textureSize(_MainTex));
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
            void main()
            {
                gl_FragColor = AdjustForColorSpace(texture(_MainTex, textureCoord));
            }
            #endif

            ENDGLSL
        }

        Pass
        {
            Name "Flip_RGBASplitRect_To_RGBA"
            ZTest Always Cull Off ZWrite Off Blend Off
            GLSLPROGRAM

            uniform sampler2DRect _MainTex;

            #ifdef VERTEX
            varying vec3 textureCoordSplit;
            void main()
            {
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                textureCoordSplit.xz = vec2(0.5f * gl_MultiTexCoord0.x, 1.0 - gl_MultiTexCoord0.y);
                textureCoordSplit.y = textureCoordSplit.x + 0.5f;
                textureCoordSplit *= vec2(textureSize(_MainTex)).xxy;
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
            void main()
            {
                gl_FragColor = AdjustForColorSpace(texture(_MainTex, textureCoordSplit.xz));
                gl_FragColor.a = texture(_MainTex, textureCoordSplit.yz).g;
            }
            #endif

            ENDGLSL
        }
    }

    FallBack Off
}
