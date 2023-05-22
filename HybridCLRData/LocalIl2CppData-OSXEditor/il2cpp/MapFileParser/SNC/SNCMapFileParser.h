#pragma once

#include "../MapFileParser.h"
#include <string>

namespace mapfileparser
{
    class SNCMapFileParser : public MapFileParser
    {
    public:
        virtual MapFile Parse(std::istream& is);
    };
}
