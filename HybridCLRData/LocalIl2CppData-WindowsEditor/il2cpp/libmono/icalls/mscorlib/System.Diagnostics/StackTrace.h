#pragma once

#include <stdint.h>
#include "il2cpp-config.h"

struct _MonoArray;
typedef _MonoArray MonoArray;

struct _MonoException;
typedef _MonoException MonoException;

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
    class StackTrace
    {
    public:
        static MonoArray* get_trace(MonoException *exc, int32_t skip, bool need_file_info);
    };
} /* namespace Diagnostics */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace il2cpp */
