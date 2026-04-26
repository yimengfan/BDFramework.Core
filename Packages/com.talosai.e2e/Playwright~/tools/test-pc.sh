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
#   UNITY_PORT  — Unity 候选 TCP 端口基值，默认 10002
#   TALOS_UNITY_TCP_TIMEOUT — TCP 就绪等待秒数，默认 180
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
IS_WINDOWS_TEAMCITY=false

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
if ${IS_WINDOWS_GIT_BASH} && [[ -n "${TEAMCITY_VERSION:-}" ]]; then
    IS_WINDOWS_TEAMCITY=true
fi

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
PLAYER_LOG_FILE_SUFFIX="${TEAMCITY_BUILD_ID:-${BUILD_ID:-local-$$}}"
PLAYER_LOG_ARCHIVE_DIR="${PLAYWRIGHT_DIR}/test-results/playerlogs"
# 默认仍把 Unity 日志写到标准输出，便于本地直接观察启动链路。
# Keep Unity logging on stdout by default so local runs can inspect the startup chain directly.
PLAYER_LAUNCH_ARGS=()
if ${IS_WINDOWS_TEAMCITY}; then
    # Windows TeamCity agent 不需要桌面输入，也不需要真实图形设备，
    # 这里改用 batchmode + nographics 让 standalone player 跳过窗口与图形初始化。
    # The Windows TeamCity agent does not need desktop input or a real graphics device,
    # so batchmode plus nographics lets the standalone player bypass window and graphics initialization.
    PLAYER_LAUNCH_ARGS+=("-batchmode" "-nographics")
fi
PLAYER_LAUNCH_ARGS+=("-logFile" "-")
echo "    启动参数: ${PLAYER_LAUNCH_ARGS[*]}"

resolve_talos_port_candidates() {
    local base_port="${1}"
    printf '%s\n' "${base_port}" "$((base_port + 10))" "$((base_port + 20))"
}

print_windows_player_logs() {
    # Windows PowerShell 分支改用 Unity 自身的 -logFile，避免 Start-Process 重定向卡住 GUI 进程启动。
    if [[ -n "${PLAYER_LOG_FILE}" && -f "${PLAYER_LOG_FILE}" ]]; then
        echo ""
        echo ">>> Unity Player log (tail 200)"
        tail -n 200 "${PLAYER_LOG_FILE}" || true
    fi
}

resolve_player_log_source_dir() {
    # 按 BDFramework 的 BApplication / BDebug 约定推导 playerlogs 目录，供 TeamCity artifact 回收持久化日志。
    # Resolve the playerlogs directory from the BDFramework BApplication and BDebug contract so TeamCity artifacts can recover persisted logs.
    if ${IS_MACOS}; then
        printf '%s/Contents/.AppData/playerlogs\n' "${EXE_PATH}"
        return 0
    fi

    local exe_dir exe_name exe_stem
    exe_dir="$(dirname "${EXE_PATH}")"
    exe_name="$(basename "${EXE_PATH}")"
    exe_stem="${exe_name%.*}"
    printf '%s/%s_Data/.AppData/playerlogs\n' "${exe_dir}" "${exe_stem}"
}

capture_persistent_player_logs() {
    # 把 BDebug 二进制日志整体拷回 test-results/playerlogs，并生成索引文件，便于 TeamCity 直接查看来源与文件清单。
    # Copy the BDebug binary logs back into test-results/playerlogs and write an index file so TeamCity can inspect the source path and file list directly.
    local player_log_source_dir player_log_index_file
    player_log_source_dir="$(resolve_player_log_source_dir)"
    rm -rf "${PLAYER_LOG_ARCHIVE_DIR}"
    mkdir -p "${PLAYER_LOG_ARCHIVE_DIR}"
    player_log_index_file="${PLAYER_LOG_ARCHIVE_DIR}/index.txt"

    {
        echo "source=${player_log_source_dir}"
        if [[ -d "${player_log_source_dir}" ]]; then
            echo "status=found"
            while IFS= read -r relative_path; do
                [[ -n "${relative_path}" ]] || continue
                echo "file=${relative_path}"
            done < <(cd "${player_log_source_dir}" && find . -type f | sed 's#^\./##' | LC_ALL=C sort)
        else
            echo "status=missing"
        fi
    } > "${player_log_index_file}"

    if [[ -d "${player_log_source_dir}" ]]; then
        cp -R "${player_log_source_dir}/." "${PLAYER_LOG_ARCHIVE_DIR}/"
        echo ">>> 已归档 BDebug playerlogs: ${PLAYER_LOG_ARCHIVE_DIR}"
    else
        echo ">>> 未发现 BDebug playerlogs: ${player_log_source_dir}"
    fi
}

cleanup_stale_windows_player_processes() {
    # 在启动新的 Windows Player 前先清理 TeamCity 工作区里遗留的旧进程，避免旧实例继续占用 Talos TCP 端口或锁住上一次运行的日志文件。
    # Clean old TeamCity workspace player processes before starting a new Windows player so stale instances do not keep the Talos TCP port or the previous run log files locked.
    if ! ${IS_WINDOWS_GIT_BASH} || ! command -v powershell.exe >/dev/null 2>&1 || ! command -v cygpath >/dev/null 2>&1; then
        return 0
    fi

    local prepared_package_root_win executable_name executable_stem cleanup_summary
    prepared_package_root_win="$(cygpath -w "${PLAYWRIGHT_DIR}/test-results/p")"
    executable_name="$(basename "${EXE_PATH}")"
    executable_stem="${executable_name%.*}"

    # 使用子 shell + set +e 避免 PowerShell 错误导致整个脚本退出
    # Use subshell + set +e to prevent PowerShell errors from exiting the entire script
    cleanup_summary="$(set +e; {
        powershell.exe -NoProfile -Command "\
            \$unityPorts = @($(resolve_talos_port_candidates "${UNITY_PORT}" | paste -sd, -)); \
            \$preparedRoot = '${prepared_package_root_win}'.ToLowerInvariant(); \
            \$targetName = '${executable_stem}'.ToLowerInvariant(); \
            \$stopped = New-Object System.Collections.Generic.List[string]; \
            \$seen = New-Object System.Collections.Generic.HashSet[int]; \
            if (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue) { \
                foreach (\$unityPort in \$unityPorts) { \
                    Get-NetTCPConnection -State Listen -LocalPort \$unityPort -ErrorAction SilentlyContinue | \
                        Select-Object -ExpandProperty OwningProcess -Unique | \
                        ForEach-Object { \
                            \$processId = [int]\$_; \
                            if (\$seen.Add(\$processId)) { \
                                Stop-Process -Id \$processId -Force -ErrorAction SilentlyContinue; \
                                [void]\$stopped.Add(\"port:\$unityPort:\$processId\"); \
                            } \
                        }; \
                } \
            } \
            Get-Process -Name '${executable_stem}' -ErrorAction SilentlyContinue | \
                Where-Object { \
                    \$_.Path -and \
                    \$_.Path.ToLowerInvariant().StartsWith(\$preparedRoot) -and \
                    \$_.ProcessName.ToLowerInvariant() -eq \$targetName \
                } | \
                ForEach-Object { \
                    if (\$seen.Add(\$_.Id)) { \
                        Stop-Process -Id \$_.Id -Force -ErrorAction SilentlyContinue; \
                        [void]\$stopped.Add(\"path:\$($_.Id)\"); \
                    } \
                }; \
            if (\$stopped.Count -gt 0) { \
                [Console]::Out.Write([string]::Join(',', \$stopped)); \
            }\
        "
    } 2>/dev/null | tr -d '\r')" || true

    if [[ -n "${cleanup_summary}" ]]; then
        echo ">>> 已清理 Windows 残留进程: ${cleanup_summary}"
    fi
}

cleanup_stale_windows_player_processes

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
        PLAYER_LOG_FILE="${PLAYWRIGHT_DIR}/test-results/unity-player-${PLAYER_LOG_FILE_SUFFIX}.log"
        : > "${PLAYER_LOG_FILE}"
        PLAYER_LOG_FILE_WIN="$(cygpath -w "${PLAYER_LOG_FILE}")"
        if ${IS_WINDOWS_TEAMCITY}; then
            POWERSHELL_ARGUMENT_LIST_LITERAL="@('-batchmode','-nographics','-logFile','${PLAYER_LOG_FILE_WIN}')"
        else
            POWERSHELL_ARGUMENT_LIST_LITERAL="@('-logFile','${PLAYER_LOG_FILE_WIN}')"
        fi
        # 使用子 shell + set +e 避免 PowerShell 错误导致整个脚本退出
        # Use subshell + set +e to prevent PowerShell errors from exiting the entire script
        APP_PID="$(set +e; {
            powershell.exe -NoProfile -Command "\
                try { \
                    \$proc = Start-Process -FilePath '${EXE_PATH_WIN}' -WorkingDirectory '${EXE_DIR_WIN}' -ArgumentList ${POWERSHELL_ARGUMENT_LIST_LITERAL} -PassThru -ErrorAction Stop; \
                    [Console]::Out.Write(\$proc.Id); \
                } catch { \
                    [Console]::Error.Write(\"PowerShell Start-Process failed: \$_\"); \
                    exit 1; \
                }\
            "
        } 2>&1 | tr -d '\r')" || true
        if [[ -z "${APP_PID}" ]]; then
            echo "    ❌ 未能获取应用 PID"
            print_windows_player_logs
            capture_persistent_player_logs
            exit 1
        fi
        # 检查 APP_PID 是否为有效数字（PowerShell 可能返回错误信息）
        # Check if APP_PID is a valid number (PowerShell might return error message)
        if ! [[ "${APP_PID}" =~ ^[0-9]+$ ]]; then
            echo "    ❌ PowerShell 返回无效 PID: ${APP_PID}"
            print_windows_player_logs
            capture_persistent_player_logs
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
# TeamCity Windows agent 上的冷启动往往慢于本地；默认对齐 Android 的 180s，并允许环境变量覆盖。
# Cold starts on TeamCity Windows agents are often slower than local runs; default to 180s like Android and allow an environment override.
MAX_WAIT="${TALOS_UNITY_TCP_TIMEOUT:-180}"
WAITED=0
RESOLVED_UNITY_PORT=""
PORT_CANDIDATES=()
while IFS= read -r candidate_port; do
    PORT_CANDIDATES+=("${candidate_port}")
done < <(resolve_talos_port_candidates "${UNITY_PORT}")
echo "    候选端口: ${PORT_CANDIDATES[*]}"

while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
    # 检查进程（如果知道 PID）
    if [[ -n "${APP_PID}" ]]; then
        if ${IS_WINDOWS_GIT_BASH} && command -v powershell.exe >/dev/null 2>&1; then
            if ! powershell.exe -NoProfile -Command "exit [int](-not (Get-Process -Id ${APP_PID} -ErrorAction SilentlyContinue))" >/dev/null 2>&1; then
                echo ""
                echo "❌ 应用进程已退出"
                print_windows_player_logs
                capture_persistent_player_logs
                exit 1
            fi
        elif ! kill -0 ${APP_PID} 2>/dev/null; then
            echo ""
            echo "❌ 应用进程已退出"
            capture_persistent_player_logs
            exit 1
        fi
    fi

    for candidate_port in "${PORT_CANDIDATES[@]}"; do
        if probe_talos_unity_ready "${UNITY_HOST}" "${candidate_port}" 1000; then
            RESOLVED_UNITY_PORT="${candidate_port}"
            UNITY_PORT="${candidate_port}"
            echo "    ✅ TCP 服务已就绪 (${WAITED}s) 端口=${UNITY_PORT}"
            break 2
        fi
    done
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
    capture_persistent_player_logs
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
    "--reporter=list,html,junit"
)

# 兼容 Playwright 1.40.1 旧版 HTML reporter 环境变量，同时保留新版变量，避免 CI 因自动打开 report 而挂住。
# Support the legacy Playwright 1.40.1 HTML reporter environment variables while keeping the newer names so CI does not hang by auto-opening the report server.
PLAYWRIGHT_HTML_OUTPUT_DIR="${PLAYWRIGHT_DIR}/test-results/html" \
PLAYWRIGHT_HTML_REPORT="${PLAYWRIGHT_DIR}/test-results/html" \
PLAYWRIGHT_HTML_OPEN=never \
PW_TEST_HTML_REPORT_OPEN=never \
PLAYWRIGHT_JUNIT_OUTPUT_FILE="${PLAYWRIGHT_DIR}/test-results/junit.xml" \
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
capture_persistent_player_logs
if [[ ${TEST_EXIT_CODE} -ne 0 || -n "${TEAMCITY_VERSION:-}" ]]; then
    print_windows_player_logs
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
