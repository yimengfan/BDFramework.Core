#include "il2cpp-config.h"
#include "MetadataCache.h"
#include "MetadataLoader.h"
#include "mono-runtime/il2cpp-mapping.h"
#include "utils/Il2CppHashMap.h"
#include "utils/HashUtils.h"
#include "utils/StringUtils.h"

using namespace mono::vm;

typedef Il2CppHashMap<uint64_t, const MonoMethodInfoMetadata*, il2cpp::utils::PassThroughHash<uint64_t> > MonoMethodInfoMap;
typedef Il2CppHashMap<uint64_t, const RuntimeGenericContextInfo*, il2cpp::utils::PassThroughHash<uint64_t> > MonoRgctxInfoMap;
typedef Il2CppHashMap<uint64_t, const MonoMethodMetadata*, il2cpp::utils::PassThroughHash<uint64_t> > MonoMethodMetadataMap;

static MonoMethodInfoMap s_GenericMonoMethodInfoMap;
static MonoMethodMetadataMap s_MonoMethodMetadataMap;

static void* s_GlobalMonoMetadata;
static const Il2CppGlobalMonoMetadataHeader* s_GlobalMonoMetadataHeader;

bool MetadataCache::Initialize()
{
    s_GlobalMonoMetadata = MetadataLoader::LoadMetadataFile("global-metadata.dat");
    if (!s_GlobalMonoMetadata)
        return false;
    s_GlobalMonoMetadataHeader = (const Il2CppGlobalMonoMetadataHeader*)s_GlobalMonoMetadata;
    IL2CPP_ASSERT(s_GlobalMonoMetadataHeader->sanity == 0xFAB11BAF);
    IL2CPP_ASSERT(s_GlobalMonoMetadataHeader->version == 1);

    const MonoMethodInfoMetadata* methodInfo = (const MonoMethodInfoMetadata*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->genericMethodInfoMappingOffset);
    int numMethods = s_GlobalMonoMetadataHeader->genericMethodInfoMappingCount / sizeof(MonoMethodInfoMetadata);
    for (int i = 0; i < numMethods; ++i)
    {
        s_GenericMonoMethodInfoMap.add(methodInfo->hash, methodInfo);
        ++methodInfo;
    }

    const MonoMethodMetadata* monoMethodMetadata = (const MonoMethodMetadata*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->methodMetadataOffset);
    int numElements = s_GlobalMonoMetadataHeader->methodMetadataCount / sizeof(MonoMethodMetadata);
    for (int i = 0; i < numElements; ++i)
    {
        s_MonoMethodMetadataMap.add(monoMethodMetadata->hash, monoMethodMetadata);
        ++monoMethodMetadata;
    }
    return true;
}

const MonoMethodInfoMetadata* MetadataCache::GetMonoGenericMethodInfoFromMethodHash(uint64_t hash)
{
    MonoMethodInfoMap::const_iterator it = s_GenericMonoMethodInfoMap.find(hash);
    if (it != s_GenericMonoMethodInfoMap.end())
        return it->second;
    else
        return NULL;
}

const char* MetadataCache::GetStringFromIndex(StringIndex index)
{
    IL2CPP_ASSERT(index <= s_GlobalMonoMetadataHeader->stringCount);
    const char* strings = ((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->stringOffset) + index;
    return strings;
}

const MonoMetadataToken* MetadataCache::GetMonoStringTokenFromIndex(StringIndex index)
{
    IL2CPP_ASSERT((index * sizeof(MonoMetadataToken)) <= (uint32_t)s_GlobalMonoMetadataHeader->monoStringCount);
    return (MonoMetadataToken*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->monoStringOffset + (index * sizeof(MonoMetadataToken)));
}

const MonoMethodMetadata* MetadataCache::GetMonoMethodMetadataFromIndex(MethodIndex index)
{
    IL2CPP_ASSERT((index * sizeof(MonoMethodMetadata)) <= (uint32_t)s_GlobalMonoMetadataHeader->methodMetadataCount);
    return (MonoMethodMetadata*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->methodMetadataOffset + (index * sizeof(MonoMethodMetadata)));
}

const MonoMethodMetadata* MetadataCache::GetMonoMethodMetadataFromHash(uint64_t hash)
{
    MonoMethodMetadataMap::const_iterator it = s_MonoMethodMetadataMap.find(hash);
    if (it != s_MonoMethodMetadataMap.end())
        return it->second;
    else
        return NULL;
}

const TypeIndex* MetadataCache::GetGenericArgumentIndices(int32_t offset)
{
    IL2CPP_ASSERT((offset * sizeof(TypeIndex)) <= (uint32_t)s_GlobalMonoMetadataHeader->genericArgumentIndicesCount);
    return (TypeIndex*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->genericArgumentIndicesOffset + (offset * sizeof(TypeIndex)));
}

const MonoClassMetadata* MetadataCache::GetClassMetadataFromIndex(TypeIndex index)
{
    IL2CPP_ASSERT((index * sizeof(MonoClassMetadata)) <= (uint32_t)s_GlobalMonoMetadataHeader->typeTableCount);
    return (MonoClassMetadata*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->typeTableOffset + (index * sizeof(MonoClassMetadata)));
}

const MonoFieldMetadata* MetadataCache::GetFieldMetadataFromIndex(EncodedMethodIndex index)
{
    IL2CPP_ASSERT((index * sizeof(MonoFieldMetadata)) <= (uint32_t)s_GlobalMonoMetadataHeader->fieldTableCount);
    return (MonoFieldMetadata*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->fieldTableOffset + (index * sizeof(MonoFieldMetadata)));
}

MethodIndex  MetadataCache::GetGenericMethodIndex(uint32_t index)
{
    IL2CPP_ASSERT((index * sizeof(MethodIndex)) <= (uint32_t)s_GlobalMonoMetadataHeader->genericMethodIndexTableCount);
    return *(MethodIndex*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->genericMethodIndexTableOffset + (index * sizeof(MethodIndex)));
}

const Il2CppMetadataUsageList* MetadataCache::GetMetadataUsageList(uint32_t index)
{
    IL2CPP_ASSERT((index * sizeof(Il2CppMetadataUsageList)) <= (uint32_t)s_GlobalMonoMetadataHeader->metaDataUsageListsTableCount);
    return (Il2CppMetadataUsageList*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->metaDataUsageListsTableOffset + (index * sizeof(Il2CppMetadataUsageList)));
}

const Il2CppMetadataUsagePair* MetadataCache::GetMetadataUsagePair(uint32_t offset)
{
    IL2CPP_ASSERT((offset * sizeof(Il2CppMetadataUsagePair)) <= (uint32_t)s_GlobalMonoMetadataHeader->metaDataUsagePairsTableCount);
    return (Il2CppMetadataUsagePair*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->metaDataUsagePairsTableOffset + (offset * sizeof(Il2CppMetadataUsagePair)));
}

const char* MetadataCache::GetMonoAssemblyNameFromIndex(AssemblyIndex aIndex)
{
    IL2CPP_ASSERT((aIndex * sizeof(StringIndex)) <= (uint32_t)s_GlobalMonoMetadataHeader->assemblyNameTableCount);
    StringIndex* sIndex = (StringIndex*)((const char*)s_GlobalMonoMetadata + s_GlobalMonoMetadataHeader->assemblyNameTableOffset + (aIndex * sizeof(StringIndex)));
    return GetStringFromIndex(*sIndex);
}

int MetadataCache::GetMonoAssemblyCount()
{
    return s_GlobalMonoMetadataHeader->assemblyNameTableCount / sizeof(StringIndex);
}
