#include "PolymorphicRawImage.h"

#include <memory>

#include "PolymorphicDefs.h"
#include "PolymorphicDatas.h"

namespace hybridclr
{
namespace metadata
{

	LoadImageErrorCode PolymorphicRawImage::LoadCLIHeader(uint32_t& entryPointToken, uint32_t& metadataRva, uint32_t& metadataSize)
	{
		if (_imageLength < 0x100)
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}

		MetadataReader reader(_imageData);

		PolymorphicImageHeaderData imageHeaderData = {};
        imageHeaderData.Read(reader);

		const char* sig = (const char*)_imageData;
		if (std::strncmp((const char*)imageHeaderData.signature, POLYMORPHIC_IMAGE_SIGNATURE, sizeof(POLYMORPHIC_IMAGE_SIGNATURE) - 1))
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}
		if (imageHeaderData.formatVersion != kPolymorphicImageVersion)
		{
			return LoadImageErrorCode::UNSUPPORT_FORMAT_VERSION;
		}
		if (imageHeaderData.formatVariant != kFormatVariantVersion)
		{
			return LoadImageErrorCode::UNMATCH_FORMAT_VARIANT;
		}

        //reader.ReadFixedBytes(polymorphic::kImageHeaderDummyDataSize); // Skip dummy data

		PolymorphicHeaderBaseData headerBaseData = {};
		headerBaseData.Read(reader);

		const size_t kEntryPointTokenOffset = 16;
		entryPointToken = headerBaseData.entryPointToken;
		metadataRva = headerBaseData.metadataRva;
		metadataSize = headerBaseData.metadataSize;

		uint32_t sectionCount = headerBaseData.sectionCount;
		for (uint32_t i = 0; i < sectionCount; i++)
		{
			PolymorphicSectionData sectionData = {};
			sectionData.Read(reader);
			_sections.push_back({ sectionData.rva, sectionData.rva + sectionData.virtualSize, sectionData.fileOffset - sectionData.rva });
		}
		return LoadImageErrorCode::OK;
	}

	LoadImageErrorCode PolymorphicRawImage::LoadStreamHeaders(uint32_t metadataRva, uint32_t metadataSize)
	{
		uint32_t metaOffset;
		if (!TranslateRVAToImageOffset(metadataRva, metaOffset))
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}
		if (metaOffset >= _imageLength)
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}

        const byte* ptrMetaData = _imageData + metaOffset;
        MetadataReader reader(ptrMetaData);

		PolymorphicMetadataHeaderBaseData metadataHeader = {};
        metadataHeader.Read(reader);
		if (metadataHeader.signature != 0x424A5342)
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}

		uint16_t numStreamHeader = metadataHeader.heapsCount;
		const StreamHeader* ptrStreamHeaders = (const StreamHeader*)(reader.CurrentDataPtr());

		const StreamHeader* curSH = ptrStreamHeaders;
		const size_t maxStreamNameSize = 16;
		for (int i = 0; i < numStreamHeader; i++)
		{
			//std::cout << "name:" << (char*)curSH->name << ", offset:" << curSH->offset << ", size:" << curSH->size << std::endl;

			if (curSH->offset >= metadataSize)
			{
				return LoadImageErrorCode::BAD_IMAGE;
			}
			CliStream* rs = nullptr;
			CliStream nonStandardStream;
			CliStream pdbStream;
			if (!std::strncmp(curSH->name, "#~", maxStreamNameSize))
			{
				rs = &_streamTables;
			}
			else if (!std::strncmp(curSH->name, "#Strings", maxStreamNameSize))
			{
				rs = &_streamStringHeap;
			}
			else if (!std::strncmp(curSH->name, "#US", maxStreamNameSize))
			{
				rs = &_streamUS;
			}
			else if (!std::strncmp(curSH->name, "#GUID", maxStreamNameSize))
			{
				rs = &_streamGuidHeap;
				if (curSH->size % 16 != 0)
				{
					return LoadImageErrorCode::BAD_IMAGE;
				}
			}
			else if (!std::strncmp(curSH->name, "#Blob", maxStreamNameSize))
			{
				rs = &_streamBlobHeap;
			}
			else if (!std::strncmp(curSH->name, "#-", maxStreamNameSize))
			{
				rs = &nonStandardStream;
			}
			else if (!std::strncmp(curSH->name, "#Pdb", maxStreamNameSize))
			{
				rs = &pdbStream;
			}
			else
			{
				//std::cerr << "unknown stream name:" << curSH->name << std::endl;
				return LoadImageErrorCode::BAD_IMAGE;
			}
			rs->data = ptrMetaData + curSH->offset;
			rs->size = curSH->size;
			rs->name = curSH->name;
			size_t sizeOfStream = 8 + (std::strlen(curSH->name) / 4 + 1) * 4;
			curSH = (const StreamHeader*)((byte*)curSH + sizeOfStream);
		}
		return LoadImageErrorCode::OK;
	}

	LoadImageErrorCode PolymorphicRawImage::LoadTables()
	{
		MetadataReader reader(_streamTables.data);

		PolymorphicTablesHeapHeaderBaseData heapHeader = {};
		heapHeader.Read(reader);

		if (heapHeader.reserved1 != 0 || heapHeader.majorVersion != 2 || heapHeader.minorVersion != 0)
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}
		if ((heapHeader.streamFlags & ~0x7))
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}
		_4byteStringIndex = heapHeader.streamFlags & 0x1;
		_4byteGUIDIndex = heapHeader.streamFlags & 0x2;
		_4byteBlobIndex = heapHeader.streamFlags & 0x4;

		uint64_t validMask = ((uint64_t)1 << TABLE_NUM) - 1;
		if (heapHeader.validMask & ~validMask)
		{
			return LoadImageErrorCode::BAD_IMAGE;
		}
		// sorted include not exist table, so check is not need.
		//if (heapHeader.sorted & ~validMask)
		//{
		//	return LoadImageErrorCode::BAD_IMAGE;
		//}

		uint32_t validTableNum = GetNotZeroBitCount(heapHeader.validMask);
		//std::cout << "valid table num:" << validTableNum << std::endl;
		//printf("#~ size:%0x\n", _streamTables.size);
		const uint32_t* tableRowNums = (uint32_t*)(reader.CurrentDataPtr());
		const byte* tableDataBegin = (const byte*)(tableRowNums + validTableNum);

		{
			int curValidTableIndex = 0;
			for (int i = 0; i <= MAX_TABLE_INDEX; i++)
			{
				uint64_t mask = (uint64_t)1 << i;
				_tables[i] = {};
				if (heapHeader.validMask & mask)
				{
					uint32_t rowNum = tableRowNums[curValidTableIndex];
					_tables[i].rowNum = rowNum;
					++curValidTableIndex;
				}
			}
		}

		BuildTableRowMetas();

		int curValidTableIndex = 0;
		const byte* curTableData = tableDataBegin;
		for (int i = 0; i <= MAX_TABLE_INDEX; i++)
		{
			uint64_t mask = (uint64_t)1 << i;
			bool sorted = heapHeader.sortedMask & mask;
			if (heapHeader.validMask & mask)
			{
				uint32_t rowNum = tableRowNums[curValidTableIndex];
				uint32_t totalSize = 0;
				auto& table = _tableRowMetas[i];
				for (auto& col : table)
				{
					col.offset = totalSize;
					totalSize += col.size;
				}
				uint32_t metaDataRowSize = totalSize;
				//uint64_t offset = curTableData - _imageData;
				_tables[i] = { curTableData, metaDataRowSize, rowNum, true, sorted };
				curTableData += metaDataRowSize * rowNum;
				//std::cout << "table:" << i << " ," << curValidTableIndex << ", row_size:" << metaDataRowSize << ", row_num:" << rowNum << std::endl;
				//printf("table:[%d][%d] offset:%0llx row_size:%d row_count:%d\n", i, curValidTableIndex, offset, metaDataRowSize, rowNum);
				++curValidTableIndex;
			}
			else
			{
				_tables[i] = { nullptr, 0, 0, false, sorted };
			}
		}

		return LoadImageErrorCode::OK;
	}

	void PolymorphicRawImage::BuildTableRowMetas()
	{
		//!!!{{TABLE_ROW_METADS
		{
			auto& table = _tableRowMetas[(int)TableType::MODULE];
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputGUIDIndexByte()});
			table.push_back({ComputGUIDIndexByte()});
			table.push_back({ComputGUIDIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::TYPEREF];
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputTableIndexByte(TableType::MODULE, TableType::MODULEREF, TableType::ASSEMBLYREF, TableType::TYPEREF, TagBits::ResoulutionScope)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::TYPEDEF];
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputTableIndexByte(TableType::FIELD)});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)});
			table.push_back({4});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputTableIndexByte(TableType::METHOD)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::FIELDPTR];
			table.push_back({ComputTableIndexByte(TableType::FIELD)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::FIELD];
			table.push_back({ComputBlobIndexByte()});
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::METHODPTR];
			table.push_back({ComputTableIndexByte(TableType::METHOD)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::METHOD];
			table.push_back({ComputBlobIndexByte()});
			table.push_back({2});
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputTableIndexByte(TableType::PARAM)});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::PARAMPTR];
			table.push_back({ComputTableIndexByte(TableType::PARAM)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::PARAM];
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
			table.push_back({2});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::INTERFACEIMPL];
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF)});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::MEMBERREF];
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputBlobIndexByte()});
			table.push_back({ComputTableIndexByte(TableType::METHOD, TableType::MODULEREF, TableType::TYPEDEF, TableType::TYPEREF, TagBits::MemberRefParent)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::CONSTANT];
			table.push_back({1});
			table.push_back({1});
			table.push_back({ComputTableIndexByte(TableType::PARAM, TableType::FIELD, TableType::PROPERTY, TagBits::HasConstant)});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::CUSTOMATTRIBUTE];
			table.push_back({ComputTableIndexByte(HasCustomAttributeAssociateTables, sizeof(HasCustomAttributeAssociateTables) / sizeof(TableType), TagBits::HasCustomAttribute)});
			table.push_back({ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::CustomAttributeType)});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::FIELDMARSHAL];
			table.push_back({ComputTableIndexByte(TableType::FIELD, TableType::PARAM, TagBits::HasFieldMarshal)});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::DECLSECURITY];
			table.push_back({2});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF, TableType::METHOD, TableType::ASSEMBLY, TagBits::HasDeclSecurity)});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::CLASSLAYOUT];
			table.push_back({4});
			table.push_back({2});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::FIELDLAYOUT];
			table.push_back({ComputTableIndexByte(TableType::FIELD)});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::STANDALONESIG];
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::EVENTMAP];
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF)});
			table.push_back({ComputTableIndexByte(TableType::EVENT)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::EVENTPTR];
			table.push_back({ComputTableIndexByte(TableType::EVENT)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::EVENT];
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)});
			table.push_back({ComputStringIndexByte()});
			table.push_back({2});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::PROPERTYMAP];
			table.push_back({ComputTableIndexByte(TableType::PROPERTY)});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::PROPERTYPTR];
			table.push_back({ComputTableIndexByte(TableType::PROPERTY)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::PROPERTY];
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::METHODSEMANTICS];
			table.push_back({ComputTableIndexByte(TableType::EVENT, TableType::PROPERTY, TagBits::HasSemantics)});
			table.push_back({ComputTableIndexByte(TableType::METHOD)});
			table.push_back({2});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::METHODIMPL];
			table.push_back({ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::MethodDefOrRef)});
			table.push_back({ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::MethodDefOrRef)});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::MODULEREF];
			table.push_back({ComputStringIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::TYPESPEC];
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::IMPLMAP];
			table.push_back({2});
			table.push_back({ComputTableIndexByte(TableType::MODULEREF)});
			table.push_back({ComputTableIndexByte(TableType::FIELD, TableType::METHOD, TagBits::MemberForwarded)});
			table.push_back({ComputStringIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::FIELDRVA];
			table.push_back({ComputTableIndexByte(TableType::FIELD)});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ENCLOG];
			table.push_back({4});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ENCMAP];
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ASSEMBLY];
			table.push_back({2});
			table.push_back({4});
			table.push_back({2});
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputBlobIndexByte()});
			table.push_back({2});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ASSEMBLYPROCESSOR];
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ASSEMBLYOS];
			table.push_back({4});
			table.push_back({4});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ASSEMBLYREF];
			table.push_back({4});
			table.push_back({2});
			table.push_back({2});
			table.push_back({ComputBlobIndexByte()});
			table.push_back({ComputBlobIndexByte()});
			table.push_back({2});
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputStringIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ASSEMBLYREFPROCESSOR];
			table.push_back({ComputTableIndexByte(TableType::ASSEMBLYREF)});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::ASSEMBLYREFOS];
			table.push_back({4});
			table.push_back({4});
			table.push_back({4});
			table.push_back({ComputTableIndexByte(TableType::ASSEMBLYREF)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::FILE];
			table.push_back({4});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::EXPORTEDTYPE];
			table.push_back({4});
			table.push_back({4});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputTableIndexByte(TableType::FILE, TableType::EXPORTEDTYPE, TableType::ASSEMBLY, TagBits::Implementation)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::MANIFESTRESOURCE];
			table.push_back({4});
			table.push_back({4});
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputTableIndexByte(TableType::FILE, TableType::ASSEMBLYREF, TagBits::Implementation)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::NESTEDCLASS];
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF)});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::GENERICPARAM];
			table.push_back({2});
			table.push_back({2});
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF, TableType::METHOD, TagBits::TypeOrMethodDef)});
			table.push_back({ComputStringIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::METHODSPEC];
			table.push_back({ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::MethodDefOrRef)});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::GENERICPARAMCONSTRAINT];
			table.push_back({ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)});
			table.push_back({ComputTableIndexByte(TableType::GENERICPARAM)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::DOCUMENT];
			table.push_back({ComputBlobIndexByte()});
			table.push_back({ComputGUIDIndexByte()});
			table.push_back({ComputBlobIndexByte()});
			table.push_back({ComputGUIDIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::METHODDEBUGINFORMATION];
			table.push_back({ComputTableIndexByte(TableType::DOCUMENT)});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::LOCALSCOPE];
			table.push_back({ComputTableIndexByte(TableType::METHOD)});
			table.push_back({ComputTableIndexByte(TableType::IMPORTSCOPE)});
			table.push_back({ComputTableIndexByte(TableType::LOCALVARIABLE)});
			table.push_back({ComputTableIndexByte(TableType::LOCALCONSTANT)});
			table.push_back({4});
			table.push_back({4});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::LOCALVARIABLE];
			table.push_back({2});
			table.push_back({2});
			table.push_back({ComputStringIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::LOCALCONSTANT];
			table.push_back({ComputStringIndexByte()});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::IMPORTSCOPE];
			table.push_back({ComputTableIndexByte(TableType::IMPORTSCOPE)});
			table.push_back({ComputBlobIndexByte()});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::STATEMACHINEMETHOD];
			table.push_back({ComputTableIndexByte(TableType::METHOD)});
			table.push_back({ComputTableIndexByte(TableType::METHOD)});
		}
		{
			auto& table = _tableRowMetas[(int)TableType::CUSTOMDEBUGINFORMATION];
			table.push_back({ComputTableIndexByte(HasCustomDebugInformation, sizeof(HasCustomDebugInformation) / sizeof(TableType), TagBits::HasCustomDebugInformation)});
			table.push_back({ComputGUIDIndexByte()});
			table.push_back({ComputBlobIndexByte()});
		}

		//!!!}}TABLE_ROW_METADS

		for (int i = 0; i < TABLE_NUM; i++)
		{
			auto& table = _tableRowMetas[i];
			if (table.empty())
			{
				IL2CPP_ASSERT(_tables[i].rowNum == 0 && _tables[i].rowMetaDataSize == 0);
			}
			else
			{
				uint32_t totalSize = 0;
				for (auto& col : table)
				{
					col.offset = totalSize;
					totalSize += col.size;
				}
				uint32_t computSize = ComputTableRowMetaDataSize((TableType)i);
				IL2CPP_ASSERT(totalSize == computSize);
			}
		}
	}

    //!!!{{READ_TABLES_IMPLEMENTATIONS
	TbTypeRef PolymorphicRawImage::ReadTypeRef(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::TYPEREF).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::TYPEREF, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::TYPEREF);
		TbTypeRef data;
		data.typeNamespace = ReadColumn(rowPtr, rowSchema[0]);
		data.typeName = ReadColumn(rowPtr, rowSchema[1]);
		data.resolutionScope = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbTypeDef PolymorphicRawImage::ReadTypeDef(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::TYPEDEF).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::TYPEDEF, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::TYPEDEF);
		TbTypeDef data;
		data.typeName = ReadColumn(rowPtr, rowSchema[0]);
		data.fieldList = ReadColumn(rowPtr, rowSchema[1]);
		data.extends = ReadColumn(rowPtr, rowSchema[2]);
		data.flags = ReadColumn(rowPtr, rowSchema[3]);
		data.typeNamespace = ReadColumn(rowPtr, rowSchema[4]);
		data.methodList = ReadColumn(rowPtr, rowSchema[5]);
		return data;
	}
	TbField PolymorphicRawImage::ReadField(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::FIELD).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::FIELD, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::FIELD);
		TbField data;
		data.signature = ReadColumn(rowPtr, rowSchema[0]);
		data.flags = ReadColumn(rowPtr, rowSchema[1]);
		data.name = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbMethod PolymorphicRawImage::ReadMethod(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::METHOD).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::METHOD, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::METHOD);
		TbMethod data;
		data.signature = ReadColumn(rowPtr, rowSchema[0]);
		data.flags = ReadColumn(rowPtr, rowSchema[1]);
		data.implFlags = ReadColumn(rowPtr, rowSchema[2]);
		data.name = ReadColumn(rowPtr, rowSchema[3]);
		data.paramList = ReadColumn(rowPtr, rowSchema[4]);
		data.rva = ReadColumn(rowPtr, rowSchema[5]);
		return data;
	}
	TbParam PolymorphicRawImage::ReadParam(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::PARAM).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::PARAM, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::PARAM);
		TbParam data;
		data.flags = ReadColumn(rowPtr, rowSchema[0]);
		data.name = ReadColumn(rowPtr, rowSchema[1]);
		data.sequence = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbInterfaceImpl PolymorphicRawImage::ReadInterfaceImpl(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::INTERFACEIMPL).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::INTERFACEIMPL, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::INTERFACEIMPL);
		TbInterfaceImpl data;
		data.classIdx = ReadColumn(rowPtr, rowSchema[0]);
		data.interfaceIdx = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}
	TbMemberRef PolymorphicRawImage::ReadMemberRef(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::MEMBERREF).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::MEMBERREF, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::MEMBERREF);
		TbMemberRef data;
		data.name = ReadColumn(rowPtr, rowSchema[0]);
		data.signature = ReadColumn(rowPtr, rowSchema[1]);
		data.classIdx = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbConstant PolymorphicRawImage::ReadConstant(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::CONSTANT).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::CONSTANT, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::CONSTANT);
		TbConstant data;
		data.padding = ReadColumn(rowPtr, rowSchema[0]);
		data.type = ReadColumn(rowPtr, rowSchema[1]);
		data.parent = ReadColumn(rowPtr, rowSchema[2]);
		data.value = ReadColumn(rowPtr, rowSchema[3]);
		return data;
	}
	TbCustomAttribute PolymorphicRawImage::ReadCustomAttribute(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::CUSTOMATTRIBUTE).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::CUSTOMATTRIBUTE, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::CUSTOMATTRIBUTE);
		TbCustomAttribute data;
		data.parent = ReadColumn(rowPtr, rowSchema[0]);
		data.type = ReadColumn(rowPtr, rowSchema[1]);
		data.value = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbClassLayout PolymorphicRawImage::ReadClassLayout(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::CLASSLAYOUT).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::CLASSLAYOUT, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::CLASSLAYOUT);
		TbClassLayout data;
		data.classSize = ReadColumn(rowPtr, rowSchema[0]);
		data.packingSize = ReadColumn(rowPtr, rowSchema[1]);
		data.parent = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbFieldLayout PolymorphicRawImage::ReadFieldLayout(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::FIELDLAYOUT).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::FIELDLAYOUT, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::FIELDLAYOUT);
		TbFieldLayout data;
		data.field = ReadColumn(rowPtr, rowSchema[0]);
		data.offset = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}
	TbStandAloneSig PolymorphicRawImage::ReadStandAloneSig(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::STANDALONESIG).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::STANDALONESIG, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::STANDALONESIG);
		TbStandAloneSig data;
		data.signature = ReadColumn(rowPtr, rowSchema[0]);
		return data;
	}
	TbEventMap PolymorphicRawImage::ReadEventMap(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::EVENTMAP).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::EVENTMAP, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::EVENTMAP);
		TbEventMap data;
		data.parent = ReadColumn(rowPtr, rowSchema[0]);
		data.eventList = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}
	TbEvent PolymorphicRawImage::ReadEvent(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::EVENT).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::EVENT, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::EVENT);
		TbEvent data;
		data.eventType = ReadColumn(rowPtr, rowSchema[0]);
		data.name = ReadColumn(rowPtr, rowSchema[1]);
		data.eventFlags = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbPropertyMap PolymorphicRawImage::ReadPropertyMap(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::PROPERTYMAP).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::PROPERTYMAP, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::PROPERTYMAP);
		TbPropertyMap data;
		data.propertyList = ReadColumn(rowPtr, rowSchema[0]);
		data.parent = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}
	TbProperty PolymorphicRawImage::ReadProperty(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::PROPERTY).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::PROPERTY, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::PROPERTY);
		TbProperty data;
		data.flags = ReadColumn(rowPtr, rowSchema[0]);
		data.name = ReadColumn(rowPtr, rowSchema[1]);
		data.type = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbMethodSemantics PolymorphicRawImage::ReadMethodSemantics(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::METHODSEMANTICS).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::METHODSEMANTICS, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::METHODSEMANTICS);
		TbMethodSemantics data;
		data.association = ReadColumn(rowPtr, rowSchema[0]);
		data.method = ReadColumn(rowPtr, rowSchema[1]);
		data.semantics = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbMethodImpl PolymorphicRawImage::ReadMethodImpl(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::METHODIMPL).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::METHODIMPL, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::METHODIMPL);
		TbMethodImpl data;
		data.methodDeclaration = ReadColumn(rowPtr, rowSchema[0]);
		data.methodBody = ReadColumn(rowPtr, rowSchema[1]);
		data.classIdx = ReadColumn(rowPtr, rowSchema[2]);
		return data;
	}
	TbModuleRef PolymorphicRawImage::ReadModuleRef(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::MODULEREF).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::MODULEREF, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::MODULEREF);
		TbModuleRef data;
		data.name = ReadColumn(rowPtr, rowSchema[0]);
		return data;
	}
	TbTypeSpec PolymorphicRawImage::ReadTypeSpec(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::TYPESPEC).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::TYPESPEC, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::TYPESPEC);
		TbTypeSpec data;
		data.signature = ReadColumn(rowPtr, rowSchema[0]);
		return data;
	}
	TbImplMap PolymorphicRawImage::ReadImplMap(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::IMPLMAP).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::IMPLMAP, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::IMPLMAP);
		TbImplMap data;
		data.mappingFlags = ReadColumn(rowPtr, rowSchema[0]);
		data.importScope = ReadColumn(rowPtr, rowSchema[1]);
		data.memberForwarded = ReadColumn(rowPtr, rowSchema[2]);
		data.importName = ReadColumn(rowPtr, rowSchema[3]);
		return data;
	}
	TbFieldRVA PolymorphicRawImage::ReadFieldRVA(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::FIELDRVA).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::FIELDRVA, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::FIELDRVA);
		TbFieldRVA data;
		data.field = ReadColumn(rowPtr, rowSchema[0]);
		data.rva = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}
	TbAssembly PolymorphicRawImage::ReadAssembly(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::ASSEMBLY).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::ASSEMBLY, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::ASSEMBLY);
		TbAssembly data;
		data.minorVersion = ReadColumn(rowPtr, rowSchema[0]);
		data.hashAlgId = ReadColumn(rowPtr, rowSchema[1]);
		data.buildNumber = ReadColumn(rowPtr, rowSchema[2]);
		data.revisionNumber = ReadColumn(rowPtr, rowSchema[3]);
		data.locale = ReadColumn(rowPtr, rowSchema[4]);
		data.name = ReadColumn(rowPtr, rowSchema[5]);
		data.publicKey = ReadColumn(rowPtr, rowSchema[6]);
		data.majorVersion = ReadColumn(rowPtr, rowSchema[7]);
		data.flags = ReadColumn(rowPtr, rowSchema[8]);
		return data;
	}
	TbAssemblyRef PolymorphicRawImage::ReadAssemblyRef(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::ASSEMBLYREF).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::ASSEMBLYREF, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::ASSEMBLYREF);
		TbAssemblyRef data;
		data.flags = ReadColumn(rowPtr, rowSchema[0]);
		data.majorVersion = ReadColumn(rowPtr, rowSchema[1]);
		data.buildNumber = ReadColumn(rowPtr, rowSchema[2]);
		data.publicKeyOrToken = ReadColumn(rowPtr, rowSchema[3]);
		data.hashValue = ReadColumn(rowPtr, rowSchema[4]);
		data.revisionNumber = ReadColumn(rowPtr, rowSchema[5]);
		data.minorVersion = ReadColumn(rowPtr, rowSchema[6]);
		data.locale = ReadColumn(rowPtr, rowSchema[7]);
		data.name = ReadColumn(rowPtr, rowSchema[8]);
		return data;
	}
	TbNestedClass PolymorphicRawImage::ReadNestedClass(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::NESTEDCLASS).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::NESTEDCLASS, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::NESTEDCLASS);
		TbNestedClass data;
		data.enclosingClass = ReadColumn(rowPtr, rowSchema[0]);
		data.nestedClass = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}
	TbGenericParam PolymorphicRawImage::ReadGenericParam(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::GENERICPARAM).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::GENERICPARAM, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::GENERICPARAM);
		TbGenericParam data;
		data.flags = ReadColumn(rowPtr, rowSchema[0]);
		data.number = ReadColumn(rowPtr, rowSchema[1]);
		data.owner = ReadColumn(rowPtr, rowSchema[2]);
		data.name = ReadColumn(rowPtr, rowSchema[3]);
		return data;
	}
	TbMethodSpec PolymorphicRawImage::ReadMethodSpec(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::METHODSPEC).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::METHODSPEC, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::METHODSPEC);
		TbMethodSpec data;
		data.method = ReadColumn(rowPtr, rowSchema[0]);
		data.instantiation = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}
	TbGenericParamConstraint PolymorphicRawImage::ReadGenericParamConstraint(uint32_t rawIndex)
	{
		IL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::GENERICPARAMCONSTRAINT).rowNum);
		const byte* rowPtr = GetTableRowPtr(TableType::GENERICPARAMCONSTRAINT, rawIndex);
		auto& rowSchema = GetRowSchema(TableType::GENERICPARAMCONSTRAINT);
		TbGenericParamConstraint data;
		data.constraint = ReadColumn(rowPtr, rowSchema[0]);
		data.owner = ReadColumn(rowPtr, rowSchema[1]);
		return data;
	}

	//!!!}}READ_TABLES_IMPLEMENTATIONS
}
}