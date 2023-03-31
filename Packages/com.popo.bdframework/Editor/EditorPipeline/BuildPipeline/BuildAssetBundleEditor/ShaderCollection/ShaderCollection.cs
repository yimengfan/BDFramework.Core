using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using LitJson;
using Sirenix.Utilities;

namespace BDFramework.Editor.BuildPipeline.AssetBundle
{
    public class ShaderCollection
    {
        private ShaderVariantCollection svc;
        static string toolsSVCpath = "Assets/Resource/Shaders/Tools.shadervariants";
        static ShaderVariantCollection ToolSVC = null;
        static List<string> allShaderNameList = new List<string>();

        /// <summary>
        /// 收集shaderVariant
        /// </summary>
        /// <param name="allAssets"></param>
        /// <returns>tuple<string,string>=> shader路径-shaderVariant </returns>
        public static List<Tuple<string, string>> CollectShaderVariant(string[] allAssets = null)
        {
            //收集材质
            string[] allMatPaths;
            //传入资源
            if (allAssets != null)
            {
                allMatPaths = allAssets.Where((asset) => asset.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            else
            {
                //收集runtime的材质
                allMatPaths = CollectMatFromRuntime();
            }

            //创建上下文
            //先搜集所有keyword到工具类SVC
            ToolSVC = new ShaderVariantCollection();
            var shaders = AssetDatabase.FindAssets("t:Shader", new string[] {"Assets", "Packages"}).ToList();
            foreach (var guid in shaders)
            {
                var shaderPath = AssetDatabase.GUIDToAssetPath(guid);
                //var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                //清理shader的默认图片
                Shader shader = null;
                bool ischanged = false;
                var ai = AssetImporter.GetAtPath(shaderPath);
                if (ai is ShaderImporter shaderImporter)
                {
                    shader = shaderImporter.GetShader();

                    for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                    {
                        var type = ShaderUtil.GetPropertyType(shader, i);
                        if (type == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            var propName = ShaderUtil.GetPropertyName(shader, i);
                            var tex = shaderImporter.GetDefaultTexture(propName);
                            if (tex)
                            {
                                ischanged = true;
                                shaderImporter.SetDefaultTextures(new string[] {propName}, new Texture[] {null});
                                Debug.Log($"清理shader默认贴图:{shaderPath} - {propName}");
                            }
                        }
                    }
                }
                else
                {
                    shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                }

                if (ischanged)
                {
                    ai.SaveAndReimport();
                }


                //添加
                ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant();
                sv.shader = shader;
                ToolSVC.Add(sv);

                allShaderNameList.Add(shaderPath);
            }


            //防空
            var dirt = Path.GetDirectoryName(toolsSVCpath);
            if (!Directory.Exists(dirt))
            {
                Directory.CreateDirectory(dirt);
                //For mac osx
                AssetDatabase.Refresh();
            }
            
            AssetDatabase.CreateAsset(ToolSVC, toolsSVCpath);

            //开始收集ShaderVaraint

            var tools = new ShaderVariantsCollectionTools();
            var svcMap = tools.CollectionKeywords(allMatPaths.ToArray(), ToolSVC);

            //输出SVC文件
            var targetDir = Path.GetDirectoryName(BResources.ALL_SHADER_VARAINT_ASSET_PATH);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            List<Tuple<string, string>> retVcsList = new List<Tuple<string, string>>();
            //创建SVC
            if (Directory.Exists(BResources.ALL_SHADER_VARAINT_ASSET_PATH))
            {
                Directory.Delete(BResources.ALL_SHADER_VARAINT_ASSET_PATH, true);
            }

            Directory.CreateDirectory(BResources.ALL_SHADER_VARAINT_ASSET_PATH);
 
          
            foreach (var item in svcMap)
            {
                var svcPath = $"{BResources.ALL_SHADER_VARAINT_ASSET_PATH}/{item.Key.name.Trim().Replace("/","@")}.shadervariants";
                AssetDatabase.CreateAsset(item.Value, svcPath);
                var shaderPath = AssetDatabase.GetAssetPath(item.Key);
                retVcsList.Add(new Tuple<string, string>(shaderPath, svcPath));
                // Debug.Log($"svc:{svcPath}");
            }
            //创建一个dummy.shadervariant,用于方便找到
            var dumnmy = $"Assets/Resource/Runtime/{BResources.DUMMY_SHADER_PATH}.shadervariants";
            var dummySVC = new ShaderVariantCollection();
            AssetDatabase.CreateAsset(dummySVC, dumnmy);
            retVcsList.Add(new Tuple<string, string>("",dumnmy));
            //防止SBP打包失败
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=red>shader_features收集完毕,multi_compiles默认全打包需要继承IPreprocessShaders.OnProcessShader自行剔除!</color>");
            return retVcsList;
        }


        /// <summary>
        /// 收集runtime中的mat
        /// </summary>
        /// <returns></returns>
        static private string[] CollectMatFromRuntime()
        {
            var paths = BApplication.GetAllRuntimeDirects().ToArray();
            //搜索所有runtime中所有可能挂载mat的地方
            var scriptObjectAssets = AssetDatabase.FindAssets("t:ScriptableObject", paths).ToList(); //自定义序列化脚本中也有可能有依赖
            var prefabAssets = AssetDatabase.FindAssets("t:Prefab", paths).ToList();
            var matAssets = AssetDatabase.FindAssets("t:Material", paths).ToList();


            //ERROR: 添加runtime中独立的shader,没有mat 视为其没有Shader_Featrue变体
            var shaderAssets = AssetDatabase.FindAssets("t:Shader", paths); //自定义序列化脚本中也有可能有依赖
            foreach (var guid in shaderAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.LogError("不建议将shader放在runtime中,没有mat信息 无法搜集变体！ " + path);
            }

            //搜索mat
            var guidList = new List<string>();
            guidList.AddRange(prefabAssets);
            guidList.AddRange(matAssets);
            guidList.AddRange(scriptObjectAssets);
            List<string> allMatPaths = new List<string>();
            //GUID to assetPath
            for (int i = 0; i < guidList.Count; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guidList[i]);
                //获取依赖中的mat
                var dependenciesPath = AssetDatabase.GetDependencies(path, true);
                foreach (var dp in dependenciesPath)
                {
                    if (Path.GetExtension(dp).Equals(".mat", StringComparison.OrdinalIgnoreCase))
                    {
                        allMatPaths.Add(dp);
                    }
                    else if (Path.GetExtension(dp).Equals(".asset", StringComparison.OrdinalIgnoreCase)) //依赖的ScripttableObject,会
                    {
                        scriptObjectAssets.Add(AssetBundleToolsV2.AssetPathToGUID(dp));
                    }
                }
            }

            //ScripttableObject 里面有可能存mat信息
            foreach (var asset in scriptObjectAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    allMatPaths.Add(path);
                }
            }


            return allMatPaths.Distinct().ToArray();
        }

        /// <summary>
        /// 打包ShaderOnly
        /// </summary>
        public static void BuildShadersAssetBundle()
        {
            //收集shaderVaraint
            // var vcsLit = CollectShaderVariant();
            // List<string> dependList = new List<string>();
            // foreach (var item in vcsLit)
            // {
            //     var dependice = AssetDatabase.GetDependencies(item.Item2);
            //     dependList.AddRange(dependice);
            // }
            //
            // //打包ab信息
            // var build = new AssetBundleBuild();
            // build.assetNames.AddRange(dependList);
            // build.assetBundleName = BResources.ALL_SHADER_VARAINT_ASSET_PATH;
            //
            //
            // //开始编译
            // var outpath = IPath.Combine(BApplication.Library, "BuildAssetsTest", BApplication.GetRuntimePlatformPath());
            // if (Directory.Exists(outpath))
            // {
            //     Directory.Delete(outpath, true);
            // }
            //
            // Directory.CreateDirectory(outpath);
            // var buildtarget = BApplication.GetBuildTarget(BApplication.RuntimePlatform);
            // UnityEditor.BuildPipeline.BuildAssetBundles(outpath, new AssetBundleBuild[] {build}, BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression, buildtarget);


            //Debug.Log("测试AB已经输出:" + outpath);
        }
    }
}
