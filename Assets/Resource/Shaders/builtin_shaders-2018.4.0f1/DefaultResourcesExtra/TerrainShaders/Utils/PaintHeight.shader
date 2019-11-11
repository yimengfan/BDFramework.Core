// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/PaintHeight" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always Cull Off ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"
            #include "TerrainTool.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

            sampler2D _BrushTex;

            float4 _BrushParams;
            #define BRUSH_STRENGTH      (_BrushParams[0])
            #define BRUSH_TARGETHEIGHT  (_BrushParams[1])

            struct appdata_t {
                float4 vertex : POSITION;
                float2 pcUV : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 pcUV : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pcUV = v.pcUV;
                return o;
            }

            float ApplyBrush(float height, float brushStrength)
            {
                float targetHeight = BRUSH_TARGETHEIGHT;
                if (targetHeight > height)
                {
                    height += brushStrength;
                    height = height < targetHeight ? height : targetHeight;
                }
                else
                {
                    height -= brushStrength;
                    height = height > targetHeight ? height : targetHeight;
                }
                return height;
            }

        ENDCG

        Pass    // 0 raise/lower heights
        {
            Name "Raise/Lower Heights"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment RaiseHeight

            float4 RaiseHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                float brushShape = oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));

                return PackHeightmap(clamp(height + BRUSH_STRENGTH * brushShape, 0, 0.5f));
            }
            ENDCG
        }

        Pass    // 1 stamp heights
        {
            Name "Stamp Heights"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment StampHeight

            #define BRUSH_OPACITY       (_BrushParams[0])
            #define BRUSH_STAMPHEIGHT   (_BrushParams[2])
            #define BRUSH_MAXBLENDADD   (_BrushParams[3])

            float SmoothMax(float a, float b, float p)
            {
                // calculates a smooth maximum of a and b, using an intersection power p
                // higher powers produce sharper intersections, approaching max()
                return log2(exp2(a * p) + exp2(b * p) - 1.0f) / p;
            }

            float4 StampHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                float brushShape = oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
                float brushHeight = brushShape * BRUSH_STAMPHEIGHT;

                float targetHeight;
                if (BRUSH_MAXBLENDADD > 0.0f)
                {
                    float brushIntersection = saturate(1.0f - BRUSH_MAXBLENDADD);
                    float brushSmooth = exp2(brushIntersection * 8.0f);
                    targetHeight = SmoothMax(height, brushHeight, brushSmooth);
                }
                else
                {
                    targetHeight = max(height, brushHeight);
                }
                targetHeight = clamp(targetHeight, 0.0f, 0.5f);          // Keep in valid range (0..0.5f)

                height = lerp(height, targetHeight, BRUSH_OPACITY);
                return PackHeightmap(height);
            }
            ENDCG
        }

        Pass    // 2 set height (flatten)
        {
            Name "Set Heights"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SetHeight

            float4 SetHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));

                // smooth set
                float targetHeight = BRUSH_TARGETHEIGHT;

                // have to do this check to ensure strength 0 == no change (code below makes a super tiny change even with strength 0)
                if (brushStrength > 0.0f)
                {
                    float deltaHeight = height - targetHeight;

                    // see https://www.desmos.com/calculator/880ka3lfkl
                    float p = saturate(brushStrength);
                    float w = (1.0f - p) / (p + 0.000001f);
                    //                  float w = (1.0f - p*p) / (p + 0.000001f);       // alternative TODO test and compare
                    float fx = clamp(w * deltaHeight, -1.0f, 1.0f);
                    float g = fx * (0.5f * fx * sign(fx) - 1.0f);

                    deltaHeight = deltaHeight + g / w;

                    height = targetHeight + deltaHeight;
                }

                return PackHeightmap(height);
            }
            ENDCG
        }

        Pass    // 3 smooth terrain
        {
            Name "Smooth Heights"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SmoothHeight

            float4 SmoothHeight(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);
                float2 heightmapUV = PaintContextUVToHeightmapUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float height = UnpackHeightmap(tex2D(_MainTex, heightmapUV));
                float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));

                float h = 0.0F;
                float xoffset = _MainTex_TexelSize.x;
                float yoffset = _MainTex_TexelSize.y;

                // 3*3 filter
                h += height;
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset,  0      )));
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset,  0      )));
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset,  yoffset))) * 0.75F;
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset,  yoffset))) * 0.75F;
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( xoffset, -yoffset))) * 0.75F;
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2(-xoffset, -yoffset))) * 0.75F;
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( 0,        yoffset)));
                h += UnpackHeightmap(tex2D(_MainTex, heightmapUV + float2( 0,       -yoffset)));
                h /= 8.0F;

                return PackHeightmap(lerp(height, h, brushStrength));
            }
            ENDCG
        }

        Pass    // 4 paint splat alphamap
        {
            Name "Paint Texture"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment PaintSplatAlphamap

            float4 PaintSplatAlphamap(v2f i) : SV_Target
            {
                float2 brushUV = PaintContextUVToBrushUV(i.pcUV);

                // out of bounds multiplier
                float oob = all(saturate(brushUV) == brushUV) ? 1.0f : 0.0f;

                float brushStrength = BRUSH_STRENGTH * oob * UnpackHeightmap(tex2D(_BrushTex, brushUV));
                float alphaMap = tex2D(_MainTex, i.pcUV).r;
                return ApplyBrush(alphaMap, brushStrength);
            }

            ENDCG
        }

    }
    Fallback Off

}
