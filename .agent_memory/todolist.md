# Source-Guard Test Fix — Complete
# Source-Guard 测试断言同步 — 已完成

**Updated**: 2026-04-30
**Branch**: `v4/v-4.0.0`
**Commit**: `edadcee0e`

---

## Current Status / 当前状态: ✅ ALL COMPLETE — LOCAL + TEAMCity 全部通过

### E2E Test Suite: 120/120 passed
### BuildTools Test Suite: 199 passed, 1 skipped

### TeamCity Remote Validation: 7/7 SUCCESS

| # | BuildType | Build ID | Status | Agent | URL |
|---|-----------|----------|--------|-------|-----|
| 1 | BuildCodeAndroid | #1291 | SUCCESS | TCMainAgent_02 | http://192.168.0.240:20000/build/1291 |
| 2 | BuildCodeWindows | #1292 | SUCCESS | TCMainAgent | http://192.168.0.240:20000/build/1292 |
| 3 | BuildTable | #1293 | SUCCESS | TCMainAgent_03 | http://192.168.0.240:20000/build/1293 |
| 4 | BuildAssetbundleAndroid | #1294 | SUCCESS | TCMainAgent_03 | http://192.168.0.240:20000/build/1294 |
| 5 | BuildAssetbundleWindows | #1295 | SUCCESS | TCMainAgent_02 | http://192.168.0.240:20000/build/1295 |
| 6 | BuildAssetbundleIos | #1296 | SUCCESS | TCMainAgent | http://192.168.0.240:20000/build/1296 |
| 7 | BuildCodeIos | #1297 | SUCCESS | TCMainAgent_02 | http://192.168.0.240:20000/build/1297 |

### 修复的 5 个 Source-Guard 测试失败

| # | Test | Fix |
|---|------|-----|
| 1 | test_android_tool::test_test_android_connect_targets_calls_adb_connect_before_devices | 将 emulator 清理块从 Step 2.5 移到 Step 3 之后，恢复 connect-before-devices 顺序 |
| 2 | test_host_baseflow_suite_source::test_host_baseflow_suites_keep_preserved_entrypoints | Type.Missing → ExecuteScalarInt, ResolveSqliteProbeRoot → BuildSqliteProbePathOptions |
| 3 | test_host_dependency_boundary::test_bdframework_launcher_owns_debug_talos_bridge | GetMethod 反射断言 → typeof(E2EAutoInit) + CheckAndLaunch() 直接调用 |
| 4 | test_host_dependency_boundary::test_talos_e2e_runner_keeps_public_type_fallback_for_player_discovery | assembly.GetName().Name → asmName 局部变量 |
| 5 | test_playwright_step_screenshot_source::test_pc_tool_defers_window_shape_to_package_defaults_and_keeps_windows_player_log_capture | not-in 断言 → 结构性检查确认 IS_WINDOWS_TEAMCITY 条件保护 |

---

## Remaining Items (Not This Task)

- BuildClientPackageWindows: Agent vswhere.exe pre-existing issue
- File server global_version.info: Pre-existing data issue
