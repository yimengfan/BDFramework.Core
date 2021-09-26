// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/TerrainEngine/CrossBlendNeighbors"
{
    Properties
    {
        _TopTex ("Top Texture", any) = "black" {}
        _BottomTex ("Bottom Texture", any) = "black" {}
        _LeftTex ("Left Texture", any) = "black" {}
        _RightTex ("Right Texture", any) = "black" {}
    }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment CrossBlendNeighbors
            #pragma target 3.0

            #include "UnityCG.cginc"

            uniform float4 _TexCoordOffsetScale;
            uniform float4 _Offsets; // bottom, top, left, right
            uniform float4 _SlopeEnableFlags; // bottom, top, left, right; 0.0f - neighbor exists, 1.0f - no neighbor
            uniform float  _AddressMode; // 0.0f - clamp, 1.0f - mirror

            sampler2D _TopTex;
            sampler2D _BottomTex;
            sampler2D _LeftTex;
            sampler2D _RightTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 texcoord : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord.xy = v.texcoord;
                o.texcoord.zw = (v.texcoord + _TexCoordOffsetScale.xy) * _TexCoordOffsetScale.zw;
                return o;
            }

            float4 CrossBlendNeighbors(v2f i) : SV_Target
            {
                // All slope offset data is static, but we calculate it on GPU because we don't want to access height data on CPU
                float2 topSlope    = float2(UnpackHeightmap(tex2Dlod(_LeftTex,   float4(1.0f, 1.0f, 0.0f, 0.0f))), UnpackHeightmap(tex2Dlod(_RightTex, float4(0.0f, 1.0f, 0.0f, 0.0f)))) + _Offsets.zw;
                float2 bottomSlope = float2(UnpackHeightmap(tex2Dlod(_LeftTex,   float4(1.0f, 0.0f, 0.0f, 0.0f))), UnpackHeightmap(tex2Dlod(_RightTex, float4(0.0f, 0.0f, 0.0f, 0.0f)))) + _Offsets.zw;
                float2 leftSlope   = float2(UnpackHeightmap(tex2Dlod(_BottomTex, float4(0.0f, 1.0f, 0.0f, 0.0f))), UnpackHeightmap(tex2Dlod(_TopTex,   float4(0.0f, 0.0f, 0.0f, 0.0f)))) + _Offsets.xy;
                float2 rightSlope  = float2(UnpackHeightmap(tex2Dlod(_BottomTex, float4(1.0f, 1.0f, 0.0f, 0.0f))), UnpackHeightmap(tex2Dlod(_TopTex,   float4(1.0f, 0.0f, 0.0f, 0.0f)))) + _Offsets.xy;
                float2 topSlopeOffset    = _Offsets.y + _SlopeEnableFlags.y * topSlope;
                float2 bottomSlopeOffset = _Offsets.x + _SlopeEnableFlags.x * bottomSlope;
                float2 leftSlopeOffset   = _Offsets.z + _SlopeEnableFlags.z * leftSlope;
                float2 rightSlopeOffset  = _Offsets.w + _SlopeEnableFlags.w * rightSlope;

                float2 blendPos = saturate(i.texcoord.zw);

                float4 weights = 1.0f / max(float4(1.0f - blendPos.y, blendPos.y, blendPos.x, 1.0f - blendPos.x), 0.0000001f);
                weights /= dot(weights, 1.0f);

                float4 heights = float4(
                    UnpackHeightmap(tex2D(_TopTex,    float2(i.texcoord.x, (1.0f - i.texcoord.y) * _AddressMode.x))),
                    UnpackHeightmap(tex2D(_BottomTex, float2(i.texcoord.x,  1.0f - i.texcoord.y  * _AddressMode.x))),
                    UnpackHeightmap(tex2D(_LeftTex,   float2( 1.0f - i.texcoord.x  * _AddressMode.x, i.texcoord.y))),
                    UnpackHeightmap(tex2D(_RightTex,  float2((1.0f - i.texcoord.x) * _AddressMode.x, i.texcoord.y)))
                );

                heights += float4(
                    lerp(topSlopeOffset.x,    topSlopeOffset.y, blendPos.x),
                    lerp(bottomSlopeOffset.x, bottomSlopeOffset.y, blendPos.x),
                    lerp(leftSlopeOffset.x,   leftSlopeOffset.y, blendPos.y),
                    lerp(rightSlopeOffset.x,  rightSlopeOffset.y, blendPos.y)
                );

                return PackHeightmap(dot(heights, weights));
            }
            ENDCG
        }
    }
    Fallback Off
}
