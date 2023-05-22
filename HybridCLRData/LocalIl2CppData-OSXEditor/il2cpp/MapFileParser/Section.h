#pragma once
#include <stdint.h>
#include <string>
#include <vector>
#include "Segment.h"
#include "Symbol.h"

namespace mapfileparser
{
    struct Section
    {
        int64_t start;
        int32_t length;
        std::string name;
        std::string segmentName;
        SegmentType segmentType;
    };
}
