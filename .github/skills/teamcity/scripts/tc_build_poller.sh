#!/usr/bin/env bash
# =============================================================================
# TeamCity Build Poller — Shell 轮询工具
# TeamCity Build Poller — Shell Polling Tool
# =============================================================================
# 
# 【用途 / Purpose】
# 人工诊断或特殊脚本集成时，按 build_id 等待 TeamCity 构建状态直到完成。
# Agent 默认不得使用本脚本等待自己刚触发的构建；默认入口是
# update_project_settings.py run-build --wait / run-build-group --wait。
#
# For manual diagnostics or special script integration, wait for an existing
# TeamCity build by build_id until completion. Agents must not use this script
# as the default wait path for builds they just triggered; use
# update_project_settings.py run-build --wait / run-build-group --wait instead.
#
# 【使用场景 / Use Cases】
# - 人工诊断一个已经存在的 build ID
# - CI/CD 脚本集成需要独立监控构建状态
# - 调试构建问题时需要持续跟踪进度，且没有并行运行的 run-build --wait helper
#
# 【依赖 / Dependencies】
# - curl: HTTP 请求
# - jq: JSON 解析（可选，如果没有则使用 grep/sed）
# - TeamCity API token: 通过环境变量 TEAMCITY_TOKEN 提供
#
# 【环境变量 / Environment Variables】
# - TEAMCITY_BASE_URL: TeamCity 服务器地址（如 http://192.168.0.240:20000）
# - TEAMCITY_TOKEN: TeamCity API token
#
# 【参数 / Parameters】
# $1: build_id — TeamCity 构建 ID（必需）
# $2: poll_interval — 轮询间隔秒数（可选，默认 30）
# $3: timeout — 超时秒数（可选，默认 7200 = 2小时）
#
# 【示例 / Examples】
# 
# 基本用法：
#   ./tc_build_poller.sh 1075
#
# 自定义轮询间隔和超时：
#   ./tc_build_poller.sh 1075 60 3600
#
# 从环境变量加载配置：
#   source .test-DevOps/.teamcity/.env
#   ./tc_build_poller.sh 1075
#
# 【输出 / Output】
# - 每次轮询打印构建状态摘要（build_id, state, status, progress）
# - 构建完成时打印最终状态和退出码
# - 失败时打印日志尾部（最后 80 行）
#
# 【退出码 / Exit Codes】
# - 0: 构建成功完成
# - 1: 构建失败
# - 2: 超时
# - 3: 参数错误或环境配置错误
# =============================================================================

set -euo pipefail

# =============================================================================
# 参数校验
# Parameter Validation
# =============================================================================

BUILD_ID="${1:-}"
POLL_INTERVAL="${2:-30}"
TIMEOUT="${3:-7200}"

if [[ -z "$BUILD_ID" ]]; then
    echo "[tc_build_poller] ERROR: build_id is required"
    echo "[tc_build_poller] Usage: $0 <build_id> [poll_interval] [timeout]"
    exit 3
fi

if [[ -z "${TEAMCITY_BASE_URL:-}" ]]; then
    echo "[tc_build_poller] ERROR: TEAMCITY_BASE_URL is not set"
    echo "[tc_build_poller] Please set TEAMCITY_BASE_URL environment variable"
    exit 3
fi

if [[ -z "${TEAMCITY_TOKEN:-}" ]]; then
    echo "[tc_build_poller] ERROR: TEAMCITY_TOKEN is not set"
    echo "[tc_build_poller] Please set TEAMCITY_TOKEN environment variable"
    exit 3
fi

# =============================================================================
# 工具函数
# Utility Functions
# =============================================================================

# 检查 jq 是否可用
# Check if jq is available
has_jq() {
    command -v jq &> /dev/null
}

# 获取构建状态
# Get build status from TeamCity API
get_build_status() {
    local build_id="$1"
    local url="${TEAMCITY_BASE_URL}/app/rest/builds/id:${build_id}"
    
    curl -s -S \
        -H "Authorization: Bearer ${TEAMCITY_TOKEN}" \
        -H "Accept: application/json" \
        "${url}"
}

# 解析构建状态（使用 jq 或 grep/sed）
# Parse build status (using jq or grep/sed)
parse_build_state() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.state // "unknown"'
    else
        echo "$json" | grep -oP '"state"\s*:\s*"\K[^"]+' || echo "unknown"
    fi
}

parse_build_status() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.status // "unknown"'
    else
        echo "$json" | grep -oP '"status"\s*:\s*"\K[^"]+' || echo "unknown"
    fi
}

parse_build_progress() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.["running-info"].percentage // 0' 2>/dev/null || echo "0"
    else
        echo "$json" | grep -oP '"percentage"\s*:\s*\K[0-9]+' || echo "0"
    fi
}

parse_build_number() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.number // "unknown"'
    else
        echo "$json" | grep -oP '"number"\s*:\s*"\K[^"]+' || echo "unknown"
    fi
}

parse_build_web_url() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.webUrl // ""'
    else
        echo "$json" | grep -oP '"webUrl"\s*:\s*"\K[^"]+' || echo ""
    fi
}

parse_build_type_name() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.buildType.name // "unknown"'
    else
        echo "$json" | grep -oP '"buildType"[^}]*"name"\s*:\s*"\K[^"]+' || echo "unknown"
    fi
}

parse_agent_name() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.agent.name // "queued"'
    else
        echo "$json" | grep -oP '"agent"[^}]*"name"\s*:\s*"\K[^"]+' || echo "queued"
    fi
}

parse_status_text() {
    local json="$1"
    if has_jq; then
        echo "$json" | jq -r '.statusText // ""'
    else
        echo "$json" | grep -oP '"statusText"\s*:\s*"\K[^"]+' || echo ""
    fi
}

# 获取构建日志尾部
# Get build log tail
get_build_log_tail() {
    local build_id="$1"
    local lines="${2:-80}"
    local url="${TEAMCITY_BASE_URL}/app/rest/builds/id:${build_id}/log"
    
    echo "[tc_build_poller] Fetching last ${lines} lines of build log..."
    curl -s -S \
        -H "Authorization: Bearer ${TEAMCITY_TOKEN}" \
        "${url}" | tail -n "$lines"
}

# =============================================================================
# 主轮询循环
# Main Polling Loop
# =============================================================================

echo "[tc_build_poller] ========================================"
echo "[tc_build_poller] TeamCity Build Poller Started"
echo "[tc_build_poller] ========================================"
echo "[tc_build_poller] Build ID:        ${BUILD_ID}"
echo "[tc_build_poller] Poll Interval:   ${POLL_INTERVAL}s"
echo "[tc_build_poller] Timeout:         ${TIMEOUT}s"
echo "[tc_build_poller] TeamCity URL:    ${TEAMCITY_BASE_URL}"
echo "[tc_build_poller] ========================================"

START_TIME=$(date +%s)
ELAPSED=0
LAST_PROGRESS=0

while true; do
    # 检查超时
    # Check timeout
    CURRENT_TIME=$(date +%s)
    ELAPSED=$((CURRENT_TIME - START_TIME))
    
    if [[ $ELAPSED -ge $TIMEOUT ]]; then
        echo "[tc_build_poller] ========================================"
        echo "[tc_build_poller] TIMEOUT: Build did not complete within ${TIMEOUT}s"
        echo "[tc_build_poller] Elapsed: ${ELAPSED}s"
        echo "[tc_build_poller] ========================================"
        exit 2
    fi
    
    # 获取构建状态
    # Get build status
    BUILD_JSON=$(get_build_status "$BUILD_ID") || {
        echo "[tc_build_poller] WARNING: Failed to fetch build status, retrying..."
        sleep "$POLL_INTERVAL"
        continue
    }
    
    # 解析状态
    # Parse status
    STATE=$(parse_build_state "$BUILD_JSON")
    STATUS=$(parse_build_status "$BUILD_JSON")
    PROGRESS=$(parse_build_progress "$BUILD_JSON")
    NUMBER=$(parse_build_number "$BUILD_JSON")
    WEB_URL=$(parse_build_web_url "$BUILD_JSON")
    BUILD_TYPE_NAME=$(parse_build_type_name "$BUILD_JSON")
    AGENT_NAME=$(parse_agent_name "$BUILD_JSON")
    STATUS_TEXT=$(parse_status_text "$BUILD_JSON")
    
    # 打印状态摘要（仅当进度变化或每 5 分钟）
    # Print status summary (only when progress changes or every 5 minutes)
    MINUTES=$((ELAPSED / 60))
    if [[ "$PROGRESS" != "$LAST_PROGRESS" ]] || [[ $((ELAPSED % 300)) -lt $POLL_INTERVAL ]]; then
        if [[ "$STATE" == "queued" ]]; then
            echo "[tc_build_poller] [${ELAPSED}s] build#${NUMBER} state=${STATE} (waiting for agent)"
        elif [[ "$STATE" == "running" ]]; then
            echo "[tc_build_poller] [${ELAPSED}s] build#${NUMBER} state=${STATE} status=${STATUS} progress=${PROGRESS}% agent=${AGENT_NAME}"
            if [[ -n "$STATUS_TEXT" ]]; then
                echo "[tc_build_poller]          └─ ${STATUS_TEXT}"
            fi
        else
            echo "[tc_build_poller] [${ELAPSED}s] build#${NUMBER} state=${STATE} status=${STATUS} progress=${PROGRESS}%"
        fi
        LAST_PROGRESS="$PROGRESS"
    fi
    
    # 检查是否完成
    # Check if finished
    if [[ "$STATE" == "finished" ]]; then
        echo "[tc_build_poller] ========================================"
        echo "[tc_build_poller] BUILD FINISHED"
        echo "[tc_build_poller] ========================================"
        echo "[tc_build_poller] Build ID:      ${BUILD_ID}"
        echo "[tc_build_poller] Build Number:  ${NUMBER}"
        echo "[tc_build_poller] Build Type:    ${BUILD_TYPE_NAME}"
        echo "[tc_build_poller] Agent:         ${AGENT_NAME}"
        echo "[tc_build_poller] State:         ${STATE}"
        echo "[tc_build_poller] Status:        ${STATUS}"
        echo "[tc_build_poller] Status Text:   ${STATUS_TEXT}"
        echo "[tc_build_poller] Elapsed:       ${ELAPSED}s"
        echo "[tc_build_poller] Web URL:       ${WEB_URL}"
        echo "[tc_build_poller] ========================================"
        
        if [[ "$STATUS" == "SUCCESS" ]]; then
            echo "[tc_build_poller] ✅ Build succeeded!"
            exit 0
        else
            echo "[tc_build_poller] ❌ Build failed!"
            echo "[tc_build_poller] ========================================"
            get_build_log_tail "$BUILD_ID" 80
            exit 1
        fi
    fi
    
    # 等待下一次轮询
    # Wait for next poll
    sleep "$POLL_INTERVAL"
done
