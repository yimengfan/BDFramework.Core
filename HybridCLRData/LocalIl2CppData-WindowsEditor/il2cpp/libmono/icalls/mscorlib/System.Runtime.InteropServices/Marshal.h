#pragma once

#include <stdint.h>
#include "il2cpp-config.h"
#include "il2cpp-object-internals.h"

struct Il2CppDelegate;
struct Il2CppReflectionType;

namespace mono
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
namespace Runtime
{
namespace InteropServices
{
    class Marshal
    {
    public:
        static intptr_t GetFunctionPointerForDelegateInternal(MonoDelegate* d);
        static Il2CppDelegate* GetDelegateForFunctionPointerInternal(intptr_t ptr, MonoReflectionType* t);
    };
} /* namespace InteropServices */
} /* namespace Runtime */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace mono */
