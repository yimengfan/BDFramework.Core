#if ENABLE_UNIT_TESTS

#include "../../../external/Catch/catch.hpp"

#include "../../Statistics/TenLargestSymbols.h"
#include <vector>
#include <sstream>

using namespace mapfileparser;


class TenLargestSymbolsFixture
{
public:
    TenLargestSymbolsFixture()
    {
        for (int i = 0; i < 11; ++i)
        {
            std::stringstream symbolName;
            symbolName << "Symbol" << i;

            std::stringstream objectFile;
            objectFile << "ObjectFile" << i << ".o";

            Symbol symbol = { 0, i + 1, symbolName.str(), objectFile.str(), kSegmentTypeCode };
            symbols.push_back(symbol);
        }
    }

    std::vector<Symbol> symbols;
};

TEST_CASE_METHOD(TenLargestSymbolsFixture, "ContainsTheSortedListTofTheTenLargestSymbols")
{
    std::string output = TenLargestSymbols(symbols);
    const char* expectedOutput =
        "Symbol10: 11 bytes\n"
        "Symbol9: 10 bytes\n"
        "Symbol8: 9 bytes\n"
        "Symbol7: 8 bytes\n"
        "Symbol6: 7 bytes\n"
        "Symbol5: 6 bytes\n"
        "Symbol4: 5 bytes\n"
        "Symbol3: 4 bytes\n"
        "Symbol2: 3 bytes\n"
        "Symbol1: 2 bytes\n";
    REQUIRE_THAT(output, Catch::Matchers::Contains(expectedOutput));
}

#endif // ENABLE_UNIT_TESTS
