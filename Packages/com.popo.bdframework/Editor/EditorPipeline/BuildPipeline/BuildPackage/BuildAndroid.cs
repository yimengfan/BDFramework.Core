using System;
using BDFramework.Core.Tools;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace BDFramework.Editor.BuildPipeline
{
    /// <summary>
    /// 构建Android面板
    /// </summary>
    [Serializable]
    public class BuildAndroid
    {
        static private BuildTarget BuildTarget = BuildTarget.Android;

        public BuildAndroid(AndroidSetting releaseSetting, AndroidSetting debugSettingSetting)
        {
            this.AndroidReleaseSetting = releaseSetting;
            this.AndroidDebugSetting = debugSettingSetting;
        }


        #region 打包参数

        [BoxGroup("Android设置[Release]")]
        [HideLabel]
        [InlineProperty]
        [DisableIf("IsLock")]
        public AndroidSetting AndroidReleaseSetting = new AndroidSetting();


        [BoxGroup("Android设置[Debug]")]
        [HideLabel]
        [InlineProperty]
        [DisableIf("IsLock")]
        public AndroidSetting AndroidDebugSetting = new AndroidSetting();

        [VerticalGroup("a")]
        [LabelText("锁住配置")]
        public bool IsLock = true;

        #endregion



        #region 标准构建

        [HorizontalGroup("a/a1")]
        [Button("构建Debug母包", ButtonSizes.Large)]
        [GUIColor(1, 0, 0)]
        public void Btn_DebugBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建Debug包体", "OK", "Cancel"))
            {
                BuildPackageTools.Build(BuildPackageTools.BuildMode.Debug, true, BApplication.DevOpsPublishPackagePath, BuildTarget.iOS);
            }
        }
        
        [HorizontalGroup("a/a1")]
        [Button("Release for profiling", ButtonSizes.Large)]
        [GUIColor(1, 1, 0)]
        public void Btn_ReleaseForProfiling()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建ReleaseForProfiling包体", "OK", "Cancel"))
            {
                BuildPackageTools.Build(BuildPackageTools.BuildMode.Profiler, true, BApplication.DevOpsPublishPackagePath, BuildTarget.iOS);
            }
        }

        [HorizontalGroup("a/a2")]
        [Button("构建发布版本", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void Btn_ReleaseBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建ReleaseForPublish版本包体？", "OK", "Cancel"))
            {
                BuildPackageTools.Build(BuildPackageTools.BuildMode.Release, true, BApplication.DevOpsPublishPackagePath, BuildTarget.iOS);
            }
        }

        #endregion


        #region 自定义构建

        // [Space(20)]
        // [VerticalGroup("b")]
        // [HorizontalGroup("b/a1", LabelWidth = 80)]
        // [Title("自定义构建", titleAlignment: TitleAlignments.Left, Bold = true)]
        // [LabelText("构建模式")]
        // private BuildPackageTools.BuildMode BuildMode = BuildPackageTools.BuildMode.Debug;
        [Space(20)]
        [Title("自定义构建", titleAlignment: TitleAlignments.Left, Bold = true)]
        [VerticalGroup("b")]
        [HorizontalGroup("b/a2", LabelWidth = 80)]
        [LabelText("打包场景")]
        public string BuildScene = BuildPackageTools.SCENEPATH;

        [HorizontalGroup("b/a3", LabelWidth = 80)]
        [LabelText("场景配置")]
        public bool IsSetBuildSceneConfig = false;

        [HorizontalGroup("b/a4", LabelWidth = 80)]
        [LabelText("打包场景")]
        [EnableIf(nameof(IsSetBuildSceneConfig))]
        public string BuildSceneConfig = BuildPackageTools.SceneConfigs[0];

        //
        [HorizontalGroup("b/a5", LabelWidth = 80)]
        [LabelText("重新构建资产")]
        public bool IsReBuildAssets = true;


        [HorizontalGroup("b/a6", LabelWidth = 80)]
        [LabelText("构建选项")]
        [EnableIf(nameof(IsReBuildAssets))]
        [EnumToggleButtons]
        public BuildAssetsTools.BuildPackageOption BuildPackageOption = BuildAssetsTools.BuildPackageOption.BuildAll;


        [HorizontalGroup("b/a7")]
        [GUIColor(1, 0, 0)]
        [Button("自定义构建（Debug）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildDebug()
        {
            CustomBuild(BuildPackageTools.BuildMode.Debug);
        }
        
        [HorizontalGroup("b/a7")]
        [GUIColor(1, 1, 0.5f)]
        [Button("自定义构建（ReleaseForProfiling）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildReleaseForProfiling()
        {
            CustomBuild(BuildPackageTools.BuildMode.Profiler);
        }

        [HorizontalGroup("b/a8")]
        [GUIColor(0, 1, 0.5f)]
        [Button("自定义构建（构建发布版本）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildReleaseForPublish()
        {
            CustomBuild(BuildPackageTools.BuildMode.Release);
        }
        
        #endregion


        /// <summary>
        /// 加载debug配置,debug构建
        /// </summary>
        public static void DebugBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildPackageTools.Build(BuildPackageTools.BuildMode.Debug, true, BApplication.DevOpsPublishPackagePath, BuildTarget.iOS);
            }
        }

        /// <summary>
        /// 加载Release配置,Release 构建
        /// </summary>
        public static void ReleaseBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildPackageTools.Build(BuildPackageTools.BuildMode.Release, true, BApplication.DevOpsPublishPackagePath, BuildTarget.iOS);
            }
        }


        /// 自定义构建
        /// </summary>
        public void CustomBuild(BuildPackageTools.BuildMode buildMode)
        {
            var buildConfig = this.IsSetBuildSceneConfig ? this.BuildSceneConfig : null;
            BuildPackageTools.Build(buildMode, this.BuildScene, buildConfig, IsReBuildAssets, BApplication.DevOpsPublishPackagePath, BuildTarget, BuildPackageOption);
        }
    }
}
