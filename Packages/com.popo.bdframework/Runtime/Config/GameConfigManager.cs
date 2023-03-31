using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Editor.Inspector.Config;
using BDFramework.Mgr;
using LitJson;
using UnityEngine;

namespace BDFramework.Configure
{
    /// <summary>
    /// 配置属性
    /// 按Tag排序
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
    /// 游戏配置中心
    /// </summary>
    public class GameConfigManager : ManagerBase<GameConfigManager, GameConfigAttribute>
    {
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
            string text = "";
            if (Application.isPlaying)
            {
                text = BDLauncher.Inst.ConfigText.text;
                BDebug.Log("GameConfig加载配置:" + BDLauncher.Inst.ConfigText.name, Color.yellow);
            }
            else
            {
                var launcher = GameObject.FindObjectOfType<BDLauncher>();
                if (launcher && launcher.ConfigText)
                {
                    text = launcher.ConfigText.text;
                    BDebug.Log("GameConfig加载配置:" + launcher.ConfigText.name, Color.yellow);
                }
                else
                {
#if UNITY_EDITOR
                    //读取默认bytes
                    var filepath = ConfigEditorUtil.DefaultEditorConfig;
                    if (File.Exists(filepath))
                    {
                        text = File.ReadAllText(filepath);
                        BDebug.Log("GameConfig加载配置:" + filepath, Color.yellow);
                    }
#endif
                }
            }

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


        /// <summary>
        /// 获取config
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfig<T>() where T : ConfigDataBase
        {
            var con = configList.FirstOrDefault((c) => c is T);
            return (T)con;
        }

        /// <summary>
        /// 获取config
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfigProcessor<T>() where T : IConfigProcessor
        {
            var con = configProcessorList.FirstOrDefault((c) => c is T);
            return (T)con;
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