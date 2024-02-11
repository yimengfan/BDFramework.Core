#pragma once

#include "../Section.h"
#include <string>

namespace mapfileparser
{
    class ClangSectionParser
    {
    public:
        static Section Parse(const std::string& line);
    };
}
