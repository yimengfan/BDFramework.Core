#include "GCCSymbolParser.h"
#include "../ParseBuffer.h"
#include <stdexcept>
#include <cstdio>
#include <algorithm>

// .text.Array_InternalArray__ICollection_Contains_TisDTMXPathLinkedNode2_t868737712_m2716310233_gshared
//                0x000000000039a1f8      0x2a8 /var/folders/tj/9rtndw3j6tdb0z3fqc21k3ph0000gp/T/il2cpp/AllTests_cpprunner_cache/objectfiles/8C79E9B8EDDA852A73F7465C089D46B3.o
namespace mapfileparser
{
    Symbol GCCSymbolParser::Parse(const std::string& firstLine, const std::string& secondLine)
    {
        int64_t address;
        int32_t length;
        size_t firstLineLength = firstLine.length();
        size_t secondLineLength = secondLine.length();
        size_t longestLength = std::max(firstLineLength, secondLineLength);
        ParseBuffer name(longestLength);
        ParseBuffer objectFile(longestLength);

        int fieldsParsed = sscanf(firstLine.c_str(), "%s %llX %X %s", name.buffer, &address, &length, objectFile.buffer);
        if (fieldsParsed != 4)
        {
            fieldsParsed = sscanf(firstLine.c_str(), "%s", name.buffer);
            if (fieldsParsed != 1)
                throw std::runtime_error(std::string("Unable to parse symbol: ") + firstLine);

            fieldsParsed = sscanf(secondLine.c_str(), "%llX %X %s", &address, &length, objectFile.buffer);
            if (fieldsParsed != 3)
                throw std::runtime_error(std::string("Unable to parse symbol: ") + secondLine);
        }

        std::string symbolName = name.buffer;

        Symbol symbol = { static_cast<int32_t>(address), length, symbolName.substr(6), objectFile.buffer, kSegmentTypeCode };
        return symbol;
    }
}
