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
# MuMu 模拟器等宿主本机模拟器的 ADB 连接目标，逗号分隔，如 127.0.0.1:16384,127.0.0.1:7555。
# Comma-separated ADB connect targets for host-local emulators such as MuMu. E.g. 127.0.0.1:16384,127.0.0.1:7555
TALOS_ADB_CONNECT_TARGETS="${TALOS_ADB_CONNECT_TARGETS:-}"
# MuMu 模拟器自动启动开关（true 启用）：先检测进程，再搜索安装目录，找到后后台启动并等待初始化。
# MuMu auto-start flag (set to "true" to enable): checks process list, searches install dirs, starts in background.
TALOS_MUMU_AUTO_START="${TALOS_MUMU_AUTO_START:-}"
UNITY_PORT="${UNITY_PORT:-10002}"
PACKAGE="${PACKAGE:-}"
ACTIVITY="${ACTIVITY:-}"
PLAYWRIGHT_TEST_FILE="${PLAYWRIGHT_TEST_FILE:-}"
ANDROID_LOGCAT_FILE="${PLAYWRIGHT_DIR}/test-results/android-logcat.txt"

# 在启动前清空并在关键失败点导出 logcat，保证 TeamCity artifact 能带回同一次 Android 启动期日志。
# Clear logcat before launch and export it on key failure paths so the TeamCity artifact contains the same Android startup session logs.
capture_android_logcat() {
    if "${ADB_CMD[@]}" logcat -d -v threadtime > "${ANDROID_LOGCAT_FILE}" 2>/dev/null; then
        echo ""
        echo ">>> Android logcat (tail 200)"
        tail -n 200 "${ANDROID_LOGCAT_FILE}" || true
    else
        echo ""
        echo ">>> Android logcat 导出失败"
    fi
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
        --help)
            echo "用法: $0 --apk <path/to/app.apk>"
            echo ""
            echo "选项:"
            echo "  --apk       APK 文件路径"
            echo "  --serial    ADB 设备序列号"
            echo "  --port      TCP 端口 (默认 10002)"
            echo "  --package   Android 包名 (默认 com.popo.bdframework)"
            echo "  --test-file Playwright 测试文件路径（相对 Playwright~ 根目录）"
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

# ======== MuMu 模拟器自动启动（在 ADB 连接前执行）========
# 当 TALOS_MUMU_AUTO_START=true 时：先检测进程列表，再搜索常见安装目录，
# 找到 exe 后后台启动并等待虚拟机初始化；之后再执行 ADB connect 与设备探测。
# When TALOS_MUMU_AUTO_START=true: check the process list, search common install dirs,
# launch exe in background and wait for VM init, then proceed with ADB connect and device detection.
if [[ "${TALOS_MUMU_AUTO_START:-}" == "true" ]]; then
    ensure_talos_mumu_running
fi

# ======== 模拟器连接修复（在设备探测前执行）========
# 当 TALOS_ADB_CONNECT_TARGETS 非空时，先尝试连接宿主机本地模拟器（如 MuMu 模拟器），
# 之后再执行 adb devices 探测，避免因模拟器未被主动连接而被误报为无设备。
# When TALOS_ADB_CONNECT_TARGETS is non-empty, attempt to connect to host-local emulators (e.g. MuMu)
# before running adb devices detection, preventing false "no device" errors.
if [[ -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
    ensure_talos_adb_connect_targets "${TALOS_ADB_CONNECT_TARGETS}"
fi

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

# 检查设备连接（带 offline→online 等待循环，含定期重连）
# MuMu 12 NX adbd 上线时，旧 ADB 连接不会自动刷新为 online，需要断开并重连才能识别为 device。
# MuMu 12 NX: when adbd comes online, stale ADB connections stay "offline"; a fresh connect is needed.
echo ""
DEVICE_WAIT_MAX="${TALOS_ADB_DEVICE_ONLINE_TIMEOUT:-600}"
echo ">>> 检查设备连接（最多等待 ${DEVICE_WAIT_MAX}s，每 60s 自动重连）..."
DEVICE_COUNT=0
DEVICE_WAITED=0
while [[ ${DEVICE_WAITED} -lt ${DEVICE_WAIT_MAX} ]]; do
    DEVICE_COUNT=$("${ADB_CMD[@]}" devices 2>/dev/null | grep -c "device$" || true)
    if [[ ${DEVICE_COUNT} -gt 0 ]]; then
        break
    fi
    OFFLINE_COUNT=$("${ADB_CMD[@]}" devices 2>/dev/null | grep -c "offline" || true)
    if [[ ${OFFLINE_COUNT} -gt 0 ]]; then
        echo "    设备 offline，等待 Android adbd 就绪... (${DEVICE_WAITED}/${DEVICE_WAIT_MAX}s)"
    else
        echo "    无设备，继续等待... (${DEVICE_WAITED}/${DEVICE_WAIT_MAX}s)"
    fi
    # 每 60s 断开并重连一次：adbd 上线后旧连接仍显示 offline，需重连才能变为 device。
    # Every 60s: disconnect + reconnect so a fresh ADB handshake picks up the now-live adbd.
    if [[ $((DEVICE_WAITED % 60)) -eq 0 && ${DEVICE_WAITED} -gt 0 && -n "${TALOS_ADB_CONNECT_TARGETS:-}" ]]; then
        echo "    >>> 重连 ADB 目标 (第 $((DEVICE_WAITED / 60)) 次重连)..."
        IFS=',' read -ra _reconnect_targets <<< "${TALOS_ADB_CONNECT_TARGETS}"
        for _rt in "${_reconnect_targets[@]}"; do
            _rt="$(printf '%s' "${_rt}" | tr -d '[:space:]')"
            [[ -z "${_rt}" ]] && continue
            "${ADB_CMD[@]}" disconnect "${_rt}" 2>/dev/null || true
            _rc=$("${ADB_CMD[@]}" connect "${_rt}" 2>/dev/null || true)
            echo "        ${_rt}: ${_rc}"
        done
    fi
    sleep 10
    DEVICE_WAITED=$((DEVICE_WAITED + 10))
done
if [[ ${DEVICE_COUNT} -eq 0 ]]; then
    echo "❌ 错误: 未检测到已连接的 Android 设备 (已等待 ${DEVICE_WAITED}s)"
    "${ADB_CMD[@]}" devices
    exit 1
fi
echo "    ✅ 设备已连接 ($("${ADB_CMD[@]}" devices 2>/dev/null | grep "device$" | head -1))"

# 未指定 ADB 序列号时，若有多台设备在线则自动选择第一台，避免 "more than one device" 错误。
# If no ADB serial specified and multiple devices are online, auto-select the first one.
if [[ -z "${ADB_SERIAL:-}" ]]; then
    _auto_serial=$("${ADB_CMD[@]}" devices 2>/dev/null | grep "device$" | awk 'NR==1{print $1}' | tr -d '\r') || true
    if [[ -n "${_auto_serial}" ]]; then
        ADB_CMD+=("-s" "${_auto_serial}")
        echo "    自动选择设备序列号: ${_auto_serial}"
    fi
fi

# ======== 安装 Playwright 依赖 ========
echo ""
echo ">>> 检查 Playwright 依赖..."
ensure_talos_playwright_dependencies "${PLAYWRIGHT_DIR}"
cd "${PLAYWRIGHT_DIR}"

# ======== 安装 APK ========
echo ""
echo ">>> 安装 APK..."
"${ADB_CMD[@]}" install -r -t "${APK_PATH}" || {
    echo "❌ APK 安装失败"
    exit 1
}
echo "    ✅ APK 安装完成"

# ======== 停止旧实例 + 设置端口转发 ========
echo ""
echo ">>> 准备端口转发..."
"${ADB_CMD[@]}" shell am force-stop "${PACKAGE}" 2>/dev/null || true
"${ADB_CMD[@]}" forward --remove "tcp:${UNITY_PORT}" 2>/dev/null || true
"${ADB_CMD[@]}" forward "tcp:${UNITY_PORT}" "tcp:${UNITY_PORT}"
"${ADB_CMD[@]}" logcat -c 2>/dev/null || true
echo "    ✅ 端口转发: localhost:${UNITY_PORT} -> device:${UNITY_PORT}"

# ======== 启动应用 ========
echo ""
echo ">>> 启动应用..."
"${ADB_CMD[@]}" shell "am start -n ${ACTIVITY} -e unity '-talosPort ${UNITY_PORT} -talosForceE2E'"
echo "    ✅ 应用已启动"

# ======== 等待 TCP 服务就绪 ========
echo ""
echo ">>> 等待 Unity E2E TCP 服务就绪..."
MAX_WAIT=60
WAITED=0

while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
    if probe_talos_unity_ready 127.0.0.1 "${UNITY_PORT}" 1000; then
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
    capture_android_logcat
    "${ADB_CMD[@]}" shell am force-stop "${PACKAGE}" 2>/dev/null || true
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
"${ADB_CMD[@]}" shell am force-stop "${PACKAGE}" 2>/dev/null || true
"${ADB_CMD[@]}" forward --remove "tcp:${UNITY_PORT}" 2>/dev/null || true

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
