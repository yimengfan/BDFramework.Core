using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BDFramework.Core.Tools;
using BDFramework.Sql;
using BDFramework.StringEx;
using LitJson;
using UnityEditor;

namespace BDFramework.Editor.Table
{
    /// <summary>
    /// Editor下Excel的操作工具
    /// </summary>
    public class ExcelEditorTools
    {
        static private string EXCEL_PATH = "Table";
        static private string EXCEL_CACHE_PATH = "ExcelCache.info";
        static private string LOCALDB_CACHE_PATH = "LacalDBCache.info";

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

            for (int i = 0; i < xlslFiles.Count; i++)
            {
                xlslFiles[i] = IPath.FormatPathOnUnity3d(xlslFiles[i]);
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
        /// 
        /// 该接口执行1次后就会将新配置覆盖本地
        /// </summary>
        public static (List<string>, Dictionary<string, string> ) GetChangedExcels()
        {
            List<string> retExchangedInfoList = new List<string>();
            //获取旧配置
            var lastCacheMap = LoadExcelCacheInfo();
            //当前配置
            var (_, newExcelCacheMap) = GetExcelsHash();

            foreach (var excelInfoItem in newExcelCacheMap)
            {
                var ret = lastCacheMap.TryGetValue(excelInfoItem.Key, out var lastHash);
                if (!ret || excelInfoItem.Value != lastHash)
                {
                    //添加没有、或者hash不相等的excel配置
                    retExchangedInfoList.Add(excelInfoItem.Key);
                }
            }

            return (retExchangedInfoList, newExcelCacheMap);
        }

        /// <summary>
        /// 获取修改的Excel文件,通过本地数据库对比
        /// 路径为Unity assets GUID
        /// 该接口执行1次后就会将新配置覆盖本地
        /// </summary>
        public static (List<string>, Dictionary<string, string> ) GetChangedExcelsFromLocalSql(string sqlPath)
        {
            List<string> retExchangedInfoList = new List<string>();
            //当前配置
            var (_, newExcelCacheMap) = GetExcelsHash();
            //获取Sql中的日志
            SqliteLoder.LoadSQLOnEditor(sqlPath);
            var logs = SqliteHelper.DB.GetTable<ImportExcelLog>().ToList();
            SqliteLoder.Close();
            //
            foreach (var excelInfoItem in newExcelCacheMap)
            {
                var excelPath = AssetDatabase.GUIDToAssetPath(excelInfoItem.Key);
                var ret = logs.FirstOrDefault((log) => log.Path.Equals(excelPath, StringComparison.OrdinalIgnoreCase));
                if (ret == null || ret.Hash != excelInfoItem.Value)
                {
                    //添加没有、或者hash不相等的excel配置
                    retExchangedInfoList.Add(excelInfoItem.Key);
                }
            }

            return (retExchangedInfoList, newExcelCacheMap);
        }

        /// <summary>
        /// 加载ExcelCache信息
        /// </summary>
        private static Dictionary<string, string> LoadExcelCacheInfo()
        {
            var excelCachePath = IPath.Combine(BApplication.BDEditorCachePath, EXCEL_CACHE_PATH);
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
            var excelCachePath = IPath.Combine(BApplication.BDEditorCachePath, EXCEL_CACHE_PATH);
            var content = JsonMapper.ToJson(cacheMap);
            FileHelper.WriteAllText(excelCachePath, content);
        }

        //
        public static string LoadLocalDBCacheInfo()
        {
            var dbCachePath = IPath.Combine(BApplication.BDEditorCachePath, LOCALDB_CACHE_PATH);
            if (File.Exists(dbCachePath))
            {
                return File.ReadAllText(dbCachePath);
            }

            return string.Empty;
        }

        public static void SaveLocalDBCacheInfo(string dbPath)
        {
            var dbCachePath = IPath.Combine(BApplication.BDEditorCachePath, LOCALDB_CACHE_PATH);
            var hash = FileHelper.GetMurmurHash3(dbPath);
            FileHelper.WriteAllText(dbCachePath, hash);
        }
    }
}
