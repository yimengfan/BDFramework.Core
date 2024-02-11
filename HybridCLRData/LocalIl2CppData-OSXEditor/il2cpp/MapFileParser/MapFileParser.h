#pragma once

#include "MapFile.h"
#include <string>

namespace mapfileparser
{
    class MapFileParser
    {
    public:
        virtual MapFile Parse(std::istream& is) = 0;
    };
}
