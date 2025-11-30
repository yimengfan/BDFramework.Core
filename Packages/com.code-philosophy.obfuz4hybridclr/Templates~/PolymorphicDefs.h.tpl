#pragma once
#include "MetadataReader.h"

namespace hybridclr
{
namespace metadata
{
    //!!!{{POLYMORPHIC_DEFINES
#define POLYMORPHIC_IMAGE_SIGNATURE "CODEPHPY"
	constexpr uint32_t kPolymorphicImageVersion = 1;
	constexpr uint32_t kFormatVariantVersion = 0;

    //!!!}}POLYMORPHIC_DEFINES

    struct PolymorphicImageHeaderData
    {
        const byte* signature;
        uint32_t formatVersion;
        uint32_t formatVariant;
        void Read(MetadataReader& reader)
        {
            signature = reader.ReadFixedBytes(8);
            formatVersion = reader.ReadUInt32();
            formatVariant = reader.ReadUInt32();
        }
    };
}
}