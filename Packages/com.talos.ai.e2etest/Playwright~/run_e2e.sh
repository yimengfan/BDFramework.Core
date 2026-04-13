#!/usr/bin/env bash
# ============================================================================
# Talos E2E 测试 CI 编排脚本。
# 
# 完整流程：
#   1. 构建母包（含 DEBUG 标记和 E2E 测试热更 DLL）
#   2. 安装母包到目标设备
#   3. 启动母包
#   4. 等待应用就绪
#   5. 运行 Playwright E2E 测试
#   6. 收集测试报告
#   7. 清理（卸载应用）
#
# 使用方式：
#   ./run_e2e.sh --platform android --package /path/to/app.apk
#   ./run_e2e.sh --platform windows --package /path/to/app.exe
#
# 环境变量：
#   PLATFORM     - 目标平台: android / windows / macos
#   PACKAGE_PATH - 安装包路径
#   UNITY_HOST   - Unity Player IP 地址（非 Android 平台）
#   UNITY_PORT   - Unity TCP 端口，默认 10002
#   ADB_SERIAL   - ADB 设备序列号（Android 多设备时使用）
# ============================================================================

set -euo pipefail

# ======== 默认参数 ========
PLATFORM="${PLATFORM:-android}"
PACKAGE_PATH="${PACKAGE_PATH:-}"
UNITY_HOST="${UNITY_HOST:-127.0.0.1}"
UNITY_PORT="${UNITY_PORT:-10002}"
ADB_SERIAL="${ADB_SERIAL:-}"
LOCAL_PORT="${LOCAL_PORT:-10002}"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLAYWRIGHT_DIR="${PROJECT_DIR}/playwright"
REPORT_DIR="${PROJECT_DIR}/test-results"

# ======== 参数解析 ========
while [[ $# -gt 0 ]]; do
    case $1 in
        --platform)  PLATFORM="$2";   shift 2 ;;
        --package)   PACKAGE_PATH="$2"; shift 2 ;;
        --host)      UNITY_HOST="$2"; shift 2 ;;
        --port)      UNITY_PORT="$2"; shift 2 ;;
        --adb-serial) ADB_SERIAL="$2"; shift 2 ;;
        --help)
            echo "用法: $0 --platform <android|windows|macos> --package <path>"
            echo ""
            echo "选项:"
            echo "  --platform    目标平台 (android/windows/macos)"
            echo "  --package     安装包路径"
            echo "  --host        Unity Player IP 地址 (非 Android)"
            echo "  --port        Unity TCP 端口 (默认 10002)"
            echo "  --adb-serial  ADB 设备序列号"
            exit 0
            ;;
        *) echo "未知参数: $1"; exit 1 ;;
    esac
done

echo "============================================"
echo "  Talos E2E 测试 CI 编排"
echo "============================================"
echo "  平台:    ${PLATFORM}"
echo "  包路径:  ${PACKAGE_PATH}"
echo "  端口:    ${UNITY_PORT}"
echo "============================================"

# ======== 前置检查 ========
if [[ -z "${PACKAGE_PATH}" ]]; then
    echo "错误: 未指定安装包路径 (--package)"
    exit 1
fi

if [[ ! -f "${PACKAGE_PATH}" && "${PLATFORM}" != "windows" && "${PLATFORM}" != "macos" ]]; then
    echo "错误: 安装包不存在: ${PACKAGE_PATH}"
    exit 1
fi

# ======== 安装 Playwright 依赖 ========
echo ""
echo ">>> 安装 Playwright 依赖..."
cd "${PLAYWRIGHT_DIR}"
if [[ ! -d "node_modules" ]]; then
    npm install
fi

# ======== 安装应用 ========
echo ""
echo ">>> 安装应用到设备..."
case "${PLATFORM}" in
    android)
        if [[ -n "${ADB_SERIAL}" ]]; then
            adb -s "${ADB_SERIAL}" install -r -t "${PACKAGE_PATH}"
        else
            adb install -r -t "${PACKAGE_PATH}"
        fi
        ;;
    windows|macos)
        echo "桌面平台跳过安装步骤，直接使用: ${PACKAGE_PATH}"
        ;;
    *)
        echo "错误: 不支持的平台: ${PLATFORM}"
        exit 1
        ;;
esac

# ======== 启动应用 ========
echo ""
echo ">>> 启动应用..."
case "${PLATFORM}" in
    android)
        # 先强制停止旧实例
        if [[ -n "${ADB_SERIAL}" ]]; then
            adb -s "${ADB_SERIAL}" shell am force-stop com.popo.bdframework || true
        else
            adb shell am force-stop com.popo.bdframework || true
        fi
        # 启动 Activity
        if [[ -n "${ADB_SERIAL}" ]]; then
            adb -s "${ADB_SERIAL}" shell am start -n com.popo.bdframework/com.unity3d.player.UnityPlayerActivity
        else
            adb shell am start -n com.popo.bdframework/com.unity3d.player.UnityPlayerActivity
        fi
        ;;
    windows)
        # 启动 Windows 可执行文件
        "${PACKAGE_PATH}" &
        ;;
    macos)
        # 启动 macOS .app
        open "${PACKAGE_PATH}"
        ;;
esac

# ======== 端口转发（Android） ========
if [[ "${PLATFORM}" == "android" ]]; then
    echo ""
    echo ">>> 设置 ADB 端口转发..."
    if [[ -n "${ADB_SERIAL}" ]]; then
        adb -s "${ADB_SERIAL}" forward "tcp:${LOCAL_PORT}" "tcp:${UNITY_PORT}"
    else
        adb forward "tcp:${LOCAL_PORT}" "tcp:${UNITY_PORT}"
    fi
fi

# ======== 运行 Playwright 测试 ========
echo ""
echo ">>> 运行 E2E 测试..."
echo ""

mkdir -p "${REPORT_DIR}"

PLATFORM="${PLATFORM}" \
UNITY_HOST="${UNITY_HOST}" \
UNITY_PORT="${UNITY_PORT}" \
LOCAL_PORT="${LOCAL_PORT}" \
ADB_SERIAL="${ADB_SERIAL}" \
npx playwright test --project="${PLATFORM}" \
    --output="${REPORT_DIR}/artifacts" \
    --reporter=list,html="${REPORT_DIR}/html",junit="${REPORT_DIR}/junit.xml" \
    2>&1 | tee "${REPORT_DIR}/test-output.log"

TEST_EXIT_CODE=${PIPESTATUS[0]}

# ======== 清理 ========
echo ""
echo ">>> 清理..."

case "${PLATFORM}" in
    android)
        if [[ -n "${ADB_SERIAL}" ]]; then
            adb -s "${ADB_SERIAL}" shell am force-stop com.popo.bdframework || true
            adb -s "${ADB_SERIAL}" forward --remove "tcp:${LOCAL_PORT}" || true
        else
            adb shell am force-stop com.popo.bdframework || true
            adb forward --remove "tcp:${LOCAL_PORT}" || true
        fi
        ;;
    windows|macos)
        echo "桌面平台请手动关闭应用"
        ;;
esac

# ======== 结果汇总 ========
echo ""
echo "============================================"
if [[ ${TEST_EXIT_CODE} -eq 0 ]]; then
    echo "  ✅ E2E 测试全部通过"
else
    echo "  ❌ E2E 测试失败 (exit code: ${TEST_EXIT_CODE})"
fi
echo "============================================"
echo "  测试报告: ${REPORT_DIR}/html/index.html"
echo "  JUnit XML: ${REPORT_DIR}/junit.xml"
echo "============================================"

exit ${TEST_EXIT_CODE}
