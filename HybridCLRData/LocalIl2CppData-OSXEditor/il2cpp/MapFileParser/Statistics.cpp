#include <ostream>
#include "Statistics.h"
#include "Statistics/HighLevelBreakdown.h"
#include "Statistics/TenLargestSymbols.h"
#include "Statistics/AllSymbolSizes.h"

namespace mapfileparser
{
    void Statistics::Generate(MapFile& mapFile, std::ostream& out)
    {
        out << "Code size statistics from the map file\n";
        out << "======================================\n";
        out << std::endl;
        out << HighLevelBreakdown(mapFile.symbols) << std::endl;
        out << TenLargestSymbols(mapFile.symbols) << std::endl;
        out << AllSymbolSizes(mapFile.symbols) << std::endl;
    }
}
