#pragma once

#include "../MapFileParser.h"
#include <string>

namespace mapfileparser
{
    class ClangMapFileParser : public MapFileParser
    {
    public:
        virtual MapFile Parse(std::istream& is);
    };
}
