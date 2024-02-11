#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include <sstream>
#include "../../MSVC/MSVCMapFileParser.h"

using namespace mapfileparser;

static const char* mockMapFile = "\
 _test\n\
\n\
 Timestamp is 5559f772 (Mon May 18 10:30 : 10 2015)\n\
\n\
 Preferred load address is 00400000\n\
\n\
 Start         Length     Name                   Class\n\
 0001:00000000 0096f9beH .text                   CODE\n\
 0001:0096f9c0 00089033H .text$x                 CODE\n\
 0001:009f8a00 00039714H .text$yc                CODE\n\
 0001:00a32120 00000411H .text$yd                CODE\n\
 0002:00000000 00000304H .idata$5                DATA\n\
 0002:00000304 00000004H .CRT$XCA                DATA\n\
 0002:00000308 0000000cH .CRT$XCC                DATA\n\
 0002:00000314 0000001cH .CRT$XCL                DATA\n\
 0002:00000330 000000ecH .CRT$XCU                DATA\n\
 0002:0000041c 00000004H .CRT$XCZ                DATA\n\
 0002:00000420 00000004H .CRT$XIA                DATA\n\
 0002:00000424 00000014H .CRT$XIC                DATA\n\
 0002:00000438 00000004H .CRT$XID                DATA\n\
 0002:0000043c 00000004H .CRT$XIY                DATA\n\
 0002:00000440 00000004H .CRT$XIZ                DATA\n\
 0002:00000444 00000004H .CRT$XPA                DATA\n\
 0002:00000448 00000008H .CRT$XPX                DATA\n\
 0002:00000450 00000004H .CRT$XPXA               DATA\n\
 0002:00000454 00000004H .CRT$XPZ                DATA\n\
 0002:00000458 00000004H .CRT$XTA                DATA\n\
 0002:0000045c 00000004H .CRT$XTZ                DATA\n\
 0002:00000460 0018a7e8H .rdata                  DATA\n\
 0002:0018ac48 0000007bH .rdata$debug            DATA\n\
 0002:0018acc4 00000cd4H .rdata$r                DATA\n\
 0002:0018b9a0 0000f1acH .rdata$sxdata           DATA\n\
 0002:0019ab4c 00000004H .rtc$IAA                DATA\n\
 0002:0019ab50 00000004H .rtc$IZZ                DATA\n\
 0002:0019ab54 00000004H .rtc$TAA                DATA\n\
 0002:0019ab58 00000004H .rtc$TZZ                DATA\n\
 0002:0019ab60 000b6c1cH .xdata$x                DATA\n\
 0002:0025177c 00000078H .idata$2                DATA\n\
 0002:002517f4 00000014H .idata$3                DATA\n\
 0002:00251808 00000304H .idata$4                DATA\n\
 0002:00251b0c 00000bcaH .idata$6                DATA\n\
 0002:002526e0 000016fbH .edata                  DATA\n\
 0003:00000000 0023deb4H .data                   DATA\n\
 0003:0023dec0 0004b020H .bss                    DATA\n\
\n\
  Address         Publics by Value              Rva+Base       Lib:Object\n\
\n\
 0000:00000000       __except_list              00000000     <absolute>\n\
 0000:00003c6b       ___safe_se_handler_count   00003c6b     <absolute>\n\
 0000:00000000       ___ImageBase               00400000     <linker - defined>\n\
 0001:00000000       _StreamingContext__ctor_m12118 00401000 f   98F5E16DA77CA137853023FCE84040FD.obj\n\
 0001:000000d0       _StreamingContext__ctor_m12119 004010d0 f   98F5E16DA77CA137853023FCE84040FD.obj\n\
 0002:00000000       __imp__CryptReleaseContext@8 00e34000     advapi32:ADVAPI32.dll\n\
 0002:00000004       __imp__CryptAcquireContextW@20 00e34004     advapi32:ADVAPI32.dll\n\
 0003:00000000 ??_R0?AUIl2CppExceptionWrapper@@@8 01088000     98F5E16DA77CA137853023FCE84040FD.obj\n\
 0003:00000028 ?Arrays_GenericArrayField_1__ctor_m14458_GenericMethod@@3UIl2CppGenericMethod@@A 01088028     E9FD63517D52C89694E224C19AA23AB9.obj";

TEST_CASE("Parsing_MSVC_MapFile_ContainsExpectedSectionsAndSymbols")
{
    std::stringstream ss(mockMapFile);
    MSVCMapFileParser parser;
    MapFile mapFile = parser.Parse(ss);
    REQUIRE(37 == mapFile.sections.size());

    Section section = mapFile.sections[0];
    REQUIRE(0x00001000 == section.start);
    REQUIRE(0x0096f9be == section.length);
    REQUIRE(".text" == section.name);
    REQUIRE("1" == section.segmentName);

    section = mapFile.sections[4];
    REQUIRE(0x00A34000 == section.start);
    REQUIRE(0x00000304 == section.length);
    REQUIRE(".idata$5" == section.name);
    REQUIRE("2" == section.segmentName);

    section = mapFile.sections[36];
    REQUIRE(0x00EC5EC0 == section.start);
    REQUIRE(0x0004b020 == section.length);
    REQUIRE(".bss" == section.name);
    REQUIRE("3" == section.segmentName);

    REQUIRE(9 == mapFile.symbols.size());
    Symbol symbol = mapFile.symbols[3];
    REQUIRE(0x00001000 == symbol.start);
    REQUIRE(0x000000d0 == symbol.length);
    REQUIRE(kSegmentTypeCode == symbol.segmentType);

    symbol = mapFile.symbols[4];
    REQUIRE(0x000010d0 == symbol.start);
    REQUIRE(0x0096F8EE == symbol.length);
    REQUIRE(kSegmentTypeCode == symbol.segmentType);

    symbol = mapFile.symbols[8];
    REQUIRE(0x00C88028 == symbol.start);
    REQUIRE(0x0023DE8C == symbol.length);
    REQUIRE(kSegmentTypeData == symbol.segmentType);
}

#endif // ENABLE_UNIT_TESTS
