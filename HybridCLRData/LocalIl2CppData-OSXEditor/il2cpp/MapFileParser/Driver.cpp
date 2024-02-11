#include <fstream>
#include <stdexcept>
#include <cstring>
#include "Driver.h"
#include "MSVC/MSVCMapFileParser.h"
#include "Clang/ClangMapFileParser.h"
#include "SNC/SNCMapFileParser.h"
#include "GCC/GCCMapFileParser.h"
#include "SymbolInfoWriter.h"
#include "Statistics.h"

#ifdef _WINDOWS
#include <Windows.h>
#endif

namespace mapfileparser
{
    enum MapFileFormat
    {
        kMapFileFormatUnknown,
        kMapFileFormatMSVC,
        kMapFileFormatClang,
        kMapFileFormatSNC,
        kMapFileFormatGCC,
    };

    static MapFileParser* GetParser(MapFileFormat mapFileFormat)
    {
        switch (mapFileFormat)
        {
            case kMapFileFormatMSVC:
                return new MSVCMapFileParser();
            case kMapFileFormatClang:
                return new ClangMapFileParser();
            case kMapFileFormatSNC:
                return new SNCMapFileParser();
            case kMapFileFormatGCC:
                return new GCCMapFileParser();
            default:
                throw new std::runtime_error(std::string("Invalid map file format specified"));
        }
    }

    static const char* GetUsage()
    {
        return "Usage: MapFileParser -format=<MSVC|Clang|SNC|GCC> mapFile <-stats|outputFile>";
    }

    static std::string NativeStringToUtf8(const NativeChar* str)
    {
#if !_WINDOWS
        return str;
#else
        size_t strLength = wcslen(str);
        std::string result;
        result.resize(4 * strLength);

        int resultLength = WideCharToMultiByte(CP_UTF8, 0, str, static_cast<int>(strLength), &result[0], static_cast<int>(4 * strLength), NULL, NULL);
        result.resize(resultLength);
        return result;
#endif
    }

    static void ParseInputAndWriteOutput(MapFileFormat mapFileFormat, std::ifstream& inputFile, std::ofstream& outputFile)
    {
        MapFileParser* parser = GetParser(mapFileFormat);
        MapFile mapFile = parser->Parse(inputFile);

        SymbolInfoWriter::Write(outputFile, mapFile);
    }

    static void ParseInputAndGenerateStatistics(MapFileFormat mapFileFormat, std::ifstream& inputFile, std::ostream& output)
    {
        MapFileParser* parser = GetParser(mapFileFormat);
        MapFile mapFile = parser->Parse(inputFile);

        Statistics::Generate(mapFile, output);
    }

    int Driver::Run(int argc, const NativeChar* const argv[], std::ostream& out)
    {
        if (argc != 4)
        {
            out << GetUsage() << std::endl;
            return 1;
        }

        MapFileFormat mapFileFormat = kMapFileFormatUnknown;
        if (!NativeStrCmp(argv[1], NativeText("-format=MSVC")))
            mapFileFormat = kMapFileFormatMSVC;
        else if (!NativeStrCmp(argv[1], NativeText("-format=Clang")))
            mapFileFormat = kMapFileFormatClang;
        else if (!NativeStrCmp(argv[1], NativeText("-format=SNC")))
            mapFileFormat = kMapFileFormatSNC;
        else if (!NativeStrCmp(argv[1], NativeText("-format=GCC")))
            mapFileFormat = kMapFileFormatGCC;

        if (mapFileFormat == kMapFileFormatUnknown)
        {
            out << "Unknown map file format.\n";
            out << GetUsage() << std::endl;
            return 1;
        }

        // If we have a runtime error, like a file failing to open or an error parsing,
        // we want to return 0, so that the build will succeed. Since map-file based
        // stack traces are not required to execute, we won't fail the build if
        // we can't generate the output file.

        const NativeChar* mapFileName = argv[2];

        std::ifstream inputFile;
        inputFile.open(mapFileName, std::ifstream::in);
        if (!inputFile.is_open())
        {
            out << "Map file " << NativeStringToUtf8(argv[2]) << " cannot be opened.\n";
            return 1;
        }

        if (NativeStrCmp(argv[3], NativeText("-stats")) == 0)
        {
            if (mapFileFormat != kMapFileFormatClang)
            {
                out << "Statistics are only supported for Clang map files now.\n";
                out << GetUsage() << std::endl;
                return 1;
            }

            try
            {
                ParseInputAndGenerateStatistics(mapFileFormat, inputFile, out);
            }
            catch (const std::exception& e)
            {
                out << e.what() << std::endl;
            }
        }
        else
        {
            const NativeChar* outputFileName = argv[3];

            std::ofstream outputFile;
            outputFile.open(outputFileName, std::ios::binary | std::ios::out);
            if (!outputFile.is_open())
            {
                out << "Output file " << NativeStringToUtf8(argv[3]) << " cannot be opened.\n";
                return 0;
            }

            try
            {
                ParseInputAndWriteOutput(mapFileFormat, inputFile, outputFile);
            }
            catch (const std::exception& e)
            {
                out << e.what() << std::endl;
            }
        }

        return 0;
    }
}
