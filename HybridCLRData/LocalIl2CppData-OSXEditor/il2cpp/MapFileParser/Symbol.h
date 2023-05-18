#pragma once

#include <stdint.h>
#include <string>
#include <vector>
#include "Segment.h"

namespace mapfileparser
{
    struct Symbol
    {
        int64_t start;
        int64_t length;
        std::string name;
        std::string objectFile;
        SegmentType segmentType;
    };
}
