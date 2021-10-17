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
        static private GameObject UI_ROOT;
        static private GameObject SCENE_ROOT;
        static private DevResourceMgr DevLoder;
        static private AssetBundleMgrV2 AssetBundleLoader;
        static private Camera Camera;
        static private EditorWindow GameView;

        /// <summary>
        /// 测试加载所有的AssetBundle
        /// </summary>
        static public void TestLoadAssetbundle(string abPath)
        {
            //打开场景
            EditorSceneManager.OpenScene("Assets/Scenes/AssetBundleTest.unity");
            //初始化加载环境
            UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
            BResources.Load(AssetLoadPath.StreamingAsset, abPath);
            //dev加载器
            DevLoder = new DevResourceMgr();
            DevLoder.Init("");
            AssetBundleLoader = new AssetBundleMgrV2();
            AssetBundleLoader.Init(Application.streamingAssetsPath);
            //节点
            UI_ROOT = GameObject.Find("UIRoot");
            SCENE_ROOT = GameObject.Find("3dRoot");
            //相机
            Camera = GameObject.Find("Camera").GetComponent<Camera>();
            Camera.cullingMask = -1;
            Camera.gameObject.hideFlags = HideFlags.DontSave;
            //获取gameview
            var assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type GameViewType = assembly.GetType("UnityEditor.GameView");
            GameView = EditorWindow.GetWindow(GameViewType);

            //开始加载
            var croutine = EditorCoroutineExtensions.StartCoroutine(IE_LoadAll(), new object());
            EditorCoroutineExtensions.StopCoroutine(IE_LoadAll(), new object());
        }


        /// <summary>
        /// 加载所有assetbundle
        /// </summary>
        /// <returns></returns>
        static IEnumerator IE_LoadAll()
        {
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
                        sw.Stop();
                        var instantTime = sw.ElapsedTicks / 10000f;
                        UnityEngine.Debug.LogFormat("<color=yellow>【LoadTest】:{0}</color> <color=green>【加载耗时】:{1}ms;【初始化耗时】:{2}ms</color>", runtimePath, loadtime, instantTime);

                        //抓屏
                        var outpath = BDApplication.BDEditorCachePath + "/AssetBundle";
                        var outpng = string.Format("{0}/{1}_ab.png", outpath, runtimePath.Replace("/", "_"));
                        UnityEngine.Debug.Log(outpng);
                         yield return null;
                        //渲染
                         GameView.Repaint();
                         yield return null;
                        //抓屏
                        CaptureCamera(Camera, new Rect(0, 0, Screen.width, Screen.height), outpng);
                        //删除
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
        }


        /// <summary>
        /// 对相机截图。 
        /// </summary>
        /// <returns>The screenshot2.</returns>
        /// <param name="camera">Camera.要被截屏的相机</param>
        /// <param name="rect">Rect.截屏的区域</param>
        static Texture2D CaptureCamera(Camera camera, Rect rect, string outPath)
        {
            // 创建一个RenderTexture对象
            RenderTexture rt = new RenderTexture((int) rect.width, (int) rect.height, 0);
            // 临时设置相关相机的targetTexture为rt, 并手动渲染相关相机
            camera.targetTexture = rt;
            camera.Render();

            // 激活这个rt, 并从中中读取像素。
            RenderTexture.active = rt;
            Texture2D screenShot = new Texture2D((int) rect.width, (int) rect.height, TextureFormat.RGB24, false);
            screenShot.ReadPixels(rect, 0, 0); // 注：这个时候，它是从RenderTexture.active中读取像素
            screenShot.Apply();

            // 重置相关参数，以使用camera继续在屏幕上显示
            camera.targetTexture = null;
            //ps: camera2.targetTexture = null;
            RenderTexture.active = null; // JC: added to avoid errors
            GameObject.DestroyImmediate(rt);
            // 最后将这些纹理数据，成一个png图片文件
            byte[] bytes = screenShot.EncodeToPNG();
            FileHelper.WriteAllBytes(outPath, bytes);

            return screenShot;
        }
    }
}
