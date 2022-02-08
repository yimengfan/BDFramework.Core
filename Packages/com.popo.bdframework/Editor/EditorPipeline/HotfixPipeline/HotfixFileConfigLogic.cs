using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LitJson;
using UnityEditor;
using UnityEngine;

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
            /// 文件夹筛选
            /// </summary>
            public class FloderFilter
            {
                /// <summary>
                /// 文件夹路径
                /// </summary>
                public string FloderPath="null";

                /// <summary>
                /// 文件后缀名
                /// </summary>
                public string FileExtensionName=".xls";
            }

            public enum DefeaultConfigTypeEnum
            {
                /// <summary>
                /// 热更
                /// </summary>
                热更,

                /// <summary>
                /// 非热更
                /// </summary>
                非热更,
            }

            /// <summary>
            /// 默认配置类型
            /// 默认hotfix 排除的文件则为nothotfix
            /// 默认为notHotfix 排除的文件则为hotfix
            /// </summary>
            public DefeaultConfigTypeEnum DefeaultConfigType = DefeaultConfigTypeEnum.热更;

            /// <summary>
            /// Tag
            /// </summary>
            public string Tag { get; set; }


            /// <summary>
            /// 文件夹列表
            /// </summary>
            private List<FloderFilter> filterFloderList { get; set; } = new List<FloderFilter>();

            public FloderFilter[] GetFloderFilters()
            {
                return this.filterFloderList.ToArray();
            }

            /// <summary>
            /// 排除文件
            /// </summary>
            private List<string> filterFileList { get; set; } = new List<string>();

            /// <summary>
            /// 添加筛除的文件
            /// </summary>
            /// <param name="Tag"></param>
            /// <param name="filePath"></param>
            public void AddFilterFile(string filePath)
            {
                this.filterFileList.Add(filePath);
            }

            /// <summary>
            /// 删除 筛除的文件
            /// </summary>
            /// <param name="Tag"></param>
            /// <param name="filePath"></param>
            public void RemoveFilterFile(string filePath)
            {
                this.filterFileList.Remove(filePath);
            }


            /// <summary>
            /// 获取所有文件
            /// </summary>
            /// <returns></returns>
            private List<string> GetFloderFiles()
            {
                //搜集所有目录下的文件
                List<string> fileList = new List<string>();
                foreach (var floderFilter in this.filterFloderList)
                {
                    if (Directory.Exists(floderFilter.FloderPath))
                    {
                        var files = Directory.GetFiles(floderFilter.FloderPath, "*", SearchOption.AllDirectories).Where((f) => f.EndsWith(floderFilter.FileExtensionName, StringComparison.OrdinalIgnoreCase)).ToList();
                        fileList.AddRange(files);
                    }

                }

                return fileList;
            }

            /// <summary>
            /// 获取hotfix文件
            /// </summary>
            /// <returns></returns>
            public string[] GetHotfixFiles()
            {
                var fileList = GetFloderFiles();

                if (this.DefeaultConfigType == DefeaultConfigTypeEnum.热更)
                {
                    var hotfixFiles = fileList.Except(this.filterFileList);
                    return hotfixFiles.Select((f) => f).ToArray();
                }
                else  if (this.DefeaultConfigType == DefeaultConfigTypeEnum.非热更)
                {
                    return this.filterFileList.Select((f) => f).ToArray();
                }
                
                return new string[]{};
            }

            /// <summary>
            /// 获取not hotfix文件
            /// </summary>
            /// <returns></returns>
            public string[] GetNotHotfixFiles()
            {
                var fileList = GetFloderFiles();

                if (this.DefeaultConfigType == DefeaultConfigTypeEnum.非热更)
                {
                    var hotfixFiles = fileList.Except(this.filterFileList);
                    return hotfixFiles.Select((f) => Path.GetFileName(f)).ToArray();
                }
                else  if (this.DefeaultConfigType == DefeaultConfigTypeEnum.热更)
                {
                    return this.filterFileList.Select((f) => Path.GetFileName(f)).ToArray();
                }

                return new string[]{};
            }


            /// <summary>
            /// 是否为热更文件
            /// </summary>
            /// <returns></returns>
            public bool IsHotfixFile(string filePath)
            {
                filePath = Path.GetFileName(filePath);

                //默认热更，则为排除文件
                if (this.DefeaultConfigType == DefeaultConfigTypeEnum.热更)
                {
                    return this.filterFileList.Contains(filePath);
                }
                //默认非热更，则不为排除文件
                else if (this.DefeaultConfigType == DefeaultConfigTypeEnum.非热更)
                {
                    return !this.filterFileList.Contains(filePath);
                }

                return false;
            }



            /// <summary>
            /// 添加目录
            /// </summary>
            /// <param name="folder"></param>
            /// <param name="extName"></param>
            public void AddFolderFilter(string folder, string extName)
            {
                this.filterFloderList.Add(new FloderFilter()
                {
                    FloderPath = folder,
                    FileExtensionName =  extName
                });
            }
            
            /// <summary>
            /// 移除目录
            /// </summary>
            public void RemoveFloderFilter(string folder)
            {
                var idx = this.filterFloderList.FindIndex((f) => f.FloderPath.Equals(folder, StringComparison.OrdinalIgnoreCase));

                //移除排除文件
                for (int i = this.filterFileList.Count - 1; i >= 0; i--)
                {
                    var file = this.filterFileList[i];
                    
                    if (file.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                    {
                        this.filterFileList.RemoveAt(i);
                    }
                }

                //移除自身
                if (idx >= 0)
                {
                    this.filterFloderList.RemoveAt(idx);
                }
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
        public bool AddConfigItem(string tag)
        {
            var ret = this.HotfixFileConfigItemList.Find((item) => item.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (ret == null)
            {
                //添加
                this.HotfixFileConfigItemList.Add(new HotfixFileConfigItem()
                {
                    Tag = tag,
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