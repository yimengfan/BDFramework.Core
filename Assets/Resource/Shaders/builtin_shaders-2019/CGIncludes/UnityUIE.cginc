// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles

#ifndef UNITY_UIE_INCLUDED
#define UNITY_UIE_INCLUDED

#ifndef UIE_SIMPLE_ATLAS
    #if SHADER_TARGET < 35
        #define UIE_SIMPLE_ATLAS 1
    #else
        #define UIE_SIMPLE_ATLAS 0
    #endif // SHADER_TARGET < 35
#endif // UIE_SIMPLE_ATLAS

#if SHADER_TARGET >= 30
    #define UIE_SHADER_INFO_IN_VS 1
#else
    #define UIE_SHADER_INFO_IN_VS 0
#endif // SHADER_TARGET >= 30

#ifndef UIE_COLORSPACE_GAMMA
    #ifdef UNITY_COLORSPACE_GAMMA
        #define UIE_COLORSPACE_GAMMA 1
    #else
        #define UIE_COLORSPACE_GAMMA 0
    #endif // UNITY_COLORSPACE_GAMMA
#endif // UIE_COLORSPACE_GAMMA

#ifndef UIE_FRAG_T
    #if UIE_COLORSPACE_GAMMA
        #define UIE_FRAG_T fixed4
    #else
        #define UIE_FRAG_T half4
    #endif // UIE_COLORSPACE_GAMMA
#endif // UIE_FRAG_T

#ifndef UIE_V2F_COLOR_T
    #if UIE_COLORSPACE_GAMMA
        #define UIE_V2F_COLOR_T fixed4
    #else
        #define UIE_V2F_COLOR_T half4
    #endif // UIE_COLORSPACE_GAMMA
#endif // UIE_V2F_COLOR_T

// The value below is only used on older shader targets, and should be configurable for the app at hand to be the smallest possible
// The first entry is always the identity matrix
#ifndef UIE_SKIN_ELEMS_COUNT_MAX_CONSTANTS
#define UIE_SKIN_ELEMS_COUNT_MAX_CONSTANTS 20
#endif // UIE_SKIN_ELEMS_COUNT_MAX_CONSTANTS

#include "UnityCG.cginc"

#if UIE_SIMPLE_ATLAS
sampler2D _MainTex;
#else
Texture2D _MainTex;
#endif
float4 _MainTex_ST;
float4 _MainTex_TexelSize;

SamplerState uie_point_clamp_sampler;
SamplerState uie_linear_clamp_sampler;

sampler2D _FontTex;
float4 _FontTex_ST;

sampler2D _CustomTex;
float4 _CustomTex_ST;
float4 _CustomTex_TexelSize;

sampler2D _GradientSettingsTex;
float4 _GradientSettingsTex_ST;
float4 _GradientSettingsTex_TexelSize;

sampler2D _ShaderInfoTex;
float4 _ShaderInfoTex_TexelSize;

float4 _1PixelClipInvView; // xy in clip space, zw inverse in view space
float4 _PixelClipRect; // In framebuffer space

#if !UIE_SHADER_INFO_IN_VS

CBUFFER_START(UITransforms)
float4 _Transforms[UIE_SKIN_ELEMS_COUNT_MAX_CONSTANTS * 3];
CBUFFER_END

CBUFFER_START(UIClipRects)
float4 _ClipRects[UIE_SKIN_ELEMS_COUNT_MAX_CONSTANTS];
CBUFFER_END

#endif // !UIE_SHADER_INFO_IN_VS

struct appdata_t
{
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 uv       : TEXCOORD0;
    float4 xformClipPages : TEXCOORD1; // Top-left of xform and clip pages: XY,XY
    float4 idsFlags : TEXCOORD2; //XYZ (xform,clip,opacity) (W flags)
    float4 opacityPageSVGSettingIndex : TEXCOORD3; //XY (ZW SVG setting index)

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex   : SV_POSITION;
    UIE_V2F_COLOR_T color : COLOR;
    float4 uvXY  : TEXCOORD0; // UV and ZW holds XY position in points
    nointerpolation fixed4 flags : TEXCOORD1;
    nointerpolation fixed3 svgFlags : TEXCOORD2;
    nointerpolation fixed4 clipRectOpacityUVs : TEXCOORD3;
#if UIE_SHADER_INFO_IN_VS
    nointerpolation fixed4 clipRect : TEXCOORD4; // Clip rect presampled
#endif // UIE_SHADER_INFO_IN_VS
    UNITY_VERTEX_OUTPUT_STEREO
};

static const float kUIEMeshZ = 0.995f; // Keep in track with UIRUtility.k_MeshPosZ
static const float kUIEMaskZ = -0.995f; // Keep in track with UIRUtility.k_MaskPosZ

static const float kUIEVertexLastFlagValue = 10.0f; // Keep in track with UIR.VertexFlags

// Notes on UIElements Spaces (Local, Bone, Group, World and Clip)
//
// Consider the following example:
//      *     <- Clip Space (GPU Clip Coordinates)
//    Proj
//      |     <- World Space
//   VEroot
//      |
//     VE1 (RenderHint = Group)
//      |     <- Group Space
//     VE2 (RenderHint = Bone)
//      |     <- Bone Space
//     VE3
//
// A VisualElement always emits vertices in local-space. They do not embed the transform of the emitting VisualElement.
// The renderer transforms the vertices on CPU from local-space to bone space (if available), or to the group space (if available),
// or ultimately to world-space if there is no ancestor with a bone transform or group transform.
//
// The world-to-clip transform is stored in UNITY_MATRIX_P
// The group-to-world transform is stored in UNITY_MATRIX_V
// The bone-to-group transform is stored in uie_toWorldMat.
//
// In this shader, we consider that vertices are always in bone-space, and we always apply the bone-to-group and the group-to-world
// transforms. It does not matter because in the event where there is no ancestor with a Group or Bone RenderHint, these transform
// will be identities.

static float3x4 uie_toWorldMat;

// Returns the view-space offset that must be applied to the vertex to satisfy a minimum displacement constraint.
// vertex               Coordinates of the vertex, in vertex-space.
// embeddedDisplacement Displacement vector that is embedded in vertex, in vertex-space.
// minDisplacement      Minimum length of the displacement that must be observed, in pixels.
float2 uie_get_border_offset(float4 vertex, float2 embeddedDisplacement, float minDisplacement, bool noShrinkX, bool noShrinkY)
{
    // Compute the absolute displacement in framebuffer space (unit = 1 pixel).
    float2 viewDisplacement = mul(uie_toWorldMat, float4(embeddedDisplacement, 0, 0)).xy;
    float2 frameDisplacementAbs = abs(viewDisplacement * _1PixelClipInvView.zw);

    // We need to meet the minimum displacement requirement before rounding so that we can simply add 1 after rounding
    // if we don't meet it anymore.
    float2 newFrameDisplacementAbs = max(minDisplacement.xx, frameDisplacementAbs);
    float2 newFrameDisplacementAbsBeforeRound = newFrameDisplacementAbs;
    newFrameDisplacementAbs = round(newFrameDisplacementAbs);
    if(noShrinkX)
        newFrameDisplacementAbs.x = max(newFrameDisplacementAbs.x, newFrameDisplacementAbsBeforeRound.x);
    if(noShrinkY)
        newFrameDisplacementAbs.y = max(newFrameDisplacementAbs.y, newFrameDisplacementAbsBeforeRound.y);
    newFrameDisplacementAbs += step(newFrameDisplacementAbs, minDisplacement - 0.001);

    // Convert the resulting displacement into an offset.
    float2 changeRatio = newFrameDisplacementAbs / (frameDisplacementAbs + 0.000001);
    changeRatio = clamp(changeRatio, 0.01, 100);
    float2 viewOffset = (changeRatio - 1) * viewDisplacement;
    return viewOffset;
}

float2 uie_snap_to_integer_pos(float2 clipSpaceXY)
{
    return ((int2)((clipSpaceXY+1)/_1PixelClipInvView.xy+0.51f)) * _1PixelClipInvView.xy-1;
}

void uie_fragment_clip(v2f IN)
{
    float4 clipRect;
#if UIE_SHADER_INFO_IN_VS
    clipRect = IN.clipRect; // Presampled in the vertex shader, and sent down to the fragment shader ready
#else // !UIE_SHADER_INFO_IN_VS
    clipRect = _ClipRects[IN.clipRectOpacityUVs.x];
#endif // UIE_SHADER_INFO_IN_VS

    float2 pointPos = IN.uvXY.zw;
    float2 pixelPos = IN.vertex.xy;
    float2 s = step(clipRect.xy,   pointPos) + step(pointPos, clipRect.zw) +
               step(_PixelClipRect.xy, pixelPos)  + step(pixelPos, _PixelClipRect.zw);
    clip(dot(float3(s,1),float3(1,1,-7.95f)));
}

float2 uie_decode_shader_info_texel_pos(float2 pageXY, float id, float yStride)
{
    const float kShaderInfoPageWidth = 32;
    const float kShaderInfoPageHeight = 8;
    id *= 255.0f;
    pageXY *= 255.0f; // From [0,1] to [0,255]
    float idX = id % kShaderInfoPageWidth;
    float idY = (id - idX) / kShaderInfoPageWidth;

    return float2(
        pageXY.x * kShaderInfoPageWidth + idX,
        pageXY.y * kShaderInfoPageHeight + idY * yStride);
}

void uie_vert_load_payload(appdata_t v)
{
#if UIE_SHADER_INFO_IN_VS

    float2 xformTexel = uie_decode_shader_info_texel_pos(v.xformClipPages.xy, v.idsFlags.x, 3.0f);
    float2 row0UV = (xformTexel + float2(0, 0) + 0.5f) * _ShaderInfoTex_TexelSize.xy;
    float2 row1UV = (xformTexel + float2(0, 1) + 0.5f) * _ShaderInfoTex_TexelSize.xy;
    float2 row2UV = (xformTexel + float2(0, 2) + 0.5f) * _ShaderInfoTex_TexelSize.xy;

    uie_toWorldMat = float3x4(
        tex2Dlod(_ShaderInfoTex, float4(row0UV, 0, 0)),
        tex2Dlod(_ShaderInfoTex, float4(row1UV, 0, 0)),
        tex2Dlod(_ShaderInfoTex, float4(row2UV, 0, 0)));

#else // !UIE_SHADER_INFO_IN_VS

    int xformConstantIndex = (int)(v.idsFlags.x * 255.0f * 3.0f);
    uie_toWorldMat = float3x4(
        _Transforms[xformConstantIndex + 0],
        _Transforms[xformConstantIndex + 1],
        _Transforms[xformConstantIndex + 2]);

#endif // UIE_SHADER_INFO_IN_VS
}

float2 uie_unpack_float2(fixed4 c)
{
    return float2(c.r*255 + c.g, c.b*255 + c.a);
}

float2 uie_ray_unit_circle_first_hit(float2 rayStart, float2 rayDir)
{
    float tca = dot(-rayStart, rayDir);
    float d2 = dot(rayStart, rayStart) - tca * tca;
    float thc = sqrt(1.0f - d2);
    float t0 = tca - thc;
    float t1 = tca + thc;
    float t = min(t0, t1);
    if (t < 0.0f)
        t = max(t0, t1);
    return rayStart + rayDir * t;
}

float uie_radial_address(float2 uv, float2 focus)
{
    uv = (uv - float2(0.5f, 0.5f)) * 2.0f;
    float2 pointOnPerimeter = uie_ray_unit_circle_first_hit(focus, normalize(uv - focus));
    float2 diff = pointOnPerimeter - focus;
    if (abs(diff.x) > 0.0001f)
        return (uv.x - focus.x) / diff.x;
    if (abs(diff.y) > 0.0001f)
        return (uv.y - focus.y) / diff.y;
    return 0.0f;
}

struct GradientLocation
{
    float2 uv;
    float4 location;
};

GradientLocation uie_sample_gradient_location(float settingIndex, float2 uv, sampler2D settingsTex, float2 texelSize)
{
    // Gradient settings are stored in 3 consecutive texels:
    // - texel 0: (float4, 1 byte per float)
    //    x = gradient type (0 = tex/linear, 1 = radial)
    //    y = address mode (0 = wrap, 1 = clamp, 2 = mirror)
    //    z = radialFocus.x
    //    w = radialFocus.y
    // - texel 1: (float2, 2 bytes per float) atlas entry position
    //    xy = pos.x
    //    zw = pos.y
    // - texel 2: (float2, 2 bytes per float) atlas entry size
    //    xy = size.x
    //    zw = size.y

    float2 settingUV = float2(0.5f, settingIndex+0.5f) * texelSize;
    fixed4 gradSettings = tex2D(settingsTex, settingUV);
    if (gradSettings.x > 0.0f)
    {
        // Radial texture case
        float2 focus = (gradSettings.zw - float2(0.5f, 0.5f)) * 2.0f; // bring focus in the (-1,1) range
        uv = float2(uie_radial_address(uv, focus), 0.0);
    }

    int addressing = gradSettings.y * 255;
    uv.x = (addressing == 0) ? fmod(uv.x,1.0f) : uv.x; // Wrap
    uv.x = (addressing == 1) ? max(min(uv.x,1.0f), 0.0f) : uv.x; // Clamp
    float w = fmod(uv.x,2.0f);
    uv.x = (addressing == 2) ? (w > 1.0f ? 1.0f-fmod(w,1.0f) : w) : uv.x; // Mirror

    GradientLocation grad;
    grad.uv = uv;

    // Adjust UV to atlas position
    float2 nextUV = float2(texelSize.x, 0);
    grad.location.xy = (uie_unpack_float2(tex2D(settingsTex, settingUV+nextUV) * 255) + float2(0.5f, 0.5f));
    grad.location.zw = uie_unpack_float2(tex2D(settingsTex, settingUV+nextUV*2) * 255);

    return grad;
}

float TestForValue(float value, inout float flags)
{
#if SHADER_API_GLES
    float result = saturate(flags - value + 1.0);
    flags -= result * value;
    return result;
#else
    return flags == value;
#endif
}

float sdf(float distanceSample)
{
    float sharpness = 0;
    float outlineSoftness = 0;
    float faceDilation = 0;

    float smoothing = fwidth(distanceSample) * (1 - sharpness) + outlineSoftness;
    float contour = 0.5 - faceDilation * 0.5;
    float2 edgeRange = float2(contour - smoothing, contour + smoothing);

    return smoothstep (edgeRange.x, edgeRange.y, distanceSample);
}

float4 uie_std_vert_shader_info(appdata_t v, out UIE_V2F_COLOR_T color)
{
#if UIE_COLORSPACE_GAMMA
    color = v.color;
#else // !UIE_COLORSPACE_GAMMA
    // Keep this in the VS to ensure that interpolation is performed in the right color space
    color = UIE_V2F_COLOR_T(GammaToLinearSpace(v.color.rgb), v.color.a);
#endif // UIE_COLORSPACE_GAMMA

    const float2 opacityUV = (uie_decode_shader_info_texel_pos(v.opacityPageSVGSettingIndex.xy, v.idsFlags.z, 1.0f) + 0.5f) * _ShaderInfoTex_TexelSize.xy;
#if UIE_SHADER_INFO_IN_VS
    const float2 clipRectUV = (uie_decode_shader_info_texel_pos(v.xformClipPages.zw, v.idsFlags.y, 1.0f) + 0.5f) * _ShaderInfoTex_TexelSize.xy;
    color.a *= tex2Dlod(_ShaderInfoTex, float4(opacityUV, 0, 0)).a;
#else // !UIE_SHADER_INFO_IN_VS
    const float2 clipRectUV = float2(v.idsFlags.y * 255.0f, 0.0f);
#endif // UIE_SHADER_INFO_IN_VS

    return float4(clipRectUV, opacityUV);
}

v2f uie_std_vert(appdata_t v)
{
    v2f OUT;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    uie_vert_load_payload(v);
    float flags = v.idsFlags.w*255.0f;
    // Keep the descending order for GLES2
    const float isCustomSVGGradients = TestForValue(9.0, flags);
    const float isSVGGradients = TestForValue(8.0, flags);
    const float isEdgeNoShrinkY = TestForValue(7.0, flags);
    const float isEdgeNoShrinkX = TestForValue(6.0, flags);
    const float isEdge = TestForValue(5.0, flags);
    const float isCustomTex = TestForValue(4.0, flags);
    const float isAtlasTexBilinear = TestForValue(3.0, flags);
    const float isAtlasTexPoint = TestForValue(2.0, flags);
    const float isText = TestForValue(1.0, flags);
    const float isAtlasTex = isAtlasTexBilinear + isAtlasTexPoint;
    const float isSolid = 1 - saturate(isText + isAtlasTex + isCustomTex + isSVGGradients + isCustomSVGGradients);

    float2 viewOffset = float2(0, 0);
    if (isEdge == 1 || isEdgeNoShrinkX == 1 || isEdgeNoShrinkY == 1)
    {
        viewOffset = uie_get_border_offset(v.vertex, v.uv, 1, isEdgeNoShrinkX == 1, isEdgeNoShrinkY == 1);
    }

    v.vertex.xyz = mul(uie_toWorldMat, v.vertex);
    v.vertex.xy += viewOffset;

    OUT.uvXY.zw = v.vertex.xy;
    OUT.vertex = UnityObjectToClipPos(v.vertex);

#ifndef UIE_SDF_TEXT
    if (isText == 1)
        OUT.vertex.xy = uie_snap_to_integer_pos(OUT.vertex.xy);
#endif

    OUT.uvXY.xy = TRANSFORM_TEX(v.uv, _MainTex);
    if (isAtlasTex == 1.0f)
        OUT.uvXY.xy *= _MainTex_TexelSize.xy;

#if UIE_SIMPLE_ATLAS
    OUT.flags = fixed4(isText, isAtlasTex, isCustomTex, isSolid);
#else
    OUT.flags = fixed4(isText, isAtlasTexBilinear - isAtlasTexPoint, isCustomTex, isSolid);
#endif
    float svgSettingsIndex = v.opacityPageSVGSettingIndex.z*(255.0f*255.0f) + v.opacityPageSVGSettingIndex.w*255.0f;
    OUT.svgFlags = fixed3(isSVGGradients, isCustomSVGGradients, svgSettingsIndex);

    OUT.clipRectOpacityUVs = uie_std_vert_shader_info(v, OUT.color);

#if UIE_SHADER_INFO_IN_VS
    OUT.clipRect = tex2Dlod(_ShaderInfoTex, float4(OUT.clipRectOpacityUVs.xy, 0, 0));
#endif // UIE_SHADER_INFO_IN_VS

    return OUT;
}

UIE_FRAG_T uie_std_frag(v2f IN)
{
    uie_fragment_clip(IN);

    // Extract the flags.
    fixed isText               = IN.flags.x;
#if UIE_SIMPLE_ATLAS
    fixed isAtlasTex           = IN.flags.y;
#else
    fixed isAtlasTexPoint      = saturate(-IN.flags.y);
    fixed isAtlasTexBilinear   = saturate(IN.flags.y);
#endif
    fixed isCustomTex          = IN.flags.z;
    fixed isSolid              = IN.flags.w;
    fixed isSVGGradients       = IN.svgFlags.x;
    fixed isCustomSVGGradients = IN.svgFlags.y;
    float settingIndex         = IN.svgFlags.z;

    float2 uv = IN.uvXY.xy;

#if !UIE_SHADER_INFO_IN_VS
    IN.color.a *= tex2D(_ShaderInfoTex, IN.clipRectOpacityUVs.zw).a;
#endif // !UIE_SHADER_INFO_IN_VS

    UIE_FRAG_T texColor = (UIE_FRAG_T)isSolid;
#if UIE_SIMPLE_ATLAS
    texColor += tex2D(_MainTex, uv) * isAtlasTex;
#else
    texColor += _MainTex.Sample(uie_point_clamp_sampler, uv) * isAtlasTexPoint;
    texColor += _MainTex.Sample(uie_linear_clamp_sampler, uv) * isAtlasTexBilinear;
#endif
#ifdef UIE_SDF_TEXT
    texColor += UIE_FRAG_T(1, 1, 1, sdf(tex2D(_FontTex, uv).a)) * isText;
#else
    texColor += UIE_FRAG_T(1, 1, 1, tex2D(_FontTex, uv).a) * isText;
#endif
    texColor += tex2D(_CustomTex, uv) * isCustomTex;

    if (isSVGGradients == 1.0f || isCustomSVGGradients == 1.0f)
    {
        float2 texelSize = isCustomSVGGradients == 1.0f ? _CustomTex_TexelSize.xy : _MainTex_TexelSize.xy;
        GradientLocation grad = uie_sample_gradient_location(settingIndex, uv, _GradientSettingsTex, _GradientSettingsTex_TexelSize.xy);
        grad.location *= texelSize.xyxy;
        grad.uv *= grad.location.zw;
        grad.uv += grad.location.xy;

#if UIE_SIMPLE_ATLAS
        texColor += tex2D(_MainTex, grad.uv) * isSVGGradients;
#else
        texColor += _MainTex.Sample(uie_linear_clamp_sampler, grad.uv) * isSVGGradients;
#endif
        texColor += tex2D(_CustomTex, grad.uv) * isCustomSVGGradients;
    }

    UIE_FRAG_T color = texColor * IN.color;
    return color;
}

#ifndef UIE_CUSTOM_SHADER

v2f vert(appdata_t v) { return uie_std_vert(v); }
UIE_FRAG_T frag(v2f IN) : SV_Target { return uie_std_frag(IN); }

#endif // UIE_CUSTOM_SHADER

#endif // UNITY_UIE_INCLUDED
