// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified SDF shader with SSD


Shader "Hidden/TextCore/Distance Field SSD"
{
    Properties
    {
        _FaceColor("Face Color", Color) = (1,1,1,1)
        _FaceDilate("Face Dilate", Range(-1,1)) = 0

        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness("Outline Softness", Range(0,1)) = 0

        _UnderlayColor("Border Color", Color) = (0,0,0,.5)
        _UnderlayOffsetX("Border OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY("Border OffsetY", Range(-1,1)) = 0
        _UnderlayDilate("Border Dilate", Range(-1,1)) = 0
        _UnderlaySoftness("Border Softness", Range(0,1)) = 0

        _WeightNormal("Weight Normal", float) = 0
        _WeightBold("Weight Bold", float) = .5

        _ShaderFlags("Flags", float) = 0
        _ScaleRatioA("Scale RatioA", float) = 1
        _ScaleRatioB("Scale RatioB", float) = 1
        _ScaleRatioC("Scale RatioC", float) = 1

        _MainTex("Font Atlas", 2D) = "white" {}
        _TextureWidth("Texture Width", float) = 512
        _TextureHeight("Texture Height", float) = 512
        _GradientScale("Gradient Scale", float) = 5
        _ScaleX("Scale X", float) = 1
        _ScaleY("Scale Y", float) = 1
        _Sharpness("Sharpness", Range(-1, 1)) = 0

        _VertexOffsetX("Vertex OffsetX", float) = 0
        _VertexOffsetY("Vertex OffsetY", float) = 0
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
            //#pragma shader_feature __ OUTLINE_ON
            //#pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

            #include "UnityCG.cginc"
            #include "TextCoreProperties.cginc"

            sampler2D _GUIClipTexture;
            uniform float4x4 unity_GUIClipTextureMatrix;

            struct vertex_t {
                float4  vertex          : POSITION;
                fixed4  color           : COLOR;
                float2  texcoord0       : TEXCOORD0;
                float2  texcoord1       : TEXCOORD1;
            };

            struct pixel_t {
                float4  vertex          : SV_POSITION;
                fixed4  faceColor       : COLOR;
                fixed4  outlineColor    : COLOR1;
                float2  texcoord0       : TEXCOORD0;    // Texture UV, Mask UV
                float2  clipUV          : TEXCOORD1;

                //#if (UNDERLAY_ON | UNDERLAY_INNER)
                //float2  texcoord1       : TEXCOORD2;    // Texture UV, alpha, reserved
                //fixed4  underlayColor   : TEXCOORD3;    // Scale(x), Bias(y)
                //#endif
            };


            pixel_t VertShader(vertex_t input)
            {
                float4 vert = input.vertex;
                vert.x += _VertexOffsetX;
                vert.y += _VertexOffsetY;
                float4 vPosition = UnityObjectToClipPos(vert);

                float opacity = input.color.a;
                //#if (UNDERLAY_ON | UNDERLAY_INNER)
                //    opacity = 1.0;
                //#endif

                fixed4 faceColor = fixed4(input.color.rgb, opacity) * _FaceColor;
                faceColor.rgb *= faceColor.a;

                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= opacity;
                outlineColor.rgb *= outlineColor.a;

                //#if (UNDERLAY_ON | UNDERLAY_INNER)
                //    float4 underlayColor = _UnderlayColor;
                //    underlayColor.a *= opacity;
                //    underlayColor.rgb *= underlayColor.a;
                //
                //    float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
                //    float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
                //    float2 underlayOffset = float2(x, y);
                //#endif

                // Generate UV for the Clip Texture
                float3 eyePos = UnityObjectToViewPos(input.vertex);
                float2 clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

                // Structure for pixel shader
                pixel_t output = {
                    vPosition,
                    faceColor,
                    outlineColor,
                    float2(input.texcoord0.x, input.texcoord0.y),
                    clipUV,
                    //#if (UNDERLAY_ON | UNDERLAY_INNER)
                    //    input.texcoord0 + underlayOffset,
                    //    underlayColor,
                    //#endif
                };

                return output;
            }

            half transition(half2 range, half distance)
            {
                return smoothstep(range.x, range.y, distance);
            }

            // PIXEL SHADER
            fixed4 PixShader(pixel_t input) : SV_Target
            {
                half distanceSample = tex2D(_MainTex, input.texcoord0).a;
                half smoothing = fwidth(distanceSample) * (1 - _Sharpness) + _OutlineSoftness * _ScaleRatioA;
                half contour = 0.5 - _FaceDilate * _ScaleRatioA * 0.5;
                half2 edgeRange = half2(contour - smoothing, contour + smoothing);

                half4 c = input.faceColor;

                //#ifdef OUTLINE_ON
                //    half halfOutlineSize = _OutlineWidth * _ScaleRatioC * 0.5;
                //    half2 faceToOutlineRange = edgeRange + halfOutlineSize;
                //    edgeRange -= halfOutlineSize;
                //
                //    half faceToOutlineTransition = transition(faceToOutlineRange, distanceSample);
                //    c = lerp(input.outlineColor, input.faceColor, faceToOutlineTransition);
                //#endif

                half edgeTransition = transition(edgeRange, distanceSample);
                c *= edgeTransition;

                //#if UNDERLAY_ON
                //    half underlayDistanceSample = tex2D(_MainTex, input.texcoord1).a;
                //    half underlaySmoothing = fwidth(underlayDistanceSample) * 0.5 + _UnderlaySoftness * _ScaleRatioC;
                //    half underlayContour = 0.5 - _UnderlayDilate * _ScaleRatioC * 0.5;
                //    half2 underlayEdgeRange = half2(underlayContour - underlaySmoothing, underlayContour + underlaySmoothing);
                //    half underlayEdgeTransition = transition(underlayEdgeRange, underlayDistanceSample);
                //
                //    c += input.underlayColor * (underlayEdgeTransition * (1 - c.a));
                //#endif

                c *= tex2D(_GUIClipTexture, input.clipUV).a;

                return c;
            }
            ENDCG
        }
    }
    //CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
