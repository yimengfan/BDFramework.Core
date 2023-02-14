using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Mgr;
using LitJson;

namespace BDFramework.Configure
{
    /// <summary>
    /// 配置属性
    /// 按Tag排序
    /// </summary>
    public class GameConfigAttribute : ManagerAttribute
    {
        public GameConfigAttribute(int intTag) : base(intTag)
        {
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
        /// config缓存
        /// </summary>
        private List<ConfigDataBase> configList { get; set; } = new List<ConfigDataBase>();


        public override void Start()
        {
            var classDataList = this.GetAllClassDatas();
            var jsonObj = JsonMapper.ToObject(BDLauncher.Inst.ConfigText.text);
            //按tag顺序执行
            foreach (var cd in classDataList)
            {
                foreach (JsonData jo in jsonObj)
                {
                    //查询内部类
                    var clsType = jo[nameof(ConfigDataBase.ClassType)].GetString();
                    var nestType = cd.Type.GetNestedType(clsType);
                    //实例化内部类
                    if (nestType != null)
                    {
                        var configData = JsonMapper.ToObject(nestType, jo.ToJson()) as ConfigDataBase;
                        var configProcessor = CreateInstance<AConfigProcessor>(cd);
                        configList.Add(configData);
                        //执行
                        configProcessor.OnConfigLoad(configData);
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// 获取config
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfig<T>() where T : ConfigDataBase
        {
            var con = configList.FirstOrDefault((c) => c is T);
            return (T) con;
        }


        /// <summary>
        /// 保存Config
        /// </summary>
        /// <param name="configMap"></param>
        public void SaveConfig(string filePath, List<ConfigDataBase> configList)
        {
            //
            foreach (var config in configList)
            {
                config.ClassType = config.GetType().FullName;
            }

            var jsonConfig = JsonMapper.ToJson(configList, true);
            FileHelper.WriteAllText(filePath, jsonConfig);
        }
    }
}
