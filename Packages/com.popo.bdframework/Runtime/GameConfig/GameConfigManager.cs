using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Mgr;
using LitJson;
using UnityEngine;

namespace BDFramework.Configure
{
    /// <summary>
    /// 配置处理器排序属性。
    /// Ordering attribute for configuration processors.
    /// 该属性延续 ManagerAttribute 的排序机制，让配置处理器按显式 tag 顺序依次执行。
    /// This attribute extends the ManagerAttribute ordering mechanism so configuration processors run in explicit tag order.
    /// </summary>
    public class GameConfigAttribute : ManagerAttribute
    {
        public string Title = "";

        public GameConfigAttribute(int intTag, string tile) : base(intTag)
        {
            Title = tile;
        }

        public GameConfigAttribute(string tag) : base(tag)
        {
        }
    }

    /// <summary>
    /// 游戏配置中心。
    /// Game configuration center.
    /// 该管理器负责解析框架配置文本、实例化配置处理器并缓存运行期配置对象，
    /// 是运行时启动与编辑器初始化共同依赖的配置装载入口。
    /// This manager parses framework configuration text, instantiates configuration processors, and caches runtime configuration objects,
    /// serving as the shared configuration-loading entry for both runtime startup and editor initialization.
    /// </summary>
    public class GameConfigManager : ManagerBase<GameConfigManager, GameConfigAttribute>
    {
        /// <summary>
        /// 编辑器默认配置文件路径常量。
        /// Constant path for the editor default configuration file.
        /// Runtime 程序集不能在玩家构建中直接依赖 Editor-only 的 ConfigEditorUtil，
        /// 因此这里保留一份无 UnityEditor 依赖的固定路径常量供回退逻辑使用。
        /// The runtime assembly cannot depend directly on the editor-only ConfigEditorUtil in player builds,
        /// so this class keeps a UnityEditor-free constant copy of the fallback path.
        /// </summary>
        private static readonly string DefaultEditorConfigPath = IPath.Combine("Assets/Scenes/Config", "editor.bytes");

        /// <summary>
        /// config数据列表
        /// </summary>
        private List<ConfigDataBase> configList { get; set; } = new List<ConfigDataBase>();

        /// <summary>
        /// config处理器列表
        /// </summary>
        private List<IConfigProcessor> configProcessorList { get; set; } = new List<IConfigProcessor>();

        /// <summary>
        /// start
        /// </summary>
        public override void Start()
        {
            base.Start();
            if (this.GetAllClassDatas().Count() == 0)
            {
                BDebug.LogError("[GameconfigManger]启动失败，class data 数量为0.");
            }

            //加载配置
            var text = GetConfigText();
            if (!string.IsNullOrEmpty(text))
            {
                //执行
                (configList, configProcessorList) = LoadConfig(text);
                for (int i = 0; i < configList.Count; i++)
                {
                    configProcessorList[i].OnConfigLoad(configList[i]);
                }
            }
        }

        /// <summary>
        /// 获取框架配置文本。
        /// Get the framework configuration text.
        /// 配置来源回退顺序固定为：运行时 launcher 文本、场景 launcher 文本、编辑器默认 bytes 文件。
        /// The fallback order is fixed as runtime launcher text, scene launcher text, and finally the editor default bytes file.
        /// </summary>
        /// <returns>本次命中的配置文本；如果所有来源都不可用，则返回空字符串。</returns>
        private string GetConfigText()
        {
            string text = "";
            var runtimeLauncher = Application.isPlaying ? BDLauncher.Inst : null;
            BDLauncher sceneLauncher = null;
            if (!(Application.isPlaying && runtimeLauncher && runtimeLauncher.ConfigText))
            {
                sceneLauncher = GameObject.FindObjectOfType<BDLauncher>();
            }

            var defaultEditorConfigPath = DefaultEditorConfigPath;
            var defaultEditorConfigExists = false;
#if UNITY_EDITOR
            defaultEditorConfigExists = File.Exists(defaultEditorConfigPath);
#endif

            var configSourcePlan = GameConfigStartupPureLogic.ResolveFrameworkConfigTextSource(
                Application.isPlaying,
                runtimeLauncher && runtimeLauncher.ConfigText,
                runtimeLauncher && runtimeLauncher.ConfigText ? runtimeLauncher.ConfigText.name : string.Empty,
                sceneLauncher && sceneLauncher.ConfigText,
                sceneLauncher && sceneLauncher.ConfigText ? sceneLauncher.ConfigText.name : string.Empty,
                Application.isEditor,
                defaultEditorConfigExists,
                defaultEditorConfigPath);

            switch (configSourcePlan.SourceKind)
            {
                case GameConfigStartupPureLogic.FrameworkConfigTextSourceKind.RuntimeLauncherTextAsset:
                    text = runtimeLauncher.ConfigText.text;
                    break;
                case GameConfigStartupPureLogic.FrameworkConfigTextSourceKind.SceneLauncherTextAsset:
                    text = sceneLauncher.ConfigText.text;
                    break;
                case GameConfigStartupPureLogic.FrameworkConfigTextSourceKind.EditorDefaultFile:
#if UNITY_EDITOR
                    text = File.ReadAllText(configSourcePlan.SourceIdentifier);
#endif
                    break;
            }

            if (!string.IsNullOrEmpty(text) && configSourcePlan.ShouldLogSource)
            {
                BDebug.Log(GameConfigStartupPureLogic.FormatFrameworkConfigSourceLogMessage(
                    configSourcePlan.SourceIdentifier), Color.yellow);
            }

            return text;
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="configText"></param>
        /// <returns></returns>
        public (List<ConfigDataBase>, List<IConfigProcessor>) LoadConfig(string configText)
        {
            List<ConfigDataBase> retDatalist = new List<ConfigDataBase>();
            List<IConfigProcessor> retProcessorList = new List<IConfigProcessor>();

            var classDataList = this.GetAllClassDatas();
            var jsonObj = JsonMapper.ToObject(configText);
            //按tag顺序执行
            foreach (var cd in classDataList)
            {
                var nestType = cd.Type.GetNestedType("Config");
                if (nestType != null)
                {
                    foreach (JsonData jo in jsonObj)
                    {
                        //查询内部类
                        var clsTypeName = jo[nameof(ConfigDataBase.ClassType)].GetString();
                        //实例化内部类
                        if (nestType.FullName == clsTypeName)
                        {
                            var configData = JsonMapper.ToObject(nestType, jo.ToJson()) as ConfigDataBase;
                            var configProcessor = CreateInstance<IConfigProcessor>(cd);
                            retDatalist.Add(configData);
                            retProcessorList.Add(configProcessor);
                            break;
                        }
                    }
                }
            }

            return (retDatalist, retProcessorList);
        }


        private JsonData configObjCache = null;
        /// <summary>
        /// 获取config
        /// 未走初始化流程时获取，会单独解析一次
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfig<T>() where T : ConfigDataBase
        {
            var conf = configList.FirstOrDefault((c) => c is T) as T;
            //没有完全初始化
            if (conf == null)
            {
                //在整套流程初始化前获取单个config
                if (configObjCache == null)
                {
                    var text = GetConfigText();
                    configObjCache = JsonMapper.ToObject(text);
                }
                //对比类型开始解析
                var typeName = typeof(T).FullName;
                foreach (JsonData jo in configObjCache)
                {
                    if (jo[nameof(ConfigDataBase.ClassType)].GetString() == typeName)
                    {
                        var inst = JsonMapper.ToObject<T>(jo.ToJson());
                        conf = inst;
                        break;
                    }
                }

                //添加临时缓存
                if (conf != null)
                {
                    configList.Add(conf);
                }
            }


            return conf;
        }

        /// <summary>
        /// 获取config
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfigProcessor<T>() where T : IConfigProcessor
        {
            var con = configProcessorList.FirstOrDefault((c) => c is T);
            return (T) con;
        }

        /// <summary>
        /// 创建新的配置
        /// </summary>
        /// <returns></returns>
        public List<ConfigDataBase> CreateNewConfig()
        {
            List<ConfigDataBase> list = new List<ConfigDataBase>();
            var allconfigtype = GameConfigManager.Inst.GetAllClassDatas();
            foreach (var cd in allconfigtype)
            {
                var configType = cd.Type.GetNestedType("Config");
                if (configType != null)
                {
                    var configInstance = Activator.CreateInstance(configType) as ConfigDataBase;
                    list.Add(configInstance);
                }
            }

            return list;
        }


        /// <summary>
        /// 读取config
        /// </summary>
        /// <param name="configText"></param>
        public Dictionary<Type, ConfigDataBase> ReadConfig(string configText)
        {
            //type=> 主class的type
            var map = new Dictionary<Type, ConfigDataBase>();

            var (datalist, processorlist) = LoadConfig(configText);
            //赋值新的
            var allconfigtype = GameConfigManager.Inst.GetAllClassDatas();
            foreach (var cd in allconfigtype)
            {
                var nestedType = cd.Type.GetNestedType("Config");
                if (nestedType != null)
                {
                    //寻找本地配置
                    var configData = datalist.FirstOrDefault((c) => c.ClassType == nestedType.FullName);
                    //不存在则创建新的配置对象
                    if (configData == null)
                    {
                        configData = Activator.CreateInstance(nestedType) as ConfigDataBase;
                    }

                    map[cd.Type] = configData;
                }
            }

            return map;
        }
    }
}
