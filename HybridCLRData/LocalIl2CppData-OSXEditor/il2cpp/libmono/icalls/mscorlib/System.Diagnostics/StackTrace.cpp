#include <stdint.h>
#include <cstddef>
#include "il2cpp-config.h"
#include "mono-api.h"
#include "il2cpp-mono-support.h"
#include "icalls/mscorlib/System.Diagnostics/StackTrace.h"

namespace mono
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
namespace Diagnostics
{
    MonoArray* StackTrace::get_trace(MonoException *exc, int32_t skip, bool need_file_info)
    {
        MonoArray *ta = mono_unity_exception_get_trace_ips(exc);

        MonoClass* stackFrameClass = mono_unity_defaults_get_stack_frame_class();
        if (ta == NULL)
        {
            /* Exception is not thrown yet */
            return MonoArrayNew(stackFrameClass, 0);
        }

        int len = (int)mono_array_length(ta);

        MonoArray* res = MonoArrayNew(stackFrameClass, len > skip ? len - skip : 0);

        for (int i = skip; i < len; i++)
        {
            MonoStackFrame* sf = (MonoStackFrame*)mono_object_new(g_MonoDomain, stackFrameClass);
            MonoMethod* method = mono_array_get(ta, MonoMethod*, i);

            mono_unity_stackframe_set_method(sf, method);
            mono_array_setref(res, i, sf);
        }

        return res;
    }
} /* namespace Diagnostics */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace mono */
