#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include <stdexcept>
#include "../../Clang/ClangSymbolParser.h"

using namespace mapfileparser;

TEST_CASE("ClangSymbolParser_ThrowsAnExceptionOnEmptyInput")
{
    REQUIRE_THROWS_AS(ClangSymbolParser::Parse(""), std::runtime_error);
}

TEST_CASE("ClangSymbolParser_ParsersA32BitSymbol")
{
    ClangSymbol clangSymbol = ClangSymbolParser::Parse(" 0x00001F40\t0x000000D0\t[  3] _U3CRegisterObjectU3Ec__AnonStorey2__ctor_m12113");
    REQUIRE(0x00001F40 == clangSymbol.symbol.start);
    REQUIRE(0x000000D0 == clangSymbol.symbol.length);
    REQUIRE("_U3CRegisterObjectU3Ec__AnonStorey2__ctor_m12113" == clangSymbol.symbol.name);
    REQUIRE(3 == clangSymbol.objectFileIndex);
}

TEST_CASE("ClangSymbolParser_ParsersA64BitSymbol")
{
    ClangSymbol clangSymbol = ClangSymbolParser::Parse(" 0x100EC9264\t0x0000004C\t[709] __ZNK5Umbra11TomeContext10getGateMapEPKv");
    REQUIRE(0x100EC9264 == clangSymbol.symbol.start);
    REQUIRE(0x0000004C == clangSymbol.symbol.length);
    REQUIRE("__ZNK5Umbra11TomeContext10getGateMapEPKv" == clangSymbol.symbol.name);
    REQUIRE(709 == clangSymbol.objectFileIndex);
}

#endif // ENABLE_UNIT_TESTS
