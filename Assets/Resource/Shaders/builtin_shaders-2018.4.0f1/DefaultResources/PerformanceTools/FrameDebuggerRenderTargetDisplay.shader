// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/FrameDebuggerRenderTargetDisplay" {
    Properties {
        _MainTex ("", any) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "HLSLSupport.cginc"
    struct appdata {
        float4 vertex : POSITION;
        float3 uv : TEXCOORD0;
    };

    struct v2f {
        float4 pos : SV_POSITION;
        float3 uv : TEXCOORD0;
    };

    v2f vert(appdata v) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    fixed4 _Channels;
    half4 _Levels;
    bool _UndoOutputSRGB;

    fixed4 ProcessColor (half4 tex)
    {
        // adjust levels
        half4 col = tex;
        col -= _Levels.rrrr;
        col /= _Levels.gggg-_Levels.rrrr;

        // leave only channels we want to show
        col *= _Channels;

        // if we're showing only a single channel, display that as grayscale
        if (dot(_Channels,fixed4(1,1,1,1)) == 1.0)
        {
            half c = dot(col,half4(1,1,1,1));
            col = c;
        }

        // When writing to the render target, it will compress our output into
        // sRGB space. If we just want to show the linear value as-is, we need
        // to cancel the hardware's sRGB conversion, so we convert "from" sRGB
        // which the HW will revert by converting back "to" sRGB.
        if (_UndoOutputSRGB)
            col.rgb = GammaToLinearSpace(saturate(col.rgb));

        return col;
    }
    ENDCG

    SubShader {
        Tags { "ForceSupported"="True" }
        Cull Off ZWrite Off ZTest Always

        // 2D texture
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D_float _MainTex;

            fixed4 frag (v2f i) : SV_Target {
                half4 tex = tex2D (_MainTex, i.uv.xy);
                return ProcessColor (tex);
            }
            ENDCG
        }

        // Cubemap
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            samplerCUBE_float _MainTex;

            fixed4 frag (v2f i) : SV_Target {
                half4 tex = texCUBE (_MainTex, i.uv.xyz);
                return ProcessColor (tex);
            }
            ENDCG
        }
    }

    SubShader{
        Cull Off ZWrite Off ZTest Always

        // 2D array texture
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

            fixed4 frag(v2f i) : SV_Target{
                half4 tex = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv.xyz);
                return ProcessColor(tex);
            }
            ENDCG
        }
    }
    Fallback Off
}
