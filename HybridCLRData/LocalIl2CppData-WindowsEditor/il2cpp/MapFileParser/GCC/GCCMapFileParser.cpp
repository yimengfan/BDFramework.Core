#include <sstream>
#include "GCCMapFileParser.h"
#include "GCCSymbolParser.h"

namespace mapfileparser
{
    static bool StartsWith(const std::string& haystack, const char* needle)
    {
        return haystack.find(needle) == 0;
    }

    static std::string ReadUntilTextSection(std::istream& is)
    {
        std::string line;
        std::getline(is, line);
        while (!is.eof() && !StartsWith(line, ".text"))
            std::getline(is, line);

        return line;
    }

    static void ParseSymbols(std::istream& is, std::vector<Symbol>& symbols)
    {
        std::string line;
        std::getline(is, line);

        while (!is.eof())
        {
            std::getline(is, line);
            // The code symbols start with this prefix.
            if (StartsWith(line, " .text.") && !StartsWith(line, " .text.__tcf_"))
            {
                std::string secondLineForSymbol;
                std::getline(is, secondLineForSymbol);
                symbols.push_back(GCCSymbolParser::Parse(line, secondLineForSymbol));
            }
        }
    }

    MapFile GCCMapFileParser::Parse(std::istream& is)
    {
        MapFile mapFile;

        ReadUntilTextSection(is);

        ParseSymbols(is, mapFile.symbols);

        return mapFile;
    }
}
