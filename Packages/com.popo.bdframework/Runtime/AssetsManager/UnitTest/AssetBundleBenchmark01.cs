using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BDFramework;
using BDFramework.Asset;
using BDFramework.Core.Tools;
using BDFramework.ResourceMgr;
using BDFramework.ResourceMgr.V2;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using DotNetExtension;
using LitJson;
using ServiceStack.Text;
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

    static public GUIStyle LabelH4;

    //参数
    private BResources.AUPLevel curAupLevel = BResources.AUPLevel.Low;
    private RuntimePlatform curRuntimePlatform = RuntimePlatform.Android;

    private void OnGUI()
    {
        if (LabelH4 == null)
        {
            LabelH4 = new GUIStyle()
            {
                fontSize = 30,
                normal = new GUIStyleState()
                {
                    textColor = Color.green
                }
            };
        }

        GUILayout.BeginVertical();
        {
            GUI.skin.button.fontSize = 30;

            //设置AUPlevel
            GUILayout.BeginHorizontal();
            GUILayout.Label("设置 AUP:", LabelH4);

            if (curAupLevel == BResources.AUPLevel.Low) GUI.color = Color.red;
            if (GUILayout.Button("Low", GUILayout.Height(50), GUILayout.Width(200)))
            {
                curAupLevel = BResources.AUPLevel.Low;
            }

            GUI.color = GUI.backgroundColor;

            if (curAupLevel == BResources.AUPLevel.Normal) GUI.color = Color.red;
            if (GUILayout.Button("Normal", GUILayout.Height(50), GUILayout.Width(200)))
            {
                curAupLevel = BResources.AUPLevel.Normal;
            }

            GUI.color = GUI.backgroundColor;

            if (curAupLevel == BResources.AUPLevel.Height) GUI.color = Color.red;
            if (GUILayout.Button("Height", GUILayout.Height(50), GUILayout.Width(200)))
            {
                curAupLevel = BResources.AUPLevel.Height;
            }

            GUI.color = GUI.backgroundColor;

            GUILayout.EndHorizontal();
            //平台
            GUILayout.BeginHorizontal();
            GUILayout.Label("加载平台:", LabelH4);

            if (curRuntimePlatform == RuntimePlatform.Android) GUI.color = Color.red;
            if (GUILayout.Button("Android", GUILayout.Height(50), GUILayout.Width(150)))
            {
                curRuntimePlatform = RuntimePlatform.Android;
            }

            GUI.color = GUI.backgroundColor;

            if (curRuntimePlatform == RuntimePlatform.IPhonePlayer) GUI.color = Color.red;
            if (GUILayout.Button("iOS", GUILayout.Height(50), GUILayout.Width(150)))
            {
                curRuntimePlatform = RuntimePlatform.IPhonePlayer;
            }

            GUI.color = GUI.backgroundColor;

            if (curRuntimePlatform == RuntimePlatform.WindowsPlayer) GUI.color = Color.red;
            if (GUILayout.Button("Windows", GUILayout.Height(50), GUILayout.Width(150)))
            {
                curRuntimePlatform = RuntimePlatform.WindowsPlayer;
            }

            GUI.color = GUI.backgroundColor;

            if (curRuntimePlatform == RuntimePlatform.OSXPlayer) GUI.color = Color.red;
            if (GUILayout.Button("OSX", GUILayout.Height(50), GUILayout.Width(150)))
            {
                curRuntimePlatform = RuntimePlatform.OSXPlayer;
            }

            GUI.color = GUI.backgroundColor;

            GUILayout.EndHorizontal();
            //同步加载
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("同步加载 测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                this.StartCoroutine(IE_01_LoadAll(false, curRuntimePlatform));
            }

            if (GUILayout.Button("卸载所有", GUILayout.Height(100), GUILayout.Width(150)))
            {
                isFirstLoad = true;
                BResources.UnloadAll();
            }

            GUILayout.EndHorizontal();

            //异步加载
            if (GUILayout.Button("异步加载 测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                this.StartCoroutine(IE_01_LoadAll(true, curRuntimePlatform));
            }

            //await加载
            if (GUILayout.Button("await加载 测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                IE_02_LoadAll(true, curRuntimePlatform);
            }

            //同异步稳定性测试
            if (GUILayout.Button("同、异步混合测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                this.StartCoroutine(TestLoadTask(curRuntimePlatform,false));
            }
            
            if (GUILayout.Button("同、异步取消测试", GUILayout.Height(100), GUILayout.Width(300)))
            {
                //取消异步任务
                this.StartCoroutine(TestLoadTask(curRuntimePlatform,true));
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
        /// <summary>
        /// load路径
        /// </summary>
        public string LoadPath { get; set; }

        /// <summary>
        /// 异步加载耗时
        /// </summary>
        public float AsyncLoadTime { get; set; }

        /// <summary>
        /// 同步加载耗时
        /// </summary>
        public float LoadTime { get; set; }

        /// <summary>
        /// 初始化耗时
        /// </summary>
        public float InstanceTime { get; set; }
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private static Dictionary<string, List<LoadTimeData>> loadDataMap = new Dictionary<string, List<LoadTimeData>>();

    private List<LoadTimeData> loadList = new List<LoadTimeData>();

    private bool isFirstLoad = true;

    /// <summary>
    /// 加载所有assetbundle
    /// </summary>
    /// <returns></returns>
    IEnumerator IE_01_LoadAll(bool isAsyncLoad, RuntimePlatform platform)
    {
        //加载
        BResources.SetAUPLEvel(curAupLevel);
        var abPath = Application.isEditor ? BApplication.DevOpsPublishAssetsPath : Application.persistentDataPath;
        BResources.InitLoadAssetBundleEnv(abPath, platform);
        BResources.ResLoader.WarmUpShaders();

        //package信息
        var pakInfo = GlobalAssetsHelper.GetPackageBuildInfo(abPath, platform);
       // Profiler.BeginSample("Benchmark Load");
        var benchmarkDataOutpath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, "Benchmark", BApplication.GetPlatformPath(platform), pakInfo.Version);
        if (!Directory.Exists(benchmarkDataOutpath))
        {
            Directory.CreateDirectory(benchmarkDataOutpath);
        }

        loadDataMap.Clear();
        loadList.Clear();
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
            Debug.Log($"Create task frame: <color=red>{Time.frameCount} </color>");
            // if (!loadDataMap.ContainsKey(typeName))
            // {
            //     loadDataMap[typeName] = new List<LoadTimeData>();
            // }
            //
            // var loadList = loadDataMap[typeName];
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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<GameObject>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }

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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<TextAsset>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }


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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Texture>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }

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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Texture2D>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }

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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Sprite>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }

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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Material>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }


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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Shader>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }


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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<AudioClip>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }


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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<AnimationClip>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }


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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Mesh>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }


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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Font>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }

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
                    //
                    yield return ret;
                    if (ret.IsSuccess)
                    {
                        obj = ret.GetResult<SpriteAtlas>();
                    }
                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<SpriteAtlas>(runtimePath);
                    sw.Stop();

                    loadData.LoadTime = sw.ElapsedTicks;
                }
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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<ShaderVariantCollection>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }

                obj?.WarmUp();

                if (!obj)
                {
                    UnityEngine.Debug.LogError("【ShaderVariantCollection】加载失败:" + runtimePath);
                }
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

                    sw.Stop();
                    loadData.AsyncLoadTime = sw.ElapsedTicks;
                    Debug.Log($"End task frame:<color=red>{Time.frameCount} </color>");
                }
                //同步
                else
                {
                    obj = BResources.Load<Object>(runtimePath);
                    sw.Stop();
                    loadData.LoadTime = sw.ElapsedTicks;
                }


                if (!obj)
                {
                    UnityEngine.Debug.LogError("【Object】加载失败:" + runtimePath);
                }

                UnityEngine.Debug.LogError("待编写测试! -" + typeName);
            }

            //打印

            var time = loadData.LoadTime > 0 ? loadData.LoadTime : loadData.AsyncLoadTime;
            Debug.LogFormat("<color=yellow>【LoadEnd】</color> {0} <color=green>【加载】:<color=yellow>{1}ms</color>;【Clone】:<color=yellow>{2}ms</color>  <color=red> [{3}] </color> </color> ",
                "", time / 10000f, loadData.InstanceTime / 10000f, Time.frameCount);

            yield return null;
        }


        Profiler.EndSample();
        yield return null;

        //
        if (isFirstLoad)
        {
            if (isAsyncLoad)
            {
                BenchmarkResultPath = benchmarkDataOutpath + "/Result_Async.csv";
            }
            else
            {
                BenchmarkResultPath = benchmarkDataOutpath + "/Result.csv";
            }

            foreach (var load in loadList)
            {
                load.LoadTime = load.LoadTime / 10000f;
                load.AsyncLoadTime = load.AsyncLoadTime / 10000f;
                load.InstanceTime = load.InstanceTime / 10000f;
            }

            //
            var content = CsvSerializer.SerializeToCsv(loadList);
            FileHelper.WriteAllText(BenchmarkResultPath, content);
            isFirstLoad = false;
        }


        yield return null;

// #if UNITY_EDITOR
//         EditorUtility.RevealInFinder(outpath);
// #endif
    }

    /// <summary>
    /// 加载所有assetbundle
    /// </summary>
    /// <returns></returns>
    async static void IE_02_LoadAll(bool isAsyncLoad, RuntimePlatform platform)
    {
        //加载器
        var abPath = Application.isEditor ? BApplication.DevOpsPublishAssetsPath : Application.persistentDataPath;
        BResources.SetAUPLEvel(BResources.AUPLevel.Low);
        BResources.InitLoadAssetBundleEnv(abPath, platform);
        BResources.ResLoader.WarmUpShaders();
        //资源信息
        var pakInfo = GlobalAssetsHelper.GetPackageBuildInfo(abPath, platform);

        Profiler.BeginSample("Benchmark Load");
        var benchmarkDataOutpath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, "BenchMark", BApplication.GetPlatformPath(platform), pakInfo.Version);
        //
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
                    await loadTask;
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

                    //
                    await UniTask.Yield();
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
                    await ret;
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
                    await ret;
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
                    await ret;
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
                    await UniTask.Yield();
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
                    await ret;
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
                    await UniTask.Yield();
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
                    await ret;
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
                    await ret;
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
                    await ret;
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
                    await ret;
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
                    await ret;
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
                    await ret;
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
                    await ret;
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
                    await ret;
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
                    await ret;
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

            Debug.LogFormat("<color=yellow>【LoadEnd】</color> {0} <color=green>【加载】:<color=yellow>{1}ms</color>;【Clone】:<color=yellow>{2}ms</color> </color> [{3}]", loadData.LoadPath, loadData.LoadTime / 10000f, loadData.InstanceTime / 10000f,
                Time.frameCount);
            await UniTask.Yield();
        }


        Profiler.EndSample();
        await UniTask.Yield();

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

// #if UNITY_EDITOR
//         EditorUtility.RevealInFinder(outpath);
// #endif
    }


    /// <summary>
    /// 随机加载测试
    /// </summary>
    /// <returns></returns>
    IEnumerator TestLoadTask(RuntimePlatform platform ,bool doCancelAysncTask)
    {
        BResources.UnloadAssets();
        //加载
        BResources.SetAUPLEvel(curAupLevel);
        var abPath = Application.isEditor ? BApplication.DevOpsPublishAssetsPath : Application.persistentDataPath;
        BResources.InitLoadAssetBundleEnv(abPath, platform);
        BResources.ResLoader.WarmUpShaders();

        //package信息
        var pakInfo = GlobalAssetsHelper.GetPackageBuildInfo(abPath, platform);

        var benchmarkDataOutpath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, "Benchmark", BApplication.GetPlatformPath(platform), pakInfo.Version);
        if (!Directory.Exists(benchmarkDataOutpath))
        {
            Directory.CreateDirectory(benchmarkDataOutpath);
        }

        loadDataMap.Clear();
        loadList.Clear();
        var AssetBundleLoader = BResources.ResLoader as AssetBundleMgrV2;
        //加载
        int idx = 0;
        foreach (var assetdata in AssetBundleLoader.AssetConfigLoder.AssetbundleItemList)
        {
            idx++;
            if (string.IsNullOrEmpty(assetdata.LoadPath))
            {
                continue;
            }

            var typeName = AssetBundleLoader.AssetConfigLoder.AssetTypes.AssetTypeList[assetdata.AssetType];
            var runtimePath = assetdata.LoadPath;

            bool isCancel = false;
            bool isLoad = false;
            bool isCacheHit = false;
            var asyncTaskStartFrameCount = Time.frameCount;
            Debug.Log($"<color=yellow>------>【开始加载】{idx} - </color>{runtimePath}  <color=red>帧号:{asyncTaskStartFrameCount}</color>");
            //开始异步
            Stopwatch swa = new Stopwatch();
            swa.Start();
            var id = BResources.AsyncLoad<Object>(runtimePath, (o) =>
            {
                swa.Stop();

                //立刻完成,就是命中缓存了
                if (asyncTaskStartFrameCount == Time.frameCount)
                {
                    isCacheHit = true;
                }
                //其他的
                if (isCancel)
                {
                    Debug.LogError($"取消失败,异步加载成功:{runtimePath} <color=red>帧号:{Time.frameCount}</color>");
                }
                else
                {
                    Debug.Log($"<color=yellow>【异步加载成功】</color>  {runtimePath} <color=yellow>{swa.ElapsedTicks / 10000f}</color>ms <color=red>帧号:{Time.frameCount}</color>");
                }
                isLoad = true;
            });

            // yield return null;
            //取消任务
            if (doCancelAysncTask)
            {
                var ret = BResources.LoadCancel(id);
                isCancel = true;
                if (ret)
                {
                    Debug.Log($"<color=green>取消任务id:{id}成功!</color>");
                }
                else
                {
                    Debug.Log($"<color=red>取消任务id:{id}失败!</color>");
                }
            }
            //已经加载完成,且没有cache命中
            if (isLoad&& !isCacheHit)
            {
                Debug.LogError($"同步异常,异步已经加载:{runtimePath} <color=red>帧号:{Time.frameCount}</color>");
            }
            
            //同步加载
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var obj = BResources.Load<Object>(runtimePath);
            isLoad = true;
            sw.Stop();
            if (obj)
            {
                Debug.Log($"<color=yellow>【同步加载成功】</color>  {runtimePath} <color=yellow> {sw.ElapsedTicks / 10000f}ms </color>  <color=red>帧号:{Time.frameCount}</color>");
            }

            //测试资产
            yield return this.StartCoroutine(ValidAsset(runtimePath, obj));

            //
            yield return null;
        }
    }


    
    /// <summary>
    /// 验证资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    IEnumerator ValidAsset(string path, Object obj)
    {
        //各种测试
        if (obj is GameObject gobj)
        {
            if (!gobj)
            {
                Debug.LogError("[Prefab]加载失败:" + path);
                yield break;
            }

            var initGo = GameObject.Instantiate(gobj);
            //UI
            var rectTransform = initGo.GetComponentInChildren<RectTransform>();
            if (rectTransform != null)
            {
                initGo.transform.SetParent(UI_ROOT, false);
            }
            else
            {
                initGo.transform.SetParent(SCENE_ROOT);
            }

            initGo.SetActive(true);

            yield return null;
            yield return null;

            GameObject.DestroyImmediate(initGo);
        }
        else if (obj is Texture2D tex2d)
        {
            if (!tex2d)
            {
                Debug.LogError("[Texture2D]加载失败:" + path);
                yield break;
            }

            spriteRendererNode.gameObject.SetActive(true);
            spriteRendererNode.sprite = Sprite.Create(tex2d, new Rect(Vector2.zero, tex2d.texelSize), new Vector2(0.5f, 0.5f), 128);
            yield return null;
            spriteRendererNode.gameObject.SetActive(false);
        }
        else if (obj is Texture tex)
        {
            if (!tex)
            {
                Debug.LogError("[Texture]加载失败:" + path);
                yield break;
            }
        }
        else if (obj is Sprite sp)
        {
            if (!sp)
            {
                Debug.LogError("[Sprite]加载失败:" + path);
                yield break;
            }


            imageNode.gameObject.SetActive(true);
            imageNode.overrideSprite = sp;
            imageNode.SetNativeSize();
            yield return null;
            imageNode.gameObject.SetActive(false);
        }
        else if (obj is TextAsset textAsset)
        {
            if (!textAsset)
            {
                Debug.LogError("[TextAsset]加载失败:" + path);
                yield break;
            }

            Debug.Log(textAsset);
        }
        else if (obj is Font font)
        {
            if (!font)
            {
                Debug.LogError("[Font]加载失败:" + path);
                yield break;
            }
        }
        else if (obj is Material mat)
        {
            if (!mat)
            {
                Debug.LogError("[Material]加载失败:" + path);
                yield break;
            }
        }
        else if (obj is Shader shader)
        {
            if (!shader)
            {
                Debug.LogError("[Shader]加载失败:" + path);
                yield break;
            }
        }
        else if (obj is Mesh mesh)
        {
            if (!mesh)
            {
                Debug.LogError("[Mesh]加载失败:" + path);
                yield break;
            }
        }
        else if (obj is AudioClip audioclip)
        {
            if (!audioclip)
            {
                Debug.LogError("[AudioClip]加载失败:" + path);
                yield break;
            }
        }
        else if (obj is ShaderVariantCollection svc)
        {
            if (!svc)
            {
                Debug.LogError("[ShaderVariantCollection]加载失败:" + path);
                yield break;
            }

            svc.WarmUp();
        }
        else
        {
            if (!obj)
            {
                Debug.LogError("加载失败:" + path);
            }
        }
    }
}
