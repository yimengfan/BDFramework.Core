#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include "../../SNC/SNCMapFileParser.h"
#include <sstream>

using namespace mapfileparser;

static const char* mockMapFile = "Address  Size     Align Out     In      File    Symbol\n\
=================================================================\n\
UNDEFINED\n\
module_start\n\
module_stop\n\
module_exit\n\
_sceLdTlsDescRegionInfo\n\
__ARM_unwind_cpp_prcommon\n\
81000000 003b220c     4 .text\n\
81000000 00000100     4         .text\n\
81000000 00000100     4                 E:/UnityInternal/Test Projects/il2cpp/Test/Temp/StagingArea/il2cppOutput/Assembly-CSharp.o\n\
003b220c 00000000     0                         __text_end\n\
81000000 00000000     0                         __start__Ztext\n\
81000100 00000100     4                 E:/UnityInternal/Test Projects/il2cpp/Test/Temp/StagingArea/il2cppOutput/Bulk_Assembly-CSharpMetadata_0.o\n\
81000200 000004c8     4                 E:/UnityInternal/Test Projects/il2cpp/Test/Temp/StagingArea/il2cppOutput/Bulk_Assembly-CSharp_0.o\n\
81000201 00000016     0                         Test__ctor_m0\n\
81000227 00000136     0                         Test_Start_m1\n\
810004c5 00000080     0                         Test_Method1_m4\n\
8100035d 0000000a     0                         Test_Update_m2\n\
81000545 00000084     0                         Test_Method2_m5\n\
";

TEST_CASE("Parsing_SNC_MapFile_ContainsExpectedSectionsAndSymbols")
{
    std::stringstream ss(mockMapFile);
    SNCMapFileParser parser;
    MapFile mapFile = parser.Parse(ss);
    REQUIRE(1 == mapFile.sections.size());

    Section section = mapFile.sections[0];
    REQUIRE(0x81000000 == section.start);
    REQUIRE(0x003b220c == section.length);
    REQUIRE(".text" == section.name);
    REQUIRE(".text" == section.segmentName);

    REQUIRE(5 == mapFile.symbols.size());

    Symbol symbol = mapFile.symbols[0];
    REQUIRE(0x00000200 == symbol.start);
    REQUIRE(0x00000016 == symbol.length);
    REQUIRE("Test__ctor_m0" == symbol.name);
    REQUIRE(kSegmentTypeCode == symbol.segmentType);

    symbol = mapFile.symbols[2];
    REQUIRE(0x0000035c == symbol.start);
    REQUIRE(0x0000000a == symbol.length);
    REQUIRE("Test_Update_m2" == symbol.name);
    REQUIRE(kSegmentTypeCode == symbol.segmentType);
}

#endif // ENABLE_UNIT_TESTS
