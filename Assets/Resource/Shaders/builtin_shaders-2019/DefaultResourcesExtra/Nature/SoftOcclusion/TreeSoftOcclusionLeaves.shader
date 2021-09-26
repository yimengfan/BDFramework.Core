// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Nature/Tree Soft Occlusion Leaves" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {  }
        _Cutoff ("Alpha cutoff", Range(0.25,0.9)) = 0.5
        _BaseLight ("Base Light", Range(0, 1)) = 0.35
        _AO ("Amb. Occlusion", Range(0, 10)) = 2.4
        _Occlusion ("Dir Occlusion", Range(0, 20)) = 7.5

        // These are here only to provide default values
        [HideInInspector] _TreeInstanceColor ("TreeInstanceColor", Vector) = (1,1,1,1)
        [HideInInspector] _TreeInstanceScale ("TreeInstanceScale", Vector) = (1,1,1,1)
        [HideInInspector] _SquashAmount ("Squash", Float) = 1
    }

    SubShader {
        Tags {
            "Queue" = "AlphaTest"
            "IgnoreProjector"="True"
            "RenderType" = "TreeTransparentCutout"
            "DisableBatching"="True"
        }
        Cull Off
        ColorMask RGB

        Pass {
            Lighting On

            CGPROGRAM
            #pragma vertex leaves
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityBuiltin2xTreeLibrary.cginc"

            sampler2D _MainTex;
            fixed _Cutoff;

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 c = tex2D( _MainTex, input.uv.xy);
                c.rgb *= input.color.rgb;

                clip (c.a - _Cutoff);
                UNITY_APPLY_FOG(input.fogCoord, c);
                return c;
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
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            v2f vert( appdata v )
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                TerrainAnimateTree(v.vertex, v.color.w);
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.texcoord;
                return o;
            }

            sampler2D _MainTex;
            fixed _Cutoff;

            float4 frag( v2f i ) : SV_Target
            {
                fixed4 texcol = tex2D( _MainTex, i.uv );
                clip( texcol.a - _Cutoff );
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    // This subshader is never actually used, but is only kept so
    // that the tree mesh still assumes that normals are needed
    // at build time (due to Lighting On in the pass). The subshader
    // above does not actually use normals, so they are stripped out.
    // We want to keep normals for backwards compatibility with Unity 4.2
    // and earlier.
    SubShader {
        Tags {
            "Queue" = "AlphaTest"
            "IgnoreProjector"="True"
            "RenderType" = "TransparentCutout"
        }
        Cull Off
        ColorMask RGB
        Pass {
            Tags { "LightMode" = "Vertex" }
            AlphaTest GEqual [_Cutoff]
            Lighting On
            Material {
                Diffuse [_Color]
                Ambient [_Color]
            }
            SetTexture [_MainTex] { combine primary * texture DOUBLE, texture }
        }
    }

    Dependency "BillboardShader" = "Hidden/Nature/Tree Soft Occlusion Leaves Rendertex"
    Fallback Off
}
