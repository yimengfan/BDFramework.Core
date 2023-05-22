#pragma once
#include <stdlib.h>

#include <cstdlib>

namespace mapfileparser
{
    struct ParseBuffer
    {
        ParseBuffer(size_t size)
        {
            buffer = (char*)malloc(size);
        }

        ~ParseBuffer()
        {
            free(buffer);
        }

        char* buffer;
    };
}
