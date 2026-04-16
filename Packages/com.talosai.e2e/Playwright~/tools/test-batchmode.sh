#!/usr/bin/env bash
# ============================================================================
# Talos E2E 测试——Unity Batchmode（无界面）模式启动脚本。
#
# 支持两种执行模式：
#   1. TCP 模式（默认）: Unity 启动 TCP 服务 → Playwright 通过 TCP 连接执行测试
#   2. Sync 模式（回退）: Unity 同步执行测试 → 写 JSON 结果文件 → 脚本解析
#
# 使用方式：
#   # TCP 模式（默认）
#   UNITY_PATH=/path/to/Unity ./tools/test-batchmode.sh
#
#   # 同步回退模式（许可证无效时）
#   TALOS_MODE=sync UNITY_PATH=/path/to/Unity ./tools/test-batchmode.sh
#
# 环境变量：
#   UNITY_PATH      — Unity 可执行文件路径（必须）
#   UNITY_PORT      — TCP 端口，默认 10002
#   PROJECT_PATH    — Unity 项目路径，默认自动检测
#   TALOS_MODE      — 执行模式: tcp（默认）或 sync
#
# 注意：
#   - 脚本自动设置 UNITY_DISABLE_HUB=1 防止 Unity Hub 弹出 "reopen" 提示
#   - 脚本启动前会清理残留的 UnityLockfile
#   - 如需带 GUI 的 Unity Editor 测试，请使用 test-editorplayer.sh
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
TALOS_MODE="${TALOS_MODE:-tcp}"

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
if [[ -z "${UNITY_PATH}" ]]; then
    echo "❌ 错误: UNITY_PATH 环境变量未设置"
    echo ""
    echo "请设置 Unity 可执行文件路径，例如："
    echo "  export UNITY_PATH=/Applications/Unity2021.3.58f1/Unity.app/Contents/MacOS/Unity"
    echo ""
    echo "然后重新运行此脚本。"
    exit 1
fi

if [[ ! -x "${UNITY_PATH}" ]]; then
    echo "❌ 错误: UNITY_PATH 指向的文件不存在或不可执行: ${UNITY_PATH}"
    exit 1
fi

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
UNITY_LOG="${PROJECT_PATH}/Logs/talos_e2e_unity_$(date +%s).log"

# ====================================================================
# 模式分支
# ====================================================================
if [[ "${TALOS_MODE}" == "sync" ]]; then
    # ==================================================================
    # Sync 模式——Unity -quit 同步执行，写 JSON 结果文件
    # ==================================================================
    echo "============================================"
    echo "  Talos E2E — Unity Batchmode（同步模式）"
    echo "============================================"
    echo "  Unity:    ${UNITY_PATH}"
    echo "  项目:     ${PROJECT_PATH}"
    echo "  模式:     sync（-quit 同步执行）"
    echo "============================================"

    RESULT_FILE="${PROJECT_PATH}/talos_e2e_results.json"
    rm -f "${RESULT_FILE}"

    echo ""
    echo ">>> 启动 Unity Batchmode（同步执行模式）..."
    echo "    日志文件: ${UNITY_LOG}"
    echo "    结果文件: ${RESULT_FILE}"

    "${UNITY_PATH}" \
        -batchmode \
        -nographics \
        -quit \
        -projectPath "${PROJECT_PATH}" \
        -executeMethod BDFramework.Editor.Environment.TalosE2EBatchBridge.RunTalosE2EAndExport \
        -talosPort "${UNITY_PORT}" \
        -talosForceE2E \
        -talosOutput "${RESULT_FILE}" \
        -logFile "${UNITY_LOG}"

    UNITY_EXIT_CODE=$?
    echo ""
    echo ">>> Unity 已退出 (退出码: ${UNITY_EXIT_CODE})"

    echo ""
    echo ">>> 解析测试结果..."

    if [[ ! -f "${RESULT_FILE}" ]]; then
        echo "❌ 结果文件不存在: ${RESULT_FILE}"
        echo "    查看 Unity 日志: ${UNITY_LOG}"
        echo ""
        echo "--- 日志最后 30 行 ---"
        tail -30 "${UNITY_LOG}" 2>/dev/null || true
        exit 1
    fi

    python3 -c "
import json, sys
with open('${RESULT_FILE}') as f:
    data = json.load(f)
status = data.get('status', 'UNKNOWN')
total = data.get('total', 0)
passed = data.get('passed', 0)
failed = data.get('failed', 0)
print()
print('============================================')
print('  Talos E2E 测试结果')
print('============================================')
print(f'  状态:  {status}')
print(f'  总计:  {total}')
print(f'  通过:  {passed}')
print(f'  失败:  {failed}')
print('============================================')
print()
for r in data.get('results', []):
    icon = '✅' if r.get('passed') else '❌'
    suite = r.get('suite', '?')
    name = r.get('methodName', '?')
    desc = r.get('description', '')
    ms = r.get('durationMs', 0)
    print(f'  {icon} [{suite}] {name} - {desc} ({ms}ms)')
    err = r.get('errorMessage', '')
    if err:
        print(f'     错误: {err}')
print()
sys.exit(1 if failed > 0 else 0)
"
    TEST_EXIT_CODE=$?
    echo ""
    echo ">>> 详细日志: ${UNITY_LOG}"
    echo ">>> 结果文件: ${RESULT_FILE}"
    exit ${TEST_EXIT_CODE}

else
    # ==================================================================
    # TCP 模式——Unity 启动 TCP 服务，Playwright 通过 TCP 连接执行测试
    # ==================================================================
    echo "============================================"
    echo "  Talos E2E — Unity Batchmode（TCP 模式）"
    echo "============================================"
    echo "  Unity:    ${UNITY_PATH}"
    echo "  项目:     ${PROJECT_PATH}"
    echo "  端口:     ${UNITY_PORT}"
    echo "  模式:     tcp（Playwright 编排）"
    echo "============================================"

    echo ""
    echo ">>> 启动 Unity Batchmode（TCP 服务模式）..."
    echo "    日志文件: ${UNITY_LOG}"

    "${UNITY_PATH}" \
        -batchmode \
        -nographics \
        -projectPath "${PROJECT_PATH}" \
        -executeMethod BDFramework.Editor.Environment.TalosE2EBatchBridge.LaunchTalosE2EEditorOnly \
        -talosPort "${UNITY_PORT}" \
        -talosForceE2E \
        -logFile "${UNITY_LOG}" &
    UNITY_PID=$!
    echo "    Unity PID: ${UNITY_PID}"

    # ======== 等待 TCP 服务就绪 ========
    echo ""
    echo -n ">>> 等待 Unity E2E TCP 服务就绪"
    MAX_WAIT=60
    WAITED=0

    while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
        if ! kill -0 ${UNITY_PID} 2>/dev/null; then
            echo ""
            echo "❌ Unity 进程已退出（可能是许可证问题）"
            echo "    尝试使用带 GUI 的 EditorPlayer 模式: ./tools/test-editorplayer.sh"
            echo ""
            echo "--- 日志最后 30 行 ---"
            tail -30 "${UNITY_LOG}" 2>/dev/null || true
            exit 1
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
        kill ${UNITY_PID} 2>/dev/null || true
        exit 1
    fi

    # ======== 运行 Playwright 测试 ========
    echo ""
    echo ">>> 运行 Playwright 测试..."
    echo ""

    cd "${PLAYWRIGHT_DIR}"
    ensure_talos_playwright_dependencies "${PLAYWRIGHT_DIR}"

    PLATFORM=unityplayer \
    UNITY_HOST=127.0.0.1 \
    UNITY_PORT="${UNITY_PORT}" \
    "${TALOS_NODE_BIN}" "${PLAYWRIGHT_DIR}/node_modules/@playwright/test/cli.js" test \
        --project=batchmode \
        --reporter=list \
        2>&1

    TEST_EXIT_CODE=$?

    # ======== 关闭 Unity ========
    echo ""
    echo ">>> 关闭 Unity (PID: ${UNITY_PID})..."
    kill ${UNITY_PID} 2>/dev/null || true
    for _ in {1..10}; do
        kill -0 ${UNITY_PID} 2>/dev/null || break
        sleep 1
    done
    kill -9 ${UNITY_PID} >/dev/null 2>&1 || true

    echo ""
    echo ">>> 详细日志: ${UNITY_LOG}"

    exit ${TEST_EXIT_CODE}
fi
