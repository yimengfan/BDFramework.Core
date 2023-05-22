#pragma once

#include <fstream>

#include "os/Lumin/File.h"

static std::ofstream* out_stream = nullptr;
static std::ofstream* err_stream = nullptr;
static std::ofstream* log_stream = nullptr;

std::ostream& Catch::cout()
{
    if (out_stream == nullptr)
    {
        out_stream = new std::ofstream(il2cpp::os::lumin::PathForOutputLog().c_str());
    }
    return *out_stream;
}

std::ostream& Catch::cerr()
{
    if (err_stream == nullptr)
    {
        err_stream = new std::ofstream(il2cpp::os::lumin::PathForErrorLog().c_str());
    }
    return *err_stream;
}

std::ostream& Catch::clog()
{
    if (log_stream == nullptr)
    {
        log_stream = new std::ofstream(il2cpp::os::lumin::PathForErrorLog().c_str());
    }
    return *log_stream;
}

static inline void FlushStreams()
{
    if (out_stream != nullptr)
        out_stream->close();
    if (err_stream != nullptr)
        err_stream->close();
    if (log_stream != nullptr)
        log_stream->close();
}