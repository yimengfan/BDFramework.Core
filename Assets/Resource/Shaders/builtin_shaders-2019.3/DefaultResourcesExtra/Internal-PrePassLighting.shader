// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/Internal-PrePassLighting" {
Properties {
    _LightTexture0 ("", any) = "" {}
    _LightTextureB0 ("", 2D) = "" {}
    _ShadowMapTexture ("", any) = "" {}
}
SubShader {

CGINCLUDE
#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"

sampler2D _CameraNormalsTexture;
float4 _CameraNormalsTexture_ST;

half4 CalculateLight (unity_v2f_deferred i)
{
    float3 wpos;
    float2 uv;
    half3 lightDir;
    float atten, fadeDist;
    UnityDeferredCalculateLightParams (i, wpos, uv, lightDir, atten, fadeDist);

    half4 nspec = tex2D (_CameraNormalsTexture, TRANSFORM_TEX(uv, _CameraNormalsTexture));
    half3 normal = nspec.rgb * 2 - 1;
    normal = normalize(normal);

    half diff = max (0, dot (lightDir, normal));
    half3 h = normalize (lightDir - normalize(wpos-_WorldSpaceCameraPos));

    float spec = pow (max (0, dot(h,normal)), nspec.a*128.0);
    spec *= saturate(atten);

    half4 res;
    res.xyz = _LightColor.rgb * (diff * atten);
    res.w = spec * Luminance (_LightColor.rgb);

    float fade = fadeDist * unity_LightmapFade.z + unity_LightmapFade.w;
    res *= saturate(1.0-fade);

    return res;
}
ENDCG

/*Pass 1: LDR Pass - Lighting encoded into a subtractive ARGB8 buffer*/
Pass {
    ZWrite Off
    Blend DstColor Zero

CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass

fixed4 frag (unity_v2f_deferred i) : SV_Target
{
    return exp2(-CalculateLight(i));
}

ENDCG
}

/*Pass 2: HDR Pass - Lighting additively blended into floating point buffer*/
Pass {
    ZWrite Off
    Blend One One

CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass

fixed4 frag (unity_v2f_deferred i) : SV_Target
{
    return CalculateLight(i);
}

ENDCG
}

}
Fallback Off
}
