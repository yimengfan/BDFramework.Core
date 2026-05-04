using System;
using BDFramework.Core.Tools;
using BDFramework.Editor.Unity3dEx.PluginsEx.Odin.Attribute;
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

        [PropertySpace(8)] [BoxGroup("母包设置")] [LabelText("母包版本")]
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
        [Button("Debug for Profiler", ButtonSizes.Large)]
        [GUIColor(1, 1, 0)]
        public void Btn_DebugForProfiler()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建DebugForProfiler包体（Debug + Profiler + Deep Profiling）", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.DebugForProfiler, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, this.ClientVersion);
            }
        }

        [HorizontalGroup("a/a2")]
        [Button("构建发布版本", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void Btn_ReleaseBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建Release版本包体？", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.Release, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, this.ClientVersion);
            }
        }

        [HorizontalGroup("a/a2")]
        [Button("Release for Test", ButtonSizes.Large)]
        [GUIColor(0, 0.8f, 1)]
        public void Btn_ReleaseForTest()
        {
            if (EditorUtility.DisplayDialog("提示", "是否构建ReleaseForTest包体（Release + 测试程序集，用于自动化测试）", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.ReleaseForTest, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, this.ClientVersion);
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
          public string HotFixFunc = "HCLR";
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

        [HorizontalGroup("b/a3", LabelWidth = 80)]
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
        [Button("自定义构建（DebugForProfiler）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildDebugForProfiler()
        {
            CustomBuild(BuildTools_ClientPackage.BuildMode.DebugForProfiler);
        }

        [HorizontalGroup("b/a8")]
        [GUIColor(0, 1, 0.5f)]
        [Button("自定义构建（Release）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildReleaseForPublish()
        {
            CustomBuild(BuildTools_ClientPackage.BuildMode.Release);
        }

        [HorizontalGroup("b/a8")]
        [GUIColor(0, 0.8f, 1)]
        [Button("自定义构建（ReleaseForTest）", ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void Btn_CustomBuildReleaseForTest()
        {
            CustomBuild(BuildTools_ClientPackage.BuildMode.ReleaseForTest);
        }
        
        #endregion


        /// <summary>
        /// 加载debug配置,debug构建
        /// </summary>
        public static void DebugBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.Debug, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, BuildTools_ClientPackage.GetDefaultClientVersion());
            }
        }

        /// <summary>
        /// 加载Release配置,Release 构建
        /// </summary>
        public static void ReleaseBuild()
        {
            if (EditorUtility.DisplayDialog("提示", "此操作会重新编译资源,是否继续？", "OK", "Cancel"))
            {
                BuildTools_ClientPackage.Build(BuildTools_ClientPackage.BuildMode.Release, true, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildTools_Assets.BuildPackageOption.BuildAll, BuildTools_ClientPackage.GetDefaultClientVersion());
            }
        }


        /// <summary>
        /// 自定义构建
        /// </summary>
        public void CustomBuild(BuildTools_ClientPackage.BuildMode buildMode)
        {
            var buildConfig = this.IsSetBuildSceneConfig ? this.BuildSceneConfig : null;
            BuildTools_ClientPackage.Build(buildMode, this.BuildScene, buildConfig, IsReBuildAssets, BApplication.DevOpsPublishClientPackagePath, BuildTarget, BuildPackageOption, this.ClientVersion);
        }
    }
}
