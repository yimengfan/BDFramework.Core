#!/usr/bin/env bash
# ============================================================================
# Talos E2E 测试——UnityPlayer (headed GUI) 模式启动脚本。
#
# 以正常带界面的 Unity Editor 启动，用于：
#   1. E2E 测试和正常 Graph 模式交互
#   2. 验证场景加载、PlayMode 切换等 Editor 操作
#
# 与 test-batchmode.sh 的区别：
#   - 不使用 -batchmode -nographics，Unity 正常显示 GUI 窗口
#   - Playwright 通过 editor_command 协议操作 Unity Editor
#   - 支持 EditorOps 的场景操作、PlayMode 控制、万能反射等能力
#
# 使用方式：
#   UNITY_PATH=/path/to/Unity ./tools/test-editorplayer.sh
#
# 环境变量：
#   UNITY_PATH      — Unity 可执行文件路径（必须）
#   UNITY_PORT      — TCP 端口，默认 10002
#   PROJECT_PATH    — Unity 项目路径，默认自动检测
#   WAIT_ONLY       — 设为 1 时只等待已运行的 Unity 实例（不启动新的）
#
# 注意：
#   - 脚本自动设置 UNITY_DISABLE_HUB=1 防止 Unity Hub 弹出 "reopen" 提示
#   - 脚本启动前会清理残留的 UnityLockfile
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLAYWRIGHT_DIR="$(dirname "${SCRIPT_DIR}")"

# ======== 参数 ========
UNITY_PATH="${UNITY_PATH:-}"
UNITY_PORT="${UNITY_PORT:-10002}"
PROJECT_PATH="${PROJECT_PATH:-}"
WAIT_ONLY="${WAIT_ONLY:-0}"

# 强制禁用 Unity Hub，防止 "reopen" 提示
export UNITY_DISABLE_HUB=1

# 自动检测项目路径（向上查找到包含 Assets/ 的目录）
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

# ======== 前置检查 ========
if [[ -z "${PROJECT_PATH}" ]]; then
    echo "❌ 错误: 无法自动检测项目路径，请设置 PROJECT_PATH 环境变量"
    exit 1
fi

# ======== 清理残留 Lock 文件 ========
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
# 启动或等待 Unity Editor
# ====================================================================
echo "============================================"
echo "  Talos E2E — UnityPlayer（headed GUI 模式）"
echo "============================================"
echo "  项目:     ${PROJECT_PATH}"
echo "  端口:     ${UNITY_PORT}"
echo "  模式:     headed GUI（Playwright 编排）"
echo "============================================"

if [[ "${WAIT_ONLY}" == "1" ]]; then
    # 仅等待已运行的 Unity 实例
    echo ""
    echo ">>> WAIT_ONLY=1，跳过 Unity 启动，等待已有实例..."
else
    # ======== 前置检查 ========
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

    # 注意：不使用 -batchmode -nographics，Unity 正常显示 GUI
    "${UNITY_PATH}" \
        -projectPath "${PROJECT_PATH}" \
        -executeMethod Talos.E2E.Editor.E2EEditorTools.LaunchE2EEditorOnly \
        -talosPort "${UNITY_PORT}" \
        -talosForceE2E \
        -logFile "${UNITY_LOG}" &
    UNITY_PID=$!
    LAUNCHED_BY_US=1
    echo "    Unity PID: ${UNITY_PID}"
fi

# ======== 等待 TCP 服务就绪 ========
echo ""
echo -n ">>> 等待 Unity E2E TCP 服务就绪"
MAX_WAIT=120
WAITED=0

while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
    # 如果我们启动了 Unity，检查进程是否存活
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

    if command -v nc &>/dev/null; then
        if nc -z 127.0.0.1 "${UNITY_PORT}" 2>/dev/null; then
            echo " ✅"
            echo "    TCP 服务已就绪 (${WAITED}s)"
            break
        fi
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

# ======== 运行 Playwright 测试 ========
echo ""
echo ">>> 运行 Playwright EditorPlayer 测试..."
echo ""

cd "${PLAYWRIGHT_DIR}"

if [[ ! -d "node_modules" ]]; then
    echo ">>> 安装 Playwright 依赖..."
    npm install
fi

PLATFORM=unityplayer \
UNITY_HOST=127.0.0.1 \
UNITY_PORT="${UNITY_PORT}" \
node "${PLAYWRIGHT_DIR}/node_modules/@playwright/test/cli.js" test \
    --project=unityplayer \
    --reporter=list \
    2>&1

TEST_EXIT_CODE=$?

# ======== 关闭 Unity ========
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
