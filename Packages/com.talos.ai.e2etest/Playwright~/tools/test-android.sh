#!/usr/bin/env bash
# ============================================================================
# Talos E2E 测试——Android 模式启动脚本。
#
# 流程：
#   1. 检查 APK 路径和 ADB 可用性
#   2. 安装 APK 到设备
#   3. 设置 ADB 端口转发
#   4. 启动应用
#   5. 启动 Playwright 测试，通过 TCP 连接到设备
#   6. 测试完成后清理
#
# 使用方式：
#   ./tools/test-android.sh --apk /path/to/app.apk
#
# 环境变量：
#   APK_PATH   — APK 文件路径（或通过 --apk 参数）
#   ADB_SERIAL — ADB 设备序列号（多设备时使用）
#   UNITY_PORT — TCP 端口，默认 10002
#   PACKAGE    — Android 包名，默认 com.popo.bdframework
#   ACTIVITY   — 启动 Activity，默认 com.unity3d.player.UnityPlayerActivity
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLAYWRIGHT_DIR="${SCRIPT_DIR}"

# ======== 默认参数 ========
APK_PATH="${APK_PATH:-}"
ADB_SERIAL="${ADB_SERIAL:-}"
UNITY_PORT="${UNITY_PORT:-10002}"
PACKAGE="${PACKAGE:-com.popo.bdframework}"
ACTIVITY="${ACTIVITY:-com.popo.bdframework/com.unity3d.player.UnityPlayerActivity}"

# ======== 参数解析 ========
while [[ $# -gt 0 ]]; do
    case $1 in
        --apk)       APK_PATH="$2";    shift 2 ;;
        --serial)    ADB_SERIAL="$2";  shift 2 ;;
        --port)      UNITY_PORT="$2";  shift 2 ;;
        --package)   PACKAGE="$2";     shift 2 ;;
        --help)
            echo "用法: $0 --apk <path/to/app.apk>"
            echo ""
            echo "选项:"
            echo "  --apk       APK 文件路径"
            echo "  --serial    ADB 设备序列号"
            echo "  --port      TCP 端口 (默认 10002)"
            echo "  --package   Android 包名 (默认 com.popo.bdframework)"
            exit 0
            ;;
        *) echo "未知参数: $1"; exit 1 ;;
    esac
done

# ADB 命令前缀
ADB="adb"
if [[ -n "${ADB_SERIAL}" ]]; then
    ADB="adb -s ${ADB_SERIAL}"
fi

echo "============================================"
echo "  Talos E2E — Android 模式测试"
echo "============================================"
echo "  APK:      ${APK_PATH}"
echo "  包名:     ${PACKAGE}"
echo "  端口:     ${UNITY_PORT}"
echo "============================================"

# ======== 前置检查 ========
if [[ -z "${APK_PATH}" ]]; then
    echo "❌ 错误: 未指定 APK 路径"
    echo "使用: $0 --apk /path/to/app.apk"
    exit 1
fi

if [[ ! -f "${APK_PATH}" ]]; then
    echo "❌ 错误: APK 文件不存在: ${APK_PATH}"
    exit 1
fi

if ! command -v adb &>/dev/null; then
    echo "❌ 错误: adb 命令未找到，请安装 Android SDK Platform Tools"
    exit 1
fi

# 检查设备连接
echo ""
echo ">>> 检查设备连接..."
DEVICE_COUNT=$(${ADB} devices | grep -c "device$" || true)
if [[ ${DEVICE_COUNT} -eq 0 ]]; then
    echo "❌ 错误: 未检测到已连接的 Android 设备"
    ${ADB} devices
    exit 1
fi
echo "    ✅ 设备已连接 ($(${ADB} devices | grep "device$" | head -1))"

# ======== 安装 Playwright 依赖 ========
echo ""
echo ">>> 检查 Playwright 依赖..."
cd "${PLAYWRIGHT_DIR}"
if [[ ! -d "node_modules" ]]; then
    npm install
fi

# ======== 安装 APK ========
echo ""
echo ">>> 安装 APK..."
${ADB} install -r -t "${APK_PATH}" || {
    echo "❌ APK 安装失败"
    exit 1
}
echo "    ✅ APK 安装完成"

# ======== 停止旧实例 + 设置端口转发 ========
echo ""
echo ">>> 准备端口转发..."
${ADB} shell am force-stop "${PACKAGE}" 2>/dev/null || true
${ADB} forward --remove "tcp:${UNITY_PORT}" 2>/dev/null || true
${ADB} forward "tcp:${UNITY_PORT}" "tcp:${UNITY_PORT}"
echo "    ✅ 端口转发: localhost:${UNITY_PORT} -> device:${UNITY_PORT}"

# ======== 启动应用 ========
echo ""
echo ">>> 启动应用..."
${ADB} shell am start -n "${ACTIVITY}"
echo "    ✅ 应用已启动"

# ======== 等待 TCP 服务就绪 ========
echo ""
echo ">>> 等待 Unity E2E TCP 服务就绪..."
MAX_WAIT=60
WAITED=0

while [[ ${WAITED} -lt ${MAX_WAIT} ]]; do
    if command -v nc &>/dev/null; then
        if nc -z 127.0.0.1 "${UNITY_PORT}" 2>/dev/null; then
            echo "    ✅ TCP 服务已就绪 (${WAITED}s)"
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
    ${ADB} shell am force-stop "${PACKAGE}" 2>/dev/null || true
    exit 1
fi

# ======== 运行 Playwright 测试 ========
echo ""
echo ">>> 运行 Playwright 测试..."
echo ""

PLATFORM=android \
UNITY_HOST=127.0.0.1 \
UNITY_PORT="${UNITY_PORT}" \
npx playwright test --project=android \
    --output="${PLAYWRIGHT_DIR}/test-results/artifacts" \
    --reporter=list,html="${PLAYWRIGHT_DIR}/test-results/html",junit="${PLAYWRIGHT_DIR}/test-results/junit.xml" \
    2>&1 | tee "${PLAYWRIGHT_DIR}/test-results/test-output.log"

TEST_EXIT_CODE=${PIPESTATUS[0]}

# ======== 清理 ========
echo ""
echo ">>> 清理..."
${ADB} shell am force-stop "${PACKAGE}" 2>/dev/null || true
${ADB} forward --remove "tcp:${UNITY_PORT}" 2>/dev/null || true

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
