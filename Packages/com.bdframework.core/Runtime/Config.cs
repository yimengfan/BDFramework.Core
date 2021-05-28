using System;
using System.IO;
using System.Linq;
using LitJson;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using System.Collections;
using UnityEditor;

#endif

namespace BDFramework
{
    public enum AssetLoadPath
    {
        Editor = 0,
        Persistent,
        StreamingAsset,
        EditorLibrary
    }

//
    public enum HotfixCodeRunMode
    {
        ByILRuntime = 0,
        ByReflection,
    }

    public enum AssetBundleManagerVersion
    {
        V1,
        V2,
    }

    [Serializable]
    public class GameConfig
    {
#if ODIN_INSPECTOR
        [LabelText("代码路径")]
#endif
        public AssetLoadPath CodeRoot = AssetLoadPath.Editor;
#if ODIN_INSPECTOR
        [LabelText("SQLite路径")]
#endif
        public AssetLoadPath SQLRoot = AssetLoadPath.Editor;
#if ODIN_INSPECTOR
        [LabelText("资源路径")]
#endif
        public AssetLoadPath ArtRoot = AssetLoadPath.Editor;
#if ODIN_INSPECTOR
        [InfoBox("StreamingAsset下生效")]
        [LabelText("配置到其他路径")]
#endif
        public string CustomArtRoot = "";
#if ODIN_INSPECTOR
        [LabelText("热更代码执行模式")]
#endif
        public HotfixCodeRunMode CodeRunMode = HotfixCodeRunMode.ByILRuntime;
#if ODIN_INSPECTOR
        [LabelText("AssetBundleManager版本")]
#endif
        public AssetBundleManagerVersion AssetBundleManagerVersion = AssetBundleManagerVersion.V1;
#if ODIN_INSPECTOR
        [LabelText("是否开启ILRuntime调试")]
#endif
        public bool IsDebuggerILRuntime = false;
#if ODIN_INSPECTOR
        [LabelText("是否打印日志")]
#endif
        public bool IsDebugLog = true;
#if ODIN_INSPECTOR
        [LabelText("是否执行热更单元测试")]
#endif
        public bool IsExcuteHotfixUnitTest = false;
#if ODIN_INSPECTOR
        [LabelText("文件服务器")]
#endif
        public string FileServerUrl = "192.168.8.68";
#if ODIN_INSPECTOR
        [LabelText("Gate服务器")]
#endif
        public string GateServerIp = "";

        public int Port;
#if ODIN_INSPECTOR
        [LabelText("是否热更")]
#endif
        public bool IsHotfix = false;
#if ODIN_INSPECTOR
        [LabelText("是否联网")]
#endif
        public bool IsNeedNet = false;
    }

    /// <summary>
    /// 游戏进入的config
    /// </summary>
    public class Config : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [HideLabel]
        [InlinePropertyAttribute]
#endif
        public GameConfig Data;

        /// <summary>
        /// 设置新配置
        /// </summary>
        /// <param name="gameConfig"></param>
        public void SetNewConfig(string gameConfig)
        {
            this.Data = JsonMapper.ToObject<GameConfig>(gameConfig);
        }

        #region 编辑器

#if UNITY_EDITOR && ODIN_INSPECTOR
        private bool isChangedData = false;

        private void OnValidate()
        {
            isChangedData = true;
        }

        //触发这个函数的时候，有些值没被修改，暂时这样让他刷新
        public void _ONGUI()
        {
            if (isChangedData)
            {
                //
                var asset = this.gameObject.GetComponent<BDLauncher>().ConfigText;
                var path = AssetDatabase.GetAssetPath(asset.GetInstanceID());
                GenGameConfig(Path.GetDirectoryName(path), asset.name);
                isChangedData = false;
            }
        }


        [TitleGroup("真机环境下,BDLauncher的Config不能为null！", alignment: TitleAlignments.Centered)]
        [LabelText("生成配置名")]
        [OnInspectorGUI(nameof(ONGUI_SelcectConfig))]
        public string ConfigFileName = "Default";

        private static int curSlectConfigIdx = -1;

        //选择配置
        static public void ONGUI_SelcectConfig()
        {
            if (Application.isPlaying)
                return;

            GUI.color = Color.green;
            var launcher = GameObject.FindObjectOfType<BDLauncher>();
            if (launcher == null)
            {
                BDebug.LogError("场景上没找到BDLauncher");
                return;
            }

            var curFilePath = AssetDatabase.GetAssetPath(launcher.ConfigText.GetInstanceID());
            var direct = Path.GetDirectoryName(curFilePath);
            var fs = Directory.GetFiles(direct, "*.json", SearchOption.AllDirectories)
                .Select((s) => s.Replace("\\", "/"))
                .ToList();
            var configNames = fs.Select((s) => Path.GetFileName(s)).ToArray();
            if (curSlectConfigIdx == -1)
            {
                curSlectConfigIdx = fs.FindIndex((s) => s == curFilePath);
            }

            curSlectConfigIdx = EditorGUILayout.Popup("选择配置:", curSlectConfigIdx, configNames);
            if (fs[curSlectConfigIdx] != curFilePath)
            {
                Debug.Log("选择配置:" + fs[curSlectConfigIdx]);
                var assetText = AssetDatabase.LoadAssetAtPath<TextAsset>(fs[curSlectConfigIdx]);
                launcher.ConfigText = assetText;
                //设置新的配置内容
                var config = GameObject.FindObjectOfType<Config>();
                config.SetNewConfig(assetText.text);
            }

            GUI.color = GUI.backgroundColor;
        }


        [ButtonGroup("1")]
        [Button("清空Persistent", ButtonSizes.Small)]
        public static void DeletePersistent()
        {
            Directory.Delete(Application.persistentDataPath, true);
        }

        [ButtonGroup("1")]
        [Button("生成Config", ButtonSizes.Small)]
        public static void GenConfig()
        {
            var config = GameObject.FindObjectOfType<Config>();
            GenGameConfig("Assets/Scenes/Config", config.ConfigFileName);

            EditorUtility.DisplayDialog("提示:", "生成:Assets/Scenes/Config/" + config.ConfigFileName, "OK");
        }


        /// <summary>
        /// 生成GameConfig
        /// </summary>
        /// <param name="str"></param>
        /// <param name="platform"></param>
        static public void GenGameConfig(string str, string filename)
        {
            if (Application.isPlaying)
            {
                return;
            }

            //config
            var config = GameObject.FindObjectOfType<Config>();
            if (config == null)
            {
                return;
            }

            var json = JsonMapper.ToJson(config.Data);
            //根据不同场景生成配置
            //Scene scene = EditorSceneManager.GetActiveScene();
            var fs = string.Format("{0}/{1}", str, filename + ".json");
            FileHelper.WriteAllText(fs, json);
            AssetDatabase.Refresh();
            //
            var content = AssetDatabase.LoadAssetAtPath<TextAsset>(fs);
            var bdconfig = GameObject.FindObjectOfType<BDLauncher>();
            if (bdconfig.ConfigText.name != filename)
            {
                bdconfig.ConfigText = content;
            }

            Debug.Log("修改配置保存成功:" + filename);
        }

        /// <summary>
        /// 值变动
        /// </summary>
        /// <param name="text"></param>
        static public void OnValueChanged_GameConfig(TextAsset text)
        {
            if (text)
            {
                var configData = JsonMapper.ToObject<GameConfig>(text.text);
                var config = GameObject.FindObjectOfType<Config>();
                config.Data = configData;
            }

            BDebug.Log("配置改变,刷新!");
        }
#endif

        #endregion
    }
}