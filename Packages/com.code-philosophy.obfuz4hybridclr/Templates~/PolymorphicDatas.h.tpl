#pragma once
#include "MetadataReader.h"

namespace hybridclr
{
namespace metadata
{
    //!!!{{POLYMORPHIC_DATA
	struct HeaderBaseData
	{
		uint32_t metadataSize;
		uint32_t sectionCount;
		const byte* dummyData;
		uint32_t metadataRva;
		uint32_t entryPointToken;
		void Read(MetadataReader& reader)
		{
			metadataSize = reader.ReadUInt32();
			sectionCount = reader.ReadUInt32();
			dummyData = reader.ReadFixedBytes(8);
			metadataRva = reader.ReadUInt32();
			entryPointToken = reader.ReadUInt32();
		}
	};

	struct SectionData
	{
		uint32_t rva;
		uint32_t fileOffset;
		uint32_t virtualSize;
		uint32_t fileLength;
		void Read(MetadataReader& reader)
		{
			rva = reader.ReadUInt32();
			fileOffset = reader.ReadUInt32();
			virtualSize = reader.ReadUInt32();
			fileLength = reader.ReadUInt32();
		}
	};

	struct MetadataHeaderBaseData
	{
		uint32_t signature;
		uint8_t reserved2;
		ByteSpan versionString;
		uint16_t majorVersion;
		uint16_t heapsCount;
		uint32_t reserved1;
		uint8_t storageFlags;
		uint16_t minorVersion;
		void Read(MetadataReader& reader)
		{
			signature = reader.ReadUInt32();
			reserved2 = reader.ReadUInt8();
			versionString = reader.ReadBytes();
			majorVersion = reader.ReadUInt16();
			heapsCount = reader.ReadUInt16();
			reserved1 = reader.ReadUInt32();
			storageFlags = reader.ReadUInt8();
			minorVersion = reader.ReadUInt16();
		}
	};

	struct TablesHeapHeaderBaseData
	{
		uint64_t validMask;
		uint32_t reserved1;
		uint8_t streamFlags;
		uint8_t majorVersion;
		uint64_t sortedMask;
		uint8_t minorVersion;
		uint8_t log2Rid;
		void Read(MetadataReader& reader)
		{
			validMask = reader.ReadUInt64();
			reserved1 = reader.ReadUInt32();
			streamFlags = reader.ReadUInt8();
			majorVersion = reader.ReadUInt8();
			sortedMask = reader.ReadUInt64();
			minorVersion = reader.ReadUInt8();
			log2Rid = reader.ReadUInt8();
		}
	};


	//!!!}}POLYMORPHIC_DATA
}
}