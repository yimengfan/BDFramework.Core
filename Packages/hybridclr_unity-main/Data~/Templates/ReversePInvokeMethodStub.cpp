#include "../metadata/ReversePInvokeMethodStub.h"
#include "../metadata/MetadataModule.h"

namespace hybridclr
{
namespace metadata
{
#if {PLATFORM_ABI}

	//!!!{{CODE

	void __ReversePInvokeMethod_0(void* xState)
	{
		CallLuaFunction(xState, 0);
	}

	void __ReversePInvokeMethod_1(void* xState)
	{
		CallLuaFunction(xState, 1);
	}

	Il2CppMethodPointer s_ReversePInvokeMethodStub[]
	{
		(Il2CppMethodPointer)__ReversePInvokeMethod_0,
		(Il2CppMethodPointer)__ReversePInvokeMethod_1,
		nullptr,
	};

	//!!!}}CODE
#endif
}
}