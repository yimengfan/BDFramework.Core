using System.Collections.Generic;
using System.CodeDom.Compiler;
using System;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

#endif
public class ScriptBiuld_Service
{
    public enum BuildStatus
    {
        Success = 0,
        Fail
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编译DLL
    /// </summary>
    static public void BuildDLL_Mono(string dataPath, string streamingAssetsPath,
        string u3dDllPath1, string u3dDllPath2)
    {
        //编译项目的base.dll
        EditorUtility.DisplayProgressBar("编译服务", "准备编译dll", 0.1f);

        //准备工作
        var path = streamingAssetsPath + "/hotfix";
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
        

        string[] searchPath = new string[] {"3rdPlugins", "Code", "Plugins", "Resource"};

        for (int i = 0; i < searchPath.Length; i++)
        {
            searchPath[i] = Path.Combine(dataPath, searchPath[i]);
        }

        List<string> files = new List<string>();
        foreach (var s in searchPath)
        {
            var fs = Directory.GetFiles(s, "*.*", SearchOption.AllDirectories).ToList();
            var _fs = fs.FindAll(f =>
            {
                var exten = Path.GetExtension(f);
                if ((!f.Contains("Editor") && !f.Contains("editor")) &&
                    (exten.Equals(".dll") || exten.Equals(".Dll") || exten.Equals(".DLL")
                     || exten.Equals(".cs")))
                {
                    return true;
                }

                return false;
            });

            files.AddRange(_fs);
        }

        files = files.Distinct().ToList();
        for (int i = 0; i < files.Count; i++)
        {
            files[i] = files[i].Replace('/', '\\').Trim('\\');
        }

        EditorUtility.DisplayProgressBar("编译服务", "开始整理script", 0.2f);

        var refDlls = files.FindAll(f => f.EndsWith(".dll"));
        var baseCs = files.FindAll(f => !f.EndsWith(".dll") && !f.Contains("@hotfix"));
        var hotfixCs = files.FindAll(f => !f.EndsWith(".dll") && f.Contains("@hotfix"));


        var tempDirect = "c:/bd_temp";
        if (Directory.Exists(tempDirect))
        {
            Directory.Delete(tempDirect, true);
        }

        Directory.CreateDirectory(tempDirect);

        //除去不需要引用的dll
        for (int i = refDlls.Count - 1; i >= 0; i--)
        {
            var str = refDlls[i];
            if (str.Contains("iOS") || str.Contains("Android"))
            {
                refDlls.RemoveAt(i);
            }
        }

        //去除同名 重复的dll
        for (int i = 0; i < refDlls.Count; i++)
        {
            var copyto = Path.Combine(tempDirect, Path.GetFileName(refDlls[i]));
            File.Copy(refDlls[i], copyto, true);
            refDlls[i] = copyto;
        }

        refDlls.Add("System.dll");
        refDlls.Add("System.Core.dll");
        refDlls.Add("System.XML.dll");
        refDlls.Add("System.Data.dll");

        //dll1
        var u3ddlls1 = Directory.GetFiles(u3dDllPath1, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var d in u3ddlls1)
        {
            refDlls.Add(d);
        }

        //dll2
        var u3ddlls2 = Directory.GetFiles(u3dDllPath2, "*.dll", SearchOption.AllDirectories);
        foreach (var d in u3ddlls2)
        {
            refDlls.Add(d);
        }

        //
        var baseDllPath = streamingAssetsPath + "/hotfix/base.dll";
        EditorUtility.DisplayProgressBar("编译服务", "复制编译代码", 0.3f);

        //为解决mono.exe error: 文件名太长问题
        tempDirect = "c:/bd_temp";
        for (int i = 0; i < baseCs.Count; i++)
        {
            var copyto = Path.Combine(tempDirect, Path.GetFileName(baseCs[i]));
            int count = 1;
            while (File.Exists(copyto))
            {
                copyto = copyto.Replace(".cs", "") + count + ".cs";
                count++;
            }

            File.Copy(baseCs[i], copyto);
            baseCs[i] = copyto;
        }

        //建立目标目录
        var outDirectory = Path.GetDirectoryName(baseDllPath);
        if (Directory.Exists(outDirectory))
        {
            Directory.Delete(outDirectory, true);
        }


        Directory.CreateDirectory(outDirectory);
        EditorUtility.DisplayProgressBar("编译服务", "[1/2]开始编译base.dll", 0.4f);
        //编译 base.dll
        try
        {
            Build(refDlls.ToArray(), baseCs.ToArray(), baseDllPath);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            EditorUtility.ClearProgressBar();
            throw;
        }

        EditorUtility.DisplayProgressBar("编译服务", "[2/2]开始编译hotfix.dll", 0.7f);
        var dependent = outDirectory + "/dependent";
        Directory.CreateDirectory(dependent);

        //将base.dll加入
        refDlls.Add(baseDllPath);
        //编译hotfix.dll
        var outHotfixDirectory = streamingAssetsPath + "/hotfix/hotfix.dll";
        try
        {
            Build(refDlls.ToArray(), hotfixCs.ToArray(), outHotfixDirectory);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            EditorUtility.ClearProgressBar();
            throw;
        }
        //拷贝依赖的dll
//        foreach (var f in refDlls)
//        {
//            if (File.Exists(f) ==false)
//            {
//                continue;
//            }
//            var fn = Path.GetFileName(f);
//            var outpath = Path.Combine(dependent, fn);
//            File.Copy(f,outpath,true);
//        }

        EditorUtility.DisplayProgressBar("编译服务", "清理临时文件", 0.9f);
        Directory.Delete(tempDirect, true);
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("编译服务", "打包成功!\n位于StreamingAssets下", "OK");
    }
#endif

    static public void BuildDLL_DotNet(string dataPath, string streamingAssetsPath,
        string u3dDllPath1, string u3dDllPath2)
    {
        //编译项目的base.dll
        Console.WriteLine("准备编译dll 10%");

        //准备工作
        var path = streamingAssetsPath + "/hotfix";
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
        //
        string[] searchPath = new string[] {"3rdPlugins", "Code", "Plugins", "Resource"};

        for (int i = 0; i < searchPath.Length; i++)
        {
            searchPath[i] = Path.Combine(dataPath, searchPath[i]);
        }

        List<string> files = new List<string>();
        foreach (var s in searchPath)
        {
            var fs = Directory.GetFiles(s, "*.*", SearchOption.AllDirectories).ToList();
            var _fs = fs.FindAll(f =>
            {
                var _f = f.ToLower();
                var exten = Path.GetExtension(_f);
                if ((!_f.Contains("editor")) &&
                    (exten.Equals(".dll") || exten.Equals(".cs")))
                {
                    return true;
                }

                return false;
            });

            files.AddRange(_fs);
        }

        files = files.Distinct().ToList();
        for (int i = 0; i < files.Count; i++)
        {
            files[i] = files[i].Replace('/', '\\').Trim('\\');
        }

        Console.WriteLine("开始整理script 20%");

        var refDlls = files.FindAll(f => f.EndsWith(".dll"));
        var baseCs = files.FindAll(f => !f.EndsWith(".dll") && !f.Contains("@hotfix"));
        var hotfixCs = files.FindAll(f => !f.EndsWith(".dll") && f.Contains("@hotfix"));


        var tempDirect = "c:/bd_temp";
        if (Directory.Exists(tempDirect))
        {
            Directory.Delete(tempDirect, true);
        }

        Directory.CreateDirectory(tempDirect);

        //除去不需要引用的dll
        for (int i = refDlls.Count - 1; i >= 0; i--)
        {
            var str = refDlls[i];
            if (str.Contains("iOS") || str.Contains("Android"))
            {
                refDlls.RemoveAt(i);
            }
        }

        //去除同名 重复的dll
        for (int i = 0; i < refDlls.Count; i++)
        {
            var copyto = Path.Combine(tempDirect, Path.GetFileName(refDlls[i]));
            File.Copy(refDlls[i], copyto, true);
            refDlls[i] = copyto;
        }

        refDlls.Add("System.dll");
        refDlls.Add("System.Core.dll");
        refDlls.Add("System.XML.dll");
        refDlls.Add("System.Data.dll");
        //dll1
        var u3ddlls1 = Directory.GetFiles(u3dDllPath1, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var d in u3ddlls1)
        {
            refDlls.Add(d);
        }

        //dll2
        var u3ddlls2 = Directory.GetFiles(u3dDllPath2, "*.dll", SearchOption.AllDirectories);
        foreach (var d in u3ddlls2)
        {
            refDlls.Add(d);
        }

        //
        var baseDllPath = streamingAssetsPath + "/hotfix/base.dll";


        Console.WriteLine("复制编译代码 30%");

        //为解决mono.exe error: 文件名太长问题
        tempDirect = "c:/bd_temp";
        for (int i = 0; i < baseCs.Count; i++)
        {
            var copyto = Path.Combine(tempDirect, Path.GetFileName(baseCs[i]));
            int count = 1;
            while (File.Exists(copyto))
            {
                copyto = copyto.Replace(".cs", "") + count + ".cs";
                count++;
            }

            File.Copy(baseCs[i], copyto);
            baseCs[i] = copyto;
        }

        //建立目标目录
        var outDirectory = Path.GetDirectoryName(baseDllPath);
        if (Directory.Exists(outDirectory))
        {
            Directory.Delete(outDirectory, true);
        }


        Directory.CreateDirectory(outDirectory);
        Console.WriteLine("[1/2]开始编译base.dll 40%");
        //编译 base.dll
        try
        {
            Build(refDlls.ToArray(), baseCs.ToArray(), baseDllPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Console.WriteLine("[2/2]开始编译hotfix.dll 70%");

        var dependent = outDirectory + "/dependent";
        Directory.CreateDirectory(dependent);

        //将base.dll加入
        refDlls.Add(baseDllPath);
        //编译hotfix.dll
        var outHotfixDirectory = streamingAssetsPath + "/hotfix/hotfix.dll";
        try
        {
            Build(refDlls.ToArray(), hotfixCs.ToArray(), outHotfixDirectory);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Console.WriteLine("清理临时文件 95%");
        Directory.Delete(tempDirect, true);
        Console.WriteLine("编译成功!位于StreamingAssets下! 100%");
        //拷贝依赖的dll
        //        foreach (var f in refDlls)
        //        {
        //            if (File.Exists(f) == false)
        //            {
        //                continue;
        //            }
        //            var fn = Path.GetFileName(f);
        //            var outpath = Path.Combine(dependent, fn);
        //            File.Copy(f, outpath, true);
        //        }
    }

    /// <summary>
    /// 编译dll
    /// </summary>
    /// <param name="rootpaths"></param>
    /// <param name="output"></param>
    static public BuildStatus Build(string[] refAssemblies, string[] codefiles, string output)
    {
        // 设定编译参数,DLL代表需要引入的Assemblies
        CompilerParameters cp = new CompilerParameters();
        //
        cp.GenerateExecutable = false;
        //在内存中生成
        cp.GenerateInMemory = true;
        //生成调试信息
        cp.IncludeDebugInformation = true;
        cp.TempFiles = new TempFileCollection(".", true);
        cp.OutputAssembly = output;
        //warning和 error分开,不然各种warning当成error,改死你
        cp.TreatWarningsAsErrors = false;
        cp.WarningLevel = 1;
        //编译选项
        cp.CompilerOptions = "/optimize /unsafe";

        if (refAssemblies != null)
        {
            foreach (var d in refAssemblies)
            {
                cp.ReferencedAssemblies.Add(d);
            }
        }

        // 编译代理
        CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
        CompilerResults cr = provider.CompileAssemblyFromFile(cp, codefiles);


        if (true == cr.Errors.HasErrors)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (System.CodeDom.Compiler.CompilerError ce in cr.Errors)
            {
                sb.Append(ce.ToString());
                sb.Append(System.Environment.NewLine);
            }

#if !UNITY_EDITOR
            Console.WriteLine(sb);
#else
            Debug.LogError(sb);
#endif
        }
        else
        {
            return BuildStatus.Success;
        }


        return BuildStatus.Fail;
    }
}