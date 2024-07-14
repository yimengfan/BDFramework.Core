using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.InternalAPIEngineBridge
{
    /// <summary>
    /// 包管理扩展
    /// </summary>
    static public class PackageManagerEx
    {
        /// <summary>
        /// 获取依赖的Package
        /// </summary>
        /// <param name="packagePath"></param>
        /// <returns></returns>
        static public void SetBDFramworkOpenUpmEnv()
        {
            string pckName = "package.openupm.com";
            string url = "https://package.openupm.com";
            //scope
            var packageContent = File.ReadAllText("Packages/com.popo.bdframework/package.json");
            var jo = JSONParser.SimpleParse(packageContent);
            var depend = jo["dependencies"];
            var scopes = depend.AsDict().Keys.Where((s) => !s.Contains(".unity.")).ToArray();
            var id = PackageManagerProjectSettings.instance.registries.Count.ToString();
            var reginfo = new RegistryInfo(id, pckName, url, scopes, false);
            //开始reginfo逻辑
            var find = PackageManagerProjectSettings.instance.registries.FirstOrDefault((reg) => reg.name == pckName);
            if (find == null)
            {
                foreach (var scope in scopes)
                {
                    Debug.Log($"<color=yellow>添加OpenUPM包: {scope}</color>" );
                }

                //添加
                PackageManagerProjectSettings.instance.AddRegistry(reginfo);
                UpmRegistryClient.instance.AddRegistry(reginfo.name, reginfo.url, reginfo.scopes);
            }
            else
            {
                var except = scopes.Except(find.scopes).ToArray();
                foreach (var scope in except)
                {
                    Debug.Log($"<color=yellow>添加OpenUPM包: {scope}</color>" );
                }

                //更新
                if (except.Length > 0)
                {
                    PackageManagerProjectSettings.instance.UpdateRegistry(pckName, reginfo);
                    UpmRegistryClient.instance.UpdateRegistry(reginfo.name, reginfo.name, reginfo.url, reginfo.scopes);
                }
            }
        }
    }
}