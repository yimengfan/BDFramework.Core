// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Nature/SpeedTree Billboard"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _HueVariation ("Hue Variation", Color) = (1.0,0.5,0.0,0.1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BumpMap ("Normalmap", 2D) = "bump" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        [MaterialEnum(None,0,Fastest,1)] _WindQuality ("Wind Quality", Range(0,1)) = 0
    }

    // targeting SM3.0+
    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "DisableBatching"="LODFading"
        }
        LOD 400

        CGPROGRAM
            #pragma surface surf Lambert vertex:SpeedTreeBillboardVert nodirlightmap nodynlightmap addshadow noinstancing dithercrossfade
            #pragma target 3.0
            #pragma multi_compile __ BILLBOARD_FACE_CAMERA_POS
            #pragma shader_feature EFFECT_BUMP
            #pragma shader_feature EFFECT_HUE_VARIATION
            #define ENABLE_WIND
            #include "SpeedTreeBillboardCommon.cginc"

            void surf(Input IN, inout SurfaceOutput OUT)
            {
                SpeedTreeFragOut o;
                SpeedTreeFrag(IN, o);
                SPEEDTREE_COPY_FRAG(OUT, o)
            }
        ENDCG

        Pass
        {
            Tags { "LightMode" = "Vertex" }

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 3.0
                #pragma multi_compile_fog
                #pragma multi_compile __ LOD_FADE_CROSSFADE
                #pragma multi_compile __ BILLBOARD_FACE_CAMERA_POS
                #pragma shader_feature EFFECT_HUE_VARIATION
                #define ENABLE_WIND
                #include "SpeedTreeBillboardCommon.cginc"

                struct v2f
                {
                    UNITY_POSITION(vertex);
                    UNITY_FOG_COORDS(0)
                    Input data      : TEXCOORD1;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert(SpeedTreeBillboardData v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    SpeedTreeBillboardVert(v, o.data);
                    o.data.color.rgb *= ShadeVertexLightsFull(v.vertex, v.normal, 4, true);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    SpeedTreeFragOut o;
                    SpeedTreeFrag(i.data, o);
                    UNITY_APPLY_DITHER_CROSSFADE(i.vertex.xy);
                    fixed4 c = fixed4(o.Albedo, o.Alpha);
                    UNITY_APPLY_FOG(i.fogCoord, c);
                    return c;
                }
            ENDCG
        }
    }

    // targeting SM2.0: Hue variation and Camera-facing billboard are turned off for less instructions
    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
        }
        LOD 400

        CGPROGRAM
            #pragma surface surf Lambert vertex:SpeedTreeBillboardVert nodirlightmap nodynlightmap noinstancing
            #pragma shader_feature EFFECT_BUMP
            #include "SpeedTreeBillboardCommon.cginc"

            void surf(Input IN, inout SurfaceOutput OUT)
            {
                SpeedTreeFragOut o;
                SpeedTreeFrag(IN, o);
                SPEEDTREE_COPY_FRAG(OUT, o)
            }
        ENDCG

        Pass
        {
            Tags { "LightMode" = "Vertex" }

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fog
                #include "SpeedTreeBillboardCommon.cginc"

                struct v2f
                {
                    UNITY_POSITION(vertex);
                    UNITY_FOG_COORDS(0)
                    Input data      : TEXCOORD1;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert(SpeedTreeBillboardData v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    SpeedTreeBillboardVert(v, o.data);
                    o.data.color.rgb *= ShadeVertexLightsFull(v.vertex, v.normal, 2, false);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    SpeedTreeFragOut o;
                    SpeedTreeFrag(i.data, o);
                    fixed4 c = fixed4(o.Albedo, o.Alpha);
                    UNITY_APPLY_FOG(i.fogCoord, c);
                    return c;
                }
            ENDCG
        }
    }

    FallBack "Transparent/Cutout/VertexLit"
}
