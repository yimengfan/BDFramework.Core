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

namespace BDFramework.Editor.AssetBundle
{
    public class ShaderCollection : EditorWindow
    {
        private ShaderVariantCollection svc;
        readonly public static string ALL_SHADER_VARAINT_PATH = "Assets/Resource/Runtime/Shader/AllShaders.shadervariants";
        #region FindMaterial

        static List<string> allShaderNameList = new List<string>();

        public static void SimpleGenShaderVariant()
        {
            //先搜集所有keyword到工具类SVC
            toolSVC = new ShaderVariantCollection();
            var shaders = AssetDatabase.FindAssets("t:Shader", new string[] {"Assets", "Packages"}).ToList();
            foreach (var shader in shaders)
            {
                ShaderVariantCollection.ShaderVariant sv         = new ShaderVariantCollection.ShaderVariant();
                var                                   shaderPath = AssetDatabase.GUIDToAssetPath(shader);
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
            var paths   = BDApplication.GetAllRuntimeDirects().ToArray();
            var assets  = AssetDatabase.FindAssets("t:Prefab", paths).ToList();
            var assets2 = AssetDatabase.FindAssets("t:Material", paths);
            assets.AddRange(assets2);
            List<string> allMats = new List<string>();

            //GUID to assetPath
            for (int i = 0; i < assets.Count; i++)
            {
                var p = AssetDatabase.GUIDToAssetPath(assets[i]);
                //获取依赖中的mat
                var dependenciesPath = AssetDatabase.GetDependencies(p, true);
                var mats             = dependenciesPath.ToList().FindAll((dp) => dp.EndsWith(".mat"));
                allMats.AddRange(mats);
            }

            //处理所有的 material
            allMats = allMats.Distinct().ToList();


            float count = 1;
            foreach (var mat in allMats)
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(mat);
                if (obj is Material)
                {
                    var _mat = obj as Material;
                    EditorUtility.DisplayProgressBar("处理mat", string.Format("处理:{0} - {1}", Path.GetFileName(mat), _mat.shader.name), count / allMats.Count);
                    AddToDict(_mat);
                }

                count++;
            }

            EditorUtility.ClearProgressBar();
            //所有的svc
            ShaderVariantCollection svc = new ShaderVariantCollection();
            foreach (var item in ShaderVariantDict)
            {
                foreach (var _sv in item.Value)
                {
                    svc.Add(_sv);
                }
            }

            AssetDatabase.DeleteAsset(ALL_SHADER_VARAINT_PATH);
            AssetDatabase.CreateAsset(svc, ALL_SHADER_VARAINT_PATH);
            AssetDatabase.Refresh();
        }


        public class ShaderData
        {
            public  int[]      PassTypes         = new int[] { };
            public string[][] KeyWords          = new string[][] { };
            public  string[]   ReMainingKeyWords = new string[] { };
        }

        //shader数据的缓存
        static Dictionary<string, ShaderData> ShaderDataDict = new Dictionary<string, ShaderData>();

        static Dictionary<string, List<ShaderVariantCollection.ShaderVariant>> ShaderVariantDict = new Dictionary<string, List<ShaderVariantCollection.ShaderVariant>>();

        //添加Material计算
        static List<string> passShaderList = new List<string>();

        /// <summary>
        /// 添加到Dictionary
        /// </summary>
        /// <param name="curMat"></param>
        static void AddToDict(Material curMat)
        {
            if (!curMat || !curMat.shader) return;

            var path = AssetDatabase.GetAssetPath(curMat.shader);
            if (!allShaderNameList.Contains(path))
            {
                Debug.LogError("不存在shader:" + curMat.shader.name);
                Debug.Log(path);
                return;
            }

            ShaderData sd = null;
            ShaderDataDict.TryGetValue(curMat.shader.name, out sd);
            if (sd == null)
            {
                //一次性取出所有的 passtypes 和  keywords
                sd                                 = GetShaderKeywords(curMat.shader);
                ShaderDataDict[curMat.shader.name] = sd;
            }

            var kwCount = sd.PassTypes.Length;
            if (kwCount > 2000)
            {
                if (!passShaderList.Contains(curMat.shader.name))
                {
                    Debug.LogFormat("Shader【{0}】,变体数量:{1},不建议继续分析,后续也会跳过!", curMat.shader.name, kwCount);
                    passShaderList.Add(curMat.shader.name);
                }
                else
                {
                    Debug.LogFormat("mat:{0} , shader:{1} ,keywordCount:{2}", curMat.name, curMat.shader.name, kwCount);
                }

                return;
            }

            //变体增加规则：https://blog.csdn.net/RandomXM/article/details/88642534
            List<ShaderVariantCollection.ShaderVariant> svlist = null;
            if (!ShaderVariantDict.TryGetValue(curMat.shader.name, out svlist))
            {
                svlist                                = new List<ShaderVariantCollection.ShaderVariant>();
                ShaderVariantDict[curMat.shader.name] = svlist;
            }

            //求所有mat的kw
            for (int i = 0; i < sd.PassTypes.Length; i++)
            {
                //
                var                                    pt = (PassType) sd.PassTypes[i];
                ShaderVariantCollection.ShaderVariant? sv = null;
                try
                {
                    if (curMat.shaderKeywords.Length > 0)
                    {
                        //变体交集 大于0 ，添加到 svcList
                        sv = new ShaderVariantCollection.ShaderVariant(curMat.shader, pt, curMat.shaderKeywords);
                    }
                    else
                    {
                        sv = new ShaderVariantCollection.ShaderVariant(curMat.shader, pt);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("{0}-当前shader不存在变体（可以无视）:{1}-{2}",curMat.name,pt, JsonMapper.ToJson( curMat.shaderKeywords) );
                    continue;
                }

                //判断sv 是否存在,不存在则添加
                if (sv != null)
                {
                    bool isContain = false;
                    var  _sv       = (ShaderVariantCollection.ShaderVariant) sv;
                    foreach (var val in svlist)
                    {
                        if (val.passType == _sv.passType && System.Linq.Enumerable.SequenceEqual(val.keywords, _sv.keywords))
                        {
                            isContain = true;
                            break;
                        }
                    }

                    if (!isContain)
                    {
                        svlist.Add(_sv);
                    }
                }
            }
        }


        static MethodInfo GetShaderVariantEntries = null;

        static ShaderVariantCollection toolSVC = null;

        //获取shader的 keywords
        public static ShaderData GetShaderKeywords(Shader shader)
        {
            ShaderData sd = new ShaderData();
            GetShaderVariantEntriesFiltered(shader, new string[] { }, out sd.PassTypes, out sd.KeyWords, out sd.ReMainingKeyWords);
            return sd;
        }

        /// <summary>
        /// 获取keyword
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="filterKeywords"></param>
        /// <param name="passTypes"></param>
        /// <param name="keywordLists"></param>
        /// <param name="remainingKeywords"></param>
        static void GetShaderVariantEntriesFiltered(Shader shader, string[] filterKeywords, out int[] passTypes, out string[][] keywordLists, out string[] remainingKeywords)
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
                GetShaderVariantEntries = typeof(ShaderUtil).GetMethod("GetShaderVariantEntriesFiltered", BindingFlags.NonPublic | BindingFlags.Static);
            }

            passTypes         = new int[] { };
            keywordLists      = new string[][] { };
            remainingKeywords = new string[] { };
            if (toolSVC != null)
            {
                var      _passtypes         = new int[] { };
                var      _keywords          = new string[] { };
                var      _remainingKeywords = new string[] { };
                object[] args               = new object[] {shader, 256, filterKeywords, toolSVC, _passtypes, _keywords, _remainingKeywords};
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