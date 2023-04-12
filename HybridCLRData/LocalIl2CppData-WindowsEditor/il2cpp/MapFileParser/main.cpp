#include <iostream>
#include "Driver.h"

#if ENABLE_UNIT_TESTS
#define CATCH_CONFIG_RUNNER
#include "../external/Catch/catch.hpp"
#endif

int tmain(int argc, NativeChar** argv)
{
#if ENABLE_UNIT_TESTS
    return Catch::Session().run(argc, argv);
#endif

    return mapfileparser::Driver::Run(argc, argv, std::cout);
}

#if _WINDOWS
#include <Windows.h>
#include <shellapi.h>

int __stdcall wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nShowCmd)
{
    int argc;
    wchar_t** argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv == NULL)
    {
        wchar_t errorMessage[256];
        int lastError = GetLastError();
        if (FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM, NULL, lastError, 0, errorMessage, 256, NULL) != 0)
        {
            std::cerr << "Failed to convert the command line to argument array: " << errorMessage << std::endl;
        }
        else
        {
            std::cerr << "Failed to convert the command line to argument array." << std::endl;
        }

        return lastError;
    }

    int returnValue = tmain(argc, argv);
    LocalFree(argv);
    return returnValue;
}

#endif
