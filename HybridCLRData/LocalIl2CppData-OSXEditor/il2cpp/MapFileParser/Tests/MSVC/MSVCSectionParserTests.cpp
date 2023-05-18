#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include <stdexcept>
#include "../../MSVC/MSVCSectionParser.h"

using namespace mapfileparser;

TEST_CASE("MSVCSectionParser_ThrowsAnExceptionOnEmptyInput")
{
    REQUIRE_THROWS_AS(MSVCSectionParser::Parse(""), std::runtime_error);
}

TEST_CASE("MSVCSectionParser_ParsesASection")
{
    Section section = MSVCSectionParser::Parse(" 0001:00000000 0096f9beH .text                   CODE");
    REQUIRE(0x00000000 == section.start);
    REQUIRE(0x0096f9be == section.length);
    REQUIRE(".text" == section.name);
    REQUIRE("1" == section.segmentName);
    REQUIRE(kSegmentTypeCode == section.segmentType);
}

TEST_CASE("MSVCSectionParser_ParsesADataSegment")
{
    Section section = MSVCSectionParser::Parse(" 0002:00000000 00000308H .idata$5                DATA");
    REQUIRE(0x00000000 == section.start);
    REQUIRE(0x00000308 == section.length);
    REQUIRE(".idata$5" == section.name);
    REQUIRE("2" == section.segmentName);
    REQUIRE(kSegmentTypeData == section.segmentType);
}

#endif // ENABLE_UNIT_TESTS
