1# BDFramework CI E2E Test Task List
# BDFramework CI E2E 测试任务列表

**Created**: 2026-04-24
**Branch**: `v4/v-4.0.0`
**Commit**: `dadd27224` (updated from 813d493a3)

---

## Current Status Summary / 当前状态总结

### ✅ Completed Tasks / 已完成任务

1. **MuMu ADB stale connection fix implemented** (commit 813d493a3)
   - Added stale ADB connection cleanup in `connect_androidVirtualDevice.sh`
   - Added MuMu service detection and process verification in `node-tools.sh`
   - Fix pushed to remote branch `v4/v-4.0.0`

2. **Android package build #1074** - SUCCESS
   - Build URL: http://192.168.0.240:20000/build/1074

3. **Todolist progress update guidelines added** (commit dadd27224)
   - Added comprehensive progress update guidelines to `.github/copilot-instructions.md`
   - Includes: when to update, cleanup strategy, update granularity
   - Pushed to remote branch `v4/v-4.0.0`

### ❌ Failed Tasks / 失败任务
1. **Android E2E test #1073** - FAILURE
   - Build URL: http://192.168.0.240:20000/build/1073
   - Duration: 1h 24m (09:30:39 - 10:54:41)
   - Exit code: 2

2. **Windows E2E test #1071** - FAILURE (separate issue)
   - Unity app crashed within 3 seconds of startup
   - Not the current focus

---

## Root Cause Analysis / 根因分析

### Build #1073 Failure Analysis / 构建 #1073 失败分析

**Problem**: `emulator-5554` stuck in `offline` state for 1200s timeout
**问题**: `emulator-5554` 卡在 `offline` 状态长达 1200s 直到超时

**Timeline / 时间线**:
- 10:15:54 - MuMu emulator launched via `mumu-cli control --vmindex 0 launch`
- 10:16:11 - MuMu processes started successfully (6 processes)
- 10:17:07 - MuMu wait complete, starting ADB connection
- 10:17:07 - Disconnected stale emulator-5554 connection
- 10:17:19 - Started waiting for device online (max 1200s)
- 10:17:30 - 10:54:34 - `emulator-5554` always offline, reconnection attempts failed
- 10:54:34 - Timeout after 1200s waiting

**Observed ADB Connection Pattern / 观察到的 ADB 连接模式**:
```
127.0.0.1:16384: connected to 127.0.0.1:16384  (sometimes succeeds)
127.0.0.1:7555: connected to 127.0.0.1:7555    (sometimes succeeds)
emulator-5554   offline                         (always offline)
```

**Root Cause / 根本原因**:
- MuMu emulator **process starts successfully**, but **adbd service does not respond properly**
- TCP connection to port 16384/7555 succeeds intermittently, but device remains `offline`
- This suggests adbd inside the emulator VM is not fully initialized or crashed

**Hypothesis / 假设**:
1. MuMu emulator VM boot is incomplete (Android system not fully loaded)
2. adbd service inside emulator is not running or crashed
3. ADB authorization issue (unauthorized device)
4. Resource contention on CI agent (CPU/memory pressure during emulator boot)

---

## Pending Tasks / 待完成任务

### Task 1: Diagnose MuMu emulator adbd issue [IN PROGRESS]
### 任务 1: 诊断 MuMu 模拟器 adbd 问题 [进行中]

**Priority**: HIGH / 高优先级

**Sub-tasks**:
- [ ] Check MuMu emulator Android boot logs
  - Use `adb logcat` or MuMu internal logs to see if Android booted fully
- [ ] Verify adbd service status inside emulator
  - Check if adbd is running: `adb shell ps -A | grep adbd`
  - Check adbd port binding: `adb shell netstat -tlnp | grep 5555`
- [ ] Test manual ADB connection on CI agent
  - SSH into CI agent and manually test `adb connect 127.0.0.1:16384`
- [ ] Check CI agent resource usage during emulator boot
  - CPU, memory, disk I/O

**Investigation Steps**:
1. Add diagnostic logging to `ensure_talos_mumu_running` function
2. Add `adb devices -l` output after each reconnect attempt
3. Add `adb shell getprop` to verify Android boot status
4. Consider adding MuMu-specific diagnostic commands

### Task 2: Fix ADB offline issue
### 任务 2: 修复 ADB offline 问题

**Priority**: HIGH / 高优先级

**Potential Solutions**:
- [ ] Increase MuMu boot wait time before ADB connection
- [ ] Add `adb kill-server` and `adb start-server` before connection
- [ ] Try alternative ADB connection methods:
  - `adb connect 127.0.0.1:16384` (MuMu NX default port)
  - `adb connect 127.0.0.1:7555` (Nox/MuMu legacy port)
- [ ] Add `adb wait-for-device` with timeout
- [ ] Check if MuMu requires specific ADB configuration

### Task 3: Re-run Android E2E test
### 任务 3: 重新运行 Android E2E 测试

**Priority**: MEDIUM / 中优先级
**Dependency**: Task 1 and Task 2 completed

**Steps**:
1. Commit fixes to `v4/v-4.0.0` branch
2. Push to remote
3. Trigger `BDFrameworkCore_TalosAIBuildAndRunE2ETest` with `talos.e2e.platform=android`
4. Monitor build progress
5. Verify test passes

### Task 4: Investigate WindowPreconfig UI issue
### 任务 4: 调查 WindowPreconfig UI 问题

**Priority**: MEDIUM / 中优先级
**Dependency**: Task 3 completed (or at least ADB connection stable)

**Problem**: User reported "app并没有进入 第一个 windows precinfog 界面显示"
**问题**: 用户报告 "app并没有进入 第一个 windows precinfog 界面显示"

**Note**: This is a separate issue from ADB connectivity. Can be investigated in parallel or after E2E test infrastructure is stable.

---

## Next Actions / 下一步行动

1. **Read current ADB connection scripts** to understand the implementation:
   - `Packages/com.talosai.e2e/Playwright~/tools/connect_androidVirtualDevice.sh`
   - `Packages/com.talosai.e2e/Playwright~/tools/node-tools.sh`

2. **Add diagnostic logging** to understand why adbd is not responding:
   - Log MuMu emulator state after launch
   - Add `adb devices -l` verbose output
   - Add retry logic with `adb kill-server` / `adb start-server`

3. **Test fix locally or via CI** by triggering new build

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
