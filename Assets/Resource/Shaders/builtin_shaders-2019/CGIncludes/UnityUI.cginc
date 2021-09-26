// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_UI_INCLUDED
#define UNITY_UI_INCLUDED

#if defined(SHADER_API_GLES)
// Original function inside #else block with step function was compiled to use "greaterThanEquals" when built for GLES2
// For some reason on iOS "greaterThanEquals" function in fragment shader was giving wrong results. Changing to simple if
// statements or converting code to "lessThanEquals" fixes the issue.

inline float UnityGet2DClipping (in float2 position, in float4 clipRect)
{
    if((clipRect.x < position.x) && (clipRect.y < position.y) &&
        (position.x < clipRect.z) && (position.y < clipRect.w))
    {
        return 1.0;
    }
    return 0.0;
}

#else

inline float UnityGet2DClipping (in float2 position, in float4 clipRect)
{
    float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

#endif

inline fixed4 UnityGetUIDiffuseColor(in float2 position, in sampler2D mainTexture, in sampler2D alphaTexture, fixed4 textureSampleAdd)
{
    return fixed4(tex2D(mainTexture, position).rgb + textureSampleAdd.rgb, tex2D(alphaTexture, position).r + textureSampleAdd.a);
}
#endif
