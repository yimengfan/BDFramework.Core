#include "SymbolInfoWriter.h"


namespace mapfileparser
{
    static bool ShouldWriteSymbol(const Symbol& symbol)
    {
        if (symbol.segmentType != kSegmentTypeCode)
            return false;

        // assume the only symbols we care about are managed methods with symbol
        // names of the form: <prefix>_m<40 character hash value>
        size_t index = symbol.name.rfind("_m");
        if (index == std::string::npos)
            return false;

        index += 2;

        if (symbol.name.size() != index + 40)
            return false;

        bool endsWithHash = symbol.name.find_first_not_of("0123456789ABCDEF", index) == std::string::npos;

        return endsWithHash;
    }

    void SymbolInfoWriter::Write(std::ostream& out, const MapFile& mapFile)
    {
        int32_t numberOfSymbols = 0;
        for (std::vector<Symbol>::const_iterator iter = mapFile.symbols.begin(); iter != mapFile.symbols.end(); ++iter)
            if (ShouldWriteSymbol(*iter))
                ++numberOfSymbols;

        out.write(reinterpret_cast<const char*>(&numberOfSymbols), sizeof(numberOfSymbols));
        for (std::vector<Symbol>::const_iterator iter = mapFile.symbols.begin(); iter != mapFile.symbols.end(); ++iter)
        {
            if (ShouldWriteSymbol(*iter))
            {
                out.write(reinterpret_cast<const char*>(&iter->start), sizeof(int64_t));
                out.write(reinterpret_cast<const char*>(&iter->length), sizeof(int32_t));
            }
        }
    }
}
