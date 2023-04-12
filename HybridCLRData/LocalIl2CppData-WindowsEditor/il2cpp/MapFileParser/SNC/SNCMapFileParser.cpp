#include "SNCMapFileParser.h"
#include <sstream>
#include <algorithm>

namespace mapfileparser
{
/*
SNC map files place fields at fixed column positions, e.g.

Address  Size     Align Out     In      File    Symbol
=================================================================
UNDEFINED
                                                module_start
                                                module_stop
                                                module_exit
81000000 003bd4d8     4 .text
81000000 00000154     4         .text
81000000 00000154     4                 E:\UnityInternal\Test Projects\il2cpp\Test-Stacktrace\Temp\StagingArea\il2cppOutput\Assembly-CSharpAttributes.o
81000000 00000000     0                         $t
81000061 000000f4     0                         __sti___29_Assembly_CSharpAttributes_cpp_19687d34

PS5 Maps look different ... more like this:

             VMA              LMA     Size Align Out     In      Symbol
               0                0       10     1 .sce_padding
               0                0        8     1         QUAD ( 0xcccccccccccccccc )
               8                8        8     1         QUAD ( 0xcccccccccccccccc )
              10               10       b8    16 .init
              10               10        0     1         PROVIDE ( __start__Zinit = . )
              10               10       b8    16         E:\SCE\PROSPERO SDKs\latest/target/lib\crtendS.o:(.init)
              10               10       b8     1                 _init
              d0               d0   c944bc    16 .text
              d0               d0        0     4         E:\SCE\PROSPERO SDKs\latest/target/lib\crti.o:(.text)
              d0               d0        0     4         E:\SCE\PROSPERO SDKs\latest/target/lib\crtbeginS.o:(.text)
              d0               d0        0     4         F:/tempDeletedDaily/PS5ManagedCallstackTest/Library/il2cpp_cache/334136F945D9BDCF93C3ABEB3C2304B8.obj:(.text)
              d0               d0        0     4         F:/tempDeletedDaily/PS5ManagedCallstackTest/Library/il2cpp_cache/DE4A19A775C4B30C0D59DF382D380F0F.obj:(.text)
              d0               d0       14    16         F:/tempDeletedDaily/PS5ManagedCallstackTest/Library/il2cpp_cache/DE4A19A775C4B30C0D59DF382D380F0F.obj:(.text.__cxx_global_var_init)
              d0               d0       14     1                 __cxx_global_var_init



*/

// Column positions and widths for each field.


    static size_t kAddress = 0;
    static size_t kAddressWidth = 8;
    static size_t kSize = 9;
    static size_t kSizeWidth = 8;
// No widths for the following; if the field is not empty then read to eol.
    static size_t kOut = 24;
    static size_t kIn = 32;
    static size_t kFile = 40;
    static size_t kSymbol = 48;


    static std::string GetColumnString(const std::string& line, size_t column, size_t width, bool ps5MapFormat)
    {
        if (column >= line.length())
        {
            return "";
        }


        if (!ps5MapFormat)  // ps5 map file values all have leading spaces
        {
            if (line[column] == ' ')
            {
                return "";
            }
        }

        if (width)
        {
            return line.substr(column, width);
        }

        return line.substr(column);
    }

    static SegmentType SegmentTypeFromName(const std::string& name)
    {
        // Only interested in the .text section so treat everything else as data.
        if (name == ".text")
        {
            return kSegmentTypeCode;
        }

        return kSegmentTypeData;
    }

    static bool SectionFilter(const Section& section)
    {
        // Only interested in code sections.
        return section.segmentType == kSegmentTypeCode;
    }

    static bool StringToLong(const std::string& text, int64_t& value)
    {
        char* endPtr = NULL;
        value = strtoul(text.c_str(), &endPtr, 16);
        return endPtr != text.c_str();
    }

    bool symbolSort(Symbol i, Symbol j)
    {
        return (i.start < j.start);
    }

    MapFile SNCMapFileParser::Parse(std::istream& is)
    {
        bool foundFirstSection = false;
        bool ProcessAsXMap = false; // if the first section start address is 0, then we assume it's a X map

        MapFile mapFile;
        std::string line;
        std::string currFile;
        Section currSection;

        bool ps5MapFormat = false;

        // Skip column headings.
        std::getline(is, line);

#if _WINDOWS
        // ps5 map headers have "VMA" as a column header
        if (strstr(line.c_str(), "VMA") != 0)
        {
            kAddress = 0;
            kAddressWidth = 16;
            kSize = 34;
            kSizeWidth = 8;
            // No widths for the following; if the field is not empty then read to eol.
            kOut = 49;
            kIn = 57;
            kFile = 57;
            kSymbol = 65;

            ps5MapFormat = true;
        }
#endif

        if (!ps5MapFormat)
            std::getline(is, line);

        bool completedParsing = false;
        while (!is.eof() && !completedParsing)
        {
            std::getline(is, line);

            std::string address = GetColumnString(line, kAddress, kAddressWidth, ps5MapFormat);
            int64_t addrVal = 0;
            if (StringToLong(address.c_str(), addrVal))
            {
                std::string size = GetColumnString(line, kSize, kSizeWidth, ps5MapFormat);
                unsigned long sizeVal = strtol(size.c_str(), NULL, 16);

                std::string outSection = GetColumnString(line, kOut, 0, ps5MapFormat);
                if (outSection.length() && outSection.c_str()[0] != ' ')
                {
                    if (ps5MapFormat && outSection == ".plt")   // as soon as we've reached the .plt section we can quit ... saves a lot of parsing
                    {
                        completedParsing = true;
                        continue;
                    }
                    // if the first section start address is 0, then we assume it's a X map
                    if (foundFirstSection == false)
                    {
                        if (addrVal == 0)
                        {
                            ProcessAsXMap = true;
                        }
                        foundFirstSection = true;
                    }


                    currSection.segmentType = SegmentTypeFromName(outSection);
                    currSection.start = addrVal;
                    currSection.length = sizeVal;
                    currSection.name = outSection;
                    currSection.segmentName = outSection;
                    if (SectionFilter(currSection))
                    {
                        mapFile.sections.push_back(currSection);
                    }
                    continue;
                }

                if (SectionFilter(currSection) && sizeVal != 0)
                {
                    if (!ps5MapFormat)
                    {
                        // ps5 maps don't have seperate in and file sections
                        if (GetColumnString(line, kIn , 0, ps5MapFormat).length())
                        {
                            continue;
                        }
                    }

                    if (ProcessAsXMap == false)
                    {
                        // Subtract the section base address, il2cpp stack trace expects 0 based offsets for symbols.
                        addrVal -= currSection.start;

                        // Mask off the 2 lowest bits of the address, LSB indicates thumb code on Vita, Il2cpp stack
                        // trace assumes thumb or thumb2 where thumb2 is the second LSB.
                        addrVal = addrVal & ~3;
                    }

                    std::string file = GetColumnString(line, kFile, 0, ps5MapFormat);
                    if (file.length() && file.c_str()[0] != ' ')
                    {
                        currFile = file;
                        continue;
                    }

                    if (ps5MapFormat)
                        addrVal -= 0x10;    // in ps5 maps the .init section starts at an offset of 0x10 and we don't use any of the "xmap" code


                    std::string name = GetColumnString(line, kSymbol, 0, ps5MapFormat);
                    Symbol symbol;
                    symbol.start = addrVal;
                    symbol.length = sizeVal;
                    symbol.name = name;
                    symbol.objectFile = currFile;
                    symbol.segmentType = currSection.segmentType;
                    mapFile.symbols.push_back(symbol);
                }
            }
        }
        std::sort(mapFile.symbols.begin(), mapFile.symbols.end(), symbolSort);
        return mapFile;
    }
}
