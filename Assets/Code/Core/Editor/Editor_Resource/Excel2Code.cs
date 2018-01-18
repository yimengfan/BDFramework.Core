using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BDFramework.ResourceMgr;
using LitJson;
using UnityEditor;
using UnityEngine;

static public class Excel2Code 
{

    public static void GenCode()
    {
        var tablePath = Path.Combine(Application.dataPath, FrameDataHelper.FrameWorkSetting.EditorTablePath);
        var tableDir = Path.GetDirectoryName(tablePath);
        var xlslFiles = Directory.GetFiles(tableDir, "*.xlsx", SearchOption.AllDirectories);

        foreach (var f in xlslFiles )
        {
            var excel =  new ExcelUtility(f);
            var json = excel.GetJson();
            var statements = excel.GetLine(0);
            Json2Class(f, json, statements);
        }

        AssetDatabase.Refresh();
    }


    private static void Json2Class( string fileName,string json , List<object> statements)
    {
        string className = Path.GetFileName(fileName).ToLower().Replace(".xlsx","");
        string outputFile = fileName.Replace(".xlsx", ".cs");
        //生成类服务
        CodeCompileUnit compunit = new CodeCompileUnit();
        CodeNamespace sample = new CodeNamespace("Game.Data");
        compunit.Namespaces.Add(sample);
        //引用命名空间
        sample.Imports.Add(new CodeNamespaceImport("System"));
        sample.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
        //在命名空间下添加一个类
        CodeTypeDeclaration wrapProxyClass = new CodeTypeDeclaration(className);
        //把这个类添加到命名空间 
        sample.Types.Add(wrapProxyClass);

        
        //
        var jsonData = JsonMapper.ToObject(json)[0];

        int i = 0;
        foreach (var key in  jsonData.Keys)
        {
            var value = jsonData[key];
            //添加字段
            CodeMemberField field = new CodeMemberField();
            field.Attributes = MemberAttributes.Public;
            CodeTypeReference type = null;
            if (value.IsArray)
            {
                var str = value.ToString();
                if (str.IndexOf("\"") > 0)
                {
                    type = new CodeTypeReference(typeof(List<string>));
                }
                else
                {
                    type = new CodeTypeReference(typeof(List<double>));
                }
                
            }
            else if (value.IsInt) type = new CodeTypeReference(typeof(int));
            else if (value.IsDouble || value.IsLong) type = new CodeTypeReference(typeof(double));
            else if (value.IsBoolean) type = new CodeTypeReference(typeof(bool));
            else if (value.IsString) type = new CodeTypeReference(typeof(string));
            //屬性
            field.Type = type;
            field.Name = key;
            field.Comments.Add(new CodeCommentStatement(statements[i].ToString()));
            
            wrapProxyClass.Members.Add(field);
            i++;
        }
       
        //生成代码       
        CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
        CodeGeneratorOptions options = new CodeGeneratorOptions();
        options.BracingStyle = "C";
        options.BlankLinesBetweenMembers = true;

        using (System.IO.StreamWriter sw = new System.IO.StreamWriter(outputFile)) {

            provider.GenerateCodeFromCompileUnit(compunit, sw, options);

        }
    }
    
}
