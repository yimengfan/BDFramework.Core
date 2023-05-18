#include "il2cpp-config.h"
#include "vm/Assembly.h"
#include "vm/AssemblyName.h"
#include "vm/MetadataCache.h"
#include "vm/Runtime.h"
#include "vm-utils/VmStringUtils.h"
#include "il2cpp-tabledefs.h"
#include "il2cpp-class-internals.h"

#include "os/Mutex.h"
#include "os/Atomic.h"

#include <vector>
#include <string>

#include "hybridclr/metadata/MetadataModule.h"

namespace il2cpp
{
namespace vm
{
    static il2cpp::os::FastMutex s_assemblyLock;
    // copy on write
    static AssemblyVector s_emptyAssemblies;
    static AssemblyVector* s_Assemblies = &s_emptyAssemblies;
    static AssemblyVector* s_lastValidAssemblies = &s_emptyAssemblies;

    AssemblyVector* Assembly::GetAllAssemblies()
    {
        os::FastAutoLock lock(&s_assemblyLock);

        size_t validAssCount = 0;
        bool assemblyChange = false;
        for (AssemblyVector::const_iterator assIt = s_Assemblies->begin(); assIt != s_Assemblies->end(); ++assIt)
        {
            const Il2CppAssembly* ass = *assIt;
            if (ass->token == 0)
            {
                continue;
            }
            if (s_lastValidAssemblies->size() <= validAssCount || (*s_lastValidAssemblies)[validAssCount] != ass)
            {
                assemblyChange = true;
                break;
            }
            ++validAssCount;
        }
        if (assemblyChange)
        {
            s_lastValidAssemblies = new AssemblyVector();
            for (AssemblyVector::const_iterator assIt = s_Assemblies->begin(); assIt != s_Assemblies->end(); ++assIt)
            {
                const Il2CppAssembly* ass = *assIt;
                if (ass->token)
                {
                    s_lastValidAssemblies->push_back(ass);
                }
            }
        }
        return s_lastValidAssemblies;
    }

    const Il2CppAssembly* Assembly::GetLoadedAssembly(const char* name)
    {
        os::FastAutoLock lock(&s_assemblyLock);
        AssemblyVector& assemblies = *s_Assemblies;
        for (AssemblyVector::const_reverse_iterator assembly = assemblies.rbegin(); assembly != assemblies.rend(); ++assembly)
        {
            if (strcmp((*assembly)->aname.name, name) == 0)
                return *assembly;
        }

        return NULL;
    }

    Il2CppImage* Assembly::GetImage(const Il2CppAssembly* assembly)
    {
        return assembly->image;
    }

    void Assembly::GetReferencedAssemblies(const Il2CppAssembly* assembly, AssemblyNameVector* target)
    {
        for (int32_t sourceIndex = 0; sourceIndex < assembly->referencedAssemblyCount; sourceIndex++)
        {
            if (hybridclr::metadata::IsInterpreterImage(assembly->image))
            {
                const Il2CppAssembly* refAssembly = hybridclr::metadata::MetadataModule::GetImage(assembly->image)
                    ->GetReferencedAssembly(sourceIndex, nullptr, assembly->referencedAssemblyCount);
                target->push_back(&refAssembly->aname);
                continue;
            }
            int32_t indexIntoMainAssemblyTable = MetadataCache::GetReferenceAssemblyIndexIntoAssemblyTable(assembly->referencedAssemblyStart + sourceIndex);
            const Il2CppAssembly* refAssembly = MetadataCache::GetAssemblyFromIndex(assembly->image, indexIntoMainAssemblyTable);

            target->push_back(&refAssembly->aname);
        }
    }

    static bool ends_with(const char *str, const char *suffix)
    {
        if (!str || !suffix)
            return false;

        const size_t lenstr = strlen(str);
        const size_t lensuffix = strlen(suffix);
        if (lensuffix >  lenstr)
            return false;

        return strncmp(str + lenstr - lensuffix, suffix, lensuffix) == 0;
    }

    const Il2CppAssembly* Assembly::Load(const char* name)
    {
        const Il2CppAssembly* loadedAssembly = MetadataCache::GetAssemblyByName(name);
        if (loadedAssembly)
        {
            return loadedAssembly;
        }

        if (!ends_with(name, ".dll") && !ends_with(name, ".exe"))
        {
            const size_t len = strlen(name);
            char *tmp = new char[len + 5];

            memset(tmp, 0, len + 5);

            memcpy(tmp, name, len);
            memcpy(tmp + len, ".dll", 4);

            loadedAssembly = MetadataCache::GetAssemblyByName(tmp);

            if (!loadedAssembly)
            {
                memcpy(tmp + len, ".exe", 4);
                loadedAssembly = MetadataCache::GetAssemblyByName(tmp);
            }

            delete[] tmp;

            return loadedAssembly;
        }
        else
        {
            return nullptr;
        }
    }

    void Assembly::Register(const Il2CppAssembly* assembly)
    {
        os::FastAutoLock lock(&s_assemblyLock);

        AssemblyVector* oldAssemblies = s_Assemblies;

        // TODO IL2CPP_MALLOC ???
        AssemblyVector* newAssemblies = oldAssemblies ? new AssemblyVector(*oldAssemblies) : new AssemblyVector();
        newAssemblies->push_back(assembly);
        s_Assemblies = newAssemblies;
        // can't delete oldAssemblies because may be using by other thread
        if (oldAssemblies)
        {
            // can't delete
            // delete oldAssemblies;
        }
    }

    void Assembly::Initialize()
    {
    }
} /* namespace vm */
} /* namespace il2cpp */
