// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Nature/Terrain/Utilities"
{
    SubShader
    {
        Pass
        {
            Name "Picking"
            Tags { "LightMode" = "Picking" }

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
                #include "UnityCG.cginc"

                #define TERRAIN_BASE_PASS
                #include "TerrainSplatmapCommon.cginc"

                float4 vert(appdata_full v) : SV_POSITION
                {
                    UNITY_SETUP_INSTANCE_ID(v);
                    Input i;
                    SplatmapVert(v, i);
                    return UnityObjectToClipPos(v.vertex);
                }

                uniform float4 _SelectionID;

                fixed4 frag() : SV_Target
                {
                    return _SelectionID;
                }
            ENDCG
        }

        Pass
        {
            Name "Selection"
            Tags { "LightMode" = "SceneSelectionPass" }

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
                #include "UnityCG.cginc"

                #define TERRAIN_BASE_PASS
                #include "TerrainSplatmapCommon.cginc"

                float4 vert(appdata_full v) : SV_POSITION
                {
                    UNITY_SETUP_INSTANCE_ID(v);
                    Input i;
                    SplatmapVert(v, i);
                    return UnityObjectToClipPos(v.vertex);
                }

                int _ObjectId;
                int _PassValue;

                float4 frag() : SV_Target
                {
                    return float4(_ObjectId, _PassValue, 1, 1);
                }
            ENDCG
        }
    }
}
