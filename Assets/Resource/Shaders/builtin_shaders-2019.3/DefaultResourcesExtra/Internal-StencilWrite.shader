// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Internal-StencilWrite"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            struct a2v {
                float4 pos : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert (a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.pos);
                return o;
            }
            fixed4 frag () : SV_Target { return fixed4(0,0,0,0); }
            ENDCG
        }
    }
    Fallback Off
}
