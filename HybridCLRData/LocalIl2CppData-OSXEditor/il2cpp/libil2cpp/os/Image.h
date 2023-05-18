#pragma once

namespace il2cpp
{
namespace os
{
namespace Image
{
    void Initialize();
    void* GetImageBase();
    bool IsInManagedSection(void*ip);
    bool ManagedSectionExists();
    void SetManagedSectionStartAndEnd(void* start, void* end);
}
}
}
