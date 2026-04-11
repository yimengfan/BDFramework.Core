"""BuildTools 测试套件共享的 pytest 配置文件。

职责：
- 注册 --run-remote-artifact-tests CLI 开关，用于控制是否执行需要真实文件服务器的远程集成测试。
- 注册 remote_artifact 标记，使 pytest 能识别并过滤远程产物上传测试用例。

使用方式：
    # 跳过远程测试（默认行为）
    pytest DevOps/CI/BuildTools/tests/

    # 启用远程测试（需要 buildtools.toml 中 [tests.remote_artifact].enabled = true）
    pytest DevOps/CI/BuildTools/tests/ --run-remote-artifact-tests
"""

from __future__ import annotations

import sys
from pathlib import Path


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))


def pytest_addoption(parser) -> None:
    """注册 --run-remote-artifact-tests CLI 开关。

    启用后会运行向真实文件服务器上传文件的远程集成测试。
    同时要求 buildtools.toml 中 [tests.remote_artifact].enabled = true。
    """
    parser.addoption(
        "--run-remote-artifact-tests",
        action="store_true",
        default=False,
        help="run integration tests that upload real files to the configured remote artifact file server; also requires [tests.remote_artifact].enabled = true in buildtools.toml",
    )


def pytest_configure(config) -> None:
    """注册 remote_artifact 自定义标记，用于标记远程产物上传集成测试。"""
    config.addinivalue_line(
        "markers",
        "remote_artifact: integration test that uploads to the configured remote artifact file server",
    )