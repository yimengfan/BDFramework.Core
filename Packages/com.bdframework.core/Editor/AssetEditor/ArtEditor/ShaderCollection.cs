using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BDFramework.Core.Tools;
using LitJson;
using Microsoft.CodeAnalysis;

namespace BDFramework.Editor.Asset
{
    /// <summary>
    /// shader 搜集
    /// </summary>
    public class ShaderCollection
    {
        static public void GenShaderVariantsCollection()
        {
            var sc = new ShaderCollection();
            sc.GenShaderVariantCollection();
        }


        /// <summary>
        /// shader varaint的路径
        /// </summary>
        readonly public static string ALL_SHADER_VARAINT_PATH = "Assets/Resource/Runtime/Shader/AllShaders.shadervariants";
        /// <summary>
        /// 最大10W个变体
        /// </summary>
        private static int MAX_VARAINT_COUNT = 400000;

        private ShaderVariantCollection svc;


        #region FindMaterial

        /// <summary>
        /// shader路径
        /// </summary>
        List<string> allShaderNameList = new List<string>();

        /// <summary>
        /// shader 缓存的列表
        /// </summary>
        private List<ShaderCache> shaderCacheList = new List<ShaderCache>();

        /// <summary>
        /// 工具svc
        /// </summary>
        static ShaderVariantCollection toolSVC = null;

        /// <summary>
        /// 跳过Shader分析列表
        /// </summary>
        List<Shader> skipShaderList = new List<Shader>();

        /// <summary>
        /// shader缓存
        /// </summary>
        Dictionary<Shader, List<ShaderVariantCollection.ShaderVariant>> ShaderVariantMap =
            new Dictionary<Shader, List<ShaderVariantCollection.ShaderVariant>>();


        /// <summary>
        /// 生成ShaderSVC
        /// </summary>
        /// <returns></returns>
        public string GenShaderVariantCollection()
        {
            //先搜集所有keyword到工具类SVC
            toolSVC = new ShaderVariantCollection();
            var shaders = AssetDatabase.FindAssets("t:Shader", new string[] {"Assets", "Packages"}).ToList();
            foreach (var shader in shaders)
            {
                ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant();
                var shaderPath = AssetDatabase.GUIDToAssetPath(shader);
                sv.shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                toolSVC.Add(sv);
                //
                allShaderNameList.Add(shaderPath);
            }

            var toolsSVCpath = "Assets/Resource/Shaders/Tools.shadervariants";
            //防空
            FileHelper.WriteAllText(toolsSVCpath, "");
            AssetDatabase.DeleteAsset(toolsSVCpath);
            AssetDatabase.CreateAsset(toolSVC, toolsSVCpath);


            //搜索所有Mat
            var paths = BDApplication.GetAllRuntimeDirects().ToArray();
            var assets = AssetDatabase.FindAssets("t:Prefab", paths).ToList();
            var assets2 = AssetDatabase.FindAssets("t:Material", paths);
            assets.AddRange(assets2);


            List<string> allMats = new List<string>();

            //GUID to assetPath
            for (int i = 0; i < assets.Count; i++)
            {
                var p = AssetDatabase.GUIDToAssetPath(assets[i]);
                //获取依赖中的mat
                var dependenciesPath = AssetDatabase.GetDependencies(p, true);
                var mats = dependenciesPath.ToList().FindAll((dp) => dp.EndsWith(".mat"));
                allMats.AddRange(mats);
            }

            //处理所有的 material
            allMats = allMats.Distinct().ToList();


            float count = 1;
            foreach (var matPath in allMats)
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(matPath);
                if (obj is Material)
                {
                    var mat = obj as Material;
                    EditorUtility.DisplayProgressBar("处理mat",
                        string.Format("处理:{0} - {1}", Path.GetFileName(matPath), mat.shader.name),
                        count / allMats.Count);
                    GenMatShaderVaraints(mat);
                }

                count++;
            }

            EditorUtility.ClearProgressBar();

            return "";
            //所有的svc
            ShaderVariantCollection svc = new ShaderVariantCollection();
            foreach (var item in ShaderVariantMap)
            {
                foreach (var sv in item.Value)
                {
                    svc.Add(sv);
                }
            }
            
            AssetDatabase.DeleteAsset(ALL_SHADER_VARAINT_PATH);
            AssetDatabase.CreateAsset(svc, ALL_SHADER_VARAINT_PATH);
            AssetDatabase.Refresh();

            return ALL_SHADER_VARAINT_PATH;
        }


        /// <summary>
        /// shader缓存
        /// </summary>
        public class ShaderCache
        {
            public Shader Shader;
            public string[] Keywords;
        }


        /// <summary>
        /// 生成shader的Varaint
        /// </summary>
        /// <param name="curMat"></param>
        void GenMatShaderVaraints(Material curMat)
        {
            if (!curMat || !curMat.shader)
                return;

            var path = AssetDatabase.GetAssetPath(curMat.shader);
            if (!allShaderNameList.Contains(path))
            {
                Debug.LogError("不存在shader:" + curMat.shader.name);
                Debug.Log(path);
                return;
            }

            if (!curMat.shader.name.Contains("PBR"))
            {
                return;
            }

            //已经处理过的直接return
            var _shaderCache = shaderCacheList.Find((sc) =>
                sc.Shader == curMat.shader && System.Linq.Enumerable.SequenceEqual(curMat.shaderKeywords, sc.Keywords));
            if (_shaderCache != null)
            {
                Debug.Log("已经处理过相同shader keyword:" + curMat.shader.name + " - " + curMat.shaderKeywords);
                return;
            }

            //获取所有keyword
            var _passtypes = new int[] { };
            var _keywords = new string[][] { };
            var _remainingKeywords = new string[] { };
            GetShaderVariantEntriesFiltered(curMat.shader, new string[]{}, out _passtypes, out _keywords,
                out _remainingKeywords);


            if (_passtypes.Length > MAX_VARAINT_COUNT)
            {
                if (!skipShaderList.Contains(curMat.shader))
                {
                    Debug.LogFormat("Shader【{0}】,变体数量:{1},变体数量太多，建议剔除后再分析!", curMat.shader.name, _passtypes.Length);
                    skipShaderList.Add(curMat.shader);
                }
                else
                {
                    Debug.LogFormat("mat:{0} , shader:{1} ,keywordCount:{2}", curMat.name, curMat.shader.name,
                        _passtypes.Length);
                }

                return;
            }

            //变体增加规则：https://blog.csdn.net/RandomXM/article/details/88642534
            List<ShaderVariantCollection.ShaderVariant> svlist = null;
            if (!ShaderVariantMap.TryGetValue(curMat.shader, out svlist))
            {
                svlist = new List<ShaderVariantCollection.ShaderVariant>();
                ShaderVariantMap[curMat.shader] = svlist;
            }

            if (curMat.shaderKeywords.Length > 0)
            {
                Debug.LogFormat("{0}-{1}", "", JsonMapper.ToJson(_remainingKeywords));
                
                for (int i = 0; i < _passtypes.Length; i++)
                {
                    //剔除延迟渲染
                    if (_passtypes[i] == (int) PassType.Deferred)
                    {
                        continue;
                    }
                    //当前keywords 包含 mat的keyword,则添加
                    var ret = _keywords[i].Intersect(curMat.shaderKeywords).ToArray();
                    if (ret.Length == curMat.shaderKeywords.Length)
                    {
                        // var includeRemainingKWs = _keywords[i].Intersect(_remainingKeywords);
                        //
                        // if (includeRemainingKWs.Count() == 0)
                        {
                            var sv = new ShaderVariantCollection.ShaderVariant(curMat.shader, (PassType) _passtypes[i],
                                _keywords[i]);
                            Debug.LogFormat("{0}-{1}", (PassType) _passtypes[i], JsonMapper.ToJson(_keywords[i]));
                            svlist.Add(sv);
                        }
                    }
                }

                Debug.Log("--------------------------------------------------------------------");
            }
            else
            {
                //keyword没选,则把对应的 passtype加入
                var pts = _passtypes.Distinct();
                foreach (var pt in pts)
                {
                    //剔除延迟渲染
                    if (pt == (int) PassType.Deferred)
                    {
                        continue;
                    }
                    
                    var sv = new ShaderVariantCollection.ShaderVariant(curMat.shader, (PassType) pt);
                    svlist.Add(sv);
                }
            }
        }


        static MethodInfo GetShaderVariantEntries = null;

        /// <summary>
        /// 获取keyword
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="filterKeywords"></param>
        /// <param name="passTypes"></param>
        /// <param name="keywordLists"></param>
        /// <param name="remainingKeywords"></param>
        static void GetShaderVariantEntriesFiltered(Shader shader,
            string[] filterKeywords,
            out int[] passTypes,
            out string[][] keywordLists,
            out string[] remainingKeywords)
        {
            //2019.3接口
//            internal static void GetShaderVariantEntriesFiltered(
//                Shader                  shader,                     0
//                int                     maxEntries,                 1
//                string[]                filterKeywords,             2
//                ShaderVariantCollection excludeCollection,          3
//                out int[]               passTypes,                  4
//                out string[]            keywordLists,               5
//                out string[]            remainingKeywords)          6
            if (GetShaderVariantEntries == null)
            {
                GetShaderVariantEntries = typeof(ShaderUtil).GetMethod("GetShaderVariantEntriesFiltered",
                    BindingFlags.NonPublic | BindingFlags.Static);
            }

            passTypes = new int[] { };
            keywordLists = new string[][] { };
            remainingKeywords = new string[] { };
            if (toolSVC != null)
            {
                var _passtypes = new int[] { };
                var _keywords = new string[] { };
                var _remainingKeywords = new string[] { };
                object[] args = new object[]
                {
                    shader, MAX_VARAINT_COUNT, filterKeywords, toolSVC, _passtypes, _keywords, _remainingKeywords
                };
                GetShaderVariantEntries.Invoke(null, args);

                var passtypes = args[4] as int[];
                passTypes = passtypes;
                //key word
                keywordLists = new string[passtypes.Length][];
                var kws = args[5] as string[];
                for (int i = 0; i < passtypes.Length; i++)
                {
                    keywordLists[i] = kws[i].Split(' ');
                }

                //Remaning key word
                var rnkws = args[6] as string[];
                remainingKeywords = rnkws;
            }
        }

        #endregion
    }
}