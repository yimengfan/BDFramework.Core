using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using marijnz.EditorCoroutines;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BDFramework.Editor.AssetBundle
{
    /// <summary>
    /// Assetbundle 检测
    /// </summary>
    static public class AssetBundleEditorToolsV2CheckAssetbundle
    {
        static private Transform UI_ROOT;
        static private Transform SCENE_ROOT;
        static private DevResourceMgr DevLoder;
        static private AssetBundleMgrV2 AssetBundleLoader;
        static private Camera Camera;
        static private EditorWindow GameView;
        
        private static string ScenePath = "Packages/com.popo.bdframework/Runtime/AssetsManager/UnitTest/AssetBundleBenchmark_01.unity";
        private static string ScenePath2 = "Packages/com.popo.bdframework/Runtime/AssetsManager/UnitTest/AssetBundleBenchmark_Async.unity";
        /// <summary>
        /// 测试加载所有的AssetBundle
        /// </summary>
        static public void TestLoadAssetbundleRuntime()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }
            //打开场景、运行
            EditorSceneManager.OpenScene(ScenePath);
            //运行场景
            EditorApplication.ExecuteMenuItem("Edit/Play");

        }
        /// <summary>
        /// 测试加载所有的AssetBundle_Async
        /// </summary>
        static public void TestLoadAssetbundleRuntimeAsync()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }
            //打开场景、运行
            EditorSceneManager.OpenScene(ScenePath2);
            //运行场景
            EditorApplication.ExecuteMenuItem("Edit/Play");

        }
        /// <summary>
        /// 测试加载所有的AssetBundle
        /// </summary>
        static public void TestLoadAssetbundleOnEditor(string abPath)
        {
            //打开场景、运行
            EditorSceneManager.OpenScene(ScenePath);
            //运行场景
            //EditorApplication.ExecuteMenuItem("Edit/Play");


            //执行
            //初始化加载环境
            // UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
            // //dev加载器
            // DevLoder = new DevResourceMgr();
            // DevLoder.Init("");
            // AssetBundleLoader = new AssetBundleMgrV2();
            // AssetBundleLoader.Init(abPath);
            // //节点
            // UI_ROOT = GameObject.Find("UIRoot").transform;
            // SCENE_ROOT = GameObject.Find("3dRoot").transform;
            // //相机
            // Camera = GameObject.Find("Camera").GetComponent<Camera>();
            // Camera.cullingMask = -1;
            // Camera.gameObject.hideFlags = HideFlags.DontSave;
            // //获取gameview
            // var assembly = typeof(UnityEditor.EditorWindow).Assembly;
            // System.Type GameViewType = assembly.GetType("UnityEditor.GameView");
            // GameView = EditorWindow.GetWindow(GameViewType);
            //
            // //开始加载
            // EditorCoroutineExtensions.StartCoroutine(IE_LoadAll(), new object());
        }


        /// <summary>
        /// 加载消耗数据
        /// </summary>
        public class LoadTimeData
        {
            public string LoadPath;

            /// <summary>
            /// 加载时长
            /// </summary>
            public float LoadTime;

            /// <summary>
            /// 初始化时长
            /// </summary>
            public float InstanceTime;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        private static Dictionary<string, List<LoadTimeData>> loadDataMap = new Dictionary<string, List<LoadTimeData>>();

        /// <summary>
        /// 加载所有assetbundle
        /// </summary>
        /// <returns></returns>
        static IEnumerator IE_LoadAll()
        {
            var outpath = BApplication.BDEditorCachePath + "/AssetBundle";
            if (!Directory.Exists(outpath))
            {
                Directory.CreateDirectory(outpath);
            }

            loadDataMap.Clear();
            //加载
            var allRuntimeAssets = BApplication.GetAllRuntimeAssetsPath();

            foreach (var asset in allRuntimeAssets)
            {
                var type = AssetBundleEditorToolsV2.GetMainAssetTypeAtPath(asset);
                if (type == null)
                {
                    Debug.LogError("无法获得资源类型:" + asset);
                    continue;
                }

                var idx = asset.IndexOf(AssetBundleBuildingContext.RUNTIME_PATH, StringComparison.OrdinalIgnoreCase);
                var runtimePath = asset.Substring(idx + AssetBundleBuildingContext.RUNTIME_PATH.Length);
                runtimePath = runtimePath.Replace(Path.GetExtension(runtimePath), "");
                runtimePath = runtimePath.Replace("\\", "/");
                //Debug.Log("【LoadTest】:" + runtimePath);
                List<LoadTimeData> loadList = null;
                if (!loadDataMap.TryGetValue(type.FullName, out loadList))
                {
                    loadList = new List<LoadTimeData>();
                    loadDataMap[type.FullName] = loadList;
                }

                var loadData = new LoadTimeData();
                loadData.LoadPath = runtimePath;
                loadList.Add(loadData);
                //计时器
                Stopwatch sw = new Stopwatch();
                if (type == typeof(GameObject))
                {
                    //加载
                    sw.Start();
                    var obj = AssetBundleLoader.Load<GameObject>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    //实例化
                    if (obj != null)
                    {
                        sw.Restart();
                        var gobj = GameObject.Instantiate(obj);
                        sw.Stop();
                        loadData.InstanceTime = sw.ElapsedTicks;
                        //UI
                        var rectTransform = gobj.GetComponentInChildren<RectTransform>();
                        if (rectTransform != null)
                        {
                            gobj.transform.SetParent(UI_ROOT, false);
                        }
                        else
                        {
                            gobj.transform.SetParent(SCENE_ROOT);
                        }

                        //抓屏 保存
                        var outpng = string.Format("{0}/{1}_ab.png", outpath, runtimePath.Replace("/", "_"));
                        yield return null;
                        //渲染
                        GameView.Repaint();
                        GameView.Focus();

                        yield return null;
                        //抓屏 
                        //TODO 这里有时候能抓到 有时候抓不到
                        ScreenCapture.CaptureScreenshot(outpng);
                        //删除
                        GameObject.DestroyImmediate(gobj);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("【Prefab】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(TextAsset))
                {
                    //测试打印AssetText资源
                    sw.Start();
                    var textAsset = AssetBundleLoader.Load<TextAsset>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!textAsset)
                    {
                        UnityEngine.Debug.LogError("【TextAsset】加载失败:" + runtimePath);
                    }
                    else
                    {
                        UnityEngine.Debug.Log(textAsset.text);
                    }
                }
                else if (type == typeof(Texture))
                {
                    sw.Start();
                    var tex = AssetBundleLoader.Load<Texture>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!tex)
                    {
                        UnityEngine.Debug.LogError("【Texture】加载失败:" + runtimePath);
                    }

                    break;
                }
                else if (type == typeof(Texture2D))
                {
                    sw.Start();
                    var tex = AssetBundleLoader.Load<Texture2D>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!tex)
                    {
                        UnityEngine.Debug.LogError("【Texture2D】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(Sprite))
                {
                    sw.Start();
                    var sp = AssetBundleLoader.Load<Sprite>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!sp)
                    {
                        UnityEngine.Debug.LogError("【Sprite】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(Material))
                {
                    sw.Start();
                    var mat = AssetBundleLoader.Load<Material>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!mat)
                    {
                        UnityEngine.Debug.LogError("【Material】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(Shader))
                {
                    sw.Start();
                    var shader = AssetBundleLoader.Load<Shader>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!shader)
                    {
                        UnityEngine.Debug.LogError("【Shader】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(AudioClip))
                {
                    sw.Start();
                    var ac = AssetBundleLoader.Load<AudioClip>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!ac)
                    {
                        UnityEngine.Debug.LogError("【AudioClip】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(AnimationClip))
                {
                    sw.Start();
                    var anic = AssetBundleLoader.Load<AnimationClip>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!anic)
                    {
                        UnityEngine.Debug.LogError("【AnimationClip】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(Mesh))
                {
                    sw.Start();
                    var mesh = AssetBundleLoader.Load<Mesh>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!mesh)
                    {
                        UnityEngine.Debug.LogError("【Mesh】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(Font))
                {
                    sw.Start();

                    var font = AssetBundleLoader.Load<Font>(runtimePath);

                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                    if (!font)
                    {
                        UnityEngine.Debug.LogError("【Font】加载失败:" + runtimePath);
                    }
                }
                else if (type == typeof(SpriteAtlas))
                {
                    sw.Start();
                    var sa = AssetBundleLoader.Load<SpriteAtlas>(runtimePath);
                    sw.Stop();
                    if (!sa)
                    {
                        UnityEngine.Debug.LogError("【SpriteAtlas】加载失败:" + runtimePath);
                    }

                    loadData.LoadTime = sw.ElapsedTicks;
                }
                else if (type == typeof(ShaderVariantCollection))
                {
                    sw.Start();
                    var svc = AssetBundleLoader.Load<ShaderVariantCollection>(runtimePath);
                    svc?.WarmUp();
                    sw.Stop();
                    if (!svc)
                    {
                        UnityEngine.Debug.LogError("【ShaderVariantCollection】加载失败:" + runtimePath);
                    }

                    loadData.LoadTime = sw.ElapsedTicks;
                }
                else if (type == typeof(AnimatorController))
                {
                    sw.Start();
                    var aniCtrl = AssetBundleLoader.Load<AnimatorController>(runtimePath);
                    sw.Stop();
                    if (!aniCtrl)
                    {
                        UnityEngine.Debug.LogError("【AnimatorController】加载失败:" + runtimePath);
                    }

                    loadData.LoadTime = sw.ElapsedTicks;
                }
                else
                {
                    sw.Start();
                    var gobj = AssetBundleLoader.Load<Object>(runtimePath);
                    sw.Stop();
                    if (!gobj)
                    {
                        UnityEngine.Debug.LogError("【Object】加载失败:" + runtimePath);
                    }

                    UnityEngine.Debug.LogError("待编写测试! -" + type.FullName);
                }

                //打印
                
                Debug.LogFormat("<color=yellow>{0}</color> <color=green>【加载】:<color=yellow>{1}ms</color>;【初始化】:<color=yellow>{2}ms</color> </color>", loadData.LoadPath, loadData.LoadTime / 10000f, loadData.InstanceTime / 10000f);
                yield return null;
            }
            
            yield return null;
            
            foreach (var item in loadDataMap)
            {
                Debug.Log("<color=red>【" + item.Key + "】</color>");
                foreach (var ld in item.Value)
                {
                    Debug.LogFormat(
                        "<color=yellow>{0}</color> <color=green>【加载】:<color=yellow>{1}ms</color>;【初始化】:<color=yellow>{2}ms</color> </color>",
                        ld.LoadPath, ld.LoadTime / 10000f, ld.InstanceTime / 10000f);
                }
            }

            yield return null;
            EditorUtility.RevealInFinder(outpath);
        }
    }
}
