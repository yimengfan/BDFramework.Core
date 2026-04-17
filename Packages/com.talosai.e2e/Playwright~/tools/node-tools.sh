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
# 可通过 TALOS_MUMU_WAIT_SECONDS 环境变量控制启动后等待时间（默认 45 秒，MuMu 12 NX 冷启动约需 40-60s）。
# The startup wait duration can be overridden via TALOS_MUMU_WAIT_SECONDS (default 45s; MuMu 12 NX cold start ~40-60s).
ensure_talos_mumu_running() {
    # 确保 MuMu Android 模拟器进程正在运行。
    # 优先顺序：① TALOS_MUMU_EXE_PATH 直接指定；② 静态候选路径（C: / D: / E: 盘）；③ where.exe 动态搜索。
    # 始终返回 0（最大努力策略），实际连通性由后续 adb connect / adb devices 验证。
    #
    # Ensure the MuMu Android emulator process is running.
    # Priority: ① direct override via TALOS_MUMU_EXE_PATH; ② static candidate list (C:/D:/E:);
    #           ③ dynamic discovery via where.exe /r.
    # Always returns 0 (best-effort); actual device availability is confirmed by later adb checks.
    #
    # 支持版本 / Supported versions:
    #   MuMu Player 2 (MuMu2): 进程 MuMuPlayer.exe，路径含 MuMuPlayer-12.0 或 MuMuPlayer
    #   MuMu 旧版 / MuMu X:    进程 NemuPlayer.exe / NemuVM.exe，路径含 nemu64 或 nemu
    #
    # 环境变量 / Environment variables:
    #   TALOS_MUMU_EXE_PATH      — 直接指定 MuMu exe 绝对路径（Git Bash 格式，如 /d/MuMuPlayer-12.0/...）
    #                              Specify exact MuMu exe absolute path (Git Bash format, e.g. /d/MuMuPlayer-12.0/...)
    #   TALOS_MUMU_WAIT_SECONDS  — 启动后等待秒数（默认 20）/ Startup wait seconds (default 20)

    # MuMu 各版本进程名，用于 tasklist.exe / ps 探测。
    # Process names covering MuMu 12 NX, MuMu2, MuMu X, and legacy MuMu.
    local -a mumu_process_names=(
        "MuMuManager.exe"   # MuMu 12 NX 主界面 / MuMu 12 NX GUI launcher (D:\Netease\MuMu\nx_main\)
        "MuMuNxMain.exe"    # MuMu 12 NX 主进程 / MuMu 12 NX main process
        "MuMuNxDevice.exe"  # MuMu 12 NX 设备进程 / MuMu 12 NX device process
        "MuMuPlayer.exe"    # MuMu Player 2 (MuMu2) 主进程 / main process
        "MuMuVMMSVC.exe"    # MuMu2 VM 服务进程 / VM service
        "NemuPlayer.exe"    # MuMu 旧版 / MuMu X 主进程 / legacy main
        "NemuVM.exe"        # MuMu 旧版 VM 进程 / legacy VM service
    )

    # MuMu 各版本常见安装目录，覆盖 C: / D: / E: 盘（MuMu 12 NX 优先）。
    # Candidate exe paths in version-priority order covering C:/D:/E: drives (MuMu 12 NX first).
    local -a mumu_exe_candidates=(
        # ---- D 盘 MuMu 12 NX 根目录（TC agent 实测路径，最高优先）----
        # D: drive MuMu 12 NX root-level install (confirmed on TC agent, highest priority)
        "/d/Netease/MuMu/nx_main/MuMuManager.exe"
        "/d/Netease/MuMu/nx_main/MuMuNxMain.exe"
        "/d/NetEase/MuMu/nx_main/MuMuManager.exe"
        "/d/NetEase/MuMu/nx_main/MuMuNxMain.exe"
        # ---- C 盘 MuMu 12 NX ----
        "/c/Netease/MuMu/nx_main/MuMuManager.exe"
        "/c/Netease/MuMu/nx_main/MuMuNxMain.exe"
        "/c/NetEase/MuMu/nx_main/MuMuManager.exe"
        "/c/NetEase/MuMu/nx_main/MuMuNxMain.exe"
        # ---- C 盘 MuMu2 / legacy ----
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
        # ---- D 盘 MuMu2 / legacy ----
        "/d/Netease/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/d/Netease/MuMuPlayer/shell/MuMuPlayer.exe"
        "/d/NetEase/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/d/NetEase/MuMuPlayer/shell/MuMuPlayer.exe"
        "/d/Program Files/Netease/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/d/Program Files/NetEase/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/d/Program Files/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/d/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/d/Program Files/Netease/MuMuPlayer/shell/MuMuPlayer.exe"
        "/d/Program Files/NetEase/MuMuPlayer/shell/MuMuPlayer.exe"
        "/d/Program Files/MuMuPlayer/shell/MuMuPlayer.exe"
        "/d/Program Files/NetEase/MuMu Player/emulator/nemu64/EmulatorShell/NemuPlayer.exe"
        "/d/Program Files (x86)/NetEase/MuMu Player/emulator/nemu64/EmulatorShell/NemuPlayer.exe"
        "/d/MuMu/emulator/nemu64/EmulatorShell/NemuPlayer.exe"
        "/d/MuMu/emulator/nemu/EmulatorShell/NemuPlayer.exe"
        # ---- E 盘 / E: drive ----
        "/e/Program Files/Netease/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/e/Program Files/NetEase/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/e/Program Files/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/e/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
        "/e/Program Files/Netease/MuMuPlayer/shell/MuMuPlayer.exe"
        "/e/Program Files/NetEase/MuMuPlayer/shell/MuMuPlayer.exe"
        "/e/MuMu/emulator/nemu64/EmulatorShell/NemuPlayer.exe"
        "/e/MuMu/emulator/nemu/EmulatorShell/NemuPlayer.exe"
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

    # 步骤 2：搜索 MuMu 可执行文件路径（优先级：env 覆盖 > 静态候选列表 > where.exe 动态搜索）。
    # Step 2: find MuMu exe path (priority: env override > static list > where.exe dynamic search).
    echo "    MuMu 未在运行，开始搜索可执行文件..."
    local exe_path=""

    # 2-a: TALOS_MUMU_EXE_PATH 环境变量直接覆盖（最高优先级）。
    # 2-a: honour TALOS_MUMU_EXE_PATH env override (highest priority).
    if [[ -n "${TALOS_MUMU_EXE_PATH:-}" ]]; then
        if [[ -f "${TALOS_MUMU_EXE_PATH}" ]]; then
            exe_path="${TALOS_MUMU_EXE_PATH}"
            echo "    已通过 TALOS_MUMU_EXE_PATH 指定路径: ${exe_path}"
        else
            echo "    ⚠️  TALOS_MUMU_EXE_PATH 指定的路径不存在: ${TALOS_MUMU_EXE_PATH}"
        fi
    fi

    # 2-b: 遍历静态候选路径（覆盖 C: / D: / E: 盘及 Windows 环境变量路径）。
    # 2-b: walk static candidate list (covers C:/D:/E: drives and Windows env-var paths).
    if [[ -z "${exe_path}" ]]; then
        # 通过 Windows 环境变量补充用户 Profile 安装路径（per-user 安装场景）。
        # Augment with user-profile-based install paths via Windows env vars (per-user install).
        local win_localappdata="${LOCALAPPDATA:-}"
        local win_userprofile="${USERPROFILE:-}"
        local win_programfiles="${PROGRAMFILES:-}"
        # 注：bash 无法引用含括号的变量名 PROGRAMFILES(X86)；x86 路径已在静态候选列表中覆盖。
        # Note: bash cannot reference PROGRAMFILES(X86) (parens in var names are illegal);
        #       x86 paths are already covered by the static candidate list.

        local bash_localappdata=""
        local bash_userprofile=""
        local bash_programfiles=""

        if command -v cygpath >/dev/null 2>&1; then
            [[ -n "${win_localappdata}" ]]     && bash_localappdata="$(cygpath -u "${win_localappdata}" 2>/dev/null || true)"
            [[ -n "${win_userprofile}" ]]       && bash_userprofile="$(cygpath -u "${win_userprofile}" 2>/dev/null || true)"
            [[ -n "${win_programfiles}" ]]      && bash_programfiles="$(cygpath -u "${win_programfiles}" 2>/dev/null || true)"
        else
            # 简单替换（无 cygpath 时）/ Simple drive-letter replacement when cygpath absent.
            local _conv
            for _conv in "win_localappdata:bash_localappdata" "win_userprofile:bash_userprofile" "win_programfiles:bash_programfiles"; do
                local _src="${!_conv%%:*}" _dst="${_conv##*:}"
                local _val="${!_src:-}"
                if [[ -n "${_val}" ]]; then
                    printf -v "${_dst}" '%s' "$(printf '%s' "${_val}" | sed 's|^[Cc]:|/c|; s|^[Dd]:|/d|; s|^[Ee]:|/e|; s|^[Ff]:|/f|; s|\\|/|g')"
                fi
            done
        fi

        echo "    LOCALAPPDATA -> ${bash_localappdata:-<未设置>}"
        echo "    USERPROFILE  -> ${bash_userprofile:-<未设置>}"
        echo "    PROGRAMFILES -> ${bash_programfiles:-<未设置>}"

        # 动态构建用户路径候选列表并追加到全局候选列表末尾。
        # Dynamically build per-user path candidates and append to the global candidate list.
        for _base in "${bash_localappdata}/Programs" "${bash_localappdata}" "${bash_userprofile}/AppData/Local/Programs" "${bash_userprofile}/AppData/Local" "${bash_userprofile}" "${bash_programfiles}"; do
            [[ -z "${_base}" ]] && continue
            mumu_exe_candidates+=(
                "${_base}/Netease/MuMu/nx_main/MuMuManager.exe"
                "${_base}/NetEase/MuMu/nx_main/MuMuManager.exe"
                "${_base}/Netease/MuMu/nx_main/MuMuNxMain.exe"
                "${_base}/Netease/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
                "${_base}/NetEase/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
                "${_base}/MuMuPlayer-12.0/shell/MuMuPlayer.exe"
                "${_base}/Netease/MuMuPlayer/shell/MuMuPlayer.exe"
                "${_base}/NetEase/MuMuPlayer/shell/MuMuPlayer.exe"
                "${_base}/MuMuPlayer/shell/MuMuPlayer.exe"
            )
        done

        local candidate=""
        for candidate in "${mumu_exe_candidates[@]}"; do
            if [[ -f "${candidate}" ]]; then
                exe_path="${candidate}"
                echo "    已在静态路径找到 MuMu 可执行文件: ${exe_path}"
                break
            fi
        done
    fi

    # 2-c: 动态搜索回退：枚举实际存在的盘符，逐盘 where.exe /r 递归查找。
    # 2-c: dynamic fallback: enumerate actual drives, then where.exe /r per drive.
    if [[ -z "${exe_path}" ]] && command -v where.exe >/dev/null 2>&1; then
        echo "    静态列表未命中，枚举实际盘符再动态搜索..."

        # 通过 wmic 获取所有本地盘符；回退到穷举 A-Z。
        # Enumerate drive letters via wmic; fall back to brute-force A-Z if wmic unavailable.
        local -a all_drives=()
        if command -v wmic.exe >/dev/null 2>&1; then
            local wmic_out
            wmic_out="$(wmic.exe logicaldisk get name 2>/dev/null | tr -d '\r' | grep -Eo '[A-Z]:' | tr '[:lower:]' '[:upper:]' || true)"
            while IFS= read -r drv; do
                [[ -n "${drv}" ]] && all_drives+=("${drv%:}")
            done <<< "${wmic_out}"
            echo "    所有盘符: ${all_drives[*]:-<wmic 无输出>}"
        fi
        if [[ ${#all_drives[@]} -eq 0 ]]; then
            # 回退：穷举常见盘符。
            # Fallback: try common drive letters.
            all_drives=(C D E F G)
        fi

        local drive=""
        for drive in "${all_drives[@]}"; do
            echo "    搜索 ${drive}: 盘..."
            local found=""
            # 按优先级搜索：MuMu 12 NX（MuMuManager）> MuMu2（MuMuPlayer）> 旧版（NemuPlayer）
            # Search in priority: MuMu 12 NX (MuMuManager) > MuMu2 (MuMuPlayer) > legacy (NemuPlayer)
            for _exe_name in "MuMuManager.exe" "MuMuNxMain.exe" "MuMuPlayer.exe" "NemuPlayer.exe"; do
                found="$(MSYS_NO_PATHCONV=1 where.exe /r "${drive}:\\" "${_exe_name}" 2>/dev/null | head -1 | tr -d '\r' || true)"
                if [[ -n "${found}" ]]; then
                    if command -v cygpath >/dev/null 2>&1; then
                        exe_path="$(cygpath -u "${found}" 2>/dev/null || printf '%s' "${found}")"
                    else
                        exe_path="$(printf '%s' "${found}" | sed 's|^[Cc]:|/c|; s|^[Dd]:|/d|; s|^[Ee]:|/e|; s|\\|/|g')"
                    fi
                    echo "    where.exe 找到 MuMu (${_exe_name}): ${found} -> ${exe_path}"
                    break 2
                fi
            done
        done
    fi

    # 2-d: Windows 注册表查询——MuMu 安装一般会在 Uninstall 键下记录安装路径。
    # 2-d: Windows Registry fallback — MuMu install usually records path under Uninstall key.
    if [[ -z "${exe_path}" ]] && command -v reg.exe >/dev/null 2>&1; then
        echo "    盘符搜索未命中，尝试注册表查询 MuMu 安装路径..."
        local reg_keys=(
            "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            "HKLM\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
        )
        local reg_key
        for reg_key in "${reg_keys[@]}"; do
            local reg_out=""
            reg_out="$(reg.exe query "${reg_key}" /s /f "MuMu" 2>/dev/null | grep -i "InstallLocation\|InstallPath\|UninstallString" | head -5 || true)"
            if [[ -n "${reg_out}" ]]; then
                echo "    注册表命中: ${reg_out}"
                # 从注册表输出提取路径，尝试拼接 exe。
                # Extract path from registry output and try to locate exe.
                local reg_path
                reg_path="$(printf '%s\n' "${reg_out}" | sed 's/.*REG_SZ\s*//' | head -1 | tr -d '\r')"
                if [[ -n "${reg_path}" ]]; then
                    local bash_path
                    if command -v cygpath >/dev/null 2>&1; then
                        bash_path="$(cygpath -u "${reg_path}" 2>/dev/null || printf '%s' "${reg_path}")"
                    else
                        # 简单替换：将 Windows 路径转换为 Git Bash 格式。
                        # Simple conversion from Windows path to Git Bash format.
                        bash_path="$(printf '%s' "${reg_path}" | sed 's|^[Cc]:|/c|; s|^[Dd]:|/d|; s|^[Ee]:|/e|; s|^[Ff]:|/f|; s|\\|/|g')"
                    fi
                    # 尝试注册表路径下的 exe 拼装。
                    # Try exe locations relative to the registry install path.
                    local try_paths=(
                        "${bash_path}/shell/MuMuPlayer.exe"
                        "${bash_path}/MuMuPlayer.exe"
                        "${bash_path}/EmulatorShell/NemuPlayer.exe"
                        "${bash_path}/NemuPlayer.exe"
                    )
                    local tp
                    for tp in "${try_paths[@]}"; do
                        if [[ -f "${tp}" ]]; then
                            exe_path="${tp}"
                            echo "    注册表路径命中 exe: ${exe_path}"
                            break 2
                        fi
                    done
                fi
            fi
        done
    fi

    if [[ -z "${exe_path}" ]]; then
        echo "    ⚠️  所有搜索策略均未找到 MuMu 可执行文件，跳过自动启动"
        echo "    提示：可通过 TALOS_MUMU_EXE_PATH 环境变量直接指定路径"
        echo "    (若已通过其他方式启动，后续 ADB connect 步骤仍会尝试连接)"
        # 诊断打印：打印各盘根目录清单和注册表 MuMu 条目，便于人工判断安装路径。
        # Diagnostic: print root dir listings and registry MuMu entries to help locate install path.
        if command -v reg.exe >/dev/null 2>&1; then
            echo "    === 诊断：注册表搜索含 MuMu/Nemu 的已安装程序 ==="
            reg.exe query "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall" /s /f "MuMu" 2>/dev/null | grep -i "DisplayName\|InstallLocation" | head -20 || true
            reg.exe query "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall" /s /f "MuMu" 2>/dev/null | grep -i "DisplayName\|InstallLocation" | head -20 || true
            reg.exe query "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall" /s /f "NetEase" 2>/dev/null | grep -i "DisplayName\|InstallLocation" | head -20 || true
            echo "    === 诊断：注册表搜索结束 ==="
        fi
        # 列出 C: / D: 盘根目录，帮助判断 MuMu 是否以非标准路径安装（如便携版）。
        # List C:/D: root directories to help detect non-standard (portable) MuMu installs.
        # 注：在 Git Bash 中 /c/ 映射到 C:\，用 ls 而非 cmd.exe dir（避免路径转换问题）。
        # Note: in Git Bash /c/ maps to C:\; use ls rather than cmd.exe dir to avoid POSIX path conversion.
        echo "    === 诊断：C:\\ 根目录内容 ==="
        ls /c/ 2>/dev/null || cmd.exe /c "dir /b C:\\" 2>/dev/null | tr -d '\r' || echo "    <无法列出 C: 内容>"
        echo "    === 诊断：D:\\ 根目录内容 ==="
        ls /d/ 2>/dev/null || cmd.exe /c "dir /b D:\\" 2>/dev/null | tr -d '\r' || echo "    <无法列出 D: 内容>"
        # 若 D:\Netease 目录存在则再列一层，辅助定位具体子目录名。
        # If D:\Netease exists, list one level deeper to pinpoint the exact subdirectory name.
        if [[ -d /d/Netease ]]; then
            echo "    === 诊断：D:\\Netease\\ 内容 ==="
            ls /d/Netease/ 2>/dev/null || true
            for _sub in /d/Netease/*/; do
                [[ -d "$_sub" ]] && { echo "      子目录: $_sub"; ls "$_sub" 2>/dev/null | head -10 || true; }
            done
            # 递归查找任意 .exe 找到 MuMu 的启动程序 / Find all .exe files to identify MuMu launcher name
            echo "    === 诊断：D:/Netease 内所有 .exe 文件 ==="
            find /d/Netease/ -name "*.exe" 2>/dev/null | grep -v "~" | head -30 || true
            # 同时列 nx_main 目录（MuMu 12 常见启动目录）/ Also list nx_main dir (common MuMu 12 launcher dir)
            if [[ -d /d/Netease/MuMu/nx_main ]]; then
                echo "    === 诊断：D:/Netease/MuMu/nx_main 内容 ==="
                ls /d/Netease/MuMu/nx_main/ 2>/dev/null | head -20 || true
            fi
        fi
        echo "    === 诊断：结束 ==="
        return 0
    fi

    # 步骤 3：后台启动 MuMu，并等待虚拟机初始化完成。
    # Step 3: launch MuMu in background and wait for VM initialization.
    #
    # MuMu 12 NX 优先使用 mumu-cli.exe 无头启动（适合 CI 无人值守场景）；
    # 若 mumu-cli.exe 不存在则回退到 MuMuManager.exe GUI 启动。
    # MuMu 12 NX: prefer mumu-cli.exe for headless launch (CI-friendly);
    # fall back to MuMuManager.exe GUI if mumu-cli.exe is absent.
    echo "    正在后台启动 MuMu: ${exe_path}"
    if command -v cmd.exe >/dev/null 2>&1; then
        # Windows Git Bash 环境：通过 cmd.exe 启动 exe。
        # Windows Git Bash: launch exe via cmd.exe.
        local win_path=""
        if command -v cygpath >/dev/null 2>&1; then
            win_path="$(cygpath -w "${exe_path}" 2>/dev/null || printf '%s' "${exe_path}")"
        else
            win_path="${exe_path}"
        fi

        # 尝试用 mumu-cli.exe 无头启动实例 0（MuMu 12 NX CI 标准方式）。
        # Try mumu-cli.exe to headlessly start instance 0 (standard CI method for MuMu 12 NX).
        local cli_path="${exe_path%/*}/mumu-cli.exe"

        # 先诊断：打印启动前 MuMuNx* 进程状态，帮助确认 SYSTEM 账号是否能与服务交互。
        # Diagnostic: show MuMuNx* processes before launch to confirm SYSTEM can interact with service.
        echo "    === 启动前 MuMuNx* 进程状态 ==="
        tasklist.exe /FI "IMAGENAME eq MuMu*" 2>/dev/null | tr -d '\r' | grep -v "^$" || echo "    (无 MuMuNx 进程)"

        if [[ -f "${cli_path}" ]]; then
            echo "    使用 mumu-cli.exe 无头启动实例 0 (headless CI mode)"
            # 注：在 Git Bash(MINGW) 中直接执行 Windows .exe 无需通过 cmd.exe；
            # 避免 cmd.exe 路径转换问题，输出可正常捕获到 bash stdout。
            # Note: in Git Bash (MINGW) run Windows .exe directly to avoid cmd.exe path conversion issues.
            # mumu-cli.exe 正确用法（来自 control --help）：
            #   control --vmindex <index> launch    # 启动指定实例
            #   control --vmindex <index> shutdown  # 关闭指定实例
            # mumu-cli.exe correct usage (from control --help):
            #   control --vmindex <index> launch    # start specified instance
            echo "    === mumu-cli control --vmindex 0 launch ==="
            MSYS_NO_PATHCONV=1 "${cli_path}" control --vmindex 0 launch 2>&1 | tr -d '\r' || true
            echo "    === mumu-cli control launch 完成 ==="
        else
            echo "    mumu-cli.exe 不存在，回退到 MuMuManager.exe GUI 直接启动"
            MSYS_NO_PATHCONV=1 "${exe_path}" 2>/dev/null &
            disown $! 2>/dev/null || true
        fi

        # 等待 15s 后打印进程状态，确认 MuMu 已启动。
        # Wait 15s then print process state to confirm MuMu started.
        sleep 10
        echo "    === 启动后 MuMuNx* 进程状态 ==="
        tasklist.exe /FI "IMAGENAME eq MuMu*" 2>/dev/null | tr -d '\r' | grep -v "^$" || echo "    (无 MuMuNx 进程)"
    else
        # 非 Windows 本地调测回退：无法真正启动 Windows .exe，仅打印告知。
        # Non-Windows local debug fallback: cannot actually run a .exe; log only.
        echo "    (非 Windows 环境，跳过 exe 启动)"
    fi

    # MuMu 12 NX 冷启动需要约 40-60s：先等待 UI 出现，再等待 ADB 设备就绪。
    # MuMu 12 NX cold start takes ~40-60s: wait for UI, then wait for ADB device ready.
    local wait_secs="${TALOS_MUMU_WAIT_SECONDS:-45}"
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