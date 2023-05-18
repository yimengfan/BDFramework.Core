#pragma once

namespace mono
{
namespace vm
{
    class MetadataLoader
    {
    public:
        static void* LoadMetadataFile(const char* fileName);
    };
} // namespace mono
} // namespace il2cpp
