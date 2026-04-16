#!/usr/bin/env bash
# ============================================================================
# Talos E2E 测试——UnityPlayer（headed GUI）Editor 控制专项启动脚本。
# Talos E2E UnityPlayer headed-GUI launcher for editor-control-only validation.
#
# 以正常带界面的 Unity Editor 启动，用于：
# Launch a normal headed Unity Editor session for:
#   1. 验证 editor_command / EditorOps 控制链路。
#   1. Validating the editor_command and EditorOps control lane.
#   2. 验证场景加载、PlayMode 切换、重连等 Editor 控制流程。
#   2. Validating editor-control flows such as scene loading, PlayMode transitions, and reconnects.
#
# 该脚本不是运行时完整闸门。
# This script is not the runtime-complete gate.
# 无平台后缀的跨平台运行时套件应通过 test-batchmode.sh 或设备/Player 运行时项目验证。
# Reusable runtime suites without a platform suffix should be validated through test-batchmode.sh or the device/player runtime projects.
#
# 与 test-batchmode.sh 的区别：
# Differences from test-batchmode.sh:
#   - 不使用 -batchmode -nographics，Unity 正常显示 GUI 窗口。
#   - It does not use -batchmode -nographics, so Unity runs with a visible GUI window.
#   - Playwright 通过 editor_command 协议操作 Unity Editor。
#   - Playwright drives the Unity Editor through the editor_command protocol.
#   - 只运行 unityplayer 项目下的 `*-EditorPlayer-e2e.spec.ts` 专项用例。
#   - It runs only the `*-EditorPlayer-e2e.spec.ts` specialty cases routed to the unityplayer project.
#
# 使用方式：
# Usage:
#   UNITY_PATH=/path/to/Unity ./tools/test-editorplayer.sh
#
# 环境变量：
# Environment variables:
#   UNITY_PATH      — Unity 可执行文件路径（必须）。
#   UNITY_PATH      — Path to the Unity executable (required).
#   UNITY_PORT      — TCP 端口，默认 10002。
#   UNITY_PORT      — TCP port, defaults to 10002.
#   PROJECT_PATH    — Unity 项目路径，默认自动检测。
#   PROJECT_PATH    — Unity project path, auto-detected by default.
#   WAIT_ONLY       — 设为 1 时只等待已运行的 Unity 实例（不启动新的）。
#   WAIT_ONLY       — When set to 1, wait for an existing Unity instance instead of launching a new one.
#
# 注意：
# Notes:
#   - 脚本自动设置 UNITY_DISABLE_HUB=1 防止 Unity Hub 弹出 "reopen" 提示。
#   - The script sets UNITY_DISABLE_HUB=1 to prevent Unity Hub from showing a "reopen" prompt.
#   - 脚本启动前会清理残留的 UnityLockfile。
#   - The script removes leftover UnityLockfile state before launch.
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLAYWRIGHT_DIR="$(dirname "${SCRIPT_DIR}")"
# shellcheck source=./node-tools.sh
source "${SCRIPT_DIR}/node-tools.sh"

# ======== 参数 ========
UNITY_PATH="${UNITY_PATH:-}"
UNITY_PORT="${UNITY_PORT:-10002}"
PROJECT_PATH="${PROJECT_PATH:-}"
WAIT_ONLY="${WAIT_ONLY:-0}"

# 强制禁用 Unity Hub，防止 "reopen" 提示。
# Force-disable Unity Hub to prevent the "reopen" prompt.
export UNITY_DISABLE_HUB=1

# 自动检测项目路径（向上查找到包含 Assets/ 的目录）。
# Auto-detect the project path by walking upward until an Assets/ directory is found.
if [[ -z "${PROJECT_PATH}" ]]; then
    SEARCH_DIR="${SCRIPT_DIR}"
    for _ in {1..5}; do
        if [[ -d "${SEARCH_DIR}/Assets" ]]; then
            PROJECT_PATH="${SEARCH_DIR}"
            break
        fi
        SEARCH_DIR="$(dirname "${SEARCH_DIR}")"
    done
fi

# ======== 前置检查 / Preconditions ========
if [[ -z "${PROJECT_PATH}" ]]; then
    echo "❌ 错误: 无法自动检测项目路径，请设置 PROJECT_PATH 环境变量"
    exit 1
fi

# ======== 清理残留 Lock 文件 / Cleanup leftover lock files ========
LOCKFILE="${PROJECT_PATH}/Temp/UnityLockfile"
if [[ -f "${LOCKFILE}" ]]; then
    echo ">>> 清理残留 Lockfile: ${LOCKFILE}"
    rm -f "${LOCKFILE}"
fi
rm -f "${PROJECT_PATH}/Temp/"*.lock 2>/dev/null || true

mkdir -p "${PROJECT_PATH}/Logs"
UNITY_LOG="${PROJECT_PATH}/Logs/talos_e2e_unity_gui_$(date +%s).log"

UNITY_PID=""
LAUNCHED_BY_US=0

# ====================================================================
# 启动或等待 Unity Editor。
# Launch Unity Editor or wait for an existing instance.
# ====================================================================
echo "============================================"
echo "  Talos E2E — UnityPlayer（Editor 控制专项）"
echo "============================================"
echo "  项目:     ${PROJECT_PATH}"
echo "  端口:     ${UNITY_PORT}"
echo "  模式:     headed GUI（Editor 控制专项）"
echo "============================================"

if [[ "${WAIT_ONLY}" == "1" ]]; then
    # 仅等待已运行的 Unity 实例。
    # Wait only for an already running Unity instance.
    echo ""
    echo ">>> WAIT_ONLY=1，跳过 Unity 启动，等待已有实例..."
else
    # ======== 前置检查 / Preconditions ========
    if [[ -z "${UNITY_PATH}" ]]; then
        echo "❌ 错误: UNITY_PATH 环境变量未设置"
        echo ""
        echo "请设置 Unity 可执行文件路径，例如："
        echo "  export UNITY_PATH=/Applications/Unity2021.3.58f1/Unity.app/Contents/MacOS/Unity"
        exit 1
    fi

    if [[ ! -x "${UNITY_PATH}" ]]; then
        echo "❌ 错误: UNITY_PATH 指向的文件不存在或不可执行: ${UNITY_PATH}"
        exit 1
    fi

    echo ""
    echo ">>> 启动 Unity Editor（headed GUI 模式，无 -batchmode）..."
    echo "    日志文件: ${UNITY_LOG}"

    # 注意：不使用 -batchmode -nographics，Unity 正常显示 GUI。
    # Keep Unity in headed GUI mode by avoiding -batchmode -nographics.
    "${UNITY_PATH}" \
        -projectPath "${PROJECT_PATH}" \
        -executeMethod BDFramework.Editor.Environment.TalosE2EBatchBridge.LaunchTalosE2EEditorOnly \
        -talosPort "${UNITY_PORT}" \
        -talosForceE2E \
        -logFile "${UNITY_LOG}" &
    UNITY_PID=$!
    LAUNCHED_BY_US=1
    echo "    Unity PID: ${UNITY_PID}"
fi

# ======== 等待 TCP 服务就绪 / Wait for the TCP service ========
echo ""
echo -n ">>> 等待 Unity E2E TCP 服务就绪"
MAX_WAIT=120
WAITED=0

while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
    # 如果我们启动了 Unity，检查进程是否存活。
    # If Unity was launched by this script, verify that the process is still alive.
    if [[ -n "${UNITY_PID}" ]] && [[ ${LAUNCHED_BY_US} -eq 1 ]]; then
        if ! kill -0 ${UNITY_PID} 2>/dev/null; then
            echo ""
            echo "❌ Unity 进程已退出"
            echo ""
            echo "--- 日志最后 30 行 ---"
            tail -30 "${UNITY_LOG}" 2>/dev/null || true
            exit 1
        fi
    fi

    if probe_talos_tcp_port 127.0.0.1 "${UNITY_PORT}"; then
        echo " ✅"
        echo "    TCP 服务已就绪 (${WAITED}s)"
        break
    fi
    sleep 2
    WAITED=$((WAITED + 2))
    echo -n "."
done

if [[ ${WAITED} -ge ${MAX_WAIT} ]]; then
    echo ""
    echo "❌ 等待 TCP 服务超时 (${MAX_WAIT}s)"
    if [[ -n "${UNITY_PID}" ]] && [[ ${LAUNCHED_BY_US} -eq 1 ]]; then
        kill ${UNITY_PID} 2>/dev/null || true
    fi
    exit 1
fi

# ======== 运行 Playwright 测试 / Run Playwright tests ========
echo ""
echo ">>> 运行 Playwright EditorPlayer 控制专项测试..."
echo ""

cd "${PLAYWRIGHT_DIR}"
ensure_talos_playwright_dependencies "${PLAYWRIGHT_DIR}"

PLATFORM=unityplayer \
UNITY_HOST=127.0.0.1 \
UNITY_PORT="${UNITY_PORT}" \
"${TALOS_NODE_BIN}" "${PLAYWRIGHT_DIR}/node_modules/@playwright/test/cli.js" test \
    --project=unityplayer \
    --reporter=list \
    2>&1

TEST_EXIT_CODE=$?

# ======== 关闭 Unity / Shut down Unity ========
if [[ -n "${UNITY_PID}" ]] && [[ ${LAUNCHED_BY_US} -eq 1 ]]; then
    echo ""
    echo ">>> 关闭 Unity (PID: ${UNITY_PID})..."
    kill ${UNITY_PID} 2>/dev/null || true
    for _ in {1..10}; do
        kill -0 ${UNITY_PID} 2>/dev/null || break
        sleep 1
    done
    kill -9 ${UNITY_PID} >/dev/null 2>&1 || true
fi

echo ""
if [[ -n "${UNITY_LOG}" ]] && [[ -f "${UNITY_LOG}" ]]; then
    echo ">>> 详细日志: ${UNITY_LOG}"
fi

exit ${TEST_EXIT_CODE}
