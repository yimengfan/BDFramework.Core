// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/GIDebug/ShowLightMask" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _LightTexture ("Light texture", 2D) = "white" {}
        _SrcBlend("SrcBlend", Int) = 1.0 // One
        _DstBlend("DstBlend", Int) = 1.0 // One
    }
    SubShader {
        Pass {
            Tags { "RenderType"="Opaque" }
            LOD 200

            Blend One One

            CGPROGRAM
            #pragma vertex vert_surf
            #pragma fragment frag_surf
            #include "UnityCG.cginc"
            #include "UnityShaderVariables.cginc"
            #include "UnityShadowLibrary.cginc"

            struct v2f_surf
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _LightTexture;
            sampler2D _LightTextureB;

            float4 _Color;
            float4 _ChannelSelect;
            float4x4 _WorldToLight;
            int _LightType;

            v2f_surf vert_surf (appdata_full v)
            {
                v2f_surf o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz,1));

                o.uv.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                return o;
            }

            fixed UnitySpotCookie(float4 lightCoord)
            {
                return tex2D(_LightTextureB, lightCoord.xy / lightCoord.w + 0.5).w;
            }

            fixed UnityDefaultAttenuate(float3 lightCoord)
            {
                return tex2D(_LightTexture, dot(lightCoord, lightCoord).xx).r;
            }

            float4 frag_surf (v2f_surf IN) : COLOR
            {
                float4 mainTexSampled = tex2D(_MainTex, IN.uv.xy);

                float result = dot(_ChannelSelect, mainTexSampled.rgba);
                if (result == 0)
                    discard;

                float4 lightCoord = mul(_WorldToLight, IN.worldPos);
                float atten = 1;

                if (_LightType == 0)
                {
                    // directional:  no attenuation
                }
                else if (_LightType == 1)
                {
                    // point
                    atten = UnityDefaultAttenuate(lightCoord.xyz);
                }
                else if (_LightType == 2)
                {
                    // spot
                    atten = (lightCoord.z > 0) * UnitySpotCookie(lightCoord) * UnityDefaultAttenuate(lightCoord.xyz);
                }
                clip(atten - 0.001f);

                return float4(_Color.xyz * result, _Color.w);
            }
            ENDCG
        }
    }
}
