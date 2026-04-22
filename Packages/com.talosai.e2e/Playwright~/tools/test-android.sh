#!/usr/bin/env bash
# ============================================================================
# Talos E2E 测试——Android 模式启动脚本。
#
# 流程：
#   1. 检查 APK 路径和 ADB 可用性
#   2. 安装 APK 到设备
#   3. 设置 ADB 端口转发
#   4. 启动应用
#   5. 启动 Playwright 测试，通过 TCP 连接到设备
#   6. 测试完成后清理
#
# 使用方式：
#   ./tools/test-android.sh --apk /path/to/app.apk
#
# 环境变量：
#   APK_PATH   — APK 文件路径（或通过 --apk 参数）
#   ADB_SERIAL — ADB 设备序列号（多设备时使用）
#   UNITY_PORT — TCP 端口，默认 10002
#   PACKAGE    — Android 包名，默认 com.popo.bdframework
#   ACTIVITY   — 启动 Activity，默认 com.unity3d.player.UnityPlayerActivity
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLAYWRIGHT_DIR="$(dirname "${SCRIPT_DIR}")"
# shellcheck source=./node-tools.sh
source "${SCRIPT_DIR}/node-tools.sh"

# ======== 默认参数 ========
APK_PATH="${APK_PATH:-}"
ADB_SERIAL="${ADB_SERIAL:-}"
# MuMu 模拟器等宿主本机模拟器的 ADB 连接目标，逗号分隔，如 127.0.0.1:16384,127.0.0.1:7555,127.0.0.1:62001。
# Comma-separated ADB connect targets for host-local emulators such as MuMu, Nox. E.g. 127.0.0.1:16384,127.0.0.1:7555,127.0.0.1:62001
TALOS_ADB_CONNECT_TARGETS="${TALOS_ADB_CONNECT_TARGETS:-}"
# MuMu 模拟器自动启动开关（true 启用）：先检测进程，再搜索安装目录，找到后后台启动并等待初始化。
# MuMu auto-start flag (set to "true" to enable): checks process list, searches install dirs, starts in background.
TALOS_MUMU_AUTO_START="${TALOS_MUMU_AUTO_START:-}"
# 模拟器类型选择（mumu / nox / none），控制自动启动逻辑。mumu 为默认值（向后兼容）。
# Emulator type selection (mumu / nox / none), controls auto-start logic. mumu is default (backward compatible).
TALOS_EMULATOR_TYPE="${TALOS_EMULATOR_TYPE:-}"
# ADB daemon 重启后的基础稳定等待秒数；默认 10 秒，测试可覆写为 0 以避免慢测。
# Baseline wait after an ADB daemon restart; defaults to 10s and can be overridden to 0 in tests.
TALOS_ADB_RECONNECT_BASELINE_SLEEP_SECONDS="${TALOS_ADB_RECONNECT_BASELINE_SLEEP_SECONDS:-10}"
# ADB 重试失败后的再次重连等待秒数；默认 5 秒，测试可覆写为 0。
# Retry wait after a failed reconnect attempt; defaults to 5s and can be overridden to 0 in tests.
TALOS_ADB_RECONNECT_RETRY_SLEEP_SECONDS="${TALOS_ADB_RECONNECT_RETRY_SLEEP_SECONDS:-5}"
# 失败采集 logcat 与清理阶段的 ADB 命令超时；用于避免 TeamCity 在 ADB 卡死时整步假死。
# Timeout for failure-path logcat export and cleanup ADB commands so TeamCity does not hang when ADB stalls.
TALOS_ADB_LOGCAT_TIMEOUT_SECONDS="${TALOS_ADB_LOGCAT_TIMEOUT_SECONDS:-20}"
TALOS_ADB_CLEANUP_TIMEOUT_SECONDS="${TALOS_ADB_CLEANUP_TIMEOUT_SECONDS:-10}"
# APK 安装命令的单次硬超时；用于拦截 adb install 在设备传输异常时长时间无响应的问题。
# Hard timeout for a single APK install command so adb install cannot stall for hours when device transport gets stuck.
TALOS_APK_INSTALL_TIMEOUT_SECONDS="${TALOS_APK_INSTALL_TIMEOUT_SECONDS:-180}"
UNITY_PORT="${UNITY_PORT:-10002}"
PACKAGE="${PACKAGE:-}"
ACTIVITY="${ACTIVITY:-}"
PLAYWRIGHT_TEST_FILE="${PLAYWRIGHT_TEST_FILE:-}"
ANDROID_LOGCAT_FILE="${PLAYWRIGHT_DIR}/test-results/android-logcat.txt"
PLAYER_LOG_ARCHIVE_DIR="${PLAYWRIGHT_DIR}/test-results/playerlogs"

# 以硬超时执行命令，避免外部工具在 TeamCity 上无限挂起。
# Run a command with a hard timeout so external tools cannot hang forever in TeamCity.
talos_run_command_with_timeout() {
    local _timeout_seconds="$1"
    local _output_file="$2"
    shift 2

    local _pid _elapsed _rc
    "$@" > "${_output_file}" 2>&1 &
    _pid=$!
    _elapsed=0

    while kill -0 "${_pid}" 2>/dev/null; do
        if [[ ${_elapsed} -ge ${_timeout_seconds} ]]; then
            if command -v pkill >/dev/null 2>&1; then
                pkill -TERM -P "${_pid}" 2>/dev/null || true
                pkill -KILL -P "${_pid}" 2>/dev/null || true
            fi
            kill "${_pid}" 2>/dev/null || true
            wait "${_pid}" 2>/dev/null || true
            return 124
        fi
        sleep 1
        _elapsed=$((_elapsed + 1))
    done

    wait "${_pid}" 2>/dev/null && _rc=0 || _rc=$?
    return ${_rc}
}

# 对原始 adb 命令增加硬超时；用于不需要自动重连的简单采集或测试桩路径。
# Add a hard timeout around raw adb commands for simple collection paths that do not need reconnect logic.
talos_run_adb_command_with_timeout() {
    local _timeout_seconds="$1"
    shift

    local _output_file _rc
    _output_file="$(mktemp "${PLAYWRIGHT_DIR}/test-results/adb-command.XXXXXX.log")"
    if talos_run_command_with_timeout "${_timeout_seconds}" "${_output_file}" "${ADB_CMD[@]}" "$@"; then
        cat "${_output_file}" 2>/dev/null || true
        rm -f "${_output_file}"
        return 0
    else
        _rc=$?
    fi

    cat "${_output_file}" 2>/dev/null || true
    rm -f "${_output_file}"
    return ${_rc}
}

# 对 adb_with_reconnect 增加硬超时；用于失败路径采集真实日志时同时处理 daemon 重启。
# Add a hard timeout around adb_with_reconnect so failure-path collection can still recover from daemon restarts.
talos_run_adb_with_reconnect_timeout() {
    local _timeout_seconds="$1"
    shift

    local _output_file _rc
    _output_file="$(mktemp "${PLAYWRIGHT_DIR}/test-results/adb-reconnect.XXXXXX.log")"
    if talos_run_command_with_timeout "${_timeout_seconds}" "${_output_file}" adb_with_reconnect "$@"; then
        cat "${_output_file}" 2>/dev/null || true
        rm -f "${_output_file}"
        return 0
    else
        _rc=$?
    fi

    cat "${_output_file}" 2>/dev/null || true
    rm -f "${_output_file}"
    return ${_rc}
}

# 在启动前清空并在关键失败点导出 logcat，保证 TeamCity artifact 能带回同一次 Android 启动期日志。
# Clear logcat before launch and export it on key failure paths so the TeamCity artifact contains the same Android startup session logs.
capture_android_logcat() {
    local _logcat_output _logcat_rc
    _logcat_output="$(talos_run_adb_with_reconnect_timeout "${TALOS_ADB_LOGCAT_TIMEOUT_SECONDS}" logcat -d -v threadtime)" && _logcat_rc=0 || _logcat_rc=$?

    if [[ ${_logcat_rc} -eq 0 ]]; then
        printf '%s' "${_logcat_output}" > "${ANDROID_LOGCAT_FILE}"
        echo ""
        echo ">>> Android logcat (tail 200)"
        tail -n 200 "${ANDROID_LOGCAT_FILE}" || true
    elif [[ ${_logcat_rc} -eq 124 ]]; then
        printf '%s' "${_logcat_output}" > "${ANDROID_LOGCAT_FILE}" 2>/dev/null || true
        echo ""
        echo ">>> Android logcat 导出超时 / Android logcat export timed out"
    else
        printf '%s' "${_logcat_output}" > "${ANDROID_LOGCAT_FILE}" 2>/dev/null || true
        echo ""
        echo ">>> Android logcat 导出失败"
    fi
}

# 归档 Android 侧 BDebug playerlogs，补足 logcat 超时或被 ADB 传输问题截断时的应用持久化日志。
# Archive Android-side BDebug playerlogs so failure diagnostics still include persisted app logs when logcat export times out or gets cut off by ADB transport issues.
resolve_android_player_log_source_dirs() {
    printf '%s\n' "/sdcard/Android/data/${PACKAGE}/files/playerlogs"
    printf '%s\n' "/storage/emulated/0/Android/data/${PACKAGE}/files/playerlogs"
}

capture_android_player_logs() {
    local _candidate _detected_dir _payload_dir _index_file _pull_output _pull_rc

    _detected_dir=""
    rm -rf "${PLAYER_LOG_ARCHIVE_DIR}"
    _payload_dir="${PLAYER_LOG_ARCHIVE_DIR}/payload"
    _index_file="${PLAYER_LOG_ARCHIVE_DIR}/index.txt"
    mkdir -p "${_payload_dir}"

    while IFS= read -r _candidate; do
        [[ -z "${_candidate}" ]] && continue
        if talos_run_adb_with_reconnect_timeout "${TALOS_ADB_LOGCAT_TIMEOUT_SECONDS}" shell "[ -d '${_candidate}' ]" >/dev/null; then
            _detected_dir="${_candidate}"
            break
        fi
    done < <(resolve_android_player_log_source_dirs)

    if [[ -z "${_detected_dir}" ]]; then
        {
            echo "source=<missing>"
            echo "status=missing"
        } > "${_index_file}"
        echo ">>> 未发现 Android playerlogs / Android playerlogs not found"
        return 0
    fi

    _pull_output="$(talos_run_adb_with_reconnect_timeout "${TALOS_ADB_LOGCAT_TIMEOUT_SECONDS}" pull "${_detected_dir}" "${_payload_dir}")" && _pull_rc=0 || _pull_rc=$?

    if [[ ${_pull_rc} -eq 124 ]]; then
        {
            echo "source=${_detected_dir}"
            echo "status=timeout"
        } > "${_index_file}"
        echo ">>> Android playerlogs 导出超时 / Android playerlogs export timed out"
        return 124
    fi

    if [[ ${_pull_rc} -ne 0 ]]; then
        {
            echo "source=${_detected_dir}"
            echo "status=failed"
        } > "${_index_file}"
        [[ -n "${_pull_output}" ]] && echo "${_pull_output}"
        echo ">>> Android playerlogs 导出失败 / Android playerlogs export failed"
        return ${_pull_rc}
    fi

    {
        echo "source=${_detected_dir}"
        echo "status=found"
        while IFS= read -r _relative_path; do
            [[ -n "${_relative_path}" ]] && echo "file=${_relative_path}"
        done < <(cd "${PLAYER_LOG_ARCHIVE_DIR}" && find . -type f ! -name 'index.txt' | sed 's#^\./##' | LC_ALL=C sort)
    } > "${_index_file}"

    echo ">>> 已归档 Android playerlogs: ${PLAYER_LOG_ARCHIVE_DIR}"
    return 0
}

# 用超时保护执行清理类 ADB 命令，保证失败路径能尽快退出并把日志返回给 TeamCity。
# Execute cleanup-style ADB commands with a timeout so failure paths exit promptly and return logs to TeamCity.
talos_run_adb_cleanup_command() {
    local _description="$1"
    shift

    local _cleanup_output _cleanup_rc
    _cleanup_output="$(talos_run_adb_with_reconnect_timeout "${TALOS_ADB_CLEANUP_TIMEOUT_SECONDS}" "$@")" && _cleanup_rc=0 || _cleanup_rc=$?

    if [[ ${_cleanup_rc} -eq 124 ]]; then
        echo ">>> ${_description} 超时 / ${_description} timed out"
        return 124
    fi

    if [[ ${_cleanup_rc} -ne 0 && -n "${_cleanup_output}" ]]; then
        echo "${_cleanup_output}"
    fi

    return ${_cleanup_rc}
}

# 从 adb devices 输出中优先选择配置顺序里的 TCP 目标；若都不存在，再退回到任意在线 TCP 设备或任意在线设备。
# Pick the preferred serial from adb devices output: first any configured TCP target in order, then any online TCP device, finally any online device.
talos_pick_preferred_adb_serial_from_devices_output() {
    local _devices_output="$1"
    local _candidate _preferred

    if [[ -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
        IFS=',' read -ra _preferred_targets <<< "${TALOS_ADB_CONNECT_TARGETS}"
        for _candidate in "${_preferred_targets[@]}"; do
            _candidate="$(printf '%s' "${_candidate}" | tr -d '[:space:]')"
            [[ -z "${_candidate}" ]] && continue
            if printf '%s\n' "${_devices_output}" | awk -v serial="${_candidate}" '$1 == serial && $2 == "device" { found = 1 } END { exit found ? 0 : 1 }'; then
                printf '%s' "${_candidate}"
                return 0
            fi
        done
    fi

    _preferred=$(printf '%s\n' "${_devices_output}" | awk '
        $2 == "device" && index($1, ":") > 0 { print $1; exit }
        $2 == "device" && first == "" { first = $1 }
        END { if (first != "") print first }
    ' | head -n 1 | tr -d '\r')
    printf '%s' "${_preferred}"
}

# 读取当前 adb devices 并解析出优先序列号，供 daemon 重启后的重连路径复用。
# Read the current adb devices output and resolve the preferred serial for daemon-restart reconnect flows.
talos_pick_preferred_adb_serial() {
    local _devices_output
    _devices_output=$("${ADB_CMD[0]}" devices 2>/dev/null || true)
    talos_pick_preferred_adb_serial_from_devices_output "${_devices_output}"
}

# 重连配置中的首选 TCP 目标，并回显 adb connect 输出便于 CI 日志定位。
# Reconnect the primary configured TCP target and echo adb connect output for CI diagnostics.
talos_reconnect_primary_adb_target() {
    local _first_target _connect_output

    _first_target="$(printf '%s' "${TALOS_ADB_CONNECT_TARGETS:-}" | cut -d',' -f1 | tr -d '[:space:]')"
    [[ -z "${_first_target}" ]] && return 1

    "${ADB_CMD[0]}" disconnect "${_first_target}" 2>/dev/null || true
    _connect_output=$("${ADB_CMD[0]}" connect "${_first_target}" 2>&1) || true
    echo "        ${_first_target}: ${_connect_output}"
    return 0
}

# 执行 ADB 命令并自动处理 daemon 重启后的 TCP 设备重连。
# Nox 模拟器等使用 TCP 连接的设备在 ADB daemon 重启后（例如版本不匹配触发 killing + restart）
# 会丢失 TCP 连接。此函数检测 daemon 重启并自动重连，然后重试命令。
#
# Execute an ADB command with automatic TCP-device reconnect after daemon restart.
# TCP-connected emulators (e.g. Nox) lose their connection when the ADB daemon restarts
# (e.g. due to version mismatch triggering killing + restart).
# This function detects the restart, reconnects, and retries the command.
#
# 用法 / Usage: adb_with_reconnect <any adb args...>
# 返回码 / Return code: 最终 adb 命令的退出码 / final adb command exit code
adb_with_reconnect() {
    local _output _stderr _rc
    # 合并 stdout+stderr 以检测 daemon 重启 / combine to detect daemon restart
    _output=$("${ADB_CMD[@]}" "$@" 2>&1) && _rc=0 || _rc=$?

    # 检测 ADB daemon 是否重启 / Detect if ADB daemon restarted
    if echo "${_output}" | grep -qi "daemon started successfully\|killing\.\.\."; then
        echo "    ⚠️ 检测到 ADB daemon 重启，正在重连 TCP 目标..."
        echo "    ⚠️ Detected ADB daemon restart, reconnecting TCP targets..."
        # 重连所有 TCP 目标 / Reconnect all TCP targets
        if [[ -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
            local _rt _rc2
            IFS=',' read -ra _reconnect_targets <<< "${TALOS_ADB_CONNECT_TARGETS}"
            for _rt in "${_reconnect_targets[@]}"; do
                _rt="$(printf '%s' "${_rt}" | tr -d '[:space:]')"
                [[ -z "${_rt}" ]] && continue
                "${ADB_CMD[0]}" disconnect "${_rt}" 2>/dev/null || true
                _rc2=$("${ADB_CMD[0]}" connect "${_rt}" 2>&1) || true
                echo "        ${_rt}: ${_rc2}"
            done
            # 等待 daemon 稳定：10s 基础等待，给 ADB 协议握手留足时间。
            # Wait for daemon to stabilise: 10s baseline so ADB protocol handshake completes.
            sleep "${TALOS_ADB_RECONNECT_BASELINE_SLEEP_SECONDS}"
            # 优先选取配置顺序里的 TCP 目标作为序列号（比 emulator-* 更稳定，不会变成 offline）。
            # Prefer the configured TCP target order as the serial because it is more stable than emulator-* aliases.
            local _wait_serial=""
            _wait_serial=$(talos_pick_preferred_adb_serial) || true
            # 如果仍无设备，轮询等待（最多 60s） / Still no device — poll up to 60s
            local _wait_tries=0
            while [[ -z "${_wait_serial}" && ${_wait_tries} -lt 12 ]]; do
                sleep "${TALOS_ADB_RECONNECT_RETRY_SLEEP_SECONDS}"
                _wait_tries=$(( _wait_tries + 1 ))
                _wait_serial=$(talos_pick_preferred_adb_serial) || true
                if [[ -n "${_wait_serial}" ]]; then
                    break
                fi
                # 重新 connect 第一个目标 / Re-connect first target
                talos_reconnect_primary_adb_target || true
            done
            if [[ -n "${_wait_serial}" ]]; then
                ADB_CMD=("${ADB_CMD[0]}" "-s" "${_wait_serial}")
                echo "    重连后序列号: ${_wait_serial} / Reconnected serial: ${_wait_serial}"
            fi
        fi
        # 重试原始命令（最多 3 次，每次失败后断开重连） / Retry (up to 3x, disconnect+reconnect on each failure)
        local _retry=0
        while [[ ${_retry} -lt 3 ]]; do
            _retry=$(( _retry + 1 ))
            echo "    重试 adb $* (attempt ${_retry}/3) ..."
            _output=$("${ADB_CMD[@]}" "$@" 2>&1) && _rc=0 || _rc=$?
            if [[ ${_rc} -eq 0 ]]; then
                break
            fi
            # 如果 device not found / offline，断开并重连第一个 TCP 目标再试 / Disconnect and reconnect first TCP target before retry
            if echo "${_output}" | grep -qi "not found\|device.*offline\|no devices"; then
                echo "    设备不可用，重连中... / Device unavailable, reconnecting..."
                if [[ -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
                    talos_reconnect_primary_adb_target || true
                    sleep "${TALOS_ADB_RECONNECT_RETRY_SLEEP_SECONDS}"
                    local _re_sn
                    _re_sn=$(talos_pick_preferred_adb_serial) || true
                    if [[ -n "${_re_sn}" ]]; then
                        ADB_CMD=("${ADB_CMD[0]}" "-s" "${_re_sn}")
                        echo "    重连后序列号: ${_re_sn}"
                    fi
                fi
                continue
            fi
            break
        done
    fi

    # 输出原始结果 / Output the result
    echo "${_output}"
    return ${_rc}
}

# ======== 参数解析 ========
while [[ $# -gt 0 ]]; do
    case $1 in
        --apk)             APK_PATH="$2";                  shift 2 ;;
        --serial)          ADB_SERIAL="$2";                shift 2 ;;
        --port)            UNITY_PORT="$2";                shift 2 ;;
        --package)         PACKAGE="$2";                   shift 2 ;;
        --test-file)       PLAYWRIGHT_TEST_FILE="$2";      shift 2 ;;
        # 模拟器修复模式：连接宿主机本地 ADB 目标（例如 MuMu2 的 127.0.0.1:16384）。
        # Emulator fix mode: connect to host-local ADB targets (e.g. MuMu2 at 127.0.0.1:16384).
        --connect-targets) TALOS_ADB_CONNECT_TARGETS="$2"; shift 2 ;;
        # MuMu 自动启动：在 ADB 连接前先检测并启动 MuMu 模拟器进程。
        # MuMu auto-start: detect and launch the MuMu emulator before ADB connect.
        --start-mumu) TALOS_MUMU_AUTO_START="$2"; shift 2 ;;
        # 模拟器类型选择：mumu（默认，自动发现和启动 MuMu）、nox（检测 Nox 进程，需手动预启动）、none（跳过自动启动）。
        # Emulator type: mumu (default, auto-discover and launch MuMu), nox (detect Nox process, must pre-start manually), none (skip auto-launch).
        --emulator-type) TALOS_EMULATOR_TYPE="$2"; shift 2 ;;
        --help)
            echo "用法: $0 --apk <path/to/app.apk>"
            echo ""
            echo "选项:"
            echo "  --apk            APK 文件路径"
            echo "  --serial         ADB 设备序列号"
            echo "  --port           TCP 端口 (默认 10002)"
            echo "  --package        Android 包名 (默认 com.popo.bdframework)"
            echo "  --test-file      Playwright 测试文件路径（相对 Playwright~ 根目录）"
            echo "  --connect-targets ADB TCP 目标 (逗号分隔, 如 127.0.0.1:62001)"
            echo "  --emulator-type  模拟器类型: mumu (默认), nox, none"
            echo "  --start-mumu     MuMu 自动启动 (true|false, 等同 --emulator-type mumu/none)"
            exit 0
            ;;
        *) echo "未知参数: $1"; exit 1 ;;
    esac
done

if [[ -z "${PACKAGE}" ]] && [[ -n "${APK_PATH}" ]]; then
    APK_BASENAME="$(basename "${APK_PATH}")"
    APK_STEM="${APK_BASENAME%.apk}"
    if [[ "${APK_STEM}" == *.* ]]; then
        PACKAGE="${APK_STEM}"
    fi
fi

PACKAGE="${PACKAGE:-com.popo.bdframework}"
ACTIVITY="${ACTIVITY:-${PACKAGE}/com.unity3d.player.UnityPlayerActivity}"

# 解析 ADB 命令前缀，兼容 TeamCity service 未把 Android SDK 注入 PATH 的场景。
# Resolve the ADB command prefix so TeamCity services can still find Android SDK installs when PATH is incomplete.
ensure_talos_adb_tooling || exit 1

ADB_CMD=("${TALOS_ADB_BIN}")
if [[ -n "${ADB_SERIAL}" ]]; then
    ADB_CMD+=("-s" "${ADB_SERIAL}")
fi

echo "============================================"
echo "  Talos E2E — Android 模式测试"
echo "============================================"
echo "  APK:      ${APK_PATH}"
echo "  包名:     ${PACKAGE}"
echo "  端口:     ${UNITY_PORT}"
if [[ -n "${PLAYWRIGHT_TEST_FILE}" ]]; then
    echo "  测试文件: ${PLAYWRIGHT_TEST_FILE}"
fi
echo "============================================"

# ======== 前置检查 ========
if [[ -z "${APK_PATH}" ]]; then
    echo "❌ 错误: 未指定 APK 路径"
    echo "使用: $0 --apk /path/to/app.apk"
    exit 1
fi

if [[ ! -f "${APK_PATH}" ]]; then
    echo "❌ 错误: APK 文件不存在: ${APK_PATH}"
    exit 1
fi

# ======== Android 虚拟设备连接（模拟器启动 + ADB 连接 + 设备上线等待）========
# 通过独立脚本处理全链路：避免 test-android.sh 承担环境搭建职责。
# 脚本 source 后：ADB_CMD 中含 -s <serial>，TALOS_ANDROID_SERIAL 已导出。
# Full Android VM setup handled by the dedicated script (launch + connect + wait).
# After sourcing: ADB_CMD contains -s <serial>, TALOS_ANDROID_SERIAL is exported.
# 导出模拟器类型供 connect_androidVirtualDevice.sh 读取。
# Export emulator type for connect_androidVirtualDevice.sh to read.
export TALOS_EMULATOR_TYPE
# shellcheck source=./connect_androidVirtualDevice.sh
source "${SCRIPT_DIR}/connect_androidVirtualDevice.sh"

# ======== 安装 Playwright 依赖 ========
echo ""
echo ">>> 检查 Playwright 依赖..."
ensure_talos_playwright_dependencies "${PLAYWRIGHT_DIR}"
cd "${PLAYWRIGHT_DIR}"

# ======== 安装 APK（带重试逻辑）========
# Android VM 可能还未完全启动（package 服务未就绪），需要重试；
# 若设备变为 offline，也要重连后重试。
# Install APK with retry: Android OS may not have finished booting (package service not ready),
# or the device may transiently go offline; reconnect + retry handles both cases.
echo ""
echo ">>> 安装 APK..."
APK_INSTALL_MAX="${TALOS_APK_INSTALL_RETRIES:-8}"
APK_INSTALL_TRIED=0
APK_INSTALL_OK=0
while [[ ${APK_INSTALL_TRIED} -lt ${APK_INSTALL_MAX} ]]; do
    APK_INSTALL_TRIED=$((APK_INSTALL_TRIED + 1))
    _apk_result="$(talos_run_adb_command_with_timeout "${TALOS_APK_INSTALL_TIMEOUT_SECONDS}" install -r -t "${APK_PATH}")" && _apk_rc=0 || _apk_rc=$?
    if [[ ${_apk_rc} -eq 0 ]]; then
        # 命令返回 0，但 adb install 有时以 0 返回 "FAILED" 字样，需检查输出。
        # Command succeeded (exit 0), but adb install may still print FAILED in output.
        if echo "${_apk_result}" | grep -qi 'FAILED\|error'; then
            echo "    [尝试 ${APK_INSTALL_TRIED}/${APK_INSTALL_MAX}] APK 安装报告错误: ${_apk_result}"
        else
            APK_INSTALL_OK=1
            break
        fi
    else
        if [[ ${_apk_rc} -eq 124 ]]; then
            echo "    [尝试 ${APK_INSTALL_TRIED}/${APK_INSTALL_MAX}] APK 安装超时: ${TALOS_APK_INSTALL_TIMEOUT_SECONDS}s"
        else
        echo "    [尝试 ${APK_INSTALL_TRIED}/${APK_INSTALL_MAX}] APK 安装失败: ${_apk_result}"
        fi
    fi
    if echo "${_apk_result}" | grep -qi "daemon started successfully\|killing\.\.\.\|not found\|device.*offline\|no devices"; then
        echo "    检测到 ADB 连接异常，尝试重连..."
        talos_reconnect_primary_adb_target || true
        sleep "${TALOS_ADB_RECONNECT_RETRY_SLEEP_SECONDS}"
        _new_serial=$(talos_pick_preferred_adb_serial) || true
        if [[ -n "${_new_serial}" ]]; then
            ADB_CMD=("${ADB_CMD[0]}" "-s" "${_new_serial}")
            echo "    重连后切换序列号: ${_new_serial}"
        fi
    fi
    if [[ ${APK_INSTALL_TRIED} -lt ${APK_INSTALL_MAX} ]]; then
        echo "    等待 20s 后重试（让 Android 系统继续启动）..."
        sleep 20
        # 若设备 offline，尝试重连
        # If device offline, attempt reconnect before retry
        _dev_state=$("${ADB_CMD[@]}" devices 2>/dev/null | grep 'device$' | wc -l || true)
        if [[ "${_dev_state}" -eq 0 && -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
            echo "    设备已离线，尝试重连..."
            IFS=',' read -ra _retry_targets <<< "${TALOS_ADB_CONNECT_TARGETS}"
            for _rt in "${_retry_targets[@]}"; do
                _rt="$(printf '%s' "${_rt}" | tr -d '[:space:]')"
                [[ -z "${_rt}" ]] && continue
                "${ADB_CMD[@]}" disconnect "${_rt}" 2>/dev/null || true
                _rc=$("${ADB_CMD[@]}" connect "${_rt}" 2>/dev/null || true)
                echo "        ${_rt}: ${_rc}"
            done
            sleep 5
            # 重连后更新序列号
            # Re-select serial after reconnect
            _new_serial=$("${ADB_CMD[@]}" devices 2>/dev/null | grep 'device$' | awk 'NR==1{print $1}' | tr -d '\r\n') || true
            if [[ -n "${_new_serial}" ]]; then
                # 更新 ADB_CMD 中的序列号 / Update serial in ADB_CMD
                ADB_CMD=("${ADB_CMD[0]}" "-s" "${_new_serial}")
                echo "    重连后切换序列号: ${_new_serial}"
            fi
        fi
    fi
done
if [[ ${APK_INSTALL_OK} -eq 0 ]]; then
    echo "❌ APK 安装失败（已重试 ${APK_INSTALL_MAX} 次）"
    exit 1
fi
echo "    ✅ APK 安装完成"

# ======== 停止旧实例 + 设置端口转发 ========
# 使用 adb_with_reconnect 包装：ADB daemon 可能因版本不匹配而重启（Nox 自带 ADB v36 vs
# 系统 ADB v41），重启后 TCP 设备断开，需要自动重连再重试。
# Wrap with adb_with_reconnect: the ADB daemon may restart due to version mismatch
# (Nox bundles ADB v36 vs system ADB v41); after restart the TCP device drops,
# so we auto-reconnect and retry.
echo ""
echo ">>> 准备端口转发..."
adb_with_reconnect shell am force-stop "${PACKAGE}" 2>/dev/null || true
adb_with_reconnect forward --remove "tcp:${UNITY_PORT}" 2>/dev/null || true
adb_with_reconnect forward "tcp:${UNITY_PORT}" "tcp:${UNITY_PORT}"
adb_with_reconnect logcat -c 2>/dev/null || true
echo "    ✅ 端口转发: localhost:${UNITY_PORT} -> device:${UNITY_PORT}"

# ======== 启动应用 ========
echo ""
echo ">>> 启动应用..."
adb_with_reconnect shell "am start -n ${ACTIVITY} -e unity '-talosPort ${UNITY_PORT} -talosForceE2E'"
echo "    ✅ 应用已启动"

# ======== 等待 TCP 服务就绪 ========
# Unity 应用启动后需要加载热更 DLL、初始化框架、启动 TCP 监听服务。
# 正常情况为秒级（< 30s）；首次安装 AOT 场景预留 180s 保障裕量。
# 若持续超时超过 MAX_WAIT，说明应用未能正常启动，需检查 logcat。
# After launch, Unity loads hotfix DLLs, initialises the framework, then starts the TCP listener.
# Normally this completes in seconds (< 30s); 180s provides ample headroom.
# If TCP is still not ready at MAX_WAIT the app has failed to start — check logcat.
echo ""
echo ">>> 等待 Unity E2E TCP 服务就绪..."
MAX_WAIT="${TALOS_UNITY_TCP_TIMEOUT:-180}"
WAITED=0

while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
    if probe_talos_unity_ready 127.0.0.1 "${UNITY_PORT}" 1000; then
        echo ""
        echo "    ✅ TCP 服务已就绪 (${WAITED}s)"
        break
    fi
    sleep 2
    WAITED=$((WAITED + 2))
    echo -n "."
done

if [[ ${WAITED} -ge ${MAX_WAIT} ]]; then
    echo ""
    echo "❌ 等待 TCP 服务超时 (${MAX_WAIT}s)"
    echo "    可能原因 / Possible causes:"
    echo "    1. 应用崩溃或启动失败（查看 logcat）"
    echo "    2. Unity 热更 DLL 加载出错"
    echo "    3. adb forward tcp:${UNITY_PORT} 未生效"
    capture_android_logcat
    capture_android_player_logs || true
    talos_run_adb_cleanup_command "force-stop package" shell am force-stop "${PACKAGE}" 2>/dev/null || true
    exit 1
fi

# ======== 运行 Playwright 测试 ========
echo ""
echo ">>> 运行 Playwright 测试..."
echo ""

PLAYWRIGHT_COMMAND=("${TALOS_NODE_BIN}" "${PLAYWRIGHT_DIR}/node_modules/@playwright/test/cli.js" test)
if [[ -n "${PLAYWRIGHT_TEST_FILE}" ]]; then
    PLAYWRIGHT_COMMAND+=("${PLAYWRIGHT_TEST_FILE}")
fi
PLAYWRIGHT_COMMAND+=(
    "--project=android"
    "--output=${PLAYWRIGHT_DIR}/test-results/artifacts"
    "--reporter=list,html,junit"
)

# 兼容 Playwright 1.40.1 旧版 HTML reporter 环境变量，同时保留新版变量，避免 CI 因自动打开 report 而挂住。
# Support the legacy Playwright 1.40.1 HTML reporter environment variables while keeping the newer names so CI does not hang by auto-opening the report server.
PLAYWRIGHT_HTML_OUTPUT_DIR="${PLAYWRIGHT_DIR}/test-results/html" \
PLAYWRIGHT_HTML_REPORT="${PLAYWRIGHT_DIR}/test-results/html" \
PLAYWRIGHT_HTML_OPEN=never \
PW_TEST_HTML_REPORT_OPEN=never \
PLAYWRIGHT_JUNIT_OUTPUT_FILE="${PLAYWRIGHT_DIR}/test-results/junit.xml" \
PLATFORM=android \
UNITY_HOST=127.0.0.1 \
UNITY_PORT="${UNITY_PORT}" \
"${PLAYWRIGHT_COMMAND[@]}" 2>&1 | tee "${PLAYWRIGHT_DIR}/test-results/test-output.log"

TEST_EXIT_CODE=${PIPESTATUS[0]}
capture_android_logcat

# ======== 清理 ========
echo ""
echo ">>> 清理..."
talos_run_adb_cleanup_command "force-stop package" shell am force-stop "${PACKAGE}" 2>/dev/null || true
talos_run_adb_cleanup_command "remove adb forward" forward --remove "tcp:${UNITY_PORT}" 2>/dev/null || true

# ======== 结果 ========
echo ""
echo "============================================"
if [[ ${TEST_EXIT_CODE} -eq 0 ]]; then
    echo "  ✅ E2E 测试全部通过"
else
    echo "  ❌ E2E 测试失败 (exit code: ${TEST_EXIT_CODE})"
fi
echo "============================================"
echo "  测试报告: ${PLAYWRIGHT_DIR}/test-results/html/index.html"
echo "============================================"

exit ${TEST_EXIT_CODE}
