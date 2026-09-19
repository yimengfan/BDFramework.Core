# Installation & Dependencies

## Requirements

| Item | Requirement | Source of truth |
|------|-------------|-----------------|
| Unity | **2021.3.x** (this project uses `2021.3.45f2c1`) | `ProjectSettings/ProjectVersion.txt` |
| Hotfix runtime | **HybridCLR** (ILRuntime was removed in v3.0.0) | `Packages/com.code-philosophy.hybridclr` |
| Code protection | Obfuz + Obfuz4HybridCLR | `Packages/com.code-philosophy.obfuz*` |
| Serialization / UI deps | Odin Inspector (`Sirenix`), UniTask, ZString, MessagePack, Protobuf, LitJson, ServiceStack.Text | `Packages/com.popo.bdframework/Nuget/`, `3rdPlugins/` |
| Editor plugins | `Unity-Logs-Viewer`, `BetterStreamingAssets`, `AssetGraph` (BDFramework fork) | `Packages/`, `Packages/com.popo.bdframework/3rdPlugins/` |

!!! warning "Odin Inspector is a hard dependency"
    Every field of `GameBaseConfigProcessor.Config` uses Odin attributes (`LabelTextAttribute`, `HorizontalGroupAttribute`) to render the config panel. Without Odin the panel cannot be drawn.

## Installing

### Option 1: Use this repository directly (recommended)

The repository *is* a Unity project; `Packages/com.popo.bdframework` is already referenced from `manifest.json`.

```bash
git clone https://github.com/yimengfan/BDFramework.Core.git
cd BDFramework.Core
git submodule update --init --recursive   # pull HybridCLR and other submodules
```

Then open the project root with Unity 2021.3.x.

### Option 2: Add it to your own project as a UPM package

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.popo.bdframework": "4.0.0"
  }
}
```

!!! note "About the legacy registries config"
    v2.x documentation told you to register a scoped registry for `com.ourpalm.ilruntime`. **ILRuntime was fully removed in v3**, so that scope is no longer needed — delete it if you are carrying over an old manifest.

## First-run checklist

The framework validates the following preconditions during startup. Any failure **logs an error but does not throw** (it shows up as "feature silently does nothing"), so verify each item when integrating for the first time:

| # | Check | Symptom on failure | Where to look |
|---|-------|--------------------|---------------|
| 1 | `BDLauncher.ConfigText` is assigned | `GameConfig配置为null,请检查!` | The `BDLauncher` component in the scene |
| 2 | Some object in the scene hosts `IEnumeratorTool` | AB async loads **never progress**, with no error | `BDLauncherBridge.Launch()` auto-attaches it |
| 3 | `ScriptLoderAOT` can read `script/aot_patch/*.zlua.bytes` | `【AOT.Load】HyCLR热更DLL不存在!` | `Assets/StreamingAssets/<platform>/` |
| 4 | Hotfix DLLs exist under `script/hotfix/` | Same as above | Same as above |
| 5 | `art_assets.info` exists | `加载不到资源` (no crash) | Asset root `art_assets/` |
| 6 | `local.db` exists | `DB不存在:<path>` | Version directory root |
| 7 | The `ENABLE_BDEBUG` macro is defined | **Every `BDebug.*` call is compiled away** | Added automatically by `EditorSetting` |
| 8 | The `ENABLE_HYCLR` macro is defined | HybridCLR-specific branches are inactive | Added automatically by `EditorSetting` |

!!! danger "`ENABLE_BDEBUG` is a compile-time macro"
    Every `BDebug` method carries `[Conditional("ENABLE_BDEBUG")]`. When the macro is undefined **even the argument expressions are not evaluated** — so never put a side-effecting expression into a log call.

    The macro is written automatically by `Packages/com.popo.bdframework/Editor/EditorAutoSetting/Editor/EditorSetting.cs`; you normally do not maintain it by hand.

## First steps after opening the project

1. Open the menu `BDFrameWork工具箱 → 框架引导` and confirm the framework environment is initialised.
2. Open `BDFrameWork工具箱 → 框架设置` and check the following:
    - `ClientVersionNum` — client version; determines the version directory name under `persistentDataPath`
    - `CodeRoot` / `SQLRoot` / `ArtRoot` — the three asset root sources; defaults to `AssetLoadPathType.Editor` in the Editor
    - `CodeRunMode` — hotfix code execution mode, defaults to `HyCLR`
3. If you are going to build hotfix content, run the HybridCLR installation and PreBuild first — see [Hotfix Code (HybridCLR)](../pipeline/build-hotfix-dll.md).

## Related pages

- [Project Layout](project-structure.md) — the semantics of the `Runtime` / `@hotfix` / `Table` directories
- [Asset Load Paths](asset-load-path.md) — Editor vs. device path differences
- [Startup Sequence](../architecture/bootstrap.md) — startup timing, down to method names
