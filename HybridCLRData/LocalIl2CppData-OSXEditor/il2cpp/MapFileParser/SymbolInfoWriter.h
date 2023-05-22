#pragma once

#include "MapFile.h"
#include <ostream>

namespace mapfileparser
{
    class SymbolInfoWriter
    {
    public:
        static void Write(std::ostream& out, const MapFile& mapFile);
    };
}
