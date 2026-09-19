# Project Layout & Conventions

The framework recognises project content through **three special directory names**. They may appear under any first-level directory of `Assets/`; a fixed location is not required.

| Conventional directory | Match rule | Code entry point | Purpose |
|------------------------|-----------|------------------|---------|
| `Runtime` | `Assets/*/Runtime/**` | `BApplication.GetAllRuntimeDirects()` | **Asset collection root.** Only assets here are collected by the AssetBundle pipeline |
| `Table` | `Assets/*/Table/**/*.xlsx` | `ExcelEditorTools.EXCEL_PATH = "Table"` | **Table source root.** Only `.xlsx` files here are exported into SQLite |
| `@hotfix` | Path contains `@hotfix` | `HotfixFileConfigLogic` / `DevOps/Config/HotfixFile.conf` | **Hotfix ownership marker** (see below) |

!!! tip "Why directory names instead of a config table"
    Conventional directories are "explicit declaration with zero configuration": when you add a business module you just create `<module>/Runtime` and `<module>/Table`, and the build pipeline picks them up automatically without touching any config file.

## This project's actual layout

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

The corresponding structure inside `Packages/com.popo.bdframework/`:

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

## What `@hotfix` really means in v4

!!! warning "Do not interpret `@hotfix` the way older docs do"
    In the v2/v3 ILRuntime era, `@hotfix` decided **which files were compiled into the hotfix DLL** (`Unity3dRoslynBuildTools.IsHotfixScript()`, splitting assemblies by `@hotfix` / `@half_hotfix` markers).

    **v4 uses HybridCLR, where the hotfix unit is a whole assembly**: `Assembly-CSharp`, `Assembly-CSharp-firstpass` and `BDFramework.Core` are hot-updated as complete assemblies (see `HyCLREditorTools.SetBDFramework2HCLRConfig()`).

    So today `@hotfix` only serves two purposes:
    1. **Organisational convention** — directory names such as `Game@hotfix/` and `Uflux@hotfix/` tell a human at a glance "this logic belongs to the hotfix business layer". The name has nothing to do with whether something is compiled.
    2. **Table code generation** — `Excel2CodeTools` uses the rules in `DevOps/Config/HotfixFile.conf` to decide whether the generated table class is `<Name>.xlsx.cs` or `<Name>.xlsx@hotfix.cs`.

    The old per-file splitting implementation is still present in `Unity3dRoslynBuildTools.cs`, but the **entire block is commented out** and inactive.

## `Assets/*/Table` and table build output

| Content | Location |
|---------|----------|
| Excel source files | `Assets/Resource/Table/*.xlsx`, `Assets/Resource_SVN/Table/*.xlsx` |
| Generated table classes (local tables) | `Assets/Code/Game/Table/Local/<Name>.xlsx.cs` |
| Generated table classes (server tables) | `Assets/Code/Game/Table/Server/<Name>.xlsx.cs` |
| Table classes generated into `Resource_SVN` | `Assets/Resource_SVN/Table/Code/` |
| Build artifact `local.db` | `<output root>/<platform>/local.db` |
| Build artifact `server.db` | `<output root>/server_data/server.db` |

The agreed namespace for table classes is `Game.Data.Local` / `Game.Data.Server`; `BuildTools_Excel2SQLite.CollectTableTypes()` collects types by `Namespace.StartsWith("Game.Data.")`. **Changing the namespace breaks table export.**

See [Table Building](../pipeline/build-table.md) and [Tables (SQLite)](../api/sqlite.md) for details.

## Compilation ownership of business code

There is **no `.asmdef`** under `Assets/Code/`, so everything lands in `Assembly-CSharp` / `Assembly-CSharp-Editor`.

That is exactly why the hotfix collection mechanism works: `ScriptLoder.GetAppDomainHostingTypes()` collects managed types by assembly-name prefix. The whitelist is:

```text
"BDFramework"
"Assembly-CSharp,"          ← 业务热更代码
"Assembly-CSharp-firstpass,"
"UnityEngine.UI"
"Game."
含 "@main"
```

!!! note "The boundary for main-project code"
    Main-project code (shipped inside the client, not hot-updated) should live in its **own asmdef**, so it is not collected by the `Assembly-CSharp` prefix. Main-project code **must not reference hotfix types directly** — communicate only via the event bus or manager forwarding.

## Related pages

- [Asset Load Paths](asset-load-path.md) — how the three asset roots (`CodeRoot` / `SQLRoot` / `ArtRoot`) resolve to real paths
- [AssetBundle Building](../pipeline/build-assetbundle.md) — how `Runtime` directories are collected into ABs
- [Table Building](../pipeline/build-table.md) — Excel format conventions and output
