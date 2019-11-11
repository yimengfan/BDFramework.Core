// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/UI/CompositeOverdraw"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "ForceSupported" = "True" "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        // No culling or depth
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Fog { Mode off }

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float v = col.r;
                // during overdraw pass, each draw call adds 0.1 on the red channel
                if(v < 0.05) // nothing
                    col = fixed4(0,0,0,0);
                else if(v < 0.15) // drawn once
                    col = fixed4(0.9,0.9,0.91, 1);
                else if(v < 0.25) // twice
                    col = fixed4(0.81,0.81,1,1);
                else if(v < 0.35) // thrice
                    col = fixed4(0.82,1,0.82, 1);
                else if(v < 0.45)
                    col = fixed4(1, 0.75, 0.75, 1);
                else // too many times
                    col = fixed4(1-(0.5-v/2), 0.5, 0.5, 1);
                return col;
            }
            ENDCG
        }
    }
}
