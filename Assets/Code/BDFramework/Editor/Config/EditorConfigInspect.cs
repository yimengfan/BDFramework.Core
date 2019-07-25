using BDFramework.Helper;
using System.IO;
using BDFramework;
using LitJson;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Config))]
public class EditorConfigInspect : Editor
{
    private bool ishotfix = false;

    static private Config config;
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
         config = this.target as Config;
        
        var platform = BDUtils.GetPlatformPath(Application.platform);
        var filepath = Application.persistentDataPath + "/" + platform;

        GUILayout.Label(string.Format("当前资源路径:{0}", filepath));
        
        base.OnInspectorGUI();
        //
        
        
        if (GUILayout.Button("清空Persistent" ,GUILayout.Width(100),GUILayout.Height(20)))
        {      
            Directory.Delete(Application.persistentDataPath,true);           
        }

        if (GUILayout.Button("生成Config",GUILayout.Width(100),GUILayout.Height(20)))
        {
            GenGameConfig(Application.streamingAssetsPath,BDUtils.GetPlatformPath(RuntimePlatform.WindowsPlayer));
        }
       
    }


    /// <summary>
    ///  生成配置
    /// </summary>
    static public void GenGameConfig(string str ,string platform)
    {
        
        var gameConfig = new GameConfig();

        var gcType =  gameConfig.GetType();
        var configType = config.GetType();
        //
        foreach (var f in gcType.GetFields() )
        {
            var ctf = configType.GetField(f.Name);
            //反射赋值
            if (f.FieldType == ctf.FieldType)
            {
                f.SetValue(gameConfig,ctf.GetValue(config));
            }
            else if(f.FieldType == typeof(int) && ctf.FieldType.IsEnum)
            {
                f.SetValue(gameConfig,(int)ctf.GetValue(config));
            }
            else
            {
                BDebug.LogError("类型不匹配:" + f.Name);
            }
        }
        
        var json = JsonMapper.ToJson(gameConfig);

        var fs = string.Format("{0}/{1}/{2}", str, platform, "GameConfig.json");
        
        File.WriteAllText(fs,json);
        
        AssetDatabase.Refresh();
        Debug.Log("导出成功：" + fs);
    }


}
