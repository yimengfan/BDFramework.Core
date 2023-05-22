#pragma once

#include <stdint.h>
#include "mono-api.h"

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
    class StackFrame
    {
    public:
        static bool get_frame_info(
            int32_t skip,
            bool needFileInfo,
            MonoReflectionMethod** method,
            int32_t* iloffset,
            int32_t* native_offset,
            MonoString** file,
            int32_t* line,
            int32_t* column);
    };
} /* namespace Diagnostics */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace mono */
