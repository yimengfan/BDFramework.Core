#include "il2cpp-config.h"
#include "il2cpp-class-internals.h"
#include "Il2CppGenericContextCompare.h"
#include "Il2CppGenericInstCompare.h"

namespace il2cpp
{
namespace metadata
{
    bool Il2CppGenericContextCompare::operator()(const Il2CppGenericContext* gc1, const Il2CppGenericContext* gc2) const
    {
        return Compare(gc1, gc2);
    }

    bool Il2CppGenericContextCompare::Compare(const Il2CppGenericContext* gc1, const Il2CppGenericContext* gc2)
    {
        //bool ret = gc1->class_inst == gc2->class_inst && gc1->method_inst == gc2->method_inst;
        //IL2CPP_ASSERT(ret == (Il2CppGenericInstCompare::Compare(gc1->class_inst, gc2->class_inst) && Il2CppGenericInstCompare::Compare(gc1->method_inst, gc2->method_inst)));
        return Il2CppGenericInstCompare::Compare(gc1->class_inst, gc2->class_inst) && Il2CppGenericInstCompare::Compare(gc1->method_inst, gc2->method_inst);
        //return ret;
    }
} /* namespace vm */
} /* namespace il2cpp */
