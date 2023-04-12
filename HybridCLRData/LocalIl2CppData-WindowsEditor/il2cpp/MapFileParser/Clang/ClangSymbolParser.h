#pragma once

#include "../Symbol.h"
#include <string>

namespace mapfileparser
{
    struct ClangSymbol
    {
        Symbol symbol;
        int32_t objectFileIndex;
    };

    class ClangSymbolParser
    {
    public:
        static ClangSymbol Parse(const std::string& line);
    };
}
