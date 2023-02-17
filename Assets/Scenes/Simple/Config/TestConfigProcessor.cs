using System.IO;
using BDFramework.Configure;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Config
{
    [GameConfig(11, "Config测试1")]
    public class TestConfigProcessor : AConfigProcessor
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
        
        public override void OnConfigLoad(ConfigDataBase config)
        {
            var con = config as Config;
            
            Debug.Log($"测试条目3:\n{JsonMapper.ToJson(con,true)}");
        }
    }
}
