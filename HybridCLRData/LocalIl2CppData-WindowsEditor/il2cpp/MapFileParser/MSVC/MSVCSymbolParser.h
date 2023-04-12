#pragma once

#include "../Symbol.h"
#include <string>

namespace mapfileparser
{
    class MSVCSymbolParser
    {
    public:
        static Symbol Parse(const std::string& line1, int64_t imageBase = 0);
    };
}
