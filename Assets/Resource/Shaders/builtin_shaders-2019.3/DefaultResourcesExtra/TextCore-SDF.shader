// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Internal SDF shader
// Requires SDF scale to be passed in texcoord1.y


Shader "Hidden/TextCore/Distance Field"
{
    Properties
    {
        _FaceColor          ("Face Color", Color) = (1,1,1,1)
        _FaceDilate         ("Face Dilate", Range(-1,1)) = 0

        _OutlineColor       ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth       ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness    ("Outline Softness", Range(0,1)) = 0

        _WeightNormal       ("Weight Normal", float) = 0
        _WeightBold         ("Weight Bold", float) = .5

        _ScaleRatioA        ("Scale RatioA", float) = 1
        _ScaleRatioB        ("Scale RatioB", float) = 1
        _ScaleRatioC        ("Scale RatioC", float) = 1

        _MainTex            ("Font Atlas", 2D) = "white" {}
        _GradientScale      ("Gradient Scale", float) = 10
        _ScaleX             ("Scale X", float) = 1
        _ScaleY             ("Scale Y", float) = 1
        _Sharpness          ("Sharpness", Range(-1, 1)) = 0

        _VertexOffsetX      ("Vertex OffsetX", float) = 0
        _VertexOffsetY      ("Vertex OffsetY", float) = 0
    }

    SubShader
    {
        Tags
        {
            "ForceSupported" = "True"
        }

        Lighting Off
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex VertShader
            #pragma fragment PixShader

            #include "UnityCG.cginc"
            #include "TextCoreProperties.cginc"

            sampler2D _GUIClipTexture;
            uniform float4x4 unity_GUIClipTextureMatrix;

            struct vertex_t
            {
                float4  vertex          : POSITION;
                fixed4  color           : COLOR;
                float2  texcoord0       : TEXCOORD0;
                float2  texcoord1       : TEXCOORD1;
            };

            struct pixel_t
            {
                float4  vertex          : SV_POSITION;
                fixed4  faceColor       : COLOR;
                fixed4  outlineColor    : COLOR1;
                float2  texcoord0       : TEXCOORD0;    // Texture UV
                half4   param           : TEXCOORD1;    // Scale(x), BiasIn(y), BiasOut(z), Bias(w)
                float2  clipUV          : TEXCOORD2;     // Position in clip space
            };

            pixel_t VertShader(vertex_t input)
            {
                float bold = step(input.texcoord1.y, 0);

                float4 vert = input.vertex;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;
                float4 vPosition = UnityObjectToClipPos(vert);

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy)); // Verify this remains valid in the Editor

                float scale = rsqrt(dot(pixelSize, pixelSize));
                scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);

                float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
                weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

                float layerScale = scale;

                scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
                float bias = (0.5 - weight) * scale - 0.5;
                float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

                float opacity = input.color.a;

                fixed4 faceColor = fixed4(input.color.rgb, opacity) * _FaceColor;
                faceColor.rgb *= faceColor.a;

                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= opacity;
                outlineColor.rgb *= outlineColor.a;
                outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));

                // Generate UV for the Clip Texture
                float3 eyePos = UnityObjectToViewPos(input.vertex);
                float2 clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

                // Structure for pixel shader
                pixel_t output = {
                    vPosition,
                    faceColor,
                    outlineColor,
                    input.texcoord0,
                    half4(scale, bias - outline, bias + outline, bias),
                    clipUV
                };

                return output;
            }


            // PIXEL SHADER
            fixed4 PixShader(pixel_t input) : SV_Target
            {
                half d = tex2D(_MainTex, input.texcoord0).a * input.param.x;
                half4 c = input.faceColor * saturate(d - input.param.w);

                c = lerp(input.outlineColor, input.faceColor, saturate(d - input.param.z));
                c *= saturate(d - input.param.y);

                c *= tex2D(_GUIClipTexture, input.clipUV).a;

                return c;
            }
            ENDCG
        }
    }
}
