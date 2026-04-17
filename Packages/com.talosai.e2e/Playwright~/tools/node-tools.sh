#!/usr/bin/env bash
# ==========================================================================
# Talos E2E Playwright Node/Android 工具解析公共函数。
# Talos E2E Playwright shared Node/Android tool resolution helpers.
#
# 目标 / Goals:
#   1. 在本机 PATH 正常时直接复用 node/npm/adb。
#      Reuse node/npm/adb directly when PATH is already configured.
#   2. 在 Windows TeamCity service 缺少 PATH 时，回退到标准安装目录。
#      Fall back to common install roots when a Windows TeamCity service has an incomplete PATH.
#   3. 允许通过显式环境变量覆盖，方便 CI 与测试注入。
#      Allow explicit environment overrides for CI and automated tests.
# ==========================================================================

# 解析通用工具候选路径，兼容 PATH、显式绝对路径和 Windows 风格路径。
# Resolve a generic tool candidate from PATH, explicit paths, and Windows-style paths.
resolve_talos_tool_candidate() {
    local candidate="${1:-}"
    local normalized=""
    if [[ -z "${candidate}" ]]; then
        return 1
    fi

    if [[ "${candidate}" == */* || "${candidate}" == *\\* || "${candidate}" =~ ^[A-Za-z]:[\\/].* ]]; then
        if [[ -f "${candidate}" || -x "${candidate}" ]]; then
            printf '%s\n' "${candidate}"
            return 0
        fi

        if command -v cygpath >/dev/null 2>&1; then
            if normalized="$(cygpath -u "${candidate}" 2>/dev/null)" && [[ -n "${normalized}" ]] && [[ "${normalized}" != "${candidate}" ]] && [[ -f "${normalized}" || -x "${normalized}" ]]; then
                printf '%s\n' "${normalized}"
                return 0
            fi
        fi

        return 1
    fi

    if command -v "${candidate}" >/dev/null 2>&1; then
        command -v "${candidate}"
        return 0
    fi

    return 1
}

collect_talos_node_homes() {
    local homes=()
    if [[ -n "${TALOS_NODEJS_HOME:-}" ]]; then
        homes+=("${TALOS_NODEJS_HOME}")
    fi
    if [[ -n "${NODEJS_HOME:-}" ]] && [[ "${NODEJS_HOME}" != "${TALOS_NODEJS_HOME:-}" ]]; then
        homes+=("${NODEJS_HOME}")
    fi
    homes+=("/c/Program Files/nodejs")
    homes+=("/c/Program Files (x86)/nodejs")

    if ((${#homes[@]} > 0)); then
        printf '%s\n' "${homes[@]}"
    fi
}

collect_talos_explicit_node_homes() {
    local homes=()
    if [[ -n "${TALOS_NODEJS_HOME:-}" ]]; then
        homes+=("${TALOS_NODEJS_HOME}")
    fi
    if [[ -n "${NODEJS_HOME:-}" ]] && [[ "${NODEJS_HOME}" != "${TALOS_NODEJS_HOME:-}" ]]; then
        homes+=("${NODEJS_HOME}")
    fi

    if ((${#homes[@]} > 0)); then
        printf '%s\n' "${homes[@]}"
    fi
}

collect_talos_default_node_homes() {
    printf '%s\n' "/c/Program Files/nodejs" "/c/Program Files (x86)/nodejs"
}

# 收集显式配置的 Android SDK 根目录，优先信任 CI 注入的环境变量。
# Collect explicitly configured Android SDK roots, prioritizing CI-injected environment variables.
collect_talos_explicit_android_sdk_homes() {
    local homes=()
    local unity_path="${UNITY_PATH:-}"
    local unity_shell_path=""
    if [[ -n "${ANDROID_SDK_ROOT:-}" ]]; then
        homes+=("${ANDROID_SDK_ROOT}")
    fi
    if [[ -n "${ANDROID_HOME:-}" ]] && [[ "${ANDROID_HOME}" != "${ANDROID_SDK_ROOT:-}" ]]; then
        homes+=("${ANDROID_HOME}")
    fi
    if [[ -n "${LOCALAPPDATA:-}" ]]; then
        homes+=("${LOCALAPPDATA}/Android/Sdk")
    fi
    if [[ -n "${unity_path}" ]]; then
        unity_shell_path="${unity_path}"
        if command -v cygpath >/dev/null 2>&1; then
            unity_shell_path="$(cygpath -u "${unity_path}" 2>/dev/null || printf '%s' "${unity_path}")"
        fi
        if [[ "${unity_shell_path}" == */Unity.exe ]]; then
            homes+=("${unity_shell_path%/Unity.exe}/Data/PlaybackEngines/AndroidPlayer/SDK")
        elif [[ "${unity_shell_path}" == */Unity ]]; then
            homes+=("${unity_shell_path%/Unity}/Data/PlaybackEngines/AndroidPlayer/SDK")
        fi
    fi

    printf '%s\n' "${homes[@]}"
}

# 收集 TeamCity Windows service 常见的 Android SDK 默认安装目录。
# Collect common Android SDK default install roots for TeamCity Windows service agents.
collect_talos_default_android_sdk_homes() {
    local homes=()
    local nullglob_enabled=0
    if shopt -q nullglob; then
        nullglob_enabled=1
    else
        shopt -s nullglob
    fi

    if [[ -n "${USERNAME:-}" ]]; then
        homes+=("/c/Users/${USERNAME}/AppData/Local/Android/Sdk")
    fi
    if [[ -n "${USER:-}" ]] && [[ "${USER}" != "${USERNAME:-}" ]]; then
        homes+=("/c/Users/${USER}/AppData/Local/Android/Sdk")
    fi
    homes+=("/c/Android/Sdk")
    homes+=("/c/Program Files/Android/Android Studio/sdk")
    homes+=("/c/Program Files/Unity/Editor/Data/PlaybackEngines/AndroidPlayer/SDK")
    homes+=("/c/Program Files/Unity/Hub/Editor"/*/Editor/Data/PlaybackEngines/AndroidPlayer/SDK)

    if [[ ${nullglob_enabled} -eq 0 ]]; then
        shopt -u nullglob
    fi

    printf '%s\n' "${homes[@]}"
}

# 解析 ADB 可执行文件，兼容 TeamCity service 未把 Android SDK 注入 PATH 的场景。
# Resolve the ADB executable when a TeamCity service has Android SDK installed but PATH is incomplete.
resolve_talos_adb_bin() {
    local candidate=""
    local sdk_home=""

    if candidate="$(resolve_talos_tool_candidate "${ADB_BIN:-}")"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    while IFS= read -r sdk_home; do
        [[ -z "${sdk_home}" ]] && continue
        if candidate="$(resolve_talos_tool_candidate "${sdk_home}/platform-tools/adb.exe")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
        if candidate="$(resolve_talos_tool_candidate "${sdk_home}/platform-tools/adb")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
    done < <(collect_talos_explicit_android_sdk_homes)

    if candidate="$(resolve_talos_tool_candidate adb)"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    if candidate="$(resolve_talos_tool_candidate adb.exe)"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    while IFS= read -r sdk_home; do
        [[ -z "${sdk_home}" ]] && continue
        if candidate="$(resolve_talos_tool_candidate "${sdk_home}/platform-tools/adb.exe")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
        if candidate="$(resolve_talos_tool_candidate "${sdk_home}/platform-tools/adb")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
    done < <(collect_talos_default_android_sdk_homes)

    return 1
}

# 确保 ADB 工具已经解析完成，并输出最终使用的可执行文件路径。
# Ensure the ADB tool is resolved and expose the final executable path for callers.
ensure_talos_adb_tooling() {
    if [[ -n "${TALOS_ADB_BIN:-}" ]]; then
        return 0
    fi

    if ! TALOS_ADB_BIN="$(resolve_talos_adb_bin)"; then
        echo "❌ 错误: 未找到 adb 可执行文件，请配置 ADB_BIN、ANDROID_SDK_ROOT 或 ANDROID_HOME"
        return 1
    fi

    export TALOS_ADB_BIN
    echo ">>> Android 工具: adb=${TALOS_ADB_BIN}"
}

# 尝试连接一批 ADB 目标地址，适用于宿主机运行 Android 模拟器（如 MuMu 模拟器）无法通过 USB 探测的场景。
# Try to connect a batch of ADB targets for host-local emulators (e.g. MuMu) that are invisible to adb devices until explicitly connected.
#
# 参数：逗号分隔的 host:port 字符串，例如 "127.0.0.1:16384,127.0.0.1:7555"。
# Argument: comma-separated host:port list, e.g. "127.0.0.1:16384,127.0.0.1:7555".
#   MuMu Player 2 (MuMu2) 第一个实例默认监听 127.0.0.1:16384。
#   MuMu Player 2 (MuMu2) first instance listens on 127.0.0.1:16384 by default.
#   MuMu 旧版 / MuMu Pro 默认监听 127.0.0.1:7555。
#   MuMu legacy / MuMu Pro listens on 127.0.0.1:7555 by default.
# 返回值：无论连接成功与否均返回 0；调用方自行校验 adb devices 输出。
# Return: always 0; callers verify adb devices output themselves.
ensure_talos_adb_connect_targets() {
    local raw_targets="${1:-}"
    [[ -z "${raw_targets}" ]] && return 0

    local adb_cmd="${TALOS_ADB_BIN:-adb}"
    local connected_any=0
    local target=""

    IFS=',' read -ra target_list <<< "${raw_targets}"
    for target in "${target_list[@]}"; do
        target="$(printf '%s' "${target}" | tr -d '[:space:]')"
        [[ -z "${target}" ]] && continue
        echo ">>> 正在尝试连接 ADB 目标: ${target}"
        local connect_output=""
        connect_output="$("${adb_cmd}" connect "${target}" 2>&1 || true)"
        echo "    ${connect_output}"
        if printf '%s' "${connect_output}" | grep -qiE 'connected to|already connected'; then
            echo "    ✅ 已连接: ${target}"
            connected_any=1
        else
            echo "    ⚠️  连接未确认: ${target} (继续尝试其余目标)"
        fi
    done

    if [[ ${connected_any} -gt 0 ]]; then
        sleep 1
    fi
    return 0
}

# 确保 MuMu Android 模拟器进程正在运行。
# 步骤：① 检测进程列表是否有 MuMu 相关进程；② 若未运行，按优先级搜索常见安装目录；
#       ③ 找到 exe 后在后台启动，并等待虚拟机初始化，之后由调用方执行 adb connect。
# 始终返回 0（最大努力策略），实际连通性由后续 adb connect / adb devices 验证。
#
# Ensure the MuMu Android emulator process is running.
# Steps: ① check the process list for any MuMu-related process; ② if not running, search
#        common install paths in version-priority order; ③ launch in background and wait for VM init,
#        then the caller runs adb connect.
# Always returns 0 (best-effort); actual device availability is confirmed by later adb checks.
#
# 支持版本 / Supported versions:
#   MuMu Player 2 (MuMu2): 进程 MuMuPlayer.exe，路径含 MuMuPlayer-12.0 或 MuMuPlayer
#   MuMu 旧版 / MuMu X:    进程 NemuPlayer.exe / NemuVM.exe，路径含 nemu64 或 nemu
#
# 可通过 TALOS_MUMU_WAIT_SECONDS 环境变量控制启动后等待时间（默认 20 秒）。
# The startup wait duration can be overridden via TALOS_MUMU_WAIT_SECONDS (default 20s).
ensure_talos_mumu_running() {
    # MuMu 各版本进程名，用于 tasklist.exe / ps 探测。
    # Process names covering MuMu2, MuMu X, and legacy MuMu.
    local -a mumu_process_names=(
        "MuMuPlayer.exe"    # MuMu Player 2 (MuMu2) 主进程 / main process
        "MuMuVMMSVC.exe"    # MuMu2 VM 服务进程 / VM service
        "NemuPlayer.exe"    # MuMu 旧版 / MuMu X 主进程 / legacy main
        "NemuVM.exe"        # MuMu 旧版 VM 进程 / legacy VM service
    )

    # MuMu 各版本常见安装目录（MuMu2 优先，向下兼容旧版）。
    # Candidate exe paths in version-priority order (MuMu2 first, legacy last).
    local -a mumu_exe_candidates=(
        "/c/Program Files/Netease/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/c/Program Files/NetEase/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/c/Program Files/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/c/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/c/Program Files/Netease/MuMuPlayer/shell/MuMuPlayer.exe"
        "/c/Program Files/NetEase/MuMuPlayer/shell/MuMuPlayer.exe"
        "/c/Program Files/MuMuPlayer/shell/MuMuPlayer.exe"
        "/c/Program Files/NetEase/MuMu Player/emulator/nemu64/EmulatorShell/NemuPlayer.exe"
        "/c/Program Files (x86)/NetEase/MuMu Player/emulator/nemu64/EmulatorShell/NemuPlayer.exe"
        "/c/MuMu/emulator/nemu64/EmulatorShell/NemuPlayer.exe"
        "/c/MuMu/emulator/nemu/EmulatorShell/NemuPlayer.exe"
    )

    echo ">>> 检查 MuMu 模拟器是否正在运行..."

    # 步骤 1：检测进程列表，Windows 用 tasklist.exe，其余环境用 ps。
    # Step 1: detect process list; use tasklist.exe on Windows, ps on other platforms.
    local pname=""
    local is_running=0
    if command -v tasklist.exe >/dev/null 2>&1; then
        for pname in "${mumu_process_names[@]}"; do
            if tasklist.exe /FI "IMAGENAME eq ${pname}" 2>/dev/null | grep -qi "${pname}"; then
                echo "    ✅ MuMu 模拟器已在运行 (进程: ${pname})"
                is_running=1
                break
            fi
        done
    else
        # 非 Windows 环境回退（macOS / Linux 单元测试场景）。
        # Non-Windows fallback (macOS/Linux unit test environments).
        for pname in "${mumu_process_names[@]}"; do
            if ps aux 2>/dev/null | grep -v grep | grep -qi "${pname}"; then
                echo "    ✅ MuMu 模拟器已在运行 (进程: ${pname})"
                is_running=1
                break
            fi
        done
    fi

    [[ ${is_running} -eq 1 ]] && return 0

    # 步骤 2：搜索常见安装目录，获取可执行文件路径。
    # Step 2: search common install directories for the MuMu executable.
    echo "    MuMu 未在运行，开始搜索常见安装目录..."
    local exe_path=""
    local candidate=""
    for candidate in "${mumu_exe_candidates[@]}"; do
        if [[ -f "${candidate}" ]]; then
            exe_path="${candidate}"
            echo "    已找到 MuMu 可执行文件: ${exe_path}"
            break
        fi
    done

    if [[ -z "${exe_path}" ]]; then
        echo "    ⚠️  常见目录下未找到 MuMu 可执行文件，跳过自动启动"
        echo "    (若已通过其他方式启动，后续 ADB connect 步骤仍会尝试连接)"
        return 0
    fi

    # 步骤 3：后台启动 MuMu，并等待虚拟机初始化完成。
    # Step 3: launch MuMu in background and wait for VM initialization.
    echo "    正在后台启动 MuMu: ${exe_path}"
    if command -v cmd.exe >/dev/null 2>&1; then
        # Windows Git Bash 环境：通过 cmd.exe /c start 在后台启动 exe。
        # Windows Git Bash: use cmd.exe /c start to launch exe detached in background.
        local win_path="${exe_path}"
        if command -v cygpath >/dev/null 2>&1; then
            win_path="$(cygpath -w "${exe_path}" 2>/dev/null || printf '%s' "${exe_path}")"
        fi
        cmd.exe /c start "" "${win_path}" 2>/dev/null || true
    else
        # 非 Windows 本地调测回退：无法真正启动 Windows .exe，仅打印告知。
        # Non-Windows local debug fallback: cannot actually run a .exe; log only.
        echo "    (非 Windows 环境，跳过 exe 启动)"
    fi

    local wait_secs="${TALOS_MUMU_WAIT_SECONDS:-20}"
    echo "    等待 MuMu 虚拟机初始化 (${wait_secs}s)..."
    sleep "${wait_secs}"
    echo "    ✅ MuMu 启动等待完成，继续后续 ADB 连接"
    return 0
}

resolve_talos_node_bin() {
    local candidate=""

    if candidate="$(resolve_talos_tool_candidate "${NODE_BIN:-}")"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    local home=""
    while IFS= read -r home; do
        [[ -z "${home}" ]] && continue
        if candidate="$(resolve_talos_tool_candidate "${home}/node.exe")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
        if candidate="$(resolve_talos_tool_candidate "${home}/node")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
    done < <(collect_talos_explicit_node_homes)

    if candidate="$(resolve_talos_tool_candidate node)"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    while IFS= read -r home; do
        [[ -z "${home}" ]] && continue
        if candidate="$(resolve_talos_tool_candidate "${home}/node.exe")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
        if candidate="$(resolve_talos_tool_candidate "${home}/node")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
    done < <(collect_talos_default_node_homes)

    return 1
}

resolve_talos_npm_bin() {
    local candidate=""

    if candidate="$(resolve_talos_tool_candidate "${NPM_BIN:-}")"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    local home=""
    while IFS= read -r home; do
        [[ -z "${home}" ]] && continue
        if candidate="$(resolve_talos_tool_candidate "${home}/npm.cmd")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
        if candidate="$(resolve_talos_tool_candidate "${home}/npm")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
    done < <(collect_talos_explicit_node_homes)

    if candidate="$(resolve_talos_tool_candidate npm)"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    if candidate="$(resolve_talos_tool_candidate npm.cmd)"; then
        printf '%s\n' "${candidate}"
        return 0
    fi

    while IFS= read -r home; do
        [[ -z "${home}" ]] && continue
        if candidate="$(resolve_talos_tool_candidate "${home}/npm.cmd")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
        if candidate="$(resolve_talos_tool_candidate "${home}/npm")"; then
            printf '%s\n' "${candidate}"
            return 0
        fi
    done < <(collect_talos_default_node_homes)

    return 1
}

ensure_talos_node_tooling() {
    if [[ -n "${TALOS_NODE_BIN:-}" ]] && [[ -n "${TALOS_NPM_BIN:-}" ]]; then
        return 0
    fi

    if ! TALOS_NODE_BIN="$(resolve_talos_node_bin)"; then
        echo "❌ 错误: 未找到 node 可执行文件，请配置 NODE_BIN 或 TALOS_NODEJS_HOME"
        return 1
    fi

    if ! TALOS_NPM_BIN="$(resolve_talos_npm_bin)"; then
        echo "❌ 错误: 未找到 npm 可执行文件，请配置 NPM_BIN 或 TALOS_NODEJS_HOME"
        return 1
    fi

    export TALOS_NODE_BIN TALOS_NPM_BIN
    echo ">>> Node 工具: node=${TALOS_NODE_BIN}"
    echo ">>> Node 工具: npm=${TALOS_NPM_BIN}"
}

ensure_talos_playwright_dependencies() {
    local playwright_dir="${1:-}"
    if [[ -z "${playwright_dir}" ]]; then
        echo "❌ 错误: 缺少 Playwright 目录参数"
        return 1
    fi

    ensure_talos_node_tooling || return 1

    if [[ ! -d "${playwright_dir}/node_modules" ]]; then
        echo ">>> 安装 Playwright 依赖..."
        (
            cd "${playwright_dir}"
            "${TALOS_NPM_BIN}" install
        )
    fi

    if [[ ! -f "${playwright_dir}/node_modules/@playwright/test/cli.js" ]]; then
        echo "❌ 错误: Playwright CLI 不存在，请检查 npm install 输出"
        return 1
    fi
}

probe_talos_tcp_port() {
    local host="${1:-}"
    local port="${2:-}"
    if [[ -z "${host}" || -z "${port}" ]]; then
        return 1
    fi

    if command -v nc >/dev/null 2>&1; then
        nc -z "${host}" "${port}" >/dev/null 2>&1
        return $?
    fi

    if [[ -z "${TALOS_NODE_BIN:-}" ]] && ! TALOS_NODE_BIN="$(resolve_talos_node_bin)"; then
        return 1
    fi

    "${TALOS_NODE_BIN}" -e "const net=require('net'); const host=process.argv[1]; const port=Number(process.argv[2]); const socket=net.createConnection({host, port}); socket.setTimeout(1000); socket.on('connect', () => { socket.end(); process.exit(0); }); socket.on('timeout', () => { socket.destroy(); process.exit(1); }); socket.on('error', () => process.exit(1));" "${host}" "${port}" >/dev/null 2>&1
}

probe_talos_unity_ready() {
        local host="${1:-}"
        local port="${2:-}"
        local timeout_ms="${3:-1000}"
        if [[ -z "${host}" || -z "${port}" ]]; then
                return 1
        fi

        if [[ -z "${TALOS_NODE_BIN:-}" ]] && ! TALOS_NODE_BIN="$(resolve_talos_node_bin)"; then
                return 1
        fi

        "${TALOS_NODE_BIN}" -e '
const net = require("net");
const host = process.argv[1];
const port = Number(process.argv[2]);
const timeoutMs = Number(process.argv[3]);
const socket = net.createConnection({ host, port });
let buffer = Buffer.alloc(0);
let ready = false;

function fail() {
    socket.destroy();
}

socket.setTimeout(timeoutMs);
socket.on("connect", () => {
    const body = Buffer.from(JSON.stringify({ type: "hello", data: {} }), "utf8");
    const header = Buffer.alloc(4);
    header.writeUInt32BE(body.length, 0);
    socket.write(Buffer.concat([header, body]));
});
socket.on("data", chunk => {
    buffer = Buffer.concat([buffer, chunk]);
    while (buffer.length >= 4) {
        const length = buffer.readUInt32BE(0);
        if (buffer.length < 4 + length) {
            return;
        }
        const payload = buffer.subarray(4, 4 + length).toString("utf8");
        buffer = buffer.subarray(4 + length);
        try {
            const message = JSON.parse(payload);
            if (message && message.type === "hello_ack") {
                ready = true;
                socket.end();
                return;
            }
        } catch (error) {
            fail();
            return;
        }
    }
});
socket.on("timeout", fail);
socket.on("error", () => process.exit(1));
socket.on("close", () => process.exit(ready ? 0 : 1));
' "${host}" "${port}" "${timeout_ms}" >/dev/null 2>&1
}