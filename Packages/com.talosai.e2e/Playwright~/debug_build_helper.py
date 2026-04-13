#!/usr/bin/env python3
"""
Talos E2E 测试——CI Debug 构建辅助脚本。

设计角色：
- 在 CI 构建热更 DLL 后，根据 TeamCity 构建参数决定是否为 Debug 构建。
- Debug 构建时：创建 DEBUG 标记文件，开启 ENABLE_DEBUG / ENABLE_E2ETEST 宏。
- Release 构建时：不创建标记，不开启额外宏。

使用方式（在 TeamCity 或命令行中）：
    python debug_build_helper.py \\
        --hotfix-dir /path/to/script/hotfix \\
        --debug-build true \\
        --output-dir /path/to/ci/output

TeamCity 参数：
    - build.debugBuild (true/false) — 是否为 Debug 构建
"""

import argparse
import os
import sys
import json
from pathlib import Path
from datetime import datetime, timezone


def create_debug_marker(hotfix_dir: str) -> None:
    """
    在热更脚本目录下创建 DEBUG 标记文件。
    
    参数:
        hotfix_dir: 热更脚本目录路径
    """
    marker_path = os.path.join(hotfix_dir, "DEBUG")
    content = (
        f"Talos E2E Debug Build\n"
        f"Created: {datetime.now(timezone.utc).isoformat()}\n"
        f"WARNING: This build is NOT for release distribution.\n"
    )
    
    os.makedirs(hotfix_dir, exist_ok=True)
    with open(marker_path, "w", encoding="utf-8") as f:
        f.write(content)
    
    print(f"[TalosE2E] DEBUG 标记已创建: {marker_path}")


def remove_debug_marker(hotfix_dir: str) -> None:
    """
    移除热更脚本目录下的 DEBUG 标记文件。
    
    参数:
        hotfix_dir: 热更脚本目录路径
    """
    marker_path = os.path.join(hotfix_dir, "DEBUG")
    if os.path.exists(marker_path):
        os.remove(marker_path)
        print(f"[TalosE2E] DEBUG 标记已移除: {marker_path}")


def write_build_info(output_dir: str, is_debug: bool, build_number: str = "") -> None:
    """
    写入构建元信息 JSON 文件，记录 Debug/Release 状态。
    
    参数:
        output_dir: CI 输出目录
        is_debug: 是否为 Debug 构建
        build_number: 构建号
    """
    info = {
        "isDebugBuild": is_debug,
        "buildNumber": build_number,
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "e2eTestEnabled": is_debug,
    }
    
    os.makedirs(output_dir, exist_ok=True)
    info_path = os.path.join(output_dir, "talos_build_info.json")
    with open(info_path, "w", encoding="utf-8") as f:
        json.dump(info, f, indent=2, ensure_ascii=False)
    
    print(f"[TalosE2E] 构建信息已写入: {info_path}")
    print(f"[TalosE2E]   isDebugBuild: {is_debug}")
    print(f"[TalosE2E]   e2eTestEnabled: {is_debug}")


def main():
    """主入口函数。"""
    parser = argparse.ArgumentParser(description="Talos E2E Debug 构建辅助工具")
    parser.add_argument(
        "--hotfix-dir",
        required=True,
        help="热更脚本目录路径（script/hotfix）",
    )
    parser.add_argument(
        "--debug-build",
        default="false",
        choices=["true", "false"],
        help="是否为 Debug 构建 (true/false)",
    )
    parser.add_argument(
        "--output-dir",
        default="",
        help="CI 输出目录（用于写入构建元信息）",
    )
    parser.add_argument(
        "--build-number",
        default="",
        help="构建号",
    )
    
    args = parser.parse_args()
    is_debug = args.debug_build.lower() == "true"
    
    print(f"[TalosE2E] Debug 构建: {is_debug}")
    
    if is_debug:
        # Phase 1: 创建 DEBUG 标记
        create_debug_marker(args.hotfix_dir)
        
        # Phase 2: 写入构建元信息
        if args.output_dir:
            write_build_info(args.output_dir, is_debug, args.build_number)
        
        print("[TalosE2E] Debug 构建准备完成")
        print("[TalosE2E] 注意: 此构建包含 E2E 测试能力，不可用于 Release 发布！")
    else:
        # 确保无 DEBUG 标记
        remove_debug_marker(args.hotfix_dir)
        
        if args.output_dir:
            write_build_info(args.output_dir, is_debug, args.build_number)
        
        print("[TalosE2E] Release 构建准备完成")


if __name__ == "__main__":
    main()
