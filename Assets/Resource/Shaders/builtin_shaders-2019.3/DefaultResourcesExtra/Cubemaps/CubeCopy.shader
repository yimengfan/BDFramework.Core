// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/CubeCopy" {
    Properties {
        _MainTex ("Main", CUBE) = "" {}
        _Level ("Level", Float) = 0.
    }
    CGINCLUDE
    #pragma vertex vert
    #pragma fragment frag

    #include "UnityCG.cginc"

    float _Level;

    struct v2f {
        float4 pos : SV_POSITION;
        float4 uvw : TEXCOORD0;
    };

    v2f vert(appdata_base v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uvw = v.texcoord;
        return o;
    }

    UNITY_DECLARE_TEXCUBE(_MainTex);

    float4 frag(v2f  i) : SV_Target
    {
        return UNITY_SAMPLE_TEXCUBE_LOD(_MainTex, i.uvw.xyz, _Level);
    }
    ENDCG
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Pass {
            ZTest Always
            Blend Off
            AlphaTest off
            Cull Off
            ZWrite Off
            Fog { Mode off }
            CGPROGRAM
            #pragma target 3.0
            ENDCG
        }
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Pass {
            ZTest Always
            Blend Off
            AlphaTest off
            Cull Off
            ZWrite Off
            Fog { Mode off }
            CGPROGRAM
            #pragma target 2.0
            ENDCG
        }
    }
}
