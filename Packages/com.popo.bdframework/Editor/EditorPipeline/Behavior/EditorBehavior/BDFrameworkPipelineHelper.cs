using System;
using System.Collections.Generic;
using System.Linq;
using BDFramework.Editor.BuildPipeline.AssetBundle;
using BDFramework.Editor.Environment;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor
{
    /// <summary>
    /// BDFramework publish pipeline各种事件
    /// </summary>
    static public class BDFrameworkPipelineHelper
    {
        static private List<ABDFrameworkPublishPipelineBehaviour> InstanceList = new List<ABDFrameworkPublishPipelineBehaviour>();

       static  private PublishBehaviour @BaseBehaviour = new PublishBehaviour();
        /// <summary>
        /// 初始化
        /// </summary>
        static public void Init()
        {
            var type = typeof(ABDFrameworkPublishPipelineBehaviour);
            var types = BDFrameworkEditorEnvironment.Types;
            foreach (var t in types)
            {
                if (t.IsSubclassOf(type))
                {
                    var ret = InstanceList.FirstOrDefault((a) => a.GetType() == t);
                    if (ret == null)
                    {
                        var buildPipelineInst = Activator.CreateInstance(t) as ABDFrameworkPublishPipelineBehaviour;
                        InstanceList.Add(buildPipelineInst);
                    }
                }
            }
        }
        //
        // [MenuItem("xxx/xxxx")]
        // static public void OnBeginPublishAssetsTest()
        // {
        //     BuildTarget platform = BuildTarget.Android;
        //     string outputPath = "";
        //     string versionNum = "";
        //     OnBeginBuildPackage(platform, outputPath);
        // }
        /// <summary>
        /// 开始打包热更dll
        /// </summary>
        static public void OnBeginBuildHotfixDLL()
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnBeginBuildDLL);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnBeginBuildDLL();
                }
            }
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnBeginBuildDLL();
            }
        }

        /// <summary>
        /// 结束打包热更dll
        /// </summary>
        static public void OnEndBuildDLL(string outputPath)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnEndBuildDLL);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnEndBuildDLL(outputPath);
                }
            }
            
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnEndBuildDLL(outputPath);
            }
        }

        /// <summary>
        /// 开始导出sqlite
        /// </summary>
        static public void OnBeginBuildSqlite()
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnBeginBuildSqlite);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnBeginBuildSqlite();
                }
            }
            
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnBeginBuildSqlite();
            }
        }

        /// <summary>
        /// 导出sqlite结束
        /// </summary>
        /// <param name="outputPath"></param>
        static public void OnEndBuildSqlite(string outputPath)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnEndBuildSqlite);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnEndBuildSqlite(outputPath);
                }
            }

            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnEndBuildSqlite(outputPath);
            }
        }

        /// <summary>
        /// 开始打包assetbundle
        /// </summary>
        /// <param name="assetbundleBuildingCtx"></param>
        static public void OnBeginBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnBeginBuildAssetBundle);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnBeginBuildAssetBundle(assetbundleBuildingCtx);
                }
            }
            
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnBeginBuildAssetBundle(assetbundleBuildingCtx);
            }
        }

        static public void OnEndBuildAssetBundle(AssetBundleBuildingContext assetbundleBuildingCtx)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnEndBuildAssetBundle);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnEndBuildAssetBundle(assetbundleBuildingCtx);
                }
            }
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnEndBuildAssetBundle(assetbundleBuildingCtx);
            }
        }

        /// <summary>
        /// 正在导出excel
        /// </summary>
        /// <param name="type"></param>
        static public void OnExportExcel(Type type)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnExportExcel);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnExportExcel(type);
                }
            }
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnExportExcel(type);
            }
        }


        #region 一键构建资源

        /// <summary>
        /// 【构建所有资源】处理前
        /// </summary>
        static public void OnBeginBuildAllAssets(RuntimePlatform platform, string outputPath, string lastVersionNum, out string newVersionNum)
        {
            Debug.Log("【OnBeginBuildAllAssets生命周期测试】构建资源,请生成版本号信息!!!  ->" + platform.ToString());
            newVersionNum = lastVersionNum;
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnBeginBuildAllAssets);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnBeginBuildAllAssets(platform, outputPath, lastVersionNum, out newVersionNum);
                }
            }

            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnBeginBuildAllAssets(platform, outputPath, lastVersionNum, out newVersionNum);
            }
            Debug.Log($"<color=red> 新版本号:{newVersionNum} </color>");
        }

        /// <summary>
        /// 【构建所有资源】 处理后
        /// </summary>
        static public void OnEndBuildAllAssets(RuntimePlatform platform, string outputPath, string newVersionNum)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnEndBuildAllAssets);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnEndBuildAllAssets(platform, outputPath, newVersionNum);
                }
            }

            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnEndBuildAllAssets(platform, outputPath, newVersionNum);
            }
            Debug.Log("【OnEndBuildAllAssets】构建资源已完成!");
        }

        #endregion

        #region SVC版本号

        /// <summary>
        ///  获取美术资源版本号(git\svn\p4...)
        /// </summary>
        static public string GetArtSVCNum(RuntimePlatform platform, string outputPath)
        {
            ABDFrameworkPublishPipelineBehaviour inst = null;
            foreach (var behaviour in InstanceList)
            {
                var method = behaviour.GetType().GetMethod(nameof(ABDFrameworkPublishPipelineBehaviour.GetArtSVCNum));
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(behaviour.GetType()))
                {
                    inst = behaviour;
                    break;
                }
            }


            return inst?.GetArtSVCNum(platform, outputPath);
        }

        /// <summary>
        ///  获取表格资源版本号(git\svn\p4...)
        /// </summary>
        static public string GetTableSVCNum(RuntimePlatform platform, string outputPath)
        {
            ABDFrameworkPublishPipelineBehaviour inst = null;
            foreach (var behaviour in InstanceList)
            {
                var method = behaviour.GetType().GetMethod(nameof(ABDFrameworkPublishPipelineBehaviour.GetTableSVCNum));
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(behaviour.GetType()))
                {
                    inst = behaviour;
                    break;
                }
            }

            return inst?.GetTableSVCNum(platform, outputPath);

        }

        /// <summary>
        ///  获取表格资源版本号(git\svn\p4...)
        /// </summary>
        static public string GetScriptSVCNum(RuntimePlatform platform, string outputPath)
        {
            ABDFrameworkPublishPipelineBehaviour inst = null;
            foreach (var behaviour in InstanceList)
            {
                var method = behaviour.GetType().GetMethod(nameof(ABDFrameworkPublishPipelineBehaviour.GetScriptSVCNum));
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(behaviour.GetType()))
                {
                    inst = behaviour;
                    break;
                }
            }

            //只获取一个
            return inst?.GetScriptSVCNum(platform, outputPath);
        }

        #endregion

        #region 发布资源

        /// <summary>
        /// 【发布资源】处理前
        /// </summary>
        static public void OnBeginPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnBeginPublishAssets);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnBeginPublishAssets(platform, outputPath, versionNum);
                }
            }
            
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnBeginPublishAssets(platform, outputPath, versionNum);
            }
        }

        /// <summary>
        /// 【发布资源】 处理后
        /// </summary>
        static public void OnEndPublishAssets(RuntimePlatform platform, string outputPath, string versionNum)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnEndPublishAssets);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnEndPublishAssets(platform, outputPath, versionNum);
                }
            }

            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnEndPublishAssets(platform, outputPath, versionNum);
            }
            Debug.Log($"<color=red> 新版本号:{versionNum} </color>");
            Debug.Log("【OnEndPublishAssets】发布资源已生成,请编写脚本提交以下目录到Server!");
            Debug.Log(outputPath);
            Debug.Log("------------------------------------------------end---------------------------------------------------");
        }

        #endregion

        #region 构建母包




        /// <summary>
        /// 【构建母包】开始
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outputpath"></param>
        static public void OnBeginBuildPackage(BuildTarget buildTarget, string outputpath)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnBeginBuildPackage);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnBeginBuildPackage(buildTarget, outputpath);
                }
            }
            
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnBeginBuildPackage(buildTarget, outputpath);
            }
        }

        /// <summary>
        /// 【构建母包】结束
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="outputpath"></param>
        static public void OnEndBuildPackage(BuildTarget buildTarget, string outputpath)
        {
            var  isCallSuccess = false;
            var fname = nameof(ABDFrameworkPublishPipelineBehaviour.OnEndBuildPackage);
            foreach (var inst in InstanceList)
            {
                var method = inst.GetType().GetMethod(fname);
                //判断是否覆盖了父类
                if (method.DeclaringType.Equals(inst.GetType()))
                {
                    Debug.Log($"执行:{inst.GetType()}.{fname}");
                    isCallSuccess = true;
                    inst.OnEndBuildPackage(buildTarget, outputpath);
                }
            }
            
            if (!isCallSuccess)
            {
                Debug.Log($"执行:{@BaseBehaviour.GetType()}.{fname}");
                @BaseBehaviour.OnEndBuildPackage(buildTarget, outputpath);
            }
        }

        #endregion
    }
}