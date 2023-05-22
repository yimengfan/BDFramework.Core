#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include "../../Clang/ClangMapFileParser.h"
#include <sstream>

using namespace mapfileparser;

static const char* mockMapFile = "# Path: /var/folders/8v/wyyvr8zd5rn3l9nfxz1dv4pw0000gn/T/il2cpp tests/csharp/SourceAssembly/GeneratedSources/_test.exe\n\
# Arch: i386\n\
# Object files:\n\
[0] linker synthesized\n\
[1] /usr/lib/libSystem.dylib\n\
[2] /usr/lib/libc++.dylib\n\
[3] /var/folders/8v/wyyvr8zd5rn3l9nfxz1dv4pw0000gn/T/il2cpp_cache/F14BC2A7F7430F841ABE66A030997030.o\n\
[4] /var/folders/8v/wyyvr8zd5rn3l9nfxz1dv4pw0000gn/T/il2cpp_cache/51181665DE4ABD7FD9F780396E0CD80A.o\n\
# Sections:\n\
# Address\tSize\t\tSegment\tSection\n\
0x00001F40\t0x00DDC22F\t__TEXT\t__text\n\
0x00DDE170\t0x000009C6\t__TEXT\t__symbol_stub\n\
0x00DDEB38\t0x0000098A\t__TEXT\t__stub_helper\n\
0x00DDF4C4\t0x000FFD68\t__TEXT\t__gcc_except_tab\n\
0x00EDF230\t0x0004CF94\t__TEXT\t__const\n\
0x00F2C1C4\t0x00053A85\t__TEXT\t__cstring\n\
0x00F7FC49\t0x00029CA8\t__TEXT\t__unwind_info\n\
0x00FA98F8\t0x000AF708\t__TEXT\t__eh_frame\n\
0x01059000\t0x00015ABC\t__DATA\t__nl_symbol_ptr\n\
0x0106EABC\t0x00000684\t__DATA\t__la_symbol_ptr\n\
0x0106F140\t0x0000006C\t__DATA\t__mod_init_func\n\
0x0106F1B0\t0x0023B008\t__DATA\t__data\n\
0x012AA1B8\t0x0005D044\t__DATA\t__const\n\
0x01307200\t0x00020454\t__DATA\t__common\n\
0x01327658\t0x0000AB48\t__DATA\t__bss\n\
# Symbols:\n\
# Address\tSize\t\tFile  Name\n\
0x00DDE170\t0x00000006[1] __NSGetEnviron2\n\
0x00001F40\t0x000000D0 [  3] _U3CRegisterObjectU3Ec__AnonStorey2__ctor_m12113\n\
0x00002010\t0x00000020 [  3] __ZL37il2cpp_codegen_method_info_from_indexj\n\
0x00DDE170\t0x00000006[1] __NSGetEnviron\n\
0x00DDE176\t0x00000006[1] __Unwind_Resume\n\
0x00002030\t0x00000120 [  4] _U3CRegisterObjectU3Ec__AnonStorey2_U3CU3Em__1_m12114\n\
# Dead Stripped Symbols:\n\
<<dead>> \t0x00000060\t[  2] l_OBJC_PROTOCOL_$_NSObject\n\
<<dead>> \t0x00000008\t[  2] l_OBJC_LABEL_PROTOCOL_$_NSObject\n\
";

TEST_CASE("Parsing_Clang_MapFile_ContainsExpectedSectionsAndSymbols")
{
    std::stringstream ss(mockMapFile);
    ClangMapFileParser parser;
    MapFile mapFile = parser.Parse(ss);
    REQUIRE(15 == mapFile.sections.size());

    Section section = mapFile.sections[0];
    REQUIRE(0x00001F40 == section.start);
    REQUIRE(0x00DDC22F == section.length);
    REQUIRE("__text" == section.name);
    REQUIRE("__TEXT" == section.segmentName);

    section = mapFile.sections[14];
    REQUIRE(0x01327658 == section.start);
    REQUIRE(0x0000AB48 == section.length);
    REQUIRE("__bss" == section.name);
    REQUIRE("__DATA" == section.segmentName);

    REQUIRE(3 == mapFile.symbols.size());

    Symbol symbol = mapFile.symbols[0];
    REQUIRE(0x00001F40 == symbol.start);
    REQUIRE(0x000000D0 == symbol.length);
    REQUIRE("_U3CRegisterObjectU3Ec__AnonStorey2__ctor_m12113" == symbol.name);
    REQUIRE(kSegmentTypeCode == symbol.segmentType);
    REQUIRE("/var/folders/8v/wyyvr8zd5rn3l9nfxz1dv4pw0000gn/T/il2cpp_cache/F14BC2A7F7430F841ABE66A030997030.o" == symbol.objectFile);

    symbol = mapFile.symbols[2];
    REQUIRE(0x00002030 == symbol.start);
    REQUIRE(0x00000120 == symbol.length);
    REQUIRE("_U3CRegisterObjectU3Ec__AnonStorey2_U3CU3Em__1_m12114" == symbol.name);
    REQUIRE(kSegmentTypeCode == symbol.segmentType);
    REQUIRE("/var/folders/8v/wyyvr8zd5rn3l9nfxz1dv4pw0000gn/T/il2cpp_cache/51181665DE4ABD7FD9F780396E0CD80A.o" == symbol.objectFile);
}

#endif // ENABLE_UNIT_TESTS
