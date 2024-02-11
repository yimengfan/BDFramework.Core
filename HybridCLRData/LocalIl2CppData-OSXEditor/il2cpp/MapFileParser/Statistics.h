#pragma once

#include <string>
#include "MapFile.h"

namespace mapfileparser
{
    class Statistics
    {
    public:
        static void Generate(MapFile& mapFile, std::ostream& out);
    };
}
