using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BDFramework;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using Cysharp.Text;
using DotNetExtension;
using LitJson;
using UnityEngine;
using UnityEngine.Profiling;
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

    static private Camera Camera;

    // static private EditorWindow     GameView;
    private static string BenchmarkResultPath;

    //
    static private Image imageNode;
    static private SpriteRenderer spriteRendererNode;

    // readonly static public GUIStyle LabelH4 = new GUIStyle()
    // {
    //     fontSize = 15,
    //     normal = new GUIStyleState()
    //     {
    //         textColor = Color.red
    //     }
    // };

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        {
            GUI.skin.button.fontSize = 30;
            //设置AUPlevel
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("设置AUP:Low", GUILayout.Height(50), GUILayout.Width(300)))
            {
                BResources.SetAUPLEvel(BResources.AUPLevel.Low);
                Debug.Log("Set async upload pipeline : low");
            }

            if (GUILayout.Button("设置AUP: Normal", GUILayout.Height(50), GUILayout.Width(300)))
            {
                BResources.SetAUPLEvel(BResources.AUPLevel.Normal);
                Debug.Log("Set async upload pipeline : normal");
            }

            if (GUILayout.Button("设置AUP: Hight", GUILayout.Height(50), GUILayout.Width(300)))
            {
                BResources.SetAUPLEvel(BResources.AUPLevel.Hight);
                Debug.Log("Set async upload pipeline : hight");
            }

            GUILayout.EndHorizontal();
            //同步加载
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("同步加载 测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                this.StartCoroutine(IE_01_LoadAll(false, RuntimePlatform.Android));
            }

            if (GUILayout.Button("卸载所有", GUILayout.Height(100), GUILayout.Width(150)))
            {
                BResources.UnloadAll();
            }

            GUILayout.EndHorizontal();

            //异步加载
            if (GUILayout.Button("异步加载 测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                this.StartCoroutine(IE_01_LoadAll(true, RuntimePlatform.Android));
            }

            if (GUILayout.Button("随机加载 测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                Profiler.BeginSample("Benchmark  Random Load");
                Profiler.EndSample();
            }
        }
        GUILayout.EndVertical();
    }

    private void Start()
    {
        this.Init();
    }


    /// <summary>
    /// 初始化
    /// </summary>
    private void Init()
    {
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
    static IEnumerator IE_01_LoadAll(bool isAsyncLoad, RuntimePlatform platform)
    {
        //dev加载器
        var abPath = Application.isEditor ? BApplication.DevOpsPublishAssetsPath : Application.persistentDataPath;
        BResources.InitLoadAssetBundleEnv(abPath, platform);
        BResources.ResLoader.WarmUpShaders();

        Profiler.BeginSample("Benchmark Load");
        var benchmarkDataOutpath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, "BenchMark", BApplication.GetPlatformPath(platform), DateTimeEx.GetTotalSeconds().ToString());
        if (!Directory.Exists(benchmarkDataOutpath))
        {
            Directory.CreateDirectory(benchmarkDataOutpath);
        }

        loadDataMap.Clear();
        var AssetBundleLoader = BResources.ResLoader as AssetBundleMgrV2;
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
            Debug.Log($"<color=yellow>【LoadTest】</color>: {runtimePath} ");
            Debug.Log("create task frame:" + Time.frameCount);
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
                    var loadTask = BResources.AsyncLoad<GameObject>(runtimePath);
                    yield return loadTask;
                    if (loadTask.IsSuccess)
                    {
                        obj = loadTask.GetResult<GameObject>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<GameObject>(runtimePath);
                }
                Debug.Log("end task frame:" + Time.frameCount);
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
                    var outpng = string.Format("{0}/{1}_grabframe.png", benchmarkDataOutpath, runtimePath.Replace("/", "_"));
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
                    var ret = BResources.AsyncLoad<TextAsset>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<TextAsset>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<TextAsset>(runtimePath);
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
                    var ret = BResources.AsyncLoad<Texture>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Texture>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Texture>(runtimePath);
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
                    var ret = BResources.AsyncLoad<Texture2D>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Texture2D>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Texture2D>(runtimePath);
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
                    var ret = BResources.AsyncLoad<Sprite>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Sprite>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Sprite>(runtimePath);
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
                    var ret = BResources.AsyncLoad<Material>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Material>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Material>(runtimePath);
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
                var obj = BResources.Load<Shader>(runtimePath);
                //异步
                if (isAsyncLoad)
                {
                    var ret = BResources.AsyncLoad<Shader>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Shader>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Shader>(runtimePath);
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
                    var ret = BResources.AsyncLoad<AudioClip>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<AudioClip>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<AudioClip>(runtimePath);
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
                    var ret = BResources.AsyncLoad<AnimationClip>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<AnimationClip>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<AnimationClip>(runtimePath);
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
                    var ret = BResources.AsyncLoad<Mesh>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Mesh>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Mesh>(runtimePath);
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
                    var ret = BResources.AsyncLoad<Font>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Font>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Font>(runtimePath);
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
                    var ret = BResources.AsyncLoad<SpriteAtlas>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<SpriteAtlas>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<SpriteAtlas>(runtimePath);
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
                    var ret = BResources.AsyncLoad<ShaderVariantCollection>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<ShaderVariantCollection>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<ShaderVariantCollection>(runtimePath);
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
                    var ret = BResources.AsyncLoad<Object>(runtimePath);
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<Object>();
                    }
                }
                //同步
                else
                {
                    obj = BResources.Load<Object>(runtimePath);
                }

                sw.Stop();
                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Object】加载失败:" + runtimePath);
                }

                UnityEngine.Debug.LogError("待编写测试! -" + typeName);
            }

            //打印

            Debug.LogFormat("<color=yellow>【LoadEnd】</color> {0} <color=green>【加载】:<color=yellow>{1}ms</color>;【Clone】:<color=yellow>{2}ms</color> </color> [{3}]", loadData.LoadPath, loadData.LoadTime / 10000f, loadData.InstanceTime / 10000f,Time.frameCount);
            yield return null;
        }


        Profiler.EndSample();
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
        BenchmarkResultPath = ZString.Format("", BApplication.DevOpsPublishAssetsPath) + "/Benchmark/AssetBundleTest01.json";
        var content = JsonMapper.ToJson(loadDataMap);
        FileHelper.WriteAllText(BenchmarkResultPath, content);

        yield return null;

// #if UNITY_EDITOR
//         EditorUtility.RevealInFinder(outpath);
// #endif
    }
}
