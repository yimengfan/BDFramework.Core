// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/TerrainLayerUtils" {

    Properties { _MainTex ("Texture", any) = "" {} }

    SubShader {

        ZTest Always
        Cull Off
        ZWrite Off

        CGINCLUDE

            #include "UnityCG.cginc"

            float4 _LayerMask;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;      // 1/width, 1/height, width, height

        ENDCG

        Pass    // Select one channel and copy it into R channel
        {
            Name "Get Terrain Layer Channel"

            BlendOp Max

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment GetLayer

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }
            float4 GetLayer(v2f i) : SV_Target
            {
                float4 layerWeights = tex2D(_MainTex, i.texcoord);
                return dot(layerWeights, _LayerMask);
            }
            ENDCG
        }

        Pass    // Copy the R channel of the input into a specific channel in the output, and renormalize the other channels
        {
            Name "Set Terrain Layer Channel"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment SetLayer

            sampler2D _AlphaMapTexture;         // Terrain space texture -- current splatmap we are modifying
            sampler2D _OldAlphaMapTexture;      // PaintContext space texture -- contains target channel (PaintContext.source)

            sampler2D _OriginalTargetAlphaMap;  // Terrain space texture, contains target channel
            float4 _OriginalTargetAlphaMask;    // mask for above, identifying target channel

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uvPC : TEXCOORD0;
                float2 uvTerrain : TEXCOORD1;
            };


            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uvPC : TEXCOORD0;
                float2 uvTerrain : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvPC = v.uvPC;
                o.uvTerrain = v.uvTerrain;
                return o;
            }

            float4 SetLayer(v2f i) : SV_Target
            {
                // alpha map we are modifying -- _LayerMask tells us which channel is the target (set to 1.0), non-targets are 0.0
                // Note: all four channels can be non-targets, as the target may be in a different alpha map texture
                float4 origAlphaMap = tex2D(_AlphaMapTexture, i.uvTerrain);

                // old alpha of the target channel (according to the current terrain tile)
                float origTarget = tex2D(_OldAlphaMapTexture, i.uvPC).r;

                // new alpha of the target channel (according to PaintContext destRenderTexture)
                float newTarget = tex2D(_MainTex, i.uvPC).r;

                // not allowed to 'erase' a target channel (cannot reduce it's weight)
                // this is a requirement to work around edge sync bugs
                newTarget = max(newTarget, origTarget);

                float origAlphaOthers = 1 - origTarget;
                if (origAlphaOthers > 0.001f)
                {
                    float nonTargetRenormalizeScale = saturate((1.0f - newTarget) / origAlphaOthers);
                    float4 othersNormalized = origAlphaMap * saturate(1.0f - _LayerMask) * nonTargetRenormalizeScale;
                    return othersNormalized + newTarget * _LayerMask;
                }
                return _LayerMask;
            }
            ENDCG
        }

    }
    Fallback Off
}
