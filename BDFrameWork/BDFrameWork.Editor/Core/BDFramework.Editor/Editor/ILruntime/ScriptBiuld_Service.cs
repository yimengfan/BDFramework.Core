using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System;
using System.IO;
using UnityEngine;
public class ScriptBiuld_Service
{
    public enum BuildStatus
    {
        Success = 0,
        Fail
    }


    /// <summary>
    /// 编译dll
    /// </summary>
    /// <param name="rootpaths"></param>
    /// <param name="outputpath"></param>
   static public BuildStatus BuildDll(string[] rootpaths, string outputpath ,string[] refAssemblies =null)
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
        //输出path
        cp.OutputAssembly = outputpath;
        //warning和 error分开,不然各种warning当成error,改死你
        cp.TreatWarningsAsErrors = false;
        cp.WarningLevel = 0;
        //编译选项
        cp.CompilerOptions = "/optimize /unsafe";
        //依赖dll
        cp.ReferencedAssemblies.Add("System.dll");
        cp.ReferencedAssemblies.Add("System.Core.dll");
        cp.ReferencedAssemblies.Add("System.XML.dll");
        cp.ReferencedAssemblies.Add("System.Data.dll");
        cp.ReferencedAssemblies.Add(Application.dataPath+ "/../Library/UnityAssemblies/UnityEngine.dll");
        cp.ReferencedAssemblies.Add(Application.dataPath + "/../Library/UnityAssemblies/UnityEngine.UI.dll");
        if (refAssemblies != null)
        {
            foreach (var d in refAssemblies)
            {
                cp.ReferencedAssemblies.Add(d);
            }      

        }

        // 编译代理
        CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
        //文件数组
        List<string> filelist = new List<string>();
        foreach (var rootpath in rootpaths)
        {
            if (rootpath == "") continue;
            
            var files = Directory.GetFiles(rootpath, "*.cs", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                //编辑器和插件下的代码不进行编译
                if (files[i].IndexOf("Editor") == -1 && files[i].IndexOf("Resources") == -1
                    && files[i].IndexOf("Plugins") == -1 && files[i].IndexOf("Test") == -1)
                {
                    //codedom只识别 [ \\ ]
                    filelist.Add(files[i].Replace('/', '\\').Trim('\\'));
                }

            }
        }
        var array = filelist.ToArray();
        CompilerResults cr = provider.CompileAssemblyFromFile(cp, array);
        //foreach (var a in array)
        //{
        //     JDeBug.I.Log(string.Format("==>{0}", a));
        //}


        if (true == cr.Errors.HasErrors)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (System.CodeDom.Compiler.CompilerError ce in cr.Errors)
            {
                sb.Append(ce.ToString());
                sb.Append(System.Environment.NewLine);
            }
            Debug.LogError(sb.ToString());

        }
        else
        {
            BDebug.Log(string.Format("编译{0}成功!", outputpath));
            return BuildStatus.Success;
        }


        return BuildStatus.Fail;
    }
}