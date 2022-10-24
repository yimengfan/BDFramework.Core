using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using BDFramework;
using BDFramework.Asset;
using Debug = UnityEngine.Debug;
using BDFramework.Core.Tools;
using BDFramework.Editor;
using BDFramework.Editor.Unity3dEx;
using BDFramework.StringEx;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Unity.CodeEditor;
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

    /// <summary>
    /// DLL path
    /// </summary>
    private static string DLLPATH { get; set; } = ScriptLoder.DLL_PATH; // "Hotfix/hotfix.dll";

    /// <summary>
    /// 热更标记
    /// 主工程不会存在，只存在热更dll中
    /// </summary>
    readonly static string STR_HOTFIX = "@hotfix";

    /// <summary>
    /// 半热更标记
    /// 存在主工程，且热更dll也会存在，此时执行热更内部访问，会优先访问热更dll域中的.
    /// </summary>
    readonly static string STR_HALF_HOTFIX = "@half_hotfix";

    private static bool IsShowTips;

    /// <summary>
    /// 宏
    /// </summary>
    private static List<string> defineList;

    /// <summary>
    ///  判断是否为热更脚本
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static public bool IsHotfixScript(string path)
    {
        if (path.EndsWith(".cs") //判断是否为cs
            && (path.Contains(STR_HOTFIX, StringComparison.OrdinalIgnoreCase) || path.Contains(STR_HALF_HOTFIX, StringComparison.OrdinalIgnoreCase))) //判断是否为热更
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 编译DLL
    /// </summary>
    static public void BuildDll(string outPath, RuntimePlatform platform, BuildMode mode, bool isShowTips = true)
    {
        IsShowTips = isShowTips;

        if (IsShowTips)
        {
            EditorUtility.DisplayProgressBar("编译服务", "准备编译环境...", 0.1f);
        }

        try
        {
            //生成CSProj
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }
        catch
        {
        }


        //准备输出环境
        var _outPath = Path.Combine(outPath, BApplication.GetPlatformPath(platform));
        try
        {
            var path = IPath.Combine(_outPath, ScriptLoder.SCRIPT_FOLDER_PATH);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            if (IsShowTips)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("提示", "请手动删除hotfix文件后重试!", "OK");
            }

            return;
        }

        if (IsShowTips)
        {
            EditorUtility.DisplayProgressBar("编译服务", "开始处理脚本", 0.2f);
        }

        #region CS DLL引用搜集处理

        List<string> dllFileList = new List<string>();
        List<string> csFileList = new List<string>();
        //所有宏
        defineList = new List<string>();

        string[] parseCsprojList = new string[] {"Assembly-CSharp.csproj", "BDFramework.Core.csproj"};
        foreach (var csproj in parseCsprojList)
        {
            var path = Path.Combine(BApplication.ProjectRoot, csproj);
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("警告", $"请保证csproj存在:\n {csproj}.\n 请在Preferces/ExternalTools 选择 Generate.csproj文件", "OK");
                return;
            }

            ParseCsprojFile(path, new List<string>() { }, ref csFileList, ref dllFileList);
        }

        //去重
        dllFileList = dllFileList.Distinct().ToList();
        csFileList = csFileList.Distinct().ToList();
        defineList = defineList.Distinct().ToList();

        //移除参与分析csproj的dll,因为已经解析 包含在cs
        foreach (var csproj in parseCsprojList)
        {
            var dll = csproj.Replace(".csproj", ".dll");

            var idx = dllFileList.FindIndex((d) => d.EndsWith(dll, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                dllFileList.RemoveAt(idx);
                //Debug.Log("[Build DLL]剔除:" + dll);
            }
        }

        //宏解析
        //移除editor相关宏
        for (int i = defineList.Count - 1; i >= 0; i--)
        {
            var symbol = defineList[i];
            if (symbol.Contains("UNITY_EDITOR"))
            {
                defineList.RemoveAt(i);
            }
        }

        //剔除不存的dll
        //TODO 这里是File 接口mac下有bug 会判断文件不存在
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
        {
            for (int i = dllFileList.Count - 1; i >= 0; i--)
            {
                var dll = dllFileList[i];
                if (!File.Exists(dll))
                {
                    dllFileList.RemoveAt(i);
                    Debug.Log("剔除:" + dll);
                }
            }
        }

        #endregion


        // 热更代码 = 框架部分@hotfix  +  游戏逻辑部分@hotfix
        var baseCs = csFileList.FindAll(f => !f.Contains(STR_HOTFIX) && f.EndsWith(".cs")); //筛选cs
        //不用ILR binding进行编译base.dll,因为binding本身会因为@hotfix调整容易报错
        baseCs = baseCs.Where((cs) => (!cs.Contains("\\ILRuntime\\Binding\\Analysis\\") && !cs.Contains("/ILRuntime/Binding/Analysis/")) || cs.EndsWith("CLRBindings.cs")).ToList();
        //
        var hotfixCs = csFileList.FindAll(f => (f.Contains(STR_HOTFIX) || f.Contains(STR_HALF_HOTFIX)) && f.EndsWith(".cs"));
        var outHotfixPath = Path.Combine(_outPath, DLLPATH);


        if (mode == BuildMode.Release)
        {
            Build(baseCs, hotfixCs, dllFileList, outHotfixPath);
        }
        else if (mode == BuildMode.Debug)
        {
            Build(baseCs, hotfixCs, dllFileList, outHotfixPath, true);
        }
        var version = BDFrameworkPipelineHelper.GetScriptSVCNum(outPath, platform);
        ClientAssetsHelper.GenBasePackageBuildInfo(outPath, platform,scriptSVC:version);
    }

    /// <summary>
    /// 编译
    /// </summary>
    /// <param name="tempCodePath"></param>
    /// <param name="outBaseDllPath"></param>
    /// <param name="outHotfixDllPath"></param>
    static public void Build(List<string> baseCs, List<string> hotfixCS, List<string> dllFiles, string outHotfixDllPath, bool isdebug = false)
    {
        var baseDll = outHotfixDllPath.Replace("hotfix.dll", "Assembly-CSharp.dll"); //这里早期叫base.dll，后因为mono执行依赖Assembly-CSharp.dll
        //开始执行
        if (IsShowTips)
        {
            EditorUtility.DisplayProgressBar("编译服务", "[1/2]开始编译base.dll...", 0.5f);
        }

        try
        {
            //使用宏编译
            BuildByRoslyn(dllFiles.ToArray(), baseCs.ToArray(), baseDll, false, true);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            EditorUtility.ClearProgressBar();
            return;
        }

        if (IsShowTips)
        {
            EditorUtility.DisplayProgressBar("编译服务", "[2/2]开始编译hotfix.dll...", 0.7f);
        }

        //将base.dll加入
        //var mainDll = BApplication.ProjectRoot + "/Library/ScriptAssemblies/Assembly-CSharp.dll";
        if (!dllFiles.Contains(baseDll))
        {
            dllFiles.Add(baseDll);
        }

        //build
        try
        {
            //这里编译 不能使用宏
            BuildByRoslyn(dllFiles.ToArray(), hotfixCS.ToArray(), outHotfixDllPath, isdebug, false);

            //检测输出的dll是否正确
            var result = hotfixCS.FirstOrDefault((t) => t.Contains("UIManager"));
            if (result == null)
            {
                Debug.LogError("打包hotfix出错,请检查是否完整收集hotfix相关脚本!  如开启了生成player .csproj等，或删除所有csproj,重启unity重新尝试打包.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            EditorUtility.ClearProgressBar();
            return;
        }

        if (IsShowTips)
            EditorUtility.DisplayProgressBar("编译服务", "清理临时文件", 0.9f);
        File.Delete(baseDll);
        if (IsShowTips)
            EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }


    /// <summary>
    /// 解析project
    /// 获取里面的dll和cs
    /// </summary>
    /// <returns></returns>
    static void ParseCsprojFile(string projpath, List<string> blackCspList, ref List<string> csList, ref List<string> dllList)
    {
        List<string> csprojList = new List<string>();

        #region 解析xml

        XmlDocument xml = new XmlDocument();
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
                        var dir = HintPath.InnerText.Replace("/", "\\");
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
                    if (item.Name == "DefineConstants")
                    {
                        var define = item.InnerText;

                        var defines = define.Split(';');

                        defineList.AddRange(defines);
                    }
                }
            }
        }

        #endregion

        //csproj也加入
        foreach (var csproj in csprojList)
        {
            //有editor退出
            if (csproj.ToLower().Contains("editor") || blackCspList.Contains(csproj))
            {
                continue;
            }

            //
            var gendll = BApplication.Library + "/ScriptAssemblies/" + csproj.Replace(".csproj", ".dll");
            if (!File.Exists(gendll))
            {
                Debug.LogError("不存在:" + gendll);
            }

            dllList?.Add(gendll);
        }
    }


    /// <summary>
    /// 编译dll
    /// </summary>
    /// <param name="rootpaths"></param>
    /// <param name="output"></param>
    static public bool BuildByRoslyn(string[] dlls, string[] codefiles, string output, bool isdebug = false, bool isUseDefine = false)
    {
        for (int i = 0; i < dlls.Length; i++)
        {
            dlls[i] = IPath.ReplaceBackSlash(dlls[i]);
        }
        for (int i = 0; i < codefiles.Length; i++)
        {
            codefiles[i] = IPath.ReplaceBackSlash(codefiles[i]); 
        }
        output = IPath.ReplaceBackSlash(output);


        //添加语法树
        var Symbols = defineList;

        List<Microsoft.CodeAnalysis.SyntaxTree> codes = new List<Microsoft.CodeAnalysis.SyntaxTree>();
        CSharpParseOptions opa = null;
        if (isUseDefine)
        {
            opa = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: Symbols);
        }
        else
        {
            opa = new CSharpParseOptions(LanguageVersion.Latest);
        }

        foreach (var cs in codefiles)
        {
            //判断文件是否存在
            if (!File.Exists(cs))
                continue;
            //
            var content = File.ReadAllText(cs);
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
            option = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug, warningLevel: 4, allowUnsafe: true);
        }
        else
        {
            option = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, warningLevel: 4, allowUnsafe: true);
        }

        //创建编译器代理
        var assemblyname = Path.GetFileNameWithoutExtension(output);
        var compilation = CSharpCompilation.Create(assemblyname, codes, assemblies, option);
        EmitResult result = null;
        if (!isdebug)
        {
            result = compilation.Emit(output);
        }
        else
        {
            var pdbPath = output + ".pdb";
            var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: pdbPath);
            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                result = compilation.Emit(dllStream, pdbStream, options: emitOptions);

                FileHelper.WriteAllBytes(output, dllStream.GetBuffer());
                FileHelper.WriteAllBytes(pdbPath, pdbStream.GetBuffer());
            }
        }

        // 编译失败，提示
        if (!result.Success)
        {
            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (var diagnostic in failures)
            {
                //System.Diagnostics.Debug.Print(diagnostic.ToString());
                var (a, b, c) = ParseRoslynLog(diagnostic.ToString());
                if (a != null)
                {
                    DebugUtilImpl.Log(diagnostic.ToString(), a, b, c);
                }
                else
                {
                    Debug.LogError(diagnostic.ToString());
                }
            }
        }

        return result.Success;
    }


    /// <summary>
    /// 解析roslyn 报错的log
    /// </summary>
    /// <param name="log"></param>
    /// <returns></returns>
    static (string, int, int) ParseRoslynLog(string log)
    {
        var part = @"(?<a>.*)\((?<b>\d+),(?<c>\d+)\)";
        var mat = Regex.Match(log, part);
        if (mat.Success)
        {
            var a = mat.Groups["a"].ToString();
            var b = mat.Groups["b"].ToString();
            var c = mat.Groups["c"].ToString();
            return (a, int.Parse(b), int.Parse(c));
        }

        return (null, -1, -1);
    }
}
