using System.IO;
using BDFramework.Configure;
using Sirenix.OdinInspector;

namespace Game.Config
{
    [GameConfig(10, "网络设置")]
    public class ServerConfigProcessor : IConfigProcessor
    {
        public class Config : ConfigDataBase
        {
            [LabelText("是否联网")]
            public bool IsNeedNet = false;

            [LabelText("文件服务器")]
            public string FileServerUrl = "192.168.0.222";

            [LabelText("Gate服务器")]
            public string GateServerIp = "";
            public int Port;
        }

        public void OnConfigLoad(ConfigDataBase config)
        {
            
        }
    }
}
