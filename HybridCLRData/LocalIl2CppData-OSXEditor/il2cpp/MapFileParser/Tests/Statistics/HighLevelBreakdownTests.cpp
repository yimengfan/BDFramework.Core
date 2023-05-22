#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include "../../Statistics/HighLevelBreakdown.h"
#include <vector>
#include <sstream>

using namespace mapfileparser;

// _ExtensionAttribute__ctor_m65804854_0 8 /Users/Josh/Library/Developer/Xcode/DerivedData/Unity-iPhone-dvhwgckagmunwcbujkhbqcbwxjah/Build/Intermediates/ArchiveIntermediates/Unity-iPhone/IntermediateBuildFilesPath/Unity-iPhone.build/Release-iphoneos/Unity-iPhone.build/Objects-normal/arm64/Bulk_Assembly-CSharp-firstpass_0.o
class HighLevelBreakdownFixture
{
public:
    HighLevelBreakdownFixture()
    {
        Symbol generatedCodeSymbol1 = { 0, 30, "GeneratedCodeSymbol1Name", "Objects-normal/arm64/Bulk_Assembly-CSharp-firstpass_0.o", kSegmentTypeCode };
        Symbol generatedCodeSymbol2 = { 0, 20, "GeneratedCodeSymbol2Name", "Objects-normal/arm64/Bulk_Assembly-CSharp_3.o", kSegmentTypeCode };
        Symbol engineCodeSymbol = { 0, 40, "EngineCodeSymbolName", "Objects-normal/arm64/Filesystem.o", kSegmentTypeCode };
        Symbol otherCodeSymbol = { 0, 10, "OtherCodeSymbolName", "Twitter/libP31Twitter.a(P31Request.o)", kSegmentTypeCode };
        Symbol dataSymbol = { 0, 30, "DataSymbolName", "DataSymbolObjectFile", kSegmentTypeData };

        symbols.push_back(generatedCodeSymbol1);
        symbols.push_back(generatedCodeSymbol2);
        symbols.push_back(engineCodeSymbol);
        symbols.push_back(otherCodeSymbol);
        symbols.push_back(dataSymbol);
    }

    std::vector<Symbol> symbols;
};

TEST_CASE_METHOD(HighLevelBreakdownFixture, "ContainsLineForGeneratedCode")
{
    std::string output = HighLevelBreakdown(symbols);
    const char* expectedLine = "Generated code: 50 bytes (50%)";
    REQUIRE_THAT(output, Catch::Matchers::Contains(expectedLine));
}

TEST_CASE_METHOD(HighLevelBreakdownFixture, "ContainsLineForEningeCode")
{
    std::string output = HighLevelBreakdown(symbols);
    const char* expectedLine = "Engine code: 40 bytes (40%)";
    REQUIRE_THAT(output, Catch::Matchers::Contains(expectedLine));
}

TEST_CASE_METHOD(HighLevelBreakdownFixture, "ContainsLineForOtherCode")
{
    std::string output = HighLevelBreakdown(symbols);
    const char* expectedLine = "Other code: 10 bytes (10%)";
    REQUIRE_THAT(output, Catch::Matchers::Contains(expectedLine));
}

TEST_CASE_METHOD(HighLevelBreakdownFixture, "ContainsLineForTotalCode")
{
    std::string output = HighLevelBreakdown(symbols);
    const char* expectedLine = "Total code: 100 bytes (100%)";
    REQUIRE_THAT(output, Catch::Matchers::Contains(expectedLine));
}

#endif // ENABLE_UNIT_TESTS
