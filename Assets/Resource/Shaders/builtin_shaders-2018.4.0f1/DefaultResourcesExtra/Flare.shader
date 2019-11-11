// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "FX/Flare" {
Properties {
    _MainTex ("Particle Texture", 2D) = "black" {}
}
SubShader {
    Tags {
        "Queue"="Transparent"
        "IgnoreProjector"="True"
        "RenderType"="Transparent"
        "PreviewType"="Plane"
    }
    Cull Off Lighting Off ZWrite Off Ztest Always
    Blend One One

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _TintColor;

        struct appdata_t {
            float4 vertex : POSITION;
            fixed4 color : COLOR;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            fixed4 color : COLOR;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float4 _MainTex_ST;

        v2f vert (appdata_t v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.color = v.color;
            o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            fixed4 col;
            fixed4 tex = tex2D(_MainTex, i.texcoord);
            col.rgb = i.color.rgb * tex.rgb;
            col.a = tex.a;
            return col;
        }
        ENDCG
    }
}

}
