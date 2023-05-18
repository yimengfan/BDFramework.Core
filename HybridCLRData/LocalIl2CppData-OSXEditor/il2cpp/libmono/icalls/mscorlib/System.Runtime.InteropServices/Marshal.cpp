#include <cstdlib>
#include "Marshal.h"
#include "vm/PlatformInvoke.h"

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
    intptr_t Marshal::GetFunctionPointerForDelegateInternal(MonoDelegate* d)
    {
        return mono::vm::PlatformInvoke::MarshalDelegate(d);
    }

    Il2CppDelegate* Marshal::GetDelegateForFunctionPointerInternal(intptr_t ptr, MonoReflectionType* t)
    {
        MonoClass *delegateType = mono_type_get_class(mono_unity_reflection_type_get_type(t));
        if (!mono_class_init(delegateType))
        {
            mono_set_pending_exception(mono_class_get_exception_for_failure(delegateType));
            return NULL;
        }
        return mono::vm::PlatformInvoke::MarshalFunctionPointerToDelegate(reinterpret_cast<void*>(ptr), delegateType);
    }
} /* namespace InteropServices */
} /* namespace Runtime */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace mono */
