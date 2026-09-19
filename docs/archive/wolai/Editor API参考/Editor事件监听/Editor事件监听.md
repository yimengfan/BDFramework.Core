# Editor事件监听

**ABDFrameworkPublishPipelineBehaviour**

使用者只需要**继承该类，重写方法**，即可被框架层调用。

能实现所有打包、发布流程中的监听和自定义实现.

demo参考：

BDFrameworkPublishPipelineBehaviourTest.cs

目前可监听：

**1.打包DLL前后 ，可以实现自定义的代码检查等.**

**2.打包Sqlite前后，可以实现自定义的表格检查.**

**3.打包Assetbundle前后，可以实现自定义的构建资源等...**

**4.版本资源上传服务器资源构建前后，可以自定义控制版本号，上传.**

参考代码

```c# 
        #region 编译DLL

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

        #endregion

        #region 打包Sqlite

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

        #endregion

        #region 导表

        /// <summary>
        /// 当excel表格导出
        /// </summary>
        /// <param name="type"></param>
        virtual public void OnExportExcel(Type type)
        {
        }

        #endregion

        #region 开始打包AssetBundle

        /// <summary>
        /// 一键打包时，开始导出AssetBundle
        /// </summary>
        /// <param name="buildInfo">自定义修改buildinfo内容 进行自定义的ab输出</param>
        virtual public void OnBuildAssetBundleBegin(BuildAssetBundleParams @params, BuildInfo buildInfo)
        {
        }

        /// <summary>
        ///  一键打包时，完成导出AssetBundle
        /// </summary>
        /// <param name="outputPath">dll输出路径</param>
        virtual public void OnBuildAssetBundleEnd(BuildAssetBundleParams @params, BuildInfo buildInfo)
        {
        }

        #endregion

        #region 资源转hash,预备上传服务器

        /// <summary>
        ///  发布资源处理前
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="outputPath"></param>
        /// <param name="versionNum"></param>
        virtual public void OnPublishAssetsProccessBegin(RuntimePlatform platform, string outputPath, out string versionNum)
        {
            versionNum = "0.0.1";
        }

        /// <summary>
        ///  发布资源处理后
        /// </summary>
        virtual public void OnPublishAssetsProccessEnd(RuntimePlatform platform, string outputPath)
        {
            
        }

        #endregion
```
