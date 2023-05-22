#include "MSVCMapFileParser.h"
#include "MSVCSectionParser.h"
#include "MSVCSymbolParser.h"
#include <sstream>
#include <algorithm>
#include <stdexcept>
#include <string>
#include <cstring>
#include <cstdio>


namespace mapfileparser
{
    static size_t GetSectionIndexForSymbol(int64_t absoluteSymbolAddress, const std::vector<Section>& sections)
    {
        for (size_t index = 0; index < sections.size(); ++index)
        {
            if (absoluteSymbolAddress >= sections[index].start && absoluteSymbolAddress <= (sections[index].start + sections[index].length))
                return index;
        }

        throw std::runtime_error(std::string("Unable to find section for symbol"));
    }

    static void CalculateLengthsAndSegmentTypesForAllSymbols(MapFile& mapFile)
    {
        bool processedImageBase = false;
        // iterate all symbols and calculate length based on difference between start and start of next symbol
        for (size_t index = 0; index < mapFile.symbols.size(); ++index)
        {
            if (!processedImageBase)
            {
                if (mapFile.symbols[index].name == "___ImageBase" || mapFile.symbols[index].name == "__ImageBase")
                    processedImageBase = true;

                continue;
            }

            size_t currentSectionIndex = GetSectionIndexForSymbol(mapFile.symbols[index].start, mapFile.sections);

            mapFile.symbols[index].segmentType = mapFile.sections[currentSectionIndex].segmentType;

            if (index == mapFile.symbols.size() - 1 || mapFile.sections[currentSectionIndex].segmentName != mapFile.sections[GetSectionIndexForSymbol(mapFile.symbols[index + 1].start, mapFile.sections)].segmentName)
                mapFile.symbols[index].length = mapFile.sections[currentSectionIndex].start + mapFile.sections[currentSectionIndex].length - mapFile.symbols[index].start;
            else
                mapFile.symbols[index].length = mapFile.symbols[index + 1].start - mapFile.symbols[index].start;
        }
    }

    static std::string ReadUntilDelimiter(std::istream& is, const char* delimiter)
    {
        const size_t delimiterLength = strlen(delimiter);
        std::string line;
        std::getline(is, line);
        while (!is.eof() && strncmp(line.c_str(), delimiter, delimiterLength) != 0)
            std::getline(is, line);

        return line;
    }

    static int64_t ParseBaseAddress(std::istream& is)
    {
        const char* preferredLoadAddressDelimiter = " Preferred load address is ";
        const size_t preferredLoadAddressDelimiterLength = strlen(preferredLoadAddressDelimiter);

        std::string line = ReadUntilDelimiter(is, preferredLoadAddressDelimiter);

        std::string imageBaseString = line.substr(preferredLoadAddressDelimiterLength);
        int64_t imageBaseOffset = 0;
        int fieldsParsed = sscanf(imageBaseString.c_str(), "%llX", &imageBaseOffset);
        if (fieldsParsed != 1)
            throw std::runtime_error("Unable to parse base address.");

        return imageBaseOffset;
    }

    static void ParseSections(std::istream& is, std::vector<Section>& sections)
    {
        std::string line;
        std::getline(is, line);
        int64_t segmentOffset = 0x1000;
        std::string currentSegmentName;
        while (!is.eof() && line.length() > 0)
        {
            Section section = MSVCSectionParser::Parse(line);
            if (currentSegmentName.length() == 0)
                currentSegmentName = section.segmentName;
            else if (currentSegmentName != section.segmentName)
            {
                Section prevSection = sections[sections.size() - 1];
                const int32_t kSectionBoundarySize = 0x1000;
                segmentOffset = prevSection.start + prevSection.length;
                if ((segmentOffset % kSectionBoundarySize) != 0)
                    segmentOffset = (segmentOffset + kSectionBoundarySize) / kSectionBoundarySize * kSectionBoundarySize;
                currentSegmentName = section.segmentName;
            }

            section.start += segmentOffset;
            sections.push_back(section);

            std::getline(is, line);
        }
    }

    static void ParseSymbols(std::istream& is, std::vector<Symbol>& symbols, int64_t imageBaseOffset)
    {
        std::string line;
        std::getline(is, line);

        while (!is.eof())
        {
            std::getline(is, line);
            if (line.length() == 0)
                break;

            symbols.push_back(MSVCSymbolParser::Parse(line, imageBaseOffset));
        }
    }

    MapFile MSVCMapFileParser::Parse(std::istream& is)
    {
        MapFile mapFile;

        int64_t imageBaseOffset = ParseBaseAddress(is);

        ReadUntilDelimiter(is, " Start ");

        ParseSections(is, mapFile.sections);

        ReadUntilDelimiter(is, "  Address ");

        ParseSymbols(is, mapFile.symbols, imageBaseOffset);

        CalculateLengthsAndSegmentTypesForAllSymbols(mapFile);

        return mapFile;
    }
}
