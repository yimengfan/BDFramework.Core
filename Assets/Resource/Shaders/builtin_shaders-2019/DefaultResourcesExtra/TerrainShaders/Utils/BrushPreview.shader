// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/BrushPreview"
{
    SubShader
    {
        ZTest Always Cull Back ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGINCLUDE
            // Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
            #pragma exclude_renderers gles

            #include "UnityCG.cginc"
            #include "TerrainPreview.cginc"

            sampler2D _BrushTex;

        ENDCG

        Pass    // 0
        {
            Name "TerrainPreviewProcedural"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct v2f {
                float4 clipPosition : SV_POSITION;
                float3 positionWorld : TEXCOORD0;
                float3 positionWorldOrig : TEXCOORD1;
                float2 pcPixels : TEXCOORD2;
                float2 brushUV : TEXCOORD3;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                // build a quad mesh, with one vertex per paint context pixel (pcPixel)
                float2 pcPixels = BuildProceduralQuadMeshVertex(vid);

                // compute heightmap UV and sample heightmap
                float2 heightmapUV = PaintContextPixelsToHeightmapUV(pcPixels);
                float heightmapSample = UnpackHeightmap(tex2Dlod(_Heightmap, float4(heightmapUV, 0, 0)));

                // compute brush UV
                float2 brushUV = PaintContextPixelsToBrushUV(pcPixels);

                // compute object position (in terrain space) and world position
                float3 positionObject = PaintContextPixelsToObjectPosition(pcPixels, heightmapSample);
                float3 positionWorld = TerrainObjectToWorldPosition(positionObject);

                v2f o;
                o.pcPixels = pcPixels;
                o.positionWorld = positionWorld;
                o.positionWorldOrig = positionWorld;
                o.clipPosition = UnityWorldToClipPos(positionWorld);
                o.brushUV = brushUV;
                return o;
            }


            float4 frag(v2f i) : SV_Target
            {
                float brushSample = UnpackHeightmap(tex2D(_BrushTex, i.brushUV));

                // out of bounds multiplier
                float oob = all(saturate(i.brushUV) == i.brushUV) ? 1.0f : 0.0f;

                // brush outline stripe
                float stripeWidth = 2.0f;       // pixels
                float stripeLocation = 0.2f;    // at 20% alpha
                float brushStripe = Stripe(brushSample, stripeLocation, stripeWidth);

                float4 color = float4(0.5f, 0.5f, 1.0f, 1.0f) * saturate(brushStripe + 0.5f * brushSample);
                color.a = 0.6f * saturate(brushSample * 5.0f);
                return color * oob;
            }
            ENDCG
        }

        Pass    // 1
        {
            Name "TerrainPreviewProceduralDeltaNormals"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _HeightmapOrig;

            struct v2f {
                float4 clipPosition : SV_POSITION;
                float3 positionWorld : TEXCOORD0;
                float3 positionWorldOrig : TEXCOORD1;
                float2 pcPixels : TEXCOORD2;
                float2 brushUV : TEXCOORD3;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                // build a quad mesh, with one vertex per paint context pixel (pcPixel)
                float2 pcPixels = BuildProceduralQuadMeshVertex(vid);

                // compute heightmap UV and sample heightmap
                float2 heightmapUV = PaintContextPixelsToHeightmapUV(pcPixels);
                float heightmapSample = UnpackHeightmap(tex2Dlod(_Heightmap, float4(heightmapUV, 0, 0)));
                float heightmapSampleOrig = UnpackHeightmap(tex2Dlod(_HeightmapOrig, float4(heightmapUV, 0, 0)));

                // compute brush UV
                float2 brushUV = PaintContextPixelsToBrushUV(pcPixels);

                // compute object position (in terrain space) and world position
                float3 positionObject = PaintContextPixelsToObjectPosition(pcPixels, heightmapSample);
                float3 positionWorld = TerrainObjectToWorldPosition(positionObject);

                float3 positionObjectOrig = PaintContextPixelsToObjectPosition(pcPixels, heightmapSampleOrig);
                float3 positionWorldOrig = TerrainObjectToWorldPosition(positionObjectOrig);

                v2f o;
                o.pcPixels = pcPixels;
                o.positionWorld = positionWorld;
                o.positionWorldOrig = positionWorldOrig;
                o.clipPosition = UnityWorldToClipPos(positionWorld);
                o.brushUV = brushUV;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float brushSample = UnpackHeightmap(tex2D(_BrushTex, i.brushUV));

                // out of bounds multiplier
                float oob = all(saturate(i.brushUV) == i.brushUV) ? 1.0f : 0.0f;

                float deltaHeight = abs(i.positionWorld.y - i.positionWorldOrig.y);

                // normal based coloring
                float3 dx = ddx(i.positionWorld);
                float3 dy = ddy(i.positionWorld);
                float3 normal = normalize(cross(dy, dx));

                float3 lightDir = UnityWorldSpaceLightDir(i.positionWorld.xyz);

                float4 color;
                color.rgb = saturate(normal.xzy * float3(-0.5f, -0.5f, 0.5f) + 0.5f);
                color.rgb = lerp(color.rgb, float3(1.0f, 1.0f, 1.0f), 0.3f);
                color.rgb = color.rgb * (0.1f + 0.9f * dot(lightDir, normal));
                color.a = 0.9f * saturate(0.2f * deltaHeight);

                return color;
            }
            ENDCG
        }

        Pass    // 2
        {
            Name "TerrainPreviewProceduralDeltaStripes"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _HeightmapOrig;

            struct v2f {
                float4 clipPosition : SV_POSITION;
                float3 positionWorld : TEXCOORD0;
                float3 positionWorldOrig : TEXCOORD1;
                float2 pcPixels : TEXCOORD2;
                float2 brushUV : TEXCOORD3;
            };

            v2f vert(uint vid : SV_VertexID)
            {
                // build a quad mesh, with one vertex per paint context pixel (pcPixel)
                float2 pcPixels = BuildProceduralQuadMeshVertex(vid);

                // compute heightmap UV and sample heightmap
                float2 heightmapUV = PaintContextPixelsToHeightmapUV(pcPixels);
                float heightmapSample = UnpackHeightmap(tex2Dlod(_Heightmap, float4(heightmapUV, 0, 0)));
                float heightmapSampleOrig = UnpackHeightmap(tex2Dlod(_HeightmapOrig, float4(heightmapUV, 0, 0)));

                // compute brush UV
                float2 brushUV = PaintContextPixelsToBrushUV(pcPixels);

                // compute object position (in terrain space) and world position
                float3 positionObject = PaintContextPixelsToObjectPosition(pcPixels, heightmapSample);
                float3 positionWorld = TerrainObjectToWorldPosition(positionObject);

                float3 positionObjectOrig = PaintContextPixelsToObjectPosition(pcPixels, heightmapSampleOrig);
                float3 positionWorldOrig = TerrainObjectToWorldPosition(positionObjectOrig);

                v2f o;
                o.pcPixels = pcPixels;
                o.positionWorld = positionWorld;
                o.positionWorldOrig = positionWorldOrig;
                o.clipPosition = UnityWorldToClipPos(positionWorld);
                o.brushUV = brushUV;
                return o;
            }

            float MultiStripes(in float x, in float freq1, in float freq2)
            {
                float2 derivatives = float2(ddx(x), ddy(x));
                float derivLen = length(derivatives);

                float tweak = 0.5;
                float sharpen = tweak / max(derivLen, 0.00001f);

                float triwave1 = abs(frac(x * freq1) - 0.5f) - 0.25f;
                float triwave2 = abs(frac(x * freq2) - 0.5f) - 0.25f;

                float width = 0.95f;

                float result1 = saturate((triwave1 - width * 0.25f) * sharpen / freq1 + 0.75f);
                float result2 = saturate((triwave2 - width * 0.25f) * sharpen / freq2 + 0.75f);

                return max(result1, result2);
            }

            float4 frag(v2f i) : SV_Target
            {
                float brushSample = UnpackHeightmap(tex2D(_BrushTex, i.brushUV));

                // out of bounds multiplier
                float oob = all(saturate(i.brushUV) == i.brushUV) ? 1.0f : 0.0f;

                // brush outline stripe
                float stripeWidth = 1.0f;       // pixels
                float stripeLocation = 0.2f;    // at 20% alpha
                float heightStripes = MultiStripes(i.positionWorld.y + 0.25f, 0.0625f, 1.0f);
                float xStripes = MultiStripes(i.positionWorld.x, 0.03125f, 0.5f);
                float zStripes = MultiStripes(i.positionWorld.z, 0.03125f, 0.5f);

                float deltaHeight = saturate(abs(i.positionWorld.y - i.positionWorldOrig.y));
                float4 color = lerp(float4(0.5f, 0.5f, 1.0f, 1.0f), float4(0.5f, 1.0f, 0.5f, 1.0f), deltaHeight);
                return color * lerp(deltaHeight * 0.5f, (brushSample > 0.0f ? 1.0f : 0.0f), 0.5f * saturate(heightStripes + xStripes + zStripes));
            }
            ENDCG
        }
    }
    Fallback Off
}
