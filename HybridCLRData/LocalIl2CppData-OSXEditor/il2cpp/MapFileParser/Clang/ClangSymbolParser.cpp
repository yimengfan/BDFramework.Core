#include "ClangSymbolParser.h"
#include "../ParseBuffer.h"
#include <stdexcept>
#include <cstdio>

// 0x00001F40   0x000000D0  [  3] _U3CRegisterObjectU3Ec__AnonStorey2__ctor_m12113
namespace mapfileparser
{
    ClangSymbol ClangSymbolParser::Parse(const std::string& line)
    {
        int64_t address;
        int32_t length;
        int32_t objectFileIndex = 0;
        size_t lineLength = line.length();
        ParseBuffer name(lineLength);

        int fieldsParsed = sscanf(line.c_str(), "%llX %X [%d] %s", &address, &length, &objectFileIndex, name.buffer);
        if (fieldsParsed != 4)
            throw std::runtime_error(std::string("Unable to parse symbol: ") + line);

        ClangSymbol symbol = { { address, length, name.buffer, "", kSegmentTypeCode }, objectFileIndex };
        return symbol;
    }
}
