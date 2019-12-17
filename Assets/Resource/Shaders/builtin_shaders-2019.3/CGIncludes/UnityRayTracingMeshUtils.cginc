// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_RAY_TRACING_MESH_UTILS_INCLUDED
#define UNITY_RAY_TRACING_MESH_UTILS_INCLUDED

// This helper file contains a list of utility functions needed to fetch vertex attributes from within closesthit or anyhit shaders.

// HLSL example:
// struct Vertex
// {
//     float3 position;
//     float2 texcoord;
// };

// Vertex FetchVertex(uint vertexIndex)
// {
//      Vertex v;
//      v.position = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
//      v.texcoord = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
//      return v;
// }

// uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());
// Vertex v0, v1, v2;
// v0 = FetchVertex(triangleIndices.x);
// v1 = FetchVertex(triangleIndices.y);
// v2 = FetchVertex(triangleIndices.z);
// Interpolate the vertices using the barycentric coordinates available as input to the closesthit or anyhit shaders.

#define MAX_VERTEX_STREAM_COUNT 4

uint unity_MeshIndexSize_RT;                                         // 0 when an index buffer is not used, 2 for 16-bit indices or 4 for 32-bit indices.
uint unity_MeshVertexSize_RT/*[MAX_VERTEX_STREAM_COUNT]*/;           // The stride between 2 consecutive vertices in the vertex buffer. Only one vertex stream is supported at this moment.
uint unity_MeshBaseVertex_RT;                                        // A value added to each index before reading a vertex from the vertex buffer.
uint unity_MeshIndexStart_RT;                                        // The location of the first index to read from the index buffer.
uint unity_MeshStartVertex_RT;                                       // Index of the first vertex - used when an index buffer is not used.

struct VertexAttributeInfo
{
    uint InputSlot;         // Not supported. Always assumed to be 0.
    uint Format;
    uint ByteOffset;
    uint Dimension;
};

// Valid values for the attributeType parameter in UnityRayTracingFetchVertexAttribute* functions.
#define kVertexAttributePosition    0
#define kVertexAttributeNormal      1
#define kVertexAttributeTangent     2
#define kVertexAttributeColor       3
#define kVertexAttributeTexCoord0   4
#define kVertexAttributeTexCoord1   5
#define kVertexAttributeTexCoord2   6
#define kVertexAttributeTexCoord3   7
#define kVertexAttributeTexCoord4   8
#define kVertexAttributeTexCoord5   9
#define kVertexAttributeTexCoord6   10
#define kVertexAttributeTexCoord7   11

// Supported
#define kVertexFormatFloat          0
#define kVertexFormatFloat16        1
// Not supported
#define kVertexFormatUNorm8         2
#define kVertexFormatSNorm8         3
#define kVertexFormatUNorm16        4
#define kVertexFormatSNorm16        5
#define kVertexFormatUInt8          6
#define kVertexFormatSInt8          7
#define kVertexFormatUInt16         8
#define kVertexFormatSInt16         9
#define kVertexFormatUInt32         10
#define kVertexFormatSInt32         11

StructuredBuffer<VertexAttributeInfo> unity_MeshVertexDeclaration_RT;

ByteAddressBuffer unity_MeshVertexBuffer_RT/*[MAX_VERTEX_STREAM_COUNT]*/;    // Only one vertex stream is supported at this moment.
ByteAddressBuffer unity_MeshIndexBuffer_RT;

uint3 UnityRayTracingFetchTriangleIndices(uint primitiveIndex)
{
    uint3 indices;

    if (unity_MeshIndexSize_RT == 2)
    {
        const uint offsetInBytes = (unity_MeshIndexStart_RT + primitiveIndex * 3) << 1;
        const uint dwordAlignedOffset = offsetInBytes & ~3;
        const uint2 fourIndices = unity_MeshIndexBuffer_RT.Load2(dwordAlignedOffset);

        if (dwordAlignedOffset == offsetInBytes)
        {
            indices.x = fourIndices.x & 0xffff;
            indices.y = (fourIndices.x >> 16) & 0xffff;
            indices.z = fourIndices.y & 0xffff;
        }
        else
        {
            indices.x = (fourIndices.x >> 16) & 0xffff;
            indices.y = fourIndices.y & 0xffff;
            indices.z = (fourIndices.y >> 16) & 0xffff;
        }

        indices = indices + unity_MeshBaseVertex_RT.xxx;
    }
    else if (unity_MeshIndexSize_RT == 4)
    {
        const uint offsetInBytes = (unity_MeshIndexStart_RT + primitiveIndex * 3) << 2;
        indices = unity_MeshIndexBuffer_RT.Load3(offsetInBytes) + unity_MeshBaseVertex_RT.xxx;
    }
    else // unity_RayTracingMeshIndexSize == 0
    {
        const uint firstVertexIndex = primitiveIndex * 3 + unity_MeshStartVertex_RT;
        indices = firstVertexIndex.xxx + uint3(0, 1, 2);
    }

    return indices;
}

#define INVALID_VERTEX_ATTRIBUTE_OFFSET 0xFFFFFFFF

// attributeType is one of the kVertexAttribute* defines
float2 UnityRayTracingFetchVertexAttribute2(uint vertexIndex, uint attributeType)
{
    const uint attributeByteOffset  = unity_MeshVertexDeclaration_RT[attributeType].ByteOffset;
    const uint attributeDimension   = unity_MeshVertexDeclaration_RT[attributeType].Dimension;

    if (attributeByteOffset == INVALID_VERTEX_ATTRIBUTE_OFFSET || attributeDimension < 2)
        return float2(0, 0);

    const uint vertexAddress    = vertexIndex * unity_MeshVertexSize_RT;
    const uint attributeAddress = vertexAddress + attributeByteOffset;
    const uint attributeFormat  = unity_MeshVertexDeclaration_RT[attributeType].Format;

    if (attributeFormat == kVertexFormatFloat)
    {
        return asfloat(unity_MeshVertexBuffer_RT.Load2(attributeAddress));
    }
    else if (attributeFormat == kVertexFormatFloat16)
    {
        const uint twoHalfs = unity_MeshVertexBuffer_RT.Load(attributeAddress);
        return float2(f16tof32(twoHalfs), f16tof32(twoHalfs >> 16));
    }
    else
        // Vertex attribute format not supported.
        return float2(0, 0);
}

// attributeType is one of the kVertexAttribute* defines
float3 UnityRayTracingFetchVertexAttribute3(uint vertexIndex, uint attributeType)
{
    const uint attributeByteOffset  = unity_MeshVertexDeclaration_RT[attributeType].ByteOffset;
    const uint attributeDimension   = unity_MeshVertexDeclaration_RT[attributeType].Dimension;

    if (attributeByteOffset == INVALID_VERTEX_ATTRIBUTE_OFFSET || attributeDimension < 3)
        return float3(0, 0, 0);

    const uint vertexAddress    = vertexIndex * unity_MeshVertexSize_RT;
    const uint attributeAddress = vertexAddress + attributeByteOffset;
    const uint attributeFormat  = unity_MeshVertexDeclaration_RT[attributeType].Format;

    if (attributeFormat == kVertexFormatFloat)
    {
        return asfloat(unity_MeshVertexBuffer_RT.Load3(attributeAddress));
    }
    else if (attributeFormat == kVertexFormatFloat16)
    {
        const uint2 fourHalfs = unity_MeshVertexBuffer_RT.Load2(attributeAddress);
        return float3(f16tof32(fourHalfs.x), f16tof32(fourHalfs.x >> 16), f16tof32(fourHalfs.y));
    }
    else
        // Vertex attribute format not supported.
        return float3(0, 0, 0);
}

// attributeType is one of the kVertexAttribute* defines
float4 UnityRayTracingFetchVertexAttribute4(uint vertexIndex, uint attributeType)
{
    const uint attributeByteOffset  = unity_MeshVertexDeclaration_RT[attributeType].ByteOffset;
    const uint attributeDimension   = unity_MeshVertexDeclaration_RT[attributeType].Dimension;

    if (attributeByteOffset == INVALID_VERTEX_ATTRIBUTE_OFFSET || attributeDimension < 4)
        return float4(0, 0, 0, 0);

    const uint vertexAddress    = vertexIndex * unity_MeshVertexSize_RT;
    const uint attributeAddress = vertexAddress + attributeByteOffset;
    const uint attributeFormat  = unity_MeshVertexDeclaration_RT[attributeType].Format;

    if (attributeFormat == kVertexFormatFloat)
    {
        return asfloat(unity_MeshVertexBuffer_RT.Load4(attributeAddress));
    }
    else if (attributeFormat == kVertexFormatFloat16)
    {
        const uint2 fourHalfs = unity_MeshVertexBuffer_RT.Load2(attributeAddress);
        return float4(f16tof32(fourHalfs.x), f16tof32(fourHalfs.x >> 16), f16tof32(fourHalfs.y), f16tof32(fourHalfs.y >> 16));
    }
    else
        // Vertex attribute format not supported.
        return float4(0, 0, 0, 0);
}

#endif  //#ifndef UNITY_RAY_TRACING_MESH_UTILS_INCLUDED
