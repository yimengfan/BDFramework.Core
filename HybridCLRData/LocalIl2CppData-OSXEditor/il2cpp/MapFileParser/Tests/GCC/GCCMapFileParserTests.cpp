#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include <sstream>
#include "../../GCC/GCCMapFileParser.h"

using namespace mapfileparser;

static const char* mockMapFile = "\
.plt            0x0000000000395fa0      0xee0\n\
*(.plt)\n\
.plt           0x0000000000395fa0      0xee0/Users/erik/src/unity/a00 - full/PlatformDependent/A00/External/a00_toolchain/builds/a00/platforms/arm64/a00 - latest/usr/lib/crtbegin_dynamic.o\n\
*(.iplt)\n\
\n\
.text           0x0000000000396e80   0xd1fbb8\n\
 * (.text.unlikely.text.*_unlikely.text.unlikely.*)\n\
 *(.text.exit.text.exit.*)\n\
 *(.text.startup.text.startup.*)\n\
 *(.text.hot.text.hot.*)\n\
 *(.text.stub.text.*.gnu.linkonce.t.*)\n\
 .text          0x0000000000396e80       0x78 /Users/erik/src/unity/a00 - full/PlatformDependent/A00/External/a00_toolchain/builds/a00/platforms/arm64/a00 - latest/usr/lib/crtbegin_dynamic.o\n\
0x0000000000396e80                _start\n\
0x0000000000396e88                __atexit_handler_wrapper\n\
0x0000000000396ea0                do_arm64_start\n\
0x0000000000396ee0                atexit\n\
 .text._ZN8IntPtr_t13set_m_value_0EPv\n\
0x0000000000396ef8       0x20 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o\n\
0x0000000000396ef8                IntPtr_t::set_m_value_0(void*)\n\
 .text.main     0x0000000000ea1750       0x28 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/AEC01B5ADC5CEF9AE119963D72C496D5.o\n\
0x0000000000ea1750                main\n\
 .text._ZN29RuntimeTypeHandle_t186487588711set_value_0E8IntPtr_t\n\
0x0000000000396f18       0x20 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o\n\
0x0000000000396f18                RuntimeTypeHandle_t1864875887::set_value_0(IntPtr_t)\n\
 .text._ZN30RuntimeFieldHandle_t318421414311set_value_0E8IntPtr_t\n\
0x0000000000396f38       0x20 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o\n\
0x0000000000396f38                RuntimeFieldHandle_t3184214143::set_value_0(IntPtr_t)\n\
 .text._Z30il2cpp_codegen_raise_exceptionP21Exception_t1967233988\n\
0x0000000000396f58       0x1c /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o\n\
0x0000000000396f58                il2cpp_codegen_raise_exception(Exception_t1967233988*)\n\
 .text._ZL24il2cpp_codegen_no_returnv\n\
0x0000000000396f74        0x4 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o\n\
 .text.__tcf_0  0x0000000000ec20f8       0x44 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/BF3F7B22EECDD073952DF537662C653D.o\n\
 .text.__tcf_1  0x0000000000ec20f8       0x44 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/BF3F7B22EECDD073952DF537662C653D.o\n\
 .text._Z48il2cpp_codegen_get_array_type_mismatch_exceptionv\n\
0x0000000000396f78       0x18 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o\n\
0x0000000000396f78                il2cpp_codegen_get_array_type_mismatch_exception()\n\
 .text._Z6IsInstP12Il2CppObjectP11Il2CppClass\n\
0x0000000000396f90       0x28 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o\n\
0x0000000000396f90                IsInst(Il2CppObject* == Il2CppClass*)\n\
 .text.Z9CastclassP12Il2CppObjectP11Il2CppClass\n\
0x0000000000396fb8      0x170 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B4.o\n\
0x0000000000396fb8                Castclass(Il2CppObject* == Il2CppClass*)\n\
";

TEST_CASE("Parsing_GCC_MapFile_ContainsExpectedSectionsAndSymbols")
{
    std::stringstream ss(mockMapFile);
    GCCMapFileParser parser;
    MapFile mapFile = parser.Parse(ss);
    REQUIRE(0 == mapFile.sections.size());

    REQUIRE(9 == mapFile.symbols.size());

    Symbol symbol = mapFile.symbols[3];
    REQUIRE(0x396f38 == symbol.start);
    REQUIRE(0x20 == symbol.length);
    REQUIRE(symbol.name == "_ZN30RuntimeFieldHandle_t318421414311set_value_0E8IntPtr_t");
    REQUIRE(symbol.objectFile == "/var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o");
    REQUIRE(kSegmentTypeCode == symbol.segmentType);

    symbol = mapFile.symbols[8];
    REQUIRE(0x396fb8 == symbol.start);
    REQUIRE(0x170 == symbol.length);
    REQUIRE(symbol.name == "Z9CastclassP12Il2CppObjectP11Il2CppClass");
    REQUIRE(symbol.objectFile == "/var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B4.o");
    REQUIRE(kSegmentTypeCode == symbol.segmentType);
}

#endif // ENABLE_UNIT_TESTS
