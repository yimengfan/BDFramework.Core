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

    printf '%s\n' "${homes[@]}"
}

collect_talos_explicit_node_homes() {
    local homes=()
    if [[ -n "${TALOS_NODEJS_HOME:-}" ]]; then
        homes+=("${TALOS_NODEJS_HOME}")
    fi
    if [[ -n "${NODEJS_HOME:-}" ]] && [[ "${NODEJS_HOME}" != "${TALOS_NODEJS_HOME:-}" ]]; then
        homes+=("${NODEJS_HOME}")
    fi

    printf '%s\n' "${homes[@]}"
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