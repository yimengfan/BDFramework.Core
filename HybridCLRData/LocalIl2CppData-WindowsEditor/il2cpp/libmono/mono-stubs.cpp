#include <cassert>
#include "mono-api.h"
#include "il2cpp-api.h"
#include "vm-utils/Debugger.h"

#ifdef WINDOWS
IL2CPP_EXPORT
void* mono_jit_info_get_code_start(void* jit)
{
    return NULL;
}

IL2CPP_EXPORT
int mono_jit_info_get_code_size(void* jit)
{
    return 0;
}

IL2CPP_EXPORT
MonoJitInfo* mono_jit_info_table_find(MonoDomain * domain, void* ip)
{
    return NULL;
}

#endif

extern void breakpoint_callback(Il2CppSequencePoint* sequencePoint);

// The mono_unity_liveness functions are not preserved in the resulting GameAssembly.dylib
// on macOS unless we reference the address of one of the methods here. This one
// reference is enough to pull in all of them.
extern "C"
{
    struct LivenessState;
    extern void mono_unity_liveness_stop_gc_world(LivenessState* state);
}
void* unused = (void*)&mono_unity_liveness_stop_gc_world;

extern "C"
{
    IL2CPP_EXPORT
    void mono_jit_parse_options(int argc, char * argv[])
    {
        for (int i = 0; i < argc; ++i)
        {
            /* TODO: uncomment after mono debugger changes merged
            if (strncmp(argv[i], "--debugger-agent=", 17) == 0)
            {
                mono_debugger_set_il2cpp_breakpoints(g_Il2CppSequencePointCount, (Il2CppSequencePoint**)g_Il2CppSequencePoints);
                mono_debugger_agent_parse_options(argv[i] + 17);
                //opt->mdb_optimizations = TRUE;
                //enable_debugging = TRUE;
                //mono_debug_init (MONO_DEBUG_FORMAT_MONO);
                il2cpp::utils::Debugger::RegisterCallbacks(breakpoint_callback);
            }
            */
        }
    }

    IL2CPP_EXPORT
    int mono_parse_default_optimizations(const char* p)
    {
        return 0;
    }

    IL2CPP_EXPORT
    char* mono_pmip(void *ip)
    {
        return NULL;
    }

    IL2CPP_EXPORT
    void mono_set_defaults(int verbose_level, int32_t opts)
    {
    }

    IL2CPP_EXPORT
    void mono_unity_jit_cleanup(MonoDomain * domain)
    {
    }

    IL2CPP_EXPORT
    void mono_set_signal_chaining(bool chain)
    {
    }
}
