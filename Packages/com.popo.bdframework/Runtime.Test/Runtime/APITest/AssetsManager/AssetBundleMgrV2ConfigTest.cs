using System;
using System.IO;
using System.Text;
using BDFramework.ResourceMgr.V2;
using UnityEngine;

namespace BDFramework.RuntimeTests.ApiTest.AssetsManager
{
    /// <summary>
    /// AssetBundleMgrV2 配置加载的 Runtime 测试主体。
    /// 固定"跳过热更直接启动"场景的关键契约：persistent（first）目录缺 art_assets.info /
    /// art_asset_type.info 时，配置必须回退到母包（second）目录读取，否则一切资源加载返回 null。
    /// </summary>
    public sealed class AssetBundleMgrV2ConfigTest
    {
        private string temporaryRootDirectory;

        /// <summary>
        /// 创建独立临时目录并输出统一日志。
        /// </summary>
        public void SetUp(string testName)
        {
            temporaryRootDirectory = Path.Combine(Path.GetTempPath(), "bdframework-abmgr-config-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryRootDirectory);

            ApiTestLog.LogTestPurposeAndMeans(
                string.IsNullOrEmpty(testName) ? nameof(AssetBundleMgrV2ConfigTest) : testName,
                "验证 AB 配置加载在 first 目录缺失时回退 second（母包）目录。",
                "在临时目录构造仅 second 目录含 art_assets.info/art_asset_type.info 的布局，调用 LoadAssetbundleConfig 并断言条目可寻址。"
            );
        }

        /// <summary>
        /// 清理临时目录。
        /// </summary>
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(temporaryRootDirectory) && Directory.Exists(temporaryRootDirectory))
            {
                Directory.Delete(temporaryRootDirectory, true);
            }

            temporaryRootDirectory = null;
        }

        /// <summary>
        /// 写入最小可用的 art_asset_type.info 与 art_assets.info（含一个 GameObject 条目）。
        /// </summary>
        private void WriteMinimalConfig(string rootDir, string loadPath)
        {
            var artDir = Path.Combine(rootDir, "art_assets");
            Directory.CreateDirectory(artDir);
            var utf8 = new UTF8Encoding(false);
            //与 AssetTypeConfig 序列化格式一致：字段头 + 单行列表值
            File.WriteAllText(Path.Combine(artDir, "art_asset_type.info"),
                "AssetTypeList\n\"[UnityEngine.GameObject]\"", utf8);
            //与 AssetBundleItem 序列化格式一致：字段头 + 一条 GameObject 记录（Id>0、LoadPath 非空 → 可加载）
            File.WriteAllText(Path.Combine(artDir, "art_assets.info"),
                "Id,AssetType,LoadPath,GUID,AssetBundleLoadType,AssetBundlePath,Hash,AssetsPackSourceHash,Mix,DependAssetIds\n" +
                $"1,0,{loadPath},testguid,0,,,,0,\"[]\"", utf8);
        }

        /// <summary>
        /// 回归用例（AB 配置回退母包目录修复）：first 目录无配置文件时，LoadAssetbundleConfig(first, second)
        /// 必须从 second 目录加载成功且条目可按类型寻址。
        /// 修复前行为：配置只读 first 目录，跳过热更直接启动时一切资源加载返回 null。
        /// </summary>
        public void LoadAssetbundleConfig_WithEmptyFirstDir_FallsBackToSecondDir()
        {
            var firstDir = Path.Combine(temporaryRootDirectory, "first");
            var secondDir = Path.Combine(temporaryRootDirectory, "second");
            Directory.CreateDirectory(firstDir);
            Directory.CreateDirectory(secondDir);
            const string loadPath = "windows/badanjin/win_home";
            WriteMinimalConfig(secondDir, loadPath);

            var loader = AssetBundleMgrV2.LoadAssetbundleConfig(firstDir, secondDir);

            ApiTestAssert.IsNotNull(loader.AssetbundleItemList, "first 缺配置时应回退 second 目录加载配置列表。");
            var item = loader.GetAssetBundleItem("Windows/badanjin/Win_Home", typeof(GameObject));
            ApiTestAssert.IsNotNull(item, "回退加载后应能按 GameObject 类型寻址到窗口 prefab 条目。");
            ApiTestAssert.AreEqual(loadPath, item.LoadPath, "寻址命中的条目 LoadPath 应与索引记录一致。");
        }

        /// <summary>
        /// 反例：两个目录都没有配置时，配置列表保持 null（不抛异常，由调用侧日志定位）。
        /// </summary>
        public void LoadAssetbundleConfig_WithBothDirsEmpty_KeepsNullList()
        {
            var firstDir = Path.Combine(temporaryRootDirectory, "first-empty");
            var secondDir = Path.Combine(temporaryRootDirectory, "second-empty");
            Directory.CreateDirectory(firstDir);
            Directory.CreateDirectory(secondDir);

            var loader = AssetBundleMgrV2.LoadAssetbundleConfig(firstDir, secondDir);

            ApiTestAssert.IsNull(loader.AssetbundleItemList, "两个目录均无配置时列表应为 null 而非异常。");
        }
    }
}
