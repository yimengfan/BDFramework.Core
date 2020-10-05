namespace BDFramework.Editor.EditorLife
{
    /// <summary>
    /// BDFrame的扩展生命周期
    /// </summary>
    abstract public class ABDFrameEditorBehavior
    {
        /// <summary>
        ///一键打包时， 开始build dll
        /// </summary>
        abstract public void OnBeginBuildDLL();
        /// <summary>
        /// 一键打包时，结束build dll
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        abstract public void OnEndBuildDLL(string outputPath);
        
        /// <summary>
        /// 一键打包时，开始导出sqlite
        /// </summary>
        abstract public void OnBeginBuildSqlite();
        /// <summary>
        ///  一键打包时，完成导出sqlite
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        abstract public void OnEndBuildSqlite(string outputPath);
        
        /// <summary>
        /// 一键打包时，开始导出AssetBundle
        /// </summary>
        abstract public void OnBeginBuildAssetBundle();
        /// <summary>
        ///  一键打包时，完成导出AssetBundle
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        abstract public void OnEndBuildAssetBundle(string outputPath);
    }
}