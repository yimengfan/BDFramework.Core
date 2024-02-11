#include "AllSymbolSizes.h"
#include <algorithm>
#include <sstream>

namespace mapfileparser
{
    static bool SortByName(Symbol left, Symbol right)
    {
        return left.name.compare(right.name) < 0;
    }

    std::string AllSymbolSizes(std::vector<Symbol>& symbols)
    {
        std::sort(symbols.begin(), symbols.end(), SortByName);

        std::stringstream output;
        output << "All symbols and their sizes (sorted by name)\n";
        output << "--------------------------------------------\n";
        for (std::vector<Symbol>::const_iterator symbol = symbols.begin(); symbol != symbols.end(); ++symbol)
        {
            output << symbol->name << ": " << symbol->length << " bytes\n";
        }

        return output.str();
    }
}
