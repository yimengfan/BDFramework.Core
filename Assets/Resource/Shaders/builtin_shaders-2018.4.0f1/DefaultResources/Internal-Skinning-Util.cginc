// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)


#if defined(SHADER_API_D3D11) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PS4) || defined(SHADER_API_GLCORE) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL) || defined(SHADER_API_SWITCH)
#define STRUCTURED_BUFFER_SUPPORT 1
#else
#define STRUCTURED_BUFFER_SUPPORT 0
#endif

struct MeshVertex
{
    float3 pos;
#if SKIN_NORM
    float3 norm;
#endif
#if SKIN_TANG
    float4 tang;
#endif
};
