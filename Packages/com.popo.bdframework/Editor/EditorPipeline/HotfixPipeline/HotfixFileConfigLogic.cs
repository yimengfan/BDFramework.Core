using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LitJson;
using UnityEditor;

namespace BDFramework.Editor.HotfixPipeline
{
    /// <summary>
    /// 热更文件配置逻辑
    /// </summary>
    public class HotfixFileConfigLogic
    {
        /// <summary>
        /// 热更文件配置
        /// 默认为热更
        /// </summary>
        public class HotfixFileConfigItem
        {
            /// <summary>
            /// Tag
            /// </summary>
            public string Tag;

            /// <summary>
            /// 文件夹路径
            /// </summary>
            public string FloderPath;

            /// <summary>
            /// 文件后缀名
            /// </summary>
            public string FileExtensionName;

            /// <summary>
            /// 热更文件
            /// </summary>
            public List<string> HotFixFileList = new List<string>();

            /// <summary>
            /// 非热更目录
            /// </summary>
            public List<string> NotHotFixFileList = new List<string>();

            /// <summary>
            /// 添加不热更的文件
            /// </summary>
            /// <param name="Tag"></param>
            /// <param name="filePath"></param>
            public void AddNotHotfixFileConfig(string filePath)
            {
                this.NotHotFixFileList.Add(filePath);
            }

            /// <summary>
            /// 添加不热更的文件
            /// </summary>
            /// <param name="Tag"></param>
            /// <param name="filePath"></param>
            public void RemoveNotHotfixFileConfig(string filePath)
            {
                this.NotHotFixFileList.Remove(filePath);
            }


            /// <summary>
            /// 获取hotfix文件
            /// </summary>
            /// <returns></returns>
            public string[] GetHotfixFiles()
            {
                var files = Directory.GetFiles(this.FloderPath, "*", SearchOption.AllDirectories).Where((f) => f.EndsWith(this.FileExtensionName, StringComparison.OrdinalIgnoreCase)).Select((p) => Path.GetFileName(p)).ToList();
                var hotfixFiles = files.Except(this.NotHotFixFileList);
                return hotfixFiles.Select((f) => Path.GetFileName(f)).ToArray();
            }


            /// <summary>
            /// 是否为热更文件
            /// </summary>
            /// <returns></returns>
            public bool IsHotfixFile(string filePath)
            {
                filePath = Path.GetFileName(filePath);
                return !this.NotHotFixFileList.Contains(filePath);
            }
        }

        /// <summary>
        /// hotfixList
        /// </summary>
        private List<HotfixFileConfigItem> HotfixFileConfigItemList = new List<HotfixFileConfigItem>();

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                this.HotfixFileConfigItemList = JsonMapper.ToObject<List<HotfixFileConfigItem>>(content);
            }
            else
            {
                this.HotfixFileConfigItemList = new List<HotfixFileConfigItem>();
            }
        }


        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var content = JsonMapper.ToJson(HotfixFileConfigItemList);
            FileHelper.WriteAllText(path, content);
        }

        /// <summary>
        ///  获取配置
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public HotfixFileConfigItem GetConfig(string tag)
        {
            return this.HotfixFileConfigItemList.Find((item) => item.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 添加配置
        /// </summary>
        /// <param name="Tag"></param>
        /// <param name="folderPath"></param>
        public bool AddConfigItem(string tag, string folderPath, string extensionName)
        {
            var ret = this.HotfixFileConfigItemList.Find((item) => item.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (ret == null && Directory.Exists(folderPath))
            {
                //添加
                this.HotfixFileConfigItemList.Add(new HotfixFileConfigItem()
                {
                    Tag = tag,
                    FloderPath = folderPath,
                    FileExtensionName = extensionName
                });
                return true;
            }

            return false;
        }


        /// <summary>
        /// 移除配置
        /// </summary>
        /// <param name="tag"></param>
        public void RemoveConfigItem(string tag)
        {
            var idx = this.HotfixFileConfigItemList.FindIndex((item) => item.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (idx != -1)
            {
                this.HotfixFileConfigItemList.RemoveAt(idx);
            }
        }


        /// <summary>
        /// 获取所有配置
        /// </summary>
        /// <returns></returns>
        public HotfixFileConfigItem[] GetAllConfig()
        {
            return this.HotfixFileConfigItemList.ToArray();
        }
    }
}