#if ENABLE_UNIT_TESTS

#include "../../external/Catch/catch.hpp"

#include <sstream>
#include "../SymbolInfoWriter.h"

using namespace mapfileparser;

#pragma pack(push, p1, 4)
struct SymbolInfo
{
    int64_t address;
    int32_t length;
};
#pragma pack(pop, p1)

class SymbolInfoWriterFixture
{
public:
    SymbolInfoWriterFixture()
    {
        Symbol codeSymbol1 = { 100, 10, "Symbol1Name_m03D674052100D1E9D6214ADD31FE7E5F7E6788DA", "Symbol1ObjectFile", kSegmentTypeCode };
        Symbol codeSymbol2 = { 200, 20, "Symbol2Name_m03D674052100D1E9D6214ADD31FE7E5F7E6788DB", "Symbol2ObjectFile", kSegmentTypeCode };
        Symbol dataSymbol = { 300, 30, "Symbol3Name_m03D674052100D1E9D6214ADD31FE7E5F7E6788DC", "Symbol3ObjectFile", kSegmentTypeData };

        mapFile.symbols.push_back(codeSymbol1);
        mapFile.symbols.push_back(codeSymbol2);
        mapFile.symbols.push_back(dataSymbol);

        SymbolInfoWriter::Write(output, mapFile);
    }

    int32_t ReadActualNumberOfSymbols()
    {
        int32_t numberOfSymbols = 0;
        output.read(reinterpret_cast<char*>(&numberOfSymbols), sizeof(numberOfSymbols));

        return numberOfSymbols;
    }

    SymbolInfo ReadOneSymbolInfo()
    {
        SymbolInfo symbolInfo;
        output.read(reinterpret_cast<char*>(&symbolInfo), sizeof(symbolInfo));

        return symbolInfo;
    }

    MapFile mapFile;
    std::stringstream output;
};

TEST_CASE_METHOD(SymbolInfoWriterFixture, "VerifyOnlySymbolsWithACodeSegmentAreWritten")
{
    REQUIRE(2 == ReadActualNumberOfSymbols());
}

TEST_CASE_METHOD(SymbolInfoWriterFixture, "VerifySymbolInformationForFirstSymbol")
{
    ReadActualNumberOfSymbols();

    SymbolInfo actualInfo = ReadOneSymbolInfo();

    REQUIRE(100 == actualInfo.address);
    REQUIRE(10 == actualInfo.length);
}

TEST_CASE_METHOD(SymbolInfoWriterFixture, "VerifySymbolInformationForSecondSymbol")
{
    ReadActualNumberOfSymbols();
    ReadOneSymbolInfo();

    SymbolInfo actualInfo = ReadOneSymbolInfo();

    REQUIRE(200 == actualInfo.address);
    REQUIRE(20 == actualInfo.length);
}


#endif // ENABLE_UNIT_TESTS
