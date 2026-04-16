#!/usr/bin/env bash
# ============================================================================
# Talos E2E 测试——PC (Windows/macOS) 模式启动脚本。
#
# 流程：
#   1. 检查可执行文件路径
#   2. 启动 PC 应用
#   3. 等待 TCP 服务就绪
#   4. 启动 Playwright 测试，通过 TCP 连接到应用
#   5. 测试完成后关闭应用
#
# 使用方式：
#   ./tools/test-pc.sh --exe /path/to/app.exe           # Windows
#   ./tools/test-pc.sh --exe /path/to/App.app --macos   # macOS
#
# 环境变量：
#   EXE_PATH    — 可执行文件路径（或通过 --exe 参数）
#   UNITY_HOST  — 应用 IP 地址，默认 127.0.0.1
#   UNITY_PORT  — TCP 端口，默认 10002
#   NODE_BIN / NPM_BIN / TALOS_NODEJS_HOME — 可选：显式指定 Node/npm 安装位置
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLAYWRIGHT_DIR="$(dirname "${SCRIPT_DIR}")"
# shellcheck source=./node-tools.sh
source "${SCRIPT_DIR}/node-tools.sh"

# ======== 默认参数 ========
EXE_PATH="${EXE_PATH:-}"
UNITY_HOST="${UNITY_HOST:-127.0.0.1}"
UNITY_PORT="${UNITY_PORT:-10002}"
IS_MACOS=false
PLAYWRIGHT_TEST_FILE="${PLAYWRIGHT_TEST_FILE:-}"
IS_WINDOWS_GIT_BASH=false

# ======== 参数解析 ========
while [[ $# -gt 0 ]]; do
    case $1 in
        --exe)       EXE_PATH="$2";    shift 2 ;;
        --host)      UNITY_HOST="$2";  shift 2 ;;
        --port)      UNITY_PORT="$2";  shift 2 ;;
        --macos)     IS_MACOS=true;    shift ;;
        --test-file) PLAYWRIGHT_TEST_FILE="$2"; shift 2 ;;
        --help)
            echo "用法: $0 --exe <path/to/executable> [--macos]"
            echo ""
            echo "选项:"
            echo "  --exe     可执行文件路径 (.exe / .app)"
            echo "  --host    应用 IP 地址 (默认 127.0.0.1)"
            echo "  --port    TCP 端口 (默认 10002)"
            echo "  --macos   标记为 macOS 应用（使用 open 命令启动）"
            echo "  --test-file Playwright 测试文件路径（相对 Playwright~ 根目录）"
            exit 0
            ;;
        *) echo "未知参数: $1"; exit 1 ;;
    esac
done

# 自动检测平台
if [[ "$(uname -s)" == "Darwin" && "${EXE_PATH}" == *.app ]]; then
    IS_MACOS=true
fi

case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) IS_WINDOWS_GIT_BASH=true ;;
esac

echo "============================================"
echo "  Talos E2E — PC 模式测试"
echo "============================================"
echo "  可执行:  ${EXE_PATH}"
echo "  地址:    ${UNITY_HOST}:${UNITY_PORT}"
echo "  平台:    $(${IS_MACOS} && echo 'macOS' || echo 'Windows/Linux')"
if [[ -n "${PLAYWRIGHT_TEST_FILE}" ]]; then
    echo "  测试文件: ${PLAYWRIGHT_TEST_FILE}"
fi
echo "============================================"

# ======== 前置检查 ========
if [[ -z "${EXE_PATH}" ]]; then
    echo "❌ 错误: 未指定可执行文件路径"
    echo "使用: $0 --exe /path/to/app.exe"
    echo "      $0 --exe /path/to/App.app --macos"
    exit 1
fi

if [[ ! -e "${EXE_PATH}" ]]; then
    echo "❌ 错误: 文件不存在: ${EXE_PATH}"
    exit 1
fi

# ======== 安装 Playwright 依赖 ========
echo ""
echo ">>> 检查 Playwright 依赖..."
ensure_talos_playwright_dependencies "${PLAYWRIGHT_DIR}"
cd "${PLAYWRIGHT_DIR}"

# ======== 启动应用 ========
echo ""
echo ">>> 启动应用..."
APP_PID=""
PLAYER_LOG_FILE=""
# 默认仍把 Unity 日志写到标准输出，便于 macOS/Linux 本地直接观察启动链路。
PLAYER_LAUNCH_ARGS=("-talosPort" "${UNITY_PORT}" "-talosForceE2E" "-screen-fullscreen" "0" "-logFile" "-")
echo "    启动参数: ${PLAYER_LAUNCH_ARGS[*]}"

print_windows_player_logs() {
    # Windows PowerShell 分支改用 Unity 自身的 -logFile，避免 Start-Process 重定向卡住 GUI 进程启动。
    if [[ -n "${PLAYER_LOG_FILE}" && -f "${PLAYER_LOG_FILE}" ]]; then
        echo ""
        echo ">>> Unity Player log (tail 200)"
        tail -n 200 "${PLAYER_LOG_FILE}" || true
    fi
}

if ${IS_MACOS}; then
    # macOS: 使用 open 命令启动 .app
    open "${EXE_PATH}" --args "${PLAYER_LAUNCH_ARGS[@]}"
    echo "    ✅ 应用已通过 open 启动"
else
    # Windows/Linux: 直接运行可执行文件
    if ${IS_WINDOWS_GIT_BASH} && command -v powershell.exe >/dev/null 2>&1 && command -v cygpath >/dev/null 2>&1; then
        # Git Bash 直接拉起 Windows exe 时会把当前目录丢成空字符串，这里改用 PowerShell 显式指定 WorkingDirectory。
        # 同时让 Unity 自己写日志文件，避免 Start-Process 重定向卡住 GUI 进程返回 PID。
        EXE_PATH_WIN="$(cygpath -w "${EXE_PATH}")"
        EXE_DIR_WIN="$(cygpath -w "$(dirname "${EXE_PATH}")")"
        PLAYER_LOG_FILE="${PLAYWRIGHT_DIR}/test-results/unity-player.log"
        : > "${PLAYER_LOG_FILE}"
        PLAYER_LOG_FILE_WIN="$(cygpath -w "${PLAYER_LOG_FILE}")"
        APP_PID="$({
            powershell.exe -NoProfile -Command "\
                \$proc = Start-Process -FilePath '${EXE_PATH_WIN}' -WorkingDirectory '${EXE_DIR_WIN}' -ArgumentList @('-talosPort','${UNITY_PORT}','-talosForceE2E','-screen-fullscreen','0','-logFile','${PLAYER_LOG_FILE_WIN}') -PassThru; \
                [Console]::Out.Write(\$proc.Id)\
            "
        } | tr -d '\r')"
        if [[ -z "${APP_PID}" ]]; then
            echo "    ❌ 未能获取应用 PID"
            print_windows_player_logs
            exit 1
        fi
        echo "    ✅ 应用已启动 (PID: ${APP_PID}, cwd: ${EXE_DIR_WIN})"
    else
        EXE_DIR="$(dirname "${EXE_PATH}")"
        EXE_NAME="$(basename "${EXE_PATH}")"
        pushd "${EXE_DIR}" >/dev/null
        "./${EXE_NAME}" "${PLAYER_LAUNCH_ARGS[@]}" &
        APP_PID=$!
        popd >/dev/null
        echo "    ✅ 应用已启动 (PID: ${APP_PID}, cwd: ${EXE_DIR})"
    fi
fi

# ======== 等待 TCP 服务就绪 ========
echo ""
echo ">>> 等待 Unity E2E TCP 服务就绪..."
MAX_WAIT=60
WAITED=0

while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
    # 检查进程（如果知道 PID）
    if [[ -n "${APP_PID}" ]]; then
        if ${IS_WINDOWS_GIT_BASH} && command -v powershell.exe >/dev/null 2>&1; then
            if ! powershell.exe -NoProfile -Command "exit [int](-not (Get-Process -Id ${APP_PID} -ErrorAction SilentlyContinue))" >/dev/null 2>&1; then
                echo ""
                echo "❌ 应用进程已退出"
                print_windows_player_logs
                exit 1
            fi
        elif ! kill -0 ${APP_PID} 2>/dev/null; then
            echo ""
            echo "❌ 应用进程已退出"
            exit 1
        fi
    fi

    if probe_talos_tcp_port "${UNITY_HOST}" "${UNITY_PORT}"; then
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
    print_windows_player_logs
    if [[ -n "${APP_PID}" ]]; then
        if ${IS_WINDOWS_GIT_BASH} && command -v taskkill.exe >/dev/null 2>&1; then
            taskkill.exe //PID ${APP_PID} //T >/dev/null 2>&1 || true
        else
            kill ${APP_PID} 2>/dev/null || true
        fi
    fi
    exit 1
fi

# ======== 运行 Playwright 测试 ========
echo ""
echo ">>> 运行 Playwright 测试..."
echo ""

# 根据平台选择 Playwright project
PROJECT="windows"
if ${IS_MACOS}; then
    PROJECT="macos"
fi

PLAYWRIGHT_COMMAND=("${TALOS_NODE_BIN}" "${PLAYWRIGHT_DIR}/node_modules/@playwright/test/cli.js" test)
if [[ -n "${PLAYWRIGHT_TEST_FILE}" ]]; then
    PLAYWRIGHT_COMMAND+=("${PLAYWRIGHT_TEST_FILE}")
fi
PLAYWRIGHT_COMMAND+=(
    "--project=${PROJECT}"
    "--output=${PLAYWRIGHT_DIR}/test-results/artifacts"
    "--reporter=list,html=${PLAYWRIGHT_DIR}/test-results/html,junit=${PLAYWRIGHT_DIR}/test-results/junit.xml"
)

PLATFORM="${PROJECT}" \
UNITY_HOST="${UNITY_HOST}" \
UNITY_PORT="${UNITY_PORT}" \
"${PLAYWRIGHT_COMMAND[@]}" 2>&1 | tee "${PLAYWRIGHT_DIR}/test-results/test-output.log"

TEST_EXIT_CODE=${PIPESTATUS[0]}

# ======== 关闭应用 ========
echo ""
echo ">>> 关闭应用..."
if [[ -n "${APP_PID}" ]]; then
    if ${IS_WINDOWS_GIT_BASH} && command -v taskkill.exe >/dev/null 2>&1; then
        taskkill.exe //PID ${APP_PID} >/dev/null 2>&1 || true
        taskkill.exe //F //PID ${APP_PID} //T >/dev/null 2>&1 || true
    else
        kill ${APP_PID} 2>/dev/null || true
        for _ in {1..10}; do
            kill -0 ${APP_PID} 2>/dev/null || break
            sleep 1
        done
        kill -9 ${APP_PID} 2>/dev/null || true
    fi
fi

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
