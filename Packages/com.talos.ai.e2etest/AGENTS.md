# Talos E2E Test Package - Agent Instructions

## Scope

- `Packages/com.talos.ai.e2etest/**` — UPM 包全部代码（Runtime / Editor / Playwright~）

## Package Structure

```
com.talos.ai.e2etest/
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
│   ├── tests/               Playwright 测试用例（fixtures.ts / framework-e2e.spec.ts）
│   ├── tools/               启动脚本
│   │   ├── test-unityplayer.sh   UNITY_PATH → Unity batchmode -quit → 同步执行 → JSON 结果
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
- `Playwright~/` 以 `~` 后缀命名，Unity Editor 自动隐藏.
- `Playwright~/tools/test-unityplayer.sh` 需要 `UNITY_PATH` 环境变量，否则报错退出.

## Testing Tools

| 脚本 | 命令 | 说明 |
|------|------|------|
| Unity Player | `UNITY_PATH=... ./Playwright~/tools/test-unityplayer.sh` | 同步执行 → JSON 结果文件 |
| Android | `./Playwright~/tools/test-android.sh --apk path.apk` | adb install/forward/am start → TCP |
| PC | `./Playwright~/tools/test-pc.sh --exe path.exe` | 启动 exe/app → TCP |

## Reading Order

1. `.github/copilot-instructions.md`
2. This file
3. `Documentation~/README.md`
4. Runtime/ source files
5. `Playwright~/src/` and `Playwright~/tests/`
