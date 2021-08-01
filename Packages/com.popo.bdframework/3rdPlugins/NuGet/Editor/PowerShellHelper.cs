using System;
using System.Collections.Generic;
using System.IO;


namespace NugetForUnity
{
    static public class PowerShellHelper
    {
        /// <summary>
        /// 执行Init
        /// </summary>
        static public void ExcuteNugetInit()
        {
        }

        /// <summary>
        /// 执行intall
        /// https://docs.microsoft.com/en-us/nuget/guides/analyzers-conventions
        /// </summary>
        static public string[] ExcuteNugetAnalyzersPackage_Install(string installPath)
        {
            var analyzersDir = installPath + "\\analyzers\\dotnet\\cs";
            if (Directory.Exists(analyzersDir))
            {
                return Directory.GetFiles(analyzersDir, "*.dll", SearchOption.TopDirectoryOnly);
            }

            return new string[] { };
        }

        /// <summary>
        /// 执行unintall
        /// </summary>
        static public void ExcuteNugetUnInstall()
        {
        }
    }
}