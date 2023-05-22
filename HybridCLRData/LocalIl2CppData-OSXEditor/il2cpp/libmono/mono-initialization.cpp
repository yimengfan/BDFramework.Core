#include <cassert>
#include "mono-api.h"
#include "il2cpp-api.h"
#include "vm/MetadataCache.h"
#include "utils/Runtime.h"
#include "os/c-api/Allocator.h"

extern void il2cpp_install_callbacks();
extern void il2cpp_mono_runtime_init();

extern "C"
{
    IL2CPP_EXPORT
    MonoDomain* mono_jit_init_version(const char *file, const char* runtime_version)
    {
        register_allocator(mono_unity_alloc);
        mono_tls_init_runtime_keys();
        mono::vm::MetadataCache::Initialize();
        //mono_debugger_agent_init(); * TODO: uncomment after mono debugger changes merged *
        mono_init(file);
        mono_icall_init();
        il2cpp_install_callbacks();
        il2cpp_mono_runtime_init();
        return mono_domain_get();
    }
}
