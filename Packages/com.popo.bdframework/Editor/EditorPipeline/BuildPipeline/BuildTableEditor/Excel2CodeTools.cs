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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BDFramework.Core.Tools;
using BDFramework.Editor.HotfixPipeline;


namespace BDFramework.Editor.Table
{
    /// <summary>
    /// Excel转代码工具
    /// </summary>
    static public class Excel2CodeTools
    {
        //旧目录,会被新目录替代
        private static string OldGameTableCodePath = "Assets/Code/Game@hotfix/Table";

        private static string OldGameResourceTableCodePath = "Assets/Resource_SVN/Table/Code@hotfix";

        //程序目录
        private static string GameTableCodePath = "Assets/Code/Game/Table";

        //策划or美术目录
     //   private static string GameResourceTableCodePath = "Assets/Resource_SVN/Table/Code";

        [MenuItem("BDFrameWork工具箱/3.表格/表格->生成Class[程序目录]", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Table_Table2Class)]
        public static void Gen()
        {
            var ret = EditorUtility.DisplayDialog("提示", @"
Excel格式如下:
1.第一行为备注
2.第二行可以自定义字段类型，如没检测到则自动分析字段类型
3.所有表格字段名必须以Id开始，即第二或第三行首列必须是Id", "OK");
            if (ret)
            {
                if (Directory.Exists(OldGameTableCodePath))
                {
                    Directory.Delete(OldGameTableCodePath, true);
                }

                GenCode(GameTableCodePath);
            }
        }

//         [MenuItem("BDFrameWork工具箱/3.表格/表格->生成Class[策划目录]", false, (int) BDEditorGlobalMenuItemOrderEnum.BuildPackage_Table_Table2Class)]
//         public static void Gen2()
//         {
//             var ret = EditorUtility.DisplayDialog("提示", @"
// Excel格式如下:
// 1.第一行为备注
// 2.第二行可以自定义字段类型，如没检测到则自动分析字段类型
// 3.所有表格字段名必须以Id开始，即第二或第三行首列必须是Id", "OK");
//             if (ret)
//             {
//                 if (Directory.Exists(OldGameResourceTableCodePath))
//                 {
//                     Directory.Delete(OldGameResourceTableCodePath, true);
//                 }
//
//                 GenCode(GameResourceTableCodePath);
//             }
//         }


        /// <summary>
        /// 生成代码
        /// </summary>
        public static void GenCode(string outputPath)
        {
            var xlslFiles = ExcelEditorTools.GetAllExcelFiles();
            //
            if (xlslFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "未发现xlsx文件，请注意不是xls", "确定");
                return;
            }

            //同名文件判断
            var fnlist = xlslFiles.Select((s) => Path.GetFileName(s).ToLower()).ToList();
            foreach (var fn in fnlist)
            {
                var rets = fnlist.FindAll((f) => f == fn);
                if (rets.Count > 1)
                {
                    EditorUtility.DisplayDialog("提示", "Sqlite表名 字段名不区分大小写,请处理重名exel! ->" + fn, "OK");

                    return;
                }
            }

            //导出excel
            foreach (var f in xlslFiles)
            {
                GenClassByExcel(outputPath, f, "Local");
                GenClassByExcel(outputPath, f, "Server");
            }

            EditorUtility.DisplayDialog("提示", "生成完成!", "确定");
            AssetDatabase.Refresh();
        }


        /// <summary>
        /// 通过excel生成class
        /// </summary>
        /// <param name="excelFilePath"></param>
        static private void GenClassByExcel(string outputDirectory, string excelFilePath, string @namespace)
        {
            Debug.LogFormat("[{0}]正在生成：" + excelFilePath, @namespace);
            var excel = new ExcelExchangeTools(excelFilePath);
            int idX = -1;
            int idY = -1;
            List<object> keepFieldList = new List<object>();
            var (json,tableMap) = excel.GetJson(@namespace, ref idX, ref idY, ref keepFieldList);
            if (idX != -1 && idY != -1)
            {
                if (idY < 2)
                {
                    Debug.LogErrorFormat("【生成失败】 {0} ,请检查表头预留3行:备注，类型，字段名!", Path.GetFileName(excelFilePath));
                    return;
                }

                //这里将前三列进行判断
                var statements = excel.GetRowDatas(idY - 2);
                var fieldTypes = excel.GetRowDatas(idY - 1);
                if (idX > 0)
                {
                    statements.RemoveRange(0, idX);
                    fieldTypes.RemoveRange(0, idX);
                    if (keepFieldList.Count > 0)
                    {
                        keepFieldList.RemoveRange(0, idX);
                    }
                }

                if (keepFieldList.Count > 0)
                {
                    for (int i = keepFieldList.Count - 1; i >= 0; i--)
                    {
                        if (!keepFieldList[i].Equals("*"))
                        {
                            statements.RemoveAt(i);
                            fieldTypes.RemoveAt(i);
                        }
                    }
                }

                var clsContent = Json2Class(excelFilePath, json, @namespace, statements, fieldTypes);

                //输出目录控制
                string outputFile = outputDirectory + "/" + @namespace;
                //获取热更config
                var config = HotfixPipelineTools.HotfixFileConfig.GetConfig("excel");
                //判断配置是否热更
                var outputHotfixFile = Path.Combine(outputFile, Path.GetFileName(excelFilePath) + "@hotfix.cs");
                outputFile = Path.Combine(outputFile, Path.GetFileName(excelFilePath) + ".cs");

                //删除旧文件
                if (File.Exists(outputHotfixFile))
                {
                    File.Delete(outputHotfixFile);
                }
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                //写入
                if (config != null && config.IsHotfixFile(excelFilePath))
                {
                    FileHelper.WriteAllText(outputHotfixFile, clsContent);
                    Debug.LogFormat("<color=red> [{0} 成功@hotfix] </color>：{1}", @namespace, excelFilePath);
                }
                else
                {
                    FileHelper.WriteAllText(outputFile, clsContent);
                    Debug.LogFormat("<color=green> [{0} 成功@main] </color>：{1}", @namespace, excelFilePath);
                }


               
            }
            else
            {
                Debug.LogError("不符合规范内容:" + excelFilePath);
            }
        }


        /// <summary>
        /// Json2Class
        /// 自动分析字段
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="json"></param>
        /// <param name="namespace"></param>
        /// <param name="statements"></param>
        /// <param name="fieldTypes"></param>
        /// <returns></returns>
        private static string Json2Class(string fileName, string json, string @namespace, List<object> statements, List<object> fieldTypes)
        {
            string clsName = "";
            clsName = Path.GetFileNameWithoutExtension(fileName);
            //生成类服务
            ExcelClassMould excelClassMould = new ExcelClassMould(clsName);
            var jsonData = JsonMapper.ToObject(json)[0];
            int i = 0;
            foreach (var key in jsonData.Keys)
            {
                //字段
                string attribute = "";
                if (key.ToLower() == "id" && key != "Id")
                {
                    Debug.LogErrorFormat("<color=yellow>表格{0}字段必须为Id[大小写区分],请修改后生成</color>", clsName);
                    break;
                }
                else if (key == "Id")
                {
                    //增加一个sqlite主键
                    attribute = "PrimaryKey";
                }

                string type = null;
                if (fieldTypes != null && fieldTypes.Count >= jsonData.Count)
                {
                    
                    type = fieldTypes[i].ToString();
                    if (type.Contains("List<"))
                    {
                        // 正则表达式匹配 List<T>
                        string pattern = @"list<([^>]+)>";
                        // 替换为 T[]
                        string replacement = "$1[]";
                        // 执行替换，使用 IgnoreCase 选项以忽略大小写
                        type = Regex.Replace(type, pattern, replacement, RegexOptions.IgnoreCase);
                    }
                    
                }

                //添加字段
                excelClassMould.AddField(statements[i].ToString(), attribute, type, key);
                i++;
            }

            //生成代码       
            return excelClassMould.GenClass(@namespace);
        }


        [MenuItem("Assets/Excel导出脚本[程序目录]", true)]
        private static bool SingleExcel2ClassValidation()
        {
            if (Selection.activeObject == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Excel生成脚本[程序目录]")]
        public static void SingleExcel2Class()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            GenClassByExcel(GameTableCodePath, path, "Local");
            GenClassByExcel(GameTableCodePath, path, "Server");
            AssetDatabase.Refresh();
        }

        // [MenuItem("Assets/Excel生成脚本[策划目录]", true)]
        // private static bool SingleExcel2ClassValidation_SVN()
        // {
        //     if (Selection.activeObject == null)
        //     {
        //         return false;
        //     }
        //
        //     string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        //     if (!path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        //     {
        //         return false;
        //     }
        //
        //     return true;
        // }
        //
        // [MenuItem("Assets/Excel导出脚本[策划目录]")]
        // public static void SingleExcel2Class_SVN()
        // {
        //     string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        //     GenClassByExcel(GameResourceTableCodePath, path, "Local");
        //     GenClassByExcel(GameResourceTableCodePath, path, "Server");
        //     AssetDatabase.Refresh();
        // }
    }
    
    
        /// <summary>
    /// Excel转class 模板
    /// </summary>
    public class ExcelClassMould
    {
        public string CalssContent = @"
//------------------------------------------------------------------------------
// <auto-generated>
//    Genera by BDFramework
// </auto-generated>
//------------------------------------------------------------------------------

namespace Game.Data.[NameSpace]
{
    using System;
    using System.Collections.Generic;
    using SQLite4Unity3d;
    
    [Serializable()]
    public class [ClsName]
    {
        [Fields]
    }
}";

        public string FieldContent = @"
        /// <summary>
        /// [Statement]
        /// </summary>
        [Attribute]
        public [Type] [Name] {get;set;}";

        public string FieldContent2 = @"
        /// <summary>
        /// [Statement]
        /// </summary>
        public [Type] [Name] {get;set;}";

        private string clsName = "Noname";

        public ExcelClassMould(string clsName)
        {
            this.clsName = clsName;
        }


        private string fieldContents = "";

        /// <summary>
        /// 添加字段
        /// </summary>
        /// <param name="statement">注释</param>
        /// <param name="attribute">属性</param>
        /// <param name="type">类型</param>
        /// <param name="fieldName">字段名</param>
        public void AddField(string statement, string attribute, string type, string fieldName)
        {
            string tempfield = "";
            if (string.IsNullOrEmpty(attribute))
            {
                tempfield = this.FieldContent2;
            }
            else
            {
                tempfield = this.FieldContent;
            }

            tempfield = tempfield.Replace("[Statement]", statement);
            if (!string.IsNullOrEmpty(attribute))
            {
                tempfield = tempfield.Replace("[Attribute]", $"[{attribute}]");
            }

            tempfield = tempfield.Replace("[Type]", type);
            tempfield = tempfield.Replace("[Name]", fieldName);

            this.fieldContents += tempfield;
        }


        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="filePath"></param>
        public string GenClass(string @namespace)
        {
            this.CalssContent = CalssContent.Replace("[ClsName]", clsName) //
                .Replace("[Fields]", fieldContents);
            this.CalssContent = this.CalssContent.Replace("[NameSpace]", @namespace);
            return this.CalssContent;
        }
    }
}
