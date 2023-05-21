#include "il2cpp-config.h"

#if IL2CPP_TARGET_WINDOWS

#include "os/Image.h"

#include "WindowsHeaders.h"

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

namespace il2cpp
{
namespace os
{
namespace Image
{
    static void InitializeManagedSection()
    {
        PIMAGE_NT_HEADERS ntHeaders = (PIMAGE_NT_HEADERS)(((char*)&__ImageBase) + __ImageBase.e_lfanew);
        PIMAGE_SECTION_HEADER sectionHeader = (PIMAGE_SECTION_HEADER)((char*)&ntHeaders->OptionalHeader + ntHeaders->FileHeader.SizeOfOptionalHeader);
        for (int i = 0; i < ntHeaders->FileHeader.NumberOfSections; i++)
        {
            if (strncmp(IL2CPP_BINARY_SECTION_NAME, (char*)sectionHeader->Name, IMAGE_SIZEOF_SHORT_NAME) == 0)
            {
                void* start = (char*)&__ImageBase + sectionHeader->VirtualAddress;
                void* end = (char*)start + sectionHeader->Misc.VirtualSize;
                SetManagedSectionStartAndEnd(start, end);
            }
            sectionHeader++;
        }
    }

    void Initialize()
    {
        InitializeManagedSection();
    }

    void* GetImageBase()
    {
        return &__ImageBase;
    }
}
}
}

#endif
