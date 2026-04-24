1# BDFramework CI E2E Test Task List
# BDFramework CI E2E 测试任务列表

**Created**: 2026-04-24
**Updated**: 2026-04-24 (session continuation after compaction)
**Branch**: `v4/v-4.0.0`
**Commit**: `55fb3c461` (ADB multi-strategy recovery + shell polling tool)

---

## Current Status Summary / 当前状态总结

### ✅ Completed Tasks / 已完成任务

1. **ADB multi-strategy recovery implemented** (commit 55fb3c461)
   - Strategy 1: `adb reconnect offline` - attempts to reconnect offline devices
   - Strategy 2: `adb kill-server + start-server` - reset ADB server state
   - Strategy 3: Check ADB authorization status (unauthorized devices)
   - Files modified: `connect_androidVirtualDevice.sh`, `node-tools.sh`

2. **Shell polling tool created** (commit 55fb3c461)
   - Created `tc_build_poller.sh` for standalone build status polling
   - Updated SKILL.md and README.md with usage examples

3. **Todolist progress update guidelines added** (commit dadd27224)
   - Added comprehensive progress update guidelines to `.github/copilot-instructions.md`

4. **Android E2E parameter fix committed** (commit 69b79f0 in .test-DevOps)
   - Root cause: `TalosAIBuildAndRunE2ETest` had empty Android E2E parameters
   - Fix: Added `talos.e2e.adb.connect.targets=127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555`
   - Fix: Added `talos.e2e.emulator.type=mumu` parameter
   - Fix: Set `talos.e2e.mumu.auto.start=true`
   - Fix: Added `--emulator-type` to script command line
   - Also updated `TalosAIStep01BaseFlowTest` with Nox port and emulator.type
   - Pushed to remote: .test-DevOps master branch

### ❌ Failed Tasks / 失败任务

1. **Android E2E test #1075** - FAILURE (ROOT CAUSE FIXED)
   - Build URL: http://192.168.0.240:20000/build/1075
   - Duration: ~84 minutes (00:14:50 → 01:38:20)
   - Agent: TCMainAgent
   - Exit code: 2
   - Root cause: `--adb-connect-targets ""` and `--start-mumu ""` were empty in build parameters
   - **FIX COMMITTED**: See commit 69b79f0 in .test-DevOps (pushed to remote)
   - Verification pending: Need to trigger new build to confirm fix works

2. **Android E2E test #1073** - FAILURE (same root cause as #1075)
   - Build URL: http://192.168.0.240:20000/build/1073
   - Same root cause: Empty Android E2E parameters in build configuration

---

## Root Cause Analysis / 根因分析

### Build #1075 Failure Deep Analysis / 构建 #1075 深度失败分析

**Problem**: `emulator-5554` stuck in `offline` state despite multi-strategy ADB recovery attempts
**问题**: `emulator-5554` 卡在 `offline` 状态，多策略 ADB 恢复尝试均无效

**Key Observation from Logs / 日志关键观察**:
```
[00:15:03] python teamcity_e2e_runner.py --platform "android" --adb-connect-targets "" --start-mumu ""
[00:54:37] emulator-5554          offline transport_id:2224  ← Stale connection from previous build
[00:54:37] [Strategy 1/3] Trying adb reconnect offline...
[00:54:37] reconnecting emulator-5554
... (repeated every 10s for 1200s)
[01:38:15] ❌ Device wait timed out (waited 1200s)
```

**ROOT CAUSE IDENTIFIED / 根本原因已确认**:
1. **`--adb-connect-targets ""`** - ADB connect targets is EMPTY! Script did NOT attempt `adb connect 127.0.0.1:16384` or `127.0.0.1:7555`
   // ADB 连接目标参数为空，脚本没有尝试连接 MuMu 端口

2. **`--start-mumu ""`** - MuMu auto-start flag is EMPTY! Script did NOT start MuMu emulator
   // MuMu 自动启动参数为空，脚本没有启动 MuMu 模拟器

3. **Stale connection `emulator-5554`** - Leftover from previous build, offline because MuMu is NOT running
   // 陈旧连接来自之前的构建，offline 因为 MuMu 没有运行

4. **Build #1075 trigger info**:
   - Triggered by user with comment: "验证 MuMu ADB 连接诊断日志"
   - `talos.e2e.platform=android` (user override from default `windows`)
   - `talos.e2e.adb.connect.targets=` (empty, inherited from build config)
   - `talos.e2e.mumu.auto.start=` (empty, inherited from build config)

**Config Comparison / 构建配置对比**:
| Parameter | TalosAIBuildAndRunE2ETest | TalosAIStep01BaseFlowTest |
|-----------|---------------------------|---------------------------|
| `talos.e2e.platform` | `windows` (default) | `windows` |
| `talos.e2e.adb.connect.targets` | **EMPTY** | `127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555` |
| `talos.e2e.emulator.type` | **MISSING** | `nox` |
| `talos.e2e.mumu.auto.start` | **EMPTY** | `true` |

**Strategy Effectiveness / 策略有效性评估**:
| Strategy | Implementation | Result | Analysis |
|----------|---------------|--------|----------|
| 1: `adb reconnect offline` | ✅ Executed multiple times | ❌ Failed | Cannot recover offline device without underlying emulator running |
| 2: `adb kill-server + start-server` | ✅ Executed multiple times | ❌ Failed | ADB server restart cannot fix emulator not running |
| 3: Check authorization | Only diagnostic log | N/A | Device was offline, not unauthorized |

**Fix Required / 修复方案**:

**方案 A (推荐)**: Update `TalosAIBuildAndRunE2ETest` defaults for Android support
- Add `talos.e2e.adb.connect.targets=127.0.0.1:62001,127.0.0.1:16384,127.0.0.1:7555`
- Add `talos.e2e.emulator.type=mumu` parameter
- Set `talos.e2e.mumu.auto.start=true`

**方案 B**: Create separate Android E2E build configuration
- Similar to `TalosAIStep01BaseFlowTest` structure
- Fixed Android platform parameters
- Clearer separation of Windows vs Android E2E

**方案 C**: Add parameter validation in `teamcity_e2e_runner.py`
- Warn if `platform=android` but `adb_connect_targets` is empty
- Suggest proper parameter values in error message

---

## Pending Tasks / 待完成任务

### Task 1: Trigger verification build [IN PROGRESS]
### 任务 1: 触发验证构建 [进行中]

**Priority**: HIGH / 高优先级

**Sub-tasks**:
- [x] Commit fix in .test-DevOps (commit 69b79f0)
- [x] Push to remote
- [x] Trigger new Android E2E build with `platform=android` (Build #1079)
  - Build URL: http://192.168.0.240:20000/build/1079
  - Comment: "验证 Android E2E 参数修复 (commit 69b79f0)"
  - Tag: android-e2e-fix-verification
- [ ] Wait for build completion
- [ ] Verify device connects successfully (not offline)

### Task 2: Investigate MuMu NX actual ADB port [PAUSED]
### 任务 2: 调查 MuMu NX 实际 ADB 端口 [已暂停]

**Priority**: MEDIUM / 中优先级

**Problem**: Current script assumes MuMu NX uses port 16384 (MuMu2 default), but MuMu 12 NX may use different port
**问题**: 当前脚本假设 MuMu NX 使用端口 16384（MuMu2 默认），但 MuMu 12 NX 可能使用不同端口

**Sub-tasks**:
- [x] Review build #1075 logs for ADB connection evidence - device shows as `emulator-5554` (TCP port 5554)
- [ ] Research MuMu 12 NX actual ADB port configuration
- [ ] Check if MuMu 12 NX uses different port naming scheme
- [ ] Verify MuMu emulator VM instance ADB settings via MuMu UI or CLI

**Investigation Notes / 调查笔记**:
- `emulator-5554` serial indicates ADB connected to TCP port 5554 (standard emulator port)
- MuMu NX may not use 16384/7555 ports for instance 0
- Need to check MuMu NX documentation or emulator settings for actual ADB port

### Task 2: Add MuMu emulator restart as recovery strategy
### 任务 2: 添加 MuMu 模拟器重启作为恢复策略

**Priority**: HIGH / 高优先级

**Problem**: ADB-level recovery (reconnect, kill-server) insufficient; need emulator-level restart
**问题**: ADB 级别恢复（reconnect, kill-server）不足；需要模拟器级别重启

**Sub-tasks**:
- [ ] Add Strategy 4: Restart MuMu emulator instance via `mumu-cli control --vmindex 0 shutdown && launch`
- [ ] Add timeout check before triggering emulator restart (e.g., after 60s of offline)
- [ ] Verify emulator restart does not break existing ADB connection attempts
- [ ] Test restart strategy in CI build

### Task 3: Stop accidentally triggered build #1077
### 任务 3: 停止意外触发的构建 #1077

**Priority**: LOW / 低优先级

**Details**: Build #1077 was accidentally triggered while trying to get build #1075 log tail
- Agent: TCMainAgent_03
- Should be stopped to save CI resources

---

## Next Actions / 下一步行动

1. **Trigger verification build**
   - Use TeamCity skill to run `TalosAIBuildAndRunE2ETest` with `platform=android`
   - Wait for build completion with `--wait`
   - Verify device transitions to `device` state (not `offline`)
   - Report build results

2. **If verification passes**:
   - Mark task as completed
   - Archive investigation to `/memories/repo/`

3. **If verification fails**:
   - Analyze new failure logs
   - Investigate MuMu NX actual ADB port configuration

---

## Related Files / 相关文件

- `Packages/com.talosai.e2e/Playwright~/tools/connect_androidVirtualDevice.sh` - ADB connection script
- `Packages/com.talosai.e2e/Playwright~/tools/node-tools.sh` - MuMu startup functions
- `Packages/com.talosai.e2e/Playwright~/tools/teamcity_e2e_runner.py` - E2E runner

---

## Memory Files / 记忆文件

- `/memories/repo/talos-e2e-validation-matrix-2026-04-16.md` - E2E validation matrix
- `/memories/repo/talos-e2e-playmode-gating.md` - PlayMode gating
- `/memories/repo/talos-e2e-editor-handoff-2026-04-15.md` - Editor handoff
- `/memories/repo/adb-reconnect-fix-verification-2026-04-23.md` - ADB reconnect fix (port 62001)
- `/memories/repo/android-e2e-force-stop-debug-fix-2026-04-23.md` - Force-stop fix
- `/memories/repo/talos-e2e-android-mumu-offline.md` - MuMu offline investigation
