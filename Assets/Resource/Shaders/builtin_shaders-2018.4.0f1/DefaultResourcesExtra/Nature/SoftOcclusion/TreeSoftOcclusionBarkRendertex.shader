// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Nature/Tree Soft Occlusion Bark Rendertex" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,0)
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseLight ("Base Light", Range(0, 1)) = 0.35
        _AO ("Amb. Occlusion", Range(0, 10)) = 2.4

        // These are here only to provide default values
        [HideInInspector] _TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
        [HideInInspector] _TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
        [HideInInspector] _SquashAmount ("Squash", Float) = 1
    }

    SubShader {
        Pass {
            Lighting On

            CGPROGRAM
            #pragma vertex bark
            #pragma fragment frag
            #define WRITE_ALPHA_1 1
            #define USE_CUSTOM_LIGHT_DIR 1
            #include "UnityBuiltin2xTreeLibrary.cginc"

            sampler2D _MainTex;

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 col = input.color;
                col.rgb *= tex2D( _MainTex, input.uv.xy).rgb;
                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
