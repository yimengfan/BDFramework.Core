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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet.PolymorphicWriter;
using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Template;

public class PolymorphicCodeGenerator
{
    public class Options
    {
        public string GenerationSecretKey { get; set; }

        public string Libil2cppDir { get; set; }

        public string TemplateDir { get; set; }

        public bool DisableLoadStandardDll { get; set; } = true;
    }

	private readonly string _libil2cppDir;
	private readonly string _metadataDir;
    private readonly string _templateDir;

    private readonly string _generationSecretKey;
    private readonly bool _disableLoadStandardImage;

    private readonly PolymorphicMetadataWriter writer;

	public PolymorphicCodeGenerator(Options options)
	{
		_libil2cppDir = options.Libil2cppDir;
		_metadataDir = Path.Combine(_libil2cppDir, "hybridclr", "metadata");
        _templateDir = options.TemplateDir;

        _generationSecretKey = options.GenerationSecretKey;
        _disableLoadStandardImage = options.DisableLoadStandardDll;

        writer = new PolymorphicMetadataWriter(_generationSecretKey);
    }

    private void CopyMetadataReaderHeader()
    {
        string srcFile = $"{_templateDir}/MetadataReader.h.tpl";
        string dstFile = $"{_metadataDir}/MetadataReader.h";
        File.Copy(srcFile, dstFile, true);
        UnityEngine.Debug.Log($"Copy MetadataReader header from {srcFile} to {dstFile}");
    }

	private void GeneratePolymorphicDefs()
    {
        string tplFile = $"{_templateDir}/PolymorphicDefs.h.tpl";
        var frr = new FileRegionReplace(File.ReadAllText(tplFile, Encoding.UTF8));
        var lines = new List<string>();
        lines.Add($"#define POLYMORPHIC_IMAGE_SIGNATURE \"{writer.ImageSignature}\"");
        lines.Add($"\tconstexpr uint32_t kPolymorphicImageVersion = {writer.FormatVersion};");
        lines.Add($"\tconstexpr uint32_t kFormatVariantVersion = {writer.FormatVariant};");
        string codes = string.Join("\n", lines);
        frr.Replace("POLYMORPHIC_DEFINES", codes);

        string outputFile = $"{_metadataDir}/PolymorphicDefs.h";
        frr.Commit(outputFile);
    }

	private void GeneratePolymorphicDatas()
    {
        string tplFile = $"{_templateDir}/PolymorphicDatas.h.tpl";
        var frr = new FileRegionReplace(File.ReadAllText(tplFile, Encoding.UTF8));
        List<string> lines = new List<string>();
        var sb = new StringBuilder();
        foreach (var type in writer.GetPolymorphicTypes())
        {
            var polymorphicType = writer.GetPolymorphicClassDef(type);
            lines.Add($"\tstruct {type.Name}");
            lines.Add("\t{");
            foreach (var field in polymorphicType.Fields)
            {
                lines.Add($"\t\t{field.fieldWriter.CppTypeName} {field.name};");
            }

            lines.Add("\t\tvoid Read(MetadataReader& reader)");
            lines.Add("\t\t{");

            foreach (var field in polymorphicType.Fields)
            {
                lines.Add($"\t\t\t{field.fieldWriter.GetMarshalCode(field.name, "reader")};");
            }
            lines.Add("\t\t}");
            lines.Add("\t};");
            lines.Add("");
        }

        string codes = string.Join("\n", lines);
        frr.Replace("POLYMORPHIC_DATA", codes);


        string outputFile = $"{_metadataDir}/PolymorphicDatas.h";
        frr.Commit(outputFile);
    }

	private void GeneratePolymorphicRawImageHeader()
    {
        string tplFile = $"{_templateDir}/PolymorphicRawImage.h.tpl";
        var frr = new FileRegionReplace(File.ReadAllText(tplFile, Encoding.UTF8));


        var tableMetaInfoMap = TableMetaInfos.tableMetaInfos.ToDictionary(t => "Raw" + t.csharpTypeName + "Row");
        List<string> lines = new List<string>();
        foreach (Type rowType in writer.GetPolymorphicTableRowTypes())
        {
            TableMetaInfo table = tableMetaInfoMap[rowType.Name];

            lines.Add($"\t\tvirtual Tb{table.cppTypeName} Read{table.cppTypeName}(uint32_t rawIndex) override;");
        }

        frr.Replace("READ_TABLES_OVERRIDES", string.Join("\n", lines));

        string outputFile = $"{_metadataDir}/PolymorphicRawImage.h";
        frr.Commit(outputFile);
    }

	private void GeneratePolymorphicRawImageSource()
    {
        string tplFile = $"{_templateDir}/PolymorphicRawImage.cpp.tpl";
        var frr = new FileRegionReplace(File.ReadAllText(tplFile, Encoding.UTF8));

        var tableMetaInfoMap = TableMetaInfos.tableMetaInfos.ToDictionary(t => "Raw" + t.csharpTypeName + "Row");
        {
            List<string> lines = new List<string>();

            foreach (Type rowType in writer.GetAllTableRowTypes())
            {
                TableMetaInfo table = tableMetaInfoMap[rowType.Name];
                PolymorphicClassDef polymorphicClassDef = writer.CreateTableRowClassDefForCodeGeneration(rowType);
                lines.Add("\t\t{");
                lines.Add($"\t\t\tauto& table = _tableRowMetas[(int)TableType::{table.cppEnumName}];");
                foreach (var fieldDef in polymorphicClassDef.Fields)
                {
                    FieldMetaInfo field = table.fields.First(f => f.csharpName == fieldDef.name);
                    lines.Add($"\t\t\ttable.push_back({{{field.cppRowSize}}});");
                }
                lines.Add("\t\t}");
            }
            string codes = string.Join("\n", lines);
            frr.Replace("TABLE_ROW_METADS", codes);
        }
        {
            List<string> lines = new List<string>();
            foreach (Type rowType in writer.GetPolymorphicTableRowTypes())
            {
                TableMetaInfo table = tableMetaInfoMap[rowType.Name];
                PolymorphicClassDef polymorphicClassDef = writer.CreateTableRowClassDefForCodeGeneration(rowType);

                lines.Add($"\tTb{table.cppTypeName} PolymorphicRawImage::Read{table.cppTypeName}(uint32_t rawIndex)");
                lines.Add("\t{");
                lines.Add($"\t\tIL2CPP_ASSERT(rawIndex > 0 && rawIndex <= GetTable(TableType::{table.cppEnumName}).rowNum);");
                lines.Add($"\t\tconst byte* rowPtr = GetTableRowPtr(TableType::{table.cppEnumName}, rawIndex);");
                lines.Add($"\t\tauto& rowSchema = GetRowSchema(TableType::{table.cppEnumName});");
                lines.Add($"\t\tTb{table.cppTypeName} data;");
                for (int i = 0; i < polymorphicClassDef.Fields.Count; i++)
                {
                    var fieldDef = polymorphicClassDef.Fields[i];
                    FieldMetaInfo field = table.fields.First(f => f.csharpName == fieldDef.name);
                    lines.Add($"\t\tdata.{field.cppName} = ReadColumn(rowPtr, rowSchema[{i}]);");
                }
                lines.Add("\t\treturn data;");
                lines.Add("\t}");
            }

            frr.Replace("READ_TABLES_IMPLEMENTATIONS", string.Join("\n", lines));
        }
        string outputFile = $"{_metadataDir}/PolymorphicRawImage.cpp";
        frr.Commit(outputFile);
    }

    private void GenerateRawImageInit()
    {
        string tplFile = $"{_metadataDir}/Image.cpp";
        var frr = new FileRegionReplace(File.ReadAllText(tplFile, Encoding.UTF8));

        {
            List<string> lines = new List<string>();
            lines.Add(@"#include ""PolymorphicRawImage.h""");

            frr.Replace("INCLUDE_RAW_IMAGE_HEADERS", string.Join("\n", lines));
        }
        {
            List<string> lines = new List<string>();

            lines.Add("\t\tif (std::strncmp((const char*)imageData, \"CODEPHPY\", 8) == 0)");
            lines.Add("\t\t{");
            lines.Add("\t\t\t_rawImage = new PolymorphicRawImage();");
            lines.Add("\t\t}");
            lines.Add("\t\telse");
            lines.Add("\t\t{");
            if (_disableLoadStandardImage)
            {
                lines.Add("\t\t\treturn LoadImageErrorCode::UNKNOWN_IMAGE_FORMAT;");
            }
            else
            {
                lines.Add("\t\t\t_rawImage = new RawImage();");
            }
            lines.Add("\t\t}");
            lines.Add("\t\treturn LoadImageErrorCode::OK;");

            frr.Replace("INIT_RAW_IMAGE", string.Join("\n", lines));
        }


        frr.Commit(tplFile);
    }

    public void Generate()
    {
        var installerController = new InstallerController();
        if (installerController.PackageVersion.CompareTo("8.4.0") < 0)
        {
            throw new Exception("Polymorphic code generation requires com.code-philosophy.hybridclr package version 8.4.0 or higher.");
        }

        CopyMetadataReaderHeader();
        GeneratePolymorphicDefs();
        GeneratePolymorphicDatas();
        GeneratePolymorphicRawImageHeader();
        GeneratePolymorphicRawImageSource();
        GenerateRawImageInit();
    }
}
