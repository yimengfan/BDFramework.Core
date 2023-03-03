using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BDFramework.Configure;
using LitJson;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BDFramework.Editor.Inspector.Config
{
#if UNITY_EDITOR
    /// <summary>
    /// 配置编辑器工具
    /// </summary>
    static public class ConfigEditorUtil
    {
        static readonly public string FILE_SUFFIX = ".bytes";

        /// <summary>
        /// 配置path
        /// </summary>
        static public string CONFIG_PATH = "Assets/Scenes/Config";

        /// <summary>
        /// 获取所有配置path
        /// </summary>
        /// <returns></returns>
        static public string[] GetConfigPaths()
        {
            var configList = Directory.GetFiles(CONFIG_PATH, "*" + FILE_SUFFIX, SearchOption.AllDirectories).Select((s) => s.Replace("\\", "/"))
                .Where((s) => Path.GetExtension(s) != ".meta").ToList();

            return configList.ToArray();
        }

        /// <summary>
        /// 获取config数据
        /// </summary>
        static public Dictionary<string, List<ConfigDataBase>> GetConfigDatas()
        {
            var retMap = new Dictionary<string, List<ConfigDataBase>>();
            //
            var files = GetConfigPaths();
            foreach (var f in files)
            {
                var text = File.ReadAllText(f);
                var ret = GameConfigManager.Inst.LoadConfig(text);
                retMap[f] = ret.Item1;
            }

            return retMap;
        }

        /// <summary>
        /// 创建配置
        /// </summary>
        /// <param name="configName"></param>
        static public void CreateConfig(string configName)
        {
            var datalist = GameConfigManager.Inst.CreateNewConfig();
            //保存默认的
            SaveConfig(IPath.Combine(CONFIG_PATH, configName + FILE_SUFFIX), datalist);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 保存Config
        /// </summary>
        /// <param name="configMap"></param>
        static public void SaveConfig(string filePath, List<ConfigDataBase> configList)
        {
            foreach (var config in configList)
            {
                config.ClassType = config.GetType().FullName;
            }

            var json = JsonMapper.ToJson(configList, true);
            if (File.Exists(filePath))
            {
                var exsitFile = File.ReadAllText(filePath);
                if (exsitFile != json)
                {
                    FileHelper.WriteAllText(filePath, json);
                    //
                    Debug.Log($"覆盖成功:{filePath} \n {json}");
                }
            }
            else
            {
                FileHelper.WriteAllText(filePath, json);
                Debug.Log($"保存成功:{filePath} \n {json}");
            }

            ;
        }


        /// <summary>
        /// 更新一个Data数据到所有
        /// </summary>
        /// <param name="data"></param>
        static public void UpdateConfigDataToAll(ConfigDataBase data)
        {
            var configMap = GetConfigDatas();
            //
            foreach (var item in configMap)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    var d = item.Value[i];
                    //覆盖
                    if (d.GetType() == d.GetType())
                    {
                        item.Value[i] = data;
                        break;
                    }
                }

                //保存
                SaveConfig(item.Key, item.Value);
            }
        }

        /// <summary>
        /// 更新一个Data数据到所有
        /// </summary>
        /// <param name="data"></param>
        static public void UpdateClientVersionToAll(string clientVersion)
        {
            var configMap = GetConfigDatas();
            //
            foreach (var item in configMap)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    var d = item.Value[i];
                    //覆盖
                    if (d is GameBaseConfigProcessor.Config con)
                    {
                        con.ClientVersionNum = clientVersion;
                        break;
                    }
                }

                //保存
                SaveConfig(item.Key, item.Value);
            }
        }


        /// <summary>
        /// 加载EditorConfig
        /// </summary>
        /// <returns></returns>
        static public T GetEditorConfig<T>()  where  T : ConfigDataBase
        {
            var editorPath = IPath.Combine(CONFIG_PATH, "editor" + FILE_SUFFIX);
            var content = File.ReadAllText(editorPath);
            //
            var item = GameConfigManager.Inst.LoadConfig(content);
            var find =  item.Item1.FirstOrDefault((t)=>t is T);
            return (T) find;
        }
        
        /// <summary>
        /// 加载正则匹配
        /// </summary>
        /// <returns></returns>
        static public string GetEditorConfig(string configType,string configKey)  
        {
            var editorPath = IPath.Combine(CONFIG_PATH, "editor" + FILE_SUFFIX);

            var json = JsonMapper.ToObject(File.ReadAllText(editorPath));
            
            foreach (JsonData jd in json)
            {
                if (jd["ClassType"].GetString() == configType)
                {
                    return jd[configKey].GetString();
                }
            }

            return "";
        }
    }
#endif
}
