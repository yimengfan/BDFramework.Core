# Talos E2E Test Package - Agent Instructions

## Scope

- `Packages/com.talosai.e2e/**` — UPM 包全部代码（Runtime / Editor / Playwright~）

## Package Structure

```
com.talosai.e2e/
├── Runtime/
│   ├── Transport/          TCP 传输层（Protocol.cs / TalosTcpServer.cs / TalosTcpClient.cs）
│   ├── TestRunner/         测试运行器（E2ETestRunner.cs / TalosE2EBootstrap.cs / E2EAutoInit.cs / DebugBuildMarker.cs）
│   └── Tests/              框架核心测试用例（LaunchTests / AssetLoadTests / DownloadUpdateTests / SqliteTests / AssetTraversalTests）
├── Editor/
│   └── E2EEditorTools.cs   Editor 菜单工具 + BatchMode 入口
│       ├── LaunchE2EBatchMode()      PlayMode 模式（需要有效许可证）
│       ├── LaunchE2EEditorOnly()     TCP 模式（不进入 PlayMode，CI 用）
│       └── RunE2EAndExport()         同步执行模式（直接输出 JSON，本地验证用）
├── Playwright~/             Playwright 编排层（Unity 内隐藏，~ 后缀）
│   ├── src/                 TypeScript 库（unity-connector.ts / device-manager.ts）
│   ├── tests/               Playwright 测试用例（fixtures.ts / testFrameworkCore-e2e.spec.ts / testBaseFlow-EditorPlayer-e2e.spec.ts）
│   ├── tools/               启动脚本
│   │   ├── test-editorplayer.sh  UNITY_PATH → Unity GUI Editor → Playwright EditorPlayer 测试
│   │   ├── test-android.sh       APK 安装 → ADB forward → TCP 连接
│   │   └── test-pc.sh            EXE / .app 启动 → TCP 连接
│   ├── playwright.config.ts
│   └── package.json
├── Documentation~/README.md 方案文档
└── package.json
```

## Execution Modes

### 1. 同步执行模式（RunE2EAndExport）—— 默认模式

```bash
Unity -batchmode -nographics -quit -projectPath <project> \
  -executeMethod Talos.E2E.Editor.E2EEditorTools.RunE2EAndExport \
  -talosForceE2E -talosOutput results.json
```

- 不进入 PlayMode，不需要 MonoBehaviour
- 直接在 Editor 中初始化框架（配置、资源、SQLite）
- 同步执行所有 [E2ETest] 测试用例
- 结果写入 JSON 文件（默认: talos_e2e_results.json）
- 使用 `-quit` 参数，避免 Unity 主循环中的许可证检查崩溃
- **适用于：本地验证、CI 环境**

### 2. TCP 模式（LaunchE2EEditorOnly）—— 需要有效许可证

```bash
Unity -batchmode -projectPath <project> \
  -executeMethod Talos.E2E.Editor.E2EEditorTools.LaunchE2EEditorOnly \
  -talosForceE2E -talosPort 10002
```

- 不进入 PlayMode，直接启动 TCP 服务
- Playwright 通过 TCP 协议连接并发送指令
- **需要 Unity Pro 许可证有效（主循环需要持续运行）**

### 3. PlayMode 模式（LaunchE2EBatchMode）—— 需要有效许可证

```bash
Unity -batchmode -projectPath <project> \
  -executeMethod Talos.E2E.Editor.E2EEditorTools.LaunchE2EBatchMode \
  -talosForceE2E -talosPort 10002
```

- 进入 PlayMode，走完整的框架启动流程（BDLauncher → ScriptLoderAOT → BDLauncherBridge）
- 适用于真机/Player 模式的完整测试
- **需要 Unity Pro 许可证有效**

## Rules

- All comments and docstrings must be written in Chinese (中文).
- Test methods must be static and use `[E2ETest]` attribute.
- Use exceptions for test failures（throw new Exception），不使用框架自带 Assert.
- TCP protocol changes must update both Unity（Protocol.cs）and TypeScript（unity-connector.ts）.
- New test suites go under `Runtime/Tests/` with `[E2ETest(suite: "name")]` attribute.
- Playwright tests go under `Playwright~/tests/`.
- E2E 调用 Unity Editor 功能前，优先搜索 `Documentation~/Unity3dDocument_2021_3/en/ScriptReference/` 与 `Documentation~/Unity3dDocument_2021_3/en/Manual/`，先复用 Unity 官方 API，再决定是否需要项目侧复杂封装。

- Talos E2E 的 Editor 控制面必须以 Playwright 作为主编排方。默认应通过带缓存的通用反射网关调用 Unity 官方 API、框架 API 与项目 API；Unity 侧不得为单个基础 API 继续增加薄封装命令。只有跨多步、带状态协调、恢复逻辑或容错逻辑的复杂流程，才允许在 Unity 侧封装，并必须在代码注释中说明保留原因。

- `Playwright~/` 以 `~` 后缀命名，Unity Editor 自动隐藏.
- `Playwright~/tools/test-editorplayer.sh` 需要 `UNITY_PATH` 环境变量，否则报错退出.
- Editor 控制面必须遵循“Playwright 主控、Unity 反射网关优先”的原则。基础 Editor API 调用应在 Playwright 侧通过通用反射入口组合，Unity 侧只保留复杂流程封装。
- Editor 反射网关必须缓存类型解析和成员解析结果，避免在高频轮询场景下重复遍历程序集或重复做重载匹配。
- `Playwright~/tests/` 中的 E2E 测试文件名使用 `测试描述-平台-e2e.spec.ts` 形式；未写平台段时表示该用例按设计可在所有平台复用。
- 显式平台后缀当前使用 `EditorPlayer`、`Android`、`Windows`、`MacOS`；无平台后缀会在 `batchmode`、`unityplayer`、`android`、`windows`、`macos` 项目统一执行。
- `EditorPlayer` 后缀用于依赖 Editor 控制面的用例；这类用例应优先保持基础流程与 batchmode 可复用，只在确实依赖 Editor 生命周期或控制面能力时才单独命名。

## Testing Tools

| 脚本 | 命令 | 说明 |
|------|------|------|
| EditorPlayer | `UNITY_PATH=... ./Playwright~/tools/test-editorplayer.sh` | Unity GUI Editor + Playwright Editor 控制面测试 |
| Android | `./Playwright~/tools/test-android.sh --apk path.apk` | adb install/forward/am start → TCP |
| PC | `./Playwright~/tools/test-pc.sh --exe path.exe` | 启动 exe/app → TCP |

## Reading Order

1. `.github/copilot-instructions.md`
2. `AI_RULES_INDEX.md`
3. `.github/instructions/e2e.instructions.md`
4. This file
5. `Documentation~/README.md`
6. Runtime/ source files
7. `Playwright~/src/` and `Playwright~/tests/`
