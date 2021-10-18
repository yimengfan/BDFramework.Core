using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using marijnz.EditorCoroutines;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

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

        /// <summary>
        /// 测试加载所有的AssetBundle
        /// </summary>
        static public void TestLoadAssetbundle(string abPath)
        {
            //打开场景、运行
            EditorSceneManager.OpenScene("Packages/com.popo.bdframework/Editor/EditorWindows/AssetBundleEditor/Scene/AssetBundleTest.unity");
            //运行场景
            //EditorApplication.ExecuteMenuItem("Edit/Play");


            //初始化加载环境
            UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
            BResources.Load(AssetLoadPath.StreamingAsset, abPath);
            //dev加载器
            DevLoder = new DevResourceMgr();
            DevLoder.Init("");
            AssetBundleLoader = new AssetBundleMgrV2();
            AssetBundleLoader.Init(Application.streamingAssetsPath);
            //节点
            UI_ROOT = GameObject.Find("UIRoot").transform;
            SCENE_ROOT = GameObject.Find("3dRoot").transform;
            //相机
            Camera = GameObject.Find("Camera").GetComponent<Camera>();
            Camera.cullingMask = -1;
            Camera.gameObject.hideFlags = HideFlags.DontSave;
            //获取gameview
            var assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type GameViewType = assembly.GetType("UnityEditor.GameView");
            GameView = EditorWindow.GetWindow(GameViewType);

            //开始加载
            EditorCoroutineExtensions.StartCoroutine(IE_LoadAll(), new object());
        }


        /// <summary>
        /// 加载所有assetbundle
        /// </summary>
        /// <returns></returns>
        static IEnumerator IE_LoadAll()
        {
            var outpath = BDApplication.BDEditorCachePath + "/AssetBundle";
            //加载
            var allRuntimeAssets = BDApplication.GetAllRuntimeAssetsPath();

            foreach (var asset in allRuntimeAssets)
            {
                var type = AssetBundleEditorToolsV2.GetAssetType(asset);
                var idx = asset.IndexOf(AssetBundleEditorToolsV2.RUNTIME_PATH, StringComparison.OrdinalIgnoreCase);
                var runtimePath = asset.Substring(idx + AssetBundleEditorToolsV2.RUNTIME_PATH.Length);
                runtimePath = runtimePath.Replace(Path.GetExtension(runtimePath), "");
                runtimePath = runtimePath.Replace("\\", "/");
                //Debug.Log("【LoadTest】:" + runtimePath);

                switch (type)
                {
                    case AssetBundleItem.AssetTypeEnum.Prefab:
                    {
                        //加载
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        var obj = AssetBundleLoader.Load<GameObject>(runtimePath);
                        sw.Stop();
                        var loadtime = sw.ElapsedTicks / 10000f;
                        //实例化
                        sw.Restart();
                        var gobj = GameObject.Instantiate(obj);

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

                        sw.Stop();
                        var instantTime = sw.ElapsedTicks / 10000f;
                        UnityEngine.Debug.LogFormat("<color=yellow>【LoadTest】:{0}</color> <color=green>【加载耗时】:{1}ms;【初始化耗时】:{2}ms</color>", runtimePath, loadtime, instantTime);

                        //抓屏

                        var outpng = string.Format("{0}/{1}_ab.png", outpath, runtimePath.Replace("/", "_"));
                        UnityEngine.Debug.Log(outpng);
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
                        break;
                    case AssetBundleItem.AssetTypeEnum.TextAsset:
                    {
                        //测试打印AssetText资源
                        var textAsset = BResources.Load<TextAsset>(runtimePath);
                        UnityEngine.Debug.Log(textAsset.text);
                    }
                        break;
                }

                yield return null;
            }

            EditorUtility.RevealInFinder(outpath);
        }
    }
}
