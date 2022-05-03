using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr.V2;
using LitJson;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


/// <summary>
/// AssetBundle测试
/// </summary>
public class AssetBundleBenchmark01 : MonoBehaviour
{
    /// <summary>
    /// 是否为异步加载
    /// </summary>
    public bool IsAsyncLoad = false;

    /// <summary>
    /// 资源分组
    /// </summary>
    public Dictionary<string, List<string>> AssetGroup = new Dictionary<string, List<string>>();


    static private Transform UI_ROOT;

    static private Transform SCENE_ROOT;

    // static private DevResourceMgr   DevLoder;
    static private AssetBundleMgrV2 AssetBundleLoader;

    static private Camera Camera;

    // static private EditorWindow     GameView;
    private static string BenchmarkResultPath;

    //
    static private Image imageNode;
    static private SpriteRenderer spriteRendererNode;

    private void Start()
    {
        BenchmarkResultPath = Application.persistentDataPath + "/Benchmark/AssetBundleTest01.json";
        this.Init();
        this.StartCoroutine(IE_01_LoadAll(IsAsyncLoad));
    }


    /// <summary>
    /// 初始化
    /// </summary>
    private void Init()
    {
        //初始化加载环境
        UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
        //dev加载器
        // DevLoder = new DevResourceMgr();
        // DevLoder.Init("");
        AssetBundleLoader = new AssetBundleMgrV2();
        var abPath = Application.isEditor ? BApplication.DevOpsPublishAssetsPath : Application.persistentDataPath;
        AssetBundleLoader.Init(abPath);
        AssetBundleLoader.WarmUpShaders();
        //节点
        UI_ROOT = GameObject.Find("UIRoot").transform;
        SCENE_ROOT = GameObject.Find("3dRoot").transform;
        imageNode = UI_ROOT.transform.GetComponentInChildren<Image>();
        spriteRendererNode = SCENE_ROOT.transform.GetComponentInChildren<SpriteRenderer>();
        imageNode.gameObject.SetActive(false);
        spriteRendererNode.gameObject.SetActive(false);
        //相机
        Camera = GameObject.Find("Camera").GetComponent<Camera>();
        Camera.cullingMask = -1;
        Camera.gameObject.hideFlags = HideFlags.DontSave;
        //获取gameview
        //  var assembly = typeof(UnityEditor.EditorWindow).Assembly;
        // System.Type GameViewType = assembly.GetType("UnityEditor.GameView");
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
    static IEnumerator IE_01_LoadAll(bool isAsyncLoad = false)
    {
        var outpath = BApplication.BDEditorCachePath + "/AssetBundle";
        if (!Directory.Exists(outpath))
        {
            Directory.CreateDirectory(outpath);
        }

        loadDataMap.Clear();
        //加载
        foreach (var assetdata in AssetBundleLoader.AssetConfigLoder.AssetbundleItemList)
        {
            if (string.IsNullOrEmpty(assetdata.LoadPath))
            {
                continue;
            }

            var typeName = AssetBundleLoader.AssetConfigLoder.AssetTypes.AssetTypeList[assetdata.AssetType];
            var runtimePath = assetdata.LoadPath;
            //加载
            //Debug.Log("【LoadTest】:" + runtimePath);
            if (!loadDataMap.ContainsKey(typeName))
            {
                loadDataMap[typeName] = new List<LoadTimeData>();
            }

            var loadList = loadDataMap[typeName];
            //
            var loadData = new LoadTimeData();
            loadData.LoadPath = runtimePath;
            loadList.Add(loadData);
            //计时器
            Stopwatch sw = new Stopwatch();
            if (typeName == typeof(GameObject).FullName)
            {
                //加载
                sw.Start();
                GameObject obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<GameObject>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<GameObject>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<GameObject>(runtimePath);
                }

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
                    // GameView.Repaint();
                    // GameView.Focus();

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
            else if (typeName == typeof(TextAsset).FullName)
            {
                //测试打印AssetText资源
                sw.Start();
                TextAsset obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<TextAsset>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<TextAsset>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<TextAsset>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【TextAsset】加载失败:" + runtimePath);
                }
                else
                {
                    UnityEngine.Debug.Log(obj.text);
                }
            }
            else if (typeName == typeof(Texture).FullName)
            {
                sw.Start();
                Texture obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Texture>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Texture>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Texture>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Texture】加载失败:" + runtimePath);
                }

                break;
            }
            else if (typeName == typeof(Texture2D).FullName)
            {
                sw.Start();
                Texture2D obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Texture2D>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Texture2D>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Texture2D>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Texture2D】加载失败:" + runtimePath);
                }
                else
                {
                    spriteRendererNode.gameObject.SetActive(true);
                    spriteRendererNode.sprite = Sprite.Create(obj, new Rect(Vector2.zero, obj.texelSize), new Vector2(0.5f, 0.5f), 128);
                    yield return null;
                    spriteRendererNode.gameObject.SetActive(false);
                }
            }
            else if (typeName == typeof(Sprite).FullName)
            {
                sw.Start();
                Sprite obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Sprite>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Sprite>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Sprite>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Sprite】加载失败:" + runtimePath);
                }
                else
                {
                    imageNode.gameObject.SetActive(true);
                    imageNode.overrideSprite = obj;
                    imageNode.SetNativeSize();
                    yield return null;
                    imageNode.gameObject.SetActive(false);
                }
            }
            else if (typeName == typeof(Material).FullName)
            {
                sw.Start();
                Material obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Material>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Material>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Material>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Material】加载失败:" + runtimePath);
                }
            }
            else if (typeName == typeof(Shader).FullName)
            {
                sw.Start();
                var obj = AssetBundleLoader.Load<Shader>(runtimePath);
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Shader>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Shader>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Shader>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Shader】加载失败:" + runtimePath);
                }
            }
            else if (typeName == typeof(AudioClip).FullName)
            {
                sw.Start();
                AudioClip obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<AudioClip>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<AudioClip>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<AudioClip>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【AudioClip】加载失败:" + runtimePath);
                }
            }
            else if (typeName == typeof(AnimationClip).FullName)
            {
                sw.Start();
                AnimationClip obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<AnimationClip>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<AnimationClip>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<AnimationClip>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【AnimationClip】加载失败:" + runtimePath);
                }
            }
            else if (typeName == typeof(Mesh).FullName)
            {
                sw.Start();
                Mesh obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Mesh>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Mesh>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Mesh>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Mesh】加载失败:" + runtimePath);
                }
            }
            else if (typeName == typeof(Font).FullName)
            {
                sw.Start();
                Font obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Font>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Font>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Font>(runtimePath);
                }

                sw.Stop();
                loadData.LoadTime = sw.ElapsedTicks;
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Font】加载失败:" + runtimePath);
                }
            }
            else if (typeName == typeof(SpriteAtlas).FullName)
            {
                sw.Start();

                SpriteAtlas obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<SpriteAtlas>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<SpriteAtlas>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<SpriteAtlas>(runtimePath);
                }

                sw.Stop();
                if (!obj)
                {
                  //  UnityEngine.Debug.LogError("【SpriteAtlas】加载失败:" + runtimePath);
                }

                loadData.LoadTime = sw.ElapsedTicks;
            }
            else if (typeName == typeof(ShaderVariantCollection).FullName)
            {
                sw.Start();

                ShaderVariantCollection obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<ShaderVariantCollection>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<ShaderVariantCollection>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<ShaderVariantCollection>(runtimePath);
                }

                obj?.WarmUp();
                sw.Stop();
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【ShaderVariantCollection】加载失败:" + runtimePath);
                }

                loadData.LoadTime = sw.ElapsedTicks;
            }
            else
            {
                sw.Start();
                Object obj = null;
                //异步
                if (isAsyncLoad)
                {
                    var ret = AssetBundleLoader.CreateAsyncLoadTask<Object>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetAssetBundleInstance<Object>();
                    }
                }
                //同步
                else
                {
                    obj = AssetBundleLoader.Load<Object>(runtimePath);
                }
                sw.Stop();
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Object】加载失败:" + runtimePath);
                }

                UnityEngine.Debug.LogError("待编写测试! -" + typeName);
            }

            //打印

            Debug.LogFormat("<color=yellow>{0}</color> <color=green>【加载】:<color=yellow>{1}ms</color>;【初始化】:<color=yellow>{2}ms</color> </color>", loadData.LoadPath, loadData.LoadTime / 10000f, loadData.InstanceTime / 10000f);
            yield return null;
        }

        yield return null;

        // foreach (var item in loadDataMap)
        // {
        //     Debug.Log("<color=red>【" + item.Key + "】</color>");
        //     foreach (var ld in item.Value)
        //     {
        //         Debug.LogFormat("<color=yellow>{0}</color> <color=green>【加载】:<color=yellow>{1}ms</color>;【初始化】:<color=yellow>{2}ms</color> </color>", ld.LoadPath, ld.LoadTime / 10000f, ld.InstanceTime / 10000f);
        //     }
        // }

        //
        var content = JsonMapper.ToJson(loadDataMap);
        FileHelper.WriteAllText(BenchmarkResultPath, content);

        yield return null;

// #if UNITY_EDITOR
//         EditorUtility.RevealInFinder(outpath);
// #endif
    }
}
