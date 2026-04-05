"""Deprecated compatibility module.

历史上这里承载了统一的执行流程，但现在 CI 规范调整为：
1. `build_android.py` / `build_ios.py` / `build_windows.py` 才是各自平台的执行主体。
2. 通用的 Unity BatchMode 调用能力被收敛到 `unity3d_batchmode.py`。

保留本文件仅用于给维护者明确迁移方向，避免继续把业务流程堆到一个 generic common 中。
"""


def __getattr__(name: str):
    raise RuntimeError(
        "common.py is deprecated. "
        "Use build_android.py / build_ios.py / build_windows.py as the entry flow, "
        "and use unity3d_batchmode.py only for shared Unity batchmode helpers. "
        f"Requested attribute: {name}"
    )


if __name__ == "__main__":
    raise SystemExit(
        "common.py is deprecated. Run build_android.py / build_ios.py / build_windows.py instead."
    )

