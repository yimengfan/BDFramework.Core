// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/GenerateNormalmap" {
    Properties { _MainTex ("Texture", any) = "" {} }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment CalculateNormalSobel
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform float4 _TerrainNormalmapGenSize;    // (1.0f / (float)m_Width, 1.0f / (float)m_Height, 1.0f / hmScale.x, 1.0f / hmScale.z);
            uniform float4 _TerrainTilesScaleOffsets[9]; // ((65535.0f / kMaxHeight) * hmScale.y, terrainPosition.y, 0.0f, 0.0f)

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 bt   : TEXCOORD0;
                float4 lr   : TEXCOORD1;
                float4 blbr : TEXCOORD2;
                float4 tltr : TEXCOORD3;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.bt   = float4(v.texcoord.x, v.texcoord.y - _TerrainNormalmapGenSize.y, v.texcoord.x, v.texcoord.y + _TerrainNormalmapGenSize.y);
                o.lr   = float4(v.texcoord.x - _TerrainNormalmapGenSize.x, v.texcoord.y, v.texcoord.x + _TerrainNormalmapGenSize.x, v.texcoord.y);
                o.blbr = float4(v.texcoord.x - _TerrainNormalmapGenSize.x, v.texcoord.y - _TerrainNormalmapGenSize.y, v.texcoord.x + _TerrainNormalmapGenSize.x, v.texcoord.y - _TerrainNormalmapGenSize.y);
                o.tltr = float4(v.texcoord.x - _TerrainNormalmapGenSize.x, v.texcoord.y + _TerrainNormalmapGenSize.y, v.texcoord.x + _TerrainNormalmapGenSize.x, v.texcoord.y + _TerrainNormalmapGenSize.y);
                return o;
            }

            int ComputeTileIndex(float2 texcoord)
            {
                int2 idx = int2(floor(texcoord + 1.0f));
                return idx.y * 3 + idx.x;
            }

            float4 CalculateNormalSobel (v2f i) : SV_Target
            {
                float2 scaleOffsetB  = _TerrainTilesScaleOffsets[ComputeTileIndex(i.bt.xy)];
                float2 scaleOffsetT  = _TerrainTilesScaleOffsets[ComputeTileIndex(i.bt.zw)];
                float2 scaleOffsetL  = _TerrainTilesScaleOffsets[ComputeTileIndex(i.lr.xy)];
                float2 scaleOffsetR  = _TerrainTilesScaleOffsets[ComputeTileIndex(i.lr.zw)];
                float2 scaleOffsetBL = _TerrainTilesScaleOffsets[ComputeTileIndex(i.blbr.xy)];
                float2 scaleOffsetBR = _TerrainTilesScaleOffsets[ComputeTileIndex(i.blbr.zw)];
                float2 scaleOffsetTL = _TerrainTilesScaleOffsets[ComputeTileIndex(i.tltr.xy)];
                float2 scaleOffsetTR = _TerrainTilesScaleOffsets[ComputeTileIndex(i.tltr.zw)];

                float2 d;

                // Do X sobel filter
                d.x  = (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.blbr.xy, _MainTex))) * scaleOffsetBL.x + scaleOffsetBL.y) * -1.0F;
                d.x += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.lr.xy, _MainTex)))   * scaleOffsetL.x  + scaleOffsetL.y)  * -2.0F;
                d.x += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.tltr.xy, _MainTex))) * scaleOffsetTL.x + scaleOffsetTL.y) * -1.0F;
                d.x += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.blbr.zw, _MainTex))) * scaleOffsetBR.x + scaleOffsetBR.y) *  1.0F;
                d.x += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.lr.zw, _MainTex)))   * scaleOffsetR.x  + scaleOffsetR.y)  *  2.0F;
                d.x += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.tltr.zw, _MainTex))) * scaleOffsetTR.x + scaleOffsetTR.y) *  1.0F;

                // Do Y sobel filter
                d.y  = (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.blbr.xy, _MainTex))) * scaleOffsetBL.x + scaleOffsetBL.y) * -1.0F;
                d.y += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.bt.xy, _MainTex)))   * scaleOffsetB.x  + scaleOffsetB.y)  * -2.0F;
                d.y += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.blbr.zw, _MainTex))) * scaleOffsetBR.x + scaleOffsetBR.y) * -1.0F;
                d.y += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.tltr.xy, _MainTex))) * scaleOffsetTL.x + scaleOffsetTL.y) *  1.0F;
                d.y += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.bt.zw, _MainTex)))   * scaleOffsetT.x  + scaleOffsetT.y)  *  2.0F;
                d.y += (UnpackHeightmap(tex2D(_MainTex, TRANSFORM_TEX(i.tltr.zw, _MainTex))) * scaleOffsetTR.x + scaleOffsetTR.y) *  1.0F;
                d *= _TerrainNormalmapGenSize.zw;

                // Cross Product of components of gradient reduces to
                float3 normal = float3(-d.x, 8.0f, -d.y);
                return float4(0.5f + 0.5f * normalize(normal), 1.0f);
            }
            ENDCG
        }
    }
    Fallback Off
}
