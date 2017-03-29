using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System;
using System.IO;
public class ScriptBiuld_Service
{


    /// <summary>
    /// 编译dll
    /// </summary>
    /// <param name="rootpaths"></param>
    /// <param name="outputpath"></param>
    public void BuildDll(string[] rootpaths, string outputpath)
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
        cp.WarningLevel = 4;
        //编译选项
        cp.CompilerOptions = "/optimize /unsafe";
        cp.ReferencedAssemblies.Add("System.dll");
        cp.ReferencedAssemblies.Add("System.Core.dll");
        cp.ReferencedAssemblies.Add("System.XML.dll");
        cp.ReferencedAssemblies.Add("System.Data.dll");

        // 编译代理类，C# CSharp都可以
        CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

        //文件数组
        List<string> filelist = new List<string>();
        foreach (var rootpath in rootpaths)
        {
            var files = Directory.GetFiles(rootpath, "*.cs", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                //编辑器和插件下的代码不进行编译
                if (files[i].IndexOf("Editor") == -1 && files[i].IndexOf("Resources") == -1
                    && files[i].IndexOf("Plugins") == -1 && files[i].IndexOf("Test") == -1)
                {
                    filelist.Add(files[i].Replace("\\", "/"));
                }

            }
        }
        var array = filelist.ToArray();
        CompilerResults cr = provider.CompileAssemblyFromFile(cp, array);
        foreach (var a in array)
        {
            Console.WriteLine(string.Format("==>{0}", a));
        }
        if (true == cr.Errors.HasErrors)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (System.CodeDom.Compiler.CompilerError ce in cr.Errors)
            {
                sb.Append(ce.ToString());
                sb.Append(System.Environment.NewLine);
            }
            Console.WriteLine(sb.ToString());

        }



    }
}