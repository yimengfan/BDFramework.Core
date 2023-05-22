#include "ClangSectionParser.h"
#include "../ParseBuffer.h"
#include <stdexcept>
#include <cstring>
#include <cstdio>

namespace mapfileparser
{
// 0x00001F40   0x00DDC22F  __TEXT  __text
    Section ClangSectionParser::Parse(const std::string& line)
    {
        int64_t start;
        int32_t length;

        size_t lineLength = line.length();
        ParseBuffer name(lineLength);
        ParseBuffer segment(lineLength);

        int fieldsParsed = sscanf(line.c_str(), "%llX %X %s %s", &start, &length, segment.buffer, name.buffer);
        if (fieldsParsed != 4)
            throw std::runtime_error(std::string("Unable to parse section: ") + line);

        Section section = { start, length, name.buffer, segment.buffer, strcmp(segment.buffer, "__DATA") == 0 ? kSegmentTypeData : kSegmentTypeCode };
        return section;
    }
}
