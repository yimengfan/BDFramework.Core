#pragma once

#include "../Symbol.h"
#include <string>

namespace mapfileparser
{
    class GCCSymbolParser
    {
    public:
        static Symbol Parse(const std::string& firstLine, const std::string& secondLine);
    };
}
