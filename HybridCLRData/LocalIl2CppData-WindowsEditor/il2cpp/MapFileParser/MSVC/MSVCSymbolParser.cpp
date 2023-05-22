#include "MSVCSymbolParser.h"
#include "../ParseBuffer.h"
#include <stdexcept>
#include <limits>
#include <cassert>
#include <cstring>
#include <cstdio>

namespace mapfileparser
{
//  0001:00000000       _StreamingContext__ctor_m12118 00401000 f   98F5E16DA77CA137853023FCE84040FD.obj
//  0000:00000000       __except_list              00000000     <absolute>
    Symbol MSVCSymbolParser::Parse(const std::string& line, int64_t imageBase)
    {
        int32_t section;
        int32_t address;
        int64_t rva;
        char unused[5];

        size_t lineLength = line.length();
        ParseBuffer name(lineLength);
        ParseBuffer objectFile(lineLength);

        int fieldsParsed = sscanf(line.c_str(), "%X:%X %s %llX%c%c%c%c%c%s", &section, &address, name.buffer, &rva, &unused[0], &unused[1], &unused[2], &unused[3], &unused[4], objectFile.buffer);
        if (fieldsParsed == 9)
            strcpy(objectFile.buffer, unused);
        else if (fieldsParsed != 10)
            throw std::runtime_error(std::string("Unable to parse symbol: ") + line);

        int64_t absoluteAddress = rva - imageBase;
        assert(absoluteAddress < std::numeric_limits<int32_t>::max());

        Symbol symbol = { static_cast<int32_t>(absoluteAddress), 0, name.buffer, objectFile.buffer, kSegmentTypeCode };
        return symbol;
    }
}
