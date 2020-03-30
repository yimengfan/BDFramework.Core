using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Debug = UnityEngine.Debug;
using Code.BDFramework.Core.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif
public class ScriptBuildTools
{
    public enum BuildMode
    {
        Release,
        Debug,
    }


    private static Dictionary<int, string> csFilesMap;
    private static string                  DLLPATH = "/Hotfix/hotfix.dll";

    /// <summary>
    /// 宏
    /// </summary>
    private static List<string> defineList;

    /// <summary>
    /// 编译DLL
    /// </summary>
    static public void BuildDll(string dataPath, string outPath, BuildMode mode)
    {
        
        EditorUtility.DisplayProgressBar("编译服务", "准备编译环境...", 0.1f);

        //输出环境
        var path = outPath + "/Hotfix";

        //准备输出环境
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("提示", "请手动删除hotfix文件后重试!", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("编译服务", "开始处理脚本", 0.2f);

        #region CS DLL引用搜集处理

        List<string> dllFiles = new List<string>();
        List<string> csFiles  = new List<string>();
        //所有宏
        defineList =new List<string>();
        
        csFiles = FindDLLByCSPROJ("Assembly-CSharp.csproj", ref dllFiles);

        //去重
        defineList.Distinct();
        var baseCs   = csFiles.FindAll(f => !f.Contains("@hotfix") && f.EndsWith(".cs"));
        var hotfixCs = csFiles.FindAll(f => f.Contains("@hotfix")  && f.EndsWith(".cs"));

        #endregion

        var outHotfixPath = outPath + DLLPATH;

        if (mode == BuildMode.Release)
        {
            Build(baseCs, hotfixCs, dllFiles, outHotfixPath);
        }
        else if (mode == BuildMode.Debug)
        {
            Build(baseCs, hotfixCs, dllFiles, outHotfixPath, true);
        }
    }

    /// <summary>
    /// 编译
    /// </summary>
    /// <param name="tempCodePath"></param>
    /// <param name="outBaseDllPath"></param>
    /// <param name="outHotfixDllPath"></param>
    static public void Build(List<string> baseCs, List<string> hotfixCS, List<string> dllFiles, string outHotfixDllPath,
                             bool         isdebug = false)
    {
        var baseDll = outHotfixDllPath.Replace("hotfix.dll", "Assembly-CSharp.dll");
        //开始执行
        EditorUtility.DisplayProgressBar("编译服务", "[1/2]开始编译base.dll...", 0.5f);
        try
        {
            BuildByRoslyn(dllFiles.ToArray(), baseCs.ToArray(), baseDll, false);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            EditorUtility.ClearProgressBar();
            return;
        }

        EditorUtility.DisplayProgressBar("编译服务", "[2/2]开始编译hotfix.dll...", 0.7f);
        //将base.dll加入
        //var mainDll = BApplication.ProjectRoot + "/Library/ScriptAssemblies/Assembly-CSharp.dll";
        var mainDll = baseDll;
        if (!dllFiles.Contains(mainDll))
        {
            dllFiles.Add(mainDll);
        }

        //build
        try
        {
            //这里编译 不能使用宏
            BuildByRoslyn(dllFiles.ToArray(), hotfixCS.ToArray(), outHotfixDllPath, isdebug, false);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            EditorUtility.ClearProgressBar();
            return;
        }

        EditorUtility.DisplayProgressBar("编译服务", "清理临时文件", 0.9f);
        File.Delete(baseDll);
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }


    /// <summary>
    /// 解析project中的dll
    /// </summary>
    /// <returns></returns>
    static List<string> FindDLLByCSPROJ(string projName, ref List<string> dllList)
    {
        if (dllList == null) dllList = new List<string>();
        //cs list
        List<string> csList = new List<string>();

        var         projpath = BApplication.ProjectRoot + "/" + projName;
        XmlDocument xml      = new XmlDocument();
        xml.Load(projpath);
        XmlNode ProjectNode = null;

        foreach (XmlNode x in xml.ChildNodes)
        {
            if (x.Name == "Project")
            {
                ProjectNode = x;
                break;
            }
        }

        List<string> csprojList = new List<string>();
        foreach (XmlNode childNode in ProjectNode.ChildNodes)
        {
            if (childNode.Name == "ItemGroup")
            {
                foreach (XmlNode item in childNode.ChildNodes)
                {
                    if (item.Name == "Compile") //cs 引用
                    {
                        var csproj = item.Attributes[0].Value;
                        csList.Add(csproj);
                    }
                    else if (item.Name == "Reference") //DLL 引用
                    {
                        var HintPath = item.FirstChild;
                        var dir      = HintPath.InnerText.Replace("/", "\\");
                        dllList.Add(dir);
                    }
                    else if (item.Name == "ProjectReference") //工程引用
                    {
                        var csproj = item.Attributes[0].Value;
                        csprojList.Add(csproj);
                    }
                }
            }
            else if (childNode.Name == "PropertyGroup")
            {
                foreach (XmlNode item in childNode.ChildNodes)
                {
                    if (item.Name =="DefineConstants")
                    {
                        var define = item.InnerText;

                        var defines = define.Split(';');
                        
                        defineList.AddRange(defines);
                    }
                    
                }
            }
        }

        //csproj也加入
        foreach (var csproj in csprojList)
        {
            //有editor退出
            if (csproj.ToLower().Contains("editor")) continue;
            //添加扫描到的dll
            FindDLLByCSPROJ(csproj, ref dllList);
            //
            var gendll = BApplication.Library + "/ScriptAssemblies/" + csproj.Replace(".csproj", ".dll");
            if (!File.Exists(gendll))
            {
                Debug.LogError("不存在:" + gendll);
            }

            dllList.Add(gendll);
        }

        //去重
        dllList = dllList.Distinct().ToList();

        return csList;
    }


    /// <summary>
    /// 编译dll
    /// </summary>
    /// <param name="rootpaths"></param>
    /// <param name="output"></param>
    static public bool BuildByRoslyn(string[] dlls, string[] codefiles, string output, bool isdebug = false,bool isUseDefine =false)
    {
        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            for (int i = 0; i < dlls.Length; i++)
            {
                dlls[i] = dlls[i].Replace("\\", "/");
            }

            for (int i = 0; i < codefiles.Length; i++)
            {
                codefiles[i] = codefiles[i].Replace("\\", "/");
            }

            output = output.Replace("\\", "/");
        }

        //添加语法树
        //宏解析
        var                                     Symbols = defineList;
        List<Microsoft.CodeAnalysis.SyntaxTree> codes   = new List<Microsoft.CodeAnalysis.SyntaxTree>();
        CSharpParseOptions opa = null;
        if (isUseDefine)
        {
            opa = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: Symbols);
        }
        else
        {
            opa= new CSharpParseOptions(LanguageVersion.Latest);
        }
        foreach (var cs in codefiles)
        {
            var content    = File.ReadAllText(cs);
            var syntaxTree = CSharpSyntaxTree.ParseText(content, opa, cs, Encoding.UTF8);
            codes.Add(syntaxTree);
        }

        //添加dll
        List<MetadataReference> assemblies = new List<MetadataReference>();
        foreach (var dll in dlls)
        {
            var metaref = MetadataReference.CreateFromFile(dll);
            if (metaref != null)
            {
                assemblies.Add(metaref);
            }
        }

        //创建目录
        var dir = Path.GetDirectoryName(output);
        Directory.CreateDirectory(dir);
        //编译参数
        CSharpCompilationOptions option = null;
        if (isdebug)
        {
            option = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                  optimizationLevel: OptimizationLevel.Debug, warningLevel: 4,
                                                  allowUnsafe: true);
        }
        else
        {
            option = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                  optimizationLevel: OptimizationLevel.Release, warningLevel: 4,
                                                  allowUnsafe: true);
        }

        //创建编译器代理
        var        assemblyname = Path.GetFileNameWithoutExtension(output);
        var        compilation  = CSharpCompilation.Create(assemblyname, codes, assemblies, option);
        EmitResult result       = null;
        if (!isdebug)
        {
            result = compilation.Emit(output);
        }
        else
        {
            var pdbPath = output + ".pdb";
            var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb,
                                              pdbFilePath: pdbPath);
            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                result = compilation.Emit(dllStream, pdbStream, options: emitOptions);

                File.WriteAllBytes(output, dllStream.GetBuffer());
                File.WriteAllBytes(pdbPath, pdbStream.GetBuffer());
            }
        }

        // 编译失败，提示
        if (!result.Success)
        {
            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                                                                            diagnostic.IsWarningAsError ||
                                                                            diagnostic.Severity ==
                                                                            DiagnosticSeverity.Error);

            foreach (var diagnostic in failures)
            {
                Debug.LogError(diagnostic.ToString());
            }
        }

        return result.Success;
    }
}