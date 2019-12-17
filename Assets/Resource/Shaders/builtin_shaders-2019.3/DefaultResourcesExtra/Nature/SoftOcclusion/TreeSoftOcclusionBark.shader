// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Nature/Tree Soft Occlusion Bark" {
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
        Tags {
            "IgnoreProjector"="True"
            "RenderType" = "TreeOpaque"
            "DisableBatching"="True"
        }

        Pass {
            Lighting On

            CGPROGRAM
            #pragma vertex bark
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityBuiltin2xTreeLibrary.cginc"

            sampler2D _MainTex;

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 col = input.color;
                col.rgb *= tex2D( _MainTex, input.uv.xy).rgb;
                UNITY_APPLY_FOG(input.fogCoord, col);
                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
            ENDCG
        }

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            #include "TerrainEngine.cginc"

            struct v2f {
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert( appdata v )
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TerrainAnimateTree(v.vertex, v.color.w);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag( v2f i ) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    Dependency "BillboardShader" = "Hidden/Nature/Tree Soft Occlusion Bark Rendertex"
    Fallback Off
}
