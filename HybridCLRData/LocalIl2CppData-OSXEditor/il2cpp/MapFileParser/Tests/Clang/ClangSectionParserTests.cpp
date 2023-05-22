#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include <stdexcept>
#include "../../Clang/ClangSectionParser.h"

using namespace mapfileparser;

TEST_CASE("ClangSectionParser_ThrowsAnExceptionOnEmptyInput")
{
    REQUIRE_THROWS_AS(ClangSectionParser::Parse(""), std::runtime_error);
}

TEST_CASE("ClangSectionParser_ParsesA32BitSection")
{
    Section section = ClangSectionParser::Parse("0x00001F40\t0x00DDC22F\t__TEXT\t__text");
    REQUIRE(0x00001F40 == section.start);
    REQUIRE(0x00DDC22F == section.length);
    REQUIRE("__text" == section.name);
    REQUIRE("__TEXT" == section.segmentName);
    REQUIRE(kSegmentTypeCode == section.segmentType);
}

TEST_CASE("ClangSectionParser_ParsesA64BitSection")
{
    Section section = ClangSectionParser::Parse("0x1000072D0\t0x018B902C\t__TEXT\t__text");
    REQUIRE(0x1000072D0 == section.start);
    REQUIRE(0x018B902C == section.length);
    REQUIRE("__text" == section.name);
    REQUIRE("__TEXT" == section.segmentName);
    REQUIRE(kSegmentTypeCode == section.segmentType);
}

TEST_CASE("ClangSectionParser_ParsesADataSegment")
{
    Section section = ClangSectionParser::Parse("0x01059000\t0x00015ABC\t__DATA\t__nl_symbol_ptr");
    REQUIRE(0x01059000 == section.start);
    REQUIRE(0x00015ABC == section.length);
    REQUIRE("__nl_symbol_ptr" == section.name);
    REQUIRE("__DATA" == section.segmentName);
    REQUIRE(kSegmentTypeData == section.segmentType);
}

#endif // ENABLE_UNIT_TESTS
