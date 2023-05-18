#include "HighLevelBreakdown.h"
#include <sstream>

namespace mapfileparser
{
    static bool Contains(const std::string& haystack, const std::string& needle)
    {
        return haystack.find(needle) != std::string::npos;
    }

    static bool EndsWith(const std::string& haystack, const std::string& needle)
    {
        if (haystack.length() >= needle.length())
            return 0 == haystack.compare(haystack.length() - needle.length(), needle.length(), needle);

        return false;
    }

    static int Percent(int64_t value, int64_t total)
    {
        return static_cast<int>(static_cast<float>(value) / static_cast<float>(total) * 100.0f);
    }

    static void FormatOutput(std::ostream& out, const std::string& name, int64_t value, int64_t total)
    {
        out << name << ": " << value << " bytes (" << Percent(value, total) << "%)" << std::endl;
    }

    static bool IsGeneratedCode(const std::string& objectFile)
    {
        return Contains(objectFile, "Bulk_") || Contains(objectFile, "Il2Cpp");
    }

    static bool IsOtherCode(const std::string& objectFile)
    {
        // This only makes sense for clang output.
        return EndsWith(objectFile, ".o)");
    }

    std::string HighLevelBreakdown(const std::vector<Symbol>& symbols)
    {
        int64_t totalCodeSizeBytes = 0;
        int64_t generatedCodeSizeBytes = 0;
        int64_t otherCodeSizeBytes = 0;
        int64_t engineCodeSizeBytes = 0;

        for (std::vector<Symbol>::const_iterator symbol = symbols.begin(); symbol != symbols.end(); ++symbol)
        {
            if (symbol->segmentType == kSegmentTypeCode)
            {
                totalCodeSizeBytes += symbol->length;
                if (IsGeneratedCode(symbol->objectFile))
                    generatedCodeSizeBytes += symbol->length;
                else if (IsOtherCode(symbol->objectFile))
                    otherCodeSizeBytes += symbol->length;
                else
                    engineCodeSizeBytes += symbol->length;
            }
        }

        std::stringstream output;
        output << "High level breakdown of code segments\n";
        output << "-------------------------------------\n";
        FormatOutput(output, "Total code", totalCodeSizeBytes, totalCodeSizeBytes);
        FormatOutput(output, "Generated code", generatedCodeSizeBytes, totalCodeSizeBytes);
        FormatOutput(output, "Engine code", engineCodeSizeBytes, totalCodeSizeBytes);
        FormatOutput(output, "Other code", otherCodeSizeBytes, totalCodeSizeBytes);

        return output.str();
    }
}
