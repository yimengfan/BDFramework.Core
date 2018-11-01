using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr;
using LitJson;
using UnityEditor;
using UnityEngine;
using System;

namespace BDFramework.Editor
{
    static public class Excel2Code
    {
        public static void GenCode()
        {
            var tablePath = Path.Combine(Application.dataPath, "Resource/Table");
            var tableDir = Path.GetDirectoryName(tablePath);
            var xlslFiles = Directory.GetFiles(tableDir, "*.xlsx", SearchOption.AllDirectories);

            if (xlslFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未发现xlsx文件，请注意不是xls", "确定");
                return;
            }

            foreach (var f in xlslFiles)
            {
                var excel = new ExcelUtility(f);
                var json = excel.GetJson();
                var statements = excel.GetLine(0);
                Json2Class(f, json, statements);
                Debug.Log("导出：" + f);
            }

            EditorUtility.DisplayDialog("提示", "生成完成!", "确定");
            AssetDatabase.Refresh();
        }


        private static void Json2Class(string fileName, string json, List<object> statements)
        {
            string structName = "";

            structName = Path.GetFileName(fileName).ToLower().Replace(".xlsx", "");

            //首字母大写
            structName = structName.Substring(0, 1).ToUpper() + structName.Substring(1);
            //输出目录控制
            string outputFile = Path.Combine(Application.dataPath,"Code/Game@hotfix/Table");
            if (Directory.Exists(outputFile) == false)
            {
                Directory.CreateDirectory(outputFile);
            }
            //输出目录
            outputFile = Path.Combine(outputFile,  Path.GetFileName(fileName).Replace(".xlsx", ".cs"));


            //生成类服务
            CodeCompileUnit compunit = new CodeCompileUnit();
            CodeNamespace sample = new CodeNamespace("Game.Data");
            compunit.Namespaces.Add(sample);
            //引用命名空间
            sample.Imports.Add(new CodeNamespaceImport("System"));
            sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            sample.Imports.Add(new CodeNamespaceImport("Game.Data"));
            sample.Imports.Add(new CodeNamespaceImport("SQLite4Unity3d"));

            //在命名空间下添加一个类
            CodeTypeDeclaration wrapProxyStruct = new CodeTypeDeclaration(structName);
            wrapProxyStruct.IsClass = false;
            wrapProxyStruct.IsEnum = false;
            wrapProxyStruct.IsInterface = false;
            wrapProxyStruct.IsPartial = false;
            wrapProxyStruct.IsStruct = true;
            //把这个类添加到命名空间 
            sample.Types.Add(wrapProxyStruct);

            //
            var jsonData = JsonMapper.ToObject(json)[0];

            int i = 0;
            foreach (var key in jsonData.Keys)
            {
                //字段

                string memberContent =
@"       public [type] [Name] {get;set;}";
                CodeSnippetTypeMember member = new CodeSnippetTypeMember();
                if (key.ToLower() == "id" && key != "Id")
                {
                    Debug.LogErrorFormat("<color=yellow>表格{0}字段必须为Id[大小写],请修改后生成</color>", structName);
                    break;
                }
                else if (key == "Id")
                {
                    //增加一个sqlite主键
                    //member.CustomAttributes.Add(new CodeAttributeDeclaration("PrimaryKey"));
                    memberContent =
@"      [PrimaryKey] 
        public [type] [Name] {get;set;}";
                }

                var value = jsonData[key];


                string type = null;
                if (value.IsArray)
                {
                    var str = value.ToJson();
                    if (str.IndexOf("\"") > 0)
                    {
                        type = "List<string>";
                    }
                    else
                    {
                        type = "List<double>";
                    }
                }
                else if (value.IsInt) type = "int";
                else if (value.IsDouble || value.IsLong) type = "double";
                else if (value.IsBoolean) type = "bool";
                else if (value.IsString) type = "string";

                //注释
                member.Comments.Add(new CodeCommentStatement(statements[i].ToString()));

                member.Text = memberContent.Replace("[type]", type).Replace("[Name]", key);


                wrapProxyStruct.Members.Add(member);
                i++;
            }

            //生成代码       
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";
            options.BlankLinesBetweenMembers = true;

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile))
            {
                provider.GenerateCodeFromCompileUnit(compunit, sw, options);
            }
        }
    }
}