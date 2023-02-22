using System.IO;
using BDFramework.Configure;
using LitJson;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Config
{
    [GameConfig(12, "Config测试2")]
    public class TestConfigProcessor2 : AConfigProcessor
    {
        public class Config : ConfigDataBase
        {
            [LabelText("测试条目")]
            public string Test1 = "222";
            [LabelText("测试条目2")]
            public string Test2 = "222";
            [LabelText("测试条目3")]
            public string Test3 = "222";
        }


        public override void OnConfigLoad(ConfigDataBase config)
        {
            var con = config as Config;
            
            
            Debug.Log($"[配置中心]测试条目2:\n{JsonMapper.ToJson(con,true)}");
        }
    }
}
