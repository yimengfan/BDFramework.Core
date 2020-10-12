using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.Asset;
using BDFramework.ResourceMgr;
using UnityEngine;

namespace BDFramework.Editor.EditorLife
{
    public class BDFrameEditorBehaviorTest : ABDFrameEditorBehavior
    {
        public override void OnBeginBuildDLL()
        {

            Debug.Log("【BDFrameEditorBehavior】打包DLL之前测试!");
        }

        public override void OnEndBuildDLL(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包DLL之后测试!");
        }

        public override void OnBeginBuildSqlite()
        {
            Debug.Log("【BDFrameEditorBehavior】打包Sqlite之前测试!");
        }

        public override void OnEndBuildSqlite(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Sqlite之后测试!");
        }

        public override void OnBeginBuildAssetBundle(BuildInfo buildInfo)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Asset之前测试!");
            //测试1：Runtime/Char 下prefab依赖 打包成一个ab
            List<string> charList = new List<string>();
            var path = "/Runtime/Char/".ToLower();
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
                foreach (var dependAssetKey in charAssetData.DependList)
                {
                    var dependAsset = buildInfo.AssetDataMaps[dependAssetKey];
                    //判断是否被多次引用，ab名是否被修改
                    if (dependAsset.ReferenceCount == 1 && dependAssetKey== dependAsset.ABName)
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
    }
}