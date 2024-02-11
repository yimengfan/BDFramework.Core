#include "TenLargestSymbols.h"
#include <algorithm>
#include <sstream>

namespace mapfileparser
{
    static bool SortBySizeDescending(Symbol left, Symbol right)
    {
        return right.length < left.length;
    }

    std::string TenLargestSymbols(std::vector<Symbol>& symbols)
    {
        std::sort(symbols.begin(), symbols.end(), SortBySizeDescending);

        std::stringstream output;
        output << "Ten largest symbols (sorted by size)\n";
        output << "------------------------------------\n";
        for (int i = 0; i < 10; ++i)
        {
            output << symbols[i].name << ": " << symbols[i].length << " bytes\n";
        }
        return output.str();
    }
}
