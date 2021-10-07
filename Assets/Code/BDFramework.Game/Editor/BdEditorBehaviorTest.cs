using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.AssetBundle;
using BDFramework.ResourceMgr;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    public class BdEditorBehaviorTest : ABDEditorBehavior
    {
        public override void OnBeginBuildDLL()
        {
            Debug.Log("【BDFrameEditorBehavior】打包DLL前,回调测试!");
        }

        public override void OnEndBuildDLL(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包DLL后,回调测试!");
        }

        public override void OnBeginBuildSqlite()
        {
            Debug.Log("【BDFrameEditorBehavior】打包Sqlite前,回调测试!");
        }

        public override void OnEndBuildSqlite(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Sqlite后,回调测试!");
        }

        public override void OnBeginBuildAssetBundle(BuildInfo buildInfo)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Asset时,回调测试!");
            //测试1：Runtime/Char 下prefab依赖 打包成一个ab
            List<string> charList = new List<string>();
            var          path     = "/Runtime/Char/".ToLower();
            foreach (var key in buildInfo.AssetDataMaps.Keys)
            {
                if (key.Contains(path))
                {
                    charList.Add(key);
                }
            }

            //角色列表
            foreach (var charPath in charList)
            {
                var charAssetData = buildInfo.AssetDataMaps[charPath];
                //所有依赖的资源
                foreach (var dependAssetKey in charAssetData.DependAssetList)
                {
                    //
                    buildInfo.SetABName(dependAssetKey, charAssetData.ABName, BuildInfo.SetABNameMode.Simple);

                    var dependAsset = buildInfo.AssetDataMaps[dependAssetKey];
                    //判断是否被其他资源引用，ab名是否被其他规则修改
                    if (!dependAsset.IsRefrenceByOtherAsset() && dependAssetKey == dependAsset.ABName)
                    {
                        //打包到一个ab中
                        dependAsset.ABName = charAssetData.ABName;
                    }
                }
            }
        }

        public override void OnEndBuildAssetBundle(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Asset之后测试!");
        }

        /// <summary>
        /// 导出一张excel
        /// </summary>
        /// <param name="type"></param>
        public override void OnExportExcel(Type type)
        {
            Debug.Log("导出表格回调测试:" + type.FullName);
        }
    }
}