using System.Collections.Generic;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.StringEx;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.Table
{
    /// <summary>
    /// Editor下Excel的操作工具
    /// </summary>
    public class ExcelEditorTools
    {
        static private string EXCEL_PATH = "Table";
        static private string EXCEL_CACHE_PATH = "excel.cache";

        /// <summary>
        /// 获取所有的xlsx文件
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllExcelFiles(string exceltype = "*.xlsx")
        {
            List<string> tablePathList = new List<string>();
            foreach (var path in Directory.GetDirectories("Assets", "*", SearchOption.TopDirectoryOnly))
            {
                var tablePath = IPath.Combine(path, EXCEL_PATH); //  + "/";
                if (!Directory.Exists(tablePath))
                {
                    continue;
                }

                tablePathList.Add(tablePath);
            }

            //寻找所有的excel文件
            List<string> xlslFiles = new List<string>();
            foreach (var r in tablePathList)
            {
                var fs = Directory.GetFiles(r, exceltype, SearchOption.AllDirectories);
                xlslFiles.AddRange(fs);
            }

            return xlslFiles;
        }


        /// <summary>
        /// 获取所有的ExcelHash
        /// 路径为Unity assets GUID
        /// </summary>
        /// <returns></returns>
        public static (string, Dictionary<string, string>) GetExcelsHash()
        {
            string allExcelHash = "";
            Dictionary<string, string> excelHashMap = new Dictionary<string, string>();
            var excelPaths = GetAllExcelFiles();
            foreach (var ep in excelPaths)
            {
                var hash = FileHelper.GetMurmurHash3(ep);
                var guid = AssetDatabase.AssetPathToGUID(ep);
                excelHashMap[guid] = hash;
                allExcelHash += hash;
            }
            
            //返回
            return (allExcelHash.ToMD5(), excelHashMap);
        }


        /// <summary>
        /// 获取修改的Excel文件
        /// 路径为Unity assets GUID
        /// </summary>
        public static List<string> GetChangedExcels()
        {
            List<string> retExchangedInfoList = new List<string>();
            //获取旧配置
            var lastCacheMap = LoadExcelCacheInfo();
            //当前配置
            var (_, lastestExcelCacheMap) = GetExcelsHash();

            foreach (var excelInfoItem in lastestExcelCacheMap)
            {
                var ret = lastCacheMap.TryGetValue(excelInfoItem.Key, out var lastHash);
                if (!ret || excelInfoItem.Value != lastHash)
                {
                    //添加没有、或者hash不相等的excel配置
                    retExchangedInfoList.Add(excelInfoItem.Key);
                }
            }

            return retExchangedInfoList;
        }

        /// <summary>
        /// 加载ExcelCache信息
        /// </summary>
        private static Dictionary<string, string> LoadExcelCacheInfo()
        {
            var excelCachePath = IPath.Combine(BDApplication.BDEditorCachePath, EXCEL_CACHE_PATH);
            Dictionary<string, string> excelCacheMap = new Dictionary<string, string>();
            if (File.Exists(excelCachePath))
            {
                var content = File.ReadAllText(excelCachePath);
                excelCacheMap = JsonMapper.ToObject<Dictionary<string, string>>(content);
            }

            return excelCacheMap;
        }

        /// <summary>
        /// 保存excel缓存文件
        /// 该接口会覆盖旧配置，注意再做完增量逻辑后才能保存
        /// </summary>
        public static void SaveExcelCacheInfo(Dictionary<string, string> cacheMap)
        {
            var excelCachePath = IPath.Combine(BDApplication.BDEditorCachePath, EXCEL_CACHE_PATH);
            var content = JsonMapper.ToJson(cacheMap);
            FileHelper.WriteAllText(excelCachePath, content);
        }
    }
}
