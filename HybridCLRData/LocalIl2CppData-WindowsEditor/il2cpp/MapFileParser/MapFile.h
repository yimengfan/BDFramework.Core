#pragma once

#include "Section.h"
#include "Symbol.h"
#include <vector>

namespace mapfileparser
{
    struct MapFile
    {
        std::vector<Section> sections;
        std::vector<Symbol> symbols;
    };
}
