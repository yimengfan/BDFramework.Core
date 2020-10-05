using BDFramework.Editor.Asset;
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
            Debug.Log("【BDFrameEditorBehavior】打包Assetb之前测试!");
        }

        public override void OnEndBuildAssetBundle(string outputPath)
        {
            Debug.Log("【BDFrameEditorBehavior】打包Assetb之后测试!");
        }
    }
}