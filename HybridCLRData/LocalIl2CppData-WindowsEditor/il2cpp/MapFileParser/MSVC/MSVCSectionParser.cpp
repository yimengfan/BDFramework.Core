#include "MSVCSectionParser.h"
#include "../ParseBuffer.h"
#include <stdexcept>
#include <sstream>
#include <cstring>
#include <cstdio>

using std::stringstream;

namespace mapfileparser
{
//  0001:00000000 0096f9beH .text                   CODE
    Section MSVCSectionParser::Parse(const std::string& line)
    {
        int32_t segmentName;
        int32_t start;
        int32_t length;

        size_t lineLength = line.length();
        ParseBuffer name(lineLength);
        ParseBuffer segmentType(lineLength);

        int fieldsParsed = sscanf(line.c_str(), "%X:%X %XH %s %s", &segmentName, &start, &length, name.buffer, segmentType.buffer);
        if (fieldsParsed != 5)
            throw std::runtime_error(std::string("Unable to parse section: ") + line);

        stringstream segmentNameStream;
        segmentNameStream << segmentName;

        Section section = { start, length, name.buffer, segmentNameStream.str(), strcmp(segmentType.buffer, "DATA") == 0 ? kSegmentTypeData : kSegmentTypeCode };
        return section;
    }
}
