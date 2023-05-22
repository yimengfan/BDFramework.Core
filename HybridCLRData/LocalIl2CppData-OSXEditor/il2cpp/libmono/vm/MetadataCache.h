#pragma once

#include "il2cpp-config.h"
#include "mono-runtime/il2cpp-mapping.h"

namespace mono
{
namespace vm
{
    class MetadataCache
    {
    public:

        static bool Initialize();

        static const MonoMethodInfoMetadata* GetMonoGenericMethodInfoFromMethodHash(uint64_t hash);
        static const char* GetStringFromIndex(StringIndex index);
        static const MonoMetadataToken* GetMonoStringTokenFromIndex(StringIndex index);
        static const MonoMethodMetadata* GetMonoMethodMetadataFromIndex(MethodIndex index);
        static const MonoMethodMetadata* GetMonoMethodMetadataFromHash(uint64_t hash);
        static const TypeIndex* GetGenericArgumentIndices(int32_t offset);
        static const MonoClassMetadata* GetClassMetadataFromIndex(TypeIndex index);
        static const MonoFieldMetadata* GetFieldMetadataFromIndex(EncodedMethodIndex index);
        static MethodIndex GetGenericMethodIndex(uint32_t index);
        static const Il2CppMetadataUsageList* GetMetadataUsageList(uint32_t index);
        static const Il2CppMetadataUsagePair* GetMetadataUsagePair(uint32_t offset);
        static const char* GetMonoAssemblyNameFromIndex(AssemblyIndex index);
        static int GetMonoAssemblyCount();
    };
} // namespace mono
} // namespace il2cpp
