# 安装与依赖

## 环境要求

| 项 | 要求 | 依据 |
|----|------|------|
| Unity | **2021.3.x**（当前工程为 `2021.3.45f2c1`） | `ProjectSettings/ProjectVersion.txt` |
| 热更运行时 | **HybridCLR**（v3.0.0 起已移除 ILRuntime） | `Packages/com.code-philosophy.hybridclr` |
| 代码保护 | Obfuz + Obfuz4HybridCLR | `Packages/com.code-philosophy.obfuz*` |
| 序列化/UI 依赖 | Odin Inspector（`Sirenix`）、UniTask、ZString、MessagePack、Protobuf、LitJson、ServiceStack.Text | `Packages/com.popo.bdframework/Nuget/`、`3rdPlugins/` |
| Editor 插件 | `Unity-Logs-Viewer`、`BetterStreamingAssets`、`AssetGraph`（BDFramework 维护分支） | `Packages/`、`Packages/com.popo.bdframework/3rdPlugins/` |

!!! warning "Odin Inspector 是必需依赖"
    `GameBaseConfigProcessor.Config` 的字段全部使用 Odin 特性（`LabelTextAttribute`、`HorizontalGroupAttribute`）渲染配置面板，缺失 Odin 时配置面板无法正常绘制。

## 安装方式

### 方式一：直接使用本仓库（推荐）

本仓库本身就是一个可打开的 Unity 工程，`Packages/com.popo.bdframework` 已在 `manifest.json` 中被引用。

```bash
git clone https://github.com/yimengfan/BDFramework.Core.git
cd BDFramework.Core
git submodule update --init --recursive   # 拉取 HybridCLR 等子模块
```

然后用 Unity 2021.3.x 打开工程根目录。

### 方式二：作为 UPM 包引入到自己的工程

在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.popo.bdframework": "4.0.0"
  }
}
```

!!! note "关于旧版本的 registries 配置"
    v2.x 时代的文档要求额外配置 `com.ourpalm.ilruntime` 的 scoped registry —— **v3 起 ILRuntime 已被完全移除**，不需要再添加该 scope。若沿用旧配置，请删除它。

## 首次运行检查清单

框架在启动时会依次校验以下前置条件，任一失败都会**打错误日志但不抛异常**（表现为"功能静默失效"），建议首次接入时逐项确认：

| # | 检查项 | 失败表现 | 排查位置 |
|---|--------|---------|---------|
| 1 | `BDLauncher.ConfigText` 已赋值 | `GameConfig配置为null,请检查!` | 场景中的 `BDLauncher` 组件 |
| 2 | 场景中存在挂载 `IEnumeratorTool` 的对象 | AB 异步加载**永不推进**，且无报错 | `BDLauncherBridge.Launch()` 会自动补挂 |
| 3 | `ScriptLoderAOT` 能读到 `script/aot_patch/*.zlua.bytes` | `【AOT.Load】HyCLR热更DLL不存在!` | `Assets/StreamingAssets/<platform>/` |
| 4 | 热更 DLL 存在于 `script/hotfix/` | 同上 | 同上 |
| 5 | `art_assets.info` 存在 | `加载不到资源`（不崩溃） | 资源根目录 `art_assets/` |
| 6 | `local.db` 存在 | `DB不存在:<path>` | 版本目录根 |
| 7 | `ENABLE_BDEBUG` 宏已定义 | **所有 `BDebug.*` 调用被编译期消除** | `EditorSetting` 自动添加 |
| 8 | `ENABLE_HYCLR` 宏已定义 | HybridCLR 分支代码不生效 | `EditorSetting` 自动添加 |

!!! danger "`ENABLE_BDEBUG` 是编译期宏"
    `BDebug` 的所有方法都带 `[Conditional("ENABLE_BDEBUG")]`。宏未定义时，连**参数表达式都不会求值**——所以不要把有副作用的表达式写进日志参数里。

    宏由 `Packages/com.popo.bdframework/Editor/EditorAutoSetting/Editor/EditorSetting.cs` 自动写入，一般不需要手工维护。

## 打开工程后的第一步

1. 打开菜单 `BDFrameWork工具箱 → 框架引导`，确认框架环境初始化完成。
2. 打开 `BDFrameWork工具箱 → 框架设置`，检查下面几项：
    - `ClientVersionNum`（客户端版本，决定 `persistentDataPath` 下的版本目录名）
    - `CodeRoot` / `SQLRoot` / `ArtRoot`（三个资源根的来源，Editor 下默认 `AssetLoadPathType.Editor`）
    - `CodeRunMode`（热更代码执行模式，默认 `HyCLR`）
3. 如果要做热更构建，先执行 HybridCLR 的安装与 PreBuild，见[热更代码 HybridCLR](../pipeline/build-hotfix-dll.md)。

## 相关页面

- [目录结构与约定](project-structure.md) —— `Runtime` / `@hotfix` / `Table` 三个特殊目录的语义
- [资源加载寻址](asset-load-path.md) —— Editor 与真机路径差异矩阵
- [启动链路](../architecture/bootstrap.md) —— 精确到方法名的启动时序
