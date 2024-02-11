#pragma once

#include "../MapFileParser.h"
#include <string>

namespace mapfileparser
{
    class MSVCMapFileParser : public MapFileParser
    {
    public:
        virtual MapFile Parse(std::istream& is);
    };
}
