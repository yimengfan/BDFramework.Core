// using System.Collections.Generic;
// using System;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Xml;
// using BDFramework;
// using BDFramework.Asset;
// using Debug = UnityEngine.Debug;
// using BDFramework.Core.Tools;
// using BDFramework.Editor;
// using BDFramework.Editor.Unity3dEx;
// using BDFramework.StringEx;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.Emit;
// using Unity.CodeEditor;
// #if UNITY_EDITOR
// using UnityEngine;
// using UnityEditor;
//
// #endif
//
// namespace BDFramework.Editor.HotfixScript
// {
//     /// <summary>
//     /// 脚本构建工具
//     /// </summary>
//     public class Unity3dRoslynBuildTools
//     {
//         public enum BuildMode
//         {
//             Release,
//             Debug,
//         }
//
//
//         private static Dictionary<int, string> csFilesMap;
//
//         /// <summary>
//         /// DLL path
//         /// </summary>
//         private static string HOTFIX_DLLPATH { get; set; } =  "Hotfix/hotfix.dll";
//
//         /// <summary>
//         /// 热更标记
//         /// 主工程不会存在，只存在热更dll中
//         /// </summary>
//         readonly static string HOTFIX_TAG = "@hotfix";
//
//         /// <summary>
//         /// 半热更标记
//         /// 存在主工程，且热更dll也会存在，此时执行热更内部访问，会优先访问热更dll域中的.
//         /// </summary>
//         readonly static string HALF_HOTFIX_TAG = "@half_hotfix";
//
//         /// <summary>
//         /// 显示tips
//         /// </summary>
//         private static bool IsShowTips;
//
//         /// <summary>
//         /// 全局宏
//         /// </summary>
//         private static List<string> GlobalSymbols;
//
//         /// <summary>
//         /// playerSetting宏
//         /// </summary>
//         private static List<string> PlayerSettingSymbols;
//
//         //收集hotfix的csproj,这里先后顺序，最优先无依赖的，最后 Assembly-CSharp
//         static string[] collectHotfixFileCsprojList = new string[] { "BDFramework.Core.csproj", "Assembly-CSharp.csproj" };
//
//         /// <summary>
//         ///  判断是否为热更脚本
//         /// </summary>
//         /// <param name="path"></param>
//         /// <returns></returns>
//         static public bool IsHotfixScript(string path)
//         {
//             if (path.EndsWith(".cs") //判断是否为cs
//                 && (path.Contains(HOTFIX_TAG, StringComparison.OrdinalIgnoreCase) || path.Contains(HALF_HOTFIX_TAG, StringComparison.OrdinalIgnoreCase))) //判断是否为热更
//             {
//                 return true;
//             }
//
//             return false;
//         }
//
//         /// <summary>
//         /// 编译DLL
//         /// </summary>
//         static public void BuildDll(string outPath, RuntimePlatform platform, BuildMode mode, bool isShowTips = true)
//         {
//             IsShowTips = isShowTips;
//
//             if (IsShowTips)
//             {
//                 EditorUtility.DisplayProgressBar("编译服务", "准备编译环境...", 0.1f);
//             }
//
//             try
//             {
//                 //生成CSProj
//                 EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
//             }
//             catch
//             {
//             }
//
//
//             //准备输出环境
//             var _outPath = Path.Combine(outPath, BApplication.GetPlatformPath(platform));
//             try
//             {
//                 var path = IPath.Combine(_outPath, ScriptLoder.HOTFIX_DLL_PATH);
//                 //删除旧的dll，按文件删除，防止勿删aot_patch
//                 if (Directory.Exists(path))
//                 {
//                     var fs = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
//                     foreach (var f in fs)
//                     {
//                         File.Delete(f);
//                     }
//                 }
//
//                 Directory.CreateDirectory(path);
//             }
//             catch (Exception e)
//             {
//                 if (IsShowTips)
//                 {
//                     EditorUtility.ClearProgressBar();
//                     EditorUtility.DisplayDialog("提示", "请手动删除hotfix文件后重试!", "OK");
//                 }
//                 throw e;
//             }
//
//             if (IsShowTips)
//             {
//                 EditorUtility.DisplayProgressBar("编译服务", "开始搜集热更cs", 0.2f);
//             }
//
//             #region CS DLL引用搜集处理
//
//             List<string> dllFileList = new List<string>();
//             Dictionary<string, Tuple<List<string>, List<string>>> buildCodeMap = new Dictionary<string, Tuple<List<string>, List<string>>>();
//             //所有宏
//             GlobalSymbols = new List<string>();
//             foreach (var csproj in collectHotfixFileCsprojList)
//             {
//                 var csprojPath = Path.Combine(BApplication.ProjectRoot, csproj);
//                 if (!File.Exists(csprojPath))
//                 {
//                     EditorUtility.DisplayDialog("警告", $"请保证csproj存在:\n {csproj}.\n 请在Preferces/ExternalTools 选择 Generate.csproj文件", "OK");
//                     return;
//                 }
//
//                 var (csarray, dllarray) = ParseCsprojFile(csprojPath);
//                 buildCodeMap[csproj] = new Tuple<List<string>, List<string>>(new List<string>(csarray), new List<string>(dllarray));
//                 dllFileList.AddRange(dllarray);
//             }
//
//             //去重
//             dllFileList = dllFileList.Distinct().ToList();
//             GlobalSymbols = GlobalSymbols.Distinct().ToList();
//             //移除参与收集hotfix csproj的dll,因为已经解析 包含在cs
//             foreach (var csproj in collectHotfixFileCsprojList)
//             {
//                 var dll = csproj.Replace(".csproj", ".dll");
//
//                 var idx = dllFileList.FindIndex((d) => d.EndsWith(dll, StringComparison.OrdinalIgnoreCase));
//                 if (idx >= 0)
//                 {
//                     dllFileList.RemoveAt(idx);
//                     Debug.Log("[Build DLL]剔除收集热更csproj，等待重新编译:" + dll);
//                 }
//             }
//
//
//             #region 宏解析
//
//             //移除editor相关宏
//             for (int i = GlobalSymbols.Count - 1; i >= 0; i--)
//             {
//                 var symbol = GlobalSymbols[i];
//                 if (symbol.Contains("UNITY_EDITOR"))
//                 {
//                     GlobalSymbols.RemoveAt(i);
//                 }
//             }
//
//             //增加buildtarget 宏
//             var custom_symbol = PlayerSettings.GetScriptingDefineSymbolsForGroup(BApplication.GetBuildTargetGroup(platform));
//             var symbols = custom_symbol.Split(';');
//             PlayerSettingSymbols = new List<string>(symbols);
//             GlobalSymbols = GlobalSymbols.Distinct().ToList();
//             //剔除不存的dll
//             if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor)
//             {
//                 for (int i = dllFileList.Count - 1; i >= 0; i--)
//                 {
//                     var dll = dllFileList[i];
//                     if (!File.Exists(dll))
//                     {
//                         dllFileList.RemoveAt(i);
//                         Debug.Log("剔除:" + dll);
//                     }
//                 }
//             }
//
//             #endregion
//
//             #endregion
//
//             //不用ILR binding进行编译base.dll,因为binding本身会因为@hotfix调整容易报错
//             // baseCs = baseCs.Where((cs) => (!cs.Contains("\\ILRuntime\\Binding\\Analysis\\") && !cs.Contains("/ILRuntime/Binding/Analysis/")) || cs.EndsWith("CLRBindings.cs")).ToList();
//             //遍历筛选hotfixCs
//             List<string> hotfixCs = new List<string>();
//             foreach (var item in buildCodeMap)
//             {
//                 for (int i = item.Value.Item1.Count - 1; i >= 0; i--)
//                 {
//                     var f = item.Value.Item1[i];
//                     if (f.Contains(HOTFIX_TAG) || f.Contains(HALF_HOTFIX_TAG))
//                     {
//                         hotfixCs.Add(f);
//                         item.Value.Item1.RemoveAt(i);
//                     }
//                 }
//             }
//
//             //
//
//             if (mode == BuildMode.Release)
//             {
//                 Build(buildCodeMap, hotfixCs, dllFileList, _outPath);
//             }
//             else if (mode == BuildMode.Debug)
//             {
//                 Build(buildCodeMap, hotfixCs, dllFileList, _outPath, true);
//             }
//
//             var version = BDFrameworkPipelineHelper.GetScriptSVCNum(platform,outPath);
//             ClientAssetsHelper.GenBasePackageBuildInfo(outPath, platform, hotfixScriptSVC: version);
//         }
//
//         /// <summary>
//         /// 编译
//         /// </summary>
//         /// <param name="tempCodePath"></param>
//         /// <param name="outBaseDllPath"></param>
//         /// <param name="outputPath"></param>
//         static private void Build(Dictionary<string, Tuple<List<string>, List<string>>> buildCodeMap, List<string> hotfixCs, List<string> hofixCsDependDllFiles, string outputPath, bool isdebug = false)
//         {
//             //开始执行
//             List<string> genTempDllList = new List<string>();
//             hofixCsDependDllFiles = new List<string>();
//             foreach (var item in buildCodeMap)
//             {
//                 var outdll = IPath.Combine(outputPath, item.Key.Replace(".csproj", ".dll"));
//                 outdll = IPath.ReplaceBackSlash(outdll);
//                 Debug.Log($"临时生成:{outdll}");
//                 if (IsShowTips)
//                 {
//                     EditorUtility.DisplayProgressBar("编译服务", $"[1/2]校验:{Path.GetFileName(outdll)}", 0.5f);
//                 }
//
//                 //
//                 try
//                 {
//                     //使用宏编译
//                     BuildByRoslyn(item.Value.Item1.ToArray(), item.Value.Item2.ToArray(), outdll, false, true);
//                     hofixCsDependDllFiles.AddRange(item.Value.Item2);
//                 }
//                 catch (Exception e)
//                 {
//                     Debug.LogError(e.Message);
//                     EditorUtility.ClearProgressBar();
//                     return;
//                 }
//
//                 genTempDllList.Add(outdll);
//             }
//
//
//             hofixCsDependDllFiles.AddRange(genTempDllList);
//             hofixCsDependDllFiles = hofixCsDependDllFiles.Distinct().ToList();
//             // var libAssembly = BApplication.Library + "/ScriptAssemblies";
//             // var dlls = Directory.GetFiles(libAssembly, "*.dll").Where((d) => !d.Contains("editor", StringComparison.OrdinalIgnoreCase)).Select((d) => IPath.ReplaceBackSlash(d));
//             //
//             // dllFiles.AddRange(dlls);
//             // dllFiles = dllFiles.Distinct().ToList();
//
//             if (IsShowTips)
//             {
//                 EditorUtility.DisplayProgressBar("编译服务", "[2/2]开始编译hotfix.dll...", 0.7f);
//             }
//
//             try
//             {
//                 var hotfixDll = Path.Combine(outputPath, HOTFIX_DLLPATH);
//                 //这里编译 不能使用宏
//                 BuildByRoslyn(hotfixCs.ToArray(), hofixCsDependDllFiles.ToArray(), hotfixDll, isdebug, false);
//                 //检测输出的dll是否正确
//                 var result = hotfixCs.FirstOrDefault((t) => t.Contains("UIManager"));
//                 if (result == null)
//                 {
//                     Debug.LogError("打包hotfix出错,请检查是否完整收集hotfix相关脚本!  如开启了生成player .csproj等，或删除所有csproj,重启unity重新尝试打包.");
//                 }
//             }
//             catch (Exception e)
//             {
//                
//                 Debug.LogError(e.Message);
//                 EditorUtility.ClearProgressBar();
//                 throw e;
//             }
//
//             if (IsShowTips)
//             {
//                 EditorUtility.DisplayProgressBar("编译服务", "清理临时文件", 0.9f);
//             }
//
//             //删除临时文件
//             foreach (var temp in genTempDllList)
//             {
//                 File.Delete(temp);
//                 Debug.Log($"删除:{temp}");
//             }
//
//             if (IsShowTips)
//             {
//                 EditorUtility.ClearProgressBar();
//             }
//
//             AssetDatabase.Refresh();
//         }
//
//
//         /// <summary>
//         /// 解析project
//         /// 获取里面的dll和cs
//         /// </summary>
//         /// <returns></returns>
//         static (string[], string[]) ParseCsprojFile(string projpath, List<string> blackCspList = null)
//         {
//             List<string> csprojList = new List<string>();
//             List<string> retCsList = new List<string>();
//             List<string> retDllList = new List<string>();
//
//             #region 解析xml
//
//             XmlDocument xml = new XmlDocument();
//             xml.Load(projpath);
//             XmlNode ProjectNode = null;
//             foreach (XmlNode x in xml.ChildNodes)
//             {
//                 if (x.Name == "Project")
//                 {
//                     ProjectNode = x;
//                     break;
//                 }
//             }
//
//             foreach (XmlNode childNode in ProjectNode.ChildNodes)
//             {
//                 if (childNode.Name == "ItemGroup")
//                 {
//                     foreach (XmlNode item in childNode.ChildNodes)
//                     {
//                         if (item.Name == "Compile") //cs 引用
//                         {
//                             var csproj = item.Attributes[0].Value;
//                             retCsList.Add(csproj);
//                         }
//                         else if (item.Name == "Reference") //DLL 引用
//                         {
//                             var HintPath = item.FirstChild;
//                             //var dir = HintPath.InnerText.Replace("/", "\\");
//                             retDllList.Add(HintPath.InnerText);
//                         }
//                         else if (item.Name == "ProjectReference") //工程引用
//                         {
//                             var csproj = item.Attributes[0].Value;
//                             csprojList.Add(csproj);
//                         }
//                     }
//                 }
//                 else if (childNode.Name == "PropertyGroup")
//                 {
//                     foreach (XmlNode item in childNode.ChildNodes)
//                     {
//                         if (item.Name == "DefineConstants")
//                         {
//                             var define = item.InnerText;
//
//                             var defines = define.Split(';');
//
//                             GlobalSymbols.AddRange(defines);
//                         }
//                     }
//                 }
//             }
//
//             #endregion
//
//             //csproj也加入
//             foreach (var csproj in csprojList)
//             {
//                 //有editor退出
//                 if (csproj.ToLower().Contains("editor") || (blackCspList != null && blackCspList.Contains(csproj)))
//                 {
//                     continue;
//                 }
//
//                 //
//                 var gendll = BApplication.Library + "/ScriptAssemblies/" + csproj.Replace(".csproj", ".dll");
//                 if (!File.Exists(gendll))
//                 {
//                     Debug.LogError("不存在:" + gendll);
//                 }
//
//                 retDllList?.Add(gendll);
//             }
//
//             return (retCsList.Where((cs) => cs.EndsWith(".cs")).ToArray(), retDllList.ToArray());
//         }
//
//
//         /// <summary>
//         /// 编译dll
//         /// </summary>
//         /// <param name="codefiles"></param>
//         /// <param name="dllfiles"></param>
//         /// <param name="outputdll"></param>
//         /// <param name="isdebug"></param>
//         /// <param name="isUseDefine"></param>
//         /// <param name="rootpaths"></param>
//         static public bool BuildByRoslyn(string[] codefiles, string[] dllfiles, string outputdll, bool isdebug = false, bool isUseDefine = false)
//         {
//             for (int i = 0; i < dllfiles.Length; i++)
//             {
//                 dllfiles[i] = IPath.ReplaceBackSlash(dllfiles[i]);
//             }
//
//             for (int i = 0; i < codefiles.Length; i++)
//             {
//                 codefiles[i] = IPath.ReplaceBackSlash(codefiles[i]);
//             }
//
//             outputdll = IPath.ReplaceBackSlash(outputdll);
//
//
//             //添加语法树
//             List<Microsoft.CodeAnalysis.SyntaxTree> codes = new List<Microsoft.CodeAnalysis.SyntaxTree>();
//             CSharpParseOptions opa = null;
//             if (isUseDefine)
//             {
//                 opa = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: GlobalSymbols);
//             }
//             else
//             {
//                 //只使用playersetting宏
//                 opa = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: PlayerSettingSymbols);
//             }
//
//             foreach (var cs in codefiles)
//             {
//                 //判断文件是否存在
//                 if (!File.Exists(cs))
//                     continue;
//                 //
//                 var content = File.ReadAllText(cs);
//                 var syntaxTree = CSharpSyntaxTree.ParseText(content, opa, cs, Encoding.UTF8);
//                 codes.Add(syntaxTree);
//             }
//
//             //添加dll
//             List<MetadataReference> assemblies = new List<MetadataReference>();
//             foreach (var dll in dllfiles)
//             {
//                 var metaref = MetadataReference.CreateFromFile(dll);
//                 if (metaref != null)
//                 {
//                     assemblies.Add(metaref);
//                 }
//             }
//
//             //创建目录
//             var dir = Path.GetDirectoryName(outputdll);
//             Directory.CreateDirectory(dir);
//             //编译参数
//             CSharpCompilationOptions option = null;
//             if (isdebug)
//             {
//                 option = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug, warningLevel: 4, allowUnsafe: true);
//             }
//             else
//             {
//                 option = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, warningLevel: 4, allowUnsafe: true);
//             }
//
//             //创建编译器代理
//             var assemblyname = Path.GetFileNameWithoutExtension(outputdll);
//             var compilation = CSharpCompilation.Create(assemblyname, codes, assemblies, option);
//             EmitResult result = null;
//             if (!isdebug)
//             {
//                 result = compilation.Emit(outputdll);
//             }
//             else
//             {
//                 var pdbPath = outputdll + ".pdb";
//                 var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: pdbPath);
//                 using (var dllStream = new MemoryStream())
//                 using (var pdbStream = new MemoryStream())
//                 {
//                     result = compilation.Emit(dllStream, pdbStream, options: emitOptions);
//
//                     FileHelper.WriteAllBytes(outputdll, dllStream.GetBuffer());
//                     FileHelper.WriteAllBytes(pdbPath, pdbStream.GetBuffer());
//                 }
//             }
//
//             // 编译失败，提示
//             if (!result.Success)
//             {
//                 IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
//
//                 foreach (var diagnostic in failures)
//                 {
//                     //System.Diagnostics.Debug.Print(diagnostic.ToString());
//                     var (a, b, c) = ParseRoslynLog(diagnostic.ToString());
//                     if (a != null)
//                     {
//                         DebugUtilImpl.Log(diagnostic.ToString(), a, b, c);
//                     }
//                     else
//                     {
//                         Debug.LogError(diagnostic.ToString());
//                     }
//                 }
//
//                 throw new Exception($"构建{Path.GetFileName(outputdll)}失败!");
//             }
//
//             return result.Success;
//         }
//
//
//         /// <summary>
//         /// 解析roslyn 报错的log
//         /// </summary>
//         /// <param name="log"></param>
//         /// <returns></returns>
//         static (string, int, int) ParseRoslynLog(string log)
//         {
//             var part = @"(?<a>.*)\((?<b>\d+),(?<c>\d+)\)";
//             var mat = Regex.Match(log, part);
//             if (mat.Success)
//             {
//                 var a = mat.Groups["a"].ToString();
//                 var b = mat.Groups["b"].ToString();
//                 var c = mat.Groups["c"].ToString();
//                 return (a, int.Parse(b), int.Parse(c));
//             }
//
//             return (null, -1, -1);
//         }
//     }
// }