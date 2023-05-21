#pragma once

#include <stdint.h>
#include "il2cpp-config.h"

struct Il2CppString;
struct mscorlib_System_Decimal;

namespace il2cpp
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
    class LIBIL2CPP_CODEGEN_API Decimal
    {
    public:
        static double ToDouble(Il2CppDecimal d);
        static int32_t FCallCompare(Il2CppDecimal* left, Il2CppDecimal* right);
        static int32_t FCallToInt32(Il2CppDecimal d);
        static int32_t GetHashCode(Il2CppDecimal* _this);
        static float ToSingle(Il2CppDecimal d);
        static void ConstructorDouble(Il2CppDecimal* _this, double value);
        static void ConstructorFloat(Il2CppDecimal* _this, float value);
        static void FCallAddSub(Il2CppDecimal* left, Il2CppDecimal* right, uint8_t sign);
        static void FCallDivide(Il2CppDecimal* left, Il2CppDecimal* right);
        static void FCallFloor(Il2CppDecimal* d);
        static void FCallMultiply(Il2CppDecimal* d1, Il2CppDecimal* d2);
        static void FCallRound(Il2CppDecimal* d, int32_t decimals);
        static void FCallTruncate(Il2CppDecimal* d);
    };
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace il2cpp */
