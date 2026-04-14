#!/usr/bin/env bash
# ==========================================================================
# Talos E2E Playwright Node 工具解析公共函数。
#
# 目标：
#   1. 在本机 PATH 正常时直接复用 node/npm。
#   2. 在 Windows TeamCity service 缺少 PATH 时，回退到标准 Node 安装目录。
#   3. 允许通过 NODE_BIN / NPM_BIN / TALOS_NODEJS_HOME 显式覆盖，方便 CI 与测试注入。
# ==========================================================================

resolve_talos_tool_candidate() {
    local candidate="${1:-}"
    if [[ -z "${candidate}" ]]; then
        return 1
    fi

    if [[ "${candidate}" == */* || "${candidate}" == *\\* ]]; then
        if [[ -f "${candidate}" || -x "${candidate}" ]]; then
            printf '%s\n' "${candidate}"
            return 0
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