#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include <stdexcept>
#include <string>

#include "../../GCC/GCCSymbolParser.h"

using namespace mapfileparser;

TEST_CASE("GCCSymbolParser_ThrowsAnExceptionOnEmptyInput")
{
    REQUIRE_THROWS_AS(GCCSymbolParser::Parse("", ""), std::runtime_error);
}

static std::string firstLine = " .text.Array_InternalArray__ICollection_Contains_TisDTMXPathLinkedNode2_t868737712_m2716310233_gshared";
static std::string secondLine = "                0x000000000039a1f8      0x2a8 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o";

TEST_CASE("GCCSymbolParser_FindsSymbolStart")
{
    Symbol symbol = GCCSymbolParser::Parse(firstLine, secondLine);
    REQUIRE(0x0039a1f8 == symbol.start);
}

TEST_CASE("GCCSymbolParser_FindsSymbolEnd")
{
    Symbol symbol = GCCSymbolParser::Parse(firstLine, secondLine);
    REQUIRE(0x2a8 == symbol.length);
}

TEST_CASE("GCCSymbolParser_SymbolName")
{
    Symbol symbol = GCCSymbolParser::Parse(firstLine, secondLine);
    REQUIRE("Array_InternalArray__ICollection_Contains_TisDTMXPathLinkedNode2_t868737712_m2716310233_gshared" == symbol.name);
}

TEST_CASE("GCCSymbolParser_SymbolObjectFile")
{
    Symbol symbol = GCCSymbolParser::Parse(firstLine, secondLine);
    REQUIRE("/var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o" == symbol.objectFile);
}

TEST_CASE("GCCSymbolParser_NameOnFirstLine")
{
    std::string specialFirstLine = " .text._Znwm    0x0000000001074d4c       0xcc /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/71ED541AC8462284F486FD872FF4956C.o";
    std::string specialSecondLine = "operator new(unsigned long)";
    Symbol symbol = GCCSymbolParser::Parse(specialFirstLine, specialSecondLine);
    REQUIRE("_Znwm" == symbol.name);
}

#endif // ENABLE_UNIT_TESTS
