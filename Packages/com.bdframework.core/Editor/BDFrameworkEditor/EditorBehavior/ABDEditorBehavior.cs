using System;
using BDFramework.Editor.Asset;

namespace BDFramework.Editor
{
    /// <summary>
    /// BDFrame的扩展生命周期
    /// </summary>
    abstract public class ABDEditorBehavior
    {
        #region 一键打包相关回调

        /// <summary>
        ///一键打包时， 开始build dll
        /// </summary>
        virtual public void OnBeginBuildDLL()
        {
        }

        /// <summary>
        /// 一键打包时，结束build dll
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        virtual public void OnEndBuildDLL(string outputPath)
        {
        }

        /// <summary>
        /// 一键打包时，开始导出sqlite
        /// </summary>
        virtual public void OnBeginBuildSqlite()
        {
        }

        /// <summary>
        ///  一键打包时，完成导出sqlite
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        virtual public void OnEndBuildSqlite(string outputPath)
        {
        }

        /// <summary>
        /// 一键打包时，开始导出AssetBundle
        /// </summary>
        /// <param name="buildInfo">自定义修改buildinfo内容 进行自定义的ab输出</param>
        virtual public void OnBeginBuildAssetBundle(BuildInfo buildInfo)
        {
        }

        /// <summary>
        ///  一键打包时，完成导出AssetBundle
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        virtual public void OnEndBuildAssetBundle(string outputPath)
        {
        }

        #endregion


        #region 导表相关

        /// <summary>
        /// 当excel表格导出
        /// </summary>
        /// <param name="type"></param>
        virtual public void OnExportExcel(Type type)
        {
        }

        #endregion
    }
}