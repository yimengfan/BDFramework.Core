using System.Text;
using System.Net;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;
using System.Linq;
using BDFramework.Editor.Unity3dEx;

namespace BDFramework.Editor.AssetBundle
{
    public class ShaderVariantsCollectionWindow : EditorWindow
    {
        //系统带的、material.shaderKeywords拿不到的宏添到这里,含有这些关键字的shader会启用对应变体
        HashSet<string> ForceEnabledGlobalKeywords = new HashSet<string>()
        {
            "_MAIN_LIGHT_SHADOWS",
            "_MAIN_LIGHT_SHADOWS_CASCADE",
            "_SHADOWS_SOFT",
            "LIGHTMAP_ON",
            "UNITY_HDR_ON",
            "_SHADOWS_SOFT",
            "_ADDITIONAL_LIGHTS",
        };

        HashSet<string> ForceDisabledGlobalKeywords = new HashSet<string>() { };

        static List<string> defultCollectingPath = new List<string>()
        {
            "Assets/App/Art/Characters",
            "Assets/App/Art/Effects",
            "Assets/App/Art/Scenes",
            "Assets/App/Pro/GameRes",
            "Assets/App/Pro/Graphic",
        };

        static List<string>            collectingPath  = new List<string>();
        static string                  savePath        = "Assets/App/Pro/Graphic/Shader";
        static string                  shaderUsagePath = "Assets/ShaderUsage.csv";
        static ShaderVariantCollection collection;
        static Shader                  srcShader;
        static Shader                  newShader;
        static string                  log;

        [MenuItem("Tools/资源管理/ShaderVariants搜集工具 &F9")]
        public static void Init()
        {
            var window = GetWindow(typeof(ShaderVariantsCollectionWindow), false, "Shader工具", true) as ShaderVariantsCollectionWindow;
            window.position = new Rect(600, 400, 400, 130);
            collectingPath  = new List<string>(defultCollectingPath);
            log             = string.Empty;
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("添加"))
            {
                var path = collectingPath.Count > 0 ? collectingPath[collectingPath.Count - 1] : "Assets";
                var str  = $"{EditorUtility.OpenFolderPanel("选择路径", path, "")}";
                if (string.IsNullOrEmpty(str)) return;
                str = str.Substring(str.LastIndexOf("Assets"));
                var flg = true;
                foreach (var p in collectingPath)
                    if (p.Equals(str))
                        flg = false;
                if (flg) collectingPath.Add(str);
                log = "Not Collected...";
            }

            if (GUILayout.Button("清理"))
            {
                collectingPath.Clear();
                log = "path is null...";
            }

            if (GUILayout.Button("默认"))
            {
                collectingPath = new List<string>(defultCollectingPath);
                log            = string.Empty;
            }

            GUILayout.Label($"路径:    {string.Join("/;", collectingPath.ToArray())}/;");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Print All Macros Used In Shader", "打印Shader中使用到的宏")))
            {
                string[] keys;
                // var material = Selection.activeObject as Material;
                // if (material == null) return;
                // keys = material.shaderKeywords;
                keys = ShaderUtilImpl.GetAllGlobalKeywords();
                // keys = ShaderUtilImpl.GetShaderLocalKeywords(material.shader);
                // keys = ShaderUtilImpl.GetShaderGlobalKeywords(material.shader);
                // Debug.Log(material.IsKeywordEnabled("_MAIN_LIGHT_SHADOWS"));

                // string[] remainingKeys;
                // string[] filterKeys = null;
                // var passTypes = new int[] { (int)PassType.Normal, (int)PassType.ShadowCaster };
                // ShaderUtilImpl.GetShaderVariantEntriesFiltered(material.shader, 1000, filterKeys, new ShaderVariantCollection(), out passTypes, out keys, out remainingKeys);
                foreach (var key in keys)
                {
                    Debug.Log(key);
                }
            }

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Print All Shader Useage To File", "打印工程中的Shader使用情况到csv文件")))
            {
                var           materialGUIDs = AssetDatabase.FindAssets("t:Material", new string[] { "Assets" });
                StringBuilder str           = new StringBuilder();
                foreach (var id in materialGUIDs)
                {
                    var path   = AssetDatabase.GUIDToAssetPath(id);
                    var mat    = AssetDatabase.LoadAssetAtPath<Material>(path);
                    var shader = mat.shader;
                    str.Append($"{path},{shader.name}\n");
                }

                // if (File.Exists(shaderUsagePath)) File.Delete(shaderUsagePath);
                File.WriteAllText(shaderUsagePath, str.ToString());
                log = $"Saved Shader Useage To {shaderUsagePath}!";
            }

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("收集Shader变体", "收集当前路径中材质球用到的Shader变体")))
            {
                Collection();
                Save();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Replace", "一键替换当前路径下所有材质球上的Shader")))
            {
                if (srcShader == null || newShader == null || collectingPath.Count <= 0)
                {
                    log = "路径或Shader为空,替换无效";
                    return;
                }

                var materialGUIDs = AssetDatabase.FindAssets("t:Material", collectingPath.ToArray());
                foreach (var guid in materialGUIDs)
                {
                    var path                                  = AssetDatabase.GUIDToAssetPath(guid);
                    var mat                                   = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat.shader == (srcShader)) mat.shader = newShader;
                    //TODO:转换材质参数
                }

                AssetDatabase.SaveAssets();
                log = $"已将当前路径下所有材质球中{srcShader.name}替换为{newShader.name}";
            }

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Swap", "交换新旧Shader")))
            {
                var temp = srcShader;
                srcShader = newShader;
                newShader = temp;
                log       = "swapped!";
            }

            ;
            srcShader = EditorGUILayout.ObjectField(srcShader, typeof(Shader), false) as Shader;
            GUILayout.Label("  To");
            newShader = EditorGUILayout.ObjectField(newShader, typeof(Shader), false) as Shader;
            GUILayout.EndHorizontal();

            GUILayout.Label(log);
        }

        void Collection()
        {
            if (collectingPath.Count <= 0)
            {
                log = "Path is null, Select at least one !";
                return;
            }

            collection = new ShaderVariantCollection();
            var materialGUIDs = AssetDatabase.FindAssets("t:Material", collectingPath.ToArray());
            foreach (var guid in materialGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // if (path.EndsWith("FBX") || path.EndsWith("fbx") || path.EndsWith("obj")) continue;
                var material = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;

                // AddVariantOfPassTypeToCollection(PassType.Normal, material);
                // AddVariantOfPassTypeToCollection(PassType.ScriptableRenderPipelineDefaultUnlit, material);
                AddVariantOfPassTypeToCollection(PassType.ScriptableRenderPipeline, material);
                if (material.FindPass("ShadowCaster") != -1)
                {
                    AddVariantOfPassTypeToCollection(PassType.ShadowCaster, material);
                }
            }

            log = $"Found  {collection.shaderCount} Shaders & {collection.variantCount} Variants  Used In All Materials.";
        }

        void AddVariantOfPassTypeToCollection(PassType passType, Material material)
        {
            var shader             = material.shader;
            var keywords           = new List<string>();
            var shaderAllkeyworlds = GetShaderAllKeyworlds(shader);
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
                        default: break;
                    }
                }
            }

            if (material.enableInstancing) keywords.Add("INSTANCING_ON");
            foreach (var key in material.shaderKeywords) keywords.Add(key);
            foreach (var key in ForceEnabledGlobalKeywords)
            {
                if (shaderAllkeyworlds.Contains(key) /*&& Shader.IsKeywordEnabled(key)*/) keywords.Add(key);
            }

            foreach (var key in ForceDisabledGlobalKeywords) keywords.Remove(key);

            collection.Add(CreateVariant(shader, passType, keywords.ToArray()));
        }

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
                variant.shader   = shader;
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

        void Save()
        {
            var str = $"{EditorUtility.SaveFilePanel("选择保存路径", savePath, "NewShaderVariants", "shadervariants")}";
            savePath = str.Substring(str.LastIndexOf("Assets"));
            if (collection && !string.IsNullOrEmpty(savePath))
            {
                if (File.Exists(str)) AssetDatabase.DeleteAsset(savePath);
                AssetDatabase.CreateAsset(collection, savePath);
                UnityEditor.EditorUtility.FocusProjectWindow();
                UnityEditor.Selection.activeObject = collection;
                log                                = $"(shader:{collection.shaderCount}, variant:{collection.variantCount}) Collection Saved At: {savePath} !";
                collection                         = null; //overwrite goes wrong...
            }
            else
                log = "Not Saved, Please Collect Them First!";
        }
    }
}