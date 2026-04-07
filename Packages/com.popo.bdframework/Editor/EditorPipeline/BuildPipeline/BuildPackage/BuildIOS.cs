using System;
using System.IO;
using BDFramework.Core.Tools;
using BDFramework.Editor.Tools;
using BDFramework.Editor.Unity3dEx.PluginsEx.Odin.Attribute;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.BuildPipeline
{
    /// <summary>
    /// 构建 iOS 面板
    /// </summary>
    [Serializable]
    public class BuildIOS
    {
        static private BuildTarget BuildTarget = BuildTarget.iOS;
        const string IOSPostBuildShellPath = "DevOps/CI/BuildTools/BuildClientPackage/build_xcode.shell";
        const string IOSBuildToolsConfigPath = "DevOps/CI/BuildTools/buildtools.toml";


        #region 打包参数

        [PropertyOrder(-20)]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("iOS 构建说明")]
        [MultiLineProperty(9)]
        public string IOSBuildGuide =
            "1. 点击本页按钮后，Unity 先导出 Xcode 到 DevOps/PublishPackages/ios/<bundle id>。\n" +
            "2. 导出完成后，编辑器固定调用 " + IOSPostBuildShellPath + "。\n" +
            "3. Unity 会自动传入 --project-dir；Debug 按钮额外传 --configuration Debug，Release / ReleaseForProfiling 传 Release。\n" +
            "4. 团队共享签名、导出方式、scheme 等默认值统一配置在 " + IOSBuildToolsConfigPath + " 的 [ios_xcode]。\n" +
            "5. shell 文件头部只保留本机兜底默认值；临时覆盖请使用 BUILD_XCODE_* 环境变量或手动执行脚本参数。\n" +
            "6. shell 成功后会在 Xcode 目录同级输出同名 ipa。";

        [PropertySpace(8)]
        [BoxGroup("母包设置")]
        [LabelText("母包版本")]
        public string ClientVersion = "0.1.0";
        #endregion


        #region 标准构建

        [HorizontalGroup("a/a1")]
        [Button("构建Debug母包", ButtonSizes.Large)]
        [GUIColor(1, 0, 0)]
        public void Btn_DebugBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建Debug包体", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.Debug, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, this.ClientVersion);
            }
        }
        
        [HorizontalGroup("a/a1")]
        [Button("Release for profiling", ButtonSizes.Large)]
        [GUIColor(1, 1, 0)]
        public void Btn_ReleaseForProfiling()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建ReleaseForProfiling包体", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.Profiler, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, this.ClientVersion);
            }
        }

        [HorizontalGroup("a/a2")]
        [Button("构建发布版本", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void Btn_ReleaseBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建ReleaseForPublish版本包体？", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.Release, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, this.ClientVersion);
            }
        }

        #endregion

        
        #region Tips
#if ENABLE_ILRUNTIME||ENABLE_HYCLR
        [Space(10)]
        [GUIColor(0f,1f,0f)]
        [LabelText("当前热更模式:"),LabelWidth(80)]
        [DisableInEditorMode]
#endif
#if ENABLE_ILRUNTIME
          public string HotFixFunc = "ILRuntime";
#elif ENABLE_HYCLR
          public string HotFixFunc = "HYCLR";
#else
        public string HotFixFunc = "未定义";
#endif

        #endregion

        

        #region 自定义构建

        [Space(20)]
        [Title("自定义构建", titleAlignment: TitleAlignments.Left, Bold = true)]
        [VerticalGroup("b")]
        [HorizontalGroup("b/a2", LabelWidth = 80)]
        [LabelText("打包场景")]
        [Ex_SelectFileFromPath("Assets/Scenes","*.unity")]
        public string BuildScene = BuildTools_ClientPackage.SCENE_PATH;

        [HorizontalGroup("b/a3", LabelWidth = 200)]
        [LabelText("场景配置(默认场景自带)")]
        public bool IsSetBuildSceneConfig = true;

        [HorizontalGroup("b/a4", LabelWidth = 80)]
        [LabelText("打包配置")]
        [EnableIf(nameof(IsSetBuildSceneConfig))]
        [Ex_SelectFileFromPath("Assets/Scenes/Config/","*.bytes")]
        public string BuildSceneConfig = BuildTools_ClientPackage.SceneConfigs[0];

        //
        [HorizontalGroup("b/a5", LabelWidth = 80)]
        [LabelText("重新构建资产")]
        public bool IsReBuildAssets = true;


        [HorizontalGroup("b/a6", LabelWidth = 80)]
        [LabelText("构建选项")]
        [EnableIf(nameof(IsReBuildAssets))]
        [EnumToggleButtons]
        public BuildTools_Assets.BuildPackageOption BuildPackageOption = BuildTools_Assets.BuildPackageOption.BuildAll;


        [HorizontalGroup("b/a7")]
        [GUIColor(1, 0, 0)]
        [Button("自定义构建（Debug）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildDebug()
        {
            CustomBuild(BuildTools_ClientPackage.BuildMode.Debug);
        }
        
        [HorizontalGroup("b/a7")]
        [GUIColor(1, 1, 0.5f)]
        [Button("自定义构建（ReleaseForProfiling）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildReleaseForProfiling()
        {
            CustomBuild(BuildTools_ClientPackage.BuildMode.Profiler);
        }

        [HorizontalGroup("b/a8")]
        [GUIColor(0, 1, 0.5f)]
        [Button("自定义构建（构建发布版本）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildReleaseForPublish()
        {
            CustomBuild(BuildTools_ClientPackage.BuildMode.Release);
        }
        
        #endregion
        

        [PropertySpace(20)]
        [HorizontalGroup("b/a9")]
        [Button("Build资源到XCode(不构建代码)", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_BuildAssetsAndCopyToXcode()
        {
            this.BuildAssetsAndCopyToXcode();
        }




        /// <summary>
        /// 自定义构建
        /// </summary>
        public void CustomBuild(BuildTools_ClientPackage.BuildMode buildMode)
        {
            var buildConfig = this.IsSetBuildSceneConfig ? this.BuildSceneConfig : null;
            BuildTools_ClientPackage.Build(buildMode, this.BuildScene, buildConfig, IsReBuildAssets, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildPackageOption, this.ClientVersion);
        }


        /// <summary>
        /// 编译资源拷贝至Xcode
        /// </summary>
        public void BuildAssetsAndCopyToXcode()
        {
            //构建
            var platform = BApplication.GetRuntimePlatform(BuildTarget);
            if (this.IsReBuildAssets)
            {
                try
                {
                    BuildTools_Assets.BuildAll(platform, BApplication.DevOpsPublishAssetsPath, opa: BuildPackageOption);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("提示",$"打包资产失败!","ok");
                    throw e;
                }
              
            }
            //拷贝到iospack
            var iosPackDir = IPath.Combine(BApplication.DevOpsPublishClientPackagePath, BApplication.GetPlatformPath(BuildTarget));
            var outDirs = Directory.GetDirectories(iosPackDir, "*", SearchOption.TopDirectoryOnly);
            //
            var iosSourcePackPath = IPath.Combine(BApplication.DevOpsPublishAssetsPath, BApplication.GetPlatformPath(BuildTarget));
            foreach (var outdir in outDirs)
            {
                var xcodeDataPath = IPath.Combine(outdir, "Data/Raw", BApplication.GetPlatformPath(BuildTarget));
                EditorUtility.DisplayDialog("提示",$"正在拷贝资源到:{xcodeDataPath}","ok");
                //删除旧data
                if (Directory.Exists(xcodeDataPath))
                {
                    Directory.Delete(xcodeDataPath, true);
                }
                //拷贝
                FileHelper.CopyFolderTo(iosSourcePackPath ,xcodeDataPath);
            }
        }
    }
}
