using System.IO;
using BDFramework.Configure;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Config
{
    [GameConfig(13, "Config测试3")]
    public class TestConfigProcessor3 : IConfigProcessor
    {
        public class Config : ConfigDataBase
        {
            [LabelText("测试条目")]
            public string Test1 = "333";
            [LabelText("测试条目2")]
            public string Test2 = "333";
            [LabelText("测试条目3")]
            public string Test3 = "333";
        }
        
        public  void OnConfigLoad(ConfigDataBase config)
        {
            var con = config as Config;
            
            
            Debug.Log($"[配置中心]测试条目3:\n{JsonMapper.ToJson(con,true)}");
        }
    }
}
