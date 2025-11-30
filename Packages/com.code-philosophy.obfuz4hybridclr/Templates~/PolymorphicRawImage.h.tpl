#pragma once

#include "RawImageBase.h"

namespace hybridclr
{
namespace metadata
{

	class PolymorphicRawImage : public RawImageBase
	{
	public:
		PolymorphicRawImage() : RawImageBase()
		{

		}

		LoadImageErrorCode LoadCLIHeader(uint32_t& entryPointToken, uint32_t& metadataRva, uint32_t& metadataSize) override;
		virtual LoadImageErrorCode LoadStreamHeaders(uint32_t metadataRva, uint32_t metadataSize) override;
		virtual LoadImageErrorCode LoadTables() override;
		virtual void BuildTableRowMetas() override;

        //!!!{{READ_TABLES_OVERRIDES
		virtual TbTypeRef ReadTypeRef(uint32_t rawIndex) override;
		virtual TbTypeDef ReadTypeDef(uint32_t rawIndex) override;
		virtual TbField ReadField(uint32_t rawIndex) override;
		virtual TbMethod ReadMethod(uint32_t rawIndex) override;
		virtual TbParam ReadParam(uint32_t rawIndex) override;
		virtual TbInterfaceImpl ReadInterfaceImpl(uint32_t rawIndex) override;
		virtual TbMemberRef ReadMemberRef(uint32_t rawIndex) override;
		virtual TbConstant ReadConstant(uint32_t rawIndex) override;
		virtual TbCustomAttribute ReadCustomAttribute(uint32_t rawIndex) override;
		virtual TbClassLayout ReadClassLayout(uint32_t rawIndex) override;
		virtual TbFieldLayout ReadFieldLayout(uint32_t rawIndex) override;
		virtual TbStandAloneSig ReadStandAloneSig(uint32_t rawIndex) override;
		virtual TbEventMap ReadEventMap(uint32_t rawIndex) override;
		virtual TbEvent ReadEvent(uint32_t rawIndex) override;
		virtual TbPropertyMap ReadPropertyMap(uint32_t rawIndex) override;
		virtual TbProperty ReadProperty(uint32_t rawIndex) override;
		virtual TbMethodSemantics ReadMethodSemantics(uint32_t rawIndex) override;
		virtual TbMethodImpl ReadMethodImpl(uint32_t rawIndex) override;
		virtual TbModuleRef ReadModuleRef(uint32_t rawIndex) override;
		virtual TbTypeSpec ReadTypeSpec(uint32_t rawIndex) override;
		virtual TbImplMap ReadImplMap(uint32_t rawIndex) override;
		virtual TbFieldRVA ReadFieldRVA(uint32_t rawIndex) override;
		virtual TbAssembly ReadAssembly(uint32_t rawIndex) override;
		virtual TbAssemblyRef ReadAssemblyRef(uint32_t rawIndex) override;
		virtual TbNestedClass ReadNestedClass(uint32_t rawIndex) override;
		virtual TbGenericParam ReadGenericParam(uint32_t rawIndex) override;
		virtual TbMethodSpec ReadMethodSpec(uint32_t rawIndex) override;
		virtual TbGenericParamConstraint ReadGenericParamConstraint(uint32_t rawIndex) override;

        //!!!}}READ_TABLES_OVERRIDES
	private:

	};
}
}
