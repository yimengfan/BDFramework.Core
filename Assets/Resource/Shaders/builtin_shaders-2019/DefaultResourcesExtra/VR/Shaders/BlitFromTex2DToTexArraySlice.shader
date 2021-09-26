// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/VR/BlitFromTex2DToTexArraySlice"
{
    Properties { _MainTex ("Texture", 2D) = "" {} }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma require setrtarrayindexfromanyshader
            #pragma exclude_renderers vulkan metal

            #include "UnityCG.cginc"

            UNITY_DECLARE_TEX2D(_MainTex);
            uniform float4 _MainTex_ST;
            uniform float _ArraySliceIndex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                uint slice : SV_RenderTargetArrayIndex;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                o.slice = _ArraySliceIndex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return UNITY_SAMPLE_TEX2D(_MainTex, i.texcoord.xy);
            }
            ENDCG
        }
    }

    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            UNITY_DECLARE_TEX2D(_MainTex);
            uniform float4 _MainTex_ST;
            uniform float _ArraySliceIndex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2g {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2g vert (appdata_t v)
            {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            struct g2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                uint slice : SV_RenderTargetArrayIndex;
            };

            [maxvertexcount(3)]
            void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
            {
                g2f o;

                o.slice = _ArraySliceIndex;

                o.vertex = i[0].vertex;
                o.texcoord = i[0].texcoord;
                triangleStream.Append(o);

                o.vertex = i[1].vertex;
                o.texcoord = i[1].texcoord;
                triangleStream.Append(o);

                o.vertex = i[2].vertex;
                o.texcoord = i[2].texcoord;
                triangleStream.Append(o);

                triangleStream.RestartStrip();
            }

            fixed4 frag (g2f i) : SV_Target
            {
                return UNITY_SAMPLE_TEX2D(_MainTex, i.texcoord.xy);
            }
            ENDCG
        }
    }

    Fallback Off
}
