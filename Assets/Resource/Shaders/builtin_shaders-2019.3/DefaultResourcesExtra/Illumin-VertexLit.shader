// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Legacy Shaders/Self-Illumin/VertexLit" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Spec Color", Color) = (1,1,1,1)
    [PowerSlider(5.0)] _Shininess ("Shininess", Range (0.1, 1)) = 0.7
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _Illum ("Illumin (A)", 2D) = "white" {}
    _Emission ("Emission (Lightmapper)", Float) = 1.0
}

SubShader {
    LOD 100
    Tags { "RenderType"="Opaque" }

    Pass {
        Name "BASE"
        Tags {"LightMode" = "Vertex"}
        Material {
            Diffuse [_Color]
            Shininess [_Shininess]
            Specular [_SpecColor]
        }
        SeparateSpecular On
        Lighting On
        SetTexture [_Illum] {
            constantColor [_Color]
            combine constant lerp (texture) previous
        }
        SetTexture [_MainTex] {
            constantColor (1,1,1,1)
            Combine texture * previous, constant // UNITY_OPAQUE_ALPHA_FFP
        }
    }

    // Extracts information for lightmapping, GI (emission, albedo, ...)
    // This pass it not used during regular rendering.
    Pass
    {
        Name "META"
        Tags { "LightMode" = "Meta" }
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        #include "UnityCG.cginc"
        #include "UnityMetaPass.cginc"

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 uvMain : TEXCOORD0;
            float2 uvIllum : TEXCOORD1;
        #ifdef EDITOR_VISUALIZATION
            float2 vizUV : TEXCOORD2;
            float4 lightCoord : TEXCOORD3;
        #endif
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float4 _MainTex_ST;
        float4 _Illum_ST;

        v2f vert (appdata_full v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
            o.uvMain = TRANSFORM_TEX(v.texcoord, _MainTex);
            o.uvIllum = TRANSFORM_TEX(v.texcoord, _Illum);
        #ifdef EDITOR_VISUALIZATION
            o.vizUV = 0;
            o.lightCoord = 0;
            if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
                o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
            else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
            {
                o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
            }
        #endif
            return o;
        }

        sampler2D _MainTex;
        sampler2D _Illum;
        fixed4 _Color;
        fixed _Emission;

        half4 frag (v2f i) : SV_Target
        {
            UnityMetaInput metaIN;
            UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);

            fixed4 tex = tex2D(_MainTex, i.uvMain);
            fixed4 c = tex * _Color;
            metaIN.Albedo = c.rgb;
            metaIN.Emission = c.rgb * tex2D(_Illum, i.uvIllum).a;
        #if defined(EDITOR_VISUALIZATION)
            metaIN.VizUV = i.vizUV;
            metaIN.LightCoord = i.lightCoord;
        #endif

            return UnityMetaFragment(metaIN);
        }
        ENDCG
    }
}

Fallback "Legacy Shaders/VertexLit"
CustomEditor "LegacyIlluminShaderGUI"
}
