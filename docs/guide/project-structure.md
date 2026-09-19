# 目录结构与约定

框架通过 **三个特殊目录名**识别工程内容。它们可以出现在 `Assets/` 下任意第一级子目录中，不要求固定位置。

| 约定目录 | 匹配规则 | 代码入口 | 作用 |
|---------|---------|---------|------|
| `Runtime` | `Assets/*/Runtime/**` | `BApplication.GetAllRuntimeDirects()` | **资源收集根**。只有这里的资源会被 AssetBundle 管线收集 |
| `Table` | `Assets/*/Table/**/*.xlsx` | `ExcelEditorTools.EXCEL_PATH = "Table"` | **表格源根**。只有这里的 `.xlsx` 会被导成 SQLite |
| `@hotfix` | 路径中包含 `@hotfix` | `HotfixFileConfigLogic` / `DevOps/Config/HotfixFile.conf` | **热更归属标记**（见下方说明） |

!!! tip "为什么用目录名而不是配置表"
    约定目录是"零配置的显式声明"：新增一个业务模块时，只要建 `<模块>/Runtime` 和 `<模块>/Table`，打包管线就会自动纳入，不需要改任何配置文件。

## 本工程的实际布局

```text
Assets/
├── Code/                            # 业务代码（无 asmdef，落在 Assembly-CSharp）
│   ├── BDFramework.Game/            # 框架配套业务代码（UFlux 绑定适配器等）
│   │   ├── Uflux@hotfix/            #   UFlux 适配器与自定义 Attribute
│   │   └── Editor/
│   ├── BDFramework.UnitTest/        # 单元测试与 E2E
│   │   ├── Runtime/APITest@hotfix/
│   │   ├── Runtime/TestRunner@hotfix/
│   │   ├── Runtime/E2E/
│   │   └── Editor/TestRunner/
│   ├── Game/                        # 主工程业务
│   │   ├── Table/Local|Server/      #   导表生成的表类
│   │   ├── NetProtocol/Protobuf/    #   Protobuf 生成类
│   │   ├── Demo/
│   │   └── Editor/
│   └── Game@hotfix/                 # 热更业务（Demo 集合）
│       ├── demo1/  demo5/  demo6_UFlux/  demo_EventManager/  demo_StatusListener/
│       ├── ScreenView/
│       └── Window_DemoMain.cs
├── Resource/                        # 普通资源（非 SVN 托管）
│   ├── Runtime/                     #   ★ 资源收集根（会被打进 AB）
│   ├── Table/                       #   Excel 源
│   ├── Image/ Mat/ Shaders/ URPSetting/
│   └── NetProtocol/                 #   .proto 源文件
├── Resource_SVN/                    # SVN 托管的大资源
│   ├── Runtime/                     #   ★ 资源收集根
│   ├── Table/                       #   Excel 源 + 生成的表代码
│   ├── Map/ Model/
│   └── readme.txt
├── Plugins/                         # 原生插件 + 少量热更示例（TestPluginHotfix.cs）
├── 3rdPlugins/                      # 第三方资产（AssetGraph 等）
├── Scenes/                          # BDFrame.unity 等场景
├── StreamingAssets/                 # 母包内置产物（script/、package_build.info）
└── Resource.meta / Resources/       # Resources 目录仅保留 DOTween 设置
```

`Packages/com.popo.bdframework/` 内的对应关系：

```text
Packages/com.popo.bdframework/
├── Runtime/            BDFramework.Core 程序集（可随 Assembly-CSharp 一起热更）
├── Runtime.AOT/        BDFramework.AOT 程序集（AOT 补充元数据，不可热更）
├── Runtime.Test/       BDFramework.Test / BDFramework.EditorTest 程序集
├── Editor/             BDFramework.Editor 程序集
├── Editor.DevOps~/     Python / shell 构建脚本（`~` 后缀 = Unity 忽略该目录）
├── Nuget/              内置 NuGet 依赖
├── 3rdPlugins/         内置第三方源码
└── Plugins/Sqlite      原生 sqlite3 / SQLCipher 库
```

## `@hotfix` 在 v4 中的真实含义

!!! warning "不要按旧文档理解 `@hotfix`"
    在 v2/v3 的 ILRuntime 时代，`@hotfix` 决定**哪些文件编译进热更 DLL**（`Unity3dRoslynBuildTools.IsHotfixScript()`，按 `@hotfix` / `@half_hotfix` 标记切分程序集）。

    **v4 使用 HybridCLR，热更单位是整个程序集**：`Assembly-CSharp`、`Assembly-CSharp-firstpass`、`BDFramework.Core` 三个程序集整体热更（见 `HyCLREditorTools.SetBDFramework2HCLRConfig()`）。

    因此现在 `@hotfix` 只剩两个用途：
    1. **组织约定** —— `Game@hotfix/`、`Uflux@hotfix/` 这类目录名用来让人一眼看出"这块逻辑属于热更业务层"，参与编译与否与名字无关。
    2. **导表代码生成** —— `Excel2CodeTools` 根据 `DevOps/Config/HotfixFile.conf` 的规则，决定生成的表类是 `<Name>.xlsx.cs` 还是 `<Name>.xlsx@hotfix.cs`。

    旧的按文件切分实现仍留在 `Unity3dRoslynBuildTools.cs` 中，但**已整段注释**，不生效。

## `Assets/*/Table` 与导表产物

| 内容 | 位置 |
|------|------|
| Excel 源文件 | `Assets/Resource/Table/*.xlsx`、`Assets/Resource_SVN/Table/*.xlsx` |
| 生成的表类（本地表） | `Assets/Code/Game/Table/Local/<Name>.xlsx.cs` |
| 生成的表类（服务器表） | `Assets/Code/Game/Table/Server/<Name>.xlsx.cs` |
| 生成到 `Resource_SVN` 的表类 | `Assets/Resource_SVN/Table/Code/` |
| 构建产物 `local.db` | `<输出根>/<platform>/local.db` |
| 构建产物 `server.db` | `<输出根>/server_data/server.db` |

表类命名空间约定为 `Game.Data.Local` / `Game.Data.Server`，`BuildTools_Excel2SQLite.CollectTableTypes()` 靠 `Namespace.StartsWith("Game.Data.")` 收集类型。**改命名空间会导致导表失败**。

详见[表格打包](../pipeline/build-table.md)与[表格 SQLite](../api/sqlite.md)。

## 业务代码的编译归属

`Assets/Code/` 下**没有 `.asmdef`**，因此全部落在 `Assembly-CSharp` / `Assembly-CSharp-Editor` 中。

这正是热更收集机制能工作的原因：`ScriptLoder.GetAppDomainHostingTypes()` 按程序集名前缀收集托管类型，白名单为：

```text
"BDFramework"
"Assembly-CSharp,"          ← 业务热更代码
"Assembly-CSharp-firstpass,"
"UnityEngine.UI"
"Game."
含 "@main"
```

!!! note "主工程代码的边界"
    主工程（母包内置、不热更）的代码应放在**独立 asmdef** 中，避免被 `Assembly-CSharp` 前缀收集。主工程代码**不得直接引用**热更类型，只能通过事件总线或管理器转发通信。

## 相关页面

- [资源加载寻址](asset-load-path.md) —— 三个资源根（`CodeRoot` / `SQLRoot` / `ArtRoot`）如何解析成真实路径
- [AssetBundle 打包](../pipeline/build-assetbundle.md) —— `Runtime` 目录如何被收集成 AB
- [表格打包](../pipeline/build-table.md) —— Excel 格式约定与产物
