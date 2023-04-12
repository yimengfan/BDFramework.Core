#if ENABLE_UNIT_TESTS

#include "../../external/Catch/catch.hpp"

#include <sstream>
#include <string>
#include "../Driver.h"

using namespace mapfileparser;

TEST_CASE("DriverReturnsCorrectValueWithIncorrectNumberOfArguments")
{
    std::stringstream unused;
    REQUIRE(1 == Driver::Run(1, NULL, unused));
}

TEST_CASE("DriverOutputsCorrectErrorWithIncorrectNumberOfArguments")
{
    std::stringstream output;
    Driver::Run(1, NULL, output);
    REQUIRE("Usage: MapFileParser -format=<MSVC|Clang|SNC|GCC> mapFile <-stats|outputFile>\n" == output.str());
}

TEST_CASE("DriverReturnsCorrectValueWithIncorrectMapFileFormat")
{
    const NativeChar* arguments[4] =
    {
        NativeText("Unused"),
        NativeText("-format=Foo"),
        NativeText("Unused"),
        NativeText("Unused")
    };

    std::stringstream unused;
    REQUIRE(1 == Driver::Run(4, arguments, unused));
}

TEST_CASE("DriverOutputsCorrectErrorWithIncorrectMapFileFormat")
{
    const NativeChar* arguments[4] =
    {
        NativeText("Unused"),
        NativeText("-format=Foo"),
        NativeText("Unused"),
        NativeText("Unused")
    };

    std::stringstream output;
    Driver::Run(4, arguments, output);
    REQUIRE("Unknown map file format.\nUsage: MapFileParser -format=<MSVC|Clang|SNC|GCC> mapFile <-stats|outputFile>\n" == output.str());
}

TEST_CASE("DriverReturnsCorrectValueWithBadInputFile")
{
    const NativeChar* arguments[4] =
    {
        NativeText("Unused"),
        NativeText("-format=MSVC"),
        NativeText("NonexistentFile"),
        NativeText("Unused")
    };

    std::stringstream unused;
    REQUIRE(1 == Driver::Run(4, arguments, unused));
}

TEST_CASE("DriverOutputsCorrectErrorWithBadInputFile")
{
    const NativeChar* arguments[4] =
    {
        NativeText("Unused"),
        NativeText("-format=MSVC"),
        NativeText("NonexistentFile"),
        NativeText("Unused")
    };

    std::stringstream output;
    Driver::Run(4, arguments, output);
    REQUIRE("Map file NonexistentFile cannot be opened.\n" == output.str());
}

#endif // ENABLE_UNIT_TESTS
