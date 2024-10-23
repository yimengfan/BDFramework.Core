using System.IO;
using BDFramework.Configure;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Config
{
    [GameConfig(11, "Config测试1")]
    public class TestConfigProcessor : IConfigProcessor
    {
        public class Config : ConfigDataBase
        {
            [LabelText("测试条目")]
            public string Test1 = "111";
            [LabelText("测试条目2")]
            public string Test2 = "111";
            [LabelText("测试条目3")]
            public string Test3 = "111";
        }
        
        public  void OnConfigLoad(ConfigDataBase config)
        {
            var con = config as Config;
            
            
            Debug.Log($"[配置中心]测试条目1:\n{JsonMapper.ToJson(con,true)}");
        }
    } 
}
