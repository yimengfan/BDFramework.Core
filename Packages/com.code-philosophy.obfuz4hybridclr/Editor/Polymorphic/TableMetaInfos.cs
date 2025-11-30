// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Generic;

class FieldMetaInfo {
	public readonly string csharpName;
	public readonly string cppName;
	public readonly string cppRowSize;
	public FieldMetaInfo(string csharpName, string cppName, string cppRowSize) {
		this.csharpName = csharpName;
		this.cppName = cppName;
		this.cppRowSize = cppRowSize;
	}

	public FieldMetaInfo(string csharpName, string cppRowSize) : this(csharpName, csharpName.Substring(0, 1).ToLower() + csharpName.Substring(1), cppRowSize) {
	}
}

class TableMetaInfo {
	public readonly string csharpTypeName;
	public readonly string cppTypeName;
	public readonly string cppEnumName;
	public readonly List<FieldMetaInfo> fields;

	public TableMetaInfo(string csharpTypeName, string cppTypeName, string cppEnumName, List<FieldMetaInfo> fields) {
		this.csharpTypeName = csharpTypeName;
		this.cppTypeName = cppTypeName;
		this.cppEnumName = cppEnumName;
		this.fields = fields;
	}


	public TableMetaInfo(string csharpTypeName, List<FieldMetaInfo> fields) : this(csharpTypeName, csharpTypeName, csharpTypeName.ToUpper(), fields) {
	}
}

class TableMetaInfos {
	public static readonly List<TableMetaInfo> tableMetaInfos = new List<TableMetaInfo> {
		new TableMetaInfo("Module", new List<FieldMetaInfo> {
			new FieldMetaInfo("Generation", "2"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Mvid", "ComputGUIDIndexByte()"),
			new FieldMetaInfo("EncId", "ComputGUIDIndexByte()"),
			new FieldMetaInfo("EncBaseId", "ComputGUIDIndexByte()"),
		}),
		new TableMetaInfo("TypeRef", new List<FieldMetaInfo> {
			new FieldMetaInfo("ResolutionScope", "ComputTableIndexByte(TableType::MODULE, TableType::MODULEREF, TableType::ASSEMBLYREF, TableType::TYPEREF, TagBits::ResoulutionScope)"),
			new FieldMetaInfo("Name", "typeName", "ComputStringIndexByte()"),
			new FieldMetaInfo("Namespace", "typeNamespace", "ComputStringIndexByte()"),
		}),
		new TableMetaInfo("TypeDef", new List<FieldMetaInfo> {
			new FieldMetaInfo("Flags", "4"),
			new FieldMetaInfo("Name", "typeName", "ComputStringIndexByte()"),
			new FieldMetaInfo("Namespace", "typeNamespace", "ComputStringIndexByte()"),
			new FieldMetaInfo("Extends", "ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)"),
			new FieldMetaInfo("FieldList", "ComputTableIndexByte(TableType::FIELD)"),
			new FieldMetaInfo("MethodList", "ComputTableIndexByte(TableType::METHOD)"),
		}),
		new TableMetaInfo("FieldPtr", new List<FieldMetaInfo> {
			new FieldMetaInfo("Field", "ComputTableIndexByte(TableType::FIELD)"),
		}),
		new TableMetaInfo("Field", new List<FieldMetaInfo> {
			new FieldMetaInfo("Flags", "2"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Signature", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("MethodPtr", new List<FieldMetaInfo> {
			new FieldMetaInfo("Method", "ComputTableIndexByte(TableType::METHOD)"),
		}),
		new TableMetaInfo("Method", new List<FieldMetaInfo> {
			new FieldMetaInfo("RVA", "rva", "4"),
			new FieldMetaInfo("ImplFlags", "2"),
			new FieldMetaInfo("Flags", "2"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Signature", "ComputBlobIndexByte()"),
			new FieldMetaInfo("ParamList", "ComputTableIndexByte(TableType::PARAM)"),
		}),
		new TableMetaInfo("ParamPtr", new List<FieldMetaInfo> {
			new FieldMetaInfo("Param", "ComputTableIndexByte(TableType::PARAM)"),
		}),
		new TableMetaInfo("Param", new List<FieldMetaInfo> {
			new FieldMetaInfo("Flags", "2"),
			new FieldMetaInfo("Sequence", "2"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
		}),
		new TableMetaInfo("InterfaceImpl", new List<FieldMetaInfo> {
			new FieldMetaInfo("Class", "classIdx", "ComputTableIndexByte(TableType::TYPEDEF)"),
			new FieldMetaInfo("Interface", "interfaceIdx", "ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)"),
		}),
		new TableMetaInfo("MemberRef", new List<FieldMetaInfo> {
			new FieldMetaInfo("Class", "classIdx", "ComputTableIndexByte(TableType::METHOD, TableType::MODULEREF, TableType::TYPEDEF, TableType::TYPEREF, TagBits::MemberRefParent)"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Signature", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("Constant", new List<FieldMetaInfo> {
			new FieldMetaInfo("Type", "1"),
			new FieldMetaInfo("Padding", "1"),
			new FieldMetaInfo("Parent", "ComputTableIndexByte(TableType::PARAM, TableType::FIELD, TableType::PROPERTY, TagBits::HasConstant)"),
			new FieldMetaInfo("Value", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("CustomAttribute", new List<FieldMetaInfo> {
			new FieldMetaInfo("Parent", "ComputTableIndexByte(HasCustomAttributeAssociateTables, sizeof(HasCustomAttributeAssociateTables) / sizeof(TableType), TagBits::HasCustomAttribute)"),
			new FieldMetaInfo("Type", "ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::CustomAttributeType)"),
			new FieldMetaInfo("Value", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("FieldMarshal", new List<FieldMetaInfo> {
			new FieldMetaInfo("Parent", "ComputTableIndexByte(TableType::FIELD, TableType::PARAM, TagBits::HasFieldMarshal)"),
			new FieldMetaInfo("NativeType", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("DeclSecurity", new List<FieldMetaInfo> {
			new FieldMetaInfo("Action", "2"),
			new FieldMetaInfo("Parent", "ComputTableIndexByte(TableType::TYPEDEF, TableType::METHOD, TableType::ASSEMBLY, TagBits::HasDeclSecurity)"),
			new FieldMetaInfo("PermissionSet", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("ClassLayout", new List<FieldMetaInfo> {
			new FieldMetaInfo("PackingSize", "2"),
			new FieldMetaInfo("ClassSize", "4"),
			new FieldMetaInfo("Parent", "ComputTableIndexByte(TableType::TYPEDEF)"),
		}),
		new TableMetaInfo("FieldLayout", new List<FieldMetaInfo> {
			new FieldMetaInfo("OffSet", "offset", "4"),
			new FieldMetaInfo("Field", "ComputTableIndexByte(TableType::FIELD)"),
		}),
		new TableMetaInfo("StandAloneSig", new List<FieldMetaInfo> {
			new FieldMetaInfo("Signature", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("EventMap", new List<FieldMetaInfo> {
			new FieldMetaInfo("Parent", "ComputTableIndexByte(TableType::TYPEDEF)"),
			new FieldMetaInfo("EventList", "ComputTableIndexByte(TableType::EVENT)"),
		}),
		new TableMetaInfo("EventPtr", new List<FieldMetaInfo> {
			new FieldMetaInfo("Event", "ComputTableIndexByte(TableType::EVENT)"),
		}),
		new TableMetaInfo("Event", new List<FieldMetaInfo> {
			new FieldMetaInfo("EventFlags", "2"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("EventType", "ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)"),
		}),
		new TableMetaInfo("PropertyMap", new List<FieldMetaInfo> {
			new FieldMetaInfo("Parent", "ComputTableIndexByte(TableType::TYPEDEF)"),
			new FieldMetaInfo("PropertyList", "ComputTableIndexByte(TableType::PROPERTY)"),
		}),
		new TableMetaInfo("PropertyPtr", new List<FieldMetaInfo> {
			new FieldMetaInfo("Property", "ComputTableIndexByte(TableType::PROPERTY)"),
		}),
		new TableMetaInfo("Property", new List<FieldMetaInfo> {
			new FieldMetaInfo("PropFlags", "flags", "2"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Type", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("MethodSemantics", new List<FieldMetaInfo> {
			new FieldMetaInfo("Semantic", "semantics", "2"),
			new FieldMetaInfo("Method", "ComputTableIndexByte(TableType::METHOD)"),
			new FieldMetaInfo("Association", "ComputTableIndexByte(TableType::EVENT, TableType::PROPERTY, TagBits::HasSemantics)"),
		}),
		new TableMetaInfo("MethodImpl", new List<FieldMetaInfo> {
			new FieldMetaInfo("Class", "classIdx", "ComputTableIndexByte(TableType::TYPEDEF)"),
			new FieldMetaInfo("MethodBody", "ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::MethodDefOrRef)"),
			new FieldMetaInfo("MethodDeclaration", "ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::MethodDefOrRef)"),
		}),
		new TableMetaInfo("ModuleRef", new List<FieldMetaInfo> {
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
		}),
		new TableMetaInfo("TypeSpec", new List<FieldMetaInfo> {
			new FieldMetaInfo("Signature", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("ImplMap", new List<FieldMetaInfo> {
			new FieldMetaInfo("MappingFlags", "2"),
			new FieldMetaInfo("MemberForwarded", "ComputTableIndexByte(TableType::FIELD, TableType::METHOD, TagBits::MemberForwarded)"),
			new FieldMetaInfo("ImportName", "ComputStringIndexByte()"),
			new FieldMetaInfo("ImportScope", "ComputTableIndexByte(TableType::MODULEREF)"),
		}),
		new TableMetaInfo("FieldRVA", new List<FieldMetaInfo> {
			new FieldMetaInfo("RVA", "rva", "4"),
			new FieldMetaInfo("Field", "ComputTableIndexByte(TableType::FIELD)"),
		}),
		new TableMetaInfo("ENCLog","EncLog", "ENCLOG", new List<FieldMetaInfo> {
			new FieldMetaInfo("Token", "4"),
			new FieldMetaInfo("FuncCode", "4"),
		}),
		new TableMetaInfo("ENCMap", "EncMap", "ENCMAP", new List<FieldMetaInfo> {
			new FieldMetaInfo("Token", "4"),
		}),
		new TableMetaInfo("Assembly", new List<FieldMetaInfo> {
			new FieldMetaInfo("HashAlgId", "4"),
			new FieldMetaInfo("MajorVersion", "2"),
			new FieldMetaInfo("MinorVersion", "2"),
			new FieldMetaInfo("BuildNumber", "2"),
			new FieldMetaInfo("RevisionNumber", "2"),
			new FieldMetaInfo("Flags", "4"),
			new FieldMetaInfo("PublicKey", "ComputBlobIndexByte()"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Locale", "ComputStringIndexByte()"),
		}),
		new TableMetaInfo("AssemblyProcessor", new List<FieldMetaInfo> {
			new FieldMetaInfo("Processor", "4"),
		}),
		new TableMetaInfo("AssemblyOS", new List<FieldMetaInfo> {
			new FieldMetaInfo("OSPlatformId", "osPlatformId", "4"),
			new FieldMetaInfo("OSMajorVersion", "osMajorVersion", "4"),
			new FieldMetaInfo("OSMinorVersion", "osMinorVersion", "4"),
		}),
		new TableMetaInfo("AssemblyRef", new List<FieldMetaInfo> {
			new FieldMetaInfo("MajorVersion", "2"),
			new FieldMetaInfo("MinorVersion", "2"),
			new FieldMetaInfo("BuildNumber", "2"),
			new FieldMetaInfo("RevisionNumber", "2"),
			new FieldMetaInfo("Flags", "4"),
			new FieldMetaInfo("PublicKeyOrToken", "ComputBlobIndexByte()"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Locale", "ComputStringIndexByte()"),
			new FieldMetaInfo("HashValue", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("AssemblyRefProcessor", new List<FieldMetaInfo> {
			new FieldMetaInfo("AssemblyRef", "4"),
			new FieldMetaInfo("Processor", "ComputTableIndexByte(TableType::ASSEMBLYREF)"),
		}),
		new TableMetaInfo("AssemblyRefOS", new List<FieldMetaInfo> {
			new FieldMetaInfo("OSPlatformId", "osPlatformId", "4"),
			new FieldMetaInfo("OSMajorVersion", "osMajorVersion", "4"),
			new FieldMetaInfo("OSMinorVersion", "osMinorVersion", "4"),
			new FieldMetaInfo("AssemblyRef", "ComputTableIndexByte(TableType::ASSEMBLYREF)"),
		}),
		new TableMetaInfo("File", new List<FieldMetaInfo> {
			new FieldMetaInfo("Flags", "4"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("HashValue", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("ExportedType", new List<FieldMetaInfo> {
			new FieldMetaInfo("Flags", "4"),
			new FieldMetaInfo("TypeDefId", "4"),
			new FieldMetaInfo("TypeName", "ComputStringIndexByte()"),
			new FieldMetaInfo("TypeNamespace", "ComputStringIndexByte()"),
			new FieldMetaInfo("Implementation", "ComputTableIndexByte(TableType::FILE, TableType::EXPORTEDTYPE, TableType::ASSEMBLY, TagBits::Implementation)"),
		}),
		new TableMetaInfo("ManifestResource", new List<FieldMetaInfo> {
			new FieldMetaInfo("Offset", "4"),
			new FieldMetaInfo("Flags", "4"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Implementation", "ComputTableIndexByte(TableType::FILE, TableType::ASSEMBLYREF, TagBits::Implementation)"),
		}),
		new TableMetaInfo("NestedClass", new List<FieldMetaInfo> {
			new FieldMetaInfo("NestedClass", "ComputTableIndexByte(TableType::TYPEDEF)"),
			new FieldMetaInfo("EnclosingClass", "ComputTableIndexByte(TableType::TYPEDEF)"),
		}),
		new TableMetaInfo("GenericParam", new List<FieldMetaInfo> {
			new FieldMetaInfo("Number", "2"),
			new FieldMetaInfo("Flags", "2"),
			new FieldMetaInfo("Owner", "ComputTableIndexByte(TableType::TYPEDEF, TableType::METHOD, TagBits::TypeOrMethodDef)"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
		}),
		new TableMetaInfo("MethodSpec", new List<FieldMetaInfo> {
			new FieldMetaInfo("Method", "ComputTableIndexByte(TableType::METHOD, TableType::MEMBERREF, TagBits::MethodDefOrRef)"),
			new FieldMetaInfo("Instantiation", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("GenericParamConstraint", new List<FieldMetaInfo> {
			new FieldMetaInfo("Owner", "ComputTableIndexByte(TableType::GENERICPARAM)"),
			new FieldMetaInfo("Constraint", "ComputTableIndexByte(TableType::TYPEDEF, TableType::TYPEREF, TableType::TYPESPEC, TagBits::TypeDefOrRef)"),
		}),

		new TableMetaInfo("Document", new List<FieldMetaInfo> {
			new FieldMetaInfo("Name", "ComputBlobIndexByte()"),
			new FieldMetaInfo("HashAlgorithm", "ComputGUIDIndexByte()"),
			new FieldMetaInfo("Hash", "ComputBlobIndexByte()"),
			new FieldMetaInfo("Language", "ComputGUIDIndexByte()"),
		}),
		new TableMetaInfo("MethodDebugInformation", new List<FieldMetaInfo> {
			new FieldMetaInfo("Document", "ComputTableIndexByte(TableType::DOCUMENT)"),
			new FieldMetaInfo("SequencePoints", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("LocalScope", new List<FieldMetaInfo> {
			new FieldMetaInfo("Method", "ComputTableIndexByte(TableType::METHOD)"),
			new FieldMetaInfo("ImportScope", "ComputTableIndexByte(TableType::IMPORTSCOPE)"),
			new FieldMetaInfo("VariableList", "variables", "ComputTableIndexByte(TableType::LOCALVARIABLE)"),
			new FieldMetaInfo("ConstantList", "constants", "ComputTableIndexByte(TableType::LOCALCONSTANT)"),
			new FieldMetaInfo("StartOffset", "4"),
			new FieldMetaInfo("Length", "4"),
		}),
		new TableMetaInfo("LocalVariable", new List<FieldMetaInfo> {
			new FieldMetaInfo("Attributes", "2"),
			new FieldMetaInfo("Index", "2"),
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
		}),
		new TableMetaInfo("LocalConstant", new List<FieldMetaInfo> {
			new FieldMetaInfo("Name", "ComputStringIndexByte()"),
			new FieldMetaInfo("Signature", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("ImportScope", new List<FieldMetaInfo> {
			new FieldMetaInfo("Parent", "ComputTableIndexByte(TableType::IMPORTSCOPE)"),
			new FieldMetaInfo("Imports", "ComputBlobIndexByte()"),
		}),
		new TableMetaInfo("StateMachineMethod", new List<FieldMetaInfo> {
			new FieldMetaInfo("MoveNextMethod", "ComputTableIndexByte(TableType::METHOD)"),
			new FieldMetaInfo("KickoffMethod", "ComputTableIndexByte(TableType::METHOD)"),
		}),
		new TableMetaInfo("CustomDebugInformation", new List<FieldMetaInfo> {
			new FieldMetaInfo("Parent", "ComputTableIndexByte(HasCustomDebugInformation, sizeof(HasCustomDebugInformation) / sizeof(TableType), TagBits::HasCustomDebugInformation)"),
			new FieldMetaInfo("Kind", "ComputGUIDIndexByte()"),
			new FieldMetaInfo("Value", "ComputBlobIndexByte()"),
		}),
	};
}
