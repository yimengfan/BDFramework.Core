#pragma once

#include <ostream>

#if _WINDOWS
#define NativeChar wchar_t
#define NativeStrCmp wcscmp
#define NativeText(t) L##t
#define tmain wmain
#else
#define NativeChar char
#define NativeStrCmp strcmp
#define NativeText(t) t
#define tmain main
#endif

namespace mapfileparser
{
    class Driver
    {
    public:
        static int Run(int argc, const NativeChar* const argv[], std::ostream& out);
    };
}
