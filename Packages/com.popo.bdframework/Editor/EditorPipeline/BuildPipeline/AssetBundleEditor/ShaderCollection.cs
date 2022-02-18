using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using LitJson;

namespace BDFramework.Editor.AssetBundle
{
    public class ShaderCollection : EditorWindow
    {
        private ShaderVariantCollection svc;
        static string toolsSVCpath = "Assets/Resource/Shaders/Tools.shadervariants";
        static ShaderVariantCollection ToolSVC = null;
        static List<string> allShaderNameList = new List<string>();
        /// <summary>
        /// 简单收集
        /// </summary>
        public static void CollectShaderVariant()
        {
            //先搜集所有keyword到工具类SVC
            ToolSVC = new ShaderVariantCollection();
            var shaders = AssetDatabase.FindAssets("t:Shader", new string[] {"Assets", "Packages"}).ToList();
            foreach (var shader in shaders)
            {
                ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant();
                var shaderPath = AssetDatabase.GUIDToAssetPath(shader);
                sv.shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                ToolSVC.Add(sv);
                //
                allShaderNameList.Add(shaderPath);
            }


            //防空
            var dirt = Path.GetDirectoryName(toolsSVCpath);
            if (!Directory.Exists(dirt))
            {
                Directory.CreateDirectory(dirt);
            }

            AssetDatabase.CreateAsset(ToolSVC, toolsSVCpath);


            //搜索所有Mat
            var paths = BDApplication.GetAllRuntimeDirects().ToArray();
            var assets = AssetDatabase.FindAssets("t:Prefab", paths).ToList();
            var assets2 = AssetDatabase.FindAssets("t:Material", paths);
            assets.AddRange(assets2);
            List<string> allMatPaths = new List<string>();

            //GUID to assetPath
            for (int i = 0; i < assets.Count; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(assets[i]);
                //获取依赖中的mat
                var dependenciesPath = AssetDatabase.GetDependencies(path, true);
                var mats = dependenciesPath.ToList().FindAll((dp) => AssetDatabase.GetMainAssetTypeAtPath(dp) == typeof(Material));
                allMatPaths.AddRange(mats);
            }

            allMatPaths = allMatPaths.Distinct().ToList();
            ShaderVariantCollection allShaderVaraint = null;

            //收集ShaderVaraint
            var tools = new ShaderVariantsCollectionTools();
            allShaderVaraint = tools.CollectionKeywords(allMatPaths.ToArray(), ToolSVC);
            


            var targetDir = Path.GetDirectoryName(BResources.ALL_SHADER_VARAINT_ASSET_PATH);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            AssetDatabase.DeleteAsset(BResources.ALL_SHADER_VARAINT_ASSET_PATH);
            AssetDatabase.CreateAsset(allShaderVaraint, BResources.ALL_SHADER_VARAINT_ASSET_PATH);
            AssetDatabase.Refresh();

            // var dependencies = AssetDatabase.GetDependencies(BResources.ALL_SHADER_VARAINT_ASSET_PATH);
            // foreach (var guid in dependencies )
            // {
            //     Debug.Log("依赖shader:" + guid);
            // }
        }
        
    }
}