#pragma once

#include "../Section.h"
#include <string>

namespace mapfileparser
{
    class MSVCSectionParser
    {
    public:
        static Section Parse(const std::string& line);
    };
}
