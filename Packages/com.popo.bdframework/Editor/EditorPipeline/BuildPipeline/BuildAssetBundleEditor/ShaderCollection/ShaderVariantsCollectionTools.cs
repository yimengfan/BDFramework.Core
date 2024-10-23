using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Linq;
using BDFramework.Editor.Unity3dEx;

namespace BDFramework.Editor.BuildPipeline.AssetBundle
{
    public class ShaderVariantsCollectionTools
    {
        //系统带的、material.shaderKeywords拿不到的宏添到这里,含有这些关键字的shader会启用对应变体
        static HashSet<string> ForceEnabledGlobalKeywords = new HashSet<string>()
        {
            "_MAIN_LIGHT_SHADOWS",
            "_MAIN_LIGHT_SHADOWS_CASCADE",
            "_SHADOWS_SOFT",
            "LIGHTMAP_ON",
            "UNITY_HDR_ON",
            "_SHADOWS_SOFT",
            "_ADDITIONAL_LIGHTS",
        };

        static HashSet<string> ForceDisabledGlobalKeywords = new HashSet<string>() { };

        /// <summary>
        /// shader数据map
        /// </summary>
        private Dictionary<Shader, ShaderUtilImpl.ShaderVariantEntriesData> shaderDataMap = new Dictionary<Shader, ShaderUtilImpl.ShaderVariantEntriesData>();

        /// <summary>
        /// 搜集keywords
        /// </summary>
        public Dictionary<Shader,ShaderVariantCollection> CollectionKeywords(string[] matPaths,ShaderVariantCollection excludeCollection)
        {
           var retSVCMap = new Dictionary<Shader, ShaderVariantCollection>();
            //遍历所有mat的KeyWords 
            foreach (var path in matPaths)
            {
                //Material;
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    Debug.LogError("加载mat失败:" + path);
                    continue;
                }
                //shader数据
                var ret = shaderDataMap.TryGetValue(material.shader, out var shaderData);
                if (!ret)
                {
                    shaderData = ShaderUtilImpl.GetShaderVariantEntriesFilteredInternal(material.shader, 256, new string[] { }, excludeCollection);
                    shaderDataMap[material.shader] = shaderData;
                }

                ret= retSVCMap.TryGetValue(material.shader, out var shaderCollection);
                if (!ret)
                {
                    shaderCollection = new ShaderVariantCollection();
                    retSVCMap[material.shader] = shaderCollection;
                }
                //收集shaderVaraint
                var passTypes = shaderData.passTypes.Distinct();
                foreach (var pt in passTypes)
                {
                   var shaderVaraint =   AddVariantOfPassTypeToCollection((PassType)pt, material);
                   shaderCollection.Add(shaderVaraint);
                }
                //SRP的搜集
                // AddVariantOfPassTypeToCollection(PassType.ScriptableRenderPipeline, material);
                // //ShadiwCast的keyword
                // if (material.FindPass("ShadowCaster") != -1)
                // {
                //     AddVariantOfPassTypeToCollection(PassType.ShadowCaster, material);
                // }
            }

            return retSVCMap;
        }

        /// <summary>
        /// 收集passtype-keyword
        /// </summary>
        /// <param name="passType"></param>
        /// <param name="material"></param>
        ShaderVariantCollection.ShaderVariant AddVariantOfPassTypeToCollection(PassType passType, Material material)
        {
            var shader = material.shader;
            var keywords = new List<string>();
            var shaderAllkeyworlds = GetShaderAllKeyworlds(shader);
            //Fog
            if (shaderAllkeyworlds.Contains("FOG_LINEAR") || shaderAllkeyworlds.Contains("FOG_EXP") || shaderAllkeyworlds.Contains("FOG_EXP2"))
            {
                if (RenderSettings.fog)
                {
                    switch (RenderSettings.fogMode)
                    {
                        case FogMode.Linear:
                            keywords.Add("FOG_LINEAR");
                            break;
                        case FogMode.Exponential:
                            keywords.Add("FOG_EXP");
                            break;
                        case FogMode.ExponentialSquared:
                            keywords.Add("FOG_EXP2");
                            break;
                        default:
                        {
                            break;
                        }
                    }
                }
            }

            //Instancing
            if (material.enableInstancing)
            {
                keywords.Add("INSTANCING_ON");
            }

            //添加mat中的keyword
            foreach (var key in material.shaderKeywords)
            {
                keywords.Add(key);
            }

            //打开的global keyword
            foreach (var key in ForceEnabledGlobalKeywords)
            {
                if (shaderAllkeyworlds.Contains(key) /*&& Shader.IsKeywordEnabled(key)*/)
                {
                    keywords.Add(key);
                }
            }

            //关闭的global keyword
            foreach (var key in ForceDisabledGlobalKeywords)
            {
                keywords.Remove(key);
            }

            return CreateVariant(shader, passType, keywords.ToArray());
        }

        /// <summary>
        /// 创建Variant
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="passType"></param>
        /// <param name="keywords"></param>
        /// <returns></returns>
        ShaderVariantCollection.ShaderVariant CreateVariant(Shader shader, PassType passType, string[] keywords)
        {
            // foreach (var k in keywords)
            // {
            //     Debug.Log($"{shader.name}:{passType}:{k}");
            // }
            try
            {
                // var variant = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);//这构造函数就是个摆设,铁定抛异常(╯‵□′)╯︵┻━┻
                var variant = new ShaderVariantCollection.ShaderVariant();
                variant.shader = shader;
                variant.passType = passType;
                variant.keywords = keywords;
                return variant;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                return new ShaderVariantCollection.ShaderVariant();
            }
        }

        Dictionary<Shader, List<string>> shaderKeyworldsDic = new Dictionary<Shader, List<string>>();

        /// <summary>
        /// 获取所有的GlobalKeyword
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        List<string> GetShaderAllKeyworlds(Shader shader)
        {
            List<string> keywords = null;
            shaderKeyworldsDic.TryGetValue(shader, out keywords);
            if (keywords == null)
            {
                keywords = new List<string>(ShaderUtilImpl.GetShaderGlobalKeywords(shader));
                shaderKeyworldsDic.Add(shader, keywords);
            }

            return keywords;
        }
    }
}