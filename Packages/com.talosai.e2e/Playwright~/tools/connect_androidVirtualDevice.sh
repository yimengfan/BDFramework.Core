#!/usr/bin/env bash
# ============================================================================
# Android 虚拟设备连接脚本 — 专门处理宿主机本地 Android 模拟器（MuMu 12 NX、Nox 等）
# 的自动发现、缓存、启动和 ADB 连接全链路流程。
#
# Android virtual-device setup script — handles full lifecycle for host-local
# Android emulators (MuMu 12 NX, Nox, etc.): discovery, path caching, launch,
# and ADB device-online wait.
#
# 使用方式 / Usage modes:
#   1. 被 test-android.sh 等脚本 source 调用（推荐）：
#      Sourced by test-android.sh or similar (recommended):
#        source ./connect_androidVirtualDevice.sh
#      成功后：ADB_CMD 已含 -s <serial>，TALOS_ANDROID_SERIAL / TALOS_MUMU_EXE_PATH
#      / TALOS_MUMU_CLI_PATH 已导出。
#      On success: ADB_CMD includes -s <serial>, TALOS_ANDROID_SERIAL /
#      TALOS_MUMU_EXE_PATH / TALOS_MUMU_CLI_PATH are exported.
#
#   2. 独立站 / Standalone:
#        ./connect_androidVirtualDevice.sh [--connect-targets <t1,t2>] [--start-mumu true]
#      成功后将路径写入 android_device.env（供后续脚本 source）。
#      Writes android_device.env on success for downstream scripts to source.
#
# 必要前置条件 / Prerequisites:
#   - ADB_CMD 数组已设置为 [ "<adb-binary>" ]（无序列号）。
#     ADB_CMD must already be set to ["<adb-binary>"] (no serial).
#   - 或由调用方传入 TALOS_ADB_BIN / --adb-bin。
#     Alternatively the caller passes TALOS_ADB_BIN or --adb-bin.
#
# 路径缓存机制 / Path cache:
#   发现 MuMu exe 后写入 TALOS_MUMU_CACHE_FILE（默认 /tmp/talos_mumu_cache.env）。
#   下次启动直接读取缓存，跳过磁盘扫描，适合 CI 连续构建场景。
#   After MuMu discovery the result is written to TALOS_MUMU_CACHE_FILE
#   (default /tmp/talos_mumu_cache.env).  Subsequent runs load the cache to
#   skip re-scanning, which is important for fast CI builds.
#
# 导出的环境变量 / Exported variables:
#   TALOS_EMULATOR_TYPE   — 模拟器类型（mumu / nox / none）。
#                           Emulator type (mumu / nox / none).
#   TALOS_MUMU_EXE_PATH   — 发现的 MuMu 主入口 exe 绝对路径（仅 MuMu）。
#                           Absolute path to discovered MuMu launcher exe (MuMu only).
#   TALOS_MUMU_CLI_PATH   — mumu-cli.exe 绝对路径（仅 MuMu）。
#                           Absolute path to mumu-cli.exe (MuMu only).
#   TALOS_ANDROID_SERIAL  — 选定的 ADB 设备序列号。
#                           Selected ADB device serial number.
# ============================================================================

# 防止被重复 source / Guard against double-sourcing
if [[ -n "${_TALOS_CONNECT_ANDROID_SOURCED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_TALOS_CONNECT_ANDROID_SOURCED=1

# ---- 确定脚本目录（source 和直接执行两种模式下均有效）----
# Determine script directory in both source and exec modes.
if [[ -n "${BASH_SOURCE[0]:-}" && "${BASH_SOURCE[0]}" != "$0" ]]; then
    _CAV_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
else
    _CAV_SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
fi

# 加载共享工具函数 / Load shared helper functions
# shellcheck source=./node-tools.sh
source "${_CAV_SCRIPT_DIR}/node-tools.sh"

# ============================================================================
# 参数解析 / Argument parsing (独立执行模式 / standalone mode only)
# ============================================================================
_cav_parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --connect-targets) TALOS_ADB_CONNECT_TARGETS="$2"; shift 2 ;;
            --start-mumu)      TALOS_MUMU_AUTO_START="$2";     shift 2 ;;
            --emulator-type)   TALOS_EMULATOR_TYPE="$2";       shift 2 ;;
            --adb-bin)         TALOS_ADB_BIN="$2";             shift 2 ;;
            --timeout)         TALOS_ADB_DEVICE_ONLINE_TIMEOUT="$2"; shift 2 ;;
            --reconnect-interval) _CAV_RECONNECT_INTERVAL="$2"; shift 2 ;;
            --help)
                echo "用法 / Usage: $0 [--connect-targets <t1,t2>] [--start-mumu true|false] [--emulator-type mumu|nox|none]"
                echo "  --connect-targets   逗号分隔的 ADB TCP 目标 / Comma-separated ADB TCP targets"
                echo "  --start-mumu        是否自动启动 MuMu (true|false) / Auto-start MuMu emulator"
                echo "  --emulator-type     模拟器类型: mumu (默认), nox, none / Emulator type: mumu (default), nox, none"
                echo "  --adb-bin           ADB 可执行文件路径 / Path to adb binary"
                echo "  --timeout           设备上线等待超时秒数 / Device-online wait timeout (seconds)"
                echo "  --reconnect-interval  重连间隔秒数（默认 30） / Reconnect interval seconds (default 30)"
                exit 0
                ;;
            *) echo "未知参数 / Unknown argument: $1" >&2 ;;
        esac
    done
}

# 仅在独立执行时解析参数 / Only parse args in standalone mode
if [[ "${BASH_SOURCE[0]:-}" == "$0" ]]; then
    _cav_parse_args "$@"
fi

# ============================================================================
# 路径缓存 — 读取 / Path cache — load
# 发现 MuMu 路径后会写入缓存，下次直接读取跳过扫描。
# Cache is populated after MuMu discovery; reload on next run to skip scanning.
# ============================================================================
_CAV_CACHE_FILE="${TALOS_MUMU_CACHE_FILE:-/tmp/talos_mumu_cache.env}"

_cav_load_cache() {
    # 若 TALOS_MUMU_EXE_PATH 已由外部设置，不需要加载缓存。
    # If TALOS_MUMU_EXE_PATH was already set externally, no need to load cache.
    if [[ -n "${TALOS_MUMU_EXE_PATH:-}" ]]; then
        return 0
    fi
    if [[ -f "${_CAV_CACHE_FILE}" ]]; then
        # shellcheck source=/dev/null
        source "${_CAV_CACHE_FILE}" 2>/dev/null || true
        if [[ -n "${TALOS_MUMU_EXE_PATH:-}" && -f "${TALOS_MUMU_EXE_PATH}" ]]; then
            echo "    [缓存命中] 从缓存读取 MuMu 路径: ${TALOS_MUMU_EXE_PATH}"
            echo "    [Cache hit] Loaded MuMu path from cache: ${TALOS_MUMU_EXE_PATH}"
            return 0
        fi
        # 缓存文件存在但路径已失效，清除 / Cache exists but path gone — clear
        echo "    [缓存失效] 缓存中的 MuMu 路径已失效，清除缓存重新扫描..."
        echo "    [Cache stale] Cached MuMu path is gone; clearing cache and re-scanning..."
        unset TALOS_MUMU_EXE_PATH TALOS_MUMU_CLI_PATH 2>/dev/null || true
        rm -f "${_CAV_CACHE_FILE}" 2>/dev/null || true
    fi
    return 1
}

_cav_load_cache || true

# ============================================================================
# 步骤 1：模拟器自动启动（如已启用）
# Step 1: Emulator auto-start (when enabled)
#
# 支持两种触发方式（向后兼容）：
# Two trigger modes supported (backward compatible):
#   - TALOS_MUMU_AUTO_START=true   → 等同于 TALOS_EMULATOR_TYPE=mumu（旧接口，仍有效）
#                                   Equivalent to TALOS_EMULATOR_TYPE=mumu (legacy, still works)
#   - TALOS_EMULATOR_TYPE=nox      → 使用 Nox 模拟器（需手动预启动）
#                                   Use Nox emulator (must be pre-started manually)
# ============================================================================
if [[ "${TALOS_MUMU_AUTO_START:-}" == "true" && -z "${TALOS_EMULATOR_TYPE:-}" ]]; then
    # 向后兼容：--start-mumu true 但未设置 --emulator-type，默认使用 mumu。
    # Backward compat: --start-mumu true without --emulator-type defaults to mumu.
    export TALOS_EMULATOR_TYPE="mumu"
fi
if [[ -n "${TALOS_EMULATOR_TYPE:-}" ]]; then
    echo ""
    echo ">>> [connect_androidVirtualDevice] 模拟器自动启动流程 (type=${TALOS_EMULATOR_TYPE})..."
    echo ">>> [connect_androidVirtualDevice] Emulator auto-start flow (type=${TALOS_EMULATOR_TYPE})..."
    # ensure_talos_emulator_running 内部会根据类型调用 MuMu/Nox 检测。
    # ensure_talos_emulator_running dispatches to MuMu/Nox detection based on type.
    TALOS_MUMU_CACHE_FILE="${_CAV_CACHE_FILE}" ensure_talos_emulator_running
fi

# ============================================================================
# 步骤 2：解析 ADB 命令（若调用方尚未设置 ADB_CMD）
# Step 2: Resolve ADB command (if caller has not yet set ADB_CMD)
# ============================================================================
if [[ -z "${ADB_CMD[*]+x}" ]] || [[ "${#ADB_CMD[@]}" -eq 0 ]]; then
    ensure_talos_adb_tooling || {
        echo "❌ [connect_androidVirtualDevice] ADB 工具初始化失败"
        echo "❌ [connect_androidVirtualDevice] ADB tooling init failed"
        exit 1
    }
    ADB_CMD=("${TALOS_ADB_BIN}")
fi

# ============================================================================
# 步骤 2.5：清理陈旧的 ADB 连接（避免误判 "already connected"）
# Step 2.5: Clean up stale ADB connections (avoid false "already connected")
# ============================================================================
echo ""
echo ">>> [connect_androidVirtualDevice] 清理陈旧 ADB 连接..."
echo ">>> [connect_androidVirtualDevice] Cleaning up stale ADB connections..."
if [[ -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
    IFS=',' read -ra _cav_stale_targets <<< "${TALOS_ADB_CONNECT_TARGETS}"
    for _cav_st in "${_cav_stale_targets[@]}"; do
        _cav_st="$(printf '%s' "${_cav_st}" | tr -d '[:space:]')"
        [[ -z "${_cav_st}" ]] && continue
        "${ADB_CMD[@]}" disconnect "${_cav_st}" 2>/dev/null || true
        echo "    已断开: ${_cav_st}"
    done
fi
# 清理 emulator-* 格式的陈旧连接 / Clean up stale emulator-* connections
"${ADB_CMD[@]}" devices 2>/dev/null | grep 'emulator' | while read -r _cav_em_line; do
    _cav_em_serial="$(printf '%s' "${_cav_em_line}" | awk '{print $1}')"
    if [[ -n "${_cav_em_serial}" ]]; then
        "${ADB_CMD[@]}" disconnect "${_cav_em_serial}" 2>/dev/null || true
        echo "    已断开陈旧模拟器连接: ${_cav_em_serial}"
    fi
done
echo "    ✅ ADB 连接清理完成"

# ============================================================================
# 步骤 3：主动连接 ADB TCP 目标（在设备探测前）
# Step 3: Proactively connect ADB TCP targets before device detection
# ============================================================================
if [[ -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
    echo ""
    echo ">>> [connect_androidVirtualDevice] 初始 ADB 连接 TCP 目标..."
    echo ">>> [connect_androidVirtualDevice] Initial ADB connect to TCP targets..."
    ensure_talos_adb_connect_targets "${TALOS_ADB_CONNECT_TARGETS}"
fi

# ============================================================================
# 步骤 4：等待设备上线（带超时 + 定期重连 + reconnect offline）
# Step 4: Wait for device online (timeout + periodic reconnect + reconnect offline)
#
# 模拟器 adbd 上线策略 / Emulator adbd online strategy:
#   a) 每 30s 重连 TCP 目标（旧连接在 adbd 上线前会记录为 offline）。
#      Reconnect TCP targets every 30s (stale connections appear offline until adbd is live).
#   b) 若 ADB 设备列表中有 offline 的 emulator-* 条目，执行 adb reconnect offline。
#      If any emulator-* entry is offline, run "adb reconnect offline".
#   c) 若已有 device 状态的条目，立即退出循环。
#      Exit immediately if any device is in online state.
# ============================================================================
_DEVICE_WAIT_MAX="${TALOS_ADB_DEVICE_ONLINE_TIMEOUT:-1200}"
_CAV_RECONNECT_INTERVAL="${_CAV_RECONNECT_INTERVAL:-30}"
echo ""
echo ">>> [connect_androidVirtualDevice] 等待 Android 设备上线（最多 ${_DEVICE_WAIT_MAX}s，每 ${_CAV_RECONNECT_INTERVAL}s 重连）..."
echo ">>> [connect_androidVirtualDevice] Waiting for Android device online (max ${_DEVICE_WAIT_MAX}s, reconnect every ${_CAV_RECONNECT_INTERVAL}s)..."

_cav_device_count=0
_cav_waited=0
_cav_last_reconnect=-1

while [[ ${_cav_waited} -lt ${_DEVICE_WAIT_MAX} ]]; do
    _cav_device_count=$("${ADB_CMD[@]}" devices 2>/dev/null | grep -c 'device$' || true)
    if [[ ${_cav_device_count} -gt 0 ]]; then
        break
    fi

    # 诊断：显示当前设备状态 / Diagnostic: show current device states
    _cav_offline=$("${ADB_CMD[@]}" devices 2>/dev/null | grep -c 'offline' || true)
    if [[ ${_cav_offline} -gt 0 ]]; then
        echo "    设备 offline，等待 adbd 就绪... (${_cav_waited}/${_DEVICE_WAIT_MAX}s)"
        echo "    Devices offline, waiting for adbd... (${_cav_waited}/${_DEVICE_WAIT_MAX}s)"

        # 诊断：打印完整的设备列表详情 / Diagnostic: print full device list details
        echo "    === ADB 设备详情 / ADB devices detail ==="
        "${ADB_CMD[@]}" devices -l 2>/dev/null || true
        echo "    === 设备详情结束 ==="

        # 若存在 offline 的 emulator-* 条目，尝试多种恢复策略。
        # If any emulator-* entry is offline, try multiple recovery strategies.
        if "${ADB_CMD[@]}" devices 2>/dev/null | grep -q 'emulator.*offline'; then
            _cav_em_serial=$("${ADB_CMD[@]}" devices 2>/dev/null | grep 'emulator.*offline' | awk '{print $1}' | head -1)
            echo "    检测到 emulator offline: ${_cav_em_serial}"
            echo "    Detected emulator offline: ${_cav_em_serial}"

            # 策略 1: adb reconnect offline
            # Strategy 1: adb reconnect offline
            echo "    [策略 1/3] 尝试 adb reconnect offline..."
            echo "    [Strategy 1/3] Trying adb reconnect offline..."
            "${ADB_CMD[@]}" reconnect offline 2>/dev/null || true
            sleep 2

            # 检查是否恢复 / Check if recovered
            if "${ADB_CMD[@]}" devices 2>/dev/null | grep -q 'device$'; then
                echo "    ✅ reconnect offline 成功，设备已上线"
                echo "    ✅ reconnect offline succeeded, device online"
                break
            fi

            # 策略 2: adb kill-server + start-server（重置 ADB 服务端状态）
            # Strategy 2: adb kill-server + start-server (reset ADB server state)
            echo "    [策略 2/3] 尝试 adb kill-server + start-server..."
            echo "    [Strategy 2/3] Trying adb kill-server + start-server..."
            "${ADB_CMD[@]}" kill-server 2>/dev/null || true
            sleep 2
            "${ADB_CMD[@]}" start-server 2>/dev/null || true
            sleep 3

            # 重新连接 TCP 目标 / Reconnect TCP targets
            if [[ -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
                echo "    重新连接 TCP 目标..."
                IFS=',' read -ra _cav_retry_targets <<< "${TALOS_ADB_CONNECT_TARGETS}"
                for _cav_rt in "${_cav_retry_targets[@]}"; do
                    _cav_rt="$(printf '%s' "${_cav_rt}" | tr -d '[:space:]')"
                    [[ -z "${_cav_rt}" ]] && continue
                    "${ADB_CMD[@]}" disconnect "${_cav_rt}" 2>/dev/null || true
                    _cav_rc=$("${ADB_CMD[@]}" connect "${_cav_rt}" 2>/dev/null || true)
                    echo "        ${_cav_rt}: ${_cav_rc}"
                done
            fi
            sleep 2

            # 检查是否恢复 / Check if recovered
            if "${ADB_CMD[@]}" devices 2>/dev/null | grep -q 'device$'; then
                echo "    ✅ kill-server + start-server 成功，设备已上线"
                echo "    ✅ kill-server + start-server succeeded, device online"
                break
            fi

            # 策略 3: 检查 ADB 授权状态（unauthorized 设备）
            # Strategy 3: check ADB authorization status (unauthorized devices)
            if "${ADB_CMD[@]}" devices 2>/dev/null | grep -q 'unauthorized'; then
                echo "    [策略 3/3] 检测到 unauthorized 设备，可能需要 ADB 授权..."
                echo "    [Strategy 3/3] Detected unauthorized device, may need ADB authorization..."
                echo "    提示：在 CI 环境中，请确保 ~/.android/adbkey 存在且已授权"
                echo "    Hint: in CI, ensure ~/.android/adbkey exists and is authorized"
            fi
        fi
    else
        echo "    等待设备出现... (${_cav_waited}/${_DEVICE_WAIT_MAX}s)"
        echo "    Waiting for device to appear... (${_cav_waited}/${_DEVICE_WAIT_MAX}s)"

        # 每 60s 打印一次设备列表 / Print device list every 60s
        if [[ $((_cav_waited % 60)) -eq 0 && ${_cav_waited} -gt 0 ]]; then
            echo "    === ADB 设备列表 / ADB devices list ==="
            "${ADB_CMD[@]}" devices -l 2>/dev/null || true
            echo "    === 设备列表结束 ==="
        fi
    fi

    # 每 _CAV_RECONNECT_INTERVAL 秒重连 TCP 目标一次（比 60s 更密集）。
    # Reconnect TCP targets every _CAV_RECONNECT_INTERVAL seconds (more frequent than 60s).
    _cav_should_reconnect=0
    if [[ ${_cav_waited} -gt 0 ]]; then
        _cav_intervals=$(( _cav_waited / _CAV_RECONNECT_INTERVAL ))
        if [[ ${_cav_intervals} -gt ${_cav_last_reconnect} ]]; then
            _cav_should_reconnect=1
            _cav_last_reconnect=${_cav_intervals}
        fi
    fi

    if [[ ${_cav_should_reconnect} -eq 1 && -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
        echo "    >>> 重连 ADB TCP 目标（第 ${_cav_last_reconnect} 次 / reconnect #${_cav_last_reconnect})..."
        IFS=',' read -ra _cav_targets <<< "${TALOS_ADB_CONNECT_TARGETS}"
        for _cav_t in "${_cav_targets[@]}"; do
            _cav_t="$(printf '%s' "${_cav_t}" | tr -d '[:space:]')"
            [[ -z "${_cav_t}" ]] && continue
            "${ADB_CMD[@]}" disconnect "${_cav_t}" 2>/dev/null || true
            _cav_rc=$("${ADB_CMD[@]}" connect "${_cav_t}" 2>/dev/null || true)
            echo "        ${_cav_t}: ${_cav_rc}"
        done
        sleep 3
    fi

    sleep 10
    _cav_waited=$(( _cav_waited + 10 ))
done

if [[ ${_cav_device_count} -eq 0 ]]; then
    echo ""
    echo "❌ [connect_androidVirtualDevice] 设备等待超时（已等待 ${_cav_waited}s）"
    echo "❌ [connect_androidVirtualDevice] Device wait timed out (waited ${_cav_waited}s)"
    echo ">>> 当前设备列表 / Current device list:"
    "${ADB_CMD[@]}" devices 2>/dev/null || true
    exit 1
fi

# 显示已上线的设备 / Show online device
_cav_online_line=$("${ADB_CMD[@]}" devices 2>/dev/null | grep 'device$' | awk 'NR==1' || true)
echo ""
echo "    ✅ 设备已上线: ${_cav_online_line} (等待 ${_cav_waited}s)"
echo "    ✅ Device online: ${_cav_online_line} (waited ${_cav_waited}s)"

# ============================================================================
# 步骤 5：自动选择 ADB 序列号（多设备时防止 "more than one device" 错误）
# Step 5: Auto-select ADB serial (prevents "more than one device" error)
# ============================================================================
if [[ -z "${ADB_SERIAL:-}" ]]; then
    _cav_serial=$("${ADB_CMD[@]}" devices 2>/dev/null | grep 'device$' | awk 'NR==1{print $1}' | tr -d '\r\n') || true
    if [[ -n "${_cav_serial}" ]]; then
        # 重置 ADB_CMD 为 [<adb-bin>, -s, <serial>]，保留 adb 二进制。
        # Reset ADB_CMD to [<adb-bin>, -s, <serial>], keeping the adb binary.
        ADB_CMD=("${ADB_CMD[0]}" "-s" "${_cav_serial}")
        ADB_SERIAL="${_cav_serial}"
        echo "    自动选择序列号: ${_cav_serial}"
        echo "    Auto-selected serial: ${_cav_serial}"
    fi
fi

# 导出选定的序列号供调用脚本使用 / Export serial for caller scripts
export TALOS_ANDROID_SERIAL="${ADB_SERIAL:-}"

# ============================================================================
# 独立执行模式：将结果写入 android_device.env 供其他脚本 source
# Standalone mode: write result to android_device.env for other scripts to source
# ============================================================================
if [[ "${BASH_SOURCE[0]:-}" == "$0" ]]; then
    _CAV_ENV_OUT="${_CAV_SCRIPT_DIR}/android_device.env"
    {
        echo "# Generated by connect_androidVirtualDevice.sh — $(date)"
        echo "# 勿手动编辑，由脚本自动写入 / Do not edit manually; auto-written by script."
        echo "export TALOS_ANDROID_SERIAL='${TALOS_ANDROID_SERIAL:-}'"
        echo "export TALOS_EMULATOR_TYPE='${TALOS_EMULATOR_TYPE:-}'"
        echo "export TALOS_MUMU_EXE_PATH='${TALOS_MUMU_EXE_PATH:-}'"
        echo "export TALOS_MUMU_CLI_PATH='${TALOS_MUMU_CLI_PATH:-}'"
    } > "${_CAV_ENV_OUT}"
    echo ""
    echo ">>> 设备连接结果已写入: ${_CAV_ENV_OUT}"
    echo ">>> Device connection result written to: ${_CAV_ENV_OUT}"
    echo "    source ${_CAV_ENV_OUT}  # 在其他脚本中加载 / load in other scripts"
fi
